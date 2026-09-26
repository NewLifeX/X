using System.Buffers;
using System.ComponentModel;
using NewLife;
using NewLife.Data;
using NewLife.Messaging;
using Xunit;

namespace XUnitTest.Messaging;

/// <summary>消息帧泵（MessagePump）与标准消息编解码器（SrmpCodec）测试</summary>
public class MessagePumpTests
{
    #region 工具
    /// <summary>构造标准消息帧（4/8字节头 + 负载）</summary>
    private static Byte[] BuildFrame(Byte[] payload, Byte sequence = 0x66, Byte flag = 0x01, Byte mode = 0)
    {
        var headerSize = payload.Length < 0xFFFF ? 4 : 8;
        var buf = new Byte[headerSize + payload.Length];
        buf[0] = (Byte)((mode << 6) | (flag & 0x3F));
        buf[1] = sequence;
        if (headerSize == 4)
        {
            buf[2] = (Byte)(payload.Length & 0xFF);
            buf[3] = (Byte)(payload.Length >> 8);
        }
        else
        {
            buf[2] = 0xFF;
            buf[3] = 0xFF;
            buf[4] = (Byte)(payload.Length & 0xFF);
            buf[5] = (Byte)((payload.Length >> 8) & 0xFF);
            buf[6] = (Byte)((payload.Length >> 16) & 0xFF);
            buf[7] = (Byte)((payload.Length >> 24) & 0xFF);
        }
        payload.CopyTo(buf, headerSize);

        return buf;
    }

    private static readonly SrmpCodec _codec = new();

    private static MessagePump NewPump() => new(_codec);

    /// <summary>测试用行协议：CRLF 分隔；空行与 # 注释行为“无消息帧”（跳过），数据行以内容为体，分隔符留给下一帧跳过</summary>
    private sealed class LineCodec : IMessageCodec
    {
        public ParseResult? TryParse(ReadOnlySequence<Byte> buffer)
        {
            // 跨段扫描 CRLF
            var reader = new SequenceReader<Byte>(buffer);
            Int64 pos = 0;
            while (reader.TryRead(out var b))
            {
                if (b == (Byte)'\r')
                {
                    // 行尾需要 \n；未到达则等更多数据（不消费）
                    if (!reader.TryPeek(out var b2)) return null;
                    if (b2 != (Byte)'\n')
                    {
                        pos++;
                        continue;
                    }

                    // 行内容 = [0, pos)；空行与 # 开头为无消息帧
                    var first = buffer.First.Span;
                    var skip = pos == 0 || (first.Length > 0 && first[0] == (Byte)'#');
                    if (skip) return new ParseResult { HeaderSize = (Int32)(pos + 2) };

                    // 数据行：内容为体；CRLF 留给下一帧（作为无消息帧跳过）
                    return new ParseResult { Message = new Message(), HeaderSize = 0, BodyLength = pos };
                }
                pos++;
            }

            return null;
        }

        public IOwnerPacket? Build(IMessage message) => throw new NotSupportedException("测试协议仅验证接收路径");

        public IOwnerPacket BuildHeader(IMessage message, Int64 bodyLength) => throw new NotSupportedException("测试协议仅验证接收路径");
    }
    #endregion

    #region 定界与构造
    [Fact]
    [DisplayName("消息编解码_4字节头_定界并构造消息")]
    public void TryParse_4ByteHeader()
    {
        var payload = new Byte[] { 0xAA, 0xBB, 0xCC };
        var frame = BuildFrame(payload, 0x21, flag: 0x03, mode: 2);

        var rs = _codec.TryParse(new ArrayPacket(frame).AsReadOnlySequence());
        Assert.NotNull(rs);
        Assert.Equal(4, rs.Value.HeaderSize);
        Assert.Equal(3L, rs.Value.BodyLength);

        var msg = rs.Value.Message;
        var dm = Assert.IsType<DefaultMessage>(msg);
        Assert.Equal(MessageKinds.Response, dm.Kind);
        Assert.Equal(0x03, dm.Flag);
        Assert.Equal(0x21, dm.Sequence);

        // 单向模式
        var rs1 = _codec.TryParse(new ArrayPacket(BuildFrame(new Byte[] { 1 }, 0x10, mode: 1)).AsReadOnlySequence());
        var m1 = Assert.IsType<DefaultMessage>(rs1!.Value.Message);
        Assert.Equal(MessageKinds.OneWay, m1.Kind);

        // 响应+错误模式
        var rs3 = _codec.TryParse(new ArrayPacket(BuildFrame(new Byte[] { 1 }, 0x10, mode: 3)).AsReadOnlySequence());
        var m3 = Assert.IsType<DefaultMessage>(rs3!.Value.Message);
        Assert.Equal(MessageKinds.Error, m3.Kind);
    }

