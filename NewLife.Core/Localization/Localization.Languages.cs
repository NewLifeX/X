using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;

namespace NewLife;

partial class Localization
{
    #region 属性
    /// <summary>内置界面语言。前三位固定为简体中文、繁体中文、英文（源文），其余与 Locales 词表集对齐</summary>
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

    /// <summary>Core 内置的界面语言。前三位固定为简体中文、繁体中文、英文（源文），其余与 Locales 词表集对齐</summary>
    /// <remarks>
    /// 英文为源文，不需要词表文件；其余语言由 Locales/*.yml 提供译文，缺失词条回退英文。
    /// 运行时注册的语言不在其中，完整清单（含显示名称）用 <see cref="GetLanguages"/>。
    /// </remarks>
    public static String[] SupportedCultures { get; } = GetNames(_builtin);

    /// <summary>运行时注册的语言。键为词表语言键，如 ta</summary>
    private static readonly ConcurrentDictionary<String, LanguageInfo> _extraLanguages = new(StringComparer.Ordinal);

    private static readonly CultureInfo _SystemCulture = CultureInfo.CurrentUICulture;

    /// <summary>已由配置强制的界面语言。为空表示自动识别</summary>
    /// <remarks>用于在配置清空时撤销强制语言，避免进程级语言状态残留。</remarks>
    public static String? ForcedLanguage { get; private set; }
    #endregion

    #region 语言清单
    /// <summary>获取受支持的界面语言清单</summary>
    /// <returns>内置语言按固定顺序在前，运行时注册的语言按名称排序在后</returns>
    /// <remarks>
    /// 清单随运行时注册动态变化，可用于生成管理端的语言下拉框，不应假定只有内置语言。
    /// 每项用 <see cref="LanguageInfo.GetDisplayName"/> 输出显示文本，如英文页面的 “Chinese (Simplified) (简体中文)”，
    /// 不熟悉英文的用户也能靠自称找到自己的语言。
    /// </remarks>
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

    /// <summary>判断是否 Core 内置语言（含英文源文）</summary>
    /// <param name="name">语言名称</param>
    /// <returns>内置语言返回 true</returns>
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
    /// <summary>归一化语言名称。把 en、de-AT、zh-Hant 等映射到受支持的区域语言</summary>
    /// <param name="name">语言名称，可带 Accept-Language 权重（如 en-US,en;q=0.9）</param>
    /// <returns>受支持的语言名称；无法匹配时返回 null</returns>
    /// <remarks>
    /// 请求头与配置常用裸语言码（en、ja）或非支持区域（de-AT），而支持列表是区域语言（en-US、ja-JP、de-DE），
    /// 直接匹配会失败并回退系统语言。中文按脚本或地区分别归一到 zh-CN / zh-TW。
    /// </remarks>
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

    /// <summary>设置界面与日志语言</summary>
    /// <param name="language">语言名称，留空表示跟随系统自动识别</param>
    /// <returns>实际生效的语言名称；自动模式返回 null</returns>
    /// <remarks>
    /// 指定受支持语言后，<see cref="Culture"/> 与 <see cref="LogCulture"/> 同时固定，
    /// 翻译不再随请求语言或系统区域变化；语言不受支持时回退自动模式。
    /// 不修改线程默认语言，数字与日期格式仍由宿主或请求语言控制。
    /// </remarks>
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
