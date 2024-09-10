using System;
using System.Buffers;
using System.Text;
using NewLife.Collections;

namespace NewLife.Data;

/// <summary>数据包接口。几乎内存共享理念，统一提供数据包，内部可能是内存池、数组和旧版Packet等多种实现</summary>
/// <remarks>
/// 常用于网络编程和协议解析，为了避免大量内存分配和拷贝，采用数据包对象池，复用内存。
/// 数据包接口一般由结构体实现，提升GC性能。
/// 
/// 特别需要注意内存管理权转移问题，一般由调用栈的上部负责释放内存。
/// Socket非阻塞事件接收时，负责申请与释放内存，数据处理是调用栈下游；
/// Socket阻塞接收时，接收函数内部申请内存，外部使用方释放内存，管理权甚至在此次传递给消息层；
/// 
/// 作为过渡期，旧版Packet也会实现该接口，以便逐步替换。
/// </remarks>
public interface IPacket
{
    /// <summary>数据长度</summary>
    Int32 Length { get; }

    /// <summary>下一个链式包</summary>
    IPacket? Next { get; set; }

    /// <summary>获取/设置 指定位置的字节</summary>
    /// <param name="index"></param>
    /// <returns></returns>
    Byte this[Int32 index] { get; set; }

    /// <summary>获取分片包。在管理权生命周期内短暂使用</summary>
    /// <returns></returns>
    Span<Byte> GetSpan();

    /// <summary>获取内存包。在管理权生命周期内短暂使用</summary>
    /// <returns></returns>
    Memory<Byte> GetMemory();

    /// <summary>切片得到新数据包，同时转移内存管理权，当前数据包应尽快停止使用</summary>
    /// <remarks>
    /// 可能是引用同一块内存，也可能是新的内存。
    /// 可能就是当前数据包，也可能引用相同的所有者或数组。
    /// </remarks>
    /// <param name="offset">偏移</param>
    /// <param name="count">个数。默认-1表示到末尾</param>
    /// <returns></returns>
    IPacket Slice(Int32 offset, Int32 count = -1);
}

/// <summary>内存包辅助类</summary>
public static class PacketHelper
{
    /// <summary>整个数据包调用链长度</summary>
    /// <param name="pk"></param>
    /// <returns></returns>
    public static Int32 GetTotal(this IPacket pk) => pk.Length + (pk.Next?.GetTotal() ?? 0);

    /// <summary>附加一个包到当前包链的末尾</summary>
    /// <param name="pk"></param>
    /// <param name="next"></param>
    public static IPacket Append(this IPacket pk, IPacket next)
    {
        if (next == null) return pk;

        var p = pk;
        while (p.Next != null) p = p.Next;
        p.Next = next;

        return pk;
    }

    /// <summary>转字符串并释放</summary>
    /// <param name="pk"></param>
    /// <param name="encoding"></param>
    /// <returns></returns>
    public static String ToStr(this IPacket pk, Encoding? encoding = null)
    {
        if (pk.Next == null)
        {
            var rs = pk.GetSpan().ToStr(encoding);
            pk.TryDispose();
            return rs;
        }

        var sb = Pool.StringBuilder.Get();
        for (var p = pk; p != null; p = p.Next)
        {
            sb.Append(p.GetSpan().ToStr(encoding));
            p.TryDispose();
        }
        return sb.Return(true);
    }

    /// <summary>拷贝</summary>
    /// <param name="pk"></param>
    /// <param name="stream"></param>
    public static void CopyTo(this IPacket pk, Stream stream)
    {
#if NETCOREAPP
        for (var p = pk; p != null; p = p.Next)
        {
            stream.Write(p.GetSpan());
        }
#else
        for (var p = pk; p != null; p = p.Next)
        {
            if (p is ArrayPacket ap)
                stream.Write(ap.Buffer, ap.Offset, ap.Length);
            else
                stream.Write(p.GetSpan().ToArray());
        }
#endif
    }

    /// <summary>异步拷贝</summary>
    /// <param name="pk"></param>
    /// <param name="stream"></param>
    /// <returns></returns>
    public static async Task CopyToAsync(this IPacket pk, Stream stream)
    {
        for (var p = pk; p != null; p = p.Next)
        {
            await stream.WriteAsync(p.GetMemory());
        }
    }

    /// <summary>获取数据流</summary>
    /// <param name="pk"></param>
    /// <returns></returns>
    public static Stream GetStream(this IPacket pk)
    {
        if (pk is ArrayPacket ap) return new MemoryStream(ap.Buffer, ap.Offset, ap.Length);

        return new MemoryStream(pk.GetSpan().ToArray());
    }

