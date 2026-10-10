using System.Buffers;
using System.ComponentModel;
using NewLife.Data;
using NewLife.Http;
using NewLife.Messaging;
using Xunit;

namespace XUnitTest.Data;

/// <summary>数据包序列桥接（AsReadOnlySequence）与序列定界测试</summary>
public class PacketSequenceTests
{
    #region 工具
    /// <summary>构建数据包链</summary>
    private static IPacket Chain(params Byte[][] parts)
    {
        IPacket pk = new ArrayPacket(parts[0]);
        for (var i = 1; i < parts.Length; i++)
        {
            pk.Append(new ArrayPacket(parts[i]));
        }

        return pk;
    }

    /// <summary>生成顺序字节序列</summary>
    private static Byte[] Range(Int32 start, Int32 count)
    {
        var buf = new Byte[count];
        for (var i = 0; i < count; i++) buf[i] = (Byte)(start + i);

        return buf;
    }

    /// <summary>快速构建字节数组</summary>
    private static Byte[] B(params Int32[] values)
    {
        var buf = new Byte[values.Length];
        for (var i = 0; i < values.Length; i++) buf[i] = (Byte)values[i];

        return buf;
    }

    /// <summary>协议解析原型。只取帧长（头部+体长），实例状态被丢弃</summary>
    private static readonly SrmpCodec _dmParser = new();
    private static readonly WebSocketCodec _wsParser = new();

    private static Int32 GetFrameLength(ParseResult? rs) => rs == null ? 0 : (Int32)(rs.Value.HeaderSize + rs.Value.BodyLength);

    private static Int32 GetLength(Byte[] data) => GetFrameLength(_dmParser.TryParse(new ArrayPacket(data).AsReadOnlySequence()));
    private static Int32 GetLength(IPacket pk) => GetFrameLength(_dmParser.TryParse(pk.AsReadOnlySequence()));
    private static Int32 GetLength(ReadOnlySequence<Byte> buffer) => GetFrameLength(_dmParser.TryParse(buffer));
    private static Int32 GetWsLength(Byte[] data) => GetFrameLength(_wsParser.TryParse(new ArrayPacket(data).AsReadOnlySequence()));
    private static Int32 GetWsLength(IPacket pk) => GetFrameLength(_wsParser.TryParse(pk.AsReadOnlySequence()));
    private static Int32 GetWsLength(ReadOnlySequence<Byte> buffer) => GetFrameLength(_wsParser.TryParse(buffer));
    #endregion

    #region 序列桥接
    [Fact]
    [DisplayName("AsReadOnlySequence_单段_与原始数据一致")]
    public void AsReadOnlySequence_SingleSegment()
    {
        var data = Range(0, 8);
        IPacket pk = new ArrayPacket(data);

        var seq = pk.AsReadOnlySequence();

        Assert.Equal(data.Length, seq.Length);
        Assert.True(seq.IsSingleSegment);
        Assert.Equal(data, seq.ToArray());
    }

    [Fact]
    [DisplayName("AsReadOnlySequence_多段链_按序拼接")]
    public void AsReadOnlySequence_MultiSegment()
    {
        var pk = Chain(B(1, 2, 3), B(4, 5), B(6));

        var seq = pk.AsReadOnlySequence();

        Assert.Equal(6, seq.Length);
        Assert.False(seq.IsSingleSegment);
        Assert.Equal(B(1, 2, 3, 4, 5, 6), seq.ToArray());
    }

