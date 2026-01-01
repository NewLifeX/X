using System.ComponentModel;
using NewLife.Net;

namespace NewLife.Http;

/// <summary>Http服务器</summary>
/// <remarks>
/// 主要职责：
/// 1. 保存路由映射 <see cref="Routes"/> 并在收到请求时根据路径匹配处理器；
/// 2. 为每个网络会话创建对应的 <see cref="HttpSession"/> 协议处理器；
/// 3. 提供多种 Map 重载（委托/控制器/静态文件）。
/// 
/// 线程安全说明：
/// - 典型使用场景下，路由在启动阶段集中注册，运行期只读访问；
/// - 若需要在运行期动态增删路由，应在外部自行序列化（加锁）调用 Map 方法，或者在未来引入并发字典方案；
/// - 当前实现为了保持兼容性，不直接改为 ConcurrentDictionary，仅在匹配时使用快照数组降低并发修改风险（仍不保证完全线程安全）。
/// </remarks>
[DisplayName("Http服务器")]
public class HttpServer : NetServer, IHttpHost
{
    #region 属性
    /// <summary>Http响应头Server名称</summary>
    public String ServerName { get; set; }

    /// <summary>路由映射。Key 为路径（可含 * 通配），Value 为处理器</summary>
    public IDictionary<String, IHttpHandler> Routes { get; set; } = new Dictionary<String, IHttpHandler>(StringComparer.OrdinalIgnoreCase);
    #endregion

    #region 构造
    /// <summary>实例化Http服务器</summary>
    public HttpServer()
    {
        Name = "Http";
        Port = 80;
        ProtocolType = NetType.Http;

        var ver = GetType().Assembly.GetName().Version ?? new Version();
        ServerName = $"NewLife-HttpServer/{ver.Major}.{ver.Minor}";
    }
    #endregion

    /// <summary>为会话创建网络数据处理器。可作为业务处理实现，也可以作为前置协议解析</summary>
    /// <param name="session">网络会话</param> 
    /// <returns>Http会话处理器</returns>
    public override INetHandler? CreateHandler(INetSession session) => new HttpSession();

    #region 路由注册
    /// <summary>映射路由处理器</summary>
    /// <param name="path">路径，如 /api/test 或 /api/*</param>
    /// <param name="handler">处理器</param>
    public void Map(String path, IHttpHandler handler) => SetRoute(path, handler);

    /// <summary>映射路由处理器（委托）</summary>
    /// <param name="path">路径</param>
    /// <param name="handler">处理委托</param>
    public void Map(String path, HttpProcessDelegate handler) => SetRoute(path, new DelegateHandler { Callback = handler });

    /// <summary>映射路由处理器（无参委托）</summary>
    /// <typeparam name="TResult">返回类型</typeparam>
    /// <param name="path">路径</param>
    /// <param name="handler">处理委托</param>
    public void Map<TResult>(String path, Func<TResult> handler) => SetRoute(path, new DelegateHandler { Callback = handler });

    /// <summary>映射路由处理器（单参数委托）</summary>
    /// <typeparam name="TModel">参数类型</typeparam>
    /// <typeparam name="TResult">返回类型</typeparam>
    /// <param name="path">路径</param>
    /// <param name="handler">处理委托</param>
    public void Map<TModel, TResult>(String path, Func<TModel, TResult> handler) => SetRoute(path, new DelegateHandler { Callback = handler });

    /// <summary>映射路由处理器（2参数委托）</summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <typeparam name="T2">参数2类型</typeparam>
    /// <typeparam name="TResult">返回类型</typeparam>
    /// <param name="path">路径</param>
    /// <param name="handler">处理委托</param>
    public void Map<T1, T2, TResult>(String path, Func<T1, T2, TResult> handler) => SetRoute(path, new DelegateHandler { Callback = handler });

    /// <summary>映射路由处理器（3参数委托）</summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <typeparam name="T2">参数2类型</typeparam>
    /// <typeparam name="T3">参数3类型</typeparam>
    /// <typeparam name="TResult">返回类型</typeparam>
    /// <param name="path">路径</param>
    /// <param name="handler">处理委托</param>
    public void Map<T1, T2, T3, TResult>(String path, Func<T1, T2, T3, TResult> handler) => SetRoute(path, new DelegateHandler { Callback = handler });

