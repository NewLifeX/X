#if !NET4
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
using NewLife.Collections;
using NewLife.Data;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Remoting
{
    /// <summary>Http应用接口客户端</summary>
    public class ApiHttpClient : IApiClient
    {
        #region 属性
        /// <summary>令牌。每次请求携带</summary>
        public String Token { get; set; }

        /// <summary>超时时间。默认15000ms</summary>
        public Int32 Timeout { get; set; } = 15_000;

        /// <summary>服务器源。正在使用的服务器</summary>
        public String Source { get; private set; }

        /// <summary>调用统计</summary>
        public ICounter StatInvoke { get; set; }

        /// <summary>慢追踪。远程调用或处理时间超过该值时，输出慢调用日志，默认5000ms</summary>
        public Int32 SlowTrace { get; set; } = 5_000;

        private readonly IList<ServiceItem> _Items = new List<ServiceItem>();
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public ApiHttpClient() { }

        /// <summary>实例化</summary>
        /// <param name="url"></param>
        public ApiHttpClient(String url) => Add("Default", new Uri(url));
        #endregion

        #region 方法
        /// <summary>添加服务地址</summary>
        /// <param name="name"></param>
        /// <param name="address"></param>
        public void Add(String name, Uri address) => _Items.Add(new ServiceItem { Name = name, Address = address });
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
        /// <returns></returns>
        public virtual async Task<TResult> InvokeAsync<TResult>(HttpMethod method, String action, Object args = null)
        {
            var rtype = typeof(TResult);

            // 发起请求
            var msg = await SendAsync(method, action, args, rtype);

            try
            {
                return await ApiHelper.ProcessResponse<TResult>(msg);
            }
            catch (ApiException ex)
            {
                ex.Source = _Items[_Index]?.Address + "/" + action;
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
        TResult IApiClient.Invoke<TResult>(String action, Object args) => Task.Run(() => InvokeAsync<TResult>(HttpMethod.Post, action, args)).Result;
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
            var request = ApiHelper.BuildRequest(method, action, args, returnType);

            // 加上令牌
            if (!Token.IsNullOrEmpty()) request.Headers.Add("Authorization", "Bearer " + Token);

            return request;
        }
        #endregion

        #region 调度池
        private Int32 _Index;
        /// <summary>异步发送</summary>
        /// <param name="method"></param>
        /// <param name="action"></param>
        /// <param name="returnType"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual async Task<HttpResponseMessage> SendAsync(HttpMethod method, String action, Object args, Type returnType)
        {
            var ms = _Items;
            if (ms.Count == 0) throw new InvalidOperationException("未添加服务地址！");

            Exception error = null;
            for (var i = 0; i < ms.Count; i++)
            {
                // 序列化参数，决定GET/POST
                var request = BuildRequest(method, action, args, returnType);

                var service = ms[_Index];
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

                    return await SendOnServiceAsync(request, service.Name, service.Client);
                }
                catch (Exception ex)
                {
                    if (error == null) error = ex;

                    service.Client = null;
                }
                finally
                {
                    var msCost = st.StopCount(sw) / 1000;
                    if (SlowTrace > 0 && msCost >= SlowTrace) WriteLog($"慢调用[{request.RequestUri.AbsoluteUri}]，耗时{msCost:n0}ms");
                }

                _Index++;
                if (_Index >= ms.Count) _Index = 0;
            }

            throw error;
        }

        /// <summary>在指定服务地址上发生请求</summary>
        /// <param name="request"></param>
        /// <param name="serviceName"></param>
        /// <param name="client"></param>
        /// <returns></returns>
        protected virtual async Task<HttpResponseMessage> SendOnServiceAsync(HttpRequestMessage request, String serviceName, HttpClient client)
        {
            var rs = await client.SendAsync(request);
            // 业务层只会返回200 OK
            rs.EnsureSuccessStatusCode();

            return rs;
        }
        #endregion

        #region 内嵌
        /// <summary>服务项</summary>
        class ServiceItem
        {
            /// <summary>名称</summary>
            public String Name { get; set; }

            /// <summary>名称</summary>
            public Uri Address { get; set; }

            /// <summary>名称</summary>
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
#endif