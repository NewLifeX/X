# 服务解析器 IServiceResolver

## 概述

`IServiceResolver` 是 NewLife 体系中的**服务发现抽象接口**，定义在 `NewLife.Core` 核心库中。它允许业务代码通过服务名获取通信客户端（`IApiClient`），无需关心服务地址的来源和管理方式。

核心价值：**类库项目仅引用 NewLife.Core 即可通过 DI 获取该接口，由上层应用提供具体实现**。

## 三层架构

```
┌─────────────────────────────────────────────────────┐
│  Stardust (星尘)                                     │
│  AppClient : IRegistry : IServiceResolver            │
│  · 从注册中心动态发现服务地址                          │
│  · 支持权重、多地址、自动更新                          │
│  · 服务变更时自动通知并更新客户端地址                    │
├─────────────────────────────────────────────────────┤
│  NewLife.Remoting                                    │
│  RemotingServiceResolver : ConfigServiceResolver     │
│  · 扩展支持 tcp/udp/ws/wss 长连接协议                 │
│  · tcp/udp → ApiClient (SRMP 长连接)                 │
│  · ws/wss → WsClient (WebSocket 长连接)              │
├─────────────────────────────────────────────────────┤
│  NewLife.Core                                        │
│  IServiceResolver (接口)                              │
│  ConfigServiceResolver (默认实现)                     │
│  · 从配置文件/配置中心读取地址                         │
│  · 仅支持 http/https 协议                            │
│  · 轮询负载均衡 (RoundRobin)                          │
└─────────────────────────────────────────────────────┘
```

## 接口定义

```csharp
public interface IServiceResolver
{
    /// <summary>为指定服务获取托管客户端，内置负载均衡、故障转移、自动更新</summary>
    Task<IApiClient> GetClientAsync(String serviceName, String? tag = null);

    /// <summary>解析服务地址列表，供调用方自建客户端</summary>
    Task<String[]> ResolveAddressesAsync(String serviceName, String? tag = null);
}
```

### 两个方法的选择

| 方法 | 适用场景 |
|------|---------|
| `GetClientAsync` | **推荐**。返回托管客户端，内置 RoundRobin/Failover/Race、故障屏蔽、自动更新地址列表 |
| `ResolveAddressesAsync` | 需要**自定义协议**（如 gRPC、自研 RPC）或需要自行控制负载均衡策略时使用 |

## ConfigServiceResolver 默认实现

### 地址解析优先级

1. **`Servers` 属性** — 直接指定地址，优先级最高
2. **DI 中的 `IConfigProvider`** — 从配置中心读取
3. **本地 `appsettings.json`** — 兜底方案

### 构造函数

```csharp
// 方式一：通过 DI 容器（推荐，可自动解析 IConfigProvider、ITracer、ILog）
var resolver = new ConfigServiceResolver(serviceProvider);

// 方式二：直接指定配置提供者（适用于不使用 DI 的场景）
var resolver = new ConfigServiceResolver(configProvider);
```

### 基本用法

```csharp
// 1. 通过 DI 注册
services.AddSingleton<IServiceResolver>(sp => new ConfigServiceResolver(sp)
{
    Servers = "http://api.example.com:8080"
});

// 2. 在业务代码中使用
public class OrderService
{
    private readonly IServiceResolver _resolver;

    public OrderService(IServiceResolver resolver) => _resolver = resolver;

    public async Task<Order?> GetOrderAsync(Int32 id)
    {
        var client = await _resolver.GetClientAsync("OrderApi");
        return await client.InvokeAsync<Order>("Get", new { id });
    }
}
```

### 配置文件方式

在 `appsettings.json` 中按服务名配置地址：

```json
{
    "OrderApi": "http://192.168.1.100:8080,http://192.168.1.101:8080",
    "UserApi": "https://user-api.example.com"
}
```

多地址用逗号或分号分隔，自动使用轮询负载均衡（RoundRobin）。

### 配置动态更新

当地址来自 `IConfigProvider`（非 `Servers` 属性）时，`ConfigServiceResolver` 会自动将客户端绑定到配置提供者。配置中服务名对应的地址发生变更时，客户端服务列表随之自动更新，无需重启应用。

