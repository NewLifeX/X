using System;
using System.ComponentModel;
using NewLife;
using Xunit;

namespace XUnitTest.Extension;

public class EnumHelperTests
{
    // 测试枚举定义
    [Flags]
    enum TestFlags
    {
        None = 0,
        Read = 1,
        Write = 2,
        Execute = 4,
        ReadWrite = Read | Write,
        All = Read | Write | Execute
    }

    enum TestEnum
    {
        [Description("第一个值")]
        First = 1,

        [Description("第二个值")]
        Second = 2,

        Third = 3,

        Fourth = 4,

        // 测试相同值的不同名称
        FirstAlias = 1
    }

    enum TestEnumWithoutAttributes
    {
        Value1 = 10,
        Value2 = 20,
        Value3 = 30
    }

    [Fact(DisplayName = "Has方法_标准标志位测试")]
    public void Has_StandardFlags_Test()
    {
        var flags = TestFlags.ReadWrite;

        // 包含的标志位
        Assert.True(flags.Has(TestFlags.Read));
        Assert.True(flags.Has(TestFlags.Write));
        
        // 不包含的标志位
        Assert.False(flags.Has(TestFlags.Execute));
        
        // 组合标志位
        Assert.True(flags.Has(TestFlags.ReadWrite));
        Assert.False(flags.Has(TestFlags.All));
        
        // None 标志位
        Assert.False(flags.Has(TestFlags.None));
        var none = TestFlags.None;
        Assert.True(none.Has(TestFlags.None));
    }

    [Fact(DisplayName = "Has方法_边界情况测试")]
    public void Has_EdgeCases_Test()
    {
        var all = TestFlags.All;
        
        // 包含所有标志位
        Assert.True(all.Has(TestFlags.Read));
        Assert.True(all.Has(TestFlags.Write));
        Assert.True(all.Has(TestFlags.Execute));
        Assert.True(all.Has(TestFlags.ReadWrite));
        Assert.True(all.Has(TestFlags.All));
        
        // 单个标志位测试
        var read = TestFlags.Read;
        Assert.True(read.Has(TestFlags.Read));
        Assert.False(read.Has(TestFlags.Write));
    }

    [Fact(DisplayName = "Has方法_类型不匹配异常")]
    public void Has_TypeMismatch_ThrowsException()
    {
        var flags = TestFlags.Read;
        var enumValue = TestEnum.First;

        var ex = Assert.Throws<ArgumentException>(() => flags.Has(enumValue));
        Assert.Equal("flag", ex.ParamName);
        Assert.Contains("Enumeration identification judgment must be of the same type", ex.Message);
    }

    [Fact(DisplayName = "Set方法_设置标志位测试")]
    public void Set_SetFlags_Test()
    {
        var flags = TestFlags.Read;

        // 设置新标志位
        var result = flags.Set(TestFlags.Write, true);
        Assert.Equal(TestFlags.ReadWrite, result);
        Assert.True(result.Has(TestFlags.Read));
        Assert.True(result.Has(TestFlags.Write));

        // 清除标志位
        result = result.Set(TestFlags.Read, false);
        Assert.Equal(TestFlags.Write, result);
        Assert.False(result.Has(TestFlags.Read));
        Assert.True(result.Has(TestFlags.Write));

        // 重复设置相同标志位
        result = result.Set(TestFlags.Write, true);
        Assert.Equal(TestFlags.Write, result);

        // 清除不存在的标志位
        result = result.Set(TestFlags.Execute, false);
        Assert.Equal(TestFlags.Write, result);
    }

    [Fact(DisplayName = "Set方法_组合标志位测试")]
    public void Set_CombinedFlags_Test()
    {
        var flags = TestFlags.None;

        // 设置组合标志位
        var result = flags.Set(TestFlags.ReadWrite, true);
        Assert.Equal(TestFlags.ReadWrite, result);
        Assert.True(result.Has(TestFlags.Read));
        Assert.True(result.Has(TestFlags.Write));

        // 清除部分组合标志位
        result = result.Set(TestFlags.Read, false);
        Assert.Equal(TestFlags.Write, result);
        Assert.False(result.Has(TestFlags.Read));
        Assert.True(result.Has(TestFlags.Write));
    }

