using NewLife.Serialization;
using Xunit;

namespace XUnitTest.Serialization;

/// <summary>XML解析器测试</summary>
public class XmlParserTests
{
    [Fact(DisplayName = "解析简单XML")]
    public void ParseSimpleXml()
    {
        var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<root>
    <name>NewLife</name>
    <value>123</value>
</root>";

        var dic = XmlParser.Decode(xml);
        Assert.NotNull(dic);
        Assert.Equal("NewLife", dic["name"]);
        Assert.Equal("123", dic["value"]);
    }

    [Fact(DisplayName = "解析嵌套XML")]
    public void ParseNestedXml()
    {
        var xml = @"<Config>
    <App>
        <Id>1</Id>
        <Name>Test</Name>
    </App>
    <Debug>true</Debug>
</Config>";

        var dic = XmlParser.Decode(xml);
        Assert.NotNull(dic);

        // App 应为嵌套字典
        var app = dic["App"] as IDictionary<String, Object?>;
        Assert.NotNull(app);
        Assert.Equal("1", app!["Id"]);
        Assert.Equal("Test", app["Name"]);

        Assert.Equal("true", dic["Debug"]);
    }

    [Fact(DisplayName = "解析带属性的XML")]
    public void ParseAttributes()
    {
        var xml = @"<Response>
    <Status Code=""200"" />
</Response>";

        var dic = XmlParser.Decode(xml);
        Assert.NotNull(dic);
        // 属性值被读取到字典中
        Assert.Equal("200", dic["Code"]);
    }

    [Fact(DisplayName = "解析空XML抛出异常")]
    public void ParseEmptyThrowsException()
    {
        Assert.Throws<System.Xml.XmlException>(() => XmlParser.Decode(""));
    }
}