```csharp
// ✅ 地址来自 IConfigProvider：支持动态更新
var resolver = new ConfigServiceResolver(configProvider);
var client = await resolver.GetClientAsync("OrderApi");
// 修改 configProvider["OrderApi"] 后，client 会自动使用新地址

// ⚠️ Servers 属性：静态指定，不监听配置变更
var resolver2 = new ConfigServiceResolver(serviceProvider)
{
    Servers = "http://192.168.1.100:8080"  // 固定地址，不会随配置更新
};
```

> **注意**：`Servers` 属性优先级最高，设置后不再绑定 `IConfigProvider`。如需动态更新，应将地址配置在 `IConfigProvider` 中，而非直接赋値 `Servers`。

### 特性标签（Tag）

同一服务可通过 `tag` 区分不同的环境或分组，不同 tag 创建独立的客户端实例：

```csharp
var devClient = await resolver.GetClientAsync("OrderApi", "dev");
var prodClient = await resolver.GetClientAsync("OrderApi", "prod");
// devClient 和 prodClient 是不同的实例
```

### 自建客户端（ResolveAddressesAsync）

当需要自定义协议（gRPC、自研 RPC 等）或自行控制负载均衡时，用 `ResolveAddressesAsync` 获取地址列表：

```csharp
// 获取地址列表，自行创建 gRPC 客户端
var addresses = await resolver.ResolveAddressesAsync("PaymentService");
foreach (var addr in addresses)
{
    // addr → "http://192.168.1.100:8080"
    var channel = GrpcChannel.ForAddress(addr);
}

// 自行随机选一个地址
var addr = addresses[Random.Shared.Next(addresses.Length)];
```

## RemotingServiceResolver 多协议支持

NewLife.Remoting 提供的扩展实现，在 `ConfigServiceResolver` 基础上增加长连接协议支持：

| 协议 | 客户端类型 | 连接方式 |
|------|-----------|---------|
| http/https | ApiHttpClient | HTTP 短连接 |
| tcp/udp | ApiClient | SRMP 长连接 |
| ws/wss | WsClient | WebSocket 长连接 |

协议按首个地址自动识别，同一服务的多个地址协议应保持一致。

```csharp
// 引用 NewLife.Remoting 后使用
var resolver = new RemotingServiceResolver(configProvider);
var client = await resolver.GetClientAsync("RpcService"); // tcp://... → ApiClient
```

## Stardust 完整实现

在星尘（Stardust）中，`AppClient` 实现了 `IRegistry`（继承自 `IServiceResolver`），提供完整的服务发现能力：

- 从星尘注册中心获取服务地址（支持多实例、权重）
- 服务提供方变更时**自动更新**客户端地址列表
- 支持服务注册、取消注册
- 本地缓存服务地址，注册中心不可用时降级使用缓存

```csharp
// Stardust 中的使用方式
var star = new StarFactory(server, appId, secret);
var client = await star.Service.GetClientAsync("OrderApi");
```

## 扩展自定义实现

继承 `ConfigServiceResolver` 并重写 `BuildClient` 方法即可支持自定义协议：

```csharp
public class MyServiceResolver : ConfigServiceResolver
{
    public MyServiceResolver(IServiceProvider sp) : base(sp) { }

    protected override IApiClient BuildClient(String name, String address)
    {
        if (address.StartsWith("grpc://"))
            return new MyGrpcClient(address);

        return base.BuildClient(name, address);
    }
}
```

## 设计要点

| 要点 | 说明 |
|------|------|
| **实例缓存** | 同名服务（含 tag）复用同一 IApiClient，避免重复创建连接 |
| **配置动态更新** | 地址来自 IConfigProvider 时自动绑定，配置变更即刻生效；Servers 属性为静态，不参与绑定 |
| **负载均衡** | 默认使用 RoundRobin 轮询模式，多地址自动分发请求 |
| **可释放** | 继承 DisposeBase，Dispose 时释放所有缓存的客户端 |
| **线程安全** | 使用 ConcurrentDictionary 保证并发安全 |
| **协议校验** | ConfigServiceResolver 仅允许 http/https，子类可扩展 |
| **渐进式架构** | 最简单的场景用配置文件，复杂场景用注册中心，业务代码无需修改 |

## 相关类型

- `IApiClient` — 应用接口客户端接口（Invoke/InvokeAsync）
- `ApiHttpClient` — HTTP 客户端实现，支持负载均衡和故障转移
- `IConfigProvider` — 配置提供者接口
- `IRegistry` — 服务注册客户端接口（Stardust 中定义，继承 IServiceResolver）
