using NewLife.Security;
using Xunit;

namespace XUnitTest.Security;

public class Crc16Tests
{
    [Fact]
    public void TestComputeWithByteArray()
    {
        var data = "123456789"u8.ToArray();
        var crc = Crc16.Compute(data);
        Assert.Equal(0x31C3, crc);
    }

    [Fact]
    public void TestComputeWithStream()
    {
        var data = "123456789"u8.ToArray();
        using var stream = new MemoryStream(data);
        var crc = Crc16.Compute(stream, -1);
        Assert.Equal(0x31C3, crc);
    }

    [Fact]
    public void TestComputeWithReadOnlySpan()
    {
        var data = "123456789"u8.ToArray();
        var crc = Crc16.Compute(new ReadOnlySpan<Byte>(data));
        Assert.Equal(0x31C3, crc);
    }

    [Fact]
    public void TestComputeModbusWithByteArray()
    {
        var data = "123456789"u8.ToArray();
        var crc = Crc16.ComputeModbus(data, 0);
        Assert.Equal(0x4B37, crc);
    }

    [Fact]
    public void TestComputeModbusWithStream()
    {
        var data = "123456789"u8.ToArray();
        using var stream = new MemoryStream(data);
        var crc = Crc16.ComputeModbus(stream);
        Assert.Equal(0x4B37, crc);
    }
}
