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
        public async Task<TResult> GetAsync<TResult>(String action, Object args = null)
        {
            return await InvokeAsync<TResult>(HttpMethod.Get, action, args);
        }

        /// <summary>异步提交，参数Json打包在Body</summary>
        /// <param name="action">服务操作</param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        public async Task<TResult> PostAsync<TResult>(String action, Object args = null)
        {
            return await InvokeAsync<TResult>(HttpMethod.Post, action, args);
        }

        /// <summary>异步调用，等待返回结果</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="method">请求方法</param>
        /// <param name="action">服务操作</param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        public virtual async Task<TResult> InvokeAsync<TResult>(HttpMethod method, String action, Object args = null)
        {
            var rtype = typeof(TResult);

            // 序列化参数，决定GET/POST
            var request = BuildRequest(method, action, args, rtype);

            // 发起请求
            var msg = await SendAsync(request);
            if (rtype == typeof(HttpResponseMessage)) return (TResult)(Object)msg;

            var code = msg.StatusCode;
            var buf = await msg.Content.ReadAsByteArrayAsync();
            if (buf == null || buf.Length == 0) return default;

            // 异常处理
            if (code != HttpStatusCode.OK)
            {
                var invoker = _Items[_Index]?.Address + "";
                var err = buf.ToStr()?.Trim('\"');
                if (err.IsNullOrEmpty()) err = msg.ReasonPhrase;
                throw new ApiException((Int32)code, $"远程[{invoker}]错误！ {err}");
            }

            // 原始数据
            if (rtype == typeof(Byte[])) return (TResult)(Object)buf;
            if (rtype == typeof(Packet)) return (TResult)(Object)new Packet(buf);

            var str = buf.ToStr();
            var js = new JsonParser(str).Decode() as IDictionary<String, Object>;
            var data = js["data"];
            var code2 = js["code"].ToInt();
            if (code2 != 0 && code2 != 200)
            {
                var invoker = _Items[_Index]?.Address + "";
                if (data == null) data = js["msg"];
                throw new ApiException(code2, $"远程[{invoker}]错误！ {data}");
            }

            // 简单类型
            if (rtype.GetTypeCode() != TypeCode.Object) return data.ChangeType<TResult>();

            // 反序列化
            if (data == null) return default;

            if (!(data is IDictionary<String, Object>) && !(data is IList<Object>)) throw new InvalidDataException("未识别响应数据");

            return JsonHelper.Convert<TResult>(data);
        }

        /// <summary>异步调用，等待返回结果</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="action">服务操作</param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        async Task<TResult> IApiClient.InvokeAsync<TResult>(String action, Object args = null) => await InvokeAsync<TResult>(HttpMethod.Post, action, args);

        /// <summary>同步调用，阻塞等待</summary>
        /// <param name="action">服务操作</param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        TResult IApiClient.Invoke<TResult>(String action, Object args = null) => Task.Run(() => InvokeAsync<TResult>(HttpMethod.Post, action, args)).Result;

        /// <summary>建立请求</summary>
        /// <param name="method">请求方法</param>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <param name="returnType"></param>
        /// <returns></returns>
        protected virtual HttpRequestMessage BuildRequest(HttpMethod method, String action, Object args, Type returnType)
        {
            // 序列化参数，决定GET/POST
            var request = new HttpRequestMessage(HttpMethod.Get, action);
            if (returnType != typeof(Byte[]) && returnType != typeof(Packet))
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var ps = args?.ToDictionary();
            if (method == HttpMethod.Get)
            {
                var url = GetUrl(action, ps);
                if (!Token.IsNullOrEmpty())
                {
                    url += url.Contains("?") ? "&" : "?";
                    url += $"token={Token}";
                }
                request.RequestUri = new Uri(url, UriKind.RelativeOrAbsolute);
            }
            else
            {
                FillContent(request, args);
                if (!Token.IsNullOrEmpty()) request.Headers.Add("X-Token", Token);
            }

            return request;
        }

        private void FillContent(HttpRequestMessage request, Object args)
        {
            if (args is Packet pk)
            {
                var content =
                    pk.Next == null ?
                    new ByteArrayContent(pk.Data, pk.Offset, pk.Count) :
                    new ByteArrayContent(pk.ToArray());
                content.Headers.ContentType = new MediaTypeHeaderValue("application/stream");
                request.Content = content;
            }
            else if (args is Byte[] buf)
            {
                var content = new ByteArrayContent(buf);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/stream");
                request.Content = content;
            }
            else if (args != null)
            {
                var ps = args?.ToDictionary();
                var content = new ByteArrayContent(ps.ToJson().GetBytes());
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                request.Content = content;
            }
            request.Method = HttpMethod.Post;
        }

        private static String Encode(String data)
        {
            if (String.IsNullOrEmpty(data)) return String.Empty;

            return Uri.EscapeDataString(data).Replace("%20", "+");
        }

        private static String GetUrl(String action, IDictionary<String, Object> ps)
        {
            var url = action;
            if (ps != null && ps.Count > 0)
            {
                var sb = Pool.StringBuilder.Get();
                sb.Append(action);
                if (action.Contains("?"))
                    sb.Append("&");
                else
                    sb.Append("?");

                var first = true;
                foreach (var item in ps)
                {
                    if (!first) sb.Append("&");
                    first = false;

                    sb.AppendFormat("{0}={1}", Encode(item.Key), Encode("{0}".F(item.Value)));
                }

                url = sb.Put(true);
            }

            return url;
        }
        #endregion

        #region 调度池
        private Int32 _Index;
        /// <summary>异步请求，等待响应</summary>
        /// <param name="request">Http请求</param>
        /// <returns></returns>
        protected virtual async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            var ms = _Items;
            if (ms.Count == 0) throw new InvalidOperationException("未添加服务地址！");

            Exception error = null;
            for (var i = 0; i < ms.Count; i++)
            {
                var mi = ms[_Index];
                HttpResponseMessage rs = null;

                // 性能计数器，次数、TPS、平均耗时
                var st = StatInvoke;
                var sw = st.StartCount();
                try
                {
                    if (mi.Client == null) mi.Client = new HttpClient { BaseAddress = mi.Address };

                    rs = await mi.Client.SendAsync(request);
                    // 业务层只会返回200 OK
                    rs.EnsureSuccessStatusCode();

                    return rs;
                }
                catch (Exception ex)
                {
                    if (error == null)
                    {
                        error = ex;

                        ex.Data.Add("Response", rs);
                        ex.Data.Add(nameof(mi.Name), mi.Name);
                        ex.Data.Add(nameof(mi.Address), mi.Address);
                        ex.Data.Add(nameof(mi.Client), mi.Client);
                    }
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
        #endregion

        #region 内嵌
        class ServiceItem
        {
            public String Name { get; set; }

            public Uri Address { get; set; }

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