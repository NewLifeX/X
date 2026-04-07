using NewLife.Serialization;
using Xunit;

namespace XUnitTest.Serialization;

public class PropertyNamingTests
{
    #region 测试模型
    class NamingModel
    {
        public String FirstName { get; set; }
        public String LastName { get; set; }
        public Int32 UserAge { get; set; }
    }

    class IdModel
    {
        public Int32 ID { get; set; }
        public String UserName { get; set; }
    }
    #endregion

    #region JsonOptions 测试

    [Fact(DisplayName = "默认 PropertyNaming 为 None")]
    public void JsonOptions_DefaultPropertyNaming_IsNone()
    {
        var options = new JsonOptions();
        Assert.Equal(PropertyNaming.None, options.PropertyNaming);
    }

    [Fact(DisplayName = "设置 PropertyNaming 后 CamelCase getter 反映驼峰状态")]
    public void JsonOptions_CamelCase_Getter_ReflectsPropertyNaming()
    {
#pragma warning disable CS0618
        var options = new JsonOptions { PropertyNaming = PropertyNaming.CamelCase };
        Assert.True(options.CamelCase);

        options.PropertyNaming = PropertyNaming.None;
        Assert.False(options.CamelCase);

        options.PropertyNaming = PropertyNaming.SnakeCaseLower;
        Assert.False(options.CamelCase);
#pragma warning restore CS0618
    }

    [Fact(DisplayName = "CamelCase setter 为 true 时设置 PropertyNaming.CamelCase")]
    public void JsonOptions_CamelCase_Setter_True_SetsPropertyNaming()
    {
#pragma warning disable CS0618
        var options = new JsonOptions();
        options.CamelCase = true;
        Assert.Equal(PropertyNaming.CamelCase, options.PropertyNaming);
#pragma warning restore CS0618
    }

    [Fact(DisplayName = "CamelCase setter 为 false 时重置 PropertyNaming.None")]
    public void JsonOptions_CamelCase_Setter_False_ResetsPropertyNaming()
    {
#pragma warning disable CS0618
        var options = new JsonOptions { PropertyNaming = PropertyNaming.CamelCase };
        options.CamelCase = false;
        Assert.Equal(PropertyNaming.None, options.PropertyNaming);
#pragma warning restore CS0618
    }

    [Fact(DisplayName = "复制构造函数正确复制 PropertyNaming")]
    public void JsonOptions_CopyConstructor_CopiesPropertyNaming()
    {
        foreach (var naming in Enum.GetValues<PropertyNaming>())
        {
            var src = new JsonOptions { PropertyNaming = naming, WriteIndented = true, IgnoreNullValues = true };
            var copy = new JsonOptions(src);

            Assert.Equal(naming, copy.PropertyNaming);
            Assert.True(copy.WriteIndented);
            Assert.True(copy.IgnoreNullValues);
        }
    }

    #endregion

    #region FastJson 命名策略测试

    [Theory(DisplayName = "FastJson-None 不转换属性名")]
    [InlineData("FirstName")]
    [InlineData("LastName")]
    [InlineData("UserAge")]
    public void FastJson_None_KeyNotTransformed(String expectedKey)
    {
        var model = new NamingModel { FirstName = "Zhang", LastName = "San", UserAge = 30 };
        var json = new FastJson().Write(model, new JsonOptions { PropertyNaming = PropertyNaming.None });
        Assert.Contains($"\"{expectedKey}\"", json);
    }

    [Theory(DisplayName = "FastJson-CamelCase 驼峰命名")]
    [InlineData("firstName")]
    [InlineData("lastName")]
    [InlineData("userAge")]
    public void FastJson_CamelCase_ConvertsToCamel(String expectedKey)
    {
        var model = new NamingModel { FirstName = "Zhang", LastName = "San", UserAge = 30 };
        var json = new FastJson().Write(model, new JsonOptions { PropertyNaming = PropertyNaming.CamelCase });
        Assert.Contains($"\"{expectedKey}\"", json);
    }

