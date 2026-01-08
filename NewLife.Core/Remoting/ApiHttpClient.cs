using System.Net.Http.Headers;
using NewLife.Configuration;
using NewLife.Data;
using NewLife.Http;
using NewLife.Log;
using NewLife.Model;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Remoting;

/// <summary>Http应用接口客户端</summary>
/// <remarks>
/// ApiHttpClient 是对多个服务地址的包装，底层管理多个 HttpClient，
/// 提供统一的负载均衡和故障转移能力。
/// 
/// 支持三种负载均衡模式：
/// <list type="bullet">
/// <item><description>故障转移（Failover）：优先使用主节点，失败时自动切换到备用节点，过一段时间自动切回</description></item>
/// <item><description>加权轮询（RoundRobin）：按权重分配请求到多个节点，自动屏蔽不可用节点</description></item>
/// <item><description>竞速调用（Race）：并行请求多个节点，取最快响应</description></item>
/// </list>
/// </remarks>
public partial class ApiHttpClient : DisposeBase, IApiClient, IConfigMapping, ILogFeature, ITracerFeature
{
    #region 属性
    /// <summary>令牌。每次请求携带</summary>
    public String? Token { get; set; }

    /// <summary>超时时间。默认15000ms</summary>
    public Int32 Timeout { get; set; } = 15_000;

    /// <summary>是否使用系统代理设置。默认false不检查系统代理设置，在某些系统上可以大大改善初始化速度</summary>
    public Boolean UseProxy { get; set; }

    /// <summary>负载均衡器</summary>
    public ILoadBalancer LoadBalancer { get; private set; }

    /// <summary>负载均衡模式。默认Failover故障转移</summary>
    public LoadBalanceMode LoadBalanceMode
    {
        get => LoadBalancer.Mode;
        set
        {
            if (LoadBalancer.Mode != value) LoadBalancer = CreateLoadBalancer(value);
        }
    }

    /// <summary>加权轮询负载均衡。默认false只使用故障转移</summary>
    [Obsolete("请使用 LoadBalanceMode 属性")]
    public Boolean RoundRobin
    {
        get => LoadBalanceMode == LoadBalanceMode.RoundRobin;
        set => LoadBalanceMode = value ? LoadBalanceMode.RoundRobin : LoadBalanceMode.Failover;
    }

    /// <summary>不可用节点的屏蔽时间。默认60秒</summary>
    public Int32 ShieldingTime
    {
        get => LoadBalancer.ShieldingTime;
        set
        {
            LoadBalancer.ShieldingTime = value;
            _shieldingTime = value;
        }
    }
    private Int32 _shieldingTime = 60;

    /// <summary>身份验证</summary>
    public AuthenticationHeaderValue? Authentication { get; set; }

    /// <summary>证书验证。进行SSL通信时，是否验证证书有效性，默认false不验证</summary>
    public Boolean CertificateValidation { get; set; }

    /// <summary>默认用户浏览器UserAgent。默认为空，可取值HttpHelper.DefaultUserAgent</summary>
    public String? DefaultUserAgent { get; set; }

    /// <summary>Json序列化主机</summary>
    public IJsonHost? JsonHost { get; set; }

    /// <summary>服务提供者。创建控制器实例时使用，可实现依赖注入。务必在注册控制器之前设置该属性</summary>
    public IServiceProvider? ServiceProvider { get; set; }

    /// <summary>创建请求时触发</summary>
    public event EventHandler<HttpRequestEventArgs>? OnRequest;

    /// <summary>创建客户端时触发</summary>
    public event EventHandler<HttpClientEventArgs>? OnCreateClient;

    /// <summary>Http过滤器</summary>
    public IHttpFilter? Filter { get; set; }

    /// <summary>状态码字段名。例如code/status等</summary>
    public String? CodeName { get; set; }

    /// <summary>数据体字段名。例如data/result等</summary>
    public String? DataName { get; set; }

