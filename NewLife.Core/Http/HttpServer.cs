using System.ComponentModel;
using NewLife.Net;

namespace NewLife.Http;

/// <summary>Http服务器</summary>
[DisplayName("Http服务器")]
public class HttpServer : NetServer, IHttpHost
{
    #region 属性
    /// <summary>Http响应头Server名称</summary>
    public String ServerName { get; set; }

    /// <summary>路由映射</summary>
    public IDictionary<String, IHttpHandler> Routes { get; set; } = new Dictionary<String, IHttpHandler>(StringComparer.OrdinalIgnoreCase);
    #endregion

    /// <summary>实例化</summary>
    public HttpServer()
    {
        Name = "Http";
        Port = 80;
        ProtocolType = NetType.Http;

        var ver = GetType().Assembly.GetName().Version ?? new Version();
        ServerName = $"NewLife-HttpServer/{ver.Major}.{ver.Minor}";
    }

    ///// <summary>创建会话</summary>
    ///// <param name="session"></param>
    ///// <returns></returns>
    //protected override INetSession CreateSession(ISocketSession session) => new HttpSession();

    /// <summary>为会话创建网络数据处理器。可作为业务处理实现，也可以作为前置协议解析</summary>
    /// <param name="session"></param> 
    /// <returns></returns>
    public override INetHandler? CreateHandler(INetSession session) => new HttpSession();

    #region 方法
    /// <summary>映射路由处理器</summary>
    /// <param name="path"></param>
    /// <param name="handler"></param>
    public void Map(String path, IHttpHandler handler) => Routes[path] = handler;

    /// <summary>映射路由处理器</summary>
    /// <param name="path"></param>
    /// <param name="handler"></param>
    public void Map(String path, HttpProcessDelegate handler) => Routes[path] = new DelegateHandler { Callback = handler };

    /// <summary>映射路由处理器</summary>
    /// <param name="path"></param>
    /// <param name="handler"></param>
    public void Map<TResult>(String path, Func<TResult> handler) => Routes[path] = new DelegateHandler { Callback = handler };

    /// <summary>映射路由处理器</summary>
    /// <param name="path"></param>
    /// <param name="handler"></param>
    public void Map<TModel, TResult>(String path, Func<TModel, TResult> handler) => Routes[path] = new DelegateHandler { Callback = handler };

    /// <summary>映射路由处理器</summary>
    /// <param name="path"></param>
    /// <param name="handler"></param>
    public void Map<T1, T2, TResult>(String path, Func<T1, T2, TResult> handler) => Routes[path] = new DelegateHandler { Callback = handler };

    /// <summary>映射路由处理器</summary>
    /// <param name="path"></param>
    /// <param name="handler"></param>
    public void Map<T1, T2, T3, TResult>(String path, Func<T1, T2, T3, TResult> handler) => Routes[path] = new DelegateHandler { Callback = handler };

    /// <summary>映射路由处理器</summary>
    /// <param name="path"></param>
    /// <param name="handler"></param>
    public void Map<T1, T2, T3, T4, TResult>(String path, Func<T1, T2, T3, T4, TResult> handler) => Routes[path] = new DelegateHandler { Callback = handler };

    /// <summary>映射控制器</summary>
    /// <typeparam name="TController"></typeparam>
    /// <param name="path"></param>
    public void MapController<TController>(String? path = null) => MapController(typeof(TController), path);

    /// <summary>映射控制器</summary>
    /// <param name="controllerType"></param>
    /// <param name="path"></param>
    public void MapController(Type controllerType, String? path = null)
    {
        if (path.IsNullOrEmpty()) path = "/" + controllerType.Name.TrimEnd("Controller");

        var path2 = path.EnsureEnd("/*");
        Routes[path2] = new ControllerHandler { ControllerType = controllerType };
    }

    /// <summary>映射静态文件</summary>
    /// <param name="path">映射路径，如 /js</param>
    /// <param name="contentPath">内容目录，如 /wwwroot/js</param>
    public void MapStaticFiles(String path, String contentPath)
    {
        path = path.EnsureEnd("/");
        var path2 = path.EnsureEnd("*");
        Routes[path2] = new StaticFilesHandler { Path = path, ContentPath = contentPath };
    }

    private readonly IDictionary<String, String> _maps = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
    /// <summary>匹配处理器</summary>
    /// <param name="path"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    public IHttpHandler? MatchHandler(String path, HttpRequest? request)
    {
        if (Routes.TryGetValue(path, out var handler)) return handler;

        // 判断缓存
        if (_maps.TryGetValue(path, out var p) &&
            Routes.TryGetValue(p, out handler)) return handler;

        // 模糊匹配
        foreach (var item in Routes)
        {
            if (item.Key.Contains('*') && item.Key.IsMatch(path))
            {
                if (Routes.TryGetValue(item.Key, out handler))
                {
                    // 大于3段的路径不做缓存，避免动态Url引起缓存膨胀
                    if (handler is StaticFilesHandler || path.Split('/').Length <= 3) _maps[path] = item.Key;

                    return handler;
                }
            }
        }

        return null;
    }
    #endregion
}