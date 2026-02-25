# IPacket 数据包帮助手册

本文档基于源码 `NewLife.Core/Data/IPacket.cs`，用于说明 `IPacket` 接口及其实现类型（`ArrayPacket` / `OwnerPacket` / `MemoryPacket` / `ReadOnlyPacket`）的设计、用法与注意事项。

> 关键词：零/少拷贝切片、链式包（`Next`）、所有权（Owner）转移、Span/Memory 短生命周期。

---

## 1. 设计目标与适用场景

`IPacket` 是 NewLife.Core 的通用数据包抽象，面向网络收发、协议解析、二进制拼装等高频场景。

核心目标：

- **减少分配**：尽量复用缓存（`ArrayPool<T>`）或复用现有数组/内存。
- **减少拷贝**：切片（`Slice`）优先共享底层缓冲区。
- **支持链式**：通过 `Next` 串接多段数据，避免为了“大包”而聚合复制。
- **明确释放责任**：通过 `IOwnerPacket` 与 `transferOwner` 描述“谁负责归还池化内存”。

典型使用：

- Socket 接收缓冲区 → 包装为 `OwnerPacket` / `ArrayPacket` → 协议头/体切片。
- 组包：多个字段/段拼接 → `Append` 形成链式包 → 发送或落盘。
- 调试展示：`ToHex()` 打印预览，`ToStr()` 按编码读取。

---

## 2. `IPacket` 接口说明

源码签名（摘要）：

- `Int32 Length { get; }`
  - 当前包段长度，仅当前段（不含 `Next`）。

- `IPacket? Next { get; set; }`
  - 链式后续包。**仅表示逻辑拼接，不意味着底层内存连续**。

- `Int32 Total { get; }`
  - 当前段 + `Next` 链的总长度。

- `Byte this[Int32 index] { get; set; }`
  - **全局索引**访问（从 0 开始，跨越链式包）。
  - 写入是否支持，取决于实现（例如 `ReadOnlyPacket` 禁止写入）。

- `Span<Byte> GetSpan()`
  - 获取当前段的 `Span` 视图。
  - **只能在当前所有权生命周期内短暂使用，禁止缓存到异步/长期结构中。**

- `Memory<Byte> GetMemory()`
  - 获取当前段的 `Memory`。
  - 同样遵循短生命周期原则。

- `IPacket Slice(Int32 offset, Int32 count = -1)`
  - “共享底层”切片得到新包。默认 `count=-1` 表示直到末尾。

- `IPacket Slice(Int32 offset, Int32 count, Boolean transferOwner)`
  - 切片并可选择**转移内存管理权**。
  - 不同实现对 `transferOwner` 支持程度不同（见后文）。

- `Boolean TryGetArray(out ArraySegment<Byte> segment)`
  - 尝试将“当前段”以 `ArraySegment<Byte>` 形式暴露。
  - **不包含 `Next`**。

---

## 3. 所有权（Owner）模型

### 3.1 谁来释放？

`IPacket` 本身不要求可释放；只有实现了 `IOwnerPacket` 的包才具备“归还池化内存”的责任。

- `IOwnerPacket : IPacket, IDisposable`
  - 用完后需要 `Dispose()`（或 `using`），以归还 `ArrayPool<T>` 缓冲区。

文档层面建议遵循源码备注中的规则：

- **获得包的一方负责最终释放**（所有权在调用栈向上传递）。
- `Span<T>`/`Memory<T>` 是“借用视图”，只能在包有效期间短暂使用。

### 3.2 `transferOwner` 的真实含义

`Slice(offset, count, transferOwner)` 允许切片时把“归还缓冲区”的责任转移给新包：

- `transferOwner = true`：新包负责释放底层资源（若实现支持）。
- `transferOwner = false`：新包仅共享视图，不负责释放。

**重要约束**：所有权转移通常只能发生一次；对同一来源反复切片时，不要多次转移。