    /// <summary>服务器源。正在使用的服务器</summary>
    public String? Source { get; private set; }

    /// <summary>调用统计</summary>
    public ICounter? StatInvoke { get; set; }

    /// <summary>慢追踪。远程调用或处理时间超过该值时，输出慢调用日志，默认5000ms</summary>
    public Int32 SlowTrace { get; set; } = 5_000;

    /// <summary>跟踪器</summary>
    public ITracer? Tracer { get; set; }

    /// <summary>服务列表。用于负载均衡和故障转移</summary>
    public IList<ServiceEndpoint> Services { get; set; } = [];

    /// <summary>当前服务</summary>
    protected ServiceEndpoint? _currentService;

    /// <summary>正在使用的服务点。最后一次调用成功的服务点，可获取其地址以及状态信息</summary>
    public ServiceEndpoint? Current { get; private set; }
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    public ApiHttpClient()
    {
        LoadBalancer = CreateLoadBalancer(LoadBalanceMode.Failover);
    }

    /// <summary>实例化</summary>
    /// <param name="urls">地址集合。多地址逗号分隔，支持权重，test1=3*http://127.0.0.1:1234,test2=7*http://127.0.0.1:3344</param>
    public ApiHttpClient(String urls) : this() => SetServer(urls);

    /// <summary>按照配置服务实例化，用于NETCore依赖注入</summary>
    /// <param name="serviceProvider">服务提供者，将要解析IConfigProvider</param>
    /// <param name="name">缓存名称，也是配置中心key</param>
    public ApiHttpClient(IServiceProvider serviceProvider, String name) : this()
    {
        ServiceProvider = serviceProvider;
        var configProvider = serviceProvider.GetRequiredService<IConfigProvider>();
        configProvider.Bind(this, true, name);
    }

    /// <summary>创建负载均衡器</summary>
    /// <remarks>使用者可以继承并扩展其它负载均衡模式</remarks>
    /// <param name="mode">负载均衡模式</param>
    /// <returns></returns>
    protected virtual ILoadBalancer CreateLoadBalancer(LoadBalanceMode mode)
    {
        var lb = mode switch
        {
            LoadBalanceMode.RoundRobin => (ILoadBalancer)new WeightedRoundRobinLoadBalancer(),
            LoadBalanceMode.Race => new RaceLoadBalancer(),
            _ => new FailoverLoadBalancer(),
        };

        if (lb is LoadBalancerBase lbb)
        {
            lbb.ShieldingTime = _shieldingTime;
            lbb.Log = Log;
        }

        return lb;
    }
    #endregion

    #region 方法
    /// <summary>添加服务地址</summary>
    /// <param name="name">名称</param>
    /// <param name="address">地址，支持名称和权重，test1=3*http://127.0.0.1:1234</param>
    public ServiceEndpoint Add(String name, String address) => ParseAndAdd(Services, name, address);

    /// <summary>添加服务地址</summary>
    /// <param name="name">名称</param>
    /// <param name="uri">地址，支持名称和权重，test1=3*http://127.0.0.1:1234</param>
    public ServiceEndpoint Add(String name, Uri uri)
    {
        var svc = new ServiceEndpoint { Name = name };
        svc.SetAddress(uri);

        Services.Add(svc);

        return svc;
    }

