# 网络编解码器Handlers

## 概述

`NewLife.Net.Handlers` 命名空间提供一套可叠加的网络编解码器，用于解决 TCP 长连接的粘包拆包、消息序列化、请求-响应匹配等问题。编解码器以处理器（`Handler`）形式注册到管道（`Pipeline`），收包时正向解码，发包时逆向编码，支持任意组合叠加。

**命名空间**：`NewLife.Net.Handlers`  
**文档地址**：/core/net_handlers

## 核心类型

| 类型 | 说明 |
|------|------|
| `StandardCodec` | 标准封包编解码，使用 `DefaultMessage` 包格式（4 字节头），集成序列号与粘包处理 |
| `LengthFieldCodec` | 按长度字段拆包，支持偏移、大小端，适配 MQTT/LwM2M 等多种协议 |
| `MessageCodec<T>` | 请求-响应匹配队列，支持 `SendMessageAsync` 异步等待响应 |
| `JsonCodec` | JSON 文本消息编解码 |
| `SplitDataCodec` | 分隔符拆包（默认 `\r\n`） |
| `WebSocketCodec` | WebSocket 帧编解码 |

## 快速开始

### 使用 StandardCodec（最常用）

```csharp
using NewLife.Net;
using NewLife.Net.Handlers;

var server = new NetServer { Port = 12345 };

server.NewSession += (sender, e) =>
{
    // 每条连接注册编解码器（顺序不能颠倒）
    var pipeline = e.Session.Pipeline;
    pipeline.Add(new StandardCodec());
};

// 收到解码后的消息
server.Received += (sender, e) =>
{
    if (e.Message is DefaultMessage msg)
    {
        var text = msg.Payload?.ToStr();
        Console.WriteLine($"收到消息: {text}");
    }
};

server.Start();
```

### 使用 LengthFieldCodec

```csharp
// 2字节小端长度字段
pipeline.Add(new LengthFieldCodec { Size = 2 });

// 2字节大端长度字段（MQTT 等协议）
pipeline.Add(new LengthFieldCodec { Size = -2 });

// 头部有2字节协议标识，然后是2字节长度字段
pipeline.Add(new LengthFieldCodec { Offset = 2, Size = 2 });
```

### 异步请求-响应

```csharp
// 客户端
var client = new NetUri("tcp://127.0.0.1:12345").CreateRemote();
client.Pipeline.Add(new StandardCodec());
client.Open();

// 发送请求并等待响应（最多30秒）
var req  = new DefaultMessage { Payload = "ping".GetBytes() };
var resp = await client.SendMessageAsync(req);
if (resp is DefaultMessage reply)
    Console.WriteLine($"收到响应: {reply.Payload?.ToStr()}");
```

## API 参考

### StandardCodec

继承自 `MessageCodec<IMessage>`，使用 `DefaultMessage`（SRMP 协议）封包。

```csharp
public class StandardCodec : MessageCodec<IMessage>
{
    // 无额外公开属性，通过 MessageCodec<T> 基类配置
}
```

**DefaultMessage 格式**：

```
[1字节Flag][1字节Sequence][2字节长度][N字节Payload]
```

- `Flag`：数据类型标志（`DataKinds`），如 `Packet`=0、`String`=1
- `Sequence`：自动递增序列号，用于请求-响应匹配
- 长度字段包含 Payload 长度（小端 2 字节）

### LengthFieldCodec

```csharp
public class LengthFieldCodec : MessageCodec<IPacket>
{
    /// <summary>头部偏移字节数。长度字段前的固定头部大小，默认 0</summary>
    public Int32 Offset { get; set; }

    /// <summary>长度字段字节数。正值=小端，负值=大端，0=变长压缩编码，默认 2</summary>
    /// <remarks>
    /// 1/-1 : 1字节（最大255）
    /// 2/-2 : 2字节（最大65535）
    /// 4/-4 : 4字节（最大4GB）
    ///   0  : 变长压缩编码（类似 protobuf varint）
    /// </remarks>
    public Int32 Size { get; set; } = 2;

    /// <summary>缓存过期时间（毫秒）。不完整包超时后丢弃，默认 5_000</summary>
    public Int32 Expire { get; set; } = 5_000;
}
```

