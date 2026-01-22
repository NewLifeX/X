# 运行时信息 Runtime

## 概述

`Runtime` 是 NewLife.Core 中的运行时信息工具类，提供当前运行环境的各种检测和操作功能。包括操作系统判断、环境变量读取、内存管理、进程信息等功能，是跨平台开发的重要基础组件。

**命名空间**：`NewLife`  
**文档地址**：https://newlifex.com/core/runtime

## 核心特性

- **平台检测**：Windows、Linux、OSX、Mono、Unity 等运行环境识别
- **环境判断**：控制台、Web、容器等应用类型检测
- **高精度计时**：跨平台的 `TickCount64` 实现，避免 32 位溢出
- **内存管理**：GC 回收和工作集释放
- **环境变量**：不区分大小写的环境变量读取

## 快速开始

```csharp
using NewLife;

// 判断操作系统
if (Runtime.Windows)
    Console.WriteLine("运行在 Windows 系统上");
else if (Runtime.Linux)
    Console.WriteLine("运行在 Linux 系统上");

// 判断运行环境
if (Runtime.IsConsole)
    Console.WriteLine("控制台应用");
if (Runtime.Container)
    Console.WriteLine("运行在容器中");

// 获取系统运行时间（毫秒）
var uptime = Runtime.TickCount64;
Console.WriteLine($"系统已运行 {uptime / 1000 / 60} 分钟");

// 获取当前进程ID
var pid = Runtime.ProcessId;
Console.WriteLine($"当前进程ID: {pid}");

// 释放内存
Runtime.FreeMemory();
```

## API 参考

### 平台检测

#### Windows

```csharp
public static Boolean Windows { get; }
```

是否 Windows 操作系统。

**实现方式**：
- .NET Core/.NET 5+：使用 `RuntimeInformation.IsOSPlatform(OSPlatform.Windows)`
- .NET Framework：检查 `Environment.OSVersion.Platform`

**示例**：
```csharp
if (Runtime.Windows)
{
    // Windows 特有的操作，如调用 Win32 API
    Console.WriteLine("Windows 版本: " + Environment.OSVersion.VersionString);
}
```

#### Linux

```csharp
public static Boolean Linux { get; }
```

是否 Linux 操作系统。

**示例**：
```csharp
if (Runtime.Linux)
{
    // Linux 特有的操作，如读取 /proc 文件系统
    var cpuInfo = File.ReadAllText("/proc/cpuinfo");
}
```

#### OSX

```csharp
public static Boolean OSX { get; }
```

是否 macOS 操作系统。

#### Mono

```csharp
public static Boolean Mono { get; }
```

是否在 Mono 运行时环境中运行。通过检测 `Mono.Runtime` 类型是否存在来判断。

**应用场景**：
- 某些 API 在 Mono 下行为不同
- 针对 Mono 进行特殊优化或兼容处理

#### Unity

```csharp
public static Boolean Unity { get; }
```

是否在 Unity 引擎环境中运行。通过检测 `UnityEngine.Application` 类型是否存在来判断。

### 环境判断

#### IsConsole

```csharp
public static Boolean IsConsole { get; set; }
```

是否控制台应用程序。

**判断逻辑**：
1. 尝试访问 `Console.ForegroundColor` 触发控制台可用性检查
2. 检查当前进程是否有主窗口句柄
3. 任何异常都视为非控制台环境

**示例**：
```csharp
if (Runtime.IsConsole)
{
    Console.WriteLine("这是控制台应用，可以使用彩色输出");
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("绿色文本");
    Console.ResetColor();
}
else
{
    // GUI 应用或服务
    Debug.WriteLine("非控制台环境");
}
```

> **注意**：可以通过设置 `Runtime.IsConsole = false` 强制禁用控制台判断。

#### Container

```csharp
public static Boolean Container { get; }
```

是否在 Docker/Kubernetes 容器中运行。通过检查环境变量 `DOTNET_RUNNING_IN_CONTAINER` 来判断。

**示例**：
```csharp
if (Runtime.Container)
{
    // 容器环境下的特殊处理
    // 例如：使用容器内的配置路径
    var configPath = "/app/config";
}
```

#### IsWeb

```csharp
public static Boolean IsWeb { get; }
```

是否 Web 应用程序。

**判断逻辑**：
- .NET Core/.NET 5+：检查是否加载了 `Microsoft.AspNetCore` 程序集
- .NET Framework：检查 `System.Web.HttpRuntime.AppDomainAppId` 是否有值

