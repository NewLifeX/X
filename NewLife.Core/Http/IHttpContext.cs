using System.Net;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Net;
using NewLife.Serialization;

namespace NewLife.Http;

/// <summary>Http上下文</summary>
public interface IHttpContext
{
    #region 属性
    /// <summary>请求</summary>
    HttpRequest Request { get; }

    /// <summary>响应</summary>
    HttpResponse Response { get; }

    /// <summary>连接会话</summary>
    INetSession? Connection { get; }

    /// <summary>Socket连接</summary>
    ISocketRemote? Socket { get; }

    /// <summary>WebSocket连接</summary>
    WebSocket? WebSocket { get; }

    /// <summary>执行路径</summary>
    String Path { get; }

    /// <summary>处理器</summary>
    IHttpHandler? Handler { get; }

    /// <summary>服务提供者</summary>
    IServiceProvider? ServiceProvider { get; }

    /// <summary>请求参数</summary>
    IDictionary<String, Object?> Parameters { get; }
    #endregion
}

/// <summary>默认Http上下文</summary>
public class DefaultHttpContext : IHttpContext
{
    #region 属性
    /// <summary>请求</summary>
    public HttpRequest Request { get; set; }

    /// <summary>响应</summary>
    public HttpResponse Response { get; set; } = new HttpResponse();

    /// <summary>连接会话</summary>
    public INetSession? Connection { get; set; }

    /// <summary>Socket连接</summary>
    public ISocketRemote? Socket { get; set; }

    /// <summary>WebSocket连接</summary>
    public WebSocket? WebSocket { get; set; }

    /// <summary>执行路径</summary>
    public String Path { get; set; }

    /// <summary>处理器</summary>
    public IHttpHandler? Handler { get; set; }

    /// <summary>服务提供者</summary>
    public IServiceProvider? ServiceProvider { get; set; }

    /// <summary>请求参数</summary>
    public IDictionary<String, Object?> Parameters { get; } = new NullableDictionary<String, Object?>(StringComparer.OrdinalIgnoreCase);
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    /// <param name="session"></param>
    /// <param name="request"></param>
    /// <param name="path"></param>
    /// <param name="handler"></param>
    public DefaultHttpContext(INetSession session, HttpRequest request, String path, IHttpHandler? handler)
    {
        Connection = session;
        Request = request;
        Path = path;
        Handler = handler;

        Socket = session?.Session;
    }

    /// <summary>实例化</summary>
    /// <param name="socket"></param>
    /// <param name="request"></param>
    /// <param name="path"></param>
    /// <param name="handler"></param>
    public DefaultHttpContext(ISocketRemote socket, HttpRequest request, String path, IHttpHandler? handler)
    {
        Socket = socket;
        Request = request;
        Path = path;
        Handler = handler;
    }
    #endregion

    #region 静态
    [ThreadStatic]
    private static IHttpContext? _current;

    /// <summary>当前上下文</summary>
    public static IHttpContext? Current { get => _current; set => _current = value; }
    #endregion
}

/// <summary>Http上下文扩展方法</summary>
/// <remarks>提供便捷的响应写入方法</remarks>
public static class HttpContextExtensions
{
    /// <summary>写入JSON响应</summary>
    /// <param name="context">Http上下文</param>
    /// <param name="data">数据对象</param>
    /// <param name="statusCode">状态码，默认200</param>
    public static void WriteJson(this IHttpContext context, Object data, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.Body = (ArrayPacket)data.ToJson().GetBytes();
    }

    /// <summary>写入纯文本响应</summary>
    /// <param name="context">Http上下文</param>
    /// <param name="text">文本内容</param>
    /// <param name="statusCode">状态码，默认200</param>
    public static void WriteText(this IHttpContext context, String text, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "text/plain; charset=utf-8";
        context.Response.Body = (ArrayPacket)text.GetBytes();
    }

    /// <summary>写入HTML响应</summary>
    /// <param name="context">Http上下文</param>
    /// <param name="html">HTML内容</param>
    /// <param name="statusCode">状态码，默认200</param>
    public static void WriteHtml(this IHttpContext context, String html, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.Body = (ArrayPacket)html.GetBytes();
    }

    /// <summary>重定向</summary>
    /// <param name="context">Http上下文</param>
    /// <param name="url">目标URL</param>
    /// <param name="statusCode">重定向状态码，默认302</param>
    public static void Redirect(this IHttpContext context, String url, HttpStatusCode statusCode = HttpStatusCode.Redirect)
    {
        context.Response.StatusCode = statusCode;
        context.Response.Headers["Location"] = url;
    }

    /// <summary>发送文件</summary>
    /// <param name="context">Http上下文</param>
    /// <param name="filePath">文件路径</param>
    /// <param name="contentType">内容类型，为空时自动检测</param>
    public static void WriteFile(this IHttpContext context, String filePath, String? contentType = null)
    {
        var fi = filePath.AsFile();
        if (!fi.Exists)
        {
            context.Response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        contentType ??= MimeHelper.GetContentType(fi.Extension) ?? "application/octet-stream";

        using var fs = fi.OpenRead();
        context.Response.SetResult(fs, contentType);
    }
}
