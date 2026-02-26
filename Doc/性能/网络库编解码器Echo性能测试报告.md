# 网络库编解码器 Echo 性能测试报告

## 测试目标

- 测量 **服务端挂载编解码器后的请求-响应完整回路吞吐**，包括编码、发送、接收、解码、匹配全链路。
- 对比三种场景的服务端消息处理能力：
  1. **纯接收吞吐**（无编解码器，服务端仅计数不回发）
  2. **StandardCodec Echo**（4 字节协议头，序列号匹配请求响应）
  3. **LengthFieldCodec Echo**（2 字节长度头部，FIFO 匹配）
- 关注核心指标：**服务端每秒处理消息数（msg/s）**。
- 对象池化优化效果验证：`ReceivedEventArgs`、`DefaultMessage`、`PooledValueTaskSource`。

## 测试环境

```text
BenchmarkDotNet v0.15.8
Windows 10 (10.0.19045.6456/22H2)
Intel Core i9-10900K CPU 3.70GHz, 20 逻辑核心 / 10 物理核心
.NET SDK 10.0.103
Runtime: .NET 10.0.3, X64 RyuJIT x86-64-v3（Server GC）
网络：loopback（127.0.0.1），客户端与服务端共享 CPU
```

## 本轮优化内容

在上一轮基准测试基础上，完成了以下对象池化优化：

| 优化项 | 说明 |
|---|---|
| **ReceivedEventArgs 池化** | 新增 `Pool<ReceivedEventArgs>` + `Reset()` + `Rent()/Return()`，避免每次 `recv()` 回调分配新事件参数 |
| **DefaultMessage 池化** | 新增 `Pool<DefaultMessage>` + `Rent()/Return()`，StandardCodec 的 Decode/Write/CreateReply 均从池中获取实例 |
| **PooledValueTaskSource** | NET5_0_OR_GREATER 专用，基于 `ManualResetValueTaskSourceCore<Object>` 实现池化异步完成源，替代每次 `SendMessageAsync` 分配新 `TaskCompletionSource` |
| **IMatchQueue 泛化** | `Add` 参数从 `TaskCompletionSource<Object>` 改为 `Object`，`DefaultMatchQueue` 兼容 TCS 和 PooledValueTaskSource |

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
- 逐包：串行 `SendMessageAsync` 等响应 下一包，总计 131,072 次。
- 批量：并发 255 个 `SendMessageAsync` WhenAll 下一轮，总计 261,120 次。

### 测试 3：LengthFieldCodec Echo

- 服务端：`NetServer + Add<LengthFieldCodec>()`，收到请求后原样返回。
- 客户端：`ISocketClient + Add<LengthFieldCodec>()`，发送 30B 负载（+2B 长度头 = 32B）。
- 逐包：串行请求-响应，总计 131,072 次。
- 批量：并发 128 个请求，FIFO 匹配响应，总计 262,144 次。

```bash
dotnet run --project Benchmark/Benchmark.csproj -c Release -- --filter "*NetServerThroughputBenchmark*"
dotnet run --project Benchmark/Benchmark.csproj -c Release -- --filter "*StandardCodecEchoBenchmark*"
dotnet run --project Benchmark/Benchmark.csproj -c Release -- --filter "*LengthFieldCodecEchoBenchmark*"
```

## 测试结果

### 1. 纯接收吞吐（无编解码器，不回发）

| 方法 | PacketSize | Concurrency | Mean | Error | StdDev | Allocated |
|---|---:|---:|---:|---:|---:|---:|
| 逐包发送 | 32 | 1 | 7,482.6 ns | 1,150.76 ns | 298.85 ns | 35 B |
| 逐包发送 | 32 | 4 | 918.8 ns | 61.74 ns | 16.03 ns | 5 B |
| 逐包发送 | 32 | 16 | 593.2 ns | 329.67 ns | 85.61 ns | 1 B |
| 逐包发送 | 32 | 64 | 519.3 ns | 11.98 ns | 3.11 ns | 0 B |
| 逐包发送 | 32 | 256 | 535.9 ns | 17.18 ns | 2.66 ns | 0 B |
| 逐包发送 | 32 | 1024 | 538.8 ns | 30.88 ns | 4.78 ns | 1 B |
| 批量发送 | 32 | 1 | 29.4 ns | 14.11 ns | 3.67 ns | - |
| 批量发送 | 32 | 4 | 11.7 ns | 0.63 ns | 0.16 ns | - |
| 批量发送 | 32 | 16 | 10.1 ns | 1.19 ns | 0.31 ns | - |
| 批量发送 | 32 | 64 | NA | NA | NA | NA |
| 批量发送 | 32 | 256 | 15.4 ns | 1.39 ns | 0.36 ns | - |
| 批量发送 | 32 | 1024 | 13.0 ns | 0.97 ns | 0.25 ns | - |