    /// <summary>映射路由处理器（4参数委托）</summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <typeparam name="T2">参数2类型</typeparam>
    /// <typeparam name="T3">参数3类型</typeparam>
    /// <typeparam name="T4">参数4类型</typeparam>
    /// <typeparam name="TResult">返回类型</typeparam>
    /// <param name="path">路径</param>
    /// <param name="handler">处理委托</param>
    public void Map<T1, T2, T3, T4, TResult>(String path, Func<T1, T2, T3, T4, TResult> handler) => SetRoute(path, new DelegateHandler { Callback = handler });

    /// <summary>映射控制器</summary>
    /// <typeparam name="TController">控制器类型</typeparam>
    /// <param name="path">可选起始路径，默认 /{ControllerName}</param>
    public void MapController<TController>(String? path = null) => MapController(typeof(TController), path);

    /// <summary>映射控制器</summary>
    /// <param name="controllerType">控制器类型</param>
    /// <param name="path">可选起始路径，默认 /{ControllerName}</param>
    public void MapController(Type controllerType, String? path = null)
    {
        if (controllerType == null) throw new ArgumentNullException(nameof(controllerType));

        if (path.IsNullOrEmpty()) path = "/" + controllerType.Name.TrimEnd("Controller");

        var path2 = path.EnsureStart("/").EnsureEnd("/*");
        SetRoute(path2, new ControllerHandler { ControllerType = controllerType });
    }

    /// <summary>映射静态文件目录</summary>
    /// <param name="path">映射路径，如 /js</param>
    /// <param name="contentPath">内容目录，如 /wwwroot/js</param>
    public void MapStaticFiles(String path, String contentPath)
    {
        if (contentPath.IsNullOrEmpty()) throw new ArgumentNullException(nameof(contentPath));

        path = path.EnsureStart("/");
        var path2 = path.EnsureEnd("/").EnsureEnd("*");
        SetRoute(path2, new StaticFilesHandler { Path = path.EnsureEnd("/"), ContentPath = contentPath });
    }

    /// <summary>统一设置路由。自动处理前导斜杠</summary>
    /// <param name="path">路径</param>
    /// <param name="handler">处理器</param>
    private void SetRoute(String path, IHttpHandler handler)
    {
        if (path.IsNullOrEmpty()) throw new ArgumentNullException(nameof(path));
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        // 统一路径格式：必须以 / 开头
        path = path.EnsureStart("/");
        Routes[path] = handler; // 保持原语义：后注册覆盖
    }
    #endregion

    #region 路由匹配
    /// <summary>路径匹配缓存。Key 为请求路径，Value 为匹配到的路由键</summary>
    private readonly IDictionary<String, String> _pathCache = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);

    /// <summary>匹配处理器</summary>
    /// <param name="path">已规范化后的请求路径（不含查询字符串）</param>
    /// <param name="request">Http请求对象（可用于深度匹配）</param>
    /// <returns>匹配到的处理器；找不到时返回 null</returns>
    public IHttpHandler? MatchHandler(String path, HttpRequest? request)
    {
        if (path.IsNullOrEmpty()) return null;

        // 直接精确匹配
        if (Routes.TryGetValue(path, out var handler)) return handler;

        // 缓存匹配
        if (_pathCache.TryGetValue(path, out var p) && Routes.TryGetValue(p, out handler)) return handler;

        // 模糊匹配（使用快照避免运行期新增导致枚举异常）
        foreach (var item in Routes)
        {
            var key = item.Key;
            if (!key.Contains('*')) continue;
            if (!key.IsMatch(path)) continue;

            if (Routes.TryGetValue(key, out handler))
            {
                // 大于3段的路径不做缓存，避免动态Url引起缓存膨胀（保持原逻辑）
                if (handler is StaticFilesHandler || path.Split('/').Length <= 3) _pathCache[path] = key;
                return handler;
            }
        }

        return null;
    }
    #endregion
}