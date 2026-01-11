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
│  ISocketClient (客户端接口)                                          │
│  ├── uri.CreateRemote() (推荐创建方式)                               │
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

### 最简单的 TCP 客户端（推荐方式）

```csharp
using NewLife;
using NewLife.Log;
using NewLife.Net;

// 创建TCP客户端（推荐方式）
var uri = new NetUri("tcp://127.0.0.1:12345");
var client = uri.CreateRemote();
client.Log = XTrace.Log;

// 打开连接
client.Open();

// 发送数据
client.Send("Hello Server");

// 同步接收数据
using var pk = client.Receive();
client.WriteLog("收到：{0}", pk?.ToStr());

// 关闭连接
client.Close("测试完成");
```

### 最简单的 UDP 客户端（推荐方式）

```csharp
using NewLife;
using NewLife.Log;
using NewLife.Net;

// 创建UDP客户端（推荐方式）
var uri = new NetUri("udp://127.0.0.1:12345");
var client = uri.CreateRemote();
client.Log = XTrace.Log;

// 发送数据（UDP无需显式Open，发送时自动打开）
client.Send("Hello UDP");

// 接收数据
using var pk = await client.ReceiveAsync(default);
client.WriteLog("收到：{0}", pk?.ToStr());

// 关闭连接
client.Close("测试完成");
```

### 使用异步接收模式

```csharp
using NewLife;
using NewLife.Log;
using NewLife.Net;

// 创建客户端
var uri = new NetUri("tcp://127.0.0.1:12345");
var client = uri.CreateRemote();
client.Name = "小tcp客户";
client.Log = XTrace.Log;

// 关闭默认的异步模式，使用手动接收
if (client is TcpSession tcp) tcp.MaxAsync = 0;

// 接收服务端握手（内部自动建立连接）
using var rs = await client.ReceiveAsync(default);
client.WriteLog("收到：{0}", rs?.ToStr());

// 发送数据
client.WriteLog("发送：Hello NewLife");
client.Send("Hello NewLife");

// 接收响应
using var rs2 = await client.ReceiveAsync(default);
client.WriteLog("收到：{0}", rs2?.ToStr());

// 关闭连接
client.Close("测试完成");
```

### 使用事件驱动模式

```csharp
using NewLife;
using NewLife.Log;
using NewLife.Net;

var uri = new NetUri("tcp://127.0.0.1:12345");
var client = uri.CreateRemote();
client.Log = XTrace.Log;

// 连接打开事件
client.Opened += (s, e) => XTrace.WriteLine("已连接");

// 连接关闭事件
client.Closed += (s, e) => XTrace.WriteLine("已断开");

// 数据接收事件
client.Received += (s, e) =>
{
    XTrace.WriteLine("收到 [{0}]: {1}", e.Packet?.Length, e.Packet?.ToStr());
};

// 错误事件
client.Error += (s, e) =>
{
    XTrace.WriteLine("错误 [{0}]: {1}", e.Action, e.Exception.Message);
};

// 打开连接（会自动开始异步接收）
client.Open();

// 发送数据
client.Send("Hello");

// 保持运行
Console.ReadLine();

client.Close("Exit");
```

---

## 核心组件

### NetUri - 网络地址

NetUri 用于描述网络地址，支持协议、主机、端口，是创建客户端的入口。

```csharp
// 从字符串解析（推荐）
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

// 创建客户端（推荐方式）
var client = uri.CreateRemote();
```

### CreateRemote - 创建客户端

`CreateRemote()` 是 NetUri 的扩展方法，根据协议类型自动创建对应的客户端实例。

```csharp
// TCP客户端
var uri = new NetUri("tcp://127.0.0.1:12345");
var client = uri.CreateRemote();  // 返回 TcpSession

// UDP客户端
var uri = new NetUri("udp://127.0.0.1:12345");
var client = uri.CreateRemote();  // 返回 UdpServer

// HTTP/HTTPS（自动启用SSL）
var uri = new NetUri("http://example.com:443");
var client = uri.CreateRemote();  // 返回 TcpSession with SSL

// WebSocket
var uri = new NetUri("ws://127.0.0.1:8080");
var client = uri.CreateRemote();  // 返回 WebSocketClient
```

