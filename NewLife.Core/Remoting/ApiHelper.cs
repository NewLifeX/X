using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Serialization;
#if !NET4
using TaskEx = System.Threading.Tasks.Task;
#endif

namespace NewLife.Remoting
{
    /// <summary>Api助手</summary>
    public static class ApiHelper
    {
        #region 远程调用
        /// <summary>性能跟踪器</summary>
        public static ITracer Tracer { get; set; } = DefaultTracer.Instance;

        /// <summary>异步调用，等待返回结果</summary>
        /// <typeparam name="TResult">响应类型，优先原始字节数据，字典返回整体，Object返回data，没找到data时返回整体字典，其它对data反序列化</typeparam>
        /// <param name="client">Http客户端</param>
        /// <param name="action">服务操作</param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        public static async Task<TResult> GetAsync<TResult>(this HttpClient client, String action, Object args = null) => await client.InvokeAsync<TResult>(HttpMethod.Get, action, args);

        /// <summary>同步获取，参数构造在Url</summary>
        /// <typeparam name="TResult">响应类型，优先原始字节数据，字典返回整体，Object返回data，没找到data时返回整体字典，其它对data反序列化</typeparam>
        /// <param name="client">Http客户端</param>
        /// <param name="action">服务操作</param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        public static TResult Get<TResult>(this HttpClient client, String action, Object args = null) => TaskEx.Run(() => GetAsync<TResult>(client, action, args)).Result;

        /// <summary>异步调用，等待返回结果</summary>
        /// <typeparam name="TResult">响应类型，优先原始字节数据，字典返回整体，Object返回data，没找到data时返回整体字典，其它对data反序列化</typeparam>
        /// <param name="client">Http客户端</param>
        /// <param name="action">服务操作</param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        public static async Task<TResult> PostAsync<TResult>(this HttpClient client, String action, Object args = null) => await client.InvokeAsync<TResult>(HttpMethod.Post, action, args);

        /// <summary>同步提交，参数Json打包在Body</summary>
        /// <typeparam name="TResult">响应类型，优先原始字节数据，字典返回整体，Object返回data，没找到data时返回整体字典，其它对data反序列化</typeparam>
        /// <param name="client">Http客户端</param>
        /// <param name="action">服务操作</param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        public static TResult Post<TResult>(this HttpClient client, String action, Object args = null) => TaskEx.Run(() => PostAsync<TResult>(client, action, args)).Result;

        /// <summary>异步调用，等待返回结果</summary>
        /// <typeparam name="TResult">响应类型，优先原始字节数据，字典返回整体，Object返回data，没找到data时返回整体字典，其它对data反序列化</typeparam>
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

            // 开始跟踪，注入TraceId
            using var span = Tracer?.NewSpan(request);
            try
            {
                // 发起请求
                var msg = await client.SendAsync(request);
                return await ProcessResponse<TResult>(msg, dataName);
            }
            catch (Exception ex)
            {
                // 跟踪异常
                span?.SetError(ex, args);

                throw;
            }
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
            else if (method == HttpMethod.Post || method == HttpMethod.Put)
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

        /// <summary>结果代码名称。默认 code/errcode</summary>
        public static IList<String> CodeNames { get; } = new List<String> { "code", "errcode" };

        /// <summary>结果消息名称。默认 message/msg/errmsg</summary>
        public static IList<String> MessageNames { get; } = new List<String> { "message", "msg", "errmsg", "error" };

        /// <summary>处理响应。统一识别code/message</summary>
        /// <typeparam name="TResult">响应类型，优先原始字节数据，字典返回整体，Object返回data，没找到data时返回整体字典，其它对data反序列化</typeparam>
        /// <param name="response">Http响应消息</param>
        /// <param name="dataName">数据字段名称，默认data。同一套rpc体系不同接口的code/message一致，但data可能不同</param>
        /// <returns></returns>
        public static async Task<TResult> ProcessResponse<TResult>(HttpResponseMessage response, String dataName = "data")
        {
            var rtype = typeof(TResult);
            if (rtype == typeof(HttpResponseMessage)) return (TResult)(Object)response;

            var buf = response.Content == null ? null : (await response.Content.ReadAsByteArrayAsync());

            // 异常处理
            if (response.StatusCode >= HttpStatusCode.BadRequest) throw new ApiException((Int32)response.StatusCode, buf.ToStr()?.Trim('\"') ?? response.ReasonPhrase);
            if (buf == null || buf.Length == 0) return default;

            // 原始数据
            if (rtype == typeof(Byte[])) return (TResult)(Object)buf;
            if (rtype == typeof(Packet)) return (TResult)(Object)new Packet(buf);

            var str = buf.ToStr()?.Trim();
            return ProcessResponse<TResult>(str, dataName);
        }

        /// <summary>处理响应。</summary>
        /// <typeparam name="TResult">响应类型，字典返回整体，Object返回data，没找到data时返回整体字典，其它对data反序列化</typeparam>
        /// <param name="response">文本响应消息</param>
        /// <param name="dataName">数据字段名称，默认data。同一套rpc体系不同接口的code/message一致，但data可能不同</param>
        /// <returns></returns>
        public static TResult ProcessResponse<TResult>(String response, String dataName = "data")
        {
            if (response.IsNullOrEmpty()) return default;

            var rtype = typeof(TResult);

            var dic = response.StartsWith("<") && response.EndsWith(">") ? XmlParser.Decode(response) : JsonParser.Decode(response);
            var nodata = dataName.IsNullOrEmpty() || !dic.ContainsKey(dataName);

            // 未指定有效数据名时，整体返回
            if (nodata && rtype == typeof(IDictionary<String, Object>)) return (TResult)dic;

            // 如果没有指定数据名，或者结果中不包含数据名，则整个字典作为结果数据
            var data = nodata ? dic : dic[dataName];

            var code = 0;
            foreach (var item in CodeNames)
            {
                if (dic.TryGetValue(item, out var v))
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
                    if (dic.TryGetValue(item, out var v))
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
                    sb.Append('&');
                else
                    sb.Append('?');

                var first = true;
                foreach (var item in ps)
                {
                    if (!first) sb.Append('&');
                    first = false;

                    var v = item.Value is DateTime dt ? dt.ToFullString() : (item.Value + "");
                    sb.AppendFormat("{0}={1}", Encode(item.Key), Encode(v));
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