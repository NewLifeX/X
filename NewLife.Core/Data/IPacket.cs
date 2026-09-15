using System.Buffers;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using NewLife.Collections;

#if DEBUG
using NewLife.Log;
#endif

namespace NewLife.Data;

/// <summary>数据包接口。基于内存共享理念，统一提供数据包处理能力</summary>
/// <remarks>
/// <para>常用于网络编程和协议解析，通过对象池复用内存避免大量分配和拷贝。</para>
/// <para>数据包接口一般由结构体实现以降低 GC 压力。</para>
/// <para><b>内存管理权转移规则</b>：调用栈上层（获得包的一方）负责最终释放。</para>
/// <list type="bullet">
/// <item>非阻塞 Socket：接收方申请与释放；解析逻辑只消费不负责释放</item>
/// <item>阻塞 Socket：接收函数申请，外部使用方释放，管理权可进一步传递</item>
/// </list>
/// <para>切片 <see cref="Slice(Int32, Int32)"/> 共享底层缓冲区；<see cref="OwnerPacket"/> 统一为引用计数共享语义——切片返回独立句柄，各自释放，最后一个归还内存池。</para>
/// <para><b>重要</b>：所有临时获得的 <see cref="Span{T}"/>/<see cref="Memory{T}"/> 仅在当前所有权生命周期内短暂使用，禁止缓存到异步/长期结构中。</para>
/// </remarks>
public interface IPacket
{
    /// <summary>数据长度。仅当前数据包，不包括 <see cref="Next"/></summary>
    Int32 Length { get; }

    /// <summary>下一个链式包</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    IPacket? Next { get; set; }

    /// <summary>总长度。包括 <see cref="Next"/> 链的长度</summary>
    Int32 Total { get; }

    /// <summary>获取/设置 指定绝对位置的字节（跨越链式包）</summary>
    /// <param name="index">0 基起始的全局位置</param>
    Byte this[Int32 index] { get; set; }

    /// <summary>获取分片视图（仅当前数据包，不包括 <see cref="Next"/> 链）。在管理权生命周期内短暂使用，禁止长期保存</summary>
    Span<Byte> GetSpan();

    /// <summary>获取内存块（仅当前数据包，不包括 <see cref="Next"/> 链）。在管理权生命周期内短暂使用，禁止长期保存</summary>
    Memory<Byte> GetMemory();

    /// <summary>切片得到新数据包，共享底层缓冲区以减少分配</summary>
    /// <remarks>
    /// <para><see cref="OwnerPacket"/> 实现为引用计数共享：返回独立句柄，与原包同时可用；各自 <c>Dispose</c>，最后一个释放时归还内存池。</para>
    /// <para>取出子窗口后不再使用原句柄时应随即释放（如拆帧后丢弃帧容器），避免句柄引用残留导致缓冲无法归池。</para>
    /// <para>结构体实现（<see cref="ArrayPacket"/> 等）返回无所有权的视图：无需释放，仅可在原数据生命周期内短暂使用。</para>
    /// </remarks>
    /// <param name="offset">相对当前包起始偏移</param>
    /// <param name="count">个数。默认 -1 表示到末尾</param>
    IPacket Slice(Int32 offset, Int32 count = -1);

    /// <summary>切片得到新数据包（兼容重载），共享底层缓冲区以减少分配</summary>
    /// <remarks>
    /// <para>为兼容基于三参签名编译的旧版库（历史版本的 Remoting/WebSocket 等）而保留，行为直接转发到两参重载。</para>
    /// <para>引用计数共享模型下不再区分“转移”与“借用”，新代码请使用 <see cref="Slice(Int32, Int32)"/>。</para>
    /// </remarks>
    /// <param name="offset">相对当前包起始偏移</param>
    /// <param name="count">个数。默认 -1 表示到末尾</param>
    /// <param name="transferOwner">是否转移内存管理权。兼容参数，忽略</param>
    [Obsolete("引用计数共享模型下切片自动共享所有权，请改用 Slice(Int32 offset, Int32 count)。")]
    IPacket Slice(Int32 offset, Int32 count, Boolean transferOwner);

    /// <summary>尝试获取当前片段的 <see cref="ArraySegment{T}"/>（不含链式后续）</summary>
    Boolean TryGetArray(out ArraySegment<Byte> segment);
}

/// <summary>拥有管理权的数据包。使用完以后需要释放</summary>
public interface IOwnerPacket : IPacket, IDisposable;

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

#if NETCOREAPP || NETSTANDARD2_1
        // 栈缓冲区在循环外分配一次，避免每次迭代累积栈空间
        const Int32 MaxStackAllocChars = 1024;
        Span<Char> stackChars = stackalloc Char[MaxStackAllocChars];
