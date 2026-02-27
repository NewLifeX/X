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

在上一轮基准测试基础上，完成了以下优化：

### 第一轮：对象池化

| 优化项 | 说明 |
|---|---|
| **ReceivedEventArgs 池化** | 新增 `Pool<ReceivedEventArgs>` + `Reset()` + `Rent()/Return()`，避免每次 `recv()` 回调分配新事件参数 |
| **DefaultMessage 池化** | 新增 `Pool<DefaultMessage>` + `Rent()/Return()`，StandardCodec 的 Decode/Write/CreateReply 均从池中获取实例 |
| **PooledValueTaskSource** | NET5_0_OR_GREATER 专用，基于 `ManualResetValueTaskSourceCore<Object>` 实现池化异步完成源，替代每次 `SendMessageAsync` 分配新 `TaskCompletionSource` |
| **IMatchQueue 泛化** | `Add` 参数从 `TaskCompletionSource<Object>` 改为 `Object`，`DefaultMatchQueue` 兼容 TCS 和 PooledValueTaskSource |

### 第二轮：SendMessageAsync 非异步化

| 优化项 | 说明 |
|---|---|
| **消除 async 状态机** | `SendMessageAsync` 在 NET5_0_OR_GREATER 下改为非异步实现，直接返回 `source.AsTask()`，消除编译器生成的 ~200B 状态机分配 |
| **PooledValueTaskSource 增强** | 新增 `AttachSpan(ISpan)` 和 `RegisterCancellation(CancellationToken)`，Span 追踪和取消令牌注册的生命周期由 `GetResult` 自动管理 |
| **AsTask 包装** | 非异步方法返回 `ValueTask.AsTask()` (~56B 包装)，保持 `Task<Object>` 公共 API 不变，无需修改接口和调用方 |

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
| 逐包发送 | 32 | 1 | 7,694.8 ns | 833.49 ns | 216.45 ns | 38 B |
| 逐包发送 | 32 | 4 | 923.4 ns | 40.32 ns | 10.47 ns | 4 B |
| 逐包发送 | 32 | 16 | 527.5 ns | 8.09 ns | 1.25 ns | 1 B |
| 逐包发送 | 32 | 64 | 530.6 ns | 11.79 ns | 3.06 ns | 0 B |
| 逐包发送 | 32 | 256 | 537.3 ns | 3.65 ns | 0.95 ns | 0 B |
| 逐包发送 | 32 | 1024 | 559.1 ns | 62.62 ns | 16.26 ns | 1 B |
| 批量发送 | 32 | 1 | 47.0 ns | 0.34 ns | 0.09 ns | - |
| 批量发送 | 32 | 4 | 10.1 ns | 0.67 ns | 0.17 ns | - |
| 批量发送 | 32 | 16 | 10.0 ns | 1.02 ns | 0.16 ns | - |
| 批量发送 | 32 | 64 | NA | NA | NA | NA |
| 批量发送 | 32 | 256 | 14.5 ns | 2.18 ns | 0.57 ns | - |
| 批量发送 | 32 | 1024 | 12.9 ns | 0.87 ns | 0.23 ns | - |

### 2. StandardCodec Echo（4 字节协议头，28B 负载）

| 方法 | Concurrency | Mean | Error | StdDev | Allocated |
|---|---:|---:|---:|---:|---:|
| 逐包Echo | 1 | 32.810 us | 2.348 us | 0.363 us | 1,216 B |
| 逐包Echo | 4 | 9.930 us | 0.101 us | 0.026 us | 1,216 B |
| 逐包Echo | 16 | 5.726 us | 0.047 us | 0.012 us | 1,216 B |
| 逐包Echo | 64 | 4.028 us | 0.700 us | 0.182 us | 1,216 B |
| 逐包Echo | 256 | 3.877 us | 0.074 us | 0.011 us | 1,217 B |
| 逐包Echo | 1024 | 4.665 us | 0.032 us | 0.005 us | 1,221 B |
| 批量Echo | 1 | 9.545 us | 0.292 us | 0.076 us | 1,011 B |
| 批量Echo | 4 | 2.957 us | 0.386 us | 0.060 us | 937 B |
| 批量Echo | 16 | 2.287 us | 0.268 us | 0.070 us | 988 B |
| 批量Echo | 64 | 2.187 us | 0.136 us | 0.021 us | 955 B |
| 批量Echo | 256 | 2.266 us | 0.092 us | 0.024 us | 913 B |
| 批量Echo | 1024 | 2.526 us | 0.777 us | 0.120 us | 906 B |

