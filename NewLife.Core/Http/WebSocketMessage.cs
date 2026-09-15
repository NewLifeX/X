using System.Buffers.Binary;
using System.Text;
using NewLife.Buffers;
using NewLife.Data;

namespace NewLife.Http;

/// <summary>WebSocket消息类型</summary>
public enum WebSocketMessageType
{
    /// <summary>附加数据</summary>
    Data = 0,

    /// <summary>文本数据</summary>
    Text = 1,

    /// <summary>二进制数据</summary>
    Binary = 2,

    /// <summary>连接关闭</summary>
    Close = 8,

    /// <summary>心跳</summary>
    Ping = 9,

    /// <summary>心跳响应</summary>
    Pong = 10,
}

/// <summary>WebSocket消息</summary>
/// <remarks>
/// <para><b>零拷贝策略</b>：<see cref="Read(IPacket)"/> 解析时，为了减少内存分配，会直接对输入 <see cref="IPacket"/> 进行 Slice / 头部跳过，<see cref="Payload"/> 共享底层缓冲区（零拷贝）而不复制数据。</para>
/// <para><b>生命周期</b>：Payload 的所有权随输入而定——拥有句柄输入（接收链路）时为共享切片（引用计数），独立持有、可跨轮/跨线程使用，用毕 <see cref="Dispose"/>；借阅视图输入（直接调用）时无所有权，仅限当前接收缓冲有效期内同步使用——缓冲归还或复用后继续访问将产生未定义行为（数据错乱、脏读）；需要对内容做破坏性修改或长期保存时，请先深拷贝 <see cref="Payload"/>。</para>
/// <para><b>掩码处理</b>：客户端->服务端方向带掩码的帧会在解析阶段<span>原地</span>异或解码（链式负载逐段 XOR，掩码跨段连续），属于破坏性操作；不要在同一底层缓冲上尝试重复解析或回放。</para>
/// </remarks>
public class WebSocketMessage : IDisposable
{
    #region 属性
    /// <summary>消息是否结束</summary>
    public Boolean Fin { get; set; }

    /// <summary>消息类型</summary>
    public WebSocketMessageType Type { get; set; }

    /// <summary>加密数据的掩码（客户端->服务端方向）</summary>
    public Byte[]? MaskKey { get; set; }

    /// <summary>负载数据</summary>
    public IPacket? Payload { get; set; }

    /// <summary>关闭状态。仅用于Close消息</summary>
    public Int32 CloseStatus { get; set; }

    /// <summary>关闭状态描述。仅用于Close消息</summary>
    public String? StatusDescription { get; set; }
    #endregion

    #region 构造
    /// <summary>销毁。回收数据包到内存池</summary>
    public void Dispose()
    {
        Payload.TryDispose();
        Payload = null;
    }
    #endregion

