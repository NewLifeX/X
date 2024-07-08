﻿using System.Net;
using System.Net.Http.Headers;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Http;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Remoting;

/// <summary>Api助手</summary>
public static class ApiHelper
{
    #region 远程调用
    /// <summary>性能跟踪器</summary>
    public static ITracer? Tracer { get; set; } = DefaultTracer.Instance;

    /// <summary>Http过滤器</summary>
    public static IHttpFilter? Filter { get; set; }

    /// <summary>异步调用，等待返回结果</summary>
    /// <typeparam name="TResult">响应类型，优先原始字节数据，字典返回整体，Object返回data，没找到data时返回整体字典，其它对data反序列化</typeparam>
    /// <param name="client">Http客户端</param>
    /// <param name="action">服务操作</param>
    /// <param name="args">参数</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    public static async Task<TResult?> GetAsync<TResult>(this HttpClient client, String action, Object? args = null, CancellationToken cancellationToken = default) => await client.InvokeAsync<TResult>(HttpMethod.Get, action, args, null, "data", cancellationToken);

    /// <summary>同步获取，参数构造在Url</summary>
    /// <typeparam name="TResult">响应类型，优先原始字节数据，字典返回整体，Object返回data，没找到data时返回整体字典，其它对data反序列化</typeparam>
    /// <param name="client">Http客户端</param>
    /// <param name="action">服务操作</param>
    /// <param name="args">参数</param>
    /// <returns></returns>
    public static TResult? Get<TResult>(this HttpClient client, String action, Object? args = null) => GetAsync<TResult>(client, action, args).ConfigureAwait(false).GetAwaiter().GetResult();

    /// <summary>异步调用，等待返回结果</summary>
    /// <typeparam name="TResult">响应类型，优先原始字节数据，字典返回整体，Object返回data，没找到data时返回整体字典，其它对data反序列化</typeparam>
    /// <param name="client">Http客户端</param>
    /// <param name="action">服务操作</param>
    /// <param name="args">参数</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    public static async Task<TResult?> PostAsync<TResult>(this HttpClient client, String action, Object? args = null, CancellationToken cancellationToken = default) => await client.InvokeAsync<TResult>(HttpMethod.Post, action, args, null, "data", cancellationToken);

    /// <summary>同步提交，参数Json打包在Body</summary>
    /// <typeparam name="TResult">响应类型，优先原始字节数据，字典返回整体，Object返回data，没找到data时返回整体字典，其它对data反序列化</typeparam>
    /// <param name="client">Http客户端</param>
    /// <param name="action">服务操作</param>
    /// <param name="args">参数</param>
    /// <returns></returns>
    public static TResult? Post<TResult>(this HttpClient client, String action, Object? args = null) => PostAsync<TResult>(client, action, args).ConfigureAwait(false).GetAwaiter().GetResult();

    /// <summary>异步上传，等待返回结果</summary>
    /// <typeparam name="TResult">响应类型，优先原始字节数据，字典返回整体，Object返回data，没找到data时返回整体字典，其它对data反序列化</typeparam>
    /// <param name="client">Http客户端</param>
    /// <param name="action">服务操作</param>
    /// <param name="args">参数</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    public static async Task<TResult?> PutAsync<TResult>(this HttpClient client, String action, Object? args = null, CancellationToken cancellationToken = default) => await client.InvokeAsync<TResult>(HttpMethod.Put, action, args, null, "data", cancellationToken);

    /// <summary>异步删除，等待返回结果</summary>
    /// <typeparam name="TResult">响应类型，优先原始字节数据，字典返回整体，Object返回data，没找到data时返回整体字典，其它对data反序列化</typeparam>
    /// <param name="client">Http客户端</param>
    /// <param name="action">服务操作</param>
    /// <param name="args">参数</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    public static async Task<TResult?> DeleteAsync<TResult>(this HttpClient client, String action, Object? args = null, CancellationToken cancellationToken = default) => await client.InvokeAsync<TResult>(HttpMethod.Delete, action, args, null, "data", cancellationToken);

