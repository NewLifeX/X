using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using NewLife.Data;
using NewLife.Http;
using NewLife.Log;
using NewLife.Threading;
#if !NET4
using TaskEx = System.Threading.Tasks.Task;
#endif

namespace NewLife.Remoting
{
    /// <summary>Http应用接口客户端</summary>
    public class ApiHttpClient : DisposeBase, IApiClient
    {
        #region 属性
        /// <summary>令牌。每次请求携带</summary>
        public String Token { get; set; }

        /// <summary>超时时间。默认15000ms</summary>
        public Int32 Timeout { get; set; } = 15_000;

        /// <summary>探测节点可用性的周期。默认600_000ms</summary>
        public Int32 TracePeriod { get; set; } = 600_000;

        /// <summary>探测次数。每个节点连续探测多次，任意一次成功即认为该节点可用，默认3</summary>
        public Int32 TraceTimes { get; set; } = 3;

        /// <summary>是否使用系统代理设置。默认false不检查系统代理设置，在某些系统上可以大大改善初始化速度</summary>
        public Boolean UseProxy { get; set; }

        /// <summary>身份验证</summary>
        public AuthenticationHeaderValue Authentication { get; set; }

        /// <summary>服务器源。正在使用的服务器</summary>
        public String Source { get; private set; }

        /// <summary>调用统计</summary>
        public ICounter StatInvoke { get; set; }

        /// <summary>慢追踪。远程调用或处理时间超过该值时，输出慢调用日志，默认5000ms</summary>
        public Int32 SlowTrace { get; set; } = 5_000;

        /// <summary>跟踪器</summary>
        public ITracer Tracer { get; set; }

        /// <summary>服务列表。用于负载均衡和故障转移</summary>
        public IList<Service> Services { get; } = new List<Service>();
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public ApiHttpClient() { }

        /// <summary>实例化</summary>
        /// <param name="urls"></param>
        public ApiHttpClient(String urls)
        {
            //Add("Default", new Uri(urls));
            if (!urls.IsNullOrEmpty())
            {
                var ss = urls.Split(",");
                for (var i = 0; i < ss.Length; i++)
                {
                    Add("service" + (i + 1), new Uri(ss[i]));
                }
            }
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void Dispose(Boolean disposing)
        {
            base.Dispose(disposing);

            _timer.TryDispose();
        }
        #endregion

        #region 方法
        /// <summary>添加服务地址</summary>
        /// <param name="name"></param>
        /// <param name="address"></param>
        public void Add(String name, Uri address) => Services.Add(new Service { Name = name, Address = address });
        #endregion

        #region 核心方法
        /// <summary>异步获取，参数构造在Url</summary>
        /// <param name="action">服务操作</param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        public async Task<TResult> GetAsync<TResult>(String action, Object args = null) => await InvokeAsync<TResult>(HttpMethod.Get, action, args);

        /// <summary>异步提交，参数Json打包在Body</summary>
        /// <param name="action">服务操作</param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        public async Task<TResult> PostAsync<TResult>(String action, Object args = null) => await InvokeAsync<TResult>(HttpMethod.Post, action, args);

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

            // 发起请求
            //var msg = await SendAsync(method, action, args, rtype, onRequest);

            // 建立请求
            var request = BuildRequest(method, action, args, returnType);
            onRequest?.Invoke(request);
            var msg = await SendAsync(request);

            try
            {
                return await ApiHelper.ProcessResponse<TResult>(msg);
            }
            catch (ApiException ex)
            {
                ex.Source = Services[_Index]?.Address + "/" + action;
                throw;
            }
        }

        /// <summary>异步调用，等待返回结果</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="action">服务操作</param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        async Task<TResult> IApiClient.InvokeAsync<TResult>(String action, Object args) => await InvokeAsync<TResult>(HttpMethod.Post, action, args);

        /// <summary>同步调用，阻塞等待</summary>
        /// <param name="action">服务操作</param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        TResult IApiClient.Invoke<TResult>(String action, Object args) => TaskEx.Run(() => InvokeAsync<TResult>(HttpMethod.Post, action, args)).Result;
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

            // 加上令牌或其它身份验证
            var auth = Authentication;
            if (auth == null && !Token.IsNullOrEmpty()) auth = new AuthenticationHeaderValue("Bearer", Token);
            if (auth != null) request.Headers.Authorization = auth;

            return request;
        }
        #endregion

        #region 调度池
        /// <summary>调度索引，当前使用该索引处的服务</summary>
        protected volatile Int32 _Index;

        /// <summary>异步发送</summary>
        /// <param name="request">请求</param>
        /// <returns></returns>
        protected virtual async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            var ms = Services;
            if (ms.Count == 0) throw new InvalidOperationException("未添加服务地址！");

            // 设置了重置周期，且地址池有两个或以上时，才启用定时重置
            if (TracePeriod > 0 && ms.Count > 1 && _timer == null)
            {
                lock (this)
                {
                    if (_timer == null) _timer = new TimerX(DoTrace, null, TracePeriod, TracePeriod) { Async = true };
                }
            }

            // 获取一个处理当前请求的服务，此处实现负载均衡LoadBalance和故障转移Failover
            var service = GetService();
            service.Total++;
            Source = service.Name;

