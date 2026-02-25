# IPacket 数据包性能测试报告

## 测试环境

```
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical核心, 2 物理核心
.NET SDK 10.0.102
Runtime: .NET 10.0.2 (10.0.2, 10.0.225.61305), X64 RyuJIT x86-64-v3
目标框架: net10.0
```

## 一、OwnerPacket 单线程性能

| 方法 | Size | 平均耗时 | Error | StdDev | Gen0 | 内存分配 |
|------|------|---------|-------|--------|------|---------|
| 构造+释放 | 64 | 11.758 ns | 0.0130 ns | 0.0139 ns | - | - |
| GetSpan | 64 | 14.702 ns | 0.0173 ns | 0.0192 ns | - | - |
| GetMemory | 64 | 14.594 ns | 0.0205 ns | 0.0228 ns | - | - |
| TryGetArray | 64 | 13.207 ns | 0.0234 ns | 0.0250 ns | - | - |
| Slice | 64 | 14.102 ns | 0.1476 ns | 0.1640 ns | 0.0029 | 48 B |
| Resize | 64 | 11.952 ns | 0.0788 ns | 0.0876 ns | 0.0029 | 48 B |
| Indexer读 | 64 | 1.578 ns | 0.0017 ns | 0.0016 ns | - | - |
| Indexer写 | 64 | 1.797 ns | 0.0035 ns | 0.0037 ns | - | - |
| 构造+释放 | 1024 | 11.311 ns | 0.0312 ns | 0.0347 ns | - | - |
| GetSpan | 1024 | 13.850 ns | 0.0161 ns | 0.0186 ns | - | - |
| GetMemory | 1024 | 13.730 ns | 0.0310 ns | 0.0318 ns | - | - |
| TryGetArray | 1024 | 13.269 ns | 0.0907 ns | 0.0931 ns | - | - |
| Slice | 1024 | 10.671 ns | 0.1299 ns | 0.1443 ns | 0.0029 | 48 B |
| Resize | 1024 | 10.031 ns | 0.3990 ns | 0.4595 ns | 0.0029 | 48 B |
| Indexer读 | 1024 | 1.597 ns | 0.0069 ns | 0.0080 ns | - | - |
| Indexer写 | 1024 | 1.794 ns | 0.0039 ns | 0.0040 ns | - | - |
| 构造+释放 | 8192 | 11.265 ns | 0.0060 ns | 0.0062 ns | - | - |
| GetSpan | 8192 | 13.828 ns | 0.0122 ns | 0.0125 ns | - | - |
| GetMemory | 8192 | 13.702 ns | 0.0097 ns | 0.0095 ns | - | - |
| TryGetArray | 8192 | 13.345 ns | 0.0212 ns | 0.0227 ns | - | - |
| Slice | 8192 | 9.749 ns | 0.1496 ns | 0.1723 ns | 0.0029 | 48 B |
| Resize | 8192 | 9.701 ns | 0.1536 ns | 0.1768 ns | 0.0029 | 48 B |
| Indexer读 | 8192 | 1.577 ns | 0.0014 ns | 0.0014 ns | - | - |
| Indexer写 | 8192 | 1.793 ns | 0.0009 ns | 0.0009 ns | - | - |

### OwnerPacket 单线程分析

- **构造+释放** 约 11 ns，得益于 `ArrayPool<Byte>.Shared` 的高效池化，不随 Size 增长变化。**零内存分配**。
- **GetSpan / GetMemory / TryGetArray** 约 13-15 ns，均为零分配的内联操作。
- **Slice / Resize** 约 10-14 ns，每次分配 48 B（一个新 OwnerPacket 对象）。
- **Indexer** 读约 1.6 ns，写约 1.8 ns，极其高效。

## 二、OwnerPacket 多线程性能