    [Fact]
    [DisplayName("消息编解码_8字节扩展头_定界与0xFFFF边界")]
    public void TryParse_8ByteExtHeader()
    {
        // 0xFFFF 边界起用 8 字节扩展头
        var payload = new Byte[0xFFFF];
        var frame = BuildFrame(payload);
        Assert.Equal(8 + payload.Length, frame.Length);

        var rs = _codec.TryParse(new ArrayPacket(frame).AsReadOnlySequence());
        Assert.NotNull(rs);
        Assert.Equal(8, rs.Value.HeaderSize);
        Assert.Equal(0xFFFFL, rs.Value.BodyLength);

        // 大于 64k
        var big = new Byte[70_000];
        var frame2 = BuildFrame(big);
        var rs2 = _codec.TryParse(new ArrayPacket(frame2).AsReadOnlySequence());
        Assert.NotNull(rs2);
        Assert.Equal(8, rs2.Value.HeaderSize);
        Assert.Equal(70_000L, rs2.Value.BodyLength);
    }

    [Fact]
    [DisplayName("消息编解码_头部不足_不消费不产生对象")]
    public void TryParse_HeaderInsufficient_NoObject()
    {
        // 4字节头不足（仅 3 字节）
        var frame = BuildFrame(new Byte[] { 1, 2, 3 });
        Assert.Null(_codec.TryParse(new ArrayPacket(frame).AsReadOnlySequence(0, 3)));

        // 扩展头不足（声明 0xFFFF 但仅 6 字节）
        var big = BuildFrame(new Byte[70_000]);
        Assert.Null(_codec.TryParse(new ArrayPacket(big).AsReadOnlySequence(0, 6)));

        // 空窗口
        Assert.Null(_codec.TryParse(ReadOnlySequence<Byte>.Empty));
    }

    [Fact]
    [DisplayName("消息编解码_扩展长度负数_标记损坏帧")]
    public void TryParse_NegativeLength_Rejected()
    {
        var frame = new Byte[8 + 10];
        frame[0] = 0x01;
        frame[1] = 0x01;
        frame[2] = 0xFF;
        frame[3] = 0xFF;
        // 扩展长度 0x80000001（负数）
        frame[4] = 0x01;
        frame[5] = 0x00;
        frame[6] = 0x00;
        frame[7] = 0x80;

        // 头部已完整而长度非法：标记损坏帧（帧泵据此立即报错），不产生消息
        var rs = _codec.TryParse(new ArrayPacket(frame).AsReadOnlySequence());
        Assert.True(rs!.Value.Invalid);
        Assert.Null(rs.Value.Message);
    }

    [Fact]
    [DisplayName("消息编解码_头部跨段_链式序列正常解析")]
    public void TryParse_ChainedHeader()
    {
        var payload = new Byte[70_000];
        var frame = BuildFrame(payload, 0x5A, mode: 2);

        // 8 字节头跨 3 段（2+3+3）
        var seq = new ArrayPacket(frame, 0, 2)
        {
            Next = new ArrayPacket(frame, 2, 3) { Next = new ArrayPacket(frame, 5) }
        }.AsReadOnlySequence();

        var rs = _codec.TryParse(seq);
        Assert.NotNull(rs);
        Assert.Equal(8, rs.Value.HeaderSize);
        Assert.Equal(70_000L, rs.Value.BodyLength);
        var dm = Assert.IsType<DefaultMessage>(rs.Value.Message);
        Assert.Equal(MessageKinds.Response, dm.Kind);
        Assert.Equal(0x5A, dm.Sequence);
    }
    #endregion