    [Fact]
    [DisplayName("AsReadOnlySequence_跨段窗口_裁剪正确")]
    public void AsReadOnlySequence_WindowCrossSegment()
    {
        var pk = Chain(Range(0, 4), Range(4, 6), Range(10, 5)); // 0..14

        // 跨两个段边界
        var seq = pk.AsReadOnlySequence(2, 10);
        Assert.Equal(10, seq.Length);
        Assert.False(seq.IsSingleSegment);
        Assert.Equal(Range(2, 10), seq.ToArray());

        // 恰好覆盖中间整段
        seq = pk.AsReadOnlySequence(4, 6);
        Assert.Equal(Range(4, 6), seq.ToArray());

        // 直到链尾
        seq = pk.AsReadOnlySequence(2);
        Assert.Equal(Range(2, 13), seq.ToArray());

        // 单段窗口落在链中某一段内
        seq = pk.AsReadOnlySequence(0, 3);
        Assert.True(seq.IsSingleSegment);
        Assert.Equal(Range(0, 3), seq.ToArray());
    }

    [Fact]
    [DisplayName("AsReadOnlySequence_空包与空窗口_返回空序列")]
    public void AsReadOnlySequence_Empty()
    {
        IPacket pk = new ArrayPacket(Array.Empty<Byte>());
        Assert.Equal(0, pk.AsReadOnlySequence().Length);
        Assert.Equal(0, pk.AsReadOnlySequence(0).Length);
        Assert.Equal(0, pk.AsReadOnlySequence(0, 0).Length);

        // 偏移恰好等于总长且长度为0
        var chain = Chain(B(1, 2), B(3));
        Assert.Equal(0, chain.AsReadOnlySequence(3, 0).Length);
    }

