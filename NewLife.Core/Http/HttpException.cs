using System.Net;

namespace NewLife.Http;

/// <summary>HTTP异常，携带Http状态码</summary>
/// <remarks>
/// 在处理请求过程中抛出此异常，HttpServer会自动返回对应状态码。
/// 其余未处理异常统一返回 500 InternalServerError。
/// 
/// <code>
/// throw new HttpException(HttpStatusCode.NotFound, "用户不存在");
/// throw new HttpException(401, "未授权访问");
/// </code>
/// </remarks>
public class HttpException : Exception
{
    /// <summary>HTTP状态码</summary>
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.InternalServerError;

    /// <summary>实例化</summary>
    /// <param name="statusCode">HTTP状态码</param>
    /// <param name="message">错误消息</param>
    public HttpException(HttpStatusCode statusCode, String message) : base(message) => StatusCode = statusCode;

    /// <summary>实例化</summary>
    /// <param name="statusCode">HTTP状态码数值</param>
    /// <param name="message">错误消息</param>
    public HttpException(Int32 statusCode, String message) : base(message) => StatusCode = (HttpStatusCode)statusCode;
}
