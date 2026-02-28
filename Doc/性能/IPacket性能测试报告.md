# IPacket 数据包性能测试报告

## 一、测试结论

IPacket 体系提供了四种数据包实现（OwnerPacket、MemoryPacket、ArrayPacket、ReadOnlyPacket），核心操作均在**亚纳秒到十几纳秒级别**，性能设计非常优秀。关键发现如下：

1. **ArrayPacket 是综合性能最优的实现**：构造和 GetSpan 被 JIT 完全内联（~0.01 ns），Indexer 读写仅 0.2 ns，struct Slice 是**唯一实现零分配切片**的类型（2.6 ns / 0B）。建议作为通用首选。

2. **OwnerPacket 适合网络 IO 场景**：虽然构造最慢（6.7 ns），但基于 ArrayPool 池化零分配，适合频繁申请/释放缓冲区。多线程高并发时 ArrayPool 存在锁竞争，32 线程构造慢于 struct 实现约 12 倍。

3. **struct 实现天然适合多线程**：MemoryPacket、ArrayPacket、ReadOnlyPacket 三种 struct 在 32 线程下构造仅约 8 us / 3.6 KB，几乎无竞争。ArrayPacket 32 线程 Slice 仅 30 us / 4.5 KB，分配量是 OwnerPacket 的 1/668。

4. **Slice 装箱是主要内存开销来源**：除 ArrayPacket struct Slice 外，其余 Slice 操作均因返回 IPacket 接口而装箱（32-48 B/次）。在已知具体类型时应优先调用 struct 版本。

5. **PacketHelper.ToStr 是性能瓶颈**：UTF-8 编码开销随 Size 线性增长（1KB→11.7 us，8KB→127 us），32 线程时内存分配达 470 MB，应避免在热点路径使用。

---

## 二、测试环境

```
BenchmarkDotNet v0.15.8, Windows 10 (10.0.19045.6456/22H2/2022Update)
Intel Core i9-10900K CPU 3.70GHz, 1 CPU, 20 logical核心, 10 物理核心
.NET SDK 10.0.103
Runtime: .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v3
目标框架: net10.0
运行模式: Release
```

---

## 三、测试方法

### 测试对象

IPacket 接口的四种实现类型及 PacketHelper 扩展方法：

| 类型 | 分类 | 说明 |
|------|------|------|
| OwnerPacket | class | 基于 ArrayPool 池化内存管理 |
| MemoryPacket | struct | 包装 Memory&lt;Byte&gt; |
| ArrayPacket | record struct | 包装 byte[] / ArraySegment |
| ReadOnlyPacket | readonly record struct | 只读数据包，线程安全 |
| PacketHelper | 扩展方法 | Append/ToStr/Clone/ToArray 等通用操作 |

### 测试维度

- **数据大小**：64 B、1024 B、8192 B（覆盖小包、中包、大包场景）
- **操作类型**：构造、GetSpan、GetMemory、TryGetArray、Slice、Resize、Indexer 读写
- **并发维度**：单线程、4 线程、16 线程、32 线程（每线程 1000 次迭代）
- **PacketHelper**：单包/链式包的 Append、ToStr、ToHex、CopyTo、Clone、ToArray 等

### 测试工具

BenchmarkDotNet v0.15.8，使用 `[MemoryDiagnoser]` 采集内存分配，`[Params]` 参数化数据大小和线程数。

---

## 四、测试结果

### 4.1 OwnerPacket 单线程

| 方法 | Size | Mean | Error | StdDev | Gen0 | Allocated |
|------|------|------|-------|--------|------|-----------|
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

### 4.2 OwnerPacket 多线程

