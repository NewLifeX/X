# 轻量级应用主机 Host

## 概述

`Host` 是 NewLife.Core 中的轻量级应用主机，提供应用程序生命周期管理功能。支持托管多个后台服务，自动处理启动、停止、优雅退出等场景，特别适合控制台应用、后台服务、微服务等场景。

**命名空间**：`NewLife.Model`  
**文档地址**：https://newlifex.com/core/host

## 核心特性

- **服务托管**：支持注册和管理多个 `IHostedService` 服务
- **生命周期管理**：自动处理启动、停止、异常回滚
- **优雅退出**：响应 Ctrl+C、SIGINT、SIGTERM 等系统信号
- **跨平台**：支持 Windows、Linux、macOS
- **依赖注入**：与 `ObjectContainer` 深度集成
- **超时控制**：支持设置最大运行时间

## 快速开始

```csharp
using NewLife.Model;

// 创建主机
var host = new Host(ObjectContainer.Provider);

// 添加后台服务
host.Add<MyBackgroundService>();
host.Add<AnotherService>();

// 运行（阻塞直到收到退出信号）
host.Run();
```

## API 参考

### IHostedService 接口

后台服务必须实现此接口：

```csharp
public interface IHostedService
{
    /// <summary>开始服务</summary>
    Task StartAsync(CancellationToken cancellationToken);
    
    /// <summary>停止服务</summary>
    Task StopAsync(CancellationToken cancellationToken);
}
```

**实现示例**：
```csharp
public class MyBackgroundService : IHostedService
{
    private CancellationTokenSource? _cts;
    private Task? _task;
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        XTrace.WriteLine("MyBackgroundService 启动");
        
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _task = ExecuteAsync(_cts.Token);
        
        return Task.CompletedTask;
    }
    
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        XTrace.WriteLine("MyBackgroundService 停止");
        
        _cts?.Cancel();
        
        if (_task != null)
            await Task.WhenAny(_task, Task.Delay(5000, cancellationToken));
    }
    
    private async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // 执行后台任务
            XTrace.WriteLine("后台任务执行中...");
            await Task.Delay(1000, stoppingToken);
        }
    }
}
```

### Host 类

#### 构造函数

```csharp
public Host(IServiceProvider serviceProvider)
```

通过服务提供者创建主机实例。

**示例**：
```csharp
// 使用全局容器
var host = new Host(ObjectContainer.Provider);

// 使用自定义容器
var ioc = new ObjectContainer();
ioc.AddSingleton<ILogger, ConsoleLogger>();
var host = new Host(ioc.BuildServiceProvider());
```

#### Add - 添加服务

```csharp
// 添加服务类型
void Add<TService>() where TService : class, IHostedService

// 添加服务实例
void Add(IHostedService service)
```

**示例**：
```csharp
var host = new Host(ObjectContainer.Provider);

// 通过类型添加
host.Add<MyBackgroundService>();
host.Add<DataSyncService>();

// 通过实例添加
var service = new CustomService(config);
host.Add(service);
```

#### Run / RunAsync - 运行主机

```csharp
// 同步运行（阻塞）
void Run()

// 异步运行
Task RunAsync()
```

运行主机，启动所有服务，然后阻塞等待退出信号。

**示例**：
```csharp
// 同步运行
host.Run();

// 异步运行
await host.RunAsync();

// 异步运行后继续其他操作
_ = host.RunAsync();
// 其他代码...
```

#### StartAsync / StopAsync

```csharp
Task StartAsync(CancellationToken cancellationToken)
Task StopAsync(CancellationToken cancellationToken)
```

手动控制启动和停止。

**示例**：
```csharp
using var cts = new CancellationTokenSource();

// 启动服务
await host.StartAsync(cts.Token);

// 做一些工作...
await Task.Delay(10000);

// 手动停止
await host.StopAsync(cts.Token);
```

#### Close - 关闭主机

```csharp
void Close(String? reason)
```

主动关闭主机，触发停止流程。

**示例**：
```csharp
// 某个条件触发关闭
if (shouldShutdown)
{
    host.Close("条件触发关闭");
}
```

#### MaxTime 属性

```csharp
public Int32 MaxTime { get; set; } = -1;
```

最大执行时间（毫秒）。默认 -1 表示永久运行。

**示例**：
```csharp
var host = new Host(ObjectContainer.Provider);
host.MaxTime = 60_000;  // 最多运行60秒
host.Add<MyService>();
host.Run();  // 60秒后自动停止
```

### 静态方法

#### RegisterExit - 注册退出事件

```csharp
// 可能被多次调用
static void RegisterExit(EventHandler onExit)

// 仅执行一次
static void RegisterExit(Action onExit)
```

注册应用退出时的回调函数。

**示例**：
```csharp
// 注册退出清理函数
Host.RegisterExit(() =>
{
    XTrace.WriteLine("应用正在退出，执行清理...");
    CleanupResources();
});

// 带参数的回调
Host.RegisterExit((sender, e) =>
{
    XTrace.WriteLine($"收到退出信号: {sender}");
});
```

## 容器集成

### AddHostedService 扩展方法

```csharp
// 通过类型注册
IObjectContainer AddHostedService<THostedService>()

// 通过工厂注册
IObjectContainer AddHostedService<THostedService>(
    Func<IServiceProvider, THostedService> factory)
```

**示例**：
```csharp
var ioc = ObjectContainer.Current;

// 注册后台服务
ioc.AddHostedService<MyBackgroundService>();
ioc.AddHostedService<DataSyncService>();

// 使用工厂
ioc.AddHostedService(sp =>
{
    var config = sp.GetRequiredService<AppConfig>();
    return new ConfigurableService(config);
});

// 创建主机并运行
var host = new Host(ioc.BuildServiceProvider());
host.Run();
```

