using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;

namespace NewLife;

/// <summary>多语言本地化。英文原文为键，缺翻译回退英文；默认跟随当前 UI 语言，可用 Culture/LogCulture 强制</summary>
public static partial class Localization
{
    #region 属性
    /// <summary>目标语言。默认 null，表示跟随 <see cref="CultureInfo.CurrentUICulture"/></summary>
    public static CultureInfo? Culture { get; set; }

    /// <summary>日志与配置注释的语言。默认 null，表示跟随 <see cref="Culture"/>、<see cref="DefaultCulture"/> 与 <see cref="SystemCulture"/></summary>
    public static CultureInfo? LogCulture { get; set; }

    /// <summary>站点默认语言。请求没有指定受支持语言时使用；空表示跟随 <see cref="CultureInfo.CurrentUICulture"/></summary>
    /// <remarks>默认语言只做请求与日志兜底，不写入 <see cref="Culture"/>，因此不会锁定界面</remarks>
    public static CultureInfo? DefaultCulture { get; private set; }

    /// <summary>进程启动时的 UI 语言。默认语言清空后恢复，并作为日志的最终兜底</summary>
    public static CultureInfo SystemCulture { get; } = CultureInfo.CurrentUICulture;

    private static readonly CultureInfo? _initialDefaultThreadUICulture = CultureInfo.DefaultThreadCurrentUICulture;
    private static readonly Object _defaultLock = new();
    private static readonly ConcurrentDictionary<String, IReadOnlyDictionary<String, String>> _overrides = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<String, String?> _cultureKeys = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<String, Dictionary<String, String>> _tables = new(StringComparer.Ordinal);
    private static readonly Dictionary<String, String> _emptyTable = new(StringComparer.Ordinal);

    private static Int32 _overrideCount;
    #endregion

    #region 注册
    /// <summary>注册本地化覆盖表。默认不覆盖已有译文，可同时注册新语言</summary>
    public static void Register(CultureInfo culture, IDictionary<String, String> table, Boolean overwrite = false)
    {
        if (culture == null) throw new ArgumentNullException(nameof(culture));
        if (table == null || table.Count == 0) return;

        var key = GetCultureKey(culture);
        if (key == null) return;

        _overrides.AddOrUpdate(
            key,
            _ => Merge(key, table, overwrite),
            (_, existing) => Merge(key, table, overwrite, existing));
        Volatile.Write(ref _overrideCount, _overrides.Count);

        // 运行时新增的语言同时进入语言清单，供管理端下拉选择与配置归一化；内置语言无需登记
        if (!IsBuiltinLanguage(culture.Name)) _extraLanguages.TryAdd(key, LanguageInfo.FromCulture(culture));
    }

    /// <summary>移除指定语言的覆盖表</summary>
    public static Boolean Unregister(CultureInfo culture)
    {
        if (culture == null) return false;

        var key = GetCultureKey(culture);
        if (key == null || !_overrides.TryRemove(key, out _)) return false;

        Volatile.Write(ref _overrideCount, _overrides.Count);
        _extraLanguages.TryRemove(key, out _);
        return true;
    }

    private static IReadOnlyDictionary<String, String> Merge(String cultureKey, IDictionary<String, String> table, Boolean overwrite, IReadOnlyDictionary<String, String>? existing = null)
    {
        var result = new Dictionary<String, String>(StringComparer.Ordinal);
        if (existing != null)
        {
            foreach (var item in existing) result[item.Key] = item.Value;
        }

        var builtin = GetTable(cultureKey);
        foreach (var item in table)
        {
            if (item.Key.IsNullOrEmpty()) continue;

            if (!overwrite && (result.ContainsKey(item.Key) || (builtin != null && builtin.ContainsKey(item.Key)))) continue;

            result[item.Key] = item.Value ?? String.Empty;
        }

        return result;
    }
    #endregion

    #region 方法
    /// <summary>翻译指定文本，缺少翻译时返回原文</summary>
    public static String Translate(String template) => Translate(null, template);

    /// <summary>翻译并格式化指定文本，失败时回退英文</summary>
    public static String Translate(String template, params Object?[]? args) => Translate(null, template, args);

    /// <summary>按日志语言翻译指定文本</summary>
    /// <param name="template">英文原文模板</param>
    /// <returns>本地化文本；缺少翻译时返回原文</returns>
    /// <remarks>日志语言优先取 <see cref="LogCulture"/>，其次锁定语言与默认语言，最后回退进程启动时的系统语言，避免随请求语言混杂</remarks>
    public static String TranslateForLog(String template) => Translate(GetLogCulture(), template);

