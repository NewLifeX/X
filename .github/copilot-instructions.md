# NewLife Copilot 协作指令

适用于新生命团队（NewLife）全部 C#/.NET 仓库。存在本文件则必须遵循。**简体中文回复。**
通用 C# 最佳实践（设计模式、SOLID、健壮性等）AI 已知，此处不赘述，**仅列出组织专属规则与反常规约定**。

---

## 1. 专用指令（前置检查，必须执行）

**开始任何任务前，必须先将用户请求与下表触发信号逐行匹配。命中则立即用 `get_file` 读取 `.github/instructions/{指令文件}`，读取成功后遵循其中全部规则。未命中任何行才跳过。**

| 触发信号（用户请求含以下任意关键词即命中） | 指令文件 |
|---------|---------|
| XCode/实体生成/Model.xml/数据库 CRUD/`NewLife.XCode` 引用/`*.xcode.xml`/项目名含 `.Data`/`XCode.*` 命名空间/用户提及修改任意 `.xml` 文件 | `xcode.instructions.md` |
| Cube/魔方/Web开发/`NewLife.Cube` 引用/`NewLife.Cube.*` 命名空间 | `cube.instructions.md` |
| 性能测试/基准测试/压力测试/压测/BenchmarkDotNet/Benchmark/benchmark/吞吐量评估/性能分析/性能对比/性能报告/速度对比/速度测试/内存分配/perf/性能优化测试/做性能/跑分/测试报告 | `benchmark.instructions.md` |
| NetServer/NetSession/网络服务器/网络客户端/Socket服务/TCP服务/UDP服务/`NewLife.Net` 引用/`NewLife.Net.*` 命名空间/ISocketClient/ISocketRemote/CreateRemote/StandardCodec/LengthFieldCodec/管道编解码/网络编程/Echo服务/网络会话/长连接/粘包拆包 | `net.instructions.md` |
| 新建系统/新建项目/新增模块/需求整理/需求文档/需求分析/架构设计/技术方案/功能清单/功能拆分/任务分解/迭代开发/迭代计划/验收/PRD/用户故事/做一个系统/做一个平台/开发流程/全部搞完/批量开发/自治模式/一次性做完/继续处理/接着做 | `development.instructions.md` |

---

## 2. 核心原则

检索优先、风格一致、兼容友好、**主动优化**。
发现明显缺陷（资源泄漏、空引用、逻辑错误）时主动修复；优化请求时深入分析，不做表面工作。
改动较小直接做并说明；改动较大（涉及公共 API 或大范围重构）先列方案询问确认。

---

## 3. 兼容性约束（极重要）

NewLife 核心库支持 `.NET 4.5` 至最新版本（`net45` → `net10`）。

- **语言版本**：`<LangVersion>latest</LangVersion>`，最大化使用最新 C# 语法糖（switch 表达式、集合表达式 `[]`、`?.`/`??`、模式匹配、目标类型 `new`、record 等）
- **禁止高版本专属 BCL API**：❌ `ArgumentNullException.ThrowIfNull()` → ✅ `if (x == null) throw new ArgumentNullException(nameof(x));`
- **条件编译符号**：`NETFRAMEWORK`、`NETSTANDARD2_0`、`NETCOREAPP`、`NET5_0_OR_GREATER`、`NET6_0_OR_GREATER`、`NET8_0_OR_GREATER`
- 新增 API 需评估各框架兼容性，必要时提供条件编译降级实现

---

## 4. 编码规范

### 4.1 类型名（关键差异）

**必须**使用 .NET 正式名：`String`/`Int32`/`Boolean`/`Int64`/`Double`/`Object` 等。
❌ **禁止**使用 C# 别名：`string`/`int`/`bool`/`long`/`double`/`object`

### 4.2 命名

| 成员类型 | 规则 | 示例 |
|---------|------|------|
| 类型/公共成员 | PascalCase | `UserService`、`GetName()` |
| 参数/局部变量 | camelCase | `userName`、`count` |
| 私有字段 | `_camelCase` | `_cache`、`_instance` |
| 扩展方法类 | `xxxHelper` 或 `xxxExtensions` | `StringHelper`、`CollectionExtensions` |

### 4.3 代码风格

- **命名空间**：file-scoped namespace
- **单文件**：每文件一个主要公共类型；较大平台差异使用 `partial`
- **集合初始化**：优先使用集合表达式 `[]`，如 `List<String> Tags { get; set; } = [];`
- **Null 条件运算符**：优先使用 `?.`/`??` 简化空值检查

```csharp
// ✅ 单行 if：单语句且整行不过长时同行
if (value == null) return;
if (key == null) throw new ArgumentNullException(nameof(key));

// ✅ 语句较长时另起一行，仍不加花括号
if (value == null)
    throw new ArgumentNullException(nameof(value), "Value cannot be null");

// ✅ 多分支单语句：不加花括号
if (count > 0)
    DoSomething();
else
    DoOther();

// ✅ 循环必须保留花括号（即使单语句）
foreach (var item in list)
{
    Process(item);
}

// ✅ using 优先无花括号声明；仅需生命周期（如锁）时用弃元
using var stream = File.OpenRead("file.txt");
using var _ = _lock.AcquireLock();
```

### 4.4 Region 与日志