> `OwnerPacket` 在源码实现中会通过 `_hasOwner` 控制“谁有权 Return 到池”，并在切片转移时让原实例失权。

---

## 4. 实现类型详解

本节覆盖 `IPacket.cs` 中出现的全部实现。

### 4.1 `ArrayPacket`（`record struct`）

特性：

- 基于 `Byte[]` + `Offset` + `Length` 的轻量封装。
- 值类型，适合高频创建和传递。
- `TryGetArray` 恒为 `true`（仅针对当前段）。
- 支持链式：`Next` 可挂接任意 `IPacket`。

切片行为：

- `Slice` 返回新的 `ArrayPacket`（共享原数组，不分配）。
- 当 `Next` 不为空时，切片可跨段，但源码对“当前段用完后取下一段”存在 **强转 `ArrayPacket`** 的分支：
  - `remain <= 0` 时使用 `(ArrayPacket)next.Slice(...)`。
  - 这意味着：如果 `Next` 不是 `ArrayPacket`，该分支可能抛出异常。

建议：

- `ArrayPacket` 链式拼接时，尽量让 `Next` 也是 `ArrayPacket`（或避免触发跨段强转分支）。
- 更通用的跨段切片需求，优先使用 `OwnerPacket` 链或在上层聚合为连续缓冲区。

性能注意事项：

- `GetSpan`/`GetMemory`/`TryGetArray` 已标记 `AggressiveInlining`，JIT 可在热路径内联这些方法。
- `IPacket.Slice(offset, count)` 的显式接口实现已优化，避免 struct 到 IPacket 的装箱分配。
- 公开的 `Slice` 方法返回 `ArrayPacket`（值类型），不产生堆分配；但链式包 `Next` 为非 `ArrayPacket` 类型时会抛出 `InvalidCastException`。

创建示例：

```csharp
var pk = new ArrayPacket(buffer, offset: 0, count: buffer.Length);
var header = ((IPacket)pk).Slice(0, 4);
var payload = ((IPacket)pk).Slice(4);
```

> 说明：`ArrayPacket` 显式接口实现了 `IPacket.Slice`，当以 `ArrayPacket` 变量调用时会优先走其自身的 `Slice` 重载；为了避免调用路径混淆，示例中用显式转换。

---

### 4.2 `OwnerPacket`（`sealed class`）

`OwnerPacket` 是“带所有权”的高性能实现，适合接收/发送缓冲区以及需要从池里申请新内存的场景。

设计决策：

- **必须为 class**：所有权语义依赖引用同一性。struct 赋值产生值拷贝会导致 double-free（Slice 转移所有权时修改的是副本而非原始实例），且 IDisposable + struct 在装箱场景下无法正确释放资源。
- **sealed 密封**：无派生需求，JIT 可对 GetSpan/GetMemory 等热路径方法去虚拟化并内联，显著提升协议解析性能。
- **不继承 MemoryManager&lt;T&gt;**：仅需 IPacket + IDisposable，MemoryManager 的 Pin/Unpin/IMemoryOwner.Memory 均未使用，移除后消除死代码和多余 vtable 开销。

关键特性：

- 使用 `ArrayPool<Byte>.Shared.Rent()` 申请缓冲区。
- 实现 `IOwnerPacket`，必须 `Dispose()` 归还缓冲区。
- 支持 `Next` 链式结构。
- 支持切片时转移所有权（`transferOwner`）。

构造方式：

- `OwnerPacket(Int32 length)`：从共享池租用缓冲区。
- `OwnerPacket(Byte[] buffer, Int32 offset, Int32 length, Boolean hasOwner)`：包装已有数组，可指定是否拥有释放权。
- `OwnerPacket(OwnerPacket owner, Int32 expandSize)`：用于头部扩展，转移所有权（见 `PacketHelper.ExpandHeader`）。

释放与链释放：

- `Dispose()` 会将自身 `_buffer` Return 给池，并尝试释放 `Next`（`Next.TryDispose()`）。
- `Free()` 会清空引用并放弃所有权（**不会**归还池化内存，存在泄漏风险，仅用于特殊场景）。

