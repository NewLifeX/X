using System.Buffers;
using System.Text;

namespace NewLife.Data;

/// <summary>数据包接口。统一提供数据包，内部可能是内存池、数组和旧版Packet等多种实现</summary>
/// <remarks>
/// 常用于网络编程和协议解析，为了避免大量内存分配和拷贝，采用数据包对象池，复用内存。
/// 数据包接口一般由结构体实现，提升GC性能。
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

/// <summary>内存包。具有所有权管理，不使用时释放</summary>
public struct MemoryPacket : IDisposable, IPacket
{
    #region 属性
    private IMemoryOwner<Byte> _memoryOwner;
    /// <summary>内存所有者</summary>
    public IMemoryOwner<Byte> MemoryOwner => _memoryOwner;

    private readonly Int32 _length;
    /// <summary>数据长度</summary>
    public Int32 Length => _length;
    #endregion

    /// <summary>实例化指定长度的内存包，从共享内存池中借出</summary>
    /// <param name="length">长度</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public MemoryPacket(Int32 length)
    {
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be non-negative and less than or equal to the memory owner's length.");

        _memoryOwner = MemoryPool<Byte>.Shared.Rent(length);
        _length = length;
    }

    /// <summary>实例化内存包，指定内存所有者和长度</summary>
    /// <param name="memoryOwner">内存所有者</param>
    /// <param name="length">长度</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public MemoryPacket(IMemoryOwner<Byte> memoryOwner, Int32 length)
    {
        if (length < 0 || length > memoryOwner.Memory.Length)
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be non-negative and less than or equal to the memory owner's length.");

        _memoryOwner = memoryOwner;
        _length = length;
    }

    /// <summary>获取分片包</summary>
    /// <returns></returns>
    public Span<Byte> GetSpan() => _memoryOwner.Memory.Span[.._length];

    /// <summary>获取内存包</summary>
    /// <returns></returns>
    public Memory<Byte> GetMemory() => _memoryOwner.Memory[.._length];

    /// <summary>释放</summary>
    public void Dispose()
    {
        // 释放内存所有者以后，直接置空，避免重复使用
        _memoryOwner?.Dispose();
        _memoryOwner = null!;
    }
}

/// <summary>字节数组包</summary>
public struct ArrayPacket : IPacket
{
    #region 属性
    private readonly Memory<Byte> _memory;

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

        _memory = new Memory<Byte>(buf, offset, count);
        _length = count;
    }

    /// <summary>获取分片包</summary>
    /// <returns></returns>
    public Span<Byte> GetSpan() => _memory.Span;

    /// <summary>获取内存包</summary>
    /// <returns></returns>
    public Memory<Byte> GetMemory() => _memory;
}
