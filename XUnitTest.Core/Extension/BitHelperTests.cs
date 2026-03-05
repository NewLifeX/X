using NewLife;
using Xunit;

namespace XUnitTest.Extension;

public class BitHelperTests
{
    #region UInt16 SetBit/GetBit
    [Theory(DisplayName = "UInt16设置单个位")]
    [InlineData(0, 0, true, 1)]
    [InlineData(0, 1, true, 2)]
    [InlineData(0, 15, true, 32768)]
    [InlineData(0xFFFF, 0, false, 0xFFFE)]
    public void UInt16_SetBit(UInt16 value, Int32 position, Boolean flag, UInt16 expected)
    {
        var result = value.SetBit(position, flag);
        Assert.Equal(expected, result);
    }

    [Theory(DisplayName = "UInt16获取单个位")]
    [InlineData(1, 0, true)]
    [InlineData(2, 1, true)]
    [InlineData(0, 0, false)]
    [InlineData(0xFF, 7, true)]
    public void UInt16_GetBit(UInt16 value, Int32 position, Boolean expected)
    {
        Assert.Equal(expected, value.GetBit(position));
    }
    #endregion

    #region UInt16 SetBits/GetBits
    [Fact(DisplayName = "UInt16设置多位")]
    public void UInt16_SetBits()
    {
        UInt16 value = 0;
        value = value.SetBits(0, 4, 0x0F);

        Assert.Equal((UInt16)0x0F, value);
    }

    [Fact(DisplayName = "UInt16获取多位")]
    public void UInt16_GetBits()
    {
        UInt16 value = 0xFF;
        var bits = value.GetBits(4, 4);

        Assert.Equal((UInt16)0x0F, bits);
    }

    [Fact(DisplayName = "UInt16位越界返回原值")]
    public void UInt16_SetBits_OutOfRange()
    {
        UInt16 value = 0x1234;
        // position >= 16 返回原值
        var result = value.SetBits(16, 1, 1);
        Assert.Equal(value, result);
    }

    [Fact(DisplayName = "UInt16获取位越界返回0")]
    public void UInt16_GetBits_OutOfRange()
    {
        UInt16 value = 0xFFFF;
        var result = value.GetBits(16, 1);
        Assert.Equal((UInt16)0, result);
    }

    [Fact(DisplayName = "UInt16 length为0返回原值")]
    public void UInt16_SetBits_LengthZero()
    {
        UInt16 value = 0x1234;
        var result = value.SetBits(0, 0, 0xFF);
        Assert.Equal(value, result);
    }
    #endregion

    #region Byte SetBit/GetBit
    [Theory(DisplayName = "Byte设置单个位")]
    [InlineData(0, 0, true, 1)]
    [InlineData(0, 7, true, 128)]
    [InlineData(0xFF, 0, false, 0xFE)]
    public void Byte_SetBit(Byte value, Int32 position, Boolean flag, Byte expected)
    {
        var result = value.SetBit(position, flag);
        Assert.Equal(expected, result);
    }

    [Theory(DisplayName = "Byte获取单个位")]
    [InlineData(1, 0, true)]
    [InlineData(0, 0, false)]
    [InlineData(128, 7, true)]
    [InlineData(128, 0, false)]
    public void Byte_GetBit(Byte value, Int32 position, Boolean expected)
    {
        Assert.Equal(expected, value.GetBit(position));
    }

    [Fact(DisplayName = "Byte位越界返回原值")]
    public void Byte_SetBit_OutOfRange()
    {
        Byte value = 0x12;
        var result = value.SetBit(8, true);
        Assert.Equal(value, result);
    }

    [Fact(DisplayName = "Byte获取位越界返回false")]
    public void Byte_GetBit_OutOfRange()
    {
        Byte value = 0xFF;
        Assert.False(value.GetBit(8));
    }
    #endregion

    #region 位操作往返
    [Fact(DisplayName = "UInt16设置后获取一致")]
    public void UInt16_RoundTrip()
    {
        UInt16 value = 0;
        value = value.SetBit(3, true);
        value = value.SetBit(7, true);

        Assert.True(value.GetBit(3));
        Assert.True(value.GetBit(7));
        Assert.False(value.GetBit(0));
        Assert.False(value.GetBit(15));
    }

    [Fact(DisplayName = "Byte设置后获取一致")]
    public void Byte_RoundTrip()
    {
        Byte value = 0;
        value = value.SetBit(0, true);
        value = value.SetBit(5, true);

        Assert.True(value.GetBit(0));
        Assert.True(value.GetBit(5));
        Assert.False(value.GetBit(3));
    }
    #endregion
}