#endif

        for (var current = pk; current != null; current = current.Next)
        {
            var span = current.GetSpan();
            if (span.Length == 0) continue;

#if NETCOREAPP || NETSTANDARD2_1
            var charCount = encoding.GetCharCount(span);
            if (charCount <= MaxStackAllocChars)
            {
                var written = encoding.GetChars(span, stackChars);
                writer.Write(stackChars[..written]);
            }
            else
            {
                // 大段使用池化缓冲区
                var chars = ArrayPool<Char>.Shared.Rent(charCount);
                try
                {
                    var written = encoding.GetChars(span, chars);
                    writer.Write(chars, 0, written);
                }
                finally
                {
                    ArrayPool<Char>.Shared.Return(chars);
                }
            }
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

    /// <summary>在数据包链中查找目标字节序列，返回相对链头的全局偏移</summary>
    /// <param name="pk">源数据包，支持链式</param>
    /// <param name="data">目标字节序列</param>
    /// <returns>匹配起点相对链头的偏移；未找到返回 -1</returns>
    /// <remarks>
    /// 逐段扫描零拷贝：段内用 span 快速查找；目标可能被切在段与段之间，因此保留“已扫描过的最后 k-1 个字节”，
    /// 与本段头部拼起来再查一次，目标跨多个短段（含空段）同样能查到。
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

            // 段内查找
            var idx = span.IndexOf(data);
            if (idx >= 0) return pos + idx;

            // 跨段查找：目标被切在段间，起点在“最后 k-1 字节”内、终点在本段头部
            if (tailLen > 0 && span.Length > 0)
            {
                var headLen = Math.Min(k1, span.Length);
                var w = win[..(tailLen + headLen)];
                tail[..tailLen].CopyTo(w);
                span[..headLen].CopyTo(w[tailLen..]);

                var j = w.IndexOf(data);
                if (j >= 0) return pos - tailLen + j;
            }

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
    /// <summary>尝试扩展头部空间，用于填充协议头等场景</summary>
    /// <param name="pk">原数据包</param>
    /// <param name="size">需要扩展的头部字节数</param>
    /// <param name="newPacket">扩展后的新数据包</param>
    /// <returns>是否成功扩展</returns>
    /// <remarks>
    /// <para>已过时，请使用 <see cref="ExpandHeader"/> 方法。</para>
    /// <para>该方法仅在原包有足够前置空间时成功，否则返回 false。</para>
    /// </remarks>
    [Obsolete("请改用 ExpandHeader，并确保根据返回结果继续使用新实例。")]
    public static Boolean TryExpandHeader(this IPacket pk, Int32 size, [NotNullWhen(true)] out IPacket? newPacket)
    {
        newPacket = null;

        if (pk is ArrayPacket ap && ap.Offset >= size)
        {
            newPacket = new ArrayPacket(ap.Buffer, ap.Offset - size, ap.Length + size) { Next = ap.Next };
            return true;
        }
        else if (pk is OwnerPacket owner && owner.Offset >= size)
        {
            newPacket = new OwnerPacket(owner, size);
            return true;
        }
        return false;
    }

    /// <summary>扩展头部空间，优先复用现有缓冲区</summary>
    /// <param name="pk">原数据包，可为 null</param>
    /// <param name="size">需要扩展的头部字节数</param>
    /// <returns>扩展后的数据包，可能复用原缓冲区或创建新缓冲区</returns>
    /// <remarks>
    /// <para><b>扩展策略</b>：</para>
    /// <list type="number">
    /// <item>ArrayPacket/OwnerPacket 有足够前置空间时，直接扩展</item>
    /// <item>否则创建新的 OwnerPacket，原包作为后继链节点</item>
    /// </list>
    /// </remarks>
    public static IPacket ExpandHeader(this IPacket? pk, Int32 size)
    {
        return pk switch
        {
            ArrayPacket ap when ap.Offset >= size =>
                new ArrayPacket(ap.Buffer, ap.Offset - size, ap.Length + size) { Next = ap.Next },
            OwnerPacket owner when owner.Offset >= size =>
                new OwnerPacket(owner, size),
            _ => new OwnerPacket(size) { Next = pk }
        };
    }
    #endregion
}

/// <summary>池化缓冲引用计数。多个数据包共享同一缓冲区时统一记录引用数量，最后一个引用释放时归还内存池</summary>
/// <remarks>
/// <para>切片共享通过 <see cref="AddRef"/> 递增引用计数；每个句柄各自 <see cref="Release"/>，归零时归还内存池。</para>
/// <para>引用计数归零时才归还缓冲区，因此多个句柄可以独立使用、各自释放，互不影响。</para>
/// <para><b>必须为 class</b>：计数器被多个句柄共享，引用同一性是引用计数成立的前提；结构体会因值拷贝导致各句柄各持一份计数，引用计数完全失效。</para>
/// </remarks>
/// <remarks>实例化，引用计数初始为 1</remarks>
/// <param name="buffer">缓冲数组</param>
/// <param name="returnToPool">引用归零时是否归还内存池</param>
internal sealed class ArrayOwner(Byte[] buffer, Boolean returnToPool)
{
    #region 属性
    /// <summary>缓冲数组</summary>
    public Byte[] Buffer { get; } = buffer;

    /// <summary>引用归零时是否归还内存池</summary>
    public Boolean ReturnToPool { get; } = returnToPool;

    private Int32 _refCount = 1;

    /// <summary>当前引用数量。供观测与决策（如接收层判断缓冲是否可复用）</summary>
    public Int32 RefCount => Volatile.Read(ref _refCount);
    #endregion

    #region 方法
    /// <summary>新增一个引用</summary>
    public void AddRef() => Interlocked.Increment(ref _refCount);

    /// <summary>释放一个引用，归零时归还内存池</summary>
    public void Release()
    {
        if (Interlocked.Decrement(ref _refCount) == 0 && ReturnToPool)
            ArrayPool<Byte>.Shared.Return(Buffer);
    }
    #endregion
}

/// <summary>所有权内存包。基于 ArrayPool 的引用计数内存管理，支持链式结构、零拷贝切片与共享使用</summary>
/// <remarks>
/// <para><b>核心特性</b>：</para>
/// <list type="bullet">
/// <item>内存池复用：使用 <see cref="ArrayPool{T}.Shared"/> 减少 GC 压力，引用计数归零时统一归还</item>
/// <item>引用计数共享：<see cref="Slice(Int32, Int32)"/> 返回独立句柄，多个句柄同时引用同一缓冲区，各自独立读取与释放</item>
/// <item>零拷贝切片：共享底层缓冲区，避免不必要的内存分配</item>
/// <item>链式结构：支持多段数据包连接，透明处理跨段访问</item>
/// </list>
/// <para><b>所有权语义</b>：</para>
/// <list type="number">
/// <item>共享切片：<see cref="Slice(Int32, Int32)"/> 按段递增引用计数，返回独立句柄；双方（或多方）均可继续使用，各自 <see cref="Dispose"/>，最后一个释放时才归还内存池</item>
/// <item>独占换窗：<see cref="OwnerPacket(OwnerPacket, Int32)"/> 头部扩展构造，接管源实例的引用与链，源实例整体作废（仅此一处保留接管语义，调用方需自行确保无其它共享句柄）</item>
/// <item>接收层轮末裁决：会话私有句柄在轮末按 <see cref="RefCount"/> 判定——无人持有（为 1）时 <see cref="Detach"/> 脱手保留缓冲复用；存在共享切片时 <see cref="Dispose"/> 本引用，缓冲由最后释放的切片归还</item>
/// </list>
/// <para><b>生命周期管理</b>：每个持有引用的句柄都必须调用 <see cref="Dispose"/>；引用计数归零时缓冲区归还内存池。未释放的句柄会让缓冲区无法回池，这是使用本类型唯一的纪律要求。
/// 开发期（DEBUG）由析构函数兜底释放漏释放的句柄并输出 XTrace 告警；发布版默认不编译析构，不产生终结队列登记与终结器调度开销，如需生产兜底可定义编译符号 OWNERPACKET_FINALIZER 开启。</para>
/// <para><b>设计决策</b>：</para>
/// <list type="bullet">
/// <item><b>必须为 class</b>：所有权语义依赖引用同一性。struct 赋值产生值拷贝会导致 double-free（Slice 转移所有权时修改的是副本而非原始实例），
/// 且 IDisposable + struct 在装箱场景下无法正确释放资源。</item>
/// <item><b>sealed 密封</b>：无派生需求，JIT 可对 GetSpan/GetMemory 等热路径方法去虚拟化并内联，显著提升协议解析性能。</item>
/// <item><b>不继承 MemoryManager&lt;T&gt;</b>：仅需 IPacket + IDisposable，MemoryManager 的 Pin/Unpin/IMemoryOwner.Memory 均未使用，
/// 移除后消除死代码和多余 vtable 开销。</item>
/// </list>
/// </remarks>
public sealed class OwnerPacket : IPacket, IOwnerPacket
{
    #region 字段与属性
    private static readonly Byte[] _empty = [];

