using NewLife.Log;

namespace NewLife.Remoting;

/// <summary>负载均衡模式</summary>
public enum LoadBalanceMode
{
    /// <summary>故障转移。优先使用第一个节点，失败时切换到下一个，过一段时间自动切回主节点</summary>
    Failover = 0,

    /// <summary>加权轮询。按权重分配请求到多个节点，自动屏蔽不可用节点</summary>
    RoundRobin = 1,

    /// <summary>竞速调用。并行请求所有可用节点，选择最快返回的结果，取消其它请求</summary>
    Race = 2,
}

/// <summary>负载均衡器接口</summary>
/// <remarks>
/// 负责从服务列表中选择一个服务节点来处理请求，支持多种负载均衡策略：
/// <list type="bullet">
/// <item><description>故障转移（Failover）：优先使用主节点，失败时自动切换到备用节点</description></item>
/// <item><description>加权轮询（RoundRobin）：按权重分配请求到多个节点</description></item>
/// <item><description>竞速调用（Race）：并行请求多个节点，取最快响应</description></item>
/// </list>
/// </remarks>
public interface ILoadBalancer
{
    /// <summary>负载均衡模式</summary>
    LoadBalanceMode Mode { get; }

    /// <summary>不可用节点的屏蔽时间。默认60秒</summary>
    Int32 ShieldingTime { get; set; }

    /// <summary>获取一个服务用于处理请求</summary>
    /// <param name="services">服务列表</param>
    /// <returns>选中的服务节点</returns>
    ServiceEndpoint GetService(IList<ServiceEndpoint> services);

    /// <summary>归还服务，报告请求结果</summary>
    /// <param name="services">服务列表</param>
    /// <param name="service">归还的服务</param>
    /// <param name="error">异常信息，null表示成功</param>
    void PutService(IList<ServiceEndpoint> services, ServiceEndpoint service, Exception? error);
}

/// <summary>负载均衡器基类</summary>
public abstract class LoadBalancerBase : ILoadBalancer
{
    #region 属性
    /// <summary>负载均衡模式</summary>
    public abstract LoadBalanceMode Mode { get; }

    /// <summary>不可用节点的屏蔽时间。默认60秒</summary>
    public Int32 ShieldingTime { get; set; } = 60;

    /// <summary>日志</summary>
    public ILog Log { get; set; } = Logger.Null;
    #endregion

    #region 方法
    /// <summary>获取一个服务用于处理请求</summary>
    /// <param name="services">服务列表</param>
    /// <returns>选中的服务节点</returns>
    public abstract ServiceEndpoint GetService(IList<ServiceEndpoint> services);

    /// <summary>归还服务，报告请求结果</summary>
    /// <param name="services">服务列表</param>
    /// <param name="service">归还的服务</param>
    /// <param name="error">异常信息，null表示成功</param>
    public virtual void PutService(IList<ServiceEndpoint> services, ServiceEndpoint service, Exception? error)
    {
        // 每过一段时间，清空客户端，让它重建连接，更新域名缓存
        if (service.CreateTime.AddMinutes(10) < DateTime.Now) service.Client = null;

        //var ex = error;
        //while (ex is AggregateException age) ex = age.InnerException;

        // 标记失败
        if (error != null)
        {
            service.MarkFailure(ShieldingTime);

            Log?.Debug("服务节点[{0}]发生错误，屏蔽{1}秒", service.Name, ShieldingTime);
        }
    }

    /// <summary>确保有可用节点，如果全部屏蔽则重置</summary>
    /// <param name="services">服务列表</param>
    protected void EnsureAvailable(IList<ServiceEndpoint> services)
    {
        // 如果全部节点不可用，则启用全部节点，避免网络恢复后无法及时通信
        if (!services.Any(e => e.IsAvailable()))
        {
            foreach (var item in services)
            {
                item.Reset();
            }
            Log?.Debug("所有服务节点已屏蔽，重置全部节点");
        }
    }
    #endregion
}
