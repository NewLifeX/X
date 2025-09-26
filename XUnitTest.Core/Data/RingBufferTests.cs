using System;
using System.ComponentModel;
using System.Text;
using NewLife;
using NewLife.Data;
using Xunit;

namespace XUnitTest.Data;

public class RingBufferTests
{
    [Fact]
    [DisplayName("测试默认构造函数和指定容量构造函数")]
    public void Constructor_Test()
    {
        // 默认构造函数
        var rb = new RingBuffer();
        Assert.Equal(1024, rb.Capacity);
        Assert.Equal(0, rb.Length);
        Assert.Equal(0, rb.Head);
        Assert.Equal(0, rb.Tail);

        // 指定容量构造函数
        rb = new RingBuffer(512);
        Assert.Equal(512, rb.Capacity);
        Assert.Equal(0, rb.Length);
        Assert.Equal(0, rb.Head);
        Assert.Equal(0, rb.Tail);

        // 最小容量
        rb = new RingBuffer(1);
        Assert.Equal(1, rb.Capacity);
    }

    [Fact]
    [DisplayName("测试容量扩展")]
    public void EnsureCapacity()
    {
        var rb = new RingBuffer();
        Assert.Equal(1024, rb.Capacity);

        rb = new RingBuffer(333);
        Assert.Equal(333, rb.Capacity);

        rb.EnsureCapacity(2048);
        Assert.Equal(2048, rb.Capacity);
    }

    [Fact]
    [DisplayName("测试容量扩展时不缩小")]
    public void EnsureCapacity_DoesNotShrink()
    {
        var rb = new RingBuffer(1024);
        Assert.Equal(1024, rb.Capacity);

        // 尝试设置更小的容量，应该不改变
        rb.EnsureCapacity(512);
        Assert.Equal(1024, rb.Capacity);

        // 设置相同容量，应该不改变
        rb.EnsureCapacity(1024);
        Assert.Equal(1024, rb.Capacity);
    }

    [Fact]
    [DisplayName("测试空缓冲区的扩容")]
    public void EnsureCapacity_EmptyBuffer()
    {
        var rb = new RingBuffer(64);
        
        // 空缓冲区扩容
        rb.EnsureCapacity(128);
        Assert.Equal(128, rb.Capacity);
        Assert.Equal(0, rb.Length);
        Assert.Equal(0, rb.Head);
        Assert.Equal(0, rb.Tail);
    }

    [Fact]
    [DisplayName("测试有数据时的连续布局扩容")]
    public void EnsureCapacity_WithContinuousData()
    {
        var rb = new RingBuffer(8);
        var data = "Hello"u8.ToArray();
        
        rb.Write(data);
        Assert.Equal(5, rb.Length);
        Assert.Equal(5, rb.Head);
        Assert.Equal(0, rb.Tail);
        
        // 扩容
        rb.EnsureCapacity(16);
        Assert.Equal(16, rb.Capacity);
        Assert.Equal(5, rb.Length);
        Assert.Equal(5, rb.Head);
        Assert.Equal(0, rb.Tail);
        
        // 验证数据完整性
        var readBuffer = new Byte[5];
        var count = rb.Read(readBuffer);
        Assert.Equal(5, count);
        Assert.Equal("Hello"u8.ToArray(), readBuffer);
    }

