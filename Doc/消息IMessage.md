# IMessage 消息帮助手册

本文档基于源码 `NewLife.Core/Messaging/IMessage.cs`，说明 `IMessage` 接口及其基类 `Message`、`DefaultMessage` 的设计、用法与注意事项。

> 关键词：请求-响应模式、Dispose 释放链、IOwnerPacket 池化内存、RPC 消息生命周期。

---

## 1. 设计目标

- **统一消息抽象**：为 RPC、网络通信提供请求-响应消息的标准接口。
- **资源安全释放**：`IMessage : IDisposable`，释放消息时自动归还内部 `Payload` 的池化内存。
- **灵活可扩展**：通过 `Message` 基类与 `DefaultMessage` 实现类，支持自定义协议格式。

---

## 2. 接口定义

```csharp
public interface IMessage : IDisposable
{
    Boolean Reply { get; set; }      // 是否响应消息
    Boolean Error { get; set; }      // 是否有错
    Boolean OneWay { get; set; }     // 单向请求
    IPacket? Payload { get; set; }   // 负载数据

    IMessage CreateReply();          // 根据请求创建配对响应
    Boolean Read(IPacket pk);        // 从数据包解析消息
    IPacket? ToPacket();             // 序列化为数据包
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
- **`Read(IPacket pk)`**：从原始数据包解析消息头和负载。
- **`ToPacket()`**：将消息序列化为数据包，用于网络发送。

---

## 3. 基类 Message

`Message` 提供 `IMessage` 的基础实现：

```csharp
public class Message : IMessage
{
    public Boolean Reply { get; set; }
    public Boolean Error { get; set; }
    public Boolean OneWay { get; set; }
    public IPacket? Payload { get; set; }

    public void Dispose() { ... }
    protected virtual void Dispose(Boolean disposing) { ... }

    public virtual IMessage CreateReply() { ... }
    public virtual Boolean Read(IPacket pk) { ... }
    public virtual IPacket? ToPacket() => Payload;
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
    → Slice(4, len, transferOwner:true) // 切片转移所有权给 Payload
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

2. **所有权转移**：`OwnerPacket.Slice(offset, count, transferOwner: true)` 在 `DefaultMessage.Read` 中将缓冲区释放责任从原始包转移给切片出的 `Payload`。原始包失去 `_hasOwner`，不会重复归还。

3. **链式递归释放**：`OwnerPacket.Dispose` 会自动释放 `Next` 链节点。即使协议解析产生了多段链式负载（如跨包拼接），一次 `Dispose` 即可全部归还。

4. **接口分层精妙**：
   - `IPacket` 不要求 `IDisposable`——值类型实现（`ArrayPacket`、`MemoryPacket`、`ReadOnlyPacket`）保持轻量。
   - `IOwnerPacket : IPacket, IDisposable`——仅池化实现需要释放。
   - `IMessage : IDisposable`——上层统一释放入口。

5. **与对象池复用配合**：`Message.Reset()` 可将消息状态清零以便复用（但不释放 `Payload`），适用于消息对象池场景。释放与复用职责分离。

### 5.4 使用示例

```csharp
// ===== 场景 1：标准 RPC 接收处理 =====
var raw = new OwnerPacket(4096);
var count = await socket.ReceiveAsync(raw.GetMemory());
raw.Resize(count);

using var msg = new DefaultMessage();
msg.Read(raw);
// raw 的所有权已转移给 msg.Payload

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
| 多次切片 | 仅最终返回的切片使用 `transferOwner: true` |
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