切片的语义（重点）：

- `Slice(offset, count)` **默认 `transferOwner: true`**。
  - 这意味着：对 `OwnerPacket` 切片后，**新包成为所有者**，原包将失去 `_hasOwner`。
- 跨链切片：当 `Next != null` 时，切片可能返回带 `Next` 的新链（或递归切到后续段）。

建议：

- 若你只想得到“视图切片”，而不希望改变释放责任，请显式调用 `Slice(offset, count, transferOwner: false)`。
- 若一次接收包需要切出多个字段（多次切片），一般不要对每个切片都转移所有权：
  - 做法 1：只让最终要返回/持有的那一个切片转移；其它切片 `transferOwner:false`。
  - 做法 2：不转移所有权，仍由原 `OwnerPacket` 统一释放。

示例：

```csharp
using var pk = new OwnerPacket(1024);

// 仅借视图，不转移释放责任
var header = pk.Slice(0, 4, transferOwner: false);

// 真正要对外返回的片段转移所有权（仅一次）
var payload = pk.Slice(4, 512, transferOwner: true);
// 此后 pk 不再拥有缓冲区，payload.Dispose() 才会归还
```

---

### 4.3 `MemoryPacket`（`struct`）

特性：

- 基于 `Memory<Byte>` 的轻量封装（无内置所有权/释放语义）。
- `TryGetArray` 通过 `MemoryMarshal.TryGetArray()` 尝试暴露数组段。
- 允许 `Next` 链，但 **一旦存在 `Next`，`Slice` 直接抛出 `NotSupportedException`**（源码：`Slice with Next`）。

适用场景：

- 与外部组件以 `Memory<Byte>` 交互时的桥接类型。
- 单段内存的视图截取。

注意：

- 由于无所有权管理，`MemoryPacket` 的底层内存可能来自池或其他临时来源，**不要长期持有**。

---

### 4.4 `ReadOnlyPacket`（`readonly record struct`）

特性：

- 基于 `Byte[]` 的只读包。
- 不支持链式：`IPacket.Next` 显式实现始终为 `null`。
- 索引器 `set` 抛出 `NotSupportedException`。
- `Slice` 返回新的 `ReadOnlyPacket`，共享底层数组。

适用场景：

- 多线程共享的模板数据、配置缓存、只读协议常量块。
- 需要明确禁止修改数据内容时。

构造：

- `ReadOnlyPacket(Byte[] buffer, Int32 offset = 0, Int32 count = -1)`
- `ReadOnlyPacket(IPacket packet)`：会复制 `packet.ToArray()`，生成独立只读副本。

---

## 5. `PacketHelper` 扩展方法速查

> `PacketHelper` 是核心操作集合：链式、转换、流、片段、读取、头部扩展。

### 5.1 链式拼接

- `Append(this IPacket pk, IPacket next)`：追加包到链尾。
  - 内置简单环检测：避免 `pk` 自引用。
  - 时间复杂度 O(n)。链条很长时要考虑性能。

- `Append(this IPacket pk, Byte[] data)`：追加数组（包装为 `ArrayPacket`）。

示例：

```csharp
IPacket message = head
    .Append(body)
    .Append(tailBytes);
```

### 5.2 字符串转换

- `ToStr(Encoding? encoding = null, Int32 offset = 0, Int32 count = -1)`
  - 单包走快速路径：`Span` 切片 + 编码。
  - 多包链走拼接路径：`Pool.StringBuilder` 分段追加。

注意：

- `offset` 为全局偏移（跨链）。`count=-1` 表示到末尾。
- `pk == null` 返回 `null`（为了兼容扩展调用）。

### 5.3 十六进制转换

- `ToHex(Int32 maxLength = 32, String? separator = null, Int32 groupSize = 0)`
  - 支持跨链连续分组，`maxLength=-1` 表示全部。

