using System.ComponentModel;
using System.Reflection;
using NewLife;
using Xunit;

namespace XUnitTest.Reflection;

public class AttributeXTests
{
    #region GetCustomAttributes
    [Fact(DisplayName = "获取程序集自定义属性")]
    public void GetCustomAttributes_Assembly()
    {
        var asm = typeof(String).Assembly;
        var attrs = AttributeX.GetCustomAttributes<AssemblyTitleAttribute>(asm);

        Assert.NotNull(attrs);
    }

    [Fact(DisplayName = "null程序集返回空数组")]
    public void GetCustomAttributes_NullAssembly()
    {
        Assembly? asm = null;
        var attrs = AttributeX.GetCustomAttributes<AssemblyTitleAttribute>(asm!);

        Assert.NotNull(attrs);
        Assert.Empty(attrs);
    }

    [Fact(DisplayName = "缓存生效返回同一实例")]
    public void GetCustomAttributes_Cached()
    {
        var asm = typeof(Int32).Assembly;

        var attrs1 = AttributeX.GetCustomAttributes<AssemblyTitleAttribute>(asm);
        var attrs2 = AttributeX.GetCustomAttributes<AssemblyTitleAttribute>(asm);

        Assert.Same(attrs1, attrs2);
    }
    #endregion

    #region GetDisplayName
    [Fact(DisplayName = "获取成员DisplayName")]
    public void GetDisplayName_HasAttribute()
    {
        var member = typeof(TestClass).GetProperty(nameof(TestClass.DisplayNameProp))!;
        var name = AttributeX.GetDisplayName(member);

        Assert.Equal("显示名称", name);
    }

    [Fact(DisplayName = "无DisplayName返回null")]
    public void GetDisplayName_NoAttribute()
    {
        var member = typeof(TestClass).GetProperty(nameof(TestClass.NoProp))!;
        var name = AttributeX.GetDisplayName(member);

        Assert.Null(name);
    }
    #endregion

    #region GetDescription
    [Fact(DisplayName = "获取成员Description")]
    public void GetDescription_HasAttribute()
    {
        var member = typeof(TestClass).GetProperty(nameof(TestClass.DescProp))!;
        var desc = AttributeX.GetDescription(member);

        Assert.Equal("描述文字", desc);
    }

    [Fact(DisplayName = "无Description返回null")]
    public void GetDescription_NoAttribute()
    {
        var member = typeof(TestClass).GetProperty(nameof(TestClass.NoProp))!;
        var desc = AttributeX.GetDescription(member);

        Assert.Null(desc);
    }
    #endregion

    #region GetCustomAttributeValue
    [Fact(DisplayName = "获取程序集属性值")]
    public void GetCustomAttributeValue_Assembly()
    {
        var asm = typeof(Object).Assembly;
        var title = AttributeX.GetCustomAttributeValue<AssemblyTitleAttribute, String>(asm);

        Assert.NotNull(title);
    }

    [Fact(DisplayName = "null程序集返回默认值")]
    public void GetCustomAttributeValue_NullAssembly()
    {
        Assembly? asm = null;
        var result = AttributeX.GetCustomAttributeValue<AssemblyTitleAttribute, String>(asm!);

        Assert.Null(result);
    }

    [Fact(DisplayName = "获取成员属性值")]
    public void GetCustomAttributeValue_Member()
    {
        var member = typeof(TestClass).GetProperty(nameof(TestClass.DescProp))!;
        var desc = AttributeX.GetCustomAttributeValue<DescriptionAttribute, String>(member);

        Assert.Equal("描述文字", desc);
    }

    [Fact(DisplayName = "null成员返回默认值")]
    public void GetCustomAttributeValue_NullMember()
    {
        MemberInfo? member = null;
        var result = AttributeX.GetCustomAttributeValue<DescriptionAttribute, String>(member!);

        Assert.Null(result);
    }
    #endregion

    #region 辅助类
    class TestClass
    {
        [DisplayName("显示名称")]
        public String? DisplayNameProp { get; set; }

        [Description("描述文字")]
        public String? DescProp { get; set; }

        public String? NoProp { get; set; }
    }
    #endregion
}