    [Fact(DisplayName = "FastJson-CamelCase 特殊处理 ID")]
    public void FastJson_CamelCase_IdProperty_ConvertedToLowerId()
    {
        var model = new IdModel { ID = 42, UserName = "test" };
        var json = new FastJson().Write(model, new JsonOptions { PropertyNaming = PropertyNaming.CamelCase });
        Assert.Contains("\"id\"", json);
        Assert.Contains("\"userName\"", json);
    }

    [Theory(DisplayName = "FastJson-KebabCaseLower 小写 kebab 命名")]
    [InlineData("first-name")]
    [InlineData("last-name")]
    [InlineData("user-age")]
    public void FastJson_KebabCaseLower_ConvertsToKebabLower(String expectedKey)
    {
        var model = new NamingModel { FirstName = "Zhang", LastName = "San", UserAge = 30 };
        var json = new FastJson().Write(model, new JsonOptions { PropertyNaming = PropertyNaming.KebabCaseLower });
        Assert.Contains($"\"{expectedKey}\"", json);
    }

    [Theory(DisplayName = "FastJson-KebabCaseUpper 大写 kebab 命名")]
    [InlineData("FIRST-NAME")]
    [InlineData("LAST-NAME")]
    [InlineData("USER-AGE")]
    public void FastJson_KebabCaseUpper_ConvertsToKebabUpper(String expectedKey)
    {
        var model = new NamingModel { FirstName = "Zhang", LastName = "San", UserAge = 30 };
        var json = new FastJson().Write(model, new JsonOptions { PropertyNaming = PropertyNaming.KebabCaseUpper });
        Assert.Contains($"\"{expectedKey}\"", json);
    }

    [Theory(DisplayName = "FastJson-SnakeCaseLower 小写 snake 命名")]
    [InlineData("first_name")]
    [InlineData("last_name")]
    [InlineData("user_age")]
    public void FastJson_SnakeCaseLower_ConvertsToSnakeLower(String expectedKey)
    {
        var model = new NamingModel { FirstName = "Zhang", LastName = "San", UserAge = 30 };
        var json = new FastJson().Write(model, new JsonOptions { PropertyNaming = PropertyNaming.SnakeCaseLower });
        Assert.Contains($"\"{expectedKey}\"", json);
    }

    [Theory(DisplayName = "FastJson-SnakeCaseUpper 大写 snake 命名")]
    [InlineData("FIRST_NAME")]
    [InlineData("LAST_NAME")]
    [InlineData("USER_AGE")]
    public void FastJson_SnakeCaseUpper_ConvertsToSnakeUpper(String expectedKey)
    {
        var model = new NamingModel { FirstName = "Zhang", LastName = "San", UserAge = 30 };
        var json = new FastJson().Write(model, new JsonOptions { PropertyNaming = PropertyNaming.SnakeCaseUpper });
        Assert.Contains($"\"{expectedKey}\"", json);
    }

    [Fact(DisplayName = "FastJson 向后兼容：CamelCase=true 等效于 PropertyNaming.CamelCase")]
    public void FastJson_BackwardCompat_CamelCaseTrue_EqualsCamelCaseNaming()
    {
        var model = new NamingModel { FirstName = "Zhang", LastName = "San", UserAge = 30 };
        var fj = new FastJson();

        // 旧方式
        var jsonOld = fj.Write(model, indented: false, nullValue: true, camelCase: true);
        // 新方式
        var jsonNew = fj.Write(model, new JsonOptions { PropertyNaming = PropertyNaming.CamelCase });

        Assert.Equal(jsonOld, jsonNew);
    }