| 方法 | ThreadCount | Mean | Error | StdDev | Gen0 | Gen1 | Allocated |
|------|------------|------|-------|--------|------|------|-----------|
| 构造+释放 | 1 | 13.36 us | 0.121 us | 0.135 us | 4.7607 | - | 48.56 KB |
| GetSpan | 1 | 15.30 us | 0.150 us | 0.173 us | 4.7607 | - | 48.56 KB |
| Slice | 1 | 11.18 us | 0.137 us | 0.158 us | 9.3384 | 0.0305 | 95.44 KB |
| 构造+释放 | 4 | 25.49 us | 0.090 us | 0.104 us | 18.6768 | 0.1221 | 189.81 KB |
| GetSpan | 4 | 26.11 us | 0.103 us | 0.110 us | 18.6768 | 0.1221 | 189.81 KB |
| Slice | 4 | 28.16 us | 0.291 us | 0.335 us | 37.1704 | 0.2747 | 377.32 KB |
| 构造+释放 | 16 | 57.07 us | 0.375 us | 0.401 us | 74.2798 | 0.9155 | 753.76 KB |
| GetSpan | 16 | 57.94 us | 0.369 us | 0.425 us | 74.2798 | 0.9155 | 753.79 KB |
| Slice | 16 | 84.88 us | 3.664 us | 4.219 us | 148.3154 | 2.0752 | 1504.06 KB |
| 构造+释放 | 32 | 98.40 us | 0.555 us | 0.639 us | 148.4375 | 2.6855 | 1505.47 KB |
| GetSpan | 32 | 102.63 us | 0.713 us | 0.821 us | 148.4375 | 2.6855 | 1505.53 KB |
| Slice | 32 | 169.03 us | 0.940 us | 1.006 us | 296.6309 | 5.8594 | 3006.05 KB |

### 4.3 MemoryPacket 单线程

| 方法 | Size | Mean | Error | StdDev | Gen0 | Allocated |
|------|------|------|-------|--------|------|-----------|
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

### 4.4 MemoryPacket 多线程

| 方法 | ThreadCount | Mean | Error | StdDev | Gen0 | Gen1 | Allocated |
|------|------------|------|-------|--------|------|------|-----------|
| 构造 | 1 | 2.608 us | 0.0139 us | 0.0160 us | 0.1488 | - | 1.53 KB |
| GetSpan | 1 | 2.716 us | 0.0154 us | 0.0177 us | 0.1602 | - | 1.63 KB |
| Slice | 1 | 6.830 us | 0.0667 us | 0.0768 us | 4.7607 | - | 48.56 KB |
| 构造 | 4 | 2.673 us | 0.0179 us | 0.0207 us | 0.1678 | - | 1.72 KB |
| GetSpan | 4 | 6.361 us | 0.0251 us | 0.0289 us | 0.1755 | - | 1.86 KB |
| Slice | 4 | 16.514 us | 0.0817 us | 0.0940 us | 18.6157 | 0.1221 | 189.66 KB |
| 构造 | 16 | 5.099 us | 0.0219 us | 0.0243 us | 0.2441 | - | 2.55 KB |
| GetSpan | 16 | 13.024 us | 0.1399 us | 0.1611 us | 0.2747 | - | 2.88 KB |
| Slice | 16 | 43.428 us | 0.2211 us | 0.2547 us | 74.2188 | 0.8545 | 753.47 KB |
| 构造 | 32 | 8.176 us | 0.1163 us | 0.1339 us | 0.3510 | - | 3.62 KB |
| GetSpan | 32 | 18.709 us | 0.0367 us | 0.0423 us | 0.3967 | - | 4.06 KB |
| Slice | 32 | 84.033 us | 0.1733 us | 0.1926 us | 148.4375 | 2.4414 | 1505.13 KB |

### 4.5 ArrayPacket 单线程

| 方法 | Size | Mean | Error | StdDev | Gen0 | Allocated |
|------|------|------|-------|--------|------|-----------|
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

### 4.6 ArrayPacket 多线程

