using System.ComponentModel;
using NewLife;
using NewLife.Data;
using NewLife.Messaging;
using NewLife.Net;
using NewLife.Net.Handlers;
using Xunit;

namespace XUnitTest.Net;

/// <summary>PacketCodec（简化模型：单缓存 + 共享切片切帧）跨轮粘包回归</summary>
/// <remarks>
/// 语义契约：
/// S1 整包恰一帧 → 共享切片返回；
/// S2 整包多完整帧 → 逐帧共享切片；
/// S3 完整帧+残片 → 帧切片 + 残片切片入缓存（引用计数持有）；
/// S4 残片续轮仍不足 → 残片切片入缓存；
/// S5 凑成一帧 → 跨段组链返回；定界由 GetLength 委托链感知完成（分隔符链内扫描，不合并段流）；
/// S6 数据到来时链头不足 HeadSize → 并入后续残片补齐后定界（头部恒连续，≤HeadSize 小拷贝）。
/// </remarks>
public class PacketCodecSplitTests
{
    private static PacketCodec CreateLengthCodec() => new()
    {
        GetLength = p => MessageCodec<DefaultMessage>.GetLength(p, 2, 2)
    };

    /// <summary>链内扫描 \r\n 的行长委托（与 SplitDataCodec 的链感知实现同构）</summary>
    private static Int32 LineLength(IPacket pk)
    {
        var prev = -1;
        var pos = 0;
        for (var node = pk; node != null; node = node.Next)
        {
            var span = node.GetSpan();
            for (var i = 0; i < span.Length; i++, pos++)
            {
                // \n 且前一字节为 \r（前一字节可能落在上一节点末尾）
                if (span[i] == (Byte)'\n' && (i > 0 ? span[i - 1] == (Byte)'\r' : prev == (Byte)'\r'))
                    return pos + 1;

                prev = span[i];
            }
        }

        return 0;
    }

    /// <summary>模拟接收层每轮交给切帧层的拥有句柄（视图层句柄同样满足该契约，换缓冲后每块缓冲各不相同）</summary>
    private static OwnerPacket CreateRound(Byte[] buffer, Int32 offset, Int32 count)
    {
        var pk = new OwnerPacket(count);
        buffer.AsSpan(offset, count).CopyTo(pk.GetSpan());
        return pk;
    }

    private static Byte[] MakeFrame(Int32 payloadLen, Int32 seed)
    {
        var payload = new Byte[payloadLen];
        for (var i = 0; i < payload.Length; i++) payload[i] = (Byte)((i + seed) & 0xFF);

        var frame = new Byte[payloadLen + 4];
        frame[2] = (Byte)(payloadLen & 0xFF);
        frame[3] = (Byte)(payloadLen >> 8);
        payload.CopyTo(frame, 4);

        return frame;
    }

    /// <summary>S1：整包恰一帧 → 共享切片返回，与轮句柄各自释放</summary>
    [Fact]
    [DisplayName("S1 快路径：整包恰一帧共享切片返回")]
    public void FastPath_SingleFrame_OwnedRound()
    {
        var frame = MakeFrame(100, 1);
        var codec = CreateLengthCodec();

        // 模拟接收层：整轮直接包装为拥有句柄（引用计数 1）
        var round = CreateRound(frame, 0, frame.Length);
        Assert.Equal(1, round.RefCount);

        var frames = codec.Parse(round);

        Assert.Single(frames);
        Assert.Equal(frame, frames[0].ToArray());
        Assert.IsAssignableFrom<OwnerPacket>(frames[0]);
        Assert.Equal(2, round.RefCount);     // 本轮 + 帧切片

        frames[0].TryDispose();
        Assert.Equal(1, round.RefCount);     // 无共享切片带出 → 接收层据此判定复用

        round.TryDispose();
    }

    /// <summary>S1 视图输入：返回借阅视图，无需释放</summary>
    [Fact]
    [DisplayName("S1 视图输入：整包恰一帧返回借阅视图")]
    public void FastPath_SingleFrame_ViewInput()
    {
        var frame = MakeFrame(100, 1);
        var codec = CreateLengthCodec();

        var frames = codec.Parse(new ArrayPacket(frame));

        Assert.Single(frames);
        Assert.Equal(frame, frames[0].ToArray());
        Assert.IsNotType<OwnerPacket>(frames[0]);

        frames[0].TryDispose();
    }