    private static ServiceEndpoint ParseAndAdd(IList<ServiceEndpoint> services, String name, String address, Int32 weight = 0)
    {
        var url = address;
        var svc = new ServiceEndpoint
        {
            Name = name
        };

        // master=3*http://newlifex.com
        var p = url.IndexOf("://");
        if (p > 0)
        {
            // 解析名称
            var p2 = url.IndexOf('=');
            if (p2 > 0 && p2 < p)
            {
                svc.Name = url[..p2];
                url = url[(p2 + 1)..];
            }

            // 解析权重
            p = url.IndexOf("://");
            p2 = url.IndexOf("*http", StringComparison.OrdinalIgnoreCase);
            if (p2 > 0 && p2 < p)
            {
                svc.Weight = url[..p2].ToInt();
                url = url[(p2 + 1)..];
            }
        }

        p = url.IndexOf("#token=", StringComparison.OrdinalIgnoreCase);
        if (p > 0)
        {
            svc.Token = url[(p + 7)..];
            url = url[..p];
        }

        //svc.Address = new Uri(url);
        svc.SetAddress(new Uri(url));
        if (svc.Weight <= 1 && weight > 0) svc.Weight = weight;

        services.Add(svc);

        return svc;
    }

    private String? _lastUrls;
    /// <summary>设置服务端地址。如果新地址跟旧地址不同，将会替换旧地址构造的Services</summary>
    /// <param name="urls">地址集。多个地址逗号隔开</param>
    public void SetServer(String urls)
    {
        if (!urls.IsNullOrEmpty() && urls != _lastUrls)
        {
            var services = new List<ServiceEndpoint>();
            var ss = urls.Split(',', StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < ss.Length; i++)
            {
                if (!ss[i].IsNullOrEmpty()) ParseAndAdd(services, "service" + (i + 1), ss[i]);
            }
            Services = services;
            _lastUrls = urls;
        }
    }

    /// <summary>添加服务端地址</summary>
    /// <param name="prefix">名称前缀</param>
    /// <param name="urls">地址集。多个地址逗号隔开</param>
    /// <param name="weight">权重</param>
    public IEnumerable<ServiceEndpoint> AddServer(String prefix, String urls, Int32 weight = 0)
    {
        if (prefix.IsNullOrEmpty()) prefix = "service";

        var idx = 0;
        var ss = urls.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var services = Services;
        foreach (var addr in ss)
        {
            if (addr.IsNullOrEmpty()) continue;

            var name = "";
            while (name.IsNullOrEmpty() || services.Any(e => e.Name == name)) name = prefix + ++idx;

            var svc = ParseAndAdd(services, name, addr, weight);
            if (svc != null) yield return svc;
        }
    }

    void IConfigMapping.MapConfig(IConfigProvider provider, IConfigSection section)
    {
        if (section != null && section.Value != null) SetServer(section.Value);
    }
    #endregion

    #region 核心方法
    /// <summary>异步获取，参数构造在Url</summary>
    /// <param name="action">服务操作</param>
    /// <param name="args">参数</param>
    /// <returns></returns>
    public Task<TResult?> GetAsync<TResult>(String action, Object? args = null) => InvokeAsync<TResult>(HttpMethod.Get, action, args);

    /// <summary>同步获取，参数构造在Url</summary>
    /// <param name="action">服务操作</param>
    /// <param name="args">参数</param>
    /// <returns></returns>
    public TResult? Get<TResult>(String action, Object? args = null) => GetAsync<TResult>(action, args).ConfigureAwait(false).GetAwaiter().GetResult();

    /// <summary>异步提交，参数Json打包在Body</summary>
    /// <param name="action">服务操作</param>
    /// <param name="args">参数</param>
    /// <returns></returns>
    public Task<TResult?> PostAsync<TResult>(String action, Object? args = null) => InvokeAsync<TResult>(HttpMethod.Post, action, args);

    /// <summary>同步提交，参数Json打包在Body</summary>
    /// <param name="action">服务操作</param>
    /// <param name="args">参数</param>
    /// <returns></returns>
    public TResult? Post<TResult>(String action, Object? args = null) => PostAsync<TResult>(action, args).ConfigureAwait(false).GetAwaiter().GetResult();

