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
        /// <param name="useHttpStatus">是否使用Http状态抛出异常</param>
        /// <returns></returns>
        public static async Task<TResult> InvokeAsync<TResult>(this HttpClient client, String action, Object args = null, Boolean useHttpStatus = false)
        {
            if (client?.BaseAddress == null) throw new ArgumentNullException(nameof(client.BaseAddress));

            var rtype = typeof(TResult);

            // 序列化参数，决定GET/POST
            var request = new HttpRequestMessage(HttpMethod.Get, action);
            if (rtype != typeof(Byte[]) && rtype != typeof(Packet))
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            Byte[] buf = null;
            var code = HttpStatusCode.OK;
            var ps = args?.ToDictionary();
            if (ps != null && ps.Any(e => e.Value != null && e.Value.GetType().GetTypeCode() == TypeCode.Object))
            {
                var content = new ByteArrayContent(ps.ToJson().GetBytes());
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                request.Content = content;
                request.Method = HttpMethod.Post;
            }
            else
            {
                var url = GetUrl(action, ps);
                // 如果长度过大，还是走POST
                if (url.Length < 1000)
                    request.RequestUri = new Uri(url, UriKind.RelativeOrAbsolute);
                else
                {
                    var content = new ByteArrayContent(ps.ToJson().GetBytes());
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    request.Content = content;
                    request.Method = HttpMethod.Post;
                }
            }

            // 发起请求
            var msg = await client.SendAsync(request);
            if (rtype == typeof(HttpResponseMessage)) return (TResult)(Object)msg;

            code = msg.StatusCode;
            buf = await msg.Content.ReadAsByteArrayAsync();
            if (buf == null || buf.Length == 0) return default;

            // 异常处理
            if (code != HttpStatusCode.OK) throw new ApiException((Int32)code, buf.ToStr()?.Trim('\"'));

            // 原始数据
            if (rtype == typeof(Byte[])) return (TResult)(Object)buf;
            if (rtype == typeof(Packet)) return (TResult)(Object)new Packet(buf);

            var str = buf.ToStr();
            Object data = str;
            if (!useHttpStatus)
            {
                var js2 = new JsonParser(str).Decode() as IDictionary<String, Object>;
                data = js2["data"];
                var code2 = js2["code"].ToInt();
                if (code2 != 0) throw new ApiException(code2, data + "");
            }

            // 简单类型
            if (rtype.GetTypeCode() != TypeCode.Object) return data.ChangeType<TResult>();

            // 反序列化
            if (useHttpStatus) data = new JsonParser(str).Decode();
            if (!(data is IDictionary<String, Object>) && !(data is IList<Object>)) throw new InvalidDataException("未识别响应数据");

            return JsonHelper.Convert<TResult>(data);
        }

        /// <summary>同步调用，阻塞等待</summary>
        /// <param name="client">Http客户端</param>
        /// <param name="action">服务操作</param>
        /// <param name="args">参数</param>
        /// <param name="useHttpStatus">是否使用Http状态抛出异常</param>
        /// <returns></returns>
        public static TResult Invoke<TResult>(this HttpClient client, String action, Object args = null, Boolean useHttpStatus = false) => Task.Run(() => InvokeAsync<TResult>(client, action, args, useHttpStatus)).Result;

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
