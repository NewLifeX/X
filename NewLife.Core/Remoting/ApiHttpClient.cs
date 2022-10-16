using System.Net;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using NewLife.Configuration;
using NewLife.Data;
using NewLife.Http;
using NewLife.Log;
using NewLife.Model;

namespace NewLife.Remoting;

/// <summary>Http应用接口客户端</summary>
public class ApiHttpClient : DisposeBase, IApiClient, IConfigMapping, ILogFeature
{
    #region 属性
    /// <summary>令牌。每次请求携带</summary>
    public String Token { get; set; }

    /// <summary>超时时间。默认15000ms</summary>
    public Int32 Timeout { get; set; } = 15_000;

    /// <summary>是否使用系统代理设置。默认false不检查系统代理设置，在某些系统上可以大大改善初始化速度</summary>
    public Boolean UseProxy { get; set; }

    /// <summary>是否使用压缩。默认true</summary>
    /// <remarks>将来可能取消该设置项，默认启用压缩</remarks>
    public Boolean Compressed { get; set; } = true;

    /// <summary>加权轮询负载均衡。默认false只使用故障转移</summary>
    public Boolean RoundRobin { get; set; }

    /// <summary>不可用节点的屏蔽时间。默认60秒</summary>
    public Int32 ShieldingTime { get; set; } = 60;

    /// <summary>身份验证</summary>
    public AuthenticationHeaderValue Authentication { get; set; }

    /// <summary>Http过滤器</summary>
    public IHttpFilter Filter { get; set; }

    /// <summary>状态码字段名。例如code/status等</summary>
    public String CodeName { get; set; }

    /// <summary>数据体字段名。例如data/result等</summary>
    public String DataName { get; set; }

    /// <summary>服务器源。正在使用的服务器</summary>
    public String Source { get; private set; }

    /// <summary>调用统计</summary>
    public ICounter StatInvoke { get; set; }

    /// <summary>慢追踪。远程调用或处理时间超过该值时，输出慢调用日志，默认5000ms</summary>
    public Int32 SlowTrace { get; set; } = 5_000;

    /// <summary>跟踪器</summary>
    public ITracer Tracer { get; set; }

    /// <summary>服务列表。用于负载均衡和故障转移</summary>
    public IList<Service> Services { get; set; } = new List<Service>();

    /// <summary>当前服务</summary>
    protected Service _currentService;
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    public ApiHttpClient() => Compressed = Net.Setting.Current.EnableHttpCompression;

    /// <summary>实例化</summary>
    /// <param name="urls">地址集合。多地址逗号分隔，支持权重，test1=3*http://127.0.0.1:1234,test2=7*http://127.0.0.1:3344</param>
    public ApiHttpClient(String urls) : this() => Init(urls);

    /// <summary>按照配置服务实例化，用于NETCore依赖注入</summary>
    /// <param name="provider">服务提供者，将要解析IConfigProvider</param>
    /// <param name="name">缓存名称，也是配置中心key</param>
    public ApiHttpClient(IServiceProvider provider, String name) : this()
    {
        //Name = name;

        var configProvider = provider.GetRequiredService<IConfigProvider>();
        configProvider.Bind(this, true, name);
    }

    ///// <summary>销毁</summary>
    ///// <param name="disposing"></param>
    //protected override void Dispose(Boolean disposing)
    //{
    //    base.Dispose(disposing);

    //    foreach (var item in Services)
    //    {
    //        item.Client?.TryDispose();
    //    }
    //}
    #endregion

    #region 方法
    /// <summary>添加服务地址</summary>
    /// <param name="name">名称</param>
    /// <param name="address">地址，支持名称和权重，test1=3*http://127.0.0.1:1234</param>
    public void Add(String name, String address) => ParseAndAdd(Services, name, address);

    /// <summary>添加服务地址</summary>
    /// <param name="name"></param>
    /// <param name="address"></param>
    public void Add(String name, Uri address) => Services.Add(new Service { Name = name, Address = address });

