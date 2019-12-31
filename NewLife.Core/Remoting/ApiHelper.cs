#if !NET4
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
        /// <returns></returns>
        public static async Task<TResult> InvokeAsync<TResult>(this HttpClient client, HttpMethod method, String action, Object args = null)
        {
            if (client?.BaseAddress == null) throw new ArgumentNullException(nameof(client.BaseAddress));

            var rtype = typeof(TResult);

            // 构建请求
            var request = BuildRequest(method, action, args, rtype);

            // 发起请求
            var msg = await client.SendAsync(request);
            return await ProcessResponse<TResult>(msg);
        }

        /// <summary>建立请求</summary>
        /// <param name="method">请求方法</param>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <param name="returnType"></param>
        /// <returns></returns>
        public static HttpRequestMessage BuildRequest(HttpMethod method, String action, Object args, Type returnType)
        {
            // 序列化参数，决定GET/POST
            var request = new HttpRequestMessage(method, action);
            if (returnType != typeof(Byte[]) && returnType != typeof(Packet))
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (method == HttpMethod.Get)
            {
                var ps = args?.ToDictionary();
                var url = GetUrl(action, ps);
                request.RequestUri = new Uri(url, UriKind.RelativeOrAbsolute);
            }
            else
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

        /// <summary>处理响应</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static async Task<TResult> ProcessResponse<TResult>(HttpResponseMessage msg)
        {
            var rtype = typeof(TResult);
            if (rtype == typeof(HttpResponseMessage)) return (TResult)(Object)msg;

            var code = msg.StatusCode;
            var buf = await msg.Content.ReadAsByteArrayAsync();
            if (buf == null || buf.Length == 0) return default;

            // 异常处理
            if (code != HttpStatusCode.OK) throw new ApiException((Int32)code, buf.ToStr()?.Trim('\"'));

            // 原始数据
            if (rtype == typeof(Byte[])) return (TResult)(Object)buf;
            if (rtype == typeof(Packet)) return (TResult)(Object)new Packet(buf);

            var str = buf.ToStr();
            var js = new JsonParser(str).Decode() as IDictionary<String, Object>;
            var data = js["data"];
            var code2 = js["code"].ToInt();
            if (code2 != 0 && code2 != 200)
            {
                var message = js["message"] + "";
                if (message.IsNullOrEmpty()) message = js["msg"] + "";
                if (message.IsNullOrEmpty()) message = data + "";
                throw new ApiException(code2, message);
            }

            // 简单类型
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
#endif