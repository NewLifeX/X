using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using NewLife.Collections;

namespace NewLife.Data;

/// <summary>数据包辅助扩展方法</summary>
/// <remarks>
/// <para>提供数据包链式操作、数据转换、流处理等核心功能。</para>
/// <para><b>设计原则</b>：</para>
/// <list type="number">
/// <item>性能优先：单包快速路径，多包链式处理</item>
/// <item>内存友好：复用缓冲区，减少分配</item>
/// <item>安全防护：环检测，边界校验</item>
/// <item>兼容扩展：支持 null 调用，便于链式编程</item>  
/// </list>
/// </remarks>
public static class PacketHelper
{
    #region 快捷转换
    /// <summary>将字节数组包装为数据包</summary>
    /// <param name="data">字节数组</param>
    /// <returns>包装后的数据包</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ArrayPacket AsPacket(this Byte[] data) => new(data);

    /// <summary>将字节数组的指定区域包装为数据包</summary>
    /// <param name="data">字节数组</param>
    /// <param name="offset">起始偏移</param>
    /// <param name="count">数据长度，-1 表示到末尾</param>
    /// <returns>包装后的数据包</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ArrayPacket AsPacket(this Byte[] data, Int32 offset, Int32 count = -1) => new(data, offset, count);

    /// <summary>将数组段包装为数据包</summary>
    /// <param name="segment">数组段</param>
    /// <returns>包装后的数据包</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ArrayPacket AsPacket(this ArraySegment<Byte> segment) => new(segment);
    #endregion

    #region 链式操作
    /// <summary>将数据包追加到当前包链末尾</summary>
    /// <param name="pk">当前包链头节点</param>
    /// <param name="next">要追加的数据包（可包含自身链）</param>
    /// <returns>原包链头节点，便于链式调用</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item>时间复杂度：O(n)，n 为当前链长度</item>
    /// <item>防护机制：自引用检测、环路检测</item>
    /// <item>若 next 已包含链，会整体挂接</item>
    /// </list>
    /// </remarks>
    public static IPacket Append(this IPacket pk, IPacket next)
    {
        if (next == null) return pk;
        if (ReferenceEquals(pk, next)) return pk; // 防止自连接

        // 遍历到链尾
        var current = pk;
        while (current.Next != null)
        {
            // 环检测：避免形成循环链表
            if (ReferenceEquals(current.Next, pk)) break;
            current = current.Next;
        }

        current.Next = next;
        return pk;
    }

    /// <summary>将字节数组作为新包追加到末尾</summary>
    /// <param name="pk">当前包链头节点</param>
    /// <param name="data">字节数组数据</param>
    /// <returns>原包链头节点，便于链式调用</returns>
    public static IPacket Append(this IPacket pk, Byte[] data) => Append(pk, new ArrayPacket(data));
    #endregion

    #region 序列视图
    /// <summary>将数据包链转换为只读字节序列（零拷贝视图）</summary>
    /// <param name="pk">数据包（单段或链式）</param>
    /// <returns>只读字节序列；空包返回空序列</returns>
    /// <exception cref="ArgumentNullException">数据包为 null</exception>
    /// <remarks>
    /// <para>零拷贝：序列直接引用数据包底层缓冲区，不复制数据、不增加引用计数。</para>
    /// <para><b>生命周期</b>：序列仅在数据包句柄有效期内可用——拥有句柄 Dispose 归还池化缓冲、或借阅视图的接收缓冲被复用后，继续读取将产生未定义行为。需要跨轮/跨异步持有时，请先切出共享切片（<see cref="IOwnerPacket.Slice(Int32, Int32)"/>）或克隆。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var seq = pk.AsReadOnlySequence();
    /// foreach (var memory in seq) { /* 逐段零拷贝读取 */ }
    /// </code>
    /// </example>
    public static ReadOnlySequence<Byte> AsReadOnlySequence(this IPacket pk)
    {
        if (pk == null) throw new ArgumentNullException(nameof(pk));

        // 单段快速路径：直接以内存创建序列，免段对象分配
        if (pk.Next == null) return new ReadOnlySequence<Byte>(pk.GetMemory());

        var first = new PacketSequenceSegment(pk.GetMemory());
        var last = first;
        for (var node = pk.Next; node != null; node = node.Next)
        {
            last = last.Append(node.GetMemory());
        }

        return new ReadOnlySequence<Byte>(first, 0, last, last.Memory.Length);
    }

