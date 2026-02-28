# IPacket 数据包性能测试报告

## 测试环境

```
BenchmarkDotNet v0.15.8, Windows 10 (10.0.19045.6456/22H2/2022Update)
Intel Core i9-10900K CPU 3.70GHz, 1 CPU, 20 logical核心, 10 物理核心
.NET SDK 10.0.103
Runtime: .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v3
目标框架: net10.0
```

## 一、OwnerPacket 单线程性能

| 方法 | Size | 平均耗时 | Error | StdDev | Gen0 | 内存分配 |
|------|------|---------|-------|--------|------|---------|
| 构造+释放 | 64 | 6.654 ns | 0.0088 ns | 0.0102 ns | - | - |
| GetSpan | 64 | 9.079 ns | 0.0046 ns | 0.0052 ns | - | - |
| GetMemory | 64 | 8.812 ns | 0.0109 ns | 0.0121 ns | - | - |
| TryGetArray | 64 | 8.130 ns | 0.0075 ns | 0.0077 ns | - | - |
| Slice | 64 | 6.646 ns | 0.2304 ns | 0.2653 ns | 0.0046 | 48 B |
| Resize | 64 | 5.889 ns | 0.0395 ns | 0.0388 ns | 0.0046 | 48 B |
| Indexer读 | 64 | 0.997 ns | 0.0020 ns | 0.0022 ns | - | - |
| Indexer写 | 64 | 0.790 ns | 0.0018 ns | 0.0019 ns | - | - |
| 构造+释放 | 1024 | 6.685 ns | 0.0055 ns | 0.0057 ns | - | - |
| GetSpan | 1024 | 11.247 ns | 0.0130 ns | 0.0150 ns | - | - |
| GetMemory | 1024 | 8.741 ns | 0.0030 ns | 0.0029 ns | - | - |
| TryGetArray | 1024 | 8.831 ns | 0.0097 ns | 0.0108 ns | - | - |
| Slice | 1024 | 6.468 ns | 0.0357 ns | 0.0411 ns | 0.0046 | 48 B |
| Resize | 1024 | 5.878 ns | 0.0266 ns | 0.0285 ns | 0.0046 | 48 B |
| Indexer读 | 1024 | 0.972 ns | 0.0022 ns | 0.0025 ns | - | - |
| Indexer写 | 1024 | 0.793 ns | 0.0005 ns | 0.0005 ns | - | - |
| 构造+释放 | 8192 | 7.219 ns | 0.0103 ns | 0.0118 ns | - | - |
| GetSpan | 8192 | 8.821 ns | 0.0034 ns | 0.0033 ns | - | - |
| GetMemory | 8192 | 9.871 ns | 0.0144 ns | 0.0166 ns | - | - |
| TryGetArray | 8192 | 8.194 ns | 0.0053 ns | 0.0057 ns | - | - |
| Slice | 8192 | 6.636 ns | 0.0280 ns | 0.0300 ns | 0.0046 | 48 B |
| Resize | 8192 | 5.856 ns | 0.0386 ns | 0.0413 ns | 0.0046 | 48 B |
| Indexer读 | 8192 | 1.197 ns | 0.0006 ns | 0.0006 ns | - | - |
| Indexer写 | 8192 | 0.791 ns | 0.0024 ns | 0.0027 ns | - | - |

### OwnerPacket 单线程分析

- **构造+释放** 约 6.7-7.2 ns，得益于 `ArrayPool<Byte>.Shared` 的高效池化，不随 Size 增长变化。**零内存分配**。
- **GetSpan / GetMemory / TryGetArray** 约 8-11 ns，均为零分配操作。
- **Slice / Resize** 约 5.9-6.6 ns，每次分配 48 B（一个新 OwnerPacket 对象）。
- **Indexer** 读约 1.0 ns，写约 0.8 ns，极其高效。

## 二、OwnerPacket 多线程性能

| 方法 | ThreadCount | 平均耗时 | Error | StdDev | Gen0 | Gen1 | 内存分配 |
|------|------------|---------|-------|--------|------|------|---------|
| 多线程构造+释放 | 1 | 13.36 us | 0.121 us | 0.135 us | 4.7607 | - | 48.56 KB |
| 多线程GetSpan | 1 | 15.30 us | 0.150 us | 0.173 us | 4.7607 | - | 48.56 KB |
| 多线程Slice | 1 | 11.18 us | 0.137 us | 0.158 us | 9.3384 | 0.0305 | 95.44 KB |
| 多线程构造+释放 | 4 | 25.49 us | 0.090 us | 0.104 us | 18.6768 | 0.1221 | 189.81 KB |
| 多线程GetSpan | 4 | 26.11 us | 0.103 us | 0.110 us | 18.6768 | 0.1221 | 189.81 KB |
| 多线程Slice | 4 | 28.16 us | 0.291 us | 0.335 us | 37.1704 | 0.2747 | 377.32 KB |
| 多线程构造+释放 | 16 | 57.07 us | 0.375 us | 0.401 us | 74.2798 | 0.9155 | 753.76 KB |
| 多线程GetSpan | 16 | 57.94 us | 0.369 us | 0.425 us | 74.2798 | 0.9155 | 753.79 KB |
| 多线程Slice | 16 | 84.88 us | 3.664 us | 4.219 us | 148.3154 | 2.0752 | 1504.06 KB |
| 多线程构造+释放 | 32 | 98.40 us | 0.555 us | 0.639 us | 148.4375 | 2.6855 | 1505.47 KB |
| 多线程GetSpan | 32 | 102.63 us | 0.713 us | 0.821 us | 148.4375 | 2.6855 | 1505.53 KB |
| 多线程Slice | 32 | 169.03 us | 0.940 us | 1.006 us | 296.6309 | 5.8594 | 3006.05 KB |