    [Fact]
    [DisplayName("AsReadOnlySequence_越界_抛参数异常")]
    public void AsReadOnlySequence_OutOfRange()
    {
        var pk = Chain(B(1, 2, 3), B(4, 5));

        Assert.Throws<ArgumentOutOfRangeException>(() => pk.AsReadOnlySequence(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => pk.AsReadOnlySequence(6));
        Assert.Throws<ArgumentOutOfRangeException>(() => pk.AsReadOnlySequence(0, 6));
    }

    [Fact]
    [DisplayName("CopyPrefix_跨段_按时序复制前缀")]
    public void CopyPrefix_CrossSegment()
    {
        var pk = Chain(B(1, 2), B(3, 4, 5), B(6));
        var seq = pk.AsReadOnlySequence();

        Span<Byte> buf = stackalloc Byte[4];
        var n = PacketHelper.CopyPrefix(seq, buf);

        Assert.Equal(4, n);
        Assert.Equal(B(1, 2, 3, 4), buf.ToArray());

        // 目标比数据长：只复制可用部分
        Span<Byte> big = stackalloc Byte[8];
        n = PacketHelper.CopyPrefix(seq, big);

        Assert.Equal(6, n);
        Assert.Equal(B(1, 2, 3, 4, 5, 6), big[..n].ToArray());
    }

    [Fact]
    [DisplayName("GetPrefix_首段足够直引_跨段拼读_数据不足取可用")]
    public void GetPrefix_CrossSegment()
    {
        // 首段不足：跨段拼读
        var pk = Chain(B(1, 2), B(3, 4, 5), B(6));
        Span<Byte> buf = stackalloc Byte[4];
        var span = pk.GetPrefix(buf, 4);
        Assert.Equal(B(1, 2, 3, 4), span.ToArray());

        // 数据总量不足：返回可用部分
        Span<Byte> big = stackalloc Byte[8];
        span = pk.GetPrefix(big, 8);
        Assert.Equal(6, span.Length);
        Assert.Equal(B(1, 2, 3, 4, 5, 6), span.ToArray());

        // 首段足够：零拷贝直引整个首段（解析方按需取前缀）
        var pk2 = Chain(B(1, 2, 3, 4, 5), B(6));
        Span<Byte> small = stackalloc Byte[3];
        span = pk2.GetPrefix(small, 3);
        Assert.Equal(B(1, 2, 3, 4, 5), span.ToArray());

        // 单段且不足：直引短跨度
        var pk3 = new ArrayPacket(B(1, 2));
        Span<Byte> any = stackalloc Byte[8];
        span = pk3.GetPrefix(any, 8);
        Assert.Equal(2, span.Length);
    }

    [Fact]
    [DisplayName("头部拼读_IPacket重载_帧首不足跨节点定界")]
    public void HeaderSpan_PacketOverloads_CrossNode()
    {
        // DefaultMessage：扩展长度需要 8 字节头，帧首仅 3/5 字节
        var ext = B(0x01, 0x02, 0xFF, 0xFF, 70000 & 0xFF, (70000 >> 8) & 0xFF, (70000 >> 16) & 0xFF, (70000 >> 24) & 0xFF);
        Assert.Equal(70008, GetLength(Chain(ext[..3], ext[3..])));
        Assert.Equal(70008, GetLength(Chain(ext[..3], ext[3..5], ext[5..])));

        // WebSocket：127 扩展长度（8 字节大端）落在第二段
        var longBuf = B(0x82, 127, 0, 0, 0, 0, 0, 1, 0x11, 0x70);
        Assert.Equal(70010, GetWsLength(Chain(longBuf[..5], longBuf[5..])));

        // 数据不足：头部不完整时返回 0
        Assert.Equal(0, GetLength(Chain(ext[..3])));
        Assert.Equal(0, GetWsLength(Chain(longBuf[..9])));
    }

    [Fact]
    [DisplayName("头部拼读_DefaultMessage哨兵边界_65534/65535/65536")]
    public void DefaultMessage_TryParse_SentinelBoundary()
    {
        // 0xFFFF 哨兵边界：< 0xFFFF 用 4 字节头；= 0xFFFF 与 > 0xFFFF 用 8 字节头（扩展长度小端）
        // 65534：长度字段为普通值，4 + 65534
        var h16 = B(0x01, 0x02, 0xFE, 0xFF);
        Assert.Equal(4 + 65534, GetLength(h16));
        Assert.Equal(4 + 65534, GetLength(new ArrayPacket(h16).AsReadOnlySequence()));

        // 65535：哨兵 + 扩展长度 0x0000FFFF → 8 + 65535
        var h32Mid = B(0x01, 0x02, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00);
        Assert.Equal(8 + 65535, GetLength(h32Mid));
        Assert.Equal(8 + 65535, GetLength(Chain(h32Mid[..3], h32Mid[3..])));

        // 65536：哨兵 + 扩展长度 0x00010000 → 8 + 65536
        var h32Big = B(0x01, 0x02, 0xFF, 0xFF, 0x00, 0x00, 0x01, 0x00);
        Assert.Equal(8 + 65536, GetLength(h32Big));
        Assert.Equal(8 + 65536, GetLength(Chain(h32Big[..5], h32Big[5..])));
    }
    #endregion

    #region 序列定界
    [Fact]
    [DisplayName("DefaultMessage_序列定界_普通包与链式跨段")]
    public void DefaultMessage_TryParse_Sequence()
    {
        // 负载10字节：Flag+Seq+Len(4字节头)
        var buf = B(0x01, 0x02, 10, 0).Concat(Range(100, 10)).ToArray();

        Assert.Equal(14, GetLength(buf)); // 完整帧（4字节头 + 10负载）
        Assert.Equal(14, GetLength(new ArrayPacket(buf).AsReadOnlySequence()));

        // 链式：头部横跨节点
        Assert.Equal(14, GetLength(Chain(buf[..2], buf[2..]).AsReadOnlySequence()));
        Assert.Equal(14, GetLength(Chain(buf[..3], buf[3..]).AsReadOnlySequence()));
        Assert.Equal(14, GetLength(Chain(buf[..1], buf[1..3], buf[3..]).AsReadOnlySequence()));
    }

    [Fact]
    [DisplayName("DefaultMessage_序列定界_扩展长度与数据不足")]
    public void DefaultMessage_TryParse_Extended()
    {
        // 扩展长度：0xFFFF + LE int32 70000
        var ext = B(0x01, 0x02, 0xFF, 0xFF, 70000 & 0xFF, (70000 >> 8) & 0xFF, (70000 >> 16) & 0xFF, (70000 >> 24) & 0xFF);

        Assert.Equal(70008, GetLength(ext));
        Assert.Equal(70008, GetLength(new ArrayPacket(ext).AsReadOnlySequence()));
        Assert.Equal(70008, GetLength(Chain(ext[..4], ext[4..]).AsReadOnlySequence()));
        Assert.Equal(70008, GetLength(Chain(ext[..7], ext[7..]).AsReadOnlySequence()));

        // 数据不足：扩展头不完整 / 基础头不完整
        Assert.Equal(0, GetLength(Chain(ext[..7]).AsReadOnlySequence()));
        Assert.Equal(0, GetLength(Chain(ext[..3]).AsReadOnlySequence()));
        Assert.Equal(0, GetLength(new ArrayPacket(Array.Empty<Byte>()).AsReadOnlySequence()));
    }

    [Fact]
    [DisplayName("WebSocket_序列定界_三种长度形态与掩码")]
    public void WebSocketMessage_TryParse_Length()
    {
        // 短长度：2 + 125
        var shortBuf = B(0x82, 125);
        Assert.Equal(127, GetWsLength(shortBuf));
        Assert.Equal(127, GetWsLength(new ArrayPacket(shortBuf).AsReadOnlySequence()));
        Assert.Equal(127, GetWsLength(Chain(shortBuf[..1], shortBuf[1..]).AsReadOnlySequence()));

        // 126：2字节大端 300 → 4 + 300
        var midBuf = B(0x82, 126, 0x01, 0x2C);
        Assert.Equal(304, GetWsLength(new ArrayPacket(midBuf).AsReadOnlySequence()));
        Assert.Equal(304, GetWsLength(Chain(midBuf[..2], midBuf[2..]).AsReadOnlySequence()));
        Assert.Equal(304, GetWsLength(Chain(midBuf[..3], midBuf[3..]).AsReadOnlySequence()));

        // 127：8字节大端 70000 → 10 + 70000
        var longBuf = B(0x82, 127, 0, 0, 0, 0, 0, 1, 0x11, 0x70);
        Assert.Equal(70010, GetWsLength(longBuf));
        Assert.Equal(70010, GetWsLength(new ArrayPacket(longBuf).AsReadOnlySequence()));
        Assert.Equal(70010, GetWsLength(Chain(longBuf[..5], longBuf[5..]).AsReadOnlySequence()));

        // 掩码：短帧 5 字节负载，头部完整（含掩码）→ 2 + 4 + 5，总长 11 已大于现有字节数（部分到达即可定界）
        var masked = B(0x82, 0x80 | 5, 1, 2, 3, 4);
        Assert.Equal(11, GetWsLength(masked));

        // 数据不足
        Assert.Equal(0, GetWsLength(new ArrayPacket(B(0x82)).AsReadOnlySequence()));
        Assert.Equal(0, GetWsLength(Chain(midBuf[..3]).AsReadOnlySequence()));
        Assert.Equal(0, GetWsLength(Chain(longBuf[..9]).AsReadOnlySequence()));
    }

    [Fact]
    [DisplayName("长度字段_序列定界_各字段规格与链式跨段")]
    public void MessageCodec_GetLength_Sequence()
    {
        // 2字节小端：300 → 帧 302
        var le = B(0x2C, 0x01);
        Assert.Equal(302, GetLen(le, 0, 2));
        Assert.Equal(302, GetLen(new ArrayPacket(le).AsReadOnlySequence(), 0, 2));
        Assert.Equal(302, GetLen(Chain(le[..1], le[1..]).AsReadOnlySequence(), 0, 2));

        // 偏移2（MQTT风格）：[前缀2][长度2]，帧长含前缀 = 2 + 300 + 2
        var mqtt = B(0x30, 0x00, 0x2C, 0x01);
        Assert.Equal(304, GetLen(mqtt, 2, 2));
        Assert.Equal(304, GetLen(new ArrayPacket(mqtt).AsReadOnlySequence(), 2, 2));
        Assert.Equal(304, GetLen(Chain(mqtt[..3], mqtt[3..]).AsReadOnlySequence(), 2, 2));

        // 2字节大端 / 4字节小端 / 1字节
        Assert.Equal(302, GetLen(new ArrayPacket(B(0x01, 0x2C)).AsReadOnlySequence(), 0, -2));
        Assert.Equal(304, GetLen(new ArrayPacket(B(0x2C, 0x01, 0, 0)).AsReadOnlySequence(), 0, 4));
        Assert.Equal(11, GetLen(new ArrayPacket(B(10)).AsReadOnlySequence(), 0, 1));

        // 变长编码：10 → 10+1；300 → 300+2；跨段
        Assert.Equal(11, GetLen(new ArrayPacket(B(0x0A)).AsReadOnlySequence(), 0, 0));
        Assert.Equal(302, GetLen(B(0xAC, 0x02), 0, 0));
        Assert.Equal(302, GetLen(new ArrayPacket(B(0xAC, 0x02)).AsReadOnlySequence(), 0, 0));
        Assert.Equal(302, GetLen(Chain(B(0xAC), B(0x02)).AsReadOnlySequence(), 0, 0));

        // 数据不足
        Assert.Equal(0, GetLen(new ArrayPacket(B(0x2C)).AsReadOnlySequence(), 0, 2));
        Assert.Equal(0, GetLen(new ArrayPacket(B(0x30, 0x00)).AsReadOnlySequence(), 2, 2));
        Assert.Equal(0, GetLen(new ArrayPacket(Array.Empty<Byte>()).AsReadOnlySequence(), 0, 2));

        // 变长字段跨轮截断（首字节高位为 1 表示继续）：按数据不足返回 0，不抛异常
        Assert.Equal(0, GetLen(new ArrayPacket(B(0xAC)).AsReadOnlySequence(), 0, 0));
        Assert.Equal(0, GetLen(B(0xAC), 0, 0));

        // 长度字段读出负值（最高位为 1）：视为无法定界返回 0，不得当作正帧长切帧
        Assert.Equal(0, GetLen(new ArrayPacket(B(0xFF, 0xFF, 0xFF, 0xFF)).AsReadOnlySequence(), 0, 4));

        // 帧长超出 Int32 上限：BodyLength 以 Int64 表示不溢出（旧 Int32 路径会溢出为负）
        var big = new LengthFieldCodec { Size = 4 }.TryParse(new ArrayPacket(B(0xFE, 0xFF, 0xFF, 0x7F)).AsReadOnlySequence());
        Assert.NotNull(big);
        Assert.Equal(4, big.Value.HeaderSize);
        Assert.Equal(0x7FFFFFFEL, big.Value.BodyLength);
    }

    /// <summary>长度字段定界便捷入口：按偏移与字段规格解析帧长（头部+体长）；无法定界返回 0</summary>
    private static Int32 GetLen(Byte[] data, Int32 offset, Int32 size) => GetLen(new ArrayPacket(data).AsReadOnlySequence(), offset, size);

    private static Int32 GetLen(ReadOnlySequence<Byte> buffer, Int32 offset, Int32 size)
    {
        var rs = new LengthFieldCodec { Offset = offset, Size = size }.TryParse(buffer);
        return rs == null ? 0 : (Int32)(rs.Value.HeaderSize + rs.Value.BodyLength);
    }
    #endregion

    #region 流式头解析（TryParse）
    [Fact]
    [DisplayName("DefaultMessage_流式头_跨段解析与状态位")]
    public void DefaultMessage_TryParse_Header()
    {
        var codec = new SrmpCodec();

        // 0x81：mode=2（响应）、Flag=1、Seq=7、负载10
        var head = B(0x81, 0x07, 10, 0);

        var rs = codec.TryParse(new ArrayPacket(head).AsReadOnlySequence());
        Assert.NotNull(rs);
        Assert.Equal(4 + 10L, rs.Value.HeaderSize + rs.Value.BodyLength);
        Assert.Equal(4, rs.Value.HeaderSize);

        var msg = Assert.IsType<DefaultMessage>(rs.Value.Message);
        Assert.Equal(MessageKinds.Response, msg.Kind);
        Assert.Equal((Byte)1, msg.Flag);
        Assert.Equal((Byte)7, msg.Sequence);

        // 跨段
        var rs2 = codec.TryParse(Chain(head[..2], head[2..]).AsReadOnlySequence());
        Assert.NotNull(rs2);
        Assert.Equal(4 + 10L, rs2.Value.HeaderSize + rs2.Value.BodyLength);
        Assert.Equal(4, rs2.Value.HeaderSize);

        // 数据不足：不消费、不产生对象
        Assert.Null(codec.TryParse(Chain(head[..3]).AsReadOnlySequence()));

        // 扩展长度：0xFFFF + LE int32 70000 → 8 字节头
        var ext = B(0x01, 0x02, 0xFF, 0xFF, 0x70, 0x11, 0x01, 0x00);
        var rs4 = codec.TryParse(Chain(ext[..5], ext[5..]).AsReadOnlySequence());
        Assert.NotNull(rs4);
        Assert.Equal(8 + 70000L, rs4.Value.HeaderSize + rs4.Value.BodyLength);
        Assert.Equal(8, rs4.Value.HeaderSize);

        // 扩展头不足
        Assert.Null(codec.TryParse(Chain(ext[..7]).AsReadOnlySequence()));
    }

    [Fact]
    [DisplayName("WebSocket_流式头_跨段解析FIN类型与掩码")]
    public void WebSocketMessage_TryParse_Header()
    {
        var codec = new WebSocketCodec();

        var masked = B(0x81, 0x80 | 5, 0x11, 0x22, 0x33, 0x44);

        var rs = codec.TryParse(new ArrayPacket(masked).AsReadOnlySequence());
        Assert.NotNull(rs);
        Assert.Equal(2 + 4 + 5L, rs.Value.HeaderSize + rs.Value.BodyLength);
        Assert.Equal(6, rs.Value.HeaderSize);

        var msg = (WsMessage)rs.Value.Message!;
        Assert.True(msg.Fin);
        Assert.Equal(WebSocketMessageType.Text, msg.Type);
        Assert.Equal(new Byte[] { 0x11, 0x22, 0x33, 0x44 }, msg.MaskKey);

        // 跨段（掩码跨段）
        var rs2 = codec.TryParse(Chain(masked[..1], masked[1..3], masked[3..]).AsReadOnlySequence());
        Assert.NotNull(rs2);
        Assert.Equal(2 + 4 + 5L, rs2.Value.HeaderSize + rs2.Value.BodyLength);
        Assert.Equal(6, rs2.Value.HeaderSize);
        Assert.Equal(new Byte[] { 0x11, 0x22, 0x33, 0x44 }, ((WsMessage)rs2.Value.Message!).MaskKey);

        // 掩码字节未到齐：头部未完整，返回 null
        Assert.Null(codec.TryParse(Chain(masked[..5]).AsReadOnlySequence()));

        // 126 扩展长度 + 掩码跨段
        var mid = B(0x82, 0x80 | 126, 0x01, 0x2C, 1, 2, 3, 4);
        var rs4 = codec.TryParse(Chain(mid[..4], mid[4..]).AsReadOnlySequence());
        Assert.NotNull(rs4);
        Assert.Equal(4 + 4 + 300L, rs4.Value.HeaderSize + rs4.Value.BodyLength);
        Assert.Equal(8, rs4.Value.HeaderSize);
    }
    #endregion
}
