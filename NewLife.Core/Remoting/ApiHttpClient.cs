using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;
using NewLife.Data;
using NewLife.Http;
using NewLife.Log;
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

            foreach (var item in Services)
            {
                item.Client?.TryDispose();
            }
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

        /// <summary>同步获取，参数构造在Url</summary>
        /// <param name="action">服务操作</param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        public TResult Get<TResult>(String action, Object args = null) => TaskEx.Run(() => GetAsync<TResult>(action, args)).Result;

        /// <summary>异步提交，参数Json打包在Body</summary>
        /// <param name="action">服务操作</param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        public async Task<TResult> PostAsync<TResult>(String action, Object args = null) => await InvokeAsync<TResult>(HttpMethod.Post, action, args);

        /// <summary>同步提交，参数Json打包在Body</summary>
        /// <param name="action">服务操作</param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        public TResult Post<TResult>(String action, Object args = null) => TaskEx.Run(() => PostAsync<TResult>(action, args)).Result;

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

                try
                {
                    var msg = await SendAsync(request);

                    return await ApiHelper.ProcessResponse<TResult>(msg);
                }
                catch (ApiException ex)
                {
                    ex.Source = svrs[_idxServer % svrs.Count]?.Address + "/" + action;
                    throw;
                }
                catch (HttpRequestException)
                {
                    // 网络异常时，自动切换到其它节点
                    _idxServer++;
                    if (++i >= svrs.Count) throw;
                }
                catch (TaskCanceledException)
                {
                    // 网络异常时，自动切换到其它节点
                    _idxServer++;
                    if (++i >= svrs.Count) throw;
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
        TResult IApiClient.Invoke<TResult>(String action, Object args) => TaskEx.Run(() => InvokeAsync<TResult>(args == null ? HttpMethod.Get : HttpMethod.Post, action, args)).Result;
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
        protected volatile Int32 _idxServer;
        private Int32 _idxLast = -1;
        private DateTime _nextTrace;

        /// <summary>异步发送</summary>
        /// <param name="request">请求</param>
        /// <returns></returns>
        protected virtual async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            if (Services.Count == 0) throw new InvalidOperationException("未添加服务地址！");

            // 获取一个处理当前请求的服务，此处实现负载均衡LoadBalance和故障转移Failover
            var service = GetService();
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
            catch
            {
                service.Client = null;

                throw;
            }
            finally
            {
                var msCost = st.StopCount(sw) / 1000;
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
            var svrs = Services;

            // 一定时间后，切换回来主节点
            var idx = _idxServer;
            if (idx > 0)
            {
                var now = DateTime.Now;
                if (_nextTrace.Year < 2000) _nextTrace = now.AddSeconds(300);
                if (now > _nextTrace)
                {
                    _nextTrace = DateTime.MinValue;

                    idx = _idxServer = 0;
                }
            }

            if (idx != _idxLast)
            {
                XTrace.WriteLine("Http使用 {0}", svrs[idx % svrs.Count].Address);

                _idxLast = idx;
            }

            return svrs[_idxServer % svrs.Count];
        }

        /// <summary>归还服务，此处实现故障转移Failover，服务的客户端被清空，说明当前服务不可用</summary>
        /// <param name="service"></param>
        protected virtual void PutService(Service service) { }

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
            [XmlIgnore, IgnoreDataMember]
            public HttpClient Client { get; set; }
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