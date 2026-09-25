using System.Buffers;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

#if DEBUG
using NewLife.Log;
#endif

namespace NewLife.Data;

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
    public Byte[] Buffer { get; private set; } = buffer;

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

    /// <summary>接收环复用：重新绑定缓冲（前提：无其它引用持有）</summary>
    /// <param name="buffer">新的缓冲数组</param>
    public void Reset(Byte[] buffer) => Buffer = buffer;
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
/// <item>共享切片：<see cref="Slice(Int32, Int32)"/> 按段递增引用计数，返回独立句柄（具体类型 <see cref="OwnerPacket"/>，可直接 <c>using</c> 释放）；双方（或多方）均可继续使用，各自 <see cref="Dispose"/>，最后一个释放时才归还内存池</item>
/// <item>独占换窗：<see cref="OwnerPacket(OwnerPacket, Int32)"/> 头部扩展构造，接管源实例的引用与链，源实例整体作废（仅此一处保留接管语义，调用方需自行确保无其它共享句柄）</item>
/// <item>接收层轮末裁决：会话私有句柄在轮末按 <see cref="RefCount"/> 判定——无人持有（为 1）时回挂接收槽，下一轮 <see cref="Rebind"/> 重绑复用；存在共享引用时释放本引用并换新缓冲。把数据交给发送管道也计入共享引用（<c>SendPump.Append</c> 切出共享句柄），保证轮末不会复用仍在发送中的缓冲</item>
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
/// <item><b>成本基线（基准实测）</b>：所有权管理开销恒定、与数据大小无关——构造+释放约 19ns、共享切片（双方各释放一次）约 42ns；
/// 对照拷贝随大小线性增长（64B≈1.4ns、4KB≈45ns、64KB≈1.05µs、128KB≈2.8µs），故 2–4KB 是“小帧拷贝、大帧切片”的经验分界（≤512B 拷贝更便宜，≥4KB 切片占优）。
/// 每个拥有句柄固定多 32B 引用计数对象（ArrayOwner）；DEBUG 构建的析构兜底另加约 30~43ns/句柄，Release 默认不编译（见《内存分配与拷贝成本报告》）。</item>
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

    /// <summary>头部可借位空间。等于分配时预留的字节数，供下游向前借位写入协议头</summary>
    public Int32 FreeHeader => _offset;

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
    /// <param name="length">数据区长度</param>
    /// <param name="reserve">前置预留头部字节数，供下游向前借位写入协议头（参考 <see cref="Serialization.SpanSerializer.HeaderReserve"/>）</param>
    /// <exception cref="ArgumentOutOfRangeException">长度为负数或预留为负数</exception>
    /// <remarks>实际分配的缓冲区可能大于请求长度，以适配内存池的分片策略；预留空间经 <see cref="FreeHeader"/> 查询</remarks>
    public OwnerPacket(Int32 length, Int32 reserve = 0)
    {
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length), "Length must be non-negative.");
        if (reserve < 0) throw new ArgumentOutOfRangeException(nameof(reserve), "Reserve must be non-negative.");

        var buffer = ArrayPool<Byte>.Shared.Rent(length + reserve);
        _buffer = buffer;
        _offset = reserve;
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
        // 数据从 reserve 处开始写入，窗口必须从 reserve 开始：否则对外前 reserve 字节是池化脏数据、真数据被截掉
        _offset = reserve;
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
    /// <para>用于接收层复用缓冲：每轮把整块缓冲包装为句柄上抛，轮末确认没有其它持有者（<see cref="RefCount"/> 为 1）时即可脱手，
    /// 缓冲留在会话继续接收，做到零 Rent/Return；归还责任随脱手转交调用方，由其在会话关闭时归还。
    /// 接收环的日常轮末改为把句柄回挂接收槽复用（见 <see cref="Rebind"/>），仅在会话关闭时才真正脱手。</para>
    /// <para>数据支撑（基准实测）：池化 Rent+Return 合计约 8ns、与大小无关，但接收环每轮必经；
    /// 轮末脱手复用把“归还+再借”的成对开销省为零，缓冲常驻不换新。</para>
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

        // 链式后续节点会在本句柄脱手时丢失引用（链上缓存的归还责任随之消失），调用方确认无链才可脱手
        if (Next != null) throw new InvalidOperationException("Cannot detach while the packet has a Next segment; dispose the chain first.");

        // 抑制析构兜底，防止 GC 时误把仍在借用中的缓冲归还池
        GC.SuppressFinalize(this);

        _owner = null;
        _buffer = null;
        _length = 0;
        Next = null;
    }

    /// <summary>接收环复用：把本句柄重新绑定到下一轮缓冲段，避免每轮新建包装对象</summary>
    /// <remarks>
    /// <para>仅用于接收层：轮末无外部持有（<see cref="RefCount"/> 为 1）时把句柄回挂接收槽，下一轮开始时重绑到新收到的数据段。</para>
    /// <para>调用前提：本实例无其它持有者。缓冲通常仍是会话常驻缓冲，地址不变时仅更新偏移与长度；所有者缺失时补建。</para>
    /// </remarks>
    /// <param name="buffer">缓冲数组</param>
    /// <param name="offset">数据起始偏移</param>
    /// <param name="length">数据长度</param>
    /// <exception cref="InvalidOperationException">仍有其它句柄持有缓冲区</exception>
    internal void Rebind(Byte[] buffer, Int32 offset, Int32 length)
    {
        if (RefCount != 1)
            throw new InvalidOperationException($"Cannot rebind while other handle(s) still hold the buffer: {RefCount}");

        _buffer = buffer;
        _offset = offset;
        _length = length;
        Next = null;

        if (_owner == null)
            _owner = new ArrayOwner(buffer, true);
        else
            _owner.Reset(buffer);
    }
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
            // 上限按“从当前偏移起可用容量”算：带偏移的视图若按整个缓冲区长度放行，
            // 后续 GetSpan/GetMemory 会越窗（offset+length 超出数组长度）
            if (size > Buffer.Length - _offset)
                throw new ArgumentOutOfRangeException(nameof(size),
                    $"Size {size} exceeds available space {Buffer.Length - _offset}");

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
    /// <summary>切片生成新数据包，共享底层缓冲区（引用计数）。返回具体类型句柄，可直接 <c>using</c> 释放</summary>
    /// <param name="offset">相对当前包的起始偏移</param>
    /// <param name="count">切片长度，-1 表示到末尾</param>
    /// <returns>新的独立句柄（具体类型 <see cref="OwnerPacket"/>），与原包同时可用；各自 Dispose，最后一个释放时归还内存池</returns>
    /// <exception cref="ArgumentOutOfRangeException">偏移量或长度超出有效范围</exception>
    /// <exception cref="ObjectDisposedException">实例已释放</exception>
    /// <remarks>
    /// <para>切片共享底层缓冲区，不拷贝数据。支持跨段切片，自动处理边界；窗口覆盖的段各递增一次引用计数。</para>
    /// <para>成本（基准实测）：共享切片双方合计约 42ns，与数据大小无关；≥4KB 帧优于拷贝（64KB 拷贝约 1.05µs），
    /// ≤512B 小帧直接拷贝更便宜——小帧不要为共享而共享。</para>
    /// <para>本方法不改变原实例，双方（或多方）均可继续读取；每个句柄各自负责 <see cref="Dispose"/>。
    /// 线性交接场景直接传递句柄本身即可，无需切片。</para>
    /// <para><see cref="IPacket.Slice(Int32, Int32)"/> 与 <see cref="IOwnerPacket.Slice(Int32, Int32)"/> 是本方法的显式接口实现，经接口访问时分别返回各自声明类型。</para>
    /// </remarks>
    public OwnerPacket Slice(Int32 offset, Int32 count = -1)
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

    /// <summary>切片得到新数据包（<see cref="IPacket"/> 接口实现），共享底层缓冲区</summary>
    /// <param name="offset">相对当前包的起始偏移</param>
    /// <param name="count">切片长度，-1 表示到末尾</param>
    /// <returns>新的独立句柄</returns>
    IPacket IPacket.Slice(Int32 offset, Int32 count) => Slice(offset, count);

    /// <summary>切片得到新数据包（拥有句柄接口实现），共享底层缓冲区。返回拥有句柄，可 <c>using</c> 释放</summary>
    /// <param name="offset">相对当前包的起始偏移</param>
    /// <param name="count">切片长度，-1 表示到末尾</param>
    /// <returns>新的独立拥有句柄</returns>
    /// <remarks>与公开方法同一实现，切片结果始终为 <see cref="OwnerPacket"/> 句柄。</remarks>
    IOwnerPacket IOwnerPacket.Slice(Int32 offset, Int32 count) => Slice(offset, count);

    /// <summary>向前扩展头部空间（借位共享）。要求分配时已预留足够头部空间</summary>
    /// <param name="size">向前扩展的字节数</param>
    /// <returns>扩展后的新句柄，调用方负责 Dispose</returns>
    /// <remarks>
    /// <para>零拷贝借位：新句柄与原句柄共享同一缓冲区（引用计数各自释放），<b>原句柄保持有效</b>，可继续读取或再次构建。</para>
    /// <para>带链句柄会先切片为独占的共享节点链（节点各自持有引用）再前移链头，源句柄依旧不受影响。</para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">实例已释放</exception>
    /// <exception cref="InvalidOperationException">前置预留空间不足</exception>
    public OwnerPacket ExpandHeader(Int32 size)
    {
        if (_buffer == null) throw new ObjectDisposedException(nameof(OwnerPacket));
        if (_offset < size) throw new InvalidOperationException($"未预留 {size} 字节头部空间（当前 {_offset}）；请在分配时预留，例如 new OwnerPacket(size, reserve)");

        // 带链：先切片成独占的共享节点链（引用计数各自持有），链头随即可安全前移；源句柄不受影响
        if (Next != null)
        {
            var head = Slice(0, -1);
            head._offset -= size;
            head._length += size;

            return head;
        }

        // 单节点：共享借位，原句柄保持有效
        var owner = _owner;
        owner?.AddRef();

        return new OwnerPacket(_buffer, _offset - size, _length + size, owner);
    }
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
