# 裸 Socket 层性能测试报告

> 测试范围：**不含协议编码器**的裸 Socket 收发链路（TCP/UDP 回环 echo），覆盖吞吐、延迟（P50/P95/P99）与内存分配三大指标。
> 关联报告：[网络库编解码器Echo性能测试报告](网络库编解码器Echo性能测试报告.md)（协议层）、[内存分配与拷贝成本报告](../内存分配与拷贝成本报告.md)。

## 测试目标

- 建立裸 Socket 层（NetServer/Echo + TcpSession/UdpServer 客户端）的**吞吐、延迟、分配基线**；
- 通过压力测试**暴露性能缺陷并闭环改进**；
- 输出可复现的测量方法与数据，作为后续协议层/管道层优化的对照基准。

## 测试环境

```text
BenchmarkDotNet v0.15.8
Windows 10 (10.0.19045.6456/22H2)
Intel Core i9-10900K CPU 3.70GHz, 1 CPU, 20 逻辑核心 / 10 物理核心
.NET SDK 10.0.401
Runtime: .NET 10.0.12, X64 RyuJIT x86-64-v3（Server GC）
网络：loopback（127.0.0.1），服务端与客户端同进程
```

## 测试方法

### 方法 1：BenchmarkDotNet 基准（`Benchmark/NetBenchmarks/NetEchoBenchmark.cs`）

- 两个模式：**逐包往返**（发一包读满回显，测往返链路成本）与**流水线**（持续发送 + 并发读取回显，测吞吐上限）；
- 维度：包大小 `16 / 256 / 4096 / 65536 / 1048576` B × 并发 `1 / 4 / 16 / 64`；
- 单迭代数据量按 `TargetBytes = 256MB` 限流，小包档以 `MaxRoundTrips = 50K` / `MaxPackets = 2M` 上限保护，保证各档迭代时长可比；
- `MemoryDiagnoser` + `Server GC`，`warmup=2 / iteration=5`；
- 客户端拉取模式（`AutoReceive=false`），服务端 `NetServer` 收到即回发。

### 方法 2：独立压测程序（`Benchmark/NetLoadTest`）

- 用法：`dotnet run --project Benchmark/NetLoadTest -c Release -- [--mode pipeline|roundtrip] [--clients N] [--size B] [--seconds S] [--warmup S] [--udp]`
- 同进程 echo 服务端（服务端侧计数回显字节）+ N 客户端拉取模式；
- 指标：吞吐（服务端回显 msg/s、MB/s）、往返延迟分位（P50/P95/P99、max，微秒级采样）、分配（`GC.GetTotalAllocatedBytes` 窗口差 / 消息数）、GC 计数、端到端完整性（发送 vs 接收）；
- 预热 2s + 正式窗口 10s；线程池最小线程已预热（见"缺陷 2"）；UDP 模式容忍丢包（接收超时视为排水结束）。

## 测试结果

### 1. 吞吐（NetLoadTest，TCP/UDP 回环 echo）

| 场景 | 吞吐 | 带宽 | 分配 | GC（10s 窗口） | 完整性 |
|---|---:|---:|---:|---:|---:|
| TCP 流水线 8 客户端 × 1KB | **1,043,725 msg/s** | **1,019 MB/s** | **4.4 B/msg** | Gen0+4 Gen1+1 Gen2+0 | 差 0 |
| TCP 流水线 8 客户端 × 64KB | 21,038 msg/s | **1,315 MB/s** | 172.6 B/msg | Gen0+3 Gen1+1 Gen2+0 | 差 0 |
| UDP 流水线 4 客户端 × 1472B | 85,856 msg/s | 120.5 MB/s | 905 B/msg¹ | Gen0+40 | 85% 丢包² |

> ¹ UDP 风暴下接收超时异常路径（SocketException）占主导分配；² UDP 无流控，发送速率远超接收能力必然丢包，见"发现 3"。

### 2. 往返延迟（NetLoadTest，微秒）

