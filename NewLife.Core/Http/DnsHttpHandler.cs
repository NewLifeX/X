﻿using System.Net.Http;
using NewLife.Net;

namespace NewLife.Http;

/// <summary>支持优化Dns解析的HttpClient处理器</summary>
/// <param name="innerHandler"></param>
public class DnsHttpHandler(HttpMessageHandler innerHandler) : DelegatingHandler(innerHandler)
{
    #region 属性
    /// <summary>DNS解析器</summary>
    public IDnsResolver Resolver { get; set; } = DnsResolver.Instance;
    #endregion

    /// <summary>发送请求</summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var uri = request.RequestUri;
        if (uri == null) return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        // 调用自定义DNS解析器
        var addrs = Resolver?.Resolve(uri.Host);
        if (addrs != null && addrs.Length > 0)
        {
            var addr = addrs[0];

            // 从请求中读取当前使用的IP索引，轮询使用。因为HttpClient调用失败后会重试，这里分配新的IP
#if NET5_0_OR_GREATER
            var key = new HttpRequestOptionsKey<Int32>("dnsIndex");
            if (!request.Options.TryGetValue(key, out var idx)) idx = 0;

            addr = addrs[idx % addrs.Length];
            request.Options.Set(key, ++idx);
#else
            var idx = request.Properties.TryGetValue("dnsIndex", out var obj) ? obj.ToInt() : 0;
            addr = addrs[idx % addrs.Length];
            request.Properties["dnsIndex"] = ++idx;
#endif

            // 先固定Host
            request.Headers.Host ??= uri.Host;

            var builder = new UriBuilder(uri)
            {
                Host = addr + "",
            };

            // 再把Uri换成IP
            request.RequestUri = builder.Uri;
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}