    private Byte[]? _buffer;
    private Int32 _offset;
    private Int32 _length;
    private ArrayOwner? _owner;

    /// <summary>缓冲区数组</summary>
    /// <exception cref="ObjectDisposedException">实例已释放</exception>
    public Byte[] Buffer { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _buffer ?? throw new ObjectDisposedException(nameof(OwnerPacket)); }

    /// <summary>数据在缓冲区中的起始偏移量</summary>
    public Int32 Offset { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _offset; }

    /// <summary>当前数据包的有效数据长度</summary>
    public Int32 Length { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _length; }

    /// <summary>下一个链式数据包节点</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public IPacket? Next { get; set; }

    /// <summary>包含链式结构的总数据长度</summary>
    public Int32 Total { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _length + (Next?.Total ?? 0); }

    /// <summary>当前缓冲区的引用句柄数（包含本句柄）。视图节点（不持引用）返回 0</summary>
    /// <remarks>仅供观测与决策（如接收层判断缓冲能否复用），不要用于同步控制。</remarks>
    public Int32 RefCount => _owner?.RefCount ?? 0;

    /// <summary>获取或设置指定位置的字节值，支持跨链式包访问</summary>
    /// <param name="index">从当前包起始的相对索引位置</param>
    /// <returns>指定位置的字节值</returns>
    /// <exception cref="IndexOutOfRangeException">索引超出有效范围</exception>
    /// <exception cref="ObjectDisposedException">实例已释放</exception>
    public Byte this[Int32 index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _buffer == null
            ? throw new ObjectDisposedException(nameof(OwnerPacket))
            : index switch
            {
                < 0 => throw new IndexOutOfRangeException($"Index cannot be negative: {index}"),
                var i when i < _length => _buffer[_offset + i],
                var i when Next != null => Next[i - _length],
                _ => throw new IndexOutOfRangeException($"Index {index} exceeds total length {Total}")
            };
        set
        {
            if (_buffer == null) throw new ObjectDisposedException(nameof(OwnerPacket));

            switch (index)
            {
                case < 0:
                    throw new IndexOutOfRangeException($"Index cannot be negative: {index}");
                case var i when i < _length:
                    _buffer[_offset + i] = value;
                    break;
                case var i when Next != null:
                    Next[i - _length] = value;
                    break;
                default:
                    throw new IndexOutOfRangeException($"Index {index} exceeds total length {Total}");
            }
        }
    }
    #endregion

    #region 构造函数
    /// <summary>创建指定长度的内存包，从共享内存池借用缓冲区</summary>
    /// <param name="length">所需缓冲区长度</param>
    /// <exception cref="ArgumentOutOfRangeException">长度为负数</exception>
    /// <remarks>实际分配的缓冲区可能大于请求长度，以适配内存池的分片策略</remarks>
    public OwnerPacket(Int32 length)
    {
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length), "Length must be non-negative.");

        var buffer = ArrayPool<Byte>.Shared.Rent(length);
        _buffer = buffer;
        _offset = 0;
        _length = length;
        _owner = new ArrayOwner(buffer, true);
    }

    /// <summary>创建内存包，使用现有缓冲区</summary>
    /// <param name="buffer">数据缓冲区</param>
    /// <param name="offset">数据起始偏移量</param>
    /// <param name="length">有效数据长度</param>
    /// <param name="hasOwner">是否拥有缓冲区管理权限</param>
    /// <exception cref="ArgumentNullException">缓冲区为 null</exception>
    /// <exception cref="ArgumentOutOfRangeException">偏移量或长度超出缓冲区范围</exception>
    public OwnerPacket(Byte[] buffer, Int32 offset, Int32 length, Boolean hasOwner)
    {
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));
        if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset), "Offset must be non-negative.");
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length), "Length must be non-negative.");
        if (offset + length > buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(length),
                "Offset and length must be within buffer bounds.");

        _buffer = buffer;
        _offset = offset;
        _length = length;
        _owner = hasOwner ? new ArrayOwner(buffer, true) : null;
    }

    /// <summary>内部构造：节点直接持有缓冲区与引用计数对象（引用计数已完成处理）</summary>
    private OwnerPacket(Byte[] buffer, Int32 offset, Int32 length, ArrayOwner? owner)
    {
        _buffer = buffer;
        _offset = offset;
        _length = length;
        _owner = owner;
    }

    /// <summary>基于现有实例创建扩展头部的新内存包，接管所有权</summary>
    /// <param name="owner">源内存包实例</param>
    /// <param name="expandSize">向前扩展的字节数</param>
    /// <exception cref="ArgumentNullException">源实例为 null</exception>
    /// <exception cref="ArgumentOutOfRangeException">扩展大小超出可用前置空间</exception>
    /// <remarks>
    /// <para>新实例接管源实例的所有权和链式结构，源实例失去管理权限。</para>
    /// <para>要求源实例的 Offset 不小于 expandSize，否则无法向前扩展。</para>
    /// </remarks>
    public OwnerPacket(OwnerPacket owner, Int32 expandSize)
    {
        if (owner == null) throw new ArgumentNullException(nameof(owner));
        if (expandSize < 0)
            throw new ArgumentOutOfRangeException(nameof(expandSize), "Expand size must be non-negative.");

        if (owner._buffer == null) throw new ObjectDisposedException(nameof(OwnerPacket), "Source packet has been disposed.");
        if (owner._offset < expandSize)
            throw new ArgumentOutOfRangeException(nameof(expandSize),
                $"Expand size {expandSize} exceeds available front space {owner._offset}");

        _buffer = owner._buffer;
        _offset = owner._offset - expandSize;
        _length = owner._length + expandSize;
        Next = owner.Next;

        // 引用与链整体移交；源实例作废（引用由新实例接管，不释放）
        _owner = owner._owner;
        DetachNode(owner, release: false);
    }

    /// <summary>从数据流创建内存包，优先窃取MemoryStream内部缓冲区，否则从池借用并拷贝数据</summary>
    /// <remarks>
    /// 窃取成功时不拥有缓冲区，Dispose不归还；窃取失败时从池借用，Dispose自动归还。
    /// 若指定预留空间，数据从 offset = reserve 位置开始存放，有效长度为 reserve + data_size。
    /// 构造完成后数据流位置不变。
    /// </remarks>
    /// <param name="stream">数据流</param>
    /// <param name="reserve">前置预留字节数，默认0</param>
    public OwnerPacket(Stream stream, Int32 reserve = 0)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (reserve < 0) throw new ArgumentOutOfRangeException(nameof(reserve), "Reserve must be non-negative.");

        if (stream is MemoryStream ms)
        {
#if !NET45
            // 尝试窃取内部存储区，需要.Net 4.6支持
            if (ms.TryGetBuffer(out var seg))
            {
                if (seg.Array == null) throw new InvalidDataException();

                _buffer = seg.Array;
                _offset = seg.Offset + (Int32)ms.Position;
                _length = seg.Count - (Int32)ms.Position;
                _owner = null;
                return;
            }
#endif
        }

        // 从池里借字节数组，存放数据流拷贝出来的数据
        var size = (Int32)(stream.Length - stream.Position);
        var buffer = ArrayPool<Byte>.Shared.Rent(reserve + size);
        _buffer = buffer;
        var count = stream.Read(buffer, reserve, size);
        _offset = 0;
        _length = count;
        _owner = new ArrayOwner(buffer, true);

        // 确保数据流位置不变
        if (count > 0) stream.Seek(-count, SeekOrigin.Current);
    }
    #endregion

    #region 内存管理
    /// <summary>释放本句柄持有的缓冲区引用。引用计数归零时归还池化缓冲区</summary>
    /// <remarks>
    /// <para>Dispose 幂等；借用视图（不持有引用）自身无操作，但同样释放链式后续节点。</para>
    /// <para>若缓冲区存在其它共享句柄，本调用不会归还缓冲区，其它句柄仍可继续使用。</para>
    /// </remarks>
    public void Dispose()
    {
        // 已显式释放，抑制析构兜底
        GC.SuppressFinalize(this);

        // 先摘除链式后续节点：借用视图头（_owner 为空）同样可能挂着拥有引用的尾链
        var next = Next;
        Next = null;

        var owner = _owner;
        if (owner != null)
        {
            _owner = null;
            _buffer = null;     // 释放后禁止再读，防止误用已归还的缓冲区
            _length = 0;
            owner.Release();
        }

        // 安全释放链式后续节点
        next.TryDispose();
    }

