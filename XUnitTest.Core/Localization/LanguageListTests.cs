using System;
using System.Collections.Generic;
using System.Globalization;
using NewLife;
using Xunit;

namespace XUnitTest.Common;

/// <summary>语言清单与显示名称测试。用例会临时注册运行时语言，放入串行集合</summary>
[Collection("GlobalState")]
public class LanguageListTests
{
    private const String Key = "Language list probe: {0}.";

    private static LanguageInfo? Find(IList<LanguageInfo> list, String name)
    {
        foreach (var item in list)
        {
            if (item.Name.EqualIgnoreCase(name)) return item;
        }

        return null;
    }

    private static Boolean CultureAvailable(String name)
    {
        try
        {
            _ = CultureInfo.GetCultureInfo(name);
            return true;
        }
        catch (Exception ex) when (ex is CultureNotFoundException || ex is ArgumentException)
        {
            return false;
        }
    }

    [Fact(DisplayName = "语言清单：内置语言全部在列且顺序固定")]
    public void Builtin()
    {
        var list = Localization.GetLanguages();

        Assert.True(list.Count >= Localization.SupportedCultures.Length);
        Assert.Equal("zh-CN", list[0].Name);
        Assert.Equal("zh-TW", list[1].Name);
        Assert.Equal("en-US", list[2].Name);

        for (var i = 0; i < Localization.SupportedCultures.Length; i++)
        {
            Assert.Equal(Localization.SupportedCultures[i], list[i].Name);
        }
    }

    [Fact(DisplayName = "语言清单：每项都带键、自称与英文名")]
    public void Names()
    {
        foreach (var item in Localization.GetLanguages())
        {
            Assert.False(item.Name.IsNullOrEmpty());
            Assert.False(item.NativeName.IsNullOrEmpty());
            Assert.False(item.EnglishName.IsNullOrEmpty());
        }
    }

    [Fact(DisplayName = "语言名称：英文界面同时显示英文名与自称，中文界面显示自称")]
    public void DisplayName()
    {
        var list = Localization.GetLanguages();
        var zh = Find(list, "zh-CN")!;
        var en = Find(list, "en-US")!;
        var enCulture = CultureInfo.GetCultureInfo("en-US");
        var zhCulture = CultureInfo.GetCultureInfo("zh-CN");

        Assert.Equal("Chinese (Simplified)", zh.EnglishName);
        Assert.Equal("简体中文", zh.NativeName);
        Assert.Equal("Chinese (Simplified)", zh.GetName(enCulture));
        Assert.Equal("Chinese (Simplified) (简体中文)", zh.GetDisplayName(enCulture));
        Assert.Equal("简体中文", zh.GetName(zhCulture));
        Assert.Equal("简体中文", zh.GetDisplayName(zhCulture));

        Assert.Equal("English", en.GetDisplayName(enCulture));
        Assert.Equal("English", en.GetDisplayName(zhCulture));

        // 同属中文的繁体在简体界面直接显示自称
        var tw = Find(list, "zh-TW")!;
        Assert.Equal("Chinese (Traditional) (繁體中文)", tw.GetDisplayName(enCulture));
        Assert.Equal("繁體中文", tw.GetDisplayName(zhCulture));
    }

    [Fact(DisplayName = "语言名称：词表提供译文时按目标语言显示")]
    public void LocalizedName()
    {
        var zh = CultureInfo.GetCultureInfo("zh-CN");
        var de = Find(Localization.GetLanguages(), "de-DE")!;
        try
        {
            // 语言名称就是普通词条，以英文名称为键登记译文即可本地化
            Localization.Register(zh, new Dictionary<String, String> { [de.EnglishName] = "德语" }, true);

            Assert.Equal("德语", de.GetName(zh));
            Assert.Equal("德语 (Deutsch)", de.GetDisplayName(zh));
        }
        finally
        {
            Localization.Unregister(zh);
        }
    }

    [Fact(DisplayName = "语言清单：运行时注册的语言自动出现，注销后移除")]
    public void Dynamic()
    {
        // 测试程序集自带的 ta.yml 在模块初始化时注册，等价于下游动态新增语言
        if (CultureAvailable("ta")) Assert.NotNull(Find(Localization.GetLanguages(), "ta"));

        var culture = CultureInfo.GetCultureInfo("af-ZA");
        try
        {
            Assert.Null(Find(Localization.GetLanguages(), "af-ZA"));

            Localization.Register(culture, new Dictionary<String, String> { [Key] = "آزمون {0}." });

            var item = Find(Localization.GetLanguages(), "af-ZA");
            Assert.NotNull(item);
            Assert.Equal(culture.NativeName, item.NativeName);
            Assert.Equal("af-ZA", Localization.NormalizeCulture("af-ZA"));
            Assert.Equal("af-ZA", Localization.NormalizeCulture("af"));

            Localization.Unregister(culture);

            Assert.Null(Find(Localization.GetLanguages(), "af-ZA"));
            Assert.Null(Localization.NormalizeCulture("af-ZA"));
        }
        finally
        {
            Localization.Unregister(culture);
        }
    }

}
