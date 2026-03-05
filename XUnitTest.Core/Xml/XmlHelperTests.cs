using System.Text;
using NewLife.Xml;
using Xunit;

namespace XUnitTest.Xml;

public class XmlHelperTests
{
    #region 测试实体
    public class TestModel
    {
        public String? Name { get; set; }
        public Int32 Age { get; set; }
        public Boolean Enabled { get; set; }
        public DateTime CreateTime { get; set; }
    }
    #endregion

    #region ToXml
    [Fact(DisplayName = "对象序列化为Xml字符串")]
    public void ToXmlTest()
    {
        var model = new TestModel
        {
            Name = "Stone",
            Age = 30,
            Enabled = true,
            CreateTime = new DateTime(2025, 1, 1)
        };

        var xml = model.ToXml();

        Assert.NotNull(xml);
        Assert.NotEmpty(xml);
        Assert.Contains("Stone", xml);
        Assert.Contains("30", xml);
    }

    [Fact(DisplayName = "null对象序列化返回空字符串")]
    public void ToXml_NullObject()
    {
        Object? obj = null;
        var xml = obj!.ToXml();

        Assert.Equal(String.Empty, xml);
    }

    [Fact(DisplayName = "指定编码序列化")]
    public void ToXml_WithEncoding()
    {
        var model = new TestModel { Name = "测试中文" };
        var xml = model.ToXml(Encoding.UTF8);

        Assert.NotNull(xml);
        Assert.Contains("测试中文", xml);
    }

    [Fact(DisplayName = "忽略XML声明")]
    public void ToXml_OmitDeclaration()
    {
        var model = new TestModel { Name = "Test" };
        var xml = model.ToXml(Encoding.UTF8, false, false, true);

        Assert.NotNull(xml);
        Assert.DoesNotContain("<?xml", xml);
    }

    [Fact(DisplayName = "序列化到数据流")]
    public void ToXml_Stream()
    {
        var model = new TestModel { Name = "StreamTest", Age = 25 };

        using var stream = new MemoryStream();
        model.ToXml(stream);

        Assert.True(stream.Length > 0);

        stream.Position = 0;
        var xml = new StreamReader(stream).ReadToEnd();
        Assert.Contains("StreamTest", xml);
    }

    [Fact(DisplayName = "null对象序列化到流不抛异常")]
    public void ToXml_StreamNullObject()
    {
        Object? obj = null;
        using var stream = new MemoryStream();
        obj!.ToXml(stream);
        Assert.Equal(0, stream.Length);
    }
    #endregion

    #region ToXmlEntity
    [Fact(DisplayName = "Xml字符串反序列化为对象")]
    public void ToXmlEntityTest()
    {
        var model = new TestModel { Name = "Hello", Age = 18, Enabled = true };
        var xml = model.ToXml();

        var model2 = xml.ToXmlEntity<TestModel>();

        Assert.NotNull(model2);
        Assert.Equal("Hello", model2!.Name);
        Assert.Equal(18, model2.Age);
        Assert.True(model2.Enabled);
    }

    [Fact(DisplayName = "空Xml字符串反序列化抛异常")]
    public void ToXmlEntity_Empty()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => "".ToXmlEntity<TestModel>());
        Assert.Contains("xml", ex.ParamName);
    }

    [Fact(DisplayName = "空白Xml字符串反序列化抛异常")]
    public void ToXmlEntity_Whitespace()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => "  ".ToXmlEntity<TestModel>());
        Assert.Contains("xml", ex.ParamName);
    }

    [Fact(DisplayName = "数据流反序列化为对象")]
    public void ToXmlEntity_FromStream()
    {
        var model = new TestModel { Name = "FromStream", Age = 20 };
        var xml = model.ToXml();

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var model2 = stream.ToXmlEntity<TestModel>();

        Assert.NotNull(model2);
        Assert.Equal("FromStream", model2!.Name);
    }

    [Fact(DisplayName = "数据流为null反序列化抛异常")]
    public void ToXmlEntity_NullStream()
    {
        Stream? stream = null;
        Assert.Throws<ArgumentNullException>(() => stream!.ToXmlEntity<TestModel>());
    }
    #endregion

    #region ToXmlFile
    [Fact(DisplayName = "序列化到文件并反序列化回来")]
    public void ToXmlFile_RoundTrip()
    {
        var dir = Path.Combine(Path.GetTempPath(), "XmlHelperTests");
        var file = Path.Combine(dir, "test_model.xml");

        try
        {
            var model = new TestModel { Name = "FileTest", Age = 42, Enabled = true };
            model.ToXmlFile(file);

            Assert.True(File.Exists(file));

            var model2 = file.ToXmlFileEntity<TestModel>();

            Assert.NotNull(model2);
            Assert.Equal("FileTest", model2!.Name);
            Assert.Equal(42, model2.Age);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    [Fact(DisplayName = "文件不存在反序列化返回null")]
    public void ToXmlFileEntity_FileNotExist()
    {
        var result = "nonexistent_file_path.xml".ToXmlFileEntity<TestModel>();
        Assert.Null(result);
    }

    [Fact(DisplayName = "空文件路径反序列化抛异常")]
    public void ToXmlFileEntity_EmptyPath()
    {
        Assert.Throws<ArgumentNullException>(() => "".ToXmlFileEntity<TestModel>());
    }
    #endregion

    #region 字典互转
    [Fact(DisplayName = "字典转Xml再转回")]
    public void DictionaryToXml_RoundTrip()
    {
        var dic = new Dictionary<String, String>
        {
            ["Name"] = "Stone",
            ["Age"] = "30",
            ["City"] = "Shanghai"
        };

        var xml = dic.ToXml("root");

        Assert.NotNull(xml);
        Assert.Contains("Stone", xml);
        Assert.Contains("Age", xml);

        var dic2 = xml.ToXmlDictionary();

        Assert.NotNull(dic2);
        Assert.Equal(3, dic2!.Count);
        Assert.Equal("Stone", dic2["Name"]);
        Assert.Equal("30", dic2["Age"]);
    }

    [Fact(DisplayName = "空字符串转字典返回null")]
    public void ToXmlDictionary_EmptyString()
    {
        var result = "".ToXmlDictionary();
        Assert.Null(result);
    }

    [Fact(DisplayName = "字典转Xml默认根节点名")]
    public void DictionaryToXml_DefaultRootName()
    {
        var dic = new Dictionary<String, String> { ["key1"] = "value1" };
        var xml = dic.ToXml();

        Assert.Contains("<xml>", xml);
    }

    [Fact(DisplayName = "空字典转Xml")]
    public void DictionaryToXml_EmptyDic()
    {
        var dic = new Dictionary<String, String>();
        var xml = dic.ToXml("test");

        Assert.NotNull(xml);
        Assert.Contains("<test", xml);
    }

    [Fact(DisplayName = "字典文件序列化")]
    public void DictionaryToXmlFile()
    {
        var dir = Path.Combine(Path.GetTempPath(), "XmlHelperTests");
        var file = Path.Combine(dir, "dic.xml");

        try
        {
            var dic = new Dictionary<String, String>
            {
                ["Key1"] = "Val1",
                ["Key2"] = "Val2"
            };

            dic.ToXmlFile(file);
            Assert.True(File.Exists(file));

            var content = File.ReadAllText(file);
            Assert.Contains("Val1", content);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }
    #endregion
}