| 方法 | ThreadCount | Mean | Error | StdDev | Gen0 | Allocated |
|------|------------|------|-------|--------|------|------|-----------|
| 构造 | 1 | 2.587 us | 0.0084 us | 0.0093 us | 0.1488 | 1.53 KB |
| GetSpan | 1 | 2.615 us | 0.0110 us | 0.0126 us | 0.1488 | 1.53 KB |
| Slice | 1 | 5.132 us | 0.0242 us | 0.0278 us | 0.1602 | 1.69 KB |
| 构造 | 4 | 2.617 us | 0.0134 us | 0.0154 us | 0.1640 | 1.71 KB |
| GetSpan | 4 | 2.688 us | 0.0136 us | 0.0156 us | 0.1678 | 1.72 KB |
| Slice | 4 | 11.341 us | 0.0274 us | 0.0316 us | 0.1831 | 2.01 KB |
| 构造 | 16 | 5.068 us | 0.0112 us | 0.0129 us | 0.2441 | 2.54 KB |
| GetSpan | 16 | 5.064 us | 0.0062 us | 0.0068 us | 0.2441 | 2.54 KB |
| Slice | 16 | 20.879 us | 0.0430 us | 0.0495 us | 0.3052 | 3.14 KB |
| 构造 | 32 | 8.046 us | 0.0165 us | 0.0183 us | 0.3510 | 3.61 KB |
| GetSpan | 32 | 8.009 us | 0.0111 us | 0.0127 us | 0.3510 | 3.60 KB |
| Slice | 32 | 30.160 us | 0.1368 us | 0.1575 us | 0.4272 | 4.50 KB |

### 4.7 ReadOnlyPacket 单线程

| 方法 | Size | Mean | Error | StdDev | Gen0 | Gen1 | Allocated |
|------|------|------|-------|--------|------|------|-----------|
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

### 4.8 ReadOnlyPacket 多线程

| 方法 | ThreadCount | Mean | Error | StdDev | Gen0 | Gen1 | Allocated |
|------|------------|------|-------|--------|------|------|-----------|
| 构造 | 1 | 2.563 us | 0.0107 us | 0.0123 us | 0.1488 | - | 1.53 KB |
| GetSpan | 1 | 2.609 us | 0.0103 us | 0.0119 us | 0.1488 | - | 1.54 KB |
| Slice | 1 | 5.487 us | 0.0415 us | 0.0478 us | 3.2272 | 0.0153 | 32.94 KB |
| 构造 | 4 | 2.615 us | 0.0098 us | 0.0113 us | 0.1640 | - | 1.71 KB |
| GetSpan | 4 | 2.665 us | 0.0288 us | 0.0332 us | 0.1526 | - | 1.71 KB |
| Slice | 4 | 13.900 us | 0.0432 us | 0.0498 us | 12.4664 | 0.0763 | 127.07 KB |
| 构造 | 16 | 5.070 us | 0.0171 us | 0.0197 us | 0.2441 | - | 2.55 KB |
| GetSpan | 16 | 5.223 us | 0.0263 us | 0.0302 us | 0.2441 | - | 2.54 KB |
| Slice | 16 | 33.225 us | 0.1152 us | 0.1281 us | 49.4385 | 0.5493 | 503.29 KB |
| 构造 | 32 | 7.991 us | 0.0186 us | 0.0214 us | 0.3510 | - | 3.61 KB |
| GetSpan | 32 | 8.033 us | 0.0485 us | 0.0559 us | 0.3510 | - | 3.62 KB |
| Slice | 32 | 60.442 us | 1.2823 us | 1.4767 us | 98.7549 | 1.5869 | 1004.79 KB |

### 4.9 PacketHelper 扩展方法单线程

| 方法 | Size | Mean | Error | StdDev | Gen0 | Gen1 | Allocated |
|------|------|------|-------|--------|------|------|-----------|
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

### 4.10 PacketHelper 扩展方法多线程（Size=1024）