### OwnerPacket 多线程分析

- 1→4 线程：耗时增长约 1.9 倍（25.49 us / 13.36 us），接近线性扩展，说明 ArrayPool 在低并发下竞争较小。
- 4→16 线程：耗时增长约 2.2 倍（57.07 us / 25.49 us），出现一定的 ArrayPool 锁竞争，但得益于 10 物理核心，扩展性仍然良好。
- 16→32 线程：耗时增长约 1.7 倍（98.40 us / 57.07 us），线程数超过物理核心（10核），CPU 调度开销成为主因。
- **Slice 操作**在多线程下内存分配翻倍，因为每次 Slice 创建新 OwnerPacket 对象（48 B/次）。

## 三、MemoryPacket 单线程性能

| 方法 | Size | 平均耗时 | Error | StdDev | Gen0 | 内存分配 |
|------|------|---------|-------|--------|------|---------|
| 构造 | 64 | 0.4552 ns | 0.0024 ns | 0.0027 ns | - | - |
| GetSpan | 64 | 1.7050 ns | 0.0025 ns | 0.0029 ns | - | - |
| GetMemory | 64 | 1.3471 ns | 0.0028 ns | 0.0033 ns | - | - |
| TryGetArray | 64 | 0.9727 ns | 0.0019 ns | 0.0021 ns | - | - |
| Slice | 64 | 6.2957 ns | 0.0953 ns | 0.1097 ns | 0.0046 | 48 B |
| Indexer读 | 64 | 1.6665 ns | 0.0033 ns | 0.0038 ns | - | - |
| Indexer写 | 64 | 1.5703 ns | 0.0038 ns | 0.0043 ns | - | - |
| 构造 | 1024 | 0.4503 ns | 0.0006 ns | 0.0007 ns | - | - |
| GetSpan | 1024 | 1.7064 ns | 0.0046 ns | 0.0053 ns | - | - |
| GetMemory | 1024 | 1.3426 ns | 0.0008 ns | 0.0009 ns | - | - |
| TryGetArray | 1024 | 0.9740 ns | 0.0018 ns | 0.0020 ns | - | - |
| Slice | 1024 | 6.1580 ns | 0.0292 ns | 0.0336 ns | 0.0046 | 48 B |
| Indexer读 | 1024 | 1.6635 ns | 0.0036 ns | 0.0042 ns | - | - |
| Indexer写 | 1024 | 1.5636 ns | 0.0013 ns | 0.0014 ns | - | - |
| 构造 | 8192 | 0.4545 ns | 0.0018 ns | 0.0021 ns | - | - |
| GetSpan | 8192 | 1.8503 ns | 0.0010 ns | 0.0011 ns | - | - |
| GetMemory | 8192 | 1.3480 ns | 0.0024 ns | 0.0023 ns | - | - |
| TryGetArray | 8192 | 0.9732 ns | 0.0011 ns | 0.0012 ns | - | - |
| Slice | 8192 | 6.1905 ns | 0.0323 ns | 0.0371 ns | 0.0046 | 48 B |
| Indexer读 | 8192 | 1.7185 ns | 0.0038 ns | 0.0044 ns | - | - |
| Indexer写 | 8192 | 1.9247 ns | 0.0025 ns | 0.0026 ns | - | - |

### MemoryPacket 单线程分析

- **构造** 约 0.45 ns，struct 无堆分配，极快。
- **GetMemory** 约 1.35 ns，最快的内存访问方式，直接返回内部 Memory 字段。
- **GetSpan** 约 1.7-1.85 ns，需要从 Memory 取 Span。
- **TryGetArray** 约 0.97 ns，通过 `MemoryMarshal.TryGetArray` 获取。
- **Slice** 约 6.2 ns，需要装箱为 IPacket 返回（48 B 分配），是主要开销来源。
- **Indexer** 读约 1.7 ns，写约 1.6 ns。
- 所有操作均**与 Size 无关**，体现了零拷贝设计。

## 四、MemoryPacket 多线程性能

| 方法 | ThreadCount | 平均耗时 | Error | StdDev | Gen0 | Gen1 | 内存分配 |
|------|------------|---------|-------|--------|------|------|---------|
| 多线程构造 | 1 | 2.608 us | 0.0139 us | 0.0160 us | 0.1488 | - | 1.53 KB |
| 多线程GetSpan | 1 | 2.716 us | 0.0154 us | 0.0177 us | 0.1602 | - | 1.63 KB |
| 多线程Slice | 1 | 6.830 us | 0.0667 us | 0.0768 us | 4.7607 | - | 48.56 KB |
| 多线程构造 | 4 | 2.673 us | 0.0179 us | 0.0207 us | 0.1678 | - | 1.72 KB |
| 多线程GetSpan | 4 | 6.361 us | 0.0251 us | 0.0289 us | 0.1755 | - | 1.86 KB |
| 多线程Slice | 4 | 16.514 us | 0.0817 us | 0.0940 us | 18.6157 | 0.1221 | 189.66 KB |
| 多线程构造 | 16 | 5.099 us | 0.0219 us | 0.0243 us | 0.2441 | - | 2.55 KB |
| 多线程GetSpan | 16 | 13.024 us | 0.1399 us | 0.1611 us | 0.2747 | - | 2.88 KB |
| 多线程Slice | 16 | 43.428 us | 0.2211 us | 0.2547 us | 74.2188 | 0.8545 | 753.47 KB |
| 多线程构造 | 32 | 8.176 us | 0.1163 us | 0.1339 us | 0.3510 | - | 3.62 KB |
| 多线程GetSpan | 32 | 18.709 us | 0.0367 us | 0.0423 us | 0.3967 | - | 4.06 KB |
| 多线程Slice | 32 | 84.033 us | 0.1733 us | 0.1926 us | 148.4375 | 2.4414 | 1505.13 KB |

