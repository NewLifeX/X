using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace NewLife.Remoting;

/// <summary>对端地址选择器，缓存可用性并排序输出候选地址</summary>
public class PeerEndpointSelector
{
    #region 属性
    /// <summary>地址状态集合</summary>
    public EndpointState[] Endpoints { get; set; } = [];

    /// <summary>失败屏蔽时间，秒。默认600秒</summary>
    public Int32 ShieldingSeconds { get; set; } = 600;

    /// <summary>探测结果缓存刷新间隔，秒。默认600秒</summary>
    public Int32 RefreshSeconds { get; set; } = 600;

    /// <summary>探测超时时间，毫秒。默认3000ms</summary>
    public Int32 ProbeTimeout { get; set; } = 3000;

    /// <summary>并行探测最大并发。默认8</summary>
    public Int32 MaxProbeConcurrency { get; set; } = 8;

    /// <summary>探测路径，附加到地址后。默认/cube/info</summary>
    public String ProbePath { get; set; } = "/cube/info";

    /// <summary>是否仅获取响应头进行探测。默认 false 使用完整 GET</summary>
    public Boolean ProbeHeadersOnly { get; set; }

    /// <summary>自定义探测委托，返回RTT；返回null视为失败</summary>
    public Func<Uri, CancellationToken, Task<TimeSpan?>>? ProbeAsync { get; set; }

    private readonly Object _lock = new();
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    public PeerEndpointSelector() { }

    /// <summary>实例化并导入地址</summary>
    /// <param name="internalAddresses">内网地址集合，逗号分隔</param>
    /// <param name="externalAddresses">外网地址集合，逗号分隔</param>
    public PeerEndpointSelector(String? internalAddresses, String? externalAddresses) => SetAddresses(internalAddresses, externalAddresses);
    #endregion

    #region 方法
    /// <summary>设置地址列表，覆盖旧值</summary>
    /// <param name="internalAddresses">内网地址集合，逗号分隔</param>
    /// <param name="externalAddresses">外网地址集合，逗号分隔</param>
    public void SetAddresses(String? internalAddresses, String? externalAddresses)
    {
        lock (_lock)
        {
            var endpoints = new List<EndpointState>();
            AddAddresses(endpoints, internalAddresses, true);
            AddAddresses(endpoints, externalAddresses, false);
            Endpoints = [.. endpoints];
        }
    }

    /// <summary>添加地址</summary>
    /// <param name="address">地址</param>
    /// <param name="internalAddress">是否内网地址</param>
    public EndpointState AddAddress(Uri address, Boolean internalAddress)
    {
        var name = address.GetLeftPart(UriPartial.Authority).TrimEnd('/');
        var state = Endpoints.FirstOrDefault(e => e.Name.EqualIgnoreCase(name));
        if (state != null) return state;

        lock (_lock)
        {
            var endpoints = new List<EndpointState>(Endpoints);
            state = CreateEndpoint(address, internalAddress);
            endpoints.Add(state);
            Endpoints = [.. endpoints];

            return state;
        }
    }

    /// <summary>获取已排序的地址列表，必要时触发探测</summary>
    /// <param name="forceProbe">是否强制探测全部地址</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns>按优先级和RTT排序的地址状态列表</returns>
    public async Task<IReadOnlyList<EndpointState>> GetOrderedEndpointsAsync(Boolean forceProbe = false, CancellationToken cancellationToken = default)
    {
        var snapshot = Endpoints;
        var hasUsable = snapshot.Any(e => e.IsUp);
        var hasStale = snapshot.Any(ShouldProbe);

        if (forceProbe || (!hasUsable && hasStale))
        {
            await ProbeEndpointsAsync(snapshot, forceProbe, cancellationToken).ConfigureAwait(false);
            snapshot = Endpoints;
        }
        else if (hasStale)
        {
            _ = Task.Run(() => ProbeEndpointsAsync(snapshot, false, CancellationToken.None), cancellationToken);
        }

        // 按分类优先级和RTT排序
        var available = snapshot.Where(e => e.IsUp)
           .OrderBy(e => e.Priority)
           .ThenBy(e => e.Rtt ?? TimeSpan.MaxValue)
           .ThenBy(e => e.Failures)
           .ThenBy(e => e.Address.AbsoluteUri)
           .ToList();

        //var available = snapshot2.Any(e => e.IsUp) ? snapshot2.Where(e => e.IsUp).ToList() : snapshot2;
        for (var i = 0; i < available.Count; i++)
        {
            available[i].Score = i * 100;
        }

        return available;
    }

