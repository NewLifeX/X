using System;
using NewLife;
using Xunit;

namespace XUnitTest.Common;

/// <summary>语言名称归一化测试</summary>
[Collection("GlobalState")]
public class LanguageSettingTests
{
    [Theory(DisplayName = "语言归一化：裸语言码、非支持区域与中文脚本")]
    [InlineData("en", "en-US")]
    [InlineData("en-GB", "en-US")]
    [InlineData("de-AT", "de-DE")]
    [InlineData("fr-CA;q=0.9", "fr-FR")]
    [InlineData("zh", "zh-CN")]
    [InlineData("zh-Hans", "zh-CN")]
    [InlineData("zh-Hant", "zh-TW")]
    [InlineData("zh-HK", "zh-TW")]
    [InlineData("ja", "ja-JP")]
    [InlineData("pt-PT", "pt-BR")]
    public void NormalizeCulture(String name, String expected) => Assert.Equal(expected, Localization.NormalizeCulture(name));

    [Theory(DisplayName = "语言归一化：空值与不支持语言返回空")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("sw-KE")]
    public void NormalizeCulture_None(String? name) => Assert.Null(Localization.NormalizeCulture(name));

    [Fact(DisplayName = "强制语言：设置后固定界面与日志语言，清空后恢复自动识别")]
    public void SetLanguage_FixAndReset()
    {
        var culture = Localization.Culture;
        var logCulture = Localization.LogCulture;
        var forced = Localization.ForcedLanguage;
        try
        {
            Assert.Equal("en-US", Localization.SetLanguage("en"));
            Assert.Equal("en-US", Localization.Culture?.Name);
            Assert.Equal("en-US", Localization.LogCulture?.Name);
            Assert.Equal("en-US", Localization.ForcedLanguage);

            // 不支持的语言不改变当前强制语言
            Assert.Null(Localization.SetLanguage("sw-KE"));
            Assert.Null(Localization.Culture);
            Assert.Null(Localization.ForcedLanguage);

            Assert.Equal("zh-TW", Localization.SetLanguage("zh-HK"));
            Assert.Equal("zh-TW", Localization.ForcedLanguage);

            // 清空配置即恢复自动识别
            Assert.Null(Localization.SetLanguage(null));
            Assert.Null(Localization.Culture);
            Assert.Null(Localization.LogCulture);
            Assert.Null(Localization.ForcedLanguage);
        }
        finally
        {
            // 先恢复强制语言标记，再还原界面与日志语言；顺序不可颠倒，避免 SetLanguage 覆盖待恢复的 LogCulture
            Localization.SetLanguage(forced);
            Localization.Culture = culture;
            Localization.LogCulture = logCulture;
        }
    }
}
