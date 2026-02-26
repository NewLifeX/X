# 网络库服务端接收吞吐量性能测试报告

## 测试目标

- 在 Benchmark 项目中以 `NetServerThroughputBenchmark` 测量**服务端纯接收吞吐**，服务端仅计数不回发，隔离回发开销对测量的干扰。
- 服务端统计每次迭代收到的总字节数，折算为每秒处理的 32 字节逻辑消息包数。
- 增加并发参数 `Concurrency = 1 / 100 / 1000 / 10000`，寻找最优并发窗口。
- 历史目标：`22,600,000 包/秒`。

## 测试环境

```text
BenchmarkDotNet v0.15.8
Windows 10 (10.0.19045.6456/22H2)
Intel Core i9-10900K CPU 3.70GHz, 20 逻辑核心 / 10 物理核心
.NET SDK 10.0.103
Runtime: .NET 10.0.3, X64 RyuJIT x86-64-v3（Workstation GC）
```

## 测试方法

- 服务端：`ThroughputNetServer`（继承 `NetServer`），重写 `OnReceive` 仅累加 `e.Packet.Total` 字节数，**不回发**，用 `ManualResetEventSlim` 在达到期望字节数时发出完成信号。
- 客户端：`new NetUri("tcp://127.0.0.1:7779").CreateRemote()`，`GlobalSetup` 中全部建立连接并保持，每次迭代将 `TotalPackets = 200,000` 个 32 B 包均分给所有并发客户端同时发送（`Task.Run`）。
- 服务端协议限定为 `NetType.Tcp + AddressFamily.InterNetwork`，排除 UDP / IPv6 干扰。
- 指标：`OperationsPerInvoke = 200,000`，BenchmarkDotNet 汇报的 `Mean` 为**每逻辑包耗时（ns）**。
- 命令：

```bash
dotnet run --project Benchmark/Benchmark.csproj -c Release -- --filter "*NetServerThroughputBenchmark*"
```

## 测试结果

| 方法 | PacketSize | Concurrency | Mean | Error | StdDev | Allocated |
|---|---:|---:|---:|---:|---:|---:|
| 服务端接收吞吐 | 32 | 1 | 7,473.6 ns | 30.38 ns | 7.89 ns | 101 B |
| 服务端接收吞吐 | 32 | 100 | 535.4 ns | 21.50 ns | 3.33 ns | 1 B |
| 服务端接收吞吐 | 32 | 1,000 | 554.4 ns | 19.56 ns | 3.03 ns | 4 B |
| 服务端接收吞吐 | 32 | 10,000 | 628.1 ns | 10.40 ns | 2.70 ns | 21 B |

`Mean` = 单逻辑包（32 B）均摊耗时；`Allocated` = 每包托管内存分配量。

换算吞吐（包/秒 = 1,000,000,000 / Mean）：

| Concurrency | Mean | 吞吐（包/秒） | 接收带宽（MB/s） | vs 目标 22,600,000 |
|---:|---:|---:|---:|---:|
| 1 | 7,473.6 ns | ~133,807 | ~4.1 | 0.59% |
| 100 | 535.4 ns | ~1,868,000 | ~57.1 | **8.27%** |
| 1,000 | 554.4 ns | ~1,804,000 | ~55.2 | 7.98% |
| 10,000 | 628.1 ns | ~1,592,000 | ~48.7 | 7.04% |

- 最优并发为 **Concurrency=100**，约 **186.8 万包/秒**，约为目标的 **8.27%**。
- 对比旧 Echo 回环测试（Concurrency=1000，577.9 ns，约 173 万包/秒），去掉回发后最优点前移至 100，吞吐提升约 **7.8%**，但未出现量级改变。

## 瓶颈分析

### 1. TCP 粘包效应明显，但快速触达上限

Concurrency=1 → 100 吞吐提升约 **14 倍**，是 TCP 粘包（多路并发写入被 OS 合并为更大的 `recv()` 缓冲）带来的最大收益。继续增加并发至 1,000 / 10,000 收益递减乃至下降，说明其他因素已成为瓶颈。

