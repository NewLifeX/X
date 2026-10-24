# 数据管道Pipe

> ⚠️ **旧栈引用（v12）**：文中与《数据包编码器PacketCodec》的对比属历史定位——`PacketCodec`/`PacketFramer` 已整删，帧定界改由 `MessagePump` + `IMessageCodec` 承担（见《消息协议栈》）。`Pipe` 本身及本文的缓冲/背压/所有权内容仍然有效。

## 概述

`Pipe` 处理字节流的**缓冲与消费**：接收方持续投递数据、消费方按序列消费、缓冲有界自动背压。与《数据包编码器PacketCodec》的区别——PacketCodec 是"粘包拆帧器"（每连接实例、提供帧切分），Pipe 是"字节流缓冲"（连接读写两侧的解耦层，数据进出都经管道，帧的定界交给上层帧泵）。

**模型：单写单读、未消费链持有、`ReadOnlySequence<Byte>` 零拷贝窗口、有界背压。** **命名与形态对齐**主流 `System.IO.Pipelines` 的 `Pipe`/`PipeReader`/`PipeWriter`（同名不同命名空间），且**全 TFM 可用（含 net45）**。

**命名空间**：`NewLife.Data`（帧泵与流式消息在 `NewLife.Messaging`，见下文组件表）

> 全局缓冲所有权与消息层归属处理见《网络缓冲所有权架构》。
>
> 零基础读者建议先读下一节《三分钟看懂》；只想查阅 API 的读者可直接跳到《组件》。

## 三分钟看懂（零基础导览）

> 本节只讲“是什么、怎么配合、为什么这么设计”，机制细节与取舍依据散见后文各节；只想查 API 的读者可跳过。

**一个比喻**：`Pipe` 是一根带“水位线”的数据水管，`PipeWriter`/`PipeReader` 是这根水管的两端管口——同一个对象、两个句柄，写方拿 `.Writer`、读方拿 `.Reader`，可以分属不同线程（单写单读）。

数据的一生只有五个动作：

```
写入 → FlushAsync 提交 → 未消费窗口（读方可见） → 解析并 AdvanceTo 推进 → 引用计数归零，内存归还池
```

| 类 | 一句话职责 | 你会用到的动作 |
|----|-----------|---------------|
| `Pipe` | 管体：自己不读写数据，只承载公共状态——双端句柄、共享锁、完成标志与异常、积压量与水位、`Resumed` 事件 | `new Pipe()`、调 `PauseThreshold`/`ResumeThreshold`、`Dispose`；日常几乎不直接使用 |
| `PipeWriter` | 唯一写者：数据先写进管道内部缓冲，**提交（FlushAsync）之前读方不可见** | `Append(IPacket)`（整包、所有权转移、零拷贝）／`GetSpan`+`Advance`+`FlushAsync`（跨度直写）／`WriteAsync`（一步写入） |
| `PipeReader` | 唯一读者：每次拿到“当前窗口”——`ReadOnlySequence<Byte>` 零拷贝视图，跨段跨包天然连续 | `ReadAsync`/`TryRead` 取窗口 → 解析 → `AdvanceTo(consumed, examined)` 报进度 → `TakeFrame` 切整帧、`Limit` 限长读体 |

**为什么是三个类而不是一个**：

- **读写节奏解耦**：收数据的一侧“来了就投”，消费的一侧按自己的节奏取，两侧可以是不同线程、不同速度；
- **零拷贝**：窗口直接指向管道持有的段链（数据包），解析不产生副本；`Append(IPacket)` 连句柄所有权一并转移；
- **缓冲有界**：读方不消费，积压涨到暂停水位就“亮红灯”（`IsPaused`），把压力传回接收方——TCP 窗口自然回压，没人读也不会无限涨内存（细节见《背压》）；
- **抗粘包/半包**：管道只管字节流，定界交给消费方（帧泵/协议编解码器）——头到齐即交付、体流式读取，各取所需。

**与 System.IO.Pipelines 的关系**：命名与形态对齐 BCL（同名不同命名空间，同时引用两者时用别名隔离），行为按 BCL 实测逐项对齐；差异只剩三条关键决策与两条有意保留的偏差，见《与 System.IO.Pipelines 的差异（三个关键决策）》。

**常见误区速记**：