| 方法 | ThreadCount | Mean | Error | StdDev | Gen0 | Gen1 | Allocated |
|------|------------|------|-------|--------|------|------|-----------|
| ToStr | 1 | 11,784 us | 15.020 us | 16.071 us | 1515.6250 | - | 15,580 KB |
| ToArray | 1 | 56.60 us | 0.541 us | 0.623 us | 104.3701 | 0.6104 | 1064 KB |
| Clone | 1 | 62.07 us | 0.688 us | 0.765 us | 108.1543 | 0.6104 | 1103 KB |
| ReadBytes | 1 | 34.87 us | 0.331 us | 0.381 us | 55.2979 | 0.3052 | 564 KB |
| ExpandHeader | 1 | 14.98 us | 0.156 us | 0.180 us | 12.3901 | 0.0610 | 127 KB |
| ToStr | 4 | 12,221 us | 60.739 us | 67.511 us | 6265.6250 | - | 61,096 KB |
| ToArray | 4 | 171.97 us | 2.001 us | 2.304 us | 447.9980 | 3.6621 | 4252 KB |
| Clone | 4 | 177.08 us | 1.508 us | 1.736 us | 448.4863 | 5.3711 | 4409 KB |
| ReadBytes | 4 | 93.04 us | 0.488 us | 0.542 us | 224.6094 | 1.8311 | 2252 KB |
| ExpandHeader | 4 | 33.05 us | 0.231 us | 0.266 us | 49.3164 | 0.4272 | 502 KB |
| ToStr | 16 | 23,688 us | 51.780 us | 57.553 us | 25062.5000 | 62.5000 | 248,880 KB |
| ToArray | 16 | 924.55 us | 9.793 us | 11.277 us | 1792.9688 | 17.5781 | 17,005 KB |
| Clone | 16 | 917.78 us | 0.987 us | 1.056 us | 1793.9453 | 25.3906 | 17,630 KB |
| ReadBytes | 16 | 461.69 us | 1.679 us | 1.649 us | 898.4375 | 14.6484 | 9005 KB |
| ExpandHeader | 16 | 109.91 us | 1.506 us | 1.735 us | 197.0215 | 3.0518 | 2004 KB |
| ToStr | 32 | 51,149 us | 2528.2 us | 2911.4 us | 50090.9091 | 181.8182 | 481,257 KB |
| ToArray | 32 | 2,026.9 us | 47.035 us | 54.166 us | 3585.9375 | 23.4375 | 34,007 KB |
| Clone | 32 | 2,082.2 us | 42.863 us | 49.361 us | 3585.9375 | 39.0625 | 35,257 KB |
| ReadBytes | 32 | 1,044.2 us | 19.457 us | 22.407 us | 1794.9219 | 25.3906 | 18,007 KB |
| ExpandHeader | 32 | 237.57 us | 3.200 us | 3.685 us | 394.0430 | 9.5215 | 4007 KB |

---

## 五、核心指标

### 5.1 单线程吞吐量（Size=1024，ops/s）

| 操作 | OwnerPacket | MemoryPacket | ArrayPacket | ReadOnlyPacket |
|------|-------------|-------------|-------------|----------------|
| 构造 | 149.5M | 2,222M | JIT内联† | 4,545M |
| GetSpan | 88.9M | 586M | JIT内联† | JIT内联† |
| GetMemory | 114.4M | 745M | 978M | 879M |
| TryGetArray | 113.2M | 1,027M | JIT内联† | 4,854M |
| Slice | 154.6M | 162.4M | 383M‡ | 213M |
| Indexer读 | 1,029M | 601M | 4,170M | 1,190M |
| Indexer写 | 1,261M | 640M | 4,808M | 不支持 |

> † 标记为"JIT内联"的操作耗时 < 0.02 ns，被 JIT 完全优化消除，等效吞吐量 > 50,000M ops/s。
> ‡ ArrayPacket Slice(struct) 为零分配版本；Slice(IPacket) 约 88.7M ops/s。

### 5.2 PacketHelper 关键操作吞吐量（单线程）

| 操作 | 64 B | 1024 B | 8192 B | 是否随 Size 增长 |
|------|------|--------|--------|----------------|
| Append | 110M ops/s | 110M ops/s | 110M ops/s | 否 |
| TryGetSpan | JIT内联 | JIT内联 | JIT内联 | 否 |
| GetStream | 79.5M ops/s | 79.6M ops/s | 79.5M ops/s | 否 |
| ToSegment(单包) | 123.6M ops/s | 123.0M ops/s | 123.5M ops/s | 否 |
| ToHex | 12.8M ops/s | 12.6M ops/s | 12.4M ops/s | 否（仅编码前32字节） |
| ToArray | 69.9M ops/s | 18.8M ops/s | 2.6M ops/s | **是** |
| Clone | 54.1M ops/s | 17.0M ops/s | 2.7M ops/s | **是** |
| ToStr | 1.5M ops/s | 85K ops/s | 7.9K ops/s | **是（最慢）** |

