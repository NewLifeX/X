using System.ComponentModel;
using NewLife.Buffers;
using NewLife.Collections;
using Xunit;

namespace XUnitTest.Buffers;

[DisplayName("数组池缓冲句柄")]
public class PoolBufferTests
{
    [Fact]
    [DisplayName("借出_Length与Span为请求长度且可读写")]
    public void Rent_Basic()
    {
        using var buffer = Pool.Rent(100);

        Assert.Equal(100, buffer.Length);
        Assert.Equal(100, buffer.Span.Length);
        Assert.True(buffer.Buffer.Length >= 100);

        buffer.Span[0] = 0x12;
        buffer.Buffer[1] = 0x34;

        Assert.Equal(0x12, buffer.Buffer[0]);
        Assert.Equal(0x34, buffer.Buffer[1]);
    }

    [Fact]
    [DisplayName("长度语义_Span只覆盖请求长度_Buffer可能更长")]
    public void LengthSemantics()
    {
        using var buffer = Pool.Rent(3);

        Assert.Equal(3, buffer.Length);
        Assert.Equal(3, buffer.Span.Length);
        Assert.True(buffer.Buffer.Length >= 3);

        // 写满请求部分不越界
        buffer.Span.Fill(0x5A);
        Assert.Equal(0x5A, buffer.Buffer[2]);
    }

    [Fact]
    [DisplayName("索引器_可直接读写元素")]
    public void Indexer()
    {
        using var buffer = Pool.Rent(4);

        buffer[0] = 0x11;
        buffer[1] = 0x22;
        buffer[1] |= 0x33;      // 复合赋值走 ref 索引器

        Assert.Equal(0x11, buffer.Buffer[0]);
        Assert.Equal(0x33, buffer.Buffer[1]);
    }

    [Fact]
    [DisplayName("隐式转换_与Buffer指向同一数组")]
    public void ImplicitConvert()
    {
        using var buffer = Pool.Rent(16);

        Byte[] array = buffer;
        Assert.Same(buffer.Buffer, array);

        // 隐式转换后可直接当数组实参传入
        Assert.True(ArrayLength(buffer) >= 16);
    }

    private static Int32 ArrayLength(Byte[] value) => value.Length;

    [Fact]
    [DisplayName("借出与归还_不产生堆分配")]
    public void Rent_Dispose_NoAllocation()
    {
        // 预热：首次借出由池新建数组
        using (var warm = Pool.Rent(1024))
        {
            warm.Buffer[0] = 1;
        }

        var memory = GC.GetAllocatedBytesForCurrentThread();

        Int32 length;
        using (var buffer = Pool.Rent(1024))
        {
            length = buffer.Length;
            buffer.Span[0] = 0x5A;
        }

        var used = GC.GetAllocatedBytesForCurrentThread() - memory;

        Assert.Equal(0, used);
        Assert.Equal(1024, length);
    }

    [Fact]
    [DisplayName("归还_数组回到池中并可被再次借出")]
    public void Dispose_ReturnToPool()
    {
        Byte[] first;
        using (var buffer = Pool.Rent(64))
        {
            first = buffer.Buffer;
        }

        // 同线程内归还的数组会被池优先复用
        using var again = Pool.Rent(64);
        Assert.Same(first, again.Buffer);
    }

    [Fact]
    [DisplayName("默认实例_空实现可安全释放")]
    public void DefaultInstance()
    {
        using var empty = default(PoolBuffer<Byte>);

        Assert.Equal(0, empty.Length);
        Assert.True(empty.Span.IsEmpty);
        Assert.Null(empty.Buffer);
    }

    [Fact]
    [DisplayName("字符缓冲_借出后按下标写入并取用")]
    public void RentChars()
    {
        const Int32 size = 600;
        using var chars = Pool.Rent<Char>(size);

        Assert.Equal(size, chars.Length);
        Assert.Equal(size, chars.Span.Length);

        for (var i = 0; i < size; i++)
            chars[i] = (Char)('A' + i % 26);

        Assert.Equal('A', chars[0]);
        Assert.Equal('B', chars[1]);
        Assert.Equal("ABC", new String(chars.Span[..3]));
    }
}
