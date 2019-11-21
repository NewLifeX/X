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
        public static async Task<TResult> GetAsync<TResult>(this HttpClient client, String action, Object args = null)
        {
            return await client.InvokeAsync<TResult>(HttpMethod.Get, action, args);
        }

        /// <summary>异步调用，等待返回结果</summary>
        /// <param name="client">Http客户端</param>
        /// <param name="action">服务操作</param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        public static async Task<TResult> PostAsync<TResult>(this HttpClient client, String action, Object args = null)
        {
            return await client.InvokeAsync<TResult>(HttpMethod.Post, action, args);
        }

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

            // 序列化参数，决定GET/POST
            var request = new HttpRequestMessage(method, action);
            if (rtype != typeof(Byte[]) && rtype != typeof(Packet))
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (args is Packet pk)
            {
                var content = new ByteArrayContent(pk.ToArray());
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                request.Content = content;
            }
            else if (args is Byte[] bufData)
            {
                var content = new ByteArrayContent(bufData);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                request.Content = content;
            }
            else if (args != null && method == HttpMethod.Post)
            {
                var ps = args?.ToDictionary();
                var content = new ByteArrayContent(ps.ToJson().GetBytes());
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                request.Content = content;
            }
            else
            {
                var ps = args?.ToDictionary();
                var url = GetUrl(action, ps);
                request.RequestUri = new Uri(url, UriKind.RelativeOrAbsolute);
            }

            // 发起请求
            var msg = await client.SendAsync(request);
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
            if (code2 != 0 && code2 != 200) throw new ApiException(code2, data + "");

            // 简单类型
            if (rtype.GetTypeCode() != TypeCode.Object) return data.ChangeType<TResult>();

            // 反序列化
            if (data == null) return default;

            if (!(data is IDictionary<String, Object>) && !(data is IList<Object>)) throw new InvalidDataException("未识别响应数据");

            return JsonHelper.Convert<TResult>(data);
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
    }
}
#endif