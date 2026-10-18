using System;
using System.Globalization;
using NewLife;
using Xunit;

namespace XUnitTest.Downstream;

/// <summary>下游组件共享词表的回归测试。本测试程序集自带 Locales\ta.yml，等价于一个下游 NewLife.* 组件</summary>
/// <remarks>用例会临时改写 <see cref="Localization.Culture"/>，与其它用例串行执行</remarks>
[Collection("GlobalState")]
public class DownstreamLocalizationTests
{
    private const String Key = "Downstream probe message: {0}.";
    private const String Tamil = "கீழ்நிலை ஆய்வுச் செய்தி: {0}.";

    [Fact(DisplayName = "下游程序集：自带语言表在模块初始化时自动注册")]
    public void AutoRegistered()
    {
        var saved = Localization.Culture;
        try
        {
            Localization.Culture = CultureInfo.GetCultureInfo("ta-IN");
            Assert.Equal(Tamil, Localization.Translate(Key));
        }
        finally
        {
            Localization.Culture = saved;
        }
    }

    [Fact(DisplayName = "下游程序集：L.T 与 Translate 结果一致且支持格式化")]
    public void ShortcutMatches()
    {
        var saved = Localization.Culture;
        try
        {
            Localization.Culture = CultureInfo.GetCultureInfo("ta-IN");
            Assert.Equal(Localization.Translate(Key, 7), L.T(Key, 7));
            Assert.Equal("கீழ்நிலை ஆய்வுச் செய்தி: 7.", L.T(Key, 7));
            Assert.Equal("கீழ்நிலை ஆய்வு, அளவுரு இல்லை.", L.T("Downstream probe without arguments."));
        }
        finally
        {
            Localization.Culture = saved;
        }
    }

    [Fact(DisplayName = "下游程序集：未注册语言仍回退英文原文")]
    public void Fallback()
    {
        Assert.Equal(Key, Localization.Translate(CultureInfo.GetCultureInfo("en-US"), Key));
        Assert.Equal(Key, Localization.Translate(CultureInfo.GetCultureInfo("sw-KE"), Key));
    }
}