    /// <summary>异步上传，参数Json打包在Body</summary>
    /// <param name="action">服务操作</param>
    /// <param name="args">参数</param>
    /// <returns></returns>
    public Task<TResult?> PutAsync<TResult>(String action, Object? args = null) => InvokeAsync<TResult>(HttpMethod.Put, action, args);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP
    /// <summary>异步修改，参数Json打包在Body</summary>
    /// <param name="action">服务操作</param>
    /// <param name="args">参数</param>
    /// <returns></returns>
    public Task<TResult?> PatchAsync<TResult>(String action, Object? args = null) => InvokeAsync<TResult>(HttpMethod.Patch, action, args);
#else
    /// <summary>异步修改，参数Json打包在Body</summary>
    /// <param name="action">服务操作</param>
    /// <param name="args">参数</param>
    /// <returns></returns>
    public Task<TResult?> PatchAsync<TResult>(String action, Object? args = null) => InvokeAsync<TResult>(new HttpMethod("Patch"), action, args);
#endif

    /// <summary>异步删除，参数Json打包在Body</summary>
    /// <param name="action">服务操作</param>
    /// <param name="args">参数</param>
    /// <returns></returns>
    public Task<TResult?> DeleteAsync<TResult>(String action, Object? args = null) => InvokeAsync<TResult>(HttpMethod.Delete, action, args);

    /// <summary>异步调用，等待返回结果</summary>
    /// <typeparam name="TResult">返回类型</typeparam>
    /// <param name="method">请求方法</param>
    /// <param name="action">服务操作</param>
    /// <param name="args">参数</param>
    /// <param name="onRequest">请求头回调</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    public virtual async Task<TResult?> InvokeAsync<TResult>(HttpMethod method, String action, Object? args = null, Action<HttpRequestMessage>? onRequest = null, CancellationToken cancellationToken = default)
    {
        var returnType = typeof(TResult);
        var svrs = Services;

        // Api调用埋点，记录整体调用。内部Http调用可能首次失败，下一次成功，整体Api调用算作成功
        using var span = Tracer?.NewSpan(action, args);

        for (var i = 0; i < svrs.Count; i++)
        {
            // 建立请求
            var request = BuildRequest(method, action, args, returnType);
            onRequest?.Invoke(request);

            var filter = Filter;
            try
            {
                using var msg = await SendAsync(request, cancellationToken).ConfigureAwait(false);

                var jsonHost = JsonHost ?? ServiceProvider?.GetService<IJsonHost>() ?? JsonHelper.Default;
                return await ApiHelper.ProcessResponse<TResult>(msg, CodeName, DataName, jsonHost).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                span?.AppendTag(ex.Message);

                while (ex is AggregateException age && age.InnerException != null) ex = age.InnerException;
                ex.Source = _currentService?.Address + "/" + action;

                var client = _currentService?.Client;
                if (client != null && filter != null)
                    await filter.OnError(client, ex, this, cancellationToken).ConfigureAwait(false);

                // 网络异常时，自动切换到其它节点
                if (ex is HttpRequestException or TaskCanceledException && i + 1 < svrs.Count) continue;

                span?.SetError(ex, null);
                throw;
            }
        }

        // 无法到达这里
        throw new InvalidOperationException();
    }

    /// <summary>异步调用，等待返回结果</summary>
    /// <typeparam name="TResult">返回类型</typeparam>
    /// <param name="action">服务操作</param>
    /// <param name="args">参数</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    public Task<TResult?> InvokeAsync<TResult>(String action, Object? args, CancellationToken cancellationToken)
    {
        var method = HttpMethod.Post;
#if NETCOREAPP || NETSTANDARD2_1
        if (args == null || args.GetType().IsBaseType() || action.StartsWithIgnoreCase("Get") || action.Contains("/get", StringComparison.OrdinalIgnoreCase))
            method = HttpMethod.Get;
#else
        if (args == null || args.GetType().IsBaseType() || action.StartsWithIgnoreCase("Get") || action.IndexOf("/get", StringComparison.OrdinalIgnoreCase) >= 0)
            method = HttpMethod.Get;
#endif

        return InvokeAsync<TResult>(method, action, args, null, cancellationToken);
    }

