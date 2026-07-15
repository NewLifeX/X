using System.Collections.Generic;
using System.ComponentModel;
using NewLife.Data;
using Xunit;

namespace XUnitTest.Data;

public class IModelTests
{
    private class SimpleModel : IModel
    {
        private readonly Dictionary<String, Object?> _data = new();

        public Object? this[String key]
        {
            get => _data.TryGetValue(key, out var val) ? val : null;
            set
            {
                if (value != null)
                    _data[key] = value;
                else
                    _data.Remove(key);
            }
        }
    }

    [Fact]
    [DisplayName("IModel_设置和获取属性值")]
    public void SetAndGet_PropertyValue()
    {
        var model = new SimpleModel();
        model["Name"] = "TestName";
        model["Age"] = 25;
        model["Active"] = true;

        Assert.Equal("TestName", model["Name"]);
        Assert.Equal(25, model["Age"]);
        Assert.Equal(true, model["Active"]);
    }

    [Fact]
    [DisplayName("IModel_不存在的键_返回null")]
    public void NonExistentKey_ReturnsNull()
    {
        var model = new SimpleModel();
        var value = model["NonExistent"];
        Assert.Null(value);
    }

    [Fact]
    [DisplayName("IModel_设置null_删除键")]
    public void SetNull_RemovesKey()
    {
        var model = new SimpleModel();
        model["Key"] = "SomeValue";
        Assert.Equal("SomeValue", model["Key"]);

        model["Key"] = null;
        Assert.Null(model["Key"]);
    }

    [Fact]
    [DisplayName("IModel_覆盖已有值")]
    public void OverwriteExistingValue()
    {
        var model = new SimpleModel();
        model["Key"] = "Original";
        Assert.Equal("Original", model["Key"]);

        model["Key"] = "Updated";
        Assert.Equal("Updated", model["Key"]);
    }

    [Fact]
    [DisplayName("IModel_多种值类型")]
    public void MultipleValueTypes()
    {
        var model = new SimpleModel();
        model["String"] = "Hello";
        model["Int32"] = 42;
        model["Double"] = 3.14;
        model["Boolean"] = false;
        model["Null"] = null;

        Assert.Equal("Hello", model["String"]);
        Assert.Equal(42, model["Int32"]);
        Assert.Equal(3.14, model["Double"]);
        Assert.Equal(false, model["Boolean"]);
        Assert.Null(model["Null"]);
    }

    [Fact]
    [DisplayName("IModel_空字符串键_可存储")]
    public void EmptyStringKey()
    {
        var model = new SimpleModel();
        model[""] = "EmptyKey";
        Assert.Equal("EmptyKey", model[""]);
    }

    [Fact]
    [DisplayName("IModel_大小写敏感键")]
    public void CaseSensitiveKeys()
    {
        var model = new SimpleModel();
        model["Key"] = "Upper";
        model["key"] = "Lower";

        Assert.Equal("Upper", model["Key"]);
        Assert.Equal("Lower", model["key"]);
        Assert.NotEqual(model["Key"], model["key"]);
    }
}
