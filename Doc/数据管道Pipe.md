# 数据管道Pipe

> ⚠️ **旧栈引用（v12）**：文中与《数据包编码器PacketCodec》的对比属历史定位——`PacketCodec`/`PacketFramer` 已整删，帧定界改由 `MessagePump` + `IMessageCodec` 承担（见《消息协议栈》）。`Pipe` 本身及本文的缓冲/背压/所有权内容仍然有效。

## 概述

`Pipe` 处理字节流的**缓冲与消费**：接收方持续投递数据、消费方按序列消费、缓冲有界自动背压。与《数据包编码器PacketCodec》的区别——PacketCodec 是"粘包拆帧器"（每连接实例、提供帧切分），Pipe 是"字节流缓冲"（连接读写两侧的解耦层，数据进出都经管道，帧的定界交给上层帧泵）。

**模型：单写单读、未消费链持有、`ReadOnlySequence<Byte>` 零拷贝窗口、有界背压。** **命名与形态对齐**主流 `System.IO.Pipelines` 的 `Pipe`/`PipeReader`/`PipeWriter`（同名不同命名空间），且**全 TFM 可用（含 net45）**。

**命名空间**：`NewLife.Data`（帧泵与流式消息在 `NewLife.Messaging`，见下文组件表）

> 全局缓冲所有权与消息层归属处理见《网络缓冲所有权架构》。

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
| 读侧结束后继续 `ReadAsync`/`TryRead` | 返回 `IsCompleted` 空结果 | 抛 `InvalidOperationException` | **已对齐**：抛 `InvalidOperationException`（静默降级会把"复用已结束读取器/并发读"掩盖成流结束）。内部消费端（`SendPump`/`WebSocket.ReadFrames`/`LimitedReader`）已加完成判定，正常关闭路径不受影响 |
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

- 未检查数据（未消费 − 已检查）达到 `PauseThreshold` 转入**暂停态**并保持（`IsPaused` 为 true）；接收方应暂停继续接收，缓冲有界、消费者不取数则接收方停止拉动（TCP 窗口自然回压）；默认值分侧：**入站 1M/512K**（Kestrel 入站缓冲上限同量级的内存安全阀）、**出站 64K/32K**（对齐 BCL/Kestrel 出站档），定档依据见《背压水位定档与内存测算报告》；
- 未检查数据降到 `ResumeThreshold` 以下解除暂停并触发 `Resumed`，接收方恢复接收；
- **记账口径（2026-09-26 修正）**：与 BCL 一致按“未检查”记账（BCL `_unconsumedBytes` 在 `AdvanceTo` 时被 examined 扣减，已实测确认），`AdvanceTo(consumed, examined)` 声明为已检查的字节即解除计入。单参 `AdvanceTo(consumed)` 等价 examined=consumed，故常规消费推进的暂停/恢复行为与“按未消费记账”完全一致；修正前按未消费记账，导致“只检查不消费”（如 `TryRead` + `AdvanceTo(0, len)` 等帧凑齐）时暂停无法解除、写侧 `FlushAsync` 永久挂起；
- **读饥饿让位（2026-09-17，实测死锁教训）**：读侧在“无新数据可交付”时才挂起等待（整帧未凑齐/凑最小长度）；此时消费不会再来、暂停无法再经消费解除，若继续持有将令接收方停摆（帧永远凑不齐）。故挂起等待时自动解除暂停并触发 `Resumed` 放行接收——**背压约束“已到达未检查的积压”，不束缚读者正在等待的数据**（大帧经此路径不受水位阻挡；代价是该帧驻留内存随帧长增长，超大 payload 建议头部先行流式）。2026-09-26 按“未检查”记账后，读者进入等待时未检查量必然不大于 0、暂停已自行解除，此处保留为运行期调低水位的兜底；
- **不能按瞬时长度判断**：多次小步消费时，跨过恢复水位那一步的起始长度已低于暂停水位，按瞬时判断会漏报恢复事件（e2e 实测教训）。默认值 0 或负数表示不启用背压。
- **写侧回压（双向背压，2026-09-17；默认挂起对齐 BCL）**：`FlushAsync(ct)` 在暂停态返回未完成任务——发送方向（应用持续写入）与接收方向共用同一套水位状态机；恢复、结束与取消都会唤醒挂起提交，避免无界积压。

## 会话集成（TcpSession）

- 入站管道由连接型会话 `TcpSession` 提供（接口 `IStreamSession`）：`Pipe` 属性懒创建（`CreatePipe()` 虚方法可定制水位）；`GetPipe()` 为不触发创建的访问器；
- 接收环每轮把轮数据以**共享切片**（`pk.Slice(0, -1)`，引用计数）投递进管道——轮末裁决因引用计数大于 1 自动换新缓冲，**Detach 协议零改动**；
- 暂停由 `TcpSession` 在发起下一次接收时判定（`OnReceiveAsync` 内查管道水位）：达到暂停水位时暂存接收事件参数，`Resumed`（消费线程）触发经接收环同一入口重启（仍暂停则再次暂存）；
- `CloseAsync` 完成管道写侧（挂起读取立即得到 IsCompleted）并释放挂起的事件参数。

