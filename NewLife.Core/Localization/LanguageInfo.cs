using System;
using System.Globalization;

namespace NewLife;

/// <summary>语言信息。描述一种可切换的界面语言</summary>
/// <remarks>
/// 提供三种口径：<see cref="Name"/> 是配置键（如 zh-CN），<see cref="NativeName"/> 是该语言的自称（如简体中文），
/// <see cref="EnglishName"/> 是英文名称（如 Chinese (Simplified)）。
/// 管理端可用 <see cref="GetDisplayName"/> 生成语言下拉框：英文页面显示 “Chinese (Simplified) (简体中文)”，
/// 中文页面显示 “简体中文”，不熟悉英文的用户也能靠自称找到自己的语言。
/// 下游可在自己的 Locales 词表中以英文名称为键登记译文，使 <see cref="GetName"/> 返回更贴近目标语言的名称。
/// </remarks>
/// <example>
/// <code>
/// // 生成语言下拉框：value 写入 Setting.Language，text 供用户选择
/// foreach (var lang in Localization.GetLanguages())
/// {
///     Console.WriteLine($"{lang.Name}\t{lang.GetDisplayName()}");
/// }
/// </code>
/// </example>
public sealed class LanguageInfo
{
    #region 属性
    /// <summary>语言键。如 zh-CN、en-US，可直接写入 Setting.Language</summary>
    public String Name { get; }

    /// <summary>该语言的自称。如简体中文、Deutsch</summary>
    public String NativeName { get; }

    /// <summary>该语言的英文名称。如 Chinese (Simplified)、German</summary>
    public String EnglishName { get; }
    #endregion

    #region 构造
    /// <summary>实例化语言信息</summary>
    /// <param name="name">语言键，如 zh-CN</param>
    /// <param name="nativeName">该语言的自称，如简体中文</param>
    /// <param name="englishName">该语言的英文名称，如 Chinese (Simplified)</param>
    internal LanguageInfo(String name, String nativeName, String englishName)
    {
        Name = name;
        NativeName = nativeName.IsNullOrEmpty() ? name : nativeName;
        EnglishName = englishName.IsNullOrEmpty() ? NativeName : englishName;
    }
    #endregion

    #region 方法
    /// <summary>获取该语言在指定语言下的名称</summary>
    public String GetName(CultureInfo? culture = null)
    {
        culture ??= Localization.Culture ?? CultureInfo.CurrentUICulture;

        // 目标语言与该语言同属一种语言（如 zh-CN 与 zh-TW），直接使用自称
        if (GetLanguage(culture.Name) == GetLanguage(Name)) return NativeName;

        // 取中性语言名称，避免“英语（美国）”这类冗长后缀
        if (culture.Name.EqualIgnoreCase(CultureInfo.CurrentUICulture.Name) && TryGetNativeDisplayName(Name, out var named) && !named.IsNullOrEmpty()) return named;

        return Localization.Translate(culture, EnglishName);
    }

    /// <summary>获取界面显示名称</summary>
    /// <param name="culture">显示语言。为空时跟随当前界面语言</param>
    /// <returns>本地名称，必要时附加自称，如 “Chinese (Simplified) (简体中文)”</returns>
    public String GetDisplayName(CultureInfo? culture = null)
    {
        var name = GetName(culture);
        return name == NativeName ? name : $"{name} ({NativeName})";
    }

    /// <summary>返回界面显示名称</summary>
    /// <returns>当前界面语言下的显示名称</returns>
    public override String ToString() => GetDisplayName();
    #endregion

    #region 辅助
    /// <summary>提取语言主代码，如 zh-CN → zh</summary>
    /// <param name="name">语言名称</param>
    /// <returns>小写语言主代码</returns>
    private static String GetLanguage(String name)
    {
        var index = name.IndexOf('-');
        return index > 0 ? name.Substring(0, index).ToLowerInvariant() : name.ToLowerInvariant();
    }

    private static Boolean TryGetNativeDisplayName(String name, out String? display)
    {
        display = null;
        try
        {
            var culture = CultureInfo.GetCultureInfo(name);
            var target = culture.Parent;
            if (target == null || target.Name.IsNullOrEmpty()) target = culture;
            if (target.Name.IsNullOrEmpty()) return false;

            display = target.DisplayName;
            return !display.IsNullOrEmpty() && display != target.Name;
        }
        catch (Exception ex) when (ex is CultureNotFoundException || ex is ArgumentException || ex is InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>按区域文化创建语言信息</summary>
    /// <param name="culture">区域文化</param>
    /// <returns>语言信息；名称缺失或与语言键相同时回退语言键</returns>
    internal static LanguageInfo FromCulture(CultureInfo culture)
    {
        var name = culture.Name;
        var native = name;
        var english = name;
        try
        {
            native = culture.NativeName;
            english = culture.EnglishName;
        }
        catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
        {
            // 自定义区域文化可能缺少名称数据，回退语言键
        }

        if (native.IsNullOrEmpty() || native == name) native = name;
        if (english.IsNullOrEmpty() || english == name) english = native;

        return new LanguageInfo(name, native, english);
    }
    #endregion
}