    #region 方法
    /// <summary>读取消息</summary>
    /// <param name="pk">包含（或至少包含头部的）数据包（帧首节点需含完整协议头；PacketCodec 输出的帧首已保证，直调链式帧首段不足时自动拼读兼容）</param>
    /// <returns>true 解析完成；false 数据不完整或为分片帧（Fin=0）</returns>
    /// <remarks>
    /// <para><b>零拷贝</b>：解析后 <see cref="Payload"/> 直接引用参数 <paramref name="pk"/> 底层缓冲区（其切片），不做深复制，性能更高。</para>
    /// <para><b>帧首头部</b>：头部与掩码合计最多 14 字节，要求完整落在帧首节点内（PacketCodec 输出帧首已保证）；直调链式帧且首段不足时自动拼入栈缓冲兼容，负载可继续为链式节点。</para>
    /// <para><b>入参所有权</b>：本方法不释放 <paramref name="pk"/>，由调用方负责释放；负载为共享切片（引用计数）独立持有，帧句柄释放后负载仍可继续使用。</para>
    /// <para><b>作用域警告</b>：负载为借阅视图（<see cref="ArrayPacket"/>）时无所有权，请勿在原始接收缓冲被复用 / 归还之后继续访问；拥有帧负载持有独立引用，不受此限。</para>
    /// <para><b>掩码</b>：客户端帧含掩码时，在原缓冲区原地 XOR 解码，链式负载逐段处理、掩码跨段连续（属破坏性操作）。</para>
    /// <para><b>Close 帧</b>：当为 Close 且负载 >=2 字节，解析 2 字节状态码 + UTF8 原因短语（跨段链式负载同样零拷贝读取）。</para>
    /// <para><b>安全限制</b>：若声明长度超过 <see cref="Int32.MaxValue"/>（当前实现处理索引为 Int32）则直接判定不支持并返回 false，避免超大内存导致异常。</para>
    /// <para>返回 false 场景：数据尚不完整、为分片后续帧（Fin=0）、长度字段尚未全部到齐。</para>
    /// </remarks>
    public Boolean Read(IPacket pk)
    {
        // 需要至少2字节基本头
        if (pk == null || pk.Total < 2) return false;

        // ------- 基础头 (2字节 + 可变扩展) + 掩码（合计最多14字节） -------
        var total = pk.Total;

        // 头部与掩码最多 14 字节：帧首节点足够时直接引用；不足且为链式时拼入栈缓冲（兼容直调链式帧；PacketCodec 输出帧首已保证）
        Span<Byte> buf = stackalloc Byte[14];
        var span = pk.Length >= 14 || pk.Next == null ? pk.GetSpan() : buf[..pk.ReadBytes(buf)];

        // 第1字节： FIN(1) RSV1-3(3) OPCODE(4)
        var b = span[0];
        Fin = (b & 0x80) != 0;
        Type = (WebSocketMessageType)(b & 0x0F); // 只取低4位OPCODE

        // 当前实现只处理单帧完整消息，忽略分片后续帧
        if (!Fin) return false;

        var b2 = span[1];
        var mask = (b2 & 0x80) != 0;

        /*
         * 数据长度
         * len < 126    单字节表示长度
         * len = 126    后续2字节表示长度，大端
         * len = 127    后续8字节表示长度
         */
        // 扩展长度需要先确认剩余空间，避免越界（返回false表示数据暂不完整）
        var headerLen = 2;      // 已消费的头部字节数（基础头 + 扩展长度）
        var len = (Int64)(b2 & 0x7F);
        if (len == 126)
        {
            if (total - headerLen < 2) return false; // 数据不完整
            len = ((Int64)span[2] << 8) | span[3];
            headerLen += 2;
        }
        else if (len == 127)
        {
            if (total - headerLen < 8) return false;
            len = 0;
            for (var i = 0; i < 8; i++) len = (len << 8) | span[headerLen + i];
            headerLen += 8;
        }

        if (len < 0) return false; // 非法长度
        if (len > Int32.MaxValue) return false; // 当前实现不支持>2GB负载（避免索引/内存问题）

        // 读取掩码与负载前完整性检查：掩码4字节 + 负载，以整帧总量判定
        var need = (mask ? 4 : 0) + len;
        if (total - headerLen < need) return false; // 数据尚未到齐

        // 掩码先于负载切片读出，供 XOR 解码使用
        var masks = mask ? new Byte[4] : null;
        if (masks != null)
        {
            for (var i = 0; i < masks.Length; i++) masks[i] = span[headerLen + i];
            MaskKey = masks;
        }

        // 负载：共享切片（引用计数）独立持有，链式帧切出链式负载；入参帧句柄由调用方释放
        Payload = pk.Slice(headerLen + (mask ? 4 : 0), (Int32)len);

        // 掩码原地 XOR 解码（链式负载逐段遍历，掩码跨段连续；属破坏性操作）
        if (masks != null)
        {
            var idx = 0;
            for (var node = Payload; node != null; node = node.Next)
            {
                var data = node.GetSpan();
                for (var i = 0; i < data.Length; i++)
                {
                    data[i] ^= masks[idx++ & 3];
                }
            }
        }

        // 特殊处理关闭消息（RFC6455：状态码 + UTF8 原因，可为空；状态码为网络字节序）
        if (Type == WebSocketMessageType.Close && Payload != null && Payload.Total >= 2)
        {
            CloseStatus = (Payload[0] << 8) | Payload[1];
            StatusDescription = Payload.ToStr(null, 2);
        }

        return true;
    }