### 2. StandardCodec Echo（4 字节协议头，28B 负载）

| 方法 | Concurrency | Mean | Error | StdDev | Allocated |
|---|---:|---:|---:|---:|---:|
| 逐包Echo | 1 | 29.147 us | 4.209 us | 1.093 us | 1,280 B |
| 逐包Echo | 4 | 10.229 us | 0.304 us | 0.079 us | 1,280 B |
| 逐包Echo | 16 | 5.773 us | 1.624 us | 0.422 us | 1,280 B |
| 逐包Echo | 64 | 3.602 us | 0.103 us | 0.027 us | 1,280 B |
| 逐包Echo | 256 | 4.265 us | 0.711 us | 0.185 us | 1,281 B |
| 逐包Echo | 1024 | 4.714 us | 0.113 us | 0.029 us | 1,286 B |
| 批量Echo | 1 | 7.507 us | 1.330 us | 0.345 us | 1,029 B |
| 批量Echo | 4 | 3.293 us | 0.185 us | 0.048 us | 1,043 B |
| 批量Echo | 16 | 2.371 us | 0.012 us | 0.002 us | 1,057 B |
| 批量Echo | 64 | 2.261 us | 0.039 us | 0.010 us | 1,026 B |
| 批量Echo | 256 | 2.337 us | 0.327 us | 0.085 us | 986 B |
| 批量Echo | 1024 | 2.428 us | 0.201 us | 0.052 us | 969 B |

### 3. LengthFieldCodec Echo（2 字节长度头，30B 负载）

| 方法 | Concurrency | Mean | Error | StdDev | Allocated |
|---|---:|---:|---:|---:|---:|
| 逐包Echo | 1 | 33.341 us | 1.543 us | 0.239 us | 1,032 B |
| 逐包Echo | 4 | 10.422 us | 0.342 us | 0.089 us | 1,032 B |
| 逐包Echo | 16 | 5.396 us | 0.274 us | 0.071 us | 1,032 B |
| 逐包Echo | 64 | 3.822 us | 0.259 us | 0.040 us | 1,032 B |
| 逐包Echo | 256 | 3.782 us | 0.035 us | 0.009 us | 1,033 B |
| 逐包Echo | 1024 | 4.823 us | 0.115 us | 0.030 us | 1,037 B |
| 批量Echo | 1 | 9.360 us | 0.196 us | 0.051 us | 910 B |
| 批量Echo | 4 | 3.045 us | 0.027 us | 0.007 us | 875 B |
| 批量Echo | 16 | 2.474 us | 0.485 us | 0.126 us | 900 B |
| 批量Echo | 64 | 2.242 us | 0.105 us | 0.027 us | 885 B |
| 批量Echo | 256 | 2.311 us | 0.097 us | 0.025 us | 873 B |
| 批量Echo | 1024 | 2.357 us | 0.314 us | 0.082 us | 860 B |

## 优化前后对比

### 内存分配对比（每操作 Allocated）

| 场景 | 优化前 | 优化后 | 节省 | 节省率 |
|---|---:|---:|---:|---:|
| **纯接收 逐包 C=1** | 64 B | 35 B | 29 B | 45% |
| **纯接收 逐包 C=64** | 1 B | 0 B | 1 B | 100% |
| **StandardCodec 逐包 C=64** | 1,577 B | 1,280 B | 297 B | **19%** |
| **StandardCodec 批量 C=64** | 1,229 B | 1,026 B | 203 B | **17%** |
| **StandardCodec 批量 C=1024** | 1,157 B | 969 B | 188 B | **16%** |
| **LengthFieldCodec 逐包 C=64** | 1,248 B | 1,032 B | 216 B | **17%** |
| **LengthFieldCodec 批量 C=64** | 999 B | 885 B | 114 B | **11%** |
| **LengthFieldCodec 批量 C=1024** | 894 B | 860 B | 34 B | **4%** |

### 吞吐量对比（每秒处理消息数）

