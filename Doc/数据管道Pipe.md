# 数据管道Pipe

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
| `PacketFramer` | `NewLife.Messaging` | 帧泵：按定界委托从管道取出完整帧；支持头部先行与同步泵（Pump） |
| `LimitedReader` | `NewLife.Data` | body 限长读取（`PipeReader.Limit` 创建）：预算裁剪、TryRead/ReadAsync 消费、ReadAllAsync 物化、DrainAsync 对齐帧尾 |
| `IFrameMessage` | `NewLife.Messaging` | 消息帧契约：`TryParseHeader`（头先行）/`ReadFrame`（整帧）+ `Body` 限长读取器；`Message` 为基类实现 |

## 与 System.IO.Pipelines 的差异（三个关键决策）

> **同名不同命名空间**：本库类型与 BCL 同名（`NewLife.Data` vs `System.IO.Pipelines`）。同一文件同时引用两者时用别名隔离——`using NlPipe = NewLife.Data.Pipe;`（同理 `NlPipeReader`/`NlPipeWriter`），或完全限定。BCL 类型需显式 using 才会冲突，实际碰撞面很小。

| 决策 | 原因 |
|------|------|
| 消费推进 `AdvanceTo` 以**字节计数**为主（另提供 `SequencePosition` 重载） | 追加数据会重建窗口序列，位置在跨追加场景不稳定；字节计数对跨段、跨轮简单可靠，且可在全部目标框架实现。位置重载取自最近一次读取窗口，供主流形态代码移植 |
| 解析优先用 `SequenceReader<T>` | 该类型不在 System.Memory 包资产中（net45~netstandard2.0 缺失），已由 `Stub/SequenceReader.cs` 垫片补齐（3.1 面，高版本经 `TypeForwardedTo` 转发到 BCL），全 18 TFM 可用；无符号端序读取由 `NewLife.Buffers.SequenceReaderHelper` 提供。协议解析（DefaultMessage/WebSocketMessage/LengthFieldCodec）已迁移为序列顺序读取；`PacketHelper.GetPrefix` 保留给 `IPacket` 链头部直读场景 |
| 不引入 NuGet 依赖 | System.IO.Pipelines 非类库箱内（最低 net462，net45 永远不可用）；`ReadOnlySequence`/`ReadOnlySequenceSegment`/`BuffersExtensions` 经现有 System.Memory 依赖全 TFM 可用 |

窗口类型仍是 BCL 的 `ReadOnlySequence<Byte>`：`IPacket` 链经 `PacketHelper.AsReadOnlySequence()` 零拷贝桥接（单段快路径零分配）。

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
var parser = new DefaultMessage();
var framer = new PacketFramer { GetFrameLength = buffer => parser.TryParse(buffer, out _) };   // 无状态，可跨连接共享

// 方式一：整帧零拷贝切出（拥有句柄，可跨轮持有，用后 Dispose）
var frame = await framer.ReadFrameAsync(pipe.Reader);

// 方式二：头部先行（头消息 + body 限长读取；body 未读部分在 Dispose 时对齐跳过）
var msg = new DefaultMessage();
if (framer.TryReadHeader(pipe.Reader, msg, frameLength))
{
    var body = msg.Body;   // LimitedReader：预算内窗口裁剪，读满恰好停在帧尾
    // await body.ReadAsync() / body.AdvanceTo(...)
}

// 方式三：接收线程同步泵（不等待；回调内处理整帧，跨轮保留请切出共享句柄）
framer.Pump(pipe.Reader, frame => { /* 处理整帧 */ });
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
| `Complete()` / `CompleteAsync(error?)` | 结束读取并释放全部未消费数据 |

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
| `CancelPendingFlush()` / `CompleteAsync(error?)` | 取消挂起的提交（结果 IsCanceled=true；无挂起提交时无效果）/ 等价 `Complete` |

```csharp
var span = pipe.Writer.GetSpan(4);
header.CopyTo(span);                 // 直接写进管道缓冲，无中间包
pipe.Writer.Advance(4);
await pipe.Writer.FlushAsync();      // 提交，读取方立即可见
```

> 未提交的数据在 `Complete`/`Dispose` 时丢弃（池缓冲归还）。**命名说明**：类型与 BCL 同名（`Pipe`/`PipeReader`/`PipeWriter`/`ReadResult`/`FlushResult`，命名空间 `NewLife.Data`），心智直接复用 Pipelines 知识；同文件同时引用两个命名空间时用别名（见上文"同名不同命名空间"）。

## 背压（迟滞状态机）

- 未消费数据达到 `PauseThreshold`（默认 1M）转入**暂停态**并保持（`IsPaused` 为 true）；接收方应暂停继续接收，缓冲有界、消费者不取数则接收方停止拉动（TCP 窗口自然回压）；
- 消费降到 `ResumeThreshold`（默认 512K）以下解除暂停并触发 `Resumed`，接收方恢复接收；
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
- **写侧回压**：未发送数据达到 `PauseThreshold` 后 `IsPaused` 为 true，`await pipe.Writer.FlushAsync(ct)` 默认挂起等待（对齐 BCL；`FlushAsync(false, ct)` 仅提交不等待）；泵 `AdvanceTo` 推进降到 `ResumeThreshold` 以下时唤醒（与入站共用同一套水位状态机）；
- **生命周期**：`TcpSession.CloseAsync` 重写内先完成写入并限时（会话超时）等待泵发完已排队数据，再进入基类关闭流程；无连接关闭时直接中止管道（幂等），避免泵悬挂；发送失败（`OnSend` 返回负值或抛异常）中止管道——错误随 `Error`，挂起提交被唤醒，后续追加的数据由管道直接释放；
- 数据报协议（如 UDP）不使用本管道，保持整包直发语义；读侧 `pipe.Reader` 为发送泵独占，请勿另作它用。

```csharp
var pipe = session.SendPipe;                          // 首次访问：创建管道并启动发送泵
pipe.Writer.Append(packet);                           // 追加（所有权转移）
await pipe.Writer.FlushAsync(ct);                     // 写侧回压：排队过深时挂起等待（默认，对齐 BCL）
await session.SendAsync(fileStream);                  // 流式发送：分块入管道 + 写侧回压，大文件不整段驻留
session.Send(msg.BuildHeaderPacket(len));             // 消息协议：头部声明负载长度（旧名 ToHeaderPacket 保留为兼容桥）
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
| `LimitedReader` | 限长视图；预算耗尽读取返回 IsCompleted；`DrainAsync`/释放对齐到帧尾 |

## 边界与事实

- net45 无 `RunContinuationsAsynchronously`，挂起读取以条件编译降级（接受同步续体）；挂起提交同样降级；
- 挂起提交为单写纪律：同一时刻至多一个，重复挂起抛 `InvalidOperationException`；令牌取消抛 `OperationCanceledException`，`CancelPendingFlush` 完成结果为 `IsCanceled`；
- `PacketFramer` 无状态可跨会话共享；`TryReadHeader` 要求帧长不小于头长（否则抛 `InvalidDataException`）；
- 掩码帧（WebSocket 客户端方向）：整帧路径解析时就地解码；流式路径由消费方把窗口解码到目标跨度（公开窗口为只读序列，不支持原地流式改写）。
