using System.Net;
using System.Text;

namespace NewLife.Log;

/// <summary>追踪器解析器</summary>
public interface ITracerResolver
{
    /// <summary>从Uri中解析出埋点名称</summary>
    /// <param name="uri"></param>
    /// <param name="userState"></param>
    /// <returns></returns>
    String? ResolveName(Uri uri, Object? userState);

    /// <summary>解析埋点名称</summary>
    /// <param name="name"></param>
    /// <param name="userState"></param>
    /// <returns></returns>
    String? ResolveName(String name, Object? userState);

    /// <summary>创建Http请求埋点</summary>
    ISpan? CreateSpan(ITracer tracer, Uri uri, Object? userState);
}

/// <summary>默认追踪器解析器</summary>
/// <remarks>
/// 解析器给予用户自定义创建Http请求埋点的机会，可以根据需要自定义埋点名称和标签。
/// 在星尘扩展中，再次扩展解析，支持自定义WebApi接口的埋点名称和标签。
/// </remarks>
public class DefaultTracerResolver : ITracerResolver
{
    /// <summary>请求内容是否作为数据标签。默认true</summary>
    /// <remarks>丰富的数据标签可以辅助分析问题，关闭后提升性能</remarks>
    public Boolean RequestContentAsTag { get; set; } = true;

    /// <summary>支持作为标签数据的内容类型</summary>
    public String[] TagTypes { get; set; } = [
        "text/plain", "text/xml", "application/json", "application/xml", "application/x-www-form-urlencoded"
    ];

    /// <summary>标签数据中要排除的头部</summary>
    public String[] ExcludeHeaders { get; set; } = ["traceparent", "Cookie"];

    /// <summary>从Uri中解析出埋点名称</summary>
    /// <param name="uri"></param>
    /// <param name="userState"></param>
    /// <returns></returns>
    public virtual String? ResolveName(Uri uri, Object? userState)
    {
        String name;
        if (uri.IsAbsoluteUri)
        {
            // 太长的Url分段，不适合作为埋点名称
            var segments = uri.Segments.Skip(1).TakeWhile(e => e.Length <= 16).ToArray();
            name = segments.Length > 0
               ? $"{uri.Scheme}://{uri.Authority}/{String.Concat(segments)}"
               : $"{uri.Scheme}://{uri.Authority}";
        }
        else
        {
            name = uri.ToString();
            var p = name.IndexOf('?');
            if (p > 0) name = name[..p];
        }

        return ResolveName(name, userState);
    }

    /// <summary>解析埋点名称</summary>
    /// <param name="name"></param>
    /// <param name="userState"></param>
    /// <returns></returns>
    public virtual String? ResolveName(String name, Object? userState) => name;

    #region 辅助

    /// <summary>读取Http内容的前缀字符串，用于埋点标签</summary>
    /// <param name="content">Http内容</param>
    /// <param name="maxLength">最大字符数</param>
    /// <returns>前缀字符串，失败时返回null</returns>
    private static String? ReadContentPrefix(ByteArrayContent content, Int32 maxLength)
    {
        if (content == null) return null;

        try
        {
            using var stream = content.ReadAsStreamAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            if (stream == null) return null;

            // 从ContentType获取编码，回退到UTF-8
            var encoding = Encoding.UTF8;
            var charset = content.Headers.ContentType?.CharSet;
            if (!charset.IsNullOrEmpty())
            {
                try
                {
                    encoding = Encoding.GetEncoding(charset);
                }
                catch
                {
                }
            }

            using var reader = new StreamReader(stream, encoding);
            var chars = new Char[maxLength];
            var read = reader.Read(chars, 0, maxLength);
            if (read > 0) return new String(chars, 0, read);

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>从多部分内容中查找第一个文本子内容</summary>
    /// <remarks>
    /// 遍历 MultipartContent 的子部分，返回第一个既是 ByteArrayContent 且 ContentType 匹配 TagTypes 的子内容。
    /// StringContent 和 FormUrlEncodedContent 均继承自 ByteArrayContent，因此可被正确处理。
    /// StreamContent 不是 ByteArrayContent，自动跳过，不会消耗文件流。
    /// </remarks>
    /// <param name="content">可能是多部分的内容</param>
    /// <returns>第一个文本子内容，未找到时返回null</returns>
    private ByteArrayContent? FindFirstTextContent(HttpContent? content)
    {
        if (content is not IEnumerable<HttpContent> parts) return null;

        foreach (var sub in parts)
        {
            if (sub is ByteArrayContent bac &&
                (bac.Headers.ContentType == null ||
                 bac.Headers.ContentType.MediaType == null ||
                 bac.Headers.ContentType.MediaType.StartsWithIgnoreCase(TagTypes)))
            {
                return bac;
            }
        }

        return null;
    }

    #endregion

    /// <summary>创建Http请求埋点</summary>
    public virtual ISpan? CreateSpan(ITracer tracer, Uri uri, Object? userState)
    {
        var name = tracer.Resolver.ResolveName(uri, userState);
        if (name.IsNullOrEmpty()) return null;

        var span = tracer.NewSpan(name);

        var request = userState as HttpRequestMessage;
        var method = request?.Method.Method ?? (userState as WebRequest)?.Method ?? "GET";
        var tag = $"{method} {uri}";

        if (RequestContentAsTag && tag.Length < tracer.MaxTagLength &&
            span is DefaultSpan ds && ds.TraceFlag > 0 && request != null)
        {
            var maxLength = ds.Tracer?.MaxTagLength ?? 1024;
            // 读取请求体前缀作为埋点标签。优先直接读取，否则尝试 multipart 子部分
            if (request.Content is ByteArrayContent bc &&
                (bc.Headers.ContentType == null ||
                 bc.Headers.ContentType.MediaType == null ||
                 bc.Headers.ContentType.MediaType.StartsWithIgnoreCase(TagTypes)))
            {
                var prefix = ReadContentPrefix(bc, maxLength);
                if (!prefix.IsNullOrEmpty()) tag += "\r\n" + prefix;
            }
            else
            {
                var child = FindFirstTextContent(request.Content);
                if (child != null)
                {
                    var prefix = ReadContentPrefix(child, maxLength);
                    if (!prefix.IsNullOrEmpty()) tag += "\r\n" + prefix;
                }
            }

            if (tag.Length < 500)
            {
                var vs = request.Headers.Where(e => !e.Key.EqualIgnoreCase(ExcludeHeaders)).ToDictionary(e => e.Key, e => e.Value.Join(";"));
                tag += "\r\n" + vs.Join("\r\n", e => $"{e.Key}: {e.Value}");
            }
        }
        span.SetTag(tag);

        return span;
    }
}