    /// <summary>计算完整 WebSocket 帧的总字节数（含头部）。仅 Read 成功后有效。</summary>
    /// <returns>头部字节数（2 + 扩展长度字段 + 掩码）加负载字节数</returns>
    public Int32 GetFrameSize()
    {
        var payload = Payload?.Total ?? 0;
        var extLen = payload > 65535 ? 8 : (payload > 125 ? 2 : 0);
        var maskLen = MaskKey != null ? 4 : 0;
        return 2 + extLen + maskLen + payload;
    }

    /// <summary>从帧头读取完整帧长度（含头部），供 PacketCodec 用作 GetLength 委托；帧首节点含完整头部时直读，不足且链式时拼接</summary>
    /// <param name="pk">数据包（PacketCodec 缓存的帧首；直调可为链式）</param>
    /// <returns>完整帧总字节数（头部+负载）；数据不足以确定长度时返回 0</returns>
    internal static Int32 GetFrameTotalLength(IPacket pk)
    {
        var span = pk.GetSpan();
        if (span.Length >= 14 || pk.Next == null) return GetFrameTotalLength(span);

        // 帧头可能跨节点：拼接前 14 字节（2 字节基础头 + 8 字节扩展长度 + 4 字节掩码）
        Span<Byte> buf = stackalloc Byte[14];
        var n = pk.ReadBytes(buf);

        return GetFrameTotalLength(buf[..n]);
    }

    /// <summary>从原始帧头字节读取完整帧长度（含头部）。</summary>
    /// <param name="span">原始字节，至少包含 2 字节 WebSocket 帧头</param>
    /// <returns>完整帧总字节数（头部+负载）；数据不足以确定长度时返回 0</returns>
    internal static Int32 GetFrameTotalLength(ReadOnlySpan<Byte> span)
    {
        if (span.Length < 2) return 0;

        var b2 = span[1];
        var masked = (b2 & 0x80) != 0;
        var len7 = b2 & 0x7F;

        Int64 payloadLen;
        var headerLen = 2;

        if (len7 == 126)
        {
            if (span.Length < 4) return 0;
            payloadLen = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(2));
            headerLen = 4;
        }
        else if (len7 == 127)
        {
            if (span.Length < 10) return 0;
            payloadLen = BinaryPrimitives.ReadInt64BigEndian(span.Slice(2));
            if (payloadLen < 0 || payloadLen > Int32.MaxValue) return 0;
            headerLen = 10;
        }
        else
        {
            payloadLen = len7;
        }