**示例**：
```csharp
if (Runtime.IsWeb)
{
    // Web 应用特有的处理
    // 例如：使用 HTTP 上下文相关功能
}
```

### 时间与计数

#### TickCount64

```csharp
public static Int64 TickCount64 { get; }
```

系统启动以来的毫秒数（64位），不会发生 32 位溢出问题。

**实现方式**：
- .NET Core 3.1+：直接使用 `Environment.TickCount64`
- Windows 旧框架：调用 `GetTickCount64` Win32 API
- 其他平台：使用 `Stopwatch.GetTimestamp()` 计算，或回退到 `Environment.TickCount`

**应用场景**：
- 高精度计时
- 计算时间间隔
- 避免 `Environment.TickCount` 约 49.7 天溢出的问题

**示例**：
```csharp
// 测量操作耗时
var start = Runtime.TickCount64;
DoSomeWork();
var elapsed = Runtime.TickCount64 - start;
Console.WriteLine($"耗时: {elapsed} ms");

// 设置超时
var timeout = Runtime.TickCount64 + 5000; // 5秒后超时
while (Runtime.TickCount64 < timeout)
{
    if (CheckCondition()) break;
    Thread.Sleep(100);
}
```

#### UtcNow

```csharp
public static DateTimeOffset UtcNow { get; }
```

获取当前 UTC 时间。基于全局时间提供者（`TimerScheduler.GlobalTimeProvider`），在星尘应用中会屏蔽服务器时间差。

**示例**：
```csharp
var utcNow = Runtime.UtcNow;
Console.WriteLine($"UTC时间: {utcNow}");
Console.WriteLine($"本地时间: {utcNow.LocalDateTime}");
```

### 进程信息

#### ProcessId

```csharp
public static Int32 ProcessId { get; }
```

当前进程 ID。使用缓存避免重复获取。

**实现方式**：
- .NET 5+：使用 `Environment.ProcessId`
- 旧框架：使用 `Process.GetCurrentProcess().Id`

**示例**：
```csharp
Console.WriteLine($"当前进程ID: {Runtime.ProcessId}");

// 用于日志记录
var logPrefix = $"[PID:{Runtime.ProcessId}]";
```

#### ClientId

```csharp
public static String ClientId { get; }
```

客户端标识，格式为 `ip@pid`。用于分布式系统中标识客户端实例。

**示例**：
```csharp
Console.WriteLine($"客户端标识: {Runtime.ClientId}");
// 输出类似: 192.168.1.100@12345

// 用于分布式锁、消息队列消费者标识等
var consumerId = Runtime.ClientId;
```

### 环境变量

#### GetEnvironmentVariable

```csharp
public static String? GetEnvironmentVariable(String variable)
```

获取环境变量，**不区分大小写**。

**特点**：
- 先尝试精确匹配
- 若未找到，遍历所有环境变量进行不区分大小写的比较

**示例**：
```csharp
// 不区分大小写获取环境变量
var path = Runtime.GetEnvironmentVariable("PATH");
var home = Runtime.GetEnvironmentVariable("HOME");
var customVar = Runtime.GetEnvironmentVariable("MY_APP_CONFIG");
```

#### GetEnvironmentVariables

```csharp
public static IDictionary<String, String?> GetEnvironmentVariables()
```

获取所有环境变量，返回不区分大小写的字典。

**示例**：
```csharp
var envVars = Runtime.GetEnvironmentVariables();
foreach (var kv in envVars.Where(e => e.Key.StartsWith("DOTNET")))
{
    Console.WriteLine($"{kv.Key} = {kv.Value}");
}
```

### 配置

#### CreateConfigOnMissing

```csharp
public static Boolean CreateConfigOnMissing { get; set; }
```

配置文件不存在时，是否生成默认配置文件。默认为 `true`。

**配置方式**：
- 环境变量：`CreateConfigOnMissing=false`
- 代码设置：`Runtime.CreateConfigOnMissing = false`

**示例**：
```csharp
// 生产环境禁止自动创建配置文件
Runtime.CreateConfigOnMissing = false;
```

### 内存管理

#### FreeMemory

```csharp
public static Boolean FreeMemory(Int32 processId = 0, Boolean gc = true, Boolean workingSet = true)
```

释放内存。执行 GC 回收并释放工作集（Windows）。

**参数说明**：
- `processId`：目标进程ID，0 表示当前进程
- `gc`：是否执行 GC 回收（仅当前进程有效）
- `workingSet`：是否释放工作集（仅 Windows 有效）