    private void ParseAndAdd(IList<Service> services, String name, String address)
    {
        var url = address;
        var svc = new Service
        {
            Name = name
        };

        // 解析名称
        var p = url.IndexOf('=');
        if (p > 0)
        {
            svc.Name = url[..p];
            url = url[(p + 1)..];
        }

        // 解析权重
        p = url.IndexOf("*http");
        if (p > 0)
        {
            svc.Weight = url[..p].ToInt();
            url = url[(p + 1)..];
        }

        svc.Address = new Uri(url);
        services.Add(svc);
    }

    private String _lastUrls;
    private void Init(String urls)
    {
        if (!urls.IsNullOrEmpty() && urls != _lastUrls)
        {
            var services = new List<Service>();
            var ss = urls.Split(',', StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < ss.Length; i++)
            {
                if (!ss[i].IsNullOrEmpty()) ParseAndAdd(services, "service" + (i + 1), ss[i]);
            }
            Services = services;
            _lastUrls = urls;
        }
    }

    void IConfigMapping.MapConfig(IConfigProvider provider, IConfigSection section)
    {
        if (section != null && section.Value != null) Init(section.Value);
    }
    #endregion

    #region 核心方法
    /// <summary>异步获取，参数构造在Url</summary>
    /// <param name="action">服务操作</param>
    /// <param name="args">参数</param>
    /// <returns></returns>
    public async Task<TResult> GetAsync<TResult>(String action, Object args = null) => await InvokeAsync<TResult>(HttpMethod.Get, action, args);

    /// <summary>同步获取，参数构造在Url</summary>
    /// <param name="action">服务操作</param>
    /// <param name="args">参数</param>
    /// <returns></returns>
    public TResult Get<TResult>(String action, Object args = null) => Task.Run(() => GetAsync<TResult>(action, args)).Result;

    /// <summary>异步提交，参数Json打包在Body</summary>
    /// <param name="action">服务操作</param>
    /// <param name="args">参数</param>
    /// <returns></returns>
    public async Task<TResult> PostAsync<TResult>(String action, Object args = null) => await InvokeAsync<TResult>(HttpMethod.Post, action, args);

    /// <summary>同步提交，参数Json打包在Body</summary>
    /// <param name="action">服务操作</param>
    /// <param name="args">参数</param>
    /// <returns></returns>
    public TResult Post<TResult>(String action, Object args = null) => Task.Run(() => PostAsync<TResult>(action, args)).Result;

    /// <summary>异步调用，等待返回结果</summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="method">请求方法</param>
    /// <param name="action">服务操作</param>
    /// <param name="args">参数</param>
    /// <param name="onRequest">请求头回调</param>
    /// <returns></returns>
    public virtual async Task<TResult> InvokeAsync<TResult>(HttpMethod method, String action, Object args = null, Action<HttpRequestMessage> onRequest = null)
    {
        var returnType = typeof(TResult);
        var svrs = Services;

        var i = 0;
        do
        {
            // 建立请求
            var request = BuildRequest(method, action, args, returnType);
            onRequest?.Invoke(request);

            var filter = Filter;
            try
            {
                var msg = await SendAsync(request);

                return await ApiHelper.ProcessResponse<TResult>(msg, CodeName, DataName);
            }
            catch (Exception ex)
            {
                while (ex is AggregateException age) ex = age.InnerException;

                if (ex is ApiException)
                {
                    if (filter != null) await filter.OnError(_currentService?.Client, ex, this);

                    ex.Source = _currentService?.Address + "/" + action;
                    throw;
                }
                else if (ex is HttpRequestException or TaskCanceledException)
                {
                    if (filter != null) await filter.OnError(_currentService?.Client, ex, this);
                    if (++i >= svrs.Count) throw;
                }
                else
                    throw;
            }
        } while (true);
    }

    /// <summary>异步调用，等待返回结果</summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="action">服务操作</param>
    /// <param name="args">参数</param>
    /// <returns></returns>
    async Task<TResult> IApiClient.InvokeAsync<TResult>(String action, Object args) => await InvokeAsync<TResult>(args == null ? HttpMethod.Get : HttpMethod.Post, action, args);

    /// <summary>同步调用，阻塞等待</summary>
    /// <param name="action">服务操作</param>
    /// <param name="args">参数</param>
    /// <returns></returns>
    TResult IApiClient.Invoke<TResult>(String action, Object args) => Task.Run(() => InvokeAsync<TResult>(args == null ? HttpMethod.Get : HttpMethod.Post, action, args)).Result;
    #endregion

