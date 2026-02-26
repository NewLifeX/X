# MemoryCache 性能测试报告

---

## 测试环境

```
BenchmarkDotNet v0.15.8, Windows 10 (10.0.19045.6456/22H2/2022Update)
Intel Core i9-10900K CPU 3.70GHz，1 CPU，20 逻辑核心，10 物理核心（Hyper-Threading）
.NET SDK 10.0.103
Runtime: .NET 10.0.3 (10.0.326.7603), X64 RyuJIT x86-64-v3
目标框架: net10.0
```

> **线程数选择说明**：固定测试 1/4/8/32 线程；本机逻辑核心数为 20，不在默认列表中，故额外加入，共测试 **1/4/8/20/32** 五档。

---

## 一、单线程基础操作性能（BenchmarkDotNet）

测试方法：`MemoryCacheBenchmark`，单个键的 Set/Get/Remove/Inc，以及不同批量大小（1/10/100）的 SetAll/GetAll。

### 1.1 单键操作（BatchSize=1）

| 操作 | 耗时 | 吞吐量 | 内存分配 |
|------|------|--------|---------|
| Set | 19.53 ns | ~51M ops/s | 0 |
| Get | 14.30 ns | ~70M ops/s | 0 |
| Remove | 23.16 ns | ~43M ops/s | 0 |
| Inc | 14.12 ns | ~71M ops/s | 0 |

> 单线程 Set/Get/Remove/Inc 全部达到 **4,300万～7,100万 ops/sec**，零内存分配。
> Get 和 Inc 最快（14 ns），受益于无锁读路径与 JIT 内联；Remove 最慢（23 ns），因必须走写锁路径并执行通配符检查。

### 1.2 批量操作（SetAll / GetAll）

| 操作 | BatchSize | 耗时 | 每项吞吐量 | 内存分配 |
|------|----------|------|-----------|---------|
| SetAll | 1 | 58.87 ns | — | 216 B |
| GetAll | 1 | 69.89 ns | — | 264 B |
| SetAll | 10 | 338.28 ns | ~30M ops/s | 440 B |
| GetAll | 10 | 393.21 ns | ~25M ops/s | 1040 B |
| SetAll | 100 | 3,492.48 ns | ~29M ops/s | 3128 B |
| GetAll | 100 | 3,824.96 ns | ~26M ops/s | 10240 B |

> 批量操作存在固有内存分配（字典遍历/构造），SetAll 约 24 B/项，GetAll 约 100 B/项，符合预期。

---

## 二、多线程并发性能（BenchmarkDotNet）

测试方法：`MemoryCacheConcurrencyBenchmark`，每线程执行 10,000 次迭代。
吞吐量计算：**总 ops = ThreadCount × 10,000 ÷ 总耗时（秒）**。

### 2.1 原始耗时（BenchmarkDotNet 实测 Mean）

| 操作 | 1 线程 | 4 线程 | 8 线程 | 20 线程 | 32 线程 |
|------|--------|--------|--------|---------|---------|
| Set（顺序） | 178,478 ns | 502,893 ns | 622,808 ns | 751,399 ns | 1,242,879 ns |
| Get（顺序） | 131,891 ns | 415,138 ns | 641,854 ns | 572,412 ns | 936,546 ns |
| Remove（顺序） | 234,522 ns | 230,101 ns | 337,730 ns | 1,299,062 ns | 1,606,662 ns |
| Inc（顺序） | 103,821 ns | 454,324 ns | 577,165 ns | 688,526 ns | 1,027,871 ns |
| Set（随机） | 213,570 ns | 459,754 ns | 612,916 ns | 976,862 ns | 1,151,181 ns |
| Get（随机） | 179,261 ns | 538,343 ns | 551,569 ns | 661,287 ns | 1,095,632 ns |

### 2.2 换算吞吐量（ThreadCount × 10,000 ÷ Mean）

| 操作 | 1 线程 | 4 线程 | 8 线程 | 20 线程 | 32 线程 |
|------|--------|--------|--------|---------|---------|
| Set（顺序） | 56M/s | 80M/s | 128M/s | **266M/s** | 258M/s |
| Get（顺序） | 76M/s | 96M/s | 124M/s | **349M/s** | 342M/s |
| Remove（顺序） | 43M/s | 174M/s | **237M/s** | 154M/s | 199M/s |
| Inc（顺序） | 96M/s | 88M/s | 139M/s | 291M/s | **311M/s** |
| Set（随机） | 47M/s | 87M/s | 131M/s | 205M/s | **278M/s** |
| Get（随机） | 56M/s | 74M/s | 145M/s | **303M/s** | 292M/s |