### ISocketClient - 客户端接口

所有客户端都实现 `ISocketClient` 接口，提供统一的操作方式。

```csharp
var uri = new NetUri("tcp://127.0.0.1:12345");
var client = uri.CreateRemote();

// 基本属性
client.Name = "MyClient";           // 客户端名称，用于日志
client.Timeout = 5000;              // 超时时间（毫秒）
client.Log = XTrace.Log;            // 日志对象

// 连接管理
client.Open();                      // 同步打开
await client.OpenAsync();           // 异步打开
client.Close("reason");             // 同步关闭
await client.CloseAsync("reason");  // 异步关闭
var active = client.Active;         // 连接状态

// 数据发送
client.Send(data);                  // 发送字节数组
client.Send("Hello");               // 发送字符串（扩展方法）
client.Send(packet);                // 发送数据包

// 数据接收
using var pk = client.Receive();           // 同步接收
using var pk = await client.ReceiveAsync();// 异步接收

// 事件
client.Opened += (s, e) => { };     // 打开事件
client.Closed += (s, e) => { };     // 关闭事件
client.Received += (s, e) => { };   // 接收事件
client.Error += (s, e) => { };      // 错误事件
```

### TcpSession - TCP客户端

TcpSession 是增强的TCP客户端，支持SSL/TLS安全连接。

#### 基本属性配置

```csharp
var uri = new NetUri("tcp://127.0.0.1:12345");
var client = uri.CreateRemote();

// 转换为具体类型以访问更多属性
if (client is TcpSession tcp)
{
    // TCP选项
    tcp.NoDelay = true;                 // 禁用Nagle算法（低延迟）
    tcp.KeepAliveInterval = 30;         // KeepAlive间隔（秒）
    
    // SSL配置
    tcp.SslProtocol = SslProtocols.Tls12;  // SSL协议版本
    tcp.Certificate = cert;                 // 客户端证书（可选）
    
    // 接收配置
    tcp.MaxAsync = 1;                   // 最大并行接收数，TCP默认1
    tcp.BufferSize = 8192;              // 接收缓冲区大小
    
    // 日志配置
    tcp.LogSend = true;                 // 记录发送日志
    tcp.LogReceive = true;              // 记录接收日志
    tcp.LogDataLength = 64;             // 日志数据长度
    
    // 追踪配置
    tcp.Tracer = tracer;                // APM追踪器
}

client.Open();
```

#### 数据发送

```csharp
var client = new NetUri("tcp://127.0.0.1:12345").CreateRemote();
client.Open();

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
var uri = new NetUri("tcp://127.0.0.1:12345");
var client = uri.CreateRemote();

// 关闭异步接收，使用手动接收模式
if (client is TcpSession tcp) tcp.MaxAsync = 0;

client.Open();

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

// 事件驱动接收（推荐，需要保持 MaxAsync >= 1）
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
// 创建UDP客户端
var uri = new NetUri("udp://127.0.0.1:12345");
var client = uri.CreateRemote();
client.Log = XTrace.Log;

// 关闭异步接收，使用手动接收模式
if (client is UdpServer udp) udp.MaxAsync = 0;

// 发送数据（UDP无需显式Open，发送时自动打开）
client.Send("Hello UDP");

// 接收数据
using var pk = await client.ReceiveAsync(default);
client.WriteLog("收到：{0}", pk?.ToStr());

// 关闭连接
client.Close("测试完成");
```

#### UDP服务端配置

```csharp
var uri = new NetUri("udp://0.0.0.0:12345");
var server = uri.CreateRemote() as UdpServer;

// 服务端配置
server.Port = 12345;                    // 本地端口
server.ReuseAddress = true;             // 地址重用
server.Loopback = false;                // 是否接收环回数据
server.SessionTimeout = 20 * 60;        // 会话超时时间（秒）
server.MaxAsync = Environment.ProcessorCount * 16 / 10; // UDP默认CPU*1.6
server.BufferSize = 8192;

// 新会话事件
server.NewSession += (s, e) =>
{
    var session = e.Session as UdpSession;
    XTrace.WriteLine("新会话：{0}", session.Remote);
    
    // 会话数据接收
    session.Received += (ss, ee) =>
    {
        XTrace.WriteLine("会话 {0} 收到：{1}", session.ID, ee.Packet?.ToStr());
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

var uri = new NetUri("tcp://127.0.0.1:12345");
var client = uri.CreateRemote();

// 使用扩展方法添加处理器
client.Add<StandardCodec>();    // 标准编解码器（4字节头部+数据）
client.Add<JsonCodec>();        // JSON编解码器

// 或者直接设置Pipeline
client.Pipeline = new Pipeline();
client.Pipeline.Add(new StandardCodec());
client.Pipeline.Add(new JsonCodec());

client.Open();
```

