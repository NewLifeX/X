# NewLife.Net 网络客户端使用手册

## 目录

- [概述](#概述)
- [架构设计](#架构设计)
- [快速开始](#快速开始)
- [核心组件](#核心组件)
- [高级功能](#高级功能)
- [最佳实践](#最佳实践)
- [性能优化](#性能优化)
- [常见问题](#常见问题)

---

## 概述

NewLife.Net 客户端组件是新生命团队开发的高性能网络通信库，提供 TCP/UDP 客户端功能，支持 IPv4 和 IPv6，具备完善的连接管理、数据收发、消息处理等能力。

### 特性

- **多协议支持**：同时支持 TCP 和 UDP 协议
- **双栈支持**：完整支持 IPv4 和 IPv6
- **高性能**：基于异步 IO 模型，使用 IOCP 实现高并发
- **管道处理**：灵活的消息管道，支持协议编解码
- **SSL/TLS**：原生支持安全连接
- **事件驱动**：数据接收通过事件通知，简单易用
- **线程安全**：所有核心操作采用原子操作保证线程安全
- **易扩展**：良好扩展的架构设计

### 适用场景

- 实时通讯客户端
- 游戏客户端
- 物联网设备通信
- RPC 远程调用
- 自定义协议客户端

---

## 架构设计

### 核心架构

```
┌─────────────────────────────────────────────────────────────────────┐
│                         应用层                                       │
│  TcpSession / UdpServer (客户端模式)                                  │
│  ├── ISocketClient (客户端接口)                                       │
│  ├── ISocketRemote (远程通信接口)                                     │
│  └── Pipeline (消息管道)                                             │
├─────────────────────────────────────────────────────────────────────┤
│                       Socket基础层                                   │
│  SessionBase                                                         │
│  ├── 连接管理 (Open/Close)                                           │
│  ├── 数据发送 (Send/SendMessage)                                     │
│  ├── 数据接收 (Receive/ReceiveAsync/Received事件)                    │
│  └── 异步IO (SocketAsyncEventArgs)                                   │
├─────────────────────────────────────────────────────────────────────┤
│                       传输层                                         │
│  ├── TcpSession (TCP客户端/会话)                                     │
│  └── UdpServer (UDP客户端/服务端)                                    │
└─────────────────────────────────────────────────────────────────────┘
```

### 接口继承关系

```
ISocket                        // 基础Socket接口
    ├── ISocketRemote          // 远程通信接口（收发数据）
    │   ├── ISocketClient      // 客户端接口（连接管理）
    │   │   └── TcpSession     // TCP客户端实现
    │   │   └── UdpServer      // UDP实现
    │   └── ISocketSession     // 服务端会话接口
    │       └── UdpSession     // UDP会话实现
    └── ISocketServer          // 服务端接口
        └── TcpServer          // TCP服务端实现
        └── UdpServer          // UDP服务端实现
```

### 数据流向

**发送流程**：
```
Send/SendMessage → Pipeline.Write → OnSend → Socket发送
```

**接收流程**：
```
Socket接收 → ProcessEvent → OnPreReceive → Pipeline.Read → OnReceive → Received事件
```

---

## 快速开始

### 最简单的 TCP 客户端

```csharp
using NewLife.Net;

// 创建TCP客户端
var client = new TcpSession
{
    Remote = new NetUri("tcp://127.0.0.1:12345"),
    Timeout = 5000,  // 超时时间5秒
};

// 打开连接
client.Open();

// 发送数据
client.Send("Hello Server"u8.ToArray());

// 同步接收数据
var pk = client.Receive();
Console.WriteLine($"收到：{pk.ToStr()}");

// 关闭连接
client.Close("Done");
```

### 使用事件驱动模式

```csharp
using NewLife.Net;

var client = new TcpSession
{
    Remote = new NetUri("tcp://127.0.0.1:12345"),
};

// 连接打开事件
client.Opened += (s, e) => Console.WriteLine("已连接");

// 连接关闭事件
client.Closed += (s, e) => Console.WriteLine("已断开");

// 数据接收事件
client.Received += (s, e) =>
{
    Console.WriteLine($"收到 [{e.Packet.Length}]: {e.Packet.ToStr()}");
};

// 错误事件
client.Error += (s, e) =>
{
    Console.WriteLine($"错误 [{e.Action}]: {e.Exception.Message}");
};

// 打开连接（会自动开始异步接收）
client.Open();

// 发送数据
client.Send("Hello");

// 保持运行
Console.ReadLine();

client.Close("Exit");
```

### 异步方式

```csharp
using NewLife.Net;

var client = new TcpSession
{
    Remote = new NetUri("tcp://127.0.0.1:12345"),
};

// 异步打开连接
await client.OpenAsync();

// 发送数据
client.Send("Hello");

// 异步接收数据
var pk = await client.ReceiveAsync();
Console.WriteLine($"收到：{pk?.ToStr()}");

// 异步关闭
await client.CloseAsync("Done");
```

### 使用 UDP 协议

```csharp
using NewLife.Net;

// UDP 使用 UdpServer 作为客户端
var client = new UdpServer
{
    Remote = new NetUri("udp://127.0.0.1:12345"),
};

// 数据接收事件
client.Received += (s, e) =>
{
    Console.WriteLine($"收到来自 {e.Remote}: {e.Packet.ToStr()}");
};

// 打开（绑定本地端口）
client.Open();

// 发送数据到远程地址
client.Send("Hello UDP");

Console.ReadLine();
client.Close("Exit");
```

---

## 核心组件

### TcpSession - TCP客户端

TcpSession 是增强的TCP客户端，支持SSL/TLS安全连接。

#### 基本属性

```csharp
var client = new TcpSession
{
    // 连接配置
    Name = "MyClient",                      // 客户端名称，用于日志
    Remote = new NetUri("tcp://host:port"), // 远程地址
    Timeout = 5000,                         // 超时时间（毫秒）
    
    // TCP选项
    NoDelay = true,                         // 禁用Nagle算法（低延迟）
    KeepAliveInterval = 30,                 // KeepAlive间隔（秒）
    
    // SSL配置
    SslProtocol = SslProtocols.Tls12,       // SSL协议版本
    Certificate = cert,                      // 客户端证书（可选）
    
    // 接收配置
    MaxAsync = 1,                           // 最大并行接收数，TCP默认1
    BufferSize = 8192,                      // 接收缓冲区大小
    
    // 日志配置
    Log = XTrace.Log,                       // 日志对象
    LogSend = true,                         // 记录发送日志
    LogReceive = true,                      // 记录接收日志
    LogDataLength = 64,                     // 日志数据长度
    
    // 追踪配置
    Tracer = tracer,                        // APM追踪器
};
```

#### 连接管理

```csharp
// 同步连接
var success = client.Open();

// 异步连接
var success = await client.OpenAsync();

// 带取消令牌的异步连接
using var cts = new CancellationTokenSource(5000);
var success = await client.OpenAsync(cts.Token);

// 关闭连接
client.Close("Reason");
await client.CloseAsync("Reason");

// 检查连接状态
if (client.Active)
{
    // 连接活跃
}
```

#### 数据发送

```csharp
// 发送字节数组
var sent = client.Send(new Byte[] { 0x01, 0x02, 0x03 });
var sent = client.Send(data, offset, count);

// 发送ArraySegment
var sent = client.Send(new ArraySegment<Byte>(data, 0, 10));

// 发送Span（高性能，零拷贝）
ReadOnlySpan<Byte> span = stackalloc Byte[100];
var sent = client.Send(span);

// 发送数据包
var packet = new ArrayPacket(data);
var sent = client.Send(packet);

// 使用扩展方法发送字符串
var sent = client.Send("Hello World");
var sent = client.Send("中文", Encoding.UTF8);

// 使用扩展方法发送流
using var stream = File.OpenRead("data.bin");
var sent = client.Send(stream);
```

#### 数据接收

```csharp
// 同步接收（阻塞）
using var pk = client.Receive();
if (pk != null)
{
    var data = pk.ToArray();
    var str = pk.ToStr();
}

// 异步接收
using var pk = await client.ReceiveAsync();

// 带取消令牌
using var cts = new CancellationTokenSource(5000);
using var pk = await client.ReceiveAsync(cts.Token);

// 扩展方法：接收字符串
var str = client.ReceiveString();

// 事件驱动接收（推荐）
client.Received += (s, e) =>
{
    var pk = e.Packet;      // 原始数据包
    var msg = e.Message;    // 经管道解码后的消息
    var remote = e.Remote;  // 远程地址
    var local = e.Local;    // 本地地址
};
```

### UdpServer - UDP客户端/服务端

UdpServer 既可以作为服务端也可以作为客户端使用。

#### 作为客户端使用

```csharp
var client = new UdpServer
{
    // 远程地址（作为客户端时指定）
    Remote = new NetUri("udp://127.0.0.1:12345"),
    
    // 本地配置
    Port = 0,                               // 本地端口，0表示自动分配
    ReuseAddress = true,                    // 地址重用
    Loopback = false,                       // 是否接收环回数据
    
    // 会话配置
    SessionTimeout = 20 * 60,               // 会话超时时间（秒）
    
    // 接收配置
    MaxAsync = Environment.ProcessorCount * 16 / 10, // UDP默认CPU*1.6
    BufferSize = 8192,
};

// 打开
client.Open();

// 发送到Remote指定的地址
client.Send("Hello");

// 关闭
client.Close("Done");
```

#### UDP会话（UdpSession）

当作为服务端使用时，每个远程地址对应一个UdpSession。

```csharp
var server = new UdpServer { Port = 12345 };

// 新会话事件
server.NewSession += (s, e) =>
{
    var session = e.Session as UdpSession;
    Console.WriteLine($"新会话：{session.Remote}");
    
    // 会话数据接收
    session.Received += (ss, ee) =>
    {
        Console.WriteLine($"会话 {session.ID} 收到：{ee.Packet.ToStr()}");
        
        // 回复
        session.Send("Reply");
    };
};

server.Open();
```

### Pipeline - 消息管道

管道用于协议编解码，支持链式处理。

#### 添加处理器

```csharp
using NewLife.Net.Handlers;

var client = new TcpSession { Remote = uri };

// 使用扩展方法添加处理器
client.Add<StandardCodec>();    // 标准编解码器（4字节头部+数据）
client.Add<JsonCodec>();        // JSON编解码器

// 或者直接设置Pipeline
client.Pipeline = new Pipeline();
client.Pipeline.Add(new StandardCodec());
client.Pipeline.Add(new JsonCodec());
```

#### 标准编解码器

StandardCodec 使用 4 字节头部标识数据长度，自动处理粘包/拆包。

```csharp
client.Add<StandardCodec>();

// 发送消息（自动添加头部）
client.SendMessage(new ArrayPacket(data));

// 接收时自动解析
client.Received += (s, e) =>
{
    // e.Message 是解码后的消息
    if (e.Message is IPacket pk)
    {
        var data = pk.ToArray();
    }
};
```

#### 自定义处理器

```csharp
public class MyCodec : Handler
{
    public override Object? Read(IHandlerContext context, Object message)
    {
        // 解码处理（接收时）
        if (message is IPacket pk)
        {
            var myMsg = Decode(pk);
            return context.FireRead(myMsg);
        }
        return base.Read(context, message);
    }
    
    public override Object? Write(IHandlerContext context, Object message)
    {
        // 编码处理（发送时）
        if (message is MyMessage msg)
        {
            var pk = Encode(msg);
            return context.FireWrite(pk);
        }
        return base.Write(context, message);
    }
}

// 使用自定义处理器
client.Add(new MyCodec());
```

### NetUri - 网络地址

NetUri 用于描述网络地址，支持协议、主机、端口。

```csharp
// 从字符串解析
var uri = new NetUri("tcp://127.0.0.1:12345");
var uri = new NetUri("udp://192.168.1.100:8080");
var uri = new NetUri("tcp://example.com:443");

// 直接构造
var uri = new NetUri(NetType.Tcp, IPAddress.Loopback, 12345);
var uri = new NetUri(NetType.Udp, "192.168.1.100", 8080);

// 获取属性
var type = uri.Type;           // 协议类型
var host = uri.Host;           // 主机名
var address = uri.Address;     // IP地址
var port = uri.Port;           // 端口
var endpoint = uri.EndPoint;   // IPEndPoint

// 域名解析
var addresses = uri.GetAddresses();    // 获取所有IP
var endpoints = uri.GetEndPoints();    // 获取所有终结点
```

---

## 高级功能

### SSL/TLS 安全连接

```csharp
// 客户端SSL（不验证服务端证书）
var client = new TcpSession
{
    Remote = new NetUri("tcp://127.0.0.1:443"),
    SslProtocol = SslProtocols.Tls12 | SslProtocols.Tls13,
};

// 客户端SSL（使用客户端证书）
var clientCert = new X509Certificate2("client.pfx", "password");
var client = new TcpSession
{
    Remote = new NetUri("tcp://127.0.0.1:443"),
    SslProtocol = SslProtocols.Tls12,
    Certificate = clientCert,  // 客户端证书
};

client.Open();
```

### 消息请求响应模式

```csharp
var client = new TcpSession { Remote = uri };
client.Add<StandardCodec>();
client.Open();

// 发送消息并等待响应
var response = await client.SendMessageAsync(request);

// 带超时的请求响应
using var cts = new CancellationTokenSource(5000);
var response = await client.SendMessageAsync(request, cts.Token);

// 发送消息不等待响应
var sent = client.SendMessage(message);
```

### 扩展数据存储

```csharp
// 使用索引器存储数据
client["userId"] = 12345;
client["loginTime"] = DateTime.Now;

// 读取数据
var userId = (Int32)client["userId"];
var loginTime = (DateTime)client["loginTime"];

// 使用 Items 字典
client.Items["userData"] = new UserData();
var userData = client.Items["userData"] as UserData;
```

### APM 性能追踪

```csharp
var client = new TcpSession
{
    Remote = uri,
    Tracer = tracer,  // APM追踪器
};

// 追踪的操作包括：
// - net:{Name}:Open       打开连接
// - net:{Name}:Close      关闭连接
// - net:{Name}:Send       发送数据
// - net:{Name}:Receive    接收数据
// - net:{Name}:SendMessage      发送消息
// - net:{Name}:SendMessageAsync 异步发送消息
```

---

## 最佳实践

### 1. 资源管理

```csharp
// 使用using确保资源释放
using var client = new TcpSession { Remote = uri };
client.Open();
// ... 使用客户端
// 自动调用Dispose

// 或者手动管理
var client = new TcpSession { Remote = uri };
try
{
    client.Open();
    // ... 使用客户端
}
finally
{
    client.Close("Done");
    client.Dispose();
}
```

### 2. 异常处理

```csharp
var client = new TcpSession { Remote = uri };

// 注册错误事件
client.Error += (s, e) =>
{
    Console.WriteLine($"错误 [{e.Action}]: {e.Exception.Message}");
};

try
{
    client.Open();
}
catch (TimeoutException ex)
{
    Console.WriteLine($"连接超时：{ex.Message}");
}
catch (SocketException ex)
{
    Console.WriteLine($"Socket错误：{ex.SocketErrorCode}");
}
```

### 3. 连接状态检查

```csharp
// 发送前检查连接状态
if (!client.Active)
{
    client.Open();
}

// 或者使用Open的幂等性
client.Open();  // 如果已连接，直接返回true
client.Send(data);
```

### 4. 日志配置

```csharp
// 生产环境
var client = new TcpSession
{
    Remote = uri,
    Log = XTrace.Log,
    LogSend = false,         // 关闭发送日志
    LogReceive = false,      // 关闭接收日志
};

// 调试环境
var client = new TcpSession
{
    Remote = uri,
    Log = XTrace.Log,
    LogSend = true,          // 开启发送日志
    LogReceive = true,       // 开启接收日志
    LogDataLength = 256,     // 日志数据长度
};
```

### 5. 心跳保活

```csharp
var client = new TcpSession
{
    Remote = uri,
    KeepAliveInterval = 60,  // 系统级KeepAlive，60秒
};

// 应用级心跳
var timer = new TimerX(async state =>
{
    if (client.Active)
    {
        client.Send("ping"u8.ToArray());
    }
}, null, 30_000, 30_000);  // 30秒发送一次
```

### 6. 断线重连

```csharp
var client = new TcpSession { Remote = uri };

client.Closed += async (s, e) =>
{
    Console.WriteLine("连接断开，尝试重连...");
    
    // 延迟重连
    await Task.Delay(3000);
    
    for (var i = 0; i < 5; i++)
    {
        try
        {
            if (client.Open())
            {
                Console.WriteLine("重连成功");
                return;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"重连失败：{ex.Message}");
        }
        
        await Task.Delay(3000 * (i + 1));  // 递增延迟
    }
    
    Console.WriteLine("重连失败，已放弃");
};
```

---

## 性能优化

### 1. 使用Span发送

```csharp
// 高性能发送，避免数组分配
ReadOnlySpan<Byte> data = stackalloc Byte[100];
// ... 填充数据
client.Send(data);
```

### 2. 缓冲区优化

```csharp
// 根据实际数据大小调整缓冲区
var client = new TcpSession
{
    Remote = uri,
    BufferSize = 64 * 1024,  // 64KB，适合大数据传输
};
```

### 3. 禁用Nagle算法

```csharp
// 对于实时性要求高的场景
var client = new TcpSession
{
    Remote = uri,
    NoDelay = true,  // 禁用Nagle算法，减少延迟
};
```

### 4. 批量发送

```csharp
// 使用数据包链式发送
var pk1 = new ArrayPacket(data1);
var pk2 = new ArrayPacket(data2);
pk1.Append(pk2);  // 链式连接

client.Send(pk1);  // 一次系统调用发送全部
```

### 5. 异步接收

```csharp
// 使用事件驱动模式，避免阻塞
client.Received += (s, e) =>
{
    // 在IO线程直接处理，高效
    ProcessData(e.Packet);
};

client.Open();  // Open后自动开始异步接收
```

---

## 常见问题

### Q: 如何处理粘包/拆包？

A: 使用 StandardCodec 或自定义协议处理器。

```csharp
client.Add<StandardCodec>();  // 4字节头部+数据
```

### Q: 连接超时怎么处理？

A: 设置 Timeout 属性，并捕获 TimeoutException。

```csharp
var client = new TcpSession
{
    Remote = new NetUri("tcp://192.0.2.1:12345"),
    Timeout = 5000,  // 5秒超时
};

try
{
    client.Open();
}
catch (TimeoutException)
{
    Console.WriteLine("连接超时");
}
```

### Q: 如何获取本地绑定地址？

A: 通过 Local 属性获取。

```csharp
client.Open();
Console.WriteLine($"本地地址：{client.Local}");
Console.WriteLine($"本地端口：{client.Port}");
```

### Q: UDP如何指定本地端口？

A: 设置 Port 属性。

```csharp
var client = new UdpServer
{
    Port = 8888,  // 绑定本地8888端口
    Remote = new NetUri("udp://127.0.0.1:12345"),
};
```

### Q: 如何发送文件？

A: 使用扩展方法。

```csharp
// 流式发送
using var stream = File.OpenRead("data.bin");
client.Send(stream);

// 分包发送（需要StandardCodec）
client.Add<StandardCodec>();
client.SendFile("data.bin");
```

### Q: 如何处理域名多IP？

A: NetUri 自动处理域名解析。

```csharp
var uri = new NetUri("tcp://example.com:80");
var addresses = uri.GetAddresses();  // 获取所有IP

// TcpSession会自动尝试所有IP直到连接成功
var client = new TcpSession { Remote = uri };
client.Open();  // 自动故障转移
```

### Q: 为什么收不到数据？

A: 检查以下几点：

1. 确保调用了 `Open()` 方法
2. 确保注册了 `Received` 事件
3. 检查防火墙设置
4. 检查服务端是否正确发送数据

```csharp
var client = new TcpSession { Remote = uri };

client.Received += (s, e) =>
{
    Console.WriteLine($"收到数据：{e.Packet.Length}字节");
};

client.Open();  // 必须调用Open
```

### Q: 如何实现广播？

A: 使用UDP广播地址。

```csharp
var client = new UdpServer
{
    Remote = new NetUri("udp://255.255.255.255:12345"),
};
client.Open();
client.Send("Broadcast message");
```

---

## 附录

### 关键类型速查

| 类型 | 说明 |
|------|------|
| `TcpSession` | TCP客户端，支持SSL/TLS |
| `UdpServer` | UDP客户端/服务端 |
| `UdpSession` | UDP会话（服务端使用） |
| `SessionBase` | 会话基类，封装通用功能 |
| `NetUri` | 网络地址，支持协议/主机/端口 |
| `ISocketClient` | 客户端接口 |
| `ISocketRemote` | 远程通信接口 |
| `IPipeline` | 消息管道接口 |
| `Handler` | 管道处理器基类 |
| `StandardCodec` | 标准编解码器 |

### 相关资源

- GitHub: https://github.com/NewLifeX/X
- Gitee: https://gitee.com/NewLifeX/X
- 文档: https://newlifex.com

---

*本文档更新：2025年7月*