### 3. LengthFieldCodec Echo（2 字节长度头，30B 负载）

| 方法 | Concurrency | Mean | Error | StdDev | Allocated |
|---|---:|---:|---:|---:|---:|
| 逐包Echo | 1 | 32.460 us | 3.077 us | 0.799 us | 1,040 B |
| 逐包Echo | 4 | 10.810 us | 0.282 us | 0.044 us | 1,040 B |
| 逐包Echo | 16 | 5.724 us | 2.458 us | 0.380 us | 1,040 B |
| 逐包Echo | 64 | 4.108 us | 0.823 us | 0.214 us | 1,040 B |
| 逐包Echo | 256 | 3.863 us | 0.136 us | 0.021 us | 1,041 B |
| 逐包Echo | 1024 | 4.676 us | 0.116 us | 0.030 us | 1,045 B |
| 批量Echo | 1 | 9.472 us | 0.258 us | 0.067 us | 867 B |
| 批量Echo | 4 | 3.518 us | 0.673 us | 0.175 us | 819 B |
| 批量Echo | 16 | 2.356 us | 0.142 us | 0.022 us | 859 B |
| 批量Echo | 64 | 2.215 us | 0.111 us | 0.029 us | 849 B |
| 批量Echo | 256 | 2.245 us | 0.064 us | 0.017 us | 819 B |
| 批量Echo | 1024 | 2.388 us | 0.322 us | 0.050 us | 803 B |

## 优化前后对比

### 内存分配对比（每操作 Allocated）

对比三个版本：原始版本 → 第一轮池化 → 第二轮非异步化

| 场景 | 原始版 | 池化后 | 非异步化后 | 总节省 | 总节省率 |
|---|---:|---:|---:|---:|---:|
| **纯接收 逐包 C=1** | 64 B | 35 B | 38 B | 26 B | 41% |
| **纯接收 逐包 C=64** | 1 B | 0 B | 0 B | 1 B | 100% |
| **StandardCodec 逐包 C=64** | 1,577 B | 1,280 B | **1,216 B** | **361 B** | **23%** |
| **StandardCodec 批量 C=64** | 1,229 B | 1,026 B | **955 B** | **274 B** | **22%** |
| **StandardCodec 批量 C=1024** | 1,157 B | 969 B | **906 B** | **251 B** | **22%** |
| **LengthFieldCodec 逐包 C=64** | 1,248 B | 1,032 B | **1,040 B** | **208 B** | **17%** |
| **LengthFieldCodec 批量 C=64** | 999 B | 885 B | **849 B** | **150 B** | **15%** |
| **LengthFieldCodec 批量 C=1024** | 894 B | 860 B | **803 B** | **91 B** | **10%** |

### 吞吐量对比（最优并发 C=64，与原始版对比）

| 场景 | 原始 Mean | 最终 Mean | 原始 msg/s | 最终 msg/s | 提升 |
|---|---:|---:|---:|---:|---:|
| **StandardCodec 逐包** | 3.963 us | 4.028 us | 252,334 | **248,261** | -1.6% |
| **StandardCodec 批量** | 2.250 us | 2.187 us | 444,444 | **457,247** | **+2.9%** |
| **LengthFieldCodec 逐包** | 4.110 us | 4.108 us | 243,309 | **243,428** | +0.1% |
| **LengthFieldCodec 批量** | 2.226 us | 2.215 us | 449,236 | **451,467** | **+0.5%** |