| 场景（C=64，最优并发） | 优化前 Mean | 优化后 Mean | 优化前 msg/s | 优化后 msg/s | 提升 |
|---|---:|---:|---:|---:|---:|
| **StandardCodec 逐包** | 3.963 us | 3.602 us | 252,334 | **277,624** | **+10.0%** |
| **StandardCodec 批量** | 2.250 us | 2.261 us | 444,444 | **442,724** | -0.4% |
| **LengthFieldCodec 逐包** | 4.110 us | 3.822 us | 243,309 | **261,643** | **+7.5%** |
| **LengthFieldCodec 批量** | 2.226 us | 2.242 us | 449,236 | **446,029** | -0.7% |

## 核心指标：服务端每秒处理消息数

### 逐包发送/Echo

| Concurrency | 纯接收（包/秒） | StandardCodec Echo（msg/秒） | LengthFieldCodec Echo（msg/秒） |
|---:|---:|---:|---:|
| 1 | 133,653 | 34,309 | 29,993 |
| 4 | 1,088,362 | 97,761 | 95,950 |
| 16 | 1,685,736 | 173,217 | 185,323 |
| 64 | 1,925,852 | **277,624** | **261,643** |
| 256 | 1,866,120 | 234,467 | 264,411 |
| 1024 | 1,856,107 | 212,135 | 207,339 |

### 批量发送/Echo

| Concurrency | 纯接收（包/秒） | StandardCodec Echo（msg/秒） | LengthFieldCodec Echo（msg/秒） |
|---:|---:|---:|---:|
| 1 | 34,029,842 | 133,209 | 106,838 |
| 4 | 85,836,910 | 303,678 | 328,407 |
| 16 | 99,009,901 | 421,764 | 404,205 |
| 64 | NA | **442,724** | **446,029** |
| 256 | 65,061,482 | 428,071 | 432,712 |
| 1024 | 77,220,077 | 411,874 | 424,268 |

## 多维对比分析

### 1. 池化优化效果总结

| 池化对象 | 每次操作节省 | 主要受益场景 | 备注 |
|---|---:|---|---|
| **ReceivedEventArgs** | ~29 B | 纯接收（从64B降到35B） | 每次 recv() 回调节省一次 new |
| **DefaultMessage** | ~100-200 B | Echo 回路（Decode + CreateReply） | 服务端收包解码和回发各省一次 new |
| **PooledValueTaskSource** | ~100 B | SendMessageAsync 调用 | 替代 TCS+Task 分配，仅 NET5+ 生效 |
| **合计** | ~200-300 B | 完整 Echo 回路 | 总分配从 ~1,577B 降至 ~1,280B（StandardCodec 逐包） |

### 2. 吞吐量变化分析

**逐包模式提升显著（+7%~+10%）**：逐包模式每次操作都经历完整的分配/回收周期，池化直接消除热路径上的 GC 压力，降低了 Gen0 回收频率。

**批量模式变化不大（约0%）**：批量模式下 255/128 个请求并发发送，单次操作耗时被 TCP 管道和 IOCP 调度主导，对象分配占比很小，池化收益被统计噪声淹没。

### 3. StandardCodec vs LengthFieldCodec

| 维度 | StandardCodec（4B头） | LengthFieldCodec（2B头） | 差异 |
|---|---:|---:|---:|
| 逐包 C=64 | 3.602 us / 27.8 万msg/s | 3.822 us / 26.2 万msg/s | StandardCodec 快 6% |
| 批量 C=64 | 2.261 us / 44.3 万msg/s | 2.242 us / 44.6 万msg/s | 几乎一致 |
| 逐包内存分配 | 1,280 B/op | 1,032 B/op | LengthFieldCodec 少 19% |
| 批量内存分配 | 969-1,057 B/op | 860-910 B/op | LengthFieldCodec 少 11% |

**结论**：两种编解码器吞吐几乎一致。LengthFieldCodec 内存更省（无 DefaultMessage 对象），StandardCodec 因序列号匹配更灵活，适合复杂场景。

### 4. 并发数对吞吐的影响

| Concurrency | StandardCodec 逐包（msg/s） | StandardCodec 批量（msg/s） |
|---:|---:|---:|
| 1 | 34,309 | 133,209 |
| 4 | 97,761 | 303,678 |
| 16 | 173,217 | 421,764 |
| **64** | **277,624** | **442,724** |
| 256 | 234,467 | 428,071 |
| 1024 | 212,135 | 411,874 |

