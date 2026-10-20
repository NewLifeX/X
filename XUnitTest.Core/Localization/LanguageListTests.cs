using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NewLife;
using NewLife.Configuration;
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

    private static String[] GetLanguageCodes(String text) => Regex.Matches(text, @"\b[a-z]{2}-[A-Za-z]{2,4}\b")
        .Select(m => m.Value)
        .OrderBy(m => m, StringComparer.Ordinal)
        .ToArray();

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

        var prev = CultureInfo.CurrentUICulture;
        try
        {
            // 名称本地化跟随当前界面语言（.NET 区域数据），显式传入的显示语言在非当前界面语言时走词表
            CultureInfo.CurrentUICulture = enCulture;

            Assert.Equal("Chinese (Simplified)", zh.EnglishName);
            Assert.Equal("简体中文", zh.NativeName);
            Assert.Equal("Chinese (Simplified)", zh.GetName(enCulture));
            Assert.Equal("Chinese (Simplified) (简体中文)", zh.GetDisplayName(enCulture));
            Assert.Equal("简体中文", zh.GetName(zhCulture));
            Assert.Equal("简体中文", zh.GetDisplayName(zhCulture));

            Assert.Equal("English", en.GetDisplayName(enCulture));

            // 同属中文的繁体在简体界面直接显示自称
            var tw = Find(list, "zh-TW")!;
            Assert.Equal("Chinese (Traditional) (繁體中文)", tw.GetDisplayName(enCulture));
            Assert.Equal("繁體中文", tw.GetDisplayName(zhCulture));

            // 当前界面语言为中文时，语言名称交给 .NET 区域数据，无需词表
            CultureInfo.CurrentUICulture = zhCulture;

            Assert.Equal("英语 (English)", en.GetDisplayName(zhCulture));
            Assert.Equal("德语 (Deutsch)", Find(list, "de-DE")!.GetDisplayName(zhCulture));
        }
        finally
        {
            CultureInfo.CurrentUICulture = prev;
        }
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

    [Fact(DisplayName = "语言清单：旧版别名不重复出现")]
    public void LegacyAlias()
    {
        if (!CultureAvailable("in")) return;

        var culture = CultureInfo.GetCultureInfo("in");
        try
        {
            Localization.Register(culture, new Dictionary<String, String> { [Key] = "Uji {0}." });

            // in/iw/no/mo 是 id/he/nb/ro 的旧名，归一化后与内置区域语言重合，不应单独出现在清单里
            Assert.Null(Find(Localization.GetLanguages(), "in"));
            Assert.Equal("id-ID", Localization.NormalizeCulture("in"));
            Assert.Equal("he-IL", Localization.NormalizeCulture("iw"));
            Assert.Equal("nb-NO", Localization.NormalizeCulture("no"));
            Assert.Equal("ro-RO", Localization.NormalizeCulture("mo"));
        }
        finally
        {
            Localization.Unregister(culture);
        }
    }

    [Fact(DisplayName = "语言选项：键为语言代码，显示名随界面语言本地化")]
    public void Options()
    {
        var prev = Localization.Culture;
        var prevUI = CultureInfo.CurrentUICulture;
        try
        {
            Localization.Culture = CultureInfo.GetCultureInfo("zh-CN");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("zh-CN");

            var options = Localization.GetLanguageOptions();

            Assert.True(options.Count >= Localization.SupportedCultures.Length);
            Assert.Equal("简体中文", options["zh-CN"]);
            Assert.Equal("英语 (English)", options["en-US"]);
            Assert.Equal("德语 (Deutsch)", options["de-DE"]);
            Assert.Equal("日语 (日本語)", options["ja-JP"]);
        }
        finally
        {
            Localization.Culture = prev;
            CultureInfo.CurrentUICulture = prevUI;
        }
    }

    [Fact(DisplayName = "界面语言：Description 与内置语言清单一致")]
    public void DescriptionListsSupportedCultures()
    {
        var property = typeof(Setting).GetProperty(nameof(Setting.Language))!;
        var description = property.GetCustomAttribute<DescriptionAttribute>()?.Description;
        Assert.False(description.IsNullOrEmpty());

        var expected = Localization.SupportedCultures.OrderBy(m => m, StringComparer.Ordinal).ToArray();
        Assert.Equal(expected, GetLanguageCodes(description!));
    }

    [Fact(DisplayName = "配置注释：界面语言列出全部内置语言码")]
    public void ConfigCommentListsSupportedCultures()
    {
        var root = new ConfigSection { Key = "Root", Childs = [] };
        root.MapFrom(new Setting());
        var comment = root.Find("Language")?.Comment;
        Assert.False(comment.IsNullOrEmpty());

        var expected = Localization.SupportedCultures.OrderBy(m => m, StringComparer.Ordinal).ToArray();
        Assert.Equal(expected, GetLanguageCodes(comment!));

        var prev = Localization.LogCulture;
        try
        {
            Localization.LogCulture = CultureInfo.GetCultureInfo("zh-CN");
            Assert.Contains("界面语言", comment!.LocalizeComment());
        }
        finally
        {
            Localization.LogCulture = prev;
        }
    }
}
