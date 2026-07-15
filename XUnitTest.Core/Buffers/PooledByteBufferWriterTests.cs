using System.ComponentModel;
using System.Text;
using NewLife.Buffers;
using Xunit;

namespace XUnitTest.Buffers;

public class PooledByteBufferWriterTests
{
    [Fact]
    [DisplayName("构造_初始容量_创建成功")]
    public void Ctor_InitialCapacity_CreatesInstance()
    {
        using var writer = new PooledByteBufferWriter(256);
        Assert.NotNull(writer);
        Assert.Equal(0, writer.WrittenCount);
        Assert.True(writer.Capacity >= 256);
        Assert.Equal(writer.Capacity, writer.FreeCapacity);
    }

    [Fact]
    [DisplayName("构造_初始容量为0_抛出异常")]
    public void Ctor_ZeroCapacity_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PooledByteBufferWriter(0));
    }

    [Fact]
    [DisplayName("构造_初始容量为负_抛出异常")]
    public void Ctor_NegativeCapacity_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PooledByteBufferWriter(-1));
    }

    [Fact]
    [DisplayName("GetSpan_写入数据_通过Advance推进")]
    public void GetSpan_WriteData_AdvanceUpdatesPosition()
    {
        using var writer = new PooledByteBufferWriter(64);
        var span = writer.GetSpan(10);
        span[0] = 0x01;
        span[1] = 0x02;
        span[2] = 0x03;
        writer.Advance(3);

        Assert.Equal(3, writer.WrittenCount);
        Assert.Equal(writer.Capacity - 3, writer.FreeCapacity);

        // 验证已写入数据
        var written = writer.WrittenSpan;
        Assert.Equal(3, written.Length);
        Assert.Equal(0x01, written[0]);
        Assert.Equal(0x02, written[1]);
        Assert.Equal(0x03, written[2]);
    }

    [Fact]
    [DisplayName("GetMemory_写入数据_通过Advance推进")]
    public void GetMemory_WriteData_AdvanceUpdatesPosition()
    {
        using var writer = new PooledByteBufferWriter(64);
        var mem = writer.GetMemory(10);
        mem.Span[0] = 0x0A;
        mem.Span[1] = 0x0B;
        writer.Advance(2);

        Assert.Equal(2, writer.WrittenCount);
        var written = writer.WrittenMemory;
        Assert.Equal(2, written.Length);
        Assert.Equal(0x0A, written.Span[0]);
        Assert.Equal(0x0B, written.Span[1]);
    }

    [Fact]
    [DisplayName("Advance_超出容量_抛出异常")]
    public void Advance_ExceedCapacity_ThrowsException()
    {
        using var writer = new PooledByteBufferWriter(64);
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.Advance(65));
    }

    [Fact]
    [DisplayName("写入数据_触发自动扩容")]
    public void WriteData_TriggersAutoResize()
    {
        using var writer = new PooledByteBufferWriter(16);
        // 写入超过初始容量的数据，触发扩容
        var data = new Byte[128];
        for (var i = 0; i < data.Length; i++) data[i] = (Byte)(i & 0xFF);

        var span = writer.GetSpan(data.Length);
        data.CopyTo(span);
        writer.Advance(data.Length);

        Assert.Equal(data.Length, writer.WrittenCount);
        Assert.True(writer.Capacity >= data.Length);

        // 验证写入内容
        var written = writer.WrittenSpan;
        for (var i = 0; i < data.Length; i++)
            Assert.Equal(data[i], written[i]);
    }

    [Fact]
    [DisplayName("Clear_清空后重新写入")]
    public void Clear_ThenWriteAgain()
    {
        using var writer = new PooledByteBufferWriter(64);
        // 先写入
        var span = writer.GetSpan(10);
        span[0] = 0xFF;
        writer.Advance(1);
        Assert.Equal(1, writer.WrittenCount);

        // 清空
        writer.Clear();
        Assert.Equal(0, writer.WrittenCount);

        // 重新写入
        span = writer.GetSpan(5);
        span[0] = 0xAA;
        writer.Advance(1);
        Assert.Equal(1, writer.WrittenCount);
        Assert.Equal(0xAA, writer.WrittenSpan[0]);
    }

    [Fact]
    [DisplayName("WriteToStream_写入到流")]
    public void WriteToStream_WritesToStream()
    {
        using var writer = new PooledByteBufferWriter(64);
        var data = "Hello, World!"u8.ToArray();
        var span = writer.GetSpan(data.Length);
        data.CopyTo(span);
        writer.Advance(data.Length);

        using var ms = new MemoryStream();
        writer.WriteToStream(ms);

        Assert.Equal(data.Length, ms.Length);
        var result = ms.ToArray();
        Assert.Equal(data, result);
    }

    [Fact]
    [DisplayName("WriteToStreamAsync_异步写入到流")]
    public async Task WriteToStreamAsync_WritesToStream()
    {
        using var writer = new PooledByteBufferWriter(64);
        var data = "Async Test!"u8.ToArray();
        var span = writer.GetSpan(data.Length);
        data.CopyTo(span);
        writer.Advance(data.Length);

        using var ms = new MemoryStream();
        await writer.WriteToStreamAsync(ms, CancellationToken.None);

        Assert.Equal(data.Length, ms.Length);
        var result = ms.ToArray();
        Assert.Equal(data, result);
    }

    [Fact]
    [DisplayName("Dispose_释放后实例标记失效")]
    public void Dispose_MarksInstanceInvalid()
    {
        var writer = new PooledByteBufferWriter(64);
        writer.Dispose();

        // 再次释放不应抛异常
        writer.Dispose();
    }

    [Fact]
    [DisplayName("ClearAndReturnBuffers_归还数组后重置")]
    public void ClearAndReturnBuffers_ReturnsBuffers()
    {
        var writer = new PooledByteBufferWriter(64);
        // 写入一些数据
        var span = writer.GetSpan(10);
        span[0] = 0x42;
        writer.Advance(1);

        writer.ClearAndReturnBuffers();
        // 调用后 WrittenCount 应为0（实际内部已标记失效）
        // Dispose 再次调用不应抛异常
        writer.Dispose();
    }

    [Fact]
    [DisplayName("InitializeEmptyInstance_重新初始化")]
    public void InitializeEmptyInstance_Reinitializes()
    {
        using var writer = new PooledByteBufferWriter(64);
        // 先写入数据
        var span = writer.GetSpan(10);
        span[0] = 0x42;
        writer.Advance(1);

        // 重新初始化
        writer.InitializeEmptyInstance(128);
        Assert.Equal(0, writer.WrittenCount);
        Assert.True(writer.Capacity >= 128);
    }
}