### MessageCodec\<T\>（基类）

```csharp
public class MessageCodec<T> : Handler
{
    /// <summary>请求-响应匹配队列</summary>
    public IMatchQueue? Queue { get; set; }

    /// <summary>匹配队列大小，默认 256</summary>
    public Int32 QueueSize { get; set; } = 256;

    /// <summary>等待响应超时（毫秒），默认 30_000</summary>
    public Int32 Timeout { get; set; } = 30_000;

    /// <summary>最大缓存数据（字节），默认 10MB</summary>
    public Int32 MaxCache { get; set; } = 10 * 1024 * 1024;

    /// <summary>用户数据包。true=读取时自动解包 Payload 返回，默认 true</summary>
    public Boolean UserPacket { get; set; } = true;
}
```

### SplitDataCodec

适用于文本协议（如 HTTP 头、自定义命令行协议）：

```csharp
public class SplitDataCodec : Handler
{
    /// <summary>分隔符，默认 "\r\n"</summary>
    public Byte[]? Separator { get; set; }
}
```

## 典型处理流程

**收包**（正向 `Read`）：

```
Socket收到字节流
   LengthFieldCodec.Read    （拆出完整包，去掉长度头）
   StandardCodec.Read       （解析 DefaultMessage，取 Payload）
   业务层 Received 事件（e.Message 为解码后的消息）
```

**发包**（逆向 `Write`）：

```
业务层 session.Send(message)
   StandardCodec.Write      （封装为 DefaultMessage，添加序列号）
   LengthFieldCodec.Write   （在头部写入长度字段）
   Socket 发送字节流
```

## 使用场景

### 场景一：自定义 JSON 协议

```csharp
// 使用 LengthFieldCodec 拆包 + JsonCodec 序列化
pipeline.Add(new LengthFieldCodec { Size = 4 });  // 4字节长度字段
pipeline.Add(new JsonCodec());

// 发送
session.SendMessage(new { cmd = "login", user = "alice" });

// 接收（e.Message 为已解码的对象）
server.Received += (sender, e) =>
{
    dynamic msg = e.Message;
    Console.WriteLine(msg?.cmd);
};
```

### 场景二：行协议（Telnet 风格）

```csharp
pipeline.Add(new SplitDataCodec());

// 发送
session.Send("HELLO\r\n");

// 接收（e.Packet 为去掉分隔符的单行数据）
server.Received += (sender, e) =>
{
    var line = e.Packet?.ToStr()?.Trim();
    // 处理命令行
};
```

### 场景三：RPC 异步调用

```csharp
// 客户端注册 StandardCodec
var client = new NetUri("tcp://127.0.0.1:8080").CreateRemote();
client.Pipeline.Add(new StandardCodec());
client.Open();

// 发送请求等待响应（如超时抛出 TimeoutException）
var req  = new DefaultMessage { Payload = JsonHelper.ToJson(query).GetBytes() };
var resp = await client.SendMessageAsync(req);

// 服务端回复
// session.ReplyMessage(e, new DefaultMessage { Payload = result.GetBytes() });
```

## 注意事项

- **编解码器按连接隔离**：每条连接新建实例，不可跨连接共享（尤其是带缓存的拆包器）。
- **拆包器在前**：`LengthFieldCodec` 必须在 `StandardCodec` 之前注册；否则收到的是粘包数据。
- **连接关闭时清理缓存**：`PacketCodec` 内部有字节缓存，断开连接时自动清理，不关闭连接不清理，长时间不活跃会导致内存滞留。
- **`MaxCache` 防内存爆炸**：超大或畸形包会导致缓存持续增长，务必根据协议最大包大小合理设置 `MaxCache`。
- **`Timeout` 按业务调整**：默认 30 秒适合大多数 RPC，对于文件传输等长耗时操作应适当增大。
