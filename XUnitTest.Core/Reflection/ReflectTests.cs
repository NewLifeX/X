using System;
using System.Collections;
using System.Collections.Generic;
using NewLife.Reflection;
using Xunit;

namespace XUnitTest.Reflection;

public class ReflectTests
{
    [Theory]
    [InlineData(typeof(Boolean))]
    [InlineData(typeof(Char))]
    [InlineData(typeof(Byte))]
    [InlineData(typeof(Int16))]
    [InlineData(typeof(UInt16))]
    [InlineData(typeof(Int32))]
    [InlineData(typeof(UInt32))]
    [InlineData(typeof(Int64))]
    [InlineData(typeof(UInt64))]
    [InlineData(typeof(Single))]
    [InlineData(typeof(Double))]
    [InlineData(typeof(Decimal))]
    [InlineData(typeof(String))]
    [InlineData(typeof(DateTime))]
    [InlineData(typeof(Guid))]
    [InlineData(typeof(Byte[]))]
    public void GetTypeExTest(Type type)
    {
        var name = type.Name;
        var t2 = name.GetTypeEx();
        Assert.Equal(type, t2);
    }

    [Theory]
    [InlineData("true", typeof(Boolean), true)]
    [InlineData("1234", typeof(Int16), (Int16)1234)]
    [InlineData("1234", typeof(Int32), 1234)]
    [InlineData("12.34", typeof(Double), 12.34)]
    [InlineData("byte[]", typeof(Type), typeof(Byte[]))]
    public void ChangeTypeTest(Object value, Type targetType, Object target)
    {
        var rs = value.ChangeType(targetType);
        Assert.Equal(target, rs);
    }

    [Fact]
    public void DateTimeOffsetChangeTypeTest()
    {
        var value = "2023/4/5 11:32 +08:00";
        var targetType = typeof(DateTimeOffset);
        var target = new DateTimeOffset(2023, 4, 5, 11, 32, 00, TimeSpan.FromHours(8));

        var rs = value.ChangeType(targetType);
        Assert.Equal(target, rs);
    }

    [Fact]
    public void AsListTest()
    {
        var list = new List<Int32>();
        var type = list.GetType();

        Assert.True(type.As<List<Int32>>());
        Assert.True(type.As<IList<Int32>>());
        Assert.True(type.As<IList>());
        Assert.True(type.As(typeof(List<Int32>)));
        Assert.True(type.As(typeof(IList<Int32>)));
        Assert.True(type.As(typeof(IList<>)));
    }

    [Fact]
    public void AsDictionaryTest()
    {
        var dic = new Dictionary<Int32, String>();
        var type = dic.GetType();

        Assert.True(type.As<Dictionary<Int32, String>>());
        Assert.True(type.As<IDictionary<Int32, String>>());
        Assert.True(type.As<IDictionary>());
        Assert.True(type.As(typeof(Dictionary<Int32, String>)));
        Assert.True(type.As(typeof(IDictionary<Int32, String>)));
        Assert.True(type.As(typeof(IDictionary<,>)));
    }
}