#if DEBUG || OWNERPACKET_FINALIZER
    /// <summary>析构。兜底释放未 Dispose 的句柄引用，避免池化缓冲区永远无法归还</summary>
    /// <remarks>
    /// <para>默认仅在 DEBUG 构建（开发期）编译本方法；发布版不含析构，不产生终结队列登记与终结器调度开销。如需生产兜底，可定义编译符号 OWNERPACKET_FINALIZER 开启。</para>
    /// <para>显式 Dispose 会抑制析构；只有漏释放的句柄才会在 GC 时走此路径，仅释放本节点自身引用。</para>
    /// <para>链式后续节点各自由自身的析构释放引用，不在此处递归触碰，避免终结器线程访问其它托管对象。</para>
    /// </remarks>
    ~OwnerPacket()
    {
        var owner = _owner;
        if (owner != null)
        {
            _owner = null;
            owner.Release();

#if DEBUG
            // 走到这里说明存在漏释放：开发期直接告警，便于定位与修复；终结器线程内日志异常不得外抛
            try
            {
                XTrace.WriteLine("[OwnerPacket] 漏释放句柄（Offset={0}，Length={1}），已由析构兜底归还缓冲区引用；请检查调用方是否遗漏 Dispose。", _offset, _length);
            }
            catch { }
#endif
        }
    }
