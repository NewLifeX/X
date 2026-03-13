# 时间提供者TimeProvider

## 概述

`TimeProvider` 是 NewLife.Core 对 .NET 系统时钟的统一封装与扩展，面向"可替换时钟"场景。在 .NET 8+ 上它直接适配官方接口 `System.TimeProvider`，在低版本上使用等效的内部实现，保证跨框架行为一致。通过将 `TimeProvider.System` 替换为测试用假时钟（`FakeTimeProvider`），可以在单元测试中精确控制时间推进，无需修改业务代码。

**命名空间**：`NewLife`  
**文档地址**：/core/time_provider

## 核心特性

- **跨框架统一**：支持 .NET Framework 4.5 ～ .NET 10，通过条件编译在高版本复用官方 API
- **可替换时钟**：注入 `TimeProvider` 依赖而非直接调用 `DateTime.UtcNow`，单元测试可精确控制时间
- **高精度时间戳**：`GetTimestamp()` 使用 `Stopwatch.GetTimestamp()` 底层，分辨率可达纳秒
- **时区支持**：`LocalTimeZone` 可重写，用于需要统一打标时区的场景（如跨时区日志系统）
- **运行时间测量**：`GetElapsedTime(start)` 配合 `GetTimestamp()` 替代 `Stopwatch`，减少对象分配

## API 参考

### 静态属性

```csharp
/// <summary>系统默认时间提供者（单例）。可替换为 FakeTimeProvider 进行单元测试</summary>
public static TimeProvider System { get; set; }

/// <summary>本地时区。默认 TimeZoneInfo.Local，可覆写以统一时区</summary>
public virtual TimeZoneInfo LocalTimeZone { get; }

/// <summary>时间戳频率（每秒 Tick 数）。Stopwatch.Frequency，通常为 10_000_000</summary>
public virtual Int64 TimestampFrequency { get; }
```

### 实例方法

```csharp
/// <summary>获取当前 UTC 时间</summary>
public virtual DateTimeOffset GetUtcNow();

/// <summary>获取当前本地时间（依据 LocalTimeZone 转换）</summary>
public virtual DateTimeOffset GetLocalNow();

/// <summary>获取当前高精度时间戳（Stopwatch Tick，非 DateTime.Ticks）</summary>
public virtual Int64 GetTimestamp();

/// <summary>计算从 startingTimestamp 到现在经过的时间</summary>
/// <param name="startingTimestamp">由 GetTimestamp() 获得的起始时间戳</param>
public virtual TimeSpan GetElapsedTime(Int64 startingTimestamp);

/// <summary>计算两个时间戳之间的时间差</summary>
/// <param name="startingTimestamp">起始时间戳</param>
/// <param name="endingTimestamp">结束时间戳</param>
public virtual TimeSpan GetElapsedTime(Int64 startingTimestamp, Int64 endingTimestamp);
```

## 快速开始

### 替代直接调用系统时钟

```csharp
// ❌ 难以测试的写法
var now = DateTime.UtcNow;

// ✅ 可注入的写法
public class OrderService
{
    private readonly TimeProvider _timeProvider;

    public OrderService(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public void CreateOrder(Order order)
    {
        order.CreatedAt = _timeProvider.GetUtcNow().UtcDateTime;
    }
}
```

### 测量代码耗时

```csharp
var tp    = TimeProvider.System;
var start = tp.GetTimestamp();

// 执行业务逻辑
await DoWorkAsync();

var elapsed = tp.GetElapsedTime(start);
Console.WriteLine($"耗时: {elapsed.TotalMilliseconds:F1} ms");
```

### 在单元测试中控制时间（.NET 8+）

```csharp
using Microsoft.Extensions.Time.Testing;

[Fact]
public void Order_CreatedAt_UsesInjectedClock()
{
    var fakeTime = new FakeTimeProvider(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));
    var service  = new OrderService(fakeTime);

    service.CreateOrder(new Order());

    // 推进假时钟 1 小时
    fakeTime.Advance(TimeSpan.FromHours(1));

    // 断言时间精确可控
    Assert.Equal(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), order.CreatedAt);
}
```

### 在低版本中使用自定义时钟

```csharp
// 继承实现自定义时间提供者
public class FrozenTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _frozen;

    public FrozenTimeProvider(DateTimeOffset frozen) => _frozen = frozen;

    public override DateTimeOffset GetUtcNow() => _frozen;
}

// 测试中替换全局默认时钟
TimeProvider.System = new FrozenTimeProvider(new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero));
// 记得测试后还原：TimeProvider.System = <original>;
```

## 条件编译说明

`TimeProvider` 存在两套实现路径：

```csharp
#if NET8_0_OR_GREATER
    // 直接继承/包装 System.TimeProvider，完整支持官方 FakeTimeProvider
    public class TimeProvider : System.TimeProvider { ... }
#else
    // newlife 自实现，API 保持兼容
    public abstract class TimeProvider
    {
        public abstract DateTimeOffset GetUtcNow();
        public virtual DateTimeOffset GetLocalNow() { ... }
        public virtual Int64 GetTimestamp() => Stopwatch.GetTimestamp();
        public virtual TimeSpan GetElapsedTime(Int64 startingTimestamp) { ... }
    }
#endif
```

这意味着：
- `.NET 8+`：可直接使用 `Microsoft.Extensions.Time.Testing.FakeTimeProvider`（来自 `Microsoft.Extensions.TimeProvider.Testing` NuGet）
- 低版本：继承 `NewLife.TimeProvider` 实现自定义假时钟

## 使用场景

### 场景一：后台任务的定期触发

```csharp
public class BackgroundWorker
{
    private readonly TimeProvider _clock;
    private DateTimeOffset _lastRun;

    public BackgroundWorker(TimeProvider clock)
    {
        _clock = clock;
        _lastRun = clock.GetUtcNow();
    }

    public void Tick()
    {
        if (_clock.GetUtcNow() - _lastRun >= TimeSpan.FromMinutes(5))
        {
            DoPeriodicWork();
            _lastRun = _clock.GetUtcNow();
        }
    }
}
```

### 场景二：Token 过期检查

```csharp
public Boolean IsExpired(String token)
{
    var expireAt = ParseExpiry(token);
    return _timeProvider.GetUtcNow() > expireAt;
}
```

## 最佳实践

- **构造函数注入，默认用 `System`**：`TimeProvider? tp = null`  `_clock = tp ?? TimeProvider.System`，既方便测试又不强依赖 DI。
- **避免 `DateTime.Now` / `DateTime.UtcNow` 混用**：统一走 `GetUtcNow()`，时区转换统一在呈现层处理。
- **高精度计时用 `GetTimestamp()`**：比 `Stopwatch.StartNew()` 减少一次对象分配。
- **生产环境不替换全局 `System`**：全局替换会影响所有模块的时钟，应改为依赖注入。