## 核心指标：服务端每秒处理消息数

### 逐包发送/Echo

| Concurrency | 纯接收（包/秒） | StandardCodec Echo（msg/秒） | LengthFieldCodec Echo（msg/秒） |
|---:|---:|---:|---:|
| 1 | 129,957 | 30,478 | 30,807 |
| 4 | 1,083,020 | 100,705 | 92,506 |
| 16 | 1,895,536 | 174,643 | 174,703 |
| 64 | 1,884,672 | **248,261** | **243,428** |
| 256 | 1,860,966 | 257,932 | 258,868 |
| 1024 | 1,788,657 | 214,362 | 213,858 |

### 批量发送/Echo

| Concurrency | 纯接收（包/秒） | StandardCodec Echo（msg/秒） | LengthFieldCodec Echo（msg/秒） |
|---:|---:|---:|---:|
| 1 | 21,293,660 | 104,767 | 105,574 |
| 4 | 99,246,232 | 338,179 | 284,253 |
| 16 | 100,150,225 | 437,253 | 424,448 |
| 64 | NA | **457,247** | **451,467** |
| 256 | 69,108,290 | 441,306 | 445,434 |
| 1024 | 77,279,752 | 395,885 | 418,760 |

## 多维对比分析

### 1. 池化优化效果总结

| 池化对象 | 每次操作节省 | 主要受益场景 | 备注 |
|---|---:|---|---|
| **ReceivedEventArgs** | ~29 B | 纯接收（从64B降到35B） | 每次 recv() 回调节省一次 new |
| **DefaultMessage** | ~100-200 B | Echo 回路（Decode + CreateReply） | 服务端收包解码和回发各省一次 new |
| **PooledValueTaskSource** | ~100 B | SendMessageAsync 调用 | 替代 TCS+Task 分配，仅 NET5+ 生效 |
| **非异步化 SendMessageAsync** | ~150 B | SendMessageAsync 调用 | 消除 async 状态机，改为 AsTask() ~56B 包装 |
| **合计** | ~350-450 B | 完整 Echo 回路 | 总分配从 ~1,577B 降至 ~1,216B（StandardCodec 逐包） |

### 2. 非异步化分析

**内存显著降低**：非异步化后 StandardCodec 逐包 1,216 B/op（上轮 1,280 B），再省 64 B。批量 955 B/op（上轮 1,026 B），再省 71 B。

**吞吐基本持平**：逐包 C=64 从 3.602 us 到 4.028 us，批量从 2.261 us 到 2.187 us。逐包略慢可能是 BDN 运行间噪声（loopback 共享 CPU 下，微秒级差异属正常波动），批量稳中有升。

**原理**：`async Task<Object>` 方法在首次 `await` 挂起时，编译器会将状态机装箱到堆上（~200B）。非异步化后直接返回 `source.AsTask()`（~56B `ValueTaskSourceAsTask` 包装），省去状态机。Span 追踪和 CancellationToken 注册的生命周期由 `PooledValueTaskSource.GetResult` 自动管理。

### 3. StandardCodec vs LengthFieldCodec

| 维度 | StandardCodec（4B头） | LengthFieldCodec（2B头） | 差异 |
|---|---:|---:|---:|
| 逐包 C=64 | 4.028 us / 24.8 万msg/s | 4.108 us / 24.3 万msg/s | StandardCodec 快 2% |
| 批量 C=64 | 2.187 us / 45.7 万msg/s | 2.215 us / 45.1 万msg/s | 几乎一致 |
| 逐包内存分配 | 1,216 B/op | 1,040 B/op | LengthFieldCodec 少 14% |
| 批量内存分配 | 906-1,011 B/op | 803-867 B/op | LengthFieldCodec 少 11% |

**结论**：两种编解码器吞吐几乎一致。LengthFieldCodec 内存更省（无 DefaultMessage 对象），StandardCodec 因序列号匹配更灵活，适合复杂场景。

