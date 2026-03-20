---
name: networking
description: 使用 NewLife.Net 构建高性能 TCP/UDP/WebSocket 网络服务和客户端
---

# NewLife 网络编程使用指南

## 适用场景

- 构建高性能 TCP/UDP 网络服务器（实测 2266 万 tps）
- 自定义二进制协议通信
- WebSocket 服务端和客户端
- IoT 设备接入网关
- 粘包拆包处理

## 服务端开发

### 基本模式（推荐）

```csharp
// 自定义服务器
class MyServer : NetServer<MySession> { }

// 自定义会话（每个连接一个实例）
class MySession : NetSession<MyServer>
{
    protected override void OnConnected()
    {
        base.OnConnected();
        WriteLog("客户端连接 {0}", Remote);
    }

    protected override void OnReceive(ReceivedEventArgs e)
    {
        base.OnReceive(e);
        var data = e.Packet;
        // 业务处理...

        // 回复
        Send(data);
    }

    protected override void OnDisconnected(String reason)
    {
        base.OnDisconnected(reason);
    }
}

// 启动
var server = new MyServer
{
    Port = 8080,
    Log = XTrace.Log,
    Tracer = tracer,
#if DEBUG
    LogSend = true,
    LogReceive = true,
#endif
};
server.Start();
```

### 事件模式（简单场景）

```csharp
var server = new NetServer { Port = 8080 };
server.Received += (sender, e) =>
{
    if (sender is INetSession session)
        session.Send(e.Packet);  // Echo
};
server.Start();
```

### 群发

```csharp
// 群发给所有在线客户端
await server.SendAllAsync(data);

// 带条件群发
await server.SendAllAsync(data, s => s["VIP"] is true);
```

## 客户端开发

### 创建客户端

```csharp
// TCP 客户端
var client = new NetUri("tcp://127.0.0.1:8080").CreateRemote();
client.Log = XTrace.Log;
client.Open();

// 发送
client.Send("Hello");

// 事件接收
client.Received += (sender, e) => { var data = e.Packet; };

// 同步接收
using var pk = client.Receive();

// 异步接收
using var pk = await client.ReceiveAsync(cancellationToken);

client.Close("完成");
```

### 请求-响应模式

```csharp
var client = new NetUri("tcp://127.0.0.1:8080").CreateRemote();
client.Add<StandardCodec>();  // 添加编解码器
client.Open();

// 发送并等待响应
var response = await client.SendMessageAsync(payload, cancellationToken);
```

## 编解码器（粘包拆包）

### 内置编解码器

| 编解码器 | 场景 |
| --------- | ------ |
| `StandardCodec` | 4 字节头部（Flag+Seq+Length），请求响应匹配 |
| `LengthFieldCodec` | 长度字段头部，可配置偏移和大小 |
| `SplitDataCodec` | 分隔符拆包（默认 `\r\n`） |
| `WebSocketCodec` | WebSocket 帧 |
| `JsonCodec` | JSON 编解码（通常与其他 Codec 级联） |

### 添加编解码器

```csharp
// 服务端
server.Add<StandardCodec>();

// 多层级联（顺序：底层先添加）
server.Add<StandardCodec>();  // 底层：粘包拆包
server.Add<JsonCodec>();      // 上层：JSON 编解码
```

### 管道数据流向

```text
接收：Socket → Codec1.Read → Codec2.Read → OnReceive
发送：SendMessage → Codec2.Write → Codec1.Write → Socket
```

## SSL/TLS

```csharp
// 服务端
var server = new NetServer
{
    Port = 443,
    SslProtocol = SslProtocols.Tls12,
    Certificate = new X509Certificate2("server.pfx", "password"),
};

// 客户端
var client = new NetUri("tcp://host:443").CreateRemote();
if (client is TcpSession tcp)
    tcp.SslProtocol = SslProtocols.Tls12;
```

## 注意事项

- 端口号 `0` 表示随机端口（测试时推荐）
- `NetServer` 默认同时监听 IPv4 + IPv6
- 会话内通过 `ServiceProvider` 获取 DI 服务
- `CreateRemote()` 根据 URI 协议自动返回 `TcpSession`/`UdpServer`/`WebSocketClient`
- 编解码器先添加的靠近 Socket 层，后添加的靠近业务层
- `SendMessage` 通过管道编码，`Send` 直接发送原始数据
- `SendMessageAsync` 发送并等待响应，需要匹配编解码器（如 `StandardCodec`）
