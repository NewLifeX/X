using System;
using NewLife;
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
        Assert.Equal("ChongQing", py);
        //Assert.Equal("ZhongQing", py);
    }

    [Fact]
    public void GetFirst()
    {
        var p = PinYin.GetFirst('重');
        Assert.Equal('Z', p);

        var py = PinYin.GetFirst("重庆");
        Assert.Equal("CQ", py);
        //Assert.Equal("ZQ", py);
    }

    //[Fact]
    //public void GetFirstOne()
    //{
    //    var py = PinYin.GetFirstOne("重庆");
    //    Assert.Equal("Z", py);
    //}

    [Theory]
    [InlineData("重庆", "ChongQing")]
    //[InlineData("重庆", "ZhongQing")]
    [InlineData("东莞", "DongGuan")]
    [InlineData("畲江", "SheJiang")]
    [InlineData("漯河", "LuoHe")]
    [InlineData("湾沚", "WanZhi")]
    [InlineData("埇桥", "YongQiao")]
    [InlineData("瀍河", "ChanHe")]
    [InlineData("浉河", "ShiHe")]
    [InlineData("猇亭", "XiaoTing")]
    [InlineData("鄠邑", "HuYi")]
    [InlineData("崁顶乡", "KanDingXiang")]
    [InlineData("深水埗", "ShenShuiBu")]
    [InlineData("漷县", "HuoXian")]
    [InlineData("甪直", "LuZhi")]
    [InlineData("道滘", "DaoJiao")]
    public void GetString(String name, String pinyin)
    {
        var py = PinYin.Get(name);
        Assert.Equal(pinyin, py);
    }

    [Theory]
    [InlineData("重庆", "ChongQing")]
    //[InlineData("重庆", "ZhongQing")]
    [InlineData("东莞", "DongGuan")]
    [InlineData("畲江", "SheJiang")]
    [InlineData("漯河", "LuoHe")]
    public void GetAll(String name, String pinyin)
    {
        var py = PinYin.GetAll(name);
        Assert.Equal(pinyin, py.Join(""));
    }
}