较长类使用 `#region` 分段，顺序：`属性` → `静态` → `构造` → `方法` → `辅助` → **`日志`**。
含 `ILog Log` 和 `WriteLog` 时：**必须放类末尾**，用名为"日志"的 region 包裹，不放入"辅助"。
关键过程可使用 `Tracer?.NewSpan()` 埋点。

### 4.5 文档注释

- `<summary>` **必须同行闭合**：`/// <summary>获取名称</summary>`
- 每个参数**必须有** `<param>` 标签，无论方法可见性
- 有返回值**必须有** `<returns>`；复杂方法可增加 `<remarks>`
- `public`/`protected` 成员必须注释；`[Obsolete]` 必须包含迁移建议

### 4.6 异步与性能

- 异步方法后缀 `Async`，库内部默认 `ConfigureAwait(false)`
- 热点路径避免反射/复杂 Linq，优先手写循环/`ArrayPool<T>`/`Span`
- 池化资源明确获取/归还，异常分支不遗失归还

### 4.7 错误处理

- 精准异常类型：`ArgumentNullException`/`InvalidOperationException` 等
- TryXxx 模式：不用异常作常规分支
- 类型转换：优先使用 `ToInt()`/`ToBoolean()` 等扩展方法
- 对外异常不暴露内部实现/路径

---

## 5. NewLife 内置工具

优先使用项目内置工具而非标准库，**禁止重复造轮子**：

- 字符串构建：`Pool.StringBuilder`（替代 `new StringBuilder()`）
- 时间戳：`Runtime.TickCount64`
- 类型转换：`ToInt()`、`ToBoolean()`、`ToDouble()`、`ToDateTime()` 等扩展方法
- 追踪埋点：`Tracer?.NewSpan()`

---

## 6. 防御性注释（禁止删除）

代码中带有说明文字的被注释代码属于**防御性注释**，记录历史踩坑经验。**禁止删除，禁止"恢复"执行**。可补充更详细说明。

```csharp
// 曾经尝试过同步等待，但会导致线程池饥饿和死锁
// var result = task.Result;

// 不要使用 SendAsync 的无超时重载，否则会造成连接泄漏
// await client.SendAsync(data);
```

---

## 7. 工作流

触发检查（第 1 节触发信号表匹配，命中则读取专用指令） → 检索（**优先复用**现有实现） → 评估（公共 API/兼容性/性能） → 方案 → 实施 → 验证 → 说明

- **触发检查**：开始工作前必须完成，遗漏专用指令将导致输出不符合要求
- **实施**：完成主任务；顺带修复明显缺陷；顺带简化重复代码；保留原注释与结构
- **验证**：代码变更必须编译通过；找到相关测试则运行；仅文档变更可跳过

### 主动优化原则

用户要求**分析/优化代码**时：

| 行动 | 说明 |
|------|------|
| **架构梳理** | 重构不清晰的结构，让代码更易懂 |
| **缺陷修复** | 资源泄漏、空引用、并发问题、逻辑错误 → 直接修复 |
| **代码简化** | 提取重复代码、合并冗余判断、应用现代语法 |
| **性能优化** | 缓存重复计算、池化高频对象、避免无用分配 |
| **注释完善** | 补充缺失的 XML 注释和关键逻辑说明 |

---

## 8. 测试

- 框架 xUnit；类名 `{ClassName}Tests`；方法加 `[DisplayName("中文描述意图")]`
- 网络端口用 `0`/随机，IO 用临时目录
- 先搜索 `{ClassName}` 引用定位测试文件，再找 `{ClassName}Tests.cs`；**未找到需说明**，不自动创建测试项目

---

## 9. 文档与发布

### Markdown 文档

UTF-8 无 BOM；存放 `Doc/` 目录；文件名优先中文。**已有文件必须先读取再增量修改，禁止覆盖。**

### NuGet 版本

| 类型 | 格式 | 示例 |
|------|------|------|
| 正式版 | `{主}.{子}.{年}.{月日}` | `11.9.2025.0701` |
| 测试版 | `{主}.{子}.{年}.{月日}-beta{时分}` | `11.9.2025.0701-beta0906` |

---

## 10. 重要禁止项

以下是 AI 容易犯但在本项目影响严重的错误：

- 将 `String`/`Int32` 改为 `string`/`int`（本项目反 C# 惯例，**必须用正式名**）
- 删除防御性注释（带说明的注释代码）
- 删除循环体的花括号
- 将 `<summary>` 拆成多行
- 擅自删除 `public`/`protected` 成员
- 擅自新增外部 NuGet 依赖（需说明理由）
- 仅删除空白行/注释制造"格式优化"提交
- 虚构不存在的 API/文件/类型
- 伪造测试结果/性能数据
- 在热点路径添加未缓存反射/复杂 Linq
- 输出敏感凭据/内部地址
- 发现问题却视而不见
- 用户要求优化时仅做注释/测试等表面工作
- **跳过第 1 节触发检查**（命中关键词却未加载专用指令文件，是最严重的遗漏错误）

---

## 11. 变更说明模板

```markdown
## 概述
做了什么 / 为什么

## 影响
- 公共 API：是/否
- 性能影响：无/有（说明）

## 兼容性
降级策略 / 条件编译点

## 风险与后续
潜在回归 / 是否补测试
```

---

（完）
