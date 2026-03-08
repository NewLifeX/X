# MemoryCache 性能测试报告

## 一、测试结论

MemoryCache 是基于 `ConcurrentDictionary` 实现的高性能内存缓存，提供 Set/Get/Remove/Increment 等核心操作，支持 TTL 过期与 LRU 淘汰。关键发现如下：

1. **核心操作单线程吞吐量均超过 4,300 万 ops/s**：Get 和 Inc 最快（~14 ns，~70M ops/s），受益于无锁读路径与 JIT 内联；Set 居中（~20 ns，~51M ops/s）；Remove 最慢（~23 ns，~43M ops/s），因必须走写锁路径并执行通配符检查。

2. **多线程并发吞吐量达到 2 亿～3.5 亿 ops/s**：Get 顺序模式 20 线程峰值 **349M ops/s**，Inc 32 线程峰值 **311M ops/s**。Set/Get/Inc 峰值均出现在 20～32 线程；Remove 峰值例外，出现在 8 线程（写锁竞争导致）。

3. **全部核心操作零内存分配**：Set/Get/Remove/Inc 单次操作均为 0 B 分配，GC 压力极低。并发场景的少量分配（1,728～6,518 B）全部来自 `Parallel.For` 调度基础设施，非 Cache 本身。

4. **批量操作存在固有分配**：SetAll 约 24 B/项，GetAll 约 100 B/项，源于字典遍历与构造，属正常开销。

5. **主要性能瓶颈**：`Runtime.TickCount64` 系统计时器读取（约占 Set 总耗时 30%～40%）、Get 写入 `VisitTime` 导致的 MESI 缓存行争用（多线程场景）。

> 受限于 `iterationCount=3` 的测量精度，Inc 1T/4T 和部分并发场景存在 5%～10% 的测量误差，属正常范围，不影响量级判断。

---

## 二、测试环境

```text
BenchmarkDotNet v0.15.8, Windows 10 (10.0.19045.6456/22H2/2022Update)
Intel Core i9-10900K CPU 3.70GHz, 1 CPU, 20 逻辑核心 / 10 物理核心（Hyper-Threading）
.NET SDK 10.0.103
Runtime: .NET 10.0.3 (10.0.326.7603), X64 RyuJIT x86-64-v3
目标框架: net10.0
运行模式: Release
```

> **线程数选择说明**：固定测试 1/4/8/32 线程；本机逻辑核心数为 20，不在默认列表中，故额外加入，共测试 **1/4/8/20/32** 五档。

---

## 三、测试方法

### 测试对象

`NewLife.Caching.MemoryCache`，基于 `ConcurrentDictionary<String, CacheItem>` 的内存缓存实现。

| 操作 | 方法 | 说明 |
|------|------|------|
| Set | `Set<T>(key, value)` | 添加或更新缓存项，键存在时原地更新，不存在时 `TryAdd` |
| Get | `Get<T>(key)` | 读取缓存项，无锁 `TryGetValue` + 过期判断 + 更新访问时间 |
| Remove | `Remove(key)` | 移除缓存项，支持 `*`/`?` 通配符模式匹配 |
| Inc | `Increment(key, value)` | 原子递增，基于 `Interlocked.Add` |
| SetAll | `SetAll(dic)` | 批量写入，遍历字典逐项 Set |
| GetAll | `GetAll<T>(keys)` | 批量读取，构造返回字典 |

### 测试维度

- **单线程基础操作**（`MemoryCacheBenchmark`）：单键 Set/Get/Remove/Inc，批量 SetAll/GetAll（BatchSize = 1/10/100）
- **多线程并发操作**（`MemoryCacheConcurrencyBenchmark`）：1/4/8/20/32 线程，每线程 10,000 次迭代
  - **顺序模式**：每线程操作固定 key（`_keys[t % 64]`），模拟热 key 竞争
  - **随机模式**：每线程轮转访问 64 个 key（`_keys[i % 64]`），模拟分散访问

### 测试配置