    [Fact(DisplayName = "FastJson 各策略之间输出互不相同")]
    public void FastJson_AllPolicies_ProduceDifferentOutput()
    {
        var model = new NamingModel { FirstName = "Zhang", LastName = "San", UserAge = 30 };
        var fj = new FastJson();

        var results = Enum.GetValues<PropertyNaming>()
            .Select(p => fj.Write(model, new JsonOptions { PropertyNaming = p }))
            .ToArray();

        // None 与 CamelCase/kebab/snake 不同
        Assert.NotEqual(results[(Int32)PropertyNaming.None], results[(Int32)PropertyNaming.CamelCase]);
        Assert.NotEqual(results[(Int32)PropertyNaming.None], results[(Int32)PropertyNaming.KebabCaseLower]);
        Assert.NotEqual(results[(Int32)PropertyNaming.None], results[(Int32)PropertyNaming.SnakeCaseLower]);
        // 大小写版本互不相同
        Assert.NotEqual(results[(Int32)PropertyNaming.KebabCaseLower], results[(Int32)PropertyNaming.KebabCaseUpper]);
        Assert.NotEqual(results[(Int32)PropertyNaming.SnakeCaseLower], results[(Int32)PropertyNaming.SnakeCaseUpper]);
    }

    #endregion

    #region SystemJson 命名策略测试

    [Theory(DisplayName = "SystemJson-None 不转换属性名")]
    [InlineData("FirstName")]
    [InlineData("LastName")]
    [InlineData("UserAge")]
    public void SystemJson_None_KeyNotTransformed(String expectedKey)
    {
        var model = new NamingModel { FirstName = "Zhang", LastName = "San", UserAge = 30 };
        var json = new SystemJson().Write(model, new JsonOptions { PropertyNaming = PropertyNaming.None });
        Assert.Contains($"\"{expectedKey}\"", json);
    }

    [Theory(DisplayName = "SystemJson-CamelCase 驼峰命名")]
    [InlineData("firstName")]
    [InlineData("lastName")]
    [InlineData("userAge")]
    public void SystemJson_CamelCase_ConvertsToCamel(String expectedKey)
    {
        var model = new NamingModel { FirstName = "Zhang", LastName = "San", UserAge = 30 };
        var json = new SystemJson().Write(model, new JsonOptions { PropertyNaming = PropertyNaming.CamelCase });
        Assert.Contains($"\"{expectedKey}\"", json);
    }

    [Theory(DisplayName = "SystemJson-KebabCaseLower 小写 kebab 命名")]
    [InlineData("first-name")]
    [InlineData("last-name")]
    [InlineData("user-age")]
    public void SystemJson_KebabCaseLower_ConvertsToKebabLower(String expectedKey)
    {
        var model = new NamingModel { FirstName = "Zhang", LastName = "San", UserAge = 30 };
        var json = new SystemJson().Write(model, new JsonOptions { PropertyNaming = PropertyNaming.KebabCaseLower });
        Assert.Contains($"\"{expectedKey}\"", json);
    }

    [Theory(DisplayName = "SystemJson-KebabCaseUpper 大写 kebab 命名")]
    [InlineData("FIRST-NAME")]
    [InlineData("LAST-NAME")]
    [InlineData("USER-AGE")]
    public void SystemJson_KebabCaseUpper_ConvertsToKebabUpper(String expectedKey)
    {
        var model = new NamingModel { FirstName = "Zhang", LastName = "San", UserAge = 30 };
        var json = new SystemJson().Write(model, new JsonOptions { PropertyNaming = PropertyNaming.KebabCaseUpper });
        Assert.Contains($"\"{expectedKey}\"", json);
    }

    [Theory(DisplayName = "SystemJson-SnakeCaseLower 小写 snake 命名")]
    [InlineData("first_name")]
    [InlineData("last_name")]
    [InlineData("user_age")]
    public void SystemJson_SnakeCaseLower_ConvertsToSnakeLower(String expectedKey)
    {
        var model = new NamingModel { FirstName = "Zhang", LastName = "San", UserAge = 30 };
        var json = new SystemJson().Write(model, new JsonOptions { PropertyNaming = PropertyNaming.SnakeCaseLower });
        Assert.Contains($"\"{expectedKey}\"", json);
    }