**执行步骤**：
1. 执行 `GC.Collect` 进行垃圾回收
2. 调用 `GC.WaitForPendingFinalizers` 等待终结器
3. 再次执行 `GC.Collect`
4. 调用 `EmptyWorkingSet` 释放工作集（Windows）

**示例**：
```csharp
// 定期释放内存
var timer = new TimerX(state =>
{
    Runtime.FreeMemory();
}, null, 60_000, 60_000);  // 每分钟执行一次

// 仅释放工作集，不触发GC
Runtime.FreeMemory(gc: false);

// 释放指定进程的内存
Runtime.FreeMemory(processId: 1234, gc: false);
```

> **注意**：频繁调用 `FreeMemory` 可能影响性能，建议在内存压力较大时定期调用。

## 使用场景

### 1. 跨平台路径处理

```csharp
public String GetConfigPath()
{
    if (Runtime.Windows)
        return @"C:\ProgramData\MyApp\config.json";
    else if (Runtime.Linux)
        return "/etc/myapp/config.json";
    else if (Runtime.OSX)
        return "/Library/Application Support/MyApp/config.json";
    else
        return "config.json";
}
```

### 2. 容器环境适配

```csharp
public void ConfigureServices()
{
    if (Runtime.Container)
    {
        // 容器环境：从环境变量读取配置
        var connStr = Runtime.GetEnvironmentVariable("DATABASE_URL");
        services.AddDbContext<MyDbContext>(options =>
            options.UseNpgsql(connStr));
    }
    else
    {
        // 本地开发：从配置文件读取
        var connStr = Configuration.GetConnectionString("Default");
        services.AddDbContext<MyDbContext>(options =>
            options.UseNpgsql(connStr));
    }
}
```

### 3. 内存监控与释放

```csharp
public class MemoryMonitor
{
    private readonly Timer _timer;
    private const Int64 MemoryThreshold = 500 * 1024 * 1024; // 500MB
    
    public MemoryMonitor()
    {
        _timer = new Timer(CheckMemory, null, 0, 30_000);
    }
    
    private void CheckMemory(Object? state)
    {
        var gcMemory = GC.GetTotalMemory(false);
        if (gcMemory > MemoryThreshold)
        {
            XTrace.WriteLine($"内存超过阈值 ({gcMemory / 1024 / 1024}MB)，开始释放");
            Runtime.FreeMemory();
        }
    }
}
```

### 4. 性能计时器

```csharp
public class PerformanceTimer : IDisposable
{
    private readonly String _operation;
    private readonly Int64 _startTime;
    
    public PerformanceTimer(String operation)
    {
        _operation = operation;
        _startTime = Runtime.TickCount64;
    }
    
    public void Dispose()
    {
        var elapsed = Runtime.TickCount64 - _startTime;
        XTrace.WriteLine($"{_operation} 耗时: {elapsed}ms");
    }
}

// 使用
using (new PerformanceTimer("数据库查询"))
{
    var result = db.Query<User>().ToList();
}
```

## 最佳实践

### 1. 平台特定代码隔离

```csharp
// 推荐：使用条件判断隔离平台特定代码
public void DoWork()
{
    if (Runtime.Windows)
        DoWorkWindows();
    else if (Runtime.Linux)
        DoWorkLinux();
    else
        DoWorkDefault();
}
```

### 2. 避免频繁调用 FreeMemory

```csharp
// 不推荐：每次操作后都释放
foreach (var item in items)
{
    ProcessItem(item);
    Runtime.FreeMemory();  // 性能杀手！
}

// 推荐：批量处理后释放，或定时释放
foreach (var item in items)
{
    ProcessItem(item);
}
Runtime.FreeMemory();  // 处理完成后统一释放
```

### 3. 使用 TickCount64 而非 DateTime 计时

```csharp
// 不推荐：DateTime 计时可能受系统时间调整影响
var start = DateTime.Now;
DoWork();
var elapsed = (DateTime.Now - start).TotalMilliseconds;

// 推荐：TickCount64 不受系统时间影响
var start = Runtime.TickCount64;
DoWork();
var elapsed = Runtime.TickCount64 - start;
```

## 相关链接

- [机器信息 MachineInfo](machine_info-机器信息MachineInfo.md)
- [日志系统 ILog](log-日志ILog.md)
- [高级定时器 TimerX](timerx-高级定时器TimerX.md)
