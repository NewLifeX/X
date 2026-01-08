namespace NewLife.Remoting;

/// <summary>加权轮询负载均衡器</summary>
/// <remarks>
/// 加权轮询策略：
/// <list type="bullet">
/// <item><description>按权重分配请求到多个节点，权重越高分配越多</description></item>
/// <item><description>自动屏蔽不可用节点，屏蔽时间过后恢复</description></item>
/// <item><description>支持动态调整权重</description></item>
/// </list>
/// 适用场景：多服务器负载分担，需要按服务器性能分配流量
/// </remarks>
public class WeightedRoundRobinLoadBalancer : LoadBalancerBase
{
    #region 属性
    /// <summary>负载均衡模式</summary>
    public override LoadBalanceMode Mode => LoadBalanceMode.RoundRobin;

    /// <summary>调度索引，当前使用该索引处的服务</summary>
    private volatile Int32 _serverIndex;
    #endregion

    #region 方法
    /// <summary>获取一个服务用于处理请求</summary>
    /// <param name="services">服务列表</param>
    /// <returns>选中的服务节点</returns>
    public override ServiceEndpoint GetService(IList<ServiceEndpoint> services)
    {
        if (services == null || services.Count == 0)
            throw new InvalidOperationException("No available service nodes!");

        EnsureAvailable(services);

        // 加权轮询选择节点
        ServiceEndpoint? svc = null;
        for (var i = 0; i < services.Count; i++)
        {
            svc = services[_serverIndex % services.Count];

            // 权重足够，又没有被屏蔽，就是它了
            // Weight <= 0 表示无限制
            // Index < Weight 表示本轮还有配额
            var hasQuota = svc.Weight <= 0 || svc.Index < svc.Weight || services.Count == 1;
            var isAvailable = svc.IsAvailable();

            if (hasQuota && isAvailable) break;

            // 这个节点用完了配额或被屏蔽，切换下一个
            svc.Index = 0;
            svc = null;
            _serverIndex++;
        }

        // 如果都没有可用节点，默认选第一个
        if (svc == null && services.Count > 0) svc = services[0];
        if (svc == null) throw new InvalidOperationException("No available service nodes!");

        svc.Times++;

        // 计算下一次节点
        svc.Index++;
        if (svc.Index >= svc.Weight && svc.Weight > 0)
        {
            svc.Index = 0;
            _serverIndex++;
        }

        // 防止索引溢出
        if (_serverIndex >= services.Count) _serverIndex = 0;

        return svc;
    }

    /// <summary>归还服务，报告请求结果</summary>
    /// <param name="services">服务列表</param>
    /// <param name="service">归还的服务</param>
    /// <param name="error">异常信息，null表示成功</param>
    public override void PutService(IList<ServiceEndpoint> services, ServiceEndpoint service, Exception? error)
    {
        base.PutService(services, service, error);

        var ex = error;
        while (ex is AggregateException age) ex = age.InnerException;

        // 网络异常时，跳过当前节点继续轮询
        if (ex is HttpRequestException or TaskCanceledException)
        {
            _serverIndex++;
            Log?.Debug("服务节点[{0}]网络异常，跳过该节点", service.Name);
        }
    }
    #endregion
}