| 场景 | P50 | P95 | P99 | 平均 | max |
|---|---:|---:|---:|---:|---:|
| TCP 4 客户端 × 1KB | 49.5 µs | 72.1 µs | 85.0 µs | 52.0 µs | 5.9 ms |
| TCP 64 客户端 × 1KB（线程池预热后） | 281.6 µs | 325.8 µs | **358.2 µs** | 283.7 µs | **15.0 ms** |
| UDP 1 客户端 × 256B | **42.7 µs** | 58.5 µs | 89.9 µs | 45.9 µs | 70 ms³ |

> ³ UDP 单点毛刺（偶发调度/内核缓冲），低频可忽略；P99 89.9µs 正常。

低并发下的基础链路成本：**最小往返 23.8 µs（TCP）/ 26.4 µs（1KB）**，即本库**同步拉取模型**的单次往返实现底线约 **25µs**。

> 口径勘误（2026-09-21）：25µs **不是回环网络的物理极限**——loopback 单次内核传输仅 1-1.2µs；这 25µs 的大头是**两次用户态系统调用（Send + 阻塞 Receive 唤醒）+ 线程调度 + 服务端回显链路**。异步/单向上行路径不受此限（见“第二轮：单向上行与极限实验”）。

### 3. 内存分配

| 场景 | 分配强度 | 来源分析 |
|---|---:|---|
| 流水线 1KB（事件批量收发） | **4.4 B/msg** | 接收缓冲 `ArrayPool` 池化 + 轮末零 Rent/Return，基本无托管分配 |
| 逐包往返 1KB | ~210 B/往返（~105 B/方向） | 每包 `new OwnerPacket(BufferSize)`：**OwnerPacket + ArrayOwner 两个对象**，缓冲本身走 `ArrayPool`（已池化） |
| 流水线 64KB | 172.6 B/msg（占负载 0.26%） | 大包下对象开销被摊薄 |
| 1MB 档（BDN） | ~168 KB/迭代 | 相比 256MB 迭代数据量可忽略 |

**结论**：分配已**接近池化极限**；进一步下降需要 `OwnerPacket` 对象池化，收益 ≤80B/包且涉及引用计数语义重构（高风险），留待专项评估。

### 4. BDN 明细（修正版基准 = 线程池预热后；含两轮对照）

（40 组合完整表见 `BenchmarkDotNet.Artifacts/results/Benchmark.NetBenchmarks.NetEchoBenchmark-report-github.md`；关键行摘录）

| 方法 | 包大小 | 并发 | Mean（每迭代） | StdDev | 分配 | 第一轮对照（Mean / StdDev） |
|---|---:|---:|---:|---:|---:|---|
| 逐包往返 | 16 | 64 | 284.6 ms | 2.8 ms | 7,853 KB | 269.6 ms / 9.8 ms |
| 流水线 | 16 | 64 | 1,144.9 ms | 7.6 ms | 2,198 KB | 1,136.8 ms / 25.8 ms |
| 逐包往返 | 4096 | 64 | 348.0 ms | 4.6 ms | 7,950 KB | 340.6 ms / 13.9 ms |
| **逐包往返** | **65536** | **64** | **107.6 ms** | **1.8 ms** | 651 KB | 695.1 ms / **875.3 ms** ← 毛刺消除 |
| **逐包往返** | **1048576** | **64** | **190.7 ms** | **1.2 ms** | 201 KB | 820.5 ms / **397.3 ms** ← 毛刺消除 |
| 流水线 | 1048576 | 64 | 196.3 ms | 2.2 ms | 201 KB | 161.2 ms / 5.7 ms |

> 线程池预热后，大并发（64）大包往返的 **StdDev 从数百毫秒收敛到 1-2ms**（-99.7% 以上），Mean 同步下降 77-84%——与压测程序侧“秒级毛刺”对照完全一致（见缺陷 2）。

**表格读法（Mean/StdDev 单位换算）**：
- **Mean/StdDev 单位为毫秒**（BDN 自动选单位，本组均为 ms），且是**单次迭代耗时**而非单次操作——每迭代受 `TargetBytes = 256MB` / `MaxPackets = 2M` / `MaxRoundTrips = 50K` 限流；
- **换算吞吐**：`吞吐 ≈ 迭代数据量 ÷ Mean`。如 1MB×64 流水线 196.3ms → 256MB ÷ 0.1963s ≈ 1.30 GB/s；
- **StdDev 是迭代间的标准差**（迭代 5 次），**StdDev/Mean 越小越稳**：预热后大包档 StdDev 1-2ms ≈ 1-2%，即抖动 <2%；毛刺轮 StdDev 875ms 甚至大于 Mean 695ms，说明迭代间极不稳定（部分迭代被秒级停顿支配）；
- 小包档 Mean 更大是**限流保护**所致（MaxPackets/MaxRoundTrips 提前终止），不同档位间**不要直接比 Mean 绝对值**，用换算吞吐对照。