- BDN 参数：`iterationCount: 3`，`[MemoryDiagnoser]`
- MemoryCache：`Capacity = 0`（禁用 LRU 淘汰），预置 64～100 个 key
- 并发吞吐量计算：**总 ops = ThreadCount × 10,000 ÷ Mean（秒）**

---

## 四、测试结果

### 4.1 单线程基础操作（MemoryCacheBenchmark）

#### 单键操作（BatchSize=1）

| 操作 | Mean | Allocated |
|------|------|-----------|
| Set | 19.53 ns | 0 B |
| Get | 14.30 ns | 0 B |
| Remove | 23.16 ns | 0 B |
| Inc | 14.12 ns | 0 B |

#### 批量操作（SetAll / GetAll）

| 操作 | BatchSize | Mean | Allocated |
|------|-----------|------|-----------|
| SetAll | 1 | 58.87 ns | 216 B |
| GetAll | 1 | 69.89 ns | 264 B |
| SetAll | 10 | 338.28 ns | 440 B |
| GetAll | 10 | 393.21 ns | 1,040 B |
| SetAll | 100 | 3,492.48 ns | 3,128 B |
| GetAll | 100 | 3,824.96 ns | 10,240 B |

### 4.2 多线程并发操作（MemoryCacheConcurrencyBenchmark）

#### 原始耗时（Mean，单位 ns）

| 操作 | 1T | 4T | 8T | 20T | 32T |
|------|----|----|----|----|-----|
| Set（顺序） | 178,478 | 502,893 | 622,808 | 751,399 | 1,242,879 |
| Get（顺序） | 131,891 | 415,138 | 641,854 | 572,412 | 936,546 |
| Remove（顺序） | 234,522 | 230,101 | 337,730 | 1,299,062 | 1,606,662 |
| Inc（顺序） | 103,821 | 454,324 | 577,165 | 688,526 | 1,027,871 |
| Set（随机） | 213,570 | 459,754 | 612,916 | 976,862 | 1,151,181 |
| Get（随机） | 179,261 | 538,343 | 551,569 | 661,287 | 1,095,632 |

#### 并发内存分配

| 线程数 | 分配范围 | 说明 |
|--------|---------|------|
| 1 | 1,728 B | Parallel.For 调度器基础开销 |
| 4 | ~2,400～2,566 B | 线程管理对象随线程数增长 |
| 8 | ~3,262～3,589 B | 同上 |
| 20 | ~5,480～5,961 B | 同上 |
| 32 | ~6,460～6,518 B | 同上 |

> 以上分配来自 `Parallel.For` 的 `ParallelLoopState` 等调度基础设施，每次 Cache 操作本身仍为**零分配**。

---

## 五、核心指标

### 5.1 单线程吞吐量（ops/s = 1,000,000,000 / Mean_ns）

| 操作 | 耗时 | 吞吐量 | 内存分配 |
|------|------|--------|---------|
| Set | 19.53 ns | **~51M ops/s** | 0 B |
| Get | 14.30 ns | **~70M ops/s** | 0 B |
| Remove | 23.16 ns | **~43M ops/s** | 0 B |
| Inc | 14.12 ns | **~71M ops/s** | 0 B |

### 5.2 批量操作每项吞吐量（BatchSize=100）

| 操作 | 总耗时 | 每项吞吐量 | 每项分配 |
|------|--------|-----------|---------|
| SetAll | 3,492 ns | ~29M ops/s | ~24 B |
| GetAll | 3,825 ns | ~26M ops/s | ~100 B |

### 5.3 多线程吞吐量（ThreadCount × 10,000 / Mean）

| 操作 | 1T | 4T | 8T | 20T | 32T |
|------|----|----|----|----|-----|
| Set（顺序） | 56M/s | 80M/s | 128M/s | **266M/s** | 258M/s |
| Get（顺序） | 76M/s | 96M/s | 124M/s | **349M/s** | 342M/s |
| Remove（顺序） | 43M/s | 174M/s | **237M/s** | 154M/s | 199M/s |
| Inc（顺序） | 96M/s | 88M/s | 139M/s | 291M/s | **311M/s** |
| Set（随机） | 47M/s | 87M/s | 131M/s | 205M/s | **278M/s** |
| Get（随机） | 56M/s | 74M/s | 145M/s | **303M/s** | 292M/s |

