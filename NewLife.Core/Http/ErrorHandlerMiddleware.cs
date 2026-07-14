using System.Net;
using NewLife.Data;
using NewLife.Serialization;

namespace NewLife.Http;

/// <summary>全局错误处理中间件</summary>
/// <remarks>
/// 捕获处理器中抛出的异常并转换为结构化 HTTP 错误响应。
/// 
/// <code>
/// server.Use(new ErrorHandlerMiddleware
/// {
///     IncludeDetails = true  // 开发模式返回详细异常信息
/// });
/// </code>
/// </remarks>
public class ErrorHandlerMiddleware
{
    #region 属性
    /// <summary>是否在响应中包含详细异常信息（堆栈跟踪等）。生产环境应设为 false</summary>
    public Boolean IncludeDetails { get; set; }
    #endregion

    /// <summary>中间件入口</summary>
    /// <param name="context">Http上下文</param>
    /// <param name="next">下一个中间件</param>
    /// <returns></returns>
    public async Task Invoke(IHttpContext context, Func<Task> next)
    {
        try
        {
            await next().ConfigureAwait(false);
        }
        catch (HttpException hex)
        {
            SetErrorResponse(context, hex.StatusCode, hex.Message, IncludeDetails ? hex : null);
        }
        catch (Exception ex)
        {
            SetErrorResponse(context, HttpStatusCode.InternalServerError, ex.Message, IncludeDetails ? ex : null);
        }
    }

    private static void SetErrorResponse(IHttpContext context, HttpStatusCode code, String message, Exception? detail)
    {
        var res = context.Response;
        res.StatusCode = code;

        if (detail != null)
        {
            // 开发模式返回结构化错误
            var error = new
            {
                error = new
                {
                    code = (Int32)code,
                    message,
                    detail = detail.ToString()
                }
            };
            res.ContentType = "application/json; charset=utf-8";
            res.Body = (ArrayPacket)error.ToJson().GetBytes();
        }
        else
        {
            res.StatusDescription = message;
        }
    }
}
