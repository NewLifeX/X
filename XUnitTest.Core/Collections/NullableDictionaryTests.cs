using NewLife.Collections;
using Xunit;

namespace XUnitTest.Collections;

public class NullableDictionaryTests
{
    [Fact(DisplayName = "不存在的键返回默认值")]
    public void MissingKey_ReturnsDefault()
    {
        var dic = new NullableDictionary<String, String>();

        var result = dic["nonexistent"];

        Assert.Null(result);
    }

    [Fact(DisplayName = "值类型不存在的键返回默认值")]
    public void MissingKey_ValueType_ReturnsDefault()
    {
        var dic = new NullableDictionary<String, Int32>();

        var result = dic["nonexistent"];

        Assert.Equal(0, result);
    }

    [Fact(DisplayName = "正常存取")]
    public void SetAndGet()
    {
        var dic = new NullableDictionary<String, String>();
        dic["key"] = "value";

        Assert.Equal("value", dic["key"]);
    }

    [Fact(DisplayName = "覆盖已有值")]
    public void OverwriteValue()
    {
        var dic = new NullableDictionary<String, Int32>();
        dic["key"] = 1;
        dic["key"] = 2;

        Assert.Equal(2, dic["key"]);
    }

    [Fact(DisplayName = "自定义比较器")]
    public void CustomComparer()
    {
        var dic = new NullableDictionary<String, String>(StringComparer.OrdinalIgnoreCase);
        dic["Key"] = "Value";

        Assert.Equal("Value", dic["key"]);
        Assert.Equal("Value", dic["KEY"]);
    }

    [Fact(DisplayName = "从已有字典构造")]
    public void CtorFromDictionary()
    {
        var source = new Dictionary<String, Int32>
        {
            ["a"] = 1,
            ["b"] = 2
        };
        var dic = new NullableDictionary<String, Int32>(source);

        Assert.Equal(1, dic["a"]);
        Assert.Equal(2, dic["b"]);
        Assert.Equal(0, dic["c"]);
    }

    [Fact(DisplayName = "从已有字典和比较器构造")]
    public void CtorFromDictionaryAndComparer()
    {
        var source = new Dictionary<String, String>
        {
            ["name"] = "test"
        };
        var dic = new NullableDictionary<String, String>(source, StringComparer.OrdinalIgnoreCase);

        Assert.Equal("test", dic["NAME"]);
    }

    [Fact(DisplayName = "Count正确")]
    public void CountTest()
    {
        var dic = new NullableDictionary<Int32, String>();
        dic[1] = "one";
        dic[2] = "two";

        Assert.Equal(2, dic.Count);
    }

    [Fact(DisplayName = "TryGetValue存在的键")]
    public void TryGetValue_Exists()
    {
        var dic = new NullableDictionary<String, Int32>();
        dic["key"] = 42;

        Assert.True(dic.TryGetValue("key", out var val));
        Assert.Equal(42, val);
    }

    [Fact(DisplayName = "TryGetValue不存在的键")]
    public void TryGetValue_NotExists()
    {
        var dic = new NullableDictionary<String, String>();

        Assert.False(dic.TryGetValue("missing", out var val));
        Assert.Null(val);
    }

    [Fact(DisplayName = "ContainsKey检查")]
    public void ContainsKeyTest()
    {
        var dic = new NullableDictionary<String, Int32>();
        dic["yes"] = 1;

        Assert.True(dic.ContainsKey("yes"));
        Assert.False(dic.ContainsKey("no"));
    }

    [Fact(DisplayName = "Remove移除")]
    public void RemoveTest()
    {
        var dic = new NullableDictionary<String, String>();
        dic["key"] = "val";

        Assert.True(dic.Remove("key"));
        Assert.Null(dic["key"]);
    }
}