| 误区 | 正解 | 细节见 |
|------|------|--------|
| 把读取窗口 `Buffer` 存下来跨轮用 | 窗口只在对应数据被消费前有效；需要跨轮持有就用 `TakeFrame` 切出拥有句柄 | 《所有权规则》 |
| `AdvanceTo` 传的是对象或位置 | 主形态是**字节计数**：消费了多少、检查到哪（examined）；位置重载仅限最近一次读取窗口 | 《读取器 API 摘要》 |
| “只检查不消费”就不用推进窗口 | 也要 `AdvanceTo(0, examined)` 报出已检查量，否则积压不解除、写侧可能永久挂起 | 《背压》 |
| `Complete()` 会丢掉没提交的数据 | 已 `Advance` 未 `Flush` 的数据会**先提交再结束**；只有未推进的写缓冲才丢弃 | 《写入缓冲（Pipelines 形态）》 |
| `Append` 之后还留着句柄 | 入参所有权无条件转移给管道，调用后不得再使用/释放；管道已关闭时由管道负责释放 | 《所有权规则》 |

## 组件

| 类型 | 命名空间 | 角色 |
|------|----------|------|
| `Pipe` | `NewLife.Data` | 管道本体：双端句柄装配、共享状态、水位背压 |
| `PipeReader` | `NewLife.Data` | 读侧句柄：持有未消费窗口（段链）、读取、推进、切帧、限长（`pipe.Reader`） |
| `PipeWriter` | `NewLife.Data` | 写侧句柄：持有写缓冲、追加、完成、跨度写入（`pipe.Writer`） |
| `ReadResult` | `NewLife.Data` | 读取结果：窗口 + IsCompleted/IsCanceled |
| `FlushResult` | `NewLife.Data` | 提交结果：IsCompleted/IsCanceled |
| `MessagePump` | `NewLife.Messaging` | 消息帧泵：按 `IMessageCodec` 定界交付消息（头到齐即交付，体流式/内存统一） |
| `LimitedReader` | `NewLife.Data` | body 限长读取（`PipeReader.Limit` 创建）：预算裁剪、TryRead/ReadAsync 消费、ReadAllAsync 物化、DrainAsync 对齐帧尾 |
| `IMessageCodec` | `NewLife.Messaging` | 协议编解码：`TryParse` 定界 / `Build` 整帧 / `BuildHeader` 头部；无状态可跨连接共享（见《消息协议栈》） |

## 与 System.IO.Pipelines 的差异（三个关键决策）

> **同名不同命名空间**：本库类型与 BCL 同名（`NewLife.Data` vs `System.IO.Pipelines`）。同一文件同时引用两者时用别名隔离——`using NlPipe = NewLife.Data.Pipe;`（同理 `NlPipeReader`/`NlPipeWriter`），或完全限定。BCL 类型需显式 using 才会冲突，实际碰撞面很小。

| 决策 | 原因 |
|------|------|
| 消费推进 `AdvanceTo` 以**字节计数**为主（另提供 `SequencePosition` 重载） | 追加数据会重建窗口序列，位置在跨追加场景不稳定；字节计数对跨段、跨轮简单可靠，且可在全部目标框架实现。位置重载取自最近一次读取窗口，供主流形态代码移植 |
| 解析优先用 `SequenceReader<T>` | 该类型不在 System.Memory 包资产中（net45~netstandard2.0 缺失），已由 `Stub/SequenceReader.cs` 垫片补齐（3.1 面，高版本经 `TypeForwardedTo` 转发到 BCL），全 18 TFM 可用；无符号端序读取由 `NewLife.Buffers.SequenceReaderHelper` 提供。协议解析（SrmpCodec/WebSocketCodec/LengthFieldCodec/SplitDataCodec）已迁移为序列顺序读取；`PacketHelper.GetPrefix` 保留给 `IPacket` 链头部直读场景 |
| 不引入 NuGet 依赖 | System.IO.Pipelines 非类库箱内（最低 net462，net45 永远不可用）；`ReadOnlySequence`/`ReadOnlySequenceSegment`/`BuffersExtensions` 经现有 System.Memory 依赖全 TFM 可用 |

窗口类型仍是 BCL 的 `ReadOnlySequence<Byte>`：`IPacket` 链经 `PacketHelper.AsReadOnlySequence()` 零拷贝桥接（单段快路径零分配）。

