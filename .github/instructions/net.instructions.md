---
applyTo: "**/Net/**"
---

# 网络编程指令

适用于基于 `NewLife.Net` 的网络服务器（`NetServer`）和客户端（`ISocketClient`）开发任务。

---

## 1. 架构概览

NewLife 网络框架分为两层：

| 层级 | 服务端 | 客户端 | 说明 |
|------|--------|--------|------|
| **应用层** | `NetServer` / `NetServer<TSession>` | — | 管理监听、会话生命周期、管道 |
| **传输层** | `TcpServer` / `UdpServer` | `TcpSession` / `UdpServer`（客户端模式） | 底层 Socket 收发 |
| **会话** | `NetSession` / `NetSession<TServer>` | — | 每个连接对应一个会话，业务逻辑入口 |
| **管道** | `IPipeline` + `IPipelineHandler` | 同左 | 编解码、粘包拆包、消息匹配 |

**关键接口**：
- `ISocketClient` — 客户端连接接口（Open/Close/Send/Receive）
- `ISocketRemote` — 远程通信接口（Send/Receive/SendMessageAsync）
- `INetSession` — 网络会话接口（服务端每个连接的业务处理单元）
- `INetHandler` — 网络数据处理器接口（Init/Process）

---

## 2. 服务端开发规范

### 2.1 基本模式

推荐使用泛型 `NetServer<TSession>` + 自定义 `NetSession` 子类：

```csharp
/// <summary>自定义网络服务器</summary>
class MyServer : NetServer<MySession> { }

/// <summary>自定义会话，每个客户端连接对应一个实例</summary>
class MySession : NetSession<MyServer>
{
    /// <summary>客户端连接</summary>
    protected override void OnConnected()
    {
        base.OnConnected();
        WriteLog("客户端已连接 {0}", Remote);
    }

    /// <summary>收到客户端数据</summary>
    protected override void OnReceive(ReceivedEventArgs e)
    {
        base.OnReceive(e);
        // 业务处理
    }

    /// <summary>客户端断开</summary>
    protected override void OnDisconnected(String reason)
    {
        base.OnDisconnected(reason);
    }
}
```

### 2.2 服务器启动配置

```csharp
var server = new MyServer
{
    Port = 8080,                              // 监听端口，0 表示随机
    ProtocolType = NetType.Tcp,               // Tcp/Udp/Unknown（同时监听）
    // AddressFamily = AddressFamily.InterNetwork, // 仅IPv4，默认同时IPv4+IPv6
    ServiceProvider = provider,               // 依赖注入
    Log = XTrace.Log,                         // 应用日志
    SessionLog = XTrace.Log,                  // 会话日志
    Tracer = tracer,                          // APM 追踪
#if DEBUG
    SocketLog = XTrace.Log,                   // Socket 层日志（仅调试）
    LogSend = true,
    LogReceive = true,
#endif
};
server.Start();
```

### 2.3 会话生命周期

```
连接建立 → OnConnected() → OnReceive()... → OnDisconnected(reason) → Dispose()
```

- **OnConnected**：初始化会话状态、发送欢迎消息
- **OnReceive**：核心业务处理入口，`e.Packet` 为原始数据，`e.Message` 为管道解码后的消息
- **OnDisconnected**：清理资源、记录日志，`reason` 包含断开原因
- 会话内可通过 `ServiceProvider` 获取 Scoped 服务

### 2.4 服务端发送数据

| 方法 | 说明 |
|------|------|
| `Send(IPacket)` | 直接发送原始数据，不经过管道 |
| `Send(String)` | 发送字符串，默认 UTF-8 |
| `Send(ReadOnlySpan<Byte>)` | 高性能发送 |
| `SendMessage(Object)` | 通过管道编码后发送，不等待响应 |
| `SendReply(Object, ReceivedEventArgs)` | 发送响应消息，与请求关联（用于 StandardCodec 等协议） |
| `SendMessageAsync(Object)` | 通过管道发送并等待响应 |

### 2.5 群发

