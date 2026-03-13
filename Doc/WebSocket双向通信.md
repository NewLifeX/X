# WebSocket双向通信

## 概述

NewLife.Core 支持 WebSocket 协议的客户端与服务端实现，基于原有的管道（Pipeline）和 TCP 连接层进行封装。`WebSocketClient` 继承自 `TcpSession`，内置 `WebSocketCodec` 编解码器，自动完成握手、心跳（Ping/Pong）、帧编解码等底层细节，应用层只需处理消息收发。

**命名空间**：`NewLife.Net`（客户端）、`NewLife.Http`（消息类型）  
**文档地址**：/core/websocket

## 核心类型

| 类型 | 说明 |
|------|------|
| `WebSocketClient` | WebSocket 客户端，继承 `TcpSession`，支持 ws:// 和 wss:// |
| `WebSocketSession` | 服务端 WebSocket 会话，适配现有 `NetServer` 体系 |
| `WebSocketCodec` | 管道编解码器，封装 WebSocket 帧格式 |
| `WebSocketMessage` | WebSocket 消息帧模型 |
| `WebSocketMessageType` | 消息类型枚举（Text/Binary/Ping/Pong/Close） |

## WebSocketMessage 消息帧

```csharp
public class WebSocketMessage : IDisposable
{
    /// <summary>是否最终帧（FIN bit）</summary>
    public Boolean Fin { get; set; }

    /// <summary>消息类型（Opcode）</summary>
    public WebSocketMessageType Type { get; set; }

    /// <summary>掩码密钥（客户端发送时自动设置）</summary>
    public Byte[]? MaskKey { get; set; }

    /// <summary>消息负载数据</summary>
    public IPacket? Payload { get; set; }

    /// <summary>关闭状态码（Close帧时有效）</summary>
    public Int32 CloseStatus { get; set; }

    /// <summary>关闭原因描述（Close帧时有效）</summary>
    public String? StatusDescription { get; set; }
}
```

### WebSocketMessageType 枚举

```csharp
public enum WebSocketMessageType
{
    Data   = 0,   // 附加数据（分片续包）
    Text   = 1,   // 文本消息（UTF-8）
    Binary = 2,   // 二进制消息
    Close  = 8,   // 连接关闭
    Ping   = 9,   // 心跳请求
    Pong   = 10,  // 心跳响应
}
```

## 快速开始

### 客户端连接与收发

```csharp
using NewLife.Net;

var client = new WebSocketClient("ws://127.0.0.1:8080/ws");

// 收到消息
client.Received += (sender, e) =>
{
    if (e.Message is WebSocketMessage msg)
    {
        if (msg.Type == WebSocketMessageType.Text)
            Console.WriteLine($"收到文本: {msg.Payload?.ToStr()}");
        else if (msg.Type == WebSocketMessageType.Binary)
            Console.WriteLine($"收到二进制: {msg.Payload?.Total} 字节");
    }
};

await client.OpenAsync();

// 发送文本
await client.SendTextAsync("Hello Server!");

// 发送二进制
await client.SendBinaryAsync("data".GetBytes());

// 发送结构化消息
var msg2 = new WebSocketMessage { Type = WebSocketMessageType.Text, Payload = "ping".GetBytes() };
await client.SendMessageAsync(msg2);
```

### 自定义请求头（携带 Token）

```csharp
var client = new WebSocketClient("ws://api.example.com/events");
client.SetRequestHeader("Authorization", "Bearer " + accessToken);
client.SetRequestHeader("X-App-Version", "2.1.0");

await client.OpenAsync();
```

## API 参考

### WebSocketClient

```csharp
public class WebSocketClient : TcpSession
{
    /// <summary>服务器 URI（ws:// 或 wss://）</summary>
    public Uri Uri { get; set; }

    /// <summary>心跳间隔。默认 120 秒（2分钟发一次 Ping）</summary>
    public TimeSpan KeepAlive { get; set; } = TimeSpan.FromSeconds(120);

    /// <summary>自定义请求头（握手时携带）</summary>
    public IDictionary<String, String?>? RequestHeaders { get; set; }

    /// <summary>设置单个请求头</summary>
    public void SetRequestHeader(String headerName, String? headerValue);

    /// <summary>发送 WebSocket 消息帧</summary>
    public Task SendMessageAsync(WebSocketMessage message, CancellationToken cancellationToken = default);

    /// <summary>发送文本消息（UTF-8）</summary>
    public Task SendTextAsync(String text, CancellationToken cancellationToken = default);

    /// <summary>发送文本消息（字节）</summary>
    public Task SendTextAsync(Byte[] data, CancellationToken cancellationToken = default);

    /// <summary>发送二进制消息</summary>
    public Task SendBinaryAsync(IPacket data, CancellationToken cancellationToken = default);

    /// <summary>握手（建立 WebSocket 升级请求），Open 阶段自动调用</summary>
    public static Boolean Handshake(ISocketClient client, Uri uri);
}
```

