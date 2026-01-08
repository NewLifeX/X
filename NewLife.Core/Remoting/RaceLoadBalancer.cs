using System.Diagnostics;

namespace NewLife.Remoting;

/// <summary>竞速负载均衡器</summary>
/// <remarks>
/// 竞速策略：
/// <list type="bullet">
/// <item><description>并行请求所有可用服务节点</description></item>
/// <item><description>选择最快成功返回的结果</description></item>
/// <item><description>取消其它未完成的请求</description></item>
/// <item><description>根据历史响应时间动态调整启动延迟</description></item>
/// </list>
/// 适用场景：对响应时间要求极高的场景，或部分节点可能有延迟的情况
/// </remarks>
public class RaceLoadBalancer : LoadBalancerBase
{
    #region 属性
    /// <summary>负载均衡模式</summary>
    public override LoadBalanceMode Mode => LoadBalanceMode.Race;

    /// <summary>RTT刷新间隔，秒。默认600秒</summary>
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

    #region 方法
    /// <summary>获取一个服务用于处理请求</summary>
    /// <param name="services">服务列表</param>
    /// <returns>选中的服务节点</returns>
    /// <remarks>竞速模式下，此方法返回第一个可用节点，实际竞速在GetAllServices中实现</remarks>
    public override ServiceEndpoint GetService(IList<ServiceEndpoint> services)
    {
        if (services == null || services.Count == 0)
            throw new InvalidOperationException("No available service nodes!");

        EnsureAvailable(services);

        // 返回第一个可用节点作为默认选择
        foreach (var svc in services)
        {
            if (svc.IsAvailable())
            {
                svc.Times++;
                return svc;
            }
        }

        // 全部不可用时返回第一个
        var first = services[0];
        first.Times++;
        return first;
    }

    /// <summary>获取所有可用服务用于竞速调用，按优先级和RTT排序</summary>
    /// <param name="services">服务列表</param>
    /// <param name="forceProbe">是否强制探测全部地址</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns>已排序的可用服务列表，Score字段表示启动延迟（毫秒）</returns>
    public async Task<IList<ServiceEndpoint>> GetAllServicesAsync(IList<ServiceEndpoint> services, Boolean forceProbe, CancellationToken cancellationToken)
    {
        EnsureAvailable(services);

        var available = services.Where(e => e.IsAvailable()).ToList();
        if (available.Count == 0) return [];

        var hasUsable = available.Any(e => e.IsAvailable());
        var hasStale = available.Any(ShouldProbe);

        // 强制探测，或没有可用节点但有RTT过期的节点，同步探测
        if (forceProbe || (!hasUsable && hasStale))
        {
            await ProbeEndpointsAsync(available, forceProbe, cancellationToken).ConfigureAwait(false);
            available = services.Where(e => e.IsAvailable()).ToList();
        }
        // 有可用但RTT过期的节点，异步探测
        else if (hasStale)
        {
            _ = Task.Run(() => ProbeEndpointsAsync(available, false, CancellationToken.None), cancellationToken);
        }

        // 按优先级和RTT排序
        var sorted = available
            .OrderBy(e => (Int32)e.Category)
            .ThenBy(e => e.Rtt ?? TimeSpan.MaxValue)
            .ThenBy(e => e.Errors)
            .ThenBy(e => e.Address.AbsoluteUri)
            .ToList();

        // 计算延迟分数
        for (var i = 0; i < sorted.Count; i++)
        {
            sorted[i].Score = i * 100;
        }

        return sorted;
    }

    /// <summary>标记服务成功，更新RTT</summary>
    /// <param name="service">服务节点</param>
    /// <param name="elapsed">耗时</param>
    public void MarkSuccess(ServiceEndpoint service, TimeSpan elapsed)
    {
        if (service == null) return;

        lock (_lock)
        {
            service.LastSuccess = DateTime.Now;
            service.Errors = 0;
            service.NextProbe = DateTime.Now.AddSeconds(RefreshSeconds);

            // RTT平滑计算：新RTT = 旧RTT * 0.75 + 新值 * 0.25
            service.Rtt = service.Rtt == null
                ? elapsed
                : TimeSpan.FromMilliseconds((service.Rtt.Value.TotalMilliseconds * 3 + elapsed.TotalMilliseconds) / 4);
        }
    }

    /// <summary>标记服务失败</summary>
    /// <param name="service">服务节点</param>
    /// <param name="error">异常</param>
    public void MarkFailure(ServiceEndpoint service, Exception? error)
    {
        if (service == null) return;

        lock (_lock)
        {
            service.LastFailure = DateTime.Now;
            service.Errors++;
            service.Rtt = null;
            service.NextProbe = DateTime.Now.AddSeconds(ShieldingTime);
        }
    }
    #endregion

    #region 探测
    private static Boolean ShouldProbe(ServiceEndpoint service) => service.NextProbe <= DateTime.Now;

    private async Task ProbeEndpointsAsync(IEnumerable<ServiceEndpoint> services, Boolean forceProbe, CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();
        using var semaphore = new SemaphoreSlim(MaxProbeConcurrency > 0 ? MaxProbeConcurrency : 1);

        foreach (var service in services)
        {
            if (forceProbe || ShouldProbe(service))
                tasks.Add(ProbeOneAsync(service, semaphore, cancellationToken));
        }

        if (tasks.Count > 0) await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task ProbeOneAsync(ServiceEndpoint service, SemaphoreSlim semaphore, CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var uri = new Uri(service.Address, ProbePath + "");
            var func = ProbeAsync ?? ExecuteProbeAsync;
            var rtt = await func(uri, cancellationToken).ConfigureAwait(false);
            if (rtt != null)
                MarkSuccess(service, rtt.Value);
            else
                MarkFailure(service, null);
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
    #endregion
}