    /// <summary>异步调用，等待返回结果</summary>
    /// <typeparam name="TResult">响应类型，优先原始字节数据，字典返回整体，Object返回data，没找到data时返回整体字典，其它对data反序列化</typeparam>
    /// <param name="client">Http客户端</param>
    /// <param name="method">请求方法</param>
    /// <param name="action">服务操作</param>
    /// <param name="args">参数</param>
    /// <param name="onRequest">请求头回调</param>
    /// <param name="dataName">数据字段名称，默认data。同一套rpc体系不同接口的code/message一致，但data可能不同</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    public static async Task<TResult?> InvokeAsync<TResult>(this HttpClient client, HttpMethod method, String action, Object? args = null, Action<HttpRequestMessage>? onRequest = null, String dataName = "data", CancellationToken cancellationToken = default)
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
        var filter = Filter;
        try
        {
            // 发起请求
            if (filter != null) await filter.OnRequest(client, request, null, cancellationToken);

            var response = await client.SendAsync(request, cancellationToken);

            if (filter != null) await filter.OnResponse(client, response, request, cancellationToken);

            return await ProcessResponse<TResult>(response, null, dataName);
        }
        catch (Exception ex)
        {
            // 跟踪异常
            span?.SetError(ex, args);

            if (filter != null) await filter.OnError(client, ex, request, cancellationToken);

            throw;
        }
    }
    #endregion

    #region 远程辅助
    /// <summary>建立请求，action写到url里面</summary>
    /// <param name="method">请求方法</param>
    /// <param name="action">动作</param>
    /// <param name="args">参数</param>
    /// <param name="jsonHost">Json序列化主机</param>
    /// <returns></returns>
    public static HttpRequestMessage BuildRequest(HttpMethod method, String action, Object? args, IJsonHost? jsonHost = null)
    {
        // 序列化参数，决定GET/POST
        var request = new HttpRequestMessage(method, action);

        if (args is HttpContent content)
            request.Content = content;
        else if (method == HttpMethod.Get || method == HttpMethod.Delete)
        {
            if (args is Packet pk)
            {
                var url = action;
                url += url.Contains('?') ? "&" : "?";
                url += pk.ToArray().ToUrlBase64();
                request.RequestUri = new Uri(url, UriKind.RelativeOrAbsolute);
            }
            else if (args is Byte[] buf)
            {
                var url = action;
                url += url.Contains('?') ? "&" : "?";
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
        else if (method == HttpMethod.Post || method == HttpMethod.Put || method.Method == "PATCH")
        {
            if (args is Packet pk)
            {
                request.Content = BuildContent(pk);
            }
            else if (args is Byte[] buf)
            {
                if (buf != null) request.Content = BuildContent(buf);
            }
            else if (args != null)
            {
                jsonHost ??= JsonHelper.Default;
                content = new ByteArrayContent(jsonHost.Write(args).GetBytes());
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                request.Content = content;
            }
        }

        return request;
    }

    /// <summary>为二进制数据生成请求体内容。对超长内容进行压缩</summary>
    /// <param name="pk"></param>
    /// <returns></returns>
    public static HttpContent BuildContent(Packet pk)
    {
        var gzip = NewLife.Net.SocketSetting.Current.AutoGZip;
        if (gzip > 0 && pk.Total >= gzip)
        {
            var buf = pk.ReadBytes();
            buf = buf.CompressGZip();
            var content = new ByteArrayContent(buf);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-gzip");
            return content;
        }
        else
        {
            var content = pk.Next == null ?
                new ByteArrayContent(pk.Data, pk.Offset, pk.Count) :
                new ByteArrayContent(pk.ToArray());
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            return content;
        }
    }

    /// <summary>结果代码名称。默认 code/errcode</summary>
    public static IList<String> CodeNames { get; } = ["code", "errcode", "status"];

    /// <summary>结果消息名称。默认 message/msg/errmsg</summary>
    public static IList<String> MessageNames { get; } = ["message", "msg", "errmsg", "error"];

    /// <summary>处理响应。统一识别code/message</summary>
    /// <typeparam name="TResult">响应类型，优先原始字节数据，字典返回整体，Object返回data，没找到data时返回整体字典，其它对data反序列化</typeparam>
    /// <param name="response">Http响应消息</param>
    /// <param name="dataName">数据字段名称，默认data。同一套rpc体系不同接口的code/message一致，但data可能不同</param>
    /// <returns></returns>
    public static async Task<TResult?> ProcessResponse<TResult>(HttpResponseMessage response, String dataName = "data") => await ProcessResponse<TResult>(response, null, dataName);

    /// <summary>处理响应。统一识别code/message</summary>
    /// <typeparam name="TResult">响应类型，优先原始字节数据，字典返回整体，Object返回data，没找到data时返回整体字典，其它对data反序列化</typeparam>
    /// <param name="codeName">状态码字段名</param>
    /// <param name="response">Http响应消息</param>
    /// <param name="dataName">数据字段名称，默认data。同一套rpc体系不同接口的code/message一致，但data可能不同</param>
    /// <returns></returns>
    public static Task<TResult?> ProcessResponse<TResult>(HttpResponseMessage response, String? codeName, String? dataName) => ProcessResponse<TResult>(response, codeName, dataName, JsonHelper.Default);

    /// <summary>处理响应。统一识别code/message</summary>
    /// <typeparam name="TResult">响应类型，优先原始字节数据，字典返回整体，Object返回data，没找到data时返回整体字典，其它对data反序列化</typeparam>
    /// <param name="codeName">状态码字段名</param>
    /// <param name="response">Http响应消息</param>
    /// <param name="dataName">数据字段名称，默认data。同一套rpc体系不同接口的code/message一致，但data可能不同</param>
    /// <param name="jsonHost">Json序列化主机</param>
    /// <returns></returns>
    public static async Task<TResult?> ProcessResponse<TResult>(HttpResponseMessage response, String? codeName, String? dataName, IJsonHost jsonHost)
    {
        var rtype = typeof(TResult);
        if (rtype == typeof(HttpResponseMessage)) return (TResult)(Object)response;

        var buf = response.Content == null ? null : (await response.Content.ReadAsByteArrayAsync());

        // 异常处理
        if (response.StatusCode >= HttpStatusCode.BadRequest)
        {
            var msg = buf?.ToStr().Trim('\"');
            // 400响应可能包含错误信息
            if (!msg.IsNullOrEmpty() && msg.StartsWith("{") && msg.EndsWith("}"))
            {
                var dic = jsonHost.Decode(msg);
                if (dic != null)
                {
                    var msg2 = "";
                    if (dic.TryGetValue("title", out var v)) msg2 = v + "";
                    if (dic.TryGetValue("errors", out v) && v != null) msg2 += jsonHost.Write(v);
                    if (!msg2.IsNullOrEmpty()) msg = msg2.Trim();
                }
            }
            if (msg.IsNullOrEmpty()) msg = response.ReasonPhrase;
            if (msg.IsNullOrEmpty()) msg = response.StatusCode + "";
            throw new ApiException((Int32)response.StatusCode, msg);
        }
        if (buf == null || buf.Length == 0) return default;

        // 原始数据
        if (rtype == typeof(Byte[])) return (TResult)(Object)buf;
        if (rtype == typeof(Packet)) return (TResult)(Object)new Packet(buf);

        var str = buf.ToStr().Trim();
        return ProcessResponse<TResult>(str, codeName, dataName ?? "data", jsonHost);
    }

    /// <summary>处理响应。</summary>
    /// <typeparam name="TResult">响应类型，字典返回整体，Object返回data，没找到data时返回整体字典，其它对data反序列化</typeparam>
    /// <param name="response">文本响应消息</param>
    /// <param name="codeName">状态码字段名</param>
    /// <param name="dataName">数据字段名称，默认data。同一套rpc体系不同接口的code/message一致，但data可能不同</param>
    /// <param name="jsonHost">Json序列化主机</param>
    /// <returns></returns>
    public static TResult? ProcessResponse<TResult>(String? response, String? codeName, String dataName, IJsonHost? jsonHost = null)
    {
        if (response.IsNullOrEmpty()) return default;

        var rtype = typeof(TResult);
        jsonHost ??= JsonHelper.Default;

        IDictionary<String, Object?>? dic = null;
        if (response.StartsWith("<") && response.EndsWith(">"))
        {
            // XML反序列化
            dic = XmlParser.Decode(response);
        }
        else
        {
            // Json反序列化，可能是字典或列表
            var obj = jsonHost.Parse(response);

            // 如果没有data部分。可能是 IDictionary<String, Object?> 或 IList<Object?> ，或其它
            if (dataName.IsNullOrEmpty() && obj is TResult result2) return result2;

            dic = obj as IDictionary<String, Object?>;
        }

        //if (dic == null) return default;
        if (dic == null) throw new InvalidCastException($"Unable to convert to type [{typeof(TResult)}]! {response.Cut(64)}");

        var nodata = dataName.IsNullOrEmpty() || !dic.ContainsKey(dataName);

        // 未指定有效数据名时，整体返回
        if (nodata && dic is TResult result3) return result3;

        // 如果没有指定数据名，或者结果中不包含数据名，则整个字典作为结果数据
        var data = nodata ? dic : dic[dataName];

        var code = 0;
        if (!codeName.IsNullOrEmpty())
        {
            if (dic.TryGetValue(codeName, out var v))
            {
                if (v is Boolean b)
                    code = b ? 0 : -1;
                else
                    code = v.ToInt();
            }
        }
        else
        {
            foreach (var item in CodeNames)
            {
                if (dic.TryGetValue(item, out var v))
                {
                    if (v is Boolean b)
                        code = b ? 0 : -1;
                    else
                        code = v.ToInt();
                    break;
                }
            }
        }
        if (code is not ApiCode.Ok and not ApiCode.Ok200)
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
        if (rtype == typeof(Object)) return (TResult?)data;
        if (data == null && rtype.IsNullable()) return (TResult?)(Object?)null;
        if (rtype.IsBaseType()) return data.ChangeType<TResult>();

        // 反序列化
        if (data == null) return default;

        if (data is not IDictionary<String, Object> and not IList<Object>)
            throw new InvalidDataException($"Unrecognized response data [{(data as String)?.Cut(64)}] for [{typeof(TResult).Name}]");

        return jsonHost.Convert<TResult>(data);
    }

    /// <summary>根据动作和参数构造Url</summary>
    /// <param name="action"></param>
    /// <param name="ps"></param>
    /// <returns></returns>
    public static String GetUrl(String action, IDictionary<String, Object?>? ps)
    {
        var url = action;
        if (ps != null && ps.Count > 0)
        {
            var sb = Pool.StringBuilder.Get();
            sb.Append(action);
            if (action.Contains('?'))
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