using NewLife.Serialization;
using Xunit;

namespace XUnitTest.Serialization;

/// <summary>二进制读取遇到残缺数据必须报失败</summary>
public class BinaryShortReadTests
{
    [Theory(DisplayName = "数据不足时读取定长值返回失败")]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ShortDataReturnsFalse(Int32 length)
    {
        var data = new Byte[length];
        var bn = new Binary(new MemoryStream(data));
        var handler = new BinaryGeneral { Host = bn };

        Assert.False(handler.TryReadInt32(out _));
    }

    [Fact(DisplayName = "数据充足时读取定长值正常返回")]
    public void FullDataReturnsTrue()
    {
        var data = new Byte[] { 0x78, 0x56, 0x34, 0x12 };
        var bn = new Binary(new MemoryStream(data));
        var handler = new BinaryGeneral { Host = bn };

        Assert.True(handler.TryReadInt32(out var value));
        Assert.NotEqual(0, value);
    }

    [Fact(DisplayName = "Guid 数据不足时返回失败")]
    public void GuidShortDataReturnsFalse()
    {
        var bn = new Binary(new MemoryStream(new Byte[15]));
        var handler = new BinaryNormal { Host = bn };

        Object? value = null;
        Assert.False(handler.TryRead(typeof(Guid), ref value));
    }

    [Fact(DisplayName = "Guid 数据充足时正常返回")]
    public void GuidFullDataReturnsTrue()
    {
        var data = Guid.NewGuid().ToByteArray();
        var bn = new Binary(new MemoryStream(data));
        var handler = new BinaryNormal { Host = bn };

        Object? value = null;
        Assert.True(handler.TryRead(typeof(Guid), ref value));
        Assert.Equal(new Guid(data), value);
    }
}