    /// <summary>S2：整包多个完整帧 → 逐帧共享切片</summary>
    [Fact]
    [DisplayName("S2 多帧：逐帧共享切片")]
    public void FastPath_MultiFrame_Slices()
    {
        var f1 = MakeFrame(100, 1);
        var f2 = MakeFrame(200, 2);

        var seq = new Byte[f1.Length + f2.Length];
        f1.CopyTo(seq, 0);
        f2.CopyTo(seq, f1.Length);

        var codec = CreateLengthCodec();
        var round = CreateRound(seq, 0, seq.Length);
        var frames = codec.Parse(round);

        Assert.Equal(2, frames.Count);
        Assert.Equal(f1, frames[0].ToArray());
        Assert.Equal(f2, frames[1].ToArray());
        Assert.Equal(3, round.RefCount);     // 本轮 + 两个帧切片

        foreach (var f in frames) f.TryDispose();
        Assert.Equal(1, round.RefCount);     // 无共享切片带出 → 接收层据此判定复用

        round.TryDispose();
    }

    /// <summary>S3：完整帧+残片同轮 → 帧切片 + 残片切片入缓存跨轮累积</summary>
    [Fact]
    [DisplayName("S3 混合轮：帧切片返回，残片入缓存")]
    public void MixedRound_SliceAndCache()
    {
        var f1 = MakeFrame(100, 1);
        var f2 = MakeFrame(100, 2);

        // 第1轮：帧1完整 + 帧2头3字节
        var seq1 = new Byte[f1.Length + 3];
        f1.CopyTo(seq1, 0);
        f2.AsSpan(0, 3).CopyTo(seq1.AsSpan(f1.Length));

        var codec = CreateLengthCodec();
        var round1 = CreateRound(seq1, 0, seq1.Length);
        var frames1 = codec.Parse(round1);

        Assert.Single(frames1);
        Assert.Equal(f1, frames1[0].ToArray());
        Assert.Equal(3, round1.RefCount);     // 本轮 + 帧切片 + 残片切片
        frames1[0].TryDispose();
        Assert.Equal(2, round1.RefCount);     // 本轮 + 残片切片（缓存跨轮持有）

        // 轮末判定：残片被缓存带出 → 释放本引用并换新缓冲；旧缓冲由最后释放的切片归还
        round1.TryDispose();

        // 第2轮：帧2剩余 → 组链返回完整帧（独立新缓冲，与真实接收层换新一致）
        var round2 = CreateRound(f2, 3, f2.Length - 3);
        var frames2 = codec.Parse(round2);

        Assert.Single(frames2);
        Assert.Equal(f2.Length, frames2[0].Total);
        Assert.Equal(f2, frames2[0].ToArray());
        Assert.Equal(2, round2.RefCount);     // 本轮 + 帧链切片
        round2.TryDispose();

        frames2[0].TryDispose();
    }

    /// <summary>S4/S5：纯残片续轮切片入缓存；帧齐后组链返回，内容逐字节一致</summary>
    [Fact]
    [DisplayName("S4/S5 大帧跨轮：切片累积，帧齐组链返回")]
    public void SliceRounds_ChainFinal()
    {
        var frame = MakeFrame(5000, 3);
        var codec = CreateLengthCodec();

        var total = 0;
        var round = 0;
        IList<IPacket>? got = null;
        while (total < frame.Length)
        {
            var count = Math.Min(1000, frame.Length - total);
            var r = CreateRound(frame, total, count);
            var frames = codec.Parse(r);

            // 每轮残片都被缓存跨轮持有：计数大于 1 → 接收层解绑换新；这里释放本引用代表换新
            Assert.True(r.RefCount > 1, $"第{round + 1}轮应有残片被带出，RefCount={r.RefCount}");
            r.TryDispose();
            if (frames.Count > 0) got = frames;

            total += count;
            round++;
        }

        Assert.True(round > 1, $"分轮次数={round}");
        Assert.NotNull(got);
        Assert.Single(got!);
        Assert.Equal(frame.Length, got![0].Total);
        Assert.Equal(frame, got![0].ToArray());

        got![0].TryDispose();
    }