    /// <summary>按日志语言翻译并格式化指定文本</summary>
    /// <param name="template">英文原文模板</param>
    /// <param name="args">格式化参数</param>
    /// <returns>本地化文本；缺少翻译或格式化失败时回退英文</returns>
    public static String TranslateForLog(String template, params Object?[]? args) => Translate(GetLogCulture(), template, args);

    private static CultureInfo GetLogCulture() => LogCulture ?? Culture ?? DefaultCulture ?? SystemCulture;

    private static CultureInfo GetCurrentCulture(CultureInfo? culture)
    {
        if (culture != null) return culture;
        if (Culture != null) return Culture;

        var current = CultureInfo.CurrentUICulture;

        // 不变全球化下当前区域性没有名称，退回站点默认语言；否则跟随当前线程的 UI 语言
        return current.Name.Length == 0 && DefaultCulture != null ? DefaultCulture : current;
    }

    /// <summary>判断指定区域是否有可用的翻译表；英文始终为 true</summary>
    public static Boolean IsSupported(CultureInfo culture)
    {
        if (culture == null) return false;

        var key = GetCultureKeyCached(culture);
        if (key == null) return false;
        if (key == "en") return true;

        // 运行时注册的覆盖表可能包含内置语言之外的新语言
        if (Volatile.Read(ref _overrideCount) != 0 && _overrides.ContainsKey(key)) return true;

        return GetTable(key) != null;
    }

    internal static String Translate(CultureInfo? culture, String template)
    {
        if (template.IsNullOrEmpty()) return template;

        culture = GetCurrentCulture(culture);
        var key = GetCultureKeyCached(culture);
        if (key == null) return template;

        // 无覆盖表时跳过覆盖查找
        if (Volatile.Read(ref _overrideCount) != 0 && _overrides.TryGetValue(key, out var overlay) && overlay.TryGetValue(template, out var text)) return text;

        // en 也可能带词表：元数据源文为其它语言时，用 en.yml 提供英文译文
        var table = GetTable(key);
        if (table != null && table.TryGetValue(template, out text)) return text;

        return template;
    }

    private static Dictionary<String, String>? GetTable(String key)
    {
        if (_tables.TryGetValue(key, out var table)) return table.Count == 0 ? null : table;

        table = CreateTable(key);
        if (table == null)
        {
            _tables.TryAdd(key, _emptyTable);
            return null;
        }

        _tables.TryAdd(key, table);
        return table;
    }

    private static Dictionary<String, String>? DecodeTable(String[] keys, Byte[] data)
    {
        if (keys == null || keys.Length == 0 || data == null || data.Length == 0) return null;

        using var input = new MemoryStream(data);
        using var deflate = new DeflateStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        deflate.CopyTo(output);
        var buffer = output.ToArray();
        if (buffer.Length < keys.Length * 4) return null;

        var table = new Dictionary<String, String>(keys.Length, StringComparer.Ordinal);
        var offset = keys.Length * 4;
        for (var i = 0; i < keys.Length; i++)
        {
            var length = buffer[i * 4] | buffer[i * 4 + 1] << 8 | buffer[i * 4 + 2] << 16 | buffer[i * 4 + 3] << 24;
            if (length < 0) continue;
            if (length > buffer.Length - offset) return null;

            table[keys[i]] = Encoding.UTF8.GetString(buffer, offset, length);
            offset += length;
        }
        return table;
    }

    private static String? GetCultureKeyCached(CultureInfo culture)
    {
        var name = culture.Name;
        if (name.Length == 0) return null;

        if (_cultureKeys.TryGetValue(name, out var key)) return key;

        key = GetCultureKey(culture);
        _cultureKeys.TryAdd(name, key);
        return key;
    }

    internal static String? GetCultureKeyOf(String name)
    {
        if (name.IsNullOrEmpty()) return null;
        if (_cultureKeys.TryGetValue(name, out var key)) return key;

        // 直接从名称推导词表键：无 ICU（DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1）时创建不了 CultureInfo
        key = GetCultureKey(name);

        _cultureKeys.TryAdd(name, key);
        return key;
    }

    internal static String Translate(CultureInfo? culture, String template, params Object?[]? args)
    {
        culture = GetCurrentCulture(culture);
        var text = Translate(culture, template);
        if (args == null || args.Length == 0) return text;

        try
        {
            return String.Format(culture, text, args);
        }
        catch (FormatException) when (!ReferenceEquals(text, template))
        {
            // 译文占位符不合法时回退英文，避免翻译问题演变成新的异常
            return String.Format(culture, template, args);
        }
    }
    #endregion
}
