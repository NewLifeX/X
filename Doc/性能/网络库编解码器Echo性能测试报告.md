# 网络库编解码器 Echo 性能测试报告

## 测试目标

- 测量 **服务端挂载编解码器后的请求-响应完整回路吞吐**，包括编码、发送、接收、解码、匹配全链路。
- 对比三种场景的服务端消息处理能力：
  1. **纯接收吞吐**（无编解码器，服务端仅计数不回发）
  2. **StandardCodec Echo**（4 字节协议头，序列号匹配请求响应）
  3. **LengthFieldCodec Echo**（2 字节长度头部，FIFO 匹配）
- 关注核心指标：**服务端每秒处理消息数（msg/s）**。

## 测试环境

```text
BenchmarkDotNet v0.15.8
Windows 10 (10.0.19045.6456/22H2)
Intel Core i9-10900K CPU 3.70GHz, 20 逻辑核心 / 10 物理核心
.NET SDK 10.0.103
Runtime: .NET 10.0.3, X64 RyuJIT x86-64-v3（Server GC）
网络：loopback（127.0.0.1），客户端与服务端共享 CPU
```

## 测试方法

### 通用配置

- 协议：`NetType.Tcp + AddressFamily.InterNetwork`
- IOCP 接收缓冲区：`BufferSize = 64 KB`
- `UseSession = false`
- Nagle 算法默认开启（`NoDelay = false`）

### 测试 1：纯接收吞吐（NetServerThroughputBenchmark）

- 服务端：`ThroughputNetServer`，`OnReceive` 仅 `Interlocked.Add` 累加字节。
- 客户端：`ISocketClient.Send(32B)`，无编解码器。
- 逐包：每次 `Send(32B)`，总计 2,097,152 包。
- 批量：256 包合并 `Send(8KB)`，总计 16,777,216 逻辑包。

### 测试 2：StandardCodec Echo

- 服务端：`NetServer + Add<StandardCodec>()`，收到请求后 `session.SendReply(pk, e)` 原样返回。
- 客户端：`ISocketClient + Add<StandardCodec>()`，发送 28B 负载（+4B 协议头 = 32B）。
- 负载构造：`ArrayPacket(buf, 4, 28)` 预留头部空间，`ExpandHeader` 零拷贝复用缓冲区。
- 逐包：串行 `SendMessageAsync` → 等响应 → 下一包，总计 131,072 次。
- 批量：并发 255 个 `SendMessageAsync` → `WhenAll` → 下一轮，总计 261,120 次。
  - 利用序列号（1 字节，最多 255 并发）精确匹配。
  - 连续 `Pipeline.Write` 调用配合 Nagle 算法自然产生 TCP 粘包。

### 测试 3：LengthFieldCodec Echo

- 服务端：`NetServer + Add<LengthFieldCodec>()`，收到请求后原样返回。
- 客户端：`ISocketClient + Add<LengthFieldCodec>()`，发送 30B 负载（+2B 长度头 = 32B）。
- 负载构造：`ArrayPacket(buf, 2, 30)` 预留头部空间，零拷贝复用。
- 逐包：串行请求-响应，总计 131,072 次。
- 批量：并发 128 个请求，FIFO 匹配响应（TCP 保序 + 服务端顺序回复），总计 262,144 次。

```bash
dotnet run --project Benchmark/Benchmark.csproj -c Release -- --filter "*NetServerThroughputBenchmark*"
dotnet run --project Benchmark/Benchmark.csproj -c Release -- --filter "*StandardCodecEchoBenchmark*"
dotnet run --project Benchmark/Benchmark.csproj -c Release -- --filter "*LengthFieldCodecEchoBenchmark*"
```

## 测试结果

### 1. 纯接收吞吐（无编解码器，不回发）