### MemoryPacket 多线程分析

- MemoryPacket 作为 struct，**构造和 GetSpan 在多线程下几乎无竞争**（仅有线程调度开销）。
- 构造操作在 1→32 线程下耗时仅从 2.6 us 增长到 8.2 us，内存分配几乎不变（1.5→3.6 KB），说明无锁设计非常高效。
- Slice 操作因装箱为 IPacket 导致大量堆分配，32 线程时 1505 KB/操作。
- 与 OwnerPacket 相比，MemoryPacket 多线程构造快约 12 倍（无 ArrayPool 锁竞争）。

## 五、ArrayPacket 单线程性能

| 方法 | Size | 平均耗时 | Error | StdDev | Gen0 | 内存分配 |
|------|------|---------|-------|--------|------|---------|
| 构造(byte[]) | 64 | 0.0130 ns | 0.0002 ns | 0.0002 ns | - | - |
| 构造(ArraySegment) | 64 | 0.1459 ns | 0.0182 ns | 0.0202 ns | - | - |
| GetSpan | 64 | 0.0128 ns | 0.0004 ns | 0.0005 ns | - | - |
| GetMemory | 64 | 1.0226 ns | 0.0006 ns | 0.0006 ns | - | - |
| TryGetArray | 64 | ~0 ns | - | - | - | - |
| Slice(struct) | 64 | 2.6142 ns | 0.0009 ns | 0.0009 ns | - | - |
| Slice(IPacket) | 64 | 11.4746 ns | 0.0553 ns | 0.0614 ns | 0.0038 | 40 B |
| Indexer读 | 64 | 0.3041 ns | 0.0021 ns | 0.0024 ns | - | - |
| Indexer写 | 64 | 0.2097 ns | 0.0017 ns | 0.0020 ns | - | - |
| 隐式转换byte[] | 64 | 1.0864 ns | 0.0004 ns | 0.0004 ns | - | - |
| 隐式转换string | 64 | 12.3402 ns | 0.0469 ns | 0.0540 ns | 0.0038 | 40 B |
| 构造(byte[]) | 1024 | 0.0137 ns | 0.0008 ns | 0.0010 ns | - | - |
| 构造(ArraySegment) | 1024 | 0.1324 ns | 0.0013 ns | 0.0014 ns | - | - |
| GetSpan | 1024 | 0.0108 ns | 0.0001 ns | 0.0001 ns | - | - |
| GetMemory | 1024 | 1.0225 ns | 0.0004 ns | 0.0005 ns | - | - |
| TryGetArray | 1024 | ~0 ns | - | - | - | - |
| Slice(struct) | 1024 | 2.6143 ns | 0.0011 ns | 0.0011 ns | - | - |
| Slice(IPacket) | 1024 | 11.2695 ns | 0.0407 ns | 0.0469 ns | 0.0038 | 40 B |
| Indexer读 | 1024 | 0.2398 ns | 0.0009 ns | 0.0009 ns | - | - |
| Indexer写 | 1024 | 0.2082 ns | 0.0003 ns | 0.0003 ns | - | - |
| 隐式转换byte[] | 1024 | 1.0888 ns | 0.0026 ns | 0.0030 ns | - | - |
| 隐式转换string | 1024 | 11.8379 ns | 0.0453 ns | 0.0504 ns | 0.0038 | 40 B |
| 构造(byte[]) | 8192 | 0.0130 ns | 0.0002 ns | 0.0002 ns | - | - |
| 构造(ArraySegment) | 8192 | 0.1436 ns | 0.0109 ns | 0.0126 ns | - | - |
| GetSpan | 8192 | 0.0139 ns | 0.0015 ns | 0.0017 ns | - | - |
| GetMemory | 8192 | 1.0220 ns | 0.0011 ns | 0.0012 ns | - | - |
| TryGetArray | 8192 | ~0 ns | - | - | - | - |
| Slice(struct) | 8192 | 2.6146 ns | 0.0009 ns | 0.0009 ns | - | - |
| Slice(IPacket) | 8192 | 11.4422 ns | 0.0520 ns | 0.0556 ns | 0.0038 | 40 B |
| Indexer读 | 8192 | 0.2470 ns | 0.0018 ns | 0.0021 ns | - | - |
| Indexer写 | 8192 | 0.2060 ns | 0.0008 ns | 0.0009 ns | - | - |
| 隐式转换byte[] | 8192 | 1.0875 ns | 0.0013 ns | 0.0015 ns | - | - |
| 隐式转换string | 8192 | 11.9719 ns | 0.0584 ns | 0.0649 ns | 0.0038 | 40 B |

### ArrayPacket 单线程分析

