# NewLife.Net 网络库使用手册

## 目录

- [概述](#概述)
- [架构设计](#架构设计)
- [快速入门](#快速入门)
- [核心组件](#核心组件)
- [高级特性](#高级特性)
- [最佳实践](#最佳实践)
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
};
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
        
        // 业务处理
        var data = e.Packet?.ToStr();
        if (!String.IsNullOrEmpty(data))
        {
            // 处理数据
            ProcessData(data);
        }
    }
    
    private void ProcessData(String data)
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

// 获取指定会话
var session = server.GetSession(sessionId);
if (session != null)
{
    session.Send("Hello");
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

// 接收解码后的消息
server.Received += (s, e) =>
{
    // e.Message 是解码后的消息对象
    if (e.Message is DefaultMessage msg)
    {
        Console.WriteLine($"序列号：{msg.Sequence}");
        Console.WriteLine($"负载：{msg.Payload?.ToStr()}");
    }
};

server.Start();
```

#### 自定义处理器

```csharp
public class MyHandler : PipelineHandler
{
    public override Object? Read(IHandlerContext context, Object message)
    {
        // 解码处理
        if (message is IPacket pk)
        {
            // 解析协议
            var myMsg = ParseMessage(pk);
            return base.Read(context, myMsg);
        }
        return base.Read(context, message);
    }
    
    public override Object? Write(IHandlerContext context, Object message)
    {
        // 编码处理
        if (message is MyMessage msg)
        {
            var pk = EncodeMessage(msg);
            return base.Write(context, pk);
        }
        return base.Write(context, message);
    }
}

// 注册处理器
server.Add(new MyHandler());
```

### TcpServer / UdpServer

底层 Socket 服务器，通常不直接使用。

```csharp
// 直接使用TcpServer
var tcpServer = new TcpServer
{
    Port = 12345,
    NoDelay = true,              // 禁用Nagle算法
    KeepAliveInterval = 60,      // KeepAlive间隔（秒）
    EnableHttp = false,          // 是否启用HTTP
};

tcpServer.NewSession += (s, e) =>
{
    // 处理新连接
};

tcpServer.Start();
```

### ISocketClient - Socket客户端

客户端接口，支持 TCP 和 UDP。

```csharp
// TCP客户端
var tcp = new TcpSession
{
    Remote = new NetUri("tcp://127.0.0.1:12345"),
    Timeout = 5000,
    NoDelay = true,
};

tcp.Received += (s, e) =>
{
    Console.WriteLine($"收到：{e.Packet?.ToStr()}");
};

tcp.Open();
tcp.Send("Hello");

// UDP客户端
var udp = new UdpServer
{
    Remote = new NetUri("udp://127.0.0.1:12345"),
};

udp.Open();
udp.Send("Hello UDP");
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
    SslProtocol = SslProtocols.Tls12,
    Certificate = cert,
};
server.Start();

// 客户端SSL
var client = new TcpSession
{
    Remote = new NetUri("tcp://127.0.0.1:443"),
    SslProtocol = SslProtocols.Tls12,
    // 可选：客户端证书验证
    Certificate = clientCert,
};
client.Open();
```

### 群发消息

```csharp
// 群发给所有客户端
await server.SendAllAsync(new ArrayPacket(data));

// 带条件群发
await server.SendAllAsync(data, session => session["RoomId"]?.ToString() == "123");

// 群发协议消息
server.SendAllMessage(myMessage, session => session.ID > 100);
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

### WebSocket 支持

```csharp
// WebSocket客户端
var ws = new WebSocketClient
{
    Remote = new NetUri("wss://echo.websocket.org"),
    SslProtocol = SslProtocols.Tls12,
};

ws.Received += (s, e) =>
{
    Console.WriteLine($"收到：{e.Packet?.ToStr()}");
};

ws.Open();
ws.Send("Hello WebSocket");
```

### 依赖注入

```csharp
// 配置服务提供者
services.AddSingleton<IMyService, MyService>();

var server = new NetServer<MySession>
{
    Port = 12345,
    ServiceProvider = serviceProvider,
};

// 在会话中使用
public class MySession : NetSession
{
    protected override void OnConnected()
    {
        var service = ServiceProvider?.GetService<IMyService>();
        service?.DoSomething();
    }
}
```

---

## 最佳实践

### 1. 资源管理

```csharp
// 使用using确保资源释放
using var server = new NetServer { Port = 12345 };
server.Start();

// 或者在finally中停止
try
{
    server.Start();
    // ...
}
finally
{
    server.Stop("Shutdown");
}
```

### 2. 异常处理

```csharp
server.Received += (s, e) =>
{
    try
    {
        ProcessData(e.Packet);
    }
    catch (Exception ex)
    {
        XTrace.WriteException(ex);
        // 不要让异常导致整个服务崩溃
    }
};
```

### 3. 日志配置

```csharp
// 生产环境配置
var server = new NetServer
{
    Log = XTrace.Log,           // 服务器日志
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
};
```

### 4. 性能优化

```csharp
// TCP服务器优化
var server = new NetServer
{
    Port = 12345,
    ProtocolType = NetType.Tcp,
    ReuseAddress = true,        // 启用地址重用
};

// 访问底层TcpServer进行更细致配置
if (server.Server is TcpServer tcp)
{
    tcp.NoDelay = true;         // 禁用Nagle算法（低延迟）
    tcp.KeepAliveInterval = 60; // KeepAlive
}
```

### 5. 会话状态管理

```csharp
public class GameSession : NetSession
{
    public Player Player { get; set; }
    
    protected override void OnReceive(ReceivedEventArgs e)
    {
        // 使用Items存储临时数据
        this["LastPacketTime"] = DateTime.Now;
        
        // 或使用强类型属性
        if (Player != null)
        {
            Player.HandlePacket(e.Packet);
        }
    }
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
    Console.WriteLine($"客户端IP：{ip}");
};
```

### Q: 如何限制最大连接数？

A: 在 NewSession 事件中检查并拒绝。

```csharp
server.NewSession += (s, e) =>
{
    if (server.SessionCount > 1000)
    {
        e.Session.Close("TooManyConnections");
    }
};
```

### Q: 服务器重启时端口被占用？

A: 启用地址重用。

```csharp
server.ReuseAddress = true;
```

### Q: 如何发送文件？

A: 使用扩展方法。

```csharp
// 简单发送
client.Send(stream);

// 分包发送（配合StandardCodec）
client.SendFile("data.bin");
```

---

## 附录

### 标准网络封包协议

新生命团队标准网络封包协议：

```
| 1 Flag | 1 Sequence | 2 Length | N Payload |
```

- **Flag** (1字节)：标识位，标识请求/响应/错误/加密/压缩等
- **Sequence** (1字节)：序列号，用于请求响应配对
- **Length** (2字节)：数据长度，最大64KB
- **Payload** (N字节)：负载数据

### 相关链接

- GitHub: https://github.com/NewLifeX/X
- Gitee: https://gitee.com/NewLifeX/X
- 文档: https://newlifex.com

---

*本文档最后更新：2025年*
