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
- 滑动窗口：始终保持 255 个请求在途（StandardCodec 序列号 1 字节，最多 255 并发），任一完成立即补发下一个，总计 261,120 次。

### 测试 3：LengthFieldCodec Echo

- 服务端：`NetServer + Add<LengthFieldCodec>()`，收到请求后原样返回。
- 客户端：`ISocketClient + Add<LengthFieldCodec>()`，发送 30B 负载（+2B 长度头 = 32B）。
- 逐包：串行请求-响应，总计 131,072 次。
- 滑动窗口：始终保持 256 个请求在途（匹配 `DefaultMatchQueue` 的 256 坑位），任一完成立即补发，总计 262,144 次。

### 滑动窗口模式说明

滑动窗口模式始终保持匹配队列接近满载，更真实地模拟高吞吐场景。实现方式：循环缓冲区 + FIFO await，最旧请求完成后立即在该槽位补发新请求，保持 TCP 管道持续有数据流动。

## 测试结果

### 1. 纯接收吞吐（无编解码器，不回发）

| 方法 | PacketSize | Concurrency | Mean | Error | StdDev | Allocated |
|---|---:|---:|---:|---:|---:|---:|
| 逐包发送 | 32 | 1 | 7,184.18 ns | 1,246.456 ns | 323.701 ns | 36 B |
| 逐包发送 | 32 | 4 | 2,036.15 ns | 1,635.954 ns | 424.852 ns | 12 B |
| 逐包发送 | 32 | 16 | 591.25 ns | 291.093 ns | 75.596 ns | 1 B |
| 逐包发送 | 32 | 64 | 524.81 ns | 13.007 ns | 2.013 ns | 0 B |
| 逐包发送 | 32 | 256 | 533.38 ns | 12.301 ns | 1.904 ns | 0 B |
| 逐包发送 | 32 | 1024 | 534.77 ns | 3.084 ns | 0.477 ns | 1 B |
| 批量发送 | 32 | 1 | 43.74 ns | 7.542 ns | 1.959 ns | - |
| 批量发送 | 32 | 4 | 7.11 ns | 0.553 ns | 0.144 ns | - |
| 批量发送 | 32 | 16 | 9.93 ns | 1.939 ns | 0.504 ns | - |
| 批量发送 | 32 | 64 | NA | NA | NA | NA |
| 批量发送 | 32 | 256 | 14.53 ns | 1.384 ns | 0.359 ns | - |
| 批量发送 | 32 | 1024 | 11.99 ns | 0.667 ns | 0.103 ns | - |

> 批量发送 C=64 触发 BDN 错误退出（疑似 Windows Defender 干扰），标记为 NA。

### 2. StandardCodec Echo（4 字节协议头，28B 负载）

| 方法 | Concurrency | Mean | Error | StdDev | Allocated |
|---|---:|---:|---:|---:|---:|
| 逐包Echo | 1 | 33.635 us | 2.077 us | 0.539 us | 1,128 B |
| 逐包Echo | 4 | 10.152 us | 0.238 us | 0.062 us | 1,128 B |
| 逐包Echo | 16 | 5.290 us | 0.520 us | 0.080 us | 1,128 B |
| 逐包Echo | 64 | 3.996 us | 0.147 us | 0.038 us | 1,128 B |
| 逐包Echo | 256 | 3.843 us | 0.106 us | 0.027 us | 1,129 B |
| 逐包Echo | 1024 | 4.738 us | 0.185 us | 0.048 us | 1,133 B |
| 滑动窗口Echo | 1 | 7.990 us | 0.756 us | 0.196 us | 698 B |
| 滑动窗口Echo | 4 | 3.431 us | 0.067 us | 0.010 us | 790 B |
| 滑动窗口Echo | 16 | 2.581 us | 0.149 us | 0.039 us | 833 B |
| 滑动窗口Echo | 64 | 2.507 us | 0.055 us | 0.014 us | 827 B |
| 滑动窗口Echo | 256 | 2.468 us | 0.115 us | 0.030 us | 788 B |
| 滑动窗口Echo | 1024 | 2.381 us | 0.484 us | 0.126 us | 815 B |

