# 日志ILog

## 概述

`NewLife.Log.ILog` 是 NewLife.Core 的核心日志接口，提供统一的日志记录规范。NewLife 全系列组件均使用该接口记录日志。

通过静态类 `XTrace` 可以方便地使用日志功能，支持：
- 文件日志（默认）
- 控制台日志
- 网络日志
- 自定义日志实现

**命名空间**: `NewLife.Log`  
**源码**: [NewLife.Core/Log/ILog.cs](https://github.com/NewLifeX/X/blob/master/NewLife.Core/Log/ILog.cs)  
**文档**: https://newlifex.com/core/log

---

## 快速入门

### 基础用法

```csharp
using NewLife.Log;

// 方式1：使用 XTrace 静态类（推荐）
XTrace.WriteLine("这是一条信息");
XTrace.WriteLine("用户{0}登录", "admin");

// 方式2：直接使用 ILog 接口
ILog log = XTrace.Log;
log.Info("这是一条信息");
log.Error("这是一条错误");
```

### 日志级别

```csharp
XTrace.Log.Debug("调试信息");      // 调试日志
XTrace.Log.Info("普通信息");       // 信息日志
XTrace.Log.Warn("警告信息");       // 警告日志
XTrace.Log.Error("错误信息");      // 错误日志
XTrace.Log.Fatal("严重错误");      // 严重错误日志
```

### 输出异常

```csharp
try
{
    // 业务代码
}
catch (Exception ex)
{
    XTrace.WriteException(ex);  // 输出异常堆栈
}
```

---

## ILog 接口

```csharp
public interface ILog
{
    /// <summary>写日志</summary>
    void Write(LogLevel level, String format, params Object?[] args);

    /// <summary>调试日志</summary>
    void Debug(String format, params Object?[] args);

    /// <summary>信息日志</summary>
    void Info(String format, params Object?[] args);

    /// <summary>警告日志</summary>
    void Warn(String format, params Object?[] args);

    /// <summary>错误日志</summary>
    void Error(String format, params Object?[] args);

    /// <summary>严重错误日志</summary>
    void Fatal(String format, params Object?[] args);

    /// <summary>是否启用日志。为false时不输出任何日志</summary>
    Boolean Enable { get; set; }

    /// <summary>日志等级，只输出大于等于该级别的日志，默认Info</summary>
    LogLevel Level { get; set; }
}
```

### 日志级别 LogLevel

```csharp
public enum LogLevel
{
    /// <summary>关闭日志</summary>
    Off = 0,
    
    /// <summary>严重错误。导致应用程序退出</summary>
    Fatal = 1,
    
    /// <summary>错误。影响功能运行，需要立即处理</summary>
    Error = 2,
    
    /// <summary>警告。不影响功能，但需要关注</summary>
    Warn = 3,
    
    /// <summary>信息。常规日志信息</summary>
    Info = 4,
    
    /// <summary>调试。调试日志，生产环境应关闭</summary>
    Debug = 5,
    
    /// <summary>全部</summary>
    All = 6
}
```

---

## XTrace 静态类

`XTrace` 是日志的主要使用入口，提供便捷的静态方法。

### 基础方法

```csharp
// 输出信息日志
XTrace.WriteLine("消息");
XTrace.WriteLine("用户{0}在{1}登录", "admin", DateTime.Now);

// 输出异常
XTrace.WriteException(ex);
```

### 关键属性

```csharp
// 获取或设置日志实现
ILog log = XTrace.Log;  // 默认为文件日志
XTrace.Log = new ConsoleLog();  // 切换为控制台日志

// 是否调试模式
XTrace.Debug = true;  // 开启调试，输出Debug级别日志

// 日志路径
XTrace.LogPath = "Logs";  // 设置日志文件夹
```

---

## 默认文件日志

NewLife 默认使用 `TextFileLog`，将日志输出到文本文件。

### 特性

- 自动按日期分割日志文件（如 `2025-01-07.log`）
- 异步写入，不阻塞业务线程
- 自动备份和清理旧日志
- 支持配置日志路径、最大文件大小等

### 配置

在 `NewLife.config` 或 `appsettings.json` 中配置：

```xml
<!-- NewLife.config -->
<Config>
  <Setting>
    <LogPath>Logs</LogPath>         <!-- 日志路径 -->
    <LogLevel>Info</LogLevel>        <!-- 日志级别 -->
    <LogFileFormat>{0:yyyy-MM-dd}.log</LogFileFormat>  <!-- 文件命名格式 -->
  </Setting>
</Config>
```

```json
// appsettings.json
{
  "NewLife": {
    "Setting": {
      "LogPath": "Logs",
      "LogLevel": "Info",
      "LogFileFormat": "{0:yyyy-MM-dd}.log"
    }
  }
}
```

### 日志文件示例

```
2025-01-07 10:15:23.456  Info  应用程序启动
2025-01-07 10:15:24.123  Info  用户admin登录
2025-01-07 10:20:15.789  Warn  连接池已满
2025-01-07 10:25:30.456  Error 数据库连接超时
System.TimeoutException: 连接超时
   at MyApp.Database.Query(String sql)
   at MyApp.Service.GetData()
```

---

## 控制台日志

在控制台应用中，可以使用 `UseConsole()` 将日志输出到控制台。

### 使用方法

```csharp
class Program
{
    static void Main(String[] args)
    {
        // 重定向日志到控制台
        XTrace.UseConsole();
        
        XTrace.WriteLine("应用程序启动");
        XTrace.Log.Error("这是一条错误");
    }
}
```

### 彩色输出

控制台日志支持彩色输出，不同日志级别使用不同颜色：
- **Debug**: 灰色
- **Info**: 白色
- **Warn**: 黄色
- **Error**: 红色
- **Fatal**: 洋红色

### 多线程彩色

```csharp
XTrace.UseConsole(useColor: true);  // 启用彩色输出

ThreadPool.QueueUserWorkItem(_ =>
{
    XTrace.WriteLine("线程1");  // 自动使用不同颜色
});

ThreadPool.QueueUserWorkItem(_ =>
{
    XTrace.WriteLine("线程2");  // 自动使用不同颜色
});
```

---

## 网络日志

将日志通过网络发送到远程日志服务器。

### 使用方法

```csharp
// 配置网络日志
XTrace.Log = new NetworkLog("tcp://logserver:514");

XTrace.WriteLine("这条日志会发送到远程服务器");
```

### 适用场景

- 集中式日志收集
- 分布式系统日志聚合
- 容器化应用日志输出

---

## 复合日志

同时输出到多个目标。

### 使用方法

```csharp
using NewLife.Log;

var compositeLog = new CompositeLog();
compositeLog.Add(new TextFileLog());    // 文件日志
compositeLog.Add(new ConsoleLog());     // 控制台日志
compositeLog.Add(new NetworkLog("tcp://logserver:514"));  // 网络日志

XTrace.Log = compositeLog;
```

---

## 自定义日志

实现 `ILog` 接口创建自定义日志。

### 示例：数据库日志

```csharp
public class DatabaseLog : ILog
{
    public Boolean Enable { get; set; } = true;
    public LogLevel Level { get; set; } = LogLevel.Info;

    public void Write(LogLevel level, String format, params Object?[] args)
    {
        if (!Enable || level > Level) return;

        var message = args.Length > 0 ? String.Format(format, args) : format;
        
        // 写入数据库
        Database.Insert("Logs", new
        {
            Level = level.ToString(),
            Message = message,
            CreateTime = DateTime.Now
        });
    }

    public void Debug(String format, params Object?[] args) => Write(LogLevel.Debug, format, args);
    public void Info(String format, params Object?[] args) => Write(LogLevel.Info, format, args);
    public void Warn(String format, params Object?[] args) => Write(LogLevel.Warn, format, args);
    public void Error(String format, params Object?[] args) => Write(LogLevel.Error, format, args);
    public void Fatal(String format, params Object?[] args) => Write(LogLevel.Fatal, format, args);
}

// 使用
XTrace.Log = new DatabaseLog();
```

---

## 使用场景

### 1. 应用程序启动日志

```csharp
class Program
{
    static void Main(String[] args)
    {
        XTrace.WriteLine("应用程序启动");
        XTrace.WriteLine("版本：{0}", Assembly.GetExecutingAssembly().GetName().Version);
        XTrace.WriteLine("运行时：{0}", Runtime.Version);
        
        try
        {
            RunApplication();
        }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);
            XTrace.Log.Fatal("应用程序异常退出");
        }
    }
}
```

### 2. 接口调用日志

```csharp
public class UserService
{
    public void Login(String username, String password)
    {
        XTrace.WriteLine("用户{0}尝试登录", username);
        
        if (ValidateUser(username, password))
        {
            XTrace.WriteLine("用户{0}登录成功", username);
        }
        else
        {
            XTrace.Log.Warn("用户{0}登录失败：密码错误", username);
        }
    }
}
```

### 3. 异常处理

```csharp
try
{
    var data = await FetchDataAsync();
    ProcessData(data);
}
catch (TimeoutException ex)
{
    XTrace.Log.Warn("数据获取超时：{0}", ex.Message);
}
catch (Exception ex)
{
    XTrace.WriteException(ex);
    throw;
}
```

### 4. 调试日志

```csharp
#if DEBUG
XTrace.Debug = true;  // 开发环境开启调试
#endif

XTrace.Log.Debug("开始处理数据：{0}条", data.Length);
foreach (var item in data)
{
    XTrace.Log.Debug("处理项目：{0}", item.Id);
    ProcessItem(item);
}
XTrace.Log.Debug("数据处理完成");
```

---

## 最佳实践

### 1. 合理使用日志级别

```csharp
// Debug：调试信息，生产环境应关闭
XTrace.Log.Debug("变量值：{0}", value);

// Info：常规信息，记录重要操作
XTrace.Log.Info("用户{0}登录", username);

// Warn：警告信息，不影响功能但需关注
XTrace.Log.Warn("连接池使用率：{0}%", usage);

// Error：错误信息，影响功能运行
XTrace.Log.Error("数据库连接失败：{0}", ex.Message);

// Fatal：严重错误，导致应用退出
XTrace.Log.Fatal("配置文件损坏，应用程序退出");
```

### 2. 避免日志信息过多

```csharp
// 不推荐：循环中输出日志
foreach (var item in items)  // 100万条数据
{
    XTrace.Log.Debug("处理：{0}", item);  // 产生100万条日志！
}

// 推荐：汇总输出
var count = 0;
foreach (var item in items)
{
    ProcessItem(item);
    count++;
}
XTrace.Log.Info("处理完成：{0}条", count);
```

### 3. 使用结构化日志

```csharp
// 推荐：使用占位符
XTrace.Log.Info("用户{0}从{1}登录，IP={2}", username, location, ip);

// 不推荐：字符串拼接
XTrace.Log.Info("用户" + username + "从" + location + "登录，IP=" + ip);
```

### 4. 性能考虑

```csharp
// 推荐：先判断级别
if (XTrace.Log.Enable && XTrace.Log.Level >= LogLevel.Debug)
{
    var expensiveData = GetExpensiveDebugInfo();  // 昂贵操作
    XTrace.Log.Debug("调试信息：{0}", expensiveData);
}

// 不推荐：直接调用
XTrace.Log.Debug("调试信息：{0}", GetExpensiveDebugInfo());  // 即使Debug关闭也会执行
```

---

## 配置管理

### 通过代码配置

```csharp
// 设置日志级别
XTrace.Log.Level = LogLevel.Warn;  // 只输出Warn及以上

// 关闭日志
XTrace.Log.Enable = false;

// 设置日志路径
XTrace.LogPath = "C:\\Logs";
```

### 通过配置文件

```xml
<!-- NewLife.config -->
<Config>
  <Setting>
    <LogPath>Logs</LogPath>
    <LogLevel>Info</LogLevel>
    <Debug>false</Debug>
  </Setting>
</Config>
```

### 运行时修改

```csharp
// 临时开启调试
var oldDebug = XTrace.Debug;
XTrace.Debug = true;

try
{
    DebugMethod();
}
finally
{
    XTrace.Debug = oldDebug;  // 恢复设置
}
```

---

## 全局异常处理

XTrace 自动捕获未处理异常：

```csharp
static XTrace()
{
    AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
}
```

当发生未处理异常时，会自动输出异常日志。

---

## 常见问题

### 1. 如何关闭日志？

```csharp
// 方式1：关闭日志输出
XTrace.Log.Enable = false;

// 方式2：设置日志级别为Off
XTrace.Log.Level = LogLevel.Off;

// 方式3：使用空日志
XTrace.Log = Logger.Null;
```

### 2. 日志文件在哪里？

默认在应用程序根目录的 `Logs` 文件夹下，文件名格式为 `yyyy-MM-dd.log`。

### 3. 如何输出到多个目标？

```csharp
var compositeLog = new CompositeLog();
compositeLog.Add(new TextFileLog());
compositeLog.Add(new ConsoleLog());
XTrace.Log = compositeLog;
```

### 4. 日志文件太大怎么办？

配置日志备份和清理策略：
```csharp
var textLog = new TextFileLog();
textLog.MaxBytes = 10 * 1024 * 1024;  // 最大10MB
textLog.Backups = 10;                  // 保留10个备份
```

### 5. 如何在ASP.NET Core中使用？

```csharp
// Startup.cs 或 Program.cs
public void Configure(IApplicationBuilder app)
{
    // 日志已自动初始化，直接使用
    XTrace.WriteLine("应用程序启动");
}
```

---

## 参考资料

- **在线文档**: https://newlifex.com/core/log
- **源码**: https://github.com/NewLifeX/X/tree/master/NewLife.Core/Log
- **链路追踪**: [tracer-链路追踪ITracer.md](tracer-链路追踪ITracer.md)

---

## 更新日志

- **2025-01**: 完善文档，补充详细示例
- **2024**: 支持 .NET 9.0
- **2023**: 优化异步写入性能
- **2022**: 增加网络日志支持
- **2020**: 重构日志架构，统一接口