- **构造(byte[])** 和 **GetSpan** 均为亚纳秒级（~0.01 ns），record struct 直接赋值字段，JIT 完全内联，BenchmarkDotNet 报告接近 ZeroMeasurement。
- **TryGetArray** 接近零耗时，直接返回内部 ArraySegment。
- **Slice(struct)** 约 2.6 ns，返回 ArrayPacket struct，**零内存分配**，是所有 Slice 中最快的。
- **Slice(IPacket)** 约 11.3 ns，因返回 IPacket 接口导致 struct 装箱，分配 40 B。
- **Indexer** 读约 0.24 ns，写约 0.21 ns，直接数组下标访问，是所有实现中最快的。
- **隐式转换string** 约 12 ns，需要 UTF-8 编码，分配 40 B。
- 所有操作均**与 Size 无关**。

## 六、ArrayPacket 多线程性能

| 方法 | ThreadCount | 平均耗时 | Error | StdDev | Gen0 | 内存分配 |
|------|------------|---------|-------|--------|------|---------|
| 多线程构造 | 1 | 2.587 us | 0.0084 us | 0.0093 us | 0.1488 | 1.53 KB |
| 多线程GetSpan | 1 | 2.615 us | 0.0110 us | 0.0126 us | 0.1488 | 1.53 KB |
| 多线程Slice | 1 | 5.132 us | 0.0242 us | 0.0278 us | 0.1602 | 1.69 KB |
| 多线程构造 | 4 | 2.617 us | 0.0134 us | 0.0154 us | 0.1640 | 1.71 KB |
| 多线程GetSpan | 4 | 2.688 us | 0.0136 us | 0.0156 us | 0.1678 | 1.72 KB |
| 多线程Slice | 4 | 11.341 us | 0.0274 us | 0.0316 us | 0.1831 | 2.01 KB |
| 多线程构造 | 16 | 5.068 us | 0.0112 us | 0.0129 us | 0.2441 | 2.54 KB |
| 多线程GetSpan | 16 | 5.064 us | 0.0062 us | 0.0068 us | 0.2441 | 2.54 KB |
| 多线程Slice | 16 | 20.879 us | 0.0430 us | 0.0495 us | 0.3052 | 3.14 KB |
| 多线程构造 | 32 | 8.046 us | 0.0165 us | 0.0183 us | 0.3510 | 3.61 KB |
| 多线程GetSpan | 32 | 8.009 us | 0.0111 us | 0.0127 us | 0.3510 | 3.60 KB |
| 多线程Slice | 32 | 30.160 us | 0.1368 us | 0.1575 us | 0.4272 | 4.50 KB |

### ArrayPacket 多线程分析

- ArrayPacket 作为 record struct，**构造和 GetSpan 在多线程下无竞争**，与 MemoryPacket 表现一致。
- **Slice 是四种实现中多线程性能最好的**：32 线程下仅 30.16 us / 4.50 KB，因为 Slice 返回 struct（零装箱），内存分配极低。
- 对比 OwnerPacket 32 线程 Slice（169.03 us / 3006 KB），ArrayPacket 快 **5.6 倍**，内存分配低 **668 倍**。

## 七、ReadOnlyPacket 单线程性能

| 方法 | Size | 平均耗时 | Error | StdDev | Gen0 | Gen1 | 内存分配 |
|------|------|---------|-------|--------|------|------|---------|
| 构造(byte[]) | 64 | 0.2153 ns | 0.0009 ns | 0.0010 ns | - | - | - |
| 构造(ArraySegment) | 64 | 0.2196 ns | 0.0007 ns | 0.0007 ns | - | - | - |
| 构造(IPacket拷贝) | 64 | 11.316 ns | 0.0927 ns | 0.0992 ns | 0.0122 | - | 128 B |
| GetSpan | 64 | 0.0131 ns | 0.0017 ns | 0.0019 ns | - | - | - |
| GetMemory | 64 | 1.1389 ns | 0.0010 ns | 0.0011 ns | - | - | - |
| TryGetArray | 64 | 0.2067 ns | 0.0016 ns | 0.0018 ns | - | - | - |
| Slice | 64 | 4.803 ns | 0.0424 ns | 0.0453 ns | 0.0031 | - | 32 B |
| Indexer读 | 64 | 1.031 ns | 0.0030 ns | 0.0034 ns | - | - | - |
| ToArray | 64 | 0.791 ns | 0.0057 ns | 0.0066 ns | - | - | - |
| 隐式转换byte[] | 64 | 1.024 ns | 0.0005 ns | 0.0005 ns | - | - | - |
| 构造(byte[]) | 1024 | 0.2175 ns | 0.0023 ns | 0.0026 ns | - | - | - |
| 构造(ArraySegment) | 1024 | 0.2192 ns | 0.0003 ns | 0.0003 ns | - | - | - |
| 构造(IPacket拷贝) | 1024 | 50.795 ns | 0.4531 ns | 0.5036 ns | 0.1041 | 0.0004 | 1088 B |
| GetSpan | 1024 | 0.0139 ns | 0.0003 ns | 0.0003 ns | - | - | - |
| GetMemory | 1024 | 1.138 ns | 0.0004 ns | 0.0004 ns | - | - | - |
| TryGetArray | 1024 | 0.206 ns | 0.0012 ns | 0.0014 ns | - | - | - |
| Slice | 1024 | 4.693 ns | 0.0329 ns | 0.0352 ns | 0.0031 | - | 32 B |
| Indexer读 | 1024 | 0.840 ns | 0.0010 ns | 0.0011 ns | - | - | - |
| ToArray | 1024 | 0.785 ns | 0.0033 ns | 0.0032 ns | - | - | - |
| 隐式转换byte[] | 1024 | 1.026 ns | 0.0028 ns | 0.0032 ns | - | - | - |
| 构造(byte[]) | 8192 | 0.2178 ns | 0.0004 ns | 0.0004 ns | - | - | - |
| 构造(ArraySegment) | 8192 | 0.2209 ns | 0.0022 ns | 0.0025 ns | - | - | - |
| 构造(IPacket拷贝) | 8192 | 376.158 ns | 3.4436 ns | 3.9656 ns | 0.7901 | 0.0243 | 8256 B |
| GetSpan | 8192 | 0.0157 ns | 0.0017 ns | 0.0020 ns | - | - | - |
| GetMemory | 8192 | 1.138 ns | 0.0004 ns | 0.0004 ns | - | - | - |
| TryGetArray | 8192 | 0.205 ns | 0.0006 ns | 0.0006 ns | - | - | - |
| Slice | 8192 | 4.692 ns | 0.0369 ns | 0.0394 ns | 0.0031 | - | 32 B |
| Indexer读 | 8192 | 0.762 ns | 0.0026 ns | 0.0030 ns | - | - | - |
| ToArray | 8192 | 0.790 ns | 0.0073 ns | 0.0084 ns | - | - | - |
| 隐式转换byte[] | 8192 | 1.024 ns | 0.0003 ns | 0.0003 ns | - | - | - |