    /// <summary>返回数据段集合</summary>
    /// <returns></returns>
    public static IList<ArraySegment<Byte>> ToSegments(this IPacket pk)
    {
        // 初始4元素，优化扩容
        var list = new List<ArraySegment<Byte>>(4);

        for (var p = pk; p != null; p = p.Next)
        {
            if (p is ArrayPacket ap)
                list.Add(new ArraySegment<Byte>(ap.Buffer, ap.Offset, ap.Length));
            else
                list.Add(new ArraySegment<Byte>(p.GetSpan().ToArray(), 0, p.Length));
        }

        return list;
    }
}

/// <summary>所有权内存包。具有所有权管理，不再使用时释放</summary>
/// <remarks>
/// 使用时务必明确所有权归属，用完后及时释放。
/// </remarks>
public struct OwnerPacket : IDisposable, IPacket
{
    #region 属性
    private IMemoryOwner<Byte> _owner;
    /// <summary>内存所有者</summary>
    public IMemoryOwner<Byte> Owner => _owner;

    private readonly Int32 _length;
    /// <summary>数据长度</summary>
    public Int32 Length => _length;

    /// <summary>获取/设置 指定位置的字节</summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Byte this[Int32 index] { get => _owner.Memory.Span[index]; set => _owner.Memory.Span[index] = value; }

    /// <summary>是否拥有管理权</summary>
    public Boolean HasOwner { get; set; }

    /// <summary>下一个链式包</summary>
    public IPacket? Next { get; set; }
    #endregion

    /// <summary>实例化指定长度的内存包，从共享内存池中借出</summary>
    /// <param name="length">长度</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public OwnerPacket(Int32 length)
    {
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be non-negative and less than or equal to the memory owner's length.");

        _owner = MemoryPool<Byte>.Shared.Rent(length);
        _length = length;
        HasOwner = true;
    }

    /// <summary>实例化内存包，指定内存所有者和长度</summary>
    /// <param name="memoryOwner">内存所有者</param>
    /// <param name="length">长度</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public OwnerPacket(IMemoryOwner<Byte> memoryOwner, Int32 length)
    {
        if (length < 0 || length > memoryOwner.Memory.Length)
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be non-negative and less than or equal to the memory owner's length.");

        _owner = memoryOwner;
        _length = length;
    }

    /// <summary>释放</summary>
    public void Dispose()
    {
        //if (!HasOwner) throw new InvalidOperationException("Has not owner.");

        var owner = _owner;
        if (HasOwner && owner != null)
        {
            // 释放内存所有者以后，直接置空，避免重复使用
            _owner = null!;
            owner.Dispose();
            HasOwner = false;
        }
    }

    /// <summary>获取分片包。在管理权生命周期内短暂使用</summary>
    /// <returns></returns>
    public Span<Byte> GetSpan() => _owner.Memory.Span[.._length];

    /// <summary>获取内存包。在管理权生命周期内短暂使用</summary>
    /// <returns></returns>
    public Memory<Byte> GetMemory() => _owner.Memory[.._length];

    /// <summary>切片得到新数据包，同时转移内存管理权，当前数据包应尽快停止使用</summary>
    /// <remarks>
    /// 可能是引用同一块内存，也可能是新的内存。
    /// 可能就是当前数据包，也可能引用相同的所有者或数组。
    /// </remarks>
    /// <param name="offset">偏移</param>
    /// <param name="count">个数。默认-1表示到末尾</param>
    public IPacket Slice(Int32 offset, Int32 count)
    {
        var remain = _length - offset;
        if (count < 0 || count > remain) count = remain;
        if (offset == 0 && count == _length) return this;

        if (offset == 0)
        {
            var pk = new OwnerPacket(_owner, count) { HasOwner = HasOwner };
            HasOwner = false;
            return pk;
        }

        // 当前数据包可能会释放，必须拷贝数据
        //return new MemoryPacket(_owner.Memory.Slice(offset, count), count);
        var rs = new ArrayPacket(count);
        GetSpan().CopyTo(rs.GetSpan());
        return rs;
    }
}

/// <summary>内存包</summary>
/// <remarks>
/// 内存包可能来自内存池，失去所有权时已被释放，因此不应该长期持有。
/// </remarks>
public struct MemoryPacket : IPacket
{
    #region 属性
    private Memory<Byte> _memory;
    /// <summary>内存</summary>
    public Memory<Byte> Memory => _memory;

    private readonly Int32 _length;
    /// <summary>数据长度</summary>
    public Int32 Length => _length;

    /// <summary>获取/设置 指定位置的字节</summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Byte this[Int32 index] { get => _memory.Span[index]; set => _memory.Span[index] = value; }