### 行为差异清单（2026-09-26 实测，2026-09-27 对齐）

上表三条是**设计取舍**，下表是**行为差异**：BCL 侧全部以 net10.0 运行时实测为准。**2026-09-27 已按建议逐条对齐**（"现状"列为对齐后的实现），仅余两条有意保留的偏差单列在表后。

| 行为 | 对齐前 | BCL（实测） | 现状（2026-09-27） |
|------|--------|-------------|--------------------|
| 读侧结束后继续 `ReadAsync`/`TryRead` | 返回 `IsCompleted` 空结果 | 抛 `InvalidOperationException` | **已对齐**：抛 `InvalidOperationException`（静默降级会把“复用已结束读取器/并发读”掩盖成流结束）。内部消费端（`WebSocket.ReadFrames`/`LimitedReader`）已加完成判定，正常关闭路径不受影响 |
| 写侧 `Complete(error)` 后的读侧读取 | 返回 `IsCompleted`，异常只在 `Pipe.Error` | 抛该异常 | **已对齐（最小版）**：新增 `PipeReader.Error`，`MessagePump.ReadAsync` 在完成且带异常时抛出该异常；不再把连接故障当优雅关闭。`ReadResult` 携带异常留作 v12 可选增强 |
| 读侧 `Complete(error)` 后的写侧提交 | 返回 `FlushResult(IsCompleted: true)` | 抛该异常 | **已对齐（最小版）**：`Reader.Complete(error)` 把异常记入 `Pipe.Error`，"两侧先完成方胜出"；写侧据 `Pipe.Error` 判断，而非依赖抛出 |
| 读侧结束后的写侧 `GetSpan`/`WriteAsync` | 抛 `InvalidOperationException`（空数据例外） | 正常返回（不抛） | **已对齐**：读侧结束后可取窗口写入，由 `FlushAsync` 的 `IsCompleted` 告知停止；**写侧自己结束**后再写仍抛异常（两种状态已区分） |
| `Complete()` 时"已 Advance 未 Flush"的数据 | 丢弃 | **先提交再结束**（实测：`Complete` 后读侧仍可读到该字节） | **已对齐**：`Complete` 先提交再标记完成（未推进的写缓冲仍丢弃） |
| `CancelPendingFlush()` 无挂起提交时 | 无效果 | **下一次提交标记取消**（实测：数据照常提交、读侧可读，且只生效一次） | **已对齐**：无挂起提交时暂存，由下一次提交标记取消一次（与 `CancelPendingRead` 对称） |

> **有意保留的两条偏差**：①`AdvanceTo` 在读侧结束后仍宽松（不抛）——内部中止竞态下"发送已完成、推进落空"属良性，且推进本身无副作用；②`MessagePump.TryRead`（同步形态）不抛错误，调用方按返回 false + `PipeReader.Error` 判断（同步形态不引入异常控制流）。
>
> 实测确认**已一致**、无需改的两项：①背压按"未检查数据"记账（见上文背压节，`AdvanceTo` 的 examined 会解除计入）；②`CancelPendingRead` 无挂起读取时，下一次读取立即返回取消结果且只生效一次。

> 实测确认**已一致**、无需改的两项：①背压按“未检查数据”记账（见上文背压节，`AdvanceTo` 的 examined 会解除计入）；②`CancelPendingRead` 无挂起读取时，下一次读取立即返回取消结果且只生效一次。

## 快速开始

```csharp
var pipe = new Pipe();                     // 写侧与读侧可来自不同线程
pipe.Writer.Append(received);              // 接收线程投递（所有权转移给管道）

var rr = await pipe.Reader.ReadAsync();    // 消费线程读取
if (!rr.Buffer.IsEmpty)
{
    // 窗口为只读序列，可跨段顺序解析；推进用字节计数
    pipe.Reader.AdvanceTo(consumed, examined);
    // examined：窗口前部已解析但不足成帧的字节数；无新数据时不会立即唤醒下一次读取
}

pipe.Writer.Complete();                    // 写入结束（唤醒挂起读取）
```

帧泵（整帧取出，含跨轮等待）：