### 5.4 流操作

- `CopyTo(Stream stream)` / `CopyToAsync(Stream stream, CancellationToken cancellationToken = default)`
  - 优先使用 `TryGetArray` 写入，失败再走 `GetMemory()`。

- `GetStream(Boolean writable = true)`
  - 单包且 `TryGetArray` 成功：直接返回 `MemoryStream` 包装底层数组段（零拷贝）。
  - 否则：聚合复制到新 `MemoryStream`。

### 5.5 片段与数组

- `ToSegment()`
  - 单包：尽量直接返回底层 `ArraySegment<Byte>`。
  - 多包：复制聚合为新数组段。

- `ToSegments()`
  - 返回每个链节点的 `ArraySegment<Byte>` 列表，保持分段结构。
  - 对无法 `TryGetArray` 的实现，会调用 `GetSpan().ToArray()`（产生复制）。

- `ToArray()`
  - 总是返回新数组副本（单包：`Span.ToArray()`；多包：通过池化 `MemoryStream` 聚合）。

### 5.6 读取与克隆

- `ReadBytes(Int32 offset = 0, Int32 count = -1)`
  - 单包在满足条件时可能直接返回底层数组（性能优化）。

- `Clone()`
  - 深度克隆：总会复制数据内容，返回 `ArrayPacket`。

### 5.7 内存视图

- `TryGetSpan(out Span<Byte> span)`
  - 仅当无 `Next` 时返回 `true`。

### 5.8 头部扩展

- `TryExpandHeader(...)`（已过时）：仅当原包有足够“前置空间”时返回新包。
- `ExpandHeader(this IPacket? pk, Int32 size)`（推荐）：
  - `ArrayPacket/OwnerPacket` 有前置空间时复用并向前扩展。
  - 否则创建新的 `OwnerPacket(size)` 作为头节点，原包挂到 `Next`。

典型用法（协议头预留）：

```csharp
var body = new ArrayPacket(payload);
var msg = body.ExpandHeader(4);

// 此时 msg 的前 4 字节可填充头部，后续链为 body
```

---

## 6. 链式包的行为约定

### 6.1 `Length` vs `Total`

- `Length`：当前段长度。
- `Total`：当前段 + `Next.Total`。

在判断“包是否为空”时，优先看 `Total`。

### 6.2 跨链索引器

- `this[index]` 的 `index` 是全局位置。
- 性能上，跨链访问需要遍历到对应段；若频繁随机访问，建议先 `ToArray()` 聚合为连续缓冲区再处理。

---

## 7. 使用建议与常见坑

### 7.1 Span/Memory 生命周期

`GetSpan()` / `GetMemory()` 返回的是视图：

- 只能在当前包的有效生命周期内使用。
- **禁止**把 `Span`/`Memory` 缓存到字段、闭包、异步回调、队列等生命周期更长的结构中。

### 7.2 `OwnerPacket.Slice` 默认转移所有权

`OwnerPacket.Slice(offset, count)` 默认 `transferOwner: true`，会让原实例失去释放权。

若你只是做协议解析切片（多次切片），通常更安全的模式是：

- 解析切片：`transferOwner:false`
- 最终返回/保存的那一段：视需求决定是否转移

### 7.3 `MemoryPacket` 的 `Next` 限制

`MemoryPacket` 一旦挂了 `Next`，对其调用 `Slice` 会抛 `NotSupportedException`。

### 7.4 `ArrayPacket` 跨段切片的类型假设

当 `ArrayPacket.Next` 不为空且切片跨越当前段时，部分逻辑会强制将结果转为 `ArrayPacket`。

- 若你构建了混合链（例如 `ArrayPacket.Next = OwnerPacket`），跨段切片（尤其是 offset 超过当前段）可能引发类型转换异常。

建议：

- 构建链时尽量保持同类链接，或改用 `OwnerPacket` 链。

---

## 8. 快速示例

### 8.1 协议解析：头 + 体