    #region 帧泵读取
    [Fact]
    [DisplayName("帧泵_整帧到达_快路径内存体零拷贝")]
    public void TryRead_WholeFrame_FastPath()
    {
        using var pipe = new Pipe();
        var pump = NewPump();

        var payload = new Byte[] { 9, 8, 7, 6, 5 };
        var frame = BuildFrame(payload, 0x11);
        pipe.Writer.Append(new ArrayPacket(frame));

        Assert.True(pump.TryRead(pipe.Reader, out var msg));
        Assert.NotNull(msg);
        Assert.Equal(0x11, ((DefaultMessage)msg!).Sequence);

        // 快路径：体为内存视图（非流式），零拷贝可用
        var body = msg!.Body!;
        Assert.False(body.IsStreaming);
        Assert.Equal(payload.Length, body.Remaining);
        Assert.Equal(payload, msg.Payload!.AsReadOnlySequence().ToArray());

        // 头与体均已切出，窗口全部消费
        Assert.Equal(0, pipe.UnconsumedLength);

        msg.Dispose();
    }

    [Fact]
    [DisplayName("帧泵_头部到齐即交付_负载流式读取")]
    public async Task TryRead_HeaderOnly_SlowPath()
    {
        using var pipe = new Pipe();
        var pump = NewPump();

        var payload = new Byte[5_000];
        for (var i = 0; i < payload.Length; i++) payload[i] = (Byte)(i * 31);
        var frame = BuildFrame(payload, 0x33);

        // 首轮：仅头部 + 96 字节负载到达
        pipe.Writer.Append(new ArrayPacket(frame[..100]));

        // 头到齐即可交付，不必等整帧
        Assert.True(pump.TryRead(pipe.Reader, out var msg));
        Assert.NotNull(msg);
        Assert.Equal(0x33, ((DefaultMessage)msg!).Sequence);

        var body = msg!.Body!;
        Assert.True(body.IsStreaming);
        Assert.Equal(payload.Length, body.Remaining);

        // 后台分块续传剩余负载
        var feed = Task.Run(() =>
        {
            for (var i = 100; i < frame.Length; i += 4096)
            {
                var count = Math.Min(4096, frame.Length - i);
                pipe.Writer.Append(new ArrayPacket(frame[i..(i + count)]));
            }
        });

        // 流式读满
        var all = await body.ReadAllAsync();
        await feed;

        Assert.Equal(payload, all.AsReadOnlySequence().ToArray());
        Assert.Equal(0, body.Remaining);
        Assert.Equal(0, pipe.UnconsumedLength);

        all.TryDispose();
        msg.Dispose();
    }

    [Fact]
    [DisplayName("帧泵_头部不足_窗口不动等追加")]
    public void TryRead_HeaderInsufficient_WindowKeeps()
    {
        using var pipe = new Pipe();
        var pump = NewPump();

        var frame = BuildFrame(new Byte[] { 1, 2, 3 });
        pipe.Writer.Append(new ArrayPacket(frame, 0, 3));

        Assert.False(pump.TryRead(pipe.Reader, out var msg));
        Assert.Null(msg);
        Assert.Equal(3, pipe.UnconsumedLength);

        // 补齐后成功
        pipe.Writer.Append(new ArrayPacket(frame, 3));
        Assert.True(pump.TryRead(pipe.Reader, out var msg2));
        Assert.NotNull(msg2);
        Assert.Equal(3, msg2!.Body!.Remaining);
        Assert.Equal(0, pipe.UnconsumedLength);

        msg2.Dispose();
    }

    [Fact]
    [DisplayName("帧泵_异步读取_等待数据后返回消息")]
    public async Task ReadAsync_WaitsThenReturns()
    {
        using var pipe = new Pipe();
        var pump = NewPump();

        var frame = BuildFrame(new Byte[] { 1, 2, 3, 4 }, 0x44);

        // 先发起读取（无数据挂起），再投递数据
        var task = pump.ReadAsync(pipe.Reader);
        Assert.False(task.IsCompleted);

        pipe.Writer.Append(new ArrayPacket(frame));

        var msg = await task;
        Assert.NotNull(msg);
        Assert.Equal(0x44, ((DefaultMessage)msg!).Sequence);
        Assert.Equal(4, msg!.Body!.Remaining);

        msg.Dispose();
    }

