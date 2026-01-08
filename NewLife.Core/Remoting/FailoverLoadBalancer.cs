namespace NewLife.Remoting;

/// <summary>故障转移负载均衡器</summary>
/// <remarks>
/// 故障转移策略：
/// <list type="bullet">
/// <item><description>优先使用第一个（主）节点</description></item>
/// <item><description>主节点失败时自动切换到下一个备用节点</description></item>
/// <item><description>不可用节点被屏蔽一段时间（ShieldingTime）</description></item>
/// <item><description>屏蔽时间过后自动尝试切回主节点</description></item>
/// </list>
/// 适用场景：需要高可用保障的业务场景，确保业务连续性
/// </remarks>
public class FailoverLoadBalancer : LoadBalancerBase
{
    #region 属性
    /// <summary>负载均衡模式</summary>
    public override LoadBalanceMode Mode => LoadBalanceMode.Failover;

    /// <summary>当前服务索引</summary>
    private volatile Int32 _currentIndex;
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

        // 优先尝试切回主节点
        var idx = _currentIndex;
        if (idx > 0 && services[0].IsAvailable())
        {
            idx = _currentIndex = 0;
            Log?.Debug("主节点[{0}]恢复可用，切回主节点", services[0].Name);
        }

        // 获取当前索引对应的服务
        var svc = services[idx % services.Count];
        svc.Times++;

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

        // 网络异常时，自动切换到下一个节点
        if (ex is HttpRequestException or TaskCanceledException)
        {
            var nextIndex = _currentIndex + 1;
            if (nextIndex < services.Count)
            {
                _currentIndex = nextIndex;
                Log?.Debug("服务节点[{0}]网络异常，切换到节点[{1}]，使用地址：{2}", service.Name, services[nextIndex].Name, services[nextIndex].Address);
            }
        }
    }
    #endregion
}