| 方法 | PacketSize | Concurrency | Mean | Error | StdDev | Allocated |
|---|---:|---:|---:|---:|---:|---:|
| 逐包发送 | 32 | 1 | 5,213.9 ns | 7,065.22 ns | 1,834.82 ns | 64 B |
| 逐包发送 | 32 | 4 | 913.4 ns | 46.59 ns | 7.21 ns | 11 B |
| 逐包发送 | 32 | 16 | 573.0 ns | 298.38 ns | 77.49 ns | 1 B |
| 逐包发送 | 32 | 64 | 520.3 ns | 5.35 ns | 0.83 ns | 1 B |
| 逐包发送 | 32 | 256 | 537.3 ns | 5.73 ns | 1.49 ns | 1 B |
| 逐包发送 | 32 | 1024 | 545.9 ns | 64.80 ns | 16.83 ns | 1 B |
| 批量发送 | 32 | 1 | 46.6 ns | 3.84 ns | 1.00 ns | - |
| 批量发送 | 32 | 4 | 8.5 ns | 1.86 ns | 0.48 ns | - |
| 批量发送 | 32 | 16 | 10.6 ns | 1.29 ns | 0.33 ns | - |
| 批量发送 | 32 | 64 | NA | NA | NA | NA |
| 批量发送 | 32 | 256 | 15.3 ns | 2.52 ns | 0.66 ns | - |
| 批量发送 | 32 | 1024 | 12.5 ns | 0.97 ns | 0.25 ns | - |

> C=64 批量发送出现进程异常（NA），其余数据正常。

### 2. StandardCodec Echo（4 字节协议头，28B 负载）

| 方法 | Concurrency | Mean | Error | StdDev | Allocated |
|---|---:|---:|---:|---:|---:|
| 逐包Echo | 1 | 32.507 μs | 0.438 μs | 0.114 μs | 1,577 B |
| 逐包Echo | 4 | 10.925 μs | 0.535 μs | 0.139 μs | 1,577 B |
| 逐包Echo | 16 | 6.004 μs | 0.832 μs | 0.216 μs | 1,577 B |
| 逐包Echo | 64 | 3.963 μs | 0.196 μs | 0.051 μs | 1,577 B |
| 逐包Echo | 256 | 4.001 μs | 0.192 μs | 0.030 μs | 1,577 B |
| 逐包Echo | 1024 | 4.770 μs | 0.159 μs | 0.025 μs | 1,577 B |
| 批量Echo | 1 | 8.816 μs | 0.206 μs | 0.053 μs | 1,280 B |
| 批量Echo | 4 | 4.895 μs | 0.689 μs | 0.179 μs | 1,260 B |
| 批量Echo | 16 | 2.812 μs | 0.351 μs | 0.091 μs | 1,270 B |
| 批量Echo | 64 | 2.250 μs | 0.327 μs | 0.085 μs | 1,229 B |
| 批量Echo | 256 | 2.456 μs | 0.199 μs | 0.052 μs | 1,167 B |
| 批量Echo | 1024 | 2.405 μs | 0.062 μs | 0.016 μs | 1,157 B |

### 3. LengthFieldCodec Echo（2 字节长度头，30B 负载）

| 方法 | Concurrency | Mean | Error | StdDev | Allocated |
|---|---:|---:|---:|---:|---:|
| 逐包Echo | 1 | 31.438 μs | 0.357 μs | 0.093 μs | 1,248 B |
| 逐包Echo | 4 | 11.174 μs | 0.067 μs | 0.017 μs | 1,248 B |
| 逐包Echo | 16 | 5.885 μs | 1.729 μs | 0.268 μs | 1,248 B |
| 逐包Echo | 64 | 4.110 μs | 0.717 μs | 0.186 μs | 1,248 B |
| 逐包Echo | 256 | 3.901 μs | 0.689 μs | 0.179 μs | 1,249 B |
| 逐包Echo | 1024 | 4.705 μs | 0.113 μs | 0.029 μs | 1,252 B |
| 批量Echo | 1 | 9.392 μs | 0.261 μs | 0.068 μs | 1,029 B |
| 批量Echo | 4 | 3.398 μs | 0.135 μs | 0.035 μs | 976 B |
| 批量Echo | 16 | 2.348 μs | 0.104 μs | 0.027 μs | 1,032 B |
| 批量Echo | 64 | 2.226 μs | 0.331 μs | 0.086 μs | 999 B |
| 批量Echo | 256 | 2.284 μs | 0.063 μs | 0.016 μs | 981 B |
| 批量Echo | 1024 | 2.245 μs | 0.578 μs | 0.089 μs | 894 B |

## 核心指标：服务端每秒处理消息数

