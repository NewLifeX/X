
# NewLife.Core — 代码风格指南（精简 / 风格专用）

本文件仅包含与代码风格、命名、可读性和性能相关的约定，来源于项目源码高频实践与团队约定（自动化扫描所得），用于保持代码库的一致性。

## 目标语言与特性
- 采用现代 C# 特性以提高可读性与表达力（文件作用域命名空间、模式匹配、switch 表达式、目标类型 `new()`、集合表达式等）。
- 支持多目标框架时使用条件编译（`#if`）或 `partial` 分离平台差异，避免在同一文件混杂大量平台判断逻辑。

## 命名与可见性
- 类型与公共成员使用 PascalCase；参数与局部变量使用 camelCase；私有字段以 `_` 前缀命名（例如 `_timer`），并靠近其对应属性或使用处放置。
- 使用 .NET 正式类型名在注释或签名中更明确（如文档中可见 `String`/`Int32`），但在代码中可按惯例使用别名（`string`/`int`）保持一致性。
- 最小可见性原则：默认使用最小必要的访问级别；显式标注 `public/internal/private`。

## 文件与目录
- 每个文件以单一公共类型为主，文件名与类型名一致（`TypeName.cs`）。
- 文件作用域命名空间优先使用（`namespace NewLife;`），目录结构与命名空间保持一致。

## 注释与文档
- 公共 API 必须提供 XML 文档注释（中文为主），`<summary>` 保持简短且在一行内；必要时使用 `<remarks>` 详细说明。
- 对外弃用使用 `[Obsolete("说明与迁移建议")]` 并在注释中提示替代方案。

## 代码格式与布局
- 缩进使用 4 个空格；文件使用 UTF-8 编码。
- 对循环语句（`for/foreach/while/do`）即使只有单条语句也保留大括号 `{}`；`if` 的单行风格可接受但不要移除已有花括号。
- `using` 排序遵循：`System.*` 在前，其余按字母或分组排列；每个 `using` 单独一行。

## 异步与同步约定
- 异步方法以 `Async` 后缀；库内部 `await` 后推荐使用 `.ConfigureAwait(false)`（除非明确需要上下文恢复）。
- 必要时允许将异步结果同步化（`.GetAwaiter().GetResult()`），但应慎用并记录原因以避免死锁风险。

## 性能与低分配实践
- 在热点路径优先采用 `Span<T>` / `ReadOnlySpan<T>` / `ref struct`（例如 `SpanReader`/`SpanWriter`）以减少分配。
- 使用对象池（如 `Pool.Shared`、ArrayPool）复用缓冲，避免频繁分配大对象。
- 对短期字符串或字节序列使用 `stackalloc` 在合适场景下降低堆分配。

## 扩展方法与小工具函数
- 扩展方法应职责单一、简洁可读。对于性能敏感的小方法，可加 `[MethodImpl(MethodImplOptions.AggressiveInlining)]` 提示，但避免滥用。
- 对 `string` 等常用类型提供明确语义的扩展（例如 `IsNullOrEmpty`、`EndsWithIgnoreCase`），并统一使用 `StringComparison.OrdinalIgnoreCase` 进行忽略大小写比较。

## API 设计与参数约定
- 参数以必选在前、可选在后排序；避免多个布尔开关，优先考虑使用 `enum` 或选项对象以增强可读性。
- 对于可能返回 `null` 的方法，使用可空类型（`?`）并在注释中说明语义，必要时用 `[NotNullWhen]` 或 `MemberNotNull` 提供额外约束。

## 单元化与错误处理风格
- 使用明确的异常类型并尽量保留原始异常链以利调试（不吞掉内部异常）。
- 入参校验采用守卫式（guard clauses）在方法开头进行，并抛出标准异常类型（`ArgumentNullException`/`ArgumentOutOfRangeException`）。

## 区域组织与就近原则
- 字段应靠近其对应属性或使用处；局部变量在首次使用前就近声明。
- 按功能分组成员，必要时使用 `#region`（中文标题如 `属性/构造/方法/静态`）以提高可读性。

## 注解与特性使用
- 内部/工具类可使用 `[EditorBrowsable(EditorBrowsableState.Never)]` 减少智能感知噪声。
- 为迁移提供清晰的 `[Obsolete]` 信息；对可空流转和静态分析使用 `[NotNullWhen]`、`[MemberNotNull]` 等。

## 代码示例要点（选摘）
- 私有字段样式：
  - private TimerX? _timer; // 字段靠近属性或方法声明
- 异步示例：
  - await client.GetAsync(url).ConfigureAwait(false);
- Span/Pool 示例：
  - Span<byte> buf = stackalloc byte[256];
  - var arr = ArrayPool<byte>.Shared.Rent(size);

## 可执行建议（便于自动化检查）
- 强制或建议项可转为静态检查规则：
  1. 文件作用域命名空间优先（检测命名空间是否为 file-scoped）。
  2. 私有字段以下划线前缀命名并靠近 property 使用处。
  3. 公共 API 必须有 XML `<summary>` 注释。
  4. 库内部的 await 推荐使用 `.ConfigureAwait(false)`。

---

最终说明：该文档为代码风格集中版，已从自动扫描与已有文档中抽取高频实践并整理为规则与建议。如需进一步转为 EditorConfig/StyleCop 或 Roslyn 规则，我可继续协助生成对应规则或示例检测脚本。