## 使用场景

### 1. 简单后台服务

```csharp
class Program
{
    static void Main()
    {
        var ioc = ObjectContainer.Current;
        ioc.AddHostedService<WorkerService>();
        
        var host = new Host(ioc.BuildServiceProvider());
        host.Run();
    }
}

public class WorkerService : IHostedService
{
    private Timer? _timer;
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(DoWork, null, 0, 5000);
        return Task.CompletedTask;
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Dispose();
        return Task.CompletedTask;
    }
    
    private void DoWork(Object? state)
    {
        XTrace.WriteLine($"工作中... {DateTime.Now}");
    }
}
```

### 2. 多服务协作

```csharp
class Program
{
    static void Main()
    {
        var ioc = ObjectContainer.Current;
        
        // 注册共享依赖
        ioc.AddSingleton<IMessageQueue, RedisMessageQueue>();
        ioc.AddSingleton<ILogger, FileLogger>();
        
        // 注册多个后台服务
        ioc.AddHostedService<MessageConsumerService>();
        ioc.AddHostedService<HealthCheckService>();
        ioc.AddHostedService<MetricsCollectorService>();
        
        var host = new Host(ioc.BuildServiceProvider());
        host.Run();
    }
}
```

### 3. 定时任务服务

```csharp
public class ScheduledTaskService : IHostedService
{
    private readonly ILogger _logger;
    private TimerX? _timer;
    
    public ScheduledTaskService(ILogger logger)
    {
        _logger = logger;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // 每天凌晨2点执行
        _timer = new TimerX(ExecuteTask, null, "0 0 2 * * *");
        _logger.Info("定时任务服务已启动");
        return Task.CompletedTask;
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Dispose();
        _logger.Info("定时任务服务已停止");
        return Task.CompletedTask;
    }
    
    private void ExecuteTask(Object? state)
    {
        _logger.Info("执行定时任务...");
        // 任务逻辑
    }
}
```

### 4. 带超时的测试运行

```csharp
class Program
{
    static async Task Main()
    {
        var ioc = ObjectContainer.Current;
        ioc.AddHostedService<TestService>();
        
        var host = new Host(ioc.BuildServiceProvider());
        host.MaxTime = 30_000;  // 30秒后自动停止
        
        await host.RunAsync();
        
        Console.WriteLine("测试完成");
    }
}
```

### 5. 优雅退出处理

```csharp
public class GracefulService : IHostedService
{
    private readonly List<Task> _runningTasks = new();
    private CancellationTokenSource? _cts;
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        // 启动多个工作任务
        for (var i = 0; i < 5; i++)
        {
            _runningTasks.Add(WorkerLoop(i, _cts.Token));
        }
        
        return Task.CompletedTask;
    }
    
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        XTrace.WriteLine("收到停止信号，等待任务完成...");
        
        // 取消工作任务
        _cts?.Cancel();
        
        // 等待所有任务完成，最多等待10秒
        var timeout = Task.Delay(10_000, cancellationToken);
        var allTasks = Task.WhenAll(_runningTasks);
        
        await Task.WhenAny(allTasks, timeout);
        
        XTrace.WriteLine("所有任务已停止");
    }
    
    private async Task WorkerLoop(Int32 id, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            XTrace.WriteLine($"Worker {id} 执行中...");
            await Task.Delay(1000, token).ConfigureAwait(false);
        }
    }
}
```

## 退出信号处理

Host 自动处理以下退出信号：

| 信号 | 平台 | 说明 |
|------|------|------|
| Ctrl+C | 全平台 | 控制台中断 |
| SIGINT | Linux/macOS | 中断信号 |
| SIGTERM | Linux/macOS | 终止信号（Docker 默认） |
| SIGQUIT | Linux/macOS | 退出信号 |
| ProcessExit | 全平台 | 进程退出事件 |

**Docker 部署注意**：
```dockerfile
# 使用 exec 形式，确保信号正确传递
CMD ["dotnet", "MyApp.dll"]

# 或者使用 tini 作为 init 进程
ENTRYPOINT ["/sbin/tini", "--"]
CMD ["dotnet", "MyApp.dll"]
```

## 最佳实践

### 1. 服务启动顺序

服务按注册顺序启动，按反向顺序停止：

```csharp
ioc.AddHostedService<DatabaseService>();  // 先启动，后停止
ioc.AddHostedService<CacheService>();     // 第二
ioc.AddHostedService<ApiService>();       // 后启动，先停止
```

### 2. 异常处理

启动失败时会自动回滚已启动的服务：

```csharp
public class MyService : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // 如果抛出异常，已启动的服务会被自动停止
        await InitializeAsync();
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        // 确保停止逻辑不抛出异常
        try
        {
            return CleanupAsync();
        }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);
            return Task.CompletedTask;
        }
    }
}
```

### 3. 资源释放

实现 `IDisposable` 进行额外清理：

```csharp
public class ResourceService : IHostedService, IDisposable
{
    private FileStream? _file;
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _file = File.OpenWrite("data.log");
        return Task.CompletedTask;
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
    
    public void Dispose()
    {
        _file?.Dispose();
    }
}
```

## 相关链接

- [对象容器 ObjectContainer](object_container-对象容器ObjectContainer.md)
- [高级定时器 TimerX](timerx-高级定时器TimerX.md)
- [日志系统 ILog](log-日志ILog.md)