---

## 六、对比分析

### 6.1 横向对比：四种实现单线程性能差异（Size=1024）

| 操作 | OwnerPacket | MemoryPacket | ArrayPacket | ReadOnlyPacket | 最快 vs 最慢 |
|------|-------------|-------------|-------------|----------------|-------------|
| 构造 | 6.69 ns | 0.45 ns | ~0.01 ns | 0.22 ns | ArrayPacket 快 **669×** |
| GetSpan | 11.25 ns | 1.71 ns | ~0.01 ns | ~0.01 ns | ArrayPacket 快 **1125×** |
| GetMemory | 8.74 ns | 1.34 ns | 1.02 ns | 1.14 ns | ArrayPacket 快 **8.6×** |
| TryGetArray | 8.83 ns | 0.97 ns | ~0 ns | 0.21 ns | ArrayPacket 快 **8830×** |
| Slice | 6.47 ns / 48B | 6.16 ns / 48B | 2.61 ns / **0B**† | 4.69 ns / 32B | ArrayPacket 快 **2.5×**，分配低 **∞** |
| Indexer读 | 0.97 ns | 1.66 ns | 0.24 ns | 0.84 ns | ArrayPacket 快 **6.9×** |
| Indexer写 | 0.79 ns | 1.56 ns | 0.21 ns | 不支持 | ArrayPacket 快 **7.4×** |

> † ArrayPacket.Slice 返回 struct 版本零分配；通过 IPacket 接口调用时装箱分配 40 B（11.27 ns）。

**关键发现**：
- ArrayPacket 在所有操作维度均为最快，构造/GetSpan/TryGetArray 被 JIT 完全内联消除
- OwnerPacket 各操作均为最慢，但提供了**池化内存管理**能力，这是其他 struct 实现无法替代的
- MemoryPacket Indexer 比 ArrayPacket 慢 6.9×，因为需要经过 `Memory.Span[index]` 间接访问

### 6.2 横向对比：四种实现多线程 Slice 性能（Size=1024，32线程）

| 实现 | Mean | Allocated | 相对 ArrayPacket 耗时 | 相对 ArrayPacket 分配 |
|------|------|-----------|---------------------|---------------------|
| ArrayPacket | 30.16 us | 4.50 KB | **1.0×（基准）** | **1.0×（基准）** |
| ReadOnlyPacket | 60.44 us | 1004.79 KB | 2.0× | 223× |
| MemoryPacket | 84.03 us | 1505.13 KB | 2.8× | 334× |
| OwnerPacket | 169.03 us | 3006.05 KB | 5.6× | 668× |

**关键发现**：
- ArrayPacket 32 线程 Slice 的内存分配仅 4.50 KB，因 struct Slice 零装箱
- OwnerPacket 分配量是 ArrayPacket 的 668 倍，GC 压力显著

### 6.3 纵向对比：OwnerPacket 并发扩展趋势

| 操作 | 1线程 | 4线程 | 16线程 | 32线程 | 1→32增长倍数 |
|------|-------|-------|--------|--------|------------|
| 构造+释放 | 13.36 us | 25.49 us | 57.07 us | 98.40 us | 7.4× |
| GetSpan | 15.30 us | 26.11 us | 57.94 us | 102.63 us | 6.7× |
| Slice | 11.18 us | 28.16 us | 84.88 us | 169.03 us | 15.1× |

- 1→4 线程增长约 1.9×，接近线性，ArrayPool 低并发竞争小
- 4→16 线程增长约 2.2×，出现 ArrayPool 锁竞争，但 10 核 CPU 扩展性仍良好
- 16→32 线程增长约 1.7×，超过物理核心数（10核），CPU 调度开销成为主因
- Slice 增长最陡（15.1×），因每次创建新对象叠加 GC 压力

### 6.4 纵向对比：struct 实现并发扩展趋势（构造操作）