            // 性能计数器，次数、TPS、平均耗时
            var st = StatInvoke;
            var sw = st.StartCount();
            try
            {
                var client = service.Client;
                if (client == null)
                {
                    WriteLog("使用[{0}]：{1}", service.Name, service.Address);

                    client = CreateClient();
                    client.BaseAddress = service.Address;
                    service.Client = client;
                }

                return await SendOnServiceAsync(request, service, client);
            }
            catch (Exception)
            {
                service.Client = null;
                service.Error++;
                service.LastError = DateTime.Now;

                // 异常发生，马上安排检查网络
                if (_timer != null)
                {
                    _timer.Period = 5_000;
                    _timer.SetNext(-1);
                }

                throw;
            }
            finally
            {
                var msCost = st.StopCount(sw) / 1000;
                service.TotalCost += msCost;
                if (SlowTrace > 0 && msCost >= SlowTrace) WriteLog($"慢调用[{request.RequestUri.AbsoluteUri}]，耗时{msCost:n0}ms");

                // 归还服务
                PutService(service);
            }
        }

        /// <summary>获取一个服务用于处理请求，此处可实现负载均衡LoadBalance。默认取当前可用服务</summary>
        /// <remarks>
        /// 如需实现负载均衡，每次取值后都累加索引，让其下一次记获取时拿到下一个服务。
        /// </remarks>
        /// <returns></returns>
        protected virtual Service GetService()
        {
            var ms = Services;
            if (_Index >= ms.Count) _Index = 0;

            return ms[_Index];
        }

        /// <summary>归还服务，此处实现故障转移Failover，服务的客户端被清空，说明当前服务不可用</summary>
        /// <param name="service"></param>
        protected virtual void PutService(Service service)
        {
            if (service.Client == null)
            {
                var idx = _Index + 1;
                if (idx >= Services.Count) idx = 0;
                _Index = idx;
            }
        }

        /// <summary>在指定服务地址上发生请求</summary>
        /// <param name="request">请求消息</param>
        /// <param name="service">服务名</param>
        /// <param name="client">客户端</param>
        /// <returns></returns>
        protected virtual async Task<HttpResponseMessage> SendOnServiceAsync(HttpRequestMessage request, Service service, HttpClient client)
        {
            var rs = await client.SendAsync(request);
            // 业务层只会返回200 OK
            rs.EnsureSuccessStatusCode();

            return rs;
        }

        /// <summary>创建客户端</summary>
        /// <returns></returns>
        protected virtual HttpClient CreateClient()
        {
            HttpMessageHandler handler = new HttpClientHandler { UseProxy = UseProxy };
            if (Tracer != null) handler = new HttpTraceHandler(handler) { Tracer = Tracer };

            var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMilliseconds(Timeout)
            };

            return client;
        }

        private TimerX _timer;
        /// <summary>定时检测网络。优先选择第一个</summary>
        /// <param name="state"></param>
        protected virtual void DoTrace(Object state)
        {
            var ms = Services;
            if (ms.Count < 1) return;

            var times = TraceTimes;
            if (times <= 0) times = 1;

            // 打开多个任务，同时检测节点
            var source = new CancellationTokenSource();
            var ts = new List<Task<Int32>>();
            foreach (var service in ms)
            {
                ts.Add(TaskEx.Run(() => TraceService(service, times, source.Token)));
            }

            // 依次等待完成
            for (var i = 0; i < ts.Count; i++)
            {
                ts[i].Wait();

                // 如果成功，则直接使用
                if (ts[i].Result >= 0)
                {
                    if (i != _Index) WriteLog("ApiHttp.DoTrace 地址切换 {0} => {1}", ms[_Index]?.Address, ms[i]?.Address);

                    _Index = i;
                    source.Cancel();

                    // 调整定时器周期。第一节点时，恢复默认定时，非第一节点时，每分钟探测一次，希望能够尽快回到第一节点
                    _timer.Period = i == 0 ? TracePeriod : 60_000;

                    return;
                }
            }
        }

        /// <summary>探测服务节点</summary>
        /// <param name="service"></param>
        /// <param name="times"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        protected virtual async Task<Int32> TraceService(Service service, Int32 times, CancellationToken cancellation)
        {
            // 每个任务若干次，任意一次成功
            for (var i = 0; i < times && !cancellation.IsCancellationRequested; i++)
            {
                try
                {
                    var request = BuildRequest(HttpMethod.Get, "api", null, null);
                    var client = CreateClient();
                    client.BaseAddress = service.Address;

                    var rs = await client.SendAsync(request);
                    if (rs != null)
                    {
                        // 该地址可用
                        service.Client = client;
                        return i;
                    }
                }
                catch { }
            }

            // 当前地址不可用
            WriteLog("ApiHttp.TraceService 地址不可用 :{0}", service.Address);

            return -1;
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

            /// <summary>客户端</summary>
            [XmlIgnore]
            public HttpClient Client { get; set; }

            /// <summary>总次数。可用于负载均衡</summary>
            public Int32 Total { get; set; }

            /// <summary>错误次数。可用于故障转移</summary>
            public Int32 Error { get; set; }

            /// <summary>最后出错时间</summary>
            public DateTime LastError { get; set; }

            /// <summary>总耗时</summary>
            public Int64 TotalCost { get; set; }

            /// <summary>平均耗时。可用于负载均衡</summary>
            public Int32 AverageCost => Total == 0 ? 0 : (Int32)(TotalCost / Total);
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
}