```csharp
IPacket pk = new ArrayPacket(buffer);

var header = pk.Slice(0, 4);
var body = pk.Slice(4);

var cmd = header[0];
```

### 8.2 组包：多段拼接

```csharp
IPacket msg = new ArrayPacket(head)
    .Append(body)
    .Append(tail);

var bytes = msg.ToArray();
```

### 8.3 输出调试预览

```csharp
var hex = pk.ToHex(maxLength: 64, separator: " ", groupSize: 2);
var text = pk.ToStr(Encoding.UTF8, offset: 0, count: 128);
```

### 8.4 发送到流

```csharp
pk.CopyTo(stream);
await pk.CopyToAsync(stream, cancellationToken);
```

---

## 9. 兼容性说明

- 本组件多目标框架（从 `net45` 到更高版本）。
- 文档中的 API 以 `IPacket.cs` 当前实现为准；对特定目标框架的差异由条件编译控制（如 `MemoryStream.TryGetBuffer` 在 `NET45` 下不可用）。

---

## 10. 与 IMessage 的联动释放设计

在 RPC / 网络通信架构中，底层接收到原始数据后，通常使用 `OwnerPacket` 从 `ArrayPool` 租用缓冲区以减少 GC 压力。解析协议后，负载数据（`Payload`）通过切片共享底层缓冲区，并随 `IMessage` 向上层传递。

**设计要点**：`IMessage` 继承 `IDisposable`，在 `Dispose` 时通过 `TryDispose` 扩展方法自动释放内部的 `Payload`。如果 `Payload` 是 `IOwnerPacket`（或任何实现了 `IDisposable` 的 `IPacket`），其池化缓冲区将被归还。

典型调用链：

```
IMessage.Dispose()
  → Message.Dispose(disposing: true)
    → Payload.TryDispose()
      → OwnerPacket.Dispose()
        → ArrayPool<Byte>.Shared.Return(buffer)
        → Next.TryDispose()  // 递归释放链式节点
```

**巧妙之处**：

1. **零感知释放**：上层代码只需 `using var msg = ...` 或手动 `msg.Dispose()`，无需关心 `Payload` 的具体类型是否需要释放。`TryDispose` 安全地处理了 `ArrayPacket`（非 `IDisposable`）和 `OwnerPacket`（`IDisposable`）的差异。

2. **所有权链条清晰**：`OwnerPacket.Slice(offset, count, transferOwner: true)` 在 `DefaultMessage.Read` 中将切片所有权从原始缓冲区转移给 `Payload`，原始包失去释放权。最终谁持有 `IMessage`，谁就负责释放整条链路。

3. **链式递归**：`OwnerPacket.Dispose` 会递归释放 `Next` 链，即使协议解析产生了多段链式负载，一次 `Dispose` 即可全部归还。

4. **与现有基础设施无缝集成**：`TryDispose` 是 `NewLife` 体系的通用扩展方法，不需要 `IPacket` 接口本身继承 `IDisposable`，保持了值类型实现（`ArrayPacket`、`MemoryPacket`）的轻量性。

使用示例：

```csharp
// RPC 接收侧
var raw = new OwnerPacket(bufferSize);  // 从池中租用
var count = await socket.ReceiveAsync(raw.GetMemory());
raw.Resize(count);

// 解析消息（Read 内部 Slice 转移所有权）
var msg = new DefaultMessage();
msg.Read(raw);

// 上层处理完毕后释放，自动归还池化内存
msg.Dispose();
```

> 更多 IMessage 设计细节，请参阅 [消息IMessage.md](消息IMessage.md)。

---

## 11. 变更记录

- 本文档根据 `IPacket.cs` 现状重写，用于替换旧版 `Doc/IPacket.md`。
- 覆盖新增实现：`ReadOnlyPacket`。
- 强调 `OwnerPacket.Slice` 默认转移所有权等关键语义。
- 增加与 `IMessage` 联动释放设计的说明（第 10 节）。