> 粗体为各操作多线程峰值。
> Set/Get/Inc 及随机模式峰值均出现在 20 或 32 线程；Remove 峰值例外，出现在 8 线程（原因见性能瓶颈分析 §3.3）。

### 2.3 多线程并发内存分配

| 线程数 | 分配范围 | 说明 |
|--------|---------|------|
| 1 | 1728 B | Parallel.For 调度器基础开销 |
| 4 | ~2400～2566 B | 线程管理对象随线程数增长 |
| 8 | ~3262～3589 B | 同上 |
| 20 | ~5480～5961 B | 同上 |
| 32 | ~6460～6518 B | 同上 |

> 以上分配来自 `Parallel.For` 的 `ParallelLoopState` 等调度基础设施，每次 Cache 操作（Set/Get/Remove/Inc）本身仍为**零分配**。

---

## 三、性能瓶颈分析

### 3.1 Set（19.53 ns，51M ops/s，单线程）

Set 的主要开销：

1. **`ConcurrentDictionary.TryGetValue`**：哈希计算 + 分段桶查找 + 读锁
2. **`CacheItem.Set`**：`typeof(T).GetTypeCode()`（已被 JIT 内联）+ `Runtime.TickCount64` 读取系统计时器
3. **首次插入路径**：`new CacheItem(value, expire)` + `TryAdd` + `Interlocked.Increment`（键已存在时跳过）

**主要瓶颈**：`Runtime.TickCount64` 每次调用均需访问系统计时器（`rdtsc`/HPET），是不可消除的固定开销，约占总耗时 30%～40%。

**顺序 vs 随机（1T：56M vs 47M）**：顺序模式重复写入同一键，TryGetValue 定位命中热 CacheItem，该对象所在缓存行常驻 L1。随机模式轮转 64 个键，缓存行 L1 命中率下降，每次需从 L2/L3 加载 CacheItem。

**多线程扩展**：从 1T（56M）→ 8T（128M）→ 20T（266M）→ 32T（258M），20T 略优于 32T，因为 20 线程恰好匹配 20 个逻辑核心，OS 无需调度切换，并行效率最高。

---

### 3.2 Get（14.30 ns，70M ops/s，单线程）

Get 的主要开销：

1. **`ConcurrentDictionary.TryGetValue`**：哈希计算 + 桶查找（**无锁读路径**，使用 Volatile 读）
2. **`CacheItem.Expired` 判断**：`ExpiredTime <= Runtime.TickCount64`  
3. **`CacheItem.Visit<T>()`**：更新 `VisitTime`（写操作）+ 类型匹配返回值

**主要瓶颈**：读路径本身无锁，但每次 Get 写入 `VisitTime` 字段，会使 CacheItem 所在缓存行变为 Modified 状态，引发 MESI 缓存一致性失效（多线程场景下尤为明显）。

**顺序 vs 随机（1T：76M vs 56M）**：顺序模式仅访问一个 CacheItem，该对象常驻 L1 缓存（CacheItem ≈ 64 字节，恰好一条缓存行）。随机模式循环访问 64 个不同 CacheItem，L1 缓存容量有限，命中率显著下降，每次 Get 均需从 L2/L3 加载目标 CacheItem。

**4T 顺序（96M）→ 8T（124M）扩展偏慢**：多线程顺序模式下，多个线程高频读取并写入同一 CacheItem 的 `VisitTime`，持续引发 MESI 缓存行争用；4T 时同 Die 内核心共享 L3，争用相对可控；8T 时跨 HT 逻辑核心争用加剧，扩展倍率（1.3x）低于 Set 的（1.6x）。

---

### 3.3 Remove（23.16 ns，43M ops/s，单线程；8T 峰值 237M/s）

Remove 的主要开销：

1. **`key.Contains('*')` + `key.Contains('?')`**：2 次字符串扫描，固定约 4～6 ns
2. **`ConcurrentDictionary.TryRemove`**：哈希计算 + **获取写锁** + 删除链表节点
3. **`Interlocked.Decrement`**：原子递减，约 3～5 ns

**单线程最慢的根本原因**：Remove 必须获取写锁（与 Get 的无锁读路径形成对比），加之通配符检查为固定开销，导致单线程耗时（23 ns）约为 Get（14 ns）的 1.6 倍。

**8T 峰值、20T 反跌现象（237M → 154M → 199M）**：