        if (masked) headerLen += 4;
        return (Int32)(headerLen + payloadLen);
    }

    /// <summary>把消息转为封包</summary>
    /// <remarks>
    /// <para><b>Close 帧</b>：正文只由 <see cref="CloseStatus"/> 和 <see cref="StatusDescription"/> 构造，忽略 <see cref="Payload"/>（RFC 6455 §5.5.1 规定关闭帧正文为 2 字节状态码加原因）。</para>
    /// <para><b>零拷贝</b>：拥有负载且前置空间足够时原地扩展头部（源句柄随即作废，调用方不得继续使用原负载句柄）；否则新建头部节点与负载组成链式数据包。</para>
    /// <para><b>掩码</b>：<see cref="MaskKey"/> 非空时对负载原地异或编码（链式负载逐段处理、跨段连续），属破坏性操作，会改写负载底层缓冲区。</para>
    /// </remarks>
    public virtual IPacket ToPacket()
    {
        var body = Payload;
        var len = body == null ? 0 : body.Total;
        // 先记录负载有无：ExpandHeader 可能原地接管拥有帧，源实例随即作废，不能再读 body
        var hasBody = len > 0;
        var masks = MaskKey;

        // Close 帧：正文只由状态码和描述构造，忽略 Payload（RFC 6455 §5.5.1 规定关闭帧正文为状态码+原因）
        if (Type == WebSocketMessageType.Close)
        {
            body = null;
            hasBody = false;

            len = 2;
            if (!StatusDescription.IsNullOrEmpty()) len += Encoding.UTF8.GetByteCount(StatusDescription);
        }

        // 计算头部大小：固定2 + 扩展长度 + 掩码（不含负载本身）
        var size = len switch
        {
            < 126 => 1 + 1,
            <= 0xFFFF => 1 + 1 + 2,
            _ => 1 + 1 + 8,
        };
        if (masks != null) size += masks.Length;
        if (Type == WebSocketMessageType.Close) size += len;

        var rs = body.ExpandHeader(size);
        var writer = new SpanWriter(rs) { IsLittleEndian = false };

        // FIN + OPCODE
        writer.WriteByte((Byte)(0x80 | (Byte)Type));

        /*
         * 数据长度
         * len < 126    单字节表示长度
         * len = 126    后续2字节表示长度，大端
         * len = 127    后续8字节表示长度
         */

        if (masks == null)
        {
            if (len < 126)
            {
                writer.WriteByte((Byte)len);
            }
            else if (len <= 0xFFFF)
            {
                writer.WriteByte(126);
                writer.Write((Int16)len);
            }
            else
            {
                writer.WriteByte(127);
                writer.Write((Int64)len);
            }
        }
        else
        {
            if (len < 126)
            {
                writer.WriteByte((Byte)(len | 0x80));
            }
            else if (len <= 0xFFFF)
            {
                writer.WriteByte(126 | 0x80);
                writer.Write((Int16)len);
            }
            else
            {
                writer.WriteByte(127 | 0x80);
                writer.Write((Int64)len);
            }

            writer.Write(masks);

            // 掩码混淆数据。直接在数据缓冲区修改，避免拷贝（链式负载逐段遍历，掩码跨段连续）。
            // 拥有帧可能在 ExpandHeader 时被原地接管而作废，因此统一从 rs 链上取数据，跳过头部区域
            if (hasBody)
            {
                var idx = 0;
                for (var node = rs; node != null; node = node.Next)
                {
                    var data = node.GetSpan();
                    for (var i = node == rs ? Math.Min(size, data.Length) : 0; i < data.Length; i++)
                    {
                        data[i] = (Byte)(data[i] ^ masks[idx++ & 3]);
                    }
                }
            }
        }

        if (hasBody)
        {
            // 注意body可能是链式数据包
            //writer.Write(body.GetSpan());

            // 扩展得到的数据包，直接写入了头部，尾部数据不用拷贝也无需切片
            return rs;

            //return rs.Slice(0, writer.Position).Append(body);
        }
        else if (Type == WebSocketMessageType.Close)
        {
            writer.Write((Int16)CloseStatus);
            if (!StatusDescription.IsNullOrEmpty()) writer.Write(StatusDescription, -1);

            // 有掩码时 Close 状态码+描述也需要 XOR 编码，因为读取端会对整个 Payload 做 XOR 解码
            if (masks != null)
            {
                var span = rs.GetSpan();
                // 定位到 Close 负载起始位置（HeaderSize 就是 writer.Position 写入 Close 数据前的位置）
                var offset = writer.Position - len;
                for (var i = 0; i < len; i++)
                {
                    span[offset + i] = (Byte)(span[offset + i] ^ masks[i % 4]);
                }
            }
        }

        // 共享窗口切片，随后释放扩展句柄自身的引用（结果句柄独立持有）
        var result = rs.Slice(0, writer.Position);
        rs.TryDispose();
        return result;
    }
    #endregion
}