    #region 构造请求
    /// <summary>建立请求</summary>
    /// <param name="method">请求方法</param>
    /// <param name="action"></param>
    /// <param name="args"></param>
    /// <param name="returnType"></param>
    /// <returns></returns>
    protected virtual HttpRequestMessage BuildRequest(HttpMethod method, String action, Object args, Type returnType)
    {
        var request = ApiHelper.BuildRequest(method, action, args);

        // 指定返回类型
        if (returnType == typeof(Byte[]) || returnType == typeof(Packet))
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
        else
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        //// 压缩
        //if (Compressed) request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

        // 加上令牌或其它身份验证
        var auth = Authentication;
        if (auth == null && !Token.IsNullOrEmpty()) auth = new AuthenticationHeaderValue("Bearer", Token);
        if (auth != null) request.Headers.Authorization = auth;

        return request;
    }
    #endregion

    #region 调度池
    /// <summary>调度索引，当前使用该索引处的服务</summary>
    private volatile Int32 _idxServer;

    /// <summary>异步发送</summary>
    /// <param name="request">请求</param>
    /// <returns></returns>
    protected virtual async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        if (Services.Count == 0) throw new InvalidOperationException("未添加服务地址！");

        // 获取一个处理当前请求的服务，此处实现负载均衡LoadBalance和故障转移Failover
        var service = GetService();
        Source = service.Name;
        _currentService = service;

