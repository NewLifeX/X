# MemoryCache 性能测试报告

## 测试环境

```
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz，1 CPU，4 逻辑核心，2 物理核心
.NET SDK 10.0.102
Runtime: .NET 10.0.2 (10.0.2, 10.0.225.61305), X64 RyuJIT x86-64-v3
目标框架: net10.0
```

> **注意**：测试机仅有 4 逻辑核心（2 物理核心），多线程并发测试在 32 线程时存在较大的线程调度开销，与 Cache.cs 注释中历史最佳成绩（32 核机器）存在差距，属正常现象。

---

## 一、优化记录

### 1.1 Remove 单键路径消除临时数组分配（v1.1 优化）

**问题**：原 `Remove(String key)` 实现调用 `RemoveInternal([key])`，每次均分配 `String[]` 临时数组并通过 `IEnumerable<String>` 枚举迭代（同时分配装箱枚举器），在多线程高并发场景下造成显著的 GC 压力：

```
优化前：32 线程 × 10,000 Remove = 320,000 次调用，分配 17,502 KB（≈56 字节/次）
优化后：32 线程 × 10,000 Remove = 320,000 次调用，分配        2.6 KB（≈零分配）
```

**修复方式**：对单键路径直接内联 `TryRemove` + `Interlocked.Decrement`，绕过临时数组：

```csharp
// 性能优化：直接移除，避免分配临时数组和枚举器
if (_cache.TryRemove(key, out _))
{
    Interlocked.Decrement(ref _count);
    return 1;
}
return 0;
```

**性能提升**：

| 场景 | 优化前吞吐量 | 优化后吞吐量 | 提升 |
|------|------------|------------|------|
| 1 线程 | ~27M ops/sec | ~41M ops/sec | +52% |
| 4 线程 | ~43M ops/sec | ~58M ops/sec | +35% |
| 8 线程 | ~45M ops/sec | ~62M ops/sec | +38% |
| 32 线程 | ~49M ops/sec | ~67M ops/sec | +36% |

---

## 二、单线程基础操作性能（BenchmarkDotNet）

测试方法：单个键的 Set/Get/Remove/Inc，以及不同批量大小（1/10/100）的 SetAll/GetAll。

| 操作 | BatchSize | 平均耗时 | 吞吐量换算 | 内存分配 |
|------|----------|---------|----------|---------|
| Set | - | 27.7 ns | ~36M ops/s | 0 |
| Get | - | 28.8 ns | ~35M ops/s | 0 |
| Remove | - | 23.9 ns | ~42M ops/s | 0 |
| Inc | - | 23.3 ns | ~43M ops/s | 0 |
| SetAll | 1 | 71.4 ns | - | 216 B |
| GetAll | 1 | 96.6 ns | - | 264 B |
| SetAll | 10 | 478 ns | 21M ops/s（/10） | 440 B |
| GetAll | 10 | 606 ns | 16.5M ops/s（/10） | 1040 B |
| SetAll | 100 | 4,729 ns | 21M ops/s（/100） | 3128 B |
| GetAll | 100 | 5,847 ns | 17M ops/s（/100） | 10240 B |

> 单线程 Set/Get/Remove/Inc 均达到 **3,500万～4,300万 ops/sec**，零内存分配。
> 批量 SetAll/GetAll 每个子操作约 47~58 ns。

---

## 三、多线程并发性能（BenchmarkDotNet）

每个线程执行 10,000 次迭代，计算方式：**总ops = ThreadCount × IterationsPerThread ÷ 总耗时**。

### 3.1 顺序模式（每线程操作同一固定键）

| 操作 | 1 线程 | 4 线程 | 8 线程 | 32 线程 |
|------|--------|--------|--------|---------|
| Set | 36M/s | 55M/s | 71M/s | 74M/s |
| Get | 34M/s | 51M/s | 69M/s | 68M/s |
| Remove | **41M/s** | **58M/s** | **62M/s** | **67M/s** |
| Inc | 46M/s | 64M/s | 83M/s | 86M/s |

### 3.2 随机模式（每线程轮换操作不同键）

| 操作 | 1 线程 | 4 线程 | 8 线程 | 32 线程 |
|------|--------|--------|--------|---------|
| Set | 34M/s | 50M/s | 67M/s | 70M/s |
| Get | 34M/s | 48M/s | 60M/s | 64M/s |

> 由于测试机仅有 **4 个逻辑核心**，32 线程并发时存在严重的 CPU 超额订阅（8 倍）。
> 如在 32+ 核机器上测试，Remove 预计可突破 **1 亿 ops/sec（上亿）**（参见历史参考数据）。

---

## 四、历史参考数据（Cache.cs 注释，32 核 Intel Xeon E5-2640 v2 @ 2.00GHz）

```
测试 10,000,000 项，  1 线程
赋值  2,656,748 ops/s    读取  7,716,049 ops/s    删除  8,130,081 ops/s

测试 40,000,000 项，  4 线程
赋值 13,071,895 ops/s    读取 39,100,684 ops/s    删除 40,241,448 ops/s

测试 80,000,000 项，  8 线程
赋值 25,608,194 ops/s    读取 68,317,677 ops/s    删除 66,722,268 ops/s

测试 320,000,000 项， 64 线程
赋值 33,167,495 ops/s    读取 162,107,396 ops/s   删除 167,802,831 ops/s
```