#endif

    /// <summary>脱手：放弃本句柄的引用，但<strong>不归还</strong>池化缓冲区，缓冲保持“已借出”状态交给调用方继续使用</summary>
    /// <remarks>
    /// <para>用于接收层复用缓冲：每轮把整块缓冲包装为句柄上抛，轮末确认没有其它持有者（<see cref="RefCount"/> 为 1）时脱手，
    /// 缓冲留在会话继续接收，做到零 Rent/Return；归还责任随脱手转交调用方，由其在会话关闭时归还。</para>
    /// <para>与 <see cref="Dispose"/> 的区别：Dispose 释放引用并可能归还池；脱手只废弃句柄，保留缓冲的借用状态。</para>
    /// <para>仍有其它持有者（<see cref="RefCount"/> 大于 1）时抛出异常：它们还在读这块缓冲，脱手会让调用方误以为缓冲可以独占复用。</para>
    /// <para>调用后实例作废，重复调用无操作；借用视图（不持有引用）调用无操作。仅用于无链式后续节点的独占句柄。</para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">仍有其它句柄持有缓冲区</exception>
    public void Detach()
    {
        var owner = _owner;
        if (owner == null) return;

        // 还有别的句柄在用这块缓冲时不能脱手：它们释放后缓冲会归还池，而调用方还在用同一块缓冲
        if (owner.RefCount > 1)
            throw new InvalidOperationException($"Cannot detach while {owner.RefCount - 1} other handle(s) still hold the buffer.");

        // 抑制析构兜底，防止 GC 时误把仍在借用中的缓冲归还池
        GC.SuppressFinalize(this);

        _owner = null;
        _buffer = null;
        _length = 0;
        Next = null;
    }

    /// <summary>立即放弃所有权，不归还缓冲区（兼容旧版）</summary>
    /// <remarks>
    /// <para>为兼容旧版编译的库而保留，内部转发到 <see cref="Detach"/>：无其它句柄持有时允许，缓冲保持已借出状态交给调用方继续使用。</para>
    /// <para>与旧版区别：仍有其它句柄持有缓冲区时抛出异常（引用计数保护）。新代码请直接使用 <see cref="Detach"/>。</para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">仍有其它句柄持有缓冲区</exception>
    [Obsolete("请改用 Detach()，语义一致且带引用计数保护。")]
    public void Free() => Detach();

    #endregion

    #region 内存访问
    /// <summary>获取当前数据包的内存片段视图（仅本段，不含 Next）</summary>
    /// <returns>只读内存片段，仅在实例生命周期内有效</returns>
    /// <exception cref="ObjectDisposedException">实例已释放</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<Byte> GetSpan() => new(Buffer, _offset, _length);

    /// <summary>获取当前数据包的内存块（仅本段，不含 Next）</summary>
    /// <returns>内存块，仅在实例生命周期内有效</returns>
    /// <exception cref="ObjectDisposedException">实例已释放</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Memory<Byte> GetMemory() => new(Buffer, _offset, _length);

    /// <summary>尝试获取当前片段的数组段表示（仅本段，不含 Next）</summary>
    /// <param name="segment">输出的数组段</param>
    /// <returns>始终返回 true</returns>
    /// <exception cref="ObjectDisposedException">实例已释放</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Boolean TryGetArray(out ArraySegment<Byte> segment)
    {
        segment = new ArraySegment<Byte>(Buffer, _offset, _length);
        return true;
    }
    #endregion

    #region 大小调整
    /// <summary>调整数据包的有效长度</summary>
    /// <param name="size">新的数据长度</param>
    /// <returns>当前实例，支持链式调用</returns>
    /// <exception cref="ArgumentOutOfRangeException">大小为负数或超出缓冲区容量</exception>
    /// <exception cref="NotSupportedException">存在链式后续节点且尝试增大长度</exception>
    /// <exception cref="ObjectDisposedException">实例已释放</exception>
    /// <remarks>
    /// <para>主要用于从缓冲区读取数据后，根据实际读取量调整有效长度。</para>
    /// <para>当存在 Next 节点时，仅允许减小当前段长度。</para>
    /// </remarks>
    public OwnerPacket Resize(Int32 size)
    {
        if (size < 0) throw new ArgumentOutOfRangeException(nameof(size), "Size must be non-negative.");

        if (Next == null)
        {
            if (size > Buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(size),
                    $"Size {size} exceeds buffer capacity {Buffer.Length}");

            _length = size;
        }
        else
        {
            if (size >= _length)
                throw new NotSupportedException("Cannot increase size when Next segment exists");

            _length = size;
        }

        return this;
    }

    /// <summary>原地前移数据窗口，丢弃开头 len 字节</summary>
    /// <param name="len">前移字节数，不能超过当前段长度</param>
    /// <returns>当前实例，支持链式调用</returns>
    /// <exception cref="ArgumentOutOfRangeException">前移长度超出当前段长度</exception>
    /// <exception cref="ObjectDisposedException">实例已释放</exception>
    /// <remarks>
    /// <para>用于拆帧残片前移等场景，不拷贝、不分配，只有窗口指针调整的微小开销。</para>
    /// <para>只影响本句柄的窗口视图，共享同一底层缓冲区的其它句柄视图不受影响；仅对独占句柄使用。</para>
    /// </remarks>
    public OwnerPacket Skip(Int32 len)
    {
        if (_buffer == null) throw new ObjectDisposedException(nameof(OwnerPacket));
        if (len < 0 || len > _length) throw new ArgumentOutOfRangeException(nameof(len), $"Skip {len} exceeds current length {_length}");

        _offset += len;
        _length -= len;

        return this;
    }
    #endregion

    #region 切片操作
    /// <summary>切片生成新数据包，共享底层缓冲区（引用计数）</summary>
    /// <param name="offset">相对当前包的起始偏移</param>
    /// <param name="count">切片长度，-1 表示到末尾</param>
    /// <returns>新的独立句柄，与原包同时可用；各自 Dispose，最后一个释放时缓冲区归还内存池</returns>
    /// <exception cref="ArgumentOutOfRangeException">偏移量或长度超出有效范围</exception>
    /// <exception cref="ObjectDisposedException">实例已释放</exception>
    /// <remarks>
    /// <para>切片共享底层缓冲区，不拷贝数据。支持跨段切片，自动处理边界；窗口覆盖的段各递增一次引用计数。</para>
    /// <para>本方法不改变原实例，双方（或多方）均可继续读取；每个句柄各自负责 <see cref="Dispose"/>。
    /// 线性交接场景直接传递句柄本身即可，无需切片。</para>
    /// </remarks>
    public IPacket Slice(Int32 offset, Int32 count = -1)
    {
        if (_buffer == null) throw new ObjectDisposedException(nameof(OwnerPacket));

        if (offset < 0)
            throw new ArgumentOutOfRangeException(nameof(offset), "Offset cannot be negative.");

        var total = Total;
        if (count > total - offset)
            throw new ArgumentOutOfRangeException(nameof(count),
                $"Count {count} with offset {offset} exceeds total length {total}");

        if (count < 0) count = total - offset;
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(offset), $"Offset {offset} exceeds total length {total}");

        OwnerPacket? head = null;
        OwnerPacket? tail = null;

        var skip = offset;
        var take = count;
        IPacket? node = this;
        while (node != null && skip >= node.Length)
        {
            skip -= node.Length;
            node = node.Next;
        }

        while (node != null && take > 0)
        {
            var next = node.Next;
            var startInNode = skip;
            var takeInNode = Math.Min(node.Length - startInNode, take);
            skip = 0;

            var item = BuildShareNode(node, startInNode, takeInNode);

            if (head == null)
                head = item;
            else
                tail!.Next = item;

            tail = item;
            take -= takeInNode;
            node = next;
        }

        return head ?? new OwnerPacket(_empty, 0, 0, null);
    }

    /// <summary>切片生成新数据包（兼容重载），共享底层缓冲区（引用计数）</summary>
    /// <param name="offset">相对当前包的起始偏移</param>
    /// <param name="count">切片长度，-1 表示到末尾</param>
    /// <param name="transferOwner">是否转移内存管理权。兼容参数，忽略；引用计数共享模型下双方各自 Dispose，最后一个释放时归还内存池</param>
    /// <returns>新的独立句柄，与原包同时可用</returns>
    [Obsolete("引用计数共享模型下切片自动共享所有权，请改用 Slice(Int32 offset, Int32 count)。")]
    public IPacket Slice(Int32 offset, Int32 count, Boolean transferOwner) => Slice(offset, count);
    #endregion

    #region 切片辅助
    /// <summary>为共享窗口构建新节点，对源节点持有引用的段递增引用计数</summary>
    private static OwnerPacket BuildShareNode(IPacket source, Int32 offset, Int32 count)
    {
        if (source is OwnerPacket op)
        {
            if (op._buffer == null) throw new ObjectDisposedException(nameof(OwnerPacket));

            var owner = op._owner;
            owner?.AddRef();

            return new OwnerPacket(op._buffer, op._offset + offset, count, owner);
        }

        if (source.TryGetArray(out var segment))
            return new OwnerPacket(segment.Array!, segment.Offset + offset, count, null);

        throw new NotSupportedException($"Cannot share chain node of type {source.GetType().Name}");
    }

    /// <summary>释放节点引用并作废节点（链式遍历用）。release 为 false 表示引用已移交给接管方</summary>
    private static void DetachNode(IPacket node, Boolean release)
    {
        if (node is not OwnerPacket op) return;

        var owner = op._owner;
        op._owner = null;
        if (release && owner != null) owner.Release();

        op._buffer = null;
        op._length = 0;
        op.Next = null;
    }
    #endregion

    #region 字符串表示
    /// <summary>返回数据包的字符串表示形式</summary>
    /// <returns>包含缓冲区大小、偏移量、长度和总长度的格式化字符串</returns>
    public override String ToString() => $"OwnerPacket[{_buffer?.Length ?? 0}]({_offset}, {_length})<{Total}>";
    #endregion
}