### 2. 客户端发送路径含 APM Span 虚调用

`NetSession.Send(ReadOnlySpan<Byte>)` 内部调用 `host.Tracer?.NewSpan()`；即使 `Tracer` 为 `null`，仍存在空值检查及接口虚调度开销。在 Concurrency=100 时每 Task 发送 2,000 次，这部分是热点。

### 3. 服务端接收路径的 Tracer Span 固定开销

服务端每次接收数据都会经过 `NetSession.Ss_Received`：

```csharp
using var span = tracer?.NewSpan($"net:{host?.Name}:Receive", ...);
```

即使 Tracer 为 null，空值检查与接口虚调用仍在每次接收中发生，且伴随 `using var` 的 dispose 路径，在高频接收下构成不可忽视的固定开销。

### 4. `OnReceive` 中重复原子读 `_expectedBytes`

当前写法每次 `OnReceive` 都执行一次 `Interlocked.Read(ref _expectedBytes)`，而该值在迭代内部从不改变，造成不必要的内存屏障。

### 5. 高并发下任务调度占据比例增大

Concurrency=10,000 时每 Task 仅发送 20 个包，`Task.Run` 的调度开销（队列、线程唤醒、上下文切换）与实际发送工作之比很高，这是 10,000 并发反而比 1,000 慢的主因。

### 6. 内存分配随并发升高

Concurrency=10,000 时 Allocated=21 B/包（Concurrency=100 时仅 1 B/包），高并发下 GC 竞争和额外分配进一步拖慢吞吐。

## 改进建议

| 优先级 | 方向 | 具体措施 |
|:---:|---|---|
| ★★★ | **去除热路径 APM 开销** | 在 `Tracer == null`（默认值）时完全跳过 `NewSpan`，避免接口虚调用与 `using var` dispose；或在 Benchmark 中明确禁用 Tracer |
| ★★★ | **缓存不变量避免重复原子读** | `OnReceive` 首行读一次 `_expectedBytes` 到栈变量，迭代内部不再重复 `Interlocked.Read` |
| ★★ | **客户端批量写入** | 将多个 32 B 包合并为一次 `Send`（如一次写 1 KB = 32 包），显著减少 send syscall 次数与服务端 `OnReceive` 调用频率，最大化 TCP 粘包收益 |
| ★★ | **细化最优并发窗口** | 在 64 / 128 / 256 / 512 之间精确测试，定位 TCP 粘包收益与调度开销的拐点 |
| ★★ | **限制高并发下发送任务数** | Concurrency > 1,000 时改用固定大小工作线程池轮询发送，避免 10,000 Task 的调度风暴 |
| ★ | **关闭 UseSession** | 高吞吐场景不需要群发时，设置 `UseSession = false` 减少 `ConcurrentDictionary` 维护开销 |
| ★ | **拆分测试维度** | 分别测试"单向发送极限"与"单向接收极限"，确认瓶颈在客户端发送路径还是服务端接收路径 |

## 结论

本次去掉回发后，服务端纯接收最优吞吐约 **186.8 万包/秒**（Concurrency=100），接收带宽约 **57 MB/s**，相比旧 Echo 回环测试（173 万包/秒）提升约 **7.8%**，但离目标 **2,260 万包/秒**（~723 MB/s）仍有约 **12 倍差距**。

差距主要来自三处：
1. **APM Span 虚调用**：每次收发路径中均有 `tracer?.NewSpan()` 的空调用开销，即使 Tracer 未配置也无法消除；
2. **TCP 粘包利用不足**：客户端逐包调用 `Send`，限制了 OS 层的写合并效果，实际送达服务端的缓冲区远小于最优值；
3. **高并发任务调度**：超过 1,000 并发后线程池调度开销超过粘包收益。

后续应优先在**热路径彻底消除 Tracer 调用**和**客户端批量写入**两个方向上突破，再结合最优并发窗口细分测试，有望大幅缩小与目标值的差距。
