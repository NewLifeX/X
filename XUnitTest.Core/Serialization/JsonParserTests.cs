using System;
using System.Collections.Generic;
using NewLife;
using NewLife.Serialization;
using Xunit;

namespace XUnitTest.Serialization;

public class JsonParserTests
{
    [Fact]
    public void Decode()
    {
        var obj = new { Name = "NewLife", Version = "5.0" };
        var json = obj.ToJson();

        var jp = new JsonParser(json);
        var rs = jp.Decode();
        Assert.NotNull(rs);

        var dic = rs as IDictionary<String, Object>;
        Assert.NotNull(dic);
        Assert.Equal("NewLife", dic["Name"]);
        Assert.Equal("5.0", dic["Version"]);
    }

    [Fact]
    public void DecodeAsDictionary()
    {
        var json = """{"Name":"NewLife","Version":"5.0"}""";

        var dic = JsonParser.Decode(json);
        Assert.NotNull(dic);
        Assert.Equal("NewLife", dic["Name"]);
        Assert.Equal("5.0", dic["Version"]);
    }

    [Fact]
    public void DecodeError()
    {
        var json = """{"Name":"NewLife","Version":"5.0}""";

        var ex = Assert.Throws<XException>(() => JsonParser.Decode(json));
        Assert.Equal("Reached the end of the string while parsing it [5.0}]", ex.Message);
    }

    [Fact]
    public void DecodeString()
    {
        var json = "NewLife";

        var ex = Assert.Throws<XException>(() => JsonParser.Decode(json));
        Assert.Equal("Non standard Json string [NewLife]", ex.Message);
    }

    [Fact]
    public void DecodeString2()
    {
        var json = " {\"name\":\"NewLife\"}";

        var dic = JsonParser.Decode(json);
        Assert.NotNull(dic);
    }

    [Fact]
    public void DecodeString3()
    {
        var json = " \r\n  \t";

        var dic = JsonParser.Decode(json);
        Assert.Null(dic);
    }

    [Fact]
    [System.ComponentModel.DisplayName("支持注释与未加引号的键")]
    public void Decode_CommentsAndUnquotedKeys()
    {
        var json = """
        {
          // 单行注释
          Name: "NewLife", /* 块注释 */
          Version: 5,
          Enabled: true
        }
        """;

        var dic = new JsonParser(json).Decode() as IDictionary<String, Object>;

        Assert.NotNull(dic);
        Assert.Equal("NewLife", dic!["Name"]);
        Assert.Equal(5, dic["Version"]);
        Assert.Equal(true, dic["Enabled"]);
    }

    [Fact]
    [System.ComponentModel.DisplayName("数组保留 null 并识别 /Date(ms)/")]
    public void Decode_ArrayNullAndDate()
    {
        var arr = new JsonParser("""[1,null,"x","/Date(0)/"]""").Decode() as IList<Object>;

        Assert.NotNull(arr);
        Assert.Equal(4, arr!.Count);
        Assert.Equal(1, arr[0]);
        Assert.Null(arr[1]);
        Assert.Equal("x", arr[2]);
        Assert.IsType<DateTime>(arr[3]);
    }

    [Fact]
    [System.ComponentModel.DisplayName("大数组未转义字符串正确且不依赖池状态")]
    public void Decode_LargeUnescapedStringArray()
    {
        var json = "[" + String.Join(",", Enumerable.Repeat("\"abcdef\"", 1000)) + "]";

        var arr = new JsonParser(json).Decode() as IList<Object>;

        Assert.NotNull(arr);
        Assert.Equal(1000, arr!.Count);
        Assert.Equal("abcdef", arr[0]);
        Assert.Equal("abcdef", arr[999]);
    }

    [Theory]
    [System.ComponentModel.DisplayName("数字类型与边界保持原有行为")]
    [InlineData("0", typeof(Int32))]
    [InlineData("-0", typeof(Int32))]
    [InlineData("2147483646", typeof(Int32))]
    [InlineData("-2147483647", typeof(Int32))]
    [InlineData("2147483647", typeof(Int64))]
    [InlineData("-2147483648", typeof(Int64))]
    [InlineData("1.5", typeof(Double))]
    [InlineData("-1.5", typeof(Double))]
    [InlineData("1e3", typeof(Double))]
    [InlineData("1E-3", typeof(Double))]
    public void Decode_NumberTypes(String literal, Type expected)
    {
        var value = new JsonParser("[" + literal + "]").Decode() as IList<Object>;

        Assert.NotNull(value);
        Assert.IsType(expected, value![0]);
    }
}