        // 性能计数器，次数、TPS、平均耗时
        var st = StatInvoke;
        var sw = st.StartCount();
        Exception error = null;
        try
        {
            var client = service.Client;
            if (client == null)
            {
                if (service.CreateTime.Year < 2000) Log?.Debug("使用[{0}]：{1}", service.Name, service.Address);

                client = CreateClient();
                client.BaseAddress = service.Address;
                service.Client = client;
                service.CreateTime = DateTime.Now;
            }

            return await SendOnServiceAsync(request, service, client);
        }
        catch (Exception ex)
        {
            error = ex;

            throw;
        }
        finally
        {
            var msCost = st.StopCount(sw) / 1000;
            if (SlowTrace > 0 && msCost >= SlowTrace) WriteLog($"慢调用[{request.RequestUri.AbsoluteUri}]，耗时{msCost:n0}ms");

            // 归还服务
            PutService(service, error);
        }
    }

    /// <summary>获取一个服务用于处理请求，此处可实现负载均衡LoadBalance。默认取当前可用服务</summary>
    /// <remarks>
    /// 如需实现负载均衡，每次取值后都累加索引，让其下一次记获取时拿到下一个服务。
    /// </remarks>
    /// <returns></returns>
    protected virtual Service GetService()
    {
        // 在可用服务节点中选择，如果全部节点不可用，则启用全部节点，避免网络恢复后无法及时通信
        var svrs = Services;
        //if (!svrs.Any(e => e.NextTime < DateTime.Now)) throw new XException("没有可用服务节点！");
        if (!svrs.Any(e => e.NextTime < DateTime.Now))
        {
            foreach (var item in svrs)
            {
                item.NextTime = DateTime.MinValue;
            }
        }

        if (RoundRobin)
        {
            // 判断当前节点是否有效
            Service svc = null;
            for (var i = 0; i < svrs.Count; i++)
            {
                svc = svrs[_idxServer % svrs.Count];

                // 权重足够，又没有错误，就是它了
                if ((svc.Weight <= 0 || svc.Index < svc.Weight || svrs.Count == 1) && svc.NextTime < DateTime.Now) break;

                // 这个就算了，再下一个
                svc.Index = 0;
                svc = null;
                _idxServer++;
            }
            // 如果都没有可用节点，默认选第一个
            if (svc == null && svrs.Count > 0) svc = svrs[0];
            //if (svc == null) throw new XException("没有可用服务节点！");

            svc.Times++;

            // 计算下一次节点
            svc.Index++;
            if (svc.Index >= svc.Weight)
            {
                svc.Index = 0;
                _idxServer++;
            }
            if (_idxServer >= svrs.Count) _idxServer = 0;

            return svc;
        }
        else
        {
            // 一定时间后，切换回来主节点
            var idx = _idxServer;
            if (idx > 0 && svrs[0].NextTime < DateTime.Now) idx = _idxServer = 0;

            var svc = svrs[idx % svrs.Count];
            svc.Times++;

            return svc;
        }
    }

    /// <summary>归还服务，此处实现故障转移Failover，服务的客户端被清空，说明当前服务不可用</summary>
    /// <param name="service"></param>
    /// <param name="error"></param>
    protected virtual void PutService(Service service, Exception error)
    {
        if (service.CreateTime.AddMinutes(10) < DateTime.Now) service.Client = null;

        var ex = error;
        while (ex is AggregateException age) ex = age.InnerException;

        if (ex is HttpRequestException or TaskCanceledException)
        {
            // 网络异常时，自动切换到其它节点
            _idxServer++;
        }
        if (error != null)
        {
            service.Errors++;
            service.Client = null;
            service.NextTime = DateTime.Now.AddSeconds(ShieldingTime);
            service.CreateTime = DateTime.MinValue;
        }
    }

    /// <summary>在指定服务地址上发生请求</summary>
    /// <param name="request">请求消息</param>
    /// <param name="service">服务名</param>
    /// <param name="client">客户端</param>
    /// <returns></returns>
    protected virtual async Task<HttpResponseMessage> SendOnServiceAsync(HttpRequestMessage request, Service service, HttpClient client)
    {
        var filter = Filter;
        if (filter != null) await filter.OnRequest(client, request, this);

        var response = await client.SendAsync(request);

        if (filter != null) await filter.OnResponse(client, response, this);

        // 业务层只会返回200 OK
        response.EnsureSuccessStatusCode();

        return response;
    }

    /// <summary>创建客户端</summary>
    /// <returns></returns>
    protected virtual HttpClient CreateClient()
    {
        var handler = new HttpClientHandler { UseProxy = UseProxy };

#if NETCOREAPP3_0_OR_GREATER
        if (Compressed && handler.SupportsAutomaticDecompression) handler.AutomaticDecompression = DecompressionMethods.All;
#else
        if (Compressed && handler.SupportsAutomaticDecompression) handler.AutomaticDecompression = DecompressionMethods.GZip;
#endif

        HttpMessageHandler handler2 = handler;
        if (Tracer != null) handler2 = new HttpTraceHandler(handler2) { Tracer = Tracer };

        var client = new HttpClient(handler2)
        {
            Timeout = TimeSpan.FromMilliseconds(Timeout)
        };

        // 默认UserAgent
        client.SetUserAgent();

        return client;
    }
    #endregion

    #region 内嵌
    /// <summary>服务项</summary>
    public class Service
    {
        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>名称</summary>
        public Uri Address { get; set; }

        /// <summary>权重。用于负载均衡，默认1</summary>
        public Int32 Weight { get; set; } = 1;

        /// <summary>轮询均衡时，本项第几次使用</summary>
        internal Int32 Index;

        /// <summary>总次数</summary>
        public Int32 Times { get; set; }

        /// <summary>错误数</summary>
        public Int32 Errors { get; set; }

        /// <summary>创建时间。每过一段时间，就清空一次客户端，让它重建连接，更新域名缓存</summary>
        [XmlIgnore, IgnoreDataMember]
        public DateTime CreateTime { get; set; }

        /// <summary>下一次时间。服务项出错时，将禁用一段时间</summary>
        [XmlIgnore, IgnoreDataMember]
        public DateTime NextTime { get; set; }

        /// <summary>客户端</summary>
        [XmlIgnore, IgnoreDataMember]
        public HttpClient Client { get; set; }

        /// <summary>已重载。友好显示</summary>
        /// <returns></returns>
        public override String ToString() => $"{Name} {Address}";
    }
    #endregion

    #region 日志
    /// <summary>日志</summary>
    public ILog Log { get; set; } = Logger.Null;

    /// <summary>写日志</summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    public void WriteLog(String format, params Object[] args) => Log?.Info(format, args);
    #endregion
}