### ReadOnlyPacket 单线程分析

- **构造(byte[]/ArraySegment)** 约 0.22 ns，readonly record struct 赋值极快。
- **构造(IPacket拷贝)** 需要拷贝数据，耗时随 Size 线性增长（64B→11 ns / 128B，8192B→376 ns / 8256B）。
- **GetSpan** 接近零耗时（~0.01 ns），直接返回 Span，JIT 完全内联。
- **TryGetArray** 约 0.21 ns，直接返回内部 ArraySegment。
- **Slice** 约 4.7 ns，分配 32 B（装箱为 IPacket），比 MemoryPacket 的 48 B 更少。
- **Indexer读** 约 0.76-1.03 ns，直接数组下标访问。不支持写操作（readonly）。
- **ToArray** 约 0.79 ns，当切片覆盖完整数组时直接返回引用，无需拷贝。

## 八、ReadOnlyPacket 多线程性能

| 方法 | ThreadCount | 平均耗时 | Error | StdDev | Gen0 | Gen1 | 内存分配 |
|------|------------|---------|-------|--------|------|------|---------|
| 多线程构造 | 1 | 2.563 us | 0.0107 us | 0.0123 us | 0.1488 | - | 1.53 KB |
| 多线程GetSpan | 1 | 2.609 us | 0.0103 us | 0.0119 us | 0.1488 | - | 1.54 KB |
| 多线程Slice | 1 | 5.487 us | 0.0415 us | 0.0478 us | 3.2272 | 0.0153 | 32.94 KB |
| 多线程构造 | 4 | 2.615 us | 0.0098 us | 0.0113 us | 0.1640 | - | 1.71 KB |
| 多线程GetSpan | 4 | 2.665 us | 0.0288 us | 0.0332 us | 0.1526 | - | 1.71 KB |
| 多线程Slice | 4 | 13.900 us | 0.0432 us | 0.0498 us | 12.4664 | 0.0763 | 127.07 KB |
| 多线程构造 | 16 | 5.070 us | 0.0171 us | 0.0197 us | 0.2441 | - | 2.55 KB |
| 多线程GetSpan | 16 | 5.223 us | 0.0263 us | 0.0302 us | 0.2441 | - | 2.54 KB |
| 多线程Slice | 16 | 33.225 us | 0.1152 us | 0.1281 us | 49.4385 | 0.5493 | 503.29 KB |
| 多线程构造 | 32 | 7.991 us | 0.0186 us | 0.0214 us | 0.3510 | - | 3.61 KB |
| 多线程GetSpan | 32 | 8.033 us | 0.0485 us | 0.0559 us | 0.3510 | - | 3.62 KB |
| 多线程Slice | 32 | 60.442 us | 1.2823 us | 1.4767 us | 98.7549 | 1.5869 | 1004.79 KB |

### ReadOnlyPacket 多线程分析

- 构造和 GetSpan 与 ArrayPacket/MemoryPacket 表现一致，struct 天然无竞争。
- Slice 操作因装箱为 IPacket 产生堆分配（每次 32 B），但总分配量低于 MemoryPacket（32 线程：1005 KB vs 1505 KB）。
- 适合多线程共享只读数据的场景，readonly 保证线程安全。

## 九、PacketHelper 扩展方法性能

### 单线程性能（基于 ArrayPacket）