    /// <summary>视图输入：残片拷贝进段链，跨轮仍能正确组帧</summary>
    [Fact]
    [DisplayName("视图输入：残片拷贝累积，跨轮组帧")]
    public void ViewInput_CopyAccumulate()
    {
        var payload = new Byte[5000];
        Random.Shared.NextBytes(payload);

        var frame = new Byte[5004];
        frame[2] = (Byte)(5000 & 0xFF);
        frame[3] = (Byte)(5000 >> 8);
        payload.CopyTo(frame, 4);

        var codec = CreateLengthCodec();

        // 第1轮只给 3 字节（长度字段不完整），第2轮给剩余
        var frames1 = codec.Parse(new ArrayPacket(frame, 0, 3));
        Assert.Empty(frames1);

        var frames2 = codec.Parse(new ArrayPacket(frame, 3, frame.Length - 3));
        Assert.Single(frames2);
        Assert.Equal(frame.Length, frames2[0].Total);
        Assert.Equal(frame, frames2[0].ToArray());
        frames2[0].TryDispose();
    }

    /// <summary>大帧按 8192 分轮到达（视图输入），跨轮段链累积，最终整帧内容一致</summary>
    [Fact]
    [DisplayName("大帧 8192 分轮：段链累积整帧返回")]
    public void SplitRounds()
    {
        var payload = new Byte[65535];
        for (var i = 0; i < payload.Length; i++) payload[i] = (Byte)(i & 0xFF);

        var frame = new Byte[65539];
        frame[2] = 0xFF;
        frame[3] = 0xFF;
        payload.CopyTo(frame, 4);

        var codec = CreateLengthCodec();

        var total = 0;
        var round = 0;
        IList<IPacket>? got = null;
        while (total < frame.Length)
        {
            var count = Math.Min(8192, frame.Length - total);
            var mem = new Memory<Byte>(frame, total, count);
            var frames = codec.Parse(new ArrayPacket(frame, total, count));
            if (frames.Count > 0) got = frames;

            total += count;
            round++;
        }

        Assert.True(round > 1, $"分轮次数={round}");
        Assert.NotNull(got);
        Assert.Single(got!);
        Assert.Equal(frame.Length, got![0].Total);
        Assert.Equal(frame, got![0].ToArray());
        got![0].TryDispose();
    }

    /// <summary>分隔符扫描：分隔符横跨段边界或位于头段之后 → 链内扫描直接命中</summary>
    [Fact]
    [DisplayName("分隔符跨段：链内扫描切帧")]
    public void Delimiter_SplitAcrossBoundary()
    {
        // 第1轮：100 字节无分隔符残片
        var part1 = new Byte[100];
        for (var i = 0; i < part1.Length; i++) part1[i] = (Byte)'A';

        // 第2轮：分隔符 + 第二行
        var part2 = "\r\nrest\r\n".GetBytes();

        var codec = new PacketCodec { GetLength = LineLength };

        var frames1 = codec.Parse(new ArrayPacket(part1));
        Assert.Empty(frames1);

        var frames2 = codec.Parse(new ArrayPacket(part2));

        Assert.Equal(2, frames2.Count);
        Assert.Equal(new String('A', 100) + "\r\n", frames2[0].ToArray().ToStr());
        Assert.Equal("rest\r\n", frames2[1].ToArray().ToStr());

        foreach (var f in frames2) f.TryDispose();
    }

