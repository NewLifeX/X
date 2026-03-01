---
applyTo: "**/Benchmark/**"
---

# 性能测试指令

适用于性能测试、压力测试、基准测试、BenchmarkDotNet 相关任务。

---

## 1. 项目结构

- 基准测试统一放在 `Benchmark/` 项目，按主题分子目录（如 `PacketBenchmarks/`、`CacheBenchmarks/`）
- 入口 `Program.cs` 使用 `BenchmarkSwitcher` 模式，**不要修改**
- TFM 使用最新稳定版，`<LangVersion>latest</LangVersion>`

## 2. 代码规范

遵循主指令全部编码规范（类型名用 `String`/`Int32` 等、file-scoped namespace），另有以下补充：

- **命名空间**：`Benchmark.{主题}Benchmarks`
- **类名**：`{被测类型}Benchmark` 或 `{被测主题}Benchmark`
- **必须标注** `[MemoryDiagnoser]` 和 `[SimpleJob]`（需调整迭代次数时用 `[SimpleJob(iterationCount: N)]`）
- **方法描述**：`[Benchmark(Description = "中文描述")]`，方便报告阅读
- **参数化**：用 `[Params]` 或 `[ParamsSource]` 控制数据规模
- **初始化 / 清理**：分别放 `[GlobalSetup]` 和 `[GlobalCleanup]`
- **分组**：同类测试用 `#region` 分组
- **多线程并发**：动态线程数包含 CPU 核心数，推荐模板：

```csharp
public static IEnumerable<Int32> ThreadCounts
{
    get
    {
        var cores = Environment.ProcessorCount;
        var set = new SortedSet<Int32> { 1, 4, 8, 32 };
        set.Add(cores);
        return set;
    }
}

[ParamsSource(nameof(ThreadCounts))]
public Int32 ThreadCount { get; set; }
```

## 3. 运行要求

- 必须以 **Release 模式**运行，获取有代表性的峰值数据
- 运行全部：`dotnet run -c Release`
- 运行指定类：`dotnet run -c Release -- --filter *ClassName*`
- ❌ 禁止在 Debug 模式下采集数据写入报告

## 4. 测试维度

- **并发维度**：单线程 + 多线程（多线程含与当前 CPU 核心数相同的并发数）
- **操作维度**：单一操作 + 批量操作

## 5. 常见错误

- ❌ 在 `[Benchmark]` 方法内做初始化（应放 `[GlobalSetup]`）
- ❌ 忽略返回值导致 JIT 死码消除（确保返回或赋值给字段）
- ❌ 手动 `Stopwatch` 计时（BDN 自动处理）
- ❌ `using` 的 `Dispose` 开销混入测量（仅在测试 Dispose 本身时才包含）

## 6. 报告存放

`Doc/Benchmark/{测试主题}性能测试.md`（UTF-8 无 BOM）

## 7. 报告结构（顺序固定）

1. **性能概览**（放最前：一句话点明被测功能用途 + 简单语言总结核心发现）
2. 测试环境 → 测试方法 → 测试结果（BDN 原始表格，保留 Mean/Error/StdDev/Allocated）
3. 核心指标（换算 msg/s、QPS 等业务指标）
4. **对比分析**
   - 纵向：同场景不同并发趋势，找出最优并发点
   - 横向：不同方案同并发差异百分比
5. 性能瓶颈定位 → 优化建议（按优先级排序，含预期收益）
