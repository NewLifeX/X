# IPacket 性能测试报告

## 1. 概述

本报告针对 `NewLife.Data` 命名空间中 `IPacket` 接口的四个主要实现者以及 `PacketHelper` 扩展方法进行全面的性能基准测试。

### 1.1 测试对象

| 类型 | 说明 | 内存管理 |
|------|------|----------|
| **OwnerPacket** | 基于 `ArrayPool<Byte>.Shared` 的池化内存包 | 需手动 Dispose 归还 |
| **MemoryPacket** | 基于 `Memory<Byte>` 的内存包（struct） | 无所有权，不管理内存生命周期 |
| **ArrayPacket** | 基于 `Byte[]` 的数组包（record struct） | 无池化，依赖 GC |
| **ReadOnlyPacket** | 只读数组包（readonly record struct） | 无池化，不可变，线程安全 |
| **PacketHelper** | 扩展方法集：链式操作、数据转换、流操作、数组操作等 | — |

### 1.2 测试框架与环境

- **测试框架**：BenchmarkDotNet v0.15.8
- **运行时**：.NET 8.0 / .NET 9.0
- **诊断器**：MemoryDiagnoser（测量 GC 分配与回收）
- **数据大小**：64B / 1KB / 64KB（覆盖小包、中包、大包场景）
- **并发线程**：1 / 4 / 16 / 32 线程

## 2. 测试项目结构

```
Benchmark/
├── Benchmark.csproj                              # 项目文件
├── Program.cs                                     # 入口，配置 MemoryDiagnoser
└── PacketBenchmarks/
    ├── OwnerPacketBenchmark.cs                    # OwnerPacket 独立测试
    ├── MemoryPacketBenchmark.cs                   # MemoryPacket 独立测试
    ├── ArrayPacketBenchmark.cs                    # ArrayPacket 独立测试
    ├── ReadOnlyPacketBenchmark.cs                 # ReadOnlyPacket 独立测试
    ├── PacketHelperBenchmark.cs                   # PacketHelper 扩展方法测试
    ├── PacketComparisonBenchmark.cs               # 四种实现横向对比
    └── PacketConcurrencyBenchmark.cs              # 多线程并发测试
```

## 3. 运行方式

### 3.1 运行全部基准测试

```bash
dotnet run --project Benchmark/Benchmark.csproj -f net8.0 -c Release -- --filter "*"
```

### 3.2 运行单个测试类

```bash
# OwnerPacket 测试
dotnet run --project Benchmark/Benchmark.csproj -f net8.0 -c Release -- --filter "*OwnerPacketBenchmark*"

# MemoryPacket 测试
dotnet run --project Benchmark/Benchmark.csproj -f net8.0 -c Release -- --filter "*MemoryPacketBenchmark*"

# ArrayPacket 测试
dotnet run --project Benchmark/Benchmark.csproj -f net8.0 -c Release -- --filter "*ArrayPacketBenchmark*"

# ReadOnlyPacket 测试
dotnet run --project Benchmark/Benchmark.csproj -f net8.0 -c Release -- --filter "*ReadOnlyPacketBenchmark*"

# PacketHelper 测试
dotnet run --project Benchmark/Benchmark.csproj -f net8.0 -c Release -- --filter "*PacketHelperBenchmark*"

# 横向对比测试
dotnet run --project Benchmark/Benchmark.csproj -f net8.0 -c Release -- --filter "*PacketComparisonBenchmark*"

# 并发测试
dotnet run --project Benchmark/Benchmark.csproj -f net8.0 -c Release -- --filter "*PacketConcurrencyBenchmark*"
```

### 3.3 列出所有可用基准

```bash
dotnet run --project Benchmark/Benchmark.csproj -f net8.0 -c Release -- --list flat
```

### 3.4 导出结果

BenchmarkDotNet 默认会在项目根目录生成 `BenchmarkDotNet.Artifacts/` 文件夹，包含：
- Markdown 格式的结果表格
- CSV 格式的原始数据
- 日志文件

## 4. 测试用例详解

### 4.1 OwnerPacket 基准测试（11 项 × 3 数据大小）

