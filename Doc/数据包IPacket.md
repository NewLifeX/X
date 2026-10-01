# IPacket 数据包帮助手册

本文档基于源码 `NewLife.Core/Data/IPacket.cs` 及实现类型文件，用于说明 `IPacket` 接口及其实现类型（`ArrayPacket` / `OwnerPacket` / `MemoryPacket` / `ReadOnlyPacket`）的设计、用法与注意事项。

> 关键词：零/少拷贝切片、链式包（`Next`）、引用计数所有权（Owner）、Span/Memory 短生命周期。

---

## 1. 设计目标与适用场景

`IPacket` 是 NewLife.Core 的通用数据包抽象，面向网络收发、协议解析、二进制拼装等高频场景。

核心目标：

- **减少分配**：尽量复用缓存（`ArrayPool<T>`）或复用现有数组/内存。
- **减少拷贝**：切片（`Slice`）优先共享底层缓冲区。
- **支持链式**：通过 `Next` 串接多段数据，避免为了“大包”而聚合复制。
- **明确释放责任**：通过 `IOwnerPacket` 与引用计数共享描述“谁负责归还池化内存”。

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
  - “共享底层”切片得到新包（引用计数共享）。默认 `count=-1` 表示直到末尾。

- `IPacket Slice(Int32 offset, Int32 count, Boolean transferOwner)`
  - **v12 破坏性变更：已删除**（原为忽略 `transferOwner` 的 `[Obsolete]` 兼容重载）。旧版编译的三参调用方会抛 `MissingMethodException`，需改用两参重载并重新编译。

- `OwnerPacket.Slice(Int32 offset, Int32 count = -1)`（公开方法，返回具体类型 `OwnerPacket`）
  - 返回拥有句柄，可以自然 `using` 释放；`IPacket.Slice` 与 `IOwnerPacket.Slice` 是其显式接口实现，经接口访问分别返回 `IPacket`/`IOwnerPacket`。
  - **v12 破坏性变更**：对 v12 之前编译、以具体类型接收者调用两参 `Slice` 的二进制会抛 `MissingMethodException`（CLR MemberRef 绑定含返回类型），需重新编译；影响面仅为具体类型调用方（如 NewLife.MySql，单元测试程序集不计），三参兼容重载与 `IPacket.Slice` 签名保持不变。

- `Boolean TryGetArray(out ArraySegment<Byte> segment)`
  - 尝试将“当前段”以 `ArraySegment<Byte>` 形式暴露。
  - **不包含 `Next`**。

---

## 3. 所有权（Owner）模型

### 3.1 谁来释放？

`IPacket` 本身不要求可释放；只有实现了 `IOwnerPacket` 的包才具备“归还池化内存”的责任。

- `IOwnerPacket : IPacket, IDisposable`
  - 用完后需要 `Dispose()`（或 `using`），以归还 `ArrayPool<T>` 缓冲区。
  - 切片返回拥有句柄：具体类型上返回 `OwnerPacket`，经该接口返回 `IOwnerPacket`（均为同一实现）；新句柄与原句柄各自释放，可以直接 `using` 释放。

文档层面建议遵循源码备注中的规则：

- **获得包的一方负责最终释放**（所有权在调用栈向上传递）。
- `Span<T>`/`Memory<T>` 是“借用视图”，只能在包有效期间短暂使用。

### 3.2 切片与引用计数（现行语义）

`Slice(offset, count)` 一律返回**引用计数共享句柄**：对窗口覆盖的每个底层段递增计数，多方各自读取、各自 `Dispose`，最后一个释放时归还内存池。

- 设计决策：切片只保留共享一种语义（历史“转移/借用”三参形态已废弃）——接收层轮末按 `RefCount` 裁决缓冲复用，前提是“一切逃逸必须计数可见”；取出子窗口后不再使用原句柄时应随即释放（等价旧的移动切片）。
- 每个句柄都必须 `Dispose`（共享≠免释放）；漏释放会让缓冲无法回池，开发期由析构兜底告警（`#if DEBUG || OWNERPACKET_FINALIZER`）。
- `OwnerPacket.Detach()`：脱手（放弃本句柄引用但不归还缓冲，仅计数为 1 时可用），供接收层在会话关闭时把常驻缓冲的归还责任交给释放路径。
- `Free()` 与三参 `Slice` 的 `[Obsolete]` 兼容壳**已在 v12 删除**（原分别转发 `Detach()` 与两参重载）；旧二进制会抛 `MissingMethodException`，需重新编译。

---

## 4. 实现类型详解

本节覆盖数据包全部实现类型。

### 4.1 `ArrayPacket`（`record struct`）

特性：

- 基于 `Byte[]` + `Offset` + `Length` 的轻量封装。
- 值类型，适合高频创建和传递。
- `TryGetArray` 恒为 `true`（仅针对当前段）。
- 支持链式：`Next` 可挂接任意 `IPacket`。
- 构造时校验 `offset`/`count` 是否落在缓冲区窗口内，越窗立即抛 `ArgumentOutOfRangeException`，不产生 `Length` 虚高的坏包。

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

