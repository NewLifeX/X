---
name: dependency-injection
description: 使用 NewLife ObjectContainer 实现轻量级依赖注入和应用主机托管
---

# NewLife 依赖注入与应用主机使用指南

## 适用场景

- 轻量级 IoC/DI 容器（替代 Microsoft.Extensions.DependencyInjection）
- 后台服务托管（Windows 服务 / Linux Systemd）
- 插件管理和模块化开发
- Actor 并发模型

## ObjectContainer（DI 容器）

### 注册服务

```csharp
var container = ObjectContainer.Current;

// 单例
container.AddSingleton<ICache, MemoryCache>();
container.AddSingleton<ICache>(MemoryCache.Instance);
container.AddSingleton<ICache>(sp => new MemoryCache { Capacity = 50000 });

// 作用域
container.AddScoped<IUserService, UserService>();

// 瞬态
container.AddTransient<IOrderService, OrderService>();

// TryAdd 版本（已存在则跳过）
container.TryAddSingleton<ICache, MemoryCache>();
```

### 解析服务

```csharp
// 通过 IServiceProvider
var provider = ObjectContainer.Provider;
var cache = provider.GetService<ICache>();
var service = provider.GetRequiredService<IUserService>();

// 全局静态访问
var cache = ObjectContainer.Provider.GetService<ICache>();
```

### 与 .NET Generic Host 集成

```csharp
var builder = Host.CreateDefaultBuilder(args);
builder.ConfigureServices(services =>
{
    // NewLife 的 ObjectContainer 可与 Microsoft DI 共存
    var container = ObjectContainer.Current;
    container.AddSingleton<ICache, MemoryCache>();

    // 将 NewLife 注册桥接到 Microsoft DI
    ObjectContainer.SetInnerProvider(services.BuildServiceProvider());
});
```

## Host（轻量级应用主机）

### 基本用法

```csharp
// 创建服务容器
var services = ObjectContainer.Current;

// 注册后台服务
services.AddSingleton<ICache, MemoryCache>();
services.AddHostedService<MyBackgroundService>();

// 创建并运行主机
var host = services.BuildHost();
host.Run();  // 阻塞运行，Ctrl+C 优雅退出
```

### 后台服务

```csharp
public class DataSyncService : IHostedService
{
    private readonly ICache _cache;
    private TimerX? _timer;

    public DataSyncService(ICache cache) => _cache = cache;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new TimerX(DoSync, null, 1000, 60_000);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer.TryDispose();
        return Task.CompletedTask;
    }

    private void DoSync(Object? state)
    {
        // 定时同步逻辑
    }
}
```

## 插件框架

### 定义插件

```csharp
public class MyPlugin : IPlugin
{
    public void Add(IObjectContainer container)
    {
        // 注册插件提供的服务
        container.AddSingleton<IMyService, MyServiceImpl>();
    }

    public void Use(IHost host)
    {
        // 使用主机服务
        var svc = host.Services.GetService<IMyService>();
    }
}
```

### 加载插件

```csharp
// 扫描程序集中的插件
var manager = new PluginManager();
manager.LoadPlugins("Plugins", "*.dll");
manager.Init(container);
```

## Actor 并发模型

```csharp
public class OrderActor : Actor
{
    protected override async Task ReceiveAsync(ActorContext context)
    {
        switch (context.Message)
        {
            case CreateOrderCommand cmd:
                await ProcessCreateOrder(cmd);
                break;
            case CancelOrderCommand cmd:
                await ProcessCancelOrder(cmd);
                break;
        }
    }
}

// 使用
var actor = new OrderActor { Name = "OrderProcessor", Tracer = tracer };
await actor.Start();

// 发送消息（异步处理，线程安全）
actor.Tell(new CreateOrderCommand { UserId = 1, Amount = 100 });
actor.Tell(new CancelOrderCommand { OrderId = 123 });
```

## 注意事项

- `ObjectContainer.Current` 是全局单例容器
- `ObjectContainer.Provider` 是全局 `IServiceProvider`
- 注册顺序：先注册基础服务，再注册依赖它们的服务
- `IHostedService` 兼容 .NET Generic Host 接口
- Actor 的 `ReceiveAsync` 保证**单线程顺序执行**，无需加锁
- `BoundedCapacity` 限制 Actor 消息队列上限，满时 `Tell` 返回 0
