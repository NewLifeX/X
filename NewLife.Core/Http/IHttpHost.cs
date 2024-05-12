namespace NewLife.Http;

/// <summary>Http主机服务</summary>
public interface IHttpHost
{
    /// <summary>匹配处理器</summary>
    /// <param name="path"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    IHttpHandler? MatchHandler(String path, HttpRequest? request);
}