#### 标准编解码器

StandardCodec 使用 4 字节头部标识数据长度，自动处理粘包/拆包。

```csharp
var client = new NetUri("tcp://127.0.0.1:12345").CreateRemote();
client.Add<StandardCodec>();
client.Open();

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
var client = new NetUri("tcp://127.0.0.1:12345").CreateRemote();
client.Add(new MyCodec());
```

---

## 高级功能

### SSL/TLS 安全连接

```csharp
// 客户端SSL（不验证服务端证书）
var uri = new NetUri("tcp://127.0.0.1:443");
var client = uri.CreateRemote();
if (client is TcpSession tcp)
{
    tcp.SslProtocol = SslProtocols.Tls12 | SslProtocols.Tls13;
}
client.Open();

// 客户端SSL（使用客户端证书）
var clientCert = new X509Certificate2("client.pfx", "password");
var uri = new NetUri("tcp://127.0.0.1:443");
var client = uri.CreateRemote();
if (client is TcpSession tcp)
{
    tcp.SslProtocol = SslProtocols.Tls12;
    tcp.Certificate = clientCert;  // 客户端证书
}
client.Open();
```

### 消息请求响应模式

```csharp
var client = new NetUri("tcp://127.0.0.1:12345").CreateRemote();
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
var client = new NetUri("tcp://127.0.0.1:12345").CreateRemote();

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
var client = new NetUri("tcp://127.0.0.1:12345").CreateRemote();
client.Tracer = tracer;  // APM追踪器

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
var uri = new NetUri("tcp://127.0.0.1:12345");
using var client = uri.CreateRemote();
client.Open();
// ... 使用客户端
// 自动调用Dispose

// 或者手动管理
var client = uri.CreateRemote();
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
var client = new NetUri("tcp://192.0.2.1:12345").CreateRemote();

// 注册错误事件
client.Error += (s, e) =>
{
    XTrace.WriteLine("错误 [{0}]: {1}", e.Action, e.Exception.Message);
};

try
{
    client.Timeout = 5000;
    client.Open();
}
catch (TimeoutException ex)
{
    XTrace.WriteLine("连接超时：{0}", ex.Message);
}
catch (SocketException ex)
{
    XTrace.WriteLine("Socket错误：{0}", ex.SocketErrorCode);
}
```

### 3. 连接状态检查

```csharp
var client = new NetUri("tcp://127.0.0.1:12345").CreateRemote();

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
var client = new NetUri("tcp://127.0.0.1:12345").CreateRemote();
client.Log = XTrace.Log;
client.LogSend = false;         // 关闭发送日志
client.LogReceive = false;      // 关闭接收日志

// 调试环境
var client = new NetUri("tcp://127.0.0.1:12345").CreateRemote();
client.Log = XTrace.Log;
client.LogSend = true;          // 开启发送日志
client.LogReceive = true;       // 开启接收日志
if (client is SessionBase sb) sb.LogDataLength = 256;  // 日志数据长度
```

### 5. 心跳保活

```csharp
var uri = new NetUri("tcp://127.0.0.1:12345");
var client = uri.CreateRemote();
if (client is TcpSession tcp)
{
    tcp.KeepAliveInterval = 60;  // 系统级KeepAlive，60秒
}
client.Open();

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
var uri = new NetUri("tcp://127.0.0.1:12345");
var client = uri.CreateRemote();

client.Closed += async (s, e) =>
{
    XTrace.WriteLine("连接断开，尝试重连...");
    
    // 延迟重连
    await Task.Delay(3000);
    
    for (var i = 0; i < 5; i++)
    {
        try
        {
            if (client.Open())
            {
                XTrace.WriteLine("重连成功");
                return;
            }
        }
        catch (Exception ex)
        {
            XTrace.WriteLine("重连失败：{0}", ex.Message);
        }
        
        await Task.Delay(3000 * (i + 1));  // 递增延迟
    }
    
    XTrace.WriteLine("重连失败，已放弃");
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
var client = new NetUri("tcp://127.0.0.1:12345").CreateRemote();
if (client is SessionBase sb)
{
    sb.BufferSize = 64 * 1024;  // 64KB，适合大数据传输
}
```