    /// <summary>将数据包链的指定窗口转换为只读字节序列（零拷贝视图，跨段窗口自动裁剪）</summary>
    /// <param name="pk">数据包（单段或链式）</param>
    /// <param name="offset">相对链首的起始偏移</param>
    /// <param name="count">字节数，-1 表示到链尾</param>
    /// <returns>只读字节序列；窗口为空时返回空序列</returns>
    /// <exception cref="ArgumentNullException">数据包为 null</exception>
    /// <exception cref="ArgumentOutOfRangeException">偏移或长度超出数据范围</exception>
    public static ReadOnlySequence<Byte> AsReadOnlySequence(this IPacket pk, Int32 offset, Int32 count = -1)
    {
        if (pk == null) throw new ArgumentNullException(nameof(pk));
        if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset), "Offset cannot be negative.");

        var total = pk.Total;
        if (count < 0) count = total - offset;
        if (offset > total || count > total - offset)
            throw new ArgumentOutOfRangeException(nameof(count), $"Offset {offset} with count {count} exceeds total length {total}");

        if (count == 0) return ReadOnlySequence<Byte>.Empty;

        // 单段快速路径
        if (pk.Next == null) return new ReadOnlySequence<Byte>(pk.GetMemory().Slice(offset, count));

        // 跳过 offset 所在的整段（空段跳过时不会停留）
        var skip = offset;
        var node = pk;
        while (node != null && skip >= node.Length)
        {
            skip -= node.Length;
            node = node.Next;
        }
        if (node == null) return ReadOnlySequence<Byte>.Empty;

        var remain = count;

        // 起点段：切掉段内偏移后取窗口内部分
        var memory = node.GetMemory();
        if (skip > 0) memory = memory.Slice(skip);

        var take = Math.Min(memory.Length, remain);
        var first = new PacketSequenceSegment(memory.Slice(0, take));
        var last = first;
        remain -= take;

        // 后续段：逐段裁剪，直到窗口取满
        for (node = node.Next; node != null && remain > 0; node = node.Next)
        {
            var mem = node.GetMemory();
            var len = Math.Min(mem.Length, remain);

            last = last.Append(mem.Slice(0, len));
            remain -= len;
        }

        return new ReadOnlySequence<Byte>(first, 0, last, last.Memory.Length);
    }

    /// <summary>把序列前缀复制到目标跨度（最多复制目标长度字节）</summary>
    /// <param name="source">源序列</param>
    /// <param name="destination">目标跨度</param>
    /// <returns>实际复制的字节数</returns>
    /// <remarks>用于把跨段头部拼入栈缓冲后复用跨度解析；跨段复制复用 BCL 的 <see cref="BuffersExtensions.CopyTo{T}"/></remarks>
    internal static Int32 CopyPrefix(ReadOnlySequence<Byte> source, Span<Byte> destination)
    {
        var count = (Int32)Math.Min(source.Length, destination.Length);
        if (count <= 0) return 0;

        source.Slice(0, count).CopyTo(destination[..count]);

        return count;
    }

    /// <summary>数据包序列段。把数据包链的单段内存适配为标准 <see cref="ReadOnlySequenceSegment{T}"/>，支撑零拷贝序列视图</summary>
    internal sealed class PacketSequenceSegment : ReadOnlySequenceSegment<Byte>
    {
        /// <summary>段承载的数据句柄。数据管道用它承担消费归还；序列桥接（无所有权）为 null</summary>
        public IPacket? Packet { get; set; }

        /// <summary>使用内存片段创建序列段</summary>
        /// <param name="memory">内存片段</param>
        public PacketSequenceSegment(ReadOnlyMemory<Byte> memory) => Memory = memory;

        /// <summary>在链尾追加内存片段，返回新的链尾段</summary>
        /// <param name="memory">内存片段</param>
        /// <returns>新的链尾段</returns>
        public PacketSequenceSegment Append(ReadOnlyMemory<Byte> memory)
        {
            var segment = new PacketSequenceSegment(memory)
            {
                RunningIndex = RunningIndex + Memory.Length,
            };

            Next = segment;

            return segment;
        }
    }
    #endregion

    #region 数据转换
    /// <summary>转换为字符串</summary>
    /// <param name="pk">数据包（允许 null）</param>
    /// <param name="encoding">字符编码，null 表示 UTF8</param>
    /// <param name="offset">起始偏移量（跨链全局）</param>
    /// <param name="count">读取字节数，-1 表示到末尾</param>
    /// <returns>转换后的字符串，pk 为 null 时返回 null</returns>
    /// <remarks>
    /// <para><b>性能优化策略</b>：</para>
    /// <list type="number">
    /// <item>单包：直接 Span 切片 + 编码，零分配</item>
    /// <item>多包链：StringBuilder 池化，按段拼接</item>
    /// <item>参数规范化：负偏移归零，超界截断</item>
    /// </list>
    /// </remarks>
    public static String ToStr(this IPacket pk, Encoding? encoding = null, Int32 offset = 0, Int32 count = -1)
    {
        // 兼容 null 扩展调用
        if (pk == null) return null!;

        // 参数规范化
        if (offset < 0) offset = 0;
        if (count == 0) return String.Empty;

        var total = pk.Total;
        if (total == 0 || offset >= total) return String.Empty;

        // 单包快速路径（热点优化）
        if (pk.Next == null)
        {
            var length = pk.Length;
            if (offset >= length) return String.Empty;

            var actualCount = count < 0 || count > length - offset ? length - offset : count;
            return pk.GetSpan().Slice(offset, actualCount).ToStr(encoding);
        }

        // 多包链处理
        var finalCount = count < 0 || count > total - offset ? total - offset : count;
        if (finalCount <= 0) return String.Empty;

        return ProcessMultiPacketString(pk, offset, finalCount, encoding);
    }

    /// <summary>处理多包链的字符串转换</summary>
    private static String ProcessMultiPacketString(IPacket pk, Int32 offset, Int32 count, Encoding? encoding)
    {
        var skip = offset;
        var remain = count;
        // 预分配容量：UTF-8 平均每字节约 1 个字符，避免 StringBuilder 扩容
        var sb = Pool.StringBuilder.Get();
        sb.EnsureCapacity(count);

        for (var current = pk; current != null && remain > 0; current = current.Next)
        {
            var span = current.GetSpan();

            // 跳过当前段
            if (skip >= span.Length)
            {
                skip -= span.Length;
                continue;
            }

            // 进入有效数据区
            if (skip > 0)
            {
                span = span[skip..];
                skip = 0;
            }

            // 限制读取长度
            if (span.Length > remain)
                span = span[..remain];

            sb.Append(span.ToStr(encoding));
            remain -= span.Length;
        }

        return sb.Return(true);
    }

    /// <summary>转换为十六进制字符串</summary>
    /// <param name="pk">数据包</param>
    /// <param name="maxLength">最大显示字节数，默认 32，-1 显示全部</param>
    /// <param name="separator">分隔符，null/空表示不分隔</param>
    /// <param name="groupSize">分组大小，0 表示每字节分隔，负数等同于 0</param>
    /// <returns>十六进制字符串表示</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item>基于 Total 判空，避免首段为空时误判</item>
    /// <item>多包处理：保持全局字节计数，确保分隔符在跨段时连续正确</item>
    /// </list>
    /// </remarks>
    public static String ToHex(this IPacket pk, Int32 maxLength = 32, String? separator = null, Int32 groupSize = 0)
    {
        if (pk == null) return null!;

        var total = pk.Total;
        if (total == 0 || maxLength == 0) return String.Empty;
        if (groupSize < 0) groupSize = 0;

        // 单包快速路径
        if (pk.Next == null)
            return pk.GetSpan().ToHex(separator, groupSize, maxLength);

        // 多包链处理
        return ProcessMultiPacketHex(pk, maxLength, separator, groupSize);
    }

    /// <summary>处理多包链的十六进制转换</summary>
    private static String ProcessMultiPacketHex(IPacket pk, Int32 maxLength, String? separator, Int32 groupSize)
    {
        var sb = Pool.StringBuilder.Get();
        const String HexDigits = "0123456789ABCDEF";
        var writtenBytes = 0;

        for (var current = pk; current != null; current = current.Next)
        {
            var span = current.GetSpan();

            for (var i = 0; i < span.Length && (maxLength < 0 || writtenBytes < maxLength); i++)
            {
                // 添加分隔符（非首字节且分隔符非空）
                if (writtenBytes > 0 && !separator.IsNullOrEmpty())
                {
                    if (groupSize <= 0 || writtenBytes % groupSize == 0)
                        sb.Append(separator);
                }

                // 转换字节为十六进制
                var b = span[i];
                sb.Append(HexDigits[b >> 4]);
                sb.Append(HexDigits[b & 0x0F]);
                writtenBytes++;
            }

            // 提前结束检查
            if (maxLength >= 0 && writtenBytes >= maxLength) break;
        }

        return sb.Return(true);
    }

    /// <summary>将数据包内容以文本形式流式写入 TextWriter，避免构建完整中间字符串</summary>
    /// <param name="pk">源数据包</param>
    /// <param name="writer">目标文本写入器</param>
    /// <param name="encoding">字符编码，null 表示 UTF8</param>
    /// <remarks>
    /// <para>适用于大数据包的文本输出场景（如日志、调试），避免 ToStr 产生的大量临时 String 分配。</para>
    /// <para>按段逐个解码写入，内存开销仅为单段大小而非总数据量。</para>
    /// </remarks>
    public static void WriteTo(this IPacket pk, TextWriter writer, Encoding? encoding = null)
    {
        if (pk == null || writer == null) return;

        encoding ??= Encoding.UTF8;

        for (var current = pk; current != null; current = current.Next)
        {
            var span = current.GetSpan();
            if (span.Length == 0) continue;

#if NETCOREAPP || NETSTANDARD2_1
            // 长度动态，统一走池化缓冲区
            var charCount = encoding.GetCharCount(span);
            using var chars = Pool.Rent<Char>(charCount);

            var written = encoding.GetChars(span, chars.Span);
            writer.Write(chars.Span[..written]);
#else
            // .NET Framework 回退路径
            if (current.TryGetArray(out var segment))
                writer.Write(encoding.GetChars(segment.Array!, segment.Offset, segment.Count));
            else
                writer.Write(encoding.GetString(span.ToArray()));
#endif
        }
    }
    #endregion

    #region 流操作
    /// <summary>将数据包内容复制到流</summary>
    /// <param name="pk">源数据包</param>
    /// <param name="stream">目标流</param>
    /// <remarks>在 .NET Framework 中可能存在二次拷贝</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> 为 null</exception>
    public static void CopyTo(this IPacket pk, Stream stream)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));

        for (var current = pk; current != null; current = current.Next)
        {
            if (current.TryGetArray(out var segment))
                stream.Write(segment.Array!, segment.Offset, segment.Count);
            else
                stream.Write(current.GetMemory());
        }
    }

    /// <summary>异步将数据包内容复制到流</summary>
    /// <param name="pk">源数据包</param>
    /// <param name="stream">目标流</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> 为 null</exception>
    public static async Task CopyToAsync(this IPacket pk, Stream stream, CancellationToken cancellationToken = default)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));

        for (var current = pk; current != null; current = current.Next)
        {
            if (current.TryGetArray(out var segment))
                await stream.WriteAsync(segment.Array!, segment.Offset, segment.Count, cancellationToken).ConfigureAwait(false);
            else
                await stream.WriteAsync(current.GetMemory(), cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>获取包含数据包内容的内存流</summary>
    /// <param name="pk">源数据包</param>
    /// <returns>可读写的内存流，位置已重置为 0</returns>
    public static Stream GetStream(this IPacket pk) => GetStream(pk, true);

    /// <summary>获取包含数据包内容的内存流</summary>
    /// <param name="pk">源数据包</param>
    /// <param name="writable">是否可写</param>
    /// <returns>可读写的内存流，位置已重置为 0</returns>
    public static Stream GetStream(this IPacket pk, Boolean writable)
    {
        if (pk.Next == null)
        {
            // 独立包且可获取数组段时直接返回内存流
            if (pk.TryGetArray(out var segment))
                return new MemoryStream(segment.Array!, segment.Offset, segment.Count, writable);
        }

        var ms = new MemoryStream(pk.Total);
        pk.CopyTo(ms);
        ms.Position = 0;
        return ms;
    }
    #endregion

    #region 数据段操作
    /// <summary>转换为数组段，多包时进行聚合复制</summary>
    /// <param name="pk">源数据包</param>
    /// <returns>数组段，单包时直接返回，多包时新建聚合数组</returns>
    public static ArraySegment<Byte> ToSegment(this IPacket pk)
    {
        // 单包且可获取数组段时直接返回
        if (pk.Next == null && pk.TryGetArray(out var segment))
            return segment;

        // 多包直接分配目标数组 + Span 拷贝，避免 MemoryStream 开销
        var buf = new Byte[pk.Total];
        var pos = 0;
        for (var current = pk; current != null; current = current.Next)
        {
            var span = current.GetSpan();
            span.CopyTo(buf.AsSpan(pos));
            pos += span.Length;
        }
        return new ArraySegment<Byte>(buf, 0, pos);
    }

    /// <summary>转换为数组段集合，每个元素对应链上一个包片段</summary>
    /// <param name="pk">源数据包</param>
    /// <returns>数组段列表，保持原始分段结构</returns>
    /// <remarks>不进行展开聚合，保持链式结构的分段信息</remarks>
    public static IList<ArraySegment<Byte>> ToSegments(this IPacket pk)
    {
        var segments = new List<ArraySegment<Byte>>(4); // 预分配 4 个元素优化扩容

        for (var current = pk; current != null; current = current.Next)
        {
            if (current.TryGetArray(out var segment))
                segments.Add(segment);
            else
                segments.Add(new ArraySegment<Byte>(current.GetSpan().ToArray(), 0, current.Length));
        }

        return segments;
    }

    /// <summary>转换为字节数组，始终返回新数组副本</summary>
    /// <param name="pk">源数据包</param>
    /// <returns>包含所有数据的新字节数组</returns>
    public static Byte[] ToArray(this IPacket pk)
    {
        // 单包直接转数组
        if (pk.Next == null)
            return pk.GetSpan().ToArray();

        // 多包直接分配目标数组 + Span 拷贝，避免 MemoryStream 开销
        var buf = new Byte[pk.Total];
        var pos = 0;
        for (var current = pk; current != null; current = current.Next)
        {
            var span = current.GetSpan();
            span.CopyTo(buf.AsSpan(pos));
            pos += span.Length;
        }
        return buf;
    }
    #endregion

    #region 数据读取
    /// <summary>读取指定范围的字节数据</summary>
    /// <param name="pk">源数据包</param>
    /// <param name="offset">相对起始偏移量</param>
    /// <param name="count">读取字节数，-1 表示到末尾</param>
    /// <returns>读取的字节数组，可能直接返回底层数组以优化性能</returns>
    /// <remarks>性能优化：读取全部数据且满足条件时，直接返回底层数组避免复制</remarks>
    public static Byte[] ReadBytes(this IPacket pk, Int32 offset = 0, Int32 count = -1)
    {
        if (pk.Next == null)
        {
            if (count < 0) count = pk.Length - offset;

            // 与本类的多包链分支保持一致：显式传入过大的 count 时夹紧到可用范围，避免越窗读
            if (offset < 0) offset = 0;
            if (count > pk.Length - offset) count = pk.Length - offset;
            if (count <= 0) return [];

            if (pk.TryGetArray(out var segment))
            {
                // 性能优化：读取全部且数组段完整时直接返回
                if (offset == 0 && count == pk.Length &&
                    segment.Offset == 0 && segment.Count == segment.Array!.Length)
                    return segment.Array;

                return segment.Array!.ReadBytes(segment.Offset + offset, count);
            }

            return pk.GetSpan().Slice(offset, count).ToArray();
        }

        // 多包链：直接跨段拷贝目标范围，避免先 ToArray 再截取的双重分配
        var total = pk.Total;
        if (count < 0) count = total - offset;
        if (offset + count > total) count = total - offset;
        if (count <= 0) return [];

        var buf = new Byte[count];
        var skip = offset;
        var remaining = count;
        var pos = 0;
        for (var current = pk; current != null && remaining > 0; current = current.Next)
        {
            var span = current.GetSpan();

            // 跳过当前段
            if (skip >= span.Length)
            {
                skip -= span.Length;
                continue;
            }

            // 进入有效数据区
            if (skip > 0)
            {
                span = span[skip..];
                skip = 0;
            }

            // 拷贝所需字节数
            var toCopy = Math.Min(span.Length, remaining);
            span[..toCopy].CopyTo(buf.AsSpan(pos));
            pos += toCopy;
            remaining -= toCopy;
        }
        return buf;
    }

    /// <summary>读取字节数据写入目标缓冲区，跨链节点自动续接</summary>
    /// <param name="pk">源数据包</param>
    /// <param name="buffer">目标缓冲区</param>
    /// <returns>实际读取的字节数（不超过缓冲区长度与数据总长度）</returns>
    /// <remarks>从数据包链起点开始读取；单节点时直接拷贝，链式时逐段续接</remarks>
    public static Int32 ReadBytes(this IPacket pk, Span<Byte> buffer)
    {
        if (buffer.Length == 0) return 0;

        var total = pk.Total;
        var count = buffer.Length < total ? buffer.Length : total;
        if (count <= 0) return 0;

        // 单节点直接拷贝
        if (pk.Next == null)
        {
            pk.GetSpan()[..count].CopyTo(buffer);
            return count;
        }

        // 多节点链：跨段续接拷贝
        var pos = 0;
        for (var node = pk; node != null && pos < count; node = node.Next)
        {
            var span = node.GetSpan();
            var c = Math.Min(span.Length, count - pos);
            if (c > 0)
            {
                span[..c].CopyTo(buffer[pos..]);
                pos += c;
            }
        }

        return pos;
    }

    /// <summary>获取数据包链的前缀跨度。帧首节点足够时直接引用；不足且为链式时拼入缓冲（不物化整帧）</summary>
    /// <param name="pk">数据包（单段或链式）</param>
    /// <param name="buffer">跨段拼读缓冲（调用方分配，长度不小于 count）</param>
    /// <param name="count">需要的最大前缀字节数</param>
    /// <returns>前缀跨度；帧首节点足够时零拷贝直引，跨段时拼入 buffer（数据总量不足时可能短于 count）</returns>
    /// <remarks>协议头部解析的公共入口：统一“首段直读 + 跨段拼读”两条路径，避免各协议各自手写拼读兜底。</remarks>
    internal static ReadOnlySpan<Byte> GetPrefix(this IPacket pk, Span<Byte> buffer, Int32 count)
    {
        // 帧首节点足够或无后续链：直接引用（数据不足由解析方判定）
        var span = pk.GetSpan();
        if (span.Length >= count || pk.Next == null) return span;

        // 前缀跨节点：拼入缓冲（最多 count 字节）
        if (buffer.Length > count) buffer = buffer[..count];

        return buffer[..pk.ReadBytes(buffer)];
    }

    /// <summary>在数据包链中查找目标字节序列，返回相对链头的全局偏移</summary>
    /// <param name="pk">源数据包，支持链式</param>
    /// <param name="data">目标字节序列</param>
    /// <returns>匹配起点相对链头的偏移；未找到返回 -1</returns>
    /// <remarks>
    /// 逐段扫描零拷贝：目标可能被切在段与段之间，因此保留“已扫描过的最后 k-1 个字节”，
    /// 与本段头部拼起来先查一次（跨段匹配的起点必然早于本段内匹配，先查才能返回全局最早匹配），再做段内 span 快速查找；
    /// 目标跨多个短段（含空段）同样能查到。
    /// </remarks>
    public static Int32 IndexOf(this IPacket pk, ReadOnlySpan<Byte> data)
    {
        if (pk == null) return -1;

        var k = data.Length;
        if (k == 0 || pk.Total < k) return -1;

        // 单段直接查找（最常见路径）
        if (pk.Next == null) return pk.GetSpan().IndexOf(data);

        // 单字节目标：逐段查找，不可能跨段
        if (k == 1)
        {
            var p1 = 0;
            for (var node = pk; node != null; node = node.Next)
            {
                var idx1 = node.GetSpan().IndexOf(data[0]);
                if (idx1 >= 0) return p1 + idx1;
                p1 += node.Length;
            }
            return -1;
        }

        var k1 = k - 1;
        // 已扫描数据的最后 k-1 字节；目标被切段时，它提供匹配起点所在的前缀
        var tail = k1 <= 256 ? stackalloc Byte[k1] : new Byte[k1];
        var tailLen = 0;
        // 拼接窗口：尾部缓冲 + 本段头部，各不超过 k-1 字节
        var winSize = k1 * 2;
        var win = winSize <= 256 ? stackalloc Byte[winSize] : new Byte[winSize];

        var pos = 0;
        for (var node = pk; node != null; node = node.Next)
        {
            var span = node.GetSpan();

            // 跨段查找：目标被切在段间，起点在“最后 k-1 字节”内、终点在本段头部。
            // 跨段匹配的起点必然早于本段内的任何匹配，必须先查，否则会返回更晚的段内匹配
            if (tailLen > 0 && span.Length > 0)
            {
                var headLen = Math.Min(k1, span.Length);
                var w = win[..(tailLen + headLen)];
                tail[..tailLen].CopyTo(w);
                span[..headLen].CopyTo(w[tailLen..]);

                var j = w.IndexOf(data);
                if (j >= 0) return pos - tailLen + j;
            }

            // 段内查找
            var idx = span.IndexOf(data);
            if (idx >= 0) return pos + idx;

            // 滚动更新尾部缓冲：保留“已扫描数据”的最后 k-1 字节
            if (span.Length >= k1)
            {
                span[^k1..].CopyTo(tail);
                tailLen = k1;
            }
            else if (span.Length > 0)
            {
                // 本段太短，先丢掉尾部缓冲里过期的头部，再追加本段
                var move = tailLen + span.Length - k1;
                if (move > 0)
                {
                    tail[move..tailLen].CopyTo(tail);
                    tailLen -= move;
                }
                span.CopyTo(tail[tailLen..]);
                tailLen += span.Length;
            }

            pos += span.Length;
        }

        return -1;
    }

    /// <summary>深度克隆数据包，完全复制数据内容</summary>
    /// <param name="pk">源数据包</param>
    /// <returns>独立的数据包副本，内存来自池，实际类型为 <see cref="IOwnerPacket"/>，调用方负责 Dispose</returns>
    public static IPacket Clone(this IPacket pk)
    {
        var total = pk.Total;
        var owner = new OwnerPacket(total);
        var dest = owner.GetSpan();
        var pos = 0;
        for (var current = pk; current != null; current = current.Next)
        {
            var span = current.GetSpan();
            span.CopyTo(dest[pos..]);
            pos += span.Length;
        }
        return owner;
    }
    #endregion

    #region 内存访问
    /// <summary>尝试获取内存片段，仅对单包有效</summary>
    /// <param name="pk">源数据包</param>
    /// <param name="span">输出的内存片段</param>
    /// <returns>是否成功获取（仅当无后续链节点时）</returns>
    public static Boolean TryGetSpan(this IPacket pk, out Span<Byte> span)
    {
        if (pk.Next == null)
        {
            span = pk.GetSpan();
            return true;
        }

        span = default;
        return false;
    }
    #endregion

    #region 头部扩展
    /// <summary>向前扩展头部空间（借位）。要求原包分配时已预留足够头部空间</summary>
    /// <param name="pk">原数据包</param>
    /// <param name="size">需要扩展的头部字节数</param>
    /// <returns>扩展后的数据包（零拷贝）</returns>
    /// <remarks>
    /// <para>组装内容时用 <c>new OwnerPacket(size, reserve)</c> 预留头部（参考 <see cref="Serialization.SpanSerializer.HeaderReserve"/>），下游即可零拷贝借位写入协议头。</para>
    /// <para><b>所有权</b>：结果与原包共享数据（拥有句柄引用计数各自释放），原句柄保持有效；未预留空间时请改用 <see cref="PrepareHeader"/> 自动降级。</para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">前置预留空间不足，或类型不支持借位</exception>
    public static IPacket ExpandHeader(this IPacket pk, Int32 size)
    {
        if (pk is OwnerPacket owner) return owner.ExpandHeader(size);
        if (pk is ArrayPacket ap) return ap.ExpandHeader(size);

        throw new InvalidOperationException($"类型 [{pk.GetType().Name}] 不支持头部借位扩展；头部写入需要预留空间的数据包（OwnerPacket/ArrayPacket）");
    }

    /// <summary>为负载准备帧头空间。已预留的拥有句柄零拷贝借位，其余新头节点挂接负载链；原负载句柄始终有效</summary>
    /// <param name="body">负载包（可空）</param>
    /// <param name="size">帧头字节数</param>
    /// <returns>带头部空间的结果包（头部区未初始化），调用方负责 Dispose</returns>
    /// <remarks>
    /// <para><b>两种策略（均为零拷贝）</b>：</para>
    /// <list type="number">
    /// <item>拥有句柄且已预留（<see cref="OwnerPacket"/>、<see cref="IPacket.FreeHeader"/> 足够）：向前借位共享，帧头落在原缓冲的预留区</item>
    /// <item>其余：新头节点挂接负载链——拥有句柄先切片为独占的共享链（引用计数各自持有），视图无所有权可直接挂接</item>
    /// </list>
    /// <para><b>所有权</b>：两种策略都不改动原负载句柄（借位为共享、链式为只读引用），消息负载在构建后依然可用。</para>
    /// <para><b>时效</b>：结果可能引用原负载缓冲，在帧发送完成前不得复用或改写该缓冲。</para>
    /// </remarks>
    public static IOwnerPacket PrepareHeader(this IPacket? body, Int32 size)
    {
        // 负长度会让借位分支变成“缩小窗口”而非扩展，必须显式拒绝
        if (size < 0) throw new ArgumentOutOfRangeException(nameof(size));

        if (body == null) return new OwnerPacket(size);

        // 拥有句柄且已预留：向前借位共享（零拷贝），原句柄保持有效
        if (body is OwnerPacket op && op.RefCount > 0 && op.FreeHeader >= size) return op.ExpandHeader(size);

        // 其余：新头节点挂接负载。拥有句柄先切片为独占共享链（引用计数各自持有）；
        // 视图无所有权，可直接挂接（Dispose 为空操作，链不会误放源）
        var payload = body is IOwnerPacket owner ? owner.Slice(0, -1) : body;

        return new OwnerPacket(size) { Next = payload };
    }
    #endregion
}