| 方法 | ThreadCount | 平均耗时 | Error | StdDev | Gen0 | Gen1 | 内存分配 |
|------|------------|---------|-------|--------|------|------|---------|
| 多线程构造+释放 | 1 | 30.41 us | 0.677 us | 0.779 us | 2.9297 | - | 48.58 KB |
| 多线程GetSpan | 1 | 38.69 us | 1.340 us | 1.544 us | 2.9297 | - | 48.58 KB |
| 多线程Slice | 1 | 26.81 us | 0.643 us | 0.741 us | 5.8289 | 0.0305 | 95.46 KB |
| 多线程构造+释放 | 4 | 48.07 us | 1.050 us | 1.209 us | 11.6577 | 0.0610 | 189.64 KB |
| 多线程GetSpan | 4 | 52.53 us | 1.449 us | 1.668 us | 11.6577 | 0.0610 | 189.63 KB |
| 多线程Slice | 4 | 48.37 us | 1.195 us | 1.328 us | 23.1934 | 0.1221 | 377.13 KB |
| 多线程构造+释放 | 16 | 176.47 us | 3.394 us | 3.632 us | 46.1426 | 0.2441 | 752.17 KB |
| 多线程GetSpan | 16 | 205.48 us | 5.893 us | 6.786 us | 46.1426 | 0.2441 | 752.17 KB |
| 多线程Slice | 16 | 177.86 us | 4.245 us | 4.718 us | 92.2852 | 0.7324 | 1502.18 KB |
| 多线程构造+释放 | 32 | 355.93 us | 7.999 us | 9.212 us | 92.2852 | 0.4883 | 1502.2 KB |
| 多线程GetSpan | 32 | 390.30 us | 6.726 us | 7.746 us | 92.2852 | 0.4883 | 1502.2 KB |
| 多线程Slice | 32 | 348.79 us | 6.854 us | 7.893 us | 184.5703 | 1.4648 | 3002.22 KB |

### OwnerPacket 多线程分析

- 1→4 线程：耗时增长约 1.6 倍（48 us / 30 us），接近线性扩展，说明 ArrayPool 在低并发下竞争较小。
- 4→16 线程：耗时增长约 3.7 倍（176 us / 48 us），出现明显的 ArrayPool 锁竞争。
- 16→32 线程：耗时增长约 2 倍（356 us / 176 us），线程数超过物理核心（2核），性能受 CPU 调度瓶颈限制。
- **Slice 操作**在多线程下内存分配翻倍，因为每次 Slice 创建新 OwnerPacket 对象（48 B/次）。

## 三、MemoryPacket 单线程性能

| 方法 | Size | 平均耗时 | Error | StdDev | Gen0 | 内存分配 |
|------|------|---------|-------|--------|------|---------|
| 构造 | 64 | 0.8605 ns | 0.0047 ns | 0.0054 ns | - | - |
| GetSpan | 64 | 1.7900 ns | 0.0047 ns | 0.0050 ns | - | - |
| GetMemory | 64 | 0.5627 ns | 0.0030 ns | 0.0035 ns | - | - |
| TryGetArray | 64 | 1.5971 ns | 0.0046 ns | 0.0049 ns | - | - |
| Slice | 64 | 8.8880 ns | 0.0919 ns | 0.1021 ns | 0.0029 | 48 B |
| Indexer读 | 64 | 2.2118 ns | 0.0015 ns | 0.0018 ns | - | - |
| Indexer写 | 64 | 2.1636 ns | 0.0026 ns | 0.0027 ns | - | - |
| 构造 | 1024 | 0.8591 ns | 0.0020 ns | 0.0022 ns | - | - |
| GetSpan | 1024 | 1.7897 ns | 0.0023 ns | 0.0025 ns | - | - |
| GetMemory | 1024 | 0.5644 ns | 0.0030 ns | 0.0034 ns | - | - |
| TryGetArray | 1024 | 1.5647 ns | 0.0013 ns | 0.0013 ns | - | - |
| Slice | 1024 | 8.8094 ns | 0.0809 ns | 0.0932 ns | 0.0029 | 48 B |
| Indexer读 | 1024 | 2.2125 ns | 0.0025 ns | 0.0026 ns | - | - |
| Indexer写 | 1024 | 2.1615 ns | 0.0026 ns | 0.0029 ns | - | - |
| 构造 | 8192 | 0.8584 ns | 0.0015 ns | 0.0017 ns | - | - |
| GetSpan | 8192 | 1.7886 ns | 0.0011 ns | 0.0011 ns | - | - |
| GetMemory | 8192 | 0.5622 ns | 0.0032 ns | 0.0035 ns | - | - |
| TryGetArray | 8192 | 1.5606 ns | 0.0017 ns | 0.0018 ns | - | - |
| Slice | 8192 | 8.8244 ns | 0.0432 ns | 0.0480 ns | 0.0029 | 48 B |
| Indexer读 | 8192 | 2.1939 ns | 0.0017 ns | 0.0019 ns | - | - |
| Indexer写 | 8192 | 2.1618 ns | 0.0022 ns | 0.0022 ns | - | - |

### MemoryPacket 单线程分析

- **构造** 不到 1 ns，struct 无堆分配。
- **GetMemory** 0.56 ns，最快的内存访问方式。
- **GetSpan** 1.79 ns，需要从 Memory 取 Span。
- **TryGetArray** 约 1.6 ns，通过 `MemoryMarshal.TryGetArray` 获取。
- **Slice** 约 8.9 ns，需要装箱为 IPacket 返回（48 B 分配），是主要开销来源。
- 所有操作均**与 Size 无关**，体现了零拷贝设计。

