using System.Buffers;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using NewLife.Collections;

namespace NewLife.Data;

/// <summary>数据包接口。几乎内存共享理念，统一提供数据包，内部可能是内存池、数组和旧版Packet等多种实现</summary>
/// <remarks>
/// 常用于网络编程和协议解析，为了避免大量内存分配和拷贝，采用数据包对象池，复用内存。数据包接口一般由结构体实现以降低 GC 压力。
/// 需特别注意 <b>内存管理权</b> 转移：调用栈上层（获得包的一方）负责最终释放（实现 <see cref="IDisposable"/> 或所有权回收）。
/// - 非阻塞 Socket：接收方申请与释放；解析逻辑只消费不负责释放；
/// - 阻塞 Socket：接收函数申请，外部使用方释放，管理权可进一步传递；
/// 旧版 Packet 作为过渡也实现该接口，便于逐步替换。切片 <see cref="Slice"/> 默认共享底层缓冲区，必要时可指定是否转移所有权（并非所有实现都支持）。
/// 所有临时获得的 <see cref="Span{T}"/> / <see cref="Memory{T}"/> 仅在当前所有权生命周期内短暂使用，禁止缓存到异步/长期结构中。
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
public interface IOwnerPacket : IPacket, IDisposable { }

/// <summary>内存包辅助类</summary>
public static class PacketHelper
{
    /// <summary>附加一个包到当前包链的末尾</summary>
    /// <remarks>保持 O(n) 尾插；若 <paramref name="next"/> 已经包含链，会整体挂接。避免自引用造成死循环。</remarks>
    /// <param name="pk">当前链首</param>
    /// <param name="next">要追加的包（可包含自身链）</param>
    /// <returns>链首（保持不变便于链式调用）</returns>
    public static IPacket Append(this IPacket pk, IPacket next)
    {
        if (next == null) return pk;
        if (ReferenceEquals(pk, next)) return pk; // 防止自连接

        var p = pk;
        while (p.Next != null)
        {
            // 避免形成环：若即将跨越到 next（或其后续）则终止，防御性处理
            if (ReferenceEquals(p.Next, pk)) break;
            p = p.Next;
        }
        p.Next = next;

        return pk;
    }

    /// <summary>附加一个字节数组为新包到末尾</summary>
    /// <param name="pk">当前链首</param>
    /// <param name="next">字节数组</param>
    public static IPacket Append(this IPacket pk, Byte[] next) => Append(pk, new ArrayPacket(next));

    /// <summary>转字符串</summary>
    /// <remarks>
    /// 1. 单包路径：直接切片 + <c>Span</c> 编码，避免额外分配。
    /// 2. 多包链：按需跳过 <paramref name="offset"/>，逐段拷贝，严格控制 <paramref name="count"/>。
    /// 3. <paramref name="count"/> 为 -1 表示到末尾；超出可用长度时自动截断。
    /// 4. 允许以扩展方式在空引用上调用：若 <paramref name="pk"/> 为 <c>null</c> 返回 <c>null</c>（兼容既有单测）。
    /// </remarks>
    /// <param name="pk">数据包（可为 null）</param>
    /// <param name="encoding">编码。null 表示 UTF8（由外部扩展处理）</param>
    /// <param name="offset">起始偏移（跨链整体）</param>
    /// <param name="count">读取字节数，-1 表示直到结尾</param>
    /// <returns>得到的字符串；<c>pk</c> 为 null 时返回 null</returns>
    public static String ToStr(this IPacket pk, Encoding? encoding = null, Int32 offset = 0, Int32 count = -1)
    {
        // 允许 null（单测覆盖）
        if (pk == null) return null!;

        if (offset < 0) offset = 0;
        if (count == 0) return String.Empty;

        // 总长度为 0
        var total = pk.Total;
        if (total == 0) return String.Empty;
        if (offset >= total) return String.Empty;

        // 单包快速路径（热点）
        if (pk.Next == null)
        {
            var len = pk.Length;
            if (offset >= len) return String.Empty;
            if (count < 0 || count > len - offset) count = len - offset;
            return pk.GetSpan().Slice(offset, count).ToStr(encoding);
        }

        // 规范化 count（多包）
        if (count < 0 || count > total - offset) count = total - offset;
        if (count <= 0) return String.Empty;

        var skip = offset;
        var remain = count;
        var sb = Pool.StringBuilder.Get();
        for (var p = pk; p != null && remain > 0; p = p.Next)
        {
            var span = p.GetSpan();
            if (skip >= span.Length)
            {
                skip -= span.Length;
                continue;
            }

            // 进入有效区
            span = span[skip..];
            skip = 0;
            if (span.Length > remain) span = span[..remain];

            sb.Append(span.ToStr(encoding));
            remain -= span.Length;
        }
        return sb.Return(true);
    }

