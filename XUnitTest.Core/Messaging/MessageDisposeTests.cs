using NewLife;
using NewLife.Data;
using NewLife.Messaging;
using NewLife.Reflection;
using Xunit;

namespace XUnitTest.Messaging;

/// <summary>IMessage 所有权释放机制单元测试</summary>
/// <remarks>
/// 契约：<see cref="IMessage.Payload"/> 为数据包。<c>Read(IPacket)</c> 解析拥有帧时
/// 经共享切片（<c>Slice</c>）使负载获得独立引用（<see cref="OwnerPacket"/> 引用计数），
/// 且不释放入参（帧句柄由调用方释放）；消息 Dispose / Reset 时唯一归还负载；借阅视图（ArrayPacket）无所有权。本组测试验证该释放链路。
/// </remarks>
public class MessageDisposeTests
{
    /// <summary>构造带头部+负载的 DefaultMessage 二进制帧（池化缓冲）</summary>
    private static OwnerPacket BuildFrame(Int32 payloadLen)
    {
        var raw = new OwnerPacket(8 + payloadLen);
        var span = raw.GetSpan();
        span[0] = 0x01; // Flag=1, 请求
        span[1] = 0x05; // Sequence=5
        span[2] = (Byte)(payloadLen & 0xFF);
        span[3] = (Byte)(payloadLen >> 8);
        for (var i = 0; i < payloadLen; i++) span[4 + i] = (Byte)(i & 0xFF);

        return raw;
    }

    [Fact(DisplayName = "Read不释放入参：负载独立持有，消息Dispose归还底层OwnerPacket")]
    public void Dispose_AfterRead_ShouldReturnOwnerPacket()
    {
        var raw = BuildFrame(10);
        Assert.NotNull(raw.GetValue("_owner"));

        var msg = new DefaultMessage();
        Assert.True(msg.Read(raw));
        Assert.Equal(5, msg.Sequence);
        Assert.Equal(10, msg.Payload!.Length);

        // 入参句柄仍归调用方：Read 不释放
        Assert.NotNull(raw.GetValue("_owner"));
        var owned = (OwnerPacket)msg.Payload;
        Assert.NotNull(owned.GetValue("_owner"));

        // 释放入参帧句柄：负载为共享切片持有独立引用，不受影响
        raw.TryDispose();
        Assert.Equal(10, msg.Payload!.Length);

        msg.Dispose();

        // Dispose 唯一归还负载引用
        Assert.Null(owned.GetValue("_owner"));
    }

    [Fact(DisplayName = "链式帧：Read取共享负载，消息与入参各自释放")]
    public void Dispose_WithChainedOwnerPacket_ShouldDisposeEntireChain()
    {
        // 链式包：头部在第一段，负载可能跨段
        var part1 = new OwnerPacket(16);
        var part2 = new OwnerPacket(16);
        part1.Next = part2;
        part1.GetSpan()[..4].Fill(0x01);
        part1.GetSpan()[2] = 0x0A; // Length=10（低字节）

        var msg = new Message();
        Assert.True(msg.Read(part1));
        Assert.NotNull(part1.GetValue("_owner"));
        Assert.NotNull(part2.GetValue("_owner"));

        msg.Dispose();

        // 消息释放负载引用后，入参链仍然可用
        Assert.NotNull(part1.GetValue("_owner"));
        Assert.NotNull(part2.GetValue("_owner"));

        // 入参句柄由调用方释放：链式递归归零
        part1.Dispose();
        Assert.Null(part1.GetValue("_owner"));
        Assert.Null(part2.GetValue("_owner"));
    }

    [Fact(DisplayName = "Dispose无所有权Payload：ArrayPacket借阅视图不受影响")]
    public void Dispose_WithArrayPacket_ShouldNotThrow()
    {
        var pk = new ArrayPacket(new Byte[] { 1, 2, 3 });
        var msg = new Message();
        Assert.True(msg.Read(pk));

        // 借阅视图无所有权，Dispose 仅清理 Payload（置 null），不应抛出
        msg.Dispose();
    }