    /// <summary>获取已排序的地址列表</summary>
    /// <param name="forceProbe">是否强制探测全部地址</param>
    /// <returns>按优先级和RTT排序的地址状态列表</returns>
    public IReadOnlyList<EndpointState> GetOrderedEndpoints(Boolean forceProbe = false) => GetOrderedEndpointsAsync(forceProbe, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

    private async Task ProbeEndpointsAsync(IEnumerable<EndpointState> snapshot, Boolean forceProbe, CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();
        using var semaphore = new SemaphoreSlim(MaxProbeConcurrency > 0 ? MaxProbeConcurrency : 1);

        foreach (var state in snapshot)
        {
            if (!forceProbe && !ShouldProbe(state)) continue;

            tasks.Add(ProbeOneAsync(state, semaphore, cancellationToken));
        }

        if (tasks.Count > 0) await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task ProbeOneAsync(EndpointState state, SemaphoreSlim semaphore, CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var uri = new Uri(state.Address, ProbePath + "");
            var func = ProbeAsync ?? ExecuteProbeAsync;
            var rtt = await func(uri, cancellationToken).ConfigureAwait(false);
            if (rtt != null)
                MarkSuccess(state.Name, rtt.Value);
            else
                MarkFailure(state.Name, null);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task<TimeSpan?> ExecuteProbeAsync(Uri uri, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(ProbeTimeout > 0 ? ProbeTimeout : 1000);

        try
        {
            var sw = Stopwatch.StartNew();
            using var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(ProbeTimeout > 0 ? ProbeTimeout : 1000) };
            var completion = ProbeHeadersOnly ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead;
            using var response = await client.GetAsync(uri, completion, cts.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return null;
            return sw.Elapsed;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>标记成功，更新RTT和可用性</summary>
    /// <param name="name">端点名称（规范化地址）</param>
    /// <param name="rtt">往返耗时</param>
    public void MarkSuccess(String name, TimeSpan rtt)
    {
        if (name.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(name));

        var state = Endpoints.FirstOrDefault(e => e.Name.EqualIgnoreCase(name));
        if (state == null) return;

        lock (_lock)
        {
            state.IsUp = true;
            state.LastSuccess = DateTime.Now;
            state.Failures = 0;
            state.Rtt = state.Rtt == null ? rtt : TimeSpan.FromMilliseconds((state.Rtt.Value.TotalMilliseconds * 3 + rtt.TotalMilliseconds) / 4);
            state.NextProbe = DateTime.Now.AddSeconds(RefreshSeconds);
        }
    }

    /// <summary>标记失败，屏蔽一段时间</summary>
    /// <param name="name">端点名称（规范化地址）</param>
    /// <param name="error">异常信息</param>
    public void MarkFailure(String name, Exception? error)
    {
        if (name.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(name));

        var state = Endpoints.FirstOrDefault(e => e.Name.EqualIgnoreCase(name));
        if (state == null) return;

        lock (_lock)
        {
            state.IsUp = false;
            state.LastFailure = DateTime.Now;
            state.Failures++;
            state.NextProbe = DateTime.Now.AddSeconds(ShieldingSeconds);
            state.LastError = error?.Message;
            state.Rtt = null;
        }
    }
    #endregion

    #region 辅助
    private static void AddAddresses(List<EndpointState> endpoints, String? addresses, Boolean internalAddress)
    {
        if (addresses.IsNullOrWhiteSpace()) return;

        var ss = addresses.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var item in ss)
        {
            var url = item.Trim();
            if (url.IsNullOrEmpty()) continue;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) continue;

            var state = CreateEndpoint(uri, internalAddress);
            endpoints.Add(state);
        }
    }

    private static EndpointState CreateEndpoint(Uri uri, Boolean internalAddress)
    {
        var category = EndpointCategory.ExternalDomain;
        if (IPAddress.TryParse(uri.Host, out var ip))
        {
#if NETCOREAPP || NETSTANDARD2_1
            if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
#endif
            if (ip.AddressFamily == AddressFamily.InterNetwork)
                category = internalAddress ? EndpointCategory.InternalIPv4 : EndpointCategory.ExternalIPv4;
            else if (ip.AddressFamily == AddressFamily.InterNetworkV6)
                category = internalAddress ? EndpointCategory.InternalIPv6 : EndpointCategory.ExternalIPv6;
        }

        return new EndpointState
        {
            Name = uri.GetLeftPart(UriPartial.Authority).TrimEnd('/'),
            Address = uri,
            Category = category,
            IsUp = true,
            NextProbe = DateTime.MinValue
        };
    }

    private static Boolean ShouldProbe(EndpointState state) => !state.IsUp || state.NextProbe <= DateTime.Now;
    #endregion

    #region 嵌套类型
    /// <summary>端点类型</summary>
    public enum EndpointCategory
    {
        /// <summary>内网IPv4</summary>
        InternalIPv4,
        /// <summary>内网IPv6</summary>
        InternalIPv6,
        /// <summary>公网IPv4</summary>
        ExternalIPv4,
        /// <summary>公网域名</summary>
        ExternalDomain,
        /// <summary>公网IPv6</summary>
        ExternalIPv6
    }

    /// <summary>端点状态</summary>
    public class EndpointState
    {
        /// <summary>名称</summary>
        public String Name { get; set; } = null!;

        /// <summary>地址</summary>
        public Uri Address { get; set; } = null!;

        /// <summary>类别</summary>
        public EndpointCategory Category { get; set; }

        /// <summary>优先级</summary>
        public Int32 Priority => (Int32)Category;

        /// <summary>是否可用</summary>
        public Boolean IsUp { get; set; }

        /// <summary>往返耗时</summary>
        public TimeSpan? Rtt { get; set; }

        /// <summary>最后成功时间</summary>
        public DateTime LastSuccess { get; set; }

        /// <summary>最后失败时间</summary>
        public DateTime LastFailure { get; set; }

        /// <summary>失败次数</summary>
        public Int32 Failures { get; set; }

        /// <summary>下一次探测时间</summary>
        public DateTime NextProbe { get; set; }

        /// <summary>最后错误</summary>
        public String? LastError { get; set; }

        /// <summary>延迟分数。用于竞速时的延时（毫秒）</summary>
        public Int32 Score { get; set; }

        /// <summary>状态对象，可存放自定义数据</summary>
        public Object? State { get; set; }
    }
    #endregion
}