```csharp
var pump = new MessagePump(new SrmpCodec());   // 无状态协议 + 帧泵，可跨连接共享

// 异步读取消息：头到齐即交付（整帧在窗口内为零拷贝切片，未齐则为流式体）
var msg = await pump.ReadAsync(pipe.Reader);
var body = msg.Body;   // LimitedReader：预算内窗口裁剪，读满恰好停在帧尾
// await body.ReadAsync() / body.AdvanceTo(...)
```

## 读取器 API 摘要

| 成员 | 说明 |
|------|------|
| `ReadAsync(ct)` | 有数据立即返回；无数据挂起，直到追加、完成或取消 |
| `ReadAtLeastAsync(min, ct)` | 等到窗口至少 min 字节（对齐 BCL ReadAtLeastAsync）；不消费数据 |
| `AsStream(leaveOpen)` | 以只读流形态消费（对齐 BCL PipeReader.AsStream） |
| `TryRead(out ReadResult)` | 同步尝试读取（不等待）；有数据、已取消或已结束时返回 true |
| `AdvanceTo(consumed)` | 消费推进（单参等价 examined=consumed） |
| `AdvanceTo(consumed, examined)` | 含 examined 语义的推进 |
| `AdvanceTo(SequencePosition…)` | 位置版推进，形态对齐 System.IO.Pipelines；位置必须取自最近一次读取窗口（过期位置抛 `InvalidOperationException`） |
| `TakeFrame(count)` | 零拷贝切出窗口前 count 字节为**拥有句柄**并推进窗口，同一锁内完成；帧可跨轮持有 |
| `Limit(count)` | 返回 `LimitedReader`，按帧长读取负载（AsPacket 视图 / ReadAllAsync 物化 / DrainAsync 丢弃余量） |
| `CancelPendingRead()` | 取消挂起读取；无挂起时下一次读取立即返回取消结果 |
| `Complete()` / `CompleteAsync(error?)` | 结束读取并释放全部未消费数据；带异常结束时记录到 `Error`（两侧先到先得）|
| `IsReaderCompleted` | 读取器自身是否已结束（结束后再读会抛 `InvalidOperationException`，对齐 BCL）；消费循环据此收尾 |
| `IsCompleted` / `Error` | 写侧是否已完成 / 管道结束时的异常。优雅结束应当“读到完成标记”，带异常结束应当“读到异常”，据此区分故障与优雅关闭 |

## 写入缓冲（Pipelines 形态）

写侧方法都挂在 `Pipe.Writer` 句柄上（与读取方 `pipe.Reader` 对称）。除 `Append(IPacket)`（包级、所有权转移、零拷贝）外，提供跨度写入形态（`PipeWriter` 实现 `IBufferWriter<Byte>`），对齐 System.IO.Pipelines 的 PipeWriter：

| 成员 | 说明 |
|------|------|
| `GetSpan(sizeHint)` / `GetMemory(sizeHint)` | 获取池化可写窗口（至少 sizeHint 字节）；FlushAsync 之前对读取方不可见 |
| `Advance(count)` | 推进写入位置（累计不得超过最近一次返回窗口长度） |
| `WriteAsync(data, ct)` | 写入并提交（拷贝进管道缓冲，等价 GetSpan + Advance + FlushAsync 的组合；形态对齐 PipeWriter.WriteAsync） |
| `FlushAsync(ct)` | 提交已推进数据（进入未消费窗口并唤醒读取）；返回 `FlushResult`（管道结束时 IsCompleted=true）；**达到暂停水位时挂起等待消费恢复（写侧回压，对齐 BCL）** |
| `FlushAsync(waitForResume, ct)` | 显式形态：true 同默认（挂起等待水位恢复）；false 仅提交不等待 |
| `AsStream(leaveOpen)` | 以只写流形态写入（写入即提交；对齐 BCL PipeWriter.AsStream） |
| `UnflushedBytes` | 尚未提交的字节数 |
| `CancelPendingFlush()` | 取消提交（结果 IsCanceled=true）：有挂起提交时立即唤醒；**无挂起提交时暂存，由下一次提交标记取消一次**（数据照常提交，对齐 BCL） |

```csharp
var span = pipe.Writer.GetSpan(4);
header.CopyTo(span);                 // 直接写进管道缓冲，无中间包
pipe.Writer.Advance(4);
await pipe.Writer.FlushAsync();      // 提交，读取方立即可见
```