```csharp
// 群发数据给所有在线客户端
await server.SendAllAsync(data);

// 带过滤条件群发
await server.SendAllAsync(data, session => session.ID > 100);

// 群发管道消息
server.SendAllMessage(message, session => session["VIP"] is true);
```

群发要求 `UseSession = true`（默认开启）。

### 2.6 事件模式（简单场景）

不需要自定义会话时，可直接使用事件：

```csharp
var server = new NetServer { Port = 8080 };
server.Received += (sender, e) =>
{
    if (sender is INetSession session)
        session.Send(e.Packet);  // Echo
};
server.Start();
```

---

## 3. 客户端开发规范

### 3.1 创建客户端

通过 `NetUri.CreateRemote()` 扩展方法创建：

```csharp
// TCP 客户端
var client = new NetUri("tcp://127.0.0.1:8080").CreateRemote();

// UDP 客户端
var client = new NetUri("udp://127.0.0.1:8080").CreateRemote();

// WebSocket 客户端
var client = new NetUri("ws://127.0.0.1:8080/path").CreateRemote();
```

`CreateRemote` 根据协议自动返回 `TcpSession` / `UdpServer` / `WebSocketClient`。

### 3.2 客户端使用

```csharp
var uri = new NetUri("tcp://127.0.0.1:8080");
var client = uri.CreateRemote();
client.Log = XTrace.Log;
client.Open();

// 发送原始数据（不经过管道）
client.Send("Hello");

// 事件驱动接收
client.Received += (sender, e) =>
{
    // e.Packet 原始数据，e.Message 管道解码后的消息
};

// 或同步/异步接收
using var pk = client.Receive();
using var pk = await client.ReceiveAsync(cancellationToken);

client.Close("完成");  // 或 client.Dispose()
```

### 3.3 请求-响应模式（需要管道编解码器）

```csharp
var client = new NetUri("tcp://127.0.0.1:8080").CreateRemote();
client.Add<StandardCodec>();
client.Open();

var response = await client.SendMessageAsync(payload, cancellationToken);  // 等待响应
client.SendMessage(message);  // 不等待响应
```

### 3.4 SSL/TLS

```csharp
// 服务端 SSL
var server = new NetServer
{
    Port = 443,
    SslProtocol = SslProtocols.Tls12,
    Certificate = new X509Certificate2("server.pfx", "password"),
};

// 客户端 SSL（自动根据端口判断，或手动指定）
var client = new NetUri("tcp://host:443").CreateRemote();
if (client is TcpSession tcp)
{
    tcp.SslProtocol = SslProtocols.Tls12;
    // tcp.Certificate = cert;  // 客户端证书（如果服务端要求）
}
```

---

## 4. 管道与编解码器

### 4.1 管道机制

管道（`IPipeline`）是处理器链，Read/Write 返回值作为下一个处理器的输入，返回 `null` 截断管道：

```
接收：Socket → [Codec1.Read] → [Codec2.Read] → FireRead → OnReceive
发送：SendMessage → [Codec2.Write] → [Codec1.Write] → FireWrite → Socket
```

Open 正序传播，Close 逆序传播。先添加的在底层（靠近 Socket），后添加的在上层（靠近业务）。

### 4.2 内置编解码器

| 编解码器 | 基类 | 说明 | 典型场景 |
|---------|------|------|---------|
| `StandardCodec` | `MessageCodec<IMessage>` | 4字节头部（Flag+Seq+Length），支持请求-响应匹配 | 自定义 RPC 协议 |
| `LengthFieldCodec` | `MessageCodec<IPacket>` | 长度字段头部，可配置偏移和大小 | MQTT、通用二进制协议 |
| `JsonCodec` | `Handler` | JSON 文本编解码，不处理粘包 | 文本协议（通常与 StandardCodec 级联） |
| `SplitDataCodec` | `Handler` | 分隔符拆包（默认 `\r\n`） | 文本行协议 |
| `WebSocketCodec` | `Handler` | WebSocket 帧编解码 | WebSocket 通信 |

### 4.3 添加编解码器

