using System.Buffers;
using NewLife;
using NewLife.Data;
using NewLife.Messaging;
using Xunit;

namespace XUnitTest.Messaging;

/// <summary>消息帧契约边界测试</summary>
/// <remarks>
/// 覆盖：SRMP 长度字段的损坏帧防御（负值 / Int32 溢出）、流式体不可整帧构建的显式失败、
/// 响应语义三布尔（Reply/Error/OneWay）的独立存储与协议四态往返。
/// </remarks>
public class MessageContractTests
{
    #region 辅助
    /// <summary>构造 8 字节扩展头 + 可选负载的数据包（Length=0xFFFF 标记 + 4 字节正式长度，小端）</summary>
    /// <param name="payloadLen">头部声明的负载长度（可为负值和溢出值，用于构造损坏帧）</param>
    /// <param name="actualPayload">实际附加的负载字节数</param>
    private static Byte[] BuildExtendedFrame(Int32 payloadLen, Int32 actualPayload = 0)
    {
        var buf = new Byte[8 + actualPayload];
        buf[2] = 0xFF;
        buf[3] = 0xFF;
        buf[4] = (Byte)(payloadLen & 0xFF);
        buf[5] = (Byte)((payloadLen >> 8) & 0xFF);
        buf[6] = (Byte)((payloadLen >> 16) & 0xFF);
        buf[7] = (Byte)((payloadLen >> 24) & 0xFF);

        return buf;
    }
    #endregion

    #region 损坏帧防御
    [Theory(DisplayName = "损坏帧_负长度字段_拒收不产生对象")]
    [InlineData(-1)]
    [InlineData(Int32.MinValue)]
    public void TryParse_NegativeLength_Rejected(Int32 len)
    {
        // 负值（最高位为 1）视为损坏帧：codec 返回 null，不产生对象
        var buf = BuildExtendedFrame(len);
        var codec = new SrmpCodec();

        Assert.Null(codec.TryParse(new ArrayPacket(buf).AsReadOnlySequence()));
    }

    [Fact(DisplayName = "损坏帧_0x7FFFFFFF长度_不消费不产出")]
    public void TryParse_OverflowLength_Rejected()
    {
        // 声明 0x7FFFFFFF 但实际只有 5 字节负载：定界成功但体不足，由帧泵等待
        var buf = BuildExtendedFrame(0x7FFFFFFF, 5);
        var codec = new SrmpCodec();

        var rs = codec.TryParse(new ArrayPacket(buf).AsReadOnlySequence());
        Assert.NotNull(rs);
        Assert.Equal(8, rs.Value.HeaderSize);
        Assert.Equal(0x7FFFFFFFL, rs.Value.BodyLength);
    }

    [Fact(DisplayName = "损坏帧_借阅视图与拥有帧行为一致")]
    public void TryParse_InvalidLength_SameForOwnerAndView()
    {
        var buf = BuildExtendedFrame(-1);
        var codec = new SrmpCodec();

        Assert.Null(codec.TryParse(new ArrayPacket(buf).AsReadOnlySequence()));

        var owner = new OwnerPacket(buf.Length);
        buf.CopyTo(owner.GetSpan());
        Assert.Null(codec.TryParse(owner.AsReadOnlySequence()));

        owner.Dispose();
    }
    #endregion

    #region 状态隔离
    [Fact(DisplayName = "损坏帧_扩展头负长度_拒收不产生对象")]
    public void TryParse_NegativeExtendedLength_Rejected()
    {
        // 数据已齐 8 字节但长度字段非法（最高位为 1）：按无法定界处理，不消费、不产生对象
        var buf = BuildExtendedFrame(Int32.MinValue);
        var codec = new SrmpCodec();

        Assert.Null(codec.TryParse(new ReadOnlySequence<Byte>(buf)));
    }
    #endregion

    #region 流式体构建守卫
    [Fact(DisplayName = "Build_流式体绑定_抛InvalidOperationException")]
    public void Build_StreamingBody_Throws()
    {
        // 流式体未持有完整负载，整帧构建必须显式失败（而非静默返回 null 让消息消失）
        using var pipe = new Pipe();
        var msg = new DefaultMessage();
        msg.BindBody(new LimitedReader(pipe.Reader, 16));

        Assert.Throws<InvalidOperationException>(() => new SrmpCodec().Build(msg));
    }
    #endregion

    #region 消息种类四态矩阵
    [Theory(DisplayName = "消息种类_四态_构建解析回环对应协议状态位")]
    [InlineData(MessageKinds.Request)]
    [InlineData(MessageKinds.OneWay)]
    [InlineData(MessageKinds.Response)]
    [InlineData(MessageKinds.Error)]
    public void MessageKinds_RoundTrip(MessageKinds kind)
    {
        var msg = new DefaultMessage { Kind = kind };

        Assert.Equal(kind, msg.Kind);

        // 构建后解析应还原消息种类（协议头部高 2 位）
        var codec = new SrmpCodec();
        var pk = codec.Build(msg)!;
        var rs = codec.TryParse(pk.AsReadOnlySequence());
        Assert.NotNull(rs);
        var msg2 = (DefaultMessage)rs.Value.Message!;
        Assert.Equal(kind, msg2.Kind);

        pk.TryDispose();
    }

    [Fact(DisplayName = "消息种类：无状态位协议产出消息不标记方向")]
    public void MessageKinds_StatelessCodec_NoDirection()
    {
        // 长度字段协议无状态位，解析产出纯负载消息，Reply 恒为 false
        var codec = new LengthFieldCodec();
        var frame = new Byte[] { 0x02, 0x00, 0xAA, 0xBB };
        var rs = codec.TryParse(new ReadOnlySequence<Byte>(frame));
        Assert.NotNull(rs);
        Assert.False(rs.Value.Message!.Reply);
    }

    [Fact(DisplayName = "消息种类：长度字段协议实现恒真配对语义")]
    public void MessageKinds_LengthFieldCodec_Matcher()
    {
        // 无配对键协议：任意响应均视为匹配，适合串行请求-响应
        var matcher = Assert.IsAssignableFrom<IMessageMatcher>(new LengthFieldCodec());
        Assert.True(matcher.Match(new Message(), new Message()));
    }

    [Fact(DisplayName = "消息种类：Reply/OneWay 便捷视图与 Kind 双向映射")]
    public void MessageKinds_ReplyOneWay_View()
    {
        var msg = new DefaultMessage();
        Assert.False(msg.Reply);
        Assert.False(msg.OneWay);

        msg.OneWay = true;
        Assert.Equal(MessageKinds.OneWay, msg.Kind);
        Assert.True(msg.OneWay);
        Assert.False(msg.Reply);

        msg.Kind = MessageKinds.Error;
        Assert.True(msg.Reply);
        Assert.False(msg.OneWay);

        msg.Reply = false;
        Assert.Equal(MessageKinds.Request, msg.Kind);
    }
    #endregion
}