    /// <summary>视图输入多帧+残片：完整帧零拷贝（视图），残片跨轮转自有后继续组帧</summary>
    [Fact]
    [DisplayName("视图输入多帧：帧零拷贝，残片转自有后跨轮组帧")]
    public void ViewInput_MultiFrame_Residue()
    {
        var f1 = MakeFrame(100, 1);
        var f2 = MakeFrame(200, 2);
        var seq = new Byte[f1.Length + f2.Length];
        f1.CopyTo(seq, 0);
        f2.CopyTo(seq, f1.Length);

        var codec = CreateLengthCodec();

        // 第1轮：f1 完整 + f2 头3字节
        var frames1 = codec.Parse(new ArrayPacket(seq, 0, f1.Length + 3));
        Assert.Single(frames1);
        Assert.Equal(f1, frames1[0].ToArray());
        Assert.IsNotType<OwnerPacket>(frames1[0]);
        frames1[0].TryDispose();

        // 第2轮：f2 剩余 → 残片已转自有拷贝，与视图组链返回
        var frames2 = codec.Parse(new ArrayPacket(seq, f1.Length + 3, f2.Length - 3));
        Assert.Single(frames2);
        Assert.Equal(f2.Length, frames2[0].Total);
        Assert.Equal(f2, frames2[0].ToArray());
        frames2[0].TryDispose();
    }

    /// <summary>链式输入：多段数据合并缓存后正确切帧</summary>
    [Fact]
    [DisplayName("链式输入：多段合并后连续切帧")]
    public void ChainedInput_CutsFrames()
    {
        var f1 = MakeFrame(100, 1);
        var f2 = MakeFrame(200, 2);
        var seq = new Byte[f1.Length + f2.Length];
        f1.CopyTo(seq, 0);
        f2.CopyTo(seq, f1.Length);

        // 两个视图节点组链，节点边界落在帧中间
        var chain = new ArrayPacket(seq, 0, 50) { Next = new ArrayPacket(seq, 50, seq.Length - 50) };

        var codec = CreateLengthCodec();
        var frames = codec.Parse(chain);

        Assert.Equal(2, frames.Count);
        Assert.Equal(f1, frames[0].ToArray());
        Assert.Equal(f2, frames[1].ToArray());

        foreach (var f in frames) f.TryDispose();
    }

    /// <summary>长行跨多轮：委托链感知扫描全链，凑齐后零拷贝组链返回</summary>
    [Fact]
    [DisplayName("长行跨多轮：链内扫描定界后组链返帧")]
    public void Delimiter_LongLine_MultiRound()
    {
        var codec = new PacketCodec { GetLength = LineLength };

        // 前两轮全是行内容（无分隔符），第三轮补齐行尾与第二行
        var part1 = new Byte[200];
        for (var i = 0; i < part1.Length; i++) part1[i] = (Byte)'A';

        var frames1 = codec.Parse(new ArrayPacket(part1, 0, 100));
        Assert.Empty(frames1);
        var frames2 = codec.Parse(new ArrayPacket(part1, 100, 100));
        Assert.Empty(frames2);

        var frames3 = codec.Parse(new ArrayPacket("\r\nB\r\n".GetBytes()));
        Assert.Equal(2, frames3.Count);
        Assert.Equal(new String('A', 200) + "\r\n", frames3[0].ToArray().ToStr());
        Assert.Equal("B\r\n", frames3[1].ToArray().ToStr());

        foreach (var f in frames3) f.TryDispose();
    }

    /// <summary>帧头跨多节点：委托链感知拼接后正确切帧</summary>
    [Fact]
    [DisplayName("帧头跨3段：链感知定界后正确切帧")]
    public void Header_AcrossMultipleSegments()
    {
        var frame = MakeFrame(100, 1);
        var codec = CreateLengthCodec();

        // 拆成 3 段：2B + 2B + 剩余，帧头（4B 长度字段）横跨三段
        var frames1 = codec.Parse(new ArrayPacket(frame, 0, 2));
        Assert.Empty(frames1);

        var frames2 = codec.Parse(new ArrayPacket(frame, 2, 2));
        Assert.Empty(frames2);

        var frames3 = codec.Parse(new ArrayPacket(frame, 4, frame.Length - 4));
        Assert.Single(frames3);
        Assert.Equal(frame.Length, frames3[0].Total);
        Assert.Equal(frame, frames3[0].ToArray());
        frames3[0].TryDispose();
    }