| 方法 | Size | 平均耗时 | Error | StdDev | Gen0 | Gen1 | 内存分配 |
|------|------|---------|-------|--------|------|------|---------|
| Append(IPacket) | 64 | 9.086 ns | 0.0892 ns | 0.0955 ns | 0.0076 | - | 80 B |
| Append(byte[]) | 64 | 9.234 ns | 0.0734 ns | 0.0845 ns | 0.0076 | - | 80 B |
| ToStr(单包) | 64 | 656.70 ns | 1.582 ns | 1.758 ns | 0.1040 | - | 1088 B |
| ToStr(链式包) | 64 | 1,437.68 ns | 5.013 ns | 5.773 ns | 0.2403 | - | 2528 B |
| ToHex(单包) | 64 | 78.36 ns | 0.207 ns | 0.230 ns | 0.0184 | - | 192 B |
| ToHex(链式包) | 64 | 99.62 ns | 0.276 ns | 0.318 ns | 0.0222 | - | 232 B |
| CopyTo | 64 | 28.76 ns | 0.206 ns | 0.237 ns | 0.0367 | - | 384 B |
| GetStream | 64 | 12.58 ns | 0.088 ns | 0.098 ns | 0.0099 | - | 104 B |
| ToSegment(单包) | 64 | 8.091 ns | 0.043 ns | 0.049 ns | 0.0038 | - | 40 B |
| ToSegment(链式包) | 64 | 45.13 ns | 0.223 ns | 0.248 ns | 0.0222 | - | 232 B |
| ToSegments | 64 | 17.81 ns | 0.117 ns | 0.130 ns | 0.0153 | - | 160 B |
| ToArray | 64 | 14.31 ns | 0.081 ns | 0.094 ns | 0.0122 | - | 128 B |
| ReadBytes | 64 | 14.63 ns | 0.076 ns | 0.084 ns | 0.0092 | - | 96 B |
| Clone | 64 | 18.48 ns | 0.084 ns | 0.093 ns | 0.0161 | - | 168 B |
| TryGetSpan | 64 | ~0 ns | - | - | - | - | - |
| ExpandHeader(ArrayPacket) | 64 | 9.177 ns | 0.062 ns | 0.071 ns | 0.0076 | - | 80 B |
| ExpandHeader(新建) | 64 | 16.22 ns | 0.086 ns | 0.095 ns | 0.0122 | - | 128 B |
| Append(IPacket) | 1024 | 9.073 ns | 0.066 ns | 0.070 ns | 0.0076 | - | 80 B |
| Append(byte[]) | 1024 | 9.199 ns | 0.074 ns | 0.085 ns | 0.0076 | - | 80 B |
| ToStr(单包) | 1024 | 11,760 ns | 33.859 ns | 38.993 ns | 1.5106 | - | 15,952 B |
| ToStr(链式包) | 1024 | 22,766 ns | 28.784 ns | 31.993 ns | 3.3569 | - | 35,240 B |
| ToHex(单包) | 1024 | 79.08 ns | 0.229 ns | 0.263 ns | 0.0184 | - | 192 B |
| ToHex(链式包) | 1024 | 99.76 ns | 0.178 ns | 0.198 ns | 0.0222 | - | 232 B |
| CopyTo | 1024 | 64.21 ns | 0.414 ns | 0.477 ns | 0.1101 | 0.0005 | 1152 B |
| GetStream | 1024 | 12.57 ns | 0.089 ns | 0.095 ns | 0.0099 | - | 104 B |
| ToSegment(单包) | 1024 | 8.127 ns | 0.039 ns | 0.044 ns | 0.0038 | - | 40 B |
| ToSegment(链式包) | 1024 | 163.15 ns | 0.431 ns | 0.461 ns | 0.2058 | 0.0014 | 2152 B |
| ToSegments | 1024 | 17.90 ns | 0.101 ns | 0.112 ns | 0.0153 | - | 160 B |
| ToArray | 1024 | 53.31 ns | 0.472 ns | 0.524 ns | 0.1041 | - | 1088 B |
| ReadBytes | 1024 | 33.46 ns | 0.218 ns | 0.242 ns | 0.0551 | - | 576 B |
| Clone | 1024 | 58.96 ns | 0.499 ns | 0.575 ns | 0.1079 | - | 1128 B |
| TryGetSpan | 1024 | ~0 ns | - | - | - | - | - |
| ExpandHeader(ArrayPacket) | 1024 | 10.64 ns | 0.057 ns | 0.063 ns | 0.0076 | - | 80 B |
| ExpandHeader(新建) | 1024 | 16.20 ns | 0.077 ns | 0.083 ns | 0.0122 | - | 128 B |
| Append(IPacket) | 8192 | 9.076 ns | 0.066 ns | 0.073 ns | 0.0076 | - | 80 B |
| Append(byte[]) | 8192 | 9.191 ns | 0.082 ns | 0.085 ns | 0.0076 | - | 80 B |
| ToStr(单包) | 8192 | 127,085 ns | 87.033 ns | 85.478 ns | 11.9629 | - | 127,168 B |
| ToStr(链式包) | 8192 | 248,279 ns | 262.535 ns | 302.336 ns | 29.7852 | 2.9297 | 312,480 B |
| ToHex(单包) | 8192 | 80.84 ns | 0.137 ns | 0.147 ns | 0.0184 | - | 192 B |
| ToHex(链式包) | 8192 | 100.05 ns | 0.164 ns | 0.182 ns | 0.0222 | - | 232 B |
| CopyTo | 8192 | 394.20 ns | 3.715 ns | 4.278 ns | 0.7968 | 0.0238 | 8320 B |
| GetStream | 8192 | 12.58 ns | 0.074 ns | 0.082 ns | 0.0099 | - | 104 B |
| ToSegment(单包) | 8192 | 8.097 ns | 0.031 ns | 0.035 ns | 0.0038 | - | 40 B |
| ToSegment(链式包) | 8192 | 949.79 ns | 8.125 ns | 7.980 ns | 1.5745 | 0.0982 | 16,488 B |
| ToSegments | 8192 | 17.80 ns | 0.095 ns | 0.109 ns | 0.0153 | - | 160 B |
| ToArray | 8192 | 377.62 ns | 3.331 ns | 3.703 ns | 0.7901 | - | 8256 B |
| ReadBytes | 8192 | 180.96 ns | 1.048 ns | 1.076 ns | 0.3979 | - | 4160 B |
| Clone | 8192 | 376.85 ns | 2.185 ns | 2.337 ns | 0.7935 | - | 8296 B |
| TryGetSpan | 8192 | ~0 ns | - | - | - | - | - |
| ExpandHeader(ArrayPacket) | 8192 | 9.809 ns | 0.104 ns | 0.116 ns | 0.0076 | - | 80 B |
| ExpandHeader(新建) | 8192 | 16.19 ns | 0.076 ns | 0.079 ns | 0.0122 | - | 128 B |