## 四、MemoryPacket 多线程性能

| 方法 | ThreadCount | 平均耗时 | Error | StdDev | Gen0 | Gen1 | 内存分配 |
|------|------------|---------|-------|--------|------|------|---------|
| 多线程构造 | 1 | 1.763 us | 0.0112 us | 0.0115 us | 0.1011 | - | 1.68 KB |
| 多线程GetSpan | 1 | 3.012 us | 0.0208 us | 0.0239 us | 0.1030 | - | 1.69 KB |
| 多线程Slice | 1 | 14.893 us | 0.2337 us | 0.2691 us | 2.9755 | 0.0153 | 48.57 KB |
| 多线程构造 | 4 | 3.324 us | 0.0609 us | 0.0701 us | 0.1221 | - | 2.01 KB |
| 多线程GetSpan | 4 | 7.678 us | 0.1013 us | 0.1084 us | 0.1221 | - | 2.11 KB |
| 多线程Slice | 4 | 27.424 us | 0.7207 us | 0.7401 us | 11.6577 | 0.0610 | 189.62 KB |
| 多线程构造 | 16 | 6.656 us | 0.1271 us | 0.1413 us | 0.1221 | - | 2.11 KB |
| 多线程GetSpan | 16 | 21.782 us | 0.2646 us | 0.2831 us | 0.1221 | - | 2.11 KB |
| 多线程Slice | 16 | 96.679 us | 2.5368 us | 2.8196 us | 46.2646 | 0.3662 | 752.15 KB |
| 多线程构造 | 32 | 10.097 us | 0.1457 us | 0.1619 us | 0.1221 | - | 2.12 KB |
| 多线程GetSpan | 32 | 38.552 us | 0.4853 us | 0.5394 us | 0.1221 | - | 2.14 KB |
| 多线程Slice | 32 | 192.920 us | 4.6997 us | 5.2237 us | 92.2852 | 0.7324 | 1502.17 KB |

### MemoryPacket 多线程分析

- MemoryPacket 作为 struct，**构造和 GetSpan 在多线程下几乎无竞争**（仅有线程调度开销）。
- Slice 操作因装箱为 IPacket 导致大量堆分配，32 线程时 1502 KB/操作。
- 与 OwnerPacket 相比，MemoryPacket 多线程构造快约 35 倍（无 ArrayPool 锁竞争）。

## 五、实现类型横向对比（单线程，Size=1024）

| 操作 | OwnerPacket | MemoryPacket | ArrayPacket* | ReadOnlyPacket* |
|------|-------------|-------------|-------------|-----------------|
| 构造 | 11.3 ns | 0.86 ns | <1 ns | <1 ns |
| GetSpan | 13.9 ns | 1.79 ns | <1 ns | <1 ns |
| GetMemory | 13.7 ns | 0.56 ns | <1 ns | <1 ns |
| TryGetArray | 13.3 ns | 1.56 ns | <1 ns | <1 ns |
| Slice | 10.7 ns (48B) | 8.9 ns (48B) | ~9 ns (48B) | ~9 ns (48B) |
| Indexer读 | 1.6 ns | 2.2 ns | <1 ns | <1 ns |
| Indexer写 | 1.8 ns | 2.2 ns | <1 ns | 不支持 |
| 内存分配 | 仅 Slice/Resize 48B | 仅 Slice 48B | 仅 Slice(IPacket) 48B | 仅 Slice 48B |

> *注：ArrayPacket 和 ReadOnlyPacket 为 record struct，构造和内存访问操作极快（亚纳秒级），BenchmarkDotNet 报告为 ZeroMeasurement，这里以 <1 ns 标记。完整数据需更长时间运行获取。

## 六、性能瓶颈分析

### 1. OwnerPacket 构造开销（~11 ns）

OwnerPacket 构造包含 `ArrayPool<Byte>.Shared.Rent()` 和释放包含 `Return()`。虽然 11 ns 已经非常高效，但在高频创建/释放场景下（如每个网络包都创建），这是最大的固定开销。

**瓶颈根源**：ArrayPool.Shared 内部使用分桶 + TLS 缓存机制，在单线程下接近零竞争，但多线程高并发时出现锁竞争（16 线程构造+释放从 30 us 增长到 176 us）。

### 2. Slice 操作的 48B 堆分配

所有 Slice 操作均产生 48B 堆分配：
- OwnerPacket.Slice → 新建 OwnerPacket 对象（class，必须堆分配）
- MemoryPacket.Slice → 返回 IPacket 接口导致 struct 装箱
- ArrayPacket.Slice(IPacket) → 返回 IPacket 接口导致 struct 装箱