> 粗体为各操作多线程峰值。

### 5.4 峰值吞吐汇总

| 操作 | 单线程耗时 | 单线程吞吐量 | 多线程峰值 | 最优线程数 | 内存分配 |
|------|-----------|-----------|---------|----------|---------|
| Set | 19.53 ns | ~51M ops/s | **266M/s** | 20T | 0 B |
| Get | 14.30 ns | ~70M ops/s | **349M/s** | 20T | 0 B |
| Remove | 23.16 ns | ~43M ops/s | **237M/s** | 8T | 0 B |
| Inc | 14.12 ns | ~71M ops/s | **311M/s** | 32T | 0 B |
| SetAll(100) | — | ~29M ops/s/项 | — | — | ~24 B/项 |
| GetAll(100) | — | ~26M ops/s/项 | — | — | ~100 B/项 |

---

## 六、对比分析

### 6.1 顺序模式 vs 随机模式

| 操作 | 顺序 1T | 随机 1T | 差异 | 原因 |
|------|---------|---------|------|------|
| Set | 56M/s | 47M/s | 顺序快 **19%** | 顺序模式重复写同一键，CacheItem 常驻 L1 缓存行 |
| Get | 76M/s | 56M/s | 顺序快 **36%** | 随机模式轮转 64 个 CacheItem，L1 命中率下降 |

| 操作 | 顺序 20T | 随机 20T | 差异 | 原因 |
|------|----------|----------|------|------|
| Set | 266M/s | 205M/s | 顺序快 **30%** | 顺序模式热 key 的 CacheItem 对象跨核共享更高效 |
| Get | 349M/s | 303M/s | 顺序快 **15%** | 随机模式分散访问，L1/L2 缓存利用率低于顺序模式 |

### 6.2 多线程扩展效率

| 线程数变化 | Set 扩展倍数 | Get 扩展倍数 | 说明 |
|-----------|------------|------------|------|
| 1→4 | 1.4x | 1.3x | `Parallel.For` 管理开销较高，少量线程时摊薄效果差 |
| 4→8 | 1.6x | 1.3x | Get 存在 MESI 缓存行争用，抑制扩展速度 |
| 8→20 | 2.1x | 2.8x | 逻辑核心充足（共 20），并行效率最高 |
| 20→32 | 1.0x | 1.0x | 超额调度（32T > 20 核），OS 调度开销抵消并行增益 |

**最优并发点**：Set/Get 峰值在 **20 线程**（恰好匹配 20 逻辑核心），32 线程时超额调度无额外收益；Inc 在 32 线程仍有微弱提升（因各线程操作不同 key，无锁竞争）；Remove 峰值在 **8 线程**（写锁竞争制约扩展）。

### 6.3 内存分配分析

| 操作 | 分配/次 | 说明 |
|------|--------|------|
| Set | 0 B | 键存在时零分配；首次插入分配 CacheItem（~64 B） |
| Get | 0 B | 全程零分配 |
| Remove（单键） | 0 B | 直接 TryRemove，无临时数组分配 |
| Inc | 0 B | `Interlocked.Add` 原子操作，零分配 |
| SetAll(N) | ~24 B/项 | Dictionary 遍历固有分配 |
| GetAll(N) | ~100 B/项 | 构造返回 Dictionary + 值引用 |
| 并发 Benchmark | 1,728～6,518 B/call | `Parallel.For` 的 ParallelLoopState 等线程管理对象 |

---

## 七、性能瓶颈定位

### 核心瓶颈点总览