> **完成语义**：`Complete()` 会**先把已推进未提交的数据提交给读侧**再结束（对齐 BCL，不静默丢数据）；未推进的写入缓冲在 `Complete`/`Dispose` 时丢弃（池缓冲归还）。读侧结束后仍可取窗口写入，由 `FlushAsync` 返回的 `IsCompleted` 告知停止；写侧自己结束后再写抛 `InvalidOperationException`。**命名说明**：类型与 BCL 同名（`Pipe`/`PipeReader`/`PipeWriter`/`ReadResult`/`FlushResult`，命名空间 `NewLife.Data`），心智直接复用 Pipelines 知识；同文件同时引用两个命名空间时用别名（见上文“同名不同命名空间”）。

## 背压（迟滞状态机）

- 未检查数据（未消费 − 已检查）达到 `PauseThreshold` 转入**暂停态**并保持（`IsPaused` 为 true）；接收方应暂停继续接收，缓冲有界、消费者不取数则接收方停止拉动（TCP 窗口自然回压）；默认值分侧：**入站 1M/512K**（Kestrel 入站缓冲上限同量级的内存安全阀）、**出站队列 256K/128K**（`TcpSession.CreateSendQueue()` 可重写；默认直发路径没有应用层水位），定档依据见《背压水位定档与内存测算报告》；
- 未检查数据降到 `ResumeThreshold` 以下解除暂停并触发 `Resumed`，接收方恢复接收；
- **记账口径（2026-09-26 修正）**：与 BCL 一致按“未检查”记账（BCL `_unconsumedBytes` 在 `AdvanceTo` 时被 examined 扣减，已实测确认），`AdvanceTo(consumed, examined)` 声明为已检查的字节即解除计入。单参 `AdvanceTo(consumed)` 等价 examined=consumed，故常规消费推进的暂停/恢复行为与“按未消费记账”完全一致；修正前按未消费记账，导致“只检查不消费”（如 `TryRead` + `AdvanceTo(0, len)` 等帧凑齐）时暂停无法解除、写侧 `FlushAsync` 永久挂起；
- **读饥饿让位（2026-09-17，实测死锁教训）**：读侧在“无新数据可交付”时才挂起等待（整帧未凑齐/凑最小长度）；此时消费不会再来、暂停无法再经消费解除，若继续持有将令接收方停摆（帧永远凑不齐）。故挂起等待时自动解除暂停并触发 `Resumed` 放行接收——**背压约束“已到达未检查的积压”，不束缚读者正在等待的数据**（大帧经此路径不受水位阻挡；代价是该帧驻留内存随帧长增长，超大 payload 建议头部先行流式）。2026-09-26 按“未检查”记账后，读者进入等待时未检查量必然不大于 0、暂停已自行解除，此处保留为运行期调低水位的兜底；
- **不能按瞬时长度判断**：多次小步消费时，跨过恢复水位那一步的起始长度已低于暂停水位，按瞬时判断会漏报恢复事件（e2e 实测教训）。默认值 0 或负数表示不启用背压。
- **写侧回压（预留能力，2026-09-17）**：`FlushAsync(ct)` 在暂停态返回未完成任务，与接收方向共用同一套水位状态机；恢复、结束与取消都会唤醒挂起提交。当前出站不使用它——出站队列（`TcpSession.SendQueue`）自行以 `IsPaused` + `Resumed` 等待水位，`PipeWriter` 的写侧提交只服务于写入器自身的缓冲提交路径。

## 在网络库中如何生效（端到端走一遍）

本节按“一个字节的旅程”把前面的类与真实网络代码对上号：谁建管道、谁写、谁读、背压怎么闭环、关闭怎么收尾；机制取舍的权威描述仍以《会话集成（TcpSession）》《背压（迟滞状态机）》《消费方接入》《所有权规则》为准。

### 全景图

