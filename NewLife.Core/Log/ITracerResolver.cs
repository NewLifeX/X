using System.Net;

namespace NewLife.Log;

/// <summary>追踪器解析器</summary>
public interface ITracerResolver
{
    /// <summary>从Uri中解析出埋点名称</summary>
    /// <param name="uri"></param>
    /// <param name="userState"></param>
    /// <returns></returns>
    String ResolveName(Uri uri, Object? userState);

    /// <summary>解析埋点名称</summary>
    /// <param name="name"></param>
    /// <param name="userState"></param>
    /// <returns></returns>
    String ResolveName(String name, Object? userState);

    /// <summary>创建Http请求埋点</summary>
    ISpan CreateSpan(ITracer tracer, Uri uri, Object? userState);
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
    public String ResolveName(Uri uri, Object? userState)
    {
        var url = uri.ToString();

        // 太长的Url分段，不适合作为埋点名称
        if (url.Length > 20 + 16)
        {
            var ss = url.Split('/', '?');
            // 从第三段开始查，跳过开头的http://和域名
            for (var i = 3; i < ss.Length; i++)
            {
                if (ss[i].Length > 16)
                {
                    url = ss.Take(i).Join("/");
                    break;
                }
            }
        }

        var p1 = url.IndexOf('?');
        var name = p1 < 0 ? url : url[..p1];
        return ResolveName(name, userState);
    }

    /// <summary>解析埋点名称</summary>
    /// <param name="name"></param>
    /// <param name="userState"></param>
    /// <returns></returns>
    public String ResolveName(String name, Object? userState) => name;

    /// <summary>创建Http请求埋点</summary>
    public ISpan CreateSpan(ITracer tracer, Uri uri, Object? userState)
    {
        var name = tracer.Resolver.ResolveName(uri, userState);
        var span = tracer.NewSpan(name);

        var request = userState as HttpRequestMessage;
        var method = request?.Method.Method ?? (userState as WebRequest)?.Method ?? "GET";
        var tag = $"{method} {uri}";

        if (RequestContentAsTag && tag.Length < tracer.MaxTagLength &&
            span is DefaultSpan ds && ds.TraceFlag > 0 && request != null)
        {
            var maxLength = ds.Tracer?.MaxTagLength ?? 1024;
            if (request.Content is ByteArrayContent content &&
                content.Headers.ContentLength != null &&
                content.Headers.ContentLength < 1024 * 8 &&
                content.Headers.ContentType != null &&
                content.Headers.ContentType.MediaType.StartsWithIgnoreCase(TagTypes))
            {
                // 既然都读出来了，不管多长，都要前面1024字符
                var str = request.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                if (!str.IsNullOrEmpty()) tag += "\r\n" + (str.Length > maxLength ? str[..maxLength] : str);
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