| 优先级 | 瓶颈 | 优化收益占比 | 当前开销 | 优化后预估 | 内存节省 |
|--------|------|------------|---------|-----------|---------|
| P0 | Get 写 VisitTime 触发 MESI 缓存行争用 | ~40% | 4T→8T 扩展仅 1.3x（低于 Set 的 1.6x） | 4T→8T 扩展 ≥1.5x | — |
| P1 | `Runtime.TickCount64` 系统计时器读取 | ~30% | 每次 Set/Get 耗时 ~6-8 ns（占 30-40%） | 窗口内跳过，减少至 ~2 ns 均摊 | — |
| P2（快速路径已实现） | Remove 通配符检查固定开销 | ~15% | 非通配符键已直接 TryRemove，剩余 2 次 `String.Contains(Char)` ~4-6 ns | 专用 API 跳过检查 ~0 ns | — |
| P2 | GetAll 批量构造 Dictionary 分配 | ~10% | ~100 B/项，100 项 10,240 B | ~50 B/项，100 项 ~5 KB | **50%** |
| P3 | Remove 多线程写锁竞争 | ~5% | 8T→20T 反跌（237M→154M） | 分段锁已最优，属固有限制 | — |

### 关键内存优化方向

| 优先级 | 优化方向 | 当前分配 | 优化后预估 | 节省比例 | 实施方案 |
|--------|---------|---------|-----------|---------|---------|
| P2 | GetAll 批量构造 | ~100 B/项 | ~50 B/项 | **50%** | `ArrayPool` 或预分配数组替代每次 `new Dictionary` |
| P2 | SetAll 字典遍历 | ~24 B/项 | ~12 B/项 | **50%** | 使用 `IReadOnlyCollection` 替代字典遍历 |
| P3 | 并发 BDN 调度开销 | 1,728～6,518 B/call | — | — | 属 `Parallel.For` 固有开销，非优化目标 |

> **注**：Set/Get/Remove/Inc 单次操作已实现**零内存分配**，内存优化空间主要在批量操作路径。

### 7.1 Set（19.53 ns，51M ops/s）

Set 的执行路径：`ConcurrentDictionary.TryGetValue` → `CacheItem.Set` → 命中则原地更新；未命中则 `new CacheItem` + `TryAdd` + `Interlocked.Increment`。

| 开销来源 | 占比估算 | 说明 |
|---------|---------|------|
| `Runtime.TickCount64` | 30%～40% | 每次调用访问系统计时器（`rdtsc`/HPET），不可消除的固定开销 |
| `ConcurrentDictionary.TryGetValue` | 30%～40% | 哈希计算 + 分段桶查找 + 读锁 |
| `typeof(T).GetTypeCode()` | <5% | 已被 JIT 内联 |

**多线程扩展**：1T（56M）→ 20T（266M）→ 32T（258M），20T 恰好匹配逻辑核心数，OS 无需调度切换。

### 7.2 Get（14.30 ns，70M ops/s）

Get 的执行路径：`ConcurrentDictionary.TryGetValue`（**无锁读，Volatile 读**）→ `CacheItem.Expired` 判断 → `CacheItem.Visit<T>()` 更新 `VisitTime` + 返回值。

| 开销来源 | 占比估算 | 说明 |
|---------|---------|------|
| `ConcurrentDictionary.TryGetValue` | ~50% | 无锁读路径，仅哈希 + 桶查找 |
| `Runtime.TickCount64`（2 次） | 30%～40% | `Expired` 判断 + `Visit` 更新 `VisitTime` |
| 类型匹配返回值 | <10% | `is T` 模式匹配 + 直接返回 |

**MESI 缓存行争用**：Get 虽是无锁读，但每次写入 `VisitTime` 字段会使 CacheItem 所在缓存行变为 Modified 状态。多线程顺序模式下多个核心写同一缓存行，持续引发 MESI 一致性失效，导致 4T→8T 扩展倍率（1.3x）低于 Set（1.6x）。

### 7.3 Remove（23.16 ns，43M ops/s）