    [Fact]
    [DisplayName("测试有数据时的分段布局扩容")]
    public void EnsureCapacity_WithWrappedData()
    {
        var rb = new RingBuffer(8);
        var data1 = "Hello"u8.ToArray(); // 5 bytes
        var data2 = "ABC"u8.ToArray();   // 3 bytes
        
        // 写入第一段数据
        rb.Write(data1);
        Assert.Equal(5, rb.Head);
        Assert.Equal(0, rb.Tail);
        
        // 读取部分数据，制造环形状态
        var tempBuffer = new Byte[3];
        rb.Read(tempBuffer);
        Assert.Equal(5, rb.Head);
        Assert.Equal(3, rb.Tail);
        Assert.Equal(2, rb.Length);
        
        // 写入更多数据，触发环形写入
        rb.Write(data2);
        Assert.Equal(0, rb.Head); // 回绕到开头
        Assert.Equal(3, rb.Tail);
        Assert.Equal(5, rb.Length);
        
        // 扩容，此时数据是分段的
        rb.EnsureCapacity(16);
        Assert.Equal(16, rb.Capacity);
        Assert.Equal(5, rb.Length);
        Assert.Equal(5, rb.Head);  // 重置为线性布局
        Assert.Equal(0, rb.Tail);  // 重置为线性布局
        
        // 验证数据完整性 - 应该是 "lo" + "ABC"
        var readBuffer = new Byte[5];
        var count = rb.Read(readBuffer);
        Assert.Equal(5, count);
        var expected = new Byte[5];
        "lo"u8.ToArray().CopyTo(expected, 0);
        "ABC"u8.ToArray().CopyTo(expected, 2);
        Assert.Equal(expected, readBuffer);
    }

    [Fact]
    [DisplayName("测试基本的写入和读取")]
    public void WriteReadRead()
    {
        var rb = new RingBuffer(128);

        Assert.Equal(128, rb.Capacity);
        Assert.Equal(0, rb.Length);
        Assert.Equal(0, rb.Head);
        Assert.Equal(0, rb.Tail);

        // 120个字符
        var sb = new StringBuilder();
        for (var i = 0; i < 10; i++)
        {
            sb.Append("HelloNewLife");
        }
        var buf = sb.ToString().GetBytes();

        // 写入数据
        rb.Write(buf);
        Assert.Equal(128, rb.Capacity);
        Assert.Equal(buf.Length, rb.Length);
        Assert.Equal(buf.Length, rb.Head);
        Assert.Equal(0, rb.Tail);

        // 读取
        var buf2 = new Byte[70];
        var count = rb.Read(buf2);
        Assert.Equal(buf2.Length, count);
        Assert.Equal(128, rb.Capacity);
        Assert.Equal(buf.Length - buf2.Length, rb.Length);
        Assert.Equal(buf.Length, rb.Head);
        Assert.Equal(buf2.Length, rb.Tail);
        Assert.Equal(sb.ToString()[..buf2.Length], buf2.ToStr());

        // 读取
        count = rb.Read(buf2);
        Assert.Equal(buf.Length - buf2.Length, count);
        Assert.Equal(128, rb.Capacity);
        Assert.Equal(0, rb.Length);
        Assert.Equal(buf.Length, rb.Head);
        Assert.Equal(buf.Length, rb.Tail);
    }

    [Fact]
    [DisplayName("测试连续写入触发扩容")]
    public void WriteWriteReadRead()
    {
        var rb = new RingBuffer(128);

        // 120个字符
        var sb = new StringBuilder();
        for (var i = 0; i < 10; i++)
        {
            sb.Append("HelloNewLife");
        }
        var buf = sb.ToString().GetBytes();

        // 写入数据
        rb.Write(buf);
        Assert.Equal(128, rb.Capacity);
        Assert.Equal(buf.Length, rb.Length);
        Assert.Equal(buf.Length, rb.Head);
        Assert.Equal(0, rb.Tail);

        // 再写，扩容
        rb.Write(buf);
        Assert.Equal(256, rb.Capacity);
        Assert.Equal(buf.Length * 2, rb.Length);
        Assert.Equal(buf.Length * 2, rb.Head);
        Assert.Equal(0, rb.Tail);

        // 读取
        var buf2 = new Byte[70];
        var count = rb.Read(buf2);
        Assert.Equal(buf2.Length, count);
        Assert.Equal(buf.Length * 2 - buf2.Length, rb.Length);
        Assert.Equal(buf2.Length, rb.Tail);
        Assert.Equal(sb.ToString()[..buf2.Length], buf2.ToStr());

        // 读取
        count = rb.Read(buf2);
        Assert.Equal(buf2.Length, count);
        Assert.Equal(buf.Length * 2 - buf2.Length * 2, rb.Length);
        Assert.Equal(buf2.Length * 2, rb.Tail);
    }

