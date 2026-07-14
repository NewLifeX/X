using System.Net;

namespace NewLife.Http;

/// <summary>HTTP中间件委托</summary>
/// <param name="context">Http上下文</param>
/// <param name="next">下一个中间件或处理器的调用委托</param>
/// <returns>异步任务</returns>
public delegate Task HttpMiddlewareDelegate(IHttpContext context, Func<Task> next);

/// <summary>CORS 跨域中间件</summary>
/// <remarks>
/// 自动处理 OPTIONS 预检请求和设置 CORS 响应头。
/// 
/// <code>
/// server.Use(new CorsMiddleware
/// {
///     AllowOrigin = "*",
///     AllowMethods = "GET, POST, PUT, DELETE",
///     AllowHeaders = "Content-Type, Authorization",
///     AllowCredentials = false,
///     MaxAge = 3600
/// });
/// </code>
/// </remarks>
public class CorsMiddleware
{
    #region 属性
    /// <summary>允许的源。默认 *</summary>
    public String AllowOrigin { get; set; } = "*";

    /// <summary>允许的方法。默认 GET, POST, PUT, DELETE, OPTIONS</summary>
    public String AllowMethods { get; set; } = "GET, POST, PUT, DELETE, OPTIONS";

    /// <summary>允许的请求头。默认 Content-Type, Authorization, X-Requested-With</summary>
    public String AllowHeaders { get; set; } = "Content-Type, Authorization, X-Requested-With";

    /// <summary>暴露的响应头</summary>
    public String? ExposeHeaders { get; set; }

    /// <summary>是否允许携带凭据</summary>
    public Boolean AllowCredentials { get; set; }

    /// <summary>预检缓存时间（秒）</summary>
    public Int32 MaxAge { get; set; } = 3600;
    #endregion

    /// <summary>中间件入口</summary>
    /// <param name="context">Http上下文</param>
    /// <param name="next">下一个中间件</param>
    /// <returns></returns>
    public Task Invoke(IHttpContext context, Func<Task> next)
    {
        var req = context.Request;

        // 设置 CORS 响应头
        SetCorsHeaders(context);

        // 处理 OPTIONS 预检请求
        if (req?.Method == "OPTIONS")
        {
            context.Response.StatusCode = HttpStatusCode.NoContent;
            return TaskEx.CompletedTask;
        }

        return next();
    }

    private void SetCorsHeaders(IHttpContext context)
    {
        var res = context.Response;

        if (!AllowOrigin.IsNullOrEmpty())
            res.Headers["Access-Control-Allow-Origin"] = AllowOrigin;

        if (!AllowMethods.IsNullOrEmpty())
            res.Headers["Access-Control-Allow-Methods"] = AllowMethods;

        if (!AllowHeaders.IsNullOrEmpty())
            res.Headers["Access-Control-Allow-Headers"] = AllowHeaders;

        if (!ExposeHeaders.IsNullOrEmpty())
            res.Headers["Access-Control-Expose-Headers"] = ExposeHeaders;

        if (AllowCredentials)
            res.Headers["Access-Control-Allow-Credentials"] = "true";

        if (MaxAge > 0)
            res.Headers["Access-Control-Max-Age"] = MaxAge + "";
    }
}
