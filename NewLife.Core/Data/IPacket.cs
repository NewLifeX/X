using System.Buffers;
using System.Text;

namespace NewLife.Data;

/// <summary>数据包接口。统一提供数据包，内部可能是内存池、数组和旧版Packet等多种实现</summary>
/// <remarks>
/// 常用于网络编程和协议解析，为了避免大量内存分配和拷贝，采用数据包对象池，复用内存。
/// 数据包接口一般由结构体实现，提升GC性能。
/// 特别需要注意内存管理权转移问题，外部持有管理权的主要逻辑，有责任在使用完成时释放内存。
/// 
/// 作为过渡期，旧版Packet也会实现该接口，以便逐步替换。
/// </remarks>
public interface IPacket
{
    /// <summary>数据长度</summary>
    Int32 Length { get; }

    /// <summary>获取分片包</summary>
    /// <returns></returns>
    Span<Byte> GetSpan();

    /// <summary>获取内存包</summary>
    /// <returns></returns>
    Memory<Byte> GetMemory();

    /// <summary>切片得到新数据包</summary>
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
    /// <summary>转字符串并释放</summary>
    /// <param name="pk"></param>
    /// <param name="encoding"></param>
    /// <returns></returns>
    public static String ToStr(this IPacket pk, Encoding? encoding = null)
    {
        var rs = pk.GetSpan().ToStr(encoding);
        pk.TryDispose();
        return rs;
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
        // 释放内存所有者以后，直接置空，避免重复使用
        _owner?.Dispose();
        _owner = null!;
    }

    /// <summary>获取分片包</summary>
    /// <returns></returns>
    public Span<Byte> GetSpan() => _owner.Memory.Span[.._length];

    /// <summary>获取内存包</summary>
    /// <returns></returns>
    public Memory<Byte> GetMemory() => _owner.Memory[.._length];

    /// <summary>切片得到新数据包</summary>
    /// <remarks>
    /// 可能是引用同一块内存，也可能是新的内存。
    /// 可能就是当前数据包，也可能引用相同的所有者或数组。
    /// </remarks>
    /// <param name="offset">偏移</param>
    /// <param name="count">个数。默认-1表示到末尾</param>
    public IPacket Slice(Int32 offset, Int32 count)
    {
        if (count < 0) count = _length - offset;
        if (offset == 0 && count == _length) return this;

        if (offset == 0)
            return new OwnerPacket(_owner, count);

        return new MemoryPacket(_owner.Memory.Slice(offset, count), count);
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

    /// <summary>获取分片包</summary>
    /// <returns></returns>
    public Span<Byte> GetSpan() => _memory.Span[.._length];

    /// <summary>获取内存包</summary>
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
        if (count < 0) count = _length - offset;
        if (offset == 0 && count == _length) return this;

        if (offset == 0)
            return new MemoryPacket(_memory, count);

        return new MemoryPacket(_memory[offset..], count);
    }
}

/// <summary>字节数组包</summary>
public struct ArrayPacket : IPacket
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
    #endregion

    /// <summary>实例化</summary>
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

    /// <summary>获取分片包</summary>
    /// <returns></returns>
    public Span<Byte> GetSpan() => new(_buffer, _offset, _length);

    /// <summary>获取内存包</summary>
    /// <returns></returns>
    public Memory<Byte> GetMemory() => new(_buffer, _offset, _length);

    /// <summary>切片得到新数据包</summary>
    /// <remarks>
    /// 可能是引用同一块内存，也可能是新的内存。
    /// 可能就是当前数据包，也可能引用相同的所有者或数组。
    /// </remarks>
    /// <param name="offset">偏移</param>
    /// <param name="count">个数。默认-1表示到末尾</param>
    public IPacket Slice(Int32 offset, Int32 count)
    {
        if (count < 0) count = _length - offset;
        if (offset == 0 && count == _length) return this;

        return new ArrayPacket(_buffer, _offset + offset, count);
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