```csharp
// 服务端添加
server.Add<StandardCodec>();

// 客户端添加
client.Add<StandardCodec>();

// 多层管道级联（按添加顺序组成链）
server.Add<StandardCodec>();  // 底层：粘包拆包 + 请求响应匹配
server.Add<JsonCodec>();      // 上层：JSON 编解码
```

### 4.4 StandardCodec 请求-响应

StandardCodec 使用 `DefaultMessage`，包含 Flag（1字节）、Sequence（1字节）、Length（2字节），
支持自动序列号分配和请求-响应匹配。

```csharp
// 服务端 Echo 示例
server.Add<StandardCodec>();
server.Received += (sender, e) =>
{
    if (sender is INetSession session && e.Message is IPacket pk)
        session.SendReply(pk, e);  // 使用 SendReply 关联请求上下文
};

// 客户端请求-响应
client.Add<StandardCodec>();
var response = await client.SendMessageAsync(payload);
```

### 4.5 基类选择

| 基类 | 适用场景 | 典型代表 |
|------|---------|---------|
| `MessageCodec<T>` | 需要粘包拆包和/或请求-响应匹配（内置 `IMatchQueue`、`Encode`/`Decode`） | `StandardCodec`、`LengthFieldCodec` |
| `Handler` | 简单转换、帧协议、文本协议（轻量，仅 `Read`/`Write`/`Open`/`Close`） | `JsonCodec`、`SplitDataCodec`、`WebSocketCodec` |

### 4.6 编解码器设计规范

#### 4.6.1 粘包拆包（PacketCodec 模式）

TCP 是字节流协议，必须处理粘包拆包。统一模式（完整实现见 4.7 模板）：

1. 每个连接独立的 `PacketCodec` 实例，存储在 `ss["Codec"]` 中
2. 通过 `GetLength2` 委托告诉 `PacketCodec` 如何计算完整帧长度
3. `PacketCodec.Parse()` 返回完整帧列表，自动缓存不完整数据

**`GetLength2` 规范**（签名 `Int32 GetLength(ReadOnlySpan<Byte> span)`）：返回帧完整长度（含头部），数据不足时返回 `0`。

```csharp
public static Int32 GetLength(ReadOnlySpan<Byte> span)
{
    if (span.Length < 4) return 0;
    var reader = new SpanReader(span) { IsLittleEndian = true };
    reader.Advance(2);
    return 4 + reader.ReadUInt16();  // 头部4字节 + 负载长度
}
```

#### 4.6.2 编码与内存管理

- **`ExpandHeader(size)`**：编码时优先复用负载缓冲区前置空间写入头部，零拷贝；空间不足时创建 `OwnerPacket`，原包作为 `Next` 链节点
- **`SpanWriter`**：配合 `ExpandHeader` 写入头部字段，注意 `IsLittleEndian` 大小端
- **兜底释放**：`MessageCodec<T>.Write` 基类自动 `TryDispose`；`Handler` 子类需在 `Write` 的 `finally` 中手动调用
- **对象池**：`DefaultMessage.Rent()` / `DefaultMessage.Return()` 减少 GC 压力

#### 4.6.3 请求-响应匹配

`MessageCodec<T>` 内置 `IMatchQueue`，流程：`Write` → `AddToQueue` 入队 → `Decode` 解码 → `Queue.Match` 按 `IsMatch` 匹配 → 唤醒 `SendMessageAsync` 的 `Task`。

- 重载 `AddToQueue`：控制哪些消息入队（通常只有请求消息）
- 重载 `IsMatch`：根据序列号等字段匹配请求和响应（见 4.7 模板）
- `QueueSize`：匹配队列大小，默认 256
- `Timeout`：等待响应超时，默认 30_000ms
- `UserPacket`：为 `true` 时向上层传递 `Payload` 而非整个 `IMessage`，用于编码器级联

#### 4.6.4 Close 清理

**必须**在 `Close` 中执行 `ss["Codec"] = null` 清理 `PacketCodec`，否则 `MemoryStream` 缓存泄漏（见 4.7 模板）。

#### 4.6.5 上下文扩展（IExtend）

管道处理器通过 `IExtend` 在会话/上下文上传递元数据：