## 缺陷发现与改进闭环

### 缺陷 1：SSL 会话断线无感知（正确性缺陷，已修复）

- **现象**：SSL 客户端强制 RST 断开后，服务端会话悬挂（`Active` 长时间保持 true），无断开事件、资源不回收。
- **根因**：`TcpSession.OnEndRead`（SSL 异步读回调）对 `IOException`/`ConnectionReset` **静默吞掉**（空 catch），既不重投接收也不触发断开链路。
- **修复**：异常归一为"对端已关闭"（0 字节）处理——`se.SocketError = SocketError.Success; ProcessEvent(se, 0, 1);`，与正常关闭路径统一（`commit f5b3ea66a`）。
- **验证**：新增用例 `SSL_客户端强制RST_服务端感知并关闭会话` **先红（10s 超时）后绿**；`SSL_回环echo_256KB` 覆盖 SSL 链路逐字节完整性。

### 缺陷 2：高并发同步往返"秒级毛刺"（性能缺陷，已改进）

- **现象**：64 客户端逐包往返（同步 `Receive`），出现 **max 1027ms 的秒级毛刺**、P99 2264µs、吞吐仅 48,747 msg/s；BDN 中 65536×64 组合同样出现高方差（StdDev 875ms）。
- **根因**：64 客户端 × 2（发/收）共 **128 个同步阻塞任务**；线程池从 ~20 线程开始按 **~1-2 线程/秒**注入 → 任务排队等待线程 → 秒级停顿 + 吞吐塌陷。
- **改进**：压测程序/基准 **预热最小线程池**（`ThreadPool.SetMinThreads(512, 512)`，`commit 5870e9a8a / 198bc471d`）。
- **复测对照（64×1KB 往返）**：

| 指标 | 改进前 | 改进后 | 变化 |
|---|---:|---:|---:|
| P50 | 300.1 µs | 281.6 µs | -6% |
| P95 | 1,330.6 µs | 325.8 µs | **-76%** |
| P99 | 2,264.2 µs | 358.2 µs | **-84%** |
| max | 1,027,783 µs | 15,000 µs | **−98.5%** |
| 吞吐 | 48,747 msg/s | 220,059 msg/s | **+351%** |

- **库级启示**：同步拉取（`Receive`）是**阻塞模型**——高并发（>16 连接）应优先 **异步 API**（`ReceiveAsync`/事件模式），或如压测程序一样预热线程池；该结论已写入本报告建议。

### 发现 3：UDP 无流控极限（记录，非缺陷）

- UDP 流水线 4 客户端极限发送下 **85% 丢包**：发送端速率远超服务端消费能力（服务端回显 ~86K msg/s / 120MB/s 为实测接收极限），UDP 无背压必然丢弃；
- 单客户端 UDP 回环完全正常（P50 42.7µs，零丢包）——**回环 UDP 链路本身健康**；
- **建议**：UDP 发送端按业务自我限速，或采用 `MaxAsync` 增大服务端并发接收槽位提高消费速度。

### 发现 4：`UdpSession` 与 `INetSession` 接口不一致（API 一致性记录）

- `NetServer.Received` 事件的 sender：TCP 为 `NetSession`（实现 `INetSession`），UDP 为 `UdpSession`（仅实现 `ISocketSession`，**不实现 `INetSession`**）；
- 影响：通用的 `s is INetSession` 分支在 UDP 下静默不命中（压测程序开发中实际踩到）；
- 处置：本次**不改库**（给 `UdpSession` 补接口涉及类型布局变更，风险高于收益），作为文档记录，建议在 `UdpSession`/`NetServer.Received` 注释中明确说明。