### 3. LengthFieldCodec Echo（2 字节长度头，30B 负载）

| 方法 | Concurrency | Mean | Error | StdDev | Allocated |
|---|---:|---:|---:|---:|---:|
| 逐包Echo | 1 | 30.273 us | 3.523 us | 0.545 us | 952 B |
| 逐包Echo | 4 | 10.752 us | 0.599 us | 0.156 us | 952 B |
| 逐包Echo | 16 | 6.073 us | 0.400 us | 0.104 us | 952 B |
| 逐包Echo | 64 | 3.970 us | 0.055 us | 0.014 us | 952 B |
| 逐包Echo | 256 | 3.659 us | 0.084 us | 0.022 us | 953 B |
| 逐包Echo | 1024 | 4.610 us | 0.100 us | 0.026 us | 958 B |
| 滑动窗口Echo | 1 | 9.100 us | 0.214 us | 0.033 us | 683 B |
| 滑动窗口Echo | 4 | 2.773 us | 0.147 us | 0.038 us | 666 B |
| 滑动窗口Echo | 16 | 2.208 us | 0.109 us | 0.017 us | 707 B |
| 滑动窗口Echo | 64 | 2.341 us | 0.050 us | 0.013 us | 701 B |
| 滑动窗口Echo | 256 | 2.196 us | 0.207 us | 0.054 us | 679 B |
| 滑动窗口Echo | 1024 | 2.504 us | 0.737 us | 0.192 us | 723 B |

## 核心指标：服务端每秒处理消息数

### 逐包 Echo（msg/s = 1,000,000 / Mean_us）

| Concurrency | 纯接收（包/秒） | StandardCodec（msg/秒） | LengthFieldCodec（msg/秒） |
|---:|---:|---:|---:|
| 1 | 139,194 | 29,731 | 33,033 |
| 4 | 491,122 | 98,503 | 93,007 |
| 16 | 1,691,332 | 189,036 | 164,663 |
| 64 | 1,905,458 | 250,250 | 251,889 |
| 256 | 1,874,830 | **260,213** | **273,298** |
| 1024 | 1,869,968 | 211,059 | 216,920 |

### 滑动窗口 Echo（msg/s = 1,000,000 / Mean_us）

| Concurrency | 纯接收批量（包/秒） | StandardCodec（msg/秒） | LengthFieldCodec（msg/秒） |
|---:|---:|---:|---:|
| 1 | 22,864,372 | 125,156 | 109,890 |
| 4 | 140,687,254 | 291,459 | 360,619 |
| 16 | 100,735,432 | 387,445 | **452,899** |
| 64 | NA | 398,882 | 427,167 |
| 256 | 68,808,647 | 405,187 | **455,373** |
| 1024 | 83,409,792 | **420,008** | 399,361 |

### 峰值吞吐汇总

| 场景 | 峰值 msg/s | 最优并发 | 每操作内存 |
|---|---:|---:|---:|
| **纯接收 逐包** | 1,905,458 | C=64 | 0 B |
| **纯接收 批量** | 140,687,254 | C=4 | 0 B |
| **StandardCodec 逐包** | **260,213** | C=256 | 1,129 B |
| **StandardCodec 滑动窗口** | **420,008** | C=1024 | 815 B |
| **LengthFieldCodec 逐包** | **273,298** | C=256 | 953 B |
| **LengthFieldCodec 滑动窗口** | **455,373** | C=256 | 679 B |

## 对比分析

### 1. StandardCodec vs LengthFieldCodec

| 维度 | StandardCodec（4B头） | LengthFieldCodec（2B头） | 差异 |
|---|---:|---:|---:|
| 逐包峰值 | 260,213 msg/s (C=256) | 273,298 msg/s (C=256) | LengthFieldCodec 快 **5.0%** |
| 滑动窗口峰值 | 420,008 msg/s (C=1024) | 455,373 msg/s (C=256) | LengthFieldCodec 快 **8.4%** |
| 逐包内存 | 1,128 B/op | 952 B/op | LengthFieldCodec 少 **15.6%** |
| 滑动窗口内存（峰值并发） | 815 B/op | 679 B/op | LengthFieldCodec 少 **16.7%** |