**瓶颈根源**：IPacket 接口返回值要求装箱。OwnerPacket 因设计为 class 无法避免。

### 3. OwnerPacket 内存访问额外开销

OwnerPacket 的 GetSpan/GetMemory/TryGetArray（~13-14 ns）比 struct 实现（MemoryPacket ~0.5-1.8 ns，ArrayPacket/ReadOnlyPacket <1 ns）慢约 10 倍。

**瓶颈根源**：OwnerPacket 是 sealed class，即使标记了 `AggressiveInlining`，benchmark 中包含了构造+释放的完整生命周期开销。纯方法调用本身已被 JIT 内联优化。

### 4. 多线程 Slice 内存压力

32 线程 Slice 操作内存分配达到 MB 级别（OwnerPacket 3002 KB，MemoryPacket 1502 KB），触发 Gen1 GC。

**瓶颈根源**：每次 Slice 创建新对象，高并发下 GC 压力显著。

### 5. MemoryPacket Indexer 略慢于 ArrayPacket

MemoryPacket Indexer（~2.2 ns）比 ArrayPacket/OwnerPacket Indexer（~1.6 ns）慢约 37%。

**瓶颈根源**：MemoryPacket 需要通过 `_memory.Span[index]` 间接访问，多一层 Span 获取开销。

## 七、优化建议

### 高优先级

1. **减少 Slice 装箱分配**
   - 为 MemoryPacket 和 ArrayPacket 提供返回自身类型的 Slice 重载（已有 ArrayPacket.Slice 返回 ArrayPacket 的版本）。
   - 在调用方已知具体类型时，优先调用 struct 版本的 Slice 避免装箱。
   - 考虑为 ReadOnlyPacket 也添加返回 `ReadOnlyPacket` 的 Slice 重载。

2. **OwnerPacket 多线程池化优化**
   - 在超高并发场景下，考虑使用 `ThreadLocal<T>` 缓存 OwnerPacket 实例，减少 ArrayPool 竞争。
   - 或者使用自定义的 Per-Thread 内存池替代 `ArrayPool<Byte>.Shared`。

3. **Slice 对象池化**
   - 对于 OwnerPacket.Slice 产生的新对象，考虑使用 `ObjectPool<OwnerPacket>` 复用实例。
   - 需要权衡池化管理开销与 GC 压力。

### 中优先级

4. **MemoryPacket Indexer 优化**
   - 考虑在 MemoryPacket 内部缓存底层数组引用，避免每次 Indexer 访问都经过 Memory→Span 转换。
   - 可通过 `MemoryMarshal.TryGetArray` 在构造时提取数组引用。

5. **PacketHelper.ToStr 多包链优化**
   - 当前多包链使用 StringBuilder 池化拼接，可考虑预计算总长度后一次性分配 char[] 进行编码。
   - 减少 StringBuilder 的扩容开销。

6. **PacketHelper.Clone 优化**
   - 单包 Clone 当前使用 `GetSpan().ToArray()` 再包装为 ArrayPacket，可以考虑直接返回 ReadOnlyPacket 减少后续可变性风险。

### 低优先级

7. **ReadOnlyPacket 的 Slice 返回类型**
   - 当前 Slice 返回 IPacket 接口，会导致 struct 装箱。可以添加返回 ReadOnlyPacket 的重载。

8. **链式包的 Total 属性缓存**
   - 当前 Total 是递归计算的 `_length + (Next?.Total ?? 0)`，对于长链路每次访问都遍历。
   - 可以考虑在 Append 时缓存总长度。

## 八、总结

| 实现类型 | 适用场景 | 关键优势 | 主要开销 |
|---------|---------|---------|---------|
| OwnerPacket | 需要池化内存管理的场景（网络IO） | 池化零分配构造/释放 | Slice 产生新对象 48B |
| MemoryPacket | 包装 Memory<Byte> 的临时场景 | struct 零堆分配，极快构造 | Slice 装箱 48B |
| ArrayPacket | 通用零拷贝缓冲区切片 | struct 零分配，struct Slice 零分配 | IPacket Slice 装箱 |
| ReadOnlyPacket | 多线程共享只读数据 | readonly struct，线程安全 | 不支持链式结构 |

整体而言，IPacket 体系的性能设计非常优秀：
- 核心操作（构造、GetSpan、GetMemory、TryGetArray、Indexer）均在 **亚纳秒到十几纳秒级别**
- 内存分配控制良好，除 Slice 外几乎零分配
- ArrayPool 池化机制在中低并发下表现优异
- struct 实现（MemoryPacket、ArrayPacket、ReadOnlyPacket）天然适合高频创建场景
