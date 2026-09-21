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

低并发下的基础链路成本：**最小往返 23.8 µs（TCP）/ 26.4 µs（1KB）**，即单次 socket 往返的物理底线约 **25µs**（回环 + 两次 IOCP 调度）。

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

## 复现命令

```bash
# BDN 基准（约 10 分钟，含 40 组合）
dotnet run --project Benchmark/Benchmark.csproj -c Release -- --filter "*NetEchoBenchmark*"

# 独立压测：吞吐 / 延迟 / UDP
dotnet run --project Benchmark/NetLoadTest -c Release -- --mode pipeline  --clients 8 --size 1024 --seconds 10
dotnet run --project Benchmark/NetLoadTest -c Release -- --mode roundtrip --clients 4 --size 1024 --seconds 10
dotnet run --project Benchmark/NetLoadTest -c Release -- --udp --mode roundtrip --clients 1 --size 256 --seconds 5
```