| 实现 | 1线程 | 4线程 | 16线程 | 32线程 | 1→32增长倍数 |
|------|-------|-------|--------|--------|------------|
| ArrayPacket | 2.587 us | 2.617 us | 5.068 us | 8.046 us | 3.1× |
| MemoryPacket | 2.608 us | 2.673 us | 5.099 us | 8.176 us | 3.1× |
| ReadOnlyPacket | 2.563 us | 2.615 us | 5.070 us | 7.991 us | 3.1× |

- 三种 struct 实现增长趋势**完全一致**（3.1×），说明仅由线程调度开销决定，无锁竞争
- 内存分配均维持在 1.5→3.6 KB，与线程数线性增长（Task 开销），几乎无额外 GC 压力
- 对比 OwnerPacket（7.4× 增长），struct 实现的多线程扩展性优 **2.4 倍**

### 6.5 纵向对比：PacketHelper 并发扩展趋势（Size=1024）

| 操作 | 1线程 | 4线程 | 16线程 | 32线程 | 1→32增长倍数 | 32线程分配 |
|------|-------|-------|--------|--------|------------|----------|
| ToStr | 11,784 us | 12,221 us | 23,688 us | 51,149 us | 4.3× | 481 MB |
| ToArray | 56.60 us | 171.97 us | 924.55 us | 2,026.9 us | 35.8× | 34 MB |
| Clone | 62.07 us | 177.08 us | 917.78 us | 2,082.2 us | 33.6× | 35 MB |
| ReadBytes | 34.87 us | 93.04 us | 461.69 us | 1,044.2 us | 29.9× | 18 MB |
| ExpandHeader | 14.98 us | 33.05 us | 109.91 us | 237.57 us | 15.9× | 4 MB |

- **ToStr** 增长倍数最低（4.3×）但绝对值最高（51 ms），瓶颈在 UTF-8 编码的 CPU 密集计算
- **ToArray / Clone** 增长倍数最高（33-36×），大量数据拷贝引发严重 GC 竞争
- **ExpandHeader** 增长相对温和（15.9×），分配量小（80 B/次），GC 压力最低

### 6.6 PacketHelper 链式包 vs 单包开销比（单线程）

| 操作 | 64 B 单包 | 64 B 链式包 | 比值 | 8192 B 单包 | 8192 B 链式包 | 比值 |
|------|----------|-----------|------|-----------|-------------|------|
| ToStr | 656.70 ns | 1,437.68 ns | 2.2× | 127,085 ns | 248,279 ns | 2.0× |
| ToHex | 78.36 ns | 99.62 ns | 1.3× | 80.84 ns | 100.05 ns | 1.2× |
| ToSegment | 8.09 ns | 45.13 ns | 5.6× | 8.10 ns | 949.79 ns | 117× |

- **ToStr** 链式包约为单包的 2.0-2.2×，额外开销来自遍历链表和 StringBuilder 拼接
- **ToSegment** 链式包开销随 Size 急剧增长（小包 5.6×，大包 117×），因为需要合并数据到单一连续数组
- **ToHex** 链式包开销稳定（1.2-1.3×），仅编码前 32 字节，不受 Size 影响

---

## 七、性能瓶颈定位

### 瓶颈 1：OwnerPacket 多线程 ArrayPool 竞争

- **现象**：32 线程构造+释放 98.40 us，是 struct 实现（~8 us）的 12 倍
- **根源**：`ArrayPool<Byte>.Shared` 内部分桶 + TLS 缓存机制在高并发下出现锁竞争
- **影响**：网络 IO 场景中每个包都需 OwnerPacket，高并发时成为瓶颈

### 瓶颈 2：Slice 操作的 IPacket 装箱开销

- **现象**：MemoryPacket.Slice 分配 48 B，ReadOnlyPacket.Slice 分配 32 B，ArrayPacket.Slice(IPacket) 分配 40 B
- **根源**：IPacket 接口返回值导致 struct 必须装箱到堆上
- **影响**：高频 Slice 场景（如协议解析逐层剥离）产生大量短生命周期对象，增加 GC 压力

### 瓶颈 3：PacketHelper.ToStr UTF-8 编码