    /// <summary>以十六进制编码表示</summary>
    /// <remarks>
    /// 修正：原实现以首段 <c>Length</c> 判空，若首段长度为 0 但存在后续链会错误返回空；现改为基于 <c>Total</c> 判空。
    /// 遍历多段时保持全局已写入计数，确保分隔与分组逻辑在跨段时连续。
    /// </remarks>
    /// <param name="pk">数据包</param>
    /// <param name="maxLength">最大显示字节数。默认 32，-1 显示全部</param>
    /// <param name="separate">分隔符。null/空表示不分隔</param>
    /// <param name="groupSize">分组大小。0 表示每字节应用分隔符</param>
    /// <returns>十六进制字符串</returns>
    public static String ToHex(this IPacket pk, Int32 maxLength = 32, String? separate = null, Int32 groupSize = 0)
    {
        if (pk == null) return null!;

        var total = pk.Total;
        if (total == 0) return String.Empty;
        if (maxLength == 0) return String.Empty;
        if (groupSize < 0) groupSize = 0;

        // 单包快速路径
        if (pk.Next == null) return pk.GetSpan().ToHex(separate, groupSize, maxLength);

        // 多包：需要跨段连续的分隔/分组
        var sb = Pool.StringBuilder.Get();
        const String HEX = "0123456789ABCDEF";
        var written = 0; // 全局已写入字节数

        for (var p = pk; p != null; p = p.Next)
        {
            var span = p.GetSpan();
            for (var i = 0; i < span.Length; i++)
            {
                if (maxLength >= 0 && written >= maxLength) goto END;

                if (written > 0 && !separate.IsNullOrEmpty())
                {
                    if (groupSize <= 0 || written % groupSize == 0) sb.Append(separate);
                }

                var b = span[i];
                sb.Append(HEX[b >> 4]);
                sb.Append(HEX[b & 0x0F]);
                written++;
            }
        }
END:
        return sb.Return(true);
    }

    /// <summary>写入数据流，netfx 中可能有二次拷贝</summary>
    /// <param name="pk">数据包</param>
    /// <param name="stream">目标流</param>
    public static void CopyTo(this IPacket pk, Stream stream)
    {
        for (var p = pk; p != null; p = p.Next)
        {
            if (p.TryGetArray(out var segment))
                stream.Write(segment.Array!, segment.Offset, segment.Count);
            else
                stream.Write(p.GetMemory());
        }
    }