    /// <summary>GetLength2 兼容：仅设置 span 委托（旧版 MQTT/RocketMQ/JT1078 模式），单段与跨轮残片均正确切帧</summary>
    [Fact]
    [DisplayName("GetLength2 兼容：span 委托单段与跨轮残片切帧")]
    public void GetLength2_SpanDelegate_Compat()
    {
        var f1 = MakeFrame(100, 1);
        var f2 = MakeFrame(200, 2);
        var seq = new Byte[f1.Length + f2.Length];
        f1.CopyTo(seq, 0);
        f2.CopyTo(seq, f1.Length);

#pragma warning disable CS0618 // 兼容旧版二进制的 span 委托
        var codec = new PacketCodec
        {
            GetLength2 = span => MessageCodec<DefaultMessage>.GetLength(span, 2, 2)
        };
#pragma warning restore CS0618 // 兼容旧版二进制的 span 委托

        // 单段：整包多帧
        var round = CreateRound(seq, 0, seq.Length);
        var frames = codec.Parse(round);
        Assert.Equal(2, frames.Count);
        Assert.Equal(f1, frames[0].ToArray());
        Assert.Equal(f2, frames[1].ToArray());
        foreach (var item in frames) item.TryDispose();
        round.TryDispose();

        // 首轮仅半截帧：残片入缓存；次轮跨段拼成完整帧（span 委托路径拼接连续片段）
        var half = f1.Length / 2;
        var r1 = CreateRound(seq, 0, half);
        Assert.Empty(codec.Parse(r1));
        r1.TryDispose();

        var r2 = CreateRound(seq, half, seq.Length - half);
        var frames2 = codec.Parse(r2);
        Assert.Equal(2, frames2.Count);
        Assert.Equal(f1, frames2[0].ToArray());
        Assert.Equal(f2, frames2[1].ToArray());
        foreach (var item in frames2) item.TryDispose();
        r2.TryDispose();

        codec.Dispose();
    }

    /// <summary>GetLength2 兼容：同时设置两个委托时单段优先 span（与旧版行为一致）</summary>
    [Fact]
    [DisplayName("GetLength2 兼容：单段优先 span 委托")]
    public void GetLength2_SingleSegment_PreferredOverPacket()
    {
        var frame = MakeFrame(100, 1);
        var ipCount = 0;
        var spanCount = 0;

#pragma warning disable CS0618 // 兼容旧版二进制的 span 委托
        var codec = new PacketCodec
        {
            GetLength = p => { ipCount++; return MessageCodec<DefaultMessage>.GetLength(p, 2, 2); },
            GetLength2 = span => { spanCount++; return MessageCodec<DefaultMessage>.GetLength(span, 2, 2); },
        };
#pragma warning restore CS0618 // 兼容旧版二进制的 span 委托

        var round = CreateRound(frame, 0, frame.Length);
        var frames = codec.Parse(round);

        Assert.Single(frames);
        Assert.Equal(frame, frames[0].ToArray());
        Assert.Equal(0, ipCount);
        Assert.Equal(1, spanCount);

        frames[0].TryDispose();
        round.TryDispose();
        codec.Dispose();
    }

    /// <summary>S6：头部逐字节到达 —— 并段补齐到 HeadSize 后正常定界，未到齐的轮次不出帧</summary>
    [Fact]
    [DisplayName("S6 头部保证：逐字节到达并段补齐后定界")]
    public void HeadAssembly_OneBytePerRound()
    {
        var frame = MakeFrame(100, 1);
        var codec = CreateLengthCodec();

        IList<IPacket>? got = null;
        for (var i = 0; i < frame.Length; i++)
        {
            var round = CreateRound(frame, i, 1);
            var frames = codec.Parse(round);
            round.TryDispose();

            if (frames.Count > 0) got = frames;
        }

        Assert.NotNull(got);
        Assert.Single(got!);
        Assert.Equal(frame.Length, got![0].Total);
        Assert.Equal(frame, got![0].ToArray());
        Assert.Equal(32, got![0].Length);            // 头部补齐到 HeadSize（单一头部节点）
        Assert.NotNull(got![0].Next);                // 后续字节零拷贝挂链

        got![0].TryDispose();
    }