| 测试方法 | 说明 | 关注指标 |
|---------|------|---------|
| 构造_从池租用 | `new OwnerPacket(size)` + `Dispose()` | 池化租借/归还耗时 |
| 构造_包装数组 | `new OwnerPacket(buf, 0, len, false)` | 零拷贝包装开销 |
| GetSpan | 获取 Span 视图 | 内存访问延迟 |
| GetMemory | 获取 Memory 视图 | 内存访问延迟 |
| TryGetArray | 获取 ArraySegment | 数组段提取开销 |
| Slice_不转移所有权 | 切片，共享缓冲区 | 零拷贝切片 |
| Slice_转移所有权 | 切片，转移管理权 | 所有权转移开销 |
| 索引器读取 | `pk[index]` 读取 | 随机读性能 |
| 索引器写入 | `pk[index] = value` 写入 | 随机写性能 |
| Resize | 调整有效长度 | 大小调整开销 |
| Dispose归还池 | `using` 释放 | 池归还延迟 |

### 4.2 MemoryPacket 基准测试（8 项 × 3 数据大小）

| 测试方法 | 说明 | 关注指标 |
|---------|------|---------|
| 构造 | `new MemoryPacket(memory, len)` | 构造开销 |
| GetSpan | 获取 Span 视图 | Span 切片延迟 |
| GetMemory | 获取 Memory 视图 | Memory 切片延迟 |
| TryGetArray | 获取 ArraySegment | Marshal 提取开销 |
| Slice | 切片操作 | 零拷贝切片 |
| 索引器读取 | 字节读取 | Memory.Span 访问 |
| 索引器写入 | 字节写入 | Memory.Span 访问 |
| Total属性 | 计算总长度 | 链式遍历开销 |

### 4.3 ArrayPacket 基准测试（12 项 × 3 数据大小）

| 测试方法 | 说明 | 关注指标 |
|---------|------|---------|
| 构造_字节数组 | `new ArrayPacket(buf)` | 最轻量构造 |
| 构造_ArraySegment | `new ArrayPacket(segment)` | 段构造开销 |
| 构造_偏移 | `new ArrayPacket(buf, off, cnt)` | 带偏移构造 |
| GetSpan | 获取 Span 视图 | 直接数组切片 |
| GetMemory | 获取 Memory 视图 | Memory 构造 |
| TryGetArray | 获取 ArraySegment | ArraySegment 构造 |
| Slice | 切片操作 | record struct 复制 |
| 索引器读取/写入 | 字节访问 | 数组直接访问 |
| Total属性 | 计算总长度 | 属性访问 |
| 隐式转换_字节数组 | `byte[]` → `ArrayPacket` | 隐式转换开销 |
| 隐式转换_字符串 | `string` → `ArrayPacket` | 含 GetBytes 分配 |

### 4.4 ReadOnlyPacket 基准测试（12 项 × 3 数据大小）

| 测试方法 | 说明 | 关注指标 |
|---------|------|---------|
| 构造_字节数组 | 零拷贝包装 | 构造开销 |
| 构造_ArraySegment | 段构造 | 构造开销 |
| 构造_偏移 | 带偏移构造 | 参数校验开销 |
| 构造_从IPacket深拷贝 | `new ReadOnlyPacket(IPacket)` | 深拷贝分配 |
| GetSpan | Span 视图 | 只读视图 |
| GetMemory | Memory 视图 | 只读视图 |
| TryGetArray | ArraySegment | 直接返回 |
| Slice | 切片 | readonly struct 复制 |
| 索引器读取 | 含边界检查 | 安全读取 |
| Total属性 | 直接返回 Length | 无链式遍历 |
| ToArray | 字节数组副本 | 可能零拷贝 |
| 隐式转换 | `byte[]` → `ReadOnlyPacket` | 转换开销 |

### 4.5 PacketHelper 扩展方法基准测试（21 项 × 3 数据大小）

