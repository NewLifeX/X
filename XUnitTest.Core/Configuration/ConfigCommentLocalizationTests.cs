using System.ComponentModel;
using System.Globalization;
using NewLife;
using NewLife.Configuration;
using Xunit;

namespace XUnitTest.Configuration;

/// <summary>配置注释本地化测试</summary>
/// <remarks>英文源文注释在保存 Xml/Ini 配置时按当前语言翻译；缺少译文时保持英文。</remarks>
[Collection("GlobalState")]
public class ConfigCommentLocalizationTests
{
    private const String Key = "Localized config comment";
    private const String Text = "本地化配置注释";
    private static readonly CultureInfo _sw = CultureInfo.GetCultureInfo("sw");

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

    private static void WithCulture(Action action)
    {
        Localization.Register(_sw, new Dictionary<String, String> { [Key] = Text }, overwrite: true);
        var prev = Localization.Culture;
        try
        {
            Localization.Culture = _sw;
            action();
        }
        finally
        {
            Localization.Culture = prev;
            Localization.Unregister(_sw);
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
