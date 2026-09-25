# IMessage 消息帮助手册

> ⚠️ **旧栈文档（v12 已退役）**：`IFrameMessage` 与消息帧方法（`TryParseHeader`/`ReadFrame`/`Build`/`BuildHeader` 等）已从 `IMessage`/`Message` 整删。现行契约（`IMessage` 消息契约 + `IMessageCodec` 协议契约）见《消息协议栈》，本文章节仅作历史参考。

本文档基于源码 `NewLife.Core/Messaging/IMessage.cs`，说明 `IMessage` 接口及其基类 `Message`、`DefaultMessage` 的设计、用法与注意事项。

> 关键词：请求-响应模式、Dispose 释放链、IOwnerPacket 池化内存、RPC 消息生命周期。

> **契约指引（v12 中间态，已被后续收尾取代）**：`IMessage : IFrameMessage` 曾为单一消息契约——帧读写（`TryParseHeader` 定界 / `ReadFrame` 整帧读体 / `BindBody` 流式体绑定 / `SetBody` 发送体绑定 / `Build` 整帧构建 / `BuildHeader` 头 + 流式体 / `Body` 读取器）在帧契约 `IFrameMessage`，请求-响应语义（`Reply/Error/OneWay` 三个独立布尔 + `Payload` + `CreateReply`）由消息契约在其上追加；`TryParse` 为帧长解析便捷入口（供 `GetFrameLength` 委托绑定）。v12 破坏性变更：旧名 `Read/ToPacket/ToHeaderPacket` 已删除，下游改用 `ReadFrame/Build/BuildHeader` 并重新编译；`WebSocketMessage` 仅实现帧契约。构建采用转移语义：`Build` 成功后消息不再持有体（`SetBody(null)` 表示所有权转移、不归还）；流式体不可整帧构建，请改用 `BuildHeader` + 流式发送。该中间态后续亦删除 `IFrameMessage`，现行契约见《消息协议栈》（`IMessage` 消息契约 + `IMessageCodec` 协议契约）。

---

## 1. 设计目标

- **统一消息抽象**：为 RPC、网络通信提供请求-响应消息的标准接口。
- **资源安全释放**：`IMessage : IDisposable`，释放消息时自动归还内部 `Payload` 的池化内存。
- **灵活可扩展**：通过 `Message` 基类与 `DefaultMessage` 实现类，支持自定义协议格式。

---

## 2. 接口定义

```csharp
public interface IMessage : IFrameMessage    // 帧契约：TryParseHeader/ReadFrame/Body/SetBody/Build/BuildHeader 等
{
    Boolean Reply { get; set; }      // 是否响应消息
    Boolean Error { get; set; }      // 是否有错
    Boolean OneWay { get; set; }     // 单向请求
    IPacket? Payload { get; set; }   // 负载数据（消息体句柄视图，流式模式为 null）

    IMessage CreateReply();          // 根据请求创建配对响应
}
```

### 2.1 核心属性

| 属性 | 说明 |
|------|------|
| `Reply` | `true` 表示响应消息，`false` 表示请求消息 |
| `Error` | `true` 表示处理过程中发生错误 |
| `OneWay` | `true` 表示单向请求，不需要等待响应 |
| `Payload` | 消息负载数据，类型为 `IPacket?`，可以是 `ArrayPacket`、`OwnerPacket` 等任意实现 |

### 2.2 核心方法

- **`CreateReply()`**：根据请求消息创建配对的响应消息，继承序列号等关键属性。仅请求消息可调用。
- **`ReadFrame(IPacket pk)`**（帧契约）：从完整帧解析消息头和负载。帧头可跨节点（首段不足时自动拼入栈缓冲拼读，负载可继续为链式节点）。
- **`Build()`**（帧契约）：将消息构建为完整数据包，用于网络发送；构建采用转移语义。

---

## 3. 基类 Message

`Message` 提供 `IMessage` 的基础实现：

```csharp
public class Message : IMessage
{
    public Boolean Reply { get; set; }
    public Boolean Error { get; set; }
    public Boolean OneWay { get; set; }
    public IPacket? Payload { get; set; }        // 消息体句柄视图
    public LimitedReader? Body => _body;         // 消息体读取视图（帧契约）

    public void Dispose() { ... }
    protected virtual void Dispose(Boolean disposing) { ... }

    public virtual IMessage CreateReply() { ... }
    public virtual Boolean ReadFrame(IPacket pk) { ... }
    public virtual IPacket? Build() { ... }      // 转移语义：构建后消息不再持有体
    public virtual void Reset() { ... }
}
```

### 3.1 释放机制

`Message.Dispose(disposing)` 的核心逻辑：

```csharp
protected virtual void Dispose(Boolean disposing)
{
    if (disposing)
    {
        Payload.TryDispose();  // 安全释放 Payload
        Payload = null;
    }
}
```

`TryDispose` 是 NewLife 的通用扩展方法，检查对象是否实现 `IDisposable`，若是则调用 `Dispose()`。

---

## 4. DefaultMessage（SRMP 标准消息）

### 4.1 协议格式

```
1 Flag + 1 Sequence + 2 Length + N Payload
```

| 字段 | 字节数 | 说明 |
|------|--------|------|
| Flag | 1 | 高 2 位为消息模式（00 请求/01 单向/10 响应/11 响应+错误），低 6 位为数据类型 |
| Sequence | 1 | 序列号，用于请求-响应配对 |
| Length | 2 | 小端字节序，负载数据长度（不含头部 4 字节） |
| Payload | N | 负载数据 |