- **现象**：8192 B 时 127 us / 127 KB，链式包翻倍至 248 us / 312 KB
- **根源**：UTF-8 编码创建的 String 分配与数据量成正比，无法避免
- **影响**：在日志输出、调试打印等场景中可能成为意外热点

### 瓶颈 4：MemoryPacket Indexer 间接访问

- **现象**：1.66 ns，比 ArrayPacket（0.24 ns）慢 6.9×
- **根源**：需经过 `Memory<Byte>.Span[index]` 间接访问，多一层 Span 获取开销
- **影响**：逐字节处理场景（如协议头解析）中累积开销

### 瓶颈 5：链式包 ToSegment 数据合并

- **现象**：8192 B 链式包 950 ns / 16.5 KB，是单包（8.1 ns / 40 B）的 117×
- **根源**：链式包必须分配新数组并拷贝所有分片数据到连续内存
- **影响**：频繁对链式包调用 ToSegment 会产生大量临时数组

---

## 八、优化建议

### 高优先级

| # | 建议 | 预期收益 |
|---|------|---------|
| 1 | **优先使用 ArrayPacket struct Slice**：在调用方已知具体类型时，直接调用返回 ArrayPacket 的 Slice 重载避免装箱 | Slice 从 11.3 ns / 40B 降至 2.6 ns / 0B，**提速 4.3×，消除分配** |
| 2 | **为 MemoryPacket / ReadOnlyPacket 添加返回自身类型的 Slice 重载**：避免 IPacket 装箱 | MemoryPacket Slice 从 6.2 ns / 48B 降至约 2-3 ns / 0B |
| 3 | **OwnerPacket 多线程池化优化**：超高并发场景使用 `ThreadLocal<T>` 缓存或自定义 Per-Thread 内存池替代 `ArrayPool.Shared` | 32 线程构造从 98.40 us 降至接近 struct 水平（~8 us），**提速约 12×** |

### 中优先级

| # | 建议 | 预期收益 |
|---|------|---------|
| 4 | **MemoryPacket Indexer 优化**：构造时通过 `MemoryMarshal.TryGetArray` 提取底层数组引用并缓存，Indexer 直接用数组下标访问 | Indexer 从 1.66 ns 降至约 0.24 ns，**提速约 7×** |
| 5 | **ToStr 流式编码 API**：大包场景提供 `WriteTo(TextWriter)` 避免一次性分配完整字符串 | 8192 B 场景减少 127 KB 临时分配 |
| 6 | **链式包 ToStr 预分配优化**：预计算 Total 长度后一次性分配 char[]，减少 StringBuilder 扩容 | 链式包 ToStr 开销从 2.0× 降至接近 1.0× |

### 低优先级

| # | 建议 | 预期收益 |
|---|------|---------|
| 7 | **链式包 Total 属性缓存**：在 Append 时缓存总长度，避免每次递归遍历 | 长链路多次访问 Total 时避免重复遍历 |
| 8 | **ToSegment 链式包零拷贝接口**：提供 `IReadOnlyList<ArraySegment<Byte>>` 返回各分片，避免合并拷贝 | 8192 B 链式包 ToSegment 从 950 ns / 16.5 KB 降至约 18 ns / 160 B |

---

## 九、总结

| 实现类型 | 适用场景 | 关键优势 | 主要开销 |
|---------|---------|---------|---------|
| OwnerPacket | 网络 IO 频繁申请/释放缓冲区 | ArrayPool 池化零分配构造/释放 | ArrayPool 竞争（多线程）、Slice 48B |
| MemoryPacket | 包装 Memory&lt;Byte&gt; 的临时场景 | struct 零堆分配，构造 0.45 ns | Slice 装箱 48B、Indexer 间接访问 |
| ArrayPacket | **通用首选**，零拷贝缓冲区切片 | struct Slice **零分配**，最快索引访问 | IPacket Slice 装箱 40B |
| ReadOnlyPacket | 多线程共享只读数据 | readonly struct，线程安全，Slice 仅 32B | 不支持写、IPacket 拷贝构造随 Size 增长 |
