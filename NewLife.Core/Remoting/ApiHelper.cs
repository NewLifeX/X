using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using NewLife.Serialization;
using NewLife.Reflection;
using NewLife.Data;
using System.IO;
using NewLife.Collections;
using System.Net.Http;
using System.Net.Http.Headers;

namespace NewLife.Remoting
{
    /// <summary>Api助手</summary>
    public static class ApiHelper
    {
        #region 远程调用
        /// <summary>异步调用，等待返回结果</summary>
        /// <param name="client">Http客户端</param>
        /// <param name="action">服务操作</param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        public static async Task<TResult> GetAsync<TResult>(this HttpClient client, String action, Object args = null) => await client.InvokeAsync<TResult>(HttpMethod.Get, action, args);

        /// <summary>异步调用，等待返回结果</summary>
        /// <param name="client">Http客户端</param>
        /// <param name="action">服务操作</param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        public static async Task<TResult> PostAsync<TResult>(this HttpClient client, String action, Object args = null) => await client.InvokeAsync<TResult>(HttpMethod.Post, action, args);

        /// <summary>异步调用，等待返回结果</summary>
        /// <param name="client">Http客户端</param>
        /// <param name="method">请求方法</param>
        /// <param name="action">服务操作</param>
        /// <param name="args">参数</param>
        /// <param name="onRequest">请求头回调</param>
        /// <param name="dataName">数据字段名称，默认data。同一套rpc体系不同接口的code/message一致，但data可能不同</param>
        /// <returns></returns>
        public static async Task<TResult> InvokeAsync<TResult>(this HttpClient client, HttpMethod method, String action, Object args = null, Action<HttpRequestMessage> onRequest = null, String dataName = "data")
        {
            //if (client?.BaseAddress == null) throw new ArgumentNullException(nameof(client.BaseAddress));

            var returnType = typeof(TResult);

            // 构建请求
            var request = BuildRequest(method, action, args);

            // 指定返回类型
            if (returnType == typeof(Byte[]) || returnType == typeof(Packet))
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
            else
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // 可能附加头部
            onRequest?.Invoke(request);

            // 发起请求
            var msg = await client.SendAsync(request);
            return await ProcessResponse<TResult>(msg, dataName);
        }
        #endregion

        #region 远程辅助
        /// <summary>建立请求，action写到url里面</summary>
        /// <param name="method">请求方法</param>
        /// <param name="action">动作</param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        public static HttpRequestMessage BuildRequest(HttpMethod method, String action, Object args)
        {
            // 序列化参数，决定GET/POST
            var request = new HttpRequestMessage(method, action);

            if (method == HttpMethod.Get)
            {
                if (args is Packet pk)
                {
                    var url = action;
                    url += url.Contains("?") ? "&" : "?";
                    url += pk.ToArray().ToUrlBase64();
                    request.RequestUri = new Uri(url, UriKind.RelativeOrAbsolute);
                }
                else if (args is Byte[] buf)
                {
                    var url = action;
                    url += url.Contains("?") ? "&" : "?";
                    url += buf.ToUrlBase64();
                    request.RequestUri = new Uri(url, UriKind.RelativeOrAbsolute);
                }
                else if (args != null)
                {
                    var ps = args?.ToDictionary();
                    var url = GetUrl(action, ps);
                    request.RequestUri = new Uri(url, UriKind.RelativeOrAbsolute);
                }
            }
            else if (method == HttpMethod.Post)
            {
                if (args is Packet pk)
                {
                    var content =
                        pk.Next == null ?
                        new ByteArrayContent(pk.Data, pk.Offset, pk.Count) :
                        new ByteArrayContent(pk.ToArray());
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    request.Content = content;
                }
                else if (args is Byte[] buf)
                {
                    var content = new ByteArrayContent(buf);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    request.Content = content;
                }
                else if (args != null)
                {
                    var content = new ByteArrayContent(args.ToJson().GetBytes());
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    request.Content = content;
                }
            }

            return request;
        }

        /// <summary>结果代码名称</summary>
        public static IList<String> CodeNames { get; } = new List<String> { "code", "errcode" };

        /// <summary>结果消息名称</summary>
        public static IList<String> MessageNames { get; } = new List<String> { "message", "msg", "errmsg" };

        /// <summary>处理响应。统一识别code/message</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="response">Http响应消息</param>
        /// <param name="dataName">数据字段名称，默认data。同一套rpc体系不同接口的code/message一致，但data可能不同</param>
        /// <returns></returns>
        public static async Task<TResult> ProcessResponse<TResult>(HttpResponseMessage response, String dataName = "data")
        {
            var rtype = typeof(TResult);
            if (rtype == typeof(HttpResponseMessage)) return (TResult)(Object)response;

            var buf = response.Content == null ? null : (await response.Content.ReadAsByteArrayAsync());

            // 异常处理
            if (response.StatusCode != HttpStatusCode.OK) throw new ApiException((Int32)response.StatusCode, buf.ToStr()?.Trim('\"') ?? response.ReasonPhrase);
            if (buf == null || buf.Length == 0) return default;

            // 原始数据
            if (rtype == typeof(Byte[])) return (TResult)(Object)buf;
            if (rtype == typeof(Packet)) return (TResult)(Object)new Packet(buf);

            var str = buf.ToStr();
            var js = new JsonParser(str).Decode() as IDictionary<String, Object>;
            var data = js[dataName];
            var code = 0;// js["code"].ToInt();
            foreach (var item in CodeNames)
            {
                if (js.TryGetValue(item, out var v))
                {
                    code = v.ToInt();
                    break;
                }
            }
            if (code != 0 && code != 200)
            {
                var message = "";
                foreach (var item in MessageNames)
                {
                    if (js.TryGetValue(item, out var v))
                    {
                        message = v as String;
                        break;
                    }
                }
                //var message = js["message"] + "";
                //if (message.IsNullOrEmpty()) message = js["msg"] + "";
                if (message.IsNullOrEmpty()) message = data + "";
                throw new ApiException(code, message);
            }

            // 简单类型
            if (data is TResult result) return result;
            if (rtype == typeof(Object)) return (TResult)data;
            if (rtype.GetTypeCode() != TypeCode.Object) return data.ChangeType<TResult>();

            // 反序列化
            if (data == null) return default;

            if (!(data is IDictionary<String, Object>) && !(data is IList<Object>)) throw new InvalidDataException("未识别响应数据");

            return JsonHelper.Convert<TResult>(data);
        }

        /// <summary>根据动作和参数构造Url</summary>
        /// <param name="action"></param>
        /// <param name="ps"></param>
        /// <returns></returns>
        public static String GetUrl(String action, IDictionary<String, Object> ps)
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

        private static String Encode(String data)
        {
            if (String.IsNullOrEmpty(data)) return String.Empty;

            return Uri.EscapeDataString(data).Replace("%20", "+");
        }
        #endregion
    }
}