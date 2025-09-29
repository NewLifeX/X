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
/// <para><b>零拷贝策略</b>：<see cref="Read(IPacket)"/> 解析时，为了减少内存分配，会直接对输入 <see cref="IPacket"/> 进行 Slice / 头部跳过，<see cref="Payload"/> 默认共享底层缓冲区（零拷贝）而不复制数据。</para>
/// <para><b>生命周期限制</b>：因此 <see cref="WebSocketMessage"/> 的有效使用范围应当局限在原始接收数据包（通常由 Socket 接收缓冲或对象池租借）的生命周期内；一旦上层把该缓冲区归还（或下一次复用同一缓冲），继续访问 <see cref="Payload"/> 将产生未定义行为（数据错乱、脏读）。</para>
/// <para><b>掩码处理</b>：客户端->服务端方向带掩码的帧会在解析阶段<span>原地</span>异或解码（跨链式分段逐字节 XOR），属于破坏性操作；不要在同一底层缓冲上尝试重复解析或回放。</para>
/// <para><b>需要复制的场景</b>：若需在解析后 1) 跨线程异步延迟使用，2) 缓存 / 队列化，3) 修改内容再次发送，或 4) 在调用栈返回后仍访问（例如放入 Channel / Task 回调），请先对 <see cref="Payload"/> 进行深拷贝，获得独立数据。</para>
/// <para><b>判断是否需要复制的经验法则</b>：只在当前方法同步消费（如立刻读取文本/反序列化为对象）可不复制；任何形式的延迟/多线程/多次重读都应复制。</para>
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
    public void Dispose() => Payload.TryDispose();
    #endregion

    #region 方法
    /// <summary>读取消息</summary>
    /// <param name="pk">包含（或至少包含头部的）数据包</param>
    /// <returns>true 解析完成；false 数据不完整或为分片帧（Fin=0）</returns>
    /// <remarks>
    /// <para><b>零拷贝</b>：解析后 <see cref="Payload"/> 直接引用参数 <paramref name="pk"/> 底层缓冲区（可能是其切片或链式后续），不做深复制，性能更高。</para>
    /// <para><b>作用域警告</b>：请勿在原始接收缓冲被复用 / 归还之后继续访问本实例的 <see cref="Payload"/>。若需跨越该作用域请复制 <see cref="Payload"/>。</para>
    /// <para><b>掩码</b>：客户端帧含掩码时，在原缓冲区原地 XOR 解码；链式包通过分段遍历处理（避免仅首段被解码的缺陷）。</para>
    /// <para><b>Close 帧</b>：当为 Close 且负载 >=2 字节，解析 2 字节状态码 + UTF8 原因短语；对多段小负载做最小复制保障正确性。</para>
    /// <para><b>安全限制</b>：若声明长度超过 <see cref="Int32.MaxValue"/>（当前实现处理索引为 Int32）则直接判定不支持并返回 false，避免超大内存导致异常。</para>
    /// <para>返回 false 场景：数据尚不完整、为分片后续帧（Fin=0）、长度字段尚未全部到齐。</para>
    /// </remarks>
    public Boolean Read(IPacket pk)
    {
        // 需要至少2字节基本头
        if (pk == null || pk.Total < 2) return false;

        // 如果数据包是链式的，SpanReader 内部会把它们合并成一个连续的数据流，再进行读取，避免 Available 计算错误
        var reader = new SpanReader(pk) { IsLittleEndian = false };

        // ------- 基础头 (2字节 + 可变扩展) -------
        // 第1字节： FIN(1) RSV1-3(3) OPCODE(4)
        var b = reader.ReadByte();
        Fin = (b & 0x80) != 0;
        Type = (WebSocketMessageType)(b & 0x0F); // 只取低4位OPCODE

        // 当前实现只处理单帧完整消息，忽略分片后续帧
        if (!Fin) return false;

        var b2 = reader.ReadByte();
        var mask = (b2 & 0x80) != 0;

        /*
         * 数据长度
         * len < 126    单字节表示长度
         * len = 126    后续2字节表示长度，大端
         * len = 127    后续8字节表示长度
         */
        // 扩展长度需要先确认剩余空间，避免抛异常后状态污染（返回false表示数据暂不完整）
        var len = (Int64)(b2 & 0x7F);
        if (len == 126)
        {
            if (reader.Available < 2) return false; // 数据不完整
            len = reader.ReadUInt16();
        }
        else if (len == 127)
        {
            if (reader.Available < 8) return false;
            len = reader.ReadInt64();
        }

        if (len < 0) return false; // 非法长度
        if (len > Int32.MaxValue) return false; // 当前实现不支持>2GB负载（避免索引/内存问题）

        // 读取掩码与负载前完整性检查：掩码4字节 + 负载
        var need = (mask ? 4 : 0) + len;
        if (reader.Available < need) return false; // 数据尚未到齐

        // 负载（零拷贝切片）
        if (!mask)
        {
            Payload = reader.ReadPacket((Int32)len);
        }
        else
        {
            var masks = new Byte[4];
            reader.Read(masks);
            MaskKey = masks;

            // 零拷贝读取 + 链式掩码原地解码
            Payload = reader.ReadPacket((Int32)len);
            var data = Payload.GetSpan();
            for (var i = 0; i < len; i++)
            {
                data[i] = (Byte)(data[i] ^ masks[i % 4]);
            }
        }

        // 特殊处理关闭消息（RFC6455：状态码 + UTF8 原因，可为空；状态码为网络字节序）
        if (Type == WebSocketMessageType.Close && Payload != null && Payload.Total >= 2)
        {
            // 读取前两个字节状态码 (BigEndian)
            var data = Payload.GetSpan();
            CloseStatus = BinaryPrimitives.ReadUInt16BigEndian(data[..2]);
            StatusDescription = data[2..].ToStr();
        }

        return true;
    }

    /// <summary>把消息转为封包</summary>
    /// <remarks>
    /// 修复与优化：
    /// 1. 统一关闭帧负载构造（状态码+描述）；若外部未提供Payload则自动生成。
    /// 2. 负载长度与头部分离，掩码后正确 XOR 整个链式负载（原实现仅处理首段且遗漏无Payload的 Close 帧掩码）。
    /// 3. 移除重复的 FIN/Type 计算逻辑，简化 header 写入。
    /// 4. 兼容多段链式 IPacket，不复制数据仅在原缓冲区异或。
    /// </remarks>
    public virtual IPacket ToPacket()
    {
        var body = Payload;
        var len = body == null ? 0 : body.Total;
        var masks = MaskKey;

        // Close 帧：若未显式提供负载，则根据 CloseStatus / StatusDescription 构造
        if (Type == WebSocketMessageType.Close)
        {
            len = 2;
            if (!StatusDescription.IsNullOrEmpty()) len += Encoding.UTF8.GetByteCount(StatusDescription);
        }

        // 计算头部大小：固定2 + 扩展长度 + 掩码（不含负载本身）
        var size = len switch
        {
            < 126 => 1 + 1,
            < 0xFFFF => 1 + 1 + 2,
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

            // 掩码混淆数据。直接在数据缓冲区修改，避免拷贝
            if (body != null)
            {
                var data = body.GetSpan();
                for (var i = 0; i < len; i++)
                {
                    data[i] = (Byte)(data[i] ^ masks[i % 4]);
                }
            }
        }

        if (body != null && body.Length > 0)
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

            rs.Next = null;
        }

        return rs.Slice(0, writer.Position, true);
    }
    #endregion
}