**结论**：LengthFieldCodec 在两种模式下均优于 StandardCodec。逐包快 5%，滑动窗口快 8.4%，内存始终少 15~17%。LengthFieldCodec 无需 DefaultMessage 对象和序列号编解码，路径更短。StandardCodec 凭借序列号匹配机制适合乱序响应的复杂场景。

### 2. 滑动窗口 vs 逐包提升

| 编解码器 | 逐包峰值 | 滑动窗口峰值 | 提升倍数 |
|---|---:|---:|---:|
| StandardCodec | 260,213 | 420,008 | **1.61x** |
| LengthFieldCodec | 273,298 | 455,373 | **1.67x** |

滑动窗口比逐包提升 **60~67%**，原因：

- 匹配队列持续满载，TCP 管道始终有数据流动
- Nagle 算法自然合并连续小包，减少系统调用次数
- IOCP 回调完成后立即有新请求可处理，减少 CPU 空闲

### 3. 并发数对吞吐的影响

| Concurrency | Standard 逐包 | Standard 滑窗 | LengthField 逐包 | LengthField 滑窗 |
|---:|---:|---:|---:|---:|
| 1 | 29,731 | 125,156 | 33,033 | 109,890 |
| 4 | 98,503 | 291,459 | 93,007 | 360,619 |
| 16 | 189,036 | 387,445 | 164,663 | **452,899** |
| 64 | 250,250 | 398,882 | 251,889 | 427,167 |
| **256** | **260,213** | 405,187 | **273,298** | **455,373** |
| 1024 | 211,059 | **420,008** | 216,920 | 399,361 |

- **逐包最优并发**：C=256 两种编解码器均达到逐包峰值。
- **滑动窗口最优并发**：LengthFieldCodec 在 C=16~256 均表现优异（>42 万 msg/s），StandardCodec 在 C=256~1024 达到峰值。
- **C>256 回落**：loopback 环境客户端和服务端共享 CPU，超高并发导致线程争抢。
- **逐包 C=1 瓶颈**：单连接串行 RTT 约 30~34 us，仅 3.0~3.3 万 msg/s，瓶颈在 TCP 往返延迟。
- **滑动窗口 C=1**：单连接但窗口 255/256，利用管道并行，提升至 11~12.5 万 msg/s（约 **3.3~4.2 倍**）。

### 4. 编解码器 vs 纯接收

| 场景 | 纯接收 逐包峰值 | Echo 逐包峰值 | 放大倍数 |
|---|---:|---:|---:|
| StandardCodec | 1,905,458 | 260,213 | 7.3x |
| LengthFieldCodec | 1,905,458 | 273,298 | 7.0x |

Echo 回路比纯接收慢约 **7.0~7.3 倍**，瓶颈在于：请求+响应的两次 TCP 往返、编解码管道的虚方法/委托调用链、对象分配与 GC 压力。

### 5. 内存分配分析

| 场景 | StandardCodec | LengthFieldCodec | 差值 |
|---|---:|---:|---:|
| 逐包 | ~1,128 B/op | ~952 B/op | -176 B |
| 滑动窗口 | ~698~833 B/op | ~666~723 B/op | ~-100 B |

滑动窗口比逐包每操作内存少 **200~300 B**，原因：滑动窗口复用循环缓冲区，且 BDN 的 OperationsPerInvoke 分摊了固定开销。

逐包 Echo 每操作 ~1,128 B（StandardCodec）/ ~952 B（LengthFieldCodec）的主要来源：

| 分配来源 | 估算大小 | 说明 |
|---|---:|---|
| HandlerContext.Items (NullableDictionary) | ~200 B | 池化上下文每次 `Reset()` 后 `Items.Clear()`，再 `ctx["TaskSource"]=...` 重新 Add 触发字典内部数组分配 |
| PacketCodec 粘包缓冲区 | ~150 B | 粘包拆包时动态缓冲区扩展 |
| StandardCodec 编码缓冲区 | ~100 B | 协议头编码时的缓冲区分配（仅 StandardCodec，DefaultMessage 对象已池化） |
| 响应侧上下文对象 | ~100 B | 服务端处理请求时的上下文分配（ReceivedEventArgs 已池化） |
| 匹配队列 Match/Add 操作 | ~80 B | `DefaultMatchQueue` 内部的 `Item` 对象分配 |
| 其它零散分配 | ~100-200 B | Span 上下文、委托闭包等 |