    /// <summary>异步拷贝</summary>
    /// <param name="pk">数据包</param>
    /// <param name="stream">目标流</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static async Task CopyToAsync(this IPacket pk, Stream stream, CancellationToken cancellationToken = default)
    {
        for (var p = pk; p != null; p = p.Next)
        {
            if (p.TryGetArray(out var segment))
                await stream.WriteAsync(segment.Array!, segment.Offset, segment.Count).ConfigureAwait(false);
            else
                await stream.WriteAsync(p.GetMemory(), cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>获取数据流（复制）</summary>
    /// <param name="pk">数据包</param>
    /// <returns>独立可读写的内存流</returns>
    public static Stream GetStream(this IPacket pk)
    {
        var ms = new MemoryStream(pk.Total);
        pk.CopyTo(ms);
        ms.Position = 0;
        return ms;
    }

    /// <summary>返回数据段，可能有拷贝</summary>
    /// <param name="pk">数据包</param>
    /// <returns>数据段（若多包则聚合复制）</returns>
    public static ArraySegment<Byte> ToSegment(this IPacket pk)
    {
        if (pk.Next == null && pk.TryGetArray(out var segment)) return segment;

        var ms = Pool.MemoryStream.Get();
        pk.CopyTo(ms);
        ms.Position = 0;
        return new ArraySegment<Byte>(ms.Return(true));
    }

    /// <summary>返回数据段集合，可能有拷贝</summary>
    /// <param name="pk">数据包</param>
    /// <returns>段集合（每个元素对应链上一个片段；不展开再聚合）</returns>
    public static IList<ArraySegment<Byte>> ToSegments(this IPacket pk)
    {
        // 初始4元素，优化扩容
        var list = new List<ArraySegment<Byte>>(4);
        for (var p = pk; p != null; p = p.Next)
        {
            if (p.TryGetArray(out var seg))
                list.Add(seg);
            else
                list.Add(new ArraySegment<Byte>(p.GetSpan().ToArray(), 0, p.Length));
        }
        return list;
    }

    /// <summary>返回字节数组。无差别复制，一定返回新数组</summary>
    /// <param name="pk">数据包</param>
    public static Byte[] ToArray(this IPacket pk)
    {
        if (pk.Next == null) return pk.GetSpan().ToArray();

        // 链式包输出
        var ms = Pool.MemoryStream.Get();
        pk.CopyTo(ms);
        return ms.Return(true);
    }

    /// <summary>从封包中读取指定数据区。读取全部时可能直接返回底层数组以提升性能</summary>
    /// <param name="pk">数据包</param>
    /// <param name="offset">相对于数据包的起始位置，实际上是数组的Offset+offset</param>
    /// <param name="count">字节个数。-1 表示直到末尾</param>
    public static Byte[] ReadBytes(this IPacket pk, Int32 offset = 0, Int32 count = -1)
    {
        if (pk.Next == null)
        {
            if (count < 0) count = pk.Length - offset;

            if (pk.TryGetArray(out var seg))
            {
                // 读取全部（直接返回以减少复制）
                if (offset == 0 && count == pk.Length)
                {
                    if (seg.Offset == 0 && seg.Count == seg.Array!.Length) return seg.Array;
                }
                return seg.Array!.ReadBytes(seg.Offset + offset, count);
            }

            var span = pk.GetSpan();
            return span.Slice(offset, count).ToArray();
        }

        return pk.ToArray().ReadBytes(offset, count);
    }

    /// <summary>深度克隆一份数据包（拷贝数据区）</summary>
    /// <param name="pk">数据包</param>
    public static IPacket Clone(this IPacket pk)
    {
        if (pk.Next == null) return new ArrayPacket(pk.GetSpan().ToArray());

        var ms = new MemoryStream();
        pk.CopyTo(ms);
        ms.Position = 0;
        return new ArrayPacket(ms);
    }

    /// <summary>尝试获取内存片段。非链式数据包时直接返回</summary>
    /// <param name="pk">数据包</param>
    /// <param name="span">输出的内存片段</param>
    /// <returns>是否成功（仅当无 Next 链）</returns>
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

    /// <summary>尝试扩展头部，用于填充包头，减少内存分配（过渡 API）</summary>
    /// <param name="pk">数据包</param>
    /// <param name="size">要扩大的头部大小，不包括负载数据</param>
    /// <param name="newPacket">扩展后的数据包</param>
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

    /// <summary>扩展头部，用于填充包头，减少内存分配</summary>
    /// <param name="pk">原始数据包</param>
    /// <param name="size">需增加的头部大小</param>
    /// <returns>新的数据包（可能复用原缓冲区）</returns>
    public static IPacket ExpandHeader(this IPacket? pk, Int32 size)
    {
        if (pk is ArrayPacket ap && ap.Offset >= size)
            return new ArrayPacket(ap.Buffer, ap.Offset - size, ap.Length + size) { Next = ap.Next };
        else if (pk is OwnerPacket owner && owner.Offset >= size)
            return new OwnerPacket(owner, size);

        return new OwnerPacket(size) { Next = pk };
    }
}

/// <summary>所有权内存包。具有所有权管理，不再使用时需调用 <see cref="Dispose"/> 或通过上层机制释放</summary>
/// <remarks>内部使用 <see cref="ArrayPool{T}"/>。切片可转移所有权（仅一次）。</remarks>
public class OwnerPacket : MemoryManager<Byte>, IPacket, IOwnerPacket
{
    #region 属性
    private Byte[] _buffer;
    /// <summary>缓冲区</summary>
    public Byte[] Buffer => _buffer;

    private Int32 _offset;
    /// <summary>数据偏移</summary>
    public Int32 Offset => _offset;

    private Int32 _length;
    /// <summary>数据长度</summary>
    public Int32 Length => _length;

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

    /// <summary>下一个链式包</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public IPacket? Next { get; set; }

    /// <summary>总长度</summary>
    public Int32 Total => Length + (Next?.Total ?? 0);

    private Boolean _hasOwner;
    #endregion

    #region 构造
    /// <summary>实例化指定长度的内存包，从共享内存池中借出</summary>
    /// <param name="length">长度</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public OwnerPacket(Int32 length)
    {
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be non-negative and less than or equal to the memory owner's length.");

        _buffer = ArrayPool<Byte>.Shared.Rent(length);
        _offset = 0;
        _length = length;
        _hasOwner = true;
    }

    /// <summary>实例化内存包，指定内存所有者和长度</summary>
    /// <param name="buffer">缓冲区</param>
    /// <param name="offset"></param>
    /// <param name="length">长度</param>
    /// <param name="hasOwner">是否转移所有权</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public OwnerPacket(Byte[] buffer, Int32 offset, Int32 length, Boolean hasOwner)
    {
        if (offset < 0 || length < 0 || offset + length > buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be non-negative and less than or equal to the memory owner's length.");

        _buffer = buffer;
        _offset = offset;
        _length = length;
        _hasOwner = hasOwner;
    }

    /// <summary>从另一个内存包创建新内存包，共用缓冲区</summary>
    /// <param name="owner"></param>
    /// <param name="expandSize"></param>
    public OwnerPacket(OwnerPacket owner, Int32 expandSize)
    {
        _buffer = owner.Buffer;
        _offset = owner.Offset - expandSize;
        _length = owner.Length + expandSize;
        Next = owner.Next;

        // 转移所有权
        _hasOwner = owner._hasOwner;
        owner._hasOwner = false;
    }

    /// <summary>销毁释放</summary>
    /// <param name="disposing"></param>
    protected override void Dispose(Boolean disposing)
    {
        if (!_hasOwner) return;
        _hasOwner = true;

        var buffer = _buffer;
        if (buffer != null)
        {
            // 释放内存所有者以后，直接置空，避免重复使用
            _buffer = null!;

            ArrayPool<Byte>.Shared.Return(buffer);
        }

        Next.TryDispose();
    }
    #endregion

    /// <summary>获取分片包。在管理权生命周期内短暂使用</summary>
    /// <returns></returns>
    public override Span<Byte> GetSpan() => new(_buffer, _offset, _length);

    /// <summary>获取内存包。在管理权生命周期内短暂使用</summary>
    /// <returns></returns>
    public Memory<Byte> GetMemory() => new(_buffer, _offset, _length);

    /// <summary>重新设置数据包大小。一般用于申请缓冲区并读取数据后设置为实际大小</summary>
    /// <param name="size"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public OwnerPacket Resize(Int32 size)
    {
        if (size < 0) throw new ArgumentNullException(nameof(size));

        if (Next == null)
        {
            if (size > _buffer.Length) throw new ArgumentOutOfRangeException(nameof(size));

            _length = size;
        }
        else
        {
            if (size >= _length) throw new NotSupportedException();

            _length = size;
        }

        return this;
    }

    /// <summary>切片得到新数据包</summary>
    /// <remarks>引用相同内存块，减少内存分配</remarks>
    /// <param name="offset">偏移</param>
    /// <param name="count">个数。默认-1表示到末尾</param>
    public IPacket Slice(Int32 offset, Int32 count) => Slice(offset, count, true);

    /// <summary>切片得到新数据包，同时转移内存管理权</summary>
    /// <remarks>引用相同内存块，减少内存分配</remarks>
    /// <param name="offset">偏移</param>
    /// <param name="count">个数。默认-1表示到末尾</param>
    /// <param name="transferOwner">转移所有权。若为true则由新数据包负责归还缓冲区，只能转移一次</param>
    public IPacket Slice(Int32 offset, Int32 count, Boolean transferOwner)
    {
        // 释放后无法再次使用
        if (_buffer == null) throw new InvalidDataException();

        var buffer = _buffer;
        var start = _offset + offset;
        var remain = _length - offset;
        var hasOwner = _hasOwner && transferOwner;

        // 超出范围
        if (count > Total - offset) throw new ArgumentOutOfRangeException(nameof(count), "count must be non-negative and less than or equal to the memory owner's length.");

        // 单个数据包
        if (Next == null)
        {
            // 转移管理权
            if (transferOwner) _hasOwner = false;

            if (count < 0 || count > remain) count = remain;
            return new OwnerPacket(buffer, start, count, hasOwner);
        }
        else
        {
            // 如果当前段用完，则取下一段。当前包自己负责释放
            if (remain <= 0) return Next.Slice(offset - _length, count, transferOwner);

            // 转移管理权
            if (transferOwner) _hasOwner = false;

            // 当前包用一截，剩下的全部。转移管理权后，Next随新包一起释放
            if (count < 0) return new OwnerPacket(buffer, start, remain, hasOwner) { Next = Next };

            // 当前包可以读完。转移管理权后，Next失去释放机会
            if (count <= remain) return new OwnerPacket(buffer, start, count, hasOwner);

            // 当前包用一截，剩下的再截取。转移管理权后，Next再次转移管理权，随新包一起释放
            return new OwnerPacket(buffer, start, remain, hasOwner) { Next = Next.Slice(0, count - remain, transferOwner) };
        }
    }

    /// <summary>尝试获取缓冲区。仅本片段，不包括Next</summary>
    /// <param name="segment"></param>
    /// <returns></returns>
    protected override Boolean TryGetArray(out ArraySegment<Byte> segment)
    {
        segment = new ArraySegment<Byte>(_buffer, _offset, _length);
        return true;
    }

    /// <summary>尝试获取数据段</summary>
    /// <param name="segment"></param>
    /// <returns></returns>
    Boolean IPacket.TryGetArray(out ArraySegment<Byte> segment) => TryGetArray(out segment);

    /// <summary>释放所有权，不再使用</summary>
    public void Free()
    {
        _buffer = null!;
        Next = null;
    }

    /// <summary>钉住内存</summary>
    /// <param name="elementIndex"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public override MemoryHandle Pin(Int32 elementIndex = 0) => throw new NotSupportedException();

    /// <summary>取消钉内存</summary>
    /// <exception cref="NotImplementedException"></exception>
    public override void Unpin() => throw new NotImplementedException();

    #region 重载运算符
    /// <summary>已重载</summary>
    /// <returns></returns>
    public override String ToString() => $"[{_buffer.Length}]({_offset}, {_length})<{Total}>";
    #endregion
}

/// <summary>内存包</summary>
/// <remarks>
/// 内存包可能来自内存池，失去所有权时已被释放，因此不应该长期持有。
/// </remarks>
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

        if (offset == 0)
            return new MemoryPacket(_memory, count);

        return new MemoryPacket(_memory[offset..], count);
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
public struct ArrayPacket : IPacket
{
    #region 属性
    private Byte[] _buffer;
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
            // GetBuffer窃取内部缓冲区后，无法得知真正的起始位置index，可能导致错误取数
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
            if (count < 0) count = 0;

            return new ArrayPacket(_buffer, start, count);
        }
        else
        {
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
    public override String ToString() => $"[{_buffer.Length}]({_offset}, {_length})<{Total}>";
    #endregion
}
