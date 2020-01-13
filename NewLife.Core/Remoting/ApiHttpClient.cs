using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Serialization;
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

        /// <summary>出错重试次数。-1不重试，0表示每个服务都试一次，默认-1</summary>
        public Int32 Retry { get; set; } = -1;

        /// <summary>重置周期。该时间后，在多地址中切换回第一个，默认600_000ms</summary>
        public Int32 ResetPeriod { get; set; } = 600_000;

        /// <summary>身份验证</summary>
        public AuthenticationHeaderValue Authentication { get; set; }

        /// <summary>服务器源。正在使用的服务器</summary>
        public String Source { get; private set; }

        /// <summary>调用统计</summary>
        public ICounter StatInvoke { get; set; }

        /// <summary>慢追踪。远程调用或处理时间超过该值时，输出慢调用日志，默认5000ms</summary>
        public Int32 SlowTrace { get; set; } = 5_000;

        /// <summary>服务列表。用于负载均衡和故障转移</summary>
        public IList<Service> Services { get; } = new List<Service>();
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public ApiHttpClient() { }

        /// <summary>实例化</summary>
        /// <param name="url"></param>
        public ApiHttpClient(String url) => Add("Default", new Uri(url));

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
            var rtype = typeof(TResult);

            // 发起请求
            var msg = await SendAsync(method, action, args, rtype, onRequest);

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
        /// <param name="method">请求方法</param>
        /// <param name="action">服务操作</param>
        /// <param name="args">参数</param>
        /// <param name="returnType">返回类型</param>
        /// <param name="onRequest">请求头回调</param>
        /// <returns></returns>
        protected virtual async Task<HttpResponseMessage> SendAsync(HttpMethod method, String action, Object args, Type returnType, Action<HttpRequestMessage> onRequest)
        {
            var ms = Services;
            if (ms.Count == 0) throw new InvalidOperationException("未添加服务地址！");

            // 设置了重置周期，且地址池有两个或以上时，才启用定时重置
            if (ResetPeriod > 0 && ms.Count > 2)
            {
                if (_timer == null)
                {
                    lock (this)
                    {
                        if (_timer == null) _timer = new TimerX(ResetIndex, null, ResetPeriod, ResetPeriod);
                    }
                }
            }

            // 重试次数
            var retry = Retry;
            if (retry == 0) retry = ms.Count;
            if (retry <= 0) retry = 1;

            Exception error = null;
            for (var i = 0; i < retry; i++)
            {
                // 建立请求
                var request = BuildRequest(method, action, args, returnType);
                onRequest?.Invoke(request);

                // 获取一个处理当前请求的服务，此处实现负载均衡LoadBalance和故障转移Failover
                var service = GetService();
                service.Total++;
                Source = service.Name;

                // 性能计数器，次数、TPS、平均耗时
                var st = StatInvoke;
                var sw = st.StartCount();
                try
                {
                    if (service.Client == null)
                    {
                        WriteLog("使用[{0}]：{1}", service.Name, service.Address);

                        service.Client = new HttpClient
                        {
                            BaseAddress = service.Address,
                            Timeout = TimeSpan.FromMilliseconds(Timeout)
                        };
                    }

                    return await SendOnServiceAsync(request, service, service.Client);
                }
                catch (Exception ex)
                {
                    if (error == null) error = ex;

                    service.Client = null;
                    service.Error++;
                    service.LastError = DateTime.Now;
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

            throw error;
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

        private TimerX _timer;
        /// <summary>定时重置索引。让其从第一个地址开始重试</summary>
        /// <param name="state"></param>
        private void ResetIndex(Object state) => _Index = 0;
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