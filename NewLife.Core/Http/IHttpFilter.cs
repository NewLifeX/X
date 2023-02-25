namespace NewLife.Http;

/// <summary>Http过滤器，拦截请求前后</summary>
public interface IHttpFilter
{
    /// <summary>请求前</summary>
    /// <param name="client">客户端</param>
    /// <param name="request">请求消息</param>
    /// <param name="state">状态数据</param>
    /// <returns></returns>
    Task OnRequest(HttpClient client, HttpRequestMessage request, Object state);

    /// <summary>获取响应后</summary>
    /// <param name="client">客户端</param>
    /// <param name="response">响应消息</param>
    /// <param name="state">状态数据</param>
    /// <returns></returns>
    Task OnResponse(HttpClient client, HttpResponseMessage response, Object state);

    /// <summary>发生错误时</summary>
    /// <param name="client">客户端</param>
    /// <param name="exception">异常</param>
    /// <param name="state">状态数据</param>
    /// <returns></returns>
    Task OnError(HttpClient client, Exception exception, Object state);
}