> 数据管道属于连接型会话能力：`TcpSession` 在接收预处理中投递轮数据、在发起接收时执行背压暂停、在关闭流程中收尾（先排空发送队列、后完成管道写侧）；基类不感知流式概念（仅保留接收环原语供子类驱动），UDP 等报文式协议每包即一帧，不需要字节流管道。

## 发送管道（出站，2026-09-17）

`TcpSession.SendPipe` 与入站 `Pipe` 对称：懒创建（`CreateSendPipe()` 虚方法可定制水位），**发送泵**由内部组件 `SendPump` 承担（唯一的读侧消费方）循环取出窗口逐段发送（一次唤醒批处理整窗、部分发送自动续发）。

- **单出口**：管道创建后 `Send` 系列方法全部改为追加进管道（`TcpSession.OnSend` 内部队列优先分发）——`Send(IPacket)` 拥有句柄零拷贝入管道（借阅视图自动转自有拷贝，`Send(byte[])`/`Span` 按副本入管道，调用方可立即复用缓冲）；与管道内排队数据天然无交错，发送在泵上异步完成；
- **流式发送**：`TcpSession.SendAsync(Stream, length, ct)` 从数据流分块（64KB）读取，每块零拷贝包装入管道并带写侧回压，大文件全程只在读块上驻留；"头 + 流式体"组合消息先 `Send(header)` 再 `SendAsync(body)`，整条消息仍走单出口顺序；
- **写侧回压**：未检查数据（泵按 consumed 推进，等价于未发送数据）达到 `PauseThreshold`（默认 64K，恢复 32K）后 `IsPaused` 为 true，`await pipe.Writer.FlushAsync(ct)` 默认挂起等待（对齐 BCL；`FlushAsync(false, ct)` 仅提交不等待）；泵 `AdvanceTo` 推进降到 `ResumeThreshold` 以下时唤醒（与入站共用同一套水位状态机）；
- **泵内异步（2026-09-18）**：泵的写委托在现代 TFM（`NET5_0_OR_GREATER`）经 `Socket.SendAsync` 真异步发出（每段预算 = 会话 `Timeout`，复用 CTS 避免热路径分配，超预算按超时中止管道），慢对端等待期间不占泵线程；`net5.0` 以下目标降级为同步续发（语义等价）。吞吐实测与直发持平（±5% 内），收益在并发线程占用而非速度；
- **背压感知发送（2026-09-18）**：除手动 `Append + FlushAsync` 外，`TcpSession` 提供两个水位感知入口——`SendAsync(IPacket, ct)`（Open 守卫 → 入队 → 提交等待：未暂停立即完成，暂停则挂起等水位恢复）与 `TrySend(IPacket)`（不主动 Open；暂停或未激活时拒绝返回 false，由调用方决策重试/丢弃）。注意：`Send` 系列经管道后**不等待**（非阻塞入队），需要背压语义必须用上述 API 或 `SendAsync(Stream)`；
- **生命周期**：`TcpSession.CloseAsync` 重写内先完成写入并限时（会话超时）等待泵发完已排队数据，再进入基类关闭流程；无连接关闭时直接中止管道（幂等），避免泵悬挂；发送失败（`OnSend` 返回负值或抛异常）中止管道——错误随 `Error`，挂起提交被唤醒，后续追加的数据由管道直接释放；
- 数据报协议（如 UDP）不使用本管道，保持整包直发语义；读侧 `pipe.Reader` 为发送泵独占，请勿另作它用。

```csharp
var pipe = session.SendPipe;                          // 首次访问：创建管道并启动发送泵
pipe.Writer.Append(packet);                           // 追加（所有权转移）
await pipe.Writer.FlushAsync(ct);                     // 写侧回压：排队过深时挂起等待（默认，对齐 BCL）
await session.SendAsync(packet, ct);                  // 背压感知入队：等待水位恢复（未暂停立即完成）
session.TrySend(packet2);                             // 暂停或未激活时返回 false，由调用方决策重试/丢弃
await session.SendAsync(fileStream);                  // 流式发送：分块入管道 + 写侧回压，大文件不整段驻留
session.Send(msg.BuildHeader(len));             // 消息协议：头部声明负载长度
await session.SendAsync(bodyStream, len);             // 体流式跟随，单出口保证无交错
```

## 消费方接入

| 消费方 | 接入方式 |
|--------|----------|
| WebSocket 服务端（`Http/WebSocket`） | `Process(IPacket)` 同步泵：入管道 → 循环取帧 → 交付消息；借阅视图自动克隆为自有拷贝 |
| WebSocket 会话（`Net/Handlers/WebSocketCodec`） | `Open` 触建管道；`Read` 检测管道同步泵帧（按 `IStreamSession` 接入）；非流式属主回退旧 PacketCodec 路径 |
| 自定义消费 | `framer.ReadFrameAsync(pipe.Reader)` 循环、`TryReadHeader` + body 限长流式，或接收线程内 `framer.Pump(reader, frame => ...)` 同步泵出 |

同步泵在接收线程内完成"取帧→交付"，线程语义与旧链路一致；背压由轮末暂停水位裁决驱动。帧泵统一序列委托 `GetFrameLength`；消息协议直接绑定 `Message.TryParseHeader`（旧桥接 `TryParse` 等价）。

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