    [Fact]
    [DisplayName("帧泵_流结束_返回空或丢弃残片")]
    public async Task ReadAsync_Completed_ReturnsNull()
    {
        var pump = NewPump();

        // 空流完成
        using (var pipe = new Pipe())
        {
            pipe.Writer.Complete();
            Assert.Null(await pump.ReadAsync(pipe.Reader));
        }

        // 残片无法成帧
        using (var pipe = new Pipe())
        {
            pipe.Writer.Append(new ArrayPacket(new Byte[] { 1, 2 }));
            pipe.Writer.Complete();
            Assert.Null(await pump.ReadAsync(pipe.Reader));
        }
    }

    [Fact]
    [DisplayName("帧泵_丢弃未读负载_对齐下一帧")]
    public async Task Discard_BodyAlignsNextFrame()
    {
        using var pipe = new Pipe();
        var pump = NewPump();

        var p1 = new Byte[5_000];
        var p2 = new Byte[] { 0x5A, 0x5B };
        for (var i = 0; i < p1.Length; i++) p1[i] = 0x11;
        var f1 = BuildFrame(p1);
        var f2 = BuildFrame(p2, 0x22);

        // f1 头部 + 96 字节负载到达
        pipe.Writer.Append(new ArrayPacket(f1[..100]));
        Assert.True(pump.TryRead(pipe.Reader, out var msg1));
        Assert.True(msg1!.Body!.IsStreaming);

        // 不读负载：剩余 f1 + 整个 f2 一次到达，丢弃对齐
        var rest = new Byte[f1.Length - 100 + f2.Length];
        Array.Copy(f1, 100, rest, 0, f1.Length - 100);
        Array.Copy(f2, 0, rest, f1.Length - 100, f2.Length);
        pipe.Writer.Append(new ArrayPacket(rest));

        await MessagePump.DiscardAsync(msg1);
        Assert.Equal(0, msg1.Body!.Remaining);
        msg1.Dispose();

        // 下一帧对齐可读
        Assert.True(pump.TryRead(pipe.Reader, out var msg2));
        Assert.Equal(0x22, ((DefaultMessage)msg2!).Sequence);
        Assert.Equal(2, msg2!.Body!.Remaining);
        msg2.Dispose();
    }

    [Fact]
    [DisplayName("帧泵_跳过无消息帧_继续解析下一帧")]
    public async Task TryRead_SkipFrames_ContinuesNext()
    {
        var pump = new MessagePump(new LineCodec());
        using var pipe = new Pipe();

        // 空行 + 注释行 + 数据行
        pipe.Writer.Append(new ArrayPacket("# comment\r\n\r\nhello\r\n".GetBytes()));

        Assert.True(pump.TryRead(pipe.Reader, out var msg));
        Assert.NotNull(msg);

        var body = await msg!.Body!.ReadAllAsync();
        Assert.Equal("hello".GetBytes(), body.AsReadOnlySequence().ToArray());
        body.TryDispose();

        // 尾部分隔符（下一轮的无消息帧）在再次读取时被跳过
        Assert.False(pump.TryRead(pipe.Reader, out _));
        Assert.Equal(0, pipe.UnconsumedLength);

        msg.Dispose();
    }

    [Fact]
    [DisplayName("帧泵_跳过帧不完整_等待补齐")]
    public void TryRead_SkipIncomplete_Waits()
    {
        var pump = new MessagePump(new LineCodec());
        using var pipe = new Pipe();

        // 注释行未结束：无法定界，窗口保持
        pipe.Writer.Append(new ArrayPacket("# com".GetBytes()));
        Assert.False(pump.TryRead(pipe.Reader, out _));
        Assert.Equal(5, pipe.UnconsumedLength);

        // 补齐后：注释行被跳过，数据行产出
        pipe.Writer.Append(new ArrayPacket("ment\r\ndata\r\n".GetBytes()));
        Assert.True(pump.TryRead(pipe.Reader, out var msg));
        Assert.NotNull(msg);
        msg!.Dispose();
    }