    [Theory(DisplayName = "SystemJson-SnakeCaseUpper 大写 snake 命名")]
    [InlineData("FIRST_NAME")]
    [InlineData("LAST_NAME")]
    [InlineData("USER_AGE")]
    public void SystemJson_SnakeCaseUpper_ConvertsToSnakeUpper(String expectedKey)
    {
        var model = new NamingModel { FirstName = "Zhang", LastName = "San", UserAge = 30 };
        var json = new SystemJson().Write(model, new JsonOptions { PropertyNaming = PropertyNaming.SnakeCaseUpper });
        Assert.Contains($"\"{expectedKey}\"", json);
    }

    [Fact(DisplayName = "SystemJson 向后兼容：CamelCase=true 等效于 PropertyNaming.CamelCase")]
    public void SystemJson_BackwardCompat_CamelCaseTrue_EqualsCamelCaseNaming()
    {
        var model = new NamingModel { FirstName = "Zhang", LastName = "San", UserAge = 30 };
        var sj = new SystemJson();

        // 旧方式
        var jsonOld = sj.Write(model, indented: false, nullValue: true, camelCase: true);
        // 新方式
        var jsonNew = sj.Write(model, new JsonOptions { PropertyNaming = PropertyNaming.CamelCase });

        Assert.Equal(jsonOld, jsonNew);
    }

    [Fact(DisplayName = "SystemJson 各策略之间输出互不相同")]
    public void SystemJson_AllPolicies_ProduceDifferentOutput()
    {
        var model = new NamingModel { FirstName = "Zhang", LastName = "San", UserAge = 30 };
        var sj = new SystemJson();

        var results = Enum.GetValues<PropertyNaming>()
            .Select(p => sj.Write(model, new JsonOptions { PropertyNaming = p }))
            .ToArray();

        Assert.NotEqual(results[(Int32)PropertyNaming.None], results[(Int32)PropertyNaming.CamelCase]);
        Assert.NotEqual(results[(Int32)PropertyNaming.None], results[(Int32)PropertyNaming.KebabCaseLower]);
        Assert.NotEqual(results[(Int32)PropertyNaming.None], results[(Int32)PropertyNaming.SnakeCaseLower]);
        Assert.NotEqual(results[(Int32)PropertyNaming.KebabCaseLower], results[(Int32)PropertyNaming.KebabCaseUpper]);
        Assert.NotEqual(results[(Int32)PropertyNaming.SnakeCaseLower], results[(Int32)PropertyNaming.SnakeCaseUpper]);
    }

    #endregion

    #region FastJson 与 SystemJson 一致性测试

    [Theory(DisplayName = "FastJson 与 SystemJson 对所有策略的属性键名一致")]
    [InlineData(PropertyNaming.None)]
    [InlineData(PropertyNaming.CamelCase)]
    [InlineData(PropertyNaming.KebabCaseLower)]
    [InlineData(PropertyNaming.KebabCaseUpper)]
    [InlineData(PropertyNaming.SnakeCaseLower)]
    [InlineData(PropertyNaming.SnakeCaseUpper)]
    public void FastJson_And_SystemJson_ProduceSameKeys(PropertyNaming policy)
    {
        var model = new NamingModel { FirstName = "Zhang", LastName = "San", UserAge = 30 };
        var options = new JsonOptions { PropertyNaming = policy };

        var fastJson = new FastJson().Write(model, options);
        var sysJson = new SystemJson().Write(model, options);

        // 解析两者的 key
        var fastKeys = JsonParser.Decode(fastJson)?.Keys.Order().ToArray();
        var sysKeys = JsonParser.Decode(sysJson)?.Keys.Order().ToArray();

        Assert.NotNull(fastKeys);
        Assert.NotNull(sysKeys);
        Assert.Equal(fastKeys, sysKeys);
    }

    #endregion
}