```
【入站】
 Socket / SslStream 读回调
    │
    ▼
 TcpSession.OnReceiveAsync ── IsPaused？ ──► ParkReceive 暂存接收（不再向内核取数）
    │ 每轮数据（SAEA / SSL BeginRead）               ▲ Resumed 后重启
    ▼                                               │
 TcpSession.OnPreReceive ─ 0 字节判定对端关闭；其余投递
    │ 共享切片 pk.Slice(0, -1)（引用计数，轮末自动换新缓冲）
    ▼
 ┌ Pipe（入站，水位 1M / 512K）──────────────────────────────┐
 │ Writer.Append → 未消费窗口（段链，经 Reader 暴露只读序列）  │
 └───────────────────────────────────────────────────────────┘
    │ pipe.Reader
    ▼
 消费方（三选一）
   ├─ 协议模式：会话自动启动的 MessagePump 泵（头到齐即交付、体为 LimitedReader）
   ├─ WebSocket：帧泵（服务端 Process / 客户端接收循环）
   └─ 自定义：自行循环 MessagePump.TryRead / ReadAsync，或 pipe.Reader 直接读取
    │
    ▼
 OnMessageAsync 事件链 → 业务处理 → AdvanceTo 推进消费 → 降到恢复水位 → Resumed → 重启接收

【出站】
 业务 Send 系列 ──► 默认直发：写锁内 0 分配 0 拷贝写出（内核发送缓冲即水位）
 业务 SendQueuedAsync ──► SendQueue（Pipe，256K/128K）──► 发送泵取整窗 ──► Socket
```

### 各环节与代码位置

| 环节 | 做什么 | 代码位置 |
|------|--------|----------|
| 建管道 | `session.Pipe` 首次访问懒创建（`CreatePipe()` 可重写水位）；`GetPipe()` 不触发创建 | `TcpSession.Receive.cs` |
| 投递 | 每轮数据以**共享切片**追加进管道；管道未创建则直接返回（没人消费则零开销） | `AppendToPipe` |
| 背压暂停 | 发起下一次接收前查 `IsPaused`；暂停则暂存接收参数 | `OnReceiveAsync` / `ParkReceive` |
| 背压恢复 | `Resumed`（消费线程触发）取回暂存接收、经接收环同一入口重启 | `CreatePipe` 挂事件 / `ResumeParkedReceive` |
| 消费（协议模式） | 启动时起 `MessagePump` 泵：循环 `pump.ReadAsync(pipe.Reader)` 定界交付 | `TcpSession.Pump.cs`（`StartMessagePump`/`PumpAsync`） |
| 收尾 | 关闭时释放管道：挂起读立即得到“已结束”，残余缓冲归还池；`Pipe` 复位为 null，再次访问可重建 | `CleanupStreamFlows`（`TcpSession.cs`） |

### 从字节到消息（编号时序）

1. 接收环向内核发起读（SAEA 异步收；SSL 走 `BeginRead`）；
2. 数据到达 `OnPreReceive`：0 字节按对端关闭走收尾，否则 `AppendToPipe` 投递共享切片；
3. 追加即唤醒挂起的消费读：协议模式泵 `MessagePump.ReadAsync` 醒来定界；
4. 默认**头到齐即交付**消息（`RequireFullFrame` 或协议头部可要求整帧），帧体经 `LimitedReader` 按预算流式读取，不必等整帧驻留；
5. 消费推进 `AdvanceTo(consumed, examined)`：声明消费与检查的字节，背压按“未检查量”记账（口径见《背压》）；
6. 积压 ≥1M 触发暂停：接收环暂存下一次接收，TCP 窗口自然回压发送方；≤512K 触发 `Resumed`，接收环重启——**背压闭环**；
7. 关闭：`CloseAsync` → `CleanupStreamFlows` 释放管道（完成写侧唤醒挂起读、结束读侧归还残余）——挂起读立即得到“已结束”，会话对象可再次访问 `Pipe` 重建。

### 背压长在网络上

管道积压达到暂停水位时，`IsPaused` 亮起；`TcpSession` 在**发起下一次接收前**看到它，就把接收参数暂存——不再向内核取数，发送方的 TCP 窗口随之收紧，全程不需要任何显式流量控制协议。消费推进到恢复水位以下触发 `Resumed`，暂存的接收被取回重启。读侧“等数据凑帧”时会自动解除暂停放行数据（饥饿让位），大帧不会被水位卡死。默认水位与定档依据见《背压（迟滞状态机）》。

### 出站方向

出站默认**直发**（写锁内 0 分配 0 拷贝，内核发送缓冲即水位）。需要排队/批量时走可选的 `SendQueue`——与入站对称的一个 `Pipe`（水位 256K/128K）：`SendQueuedAsync` 入队后，发送泵循环取走整个读窗口（`TakeFrame`）一次散列写提交多条消息。两条出口各自内部有序、混用不保证彼此先后；**同一条逻辑消息必须走同一出口**。详见《出站方向：默认直发，可选发送队列（2026-10-01）》。

