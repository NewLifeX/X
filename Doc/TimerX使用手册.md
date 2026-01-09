# TimerX 使用手册

`NewLife.Threading.TimerX` 是一个不可重入的定时器实现，支持毫秒周期、绝对时间对齐、以及 Cron 表达式调度。

- 源码：`NewLife.Core\Threading\TimerX.cs`
- 命名空间：`NewLife.Threading`

---

## 1. 适用场景

- 需要避免系统 `Timer` 在回调耗时较长时的“并发重入”
- 需要以“任务执行结束”为基准计算下一次间隔
- 需要以固定时刻对齐（绝对时间）执行
- 需要基于 Cron 表达式执行（可多个表达式取最近触发）

> 注意：`TimerX` 会被 `TimerScheduler` 持有（挂在静态列表上）。用完必须 `Dispose()`，否则可能导致对象长期存活与任务持续执行。

---

## 2. 核心概念

### 2.1 不可重入

`TimerX` 的调度策略是“回调执行完毕后再计算下一次间隔”，从根源上避免并行重入。

### 2.2 调度基准（Tick）与时间回拨

`TimerX` 使用 `Runtime.TickCount64` 作为调度基准，通过 `_baseTime + _nextTick` 映射到 `NextTime`：

- 优点：不受系统时间回拨影响（相对时钟）
- 每次 `SetNextTick` 会刷新 `_baseTime = Scheduler.GetNow() - tick`，减少漂移

### 2.3 绝对时间与 Cron

- `Absolutely=true`：按“绝对时间点”执行（与 `SetNext` 无关）
- Cron 构造函数会自动开启 `Absolutely=true`，并通过 `Cron.GetNext(now)` 计算下一次触发

---

## 3. 构造函数与用法

### 3.1 普通周期定时器（毫秒）

```csharp
var timer = new TimerX(_ =>
{
    // 同步回调
}, state: null, dueTime: 1000, period: 5000);

// ...使用...

timer.Dispose();
```

参数含义：

- `dueTime`：首次延迟（毫秒）
- `period`：后续周期（毫秒）；`0` 或 `-1` 表示只执行一次

### 3.2 异步周期定时器

```csharp
var timer = new TimerX(async _ =>
{
    await Task.Delay(100);
}, state: null, dueTime: 1000, period: 5000);
```

该构造函数会自动设置：

- `IsAsyncTask = true`
- `Async = true`

### 3.3 绝对时间定时器（对齐到固定时间点）

```csharp
var start = DateTime.Today.AddHours(2);
var timer = new TimerX(_ =>
{
    // 每天 02:00 执行
}, state: null, startTime: start, period: 24 * 3600 * 1000);
```

特点：

- 若 `startTime` 早于当前时间，会不断加 `period` 直到大于当前时间
- 适合“整点执行/固定时刻执行”的需求

### 3.4 Cron 定时器（支持多个表达式）

```csharp
var timer = new TimerX(_ =>
{
    // 业务逻辑
}, state: null, cronExpression: "0 0 2 * * 1-5;0 0 3 * * 6");
```

规则：

- 多表达式用 `;` 分隔
- 内部会解析为 `Cron[]`（`Crons` 属性可读）
- 下一次执行时间取 `Min(cron.GetNext(now))`

异步 Cron：

```csharp
var timer = new TimerX(async _ =>
{
    await Task.Delay(100);
}, state: null, cronExpression: "0 */1 * * * *");
```

---

## 4. 重要属性/方法

### 4.1 关键属性

- `Int32 Id`：定时器编号，由调度器分配
- `TimerScheduler Scheduler`：所属调度器
- `Object? State`：用户数据（弱引用存储）
- `Int32 Period`：周期毫秒数
- `Boolean Async`：是否异步执行任务
- `Boolean Absolutely`：是否按绝对时间执行
- `Cron[]? Crons`：Cron 表达式集合（Cron 模式才有）
- `DateTime NextTime` / `Int64 NextTick`：下一次调度时间（分别是实际时间 / tick）

> `State` 使用 `WeakReference` 保存：如果调用方未持有 `State` 引用，可能被 GC 回收。

### 4.2 设置下一次执行：`SetNext`

`SetNext(Int32 ms)` 可以手工设置下一次执行时间（相对当前 tick），并唤醒调度器：

```csharp
timer.SetNext(500);
```

注意：

- 仅影响“普通周期定时器”路径（即 `Absolutely=false` 且非 Cron）更符合直觉
- `Absolutely=true`/Cron 的下一次时间由内部绝对时间计算逻辑决定

### 4.3 修改周期：`Change`

`Change(TimeSpan dueTime, TimeSpan period)` 仅对“普通周期定时器”有效：

- 若 `Absolutely=true`，返回 `false`
- 若存在 `Crons`，返回 `false`

```csharp
var ok = timer.Change(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));
```

---

## 5. 关键行为说明（避免踩坑）

### 5.1 必须 `Dispose`

`TimerX` 实例会被 `TimerScheduler` 引用。

- 不 `Dispose`：可能导致定时器一直存在并持续触发
- 推荐：用 `using var timer = ...` 或放到宿主生命周期里统一释放

### 5.2 Cron 下一次时间过近的处理

在 `SetAndGetNextTime` 的 Cron 分支中：

- 先算 `next = Min(GetNext(now))`
- 若 `(next - now) < 1000ms`，则再算一次 `GetNext(next)`

目的：避免“刚好命中/过近”导致重复触发。

### 5.3 任务不要太多

`TimerX` 的设计适合少量高价值定时任务。

- 如果挂载大量任务，单个调度器线程可能会成为瓶颈
- 复杂调度建议上层构建任务队列，回调中仅投递，不做重活

---

## 6. 示例与测试参考

仓库内可参考测试：

- `XUnitTest.Core\Threading\TimerXTests.cs`
  - `NormalTest`：普通周期
  - `AsyncTest`：异步回调
  - `AbsolutelyTest`：绝对时间
  - `CronTest`：Cron 表达式（含多表达式 `;`）

---

## 7. 相关链接

- 文档：<https://newlifex.com/core/timerx>