超大包支持：当 Length 为 `0xFFFF` 时，后续 4 字节为实际长度。

### 4.2 示例

```
请求 Open:  01-01-04-00-"Open"
响应 OK:    81-01-02-00-"OK"
```

---

## 5. 与 IOwnerPacket 的联动释放设计（核心亮点）

### 5.1 问题背景

在 RPC / 网络通信中，底层需要高效接收数据：

1. 使用 `OwnerPacket` 从 `ArrayPool` 租用缓冲区，避免频繁 GC。
2. 协议解析后，负载数据通过 `Slice` 切片共享底层缓冲区（零拷贝）。
3. 负载被包装到 `IMessage.Payload` 中，传递给上层业务代码。

**核心问题**：谁来释放池化内存？

### 5.2 设计方案

```
底层网络接收
  → new OwnerPacket(bufferSize)        // 从 ArrayPool 租用
  → socket.ReceiveAsync(...)           // 填充数据
  → DefaultMessage.Read(ownerPacket)   // 解析协议
    → Slice(4, len)                     // 切出共享切片给 Payload（引用计数）
  → 返回 IMessage 给上层
  → 上层使用完毕
  → msg.Dispose()                      // 自动归还池化内存
```

**释放链路**：

```
IMessage.Dispose()
  → Message.Dispose(disposing: true)
    → Payload.TryDispose()
      → OwnerPacket.Dispose()
        → ArrayPool<Byte>.Shared.Return(buffer)
        → Next.TryDispose()  // 递归释放链式节点
```

### 5.3 设计巧妙之处

1. **透明释放**：上层代码只需 `using var msg = ...`，无需知道 `Payload` 的具体实现类型。`TryDispose` 扩展方法安全处理了所有情况：
   - `ArrayPacket`（值类型，非 `IDisposable`）：跳过，无操作。
   - `OwnerPacket`（引用类型，`IDisposable`）：调用 `Dispose()`，归还 `ArrayPool` 缓冲区。
   - `null`：安全跳过。

2. **引用计数共享**：`DefaultMessage.Read` 用 `Slice(offset, count)` 把负载切为共享切片交给 `Payload`（每段递增计数）。所有句柄各自释放、最后一个归还；原始包不被修改，可继续使用并同样需要释放。

3. **链式递归释放**：`OwnerPacket.Dispose` 会自动释放 `Next` 链节点。即使协议解析产生了多段链式负载（如跨包拼接），一次 `Dispose` 即可全部归还。

4. **接口分层精妙**：
   - `IPacket` 不要求 `IDisposable`——值类型实现（`ArrayPacket`、`MemoryPacket`、`ReadOnlyPacket`）保持轻量。
   - `IOwnerPacket : IPacket, IDisposable`——仅池化实现需要释放。
   - `IMessage : IDisposable`——上层统一释放入口。

5. **与对象池复用配合**：`Message.Reset()` 可将消息状态清零并归还 `Payload` 引用（交付前须先摘除已转移的负载，如 `Payload=null`），适用于消息对象池场景。

### 5.4 使用示例

```csharp
// ===== 场景 1：标准 RPC 接收处理 =====
var raw = new OwnerPacket(4096);
var count = await socket.ReceiveAsync(raw.GetMemory());
raw.Resize(count);

using var msg = new DefaultMessage();
msg.Read(raw);
// Payload 持有共享切片（引用计数），raw 可继续使用、同样需要释放

var response = ProcessRequest(msg);
// using 块退出后自动归还池化内存


// ===== 场景 2：手动管理生命周期 =====
var msg = new DefaultMessage();
msg.Read(rawPacket);
try
{
    // 使用消息...
    var data = msg.Payload.ToStr();
}
finally
{
    msg.Dispose();  // 归还 Payload 的池化内存
}


// ===== 场景 3：返回 IMessage 给上层 =====
public IMessage Receive()
{
    var raw = new OwnerPacket(bufferSize);
    var count = socket.Receive(raw.GetSpan());
    raw.Resize(count);

    var msg = new DefaultMessage();
    msg.Read(raw);
    return msg;  // 所有权转移给调用方，调用方负责 Dispose
}
```

---

## 6. 线程安全性

- `Message` 及其子类**不是线程安全的**。
- 消息实例应在单一线程/任务中使用，不应跨线程共享。
- 池化复用时，需确保取出后独占使用，用完后 `Reset()` 再归还。

---

## 7. 最佳实践

| 场景 | 建议 |
|------|------|
| 接收消息 | 使用 `using var msg = ...` 确保自动释放 |
| 返回消息给上层 | 文档说明调用方需要 `Dispose` |
| 消息对象池 | 使用 `Reset()` 重置状态，`Dispose()` 释放 Payload |
| 多次切片 | 各切片共享引用计数，用毕各自 `Dispose`（最后一个归还池） |
| 长期持有负载 | 先 `Clone()` 复制数据，避免持有池化内存 |

---

## 8. 兼容性说明

- 本组件多目标框架（从 `net45` 到更高版本）。
- `IMessage : IDisposable` 在所有目标框架上可用。
- `TryDispose` 扩展方法无框架限制。

---

## 9. 变更记录

- 初始版本：基于 `IMessage.cs` 和 `DefaultMessage.cs` 现状编写。
- 重点说明 `IMessage.Dispose` 与 `IOwnerPacket` 的联动释放设计。