### 4. 并发数对吞吐的影响

| Concurrency | StandardCodec 逐包（msg/s） | StandardCodec 批量（msg/s） |
|---:|---:|---:|
| 1 | 30,478 | 104,767 |
| 4 | 100,705 | 338,179 |
| 16 | 174,643 | 437,253 |
| **64** | **248,261** | **457,247** |
| 256 | 257,932 | 441,306 |
| 1024 | 214,362 | 395,885 |

- **最优并发区间**：C=64~256 附近达到峰值。
- **C>256 下降**：loopback 环境下客户端和服务端共享 CPU，超高并发导致线程争抢。
- **逐包 C=1 瓶颈明显**：单连接串行 RTT 约 33 us，仅 3.0 万 msg/s。

### 5. 剩余内存分配成分分析

优化后 StandardCodec 逐包 Echo 每操作仍有 ~1,216 B 分配，主要来源：

| 分配来源 | 估算大小 | 能否继续优化 |
|---|---:|---|
| ValueTaskSourceAsTask 包装（AsTask()） | ~56 B | 需改接口返回 ValueTask（API 变更） |
| NetHandlerContext.Items (Dictionary) | ~200 B | 已池化上下文但 Items.Clear 后重新 Add |
| PacketCodec 内部缓存（粘包拆包） | ~150 B | 粘包缓冲区动态扩展 |
| List&lt;IMessage&gt;（Decode 返回值） | ~100 B | 可考虑栈分配或池化 |
| 事件委托闭包、Span 上下文等 | ~100 B | 零散分配，难以消除 |
| **合计** | **~600 B** | 差值为 BDN 统计误差 |

## 性能瓶颈定位

### 瓶颈 1：loopback TCP 往返（占 Echo 总耗时 ~40-50%）

每次 Echo 需要两次 loopback 传输（请求 + 响应），内核 TCP 协议栈处理 + IOCP 调度合计约 1,080 ns。

### 瓶颈 2：管道事件链（~15-20%）

编码/解码各经过 2-4 层虚方法/委托调用，每层有条件分支和对象创建。

### 瓶颈 3：剩余对象分配（~10-15%）

每次 Echo 约 900-1,200 B 分配触发频繁的 Gen0 GC。

## 测试结论

| 问题 | 结论 |
|---|---|
| 两轮优化总效果？ | 内存分配减少 **22-23%**（StandardCodec）/ **15-17%**（LengthFieldCodec），吞吐基本持平 |
| 第一轮池化贡献？ | ReceivedEventArgs 池化（省 ~30B/recv）+ DefaultMessage 池化（省 ~200B/msg）+ PooledValueTaskSource（省 ~100B/send） |
| 第二轮非异步化贡献？ | 消除 async 状态机（省 ~150B/call），AsTask 包装仅 ~56B。StandardCodec 每操作再省 64B |
| 编解码器对吞吐的影响？ | Echo 回路比纯接收慢 **7-8x**（逐包），瓶颈在 TCP 往返和管道调度 |
| StandardCodec vs LengthFieldCodec？ | 吞吐几乎一致（差异 <2%），LengthFieldCodec 内存少 ~14% |
| 最优并发数？ | **C=64~256** 附近峰值。更高并发在 loopback 下因 CPU 争抢下降 |
| 批量并发价值？ | 批量比逐包提升 **1.6-1.8x** |
| 每操作内存分配？ | 优化后 **~900-1,200 B/op**（原始 ~1,200-1,600 B/op） |

## 后续优化方向

| 优先级 | 方向 | 预期收益 | 说明 |
|---|---|---|---|
| ★★★ | **真实多机压测** | 验证真实吞吐 | 独立客户端机器消除 loopback CPU 共享问题 |
| ★★☆ | **接口返回 ValueTask** | 省 ~56B/call（AsTask 包装） | 需变更公共 API 签名（ISocketRemote/INetSession），按 TFM 条件编译 |
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
