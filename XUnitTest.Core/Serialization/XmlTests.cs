using NewLife;
using NewLife.Serialization;
using NewLife.Xml;
using Xunit;

namespace XUnitTest.Serialization;

public class XmlTests
{
    [Fact]
    public void ToXml_BasicObject()
    {
        var obj = new XmlModel { Name = "test", Value = 42 };
        var result = XmlHelper.ToXml(obj);

        Assert.NotNull(result);
        Assert.Contains("test", result);
        Assert.Contains("42", result);
    }

    [Fact]
    public void ToXmlEntity_BasicObject()
    {
        var xmlStr = "<?xml version=\"1.0\" encoding=\"utf-8\"?><XmlModel><Name>hello</Name><Value>99</Value></XmlModel>";
        var result = XmlHelper.ToXmlEntity<XmlModel>(xmlStr);

        Assert.NotNull(result);
        Assert.Equal("hello", result.Name);
        Assert.Equal(99, result.Value);
    }

    [Fact]
    public void ToXml_ToXmlEntity_RoundTrip()
    {
        var original = new XmlModel { Name = "roundtrip", Value = 123 };
        var xmlStr = XmlHelper.ToXml(original);
        var restored = XmlHelper.ToXmlEntity<XmlModel>(xmlStr);

        Assert.NotNull(restored);
        Assert.Equal(original.Name, restored.Name);
        Assert.Equal(original.Value, restored.Value);
    }

    private class XmlModel
    {
        public String? Name { get; set; }
        public Int32 Value { get; set; }
    }
}

