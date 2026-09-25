using NewLife;
using NewLife.Data;
using NewLife.Messaging;
using NewLife.Reflection;
using Xunit;

namespace XUnitTest.Messaging;

/// <summary>IMessage 所有权释放机制单元测试</summary>
/// <remarks>
/// 契约：<see cref="IMessage.Payload"/> 为数据包。编解码器定界产出消息后，帧层以共享切片（<c>Slice</c>）
/// 绑定负载（<see cref="OwnerPacket"/> 引用计数），且不释放入参（帧句柄由调用方释放）；消息 Dispose 时唯一归还负载；借阅视图（ArrayPacket）无所有权。本组测试验证该释放链路。
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

    [Fact(DisplayName = "解析不释放入参：负载独立持有，消息Dispose归还底层OwnerPacket")]
    public void Dispose_AfterRead_ShouldReturnOwnerPacket()
    {
        var raw = BuildFrame(10);
        Assert.NotNull(raw.GetValue("_owner"));

        var rs = new SrmpCodec().TryParse(raw.AsReadOnlySequence());
        Assert.NotNull(rs);
        var msg = (DefaultMessage)rs.Value.Message!;
        Assert.Equal(5, msg.Sequence);

        // 帧层绑定：窗口切片共享负载（拥有帧引用计数 +1），不释放入参
        msg.SetBody(raw.Slice(4, 10));
        Assert.Equal(10, msg.Payload!.Length);

        // 入参句柄仍归调用方
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

    [Fact(DisplayName = "链式帧：解析取共享负载，消息与入参各自释放")]
    public void Dispose_WithChainedOwnerPacket_ShouldDisposeEntireChain()
    {
        // 链式包：头部在第一段，负载可能跨段
        var part1 = new OwnerPacket(16);
        var part2 = new OwnerPacket(16);
        part1.Next = part2;
        part1.GetSpan()[..4].Fill(0x01);
        part1.GetSpan()[2] = 0x0A; // Length=10（低字节）

        var rs = new SrmpCodec().TryParse(part1.AsReadOnlySequence());
        Assert.NotNull(rs);
        var msg = (DefaultMessage)rs.Value.Message!;

        // 帧层绑定：负载切片共享引用
        msg.SetBody(part2.Slice(4, 10));
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
        msg.SetBody(pk);

        // 借阅视图无所有权，Dispose 仅清理 Payload（置 null），不应抛出
        msg.Dispose();
    }

    [Fact(DisplayName = "Dispose幂等性：多次Dispose不应抛出异常")]
    public void Dispose_MultipleTimes_ShouldNotThrow()
    {
        var raw = BuildFrame(4);
        var rs = new SrmpCodec().TryParse(raw.AsReadOnlySequence());
        Assert.NotNull(rs);
        var msg = (DefaultMessage)rs.Value.Message!;
        msg.SetBody(raw.Slice(4, 4));

        msg.Dispose();
        msg.Dispose(); // 第二次不应抛出

        // 入参句柄保持有效（解析不释放）
        Assert.NotNull(raw.GetValue("_owner"));
        raw.TryDispose();
    }

    [Fact(DisplayName = "using模式：作用域退出自动归还消息持有的缓冲")]
    public void Using_ShouldAutoDisposePayloadOnScopeExit()
    {
        var raw = BuildFrame(64);
        var rs = new SrmpCodec().TryParse(raw.AsReadOnlySequence());
        Assert.NotNull(rs);

        OwnerPacket? owned = null;
        using (var msg = rs.Value.Message!)
        {
            msg.SetBody(raw.Slice(4, 64));
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
        var rs = new SrmpCodec().TryParse(raw.AsReadOnlySequence());
        Assert.NotNull(rs);
        var request = (DefaultMessage)rs.Value.Message!;
        request.SetBody(raw.Slice(4, 8));

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

    [Fact(DisplayName = "完整生命周期：借→解析→切片绑定→Dispose归还")]
    public void FullLifecycle_OwnerPacketAsRaw()
    {
        var raw = BuildFrame(4);
        Assert.NotNull(raw.GetValue("_owner"));

        var rs = new SrmpCodec().TryParse(raw.AsReadOnlySequence());
        Assert.NotNull(rs);
        var msg = (DefaultMessage)rs.Value.Message!;
        msg.SetBody(raw.Slice(4, 4));
        Assert.Equal(4, msg.Payload!.Length);

        // 负载共享持有引用；入参句柄由调用方释放
        Assert.NotNull(raw.GetValue("_owner"));
        var owned = (OwnerPacket)msg.Payload;

        msg.Dispose();

        Assert.Null(owned.GetValue("_owner"));

        raw.TryDispose();
    }

    [Fact(DisplayName = "解析后入参窗口完整可读：不消费、不破坏帧头与负载")]
    public void TryParse_AfterOwnedFrameSlice_ShouldReadFullFrame()
    {
        var raw = BuildFrame(10);
        var rs = new SrmpCodec().TryParse(raw.AsReadOnlySequence());
        Assert.NotNull(rs);
        var msg = (DefaultMessage)rs.Value.Message!;
        msg.SetBody(raw.Slice(4, 10));

        // 解析不消费、不破坏入参窗口：帧头字节与负载完整可读
        var span = raw.GetSpan();
        Assert.Equal(0x01, span[0]);
        Assert.Equal(0x05, span[1]);
        Assert.Equal(10, span[2]);
        Assert.Equal(0x00, span[3]);
        for (var i = 0; i < 10; i++)
            Assert.Equal((Byte)(i & 0xFF), span[4 + i]);

        msg.Dispose();
        raw.TryDispose();
    }

    [Fact(DisplayName = "链式帧：首段不足头部时跨段解析正确")]
    public void TryParse_WithChainedFrame_ShouldParse()
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

        var rs = new SrmpCodec().TryParse(part1.AsReadOnlySequence());
        Assert.NotNull(rs);
        Assert.Equal(4, rs.Value.HeaderSize);
        Assert.Equal(10L, rs.Value.BodyLength);
        var msg = (DefaultMessage)rs.Value.Message!;
        Assert.Equal(0x01, msg.Flag);
        Assert.Equal(5, msg.Sequence);

        // 帧层绑定：负载切片共享引用
        msg.SetBody(part2.Slice(2, payloadLen));
        Assert.Equal(payloadLen, msg.Payload!.Total);
        for (var i = 0; i < payloadLen; i++)
            Assert.Equal((Byte)(i & 0xFF), msg.Payload[i]);

        msg.Dispose();
        part1.TryDispose();
    }

    [Fact(DisplayName = "链式帧：解析后入参链完整可读，负载切片独立释放")]
    public void TryParse_ChainedOwnedFrame_ShouldKeepWindowIntact()
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

        var rs = new SrmpCodec().TryParse(part1.AsReadOnlySequence());
        Assert.NotNull(rs);
        var msg = (DefaultMessage)rs.Value.Message!;

        // 帧层绑定：负载切片共享引用
        var owned = (OwnerPacket)part2.Slice(2, payloadLen);
        msg.SetBody(owned);
        Assert.Equal(payloadLen, msg.Payload!.Length);

        // 解析不消费、不破坏入参链：两段数据完整可读
        Assert.Equal(0x01, part1.GetSpan()[0]);
        Assert.Equal(0x05, part1.GetSpan()[1]);
        Assert.Equal((Byte)(payloadLen & 0xFF), part2.GetSpan()[0]);
        for (var i = 0; i < payloadLen; i++)
            Assert.Equal((Byte)(i & 0xFF), part2.GetSpan()[2 + i]);

        // 消息释放负载引用后，入参链仍归调用方
        msg.Dispose();
        Assert.Null(owned.GetValue("_owner"));
        Assert.NotNull(part1.GetValue("_owner"));
        Assert.NotNull(part2.GetValue("_owner"));

        part1.TryDispose();
    }

    [Fact(DisplayName = "SetBody(null)表示所有权转移：消息Dispose不归还已转移句柄")]
    public void SetBodyNull_TransfersOwnership()
    {
        var owner = new OwnerPacket(8);
        owner.GetSpan().Fill(0x41);

        var msg = new DefaultMessage();
        msg.SetBody(owner);
        Assert.NotNull(owner.GetValue("_owner"));

        // 转移：消息放弃持有、不归还
        msg.SetBody((IPacket?)null);
        msg.Dispose();

        // 已转移句柄未被归还，由调用方接管
        Assert.NotNull(owner.GetValue("_owner"));

        owner.Dispose();
        Assert.Null(owner.GetValue("_owner"));
    }

    [Fact(DisplayName = "Build构建后所有权转移：消息Dispose不击穿结果包链")]
    public void Build_TransfersOwnership_DisposeDoesNotBreakResult()
    {
        var owner = new OwnerPacket(5);
        "hello".GetBytes().CopyTo(owner.GetSpan());

        var msg = new DefaultMessage { Sequence = 3 };
        msg.SetBody(owner);
        var pk = new SrmpCodec().Build(msg);
        Assert.NotNull(pk);
        Assert.Equal(4 + 5, pk!.Total);

        // 转移：消息不再持有负载（构建结果包接管），Dispose 不得归还
        msg.Dispose();
        Assert.NotNull(owner.GetValue("_owner"));
        Assert.Equal(4 + 5, pk.Total);

        var tail = pk.Slice(4, -1);
        Assert.Equal("hello", tail.ToStr());
        tail.TryDispose();

        // 结果包释放时才级联归还链上缓冲
        pk.TryDispose();
        Assert.Null(owner.GetValue("_owner"));
    }

    [Fact(DisplayName = "CreateReply继承Flag/Sequence并置为响应；响应上创建回应返回 null")]
    public void CreateReply_InheritsFlagAndSequence()
    {
        var req = new DefaultMessage { Flag = (Byte)DataKinds.Json, Sequence = 42 };
        var reply = req.CreateReply();

        var dm = Assert.IsType<DefaultMessage>(reply);
        Assert.Equal(MessageKinds.Response, dm.Kind);
        Assert.Equal((Byte)DataKinds.Json, dm.Flag);
        Assert.Equal(42, dm.Sequence);

        Assert.Null(dm.CreateReply());
    }
}
