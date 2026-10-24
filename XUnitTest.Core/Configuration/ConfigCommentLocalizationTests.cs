using System.ComponentModel;
using System.Globalization;
using NewLife;
using NewLife.Configuration;
using Xunit;

namespace XUnitTest.Configuration;

/// <summary>配置注释本地化测试</summary>
/// <remarks>英文源文注释在保存 Xml/Ini 配置时按显式开发者语言翻译；未显式设置时保持英文，不跟随 CurrentUICulture。</remarks>
[Collection("GlobalState")]
public class ConfigCommentLocalizationTests
{
    private const String Key = "Localized config comment";
    private const String Text = "本地化配置注释";
    private static readonly CultureInfo _sw = CultureInfo.GetCultureInfo("sw");
    private static readonly CultureInfo _zh = CultureInfo.GetCultureInfo("zh-Hans");

    private class CommentModel
    {
        [Description(Key)]
        public String Value { get; set; } = "1";
    }

    private class FallbackModel
    {
        [Description("Untranslated config comment")]
        public String Value { get; set; } = "1";
    }

    private static void WithCulture(Action action) => WithCultures(_sw, _sw, action);

    private static void WithCultures(CultureInfo? culture, CultureInfo? logCulture, Action action)
    {
        Localization.Register(_sw, new Dictionary<String, String> { [Key] = Text }, overwrite: true);
        Localization.Register(_zh, new Dictionary<String, String> { [Key] = Text }, overwrite: true);
        var prevCulture = Localization.Culture;
        var prevLogCulture = Localization.LogCulture;
        try
        {
            Localization.Culture = culture;
            Localization.LogCulture = logCulture;
            action();
        }
        finally
        {
            Localization.Culture = prevCulture;
            Localization.LogCulture = prevLogCulture;
            Localization.Unregister(_sw);
            Localization.Unregister(_zh);
        }
    }

    [Fact]
    public void Xml_Save_WritesLocalizedComment()
    {
        var file = Path.GetTempFileName();
        try
        {
            WithCulture(() =>
            {
                var prv = new XmlConfigProvider { FileName = file };
                prv.Save(new CommentModel());

                var xml = File.ReadAllText(file);
                Assert.Contains($"<!--{Text}-->", xml);
            });
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void Ini_Save_WritesLocalizedComment()
    {
        var file = Path.GetTempFileName();
        try
        {
            WithCulture(() =>
            {
                var prv = new IniConfigProvider { FileName = file };
                prv.Save(new CommentModel());

                var text = File.ReadAllText(file);
                Assert.Contains($"; {Text}", text);
            });
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void Xml_Save_NoExplicitCulture_KeepsEnglish()
    {
        var file = Path.GetTempFileName();
        var prev = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("zh-CN");
            WithCultures(null, null, () =>
            {
                var prv = new XmlConfigProvider { FileName = file };
                prv.Save(new CommentModel());

                // 未显式设置 LogCulture/Culture 时输出英文源文，即使 CurrentUICulture 是中文
                Assert.Contains($"<!--{Key}-->", File.ReadAllText(file));
            });
        }
        finally
        {
            CultureInfo.CurrentUICulture = prev;
            File.Delete(file);
        }
    }

    [Fact]
    public void Xml_Save_ExplicitCulture_FallsBackToCulture()
    {
        var file = Path.GetTempFileName();
        try
        {
            WithCultures(_sw, null, () =>
            {
                var prv = new XmlConfigProvider { FileName = file };
                prv.Save(new CommentModel());

                Assert.Contains($"<!--{Text}-->", File.ReadAllText(file));
            });
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void Xml_Save_LogCulture_OverridesCulture()
    {
        var file = Path.GetTempFileName();
        try
        {
            WithCultures(_sw, CultureInfo.GetCultureInfo("en"), () =>
            {
                var prv = new XmlConfigProvider { FileName = file };
                prv.Save(new CommentModel());

                // 显式 en 覆盖 Culture，配置注释保持英文源文
                Assert.Contains($"<!--{Key}-->", File.ReadAllText(file));
            });
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void Xml_Save_UnknownComment_KeepsEnglish()
    {
        var file = Path.GetTempFileName();
        try
        {
            WithCulture(() =>
            {
                var prv = new XmlConfigProvider { FileName = file };
                prv.Save(new FallbackModel());

                // 未注册翻译的键原样输出
                Assert.Contains("<!--Untranslated config comment-->", File.ReadAllText(file));
            });
        }
        finally
        {
            File.Delete(file);
        }
    }
}
