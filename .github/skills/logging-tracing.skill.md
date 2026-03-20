---
name: logging-tracing
description: 使用 NewLife 日志框架和分布式链路追踪 APM，实现结构化日志、多输出和性能追踪
---

# NewLife 日志与链路追踪使用指南

## 适用场景

- 应用程序日志输出（控制台、文件、网络）
- 分布式链路追踪和 APM 性能监控
- 代码执行性能计时
- 与星尘（Stardust）监控平台集成

## 日志系统

### 快速启动

```csharp
// 启用控制台 + 文件日志
XTrace.UseConsole();

// 输出日志
XTrace.WriteLine("应用启动");
XTrace.WriteLine("处理用户 {0} 的请求，耗时 {1}ms", userId, elapsed);

// 输出异常
try { ... }
catch (Exception ex) { XTrace.WriteException(ex); }

// 输出版本信息（应用启动时调用）
XTrace.WriteVersion();
```

### ILog 接口

```csharp
public class MyService
{
    public ILog Log { get; set; } = Logger.Null;

    public void Process()
    {
        Log.Info("开始处理");
        Log.Debug("调试信息：count={0}", count);
        Log.Warn("注意：队列积压 {0} 条", queue.Count);
        Log.Error("处理失败：{0}", ex.Message);
    }
}

// 注入日志
var svc = new MyService { Log = XTrace.Log };
```

### 日志级别

| 级别 | 方法 | 场景 |
|------|------|------|
| Debug | `Log.Debug()` | 开发调试信息 |
| Info | `Log.Info()` | 正常业务流程 |
| Warn | `Log.Warn()` | 预警但不影响运行 |
| Error | `Log.Error()` | 错误需要关注 |
| Fatal | `Log.Fatal()` | 致命错误 |

### 多目标日志

```csharp
// 组合日志：同时输出到控制台和文件
var log = new CompositeLog(new ConsoleLog(), new TextFileLog("logs"));

// 文本文件日志
var fileLog = new TextFileLog("logs") { MaxBytes = 10 * 1024 * 1024 }; // 10MB 滚动

// 网络日志
var netLog = new NetworkLog("udp://log-server:514");
```

## 链路追踪（ITracer）

### 基本埋点

```csharp
public class OrderService
{
    public ITracer? Tracer { get; set; }

    public void CreateOrder(Order order)
    {
        // 创建追踪 Span（自动计时，自动记录异常）
        using var span = Tracer?.NewSpan("CreateOrder", new { order.UserId, order.Amount });

        try
        {
            ValidateOrder(order);
            SaveToDb(order);
            SendNotification(order);
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);  // 记录异常到 Span
            throw;
        }
    }
}
```

### 配置追踪器

```csharp
// 本地追踪器
var tracer = new DefaultTracer
{
    Period = 15,          // 采样周期秒
    MaxSamples = 1,       // 正常采样数
    MaxErrors = 10,       // 异常采样数
    Timeout = 15000,      // 超时强制采样（毫秒）
    Log = XTrace.Log,
};
DefaultTracer.Instance = tracer;

// 星尘追踪器（需要 Stardust 包）
// var tracer = new StarTracer("http://star.newlifex.com:6600") { ... };
```

### Span 嵌套

```csharp
public async Task ProcessAsync()
{
    using var span = Tracer?.NewSpan("Process");

    // 子 Span 自动关联父 Span
    using var span2 = Tracer?.NewSpan("Step1");
    await Step1Async();
    span2?.Dispose();

    using var span3 = Tracer?.NewSpan("Step2", new { key = "value" });
    await Step2Async();
}
```

### 附加标签

```csharp
using var span = Tracer?.NewSpan("HttpCall");
span?.AppendTag($"url={url}");
span?.AppendTag($"status={response.StatusCode}");
```

## 代码计时器

```csharp
// 快速性能测试
var timer = new CodeTimer();
timer.ShowHeader();
timer.Show("字符串拼接", () =>
{
    var s = "";
    for (var i = 0; i < 1000; i++) s += i;
});
timer.Show("StringBuilder", () =>
{
    var sb = Pool.StringBuilder.Get();
    for (var i = 0; i < 1000; i++) sb.Append(i);
    sb.Put();
});
```

## 在 NetServer/NetSession 中使用

```csharp
class MySession : NetSession<MyServer>
{
    protected override void OnReceive(ReceivedEventArgs e)
    {
        // NetSession 内置 Log 和 Tracer
        WriteLog("收到数据 {0} 字节", e.Packet?.Total);
        using var span = Session?.Tracer?.NewSpan("OnReceive");
        // ...
    }
}
```

## 注意事项

- `Tracer?.NewSpan()` 使用 null 条件运算符，未配置追踪器时零开销
- `using var span = ...` 确保 Span 结束时自动上报
- `span?.SetError(ex, null)` 在 catch 中调用，不影响异常传播
- 热点路径（每秒万次以上调用）的 Span 名称应固定，不含变量
- `XTrace.Log` 是全局日志入口，`XTrace.UseConsole()` 初始化控制台输出
