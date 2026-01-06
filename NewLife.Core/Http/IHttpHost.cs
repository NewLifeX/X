namespace NewLife.Http;

/// <summary>Http主机服务接口</summary>
/// <remarks>提供路由匹配能力，由 HttpServer 或其他自定义主机实现</remarks>
public interface IHttpHost
{
    /// <summary>匹配处理器</summary>
    /// <param name="path">请求路径（不含查询字符串）</param>
    /// <param name="request">Http请求对象，可用于更精细的匹配逻辑</param>
    /// <returns>匹配到的处理器；找不到时返回 null</returns>
    IHttpHandler? MatchHandler(String path, HttpRequest? request);
}
