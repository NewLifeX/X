# 负载均衡与故障转移LoadBalancer

## 概述

`NewLife.Remoting` 提供三种内置负载均衡策略，用于在多个 HTTP 服务节点之间分配请求，并在节点故障时自动屏蔽和恢复，保证业务高可用。每种策略均实现 `ILoadBalancer` 接口，可配合 `ApiHttpClient` 使用，也可独立使用。

**命名空间**：`NewLife.Remoting`  
**文档地址**：/core/load_balancer

## 三种策略对比

| 策略 | 类型 | 适用场景 |
|------|------|---------|
| 故障转移 | `FailoverLoadBalancer` | 主备切换，保证高可用；正常只用主节点 |
| 加权轮询 | `WeightedRoundRobinLoadBalancer` | 多实例负载分担，按性能配比流量 |
| 竞速调用 | `RaceLoadBalancer` | 极低延迟要求，容忍较高带宽消耗 |

## 服务节点 ServiceEndpoint

节点信息承载在 `ServiceEndpoint`，包含地址、权重、健康状态等：

```csharp
public class ServiceEndpoint
{
    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary>服务地址</summary>
    public Uri Address { get; set; }

    /// <summary>访问令牌</summary>
    public String? Token { get; set; }

    /// <summary>权重。用于加权轮询，默认 1，值越大分配请求越多</summary>
    public Int32 Weight { get; set; } = 1;

    /// <summary>总请求次数</summary>
    public Int32 Times { get; }

    /// <summary>错误次数</summary>
    public Int32 Errors { get; }
}
```

## 接口定义

```csharp
public interface ILoadBalancer
{
    /// <summary>负载均衡模式</summary>
    LoadBalanceMode Mode { get; }

    /// <summary>不可用节点的屏蔽时间（秒），默认 60</summary>
    Int32 ShieldingTime { get; set; }

    /// <summary>获取一个服务节点处理请求</summary>
    ServiceEndpoint GetService(IList<ServiceEndpoint> services);

    /// <summary>归还节点并报告结果（error=null 表示成功）</summary>
    void PutService(IList<ServiceEndpoint> services, ServiceEndpoint service, Exception? error);
}
```

## 快速开始

### 故障转移

```csharp
using NewLife.Remoting;

var services = new List<ServiceEndpoint>
{
    new ServiceEndpoint("primary", new Uri("http://10.0.0.1:8080")),
    new ServiceEndpoint("backup1", new Uri("http://10.0.0.2:8080")),
    new ServiceEndpoint("backup2", new Uri("http://10.0.0.3:8080")),
};

var lb = new FailoverLoadBalancer { ShieldingTime = 30 };

var svc = lb.GetService(services);
try
{
    var result = await CallAsync(svc);
    lb.PutService(services, svc, null);   // 成功：null
}
catch (Exception ex)
{
    lb.PutService(services, svc, ex);     // 失败：传异常，自动切换到下一节点
}
```

### 加权轮询

```csharp
var services = new List<ServiceEndpoint>
{
    new ServiceEndpoint("s1", new Uri("http://10.0.0.1:8080")) { Weight = 3 },  // 60%
    new ServiceEndpoint("s2", new Uri("http://10.0.0.2:8080")) { Weight = 2 },  // 40%
};

var lb = new WeightedRoundRobinLoadBalancer();
// 使用方式与故障转移相同
```

## API 参考

### FailoverLoadBalancer（故障转移）

```csharp
public class FailoverLoadBalancer : LoadBalancerBase
{
    public override LoadBalanceMode Mode => LoadBalanceMode.Failover;
}
```

**行为**：

1. 优先使用 `services[0]`（主节点）
2. 主节点出现 `HttpRequestException` 或 `TaskCanceledException` 时，自动切换到 `services[1]`
3. 节点被屏蔽 `ShieldingTime` 秒后，下次 `GetService` 时尝试切回主节点（若主节点恢复可用）
4. 全部节点不可用时自动重置所有节点，重新尝试

### WeightedRoundRobinLoadBalancer（加权轮询）

```csharp
public class WeightedRoundRobinLoadBalancer : LoadBalancerBase
{
    public override LoadBalanceMode Mode => LoadBalanceMode.RoundRobin;
}
```

**行为**：

1. 按 `Weight` 属性分配请求比例
2. 每个节点在轮次内用完配额（`Index >= Weight`）后自动切换到下一个
3. 网络异常时跳过该节点继续轮询
4. 全部节点不可用时重置所有节点

### RaceLoadBalancer（竞速调用）

并行请求多个节点，返回最快响应并取消其他请求：

```csharp
public class RaceLoadBalancer : LoadBalancerBase
{
    public override LoadBalanceMode Mode => LoadBalanceMode.Race;
}
```

**行为**：

1. 并行向所有可用节点发起请求
2. 返回最先成功的响应，取消其余未完成请求
3. 按 `Rtt`（往返延迟）和 `Category`（网络类型）排序偏好低延迟节点
4. 需要使用方（`ApiHttpClient.Race`）配合驱动，不单独调用 `GetService`

## 与 ApiHttpClient 集成

```csharp
var client = new ApiHttpClient("http://10.0.0.1:8080,http://10.0.0.2:8080")
{
    Mode = LoadBalanceMode.RoundRobin,  // 或 Failover / Race
};
client.ShieldingTime = 30;

var result = await client.GetAsync<String>("api/version");
```

## 使用场景

### 场景一：主备数据库网关

```csharp
var services = new List<ServiceEndpoint>
{
    new ServiceEndpoint("写库",   new Uri("http://db1:8080")) { Weight = 1 },
    new ServiceEndpoint("只读副本1", new Uri("http://db2:8080")) { Weight = 1 },
};

var lb = new FailoverLoadBalancer { ShieldingTime = 60 };

// 写操作使用故障转移
await ExecuteWithLb(lb, services, node => dbClient.WriteAsync(node.Address, data));
```

### 场景二：多机房同服务

```csharp
var services = new List<ServiceEndpoint>
{
    // 本机房多实例，按性能分配
    new ServiceEndpoint("node1", new Uri("http://192.168.1.10:8080")) { Weight = 4 },
    new ServiceEndpoint("node2", new Uri("http://192.168.1.11:8080")) { Weight = 2 },
    new ServiceEndpoint("node3", new Uri("http://192.168.1.12:8080")) { Weight = 1 },
};

var lb = new WeightedRoundRobinLoadBalancer();
```

### 辅助方法封装

```csharp
public static async Task<T> ExecuteWithLb<T>(
    ILoadBalancer lb,
    IList<ServiceEndpoint> services,
    Func<ServiceEndpoint, Task<T>> action)
{
    var svc = lb.GetService(services);
    try
    {
        var result = await action(svc);
        lb.PutService(services, svc, null);
        return result;
    }
    catch (Exception ex)
    {
        lb.PutService(services, svc, ex);
        throw;
    }
}
```

## 最佳实践

- **`ShieldingTime` 按恢复时间设置**：通常 30~60 秒，过短会频繁切换，过长影响主节点恢复后的及时切回。
- **`Log` 属性赋值**：设置 `lb.Log = XTrace.Log` 以记录节点切换日志，便于排查。
- **全节点不可用时有告警**：`EnsureAvailable` 重置全部节点前会静默尝试，生产环境应在 `PutService` 后检查错误率并触发告警。
- **竞速策略慎用于外网**：并行请求会倍增带宽消耗，仅在延迟要求极严苛（< 50ms P99）时使用。
- **节点健康可从 `Times/Errors` 读取**：用于监控大盘展示当前各节点状态。
