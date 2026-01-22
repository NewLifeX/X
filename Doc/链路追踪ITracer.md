# 链路追踪ITracer

## 概述

可观测性是衡量现代应用质量的核心指标之一。NewLife.Core 提供了一套完整的链路追踪规范：`ITracer`/`ISpan`，用于实现轻量级 APM（Application Performance Monitoring）。

与传统 APM 系统不同，NewLife 的链路追踪采用**本地采样+统计**的方式：
- 在本地完成数据采样和初步统计
- 仅上报统计数据和少量采样样本
- 极大节省网络传输和存储成本

NewLife 全系列项目（30+ 组件）均内置了 `ITracer` 埋点，开发者可以：
1. 无侵入地获取组件运行指标
2. 接入星尘监控平台实现分布式追踪
3. 基于规范扩展自定义埋点

**Nuget 包**: `NewLife.Core`  
**源码**: [NewLife.Core/Log/ITracer.cs](https://github.com/NewLifeX/X/blob/master/NewLife.Core/Log/ITracer.cs)

---

## 快速入门

### 基础用法

最简单的埋点示例：

```csharp
using var span = tracer?.NewSpan("操作名称");
```

使用 `using` 关键字确保 span 在作用域结束时自动完成，记录耗时和次数。

### 完整示例

以下是网络接收数据的埋点示例：

```csharp
private void Ss_Received(Object? sender, ReceivedEventArgs e)
{
    var ns = (this as INetSession).Host;
    var tracer = ns?.Tracer;
    
    // 创建埋点，记录接收操作
    using var span = tracer?.NewSpan($"net:{ns?.Name}:Receive", e.Message);
    try
    {
        OnReceive(e);
    }
    catch (Exception ex)
    {
        // 标记错误并记录异常信息
        span?.SetError(ex, e.Message ?? e.Packet);
        throw;
    }
}
```

这个示例演示了：
- **创建埋点**：使用动态生成的名称
- **数据标签**：第二个参数 `e.Message` 作为数据标签
- **异常处理**：通过 `SetError` 标记异常埋点
- **自动记录**：`using` 语句自动记录耗时

通过这个埋点，我们可以：
- 统计接收数据包的次数
- 记录每次接收的耗时
- 统计异常发生次数
- 查看采样数据和标签

---

## 核心接口

### ITracer 接口

性能跟踪器，轻量级 APM 规范的核心接口。

```csharp
public interface ITracer
{
    #region 属性
    /// <summary>采样周期。单位秒，默认15秒</summary>
    Int32 Period { get; set; }

    /// <summary>最大正常采样数。采样周期内，最多只记录指定数量的正常事件，默认1</summary>
    Int32 MaxSamples { get; set; }

    /// <summary>最大异常采样数。采样周期内，最多只记录指定数量的异常事件，默认10</summary>
    Int32 MaxErrors { get; set; }

    /// <summary>超时时间。超过该时间时强制采样，单位毫秒，默认5000</summary>
    Int32 Timeout { get; set; }

    /// <summary>最大标签长度。超过该长度时将截断，默认1024字符</summary>
    Int32 MaxTagLength { get; set; }

    /// <summary>向http/rpc请求注入TraceId的参数名，为空表示不注入，默认W3C标准的traceparent</summary>
    String? AttachParameter { get; set; }
    #endregion

    /// <summary>建立Span构建器</summary>
    /// <param name="name">操作名称，用于标识不同的埋点类型</param>
    ISpanBuilder BuildSpan(String name);

    /// <summary>开始一个Span</summary>
    /// <param name="name">操作名称，用于标识不同的埋点类型</param>
    ISpan NewSpan(String name);

    /// <summary>开始一个Span，指定数据标签</summary>
    /// <param name="name">操作名称，用于标识不同的埋点类型</param>
    /// <param name="tag">数据标签，记录关键参数信息</param>
    ISpan NewSpan(String name, Object? tag);

    /// <summary>截断所有Span构建器数据，重置集合</summary>
    ISpanBuilder[] TakeAll();
}
```

#### 关键属性说明

| 属性 | 默认值 | 说明 |
|-----|-------|------|
| `Period` | 15秒 | 采样周期，定期上报数据的时间间隔 |
| `MaxSamples` | 1 | 每个周期内记录的正常采样数，用于绘制调用依赖关系 |
| `MaxErrors` | 10 | 每个周期内记录的异常采样数，用于问题诊断 |
| `Timeout` | 5000ms | 超时阈值，超过此时间的操作会被强制采样 |
| `MaxTagLength` | 1024 | 标签最大长度，超过会被截断 |
| `AttachParameter` | "traceparent" | HTTP/RPC 请求中注入 TraceId 的参数名，遵循 W3C 标准 |

这些参数通常由星尘监控中心动态下发调整，无需手动配置。

#### 核心方法

- **`NewSpan(String name)`**: 最常用的方法，创建一个新埋点
- **`NewSpan(String name, Object? tag)`**: 创建埋点并附加数据标签
- **`BuildSpan(String name)`**: 获取或创建 SpanBuilder，用于高级场景

### ISpan 接口

性能跟踪片段，代表一个具体的埋点实例。

```csharp
public interface ISpan : IDisposable
{
    /// <summary>唯一标识。随线程上下文、Http、Rpc传递，作为内部片段的父级</summary>
    String Id { get; set; }

    /// <summary>埋点名。用于标识不同类型的操作</summary>
    String Name { get; set; }

    /// <summary>父级片段标识。用于构建调用链</summary>
    String? ParentId { get; set; }

    /// <summary>跟踪标识。可用于关联多个片段，建立依赖关系，随线程上下文、Http、Rpc传递</summary>
    String TraceId { get; set; }

    /// <summary>开始时间。Unix毫秒时间戳</summary>
    Int64 StartTime { get; set; }

    /// <summary>结束时间。Unix毫秒时间戳</summary>
    Int64 EndTime { get; set; }

    /// <summary>用户数值。记录数字型标量，如每次数据库操作行数，星尘平台汇总统计</summary>
    Int64 Value { get; set; }

    /// <summary>数据标签。记录一些附加数据，如请求参数、响应结果等</summary>
    String? Tag { get; set; }

    /// <summary>错误信息。记录异常消息</summary>
    String? Error { get; set; }

    /// <summary>设置错误信息，ApiException除外</summary>
    void SetError(Exception ex, Object? tag = null);

    /// <summary>设置数据标签。内部根据最大长度截断</summary>
    void SetTag(Object tag);

    /// <summary>抛弃埋点，不计入采集</summary>
    void Abandon();
}
```

#### 关键属性

| 属性 | 类型 | 说明 |
|-----|------|------|
| `Id` | String | Span 唯一标识，遵循 W3C 标准 |
| `ParentId` | String? | 父级 Span ID，用于构建调用树 |
| `TraceId` | String | 跟踪链标识，同一调用链中的所有 Span 共享相同 TraceId |
| `StartTime` | Int64 | 开始时间（Unix 毫秒） |
| `EndTime` | Int64 | 结束时间（Unix 毫秒） |
| `Value` | Int64 | 用户数值，可记录业务指标（如数据库行数） |
| `Tag` | String? | 数据标签，记录请求参数、响应数据等 |
| `Error` | String? | 错误信息 |

#### 核心方法

- **`SetError(Exception ex, Object? tag)`**: 标记异常，`ApiException` 类型会被特殊处理
- **`SetTag(Object tag)`**: 设置数据标签，支持多种类型自动序列化
- **`Abandon()`**: 丢弃当前埋点，常用于过滤无效请求（如404扫描）

---

## 使用最佳实践

### 1. 注入 ITracer

在 ASP.NET Core 应用中，通过星尘扩展注入：

```csharp
using NewLife.Stardust.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 注入星尘，包含 ITracer 实现
builder.Services.AddStardust();
```

详见：[星尘分布式服务平台](https://newlifex.com/blood/stardust)

### 2. 获取 ITracer 实例

有多种方式获取 `ITracer`：

```csharp
// 方式1：构造函数注入（推荐）
public class MyService
{
    private readonly ITracer _tracer;
    
    public MyService(ITracer tracer)
    {
        _tracer = tracer;
    }
}

// 方式2：属性注入
public class MyService
{
    public ITracer? Tracer { get; set; }
}

// 方式3：使用全局静态实例
var tracer = DefaultTracer.Instance;
```

### 3. 创建埋点

#### 基础埋点

```csharp
using var span = _tracer?.NewSpan("MyOperation");
// 业务逻辑
```

#### 带数据标签的埋点

```csharp
var request = new { UserId = 123, Action = "Query" };
using var span = _tracer?.NewSpan("UserQuery", request);
// 业务逻辑
```

#### 带异常处理的埋点

```csharp
using var span = _tracer?.NewSpan("DatabaseQuery");
try
{
    // 执行数据库查询
    var result = await ExecuteQueryAsync();
}
catch (Exception ex)
{
    span?.SetError(ex, "查询失败");
    throw;
}
```

#### 记录业务指标

```csharp
using var span = _tracer?.NewSpan("BatchProcess");
var count = ProcessRecords();
span.Value = count;  // 记录处理的记录数
```

### 4. 定时任务埋点示例

```csharp
public class DataRetentionService : IHostedService
{
    private readonly ITracer _tracer;
    private readonly StarServerSetting _setting;
    private TimerX? _timer;

    public DataRetentionService(StarServerSetting setting, ITracer tracer)
    {
        _setting = setting;
        _tracer = tracer;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // 每600秒执行一次，随机延迟启动
        _timer = new TimerX(DoWork, null, 
            DateTime.Today.AddMinutes(Rand.Next(60)), 
            600 * 1000) 
        { 
            Async = true 
        };
        
        return Task.CompletedTask;
    }

    private void DoWork(Object? state)
    {
        var set = _setting;
        if (set.DataRetention <= 0) return;

        var time = DateTime.Now.AddDays(-set.DataRetention);
        var time2 = DateTime.Now.AddDays(-set.DataRetention2);
        var time3 = DateTime.Now.AddDays(-set.DataRetention3);

        // 创建埋点，记录清理任务
        using var span = _tracer?.NewSpan("DataRetention", new { time, time2, time3 });
        try
        {
            // 删除旧数据
            var rs = AppMinuteStat.DeleteBefore(time);
            XTrace.WriteLine("删除[{0}]之前的AppMinuteStat共：{1:n0}", time, rs);

            rs = TraceMinuteStat.DeleteBefore(time);
            XTrace.WriteLine("删除[{0}]之前的TraceMinuteStat共：{1:n0}", time, rs);
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.TryDispose();
        return Task.CompletedTask;
    }
}
```

### 5. 过滤无效请求

在 Web 应用中，经常遇到各种扫描请求，可以使用 `Abandon()` 丢弃这些埋点：

```csharp
public IActionResult ProcessRequest()
{
    using var span = _tracer?.NewSpan("WebRequest");
    
    // 检测到无效请求（如404扫描）
    if (IsInvalidScan())
    {
        span?.Abandon();  // 丢弃埋点，不计入统计
        return NotFound();
    }
    
    // 正常处理
    return Ok();
}
```

---

## 分布式链路追踪

### 跨服务传递

ISpan 提供了扩展方法，支持通过 HTTP/RPC 传递跟踪上下文：

```csharp
// HTTP 请求注入
var request = new HttpRequestMessage(HttpMethod.Get, url);
span?.Attach(request);  // 注入 traceparent 头
await httpClient.SendAsync(request);

// RPC 调用注入
var args = new { UserId = 123 };
span?.Attach(args);  // 注入跟踪参数
await rpcClient.InvokeAsync("Method", args);
```

服务端收到请求后，会自动解析 `traceparent` 参数，恢复跟踪上下文。

### 调用链构建

同一个 `TraceId` 下的所有 Span 会自动形成调用链：

```
TraceId: ac1b1e8617342790000015eb0ea2a6
├─ DataRetention (父)
   ├─ SQL:AppMinuteStat.DeleteBefore (子)
   ├─ SQL:TraceMinuteStat.DeleteBefore (子)
   └─ SQL:TraceHourStat.DeleteBefore (子)
```

在星尘监控平台可以查看：
- [调用链火焰图](https://star.newlifex.com/trace?id=ac1b1e8617342790000015eb0ea2a6)
- [调用链日志视图](https://star.newlifex.com/trace?id=ac1b1e8617342790000015eb0ea2a6&layout=detail)

---

## 监控与分析

### 查看统计数据

在星尘监控平台可以查看埋点统计：
- 调用总次数
- 异常次数
- 平均耗时、最大耗时、最小耗时
- QPS（每秒请求数）

示例：[DataRetention 埋点统计](https://star.newlifex.com/Monitors/traceDayStat?appId=4&itemId=284)

### 采样策略

ITracer 采用智能采样策略：
1. **正常采样**：每个周期内最多保留 `MaxSamples` 个正常样本（默认1个）
2. **异常采样**：每个周期内最多保留 `MaxErrors` 个异常样本（默认10个）
3. **超时采样**：耗时超过 `Timeout` 的操作强制采样
4. **全链路采样**：设置 `TraceFlag` 的调用链全量采样

### 本地统计机制

每个埋点名对应一个 `SpanBuilder`，用于累加统计：
- **Total**: 总次数
- **Errors**: 异常次数
- **Cost**: 总耗时
- **MaxCost**: 最大耗时
- **MinCost**: 最小耗时

每个采样周期结束后，`SpanBuilder` 数据打包上报，然后重置计数器。

---

## 高级特性

### 埋点命名规范

建议使用分层命名，便于分类统计：

```csharp
// 网络层
tracer.NewSpan("net:{协议}:Receive");
tracer.NewSpan("net:tcp:Send");

// 数据库层
tracer.NewSpan("db:{表名}:Select");
tracer.NewSpan("db:User:Insert");

// 业务层
tracer.NewSpan("biz:{模块}:{操作}");
tracer.NewSpan("biz:Order:Create");

// 外部调用
tracer.NewSpan("http:{服务}:{方法}");
tracer.NewSpan("http:PaymentApi:Pay");
```

### 动态参数调整

可以动态调整采样参数：

```csharp
var tracer = DefaultTracer.Instance;
tracer.Period = 30;           // 改为30秒周期
tracer.MaxSamples = 5;        // 增加正常采样数
tracer.Timeout = 10000;       // 超时阈值改为10秒
tracer.MaxTagLength = 2048;   // 标签长度改为2K
```

通常这些参数由星尘监控中心统一下发。

### 自定义 ITracer 实现

可以实现自己的 `ITracer`：

```csharp
public class CustomTracer : ITracer
{
    public Int32 Period { get; set; } = 15;
    public Int32 MaxSamples { get; set; } = 1;
    public Int32 MaxErrors { get; set; } = 10;
    // ... 实现接口方法
    
    public ISpan NewSpan(String name)
    {
        // 自定义埋点创建逻辑
        var span = new CustomSpan { Name = name };
        span.Start();
        return span;
    }
}

// 注册自定义实现
DefaultTracer.Instance = new CustomTracer();
```

---

## 性能优化

### 对象池

`DefaultTracer` 内置了对象池，复用 `ISpanBuilder` 和 `ISpan` 实例：

```csharp
public IPool<ISpanBuilder> BuilderPool { get; }
public IPool<ISpan> SpanPool { get; }
```

频繁创建的埋点对象会自动归还对象池，减少 GC 压力。

### 标签长度控制

对于大型对象，建议控制标签内容：

```csharp
// 不推荐：记录整个大对象
span.SetTag(largeObject);

// 推荐：只记录关键信息
span.SetTag(new { Id = obj.Id, Name = obj.Name });
```

### 条件埋点

对于非关键路径，可以条件性创建埋点：

```csharp
ISpan? span = null;
if (_tracer != null && IsImportantOperation())
{
    span = _tracer.NewSpan("ImportantOp");
}

try
{
    // 业务逻辑
}
finally
{
    span?.Dispose();
}
```

---

## 异常处理

### ApiException 特殊处理

业务异常 `ApiException` 不会被标记为错误：

```csharp
catch (ApiException aex)
{
    span?.SetError(aex, request);
    // 记录为正常埋点，Tag中包含业务错误码
}
catch (Exception ex)
{
    span?.SetError(ex, request);
    // 记录为异常埋点，Error中包含异常消息
}
```

### 异常埋点

每个异常会自动创建独立的异常埋点，便于按异常类型统计：

```csharp
// 原始埋点
using var span = tracer.NewSpan("DatabaseQuery");

try
{
    // 抛出异常
    throw new TimeoutException("查询超时");
}
catch (Exception ex)
{
    span.SetError(ex, query);
    // 自动创建 "ex:TimeoutException" 埋点
}
```

---

## 常见问题

### 1. 埋点数据在星尘平台看不到？

检查以下几点：
- 确认已注入星尘扩展：`services.AddStardust()`
- 检查网络连接到星尘服务器
- 检查应用在星尘平台是否已注册
- 查看本地日志，确认埋点正常创建

### 2. 采样数据太少？

调整采样参数：
```csharp
tracer.MaxSamples = 10;    // 增加正常采样数
tracer.MaxErrors = 50;     // 增加异常采样数
tracer.Timeout = 1000;     // 降低超时阈值
```

### 3. 如何关闭埋点？

```csharp
// 方式1：不注入 ITracer
// services.AddStardust();  // 注释掉

// 方式2：使用空实现
DefaultTracer.Instance = null;
```

### 4. 如何查看本地埋点数据？

DefaultTracer 会输出到日志：

```
Tracer[DataRetention] Total=10 Errors=0 Speed=0.02tps Cost=1500ms MaxCost=2000ms MinCost=1000ms
```

### 5. 并发场景下 Span 会乱吗？

不会。`ISpan` 使用 `AsyncLocal<ISpan>` 保存上下文，每个异步流程独立：

```csharp
public static AsyncLocal<ISpan?> Current { get; }
```

---

## 参考资料

- **星尘监控平台**: https://newlifex.com/blood/stardust
- **W3C Trace Context**: https://www.w3.org/TR/trace-context/
- **源码仓库**: https://github.com/NewLifeX/X
- **在线文档**: https://newlifex.com/core/tracer

---

## 更新日志

- **2024-12-16**: 完善文档，补充最佳实践和示例代码
- **2024-08**: 支持 .NET 9.0
- **2023-11**: 增加 `Abandon()` 方法
- **2023-06**: 优化对象池，提升性能
- **2022**: 初始版本发布