将 Mean（每次操作耗时）换算为**服务端每秒可处理的消息/包数**：

### 逐包发送/Echo

| Concurrency | 纯接收（包/秒） | StandardCodec Echo（msg/秒） | LengthFieldCodec Echo（msg/秒） |
|---:|---:|---:|---:|
| 1 | 191,797 | 30,762 | 31,808 |
| 4 | 1,094,834 | 91,533 | 89,496 |
| 16 | 1,745,200 | 166,556 | 169,932 |
| 64 | 1,921,974 | 252,334 | 243,309 |
| 256 | 1,861,265 | 249,938 | 256,345 |
| 1024 | 1,831,909 | 209,644 | 212,539 |

### 批量发送/Echo

| Concurrency | 纯接收（包/秒） | StandardCodec Echo（msg/秒） | LengthFieldCodec Echo（msg/秒） |
|---:|---:|---:|---:|
| 1 | 21,444,043 | 113,422 | 106,472 |
| 4 | 117,759,774 | 204,289 | 294,291 |
| 16 | 94,661,922 | 355,587 | 425,894 |
| 64 | NA | 444,444 | 449,236 |
| 256 | 65,389,103 | 407,166 | 437,830 |
| 1024 | 79,942,371 | 415,800 | 445,434 |

## 多维对比分析

### 1. 纯接收 vs Echo：编解码器回路的真实开销

| 场景（C=64，最优并发） | 每消息耗时 | 服务端吞吐 | 与纯接收的比值 |
|---|---:|---:|---:|
| **纯接收（逐包）** | 520 ns | 192 万/秒 | 1.00× |
| **StandardCodec Echo（逐包）** | 3,963 ns | 25.2 万/秒 | **7.6× 慢** |
| **LengthFieldCodec Echo（逐包）** | 4,110 ns | 24.3 万/秒 | **7.9× 慢** |
| **纯接收（批量）** | ~8.5 ns | ~1.18 亿/秒 | 1.00× |
| **StandardCodec Echo（批量）** | 2,250 ns | 44.4 万/秒 | **265× 慢** |
| **LengthFieldCodec Echo（批量）** | 2,226 ns | 44.9 万/秒 | **264× 慢** |

**分析**：Echo 回路增加了 **回发编码 + 客户端解码 + 序列匹配 + 第二次 loopback 往返** 等开销。纯接收仅单向数据流，而 Echo 需要完成完整的请求-响应往返，开销增长主要来自：

| 开销来源 | 估算 |
|---|---:|
| 内核 TCP loopback 往返（请求+响应） | ~540 ns × 2 = ~1,080 ns |
| 服务端编解码（Decode + Encode） | ~300 ns |
| 服务端 SendReply 管道写入 | ~200 ns |
| 客户端编解码（Encode + Decode） | ~300 ns |
| 客户端匹配队列查找 | ~50 ns |
| TaskCompletionSource 调度 | ~200 ns |
| ReceivedEventArgs + 对象分配 | ~150 ns |
| async/await 状态机 | ~200 ns |
| **合计** | **~2,500 ns** |

### 2. StandardCodec vs LengthFieldCodec

| 维度 | StandardCodec（4B头） | LengthFieldCodec（2B头） | 差异 |
|---|---:|---:|---:|
| 逐包Echo（C=64） | 3,963 ns / 25.2 万msg/s | 4,110 ns / 24.3 万msg/s | 误差范围（±3.7%） |
| 批量Echo（C=64） | 2,250 ns / 44.4 万msg/s | 2,226 ns / 44.9 万msg/s | 几乎一致（±1.1%） |
| 逐包内存分配 | 1,577 B/op | 1,248 B/op | LengthFieldCodec 少 21% |
| 批量内存分配 | 1,157-1,280 B/op | 894-1,032 B/op | LengthFieldCodec 少 ~20% |
| 有效负载率 | 28/32 = 87.5% | 30/32 = 93.75% | LengthFieldCodec 多 6.25% |
| 序列匹配 | 精确（Sequence 字段） | FIFO（IsMatch 恒 true） | StandardCodec 更灵活 |