### PacketHelper 单线程分析

- **TryGetSpan** 接近零耗时，JIT 完全内联。
- **Append** 约 9.1 ns / 80 B，链接两个包的元数据开销，与 Size 无关。
- **GetStream** 约 12.6 ns / 104 B，创建 MemoryStream 包装，与 Size 无关。
- **ToSegment(单包)** 约 8.1 ns / 40 B，直接装箱返回 ArraySegment。
- **ToSegment(链式包)** 随 Size 线性增长（64B→45 ns，8192B→950 ns），需要合并多个包的数据。
- **ToStr** 是最耗时的操作，UTF-8 编码开销随 Size 线性增长（64B→657 ns，8192B→127 us）。
- **ToHex** 约 79-100 ns，仅编码前 32 字节，与 Size 无关。
- **ToArray / Clone / ReadBytes** 涉及数据拷贝，耗时随 Size 线性增长。
- **ExpandHeader(ArrayPacket)** 约 9.2-10.6 ns / 80 B，在已有数据前面扩展头部空间。

### 多线程性能（Size=1024）

| 方法 | ThreadCount | 平均耗时 | Error | StdDev | Gen0 | Gen1 | 内存分配 |
|------|------------|---------|-------|--------|------|------|---------|
| 多线程ToStr | 1 | 11,784 us | 15.020 us | 16.071 us | 1515.6250 | - | 15,580 KB |
| 多线程ToArray | 1 | 56.60 us | 0.541 us | 0.623 us | 104.3701 | 0.6104 | 1064 KB |
| 多线程Clone | 1 | 62.07 us | 0.688 us | 0.765 us | 108.1543 | 0.6104 | 1103 KB |
| 多线程ReadBytes | 1 | 34.87 us | 0.331 us | 0.381 us | 55.2979 | 0.3052 | 564 KB |
| 多线程ExpandHeader | 1 | 14.98 us | 0.156 us | 0.180 us | 12.3901 | 0.0610 | 127 KB |
| 多线程ToStr | 4 | 12,221 us | 60.739 us | 67.511 us | 6265.6250 | - | 61,096 KB |
| 多线程ToArray | 4 | 171.97 us | 2.001 us | 2.304 us | 447.9980 | 3.6621 | 4252 KB |
| 多线程Clone | 4 | 177.08 us | 1.508 us | 1.736 us | 448.4863 | 5.3711 | 4409 KB |
| 多线程ReadBytes | 4 | 93.04 us | 0.488 us | 0.542 us | 224.6094 | 1.8311 | 2252 KB |
| 多线程ExpandHeader | 4 | 33.05 us | 0.231 us | 0.266 us | 49.3164 | 0.4272 | 502 KB |
| 多线程ToStr | 16 | 23,688 us | 51.780 us | 57.553 us | 25062.5000 | 62.5000 | 248,880 KB |
| 多线程ToArray | 16 | 924.55 us | 9.793 us | 11.277 us | 1792.9688 | 17.5781 | 17,005 KB |
| 多线程Clone | 16 | 917.78 us | 0.987 us | 1.056 us | 1793.9453 | 25.3906 | 17,630 KB |
| 多线程ReadBytes | 16 | 461.69 us | 1.679 us | 1.649 us | 898.4375 | 14.6484 | 9005 KB |
| 多线程ExpandHeader | 16 | 109.91 us | 1.506 us | 1.735 us | 197.0215 | 3.0518 | 2004 KB |
| 多线程ToStr | 32 | 51,149 us | 2528.2 us | 2911.4 us | 50090.9091 | 181.8182 | 481,257 KB |
| 多线程ToArray | 32 | 2,026.9 us | 47.035 us | 54.166 us | 3585.9375 | 23.4375 | 34,007 KB |
| 多线程Clone | 32 | 2,082.2 us | 42.863 us | 49.361 us | 3585.9375 | 39.0625 | 35,257 KB |
| 多线程ReadBytes | 32 | 1,044.2 us | 19.457 us | 22.407 us | 1794.9219 | 25.3906 | 18,007 KB |
| 多线程ExpandHeader | 32 | 237.57 us | 3.200 us | 3.685 us | 394.0430 | 9.5215 | 4007 KB |

### PacketHelper 多线程分析

- **ToStr 是最大瓶颈**：32 线程时 51.1 ms / 470 MB，UTF-8 编码的大量临时字符串分配触发频繁 GC。
- **ToArray / Clone** 表现相近（32 线程约 2 ms / 34 MB），数据拷贝产生的 GC 压力是主因。
- **ExpandHeader** 在多线程下表现良好（32 线程 237.6 us / 3.9 MB），分配量小。

## 十、实现类型横向对比（单线程，Size=1024）

