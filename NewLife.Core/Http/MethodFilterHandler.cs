using System.Net;
using NewLife.Remoting;

namespace NewLife.Http;

/// <summary>HTTP方法过滤处理器，将请求方法（GET/POST/PUT/DELETE）不匹配的请求返回405</summary>
/// <remarks>
/// 供 MapGet/MapPost/MapPut/MapDelete 内部使用，包装实际处理器。
/// </remarks>
internal class MethodFilterHandler : IHttpHandler
{
    /// <summary>允许的HTTP方法</summary>
    public String Method { get; }

    /// <summary>实际处理器</summary>
    public IHttpHandler Handler { get; }

    /// <summary>实例化</summary>
    /// <param name="method">允许的HTTP方法（如 GET）</param>
    /// <param name="handler">实际处理器</param>
    public MethodFilterHandler(String method, IHttpHandler handler)
    {
        Method = method.ToUpper();
        Handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    /// <summary>处理请求</summary>
    /// <param name="context">Http上下文</param>
    public void ProcessRequest(IHttpContext context)
    {
        // 直接比方法名：请求或方法名缺失时也按不匹配处理。
        // 旧写法 `!context.Request?.Method.EqualIgnoreCase(Method) == true` 在 Request 为 null 时整体为 null，
        // 条件不成立会直接放行到业务处理器，方法校验被绕过
        if (!Method.EqualIgnoreCase(context.Request?.Method))
        {
            context.Response.StatusCode = HttpStatusCode.MethodNotAllowed;
            context.Response.Headers["Allow"] = Method;
            return;
        }

        Handler.ProcessRequest(context);
    }
}