    /// <summary>同步调用，阻塞等待</summary>
    /// <param name="action">服务操作</param>
    /// <param name="args">参数</param>
    /// <returns></returns>
    public TResult? Invoke<TResult>(String action, Object? args) => InvokeAsync<TResult>(action, args, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

    /// <summary>下载文件到本地并校验哈希（可取消）</summary>
    /// <param name="requestUri">请求资源地址</param>
    /// <param name="fileName">目标文件名</param>
    /// <param name="expectedHash">预期哈希字符串，支持带算法前缀或自动识别</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    public virtual async Task DownloadFileAsync(String requestUri, String fileName, String? expectedHash, CancellationToken cancellationToken = default)
    {
        var svrs = Services;

        // Api调用埋点，记录整体调用。内部Http调用可能首次失败，下一次成功，整体Api调用算作成功
        var action = requestUri;
        if (requestUri.StartsWithIgnoreCase("http://", "https://"))
            action = new Uri(requestUri).AbsolutePath.TrimStart('/');
        using var span = Tracer?.NewSpan(action, expectedHash);

        for (var i = 0; i < svrs.Count; i++)
        {
            // 建立请求
            var request = BuildRequest(HttpMethod.Get, requestUri, null, null);

            var filter = Filter;
            try
            {
                using var rs = await SendAsync(request, cancellationToken).ConfigureAwait(false);
                rs.EnsureSuccessStatusCode();

#if NET5_0_OR_GREATER
                var stream = await rs.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#else
                var stream = await rs.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif

                await HttpHelper.SaveFileAsync(stream, fileName, expectedHash, cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (Exception ex)
            {
                span?.AppendTag(ex.Message);

                while (ex is AggregateException age && age.InnerException != null) ex = age.InnerException;
                ex.Source = _currentService?.Address + "/" + action;

                var client = _currentService?.Client;
                if (client != null && filter != null)
                    await filter.OnError(client, ex, this, cancellationToken).ConfigureAwait(false);

                // 网络异常时，自动切换到其它节点
                if (ex is HttpRequestException or TaskCanceledException && i + 1 < svrs.Count) continue;

                span?.SetError(ex, null);
                throw;
            }
        }
    }
    #endregion

    #region 构造请求
    /// <summary>建立请求</summary>
    /// <param name="method">请求方法</param>
    /// <param name="action">服务操作</param>
    /// <param name="args">参数</param>
    /// <param name="returnType">返回类型</param>
    /// <returns></returns>
    protected virtual HttpRequestMessage BuildRequest(HttpMethod method, String action, Object? args, Type? returnType)
    {
        HttpRequestMessage request;
        if (args == null)
            request = new HttpRequestMessage(method, action);
        else
        {
            var jsonHost = JsonHost ?? ServiceProvider?.GetService<IJsonHost>() ?? JsonHelper.Default;
            request = ApiHelper.BuildRequest(method, action, args, jsonHost);
        }

        if (returnType != null)
        {
            // 指定返回类型
#pragma warning disable CS0618 // 类型或成员已过时
            if (returnType == typeof(Byte[]) || returnType == typeof(IPacket) || returnType == typeof(Packet))
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
            else
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
#pragma warning restore CS0618 // 类型或成员已过时
        }

        //// 压缩
        //if (Compressed) request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

        // 加上令牌或其它身份验证
        var auth = Authentication;
        if (auth == null && !Token.IsNullOrEmpty()) auth = new AuthenticationHeaderValue("Bearer", Token);
        if (auth != null) request.Headers.Authorization = auth;

        OnRequest?.Invoke(this, new HttpRequestEventArgs { Request = request });

        return request;
    }
    #endregion

    #region 调度池
    /// <summary>异步发送</summary>
    /// <param name="request">请求</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    protected virtual async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (Services.Count == 0) throw new InvalidOperationException("Service address not added!");

        // 获取一个处理当前请求的服务，使用负载均衡器
        var service = LoadBalancer.GetService(Services) ?? throw new InvalidOperationException("No available service nodes!");
        Source = service.Name;
        _currentService = service;

        DefaultSpan.Current?.AppendTag($"[{service.Name}]={service.Address}");

        // 性能计数器，次数、TPS、平均耗时
        var st = StatInvoke;
        var sw = st?.StartCount();
        Exception? error = null;
        try
        {
            var client = EnsureClient(service);

            var rs = await SendOnServiceAsync(request, service, client, false, cancellationToken).ConfigureAwait(false);

            // 调用成功，当前服务点可用
            Current = service;

            return rs;
        }
        catch (Exception ex)
        {
            error = ex;

            throw;
        }
        finally
        {
            if (st != null)
            {
                var msCost = st.StopCount(sw) / 1000;
                if (SlowTrace > 0 && msCost >= SlowTrace) WriteLog($"慢调用[{request.RequestUri?.AbsoluteUri}]，耗时{msCost:n0}ms");
            }

            // 归还服务
            LoadBalancer.PutService(Services, service, error);
        }
    }

    /// <summary>在指定服务地址上发生请求</summary>
    /// <param name="request">请求消息</param>
    /// <param name="service">服务名</param>
    /// <param name="client">客户端</param>
    /// <param name="onlyHeader">仅头部响应</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    protected virtual async Task<HttpResponseMessage> SendOnServiceAsync(HttpRequestMessage request, ServiceEndpoint service, HttpClient client, Boolean onlyHeader, CancellationToken cancellationToken)
    {
        var filter = Filter;
        if (filter != null) await filter.OnRequest(client, request, this, cancellationToken).ConfigureAwait(false);

        var completionOption = onlyHeader ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead;
        var response = await client.SendAsync(request, completionOption, cancellationToken).ConfigureAwait(false);

        if (filter != null) await filter.OnResponse(client, response, this, cancellationToken).ConfigureAwait(false);

        //// 业务层只会返回200 OK
        //response.EnsureSuccessStatusCode();

        return response;
    }

    /// <summary>确保服务有可用的 HttpClient</summary>
    /// <param name="service">服务</param>
    /// <returns></returns>
    internal HttpClient EnsureClient(ServiceEndpoint service)
    {
        var client = service.Client;
        if (client == null)
        {
            if (service.CreateTime.Year < 2000) Log?.Debug("使用[{0}]：{1}", service.Name, service.Address);

            client = CreateClient();
            client.BaseAddress = service.Address;
            if (!service.Token.IsNullOrEmpty()) Token = service.Token;

            service.Client = client;
            service.CreateTime = DateTime.Now;
        }

        // 正常使用不会满足这个条件，仅用于单元测试
        if (client.BaseAddress == null) client.BaseAddress = service.Address;

        return client;
    }

    /// <summary>创建客户端</summary>
    /// <returns></returns>
    protected virtual HttpClient CreateClient()
    {
        var handler = HttpHelper.CreateHandler(UseProxy, false, !CertificateValidation);

        if (Tracer != null) handler = new HttpTraceHandler(handler) { Tracer = Tracer };

        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMilliseconds(Timeout)
        };

        //// 默认UserAgent
        //client.SetUserAgent();

        var userAgent = DefaultUserAgent;
        if (!userAgent.IsNullOrEmpty()) client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);

        OnCreateClient?.Invoke(this, new HttpClientEventArgs { Client = client });

        return client;
    }
    #endregion

    #region 日志
    /// <summary>日志</summary>
    public ILog Log { get; set; } = Logger.Null;

    /// <summary>写日志</summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="args">参数</param>
    public void WriteLog(String format, params Object?[] args) => Log?.Info(format, args);
    #endregion
}