    [Fact(DisplayName = "Set方法_类型不匹配异常")]
    public void Set_TypeMismatch_ThrowsException()
    {
        var enumValue = TestEnum.First;

        var ex = Assert.Throws<ArgumentException>(() => enumValue.Set(TestFlags.Read, true));
        Assert.Equal("source", ex.ParamName);
        Assert.Contains("Enumeration identification judgment must be of the same type", ex.Message);
    }

    [Fact(DisplayName = "GetDescription方法_有描述属性测试")]
    public void GetDescription_WithDescriptionAttribute_Test()
    {
        Assert.Equal("第一个值", TestEnum.First.GetDescription());
        Assert.Equal("第二个值", TestEnum.Second.GetDescription());
    }

    [Fact(DisplayName = "GetDescription方法_无描述属性测试")]
    public void GetDescription_WithoutDescriptionAttribute_Test()
    {
        // 没有 DescriptionAttribute 但有 DisplayName 的枚举值
        Assert.Null(TestEnum.Third.GetDescription());
        
        // 完全没有属性的枚举值
        Assert.Null(TestEnum.Fourth.GetDescription());
        
        // 没有任何属性的枚举
        Assert.Null(TestEnumWithoutAttributes.Value1.GetDescription());
    }

    [Fact(DisplayName = "GetDescription方法_空值测试")]
    public void GetDescription_NullValue_Test()
    {
        TestEnum? nullEnum = null;
        Assert.Null(nullEnum.GetDescription());
    }

    [Fact(DisplayName = "GetDescription方法_无效枚举值测试")]
    public void GetDescription_InvalidEnumValue_Test()
    {
        // 不存在的枚举值
        var invalidEnum = (TestEnum)999;
        Assert.Null(invalidEnum.GetDescription());
    }

    [Fact(DisplayName = "GetDescriptions泛型方法_完整测试")]
    public void GetDescriptions_Generic_Test()
    {
        var descriptions = EnumHelper.GetDescriptions<TestEnum>();

        Assert.NotNull(descriptions);
        Assert.Equal(4, descriptions.Count); // First, Second, Third, Fourth (FirstAlias会覆盖First，因为值相同)

        // 验证描述内容 - 优先使用 DisplayName，其次 Description，最后字段名
        Assert.Equal("FirstAlias", descriptions[TestEnum.First]);   // FirstAlias覆盖了First，因为值相同 
        Assert.Equal("第二个值", descriptions[TestEnum.Second]);    // 只有 Description  
        Assert.Equal("Third", descriptions[TestEnum.Third]);        // 没有属性，使用字段名
        Assert.Equal("Fourth", descriptions[TestEnum.Fourth]);      // 没有属性，使用字段名
        // FirstAlias和First指向同一个值，所以应该是相同的
        Assert.Equal("FirstAlias", descriptions[TestEnum.FirstAlias]);
    }

    [Fact(DisplayName = "GetDescriptions泛型方法_无属性枚举测试")]
    public void GetDescriptions_Generic_NoAttributes_Test()
    {
        var descriptions = EnumHelper.GetDescriptions<TestEnumWithoutAttributes>();

        Assert.NotNull(descriptions);
        Assert.Equal(3, descriptions.Count);

        Assert.Equal("Value1", descriptions[TestEnumWithoutAttributes.Value1]);
        Assert.Equal("Value2", descriptions[TestEnumWithoutAttributes.Value2]);
        Assert.Equal("Value3", descriptions[TestEnumWithoutAttributes.Value3]);
    }

    [Fact(DisplayName = "GetDescriptions类型参数方法_完整测试")]
    public void GetDescriptions_Type_Test()
    {
        var descriptions = EnumHelper.GetDescriptions(typeof(TestEnum));

        Assert.NotNull(descriptions);
        Assert.Equal(4, descriptions.Count); // First, Second, Third, Fourth (FirstAlias会覆盖First)

        // 验证键值对应关系
        Assert.Equal("FirstAlias", descriptions[1]);  // FirstAlias = 1，会覆盖 First
        Assert.Equal("第二个值", descriptions[2]);     // Second = 2，Description
        Assert.Equal("Third", descriptions[3]);       // Third = 3，字段名
        Assert.Equal("Fourth", descriptions[4]);      // Fourth = 4，字段名
        
        // FirstAlias = 1，相同值会覆盖，保留最后一个
        Assert.True(descriptions.ContainsKey(1));
    }