## 会话集成（TcpSession）

- 入站管道由连接型会话 `TcpSession` 提供（接口 `IStreamSession`）：`Pipe` 属性懒创建（`CreatePipe()` 虚方法可定制水位）；`GetPipe()` 为不触发创建的访问器；
- 接收环每轮把轮数据以**共享切片**（`pk.Slice(0, -1)`，引用计数）投递进管道——轮末裁决因引用计数大于 1 自动换新缓冲，**所有权协议零改动**；
- 暂停由 `TcpSession` 在发起下一次接收时判定（`OnReceiveAsync` 内查管道水位）：达到暂停水位时暂存接收事件参数，`Resumed`（消费线程）触发经接收环同一入口重启（仍暂停则再次暂存）；
- `CloseAsync` 完成管道写侧（挂起读取立即得到 IsCompleted）并释放挂起的事件参数。

> 数据管道属于连接型会话能力：`TcpSession` 在接收预处理中投递轮数据、在发起接收时执行背压暂停、在关闭流程中收尾（完成管道写侧；出站队列残余不排空，直接丢弃并归还缓冲）；基类不感知流式概念（仅保留接收环原语供子类驱动），UDP 等报文式协议每包即一帧，不需要字节流管道。

## 出站方向：默认直发，可选发送队列（2026-10-01）

出站曾与入站对称地引入 `SendPipe` + `SendPump` 发送泵（粘性队列单出口 + 水位背压，2026-09-17 落地）。该形态已废弃：管道一旦发布，`Send` 系列全部改入队，同一会话同时存在“持锁直发”与“泵写”两条路径，必须再加锁协商才能避免并发写同一 Socket/SslStream；且队列对借用视图与 `byte[]`/`Span` 入队必须拷贝，而直发可以做到 0 分配 0 拷贝。

现在的出站是**默认直发 + 一把写锁**（`TcpSession` 的 `_writeLock`，下列入口共用；`SessionBase.Send` 系列经 `OnSend` 直达写锁）：

| 入口 | 语义 |
|------|------|
| `Send(IPacket)` / `Send(byte[])` / `Send(Span)` | 锁内同步直写，写完才返回；0 分配 0 拷贝（链式包散列写，一次系统调用提交整条链） |
| `SendAsync(IPacket, ct)` | 锁内异步直写，等待可写期间不占线程 |
| `SendAsync(Stream, length, ct)` | 读一块（64KB 池化块）→ 写完或挂起 → 再读下一块，内存恒为一块 |
| `SendFileAsync(path, ct)` | 交给内核零拷贝推送（sendfile / TransmitFile），应用层不经读块 |

- **背压**：内核发送缓冲充当水位——慢对端让写调用挂起（同步）或异步等待（异步），应用层不积压；
- **排队与限流**：默认路径不承担，需要时在上层用 `Actor` 组合（见《并行模型Actor.md》）；批量场景可选走下面的发送队列；发送失败统一上报 `OnError("Send")` 并关闭会话；
- **数据报协议（UDP）**：保持整包直发语义，不受本变更影响。

### 可选的第二出口：发送队列

批量场景可改走 `TcpSession.SendQueue`（一个 `Pipe`，与入站 `Pipe` 对称），`Send` 系列不受影响：

| 项 | 约定 |
|----|------|
| 入口 | **只有异步** `SendQueuedAsync(IPacket, ct)`；无同步重载（同步等待会把入队方线程挂在水位上） |
| 所有权 | 入队即转移：泵写出后由队列释放，调用方不得再释放 |
| 水位 | 出站 256K 暂停 / 128K 恢复（`CreateSendQueue()` 可重写）；积压达上限时入队异步等待，泵写出后自动恢复 |
| 顺序 | 同一条逻辑消息必须走同一出口；两条出口混用时消息之间不保证先后（各自内部有序） |
| 批量 | 发送泵循环取走**整个读窗口**，一次散列写提交多条消息；写出时取同一把写锁，故两条出口不会字节交错 |
| 关闭 | **不排空**：残余直接丢弃并归还池缓冲，避免批量停机耗时随会话数线性放大 |
| 线程 | 泵运行在专用线程（`LongRunning`）上，不与线程池争抢 |

