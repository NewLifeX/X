# 延迟队列DeferredQueue

## 概述

`DeferredQueue` 是 NewLife.Core 提供的高性能延迟批处理队列，通过"先聚合后批量处理"模式大幅降低高频写操作对外部系统（数据库、消息队列、外部 API）的压力。其核心思路是用 `ConcurrentDictionary` 缓存待处理对象，通过 `TimerX` 定时触发批量处理，相同键的多次写入在同一周期内自动合并为一条。

**命名空间**：`NewLife.Model`  
**文档地址**：https://newlifex.com/core/deferred_queue

## 核心特性

- **写入合并**：同一 key 多次写入只保留最新版本，天然防止冗余更新
- **周期批处理**：定时触发，支持自定义处理间隔（默认 10 秒）
- **批大小控制**：单批最多处理 N 条，防止单次操作超时
- **流量背压**：队列超限时阻塞等待，保护外部系统
- **借出-提交模式**：`GetOrAdd` + `Commit` 支持先借出对象修改再归还，处理器等待修改完成后才消费
- **安全退出**：`Dispose` 时自动执行 `Flush`，避免关闭服务时丢失尾批数据
- **链路追踪**：内置 `Tracer?.NewSpan` 埋点，支持 APM 监控

## 快速开始

```csharp
using NewLife.Model;

var dq = new DeferredQueue
{
    Name      = "OrderSave",
    Period    = 5_000,      // 每5秒触发一次
    BatchSize = 1_000,      // 每批最多1000条
    Finish    = list => SaveBatch(list),
    Error     = (list, ex) => XTrace.WriteException(ex),
};

// 高频写入（相同 orderId 同一周期内自动合并）
dq.TryAdd(order.Id.ToString(), order);

// 程序退出前
using (dq) { }  // Dispose 自动 Flush
```

## API 参考

### 属性

```csharp
/// <summary>名称。用于日志和调试</summary>
public String Name { get; set; }

/// <summary>周期（毫秒）。定时处理间隔，默认 10_000</summary>
public Int32 Period { get; set; } = 10_000;

/// <summary>最大个数。超过后 TryAdd 将阻塞等待消费，默认 10_000_000</summary>
public Int32 MaxEntity { get; set; } = 10_000_000;

/// <summary>批大小。每次最多处理的对象数，默认 5_000</summary>
public Int32 BatchSize { get; set; } = 5_000;

/// <summary>等待借出对象确认修改的时间（毫秒），默认 3_000</summary>
public Int32 WaitForBusy { get; set; } = 3_000;

/// <summary>跟踪数。达到该值时输出跟踪日志，默认 1000</summary>
public Int32 TraceCount { get; set; } = 1000;

/// <summary>是否异步调度。true=共用 DQ 调度线程；false=独立线程，默认 true</summary>
public Boolean Async { get; set; } = true;

/// <summary>保存速度（每秒处理对象数），只读</summary>
public Int32 Speed { get; }

/// <summary>合并保存的总次数，只读</summary>
public Int32 Times { get; }

/// <summary>当前缓存个数，只读</summary>
public Int32 Count { get; }
```

### 回调

```csharp
/// <summary>批次处理成功回调</summary>
public Action<IList<Object>>? Finish;

/// <summary>批次处理失败回调</summary>
public Action<IList<Object>, Exception>? Error;

/// <summary>队列溢出通知，参数为当前缓存个数</summary>
public Action<Int32>? Overflow;
```

### 方法

#### TryAdd - 写入（最常用）

```csharp
public virtual Boolean TryAdd(String key, Object value)
```

将对象加入队列。相同 key 已存在时返回 `false`（不覆盖）。  
首次调用时自动初始化内部定时器。

```csharp
// 设备每次上报，相同设备ID同一周期内只落库一次
dq.TryAdd(device.Id.ToString(), device);
```

#### GetOrAdd - 借出修改模式

```csharp
public virtual T? GetOrAdd<T>(String key, Func<String, T>? valueFactory = null) where T : class, new()
```

从队列中借出对象直接修改，借出期间批处理会等待。使用完毕**必须**调用 `Commit` 归还。

```csharp
var stat = dq.GetOrAdd<DeviceStat>(deviceId.ToString());
if (stat != null)
{
    stat.Count++;
    dq.Commit(deviceId.ToString());  // 必须归还
}
```

#### Commit - 提交修改

```csharp
public virtual void Commit(String key)
```

通知队列对象修改完毕，可以开始消费该对象。

#### TryRemove - 移除

```csharp
public virtual Boolean TryRemove(String key)
```

从队列中移除指定对象，适合"取消待处理"的场景。

#### Trigger - 立即触发

```csharp
public void Trigger()
```

立即触发一次批处理，无需等待下一个 Period，适合"紧急落库"场景。

#### Flush - 同步清空

```csharp
public void Flush()
```

同步清空并处理当前所有缓存，关闭服务前调用可零丢失。`Dispose` 内部会自动调用。

## 使用模式

### 模式一：高频设备状态落库

```csharp
var dq = new DeferredQueue
{
    Name      = "DeviceStatus",
    Period    = 3_000,
    BatchSize = 500,
    Finish    = list =>
    {
        var devices = list.Cast<DeviceStatus>().ToList();
        DeviceStatus.Upsert(devices);  // 批量 UPSERT
    },
    Error = (list, ex) => XTrace.WriteException(ex),
};

// 设备每100ms上报一次
public void OnReport(DeviceStatus status)
{
    if (!dq.TryAdd(status.DeviceId.ToString(), status))
    {
        // 已有旧值，借出改值
        var old = dq.GetOrAdd<DeviceStatus>(status.DeviceId.ToString());
        if (old != null)
        {
            old.Temperature = status.Temperature;
            old.Online      = status.Online;
            dq.Commit(status.DeviceId.ToString());
        }
    }
}
```

### 模式二：统计计数聚合

```csharp
var dq = new DeferredQueue
{
    Name      = "PageView",
    Period    = 60_000,
    BatchSize = 10_000,
    Finish    = list => PageViewStat.BatchMerge(list.Cast<PageViewStat>().ToList()),
};

// 极高频 PV 计数（原子累加）
public void IncrPageView(String page)
{
    var stat = dq.GetOrAdd<PageViewStat>(page, k => new PageViewStat { Page = k });
    if (stat != null)
    {
        Interlocked.Increment(ref stat._count);
        dq.Commit(page);
    }
}
```

### 模式三：继承覆写批处理逻辑

```csharp
public class OrderDeferredQueue : DeferredQueue
{
    protected override Int32 ProcessAll(ICollection<Object> list)
    {
        var orders = list.Cast<Order>().ToList();
        return Order.BulkUpsert(orders);
    }
}
```

## 最佳实践

- **`Finish` 使用批量接口**：`BulkInsert/Upsert` 而非逐条 Save，是获得高吞吐的关键。
- **`Error` 回调不能缺失**：否则批处理失败静默丢失数据。
- **`GetOrAdd` 配对 `Commit`**：忘记 `Commit` 导致 `_busy` 不归零，批处理等待后强制消费，可能丢失中间修改。
- **`Overflow` 触发告警**：队列超限说明处理速度跟不上写入，需检查 `Finish` 耗时或扩容外部系统。
- **服务关闭前 Dispose 或 Flush**：避免进程退出时最后一批数据未落库。
- **高耗时场景用独立线程**：设置 `Async = false`，防止阻塞其他定时任务。