## 第二轮：单向上行、接收环复用与极限实验（2026-09-21）

### 1. 分离进程与单向上行（消除 CPU 共享与回显流量）

原同进程 echo 双向流量下（1KB ≈1.0GB/s），将服务端独立为进程（`--server`）、客户端只发不收（`--oneway`）后：

| 场景 | 吞吐 | 带宽 | 分配 |
|---|---:|---:|---:|
| 8 客户端 × 1KB 单向 | 139.5 万 pkt/s | **1.36 GB/s** | 0.0 B/msg |
| 8 客户端 × 64KB 单向 | 6.9 万 pkt/s | **4.34 GB/s**（粘包口径峰值 5.03） | 0.1 B/msg |

- 64KB 单向 **4.3-5.0 GB/s（34-40 Gbps）** 已超越历史版本记录（23.4Gbps）；
- 1KB 单向服务端稳态 1.42-1.44 GB/s（约 142 万 pkt/s）。

### 2. 服务端单进程接收饱和

4 客户端进程 × 8 连接 = 32 连接 × 1KB 单向：服务端稳态 **1,542-1,610 MB/s（≈155-160 万 pkt/s）**，较 8 客户端仅 +20%——**单进程接收环已饱和**，瓶颈为每包固定成本（回调、轮末裁决、互锁计数）。横向扩展需多服务端实例。

### 3. 接收环 OwnerPacket 复用（commit ff0514c11）

- **原状**：每轮接收固定 `new OwnerPacket + new ArrayOwner`（2 对象），轮末无外部持有时 `Detach` 脱手；有共享切片时换缓冲；
- **改造**：句柄轮末**回挂接收槽**（`RecvSlot` 挂 `SocketAsyncEventArgs.UserToken`：槽序号 + 回挂句柄），下一轮 `Rebind` 重绑到新数据段（`ArrayOwner.Reset` 同步缓冲），会话释放时统一脱手归还——**热路径零包装对象分配**；
- **对照**：

| 场景 | 改造前 | 改造后 | 变化 |
|---|---:|---:|---:|
| 1KB 单向 | 131.7 万 pkt/s | **139.5 万 pkt/s** | **+6~10%** |
| 64KB 单向 | 3.87 GB/s | **4.34 GB/s** | **+12~15%** |
| 分配（两档） | 0.0 / 0.1 B/msg | 0.0 / 0.1 B/msg | 持平（本就池化极限） |

- 收益来源：省去每轮对象构造、`Detach`/finalizer 抑制与所有者重建的固定成本——包越小、轮次越多，收益越显著；
- 正确性：全量 2943 用例回归通过（含切片共享、链式帧、会话生命周期专项）。

### 4. 粘包口径与历史记录对照

历史记录（ChangeLog：**23.4 Gbps / 1.4 亿 pkt/s**）经确认为“带协议 TCP + 大量 24B 小帧粘连成大包整体收发”的口径。新压测程序支持 `--frame 24`：发送缓冲对齐到帧整倍数，吞吐按“接收字节 ÷ 帧大小”折算逻辑帧：

| 场景 | 大包尺寸 | 逻辑帧/包 | 帧吞吐 | 字节带宽 |
|---|---:|---:|---:|---:|
| 1KB 邻接 | 1,008 B | 42 | **5,636 万 frame/s** | 1.29 GB/s |
| 64KB 大粘包 | 65,520 B | 2,730 | **2.2 亿 frame/s**（服务端峰值 2.31 亿） | **5.03 GB/s** |

- **64KB 粘包场景帧吞吐 2.2 亿 frame/s，超越历史 1.4 亿记录 57%**；对应带宽 40.2Gbps（历史 23.4Gbps）；
- 帧率随粘包粒度上升而增长（1KB 档受“每轮固定成本”主导，64KB 档接近“每字节成本”极限），历史 1.4 亿对应约 3.4GB/s 带宽，位于两档之间，符合“当时版本 + 当时粘包粒度”的预期；
- 本组为**裸 Socket 口径**（不含协议解码）；协议层解码帧率另见编解码器报告。

## 分析