    [Fact]
    [DisplayName("测试读取后写入的环形处理")]
    public void WriteRead3()
    {
        var rb = new RingBuffer(128);

        Assert.Equal(128, rb.Capacity);
        Assert.Equal(0, rb.Length);
        Assert.Equal(0, rb.Head);
        Assert.Equal(0, rb.Tail);

        // 120个字符
        var sb = new StringBuilder();
        for (var i = 0; i < 10; i++)
        {
            sb.Append("HelloNewLife");
        }
        var buf = sb.ToString().GetBytes();

        // 写入数据
        rb.Write(buf);
        Assert.Equal(128, rb.Capacity);
        Assert.Equal(buf.Length, rb.Length);
        Assert.Equal(buf.Length, rb.Head);
        Assert.Equal(0, rb.Tail);

        // 读取
        var buf2 = new Byte[115];
        var count = rb.Read(buf2);
        Assert.Equal(buf2.Length, count);
        Assert.Equal(buf.Length - buf2.Length, rb.Length);
        Assert.Equal(buf2.Length, rb.Tail);
        Assert.Equal(sb.ToString()[..buf2.Length], buf2.ToStr());

        // 再写，扩容
        rb.Write(buf);
        Assert.Equal(128, rb.Capacity);
        Assert.Equal(buf.Length * 2 - buf2.Length, rb.Length);
        Assert.Equal(buf.Length * 2 - rb.Capacity, rb.Head);
        Assert.Equal(buf2.Length, rb.Tail);
    }

    [Fact]
    [DisplayName("测试跨边界写入")]
    public void Write_CrossBoundary()
    {
        var rb = new RingBuffer(10);
        
        // 写入8字节数据
        var data1 = "12345678"u8.ToArray();
        rb.Write(data1);
        Assert.Equal(8, rb.Head);
        Assert.Equal(8, rb.Length);
        
        // 读取5字节，为后续跨边界写入腾出空间
        var readBuffer = new Byte[5];
        rb.Read(readBuffer);
        Assert.Equal(8, rb.Head);
        Assert.Equal(5, rb.Tail);
        Assert.Equal(3, rb.Length);
        
        // 写入5字节数据，应该跨边界：2字节到末尾，3字节到开头
        var data2 = "ABCDE"u8.ToArray();
        rb.Write(data2);
        Assert.Equal(3, rb.Head); // 回绕到开头位置3
        Assert.Equal(5, rb.Tail);
        Assert.Equal(8, rb.Length);
        
        // 验证数据完整性
        var allData = new Byte[8];
        var count = rb.Read(allData);
        Assert.Equal(8, count);
        Assert.Equal("678ABCDE"u8.ToArray(), allData);
    }

    [Fact]
    [DisplayName("测试跨边界读取")]
    public void Read_CrossBoundary()
    {
        var rb = new RingBuffer(8);
        
        // 先写入一些数据并读取一部分，制造环形状态
        var data1 = "123456"u8.ToArray();
        rb.Write(data1);
        
        var tempBuffer = new Byte[3];
        rb.Read(tempBuffer);
        Assert.Equal("123", tempBuffer.ToStr());
        
        // 再写入数据，触发跨边界
        var data2 = "ABCDE"u8.ToArray();
        rb.Write(data2);
        
        // 此时数据布局：[CDE][456AB]，Tail=3, Head=3
        Assert.Equal(3, rb.Head);
        Assert.Equal(3, rb.Tail);
        Assert.Equal(8, rb.Length);
        
        // 跨边界读取
        var readBuffer = new Byte[8];
        var count = rb.Read(readBuffer);
        Assert.Equal(8, count);
        Assert.Equal("456ABCDE"u8.ToArray(), readBuffer);
    }

    [Fact]
    [DisplayName("测试空缓冲区读取")]
    public void Read_EmptyBuffer()
    {
        var rb = new RingBuffer(10);
        
        var buffer = new Byte[5];
        var count = rb.Read(buffer);
        
        Assert.Equal(0, count);
        Assert.Equal(0, rb.Length);
        Assert.Equal(0, rb.Head);
        Assert.Equal(0, rb.Tail);
    }

