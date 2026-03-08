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

## 7. 报告结构（顺序固定，精简为主）

1. **性能概览**（一句话用途 + 核心结论，3~5 条要点，每条一句话）
2. **测试环境**（CPU / OS / Runtime，代码块 3~4 行即可）
3. **测试结果**（BDN 原始表格，保留 Mean / Error / StdDev / Allocated）
4. **结果分析**（见下方约束）
5. **瓶颈与优化建议**（见第 8 节，仅列有数据支撑的瓶颈）

### 7.1 结果分析约束

结果分析是对 BDN 数据的**提炼**，不是重新排列。遵循以下规则：

- **禁止重复制表**：不要把 BDN 数据换个单位（如 ops/s）再列一张完整表格；如需标注业务指标，在正文中用"XXX 操作约 N M ops/s"一笔带过
- **对比用文字而非新表**：横向/纵向对比直接写结论（"ArrayPacket 构造比 OwnerPacket 快 ~670x，Slice 零分配"），不为每个对比维度单独建表
- **仅在对比维度 ≥3 且差异显著时**才建一张对比表，且最多一张
- **总量控制**：结果分析文字不超过 BDN 原始表格总行数的 1/3

### 7.2 篇幅控制

- 分析 + 瓶颈部分的文字行数 ≤ 数据表格行数（含表头）
- 无需为每个数字都单独解读；读者能从原表看出的趋势不必复述

## 8. 瓶颈与优化建议规范

### 8.1 撰写原则

- **数据驱动**：所有结论必须有 BDN 实测数据支撑，**禁止无数据臆测**，没有 profiler 数据时不编造 ns 级拆解
- **量化表达**：用"快 X 倍"、"省 Y%"、"降 Z B/op"，避免"显著""明显"等模糊词
- **可操作**：指明具体修改位置和方案，不泛泛建议
- **宁少勿凑**：只列真正有影响的瓶颈（通常 1~3 个），不为凑数列 P3 级微小问题

### 8.2 瓶颈表格（唯一模板，合并展示）

用一张表汇总，不再拆分多张表：

```markdown
| 优先级 | 瓶颈 | 现象（实测数据） | 优化方向 | 预期收益 |
|--------|------|----------------|---------|---------|
| P0 | {名称} | {BDN 数据描述} | {方案} | {速度/内存预估} |
| P1 | {名称} | {BDN 数据描述} | {方案} | {速度/内存预估} |
```

- **P0**：影响核心吞吐或 >30% 性能损失，必须优化
- **P1**：影响扩展性或有明显内存压力，建议优化
- **P2**：次要瓶颈，可选优化（仅在确有数据支撑时列出）
- 同级按影响程度降序；无明显瓶颈时写"未发现显著瓶颈"即可

### 8.3 补充说明（可选）

表格之后可对 P0/P1 瓶颈各写 2~3 句补充根因和方案细节，**不要求也不鼓励**对每个瓶颈展开长篇分析。