| 键 | 用途 | 示例 |
|---|------|------|
| `"Codec"` | 每连接的 `PacketCodec` 实例 | 编解码器的 `Decode`/`Close` 中读写 |
| `"Flag"` | 数据类型标记 `DataKinds` | `JsonCodec.Write` 设置 → `StandardCodec.Write` 消费 |
| `"_raw_message"` | 原始请求消息 | `MessageCodec.Read` 设置 → `Write` 中创建响应时消费 |
| `"TaskSource"` | `TaskCompletionSource` | 框架内部，`AddToQueue` 消费 |

#### 4.6.6 多层管道级联

- 底层编解码器处理粘包拆包和请求-响应匹配，上层处理数据格式转换
- `UserPacket = true` 让底层向上层传递 `Payload` 而非整个 `IMessage`
- 上层通过 `ext["Flag"]` 向底层传递数据类型标记

### 4.7 自定义编解码器模板

#### 方式一：继承 MessageCodec<T>（需要粘包/请求响应匹配）

```csharp
/// <summary>自定义协议编解码器</summary>
public class MyCodec : MessageCodec<MyMessage>
{
    /// <summary>编码消息为数据包</summary>
    protected override Object? Encode(IHandlerContext context, MyMessage msg)
    {
        return msg.ToPacket();
    }

    /// <summary>解码数据包为消息</summary>
    protected override IEnumerable<MyMessage>? Decode(IHandlerContext context, IPacket pk)
    {
        if (context.Owner is not IExtend ss) yield break;

        if (ss["Codec"] is not PacketCodec pc)
        {
            ss["Codec"] = pc = new PacketCodec
            {
                GetLength2 = MyMessage.GetLength,
                MaxCache = MaxCache,
                Tracer = (context.Owner as ISocket)?.Tracer
            };
        }

        foreach (var item in pc.Parse(pk))
        {
            var msg = new MyMessage();
            if (msg.Read(item)) yield return msg;
        }
    }

    /// <summary>是否匹配响应</summary>
    protected override Boolean IsMatch(Object? request, Object? response) =>
        request is MyMessage req && response is MyMessage res
        && req.Sequence == res.Sequence;

    /// <summary>连接关闭时清理</summary>
    public override Boolean Close(IHandlerContext context, String reason)
    {
        if (context.Owner is IExtend ss) ss["Codec"] = null;

        return base.Close(context, reason);
    }
}
```

#### 方式二：继承 Handler（简单转换/帧协议）

```csharp
/// <summary>自定义帧编解码器</summary>
public class MyFrameCodec : Handler
{
    /// <summary>读取数据（接收时）</summary>
    public override Object? Read(IHandlerContext context, Object message)
    {
        if (message is IPacket pk)
        {
            // 解码：二进制 → 业务对象
            var frame = MyFrame.Parse(pk);
            message = frame;
        }

        return base.Read(context, message);
    }

    /// <summary>写入数据（发送时）</summary>
    public override Object? Write(IHandlerContext context, Object message)
    {
        IPacket? owner = null;
        if (message is MyFrame frame)
        {
            // 编码：业务对象 → 二进制
            message = owner = frame.ToPacket();
        }

        try
        {
            return base.Write(context, message);
        }
        finally
        {
            owner.TryDispose();  // 兜底释放
        }
    }

    /// <summary>连接关闭时清理缓存</summary>
    public override Boolean Close(IHandlerContext context, String reason)
    {
        if (context.Owner is IExtend ss) ss["Codec"] = null;

        return base.Close(context, reason);
    }
}
```

---

## 5. 常见模式与最佳实践

### 5.1 端口选择

- 测试代码使用端口 `0`（系统自动分配随机端口），避免端口冲突
- 正式服务指定固定端口
- 启动后可通过 `server.Port` 获取实际监听端口

### 5.2 协议选择

| 场景 | 推荐 |
|------|------|
| 可靠传输、长连接 | `NetType.Tcp` |
| 低延迟、广播、允许丢包 | `NetType.Udp` |
| 同时支持（默认） | `NetType.Unknown` |
| Web 浏览器通信 | `NetType.WebSocket` |

