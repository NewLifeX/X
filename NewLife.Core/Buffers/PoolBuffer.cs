using System.Buffers;
using System.Runtime.CompilerServices;

namespace NewLife.Buffers;

/// <summary>数组池缓冲句柄。从池中借出缓冲区，离开 using 作用域自动归还</summary>
/// <remarks>
/// <para>借出请用 <see cref="Collections.Pool.Rent(Int32)"/>；调用处用 <c>var</c> 接收即可，不必写类型名。</para>
/// <para>用于替代 <c>var buffer = pool.Rent(size); try { ... } finally { pool.Return(buffer); }</c> 这套样板代码。</para>
/// <para><b>零分配</b>：本类型为栈上类型，借出与归还全程不产生堆分配（对比 <see cref="Data.OwnerPacket"/> 每次借用需要两个对象）。</para>
/// <para><b>只能同步使用</b>：栈上类型不允许进入 async 方法、不允许存入字段、不允许被 lambda 捕获，编译器会直接报错。
/// async 方法内借出缓冲，请继续使用 try/finally。</para>
/// <para><b>使用纪律</b>：句柄不可复制后分开使用（原件与复制品都会归还同一个数组，造成池内同一块缓冲被两次借用）；
/// <see cref="Buffer"/> 就是借出的数组本身，生命周期与句柄绑定，出了 using 作用域它已归还，不能再读写。</para>
/// <para><b>长度语义</b>：<see cref="Length"/> 与 <see cref="Span"/> 均为<b>请求长度</b>（借出时传入的 size），
/// 池实际返回的数组可能更长，只在 <see cref="Buffer"/> / 隐式转换上体现。</para>
/// <para><b>与 <see cref="Data.OwnerPacket"/> 的分工</b>：本类型是单句柄、按作用域归还的轻量借用，不做引用计数、不支持切片共享；
/// 需要跨线程/跨 await 共享同一块内存时使用 <see cref="Data.OwnerPacket"/>。</para>
/// </remarks>
/// <example>
/// <code>
/// using var buffer = Pool.Rent(1472);
/// var count = stream.Read(buffer, 0, buffer.Length);
/// </code>
/// </example>
/// <typeparam name="T">元素类型</typeparam>
public readonly ref struct PoolBuffer<T>
{
    #region 属性
    /// <summary>借出它的数组池。null 表示默认实例（未借出任何缓冲）</summary>
    private readonly ArrayPool<T>? _pool;

    /// <summary>池实际返回的数组，长度不小于请求长度</summary>
    private readonly T[]? _buffer;

    /// <summary>请求的元素数量</summary>
    private readonly Int32 _length;

    /// <summary>池实际返回的数组，长度不小于请求长度；默认实例（未借出）时为 null</summary>
    /// <remarks>只给“只接受数组”的下游 API 用（如旧框架的 Stream.Read/Write、Socket.SendTo/ReceiveFrom、IOControl、crypto 变换）。</remarks>
    public T[] Buffer => _buffer!;

    /// <summary>请求的元素数量。等于借出时请求的长度，不受池实际返回长度影响；默认实例为 0</summary>
    public Int32 Length => _length;

    /// <summary>借出缓冲区的可写视图，长度等于请求长度（池返回更长时也不碰多出来的部分）。默认实例为空跨度</summary>
    public Span<T> Span => _buffer == null ? default : _buffer.AsSpan(0, _length);

    /// <summary>获取或设置指定位置的元素，直接作用于池实际返回的数组</summary>
    /// <param name="index">元素索引</param>
    /// <returns>元素引用</returns>
    /// <remarks>索引范围是数组长度，正常使用不要越过 <see cref="Length"/>。</remarks>
    public ref T this[Int32 index] => ref _buffer![index];

    /// <summary>转换为借出的数组，便于直接把句柄当作数组实参传入</summary>
    /// <param name="buffer">缓冲句柄</param>
    public static implicit operator T[](PoolBuffer<T> buffer) => buffer._buffer!;
    #endregion

    #region 构造
    /// <summary>实例化缓冲句柄。请通过 <see cref="Collections.Pool.Rent(Int32)"/> 借出，不要手工构造</summary>
    /// <param name="pool">数组池</param>
    /// <param name="buffer">池返回的数组</param>
    /// <param name="length">请求的元素数量</param>
    internal PoolBuffer(ArrayPool<T> pool, T[] buffer, Int32 length)
    {
        _pool = pool;
        _buffer = buffer;
        _length = length;
    }
    #endregion

    #region 方法
    /// <summary>归还缓冲区到数组池。默认实例（未借出）为空操作</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose() => _pool?.Return(_buffer!);
    #endregion
}