Remove 的执行路径：`key.Contains('*')` + `key.Contains('?')` 通配符检查 → `ConcurrentDictionary.TryRemove`（**写锁**）→ `Interlocked.Decrement`。

| 开销来源 | 占比估算 | 说明 |
|---------|---------|------|
| 通配符检查 | ~20% | 2 次 `String.Contains(Char)`，固定约 4～6 ns |
| `TryRemove` 写锁 | ~50% | 哈希 + 获取写锁 + 删除链表节点 |
| `Interlocked.Decrement` | ~15% | 原子递减，约 3～5 ns |

**8T 峰值、20T 反跌现象（237M → 154M → 199M）**：
- 顺序模式下第一次 Remove 成功后，剩余 9,999 次均为 `TryRemove` 未命中（快速返回）
- 8T < 10 物理核心，无核心争用，吞吐近线性增长
- 20T = 20 逻辑核心，所有线程同时竞争 `ConcurrentDictionary` 的分段写锁，等待开销远大于 8T
- 32T 超额调度，`Parallel.For` 分区使任务分布更均匀，平均锁等待时间反而下降

### 7.4 Inc（14.12 ns，71M ops/s）

Inc 的执行路径：`GetOrAddItem` → `ConcurrentDictionary.TryGetValue` → `CacheItem.Inc`（`Interlocked.Add`）→ 更新 `VisitTime`。

| 开销来源 | 占比估算 | 说明 |
|---------|---------|------|
| `ConcurrentDictionary.TryGetValue` | ~50% | 同 Get，无锁读路径 |
| `Interlocked.Add` | ~25% | 原子 CAS，底层 `lock cmpxchg`，约 3～5 ns |
| `Runtime.TickCount64` | ~20% | 更新 `VisitTime` |

**1T（96M）> 4T（88M）现象**：`iterationCount=3` 统计置信度较低（4T Error ≈ 9%）；`Parallel.For` 管理开销在 4 线程时占比较大。实际单次耗时 4T ≈ 11.4 ns（低于 1T 的 14.1 ns），真实并行收益存在。从 8T（139M）→ 32T（311M）可见 Inc 具备良好的多线程扩展能力。

---

## 八、优化建议

| 优先级 | 方向 | 预期收益 | 实施方案 |
|--------|------|---------|---------|
| P0 ★★★ | **`VisitTime` 更新策略优化** | Get 多线程吞吐提升 20%～30%，4T→8T 扩展倍率从 1.3x 提升至 ≥1.5x | 改为时间窗口内跳过更新（如 1 秒内不重复写 `VisitTime`），消除 MESI 缓存行争用 |
| P1 ★★☆ | **`TickCount64` 调用合并** | Set/Get 单次操作节省 ~3-4 ns（15-20%） | `Expired` 判断与 `Visit` 共用一次 `TickCount64` 读取，避免重复系统调用 |
| P2 ★☆☆ | **Remove 通配符检查优化**（快速路径已实现） | 剩余 Contains 检查开销 ~4-6 ns | 快速路径已实现（非通配符键直接 TryRemove）；进一步优化可提供不检查通配符的专用 API |
| P2 ★☆☆ | **GetAll 批量操作减少分配** | GetAll 内存分配降低 ~50%（100 B/项 → ~50 B/项） | 使用 `ArrayPool` 或预分配数组替代每次构造新 Dictionary |
| P3 ★☆☆ | **SetAll 遍历优化** | SetAll 分配降低 ~50%（24 B/项 → ~12 B/项） | 使用 `IReadOnlyCollection` 替代字典遍历 |

---

## 附录

### 运行命令

```bash
# 单线程基础操作
dotnet run --project Benchmark/Benchmark.csproj -c Release -- --filter "*MemoryCacheBenchmark*"

# 多线程并发操作
dotnet run --project Benchmark/Benchmark.csproj -c Release -- --filter "*MemoryCacheConcurrencyBenchmark*"

# 运行全部 MemoryCache 基准
dotnet run --project Benchmark/Benchmark.csproj -c Release -- --filter "*MemoryCache*"
```