    [Fact]
    [DisplayName("帧泵_跳过帧_行尾跨段正常解析")]
    public void TryRead_SkipCrlfAcrossSegments()
    {
        var pump = new MessagePump(new LineCodec());
        using var pipe = new Pipe();

        // \r 已到达、\n 未到：等待（不消费）
        pipe.Writer.Append(new ArrayPacket("# x\r".GetBytes()));
        Assert.False(pump.TryRead(pipe.Reader, out _));

        // \n 到达后跳过注释行，数据行产出
        pipe.Writer.Append(new ArrayPacket("\ndata\r\n".GetBytes()));
        Assert.True(pump.TryRead(pipe.Reader, out var msg));
        Assert.NotNull(msg);
        msg!.Dispose();
    }
    #endregion

    #region 构建
    [Fact]
    [DisplayName("消息编解码_整帧构建_不消费负载与往返")]
    public void Build_FrameRoundTrip()
    {
        var payload = new Byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var msg = new DefaultMessage { Sequence = 0x66 };
        msg.SetBody(new ArrayPacket(payload));

        var frame = _codec.Build(msg);
        Assert.NotNull(frame);

        // 构建不消费消息负载：消息仍持有原体
        Assert.NotNull(msg.Payload);

        // 解析回读
        var rs = _codec.TryParse(frame!.AsReadOnlySequence());
        Assert.NotNull(rs);
        Assert.Equal(4, rs.Value.HeaderSize);
        Assert.Equal(payload.Length, rs.Value.BodyLength);
        Assert.Equal(0x66, ((DefaultMessage)rs.Value.Message!).Sequence);

        // 帧泵整帧快路径体内容一致
        using var pipe = new Pipe();
        pipe.Writer.Append(frame);
        Assert.True(NewPump().TryRead(pipe.Reader, out var msg3));
        Assert.Equal(payload, msg3!.Payload!.AsReadOnlySequence().ToArray());
        msg3.Dispose();
    }

    [Fact]
    [DisplayName("消息编解码_流式体_整帧构建抛异常")]
    public async Task Build_StreamingBody_Throws()
    {
        using var pipe = new Pipe();
        var frame = BuildFrame(new Byte[100]);

        // 仅到达部分：帧未完整 → 体为流式
        pipe.Writer.Append(new ArrayPacket(frame, 0, 50));

        var pump = NewPump();
        Assert.True(pump.TryRead(pipe.Reader, out var msg));
        Assert.True(msg!.Body!.IsStreaming);

        Assert.Throws<InvalidOperationException>(() => _codec.Build(msg));

        // 补投剩余并丢弃未读体收尾
        pipe.Writer.Append(new ArrayPacket(frame, 50));
        await MessagePump.DiscardAsync(msg);
        msg.Dispose();
    }