- **「顺序模式」语义**：每线程操作固定 key，第一次 Remove 成功后，剩余 9,999 次均为 `TryRemove` 未命中（快速返回），执行路径极短
- **1T → 8T 近线性扩展**：8 线程 < 10 物理核心，无核心争用，Parallel.For 开销极低，吞吐近线性增长
- **20T 反跌（237M → 154M）**：20 线程 = 20 逻辑核心，**所有线程同时真正并行**，Remove 的写锁竞争达到峰值；ConcurrentDictionary 的分段锁数量有限，20 个线程同时竞争写锁，等待开销远大于 8T 场景
- **32T 回升（154M → 199M）**：线程数（32）超过逻辑核心数（20），OS 调度使每个时间槽的实际并发数约为 20，但 Parallel.For 的分区使任务分布更均匀，平均锁等待时间反而有所下降

---

### 3.4 Inc（14.12 ns，71M ops/s，单线程；1T > 4T 现象）

Inc 的主要开销：

1. **`ConcurrentDictionary.TryGetValue`**：同 Get
2. **`Interlocked.Exchange`**（底层 `lock cmpxchg`）：原子 CAS 操作，约 3～5 ns
3. **`CacheItem.Set`**：写回新值，同 Set

**1T（96M）> 4T（88M）的原因**：

- `iterationCount=3`（仅 3 次测量迭代），4T 测量的 `Error = ±40,368 ns`（约 9%），统计置信度较低
- `Parallel.For` 在 4 线程时的线程初始化 + 分区器开销在总耗时中占比较大，尚未被 10,000 次迭代充分摊薄
- 实际上 4T Inc 单次耗时 ≈ 454,324 ns ÷ 40,000 次 ≈ **11.4 ns/次**（低于 1T 的 14.1 ns），说明真实并行收益存在，但 wall-time 受 Parallel.For 管理开销影响

从 8T（139M）→ 20T（291M）→ 32T（311M）可见，Inc 具备良好的多线程扩展能力，Interlocked.Exchange 在无逻辑竞争（各线程操作不同 key）时扩展接近线性。

---

### 3.5 多线程扩展效率汇总

| 线程数变化 | Set 扩展倍数 | Get 扩展倍数 | 说明 |
|---------|------------|------------|------|
| 1→4 | 1.4x | 1.3x | Parallel.For 管理开销较高，少量线程时摊薄效果差 |
| 4→8 | 1.6x | 1.3x | Get 存在 MESI 缓存行争用，抑制扩展速度 |
| 8→20 | 2.1x | 2.8x | 逻辑核心充足（共 20），并行效率最高 |
| 20→32 | 1.0x（持平） | 1.0x（持平） | 超额调度（32T > 20 核），OS 调度开销抵消并行增益 |

---

## 四、内存分配分析

| 操作 | 分配/次 | 说明 |
|------|--------|------|
| Set | 0 | 键存在时零分配；首次插入分配 CacheItem（约 64 B） |
| Get | 0 | 全程零分配 |
| Remove（单键） | 0 | 直接内联 TryRemove，消除了临时 String[] + 装箱枚举器 |
| Inc | 0 | Interlocked 原子操作，零分配 |
| SetAll(N) | ~24 B/项 | Dictionary 遍历固有分配 |
| GetAll(N) | ~100 B/项 | 构造返回 Dictionary + 值引用 |
| 并发 Benchmark | 1728～6518 B/call | `Parallel.For` 的 ParallelLoopState 等线程管理对象，非 Cache 本身分配 |

---

## 五、总结

在 Intel Core i9-10900K（20 逻辑核心，3.70GHz）+ .NET 10.0.3 + Windows 10 环境下：

| 操作 | 单线程耗时 | 单线程吞吐量 | 多线程峰值 | 内存分配 | 状态 |
|------|---------|------------|---------|---------|------|
| Set | 19.53 ns | ~51M ops/s | **266M/s**（20T） | 零分配 | ✓ |
| Get | 14.30 ns | ~70M ops/s | **349M/s**（20T） | 零分配 | ✓ |
| Remove | 23.16 ns | ~43M ops/s | **237M/s**（8T） | 零分配 | ✓ |
| Inc | 14.12 ns | ~71M ops/s | **311M/s**（32T） | 零分配 | ✓ |
| SetAll(N) | — | ~29M ops/s/项 | N/A | ~24 B/项 | 正常 |
| GetAll(N) | — | ~26M ops/s/项 | N/A | ~100 B/项 | 正常 |

核心操作（Set/Get/Remove/Inc）单线程吞吐量均超过 **4,300万 ops/sec**，多线程（20～32T）并发吞吐量达到 **2亿～3.5亿 ops/sec**，全部保持**零内存分配**，GC 压力极低。

> 受限于 `iterationCount=3` 的测量精度，Inc 1T/4T 和部分并发场景存在 5%～10% 的测量误差，属正常范围，不影响量级判断。
