using System.Net;
using System.Net.Http;
using NewLife.Net;

namespace NewLife.Http;

/// <summary>支持优化Dns解析的HttpClient处理器</summary>
/// <remarks>
/// 通过自定义 <see cref="IDnsResolver"/> 实现对域名的本地解析与多IP轮询。会把请求 Uri 中的主机替换为解析得到的 IP，并将原始域名保留在 <c>Host</c> 请求头中，
/// 从而实现：自定义 DNS、IP 轮询、快速故障切换等能力。适用于 HTTP / HTTPS；注意：对于 HTTPS，.NET 在 TLS 握手阶段使用 <see cref="HttpRequestMessage.RequestUri"/> 的 Host 参与 SNI，
/// 因此替换为 IP 后可能导致服务器返回的证书与 IP 不匹配而校验失败（常见于仅颁发给域名的证书）。如果目标服务器证书包含该域名且允许通过 IP 访问，或已禁用证书校验，则可正常使用。
/// 如需在保持域名参与 SNI 前提下自定义解析，应考虑直接使用 <c>SocketsHttpHandler</c> 的 ConnectCallback（仅支持较新运行时）而非修改 Uri，这超出当前处理器能力范围。
/// </remarks>
/// <param name="innerHandler">下层处理器，一般为 SocketsHttpHandler 或 HttpClientHandler</param>
public class DnsHttpHandler(HttpMessageHandler innerHandler) : DelegatingHandler(innerHandler)
{
    #region 属性
    /// <summary>DNS解析器</summary>
    public IDnsResolver Resolver { get; set; } = DnsResolver.Instance;
    #endregion

#if NET5_0_OR_GREATER
    // 复用请求选项 Key，避免每次创建。仅在 .NET5+ 可用
    private static readonly HttpRequestOptionsKey<Int32> _dnsIndexKey = new("dnsIndex");
#endif

    #region 方法
    /// <summary>解析域名，可覆写以实现自定义缓存、日志、黑名单等。</summary>
    /// <param name="host">域名/主机</param>
    /// <returns>解析得到的地址数组，可为 null 或空表示放弃处理。</returns>
    protected virtual IPAddress[]? Resolve(String host) => Resolver?.Resolve(host);
    #endregion

    /// <summary>发送请求</summary>
    /// <param name="request">请求消息</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>响应</returns>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var uri = request.RequestUri;
        if (uri == null) return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        // 跳过：已是 IP；或无解析器
        if (IPAddress.TryParse(uri.Host, out _)) return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        // 调用自定义DNS解析器
        var addrs = Resolve(uri.Host);
        if (addrs is { Length: > 0 })
        {
            // 从请求中读取当前使用的IP索引，轮询使用。首个请求从第一个地址开始，保持与系统默认行为一致（按解析顺序优先）
            IPAddress addr;
#if NET5_0_OR_GREATER
            if (!request.Options.TryGetValue(_dnsIndexKey, out var idx)) idx = 0; // 不再随机初始化
            addr = addrs[idx % addrs.Length];
            request.Options.Set(_dnsIndexKey, unchecked(idx + 1));
#else
            var idx = request.Properties.TryGetValue("dnsIndex", out var obj) ? obj.ToInt() : 0; // 不再随机初始化
            addr = addrs[idx % addrs.Length];
            request.Properties["dnsIndex"] = unchecked(idx + 1);
#endif

            // 如果解析得到的地址与原主机文本完全相同（极少数 host 传入已是IP，但前面 TryParse 判定失败情况），则无需替换
            if (!addr.ToString().Equals(uri.Host, StringComparison.OrdinalIgnoreCase))
            {
                // 固定 Host 头，保持逻辑主机不变，便于服务器端基于 Host / 虚拟主机路由
                request.Headers.Host ??= uri.Host;

                // 构造新的 Uri（仅替换 Host）。UriBuilder 会正确处理 IPv6 加[]
                var builder = new UriBuilder(uri)
                {
                    Host = addr.ToString(),
                };

                // 再把Uri换成IP（注意：HTTPS 下会影响 SNI，详见类注释）
                request.RequestUri = builder.Uri;
            }
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}