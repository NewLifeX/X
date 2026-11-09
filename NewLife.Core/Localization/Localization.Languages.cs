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
            foreach (var item in _extraLanguages)
            {
                // 旧版别名（in/iw/no/mo）与内置区域语言重复，归一化命中内置语言时跳过
                var name = NormalizeCulture(item.Value.Name);
                if (name != null && Array.IndexOf(SupportedCultures, name) >= 0) continue;

                extras.Add(item.Value);
            }

            extras.Sort((x, y) => StringComparer.Ordinal.Compare(x.Name, y.Name));
            list.AddRange(extras);
        }

        return list;
    }

    /// <summary>获取语言选项。可直接绑定管理端下拉框</summary>
    /// <remarks>
    /// 只列出**真正有词表**的语言。小体积发布（如 NativeAOT 边缘代理）可用
    /// <c>NewLifeLocalizationLanguages</c> 只编译少数语言词表，此时下拉框不能再列出其它语言，
    /// 否则用户选中后界面与日志会静默回退原文。需要完整语言清单请用 <see cref="GetLanguages"/>。
    /// </remarks>
    public static IDictionary<String, String> GetLanguageOptions()
    {
        var dic = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in GetLanguages())
        {
            if (IsLanguageAvailable(item.Name)) dic[item.Name] = item.GetDisplayName();
        }

        return dic;
    }

    /// <summary>判断指定语言是否有可用词表</summary>
    /// <param name="name">语言代码，如 zh-CN</param>
    /// <returns></returns>
    private static Boolean IsLanguageAvailable(String name)
    {
        var key = GetCultureKeyOf(name);
        if (key == null) return false;
        if (key == "en") return true;

        // 运行时注册的覆盖表可能包含内置词表之外的新语言
        if (Volatile.Read(ref _overrideCount) != 0 && _overrides.ContainsKey(key)) return true;

        return GetTable(key) != null;
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

        // 旧版 .NET 与浏览器仍可能发送被弃用的语言别名，先归一到现代语言代码
        first = NormalizeAlias(first);

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

    /// <summary>设置站点默认语言。留空恢复进程启动时的系统语言</summary>
    /// <param name="language">语言代码，如 zh-CN；为空表示不指定默认语言</param>
    /// <returns>归一化后的语言名称；不受支持时返回 null</returns>
    /// <remarks>
    /// 默认语言只作为请求与日志的兜底，不写入 <see cref="Culture"/>，因此不会锁定界面；
    /// 请求级语言（如 Accept-Language）仍可覆盖它。
    /// </remarks>
    public static String? SetDefaultLanguage(String? language)
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
                // 不变全球化下具名区域性不可用，保持跟随系统
                name = null;
            }
        }

        lock (_defaultLock)
        {
            DefaultCulture = culture;

            // 只影响新线程与线程池线程的默认值；当前请求线程由 RequestCultureScope 或宿主中间件显式设置。
            // 锁定语言优先于默认语言，保证后台线程直接读取 CurrentUICulture 时也与界面一致
            CultureInfo.DefaultThreadCurrentUICulture = Culture ?? culture ?? _initialDefaultThreadUICulture;
        }

        return name;
    }

    /// <summary>设置界面与日志语言。留空表示跟随默认语言与系统自动识别</summary>
    /// <param name="language">语言代码，如 zh-CN；为空表示取消锁定</param>
    /// <returns>归一化后的语言名称；不受支持时返回 null</returns>
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
        LogCulture = culture;
        ForcedLanguage = name;

        return name;
    }

    internal static String NormalizeAlias(String name)
    {
        if (name.IsNullOrEmpty()) return name;

        var dash = name.IndexOf('-');
        var lang = dash > 0 ? name[..dash] : name;
        var alias = lang.ToLowerInvariant() switch
        {
            "in" => "id",
            "iw" => "he",
            "mo" => "ro",
            "no" => "nb",
            _ => null,
        };

        return alias == null ? name : (dash > 0 ? alias + name[dash..] : alias);
    }
    #endregion
}