    [Fact]
    [DisplayName("消息编解码_头部构建_4或8字节边界")]
    public void BuildHeader_SizeBoundary()
    {
        var msg = new DefaultMessage { Sequence = 0x77 };

        // 小负载：4 字节头
        var pk = _codec.BuildHeader(msg, 100);
        Assert.Equal(4, pk.Total);
        var rs1 = _codec.TryParse(pk.AsReadOnlySequence());
        Assert.NotNull(rs1);
        Assert.Equal(4, rs1.Value.HeaderSize);
        Assert.Equal(100L, rs1.Value.BodyLength);
        Assert.Equal(0x77, ((DefaultMessage)rs1.Value.Message!).Sequence);
        pk.TryDispose();

        // 0xFFFF 边界：8 字节扩展头
        pk = _codec.BuildHeader(msg, 0xFFFF);
        Assert.Equal(8, pk.Total);
        var rs2 = _codec.TryParse(pk.AsReadOnlySequence());
        Assert.NotNull(rs2);
        Assert.Equal(8, rs2.Value.HeaderSize);
        Assert.Equal(0xFFFFL, rs2.Value.BodyLength);
        pk.TryDispose();

        // 非法长度
        Assert.Throws<ArgumentOutOfRangeException>(() => _codec.BuildHeader(msg, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => _codec.BuildHeader(msg, Int32.MaxValue + 1L));

        // 类型守卫
        Assert.Throws<ArgumentException>(() => _codec.BuildHeader(new Message(), 10));
    }

    [Fact]
    [DisplayName("消息编解码_头部包加流式体_组装为完整帧")]
    public async Task BuildHeader_PlusStream_RoundTrip()
    {
        using var pipe = new Pipe();
        var pump = NewPump();

        var payload = new Byte[300_000];
        for (var i = 0; i < payload.Length; i++) payload[i] = (Byte)(i * 7);

        // 发送侧：先发头声明长度
        var msg = new DefaultMessage { Sequence = 0x5A };
        pipe.Writer.Append(_codec.BuildHeader(msg, payload.Length));

        // 接收：头部到齐即交付（扩展头 8 字节）
        Assert.True(pump.TryRead(pipe.Reader, out var recv));
        Assert.Equal(0x5A, ((DefaultMessage)recv!).Sequence);
        Assert.True(recv!.Body!.IsStreaming);

        // 发送侧：负载分块流式追加
        var feed = Task.Run(() =>
        {
            for (var i = 0; i < payload.Length; i += 16384)
            {
                var count = Math.Min(16384, payload.Length - i);
                pipe.Writer.Append(new ArrayPacket(payload[i..(i + count)]));
            }
        });

        var all = await recv.Body.ReadAllAsync();
        await feed;

        Assert.Equal(payload, all.AsReadOnlySequence().ToArray());
        all.TryDispose();
        recv.Dispose();
    }
    #endregion

    #region 防护
    [Fact]
    [DisplayName("帧泵_损坏帧_立即报协议错误")]
    public async Task ReadAsync_CorruptFrame_Throws()
    {
        using var pipe = new Pipe();
        var pump = NewPump();

        // 0xFF 序列：0xFFFF 扩展头声明负数长度，头部完整即判定损坏。
        // 旧行为把它当作“无法定界”一直等待到残余上限，连接在此期间无法恢复
        var garbage = new Byte[64];
        for (var i = 0; i < garbage.Length; i++) garbage[i] = 0xFF;
        pipe.Writer.Append(new ArrayPacket(garbage));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => pump.ReadAsync(pipe.Reader).AsTask());
        Assert.Contains("协议帧损坏", ex.Message);
    }

    /// <summary>永不定界的协议：用于验证残余上限防护（不依赖具体协议的损坏特例）</summary>
    private sealed class NeverDelimitsCodec : IMessageCodec
    {
        public ParseResult? TryParse(ReadOnlySequence<Byte> buffer) => null;

        public IOwnerPacket? Build(IMessage message) => null;

        public IOwnerPacket BuildHeader(IMessage message, Int64 bodyLength) => new OwnerPacket(0);
    }