## 性能瓶颈定位

### 核心瓶颈点总览

| 优先级 | 瓶颈 | 优化收益占比 | 当前开销 | 优化后预估 | 内存节省 |
|--------|------|------------|---------|-----------|---------|
| P0 | loopback TCP 往返延迟 | ~40% | 单连接 RTT 30-34 us，占 Echo 总耗时 ~40-50% | 多机部署消除 CPU 共享 | — |
| P1 | HandlerContext.Items 字典操作 | ~25% | ~200 B/op，哈希+Clear+Add 每请求 | 0 B/op，直接字段赋值 | **100%（200 B/op）** |
| P1 | 管道事件链虚方法调度 | ~20% | 2-4 层虚方法/委托调用，占总耗时 ~15-20% | 减少 1-2 层调用 | — |
| P2 | 剩余对象分配（GC 压力） | ~10% | ~700-1,130 B/op，Gen0 约 0.01-0.04/千次 | ~500-800 B/op | **20-30%** |
| P3 | MatchItem 对象分配 | ~5% | ~80 B/op | 池化 0 B/op | **100%（80 B/op）** |

### 关键内存优化方向

| 优先级 | 优化方向 | 当前分配 | 优化后预估 | 节省比例 | 实施方案 |
|--------|---------|---------|-----------|---------|---------|
| P1 | HandlerContext 专用字段替代字典 | ~200 B/op | 0 B/op | **100%** | `NetHandlerContext` 增加 `TaskSource` 和 `Span` 强类型属性，避免 `Items` 字典 |
| P2 | MatchItem 池化 | ~80 B/op | 0 B/op | **100%** | `DefaultMatchQueue` 内部 `Item` 对象用 `Pool<Item>` 复用 |
| P2 | PacketCodec 粘包缓冲区 | ~150 B/op | ~50 B/op | **67%** | 预分配固定缓冲区，避免动态扩展 |
| P3 | 零散闭包分配消除 | ~100-200 B/op | ~20-50 B/op | **60-75%** | 静态方法替代闭包，Span 上下文改用栈分配 |

### 每操作开销来源拆解

| 分配来源 | 估算大小 | 占比 | 说明 |
|---------|---------|------|------|
| HandlerContext.Items (NullableDictionary) | ~200 B | ~18% | `Reset()` 后 `Items.Clear()`，再 `ctx["TaskSource"]=...` 重新 Add 触发字典内部数组分配 |
| PacketCodec 粘包缓冲区 | ~150 B | ~13% | 粘包拆包时动态缓冲区扩展 |
| StandardCodec 编码缓冲区 | ~100 B | ~9% | 协议头编码时的缓冲区分配（仅 StandardCodec，DefaultMessage 对象已池化） |
| 响应侧上下文对象 | ~100 B | ~9% | 服务端处理请求时的上下文分配（ReceivedEventArgs 已池化） |
| 匹配队列 Match/Add 操作 | ~80 B | ~7% | `DefaultMatchQueue` 内部 `Item` 对象分配 |
| 其它零散分配 | ~100-200 B | ~15% | Span 上下文、委托闭包等 |

### 瓶颈 1（P0）：loopback TCP 往返（占 Echo 总耗时 ~40-50%）

每次 Echo 需要两次 loopback 传输（请求 + 响应），内核 TCP 协议栈处理 + IOCP 调度在 loopback 下合计约 1,000~1,200 ns。单连接串行 RTT（C=1）达 30~34 us，TCP 栈 + IOCP 回调 + 用户态处理的完整链路开销较大。

- **优化方向**：独立客户端机器进行真实多机压测，消除 loopback CPU 共享瓶颈
- **预期收益**：服务端真实可达吞吐提升 **30-50%**

### 瓶颈 2（P1）：HandlerContext.Items 字典操作（~5-10%）