- **必须为 class**：所有权语义依赖引用同一性。struct 赋值产生值拷贝会导致 double-free（引用计数被拷贝成多份、各句柄计数互不可见），且 IDisposable + struct 在装箱场景下无法正确释放资源。
- **sealed 密封**：无派生需求，JIT 可对 GetSpan/GetMemory 等热路径方法去虚拟化并内联，显著提升协议解析性能。
- **不继承 MemoryManager&lt;T&gt;**：仅需 IPacket + IDisposable，MemoryManager 的 Pin/Unpin/IMemoryOwner.Memory 均未使用，移除后消除死代码和多余 vtable 开销。

关键特性：

- 使用 `ArrayPool<Byte>.Shared.Rent()` 申请缓冲区。
- 实现 `IOwnerPacket`，必须 `Dispose()` 归还缓冲区。
- 支持 `Next` 链式结构。
- 切片为引用计数共享；支持轮句柄重设窗口复用（接收环）、`Detach` 脱手与 `Skip` 窗口前移。

构造方式：

- `OwnerPacket(Int32 length, Int32 reserve = 0)`：从共享池租用缓冲区，`reserve` 为前置预留头部字节数（见 5.8 头部扩展）。
- `OwnerPacket(Byte[] buffer, Int32 offset, Int32 length, Boolean hasOwner)`：包装已有数组，可指定是否拥有释放权。
- `OwnerPacket(OwnerPacket owner, Int32 expandSize)`：原地向前借位扩展头部并**接管**源句柄（源作废）；专供序列化器 `SpanSerializer.ToFrame` 这类句柄独占场景，共享借位请用 `OwnerPacket.ExpandHeader`。

释放与链释放：

- `Dispose()` 会将自身 `_buffer` Return 给池，并尝试释放 `Next`（`Next.TryDispose()`）。
- `Detach()` 会清空引用并放弃所有权（**不会**归还池化内存），同时抑制析构兜底；供接收层在会话关闭（以及接收槽缺失兜底）时脱手，仍有其它持有者时抛出异常。

切片的语义（重点）：

- `Slice(offset, count)` 返回**引用计数共享切片**：原包与新包同时可用，各自 `Dispose`，最后一个释放时归还池。
- 跨链切片：当 `Next != null` 时，切片可能返回带 `Next` 的新链（或递归切到后续段）。

建议：

- 一次接收包切出多个字段时，各方各自 `Dispose` 即可（引用计数保证最后一个释放者归还池）。
- 跨轮/跨 await 带出切片后必须 `Dispose`，否则缓冲引用不归零；开发期漏释放会由析构兜底并输出 XTrace 告警。

示例：

```csharp
using var pk = new OwnerPacket(1024);

// 切片共享底层缓冲（引用计数），双方同时可用
var header = pk.Slice(0, 4);
var payload = pk.Slice(4, 512);

// 各自释放；最后一个释放者（含 pk 自身）把缓冲归还池
header.TryDispose();
payload.TryDispose();
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

- `ReadOnlyPacket(Byte[] buffer, Int32 offset = 0, Int32 count = -1)`：`offset`/`count` 越窗立即抛 `ArgumentOutOfRangeException`。
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

### 5.8 头部准备

- `FreeHeader`（`IPacket` 属性）：本视图之前的字节数（同一缓冲区内）。分配时预留（`new OwnerPacket(size, reserve)`）或由预留区切片/借位派生时，即为可零拷贝借位写入协议头的空间。
- `ExpandHeader(this IPacket pk, Int32 size)`（借位原语）：要求已预留足够头部空间，向前借位共享（零拷贝）；未预留抛 `InvalidOperationException`。结果与原句柄共享缓冲（引用计数各自释放），**原句柄保持有效**；带链拥有句柄会先切片为独占共享链再前移链头。`ArrayPacket.ExpandHeader` 为结构体视图，本就无所有权。
- `PrepareHeader(this IPacket? body, Int32 size)`（构建推荐）：为负载准备帧头空间，返回 `IOwnerPacket`。两种策略**都零拷贝、都不改动原负载句柄**：
  - 拥有句柄且已预留（`OwnerPacket` 且 `FreeHeader >= size`）→ 向前借位共享，帧头落在原缓冲预留区；
  - 其余（未预留、带链、视图）→ 新头节点挂接负载链——拥有句柄先 `Slice(0, -1)` 得到独占的共享链（引用计数各自持有），视图无所有权可直接挂接。
- 消息构建（`IMessageCodec.Build` / `BuildHeader`）统一走上述原语，因此**构建不消费消息负载**，`message.Payload` 构建后依然可读。

典型用法（上游预留、下游零拷贝写头）：

```csharp
// 上游：组装内容时预留头部（32 字节可容纳各类二进制协议头，见 SpanSerializer.HeaderReserve）
var body = new OwnerPacket(payload.Length, 32);
payload.CopyTo(body.GetSpan());