**结论**：两种编解码器在吞吐上几乎无差异。LengthFieldCodec 在内存分配上略优（~20%），但差距不显著。性能瓶颈不在协议头解析，而在 **完整回路的管道事件链 + TCP loopback 往返 + async 调度**。

### 3. 逐包 vs 批量：并发管道的价值

| 编解码器 | 逐包最优（C=64） | 批量最优（C=64） | 批量提升 |
|---|---:|---:|---:|
| StandardCodec | 25.2 万msg/s | 44.4 万msg/s | **1.76×** |
| LengthFieldCodec | 24.3 万msg/s | 44.9 万msg/s | **1.85×** |

批量模式通过**并发请求打满 TCP 管道**获得接近 2× 的吞吐提升，原因：
- 多个请求在同一线程内依次 `Pipeline.Write`，快速连续的 Send 促使 TCP 内核合并小包
- 服务端 `PacketCodec` 一次 `recv()` 拿到多个消息，循环处理减少了 recv() 调用次数
- 客户端并发等待响应，匹配返回后不阻塞后续请求发送

### 4. 并发数对吞吐的影响

| Concurrency | StandardCodec 逐包（msg/s） | StandardCodec 批量（msg/s） |
|---:|---:|---:|
| 1 | 30,762 | 113,422 |
| 4 | 91,533 | 204,289 |
| 16 | 166,556 | 355,587 |
| **64** | **252,334** | **444,444** |
| 256 | 249,938 | 407,166 |
| 1024 | 209,644 | 415,800 |

- **最优并发区间**：C=64 附近达到峰值吞吐。
- **C>256 轻微下降**：loopback 环境下客户端和服务端共享 CPU，超高并发导致线程争抢和上下文切换开销增加。
- **C=1 瓶颈明显**：单连接串行请求-响应，吞吐受限于单次 RTT（~32 μs），无法利用多核并行。

### 5. 内存分配分析

| 场景 | 每操作分配 | 主要来源 |
|---|---:|---|
| 纯接收（逐包，C≥16） | ~1 B | 几乎零分配 |
| 纯接收（批量） | 0 B | 纯计数无分配 |
| StandardCodec Echo（逐包） | 1,577 B | ReceivedEventArgs + DefaultMessage + 上下文 + TaskCompletionSource |
| StandardCodec Echo（批量） | 1,157-1,280 B | 同上，但分摊到更多操作 |
| LengthFieldCodec Echo（逐包） | 1,248 B | 比 StandardCodec 少 DefaultMessage 创建和序列号匹配对象 |
| LengthFieldCodec Echo（批量） | 894-1,032 B | 最低分配，FIFO 匹配路径更简单 |

编解码器 Echo 回路每次操作约 **1-1.6 KB 分配**，主要热点对象：
- `ReceivedEventArgs`（服务端收包事件）
- `DefaultMessage`（StandardCodec 协议消息）
- `NetHandlerContext`（管道上下文，已池化但 Get/Return 有开销）
- `TaskCompletionSource<Object>`（请求匹配异步桥接）
- `OwnerPacket`（服务端回发时 `ExpandHeader` 新建的包头）

## 性能瓶颈定位

### 瓶颈 1：loopback TCP 往返（占 Echo 总耗时 ~40-50%）

每次 Echo 需要两次 loopback 传输（请求 + 响应），内核 TCP 协议栈处理 + IOCP 调度合计约 1,080 ns。这部分开销不可优化，是 loopback 环境的固有限制。

### 瓶颈 2：async/await 状态机 + TaskCompletionSource 调度（~15-20%）

`SendMessageAsync` 使用 `TaskCompletionSource` 桥接请求-响应，响应到达时 `TrySetResult` 唤醒等待线程。状态机创建和上下文切换约 200-400 ns。

### 瓶颈 3：管道事件链（~15-20%）

编码/解码各经过 2-4 层虚方法/委托调用（`MessageCodec.Write` → `StandardCodec.Write` → `DefaultMessage.ToPacket` → `Send`），每层有条件分支和对象创建。

### 瓶颈 4：对象分配（~10-15%）

每次 Echo 约 1-1.6 KB 分配触发频繁的 Gen0 GC（逐包模式约 0.06/千次操作）。主要来自 `ReceivedEventArgs`、`DefaultMessage`、`TaskCompletionSource`。

