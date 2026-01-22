# 高级定时器TimerX

## 概述

`NewLife.Threading.TimerX` 是一个功能强大的定时器实现，相比系统 `System.Threading.Timer` 具有以下优势：

- **不可重入**：回调执行完毕后才开始计时下一次
- **支持异步回调**：原生支持 `async/await`
- **绝对时间执行**：支持固定时刻执行（如每天2点）
- **Cron 表达式**：支持复杂的定时规则
- **链路追踪**：内置 `ITracer` 埋点支持
- **安全释放**：挂载在静态调度器上，避免被GC提前回收

**命名空间**: `NewLife.Threading`  
**源码**: [NewLife.Core/Threading/TimerX.cs](https://github.com/NewLifeX/X/blob/master/NewLife.Core/Threading/TimerX.cs)

---

## 快速入门

### 基础用法：周期性执行

```csharp
using NewLife.Threading;

// 1秒后首次执行，然后每5秒执行一次
var timer = new TimerX(state =>
{
    Console.WriteLine($"执行时间：{DateTime.Now}");
}, null, 1000, 5000);

// 使用完毕记得释放
timer.Dispose();
```

**参数说明**：
- `state`：用户数据，会传递给回调函数
- `dueTime`：延迟时间（毫秒），首次执行前等待的时间
- `period`：间隔周期（毫秒），设为0或-1表示只执行一次

### 异步回调

```csharp
// 支持 async/await 的异步回调
var timer = new TimerX(async state =>
{
    await Task.Delay(100);  // 模拟异步操作
    Console.WriteLine("异步任务完成");
}, null, 1000, 5000);
```

**自动设置**：
- 使用 `Func<Object, Task>` 时，自动设置 `IsAsyncTask = true` 和 `Async = true`

### 绝对时间执行

```csharp
// 每天凌晨2点执行
var start = DateTime.Today.AddHours(2);
var timer = new TimerX(state =>
{
    Console.WriteLine("执行数据清理任务");
}, null, start, 24 * 3600 * 1000);  // 周期24小时
```

**特点**：
- 如果 `startTime` 小于当前时间，会自动加 `period` 直到大于当前时间
- 自动设置 `Absolutely = true`
- 不受 `SetNext` 影响，始终在固定时刻执行

### Cron 表达式

```csharp
// 每个工作日早上9点执行
var timer = new TimerX(state =>
{
    Console.WriteLine("工作日任务");
}, null, "0 0 9 * * 1-5");

// 支持多个Cron表达式，分号分隔
var timer2 = new TimerX(state =>
{
    Console.WriteLine("混合任务");
}, null, "0 0 2 * * 1-5;0 0 3 * * 6");  // 工作日2点，周六3点
```

**自动设置**：
- 使用 Cron 表达式时，自动设置 `Absolutely = true`
- 下一次执行时间由 Cron 计算

---

## 核心特性

### 1. 不可重入机制

系统 `Timer` 的问题：
```csharp
// System.Threading.Timer 的问题
var timer = new Timer(_ =>
{
    Thread.Sleep(3000);  // 假设任务耗时3秒
    Console.WriteLine("执行");
}, null, 0, 1000);  // 每1秒触发

// 结果：可能同时有多个回调在执行，造成并发问题！
```

TimerX 的解决方案：
```csharp
var timer = new TimerX(_ =>
{
    Thread.Sleep(3000);  // 耗时3秒
    Console.WriteLine("执行");
}, null, 0, 1000);

// 执行流程：
// 0秒：开始第1次执行
// 3秒：第1次执行完毕，等待1秒
// 4秒：开始第2次执行
// 7秒：第2次执行完毕，等待1秒
// 8秒：开始第3次执行
// ...
```

**原理**：回调执行完毕后才开始计算下一次的延迟时间，确保同一时刻只有一个回调在执行。

### 2. 基于 TickCount 的精准计时

TimerX 使用 `Runtime.TickCount64` 作为计时基准，无惧系统时间回拨：

```csharp
// 即使手动调整系统时间，TimerX 也不受影响
var timer = new TimerX(_ =>
{
    Console.WriteLine($"执行：{DateTime.Now}");
}, null, 0, 5000);
```

**优势**：
- 使用开机嘀嗒数而非系统时钟
- 不受夏令时、时区调整影响
- 每次 `SetNextTick` 会刷新时间基准，自动修正漂移

### 3. 绝对时间与 Cron

- **`Absolutely = true`**：表示绝对时间执行，不受 `SetNext` 影响
- **Cron 构造函数**：自动设置 `Absolutely = true`，通过 `Cron.GetNext(now)` 计算下一次执行时间

```csharp
// 绝对时间定时器
var timer = new TimerX(_ => { }, null, DateTime.Today.AddHours(2), 24 * 3600 * 1000);
timer.Absolutely;  // true

// Cron 定时器
var timer2 = new TimerX(_ => { }, null, "0 0 2 * * *");
timer2.Absolutely;  // true
timer2.Crons;       // Cron数组
```

---

## 构造函数详解

### 1. 普通周期定时器（同步）

```csharp
public TimerX(TimerCallback callback, Object? state, Int32 dueTime, Int32 period, String? scheduler = null)
```

**参数**：
- `callback`: 回调委托 `void Callback(Object state)`
- `state`: 用户数据
- `dueTime`: 延迟时间（毫秒），首次执行前等待
- `period`: 间隔周期（毫秒），0或-1表示只执行一次
- `scheduler`: 调度器名称，默认使用 `TimerScheduler.Default`

**示例**：
```csharp
var timer = new TimerX(_ =>
{
    Console.WriteLine("执行");
}, null, 1000, 5000);
```

### 2. 异步周期定时器

```csharp
public TimerX(Func<Object, Task> callback, Object? state, Int32 dueTime, Int32 period, String? scheduler = null)
```

**参数**：
- `callback`: 异步回调委托 `async Task Callback(Object state)`
- 其他参数同上

**自动设置**：
- `IsAsyncTask = true`
- `Async = true`

**示例**：
```csharp
var timer = new TimerX(async _ =>
{
    await DoWorkAsync();
}, null, 1000, 5000);
```

### 3. 绝对时间定时器（同步）

```csharp
public TimerX(TimerCallback callback, Object? state, DateTime startTime, Int32 period, String? scheduler = null)
```

**参数**：
- `startTime`: 绝对开始时间，指定时刻执行
- `period`: 间隔周期（毫秒），必须大于0

**自动设置**：
- `Absolutely = true`
- 如果 `startTime` 小于当前时间，自动加 `period` 对齐

**示例**：
```csharp
// 每天凌晨2点执行
var start = DateTime.Today.AddHours(2);
var timer = new TimerX(_ =>
{
    Console.WriteLine("凌晨任务");
}, null, start, 24 * 3600 * 1000);
```

### 4. 绝对时间定时器（异步）

```csharp
public TimerX(Func<Object, Task> callback, Object? state, DateTime startTime, Int32 period, String? scheduler = null)
```

**自动设置**：
- `IsAsyncTask = true`
- `Async = true`
- `Absolutely = true`

### 5. Cron 定时器（同步）

```csharp
public TimerX(TimerCallback callback, Object? state, String cronExpression, String? scheduler = null)
```

**参数**：
- `cronExpression`: Cron 表达式，支持分号分隔多个表达式

**自动设置**：
- `Absolutely = true`
- 解析表达式并保存到 `_crons` 数组

**示例**：
```csharp
// 每个工作日早上9点
var timer = new TimerX(_ =>
{
    Console.WriteLine("工作日任务");
}, null, "0 0 9 * * 1-5");

// 多个时间点
var timer2 = new TimerX(_ =>
{
    Console.WriteLine("混合任务");
}, null, "0 0 2 * * 1-5;0 0 3 * * 6");
```

### 6. Cron 定时器（异步）

```csharp
public TimerX(Func<Object, Task> callback, Object? state, String cronExpression, String? scheduler = null)
```

**自动设置**：
- `IsAsyncTask = true`
- `Async = true`
- `Absolutely = true`

---

## 核心属性

| 属性 | 类型 | 说明 |
|-----|------|------|
| `Id` | Int32 | 定时器唯一标识，自动分配 |
| `Period` | Int32 | 间隔周期（毫秒），0或-1表示只执行一次 |
| `Async` | Boolean | 是否异步执行，默认 false |
| `Absolutely` | Boolean | 是否绝对精确时间执行，默认 false |
| `Calling` | Boolean | 回调是否正在执行（只读） |
| `Timers` | Int32 | 累计调用次数（只读） |
| `Cost` | Int32 | 平均耗时（毫秒，只读） |
| `NextTime` | DateTime | 下一次调用时间（只读） |
| `NextTick` | Int64 | 下一次执行时间的嘀嗒数（只读） |
| `Crons` | Cron[] | Cron 表达式集合（只读） |
| `Tracer` | ITracer | 链路追踪器 |
| `TracerName` | String | 链路追踪名称，默认 `timer:{方法名}` |
| `State` | Object | 用户数据，弱引用存储 |
| `Scheduler` | TimerScheduler | 所属调度器 |

---

## 核心方法

### SetNext - 设置下一次执行时间

```csharp
/// <summary>设置下一次运行时间</summary>
/// <param name="ms">延迟毫秒数。小于等于0表示马上调度</param>
public void SetNext(Int32 ms)
```

**示例**：
```csharp
var timer = new TimerX(_ =>
{
    Console.WriteLine("执行");
    
    // 动态调整下一次执行时间
    if (someCondition)
        timer.SetNext(10000);  // 10秒后执行
    else
        timer.SetNext(1000);   // 1秒后执行
}, null, 1000, 5000);
```

**注意**：
- 对于 `Absolutely = true` 的定时器（绝对时间、Cron），`SetNext` 无效
- 调用后会唤醒调度器立即检查

### Change - 更改计时器参数

```csharp
/// <summary>更改计时器的启动时间和方法调用之间的时间间隔</summary>
/// <param name="dueTime">延迟时间</param>
/// <param name="period">间隔周期</param>
/// <returns>是否成功更改</returns>
public Boolean Change(TimeSpan dueTime, TimeSpan period)
```

**示例**：
```csharp
var timer = new TimerX(_ =>
{
    Console.WriteLine("执行");
}, null, 1000, 5000);

// 修改为2秒后首次执行，然后每10秒一次
timer.Change(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10));
```

**限制**：
- 对于 `Absolutely = true` 的定时器，返回 false，修改失败
- 对于 Cron 定时器，返回 false，修改失败
- `period` 为负数或 Infinite 时，定时器会被销毁

### Dispose - 销毁定时器

```csharp
/// <summary>销毁定时器</summary>
public void Dispose()
```

**示例**：
```csharp
var timer = new TimerX(_ =>
{
    Console.WriteLine("执行");
}, null, 1000, 5000);

// 使用完毕释放
timer.Dispose();
```

**重要性**：
- TimerX 挂载在静态调度器 `TimerScheduler` 上
- **必须手动调用 `Dispose()`** 才能从调度器移除
- 否则会造成内存泄漏和定时器继续执行

---

## 静态方法

### Delay - 延迟执行

```csharp
/// <summary>延迟执行一个委托</summary>
/// <param name="callback">回调委托</param>
/// <param name="ms">延迟毫秒数</param>
/// <returns>定时器实例</returns>
public static TimerX Delay(TimerCallback callback, Int32 ms)
```

**示例**：
```csharp
// 10秒后执行一次，然后销毁
var timer = TimerX.Delay(_ =>
{
    Console.WriteLine("延迟任务执行");
}, 10000);

// 注意：委托可能还没执行，timer对象就被GC回收了
// 建议保持timer引用
```

**注意**：
- 自动设置 `Async = true`
- 仅执行一次（Period=0）
- **警告**：如果不保持 `timer` 引用，可能被 GC 回收导致未执行

### Now - 缓存的当前时间

```csharp
/// <summary>当前时间。定时读取系统时间，避免频繁读取造成性能瓶颈</summary>
public static DateTime Now { get; }
```

**示例**：
```csharp
var now = TimerX.Now;  // 性能优于 DateTime.Now
Console.WriteLine(now);
```

**原理**：
- 每500毫秒更新一次系统时间
- 避免频繁调用 `DateTime.Now` 造成性能瓶颈
- 适用于对时间精度要求不高的场景（±500ms）

---

## 调度器 TimerScheduler

TimerX 依赖 `TimerScheduler` 进行统一调度。

### 默认调度器

```csharp
var timer = new TimerX(_ => { }, null, 1000, 5000);
timer.Scheduler;  // TimerScheduler.Default
```

### 自定义调度器

```csharp
// 创建专属调度器
var timer = new TimerX(_ =>
{
    Console.WriteLine("执行");
}, null, 1000, 5000, "MyScheduler");

timer.Scheduler.Name;  // "MyScheduler"
```

**适用场景**：
- 需要独立的调度线程池
- 隔离不同业务的定时器
- 控制不同调度器的优先级

---

## 链路追踪

TimerX 内置链路追踪支持，自动为每次执行创建 Span。

### 设置追踪器

```csharp
using NewLife.Log;

var timer = new TimerX(_ =>
{
    Console.WriteLine("执行");
}, null, 1000, 5000)
{
    Tracer = DefaultTracer.Instance,  // 设置追踪器
    TracerName = "MyTask"              // 自定义埋点名称
};
```

### 追踪数据

每次定时器触发，会创建一个 Span：
- **名称**：默认为 `timer:{方法名}`，可通过 `TracerName` 自定义
- **标签**：记录定时器ID、周期等信息
- **耗时**：记录每次回调执行的时间

**查看统计**：
```
Tracer[timer:DoWork] Total=100 Errors=0 Speed=0.02tps Cost=150ms MaxCost=200ms MinCost=100ms
```

详见：[链路追踪ITracer](tracer-链路追踪ITracer.md)

---

## 使用场景

### 1. 定期数据清理

```csharp
// 每天凌晨3点清理过期数据
var timer = new TimerX(state =>
{
    var days = (Int32)state!;
    var before = DateTime.Now.AddDays(-days);
    
    var count = Database.Delete("WHERE CreateTime < @time", new { time = before });
    Console.WriteLine($"清理了 {count} 条过期数据");
}, 30, DateTime.Today.AddHours(3), 24 * 3600 * 1000);
```

### 2. 心跳检测

```csharp
var timer = new TimerX(_ =>
{
    foreach (var client in clients)
    {
        if (client.LastActive.AddMinutes(5) < DateTime.Now)
        {
            Console.WriteLine($"客户端 {client.Id} 超时，断开连接");
            client.Disconnect();
        }
    }
}, null, 0, 60000);  // 每分钟检查一次
```

### 3. 定期数据同步

```csharp
var timer = new TimerX(async _ =>
{
    var data = await FetchDataFromApiAsync();
    await SaveToDatabase(data);
    Console.WriteLine("数据同步完成");
}, null, 0, 300000);  // 每5分钟同步一次
```

### 4. 工作日定时报表

```csharp
// 每个工作日早上9点生成报表
var timer = new TimerX(_ =>
{
    var report = GenerateReport(DateTime.Today.AddDays(-1));
    SendEmail(report);
    Console.WriteLine("报表已发送");
}, null, "0 0 9 * * 1-5");
```

### 5. 定期健康检查

```csharp
var timer = new TimerX(_ =>
{
    var healthy = CheckSystemHealth();
    if (!healthy)
    {
        SendAlert("系统异常");
    }
}, null, 0, 30000);  // 每30秒检查一次
```

---

## 最佳实践

### 1. 异步回调优先

对于耗时操作，使用异步回调避免阻塞：

```csharp
// 推荐
var timer = new TimerX(async _ =>
{
    await DoHeavyWorkAsync();
}, null, 0, 5000);

// 不推荐
var timer2 = new TimerX(_ =>
{
    DoHeavyWork();  // 阻塞线程
}, null, 0, 5000);
```

### 2. 主动释放资源

```csharp
public class MyService : IDisposable
{
    private readonly TimerX _timer;
    
    public MyService()
    {
        _timer = new TimerX(_ => DoWork(), null, 0, 5000);
    }
    
    public void Dispose()
    {
        _timer?.Dispose();  // 释放定时器
    }
}
```

### 3. 使用 Current 属性

在回调中可以访问当前定时器：

```csharp
var timer = new TimerX(_ =>
{
    var current = TimerX.Current;
    Console.WriteLine($"定时器[{current.Id}]第{current.Timers}次执行");
    
    // 动态调整周期
    if (current.Timers > 10)
        current.Period = 10000;
}, null, 0, 5000);
```

### 4. 错误处理

```csharp
var timer = new TimerX(_ =>
{
    try
    {
        DoWork();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"定时任务异常：{ex.Message}");
        // 记录日志，不要抛出异常
    }
}, null, 0, 5000);
```

### 5. Cron 复杂场景

```csharp
// 工作日早上9点，周六早上10点
var crons = "0 0 9 * * 1-5;0 0 10 * * 6";
var timer = new TimerX(_ =>
{
    Console.WriteLine("执行任务");
}, null, crons);
```

---

## 注意事项

### 1. 必须主动释放

```csharp
var timer = new TimerX(_ => { }, null, 0, 5000);
// ... 使用 ...
timer.Dispose();  // 必须调用，否则内存泄漏
```

### 2. Delay 方法的陷阱

```csharp
// 错误：timer可能被GC回收
TimerX.Delay(_ => Console.WriteLine("执行"), 10000);

// 正确：保持引用
var timer = TimerX.Delay(_ => Console.WriteLine("执行"), 10000);
// ... 保持timer在作用域内 ...
```

### 3. 绝对时间不可修改

```csharp
var timer = new TimerX(_ => { }, null, DateTime.Today.AddHours(2), 86400000);
timer.SetNext(1000);  // 无效
timer.Change(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));  // 返回 false
```

### 4. State 是弱引用

```csharp
var obj = new MyObject();
var timer = new TimerX(_ =>
{
    var state = _.State as MyObject;  // 可能为 null
}, obj, 0, 5000);

// 如果 obj 被 GC 回收，State 会变为 null
```

### 5. 周期为0只执行一次

```csharp
var timer = new TimerX(_ =>
{
    Console.WriteLine("只执行一次");
}, null, 1000, 0);  // Period=0，只执行一次
```

---

## 常见问题

### 1. 定时器不执行？

检查：
- 是否被 GC 回收（保持引用）
- 是否已 Dispose
- Period 是否为负数
- Cron 表达式是否正确

### 2. 定时器执行了多次？

TimerX 是不可重入的，不会出现这个问题。如果出现，检查是否创建了多个定时器实例。

### 3. 如何停止定时器？

```csharp
timer.Dispose();  // 销毁并移除
```

### 4. 如何暂停和恢复？

```csharp
// 暂停（设置为只执行一次）
timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

// 恢复
timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(5));
```

### 5. Cron 和绝对时间的区别？

| 特性 | Cron | 绝对时间 |
|-----|------|---------|
| 表达式 | 支持复杂规则 | 固定时刻+周期 |
| 灵活性 | 高（秒级、星期等） | 低（固定周期） |
| 性能 | 略低（需解析） | 高 |
| 适用场景 | 复杂定时 | 简单周期 |

---

## 参考资料

- **Cron 文档**: [cron-Cron表达式.md](cron-Cron表达式.md)
- **链路追踪**: [tracer-链路追踪ITracer.md](tracer-链路追踪ITracer.md)
- **在线文档**: https://newlifex.com/core/timerx
- **源码**: https://github.com/NewLifeX/X/blob/master/NewLife.Core/Threading/TimerX.cs

---

## 更新日志

- **2025-01**: 完善文档，补充详细示例和最佳实践
- **2024**: 支持 .NET 9.0，优化性能
- **2023**: 增加 Cron 多表达式支持
- **2022**: 增加异步回调支持
- **2020**: 初始版本，基于 TickCount 的精准计时