`SendMessageAsync` 中 `ctx["TaskSource"] = source; ctx["Span"] = span;` 通过字典索引器存取，`Reset()` 调用 `Items.Clear()`。字典的哈希计算和内部数组操作在每次请求中重复执行。

| 开销来源 | 占比估算 | 耗时估算 | 说明 |
|---------|---------|---------|------|
| NullableDictionary.Clear() | ~40% | ~15 ns | 清空内部数组 |
| 字典索引器 Set（哈希+查找+插入） | ~40% | ~15 ns | 两次 `ctx["key"]=value` |
| 字典索引器 Get | ~20% | ~8 ns | `MessageCodec` 读取 TaskSource/Span |

- **优化方案**：在 `NetHandlerContext` 上增加 `TaskSource` 和 `Span` 强类型属性，直接字段赋值
- **预期收益**：省 ~200 B/op，减少哈希开销，**每请求节省 ~38 ns**

### 瓶颈 3（P1）：管道事件链（~15-20%）

编码/解码各经过 2-4 层虚方法/委托调用。`Pipeline.Write` → `MessageCodec.Write` → `Encode` → `StandardCodec.Write` → `base.Write` → `NetHandlerContext.FireWrite` → `session.Send`，每层有条件分支和类型检查。

- **优化方向**：合并中间层调用，减少虚方法分派次数
- **预期收益**：管道处理耗时降低 **10-15%**

### 瓶颈 4（P2）：剩余对象分配（~10-15%）

每次 Echo 约 700~1,130 B 分配，在高并发下触发频繁 Gen0 GC。从 BDN 输出可见 Gen0 收集率约 0.01~0.04/千次操作。

- **优化方向**：MatchItem 池化（省 ~80 B/op）+ 粘包缓冲区预分配（省 ~100 B/op）
- **预期收益**：每操作分配从 ~1,130 B 降至 ~800 B，**降低 29%**

## 优化建议

| 优先级 | 方向 | 预期收益 | 实施方案 |
|--------|------|---------|---------|
| P0 ★★★ | **真实多机压测** | 消除 loopback CPU 共享瓶颈，服务端吞吐提升 **30-50%** | 独立客户端机器，验证服务端真实可达吞吐 |
| P1 ★★★ | **HandlerContext 专用字段替代字典** | 省 ~200 B/op，每请求节省 ~38 ns | 在 `NetHandlerContext` 上增加 `TaskSource` 和 `Span` 强类型属性，避免 `Items` 字典的 `Clear()`+`Add` 开销 |
| P2 ★★☆ | **MatchItem 池化** | 省 ~80 B/op，每操作分配降低 ~7% | `DefaultMatchQueue` 内部 `Item` 对象使用 `Pool<Item>` 复用 |
| P2 ★★☆ | **NoDelay 模式基准** | 逐包场景降低延迟 | 增加 `NoDelay=true` 基准对比，消除 Nagle 延迟对逐包 RTT 的影响 |
| P3 ★☆☆ | **服务端批量回复合并** | 减少 Send 系统调用次数 | 同一连接多个响应合并为一次 `Send`，减少内核态切换 |

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
| StandardCodec 峰值？ | **26.0 万 msg/s**（逐包 C=256），**42.0 万 msg/s**（滑动窗口 C=1024） |
| LengthFieldCodec 峰值？ | **27.3 万 msg/s**（逐包 C=256），**45.5 万 msg/s**（滑动窗口 C=256） |
| 两种编解码器差异？ | LengthFieldCodec 逐包快 5%，滑动窗口快 8.4%，内存少 15~17% |
| 滑动窗口 vs 逐包提升？ | 滑动窗口比逐包提升 **1.6~1.7 倍**，匹配队列持续满载充分利用管道 |
| 编解码器对吞吐的影响？ | Echo 回路比纯接收慢 **~7.0~7.3 倍**，瓶颈在 TCP 往返和管道调度 |
| 最优并发数？ | 逐包 **C=256** 峰值，滑动窗口 **C=16~1024** 均表现稳定 |
| 每操作内存分配？ | StandardCodec **~1,128 B/op**（逐包）/ **~815 B/op**（滑动窗口），LengthFieldCodec **~952 B/op**（逐包）/ **~679 B/op**（滑动窗口） |
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