| 分类 | 测试方法 | 说明 |
|------|---------|------|
| **链式操作** | Append_IPacket | 追加 IPacket 到链尾 |
| | Append_ByteArray | 追加字节数组到链尾 |
| **数据转换** | ToStr_单包 | 单包 UTF-8 转字符串 |
| | ToStr_链式包 | 链式包转字符串（StringBuilder 池化） |
| | ToHex_单包 | 转十六进制（32 字节） |
| | ToHex_带分隔符 | 转十六进制（含分隔符） |
| | ToHex_链式包 | 链式包转十六进制 |
| **流操作** | CopyTo | 复制到 MemoryStream |
| | GetStream_单包 | 获取 MemoryStream 视图 |
| | GetStream_链式包 | 链式包获取流 |
| **数据段** | ToSegment_单包 | 单包获取 ArraySegment |
| | ToSegment_链式包 | 链式包聚合到新数组 |
| | ToSegments | 获取分段列表 |
| | ToArray_单包 | 转字节数组 |
| | ToArray_链式包 | 链式包聚合转数组 |
| **数据读取** | ReadBytes_全部 | 读取全部字节 |
| | ReadBytes_切片 | 读取部分字节 |
| | Clone | 深拷贝 |
| | Clone_链式包 | 链式包深拷贝 |
| **内存访问** | TryGetSpan_单包 | 尝试获取 Span（成功） |
| | TryGetSpan_链式包 | 尝试获取 Span（失败） |
| **头部扩展** | ExpandHeader_ArrayPacket有空间 | 原地扩展 |
| | ExpandHeader_创建新包 | 创建新 OwnerPacket |
| | ExpandHeader_OwnerPacket有空间 | 原地扩展 |

### 4.6 横向对比基准测试（24 项 × 3 数据大小）

将四种实现在**相同操作**下进行横向对比，覆盖：
- **构造**：OwnerPacket（含池化）vs MemoryPacket vs ArrayPacket vs ReadOnlyPacket
- **GetSpan**：各实现的 Span 获取性能
- **GetMemory**：各实现的 Memory 获取性能
- **Slice**：各实现的切片性能
- **TryGetArray**：各实现的数组段获取性能
- **Indexer**：各实现的索引器访问性能

### 4.7 多线程并发基准测试（20 项 × 4 线程数）

每个测试在 1/4/16/32 个线程下运行，每线程执行 1000 次操作。覆盖：

| 分类 | 测试项目 | 并发关注点 |
|------|---------|-----------|
| OwnerPacket | 构造与释放 | ArrayPool 线程安全性能 |
| | GetSpan | 并发读性能 |
| | Slice | 并发切片性能 |
| ArrayPacket | 构造 | struct 构造无竞争 |
| | GetSpan | 并发 Span 创建 |
| | Slice | 并发切片 |
| | ToArray | 并发数组拷贝与 GC 压力 |
| MemoryPacket | 构造 | struct 构造 |
| | GetSpan | 并发 Span 创建 |
| | Slice | 并发切片 |
| ReadOnlyPacket | 构造 | readonly struct 构造 |
| | GetSpan | 并发只读 Span |
| | Slice | 并发只读切片 |
| PacketHelper | ToStr | 并发字符串转换与 StringBuilder 池竞争 |
| | ToHex | 并发十六进制转换 |
| | Clone | 并发深拷贝与 GC 压力 |
| | ReadBytes | 并发字节读取 |
| | ToSegment | 并发段获取 |

## 5. 预期性能特征分析

### 5.1 构造性能

| 实现 | 预期开销 | 原因 |
|------|---------|------|
| **ArrayPacket** | ⭐ 最快 | record struct，仅赋值字段 |
| **ReadOnlyPacket** | ⭐ 最快 | readonly record struct，含参数校验 |
| **MemoryPacket** | ⭐ 接近最快 | struct，含参数校验 |
| **OwnerPacket** | ⚡ 较慢 | 需要从 ArrayPool 租借缓冲区 |

### 5.2 内存分配

| 实现 | GC 分配 | 原因 |
|------|--------|------|
| **ArrayPacket** | 零分配 | struct，共享原数组 |
| **ReadOnlyPacket** | 零分配 | struct，共享原数组 |
| **MemoryPacket** | 零分配 | struct，共享 Memory |
| **OwnerPacket** | 有分配 | class 实例 + ArrayPool.Rent |