### 3. 禁用Nagle算法

```csharp
// 对于实时性要求高的场景
var client = new NetUri("tcp://127.0.0.1:12345").CreateRemote();
if (client is TcpSession tcp)
{
    tcp.NoDelay = true;  // 禁用Nagle算法，减少延迟
}
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
var client = new NetUri("tcp://127.0.0.1:12345").CreateRemote();
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
var client = new NetUri("tcp://127.0.0.1:12345").CreateRemote();
client.Add<StandardCodec>();  // 4字节头部+数据
```

### Q: 连接超时怎么处理？

A: 设置 Timeout 属性，并捕获 TimeoutException。

```csharp
var client = new NetUri("tcp://192.0.2.1:12345").CreateRemote();
client.Timeout = 5000;  // 5秒超时

try
{
    client.Open();
}
catch (TimeoutException)
{
    XTrace.WriteLine("连接超时");
}
```

### Q: 如何获取本地绑定地址？

A: 通过 Local 属性获取。

```csharp
var client = new NetUri("tcp://127.0.0.1:12345").CreateRemote();
client.Open();
XTrace.WriteLine("本地地址：{0}", client.Local);
XTrace.WriteLine("本地端口：{0}", client.Port);
```

### Q: UDP如何指定本地端口？

A: 设置 Port 属性。

```csharp
var client = new NetUri("udp://127.0.0.1:12345").CreateRemote();
client.Port = 8888;  // 绑定本地8888端口
```

### Q: 如何发送文件？

A: 使用扩展方法。

```csharp
var client = new NetUri("tcp://127.0.0.1:12345").CreateRemote();
client.Open();

// 流式发送
using var stream = File.OpenRead("data.bin");
client.Send(stream);

// 分包发送（需要StandardCodec）
client.Add<StandardCodec>();
client.SendFile("data.bin");
```

### Q: 如何处理域名多IP？

A: NetUri 自动处理域名解析，CreateRemote() 创建的客户端会自动尝试所有IP。

```csharp
var uri = new NetUri("tcp://example.com:80");
var addresses = uri.GetAddresses();  // 获取所有IP

// 客户端会自动故障转移
var client = uri.CreateRemote();
client.Open();  // 自动尝试所有IP直到连接成功
```

### Q: 为什么收不到数据？

A: 检查以下几点：

1. 确保调用了 `Open()` 方法
2. 如果使用事件驱动，确保 `MaxAsync >= 1`
3. 如果使用手动接收，设置 `MaxAsync = 0`
4. 检查防火墙设置
5. 检查服务端是否正确发送数据

```csharp
var client = new NetUri("tcp://127.0.0.1:12345").CreateRemote();

// 事件驱动模式（默认）
client.Received += (s, e) =>
{
    XTrace.WriteLine("收到数据：{0}字节", e.Packet?.Length);
};
client.Open();  // 必须调用Open

// 或者手动接收模式
if (client is TcpSession tcp) tcp.MaxAsync = 0;
client.Open();
using var pk = await client.ReceiveAsync(default);
```

### Q: 如何实现广播？

A: 使用UDP广播地址。

```csharp
var client = new NetUri("udp://255.255.255.255:12345").CreateRemote();
client.Open();
client.Send("Broadcast message");
```

---

## 附录

### 关键类型速查

| 类型 | 说明 |
|------|------|
| `NetUri` | 网络地址，支持协议/主机/端口 |
| `CreateRemote()` | NetUri扩展方法，创建客户端 |
| `TcpSession` | TCP客户端，支持SSL/TLS |
| `UdpServer` | UDP客户端/服务端 |
| `UdpSession` | UDP会话（服务端使用） |
| `SessionBase` | 会话基类，封装通用功能 |
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