    [Fact]
    [DisplayName("帧泵_无法定界残余超限_报协议错误")]
    public async Task ReadAsync_ExceedsMaxCache_Throws()
    {
        using var pipe = new Pipe();
        var pump = new MessagePump(new NeverDelimitsCodec()) { MaxCache = 32 };

        // 残余达到上限（不依赖报文内容的特殊形式）
        pipe.Writer.Append(new ArrayPacket(new Byte[64]));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => pump.ReadAsync(pipe.Reader).AsTask());
        Assert.Contains("无法定界", ex.Message);
    }

    [Fact]
    [DisplayName("帧泵_等待中残余增长_超限时报协议错误")]
    public async Task ReadAsync_GrowingResidue_Throws()
    {
        using var pipe = new Pipe();
        var pump = NewPump();
        pump.MaxCache = 64;

        // 头不齐（3 字节）：等待
        pipe.Writer.Append(new ArrayPacket(new Byte[] { 0xFF, 0xFF, 0xFF }));
        var task = pump.ReadAsync(pipe.Reader).AsTask();
        Assert.False(task.IsCompleted);

        // 追加损坏数据：残余超过上限，等待方被唤醒并报错
        var more = new Byte[61];
        for (var i = 0; i < more.Length; i++) more[i] = 0xFF;
        pipe.Writer.Append(new ArrayPacket(more));

        await Assert.ThrowsAsync<InvalidOperationException>(() => task);
    }

    [Fact]
    [DisplayName("帧泵_大帧流式_不受最大缓存误报")]
    public async Task ReadAsync_LargeStreaming_NoFalsePositive()
    {
        using var pipe = new Pipe();
        var pump = NewPump();
        pump.MaxCache = 1024;    // 远小于帧长

        // 仅头部（8 字节扩展头声明 300KB）：头部到齐即有进展，体流式，不触发防护
        var msg = new DefaultMessage { Sequence = 0x6B };
        pipe.Writer.Append(_codec.BuildHeader(msg, 300_000));

        var recv = await pump.ReadAsync(pipe.Reader);
        Assert.NotNull(recv);
        Assert.True(recv!.Body!.IsStreaming);
        recv.Dispose();
    }

    [Fact]
    [DisplayName("帧泵_整帧模式_体未齐不产出")]
    public void TryRead_RequireFullFrame()
    {
        using var pipe = new Pipe();
        var pump = NewPump();
        pump.RequireFullFrame = true;

        var frame = BuildFrame(new Byte[10]);

        // 头 + 部分体：不产出、不消费（窗口原样留给下一轮）
        pipe.Writer.Append(new ArrayPacket(frame[..6]));
        Assert.False(pump.TryRead(pipe.Reader, out _));
        Assert.Equal(6, pipe.UnconsumedLength);

        // 余下体到齐：整帧产出（体为内存视图）
        pipe.Writer.Append(new ArrayPacket(frame[6..]));
        Assert.True(pump.TryRead(pipe.Reader, out var msg));
        Assert.False(msg!.Body!.IsStreaming);
        Assert.Equal(10, msg.Payload!.Total);
        msg.Dispose();
    }

    [Fact]
    [DisplayName("帧泵_整帧模式_帧长超上限_抛异常")]
    public void TryRead_RequireFullFrame_ExceedsMaxFrameSize_Throws()
    {
        using var pipe = new Pipe();
        var pump = NewPump();
        pump.RequireFullFrame = true;
        pump.MaxFrameSize = 64;

        // 声明 4 字节头 + 1000 字节体，实际只发头部：整帧模式不消费未完整帧，
        // 若不设上限，对端仅凭这一条声明就能让本连接的内存无限增长
        var frame = BuildFrame(new Byte[1000]);
        pipe.Writer.Append(new ArrayPacket(frame[..4]));

        var ex = Assert.Throws<InvalidOperationException>(() => pump.TryRead(pipe.Reader, out _));
        Assert.Contains("超过上限", ex.Message);
        Assert.Equal(4, pipe.UnconsumedLength);

        // 上限之内的未完整帧仍按“等待后续分片”处理
        pump.MaxFrameSize = 4096;
        Assert.False(pump.TryRead(pipe.Reader, out _));
    }

    [Fact]
    [DisplayName("帧泵_整帧模式_大帧分批到达_不受最大缓存误报")]
    public async Task ReadAsync_RequireFullFrame_LargeFrame_NoMaxCacheFalsePositive()
    {
        using var pipe = new Pipe();
        var pump = NewPump();
        pump.RequireFullFrame = true;
        pump.MaxCache = 256;                 // 远小于帧长
        pump.MaxFrameSize = 1024 * 1024;

        var frame = BuildFrame(new Byte[200_000], 0x6C);

        // 先写入不足整帧的一大段（已超过 MaxCache）：已定界但未完整
        pipe.Writer.Append(new ArrayPacket(frame[..(frame.Length - 1)]));

        var task = pump.ReadAsync(pipe.Reader).AsTask();
        Assert.False(task.IsCompleted);

        // 补上最后一字节：整帧到齐。旧实现把“已定界待整帧”当成无法定界的残余而误报
        pipe.Writer.Append(new ArrayPacket(frame[(frame.Length - 1)..]));

        var msg = await task;
        Assert.NotNull(msg);
        Assert.Equal(0x6C, ((DefaultMessage)msg!).Sequence);
        Assert.Equal(200_000, msg.Payload!.Total);
        msg.Dispose();
    }
    #endregion
}
