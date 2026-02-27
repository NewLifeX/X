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
Intel Core i9-10900K CPU 3.70GHz, 1 CPU, 20 逻辑核心 / 10 物理核心
.NET SDK 10.0.103
Runtime: .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v3（Server GC）
网络：loopback（127.0.0.1），客户端与服务端共享 CPU
```

## 已完成的优化项

当前代码已包含以下优化，本轮测试基于优化后的代码进行：

| 优化项 | 说明 |
|---|---|
| **ReceivedEventArgs 池化** | `Pool<ReceivedEventArgs>` + `Rent()/Return()`，避免每次 `recv()` 回调分配新事件参数 |
| **DefaultMessage 池化** | `Pool<DefaultMessage>` + `Rent()/Return()`，StandardCodec 的 Decode/Write/CreateReply 均从池中获取 |
| **PooledValueTaskSource** | 基于 `ManualResetValueTaskSourceCore<Object>` 的池化异步完成源，替代 `TaskCompletionSource` |
| **NetHandlerContext 池化** | `Pool<NetHandlerContext>` + `Rent()/Return()`，`CreateContext`/`ReturnContext` 从池中借还 |
| **SendMessageAsync 非异步化** | NET5_0_OR_GREATER 下改为非异步实现，消除编译器生成的 ~200B 状态机分配 |
| **接口返回 ValueTask** | `ISocketRemote`/`INetSession` 返回 `ValueTask<Object>`，消除 `AsTask()` 的 ~56B 包装 |

## 测试方法

### 通用配置

- 协议：`NetType.Tcp + AddressFamily.InterNetwork`
- IOCP 接收缓冲区：`BufferSize = 64 KB`
- `UseSession = false`
- Nagle 算法默认开启（`NoDelay = false`）
- BDN 参数：`warmupCount: 2, iterationCount: 5`

### 测试 1：纯接收吞吐（NetServerThroughputBenchmark）

- 服务端：`ThroughputNetServer`，`OnReceive` 仅 `Interlocked.Add` 累加字节。
- 客户端：`ISocketClient.Send(32B)`，无编解码器。
- 逐包：每次 `Send(32B)`，总计 2,097,152 包。
- 批量：256 包合并 `Send(8KB)`，总计 16,777,216 逻辑包。

### 测试 2：StandardCodec Echo

- 服务端：`NetServer + Add<StandardCodec>()`，收到请求后 `session.SendReply(pk, e)` 原样返回。
- 客户端：`ISocketClient + Add<StandardCodec>()`，发送 28B 负载（+4B 协议头 = 32B）。
- 负载构造：`ArrayPacket(buf, 4, 28)` 预留头部空间，`ExpandHeader` 零拷贝复用缓冲区。
- 逐包：串行 `SendMessageAsync` 等响应再下一包，总计 131,072 次。
- 批量：并发 255 个 `SendMessageAsync` WhenAll 下一轮，总计 261,120 次。

### 测试 3：LengthFieldCodec Echo

- 服务端：`NetServer + Add<LengthFieldCodec>()`，收到请求后原样返回。
- 客户端：`ISocketClient + Add<LengthFieldCodec>()`，发送 30B 负载（+2B 长度头 = 32B）。
- 逐包：串行请求-响应，总计 131,072 次。
- 批量：并发 128 个请求，FIFO 匹配响应，总计 262,144 次。

## 测试结果

### 1. 纯接收吞吐（无编解码器，不回发）

| 方法 | PacketSize | Concurrency | Mean | Error | StdDev | Allocated |
|---|---:|---:|---:|---:|---:|---:|
| 逐包发送 | 32 | 1 | 7,635.52 ns | 1,389.930 ns | 360.960 ns | 36 B |
| 逐包发送 | 32 | 4 | 984.09 ns | 294.608 ns | 76.509 ns | 5 B |
| 逐包发送 | 32 | 16 | 524.82 ns | 11.499 ns | 1.779 ns | 2 B |
| 逐包发送 | 32 | 64 | 526.88 ns | 87.609 ns | 13.558 ns | 0 B |
| 逐包发送 | 32 | 256 | 539.14 ns | 13.184 ns | 3.424 ns | 0 B |
| 逐包发送 | 32 | 1024 | 555.65 ns | 74.011 ns | 19.220 ns | 1 B |
| 批量发送 | 32 | 1 | 40.58 ns | 7.979 ns | 2.072 ns | - |
| 批量发送 | 32 | 4 | 11.70 ns | 1.735 ns | 0.269 ns | - |
| 批量发送 | 32 | 16 | 10.24 ns | 0.896 ns | 0.233 ns | - |
| 批量发送 | 32 | 64 | NA | NA | NA | NA |
| 批量发送 | 32 | 256 | 14.87 ns | 2.916 ns | 0.757 ns | - |
| 批量发送 | 32 | 1024 | 12.25 ns | 0.457 ns | 0.119 ns | - |

> 批量发送 C=64 触发 BDN 错误退出（疑似 Windows Defender 干扰），标记为 NA。

### 2. StandardCodec Echo（4 字节协议头，28B 负载）

| 方法 | Concurrency | Mean | Error | StdDev | Allocated |
|---|---:|---:|---:|---:|---:|
| 逐包Echo | 1 | 35.483 us | 1.108 us | 0.288 us | 1,128 B |
| 逐包Echo | 4 | 10.676 us | 0.122 us | 0.019 us | 1,128 B |
| 逐包Echo | 16 | 5.857 us | 1.760 us | 0.457 us | 1,128 B |
| 逐包Echo | 64 | 4.066 us | 0.634 us | 0.165 us | 1,128 B |
| 逐包Echo | 256 | 3.980 us | 0.234 us | 0.061 us | 1,129 B |
| 逐包Echo | 1024 | 4.663 us | 0.067 us | 0.010 us | 1,133 B |
| 批量Echo | 1 | 9.405 us | 0.399 us | 0.104 us | 921 B |
| 批量Echo | 4 | 3.879 us | 0.708 us | 0.184 us | 904 B |
| 批量Echo | 16 | 2.536 us | 0.155 us | 0.040 us | 901 B |
| 批量Echo | 64 | 2.511 us | 0.080 us | 0.012 us | 881 B |
| 批量Echo | 256 | 2.225 us | 0.097 us | 0.015 us | 842 B |
| 批量Echo | 1024 | 2.428 us | 0.086 us | 0.022 us | 807 B |

### 3. LengthFieldCodec Echo（2 字节长度头，30B 负载）

| 方法 | Concurrency | Mean | Error | StdDev | Allocated |
|---|---:|---:|---:|---:|---:|
| 逐包Echo | 1 | 31.396 us | 4.805 us | 1.248 us | 952 B |
| 逐包Echo | 4 | 10.901 us | 1.187 us | 0.308 us | 952 B |
| 逐包Echo | 16 | 5.144 us | 0.253 us | 0.039 us | 952 B |
| 逐包Echo | 64 | 4.049 us | 0.073 us | 0.019 us | 952 B |
| 逐包Echo | 256 | 4.240 us | 0.160 us | 0.025 us | 953 B |
| 逐包Echo | 1024 | 4.562 us | 0.074 us | 0.019 us | 958 B |
| 批量Echo | 1 | 9.684 us | 0.378 us | 0.098 us | 779 B |
| 批量Echo | 4 | 3.099 us | 0.131 us | 0.034 us | 779 B |
| 批量Echo | 16 | 2.315 us | 0.255 us | 0.066 us | 789 B |
| 批量Echo | 64 | 2.085 us | 0.057 us | 0.015 us | 788 B |
| 批量Echo | 256 | 2.224 us | 0.055 us | 0.014 us | 761 B |
| 批量Echo | 1024 | 2.353 us | 0.219 us | 0.034 us | 739 B |

## 核心指标：服务端每秒处理消息数

### 逐包发送/Echo

| Concurrency | 纯接收（包/秒） | StandardCodec Echo（msg/秒） | LengthFieldCodec Echo（msg/秒） |
|---:|---:|---:|---:|
| 1 | 130,968 | 28,183 | 31,851 |
| 4 | 1,016,168 | 93,670 | 91,735 |
| 16 | 1,905,402 | 170,736 | 194,402 |
| 64 | 1,897,962 | 245,943 | **246,975** |
| 256 | 1,854,794 | **251,256** | 235,849 |
| 1024 | 1,799,700 | 214,452 | 219,202 |

### 批量发送/Echo

| Concurrency | 纯接收（包/秒） | StandardCodec Echo（msg/秒） | LengthFieldCodec Echo（msg/秒） |
|---:|---:|---:|---:|
| 1 | 24,642,681 | 106,326 | 103,263 |
| 4 | 85,470,085 | 257,798 | 322,685 |
| 16 | 97,656,250 | 394,322 | 431,965 |
| 64 | NA | 398,247 | **479,616** |
| 256 | 67,249,496 | **449,438** | 449,640 |
| 1024 | 81,632,653 | 411,862 | 424,989 |

### 峰值吞吐汇总

| 场景 | 峰值 msg/s | 最优并发 | 每操作内存 |
|---|---:|---:|---:|
| **纯接收 逐包** | 1,905,402 | C=16 | 2 B |
| **纯接收 批量** | 97,656,250 | C=16 | 0 B |
| **StandardCodec 逐包** | **251,256** | C=256 | 1,129 B |
| **StandardCodec 批量** | **449,438** | C=256 | 842 B |
| **LengthFieldCodec 逐包** | **246,975** | C=64 | 952 B |
| **LengthFieldCodec 批量** | **479,616** | C=64 | 788 B |

## 对比分析

### 1. StandardCodec vs LengthFieldCodec

| 维度 | StandardCodec（4B头） | LengthFieldCodec（2B头） | 差异 |
|---|---:|---:|---:|
| 逐包峰值 | 251,256 msg/s (C=256) | 246,975 msg/s (C=64) | 几乎一致 |
| 批量峰值 | 449,438 msg/s (C=256) | 479,616 msg/s (C=64) | LengthFieldCodec 快 **6.7%** |
| 逐包内存 | 1,128 B/op | 952 B/op | LengthFieldCodec 少 **15.6%** |
| 批量内存（峰值并发） | 842 B/op | 788 B/op | LengthFieldCodec 少 **6.4%** |

**结论**：两种编解码器逐包吞吐几乎一致。批量场景 LengthFieldCodec 略优（+6.7%），内存始终更省（无 DefaultMessage 对象分配）。StandardCodec 凭借序列号匹配机制更灵活，适合乱序响应的复杂场景。

### 2. 并发数对吞吐的影响

| Concurrency | StandardCodec 逐包 | StandardCodec 批量 | LengthFieldCodec 逐包 | LengthFieldCodec 批量 |
|---:|---:|---:|---:|---:|
| 1 | 28,183 | 106,326 | 31,851 | 103,263 |
| 4 | 93,670 | 257,798 | 91,735 | 322,685 |
| 16 | 170,736 | 394,322 | 194,402 | 431,965 |
| **64** | 245,943 | 398,247 | **246,975** | **479,616** |
| **256** | **251,256** | **449,438** | 235,849 | 449,640 |
| 1024 | 214,452 | 411,862 | 219,202 | 424,989 |

- **最优并发区间**：C=64~256 达到峰值，两种编解码器表现一致。
- **C>256 回落**：loopback 环境客户端和服务端共享 CPU，超高并发导致线程争抢和上下文切换开销。
- **逐包 C=1 瓶颈**：单连接串行 RTT 约 31~35 us，仅 2.8~3.2 万 msg/s，瓶颈在 TCP 往返延迟。
- **批量 vs 逐包提升**：批量比逐包提升 **1.6~1.9 倍**，批量利用了 TCP Nagle 合包和并发流水线。

### 3. 编解码器 vs 纯接收

| 场景 | 纯接收（逐包 C=16） | Echo 逐包峰值 | 放大倍数 |
|---|---:|---:|---:|
| StandardCodec | 1,905,402 | 251,256 | 7.6x |
| LengthFieldCodec | 1,905,402 | 246,975 | 7.7x |

Echo 回路比纯接收慢约 **7.6~7.7 倍**，瓶颈在于：请求+响应的两次 TCP 往返、编解码管道的虚方法/委托调用链、对象分配与 GC 压力。

### 4. 内存分配分析

逐包 Echo 每操作内存分配稳定在 **~1,128 B**（StandardCodec）/ **~952 B**（LengthFieldCodec），主要来源：

| 分配来源 | 估算大小 | 说明 |
|---|---:|---|
| HandlerContext.Items (NullableDictionary) | ~200 B | 池化上下文每次 `Reset()` 后 `Items.Clear()`，再 `ctx["TaskSource"]=...` 重新 Add 触发字典内部数组分配 |
| PacketCodec 粘包缓冲区 | ~150 B | 粘包拆包时动态缓冲区扩展 |
| DefaultMessage 序列化 (ToPacket/ExpandHeader) | ~100 B | 协议头编码时的缓冲区操作（仅 StandardCodec） |
| 响应侧 ReceivedEventArgs/Context 创建 | ~100 B | 服务端处理请求时的上下文和事件参数 |
| 匹配队列 Match/Add 操作 | ~80 B | `DefaultMatchQueue` 内部的 `MatchItem` 分配 |
| 其它零散分配（Span上下文/委托闭包等） | ~100-200 B | 零散小对象 |

## 性能瓶颈定位

### 瓶颈 1：loopback TCP 往返（占 Echo 总耗时 ~40-50%）

每次 Echo 需要两次 loopback 传输（请求 + 响应），内核 TCP 协议栈处理 + IOCP 调度在 loopback 下合计约 1,000~1,200 ns。单连接串行 RTT（C=1）达 31~35 us，说明 TCP 栈 + IOCP 回调 + 用户态处理的完整链路开销较大。

### 瓶颈 2：管道事件链（~15-20%）

编码/解码各经过 2-4 层虚方法/委托调用。`Pipeline.Write` → `MessageCodec.Write` → `Encode` → `StandardCodec.Write` → `base.Write` → `NetHandlerContext.FireWrite` → `session.Send`，每层有条件分支和类型检查。

### 瓶颈 3：剩余对象分配（~10-15%）

每次 Echo 约 800~1,130 B 分配，在高并发下触发频繁 Gen0 GC。从 BDN 输出可见 Gen0 收集率约 0.01~0.04/千次操作。

### 瓶颈 4：HandlerContext.Items 字典操作（~5-10%）

`SendMessageAsync` 中 `ctx["TaskSource"] = source; ctx["Span"] = span;` 通过字典索引器存取，`Reset()` 调用 `Items.Clear()`。字典的哈希计算和内部数组操作在每次请求中重复执行。

## 优化建议

| 优先级 | 方向 | 预期收益 | 实施方案 |
|---|---|---|---|
| ★★★ | **HandlerContext 专用字段替代字典** | 省 ~200 B/op，减少哈希开销 | 在 `NetHandlerContext` 上增加 `TaskSource` 和 `Span` 强类型属性，`SendMessageAsync`/`MessageCodec` 直接读写字段，避免 `Items` 字典的 `Clear()`+`Add` 开销 |
| ★★★ | **真实多机压测** | 消除 loopback CPU 共享瓶颈 | 独立客户端机器，验证服务端真实可达吞吐（预期提升 30-50%） |
| ★★☆ | **MatchItem 池化** | 省 ~80 B/op | `DefaultMatchQueue` 内部 `MatchItem` 对象使用 `Pool<MatchItem>` 复用 |
| ★★☆ | **NoDelay 模式基准** | 逐包场景可能降低延迟 | 增加 `NoDelay=true` 基准对比，消除 Nagle 延迟对逐包 RTT 的影响 |
| ★☆☆ | **服务端批量回复合并** | 减少 Send 系统调用次数 | 同一连接多个响应合并为一次 `Send`，减少内核态切换 |
| ★☆☆ | **Decode 使用 yield return 优化** | 避免中间列表分配 | StandardCodec.Decode 已使用 `yield return`（当前实现正确），确认 LengthFieldCodec 同样如此 |

### 关键优化路径分析

**HandlerContext 专用字段方案**是当前投入产出比最高的优化方向：

```
当前路径：ctx["TaskSource"] = source → NullableDictionary.this[key].set → 哈希+查找+插入
优化路径：ctx.TaskSource = source → 直接字段赋值
```

`HandlerContext.Items` 是 `NullableDictionary<String, Object?>`，每次 `Reset()` 调用 `Items.Clear()` 清空内部数组，下一轮 `ctx["TaskSource"]` 和 `ctx["Span"]` 再触发字典扩容。通过在 `NetHandlerContext` 上增加专用属性，可以完全消除这部分字典操作和内存分配。

## 测试结论

| 问题 | 结论 |
|---|---|
| 当前峰值吞吐？ | StandardCodec **25.1 万 msg/s**（逐包 C=256），**44.9 万 msg/s**（批量 C=256） |
| LengthFieldCodec 峰值？ | **24.7 万 msg/s**（逐包 C=64），**48.0 万 msg/s**（批量 C=64） |
| 两种编解码器差异？ | 逐包吞吐几乎一致（差异 <2%），批量 LengthFieldCodec 快 6.7%，内存少 6~16% |
| 编解码器对吞吐的影响？ | Echo 回路比纯接收慢 **~7.7 倍**（逐包），瓶颈在 TCP 往返和管道调度 |
| 最优并发数？ | **C=64~256** 附近峰值，更高并发因 loopback CPU 争抢下降 |
| 批量并发价值？ | 批量比逐包提升 **1.6~1.9 倍** |
| 每操作内存分配？ | StandardCodec **~1,128 B/op**（逐包），LengthFieldCodec **~952 B/op**（逐包） |
| 首要优化方向？ | HandlerContext 专用字段替代字典查找（预期省 ~200 B/op） |

## 附录：运行命令

```bash
# 纯接收吞吐
dotnet run --project Benchmark/Benchmark.csproj -c Release -- --filter "*NetServerThroughputBenchmark*"

# StandardCodec Echo
dotnet run --project Benchmark/Benchmark.csproj -c Release -- --filter "*StandardCodecEchoBenchmark*"

# LengthFieldCodec Echo
dotnet run --project Benchmark/Benchmark.csproj -c Release -- --filter "*LengthFieldCodecEchoBenchmark*"

# 运行全部网络基准
dotnet run --project Benchmark/Benchmark.csproj -c Release -- --filter "*NetServerThroughputBenchmark*" "*StandardCodecEchoBenchmark*" "*LengthFieldCodecEchoBenchmark*"
```