/// <summary>内存包</summary>
/// <remarks>内存包可能来自内存池，失去所有权时已被释放，因此不应该长期持有。</remarks>
public struct MemoryPacket : IPacket
{
    #region 属性
    private readonly Memory<Byte> _memory;
    /// <summary>内存</summary>
    public readonly Memory<Byte> Memory => _memory;

    private readonly Int32 _length;
    /// <summary>数据长度</summary>
    public readonly Int32 Length => _length;

    // 缓存底层数组引用，Indexer 直接数组访问避免 Memory.Span 间接开销（1.66ns → ~0.24ns）
    private readonly Byte[]? _cachedArray;
    private readonly Int32 _cachedOffset;

    /// <summary>获取/设置 指定位置的字节</summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Byte this[Int32 index]
    {
        get
        {
            var p = index - _length;
            if (p >= 0)
            {
                if (Next == null) throw new IndexOutOfRangeException(nameof(index));

                return Next[p];
            }

            var arr = _cachedArray;
            return arr != null ? arr[_cachedOffset + index] : _memory.Span[index];
        }
        set
        {
            var p = index - _length;
            if (p >= 0)
            {
                if (Next == null) throw new IndexOutOfRangeException(nameof(index));

                Next[p] = value;
            }
            else
            {
                var arr = _cachedArray;
                if (arr != null)
                    arr[_cachedOffset + index] = value;
                else
                    _memory.Span[index] = value;
            }
        }
    }

    /// <summary>下一个链式包</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public IPacket? Next { get; set; }

    /// <summary>总长度</summary>
    public readonly Int32 Total => Length + (Next?.Total ?? 0);
    #endregion

    /// <summary>实例化内存包，指定内存和长度</summary>
    /// <param name="memory">内存</param>
    /// <param name="length">长度</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public MemoryPacket(Memory<Byte> memory, Int32 length)
    {
        if (length < 0 || length > memory.Length)
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be non-negative and less than or equal to the memory owner's length.");

        _memory = memory;
        _length = length;

        // 缓存底层数组引用，加速 Indexer 直接数组访问
        if (MemoryMarshal.TryGetArray((ReadOnlyMemory<Byte>)memory, out var seg))
        {
            _cachedArray = seg.Array;
            _cachedOffset = seg.Offset;
        }
        else
        {
            _cachedArray = null;
            _cachedOffset = 0;
        }
    }

    /// <summary>获取分片包。在管理权生命周期内短暂使用</summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<Byte> GetSpan() => _memory.Span[.._length];

    /// <summary>获取内存包。在管理权生命周期内短暂使用</summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Memory<Byte> GetMemory() => _memory[.._length];

    /// <summary>切片得到新数据包，共用内存块</summary>
    /// <param name="offset">偏移</param>
    /// <param name="count">个数。默认-1表示到末尾</param>
    IPacket IPacket.Slice(Int32 offset, Int32 count) => Slice(offset, count);

    /// <summary>切片得到新数据包（兼容重载），共用内存块。无所有权，忽略转移参数</summary>
    /// <param name="offset">偏移</param>
    /// <param name="count">个数。默认-1表示到末尾</param>
    /// <param name="transferOwner">转移所有权。无所有权结构体忽略该参数</param>
    [Obsolete("引用计数共享模型下切片自动共享所有权，请改用 Slice(Int32 offset, Int32 count)。")]
    IPacket IPacket.Slice(Int32 offset, Int32 count, Boolean transferOwner) => Slice(offset, count);

    /// <summary>切片得到新数据包，共用内存块，无内存分配</summary>
    /// <param name="offset">偏移</param>
    /// <param name="count">个数。默认-1表示到末尾</param>
    public MemoryPacket Slice(Int32 offset, Int32 count = -1)
    {
        // 带有Next时，不支持Slice
        if (Next != null) throw new NotSupportedException("Slice with Next");

        var remain = _length - offset;
        if (count < 0 || count > remain) count = remain;
        if (offset == 0 && count == _length) return this;

        return offset == 0
            ? new MemoryPacket(_memory, count)
            : new MemoryPacket(_memory[offset..], count);
    }

    /// <summary>切片得到新数据包（兼容重载），共用内存块。无所有权，忽略转移参数</summary>
    /// <param name="offset">偏移</param>
    /// <param name="count">个数。默认-1表示到末尾</param>
    /// <param name="transferOwner">转移所有权。无所有权结构体忽略该参数</param>
    /// <returns>新的数据包实例</returns>
    [Obsolete("引用计数共享模型下切片自动共享所有权，请改用 Slice(Int32 offset, Int32 count)。")]
    public MemoryPacket Slice(Int32 offset, Int32 count, Boolean transferOwner) => Slice(offset, count);

    /// <summary>尝试获取缓冲区（仅本段，不含 Next）</summary>
    /// <param name="segment"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Boolean TryGetArray(out ArraySegment<Byte> segment) => MemoryMarshal.TryGetArray(GetMemory(), out segment);

    /// <summary>已重载</summary>
    /// <returns></returns>
    public override readonly String ToString() => $"MemoryPacket[{_memory.Length}](0, {_length})<{Total}>";
}

/// <summary>字节数组包</summary>
public record struct ArrayPacket : IPacket
{
    #region 属性
    private readonly Byte[] _buffer;
    /// <summary>缓冲区</summary>
    public readonly Byte[] Buffer => _buffer;

    private readonly Int32 _offset;
    /// <summary>数据偏移</summary>
    public readonly Int32 Offset => _offset;

    private readonly Int32 _length;
    /// <summary>数据长度</summary>
    public readonly Int32 Length => _length;

    /// <summary>下一个链式包</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public IPacket? Next { get; set; }

    /// <summary>总长度</summary>
    public readonly Int32 Total => Length + (Next?.Total ?? 0);