    [Fact(DisplayName = "Dispose幂等性：多次Dispose不应抛出异常")]
    public void Dispose_MultipleTimes_ShouldNotThrow()
    {
        var raw = BuildFrame(4);
        var msg = new DefaultMessage();
        Assert.True(msg.Read(raw));

        msg.Dispose();
        msg.Dispose(); // 第二次不应抛出

        // 入参句柄保持有效（Read 不释放）
        Assert.NotNull(raw.GetValue("_owner"));
        raw.TryDispose();
    }

    [Fact(DisplayName = "Reset归还原Owner：对象池复用前归还池化缓冲")]
    public void Reset_ShouldReturnOwnerPacket()
    {
        var raw = BuildFrame(8);
        var msg = new DefaultMessage();
        Assert.True(msg.Read(raw));

        // 负载获得独立引用，消息持有；入参句柄仍归调用方
        Assert.NotNull(raw.GetValue("_owner"));
        var owned = (OwnerPacket)msg.Payload!;
        Assert.NotNull(owned.GetValue("_owner"));

        msg.Reset();

        // Reset 归还负载（池化缓冲）并清空 Payload
        Assert.Null(owned.GetValue("_owner"));
        Assert.Null(msg.Payload);

        raw.TryDispose();
    }

    [Fact(DisplayName = "using模式：作用域退出自动归还消息持有的缓冲")]
    public void Using_ShouldAutoDisposePayloadOnScopeExit()
    {
        var raw = BuildFrame(64);

        OwnerPacket? owned = null;
        using (var msg = new DefaultMessage())
        {
            Assert.True(msg.Read(raw));
            owned = (OwnerPacket)msg.Payload!;
            Assert.NotNull(owned.GetValue("_owner"));
        }

        // using 块退出后，缓冲应被归还
        Assert.Null(owned!.GetValue("_owner"));

        raw.TryDispose();
    }

    [Fact(DisplayName = "CreateReply不影响原始消息所有权")]
    public void CreateReply_ShouldNotAffectOriginal()
    {
        using var raw = BuildFrame(8);
        var request = new DefaultMessage();
        Assert.True(request.Read(raw));

        // 负载独立持有引用；入参由 using 作用域释放
        Assert.NotNull(raw.GetValue("_owner"));
        var owned = (OwnerPacket)request.Payload!;

        // 创建响应消息
        var reply = request.CreateReply();

        // 响应为独立消息，不影响原消息持有的所有权
        Assert.Null(reply.Payload);

        reply.Dispose();
        Assert.NotNull(owned.GetValue("_owner"));

        request.Dispose();
        Assert.Null(owned.GetValue("_owner"));
    }

    [Fact(DisplayName = "完整生命周期：借→Read→解析→Dispose归还")]
    public void FullLifecycle_OwnerPacketAsRaw()
    {
        var raw = BuildFrame(4);
        Assert.NotNull(raw.GetValue("_owner"));

        var msg = new DefaultMessage();
        Assert.True(msg.Read(raw));
        Assert.Equal(4, msg.Payload!.Length);

        // 负载共享持有引用；入参句柄由调用方释放
        Assert.NotNull(raw.GetValue("_owner"));
        var owned = (OwnerPacket)msg.Payload;

        msg.Dispose();

        Assert.Null(owned.GetValue("_owner"));

        raw.TryDispose();
    }

    [Fact(DisplayName = "GetRaw：拥有帧负载切片后帧头+负载完整可读（展示链）")]
    public void GetRaw_AfterOwnedFrameSlice_ShouldReadFullFrame()
    {
        var raw = BuildFrame(10);
        var msg = new DefaultMessage();
        Assert.True(msg.Read(raw));

        var view = msg.GetRaw();
        Assert.NotNull(view);
        Assert.Equal(14, view!.Total);

        // 帧头字节与负载完整可读
        Assert.Equal(0x01, view[0]);
        Assert.Equal(0x05, view[1]);
        Assert.Equal(10, view[2]);
        Assert.Equal(0x00, view[3]);
        for (var i = 0; i < 10; i++)
            Assert.Equal((Byte)(i & 0xFF), view[4 + i]);

        msg.Dispose();
        raw.TryDispose();
    }

