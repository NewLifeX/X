using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;

namespace NewLife;

/// <summary>多语言本地化</summary>
/// <remarks>
/// 默认使用 <see cref="CultureInfo.CurrentUICulture"/> 作为目标语言，应用可通过 <see cref="Culture"/> 强制指定。
/// 翻译表以英文原文为键，缺少翻译或格式化失败时回退英文原文，因此未接入本地化的组件行为不变。
/// 语言表由 <c>Locales/*.yml</c> 在编译期生成：Core 内置表共享一份键表，译文以 Deflate 压缩的字节数组内嵌，
/// 首次使用时解压建表；下游组件生成的表在模块初始化时注册。不依赖 ResourceManager 与卫星程序集，原生支持 AOT。
/// </remarks>
public static partial class Localization
{
    #region 属性
    /// <summary>目标语言。默认 null，表示跟随 <see cref="CultureInfo.CurrentUICulture"/></summary>
    public static CultureInfo? Culture { get; set; }

    /// <summary>日志语言。默认 null，表示跟随 <see cref="Culture"/> 或 <see cref="CultureInfo.CurrentUICulture"/></summary>
    /// <remarks>启用 ASP.NET Core 请求语言识别后，可把它固定为宿主语言，避免日志随请求变化。</remarks>
    public static CultureInfo? LogCulture { get; set; }

    private static readonly ConcurrentDictionary<String, IReadOnlyDictionary<String, String>> _overrides = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<String, String?> _cultureKeys = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<String, Dictionary<String, String>> _tables = new(StringComparer.Ordinal);
    private static readonly Dictionary<String, String> _emptyTable = new(StringComparer.Ordinal);

    /// <summary>非空覆盖表数量。为 0 时热路径可跳过一次字典查找</summary>
    private static Int32 _overrideCount;
    #endregion

    #region 注册
    /// <summary>注册本地化覆盖表</summary>
    /// <param name="culture">目标语言</param>
    /// <param name="table">英文原文到译文的映射</param>
    /// <param name="overwrite">是否覆盖已存在的译文。默认 false，内置译文优先</param>
    /// <remarks>
    /// 用于下游组件补充自己的译文，或在运行时新增语言。查找顺序：覆盖表 → 内置表 → 英文原文。
    /// 源生成器产出的自动注册按程序集类型传参：应用（可执行程序集）传 true 以覆盖 Core 同名译文，类库传 false 只补充新 key。
    /// 注册是进程级全局状态，建议在启动阶段完成；多个注册方修改同一 key 时后注册者生效。
    /// </remarks>
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
    }

    /// <summary>移除指定语言的覆盖表</summary>
    /// <param name="culture">目标语言</param>
    /// <returns>是否移除了覆盖表</returns>
    public static Boolean Unregister(CultureInfo culture)
    {
        if (culture == null) return false;

        var key = GetCultureKey(culture);
        if (key == null || !_overrides.TryRemove(key, out _)) return false;

        Volatile.Write(ref _overrideCount, _overrides.Count);
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
    /// <summary>翻译指定文本</summary>
    /// <param name="template">英文原文模板</param>
    /// <returns>本地化文本；缺少翻译时返回原文</returns>
    public static String Translate(String template) => Translate(null, template);

    /// <summary>翻译并格式化指定文本</summary>
    /// <param name="template">英文原文模板</param>
    /// <param name="args">格式化参数</param>
    /// <returns>本地化文本；缺少翻译或格式化失败时回退英文</returns>
    public static String Translate(String template, params Object?[]? args) => Translate(null, template, args);

    /// <summary>按日志语言翻译指定文本</summary>
    /// <param name="template">英文原文模板</param>
    /// <returns>本地化文本；缺少翻译时返回原文</returns>
    /// <remarks>优先使用 <see cref="LogCulture"/>，其次 <see cref="Culture"/>，最后当前 UI 文化。供自定义日志实现复用。</remarks>
    public static String TranslateForLog(String template) => Translate(LogCulture ?? Culture, template);

    /// <summary>按日志语言翻译并格式化指定文本</summary>
    /// <param name="template">英文原文模板</param>
    /// <param name="args">格式化参数</param>
    /// <returns>本地化文本；缺少翻译或格式化失败时回退英文</returns>
    public static String TranslateForLog(String template, params Object?[]? args) => Translate(LogCulture ?? Culture, template, args);

    internal static String Translate(CultureInfo? culture, String template)
    {
        if (template.IsNullOrEmpty()) return template;

        culture ??= Culture ?? CultureInfo.CurrentUICulture;
        var key = GetCultureKeyCached(culture);
        if (key == null) return template;

        // 无覆盖表时跳过覆盖查找；英文原文即 key，直接返回
        if (Volatile.Read(ref _overrideCount) != 0 && _overrides.TryGetValue(key, out var overlay) && overlay.TryGetValue(template, out var text)) return text;

        if (key == "en") return template;

        var table = GetTable(key);
        if (table != null && table.TryGetValue(template, out text)) return text;

        return template;
    }

    /// <summary>获取指定语言的编译期语言表。首次访问时按需构建并缓存，未提供的语言返回 null</summary>
    /// <param name="key">语言键</param>
    /// <returns>语言表；该语言无内置表时返回 null</returns>
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

    /// <summary>解压并构建编译期语言表</summary>
    /// <param name="keys">全部语言共享的键表，译文按该顺序排列</param>
    /// <param name="data">Deflate 压缩的译文数据，前面是每个键的 Int32 小端长度（-1 表示缺失）</param>
    /// <returns>语言表；数据无效时返回 null</returns>
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

    internal static String Translate(CultureInfo? culture, String template, params Object?[]? args)
    {
        culture ??= Culture ?? CultureInfo.CurrentUICulture;
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
