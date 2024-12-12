using NewLife.Security;
using Xunit;

namespace XUnitTest.Security;

public class Crc32Tests
{
    [Fact]
    public void Compute_ByteArray_ReturnsExpectedCrc()
    {
        Byte[] data = { 1, 2, 3, 4, 5 };
        var expectedCrc = 0x470B99F4u;

        var actualCrc = Crc32.Compute(data);

        Assert.Equal(expectedCrc, actualCrc);
    }

    [Fact]
    public void Compute_ReadOnlySpan_ReturnsExpectedCrc()
    {
        Byte[] data = { 1, 2, 3, 4, 5 };
        var expectedCrc = 0x470B99F4u;

        var actualCrc = Crc32.Compute(new ReadOnlySpan<Byte>(data));

        Assert.Equal(expectedCrc, actualCrc);
    }

    [Fact]
    public void Compute_Stream_ReturnsExpectedCrc()
    {
        Byte[] data = { 1, 2, 3, 4, 5 };
        var expectedCrc = 0x470B99F4u;

        using var stream = new MemoryStream(data);
        var actualCrc = Crc32.Compute(stream);

        Assert.Equal(expectedCrc, actualCrc);
    }

    [Fact]
    public void ComputeRange_Stream_ReturnsExpectedCrc()
    {
        Byte[] data = { 1, 2, 3, 4, 5 };
        var expectedCrc = 0x470B99F4u;

        using var stream = new MemoryStream(data);
        var actualCrc = Crc32.ComputeRange(stream, 0, data.Length);

        Assert.Equal(expectedCrc, actualCrc);
    }

    [Fact]
    public void Update_ByteArray_ReturnsExpectedCrc()
    {
        Byte[] data = { 1, 2, 3, 4, 5 };
        var expectedCrc = 0x470B99F4u;

        var crc32 = new Crc32();
        crc32.Update(data);

        Assert.Equal(expectedCrc, crc32.Value);
    }

    [Fact]
    public void Update_ReadOnlySpan_ReturnsExpectedCrc()
    {
        Byte[] data = { 1, 2, 3, 4, 5 };
        var expectedCrc = 0x470B99F4u;

        var crc32 = new Crc32();
        crc32.Update(new ReadOnlySpan<Byte>(data));

        Assert.Equal(expectedCrc, crc32.Value);
    }

    [Fact]
    public void Update_Stream_ReturnsExpectedCrc()
    {
        Byte[] data = { 1, 2, 3, 4, 5 };
        var expectedCrc = 0x470B99F4u;

        using var stream = new MemoryStream(data);
        var crc32 = new Crc32();
        crc32.Update(stream);

        Assert.Equal(expectedCrc, crc32.Value);
    }
}