    [Fact(DisplayName = "GetRaw：帧头跨轮组链后完整可读")]
    public void GetRaw_FrameAcrossRounds_ShouldReadFullFrame()
    {
        // 帧头跨轮：第1轮仅 2 字节（不足 4 字节帧头），第2轮补齐后直接组链成帧（不并段）
        using var full = BuildFrame(10);
        // BuildFrame 缓冲为 8+payload，这里截取真实帧窗口（4 头 + 10 负载）
        var raw = full.ToArray()[..14];

        var codec = new PacketCodec { GetLength = DefaultMessage.GetLength };
        Assert.Empty(codec.Parse(new ArrayPacket(raw, 0, 2)));

        var frames = codec.Parse(new ArrayPacket(raw, 2, raw.Length - 2));
        Assert.Single(frames);
        var frame = frames[0];
        Assert.Equal(14, frame.Total);    // 帧头跨轮组链成帧
        Assert.NotNull(frame.Next);

        var msg = new DefaultMessage();
        Assert.True(msg.Read(frame));
        Assert.Equal(10, msg.Payload!.Length);

        var view = msg.GetRaw();
        Assert.NotNull(view);
        Assert.Equal(4 + 10, view!.Total);
        Assert.Equal(0x01, view[0]);
        Assert.Equal(0x05, view[1]);
        Assert.Equal(10, view[2]);
        for (var i = 0; i < 10; i++)
            Assert.Equal((Byte)(i & 0xFF), view[4 + i]);

        msg.Dispose();
        frame.TryDispose();
    }

    [Fact(DisplayName = "Read：链式帧首段不足头部时拼读兼容（兼容旧直调路径）")]
    public void Read_WithChainedFrame_ShouldParse()
    {
        // 头部前 2 字节在首段，长度与负载在次段——首段不足 8 字节头部，拼入栈缓冲后正常解析
        var part1 = new OwnerPacket(2);
        var part2 = new OwnerPacket(12);
        part1.Next = part2;

        var payloadLen = 10;
        part1.GetSpan()[0] = 0x01;
        part1.GetSpan()[1] = 0x05;
        var span2 = part2.GetSpan();
        span2[0] = (Byte)(payloadLen & 0xFF);
        span2[1] = (Byte)(payloadLen >> 8);
        for (var i = 0; i < payloadLen; i++) span2[2 + i] = (Byte)(i & 0xFF);

        var msg = new DefaultMessage();
        Assert.True(msg.Read(part1));
        Assert.Equal(0x01, msg.Flag);
        Assert.Equal(5, msg.Sequence);
        Assert.Equal(payloadLen, msg.Payload!.Total);
        for (var i = 0; i < payloadLen; i++)
            Assert.Equal((Byte)(i & 0xFF), msg.Payload[i]);

        msg.Dispose();
        part1.TryDispose();
    }

    [Fact(DisplayName = "GetRaw：链式拥有帧（首段短于帧头）切片后完整可读")]
    public void GetRaw_ChainedOwnedFrame_ShouldReadFullFrame()
    {
        // 首段仅 2 字节（不足 4 字节帧头），帧头跨段
        var part1 = new OwnerPacket(2);
        var part2 = new OwnerPacket(12);
        part1.Next = part2;

        var payloadLen = 10;
        part1.GetSpan()[0] = 0x01;
        part1.GetSpan()[1] = 0x05;
        var span2 = part2.GetSpan();
        span2[0] = (Byte)(payloadLen & 0xFF);
        span2[1] = (Byte)(payloadLen >> 8);
        for (var i = 0; i < payloadLen; i++) span2[2 + i] = (Byte)(i & 0xFF);

        var msg = new DefaultMessage();
        Assert.True(msg.Read(part1));
        Assert.Equal(payloadLen, msg.Payload!.Length);

        var view = msg.GetRaw();
        Assert.NotNull(view);
        Assert.Equal(4 + payloadLen, view!.Total);
        Assert.Equal(0x01, view[0]);
        Assert.Equal(0x05, view[1]);
        Assert.Equal(payloadLen, view[2]);
        for (var i = 0; i < payloadLen; i++)
            Assert.Equal((Byte)(i & 0xFF), view[4 + i]);

        msg.Dispose();
        part1.TryDispose();
    }
}
