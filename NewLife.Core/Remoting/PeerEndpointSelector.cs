using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace NewLife.Remoting;

/// <summary>对端地址选择器，缓存可用性并排序输出候选地址</summary>
public class PeerEndpointSelector
{
    #region 属性
    /// <summary>地址状态集合</summary>
    public IList<EndpointState> Endpoints { get; } = [];

    /// <summary>失败屏蔽时间，秒。默认600秒</summary>
    public Int32 ShieldingSeconds { get; set; } = 600;

    /// <summary>探测结果缓存刷新间隔，秒。默认600秒</summary>
    public Int32 RefreshSeconds { get; set; } = 600;

    /// <summary>探测超时时间，毫秒。默认1000ms</summary>
    public Int32 ProbeTimeout { get; set; } = 1000;

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
            Endpoints.Clear();
            AddAddresses(internalAddresses, true);
            AddAddresses(externalAddresses, false);
        }
    }

    /// <summary>获取已排序的地址列表，必要时触发探测</summary>
    /// <param name="forceProbe">是否强制探测全部地址</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns>按优先级和RTT排序的地址状态列表</returns>
    public async Task<IReadOnlyList<EndpointState>> GetOrderedEndpointsAsync(Boolean forceProbe = false, CancellationToken cancellationToken = default)
    {
        List<EndpointState> snapshot;
        lock (_lock)
        {
            snapshot = Endpoints.ToList();
        }

        var now = DateTime.Now;
        var hasUsable = snapshot.Any(e => e.IsUp);
        var hasStale = snapshot.Any(ShouldProbe);

        if (forceProbe || (!hasUsable && hasStale))
        {
            await ProbeEndpointsAsync(snapshot, forceProbe, cancellationToken).ConfigureAwait(false);
            lock (_lock) snapshot = Endpoints.ToList();
        }
        else if (hasStale)
        {
            _ = Task.Run(() => ProbeEndpointsAsync(snapshot, false, CancellationToken.None));
        }

        snapshot = snapshot
           .OrderBy(e => GetPriority(e.Category))
           .ThenBy(e => e.Rtt ?? TimeSpan.MaxValue)
           .ThenBy(e => e.Failures)
           .ThenBy(e => e.Address.AbsoluteUri)
           .ToList();

        return snapshot.Any(e => e.IsUp) ? snapshot.Where(e => e.IsUp).ToList() : snapshot;
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
            var rtt = await ExecuteProbeAsync(state, cancellationToken).ConfigureAwait(false);
            if (rtt != null)
                MarkSuccess(state.Address, rtt.Value);
            else
                MarkFailure(state.Address, null);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task<TimeSpan?> ExecuteProbeAsync(EndpointState state, CancellationToken cancellationToken)
    {
        if (ProbeAsync != null) return await ProbeAsync(state.Address, cancellationToken).ConfigureAwait(false);

        var uri = new Uri(state.Address, ProbePath + "");
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
    /// <param name="address">地址</param>
    /// <param name="rtt">往返耗时</param>
    public void MarkSuccess(Uri address, TimeSpan rtt)
    {
        var state = FindOrCreate(address);
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
    /// <param name="address">地址</param>
    /// <param name="error">异常信息</param>
    public void MarkFailure(Uri address, Exception? error)
    {
        var state = FindOrCreate(address);
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
    private void AddAddresses(String? addresses, Boolean internalAddress)
    {
        if (addresses.IsNullOrWhiteSpace()) return;

        var ss = addresses.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var item in ss)
        {
            var url = item.Trim();
            if (url.IsNullOrEmpty()) continue;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) continue;

            var state = CreateEndpoint(uri, internalAddress);
            Endpoints.Add(state);
        }
    }

    private EndpointState FindOrCreate(Uri address)
    {
        lock (_lock)
        {
            var state = Endpoints.FirstOrDefault(e => Uri.Compare(e.Address, address, UriComponents.AbsoluteUri, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase) == 0);
            if (state != null) return state;

            state = CreateEndpoint(address, false);
            Endpoints.Add(state);
            return state;
        }
    }

    private static EndpointState CreateEndpoint(Uri uri, Boolean internalAddress)
    {
        var category = DetectCategory(uri, internalAddress);
        return new EndpointState
        {
            Address = uri,
            Category = category,
            IsUp = true,
            NextProbe = DateTime.MinValue
        };
    }

    private static EndpointCategory DetectCategory(Uri uri, Boolean internalAddress)
    {
        if (IPAddress.TryParse(uri.Host, out var ip))
        {
#if NETCOREAPP || NETSTANDARD2_1
            if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
#endif
            if (ip.AddressFamily == AddressFamily.InterNetwork) return internalAddress ? EndpointCategory.InternalIPv4 : EndpointCategory.ExternalIPv4;
            if (ip.AddressFamily == AddressFamily.InterNetworkV6) return internalAddress ? EndpointCategory.InternalIPv6 : EndpointCategory.ExternalIPv6;
        }

        return EndpointCategory.ExternalDomain;
    }

    private static Int32 GetPriority(EndpointCategory category) => category switch
    {
        EndpointCategory.InternalIPv4 => 0,
        EndpointCategory.InternalIPv6 => 1,
        EndpointCategory.ExternalIPv4 => 2,
        EndpointCategory.ExternalDomain => 3,
        EndpointCategory.ExternalIPv6 => 4,
        _ => 10
    };

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
        /// <summary>地址</summary>
        public Uri Address { get; set; } = null!;

        /// <summary>类别</summary>
        public EndpointCategory Category { get; set; }

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
    }
    #endregion
}
