# 设备数据从 MQ 分发到 WebSocket

物联网场景中，设备采集数据写入 Redis 消息队列（`RedisStream`），前端通过 WebSocket 实时订阅。本文介绍如何用 **单消费者 + 内存事件分发** 模式，以最少的 Redis 连接支撑大量 WebSocket 推送。

---

## 1. 问题与方案

### 1.1 朴素方案的缺陷

每个 WebSocket 连接各自创建一个 `RedisStream` 消费者：

```text
WebSocket A → RedisStream 消费者 → Redis 长连接
WebSocket B → RedisStream 消费者 → Redis 长连接
WebSocket C → RedisStream 消费者 → Redis 长连接
…（成千上万）
```

`RedisStream` 消费使用长连接，WebSocket 数量膨胀后迅速耗尽 Redis 连接池，系统崩溃。

### 1.2 单消费者 + EventHub 内存分发

每个进程只启动 **一个 MQ 消费者**（一条 Redis 长连接），消费到数据后由 `EventHub` 在内存中按设备路由，分发给同进程内的 WebSocket 处理器：

```text
Redis MQ（1 条长连接 / 进程）
    ↓
DataConsumer（单例 IHostedService）
    ↓
EventHub<DeviceDataDTO>（按 topic=DeviceId 路由）
    ↓
EventBus<DeviceDataDTO>（同设备共享总线）
    ↙        ↓        ↘
WebSocket A  B  C …（IEventHandler）
```

Redis 连接数 = 进程数，与 WebSocket 连接数完全解耦。

---

## 2. 整体架构

```text
┌──────────────────────────────────────────────────────┐
│  ASP.NET Core 进程                                    │
│                                                       │
│  DataConsumer（Singleton + IHostedService）            │
│    ├─ ICacheProvider → RedisStream<DeviceDataDTO>     │
│    ├─ ConsumeAsync(OnConsume) 后台消费循环             │
│    └─ EventHub<DeviceDataDTO> 内存路由                │
│         │                                             │
│         │ DispatchAsync(topic=DeviceId)               │
│         ↓                                             │
│    EventBus（每设备一个共享实例）                       │
│      ├─ Controller A → ws.SendAsync → 客户端 A       │
│      ├─ Controller B → ws.SendAsync → 客户端 B       │
│      └─ Controller C → ws.SendAsync → 客户端 C       │
│                                                       │
└──────────────────────────────────────────────────────┘
         ↑
   Redis MQ（队列：DeviceData，消费组=机器名）
```

**组件职责**：

| 组件 | 职责 |
|------|------|
| `DataConsumer` | 单例托管服务，持有唯一 Redis 长连接，消费 `DeviceData` 队列 |
| `EventHub<DeviceDataDTO>` | 按 `topic`（DeviceId）路由，管理每设备的事件总线 |
| `EventBus<DeviceDataDTO>` | 同设备共享的内存总线，广播给所有订阅该设备的处理器 |
| `DeviceDataController` | WebSocket 端点，实现 `IEventHandler<DeviceDataDTO>`，收到事件后推送 JSON |

---

## 3. 数据模型

设备数据报文使用 `IoT.Data.DeviceDataDTO`，队列名称固定为 `DeviceData`：

```csharp
namespace IoT.Data;

/// <summary>设备数据。设备采集原始数据，按天分表存储</summary>
public partial class DeviceDataDTO : IDeviceData
{
    /// <summary>编号</summary>
    public Int64 Id { get; set; }

    /// <summary>设备</summary>
    public Int32 DeviceId { get; set; }

    /// <summary>名称。MQTT的Topic，或者属性名</summary>
    public String Name { get; set; }

    /// <summary>数值</summary>
    public String Value { get; set; }

    /// <summary>时间戳。设备生成数据时的UTC毫秒</summary>
    public Int64 Timestamp { get; set; }
}
```

---

## 4. MQ 消费者

`DataConsumer` 作为 `IHostedService` 单例注册，职责：以本机计算机名为消费组获取 `RedisStream<DeviceDataDTO>`，调用 `ConsumeAsync` 注册回调，回调中按 `DeviceId` 路由——无订阅直接丢弃，有订阅才分发。