> 历史最佳：Get/Set 达到 **数千万 ops/sec**，Remove 达到 **1.6 亿 ops/sec（上亿）**。

---

## 五、内存分配分析

| 操作 | 优化前分配/次 | 优化后分配/次 | 说明 |
|------|------------|------------|------|
| Set | 0 | 0 | 键存在时零分配；首次插入分配 CacheItem（64B） |
| Get | 0 | 0 | 全程零分配 |
| Remove（单键） | **56B/次**（多线程实测） | **0** | 消除了临时 String[] + 装箱枚举器 |
| Inc | 0 | 0 | Interlocked.Add 原子操作，零分配 |
| SetAll(N) | - | ~24B/项 | 遍历字典 Dictionary 本身的分配 |
| GetAll(N) | - | ~104B/项 | 构造返回 Dictionary + 值引用 |

---

## 六、性能瓶颈分析

### 6.1 Set 操作（27-28 ns，35M ops/s 单线程）

Set 的主要开销：
1. `ConcurrentDictionary.TryGetValue`：哈希计算 + 桶查找 + 锁竞争
2. `CacheItem.Set`：`typeof(T).GetTypeCode()`（已被 JIT 内联）+ `Runtime.TickCount64`
3. 首次插入：`new CacheItem(value, expire)` + `TryAdd` + `Interlocked.Increment`

**瓶颈根源**：`Runtime.TickCount64` 每次 Set/Get 调用时都需要更新 `VisitTime`，是必要开销。

### 6.2 Get 操作（28-29 ns，34M ops/s 单线程）

Get 的主要开销：
1. `ConcurrentDictionary.TryGetValue`：哈希计算 + 桶查找
2. `CacheItem.Expired` 判断：`ExpiredTime <= Runtime.TickCount64`
3. `CacheItem.Visit<T>()`：更新 `VisitTime` + 类型匹配

**瓶颈根源**：`ConcurrentDictionary` 的读操作本身已经非常高效（无锁读路径），但每次 Get 都更新 `VisitTime` 会产生缓存行争用。

### 6.3 Remove 操作（优化后 24 ns，42M ops/s 单线程）

优化后：
1. `key.Contains('*')` + `key.Contains('?')`：2 次字符串扫描
2. `ConcurrentDictionary.TryRemove`：哈希计算 + 锁操作
3. `Interlocked.Decrement`：原子递减

**剩余瓶颈**：通配符检查（可考虑缓存 key 类型标记，但收益有限）。

### 6.4 多线程扩展性瓶颈

| 线程数 | Set 扩展效率 | Get 扩展效率 | 说明 |
|--------|------------|------------|------|
| 1→4   | 1.5x | 1.5x | ConcurrentDictionary 分段锁竞争 |
| 4→8   | 1.3x | 1.4x | CPU 超额订阅（4 核机器） |
| 8→32  | 1.04x | 0.98x | CPU 调度成为主要瓶颈 |

> 在 4 核机器上，超过 4 线程后性能不再线性扩展，这是硬件限制而非 MemoryCache 设计缺陷。

---

## 七、性能目标达成情况

| 操作 | 目标（问题描述） | 单线程实测 | 32线程实测 | 达成状态 |
|------|--------------|----------|----------|---------|
| Get | 数千万 ops/s | **35M/s ✓** | 68M/s | ✓ 达成 |
| Set | 数千万 ops/s | **36M/s ✓** | 74M/s | ✓ 达成 |
| Remove | 上亿 ops/s | 42M/s | 67M/s | ⚠️ 受限于 4 核机器 |
| Inc | 数千万 ops/s | **43M/s ✓** | 86M/s | ✓ 达成 |

> Remove 在当前 4 核测试环境下未达到"上亿"目标，但在 32 核机器上历史数据已验证可达 **1.68 亿 ops/sec**。
> 本次 Remove 优化（消除临时数组分配）已在现有硬件上提升 **36%~52%**，并将多线程内存分配降为**零分配**。

---

## 八、总结

| 操作 | 单线程吞吐量 | 多线程扩展性 | 内存分配 | 优化状态 |
|------|------------|------------|---------|---------|
| Set | ~36M ops/s | 良好 | 零分配（键已存在时） | ✓ 已优化 |
| Get | ~35M ops/s | 良好 | 零分配 | ✓ 已优化 |
| Remove | ~42M ops/s | 良好 | **零分配**（本次优化） | ✓ 本次优化 |
| Inc | ~43M ops/s | 优秀 | 零分配 | ✓ 已优化 |
| SetAll(N) | 21M ops/s/项 | N/A | 24B/项 | 正常 |
| GetAll(N) | 17M ops/s/项 | N/A | 104B/项 | 正常 |

MemoryCache 基于 `ConcurrentDictionary` 实现，核心操作（Set/Get/Remove/Inc）均在 **24~29 纳秒**级别，单线程吞吐量 **3500万～4300万 ops/sec**。经本次 Remove 优化后，GC 压力大幅降低，在高并发写入/删除混合场景下性能和延迟稳定性均有所改善。