### 5.3 GetSpan / GetMemory

所有实现均为 O(1) 操作，预期差异极小：
- **ArrayPacket / ReadOnlyPacket**：直接构造 `new Span<Byte>(buf, off, len)`
- **MemoryPacket**：通过 `Memory.Span` 切片
- **OwnerPacket**：同 ArrayPacket

### 5.4 Slice 切片

| 实现 | 特点 |
|------|------|
| **ArrayPacket** | 零拷贝，返回新 record struct |
| **ReadOnlyPacket** | 零拷贝，返回新 readonly record struct |
| **MemoryPacket** | 零拷贝，Memory 切片 |
| **OwnerPacket** | 零拷贝，可能伴随 new class 实例 |

### 5.5 并发扩展性

| 实现 | 并发预期 |
|------|---------|
| **ArrayPacket** | 线性扩展，无共享状态 |
| **ReadOnlyPacket** | 线性扩展，不可变 |
| **MemoryPacket** | 线性扩展，无共享状态 |
| **OwnerPacket** | 受 ArrayPool 锁竞争影响 |
| **PacketHelper.ToStr** | 受 StringBuilder 池竞争影响 |

## 6. 使用建议

### 6.1 选型指南

| 场景 | 推荐实现 | 理由 |
|------|---------|------|
| 网络收发缓冲区 | **OwnerPacket** | 池化复用，减少大量 GC |
| 协议解析临时切片 | **ArrayPacket** | 零分配，高吞吐 |
| 多线程共享只读数据 | **ReadOnlyPacket** | 不可变，天然线程安全 |
| Memory/IMemoryOwner 适配 | **MemoryPacket** | 适配 Memory-based API |
| 一次性数据传递 | **ArrayPacket** | 最轻量级 |

### 6.2 性能优化建议

1. **避免不必要的 ToArray**：优先使用 `GetSpan()` / `GetMemory()` 避免数组拷贝
2. **合理使用所有权转移**：`Slice(offset, count, transferOwner: true)` 避免重复释放
3. **利用单包快速路径**：PacketHelper 的大多数方法对单包（无 Next）有专门的快速路径优化
4. **控制链式包长度**：过长的链式结构会导致遍历开销增大
5. **注意 OwnerPacket 的及时释放**：未及时 Dispose 会导致 ArrayPool 碎片化

## 7. 测试覆盖统计

| 类别 | 测试类 | 方法数 | 参数组合 | 合计用例 |
|------|--------|--------|---------|---------|
| 独立测试 | OwnerPacketBenchmark | 11 | × 3 | 33 |
| 独立测试 | MemoryPacketBenchmark | 8 | × 3 | 24 |
| 独立测试 | ArrayPacketBenchmark | 12 | × 3 | 36 |
| 独立测试 | ReadOnlyPacketBenchmark | 12 | × 3 | 36 |
| 扩展方法 | PacketHelperBenchmark | 21 | × 3 | 63 |
| 横向对比 | PacketComparisonBenchmark | 24 | × 3 | 72 |
| 并发测试 | PacketConcurrencyBenchmark | 20 | × 4 | 80 |
| **合计** | **7 个测试类** | **108** | | **344** |

## 8. 附录

### 8.1 数据大小选择依据

| 大小 | 典型场景 |
|------|---------|
| 64 B | 小型控制指令、心跳包、短消息 |
| 1 KB | 常规 RPC 请求/响应、JSON 消息 |
| 64 KB | 文件传输分片、大型序列化对象 |

### 8.2 并发线程数选择依据

| 线程数 | 场景 |
|--------|------|
| 1 | 基线单线程性能 |
| 4 | 典型小型服务器 |
| 16 | 中型服务器 |
| 32 | 高并发服务器 |

### 8.3 关键指标说明

| 指标 | 说明 |
|------|------|
| Mean | 平均执行时间 |
| Error | 99.9% 置信区间的一半 |
| StdDev | 标准差 |
| Gen0/Gen1/Gen2 | 各代 GC 收集次数 |
| Allocated | 每次操作分配的托管内存 |
