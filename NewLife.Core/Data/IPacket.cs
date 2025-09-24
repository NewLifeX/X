using System.Buffers;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using NewLife.Collections;

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
/// <para>切片 <see cref="Slice(Int32, Int32)"/> 默认共享底层缓冲区，必要时可指定是否转移所有权。</para>
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

    /// <summary>获取分片视图。在管理权生命周期内短暂使用，禁止长期保存</summary>
    Span<Byte> GetSpan();

    /// <summary>获取内存块。在管理权生命周期内短暂使用，禁止长期保存</summary>
    Memory<Byte> GetMemory();

    /// <summary>切片得到新数据包，共享底层内存以减少分配</summary>
    /// <param name="offset">相对当前包起始偏移</param>
    /// <param name="count">个数。默认 -1 表示到末尾</param>
    IPacket Slice(Int32 offset, Int32 count = -1);

    /// <summary>切片得到新数据包，可选择转移内存管理权</summary>
    /// <remarks>若 <paramref name="transferOwner"/> 为 true，表示新包负责归还缓冲区（仅支持一次转移）；多次切分同一来源时不要转移。</remarks>
    /// <param name="offset">相对当前包起始偏移</param>
    /// <param name="count">个数。默认 -1 表示到末尾</param>
    /// <param name="transferOwner">是否转移所有权（实现可能不支持）</param>
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
        var sb = Pool.StringBuilder.Get();

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

    /// <summary>获取包含数据包内容的独立内存流</summary>
    /// <param name="pk">源数据包</param>
    /// <returns>可读写的内存流，位置已重置为 0</returns>
    public static Stream GetStream(this IPacket pk)
    {
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

        // 多包或无法获取数组段时，复制到新数组
        var ms = Pool.MemoryStream.Get();
        pk.CopyTo(ms);
        ms.Position = 0;
        return new ArraySegment<Byte>(ms.Return(true));
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

        // 多包通过内存流聚合
        var ms = Pool.MemoryStream.Get();
        pk.CopyTo(ms);
        return ms.Return(true);
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

        // 多包链通过完整数组再读取
        return pk.ToArray().ReadBytes(offset, count);
    }

    /// <summary>深度克隆数据包，完全复制数据内容</summary>
    /// <param name="pk">源数据包</param>
    /// <returns>独立的数据包副本</returns>
    public static IPacket Clone(this IPacket pk)
    {
        if (pk.Next == null)
            return new ArrayPacket(pk.GetSpan().ToArray());

        var ms = new MemoryStream();
        pk.CopyTo(ms);
        ms.Position = 0;
        return new ArrayPacket(ms);
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

/// <summary>所有权内存包。基于 ArrayPool 的高性能内存管理，支持链式结构与所有权转移</summary>
/// <remarks>
/// <para><b>核心特性</b>：</para>
/// <list type="bullet">
/// <item>内存池复用：使用 <see cref="ArrayPool{T}.Shared"/> 减少 GC 压力</item>
/// <item>所有权转移：切片操作可选择转移内存管理责任（仅一次）</item>
/// <item>链式结构：支持多段数据包连接，透明处理跨段访问</item>
/// <item>零拷贝切片：共享底层缓冲区，避免不必要的内存分配</item>
/// </list>
/// <para><b>生命周期管理</b>：必须调用 <see cref="Dispose"/> 归还池化内存，或通过所有权转移由新实例负责释放。</para>
/// </remarks>
public class OwnerPacket : MemoryManager<Byte>, IPacket, IOwnerPacket
{
    #region 字段与属性
    private Byte[]? _buffer;
    private Int32 _offset;
    private Int32 _length;
    private Boolean _hasOwner;

    /// <summary>缓冲区数组</summary>
    /// <exception cref="ObjectDisposedException">实例已释放</exception>
    public Byte[] Buffer => _buffer ?? throw new ObjectDisposedException(nameof(OwnerPacket));

    /// <summary>数据在缓冲区中的起始偏移量</summary>
    public Int32 Offset => _offset;

    /// <summary>当前数据包的有效数据长度</summary>
    public Int32 Length => _length;

    /// <summary>下一个链式数据包节点</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public IPacket? Next { get; set; }

    /// <summary>包含链式结构的总数据长度</summary>
    public Int32 Total => Length + (Next?.Total ?? 0);

    /// <summary>获取或设置指定位置的字节值，支持跨链式包访问</summary>
    /// <param name="index">从当前包起始的相对索引位置</param>
    /// <returns>指定位置的字节值</returns>
    /// <exception cref="IndexOutOfRangeException">索引超出有效范围</exception>
    /// <exception cref="ObjectDisposedException">实例已释放</exception>
    public Byte this[Int32 index]
    {
        get => index switch
        {
            < 0 => throw new IndexOutOfRangeException($"Index cannot be negative: {index}"),
            var i when i < _length => Buffer[_offset + i],
            var i when Next != null => Next[i - _length],
            _ => throw new IndexOutOfRangeException($"Index {index} exceeds total length {Total}")
        };
        set
        {
            switch (index)
            {
                case < 0:
                    throw new IndexOutOfRangeException($"Index cannot be negative: {index}");
                case var i when i < _length:
                    Buffer[_offset + i] = value;
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

        _buffer = ArrayPool<Byte>.Shared.Rent(length);
        _offset = 0;
        _length = length;
        _hasOwner = true;
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
        _hasOwner = hasOwner;
    }

    /// <summary>基于现有实例创建扩展头部的新内存包，转移所有权</summary>
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

        if (owner._offset < expandSize)
            throw new ArgumentOutOfRangeException(nameof(expandSize),
                $"Expand size {expandSize} exceeds available front space {owner._offset}");

        _buffer = owner._buffer;
        _offset = owner._offset - expandSize;
        _length = owner._length + expandSize;
        Next = owner.Next;

        // 转移所有权：新实例接管，原实例失权
        _hasOwner = owner._hasOwner;
        owner._hasOwner = false;
    }
    #endregion

    #region 内存管理
    /// <summary>释放资源，归还池化缓冲区并清理链式结构</summary>
    /// <param name="disposing">是否由 Dispose 调用</param>
    protected override void Dispose(Boolean disposing)
    {
        if (!_hasOwner) return;

        _hasOwner = false;
        var buffer = _buffer;
        _buffer = null; // 防止重复使用已释放的缓冲区

        if (buffer != null)
            ArrayPool<Byte>.Shared.Return(buffer);

        // 安全释放链式后续节点
        Next.TryDispose();
        Next = null;
    }

    /// <summary>立即放弃所有权，不归还缓冲区</summary>
    /// <remarks>
    /// <para>用于特殊场景下的所有权转移，调用后实例变为无效状态。</para>
    /// <para>警告：缓冲区不会被归还到池中，可能导致内存泄漏。</para>
    /// </remarks>
    public void Free()
    {
        _buffer = null;
        Next = null;
        _hasOwner = false;
    }
    #endregion

    #region 内存访问
    /// <summary>获取当前数据包的内存片段视图</summary>
    /// <returns>只读内存片段，仅在实例生命周期内有效</returns>
    /// <exception cref="ObjectDisposedException">实例已释放</exception>
    public override Span<Byte> GetSpan() => new(Buffer, _offset, _length);

    /// <summary>获取当前数据包的内存块</summary>
    /// <returns>内存块，仅在实例生命周期内有效</returns>
    /// <exception cref="ObjectDisposedException">实例已释放</exception>
    public Memory<Byte> GetMemory() => new(Buffer, _offset, _length);

    /// <summary>尝试获取当前片段的数组段表示</summary>
    /// <param name="segment">输出的数组段</param>
    /// <returns>始终返回 true</returns>
    /// <exception cref="ObjectDisposedException">实例已释放</exception>
    protected override Boolean TryGetArray(out ArraySegment<Byte> segment)
    {
        segment = new ArraySegment<Byte>(Buffer, _offset, _length);
        return true;
    }

    /// <summary>尝试获取当前片段的数组段表示（显式接口实现）</summary>
    /// <param name="segment">输出的数组段</param>
    /// <returns>始终返回 true</returns>
    Boolean IPacket.TryGetArray(out ArraySegment<Byte> segment) => TryGetArray(out segment);
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
    #endregion

    #region 切片操作
    /// <summary>切片生成新数据包，默认转移所有权</summary>
    /// <param name="offset">相对当前包的起始偏移</param>
    /// <param name="count">切片长度，-1 表示到末尾</param>
    /// <returns>新的数据包实例</returns>
    public IPacket Slice(Int32 offset, Int32 count = -1) => Slice(offset, count, transferOwner: true);

    /// <summary>切片生成新数据包，可选择是否转移所有权</summary>
    /// <param name="offset">相对当前包的起始偏移</param>
    /// <param name="count">切片长度，-1 表示到末尾</param>
    /// <param name="transferOwner">是否转移内存管理权</param>
    /// <returns>新的数据包实例</returns>
    /// <exception cref="ArgumentOutOfRangeException">偏移量或长度超出有效范围</exception>
    /// <exception cref="ObjectDisposedException">实例已释放</exception>
    /// <remarks>
    /// <para>切片操作共享底层缓冲区以避免内存拷贝。</para>
    /// <para>当 transferOwner 为 true 时，新实例负责缓冲区释放，原实例失去管理权。</para>
    /// <para>支持跨链式包切片，自动处理边界情况。</para>
    /// </remarks>
    public IPacket Slice(Int32 offset, Int32 count, Boolean transferOwner)
    {
        if (_buffer == null) throw new ObjectDisposedException(nameof(OwnerPacket));

        if (offset < 0)
            throw new ArgumentOutOfRangeException(nameof(offset), "Offset cannot be negative.");

        if (count > Total - offset)
            throw new ArgumentOutOfRangeException(nameof(count),
                $"Count {count} with offset {offset} exceeds total length {Total}");

        var startPosition = _offset + offset;
        var remainInCurrent = _length - offset;
        var hasOwnership = _hasOwner && transferOwner;

        // 单段数据包处理
        if (Next == null)
        {
            // 转移管理权
            if (transferOwner) _hasOwner = false;

            var actualCount = count < 0 || count > remainInCurrent ? remainInCurrent : count;
            return new OwnerPacket(_buffer, startPosition, actualCount, hasOwnership);
        }

        // 多段链式包处理
        if (remainInCurrent <= 0)
        {
            // 完全跳过当前段，递归处理后续段
            return Next.Slice(offset - _length, count, transferOwner);
        }

        // 转移管理权
        if (transferOwner) _hasOwner = false;

        if (count < 0)
        {
            // 当前段部分 + 所有后续段
            return new OwnerPacket(_buffer, startPosition, remainInCurrent, hasOwnership) { Next = Next };
        }

        if (count <= remainInCurrent)
        {
            // 仅在当前段内
            return new OwnerPacket(_buffer, startPosition, count, hasOwnership);
        }

        // 跨段处理：当前段 + 后续段切片
        return new OwnerPacket(_buffer, startPosition, remainInCurrent, hasOwnership)
        {
            Next = Next.Slice(0, count - remainInCurrent, transferOwner)
        };
    }
    #endregion

    #region 不支持的 MemoryManager 操作
    /// <summary>不支持内存钉住操作</summary>
    /// <exception cref="NotSupportedException">该操作不被支持</exception>
    public override MemoryHandle Pin(Int32 elementIndex = 0) =>
        throw new NotSupportedException("Memory pinning is not supported by OwnerPacket");

    /// <summary>不支持取消内存钉住操作</summary>
    /// <exception cref="NotSupportedException">该操作不被支持</exception>
    public override void Unpin() =>
        throw new NotSupportedException("Memory unpinning is not supported by OwnerPacket");
    #endregion

    #region 字符串表示
    /// <summary>返回数据包的字符串表示形式</summary>
    /// <returns>包含缓冲区大小、偏移量、长度和总长度的格式化字符串</returns>
    public override String ToString() =>
        $"[{_buffer?.Length ?? 0}]({_offset}, {_length})<{Total}>";
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

            return _memory.Span[index];
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
    }

    /// <summary>获取分片包。在管理权生命周期内短暂使用</summary>
    /// <returns></returns>
    public readonly Span<Byte> GetSpan() => _memory.Span[.._length];

    /// <summary>获取内存包。在管理权生命周期内短暂使用</summary>
    /// <returns></returns>
    public readonly Memory<Byte> GetMemory() => _memory[.._length];

    /// <summary>切片得到新数据包，共用内存块</summary>
    /// <param name="offset">偏移</param>
    /// <param name="count">个数。默认-1表示到末尾</param>
    public IPacket Slice(Int32 offset, Int32 count) => Slice(offset, count, true);

    /// <summary>切片得到新数据包，共用内存块</summary>
    /// <param name="offset">偏移</param>
    /// <param name="count">个数。默认-1表示到末尾</param>
    /// <param name="transferOwner">转移所有权。不支持</param>
    public IPacket Slice(Int32 offset, Int32 count, Boolean transferOwner)
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

    /// <summary>尝试获取缓冲区。仅本片段，不包括Next</summary>
    /// <param name="segment"></param>
    /// <returns></returns>
    public readonly Boolean TryGetArray(out ArraySegment<Byte> segment) => MemoryMarshal.TryGetArray(GetMemory(), out segment);

    /// <summary>已重载</summary>
    /// <returns></returns>
    public override readonly String ToString() => $"[{_memory.Length}](0, {_length})<{Total}>";
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
    public readonly Span<Byte> GetSpan() => new(_buffer, _offset, _length);

    /// <summary>获取内存包。在管理权生命周期内短暂使用</summary>
    /// <returns></returns>
    public readonly Memory<Byte> GetMemory() => new(_buffer, _offset, _length);

    /// <summary>切片得到新数据包，共用缓冲区</summary>
    /// <param name="offset">偏移</param>
    /// <param name="count">个数。默认-1表示到末尾</param>
    IPacket IPacket.Slice(Int32 offset, Int32 count) => (this as IPacket).Slice(offset, count, true);

    /// <summary>切片得到新数据包，共用缓冲区</summary>
    /// <param name="offset">偏移</param>
    /// <param name="count">个数。默认-1表示到末尾</param>
    /// <param name="transferOwner">转移所有权。仅对Next有效</param>
    IPacket IPacket.Slice(Int32 offset, Int32 count, Boolean transferOwner)
    {
        if (count == 0) return Empty;

        var remain = _length - offset;
        var next = Next;
        if (next != null && remain <= 0) return next.Slice(offset - _length, count, transferOwner);

        return Slice(offset, count, transferOwner);
    }

    /// <summary>切片得到新数据包，共用缓冲区，无内存分配</summary>
    /// <param name="offset">偏移</param>
    /// <param name="count">个数。默认-1表示到末尾</param>
    /// <param name="transferOwner">转移所有权。仅对Next有效</param>
    public ArrayPacket Slice(Int32 offset, Int32 count = -1, Boolean transferOwner = false)
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
            return (ArrayPacket)next.Slice(offset - _length, count, transferOwner);

        // 当前包用一截，剩下的全部
        if (count < 0)
            return new ArrayPacket(_buffer, start, remain) { Next = next };

        // 当前包可以读完
        if (count <= remain)
            return new ArrayPacket(_buffer, start, count);

        // 当前包用一截，剩下的再截取
        return new ArrayPacket(_buffer, start, remain) { Next = next.Slice(0, count - remain, transferOwner) };
    }

    /// <summary>尝试获取缓冲区。仅本片段，不包括Next</summary>
    /// <param name="segment"></param>
    /// <returns></returns>
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
    public override readonly String ToString() => $"[{_buffer.Length}]({_offset}, {_length})<{Total}>";
    #endregion
}