| 操作 | OwnerPacket | MemoryPacket | ArrayPacket | ReadOnlyPacket |
|------|-------------|-------------|-------------|----------------|
| 构造 | 6.69 ns | 0.45 ns | ~0.01 ns | 0.22 ns |
| GetSpan | 11.25 ns | 1.71 ns | ~0.01 ns | ~0.01 ns |
| GetMemory | 8.74 ns | 1.34 ns | 1.02 ns | 1.14 ns |
| TryGetArray | 8.83 ns | 0.97 ns | ~0 ns | 0.21 ns |
| Slice | 6.47 ns (48B) | 6.16 ns (48B) | 2.61 ns (0B)† | 4.69 ns (32B) |
| Slice(IPacket) | - | - | 11.27 ns (40B) | - |
| Indexer读 | 0.97 ns | 1.66 ns | 0.24 ns | 0.84 ns |
| Indexer写 | 0.79 ns | 1.56 ns | 0.21 ns | 不支持 |
| 内存分配 | 仅Slice/Resize 48B | 仅Slice 48B | Slice(struct) 0B | 仅Slice 32B |

> † ArrayPacket.Slice 返回 ArrayPacket struct 版本为零分配；通过 IPacket 接口调用时装箱分配 40 B。

### 横向对比分析

1. **构造速度**：ArrayPacket（~0.01 ns） > ReadOnlyPacket（0.22 ns） > MemoryPacket（0.45 ns） >> OwnerPacket（6.69 ns）
2. **内存访问**：ArrayPacket 的 GetSpan/TryGetArray 接近零耗时，远超其他实现
3. **Slice 效率**：ArrayPacket struct Slice（2.61 ns/0B）是唯一实现零分配切片的类型
4. **Indexer 速度**：ArrayPacket（0.21-0.24 ns） > ReadOnlyPacket（0.84 ns） > OwnerPacket（0.79-0.97 ns） > MemoryPacket（1.56-1.66 ns）
5. **OwnerPacket 虽然最慢，但提供了池化内存管理**，适合网络 IO 场景中频繁申请/释放缓冲区

## 十一、多线程横向对比（Size=1024，32线程）

| 操作 | OwnerPacket | MemoryPacket | ArrayPacket | ReadOnlyPacket |
|------|-------------|-------------|-------------|----------------|
| 构造 | 98.40 us / 1505 KB | 8.18 us / 3.6 KB | 8.05 us / 3.6 KB | 7.99 us / 3.6 KB |
| GetSpan | 102.63 us / 1506 KB | 18.71 us / 4.1 KB | 8.01 us / 3.6 KB | 8.03 us / 3.6 KB |
| Slice | 169.03 us / 3006 KB | 84.03 us / 1505 KB | 30.16 us / 4.5 KB | 60.44 us / 1005 KB |

### 多线程横向对比分析

- **构造**：三种 struct 实现（MemoryPacket/ArrayPacket/ReadOnlyPacket）在 32 线程下表现几乎一致（~8 us / 3.6 KB），OwnerPacket 因 ArrayPool 竞争慢 12 倍。
- **Slice**：ArrayPacket 以绝对优势领先（30.16 us / 4.5 KB），因为 struct Slice 零装箱。ReadOnlyPacket 次之（60.44 us / 1005 KB），OwnerPacket 最慢（169.03 us / 3006 KB）。
- **内存效率**：ArrayPacket 多线程 Slice 的内存分配仅为 OwnerPacket 的 1/668，GC 压力极低。

## 十二、性能瓶颈分析

### 1. OwnerPacket 构造开销（~6.7 ns）

OwnerPacket 构造包含 `ArrayPool<Byte>.Shared.Rent()` 和释放包含 `Return()`。虽然 6.7 ns 已经非常高效，但在高频创建/释放场景下（如每个网络包都创建），这是最大的固定开销。

**瓶颈根源**：ArrayPool.Shared 内部使用分桶 + TLS 缓存机制，在单线程下接近零竞争，但多线程高并发时出现锁竞争（32 线程构造+释放从 13.36 us 增长到 98.40 us）。

### 2. Slice 操作的堆分配

不同实现的 Slice 分配差异：
- OwnerPacket.Slice → 新建 OwnerPacket 对象（class，48 B 堆分配）
- MemoryPacket.Slice → 返回 IPacket 接口导致 struct 装箱（48 B）
- ArrayPacket.Slice(struct) → 返回 ArrayPacket struct，**零分配**
- ArrayPacket.Slice(IPacket) → 返回 IPacket 接口导致 struct 装箱（40 B）
- ReadOnlyPacket.Slice → 返回 IPacket 接口导致 struct 装箱（32 B）

**瓶颈根源**：IPacket 接口返回值要求装箱。ArrayPacket 的 struct Slice 是唯一避免此开销的方案。

### 3. MemoryPacket Indexer 额外开销

MemoryPacket Indexer（~1.66 ns）比 ArrayPacket（~0.24 ns）慢约 6.9 倍。

**瓶颈根源**：MemoryPacket 需要通过 `_memory.Span[index]` 间接访问，多一层 Span 获取开销。

### 4. ToStr 编码瓶颈

ToStr 是所有操作中最耗时的（8192B 时 127 us），且链式包翻倍增长。

**瓶颈根源**：UTF-8 编码的字符串分配是主要开销，大包时分配量与数据量成正比。

### 5. 多线程 GC 压力

32 线程 ToStr 操作内存分配达到 470 MB，触发大量 GC。Slice 操作中 OwnerPacket 和 MemoryPacket 也有 MB 级分配。

**瓶颈根源**：高频创建临时对象（装箱、字符串、数组拌Copy）