1. **TCP 裸层吞吐**：单进程回环 echo 达 **1.0-1.3 GB/s / 104 万 msg/s（1KB）**；对照纯接收基线（32B 不回发 C=64 时 ~524ns/包 ≈ 190 万包/s），echo 往返流量（双向）下的 104 万 msg/s 处于合理区间，链路无显著瓶颈。
2. **延迟**：单连接往返底线 ~25-45µs（回环物理 + IOCP 调度）；4 并发 1KB P50 49.5µs、P99 85µs 属于健康水平；延迟主要构成为**调度与系统调用**（同步拉取两次 syscall + 服务端回显链路），非库内计算。
3. **分配**：流水线模式 4.4B/msg 已证明接收路径的零分配优化（P0 轮缓冲复用）有效；逐包往返的 ~105B/方向来自 `OwnerPacket` 对象对，属可接受的常数级开销。
4. **并发扩展性**：修复线程池毛刺后，64 并发小包往返吞吐为 4 并发的 ~2.9 倍（220K vs 76K msg/s），P99 从 2.3ms 收敛到 0.36ms——**瓶颈在测试端线程模型而非库本身**。

## 建议

1. **高并发（>16 连接）使用异步接收**（`ReceiveAsync`/事件模式），避免同步阻塞耗尽/拥塞线程池；必须同步时预热 `ThreadPool.SetMinThreads`。
   与《[网络库同步异步分工](../网络库同步异步分工.md)》双轨设计呼应：同步直发/同步拉取定位为低并发快路径（本轮实测 4 并发 P50 49.5µs 验证"微秒级"定位）；该文档未展开的边界是**拉取模式同步 `Receive` 每连接占用一个阻塞等待线程**——本轮实测补充：64 连接同步往返若不预热线程池，会出现秒级毛刺与吞吐塌陷（见缺陷 2）。
2. **UDP 发送端限速**（无流控本质），或通过 `UdpServer.MaxAsync` 提高接收环并发度；
3. **延迟基线参考**：单连接回环往返 25-45µs、4 并发 P99 ≈85µs、64 并发（预热后）P99 ≈360µs；
4. 后续若需进一步压榨往返路径分配，可专项评估 `OwnerPacket` 对象池化（预估收益 ≤80B/包，需重审引用计数语义）。
   ——已闭环：接收环改用“句柄回挂接收槽”复用（不离散化对象池），见“第二轮 · 3”，单向吞吐 +6~15%（commit ff0514c11）。
5. **接收环饱和与扩展**：单进程 1KB 接收饱和 ≈1.6GB/s（≈155 万 pkt/s），更高吞吐需横向多实例；每包固定成本（回调/轮末裁决/引用计数）是小包档主要开销，后续可评估“批处理回调”。
6. **粘包粒度红利**：业务允许时增大发送聚合（更大的应用层批），可显著摊薄每包固定成本（64KB 档帧率 2.2 亿 vs 1KB 档 5,636 万）。

## 复现命令

```bash
# BDN 基准（约 10 分钟，含 40 组合）
dotnet run --project Benchmark/Benchmark.csproj -c Release -- --filter "*NetEchoBenchmark*"

# 独立压测：吞吐 / 延迟 / UDP
dotnet run --project Benchmark/NetLoadTest -c Release -- --mode pipeline  --clients 8 --size 1024 --seconds 10
dotnet run --project Benchmark/NetLoadTest -c Release -- --mode roundtrip --clients 4 --size 1024 --seconds 10
dotnet run --project Benchmark/NetLoadTest -c Release -- --udp --mode roundtrip --clients 1 --size 256 --seconds 5

# 分离进程单向压测（服务端独立进程，消除 CPU 共享）
Benchmark/NetLoadTest/bin/Release/net10.0/NetLoadTest.exe --server --port 7789 --oneway
Benchmark/NetLoadTest/bin/Release/net10.0/NetLoadTest.exe --remote 127.0.0.1:7789 --clients 8 --size 65536 --seconds 10 --warmup 2 --oneway

# 粘包口径（24B 逻辑帧，对标历史 1.4 亿 pkt/s）
Benchmark/NetLoadTest/bin/Release/net10.0/NetLoadTest.exe --remote 127.0.0.1:7789 --clients 8 --size 65536 --seconds 10 --oneway --frame 24
```