原 `SendPipe` 用法的迁移：

```csharp
// 旧：入队 + 写侧回压，发送在泵上异步完成（粘性单出口，Send 系列会一并改入队）
// var pipe = session.SendPipe;
// pipe.Writer.Append(packet);
// await pipe.Writer.FlushAsync(ct);

session.Send(packet);                  // 新：同步直发，写完才返回（借用语义，调用方仍负责释放句柄）

// 需要排队/批量时，改用可选的第二出口（只异步，所有权转移）
await session.SendQueuedAsync(packet, ct);

// 旧：TrySend（暂停时拒绝）已无对应概念；需要拒绝/丢弃策略时由上层 Actor 决定

// 流式与文件发送（两版都可用）
await session.SendAsync(bodyStream, len);     // 分块读写，内存恒为一块
await session.SendFileAsync(filePath);        // 内核零拷贝推文件
```

> **入站水位不受影响**：`Pipe.PauseThreshold`/`ResumeThreshold`、暂停接收与 `Resumed` 重启仍是入站机制；出站只是复用同一套水位原语，不改变入站行为。

> 历史沿革：出站曾于 2026-09-17 引入与入站对称的发送管道 + 发送泵（`SendPipe`/`SendPump`，粘性队列单出口 + 出站水位背压 + 背压感知入口 `TrySend`），2026-10-01 先整块移除、回归直发，再以**可选的第二出口**加回：形态不同——`Send` 系列始终直发，队列只作 `SendQueuedAsync` 入口，泵与直发共用写锁（避免旧形态的锁协商）。

## 消费方接入

| 消费方 | 接入方式 |
|--------|----------|
| WebSocket 服务端（`Http/WebSocket`） | `Process(IPacket)` 同步泵：入管道 → 循环取帧 → 交付消息；借阅视图自动克隆为自有拷贝 |
| WebSocket 客户端（`Net/WebSocketClient`） | 协议模式接入：`RequireFullFrame` 整帧泵出后经 `OnMessage` 校验交付；握手残片先投递入站管道 |
| 自定义消费 | 同步泵 `MessagePump.TryRead(reader, out message)` 循环、异步泵 `MessagePump.ReadAsync(reader)` 循环，或 `pipe.Reader` 自行读取 + `Limit` 限长流式消费 body |

同步泵在接收线程内完成"取帧→交付"，线程语义与旧链路一致；背压由轮末暂停水位裁决驱动。帧的定界信息由 `IMessageCodec.TryParse` 返回（`HeaderSize` + `BodyLength` 即本帧消费字节数），`MessagePump` 负责窗口推进与交付，消息体由帧层绑定（整帧内存视图 / 未齐流式读取器）。

## 所有权规则

| 对象 | 所有权 |
|------|--------|
| `Writer.Append(pk)` 入参 | 无条件转移给管道；调用后不得再使用或释放；管道已关闭时由管道负责释放 |
| 读取窗口 `Buffer` | 管道持有；仅对应数据被消费前有效，禁止跨轮缓存（跨轮请 `TakeFrame` 或 `Slice`） |
| `TakeFrame` 帧 | 拥有句柄（引用计数），与其他句柄各自释放，最后一个归还内存池 |
| `LimitedReader` | 限长视图；预算耗尽读取返回 IsCompleted；`DrainAsync`/释放对齐到帧尾；流式模式未读满就结束或取消时 `ReadAllAsync` 抛异常（取消 → `OperationCanceledException`；管道带错误结束 → 透传 `Pipe.Error`；否则 → `EndOfStreamException`），不返回半截体 |

## 边界与事实

- net45 无 `RunContinuationsAsynchronously`，挂起读取以条件编译降级（接受同步续体）；挂起提交同样降级；
- 挂起提交为单写纪律：同一时刻至多一个，重复挂起抛 `InvalidOperationException`；令牌取消抛 `OperationCanceledException`，`CancelPendingFlush` 完成结果为 `IsCanceled`；
- `MessagePump` 无状态可跨会话共享；`TryRead` 内部完成头部定界与体绑定（无需调用方预先取帧长）；
- 掩码帧（WebSocket 客户端方向）：整帧路径解析时就地解码；流式路径由消费方把窗口解码到目标跨度（公开窗口为只读序列，不支持原地流式改写）。