### 5.3 会话管理

- `UseSession = true`（默认）：维护会话集合，支持群发、按 ID 查找
- `UseSession = false`：不维护会话集合，减少内存开销，适合海量短连接
- `SessionTimeout`：设置会话超时时间（秒），超时无数据自动断开
- 会话中通过 `Items` 字典存储自定义数据

### 5.4 日志分层

| 属性 | 用途 | 建议 |
|------|------|------|
| `Log` | 服务器应用层日志 | 始终设置 |
| `SessionLog` | 会话级别日志 | 调试时设置 |
| `SocketLog` | 底层 Socket 日志 | 仅 DEBUG 时设置 |
| `LogSend` / `LogReceive` | 收发数据内容日志 | 仅 DEBUG 时开启 |
| `Tracer` | 应用层 APM | 生产环境追踪 |
| `SocketTracer` | Socket 层 APM | 排查底层问题 |

### 5.5 资源释放

- 服务端：调用 `server.Stop(reason)` 或 `server.Dispose()`
- 客户端：调用 `client.Close(reason)` 或 `client.Dispose()`
- 会话自动随连接断开释放，无需手动管理
- `ISocketClient` 实现 `IDisposable`，推荐 `using` 模式

### 5.6 INetHandler 业务处理器

通过重载 `NetServer.CreateHandler` 注入自定义业务处理器：

```csharp
class MyServer : NetServer<MySession>
{
    /// <summary>为会话创建网络数据处理器</summary>
    public override INetHandler? CreateHandler(INetSession session) => new MyHandler();
}
```

处理器在会话 `Start` 时初始化，`OnReceive` 前调用 `Process`，适合前置协议解析。

---

## 6. 常见错误

- ❌ 在 `OnReceive` 中执行长时间阻塞操作（会影响其他连接的数据接收）
- ❌ 不加管道编解码器直接调用 `SendMessageAsync`（无法匹配响应）
- ❌ 混淆 `Send` 与 `SendMessage`：前者直接发原始数据，后者经过管道编码
- ❌ 混淆 `SendMessage` 与 `SendReply`：响应消息必须用 `SendReply` 关联请求上下文
- ❌ 忘记调用 `base.OnConnected()` / `base.OnDisconnected(reason)` / `base.OnReceive(e)`
- ❌ 在会话中使用 `Task.Result` 或 `Task.Wait()`（导致死锁和线程池饥饿）
- ❌ 使用固定端口编写测试（端口冲突），应使用 `Port = 0`
- ❌ 服务端 SSL 未指定证书

---

## 7. 完整示例

### 7.1 带 StandardCodec 的 Echo 服务

```csharp
// 服务端
var server = new NetServer
{
    Port = 8080,
    ProtocolType = NetType.Tcp,
    Log = XTrace.Log,
};
server.Add<StandardCodec>();
server.Received += (sender, e) =>
{
    if (sender is INetSession session && e.Message is IPacket pk)
        session.SendReply(pk, e);
};
server.Start();

// 客户端
var client = new NetUri($"tcp://127.0.0.1:{server.Port}").CreateRemote();
client.Add<StandardCodec>();
client.Open();

var response = await client.SendMessageAsync(new ArrayPacket("Hello".GetBytes()));
```

### 7.2 自定义会话服务器

```csharp
class ChatServer : NetServer<ChatSession> { }

class ChatSession : NetSession<ChatServer>
{
    protected override void OnConnected()
    {
        base.OnConnected();
        Send($"欢迎 [{Remote}] 进入聊天室！\r\n");
    }

    protected override void OnReceive(ReceivedEventArgs e)
    {
        base.OnReceive(e);
        var msg = e.Packet?.ToStr();
        if (msg.IsNullOrEmpty()) return;

        // 广播给所有在线用户
        var host = (this as INetSession).Host;
        host.SendAllMessage($"[{ID}] {msg}");
    }

    protected override void OnDisconnected(String reason)
    {
        base.OnDisconnected(reason);
        WriteLog("用户离开：{0}", reason);
    }
}
```

---

（完）
