# NewLife.Net 网络库使用手册

## 目录

- [概述](#概述)
- [架构设计](#架构设计)
- [快速入门](#快速入门)
- [核心组件](#核心组件)
- [高级特性](#高级特性)
- [最佳实践](#最佳实践)
- [性能优化](#性能优化)
- [常见问题](#常见问题)

---

## 概述

NewLife.Net 是新生命团队开发的高性能网络通信库，支持 TCP/UDP 协议，同时兼容 IPv4 和 IPv6。该库设计精良，久经生产环境考验，适用于构建各种网络应用服务器。

### 特性

- **多协议支持**：同时支持 TCP 和 UDP 协议
- **双栈支持**：完美支持 IPv4 和 IPv6
- **高性能**：基于异步 IO 模型，使用 IOCP 实现高并发
- **管道处理**：灵活的消息管道，支持协议编解码
- **SSL/TLS**：原生支持安全传输
- **会话管理**：完善的连接会话管理机制
- **线程安全**：所有核心操作采用原子操作保证线程安全
- **可扩展**：易于扩展的架构设计

### 适用场景

- 即时通讯服务器
- 游戏服务器
- 物联网数据采集
- RPC 远程调用
- 自定义协议服务

---

## 架构设计

### 整体架构

```
┌─────────────────────────────────────────────────────────────────┐
│                         应用层                                   │
│  NetServer / NetServer<TSession>                                │
│  ├── INetSession (会话管理)                                      │
│  ├── INetHandler (数据处理器)                                    │
│  └── Pipeline (消息管道)                                         │
├─────────────────────────────────────────────────────────────────┤
│                       Socket服务层                               │
│  ISocketServer                                                   │
│  ├── TcpServer (TCP服务端)                                      │
│  └── UdpServer (UDP服务端)                                      │
├─────────────────────────────────────────────────────────────────┤
│                       Socket会话层                               │
│  ISocketSession                                                  │
│  ├── TcpSession (TCP会话)                                       │
│  └── UdpSession (UDP会话)                                       │
├─────────────────────────────────────────────────────────────────┤
│                       基础设施层                                 │
│  SessionBase                                                     │
│  ├── ProcessEvent (核心接收处理)                                 │
│  ├── 异步IO (SocketAsyncEventArgs)                              │
│  └── 缓冲区管理                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 核心类关系

```
NetServer
    ├── ISocketServer[] Servers      // Socket服务器集合
    │   ├── TcpServer                // TCP服务
    │   └── UdpServer                // UDP服务
    ├── IDictionary<Int32, INetSession> Sessions  // 网络会话集合
    └── IPipeline Pipeline           // 消息管道

INetSession (网络会话)
    ├── ISocketSession Session       // 底层Socket会话
    ├── NetServer Host               // 所属服务器
    └── INetHandler Handler          // 数据处理器

ISocketSession (Socket会话)
    ├── TcpSession                   // TCP连接会话
    └── UdpSession                   // UDP连接会话
```

### 数据流向

**接收数据流**：
```
Socket接收 → ProcessEvent → OnPreReceive → Pipeline.Read → OnReceive → Received事件
```

**发送数据流**：
```
Send/SendMessage → Pipeline.Write → OnSend → Socket发送
```

### 生命周期

```
[客户端连接] → [创建ISocketSession] → [Server_NewSession]
                                            ↓
                     [OnNewSession] → [CreateSession] → [AddSession]
                                            ↓
                              [NetSession.Start()] → [OnConnected]
                                            ↓
                                    [数据收发循环]
                                            ↓
[客户端断开/超时] → [Close(reason)] → [OnDisconnected] → [Dispose]
```

---

## 快速入门

### 最简单的 Echo 服务器

```csharp
using NewLife.Net;

// 创建服务器
var server = new NetServer
{
    Port = 12345,          // 监听端口
    Log = XTrace.Log,      // 启用日志
};

// 处理接收数据
server.Received += (sender, e) =>
{
    if (sender is INetSession session)
    {
        // Echo 回复
        session.Send(e.Packet);
    }
};

// 启动服务
server.Start();

Console.WriteLine($"服务已启动，监听端口：{server.Port}");
Console.ReadLine();

// 停止服务
server.Stop("Manual");
```

### 创建客户端连接

```csharp
using NewLife.Net;

// 创建TCP客户端
var uri = new NetUri("tcp://127.0.0.1:12345");
var client = uri.CreateRemote();
client.Log = XTrace.Log;

// 打开连接
client.Open();

// 发送数据
client.Send("Hello Server");

// 接收数据
var pk = client.Receive();
Console.WriteLine($"收到：{pk.ToStr()}");

// 关闭连接
client.Close("Done");
```

### 使用 UDP 协议

```csharp
// 服务端
var server = new NetServer
{
    Port = 12345,
    ProtocolType = NetType.Udp,  // 指定UDP
};
server.Start();

// 客户端
var uri = new NetUri("udp://127.0.0.1:12345");
var client = uri.CreateRemote();
client.Open();
client.Send("Hello UDP");
```

---

## 核心组件

### NetServer - 网络服务器

NetServer 是网络服务器的主入口，管理多个底层 Socket 服务器。

#### 基本配置

```csharp
var server = new NetServer
{
    // 基本配置
    Name = "MyServer",                      // 服务名（默认类名去掉Server后缀）
    Port = 12345,                           // 监听端口，0为随机端口
    ProtocolType = NetType.Tcp,             // 协议类型：Tcp/Udp/Unknown(同时监听)
    AddressFamily = AddressFamily.InterNetwork, // 地址族：IPv4/IPv6/Unspecified(同时)
    
    // 会话配置
    SessionTimeout = 1200,                  // 会话超时（秒），默认20分钟
    UseSession = true,                      // 是否使用会话集合
    
    // SSL配置
    SslProtocol = SslProtocols.Tls12,      // SSL协议版本
    Certificate = cert,                     // X509证书
    
    // 日志配置
    Log = XTrace.Log,                       // 服务器日志
    SocketLog = XTrace.Log,                 // Socket日志
    SessionLog = XTrace.Log,                // 会话日志
    LogSend = true,                         // 记录发送日志
    LogReceive = true,                      // 记录接收日志
    
    // 性能配置
    StatPeriod = 600,                       // 统计周期（秒），0禁用
    ReuseAddress = true,                    // 地址重用
    
    // 追踪配置
    Tracer = tracer,                        // APM追踪器
    SocketTracer = tracer,                  // Socket层追踪器
    
    // 依赖注入
    ServiceProvider = serviceProvider,      // 服务提供者
};
```

#### 协议类型配置

```csharp
// 仅监听 TCP
server.ProtocolType = NetType.Tcp;

// 仅监听 UDP
server.ProtocolType = NetType.Udp;

// 同时监听 TCP 和 UDP（默认）
server.ProtocolType = NetType.Unknown;

// HTTP/WebSocket（自动启用HTTP解析）
server.ProtocolType = NetType.Http;
```

#### 地址族配置

```csharp
// 仅 IPv4
server.AddressFamily = AddressFamily.InterNetwork;

// 仅 IPv6
server.AddressFamily = AddressFamily.InterNetworkV6;

// 同时监听 IPv4 和 IPv6（默认）
server.AddressFamily = AddressFamily.Unspecified;
```

#### 事件处理

```csharp
// 新会话事件
server.NewSession += (sender, e) =>
{
    var session = e.Session;
    Console.WriteLine($"新连接：{session.Remote}");
};

// 数据接收事件
server.Received += (sender, e) =>
{
    if (sender is INetSession session)
    {
        Console.WriteLine($"收到[{session.Remote}]：{e.Packet?.ToStr()}");
        
        // 处理解码后的消息
        if (e.Message != null)
        {
            // 处理消息对象
        }
    }
};

// 错误事件
server.Error += (sender, e) =>
{
    Console.WriteLine($"错误：{e.Exception.Message}");
};
```

#### 会话管理

```csharp
// 获取指定会话
var session = server.GetSession(sessionId);

// 遍历所有会话
foreach (var item in server.Sessions)
{
    Console.WriteLine($"会话 {item.Key}: {item.Value.Remote}");
}

// 当前会话数和最高会话数
Console.WriteLine($"在线: {server.SessionCount}/{server.MaxSessionCount}");
```

#### 手动添加服务器

```csharp
// 方式1：使用 AddServer
server.AddServer(IPAddress.Any, 8080, NetType.Tcp);
server.AddServer(IPAddress.Any, 8081, NetType.Udp);

// 方式2：使用 AttachServer
var tcpServer = new TcpServer { Port = 8080 };
server.AttachServer(tcpServer);

// 方式3：重载 EnsureCreateServer
public class MyServer : NetServer
{
    public override void EnsureCreateServer()
    {
        if (Servers.Count > 0) return;
        
        var tcp = new TcpServer { Port = Port };
        AttachServer(tcp);
    }
}
```

### NetSession - 网络会话

每个连接对应一个会话，用于处理该连接的业务逻辑。

#### 自定义会话

```csharp
public class MySession : NetSession
{
    /// <summary>用户ID</summary>
    public Int32 UserId { get; set; }
    
    /// <summary>连接时间</summary>
    public DateTime ConnectTime { get; set; }
    
    /// <summary>连接建立</summary>
    protected override void OnConnected()
    {
        base.OnConnected();
        ConnectTime = DateTime.Now;
        WriteLog("客户端已连接");
        
        // 发送欢迎消息
        Send("Welcome!");
    }
    
    /// <summary>连接断开</summary>
    protected override void OnDisconnected(String reason)
    {
        base.OnDisconnected(reason);
        WriteLog($"客户端已断开：{reason}");
    }
    
    /// <summary>收到数据</summary>
    protected override void OnReceive(ReceivedEventArgs e)
    {
        base.OnReceive(e);
        
        // 原始数据包
        var pk = e.Packet;
        
        // 经过管道解码后的消息对象
        var msg = e.Message;
        
        // 业务处理
        ProcessData(pk, msg);
    }
    
    /// <summary>发生错误</summary>
    protected override void OnError(Object? sender, ExceptionEventArgs e)
    {
        base.OnError(sender, e);
        WriteError("发生错误：{0}", e.Exception.Message);
    }
    
    private void ProcessData(IPacket? pk, Object? msg)
    {
        // 业务逻辑
    }
}

// 使用泛型服务器
var server = new NetServer<MySession>
{
    Port = 12345,
};
server.Start();

// 获取指定会话（强类型）
var session = server.GetSession(sessionId);
if (session != null)
{
    session.UserId = 123;
    session.Send("Hello");
}
```

#### 发送数据

```csharp
public class MySession : NetSession
{
    public void SendData()
    {
        // 发送字节数组
        Send(new Byte[] { 0x01, 0x02, 0x03 });
        Send(data, offset, count);

        // 发送字符串（默认UTF-8）
        Send("Hello World");
        Send("你好", Encoding.UTF8);

        // 发送数据包
        var packet = new ArrayPacket(data);
        Send(packet);

        // 发送流
        using var stream = File.OpenRead("data.bin");
        Send(stream);

        // 发送Span（高性能，零拷贝）
        ReadOnlySpan<Byte> span = stackalloc Byte[100];
        Send(span);

        // 通过管道发送消息（会经过编码器）
        var bytes = SendMessage(new MyRequest { Cmd = "ping" });

        // 发送响应消息（关联请求上下文）
        SendReply(new MyResponse { Result = "OK" }, e);
    }

    // 异步发送并等待响应
    public async Task<MyResponse> QueryAsync()
    {
        var response = await SendMessageAsync(new MyRequest { Cmd = "query" });
        return response as MyResponse;
    }

    // 带超时的异步请求
    public async Task<MyResponse> QueryWithTimeoutAsync()
    {
        using var cts = new CancellationTokenSource(5000);
        var response = await SendMessageAsync(request, cts.Token);
        return response as MyResponse;
    }
}
```

#### 会话数据存储

```csharp
public class MySession : NetSession
{
    protected override void OnConnected()
    {
        base.OnConnected();
        
        // 使用索引器存储数据（与底层Socket会话共享）
        this["userId"] = 12345;
        this["loginTime"] = DateTime.Now;
        
        // 使用 Items 字典
        Items["userData"] = new UserData();
    }

    protected override void OnReceive(ReceivedEventArgs e)
    {
        base.OnReceive(e);
        
        // 读取会话数据
        var userId = (Int32)this["userId"];
        var userData = Items["userData"] as UserData;
    }
}
```

#### 泛型会话（强类型Host访问）

```csharp
// 自定义服务器
public class GameServer : NetServer<GameSession>
{
    public GameConfig Config { get; set; }
    public IDatabase Database { get; set; }
}

// 自定义会话
public class GameSession : NetSession<GameServer>
{
    protected override void OnReceive(ReceivedEventArgs e)
    {
        base.OnReceive(e);
        
        // 直接访问自定义服务器属性（强类型）
        var config = Host.Config;
        var db = Host.Database;
    }
}
```

### Pipeline - 消息管道

管道用于协议编解码，支持链式处理。

#### 标准编解码器

```csharp
using NewLife.Net.Handlers;

var server = new NetServer { Port = 12345 };

// 添加标准编解码器（4字节头部+数据）
server.Add<StandardCodec>();

// 添加JSON编解码器
server.Add<JsonCodec>();

// 接收解码后的消息
server.Received += (s, e) =>
{
    // e.Message 是解码后的消息对象
    if (e.Message is DefaultMessage msg)
    {
        Console.WriteLine($"标志：{msg.Flag}");
        Console.WriteLine($"序列号：{msg.Sequence}");
        Console.WriteLine($"负载：{msg.Payload?.ToStr()}");
    }
};

server.Start();
```

#### 自定义处理器

```csharp
public class MyHandler : Handler
{
    public override Boolean Read(IHandlerContext context, Object message)
    {
        // 解码处理（接收数据时）
        if (message is IPacket pk)
        {
            var myMsg = ParseMessage(pk);
            return context.FireRead(myMsg);
        }
        return base.Read(context, message);
    }
    
    public override Boolean Write(IHandlerContext context, Object message)
    {
        // 编码处理（发送数据时）
        if (message is MyMessage msg)
        {
            var pk = EncodeMessage(msg);
            return context.FireWrite(pk);
        }
        return base.Write(context, message);
    }
}

// 注册处理器
server.Add(new MyHandler());
```

### INetHandler - 网络处理器

用于会话级别的数据预处理。

```csharp
public class MyServer : NetServer<MySession>
{
    // 为每个会话创建处理器
    public override INetHandler? CreateHandler(INetSession session)
    {
        return new MyNetHandler();
    }
}

public class MyNetHandler : INetHandler
{
    public INetSession? Session { get; set; }

    public void Init(INetSession session)
    {
        Session = session;
    }

    public void Process(ReceivedEventArgs e)
    {
        // 预处理数据（在OnReceive之前）
        // 可以修改 e.Packet 或 e.Message
        // 可以设置 e.Packet = null 来阻止后续处理
    }

    public void Dispose()
    {
        // 清理资源
    }
}
```

---

## 高级特性

### SSL/TLS 加密

```csharp
// 服务端SSL
var cert = new X509Certificate2("server.pfx", "password");
var server = new NetServer
{
    Port = 443,
    ProtocolType = NetType.Tcp,
    SslProtocol = SslProtocols.Tls12 | SslProtocols.Tls13,
    Certificate = cert,
};
server.Start();

// 客户端SSL
var client = new TcpSession
{
    Remote = new NetUri("tcp://127.0.0.1:443"),
    SslProtocol = SslProtocols.Tls12,
    // 可选：客户端证书
    Certificate = clientCert,
};
client.Open();
```

### 群发消息

```csharp
// 群发数据包给所有客户端
await server.SendAllAsync(new ArrayPacket(data));

// 带条件群发（过滤器）
await server.SendAllAsync(data, session => 
    session["RoomId"]?.ToString() == "123");

// 群发管道消息（会经过编码器）
server.SendAllMessage(new BroadcastMessage { Content = "Hello" });

// 群发并过滤
server.SendAllMessage(message, session => session.ID > 100);

// 排除自己
server.SendAllMessage(message, session => session.ID != mySession.ID);
```

### 消息请求响应

```csharp
// 客户端发送请求并等待响应
var client = new TcpSession { Remote = uri };
client.Add<StandardCodec>();
client.Open();

// 异步发送消息并等待响应
var response = await client.SendMessageAsync(request);

// 带超时的请求
using var cts = new CancellationTokenSource(5000);
var response = await client.SendMessageAsync(request, cts.Token);
```

### 依赖注入

```csharp
// 配置服务
services.AddSingleton<IMyService, MyService>();
services.AddScoped<IScopedService, ScopedService>();

var server = new NetServer<MySession>
{
    Port = 12345,
    ServiceProvider = serviceProvider,
};

// 在会话中使用（自动创建Scope）
public class MySession : NetSession
{
    protected override void OnConnected()
    {
        base.OnConnected();
        
        // ServiceProvider 已自动创建 Scope
        var service = ServiceProvider?.GetService<IMyService>();
        var scoped = ServiceProvider?.GetService<IScopedService>();
    }
    
    // 内置服务快速获取
    public override Object GetService(Type serviceType)
    {
        if (serviceType == typeof(INetSession)) return this;
        if (serviceType == typeof(NetServer)) return (this as INetSession).Host;
        if (serviceType == typeof(ISocketSession)) return Session;
        if (serviceType == typeof(ISocketServer)) return Server;
        
        return base.GetService(serviceType);
    }
}
```

### APM 性能追踪

```csharp
var server = new NetServer
{
    Port = 12345,
    Tracer = tracer,           // 应用层追踪
    SocketTracer = tracer,     // Socket层追踪
};

// 追踪的操作包括：
// - net:{Name}:Connect    连接事件
// - net:{Name}:Receive    接收数据
// - net:{Name}:Send       发送数据
// - net:{Name}:Disconnect 断开连接
```

---

## 最佳实践

### 1. 资源管理

```csharp
// 使用using确保资源释放
using var server = new NetServer { Port = 12345 };
server.Start();

// 或者在finally中停止
var server = new NetServer { Port = 12345 };
try
{
    server.Start();
    // ...
}
finally
{
    server.Stop("Shutdown");
    server.Dispose();
}
```

### 2. 异常处理

```csharp
// 会话级异常处理
public class MySession : NetSession
{
    protected override void OnReceive(ReceivedEventArgs e)
    {
        try
        {
            base.OnReceive(e);
            ProcessData(e.Packet);
        }
        catch (Exception ex)
        {
            WriteError("处理数据异常：{0}", ex.Message);
            // 不要在这里关闭连接，让上层决定
        }
    }
}

// 服务器级异常处理
server.Error += (s, e) =>
{
    XTrace.WriteException(e.Exception);
};
```

### 3. 日志配置

```csharp
// 生产环境配置
var server = new NetServer
{
    Log = XTrace.Log,           // 服务器日志
    SessionLog = null,          // 关闭会话日志
    SocketLog = null,           // 关闭Socket日志
    LogSend = false,            // 关闭发送日志
    LogReceive = false,         // 关闭接收日志
    StatPeriod = 600,           // 10分钟输出一次统计
};

// 调试环境配置
var server = new NetServer
{
    Log = XTrace.Log,
    SocketLog = XTrace.Log,
    SessionLog = XTrace.Log,
    LogSend = true,
    LogReceive = true,
    StatPeriod = 60,            // 1分钟输出统计
};
```

### 4. 会话状态管理

```csharp
public class GameSession : NetSession
{
    public Player Player { get; set; }
    
    protected override void OnConnected()
    {
        base.OnConnected();
        // 初始化玩家对象
        Player = new Player();
    }
    
    protected override void OnReceive(ReceivedEventArgs e)
    {
        base.OnReceive(e);
        
        // 使用Items存储临时数据
        this["LastActiveTime"] = DateTime.Now;
        
        // 使用强类型属性
        Player?.HandlePacket(e.Packet);
    }
    
    protected override void OnDisconnected(String reason)
    {
        base.OnDisconnected(reason);
        // 清理玩家数据
        Player?.SaveAndCleanup();
        Player = null;
    }
}
```

---

## 性能优化

### 1. 会话集合优化

```csharp
// 高并发场景下如不需要遍历会话，可禁用会话集合
server.UseSession = false;

// 会话集合使用 ConcurrentDictionary，遍历时直接遍历 Values
foreach (var session in server.Sessions.Values)
{
    // 避免 KeyValuePair 的额外开销
}
```

### 2. 群发优化

```csharp
// 群发已优化为同步发送，避免 Task.Run 开销
// 直接遍历 _Sessions.Values，减少字典操作开销
await server.SendAllAsync(data);

// 如果需要并行发送，自行实现
var tasks = server.Sessions.Values
    .Where(predicate)
    .Select(s => Task.Run(() => s.Send(data)));
await Task.WhenAll(tasks);
```

### 3. 原子操作

```csharp
// SessionCount 和 MaxSessionCount 使用原子操作更新
// 避免锁竞争，提高并发性能
var count = server.SessionCount;      // 当前会话数
var max = server.MaxSessionCount;     // 历史最高会话数
```

### 4. 追踪数据优化

```csharp
// Send 方法追踪数据限制长度，避免大数据包影响追踪性能
// 字符串和字节数组最多记录64字节
public virtual INetSession Send(String msg, Encoding? encoding = null)
{
    // 追踪时只记录前64字符
    using var span = host?.Tracer?.NewSpan($"net:{host.Name}:Send", 
        msg.Length > 64 ? msg[..64] : msg, ...);
    ...
}
```

### 5. 防重入保护

```csharp
// Start 和 Close 方法都有防重入保护
// 使用 Interlocked.CompareExchange 确保只执行一次
public virtual void Start()
{
    if (Interlocked.CompareExchange(ref _running, 1, 0) != 0) return;
    ...
}

public void Close(String reason)
{
    if (Interlocked.CompareExchange(ref _running, 0, 1) != 1) return;
    ...
}
```

### 6. TCP 性能配置

```csharp
// 访问底层TcpServer进行配置
if (server.Server is TcpServer tcp)
{
    tcp.NoDelay = true;         // 禁用Nagle算法（低延迟）
    tcp.KeepAliveInterval = 60; // KeepAlive间隔
}
```

---

## 常见问题

### Q: 如何处理粘包/拆包？

A: 使用 StandardCodec 或自定义协议处理器。StandardCodec 采用 4 字节头部标识数据长度。

```csharp
server.Add<StandardCodec>();
```

### Q: 如何实现心跳检测？

A: 设置会话超时时间，客户端定期发送心跳包。

```csharp
server.SessionTimeout = 120;  // 2分钟无数据则断开

// 客户端定时发送心跳
timer.Elapsed += (s, e) => client.Send("ping");
```

### Q: 如何获取客户端真实IP？

A: 通过会话的 Remote 属性获取。

```csharp
server.NewSession += (s, e) =>
{
    var ip = e.Session.Remote.Address;
    var port = e.Session.Remote.Port;
    Console.WriteLine($"客户端：{ip}:{port}");
};
```

### Q: 如何限制最大连接数？

A: 在 OnNewSession 中检查并拒绝。

```csharp
public class MyServer : NetServer
{
    public Int32 MaxConnections { get; set; } = 1000;

    protected override INetSession OnNewSession(ISocketSession session)
    {
        if (SessionCount >= MaxConnections)
        {
            WriteLog("连接数超限，拒绝连接：{0}", session.Remote);
            session.Dispose();
            return null;
        }
        return base.OnNewSession(session);
    }
}
```

### Q: 服务器重启时端口被占用？

A: 启用地址重用。

```csharp
server.ReuseAddress = true;
```

### Q: 如何发送文件？

A: 使用流发送或扩展方法。

```csharp
// 简单发送流
using var stream = File.OpenRead("data.bin");
session.Send(stream);

// 分包发送（配合StandardCodec）
client.SendFile("data.bin");
```

### Q: UDP 如何区分客户端？

A: UDP 协议下，每个不同的远程地址会创建独立的会话。

```csharp
server.Received += (s, e) =>
{
    var session = s as INetSession;
    Console.WriteLine($"来自 {session.Remote} 的数据");
};
```

### Q: 如何实现广播房间？

A: 使用会话数据标记房间，群发时过滤。

```csharp
// 加入房间
session["RoomId"] = "room1";

// 房间广播
server.SendAllMessage(message, s => s["RoomId"]?.ToString() == "room1");
```

---

## 附录

### 标准网络封包协议

新生命团队标准网络封包协议（DefaultMessage）：

```
| 1 Flag | 1 Sequence | 2 Length | N Payload |
```

- **Flag** (1字节)：标识位，可用范围0~63，标识消息类型/加密/压缩等
- **Sequence** (1字节)：序列号，用于请求响应配对
- **Length** (2字节)：数据长度，最大64KB
- **Payload** (N字节)：负载数据

### 关键类型速查

| 类型 | 说明 |
|------|------|
| `NetServer` | 网络服务器，管理多个Socket服务器和会话 |
| `NetServer<TSession>` | 泛型网络服务器，自动创建指定类型会话 |
| `NetSession` | 网络会话基类，处理单个连接的业务逻辑 |
| `NetSession<TServer>` | 泛型网络会话，强类型访问Host |
| `INetSession` | 网络会话接口 |
| `INetHandler` | 网络处理器接口，会话级数据预处理 |
| `IPipeline` | 消息管道接口 |
| `ISocketServer` | Socket服务器接口 |
| `ISocketSession` | Socket会话接口 |
| `TcpServer` | TCP服务器 |
| `UdpServer` | UDP服务器 |
| `TcpSession` | TCP客户端/会话 |

### 相关链接

- GitHub: https://github.com/NewLifeX/X
- Gitee: https://gitee.com/NewLifeX/X
- 文档: https://newlifex.com

---

*本文档最后更新：2025年7月*