```csharp
/// <summary>设备数据 MQ 消费者（每进程单例）</summary>
public class DataConsumer(ICacheProvider cacheProvider) : IHostedService
{
    #region 属性
    /// <summary>内存事件枢纽</summary>
    public EventHub<DeviceDataDTO> Hub { get; } = new();

    private RedisStream<DeviceDataDTO>? _redisStream;
    private CancellationTokenSource _source = new();
    #endregion

    #region IHostedService
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Hub.Log = XTrace.Log;

        // 以本机计算机名为消费组，每台服务器独立全量消费
        var group = Environment.MachineName;

        // RedisStream 内部处理反序列化，回调直接拿到强类型对象
        _redisStream = cacheProvider.GetQueue<DeviceDataDTO>("DeviceData", group) as RedisStream<DeviceDataDTO>;

        // ConsumeAsync 启动后台循环，每条消息回调 OnConsume
        _redisStream?.ConsumeAsync(OnConsume, _source.Token);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _source.Cancel();
        return Task.CompletedTask;
    }
    #endregion

    #region 消费回调
    private async void OnConsume(DeviceDataDTO data)
    {
        var topic = data.DeviceId.ToString();

        // 无订阅直接丢弃，避免无意义分发
        if (!Hub.TryGetBus<DeviceDataDTO>(topic, out _)) return;

        var clientId = Environment.MachineName;
        try
        {
            await Hub.DispatchAsync(topic, clientId, data).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Hub.WriteLog("分发异常：{0}", ex.Message);
        }
    }
    #endregion
}
```

> `ConsumeAsync` 内部已处理重试与异常，无需手动写消费循环。`async void` 是回调签名要求，异常需在方法内捕获。

---

## 5. WebSocket 控制器

控制器直接实现 `IEventHandler<DeviceDataDTO>`，WebSocket 连接时订阅设备事件总线，断开时取消订阅：

```csharp
[ApiController]
[Route("ws")]
public class DeviceDataController(DataConsumer consumer) : ControllerBase, IEventHandler<DeviceDataDTO>
{
    private WebSocket? _ws;
    private readonly TaskCompletionSource<Boolean> _closed = new();

    /// <summary>订阅设备实时数据推送</summary>
    /// <param name="deviceId">设备编号，用作事件 topic</param>
    [HttpGet("data/{deviceId:int}")]
    public async Task GetData(Int32 deviceId)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = 400;
            return;
        }

        using var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();
        _ws = ws;

        var clientId = Guid.NewGuid().ToString("N")[..8];
        var topic = deviceId.ToString();

        var bus = consumer.Hub.GetEventBus(topic, clientId);
        bus.Subscribe(this, clientId);

        try
        {
            await WaitForCloseAsync(HttpContext.RequestAborted).ConfigureAwait(false);
        }
        finally
        {
            bus.Unsubscribe(clientId);
            _ws = null;
        }
    }

    private Task WaitForCloseAsync(CancellationToken cancellationToken)
    {
        cancellationToken.Register(() => _closed.TrySetResult(false));
        return _closed.Task;
    }

    /// <summary>收到设备数据时推送 JSON 给 WebSocket 客户端</summary>
    public async Task HandleAsync(DeviceDataDTO @event, IEventContext? context, CancellationToken cancellationToken)
    {
        var ws = _ws;
        if (ws == null || ws.State != WebSocketState.Open)
        {
            _closed.TrySetResult(true);
            return;
        }

        try
        {
            var json = @event.ToJson();
            var buffer = Encoding.UTF8.GetBytes(json);
            await ws.SendAsync(new ArraySegment<Byte>(buffer), WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
        }
        catch (WebSocketException)
        {
            _closed.TrySetResult(true);
        }
    }
}
```

---

## 6. 服务注册（Program.cs）

```csharp
var builder = WebApplication.CreateBuilder(args);

// 注册 Redis（NewLife.Redis 扩展包）
builder.Services.AddRedis("127.0.0.1:6379");

// 单例 + 托管服务，整个进程只占一条 Redis 长连接
builder.Services.AddSingleton<DataConsumer>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<DataConsumer>());

builder.Services.AddControllers();

var app = builder.Build();

app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(30) });
app.MapControllers();
app.Run();
```

---

## 7. 关键机制

### 7.1 消费组命名与多服务器部署

RedisStream 的消费组决定了消息分发策略。不同服务器必须使用不同消费组名，否则消息被 Redis 轮流派发，客户端只收到部分数据。

