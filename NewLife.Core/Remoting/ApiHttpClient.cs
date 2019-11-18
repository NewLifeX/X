#if !NET4
using System;
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
        /// <summary>请求方法。默认Auto自动选择GET，复杂对象和二进制选POST</summary>
        public HttpMethod Method { get; set; } = new HttpMethod("Auto");

        /// <summary>是否使用Http状态抛出异常。默认false，使用ApiException抛出异常</summary>
        public Boolean UseHttpStatus { get; set; }

        /// <summary>令牌。每次请求携带</summary>
        public String Token { get; set; }

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
        /// <summary>异步调用，等待返回结果</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="action">服务操作</param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        public virtual async Task<TResult> InvokeAsync<TResult>(String action, Object args = null)
        {
            var rtype = typeof(TResult);

            // 序列化参数，决定GET/POST
            var request = BuildRequest(action, args, rtype);

            // 发起请求
            var msg = await SendAsync(request);
            if (rtype == typeof(HttpResponseMessage)) return (TResult)(Object)msg;

            // 使用Http状态抛出异常
            if (UseHttpStatus) msg.EnsureSuccessStatusCode();

            var code = msg.StatusCode;
            var buf = await msg.Content.ReadAsByteArrayAsync();
            if (buf == null || buf.Length == 0) return default;

            // 异常处理
            //if (code != HttpStatusCode.OK) throw new ApiException((Int32)code, buf.ToStr()?.Trim('\"'));
            if (code != HttpStatusCode.OK)
            {
                var invoker = _Items[_Index]?.Address + "";
                throw new ApiException((Int32)code, $"远程[{invoker}]错误！ {buf.ToStr()?.Trim('\"')}");
            }

            // 原始数据
            if (rtype == typeof(Byte[])) return (TResult)(Object)buf;
            if (rtype == typeof(Packet)) return (TResult)(Object)new Packet(buf);

            var str = buf.ToStr();
            Object data = str;
            if (!UseHttpStatus)
            {
                var js2 = new JsonParser(str).Decode() as IDictionary<String, Object>;
                data = js2["data"];
                var code2 = js2["code"].ToInt();
                if (code2 != 0)
                {
                    var invoker = _Items[_Index]?.Address + "";
                    throw new ApiException(code2, $"远程[{invoker}]错误！ {data}");
                }
            }

            // 简单类型
            if (rtype.GetTypeCode() != TypeCode.Object) return data.ChangeType<TResult>();

            // 反序列化
            if (UseHttpStatus) data = new JsonParser(str).Decode();
            if (!(data is IDictionary<String, Object>) && !(data is IList<Object>)) throw new InvalidDataException("未识别响应数据");

            return JsonHelper.Convert<TResult>(data);
        }

        /// <summary>同步调用，阻塞等待</summary>
        /// <param name="action">服务操作</param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        public virtual TResult Invoke<TResult>(String action, Object args = null) => Task.Run(() => InvokeAsync<TResult>(action, args)).Result;

        /// <summary>建立请求</summary>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <param name="returnType"></param>
        /// <returns></returns>
        protected virtual HttpRequestMessage BuildRequest(String action, Object args, Type returnType)
        {
            // 序列化参数，决定GET/POST
            var request = new HttpRequestMessage(HttpMethod.Get, action);
            if (returnType != typeof(Byte[]) && returnType != typeof(Packet))
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // 令牌
            //if (!Token.IsNullOrEmpty()) request.Headers.Add(nameof(Token), Token);
            if (!Token.IsNullOrEmpty())
            {
                action += action.Contains("?") ? "&" : "?";
                action += $"token={Token}";
            }

            if (Method == HttpMethod.Post || args is Packet || args is Byte[])
            {
                FillContent(request, args);
                // 避免token 保证token可以正常传输到服务端
                request.Headers.Add("x-token", Token);
            }
            else if (Method == HttpMethod.Get)
            {
                var ps = args?.ToDictionary();
                var url = GetUrl(action, ps);
                request.RequestUri = new Uri(url, UriKind.RelativeOrAbsolute);
            }
            else
            {
                var ps = args?.ToDictionary();
                if (ps != null && ps.Any(e => e.Value != null && e.Value.GetType().GetTypeCode() == TypeCode.Object))
                {
                    FillContent(request, args);
                }
                else
                {
                    var url = GetUrl(action, ps);
                    // 如果长度过大，还是走POST
                    if (url.Length < 1000)
                        request.RequestUri = new Uri(url, UriKind.RelativeOrAbsolute);
                    else
                        FillContent(request, args);
                }
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
                try
                {
                    if (mi.Client == null) mi.Client = new HttpClient { BaseAddress = mi.Address };

                    rs = await mi.Client.SendAsync(request);
                    //if (!UseHttpStatus || rs.StatusCode == HttpStatusCode.OK) return rs;
                    if (UseHttpStatus) rs.EnsureSuccessStatusCode();
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