// 下游：借位写入协议头（零拷贝，帧头连续），body 仍然有效
var frame = body.PrepareHeader(4);
frame.GetSpan()[..4].CopyTo(headBytes);
```

> **时效**：结果可能引用负载缓冲（借位共享或链式引用），帧发送完成前不得复用或改写该缓冲。
>
> **掩码例外**：客户端 WebSocket 掩码是原地 XOR（破坏性），必须独占，故 `WebSocketCodec` 客户端方向改为新分配 + 拷贝时掩码，消息负载同样不受影响。

### 5.9 序列桥接（ReadOnlySequence）

- `AsReadOnlySequence(this IPacket pk)`
  - 数据包链 → `ReadOnlySequence<Byte>` 零拷贝视图；单段快速路径零分配，多段构建段链适配。
  - 序列仅在数据包句柄有效期内可用；跨轮/跨异步持有请先 `Slice` 共享切片。

- `AsReadOnlySequence(this IPacket pk, Int32 offset, Int32 count = -1)`
  - 指定窗口的序列视图，跨段窗口自动裁剪。

典型用法（与协议解析/数据管道对接）：

```csharp
var seq = pk.AsReadOnlySequence();      // 只读窗口，顺序解析
var head = pk.AsReadOnlySequence(0, 8); // 头部窗口（跨段自动裁剪）
```

> 协议解析优先使用 `SequenceReader<T>` 在只读序列上顺序读取（全 18 TFM 可用，旧框架经 `Stub/SequenceReader.cs` 垫片补齐）；
> `IPacket` 链的帧首头部直读仍可用 `PacketHelper.GetPrefix`。
> 数据管道的读取窗口同样直接使用 `ReadOnlySequence<Byte>`，详见《数据管道Pipe.md》与《序列读取器SequenceReader.md》。

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

### 7.2 `OwnerPacket.Slice` 为引用计数共享

`OwnerPacket.Slice(offset, count)` 返回共享切片：原实例不受影响，双方（或多方）各自 `Dispose`，最后一个释放时归还内存池。

若你只是做协议解析切片（多次切片），按需使用、用毕 `Dispose` 即可；漏释放的句柄会阻止缓冲回池（开发期由析构兜底告警）。

拥有句柄经 `IOwnerPacket` 接口访问切片时返回 `IOwnerPacket`，可以直接 `using` 释放：

```csharp
IOwnerPacket pk = client.Receive();     // 拥有句柄
using var view = pk.Slice(0, 4);        // 视图持有独立引用，可 using 释放；pk 照常使用与释放
```

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
- 文档中的 API 以 `IPacket.cs` 及实现类型文件的当前实现为准；对特定目标框架的差异由条件编译控制（如 `MemoryStream.TryGetBuffer` 在 `NET45` 下不可用）。

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

2. **所有权链条清晰**：codec 解析（如 `SrmpCodec.TryParse`）用 `Slice(offset, count)` 把负载切为共享切片经 `SetBody` 归 `Payload` 持有（引用计数）。最终谁持有 `IMessage`，谁就负责释放其持有的引用。

3. **链式递归**：`OwnerPacket.Dispose` 会递归释放 `Next` 链，即使协议解析产生了多段链式负载，一次 `Dispose` 即可全部归还。

4. **与现有基础设施无缝集成**：`TryDispose` 是 `NewLife` 体系的通用扩展方法，不需要 `IPacket` 接口本身继承 `IDisposable`，保持了值类型实现（`ArrayPacket`、`MemoryPacket`）的轻量性。

使用示例：

```csharp
// RPC 接收侧
var raw = new OwnerPacket(bufferSize);  // 从池中租用
var count = await socket.ReceiveAsync(raw.GetMemory());
raw.Resize(count);

// 解析消息（codec 定界；消息经 SetBody 持有负载切片，帧句柄仍归调用方）
var rs = new SrmpCodec().TryParse(raw.AsReadOnlySequence());
var msg = rs?.Message;

// 上层处理完毕后释放，自动归还池化内存
msg?.Dispose();
raw.TryDispose();
```

> 更多 IMessage 设计细节，请参阅 [消息IMessage.md](消息IMessage.md)。

---

## 11. 变更记录

- 本文档根据 `IPacket.cs` 现状重写，用于替换旧版 `Doc/IPacket.md`。
- 覆盖新增实现：`ReadOnlyPacket`。
- 强调 `OwnerPacket` 引用计数共享切片等关键语义。
- 增加与 `IMessage` 联动释放设计的说明（第 10 节）。
- 增加序列桥接说明（第 5.9 节）：`AsReadOnlySequence` 零拷贝视图与窗口重载。