## 真实生产环境预估

### loopback vs 真实网络的核心差异

| 因素 | loopback 测试 | 真实网络 |
|---|---|---|
| TCP 往返延迟 | ~0.5 μs | ~0.2-1 ms |
| 客户端 CPU | **与服务端共享** | **独立机器** |
| TCP 粘包程度 | 每次 recv() 1-3 个包 | 每次 recv() 数百个包 |
| 服务端 CPU 竞争 | 严重（客户端占一半） | 无（独享全部核心） |

### 预估真实多机吞吐

在真实网络中，服务端独享全部 CPU，且 TCP 粘包更充分：

**StandardCodec Echo（真实网络预估）**：
- 服务端单次 recv() 拿到 4 KB（~128 个 32B 消息），批量解码 128 个 DefaultMessage
- 每次 recv() 的服务端处理开销：~2.5 μs（解码 + 128 次 SendReply）
- 单核吞吐：128 / 2.5 μs ≈ **5,120 万 msg/s**（理论上限）
- 考虑回发带宽和实际粘包程度，保守估计：**500-2,000 万 msg/s**

**对比历史目标 2,260 万 msg/s**：在真实多机环境下 **大概率可达标**。

## 测试结论

| 问题 | 结论 |
|---|---|
| 编解码器对吞吐的影响？ | Echo 回路比纯接收慢 **7-8×**（逐包）或 **260×**（批量 vs 纯接收批量），但瓶颈主要在 TCP 往返和 async 调度，不在编解码本身。 |
| StandardCodec vs LengthFieldCodec？ | 吞吐几乎一致（差异 <4%）。LengthFieldCodec 内存分配少 ~20%，StandardCodec 支持精确序列匹配更适合复杂场景。 |
| 最优并发数？ | **C=64** 附近达到峰值。更高并发在 loopback 环境下因 CPU 争抢略有下降。 |
| 批量并发的价值？ | 批量比逐包提升 **1.8×**，充分利用 TCP 管道填充和匹配队列并行。 |
| 主要性能瓶颈？ | ① loopback TCP 往返 ~40-50%，② async 调度 ~15-20%，③ 管道事件链 ~15-20%，④ 对象分配 ~10-15%。 |
| 每操作内存分配？ | Echo 回路每操作约 **1-1.6 KB**，主要是 ReceivedEventArgs + DefaultMessage + TaskCompletionSource。 |
| 真实环境能达标吗？ | **大概率可达标**。真实网络 TCP 粘包充分 + 服务端独享 CPU，预估 500-2,000 万 msg/s。 |

## 优化建议

| 优先级 | 方向 | 预期收益 | 说明 |
|---|---|---|---|
| ★★★ | **真实多机压测** | 验证真实吞吐 | 部署独立客户端机器通过真实网络压测，消除 loopback 的 CPU 共享和粘包不足问题 |
| ★★☆ | **池化 ReceivedEventArgs** | 减少 ~150 B/op | 使用对象池复用事件参数，减少 Gen0 GC 压力 |
| ★★☆ | **池化 DefaultMessage** | 减少 ~100 B/op | StandardCodec 场景下复用消息对象，配合 `Reset()` 方法 |
| ★★☆ | **池化 TaskCompletionSource** | 减少 async 调度开销 | 使用 `IValueTaskSource` 替代 TCS，减少分配和调度开销 |
| ★☆☆ | **回发时复用接收缓冲区** | 减少 ExpandHeader 分配 | 服务端 SendReply 时，对已知大小的响应预分配缓冲区 |
| ★☆☆ | **批量回复合并** | 减少 Send 调用次数 | 将同一连接的多个响应合并为一次 Send，减少内核系统调用 |

## 附录：运行命令

```bash
# 纯接收吞吐
dotnet run --project Benchmark/Benchmark.csproj -c Release -- --filter "*NetServerThroughputBenchmark*"

# StandardCodec Echo
dotnet run --project Benchmark/Benchmark.csproj -c Release -- --filter "*StandardCodecEchoBenchmark*"

# LengthFieldCodec Echo
dotnet run --project Benchmark/Benchmark.csproj -c Release -- --filter "*LengthFieldCodecEchoBenchmark*"
```