    /// <summary>空数组</summary>
    public static ArrayPacket Empty = new([]);
    #endregion

    #region 索引
    /// <summary>获取/设置 指定位置的字节</summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Byte this[Int32 index]
    {
        get
        {
            var p = index - _length;
            if (p >= 0)
            {
                if (Next == null) throw new IndexOutOfRangeException(nameof(index));

                return Next[p];
            }

            return _buffer[_offset + index];
        }
        set
        {
            var p = index - _length;
            if (p >= 0)
            {
                if (Next == null) throw new IndexOutOfRangeException(nameof(index));

                Next[p] = value;
            }
            else
            {
                _buffer[_offset + index] = value;
            }
        }
    }
    #endregion

    #region 构造
    /// <summary>通过指定字节数组来实例化数据包</summary>
    /// <param name="buf"></param>
    /// <param name="offset"></param>
    /// <param name="count"></param>
    public ArrayPacket(Byte[] buf, Int32 offset = 0, Int32 count = -1)
    {
        if (count < 0) count = buf.Length - offset;

        _buffer = buf;
        _offset = offset;
        _length = count;
    }

    /// <summary>从可扩展内存流实例化，尝试窃取内存流内部的字节数组，失败后拷贝</summary>
    /// <remarks>因数据包内数组窃取自内存流，需要特别小心，避免多线程共用。常用于内存流转数据包，而内存流不再使用</remarks>
    /// <param name="stream"></param>
    public ArrayPacket(Stream stream)
    {
        if (stream is MemoryStream ms)
        {
#if !NET45
            // 尝试抠了内部存储区，下面代码需要.Net 4.6支持
            if (ms.TryGetBuffer(out var seg))
            {
                if (seg.Array == null) throw new InvalidDataException();

                _buffer = seg.Array;
                _offset = seg.Offset + (Int32)ms.Position;
                _length = seg.Count - (Int32)ms.Position;
                return;
            }
            // GetBuffer窃盗内部缓冲区后，无法得知真正的起始位置index，可能导致错误取数
            // public MemoryStream(byte[] buffer, int index, int count, bool writable, bool publiclyVisible)

            //try
            //{
            //    Set(ms.GetBuffer(), (Int32)ms.Position, (Int32)(ms.Length - ms.Position));
            //}
            //catch (UnauthorizedAccessException) { }
#endif
        }

        var buf = new Byte[stream.Length - stream.Position];
        var count = stream.Read(buf, 0, buf.Length);
        _buffer = buf;
        _offset = 0;
        _length = count;

        // 必须确保数据流位置不变
        if (count > 0) stream.Seek(-count, SeekOrigin.Current);
    }

    /// <summary>从数据段实例化数据包</summary>
    /// <param name="segment"></param>
    public ArrayPacket(ArraySegment<Byte> segment) : this(segment.Array!, segment.Offset, segment.Count) { }
    #endregion

    /// <summary>获取分片包。在管理权生命周期内短暂使用</summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<Byte> GetSpan() => new(_buffer, _offset, _length);

    /// <summary>获取内存包。在管理权生命周期内短暂使用</summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Memory<Byte> GetMemory() => new(_buffer, _offset, _length);

    /// <summary>切片得到新数据包，共用缓冲区</summary>
    /// <param name="offset">偏移</param>
    /// <param name="count">个数。默认-1表示到末尾</param>
    IPacket IPacket.Slice(Int32 offset, Int32 count)
    {
        // 内联逻辑，避免 struct 通过 (this as IPacket) 装箱
        if (count == 0) return Empty;

        var remain = _length - offset;
        var next = Next;
        if (next != null && remain <= 0) return next.Slice(offset - _length, count);

        return Slice(offset, count);
    }

    /// <summary>切片得到新数据包（兼容重载），共用缓冲区。无所有权，忽略转移参数</summary>
    /// <param name="offset">偏移</param>
    /// <param name="count">个数。默认-1表示到末尾</param>
    /// <param name="transferOwner">转移所有权。无所有权结构体忽略该参数</param>
    [Obsolete("引用计数共享模型下切片自动共享所有权，请改用 Slice(Int32 offset, Int32 count)。")]
    IPacket IPacket.Slice(Int32 offset, Int32 count, Boolean transferOwner) => Slice(offset, count);

    /// <summary>切片得到新数据包，共用缓冲区，无内存分配</summary>
    /// <param name="offset">偏移</param>
    /// <param name="count">个数。默认-1表示到末尾</param>
    public ArrayPacket Slice(Int32 offset, Int32 count = -1)
    {
        if (count == 0) return Empty;

        var start = Offset + offset;
        var remain = _length - offset;

        var next = Next;
        if (next == null)
        {
            // count 是 offset 之后的个数
            if (count < 0 || count > remain) count = remain;
            return count <= 0 ? Empty : new ArrayPacket(_buffer, start, count);
        }

        // 如果当前段用完，则取下一段。强转ArrayPacket，如果不是则抛出异常
        if (remain <= 0)
            return (ArrayPacket)next.Slice(offset - _length, count);

        // 当前包用一截，剩下的全部
        if (count < 0)
            return new ArrayPacket(_buffer, start, remain) { Next = next };

        // 当前包可以读完
        if (count <= remain)
            return new ArrayPacket(_buffer, start, count);

        // 当前包用一截，剩下的再截取
        return new ArrayPacket(_buffer, start, remain) { Next = next.Slice(0, count - remain) };
    }

    /// <summary>切片得到新数据包（兼容重载），共用缓冲区，无内存分配。无所有权，忽略转移参数</summary>
    /// <param name="offset">偏移</param>
    /// <param name="count">个数。默认-1表示到末尾</param>
    /// <param name="transferOwner">转移所有权。无所有权结构体忽略该参数</param>
    /// <returns>新的数据包实例</returns>
    [Obsolete("引用计数共享模型下切片自动共享所有权，请改用 Slice(Int32 offset, Int32 count)。")]
    public ArrayPacket Slice(Int32 offset, Int32 count, Boolean transferOwner) => Slice(offset, count);

    /// <summary>尝试获取缓冲区（仅本段，不含 Next）</summary>
    /// <param name="segment"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Boolean TryGetArray(out ArraySegment<Byte> segment)
    {
        segment = new ArraySegment<Byte>(_buffer, _offset, _length);
        return true;
    }

    #region 重载运算符
    /// <summary>重载类型转换，字节数组直接转为Packet对象</summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static implicit operator ArrayPacket(Byte[] value) => new(value);

    /// <summary>重载类型转换，一维数组直接转为Packet对象</summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static implicit operator ArrayPacket(ArraySegment<Byte> value) => new(value.Array!, value.Offset, value.Count);