    [Fact]
    [DisplayName("测试零长度写入")]
    public void Write_ZeroLength()
    {
        var rb = new RingBuffer(10);
        var data = "Hello"u8.ToArray();
        
        // 零长度写入
        rb.Write(data, 0, 0);
        Assert.Equal(0, rb.Length);
        Assert.Equal(0, rb.Head);
        Assert.Equal(0, rb.Tail);
        
        // 空数组写入
        rb.Write(Array.Empty<Byte>());
        Assert.Equal(0, rb.Length);
    }

    [Fact]
    [DisplayName("测试零长度读取")]
    public void Read_ZeroLength()
    {
        var rb = new RingBuffer(10);
        var data = "Hello"u8.ToArray();
        rb.Write(data);
        
        var buffer = new Byte[10];
        var count = rb.Read(buffer, 0, 0);
        
        Assert.Equal(0, count);
        Assert.Equal(5, rb.Length); // 数据不应该被消耗
    }

    [Fact]
    [DisplayName("测试写入参数校验")]
    public void Write_ParameterValidation()
    {
        var rb = new RingBuffer(10);
        
        // null数组
        Assert.Throws<ArgumentNullException>(() => rb.Write(null));
        
        var data = "Hello"u8.ToArray();
        
        // 负偏移量
        Assert.Throws<ArgumentOutOfRangeException>(() => rb.Write(data, -1));
        
        // 偏移量越界
        Assert.Throws<ArgumentOutOfRangeException>(() => rb.Write(data, data.Length));
        
        // count越界
        Assert.Throws<ArgumentOutOfRangeException>(() => rb.Write(data, 0, data.Length + 1));
        
        // offset + count 越界
        Assert.Throws<ArgumentOutOfRangeException>(() => rb.Write(data, 3, 4));
    }

    [Fact]
    [DisplayName("测试读取参数校验")]
    public void Read_ParameterValidation()
    {
        var rb = new RingBuffer(10);
        
        // null数组
        Assert.Throws<ArgumentNullException>(() => rb.Read(null));
        
        var buffer = new Byte[5];
        
        // 负偏移量
        Assert.Throws<ArgumentOutOfRangeException>(() => rb.Read(buffer, -1));
        
        // 偏移量越界
        Assert.Throws<ArgumentOutOfRangeException>(() => rb.Read(buffer, buffer.Length));
        
        // count越界
        Assert.Throws<ArgumentOutOfRangeException>(() => rb.Read(buffer, 0, buffer.Length + 1));
        
        // offset + count 越界
        Assert.Throws<ArgumentOutOfRangeException>(() => rb.Read(buffer, 3, 4));
    }

    [Fact]
    [DisplayName("测试带偏移量的写入")]
    public void Write_WithOffset()
    {
        var rb = new RingBuffer(10);
        var data = "HelloWorld"u8.ToArray();
        
        // 从偏移量5开始写入5个字节
        rb.Write(data, 5, 5);
        Assert.Equal(5, rb.Length);
        
        var buffer = new Byte[5];
        var count = rb.Read(buffer);
        Assert.Equal(5, count);
        Assert.Equal("World"u8.ToArray(), buffer);
    }

    [Fact]
    [DisplayName("测试带偏移量的读取")]
    public void Read_WithOffset()
    {
        var rb = new RingBuffer(10);
        var data = "Hello"u8.ToArray();
        rb.Write(data);
        
        var buffer = new Byte[10];
        var count = rb.Read(buffer, 3, 5);
        
        Assert.Equal(5, count);
        Assert.Equal(0, rb.Length);
        
        // 验证数据写入到正确位置
        var expected = new Byte[10];
        "Hello"u8.ToArray().CopyTo(expected, 3);
        Assert.Equal(expected, buffer);
    }

    [Fact]
    [DisplayName("测试读取部分数据")]
    public void Read_PartialData()
    {
        var rb = new RingBuffer(10);
        var data = "HelloWorld"u8.ToArray();
        rb.Write(data);
        
        // 只读取一部分数据
        var buffer = new Byte[5];
        var count = rb.Read(buffer);
        
        Assert.Equal(5, count);
        Assert.Equal(5, rb.Length); // 还剩5字节
        Assert.Equal("Hello"u8.ToArray(), buffer);
        
        // 读取剩余数据
        count = rb.Read(buffer);
        Assert.Equal(5, count);
        Assert.Equal(0, rb.Length);
        Assert.Equal("World"u8.ToArray(), buffer);
    }