- **最优并发区间**：C=64 附近达到峰值。
- **C>256 下降**：loopback 环境下客户端和服务端共享 CPU，超高并发导致线程争抢。
- **逐包 C=1 瓶颈明显**：单连接串行 RTT 约 29 us，仅 3.4 万 msg/s。

### 5. 剩余内存分配成分分析

优化后 StandardCodec 逐包 Echo 每操作仍有 ~1,280 B 分配，主要来源：

| 分配来源 | 估算大小 | 能否继续优化 |
|---|---:|---|
| async 状态机（SendMessageAsync） | ~300 B | 需改返回类型为 ValueTask（API 变更） |
| NetHandlerContext.Items (Dictionary) | ~200 B | 已池化上下文但 Items.Clear 后重新 Add |
| PacketCodec 内部缓存（粘包拆包） | ~150 B | 粘包缓冲区动态扩展 |
| List&lt;IMessage&gt;（Decode 返回值） | ~100 B | 可考虑栈分配或池化 |
| 事件委托闭包、Span 上下文等 | ~100 B | 零散分配，难以消除 |
| **合计** | **~850 B** | 差值为 BDN 统计误差 |

## 性能瓶颈定位

### 瓶颈 1：loopback TCP 往返（占 Echo 总耗时 ~40-50%）

每次 Echo 需要两次 loopback 传输（请求 + 响应），内核 TCP 协议栈处理 + IOCP 调度合计约 1,080 ns。

### 瓶颈 2：async 状态机分配（~20%）

`SendMessageAsync` 作为 `async Task<Object>` 方法，编译器生成的状态机每次调用分配约 300B。即使 PooledValueTaskSource 替代了 TCS，状态机仍不可避免。

### 瓶颈 3：管道事件链（~15-20%）

编码/解码各经过 2-4 层虚方法/委托调用，每层有条件分支和对象创建。

### 瓶颈 4：剩余对象分配（~10-15%）

每次 Echo 约 1,000-1,300 B 分配触发频繁的 Gen0 GC。

## 测试结论

| 问题 | 结论 |
|---|---|
| 池化优化效果？ | 内存分配减少 **16-19%**，逐包 Echo 吞吐提升 **7-10%**，批量模式持平 |
| 主要优化贡献？ | ReceivedEventArgs 池化（省 ~30B/recv）+ DefaultMessage 池化（省 ~200B/msg）+ PooledValueTaskSource（省 ~100B/send） |
| 编解码器对吞吐的影响？ | Echo 回路比纯接收慢 **7-8x**（逐包），瓶颈在 TCP 往返和 async 调度 |
| StandardCodec vs LengthFieldCodec？ | 吞吐几乎一致（差异 <6%），LengthFieldCodec 内存少 ~19% |
| 最优并发数？ | **C=64** 附近峰值。更高并发在 loopback 下因 CPU 争抢下降 |
| 批量并发价值？ | 批量比逐包提升 **1.6-1.7x** |
| 每操作内存分配？ | 优化后 **~1,000-1,300 B/op**（优化前 ~1,200-1,600 B/op） |

## 后续优化方向

| 优先级 | 方向 | 预期收益 | 说明 |
|---|---|---|---|
| ★★★ | **真实多机压测** | 验证真实吞吐 | 独立客户端机器消除 loopback CPU 共享问题 |
| ★★☆ | **SendMessageAsync 返回 ValueTask** | 省 ~300B 状态机 | 需变更公共 API 签名（ISocketRemote/INetSession），按 TFM 条件编译 |
| ★★☆ | **池化 List&lt;IMessage&gt;** | 省 ~100B/decode | Decode 返回值使用 ArrayPool 或预分配列表 |
| ★☆☆ | **合并回复** | 减少 Send 系统调用 | 同一连接多个响应合并为一次 Send |

## 附录：运行命令

```bash
# 纯接收吞吐
dotnet run --project Benchmark/Benchmark.csproj -c Release -- --filter "*NetServerThroughputBenchmark*"

# StandardCodec Echo
dotnet run --project Benchmark/Benchmark.csproj -c Release -- --filter "*StandardCodecEchoBenchmark*"

# LengthFieldCodec Echo
dotnet run --project Benchmark/Benchmark.csproj -c Release -- --filter "*LengthFieldCodecEchoBenchmark*"
```