    /// <summary>重载类型转换，字符串直接转为Packet对象</summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static implicit operator ArrayPacket(String value) => new(value.GetBytes());

    /// <summary>已重载</summary>
    /// <returns></returns>
    public override readonly String ToString() => $"ArrayPacket[{_buffer.Length}]({_offset}, {_length})<{Total}>";
    #endregion
}

/// <summary>只读数据包。禁止修改数据，适合多线程共享场景</summary>
/// <remarks>
/// <para>与 <see cref="ArrayPacket"/> 的区别：</para>
/// <list type="bullet">
/// <item>索引器为只读，禁止修改数据</item>
/// <item>不支持 Next 链式结构（始终为 null）</item>
/// <item>GetSpan 返回只读视图（通过 GetMemory().Span 获取）</item>
/// </list>
/// <para>适用场景：配置数据、协议模板、缓存数据等需要防止意外修改的场合</para>
/// </remarks>
public readonly record struct ReadOnlyPacket : IPacket
{
    #region 属性
    private readonly Byte[] _buffer;
    /// <summary>缓冲区</summary>
    public Byte[] Buffer => _buffer;

    private readonly Int32 _offset;
    /// <summary>数据偏移</summary>
    public Int32 Offset => _offset;

    private readonly Int32 _length;
    /// <summary>数据长度</summary>
    public Int32 Length => _length;

    /// <summary>下一个链式包。只读包不支持链式结构，始终返回 null</summary>
    IPacket? IPacket.Next { get => null; set { } }

    /// <summary>总长度。只读包不支持链式，等于 Length</summary>
    public Int32 Total => _length;

    /// <summary>空数据包</summary>
    public static ReadOnlyPacket Empty { get; } = new([]);
    #endregion

    #region 索引
    /// <summary>获取指定位置的字节（只读）</summary>
    /// <param name="index">索引位置</param>
    /// <returns>字节值</returns>
    /// <exception cref="IndexOutOfRangeException">索引超出范围</exception>
    public Byte this[Int32 index]
    {
        get
        {
            if (index < 0 || index >= _length)
                throw new IndexOutOfRangeException($"Index {index} is out of range [0, {_length})");
            return _buffer[_offset + index];
        }
        set => throw new NotSupportedException("ReadOnlyPacket does not support modification");
    }
    #endregion

    #region 构造
    /// <summary>通过字节数组实例化只读数据包</summary>
    /// <param name="buffer">字节数组</param>
    /// <param name="offset">起始偏移</param>
    /// <param name="count">数据长度，-1 表示到数组末尾</param>
    public ReadOnlyPacket(Byte[] buffer, Int32 offset = 0, Int32 count = -1)
    {
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));
        if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
        if (count < 0) count = buffer.Length - offset;
        if (offset + count > buffer.Length) throw new ArgumentOutOfRangeException(nameof(count));

        _buffer = buffer;
        _offset = offset;
        _length = count;
    }

    /// <summary>从数组段实例化只读数据包</summary>
    /// <param name="segment">数组段</param>
    public ReadOnlyPacket(ArraySegment<Byte> segment) : this(segment.Array!, segment.Offset, segment.Count) { }

    /// <summary>从 IPacket 创建只读副本</summary>
    /// <param name="packet">源数据包</param>
    /// <remarks>会复制数据到新的缓冲区，确保完全独立</remarks>
    public ReadOnlyPacket(IPacket packet) : this(packet.ToArray()) { }
    #endregion

    #region 方法
    /// <summary>获取分片视图（仅本段，只读包不支持 Next）</summary>
    /// <returns>只读字节片段</returns>
    public Span<Byte> GetSpan() => new(_buffer, _offset, _length);

    /// <summary>获取内存块（仅本段，只读包不支持 Next）</summary>
    /// <returns>只读内存块</returns>
    public Memory<Byte> GetMemory() => new(_buffer, _offset, _length);

    /// <summary>切片得到新的只读数据包</summary>
    /// <param name="offset">相对偏移</param>
    /// <param name="count">数据长度，-1 表示到末尾</param>
    /// <returns>新的只读数据包</returns>
    IPacket IPacket.Slice(Int32 offset, Int32 count) => Slice(offset, count);

    /// <summary>切片得到新的只读数据包（兼容重载）。无所有权，忽略转移参数</summary>
    /// <param name="offset">相对偏移</param>
    /// <param name="count">数据长度，-1 表示到末尾</param>
    /// <param name="transferOwner">转移所有权。只读包忽略该参数</param>
    /// <returns>新的只读数据包</returns>
    [Obsolete("引用计数共享模型下切片自动共享所有权，请改用 Slice(Int32 offset, Int32 count)。")]
    IPacket IPacket.Slice(Int32 offset, Int32 count, Boolean transferOwner) => Slice(offset, count);

    /// <summary>切片得到新的只读数据包，无内存分配</summary>
    /// <param name="offset">相对偏移</param>
    /// <param name="count">数据长度，-1 表示到末尾</param>
    /// <returns>新的只读数据包</returns>
    public ReadOnlyPacket Slice(Int32 offset, Int32 count = -1)
    {
        if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));

        var newOffset = _offset + offset;
        var remain = _length - offset;
        if (count < 0 || count > remain) count = remain;
        if (count < 0) count = 0;

        return new ReadOnlyPacket(_buffer, newOffset, count);
    }

    /// <summary>尝试获取数组段（仅本段，只读包不支持 Next）</summary>
    /// <param name="segment">输出的数组段</param>
    /// <returns>始终返回 true</returns>
    public Boolean TryGetArray(out ArraySegment<Byte> segment)
    {
        segment = new ArraySegment<Byte>(_buffer, _offset, _length);
        return true;
    }
    #endregion

    #region 转换
    /// <summary>转换为字节数组</summary>
    /// <returns>字节数组副本</returns>
    public Byte[] ToArray()
    {
        if (_offset == 0 && _length == _buffer.Length) return _buffer;
        return GetSpan().ToArray();
    }

    /// <summary>从字节数组隐式转换</summary>
    /// <param name="buffer">字节数组</param>
    public static implicit operator ReadOnlyPacket(Byte[] buffer) => new(buffer);

    /// <summary>从数组段隐式转换</summary>
    /// <param name="segment">数组段</param>
    public static implicit operator ReadOnlyPacket(ArraySegment<Byte> segment) => new(segment);

    /// <summary>已重载</summary>
    public override String ToString() => $"ReadOnlyPacket[{_buffer.Length}]({_offset}, {_length})";
    #endregion
}
