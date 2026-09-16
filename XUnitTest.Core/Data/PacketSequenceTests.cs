using System.Buffers;
using System.ComponentModel;
using NewLife.Data;
using NewLife.Http;
using NewLife.Messaging;
using NewLife.Net.Handlers;
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
        Assert.Equal(70008, DefaultMessage.GetLength(Chain(ext[..3], ext[3..])));
        Assert.Equal(70008, DefaultMessage.GetLength(Chain(ext[..3], ext[3..5], ext[5..])));

        // WebSocket：127 扩展长度（8 字节大端）落在第二段
        var longBuf = B(0x82, 127, 0, 0, 0, 0, 0, 1, 0x11, 0x70);
        Assert.Equal(70010, WebSocketMessage.GetFrameTotalLength(Chain(longBuf[..5], longBuf[5..])));

        // 数据不足：头部不完整时返回 0
        Assert.Equal(0, DefaultMessage.GetLength(Chain(ext[..3])));
        Assert.Equal(0, WebSocketMessage.GetFrameTotalLength(Chain(longBuf[..9])));
    }
    #endregion

    #region 序列定界
    [Fact]
    [DisplayName("DefaultMessage_序列定界_普通包与链式跨段")]
    public void DefaultMessage_GetLength_Sequence()
    {
        // 负载10字节：Flag+Seq+Len(4字节头)
        var buf = B(0x01, 0x02, 10, 0).Concat(Range(100, 10)).ToArray();

        Assert.Equal(14, DefaultMessage.GetLength(buf)); // 跨度版本对照
        Assert.Equal(14, DefaultMessage.GetLength(new ArrayPacket(buf).AsReadOnlySequence()));

        // 链式：头部横跨节点
        Assert.Equal(14, DefaultMessage.GetLength(Chain(buf[..2], buf[2..]).AsReadOnlySequence()));
        Assert.Equal(14, DefaultMessage.GetLength(Chain(buf[..3], buf[3..]).AsReadOnlySequence()));
        Assert.Equal(14, DefaultMessage.GetLength(Chain(buf[..1], buf[1..3], buf[3..]).AsReadOnlySequence()));
    }

    [Fact]
    [DisplayName("DefaultMessage_序列定界_扩展长度与数据不足")]
    public void DefaultMessage_GetLength_Extended()
    {
        // 扩展长度：0xFFFF + LE int32 70000
        var ext = B(0x01, 0x02, 0xFF, 0xFF, 70000 & 0xFF, (70000 >> 8) & 0xFF, (70000 >> 16) & 0xFF, (70000 >> 24) & 0xFF);

        Assert.Equal(70008, DefaultMessage.GetLength(ext));
        Assert.Equal(70008, DefaultMessage.GetLength(new ArrayPacket(ext).AsReadOnlySequence()));
        Assert.Equal(70008, DefaultMessage.GetLength(Chain(ext[..4], ext[4..]).AsReadOnlySequence()));
        Assert.Equal(70008, DefaultMessage.GetLength(Chain(ext[..7], ext[7..]).AsReadOnlySequence()));

        // 数据不足：扩展头不完整 / 基础头不完整
        Assert.Equal(0, DefaultMessage.GetLength(Chain(ext[..7]).AsReadOnlySequence()));
        Assert.Equal(0, DefaultMessage.GetLength(Chain(ext[..3]).AsReadOnlySequence()));
        Assert.Equal(0, DefaultMessage.GetLength(new ArrayPacket(Array.Empty<Byte>()).AsReadOnlySequence()));
    }

    [Fact]
    [DisplayName("WebSocket_序列定界_三种长度形态与掩码")]
    public void WebSocketMessage_GetFrameTotalLength_Sequence()
    {
        // 短长度：2 + 125
        var shortBuf = B(0x82, 125);
        Assert.Equal(127, WebSocketMessage.GetFrameTotalLength(shortBuf));
        Assert.Equal(127, WebSocketMessage.GetFrameTotalLength(new ArrayPacket(shortBuf).AsReadOnlySequence()));
        Assert.Equal(127, WebSocketMessage.GetFrameTotalLength(Chain(shortBuf[..1], shortBuf[1..]).AsReadOnlySequence()));

        // 126：2字节大端 300 → 4 + 300
        var midBuf = B(0x82, 126, 0x01, 0x2C);
        Assert.Equal(304, WebSocketMessage.GetFrameTotalLength(new ArrayPacket(midBuf).AsReadOnlySequence()));
        Assert.Equal(304, WebSocketMessage.GetFrameTotalLength(Chain(midBuf[..2], midBuf[2..]).AsReadOnlySequence()));
        Assert.Equal(304, WebSocketMessage.GetFrameTotalLength(Chain(midBuf[..3], midBuf[3..]).AsReadOnlySequence()));

        // 127：8字节大端 70000 → 10 + 70000
        var longBuf = B(0x82, 127, 0, 0, 0, 0, 0, 1, 0x11, 0x70);
        Assert.Equal(70010, WebSocketMessage.GetFrameTotalLength(longBuf));
        Assert.Equal(70010, WebSocketMessage.GetFrameTotalLength(new ArrayPacket(longBuf).AsReadOnlySequence()));
        Assert.Equal(70010, WebSocketMessage.GetFrameTotalLength(Chain(longBuf[..5], longBuf[5..]).AsReadOnlySequence()));

        // 掩码：短帧 5 字节负载 → 2 + 4 + 5
        var masked = B(0x82, 0x80 | 5);
        Assert.Equal(11, WebSocketMessage.GetFrameTotalLength(new ArrayPacket(masked).AsReadOnlySequence()));

        // 数据不足
        Assert.Equal(0, WebSocketMessage.GetFrameTotalLength(new ArrayPacket(B(0x82)).AsReadOnlySequence()));
        Assert.Equal(0, WebSocketMessage.GetFrameTotalLength(Chain(midBuf[..3]).AsReadOnlySequence()));
        Assert.Equal(0, WebSocketMessage.GetFrameTotalLength(Chain(longBuf[..9]).AsReadOnlySequence()));
    }

    [Fact]
    [DisplayName("长度字段_序列定界_各字段规格与链式跨段")]
    public void MessageCodec_GetLength_Sequence()
    {
        // 2字节小端：300 → 帧 302
        var le = B(0x2C, 0x01);
        Assert.Equal(302, MessageCodec<IPacket>.GetLength(le, 0, 2));
        Assert.Equal(302, MessageCodec<IPacket>.GetLength(new ArrayPacket(le).AsReadOnlySequence(), 0, 2));
        Assert.Equal(302, MessageCodec<IPacket>.GetLength(Chain(le[..1], le[1..]).AsReadOnlySequence(), 0, 2));

        // 偏移2（MQTT风格）：[前缀2][长度2]，帧长含前缀 = 2 + 300 + 2
        var mqtt = B(0x30, 0x00, 0x2C, 0x01);
        Assert.Equal(304, MessageCodec<IPacket>.GetLength(mqtt, 2, 2));
        Assert.Equal(304, MessageCodec<IPacket>.GetLength(new ArrayPacket(mqtt).AsReadOnlySequence(), 2, 2));
        Assert.Equal(304, MessageCodec<IPacket>.GetLength(Chain(mqtt[..3], mqtt[3..]).AsReadOnlySequence(), 2, 2));

        // 2字节大端 / 4字节小端 / 1字节
        Assert.Equal(302, MessageCodec<IPacket>.GetLength(new ArrayPacket(B(0x01, 0x2C)).AsReadOnlySequence(), 0, -2));
        Assert.Equal(304, MessageCodec<IPacket>.GetLength(new ArrayPacket(B(0x2C, 0x01, 0, 0)).AsReadOnlySequence(), 0, 4));
        Assert.Equal(11, MessageCodec<IPacket>.GetLength(new ArrayPacket(B(10)).AsReadOnlySequence(), 0, 1));

        // 变长编码：10 → 10+1；300 → 300+2；跨段
        Assert.Equal(11, MessageCodec<IPacket>.GetLength(new ArrayPacket(B(0x0A)).AsReadOnlySequence(), 0, 0));
        Assert.Equal(302, MessageCodec<IPacket>.GetLength(B(0xAC, 0x02), 0, 0));
        Assert.Equal(302, MessageCodec<IPacket>.GetLength(new ArrayPacket(B(0xAC, 0x02)).AsReadOnlySequence(), 0, 0));
        Assert.Equal(302, MessageCodec<IPacket>.GetLength(Chain(B(0xAC), B(0x02)).AsReadOnlySequence(), 0, 0));

        // 数据不足
        Assert.Equal(0, MessageCodec<IPacket>.GetLength(new ArrayPacket(B(0x2C)).AsReadOnlySequence(), 0, 2));
        Assert.Equal(0, MessageCodec<IPacket>.GetLength(new ArrayPacket(B(0x30, 0x00)).AsReadOnlySequence(), 2, 2));
        Assert.Equal(0, MessageCodec<IPacket>.GetLength(new ArrayPacket(Array.Empty<Byte>()).AsReadOnlySequence(), 0, 2));
    }
    #endregion
}
