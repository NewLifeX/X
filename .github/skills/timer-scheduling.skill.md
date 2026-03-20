---
name: timer-scheduling
description: 使用 NewLife TimerX 和 Cron 实现高精度定时任务和 Cron 调度
---

# NewLife 定时任务与调度使用指南

## 适用场景

- 周期性后台任务
- Cron 表达式定时调度
- 延迟执行一次性任务
- 高精度定时器
- 替代 System.Threading.Timer

## TimerX 基本用法

### 周期定时器

```csharp
// 同步回调：延迟 1 秒启动，每 60 秒执行一次
var timer = new TimerX(DoWork, null, 1_000, 60_000);

void DoWork(Object? state)
{
    // 定时执行的逻辑
}

// 异步回调
var timer = new TimerX(async state =>
{
    await SyncDataAsync();
}, null, 1_000, 60_000);

// 释放定时器
timer.Dispose();
```

### 延迟执行（一次性）

```csharp
// 延迟 5 秒执行一次（period = 0 表示不重复）
var timer = new TimerX(state =>
{
    SendNotification();
}, null, 5_000, 0);
```

### 绝对时间触发

```csharp
// 从明天凌晨 2 点开始，每 24 小时执行一次
var startTime = DateTime.Today.AddDays(1).AddHours(2);
var timer = new TimerX(DailyCleanup, null, startTime, 24 * 3600 * 1000);
```

## Cron 表达式调度

### 表达式格式

```text
秒 分 时 天 月 星期 [年]
```

| 位置 | 字段 | 范围 | 特殊字符 |
| ---- | ---- | ---- | -------- |
| 1 | 秒 | 0-59 | , - * / |
| 2 | 分 | 0-59 | , - * / |
| 3 | 时 | 0-23 | , - * / |
| 4 | 天 | 1-31 | , - * / |
| 5 | 月 | 1-12 | , - * / |
| 6 | 星期 | 0-6 | , - * / |

### 常用示例

```csharp
// 每分钟执行
var timer = new TimerX(DoWork, null, "0 * * * * *");

// 每天凌晨 3 点
var timer = new TimerX(DoWork, null, "0 0 3 * * *");

// 每小时第 30 分钟
var timer = new TimerX(DoWork, null, "0 30 * * * *");

// 每周一和周五 9 点
var timer = new TimerX(DoWork, null, "0 0 9 * * 1,5");

// 每 5 分钟
var timer = new TimerX(DoWork, null, "0 */5 * * * *");

// 工作日 9-18 点每小时
var timer = new TimerX(DoWork, null, "0 0 9-18 * * 1-5");

// 多个 Cron 表达式（分号分隔）
var timer = new TimerX(DoWork, null, "0 0 9 * * *;0 0 18 * * *");
```

### Cron 类直接使用

```csharp
var cron = new Cron("0 0 3 * * *");

// 判断当前时间是否匹配
if (cron.IsTime(DateTime.Now)) { /* 执行 */ }

// 获取下一次执行时间
var next = cron.GetNext(DateTime.Now);
XTrace.WriteLine("下次执行：{0}", next);
```

## 在 IHostedService 中使用

```csharp
public class ScheduleService : IHostedService
{
    private TimerX? _syncTimer;
    private TimerX? _cleanupTimer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // 每 30 秒同步数据
        _syncTimer = new TimerX(SyncData, null, 1_000, 30_000);

        // 每天凌晨 2 点清理
        _cleanupTimer = new TimerX(Cleanup, null, "0 0 2 * * *");

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _syncTimer.TryDispose();
        _cleanupTimer.TryDispose();
        return Task.CompletedTask;
    }

    private void SyncData(Object? state) { /* ... */ }
    private void Cleanup(Object? state) { /* ... */ }
}
```

## 高级特性

### 异步执行

```csharp
// Async = true 时回调在线程池中异步执行，不阻塞定时器调度
var timer = new TimerX(DoWork, null, 1_000, 60_000) { Async = true };
```

### 绝对时间模式

```csharp
// Absolutely = true 时基于绝对时间计算下次执行，不受实际执行耗时影响
var timer = new TimerX(DoWork, null, 1_000, 60_000) { Absolutely = true };
```

### 链路追踪

```csharp
var timer = new TimerX(DoWork, null, 1_000, 60_000)
{
    Tracer = tracer,
    TracerName = "DataSync",  // Span 名称
};
```

### 状态查询

```csharp
timer.NextTime    // 下次执行时间
timer.Timers      // 已执行次数
timer.Cost        // 最近执行耗时（毫秒）
timer.Calling     // 是否正在执行
timer.Period      // 执行间隔（可动态调整）
```

## 注意事项

- TimerX 默认**同步串行**执行，上次未完成则跳过本次
- 设置 `Async = true` 允许异步并发执行
- `Period = 0` 表示只执行一次
- Cron 支持秒级调度（标准 cron 只支持分钟级）
- 多个 Cron 表达式用分号分隔
- 定时器回调中的异常会被捕获并记录日志，不会中断定时器
- 务必在 `StopAsync` 或 `Dispose` 中释放定时器
