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
}
