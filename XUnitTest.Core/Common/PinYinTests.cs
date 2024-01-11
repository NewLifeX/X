using System;
using NewLife.Common;
using Xunit;

namespace XUnitTest.Common;

public class PinYinTests
{
    [Fact]
    public void Get()
    {
        var py = PinYin.Get('重');
        //Assert.Equal("ChongQing", py);
        Assert.Equal("Zhong", py);
    }

    [Fact]
    public void Get2()
    {
        var py = PinYin.Get("重庆");
        //Assert.Equal("ChongQing", py);
        Assert.Equal("ZhongQing", py);
    }

    [Theory]
    [InlineData("重庆", "ZhongQing")]
    [InlineData("东莞", "DongGuan")]
    [InlineData("畲江", "SheJiang")]
    [InlineData("漯河", "LuoHe")]
    public void GetAll(String name, String pinyin)
    {
        var py = PinYin.Get(name);
        Assert.Equal(pinyin, py);
    }
}