    /// <summary>下一个链式包</summary>
    public IPacket? Next { get; set; }
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
    public Span<Byte> GetSpan() => _memory.Span[.._length];

    /// <summary>获取内存包。在管理权生命周期内短暂使用</summary>
    /// <returns></returns>
    public Memory<Byte> GetMemory() => _memory[.._length];

    /// <summary>切片得到新数据包</summary>
    /// <remarks>
    /// 可能是引用同一块内存，也可能是新的内存。
    /// 可能就是当前数据包，也可能引用相同的所有者或数组。
    /// </remarks>
    /// <param name="offset">偏移</param>
    /// <param name="count">个数。默认-1表示到末尾</param>
    public IPacket Slice(Int32 offset, Int32 count)
    {
        var remain = _length - offset;
        if (count < 0 || count > remain) count = remain;
        if (offset == 0 && count == _length) return this;

        if (offset == 0)
            return new MemoryPacket(_memory, count);

        return new MemoryPacket(_memory[offset..], count);
    }
}

/// <summary>字节数组包</summary>
public struct ArrayPacket : IDisposable, IPacket
{
    #region 属性
    private Byte[] _buffer;
    /// <summary>缓冲区</summary>
    public Byte[] Buffer => _buffer;

    private readonly Int32 _offset;
    /// <summary>数据偏移</summary>
    public Int32 Offset => _offset;

    private readonly Int32 _length;
    /// <summary>数据长度</summary>
    public Int32 Length => _length;

    /// <summary>获取/设置 指定位置的字节</summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Byte this[Int32 index] { get => _buffer[_offset + index]; set => _buffer[_offset + index] = value; }

    /// <summary>是否拥有管理权。Dispose时，若有管理权则还给池里</summary>
    public Boolean HasOwner { get; set; }

    /// <summary>下一个链式包</summary>
    public IPacket? Next { get; set; }
    #endregion

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

    /// <summary>通过从池里借出字节数组来实例化数据包</summary>
    /// <param name="length"></param>
    public ArrayPacket(Int32 length)
    {
        if (length <= 1024 * 1024 * 1024)
        {
            _buffer = Pool.Shared.Rent(length);
            _length = length;
            HasOwner = true;
        }
        else
        {
            _buffer = new Byte[length];
            _length = length;
        }
    }

    /// <summary>释放</summary>
    public void Dispose()
    {
        var buf = _buffer;
        if (HasOwner && buf != null)
        {
            _buffer = null!;
            Pool.Shared.Return(buf);
            HasOwner = false;
        }
    }

    /// <summary>获取分片包。在管理权生命周期内短暂使用</summary>
    /// <returns></returns>
    public Span<Byte> GetSpan() => new(_buffer, _offset, _length);

    /// <summary>获取内存包。在管理权生命周期内短暂使用</summary>
    /// <returns></returns>
    public Memory<Byte> GetMemory() => new(_buffer, _offset, _length);

    /// <summary>切片得到新数据包，同时转移内存管理权，当前数据包应尽快停止使用</summary>
    /// <remarks>
    /// 可能是引用同一块内存，也可能是新的内存。
    /// 可能就是当前数据包，也可能引用相同的所有者或数组。
    /// </remarks>
    /// <param name="offset">偏移</param>
    /// <param name="count">个数。默认-1表示到末尾</param>
    public IPacket Slice(Int32 offset, Int32 count)
    {
        //var remain = _length - offset;
        //if (count < 0 || count > remain) count = remain;
        //if (offset == 0 && count == _length) return this;

        //var pk = new ArrayPacket(_buffer, _offset + offset, count) { HasOwner = HasOwner };
        //HasOwner = false;

        //return pk;

        IPacket? pk = null;
        var start = Offset + offset;
        var remain = _length - offset;

        if (Next == null)
        {
            // count 是 offset 之后的个数
            if (count < 0 || count > remain) count = remain;
            if (count < 0) count = 0;

            pk = new ArrayPacket(_buffer, start, count);
        }
        else
        {
            // 如果当前段用完，则取下一段
            if (remain <= 0)
                pk = Next.Slice(offset - _length, count);

            // 当前包用一截，剩下的全部
            else if (count < 0)
                pk = new ArrayPacket(_buffer, start, remain) { Next = Next };

            // 当前包可以读完
            else if (count <= remain)
                pk = new ArrayPacket(_buffer, start, count);

            // 当前包用一截，剩下的再截取
            else
                pk = new ArrayPacket(_buffer, start, remain) { Next = Next.Slice(0, count - remain) };
        }

        if (pk is ArrayPacket ap) ap.HasOwner = HasOwner;
        HasOwner = false;

        return pk;
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
    #endregion
}