    [Fact(DisplayName = "GetDescriptions类型参数方法_标志位枚举测试")]
    public void GetDescriptions_Type_FlagsEnum_Test()
    {
        var descriptions = EnumHelper.GetDescriptions(typeof(TestFlags));

        Assert.NotNull(descriptions);
        Assert.Equal(6, descriptions.Count); // None, Read, Write, Execute, ReadWrite, All

        Assert.Equal("None", descriptions[0]);      // None = 0
        Assert.Equal("Read", descriptions[1]);      // Read = 1
        Assert.Equal("Write", descriptions[2]);     // Write = 2
        Assert.Equal("Execute", descriptions[4]);   // Execute = 4
        Assert.Equal("ReadWrite", descriptions[3]); // ReadWrite = 3 (Read | Write)
        Assert.Equal("All", descriptions[7]);       // All = 7 (Read | Write | Execute)
    }

    [Fact(DisplayName = "GetDescriptions类型参数方法_非枚举类型测试")]
    public void GetDescriptions_Type_NonEnumType_Test()
    {
        var descriptions = EnumHelper.GetDescriptions(typeof(string));

        Assert.NotNull(descriptions);
        Assert.Empty(descriptions); // 非枚举类型返回空字典
    }

    [Fact(DisplayName = "EnumHelper方法_复杂场景综合测试")]
    public void EnumHelper_ComplexScenario_Test()
    {
        // 复杂的标志位操作场景
        var permissions = TestFlags.None;
        
        // 逐步添加权限
        permissions = permissions.Set(TestFlags.Read, true);
        Assert.True(permissions.Has(TestFlags.Read));
        Assert.False(permissions.Has(TestFlags.Write));
        
        permissions = permissions.Set(TestFlags.Write, true);
        Assert.True(permissions.Has(TestFlags.ReadWrite));
        
        permissions = permissions.Set(TestFlags.Execute, true);
        Assert.True(permissions.Has(TestFlags.All));
        
        // 部分撤销权限
        permissions = permissions.Set(TestFlags.Write, false);
        Assert.False(permissions.Has(TestFlags.Write));
        Assert.True(permissions.Has(TestFlags.Read));
        Assert.True(permissions.Has(TestFlags.Execute));
        Assert.False(permissions.Has(TestFlags.All));
        
        // 验证最终状态
        var expected = TestFlags.Read | TestFlags.Execute;
        Assert.Equal(expected, permissions);
    }

    [Fact(DisplayName = "枚举描述_属性优先级测试")]
    public void EnumDescription_AttributePriority_Test()
    {
        // 验证属性优先级：DisplayName > Description > FieldName
        var descriptions = EnumHelper.GetDescriptions(typeof(TestEnum));
        
        // FirstAlias = 1，会覆盖 First 的值，保留最后一个字段名
        Assert.Equal("FirstAlias", descriptions[1]);
        
        // Second 只有 Description
        Assert.Equal("第二个值", descriptions[2]);
        
        // Third 没有属性，使用字段名
        Assert.Equal("Third", descriptions[3]);
        
        // Fourth 没有属性，使用字段名
        Assert.Equal("Fourth", descriptions[4]);
    }

    // 测试用于验证空描述不会覆盖字段名的枚举
    enum TestEmptyDescription
    {
        [Description("")]
        EmptyDesc = 1,
        
        [Description("   ")]
        WhitespaceDesc = 2,
        
        EmptyDisplayName = 3,
        
        WhitespaceDisplayName = 4
    }

    [Fact(DisplayName = "枚举描述_空值处理测试")]
    public void EnumDescription_EmptyValue_Test()
    {
        var descriptions = EnumHelper.GetDescriptions(typeof(TestEmptyDescription));

        // 空字符串或空白字符串的描述应该回退到字段名
        Assert.Equal("EmptyDesc", descriptions[1]);
        Assert.Equal("   ", descriptions[2]);
        Assert.Equal("EmptyDisplayName", descriptions[3]);
        Assert.Equal("WhitespaceDisplayName", descriptions[4]);
    }
}