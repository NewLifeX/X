using System.ComponentModel;
using NewLife.Net;

namespace NewLife.Http;

/// <summary>Http服务器</summary>
/// <remarks>
/// 主要职责：
/// 1. 路由映射：支持精确路径、参数化路由 {param}、通配符 * 三种模式；
/// 2. 中间件管道：通过 Use() 注册中间件，按顺序执行；
/// 3. 方法感知注册：MapGet/MapPost/MapPut/MapDelete 按 HTTP 方法过滤；
/// 4. 为每个网络会话创建对应的 <see cref="HttpSession"/> 协议处理器。
/// 
/// 线程安全说明：
/// - 典型使用场景下，路由在启动阶段集中注册，运行期只读访问；
/// - 若需要在运行期动态增删路由，应在外部自行序列化（加锁）调用 Map 方法。
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
    /// <param name="path">路径，如 /api/test、/api/{id} 或 /api/*</param>
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

    /// <summary>映射 GET 路由</summary>
    /// <param name="path">路径，支持 {param} 参数化</param>
    /// <param name="handler">处理委托</param>
    public void MapGet(String path, HttpProcessDelegate handler) => SetRoute(path, new MethodFilterHandler("GET", new DelegateHandler { Callback = handler }));

    /// <summary>映射 GET 路由（无参）</summary>
    public void MapGet<TResult>(String path, Func<TResult> handler) => SetRoute(path, new MethodFilterHandler("GET", new DelegateHandler { Callback = handler }));

    /// <summary>映射 GET 路由（单参）</summary>
    public void MapGet<TModel, TResult>(String path, Func<TModel, TResult> handler) => SetRoute(path, new MethodFilterHandler("GET", new DelegateHandler { Callback = handler }));

    /// <summary>映射 POST 路由</summary>
    /// <param name="path">路径，支持 {param} 参数化</param>
    /// <param name="handler">处理委托</param>
    public void MapPost(String path, HttpProcessDelegate handler) => SetRoute(path, new MethodFilterHandler("POST", new DelegateHandler { Callback = handler }));

    /// <summary>映射 POST 路由（无参）</summary>
    public void MapPost<TResult>(String path, Func<TResult> handler) => SetRoute(path, new MethodFilterHandler("POST", new DelegateHandler { Callback = handler }));

    /// <summary>映射 POST 路由（单参）</summary>
    public void MapPost<TModel, TResult>(String path, Func<TModel, TResult> handler) => SetRoute(path, new MethodFilterHandler("POST", new DelegateHandler { Callback = handler }));

    /// <summary>映射 PUT 路由</summary>
    public void MapPut(String path, HttpProcessDelegate handler) => SetRoute(path, new MethodFilterHandler("PUT", new DelegateHandler { Callback = handler }));

    /// <summary>映射 PUT 路由（无参）</summary>
    public void MapPut<TResult>(String path, Func<TResult> handler) => SetRoute(path, new MethodFilterHandler("PUT", new DelegateHandler { Callback = handler }));

    /// <summary>映射 PUT 路由（单参）</summary>
    public void MapPut<TModel, TResult>(String path, Func<TModel, TResult> handler) => SetRoute(path, new MethodFilterHandler("PUT", new DelegateHandler { Callback = handler }));

    /// <summary>映射 DELETE 路由</summary>
    public void MapDelete(String path, HttpProcessDelegate handler) => SetRoute(path, new MethodFilterHandler("DELETE", new DelegateHandler { Callback = handler }));

    /// <summary>映射 DELETE 路由（无参）</summary>
    public void MapDelete<TResult>(String path, Func<TResult> handler) => SetRoute(path, new MethodFilterHandler("DELETE", new DelegateHandler { Callback = handler }));

    /// <summary>映射 DELETE 路由（单参）</summary>
    public void MapDelete<TModel, TResult>(String path, Func<TModel, TResult> handler) => SetRoute(path, new MethodFilterHandler("DELETE", new DelegateHandler { Callback = handler }));

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

        if (path.IsNullOrEmpty()) path = "/" + controllerType.Name.TrimSuffix("Controller");

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

    /// <summary>映射内嵌资源目录</summary>
    /// <remarks>
    /// 将程序集中的嵌入资源映射为 HTTP 静态文件服务。
    /// 例如 MapEmbedded("/panel", "MyApp.Resources.Panel") 将 /panel/ 下的请求映射到嵌入资源。
    /// </remarks>
    /// <param name="path">映射路径，如 /panel</param>
    /// <param name="contentPath">资源名前缀，如 MyApp.Resources.Panel</param>
    /// <param name="assembly">资源所在的程序集。为 null 时使用入口程序集</param>
    public void MapEmbedded(String path, String contentPath, System.Reflection.Assembly? assembly = null)
    {
        if (contentPath.IsNullOrEmpty()) throw new ArgumentNullException(nameof(contentPath));

        path = path.EnsureStart("/");
        var path2 = path.EnsureEnd("/").EnsureEnd("*");
        SetRoute(path2, new EmbeddedFileHandler { Path = path.EnsureEnd("/"), ContentPath = contentPath, Assembly = assembly });
    }

    /// <summary>映射内嵌资源目录（泛型重载，从类型推断程序集）</summary>
    /// <typeparam name="T">目标程序集中的任一类型</typeparam>
    /// <param name="path">映射路径，如 /panel</param>
    /// <param name="contentPath">资源名前缀，如 MyApp.Resources.Panel</param>
    public void MapEmbedded<T>(String path, String contentPath) => MapEmbedded(path, contentPath, typeof(T).Assembly);

    /// <summary>统一设置路由。自动识别 {param} 语法注册到参数化路由器</summary>
    /// <param name="path">路径</param>
    /// <param name="handler">处理器</param>
    private void SetRoute(String path, IHttpHandler handler)
    {
        if (path.IsNullOrEmpty()) throw new ArgumentNullException(nameof(path));
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        // 统一路径格式：必须以 / 开头
        path = path.EnsureStart("/");

        // 参数化路由（含 {param} 语法）注册到 HttpRouter
        if (path.Contains('{'))
            _router.Register(path, handler);
        else
            Routes[path] = handler; // 精确/通配符路由保持原语义
    }
    #endregion

    #region 中间件
    private readonly List<HttpMiddlewareDelegate> _middlewares = [];

    /// <summary>注册中间件。中间件按注册顺序依次执行，可在处理器前后添加逻辑</summary>
    /// <param name="middleware">中间件委托：第一个参数为上下文，第二个为调用下一中间件的委托</param>
    /// <remarks>
    /// <code>
    /// server.Use(async (ctx, next) => {
    ///     // 请求前逻辑
    ///     await next();
    ///     // 请求后逻辑
    /// });
    /// </code>
    /// </remarks>
    public void Use(HttpMiddlewareDelegate middleware)
    {
        if (middleware == null) throw new ArgumentNullException(nameof(middleware));
        _middlewares.Add(middleware);
    }

    /// <summary>启用 CORS 跨域支持</summary>
    /// <param name="allowOrigin">允许的源，默认 *</param>
    /// <param name="allowMethods">允许的方法</param>
    /// <param name="allowHeaders">允许的请求头</param>
    public void UseCors(String allowOrigin = "*", String allowMethods = "GET, POST, PUT, DELETE, OPTIONS", String allowHeaders = "Content-Type, Authorization, X-Requested-With")
    {
        var cors = new CorsMiddleware
        {
            AllowOrigin = allowOrigin,
            AllowMethods = allowMethods,
            AllowHeaders = allowHeaders
        };
        Use(cors.Invoke);
    }

    /// <summary>启用全局错误处理中间件（推荐在最外层注册）</summary>
    /// <param name="includeDetails">是否返回详细异常信息（开发环境建议true）</param>
    public void UseErrorHandler(Boolean includeDetails = false)
    {
        var handler = new ErrorHandlerMiddleware { IncludeDetails = includeDetails };
        Use(handler.Invoke);
    }

    /// <summary>构建中间件管道，将最终处理器包装为管道末端</summary>
    /// <param name="context">Http上下文</param>
    /// <param name="finalHandler">最终业务处理器（可为null）</param>
    /// <returns>可等待的任务</returns>
    internal Task ExecutePipeline(IHttpContext context, IHttpHandler? finalHandler)
    {
        // 无中间件时直接执行处理器
        if (_middlewares.Count == 0)
        {
            finalHandler?.ProcessRequest(context);
            return TaskEx.CompletedTask;
        }

        // 构建管道链：middleware1 → middleware2 → ... → finalHandler
        Func<Task> handler = () =>
        {
            finalHandler?.ProcessRequest(context);
            return TaskEx.CompletedTask;
        };

        // 反向构建：最内层是 handler，往外逐层包装中间件
        for (var i = _middlewares.Count - 1; i >= 0; i--)
        {
            var middleware = _middlewares[i];
            var next = handler;
            handler = () => middleware(context, next);
        }

        return handler();
    }
    #endregion

    #region 路由匹配
    /// <summary>参数化路由器，处理 {param} 模式路由</summary>
    private readonly HttpRouter _router = new();

    /// <summary>路径匹配缓存。Key 为请求路径，Value 为匹配到的路由键</summary>
    private readonly IDictionary<String, String> _pathCache = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);

    /// <summary>匹配处理器（兼容 IHttpHost 接口）</summary>
    /// <param name="path">已规范化后的请求路径（不含查询字符串）</param>
    /// <param name="request">Http请求对象（可用于深度匹配）</param>
    /// <returns>匹配到的处理器；找不到时返回 null</returns>
    public IHttpHandler? MatchHandler(String path, HttpRequest? request)
    {
        // 使用临时字典兼容旧接口（参数化路由的参数不会丢失，因为调用方会通过新重载获取）
        var tempParams = new Dictionary<String, Object?>();
        return MatchHandler(path, request, tempParams);
    }

    /// <summary>匹配处理器（增强版，同时输出路由参数）</summary>
    /// <param name="path">已规范化后的请求路径（不含查询字符串）</param>
    /// <param name="request">Http请求对象（可用于深度匹配）</param>
    /// <param name="parameters">输出路由参数（如 {id}=123）</param>
    /// <returns>匹配到的处理器；找不到时返回 null</returns>
    public IHttpHandler? MatchHandler(String path, HttpRequest? request, IDictionary<String, Object?> parameters)
    {
        if (path.IsNullOrEmpty()) return null;

        // 直接精确匹配
        if (Routes.TryGetValue(path, out var handler)) return handler;

        // 缓存匹配
        if (_pathCache.TryGetValue(path, out var p) && Routes.TryGetValue(p, out handler)) return handler;

        // 参数化路由匹配（{param} 模式）
        handler = _router.Match(path, parameters);
        if (handler != null)
        {
            // 参数化路由结果也缓存
            if (path.Split('/').Length <= 3) _pathCache[path] = path;
            return handler;
        }

        // 模糊匹配（* 通配符模式）
        foreach (var item in Routes)
        {
            var key = item.Key;
            if (!key.Contains('*')) continue;
            if (!key.IsMatch(path)) continue;

            if (Routes.TryGetValue(key, out handler))
            {
                // 大于3段的路径不做缓存，避免动态Url引起缓存膨胀
                if (handler is StaticFilesHandler || path.Split('/').Length <= 3) _pathCache[path] = key;
                return handler;
            }
        }

        return null;
    }
    #endregion
}