    [Fact]
    [DisplayName("测试自动扩容策略")]
    public void Write_AutoExpansion()
    {
        var rb = new RingBuffer(4);
        
        // 写入超过初始容量的数据，测试两倍扩容
        var data = "Hello"u8.ToArray(); // 5 bytes
        rb.Write(data);
        
        Assert.Equal(8, rb.Capacity); // 4 -> 8
        Assert.Equal(5, rb.Length);
        
        // 再写入更多数据
        var data2 = "World!!!"u8.ToArray(); // 8 bytes
        rb.Write(data2);
        
        Assert.Equal(16, rb.Capacity); // 8 -> 16 (因为需要13字节空间)
        Assert.Equal(13, rb.Length);
        
        // 验证数据完整性
        var buffer = new Byte[13];
        var count = rb.Read(buffer);
        Assert.Equal(13, count);
        Assert.Equal("HelloWorld!!!"u8.ToArray(), buffer);
    }

    [Fact]
    [DisplayName("测试复杂的环形操作序列")]
    public void ComplexRingOperations()
    {
        var rb = new RingBuffer(8);
        
        // 写入 "ABCDEF" (6字节)
        rb.Write("ABCDEF"u8.ToArray());
        Assert.Equal(6, rb.Length);
        
        // 读取 "ABC" (3字节)
        var buffer = new Byte[3];
        rb.Read(buffer);
        Assert.Equal("ABC", buffer.ToStr());
        Assert.Equal(3, rb.Length);
        Assert.Equal(6, rb.Head);
        Assert.Equal(3, rb.Tail);
        
        // 写入 "12345" (5字节)，会触发跨边界：2字节到末尾，3字节到开头
        rb.Write("12345"u8.ToArray());
        Assert.Equal(8, rb.Length);
        Assert.Equal(3, rb.Head); // 回绕到位置3
        Assert.Equal(3, rb.Tail);
        
        // 读取所有数据 "DEF12" + "345"
        var allBuffer = new Byte[8];
        var count = rb.Read(allBuffer);
        Assert.Equal(8, count);
        Assert.Equal("DEF12345"u8.ToArray(), allBuffer);
        Assert.Equal(0, rb.Length);
    }

    [Fact]
    [DisplayName("测试大数据量操作")]
    public void LargeDataOperations()
    {
        var rb = new RingBuffer(16);
        
        // 准备大数据
        var largeData = new Byte[1024];
        for (var i = 0; i < largeData.Length; i++)
        {
            largeData[i] = (Byte)(i % 256);
        }
        
        // 写入大数据，会触发多次扩容
        rb.Write(largeData);
        Assert.True(rb.Capacity >= 1024);
        Assert.Equal(1024, rb.Length);
        
        // 分批读取
        var readData = new Byte[1024];
        var totalRead = 0;
        var batchSize = 100;
        
        while (totalRead < 1024)
        {
            var toRead = Math.Min(batchSize, 1024 - totalRead);
            var count = rb.Read(readData, totalRead, toRead);
            totalRead += count;
            if (count == 0) break; // 防止无限循环
        }
        
        Assert.Equal(1024, totalRead);
        Assert.Equal(0, rb.Length);
        Assert.Equal(largeData, readData);
    }

    [Fact]
    [DisplayName("测试边界条件：满缓冲区")]
    public void FullBufferScenario()
    {
        var rb = new RingBuffer(5);
        
        // 写满缓冲区
        rb.Write("12345"u8.ToArray());
        Assert.Equal(5, rb.Length);
        Assert.Equal(5, rb.Capacity);
        
        // 再写入数据，应该触发扩容
        rb.Write("A"u8.ToArray());
        Assert.Equal(6, rb.Length);
        Assert.Equal(10, rb.Capacity); // 扩容到10
        
        // 验证数据完整性
        var buffer = new Byte[6];
        var count = rb.Read(buffer);
        Assert.Equal(6, count);
        Assert.Equal("12345A"u8.ToArray(), buffer);
    }
}
