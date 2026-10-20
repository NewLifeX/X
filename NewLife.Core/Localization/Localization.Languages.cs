using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;

namespace NewLife;

partial class Localization
{
    #region 属性
    private static readonly LanguageInfo[] _builtin =
    [
        new("zh-CN", "简体中文", "Chinese (Simplified)"),
        new("zh-TW", "繁體中文", "Chinese (Traditional)"),
        new("en-US", "English", "English"),
        new("es-ES", "Español", "Spanish"),
        new("de-DE", "Deutsch", "German"),
        new("ja-JP", "日本語", "Japanese"),
        new("fr-FR", "Français", "French"),
        new("pt-BR", "Português", "Portuguese"),
        new("ru-RU", "Русский", "Russian"),
        new("ko-KR", "한국어", "Korean"),
        new("ar-SA", "العربية", "Arabic"),
        new("bn-BD", "বাংলা", "Bengali"),
        new("cs-CZ", "Čeština", "Czech"),
        new("da-DK", "Dansk", "Danish"),
        new("el-GR", "Ελληνικά", "Greek"),
        new("fa-IR", "فارسی", "Persian"),
        new("fi-FI", "Suomi", "Finnish"),
        new("he-IL", "עברית", "Hebrew"),
        new("hi-IN", "हिन्दी", "Hindi"),
        new("hu-HU", "Magyar", "Hungarian"),
        new("id-ID", "Bahasa Indonesia", "Indonesian"),
        new("it-IT", "Italiano", "Italian"),
        new("ms-MY", "Bahasa Melayu", "Malay"),
        new("nb-NO", "Norsk bokmål", "Norwegian"),
        new("nl-NL", "Nederlands", "Dutch"),
        new("pl-PL", "Polski", "Polish"),
        new("ro-RO", "Română", "Romanian"),
        new("sv-SE", "Svenska", "Swedish"),
        new("th-TH", "ไทย", "Thai"),
        new("tr-TR", "Türkçe", "Turkish"),
        new("uk-UA", "Українська", "Ukrainian"),
        new("vi-VN", "Tiếng Việt", "Vietnamese"),
    ];

    /// <summary>Core 内置的界面语言</summary>
    public static String[] SupportedCultures { get; } = GetNames(_builtin);

    private static readonly ConcurrentDictionary<String, LanguageInfo> _extraLanguages = new(StringComparer.Ordinal);

    private static readonly CultureInfo _SystemCulture = CultureInfo.CurrentUICulture;

    /// <summary>已由配置强制的界面语言。为空表示自动识别</summary>
    public static String? ForcedLanguage { get; private set; }
    #endregion

    #region 语言清单
    /// <summary>获取受支持的界面语言清单</summary>
    public static IList<LanguageInfo> GetLanguages()
    {
        var list = new List<LanguageInfo>(_builtin.Length + _extraLanguages.Count);
        foreach (var item in _builtin) list.Add(item);

        if (!_extraLanguages.IsEmpty)
        {
            var extras = new List<LanguageInfo>(_extraLanguages.Count);
            foreach (var item in _extraLanguages) extras.Add(item.Value);

            extras.Sort((x, y) => StringComparer.Ordinal.Compare(x.Name, y.Name));
            list.AddRange(extras);
        }

        return list;
    }

    private static Boolean IsBuiltinLanguage(String name)
    {
        var key = GetCultureKeyOf(name);
        if (key == null) return false;

        return key == "en" || GetTable(key) != null;
    }

    private static String[] GetNames(LanguageInfo[] languages)
    {
        var names = new String[languages.Length];
        for (var i = 0; i < languages.Length; i++) names[i] = languages[i].Name;

        return names;
    }
    #endregion

    #region 方法
    /// <summary>归一化语言名称，无法匹配时返回 null</summary>
    public static String? NormalizeCulture(String? name)
    {
        if (name.IsNullOrWhiteSpace()) return null;

        // 取首个候选并去掉权重
        var first = name.Split(',')[0].Trim();
        var p = first.IndexOf(';');
        if (p > 0) first = first.Substring(0, p).Trim();
        if (first.IsNullOrEmpty()) return null;

        // 中文按脚本/地区归一，zh-Hant、zh-TW、zh-HK、zh-MO 均为繁体
        if (first.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
        {
            var hant = first.IndexOf("Hant", StringComparison.OrdinalIgnoreCase) >= 0
                || first.EndsWith("-TW", StringComparison.OrdinalIgnoreCase)
                || first.EndsWith("-HK", StringComparison.OrdinalIgnoreCase)
                || first.EndsWith("-MO", StringComparison.OrdinalIgnoreCase);

            return hant ? "zh-TW" : "zh-CN";
        }

        // 精确匹配内置语言
        foreach (var item in SupportedCultures)
        {
            if (item.EqualIgnoreCase(first)) return item;
        }

        // 精确匹配运行时注册的语言，优先于内置语言的语言前缀匹配
        foreach (var item in _extraLanguages)
        {
            if (item.Value.Name.EqualIgnoreCase(first)) return item.Value.Name;
        }

        // 语言前缀匹配：en → en-US，de-AT → de-DE，ja → ja-JP
        var lang = first;
        var dash = first.IndexOf('-');
        if (dash > 0) lang = first.Substring(0, dash);
        foreach (var item in SupportedCultures)
        {
            if (item.StartsWith(lang + "-", StringComparison.OrdinalIgnoreCase)) return item;
        }
        foreach (var item in _extraLanguages)
        {
            if (item.Value.Name.StartsWith(lang + "-", StringComparison.OrdinalIgnoreCase)) return item.Value.Name;
        }

        // 运行时注册的中性语言（如 ta）承接同语言的区域请求（ta-IN）
        foreach (var item in _extraLanguages)
        {
            if (String.Equals(GetCultureKeyOf(item.Value.Name), lang, StringComparison.OrdinalIgnoreCase)) return item.Value.Name;
        }

        return null;
    }

    /// <summary>设置界面与日志语言。留空表示跟随系统自动识别</summary>
    public static String? SetLanguage(String? language)
    {
        var name = NormalizeCulture(language);
        CultureInfo? culture = null;
        if (name != null)
        {
            try
            {
                culture = new CultureInfo(name);
            }
            catch (Exception ex) when (ex is CultureNotFoundException || ex is ArgumentException)
            {
                // 不变全球化（InvariantGlobalization=true）下具名区域性不可用，保持自动模式
                name = null;
            }
        }

        Culture = culture;
        LogCulture = culture ?? _SystemCulture;
        ForcedLanguage = name;

        return name;
    }
    #endregion
}