### WebSocketCodec（管道处理器）

```csharp
public class WebSocketCodec : Handler
{
    /// <summary>
    /// 用户数据包模式。
    /// false（默认）：上层 Received 事件的 e.Message 为 WebSocketMessage 对象；
    /// true：自动提取 Payload，e.Packet 为原始数据，忽略帧类型
    /// </summary>
    public Boolean UserPacket { get; set; }
}
```

## 握手流程（HTTP Upgrade）

```
客户端                                  服务端
  |  TCP建连                              |
  |>|
  |  GET /ws HTTP/1.1                     |
  |  Upgrade: websocket                   |
  |  Connection: Upgrade                  |
  |  Sec-WebSocket-Key: <随机Base64>      |
  |>|
  |                       HTTP/1.1 101    |
  |           Sec-WebSocket-Accept: <哈希>|
  |<|
  |  [WebSocket双向帧传输]                |
```

握手完全由 `WebSocketCodec.Open` 和 `WebSocketSession.HandeShake` 自动处理，无需应用层介入。

## 使用场景

### 场景一：实时消息推送（服务端主动推送）

```csharp
// 服务端（继承 NetServer）
public class PushServer : NetServer<WebSocketSession>
{
    protected override void OnStart()
    {
        base.OnStart();
        // 启动定时推送
        TimerX.Delay(DoPush, 1000);
    }

    private void DoPush(Object state)
    {
        var msg = new WebSocketMessage
        {
            Type    = WebSocketMessageType.Text,
            Payload = $"{{\"time\":\"{DateTime.Now:HH:mm:ss}\"}}".GetBytes(),
        };

        foreach (var session in Sessions.Values.OfType<WebSocketSession>())
            session.Send(msg.ToPacket());
    }
}
```

### 场景二：客户端订阅实时报价

```csharp
var client = new WebSocketClient("wss://stream.example.com/quotes");
client.KeepAlive = TimeSpan.FromSeconds(30);

client.Received += (sender, e) =>
{
    if (e.Message is WebSocketMessage msg && msg.Type == WebSocketMessageType.Text)
    {
        var quote = JsonHelper.ToObject<Quote>(msg.Payload?.ToStr());
        UpdateUI(quote);
    }
};

await client.OpenAsync();

// 订阅频道
await client.SendTextAsync("{\"cmd\":\"subscribe\",\"channel\":\"BTCUSDT\"}");
```

### 场景三：双向 RPC 通道

```csharp
// 客户端发送请求
var req = new
{
    id  = Interlocked.Increment(ref _seq),
    cmd = "getUserInfo",
    uid = 12345,
};
await client.SendTextAsync(JsonHelper.ToJson(req));

// 服务端响应
server.Received += (sender, e) =>
{
    if (e.Message is WebSocketMessage msg && msg.Type == WebSocketMessageType.Text)
    {
        var request = JsonHelper.ToObject<RpcRequest>(msg.Payload?.ToStr());
        var result  = ProcessRpc(request);

        if (e.Session is WebSocketSession session)
            session.Send(JsonHelper.ToJson(result).GetBytes());
    }
};
```

## 注意事项

- **Payload 零拷贝，生命周期短**：`WebSocketMessage.Payload` 直接切片自接收缓冲区，不能跨异步调用持有。需要延迟使用时，先 `payload.ToArray()` 深拷贝。
- **掩码破坏性操作**：客户端服务端的帧带掩码，解帧时原地 XOR 解码，不要对同一缓冲重复解析。
- **`KeepAlive` 默认 120 秒**：若服务器配置了更短的连接超时，应缩小 `KeepAlive` 以防断连。
- **wss:// 需要 TLS 支持**：在 .NET Framework 上需要确保 TLS 可用（`SslStream`），且服务器证书有效。
- **服务端会话隔离**：每条连接对应一个 `WebSocketSession` 实例，不要在多个 Session 之间共享状态，否则需加锁。
- **消息分片**：当前实现中 `Fin=false` 的分片帧会被丢弃，仅处理完整帧（FIN=1）；应用层发送时无需分片。