```csharp
// 推荐用机器名（短且唯一）
var group = Environment.MachineName;

// 或用局域网 IP
var group = NetHelper.MyIP()?.ToString() ?? Environment.MachineName;
```

| 部署场景 | 消费组命名 | 效果 |
|---------|-----------|------|
| 单台服务器 | `MACHINE-A` | 全量消费 |
| 多台负载均衡 | 各用机器名 | 每台**独立全量消费**，各自分发给本机 WebSocket |
| 多台共用组名 | `myapp`（❌） | 消息**轮流派发**，客户端只收到部分数据 |

> **原则**：广播场景（每台服务器都需要完整消息）用不同消费组名；竞争消费场景（消息只需处理一次）才用相同组名。

### 7.2 同设备多连接共享事件总线

`EventHub` 按 `topic`（DeviceId）管理事件总线，同一设备的所有 WebSocket 连接共享同一个 `IEventBus<DeviceDataDTO>` 实例：

```text
1 个设备 (topic) → 1 个 EventBus → N 个 WebSocket 处理器
```

- `DataConsumer.OnConsume` 调用 `Hub.DispatchAsync(topic, ...)` 时，`EventHub` 按 `topic` 定位总线
- 各 WebSocket 通过 `Hub.GetEventBus(topic, clientId)` 拿到同一个总线实例
- 消息由该总线广播给该设备下所有订阅连接

### 7.3 无订阅即丢弃

消费回调中通过 `TryGetBus` 检查是否有订阅者，无则直接丢弃。大量未被关注的设备数据在入口即被过滤，不产生任何分发开销。

不使用 `QueueEventBus` 缓冲：本场景中未订阅的数据本就应丢弃，缓冲只会占用额外内存。`EventHub<DeviceDataDTO>` 直接分发最合适。

### 7.4 订阅生命周期

| 时机 | 操作 |
|------|------|
| WebSocket 建立 | `bus.Subscribe(this, clientId)` |
| 正常断开 | `finally` 块 `bus.Unsubscribe(clientId)` |
| 异常断开 | `HandleAsync` 捕获 `WebSocketException` → `_closed.TrySetResult` → 触发 `finally` |
| topic 无任何订阅者 | `EventBus` 自动清空；`EventHub` 移除总线缓存 |

---

## 8. 数据流时序

```text
Redis MQ（DeviceData）
  │  ConsumeAsync（消费组=机器名）
  ▼
DataConsumer.OnConsume
  │  TryGetBus → 无订阅？丢弃
  │            → 有订阅？DispatchAsync(deviceId, machineName, data)
  ▼
EventHub<DeviceDataDTO>
  │  定位 topic 对应的 EventBus
  ▼
EventBus<DeviceDataDTO>.PublishAsync
  │  广播给所有 handler
  ├── Controller(a1b2) → ws.SendAsync → 客户端 A
  ├── Controller(d4e5) → ws.SendAsync → 客户端 B
  └── Controller(g7h8) → ws.SendAsync → 客户端 C
```

---

## 9. 进阶：预序列化减少开销

多个 WebSocket 推送同一条数据时，可预生成 JSON 放入 `EventContext`，避免重复序列化。`EventContext` 实现了 `IExtend`，支持通过索引器存取任意扩展数据：

```csharp
// DataConsumer.OnConsume 中预序列化
private async void OnConsume(DeviceDataDTO data)
{
    var topic = data.DeviceId.ToString();
    if (!Hub.TryGetBus<DeviceDataDTO>(topic, out _)) return;

    var ctx = new EventContext();
    ctx["Raw"] = data.ToJson();

    await Hub.DispatchAsync(topic, Environment.MachineName, data, ctx).ConfigureAwait(false);
}

// DeviceDataController.HandleAsync 中优先取预序列化结果
public async Task HandleAsync(DeviceDataDTO @event, IEventContext? context, CancellationToken cancellationToken)
{
    var raw = (context as IExtend)?["Raw"] as String ?? @event.ToJson();
    var buffer = Encoding.UTF8.GetBytes(raw);
    await _ws.SendAsync(new ArraySegment<Byte>(buffer), WebSocketMessageType.Text, true, cancellationToken)
             .ConfigureAwait(false);
}
```