    /// <summary>S6：头部残片 + 大段到达 —— 仅补足 HeadSize，余量零拷贝挂链</summary>
    [Fact]
    [DisplayName("S6 头部保证：大段仅补足头部，余量零拷贝挂链")]
    public void HeadAssembly_BigRound_MergeCap()
    {
        var frame = MakeFrame(5000, 7);
        var codec = CreateLengthCodec();

        // 第1轮仅 3 字节（定界所需 4 字节不足），第2轮给足余下
        var r1 = CreateRound(frame, 0, 3);
        Assert.Empty(codec.Parse(r1));
        r1.TryDispose();

        var r2 = CreateRound(frame, 3, frame.Length - 3);
        var frames = codec.Parse(r2);

        Assert.Single(frames);
        Assert.Equal(frame.Length, frames[0].Total);
        Assert.Equal(frame, frames[0].ToArray());
        Assert.Equal(32, frames[0].Length);          // 并段只补足到 HeadSize，未整段拷贝
        Assert.NotNull(frames[0].Next);              // 余量零拷贝挂链
        Assert.Equal(2, r2.RefCount);                // 本轮 + 帧链余段切片
        r2.TryDispose();

        frames[0].TryDispose();
    }

    /// <summary>S6：Append 时并段补齐后，span 委托对多帧连续切帧（含跨节点小窗口）</summary>
    [Fact]
    [DisplayName("S6 头部保证：并段后 span 委托连续切帧")]
    public void HeadAssembly_ConsecutiveCuts()
    {
        // 构造 5 个 20 字节小帧（4 头 + 16 负载）
        var f1 = MakeFrame(16, 1);
        var f2 = MakeFrame(16, 2);
        var f3 = MakeFrame(16, 3);
        var f4 = MakeFrame(16, 4);
        var f5 = MakeFrame(16, 5);
        var seq = new Byte[5 * f1.Length];
        f1.CopyTo(seq, 0);
        f2.CopyTo(seq, f1.Length);
        f3.CopyTo(seq, 2 * f1.Length);
        f4.CopyTo(seq, 3 * f1.Length);
        f5.CopyTo(seq, 4 * f1.Length);

#pragma warning disable CS0618 // 兼容旧版二进制的 span 委托
        var codec = new PacketCodec
        {
            GetLength2 = span => MessageCodec<DefaultMessage>.GetLength(span, 2, 2)
        };
#pragma warning restore CS0618 // 兼容旧版二进制的 span 委托

        // 第1轮：前两帧完整 + 第三帧半截
        var r1 = CreateRound(seq, 0, 50);
        var frames1 = codec.Parse(r1);
        Assert.Equal(2, frames1.Count);
        Assert.Equal(f1, frames1[0].ToArray());
        Assert.Equal(f2, frames1[1].ToArray());
        foreach (var f in frames1) f.TryDispose();
        r1.TryDispose();

        // 第2轮：第三帧尾部 + 第四、五帧；Append 时并段补齐（残留 10 字节 < HeadSize），随后连续切出三帧
        var r2 = CreateRound(seq, 50, seq.Length - 50);
        var frames2 = codec.Parse(r2);
        Assert.Equal(3, frames2.Count);
        Assert.Equal(f3, frames2[0].ToArray());
        Assert.Equal(f4, frames2[1].ToArray());
        Assert.Equal(f5, frames2[2].ToArray());
        foreach (var f in frames2) f.TryDispose();
        r2.TryDispose();

        codec.Dispose();
    }

    /// <summary>链式兼容：GetLength 在帧首不足头部时拼入栈缓冲跨段定界</summary>
    [Fact]
    [DisplayName("链式兼容：GetLength 跨段拼读定界")]
    public void ChainedFrame_CompatibleGetLength()
    {
        var f1 = MakeFrame(100, 1);
        var chain = new ArrayPacket(f1, 0, 50) { Next = new ArrayPacket(f1, 50, f1.Length - 50) };

        // 头部（2 字节偏移起 2 字节长度）完整落在首段，直读即可
        Assert.Equal(f1.Length, MessageCodec<DefaultMessage>.GetLength(chain, 2, 2));

        // 首段仅 3 字节（不足头部）——拼入栈缓冲跨段拼读
        var shortHead = new ArrayPacket(f1, 0, 3) { Next = new ArrayPacket(f1, 3, f1.Length - 3) };
        Assert.Equal(f1.Length, MessageCodec<DefaultMessage>.GetLength(shortHead, 2, 2));
    }
}
