using System.Globalization;

namespace NewLife;

/// <summary>请求语言作用域。按 Accept-Language 设置当前 UI 文化，释放时还原</summary>
/// <remarks>
/// 仅在 <see cref="Localization.Culture"/> 为空时生效，便于宿主固定全局语言。
/// 从请求头中按 q 权重选择最高权重且存在译文表的语言；全部不支持时不改变当前 UI 文化。
/// 只修改 <see cref="CultureInfo.CurrentUICulture"/>，不修改 <see cref="CultureInfo.CurrentCulture"/>，
/// 避免影响协议中的数字与日期格式。
/// </remarks>
public sealed class RequestCultureScope : IDisposable
{
    private readonly CultureInfo? _saved;
    private readonly Boolean _changed;

    private RequestCultureScope(CultureInfo? saved, Boolean changed)
    {
        _saved = saved;
        _changed = changed;
    }

    /// <summary>按 Accept-Language 头创建请求语言作用域</summary>
    /// <param name="acceptLanguage">Accept-Language 头原始值</param>
    /// <returns>请求语言作用域；未识别到受支持语言时释放不做任何事</returns>
    public static RequestCultureScope Create(String? acceptLanguage)
    {
#if NETFRAMEWORK
        // .NET Framework 的 CurrentUICulture 为线程静态，异步请求之间可能串语言，保持宿主语言
        return new(null, false);
#else
        if (Localization.Culture != null || !TryParseAcceptLanguage(acceptLanguage, out var culture)) return new(null, false);

        var saved = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = culture!;

        return new(saved, true);
#endif
    }

    /// <summary>解析 Accept-Language 头，返回权重最高且受支持的区域性</summary>
    /// <param name="acceptLanguage">Accept-Language 头原始值</param>
    /// <returns>区域性；没有受支持语言时返回 null</returns>
    public static CultureInfo? ParseAcceptLanguage(String? acceptLanguage)
    {
        TryParseAcceptLanguage(acceptLanguage, out var culture);

        return culture;
    }

    /// <summary>还原当前 UI 文化</summary>
    public void Dispose()
    {
#if !NETFRAMEWORK
        if (_changed) CultureInfo.CurrentUICulture = _saved!;
#endif
    }

    private static Boolean TryParseAcceptLanguage(String? acceptLanguage, out CultureInfo? culture)
    {
        culture = null;
        if (acceptLanguage.IsNullOrEmpty()) return false;

        var bestQ = 0.0;
        foreach (var item in acceptLanguage.Split(','))
        {
            var parts = item.Split(';');
            var name = parts[0].Trim();
            if (name.IsNullOrEmpty() || name == "*") continue;

            var q = 1.0;
            for (var i = 1; i < parts.Length; i++)
            {
                var part = parts[i].Trim();
                if (part.StartsWith("q=", StringComparison.OrdinalIgnoreCase) &&
                    Double.TryParse(part.Substring(2), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
                    q = value;
            }
            if (q <= 0 || q <= bestQ) continue;

            try
            {
                var candidate = CultureInfo.GetCultureInfo(name);
                if (!Localization.IsSupported(candidate)) continue;

                culture = candidate;
                bestQ = q;
            }
            catch (ArgumentException) { }
        }

        return culture != null;
    }
}
