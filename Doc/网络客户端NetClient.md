# NetClient 网络客户端使用手册

## 目录

- [概述](#概述)
- [架构设计](#架构设计)
- [快速开始](#快速开始)
- [属性参考](#属性参考)
- [连接管理](#连接管理)
- [数据收发](#数据收发)
- [事件](#事件)
- [断线重连](#断线重连)
- [管道编解码](#管道编解码)
- [扩展与继承](#扩展与继承)
- [扩展数据](#扩展数据)
- [日志与追踪](#日志与追踪)
- [常见问题](#常见问题)

---

## 概述

`NetClient` 是对 `ISocketClient` 的**应用层封装**，与 `NetServer` 配对使用，提供一致的客户端-服务端通信体验。

主要特性：

- **协议自动识别**：通过 `Server` 地址字符串（`tcp://`、`udp://`、`ws://`）自动创建对应的底层 Socket 客户端
- **透明断线重连**：连接意外断开后自动重连，内部替换 `ISocketClient` 实例，上层业务代码感知不到切换过程
- **管道编解码**：通过 `Add<T>()` 注册编解码处理器，解决粘包、拆包和协议解析问题
- **事件驱动接收**：订阅 `Received` 事件，异步处理到达的数据或消息
- **同步 / 异步双模式**：`Open` / `OpenAsync`、`Send` / `SendMessageAsync` 覆盖全部场景

```csharp
// 典型用法
var client = new NetClient("tcp://127.0.0.1:8080");
client.Add<StandardCodec>();
client.Received += (s, e) => XTrace.WriteLine("收到：{0}", e.Packet?.ToStr());
client.Open();
client.SendMessage(payload);
```

---

## 架构设计

```text
┌─────────────────────────────────────────────┐
│                 NetClient                   │
│  Server / Remote → CreateClient()           │
│  AutoReconnect    Pipeline                  │
│  Events: Opened / Closed / Received / Error │
└─────────────┬───────────────────────────────┘
              │ 持有（volatile，断线后替换）
              ▼
┌─────────────────────────────────────────────┐
│             ISocketClient                   │
│  TcpSession / UdpSession / WsSession        │
└─────────────────────────────────────────────┘
```

**发送流程**

```text
SendMessage(msg)
  → EnsureClient()       // 确保已连接，AutoReconnect=false 时未连接直接抛出
  → Pipeline.Encode(msg) // 经管道编码为二进制（若设置了 Pipeline）
  → ISocketClient.Send() // 底层发送
```

**接收流程**

```text
ISocketClient 收到数据
  → Pipeline.Decode()            // 管道解码
  → NetClient.OnClientReceived() // 事件转发
  → Received 事件                // 业务层处理
```

**断线重连流程**

```text
Closed / Error 事件触发
  → ScheduleReconnect()          // 一次性 TimerX（Period=0）
  → DoReconnect()                // 定时器回调
      ├── 成功：替换 _client，_reconnectCount = 0
      └── 失败：计数++，再次 ScheduleReconnect()
```

---

## 快速开始

### 最简用法

```csharp
using NewLife.Net;

var client = new NetClient("tcp://127.0.0.1:8080");
if (client.Open())
{
    client.Send("Hello World"u8);
    client.Close("done");
}
```

### 事件驱动接收

```csharp
var client = new NetClient("tcp://127.0.0.1:8080");
client.Log = XTrace.Log;

client.Opened   += (s, e) => XTrace.WriteLine("已连接");
client.Closed   += (s, e) => XTrace.WriteLine("已断开");
client.Received += (s, e) => XTrace.WriteLine("收到：{0} 字节", e.Packet?.Total);

client.Open();
```

### 使用管道处理粘包

```csharp
var client = new NetClient("tcp://127.0.0.1:8080");
client.Add<StandardCodec>();   // 标准长度帧编解码

client.Received += (s, e) =>
{
    // e.Message 是管道解码后的消息对象
    XTrace.WriteLine("消息：{0}", e.Message);
};
client.Open();
client.SendMessage(myMessage);
```

### 请求-响应模式

```csharp
var client = new NetClient("tcp://127.0.0.1:8080");
client.Add<StandardCodec>();
await client.OpenAsync();

// SendMessageAsync 等待管道匹配到对应响应后返回
var response = await client.SendMessageAsync(request);
XTrace.WriteLine("响应：{0}", response);
```

---

## 属性参考

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Name` | `String` | 类名 | 标识名称，用于日志输出 |
| `Server` | `String?` | `null` | 服务端地址字符串，自动解析为 `Remote` |
| `Remote` | `NetUri?` | `null` | 远程地址（协议 + 主机 + 端口） |
| `Client` | `ISocketClient?` | `null` | 当前底层 Socket 客户端，重连后会替换，**不建议外部长持引用** |
| `Active` | `Boolean` | `false` | 当前是否已连接 |
| `Local` | `NetUri` | 空 | 本地绑定地址，通常无需设置 |
| `Port` | `Int32` | `0` | 本地绑定端口（`Local.Port` 快捷属性） |
| `Timeout` | `Int32` | `3000` | 连接和读写超时（毫秒） |
| `AutoReconnect` | `Boolean` | `true` | 是否在意外断线后自动重连 |
| `ReconnectDelay` | `Int32` | `5000` | 两次重连之间的等待时间（毫秒） |
| `MaxReconnect` | `Int32` | `0` | 最大重连次数，`0` 表示无限重连 |
| `Pipeline` | `IPipeline?` | `null` | 消息管道，通过 `Add<T>()` 自动创建 |
| `Tracer` | `ITracer?` | `null` | APM 追踪器 |
| `Log` | `ILog` | `Logger.Null` | 日志对象 |
| `LogPrefix` | `String` | `"{Name} "` | 日志行前缀 |
| `Items` | `IDictionary<String, Object?>` | 懒加载 | 扩展数据字典 |

---

## 连接管理

### Open / OpenAsync

```csharp
// 同步连接，适用于简单场景
Boolean ok = client.Open();

// 异步连接，推荐在 async 方法中使用
Boolean ok = await client.OpenAsync(cancellationToken);
```

- 已连接时直接返回 `true`，不重复建立连接
- 网络错误（如目标不可达）返回 `false`，不抛出异常
- `InvalidOperationException`（如未设置 `Remote`）**会抛出**，属于配置错误，需修正代码

### Close / CloseAsync

```csharp
client.Close("用户主动断开");
await client.CloseAsync("用户主动断开", cancellationToken);
```

- 主动关闭会设置 `_userClosed = true`，阻止自动重连
- 内部先取消事件订阅（`Detach`），再关闭底层 Socket，最后手动触发 `Closed` 事件

### Dispose

```csharp
client.Dispose(); // 或 using var client = new NetClient(...);
```

`Dispose` 会停止重连定时器并释放底层 Socket，适合不再使用的场景。

---

## 数据收发

### 发送

```csharp
// 发送字节数组（全部）
client.Send(bytes);

// 发送字节数组（部分）
client.Send(bytes, offset, count);

// 发送数组段
client.Send(new ArraySegment<Byte>(bytes, 0, len));

// 零拷贝发送 ReadOnlySpan（不适用于 async 上下文）
client.Send(span);

// 发送消息对象（经管道编码）
client.SendMessage(myMessage);

// 异步发送并等待响应（需管道支持请求响应匹配）
var reply = await client.SendMessageAsync(myRequest);
```

> `SendMessageAsync` 仅在 `NETCOREAPP` 或 `NETSTANDARD2_1+` 下返回 `ValueTask`，其他平台返回 `Task`。

### 接收——事件驱动（推荐）

```csharp
client.Received += (s, e) =>
{
    var pkt = e.Packet;   // 原始数据包（IOwnerPacket）
    var msg = e.Message;  // 管道解码后的消息（未设置管道时为 null）
    XTrace.WriteLine("收到 {0} 字节", pkt?.Total);
};
```

### 接收——主动拉取

```csharp
// 同步阻塞（不推荐在异步程序中使用）
using var pkt = client.Receive();

// 异步接收
using var pkt = await client.ReceiveAsync(cancellationToken);
```

---

## 事件

| 事件 | 签名 | 触发时机 |
|------|------|---------|
| `Opened` | `EventHandler` | 底层连接建立成功后 |
| `Closed` | `EventHandler` | 连接关闭后（主动关闭或断线均触发） |
| `Received` | `EventHandler<ReceivedEventArgs>` | 收到数据或管道解码出消息时 |
| `Error` | `EventHandler<ExceptionEventArgs>` | 发生错误或连接断开时 |

> 所有事件的 `sender` 均为 `NetClient` 实例本身，而非底层 `ISocketClient`。

```csharp
client.Error += (s, e) =>
{
    XTrace.WriteLine("错误 [{0}]：{1}", e.Action, e.Exception?.Message);
    // e.Action 可能为 "Disconnect" / "Close" / "Receive" / "Send" 等
};
```

---

## 断线重连

### 配置

```csharp
client.AutoReconnect  = true;   // 默认 true，意外断线后自动重连
client.ReconnectDelay = 5_000;  // 每次重连间隔 5s（毫秒）
client.MaxReconnect   = 10;     // 最多重连 10 次，0 = 无限
```

### 重连机制

```text
意外断线
  ├── OnClientClosed → ScheduleReconnect（若非主动关闭）
  └── OnClientError  → ScheduleReconnect（Disconnect/Close/Receive 动作）

ScheduleReconnect：
  1. 检查 AutoReconnect / Disposed / _userClosed（任一为假则跳过）
  2. 检查 _reconnectTimer != null（已有挂起的定时器则跳过）
  3. 检查 _reconnectCount >= MaxReconnect（超限则停止，不清零）
  4. 创建一次性 TimerX（Period=0），延迟 ReconnectDelay 毫秒后触发 DoReconnect

DoReconnect：
  - 成功：替换 _client，重置 _reconnectCount = 0
  - 失败：保留计数，再次调用 ScheduleReconnect
```

- **主动调用 `Close()`**：设置 `_userClosed = true`，任何重连调用均被拦截
- **超过最大次数**：计数不清零，后续 Closed/Error 事件触发时仍被拦截

### 监控重连状态

```csharp
client.Log = XTrace.Log; // 启用日志后，重连过程会输出如：
// NetClient 连接断开，5000ms 后发起第 1 次重连 tcp://127.0.0.1:8080
// NetClient 正在重连 [1] tcp://127.0.0.1:8080
// NetClient 重连成功 tcp://127.0.0.1:8080
```

---

## 管道编解码

### 注册处理器

```csharp
// 泛型方式（推荐）
client.Add<StandardCodec>();

// 实例方式
client.Add(new LengthFieldCodec { MaxLength = 1024 * 1024 });

// 链式调用
client.Add<StandardCodec>()
      .Add<MyBusinessHandler>();
```

### StandardCodec

`StandardCodec` 是 NewLife 标准帧编解码器，报文格式：

```text
[4字节长度][负载数据]
```

与 `NetServer` + `StandardCodec` 配合使用可直接收发任意长度消息。

### 自定义管道处理器

```csharp
public class MyHandler : HandlerBase
{
    public override Object? Read(IHandlerContext context, Object message)
    {
        if (message is IPacket pkt)
        {
            // 自定义解码逻辑
            var msg = MyProtocol.Decode(pkt);
            return base.Read(context, msg);
        }
        return base.Read(context, message);
    }

    public override Object? Write(IHandlerContext context, Object message)
    {
        if (message is MyMessage msg)
        {
            var pkt = MyProtocol.Encode(msg);
            return base.Write(context, pkt);
        }
        return base.Write(context, message);
    }
}
```

---

## 扩展与继承

通过重载 `CreateClient()` 可定制底层 Socket 客户端的行为（如 TLS、KeepAlive 等）：

```csharp
public class SslNetClient : NetClient
{
    public SslNetClient(String server) : base(server) { }

    protected override ISocketClient CreateClient()
    {
        var client = base.CreateClient();

        // 在 base.CreateClient() 已完成基础初始化后追加定制
        if (client is TcpSession tcp)
        {
            tcp.SslProtocol = SslProtocols.Tls12;
            tcp.Certificate = LoadCert();
        }

        return client;
    }
}
```

> `base.CreateClient()` 内部已完成：设置 `Name`、`Timeout`、`Log`、`Pipeline`、`Tracer`、`Local` 并绑定事件监听。子类在调用 `base.CreateClient()` 后只需追加特定配置。

---

## 扩展数据

`Items` / 索引器提供线程安全的扩展数据附加能力，无需继承即可在 `NetClient` 实例上存储业务上下文：

```csharp
// 存入
client["userId"] = 42;
client["loginTime"] = DateTime.UtcNow;
client.Items["tag"] = "vip";

// 读取
var id = (Int32?)client["userId"];

// 批量操作
foreach (var kv in client.Items)
    XTrace.WriteLine("{0} = {1}", kv.Key, kv.Value);
```

底层使用 `ConcurrentDictionary<String, Object?>` 懒加载，未使用前不分配内存。

---

## 日志与追踪

### 日志

```csharp
client.Log = XTrace.Log;          // 输出到全局日志
client.LogPrefix = "MyClient ";   // 自定义前缀（默认为 "{Name} "）

// 自定义日志输出（子类重载）
public override void WriteLog(String format, params Object?[] args)
{
    base.WriteLog("[{0}] " + format, new[] { Thread.CurrentThread.ManagedThreadId }.Concat(args).ToArray());
}
```

### APM 链路追踪

```csharp
// 接入 NewLife.Stardust 或其他 ITracer 实现
client.Tracer = DefaultTracer.Instance;

// NetClient 将 Tracer 传递给底层 ISocketClient，由管道自动创建追踪 Span
```

---

## 常见问题

**Q：`Open()` 返回 `false` 时我该怎么办？**

A：`false` 表示网络层连接失败（如目标不可达、端口未开放）。若开启了 `AutoReconnect`，后台会自动重试；否则请检查服务端是否正常，再手动重新调用 `Open()`。

---

**Q：设置 `MaxReconnect` 后，超过次数是否可以手动恢复？**

A：超过次数后，重连计数不会自动清零。若需重新启用重连，请调用 `Close()` 后再调用 `Open()`——`Open()` 成功会将 `_reconnectCount` 重置为 `0`（实际上 `Open` 走 `DoReconnect` 成功分支才重置；简单的重试路径是先 `Close` 再 `Open` 并在成功建立后自然重置计数）。

---

**Q：为什么 `AutoReconnect=false` 时调用 `Send` 会直接抛出异常？**

A：`AutoReconnect=false` 意味着调用方自行管理连接生命周期。`EnsureClient()` 发现客户端未连接时，不会尝试主动连接，直接抛出 `InvalidOperationException`，提示上层调用 `Open()`。

---

**Q：断线重连后，原来的事件订阅还有效吗？**

A：有效。`NetClient` 的 `Opened`、`Closed`、`Received`、`Error` 事件订阅在 `NetClient` 层面，与底层 `ISocketClient` 实例无关。重连时底层实例被替换，但 `NetClient` 通过 `Attach` / `Detach` 机制将新实例的事件重新映射，上层订阅者无需关注。

---

**Q：`Client` 属性可以长期持有引用吗？**

A：**不建议**。断线重连时，`NetClient` 内部会原子地替换 `_client` 字段为新的 `ISocketClient` 实例。如果外部持有旧实例的引用，它将是一个已销毁的对象，继续操作会出错。推荐始终通过 `NetClient` 的方法进行收发操作。

---

**Q：如何同时连接多个服务端？**

A：每个连接创建独立的 `NetClient` 实例即可。`NetClient` 是单连接封装，多连接场景下请管理好实例生命周期（推荐放入 `IDisposable` 容器或 `using` 块）。

---

*文档更新：2026年3月*
