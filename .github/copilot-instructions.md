# NewLife Copilot 协作指令

本说明适用于新生命团队（NewLife）及其全部开源/衍生项目，规范 Copilot 及类似智能助手在 C#/.NET 项目中的协作行为。

---

## 1. 核心原则

| 原则 | 说明 |
|------|------|
| **提效** | 减少机械样板，聚焦业务/核心算法 |
| **一致** | 风格、结构、命名、API 行为稳定 |
| **可控** | 限制改动影响面，可审计，兼容友好 |
| **可靠** | 先检索再生成，不虚构，不破坏现有合约 |
| **主动** | 发现问题主动修复，不回避合理优化 |

---

## 2. 适用范围

- 含 NewLife 组件或衍生的全部 C#/.NET 仓库
- 不含纯前端/非 .NET/市场文案
- 存在本文件 → 必须遵循

---

## 3. 工作流

```
需求分类 → 检索 → 评估 → 设计 → 实施 → 验证 → 说明
```

1. **需求分类**：功能/修复/性能/重构/文档
2. **检索**：相关类型、目录、方法、已有扩展/工具（**优先复用**）
3. **评估**：是否公共 API？是否性能热点？**是否存在潜在问题？**
4. **设计**：列出改动点 + 兼容/降级策略
5. **实施**：
   - 完成用户请求的核心任务
   - **顺带修复**发现的明显缺陷（资源泄漏、空引用、逻辑错误）
   - **顺带优化**可简化的重复代码
   - 保留原注释与结构，除非注释本身有误
6. **验证**：
   - 代码变更：必须编译通过；运行相关单元测试（未找到需说明）
   - 仅文档变更（未修改任何代码文件）：可跳过编译与单元测试
7. **说明**：变更摘要/影响范围/风险点

### 3.1 主动优化原则

当用户请求分析或优化代码时，**应主动**：

| 类型 | 行动 |
|------|------|
| **架构梳理** | 梳理代码架构并进行重构，让代码结构更清晰易懂 |
| **语法现代化** | 使用最新的 C# 语法来简化代码，提升可读性 |
| **缺陷修复** | 资源泄漏、空引用风险、并发问题、逻辑错误 → 直接修复，让代码更健壮 |
| **性能优化** | 无用分配、重复计算、可池化资源 → 通过缓存减少耗时的重复计算 |
| **代码简化** | 重复代码提取、冗余判断合并、现代语法替换 → 在不影响可读性前提下简化 |
| **注释完善** | 补充类、接口、属性、方法头部的注释，以及方法内部重要代码的注释 |
| **架构参考** | 参考网络上同类功能的优秀架构，给出架构调整建议 |

**架构调整策略**：
- **改动较小**：直接调整，完成后说明变更内容
- **改动较大**：先列出调整方案，询问用户意见，待确认后再修改

**不应过度保守**：
- ❌ 仅添加注释而忽略明显的代码问题
- ❌ 发现资源泄漏却不修复
- ❌ 看到重复代码却不提取
- ❌ 用户要求优化时只做表面工作

**保持谨慎的场景**：
- 公共 API 签名变更 → 需说明兼容性影响
- 性能关键路径 → 需有依据或说明推理
- 大范围重构 → 需先与用户确认范围

### 3.2 防御性注释规则

在旧有代码中，经常可以看到**被注释掉的代码**，这些注释代码前面通常带有说明文字。

**这些是防御性注释**：
- 记录了过去曾经踩过的坑
- 目的是告诉后来人不要按照注释代码去写，否则会有问题
- **禁止删除此类防御性注释**，用于警示后人

**识别特征**：
```csharp
// 曾经尝试过 xxx 方案，但会导致 yyy 问题
// var result = DoSomethingWrong();

// 不要使用 xxx，否则会造成 yyy
// await client.SendAsync(data);

// 这里不能用 xxx，因为 yyy
// stream.Flush();
```

**处理原则**：
- ✅ 保留这类带说明的注释代码
- ✅ 可以补充更详细的说明，解释为什么不能这样做
- ❌ 不要删除这类防御性注释
- ❌ 不要尝试"恢复"这些被注释的代码

---

## 4. 编码规范

### 4.1 基础规范

| 项目 | 规范 |
|------|------|
| 语言版本 | `<LangVersion>latest</LangVersion>`，所有目标框架均使用最新 C# 语法 |
| 命名空间 | file-scoped namespace |
| 类型名 | **必须**使用 .NET 正式名 `String`/`Int32`/`Boolean` 等，避免 `string`/`int`/`bool` |
| 兼容性 | 代码需兼容 .NET 4.5+；**禁止**使用 `ArgumentNullException.ThrowIfNull`，改用 `if (value == null) throw new ArgumentNullException(nameof(value));` |
| 单文件 | 每文件一个主要公共类型；较大平台差异使用 `partial` |

### 4.2 命名规范

| 成员类型 | 命名规则 | 示例 |
|---------|---------|------|
| 类型/公共成员 | PascalCase | `UserService`、`GetName()` |
| 参数/局部变量 | camelCase | `userName`、`count` |
| 私有字段（实例/静态） | `_camelCase` | `_cache`、`_instance` |
| 属性/方法（实例/静态） | PascalCase | `Name`、`Default`、`Create()` |
| 扩展方法类 | `xxxHelper` 或 `xxxExtensions` | `StringHelper`、`CollectionExtensions` |

### 4.3 代码风格

```csharp
// ✅ 单行 if：单语句且整行不过长时同行
if (value == null) return;
if (key == null) throw new ArgumentNullException(nameof(key));

// ✅ 单行 if：语句较长时另起一行
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
```

### 4.4 Region 组织结构

较长的类使用 `#region` 分段组织，遵循以下顺序：

```csharp
public class MyService : DisposeBase
{
    #region 属性
    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary>是否启用</summary>
    public Boolean Enabled { get; set; }

    // 私有字段放在属性段末尾
    private ConcurrentDictionary<String, Object> _cache = new();
    private TimerX? _timer;
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    public MyService() { }

    /// <summary>销毁资源</summary>
    /// <param name="disposing">是否释放托管资源</param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);
        _timer.TryDispose();
    }
    #endregion

    #region 方法
    /// <summary>启动服务</summary>
    public void Start() { }
    #endregion

    #region 日志
    /// <summary>日志</summary>
    public ILog Log { get; set; } = Logger.Null;

    /// <summary>写日志</summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="args">参数</param>
    protected void WriteLog(String format, params Object[] args) => Log?.Info(format, args);
    #endregion
}
```

**Region 顺序**：`属性` → `静态`（如有）→ `构造` → `方法` → `辅助`（如有）→ `日志`

**日志 Region 规则**：
- 类代码中如果带有 `ILog Log { get; set; }` 和 `WriteLog` 方法
- **必须放在类代码的最后**
- **必须用名为"日志"的 region 包裹**
- 不要放在"辅助" region 中，应单独作为"日志" region

### 4.5 现代 C# 语法

优先使用最新语法，即使目标框架是 net45（Visual Studio 支持）：

```csharp
// switch 表达式
var result = code switch
{
    200 => "OK",
    >= 400 and < 500 => "ClientError",
    _ => "Other"
};

// 目标类型 new
using var ms = new MemoryStream();

// record（DTO 场景）
public record UserInfo(String Name, Int32 Age);

// 模式匹配
if (obj is String { Length: > 0 } str) { }
```

### 4.6 集合表达式

优先使用集合表达式 `[]` 初始化集合，代码更简洁：

```csharp
// ✅ 属性定义：使用集合表达式
public List<String> Tags { get; set; } = [];
public Dictionary<String, Object> Data { get; set; } = [];
public Int32[] Numbers { get; set; } = [];

// ❌ 避免冗长的初始化方式
public List<String> Tags { get; set; } = new List<String>();
public List<String> Tags { get; set; } = new();

// ✅ 方法内局部变量
List<String> list = [];
var items = new List<Item>();  // 需要立即 Add 时可用 new

// ✅ 带初始值的集合
List<Int32> nums = [1, 2, 3];
String[] names = ["Alice", "Bob"];
Dictionary<String, Int32> scores = new() { ["Math"] = 90, ["English"] = 85 };

// ✅ 集合展开（spread）
List<Int32> combined = [..first, ..second, 100];

// ✅ 返回空集合
public IList<String> GetItems() => [];
```

### 4.7 Null 条件运算符

优先使用 `?.`（null 条件运算符）简化空值检查，提升代码简洁性与可读性：

```csharp
// ✅ 方法调用：使用 null 条件运算符
span?.AppendTag("test");
handler?.Invoke(this, args);
list?.Clear();

// ❌ 避免冗余的 if 判断
if (span != null) span.AppendTag("test");
if (handler != null) handler.Invoke(this, args);

// ✅ 属性赋值：使用 null 条件赋值（C# 14 新特性）
customer?.Order = GetCurrentOrder();
span?.Value = 1234;
config?.Name = "test";

// ❌ 避免冗余的 if 判断
if (customer != null) customer.Order = GetCurrentOrder();
if (span != null) span.Value = 1234;

// ✅ 复合赋值运算符也支持
counter?.Count += 1;
list?.Capacity *= 2;

// ✅ 链式调用：安全访问嵌套属性
var name = user?.Profile?.Name;
var count = order?.Items?.Count ?? 0;

// ✅ 结合 null 合并运算符提供默认值
var length = str?.Length ?? 0;
var display = item?.ToString() ?? "N/A";

// ✅ 索引器访问
var first = list?[0];
var value = dict?["key"];

// ✅ 委托调用（推荐写法）
PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
```

**注意**：null 条件赋值时，右侧表达式仅在左侧非 null 时才会求值；不支持自增/自减运算符（`++`/`--`）。

---

## 5. 多目标框架

NewLife 支持 `net45` 到 `net10`，使用条件编译处理 API 差异：

```csharp
// 常用条件符号
#if NETFRAMEWORK          // net45/net461/net462
#if NETSTANDARD2_0        // netstandard2.0
#if NETCOREAPP            // netcoreapp3.1+
#if NET5_0_OR_GREATER     // net5.0+
#if NET6_0_OR_GREATER     // net6.0+
#if NET8_0_OR_GREATER     // net8.0+

```

**注意**：新增 API 时需评估各框架兼容性，必要时提供降级实现。

---

## 6. 文档注释

```csharp
/// <summary>执行指定操作</summary>
/// <param name="action">操作委托</param>
/// <param name="timeout">超时时间，单位毫秒</param>
/// <returns>执行是否成功</returns>
public Boolean Execute(Action action, Int32 timeout) { }

/// <summary>初始化服务</summary>
/// <remarks>
/// 复杂方法可增加详细说明。
/// 中文优先，必要时补精简英文。
/// </remarks>
/// <param name="config">配置对象</param>
/// <param name="log">日志接口</param>
public MyService(Config config, ILog log) { }
```

| 规则 | 说明 |
|------|------|
| `<summary>` | **必须同一行闭合**，简短描述方法用途 |
| `<param>` | **必须为每个参数添加**，无论方法可见性如何 |
| `<returns>` | 有返回值时必须添加 |
| `<remarks>` | 复杂方法可增加详细说明（可多行） |
| 覆盖范围 | `public`/`protected` 成员必须注释，包括构造函数；`private`/`internal` 方法建议添加 |
| `[Obsolete]` | 必须包含迁移建议 |

### 6.1 注释完整性检查清单

生成或修改方法注释时，**必须逐项检查**：

1. ✅ `<summary>` 是否单行闭合？
2. ✅ 是否为**每个参数**都添加了 `<param>`？
3. ✅ 有返回值时是否添加了 `<returns>`？
4. ✅ 泛型方法是否添加了 `<typeparam>`？

**正确示例**：
```csharp
/// <summary>获取或设置名称</summary>
public String Name { get; set; }

/// <summary>创建客户端连接</summary>
/// <param name="host">服务器地址</param>
/// <param name="port">端口号</param>
/// <returns>客户端实例</returns>
public TcpClient Create(String host, Int32 port) { }

/// <summary>启动服务</summary>
/// <remarks>
/// 启动后将开始监听端口，支持多次调用（幂等）。
/// 建议在应用启动时调用一次。
/// </remarks>
public void Start() { }

/// <summary>映射配置到对象</summary>
/// <param name="reader">Xml读取器</param>
/// <param name="section">目标配置节</param>
private void ReadNode(XmlReader reader, IConfigSection section) { }
```

**错误示例**（禁止）：
```csharp
// ❌ summary 拆成多行
/// <summary>
/// 获取或设置名称
/// </summary>
public String Name { get; set; }

// ❌ 缺少参数注释（即使是私有方法也不允许）
/// <summary>创建客户端连接</summary>
public TcpClient Create(String host, Int32 port) { }

// ❌ 有参数但没有 param 标签
/// <summary>递归读取节点</summary>
private void ReadNode(XmlReader reader, IConfigSection section) { }
```

---

## 7. 异步与性能

| 规范 | 说明 |
|------|------|
| 方法命名 | 异步方法后缀 `Async` |
| ConfigureAwait | 库内部默认 `ConfigureAwait(false)` |
| 高频路径 | 优先对象池/`ArrayPool<T>`/`Span`，避免多余分配 |
| 反射/Linq | 仅用于非热点路径；热点使用手写循环/缓存 |
| 池化资源 | 明确获取/归还；异常分支不遗失归还 |

### 内置工具优先

```csharp
// ✅ 使用 Pool.StringBuilder 避免频繁分配
var sb = Pool.StringBuilder.Get();
sb.Append("Hello");
return sb.Return(true);

// ✅ 使用 Runtime.TickCount64 避免时间回拨
var tick = Runtime.TickCount64;

// ✅ 使用扩展方法进行类型转换
var num = str.ToInt();
var flag = str.ToBoolean();
```

---

## 8. 日志与追踪

```csharp
#region 日志
/// <summary>日志</summary>
public ILog Log { get; set; } = Logger.Null;

/// <summary>链路追踪</summary>
public ITracer? Tracer { get; set; }

/// <summary>写日志</summary>
/// <param name="format">格式化字符串</param>
/// <param name="args">参数</param>
protected void WriteLog(String format, params Object?[] args) => Log?.Info(format, args);
#endregion

// ✅ 关键过程埋点（简易版，不关注异常）
using var span = Tracer?.NewSpan("ProcessData");
// 业务逻辑

// ✅ 关键过程埋点（标准版，需要捕获异常）
using var span = Tracer?.NewSpan("ProcessData");
try
{
    // 业务逻辑
}
catch (Exception ex)
{
    span?.SetError(ex, null);
    throw;
}
```

---

## 9. 错误处理

- **精准异常类型**：`ArgumentNullException`/`InvalidOperationException` 等
- **参数校验**：空/越界/格式
- **TryXxx 模式**：不用异常作常规分支
- **类型转换**：优先使用 `ToInt()`/`ToBoolean()` 等扩展方法
- **对外异常**：不暴露内部实现/路径

---

## 10. 测试规范

| 项目 | 规范 |
|------|------|
| 框架 | xUnit |
| 命名 | `{ClassName}Tests` |
| 描述 | `[DisplayName("中文描述意图")]` |
| IO | 使用临时目录；端口用 0/随机 |
| 覆盖 | 正常/边界/异常/并发（必要时） |

### 测试执行策略

1. 优先检索 `{ClassName}` 引用，若落入测试项目则运行
2. 未命中则查找 `{ClassName}Tests.cs`
3. **未发现相关测试需明确说明**，不自动创建测试项目

---

## 11. NuGet 发布规范

| 类型 | 命名规则 | 示例 |
|------|---------|------|
| 正式版 | `{主版本}.{子版本}.{年}.{月日}` | `11.9.2025.0701` |
| 测试版 | `{主版本}.{子版本}.{年}.{月日}-beta{时分}` | `11.9.2025.0701-beta0906` |

- **正式版**：每月月初发布
- **测试版**：提交代码到 GitHub 时自动发布

---

## 12. Copilot 行为守则

### 必须

- 简体中文回复
- 输出前检索现有实现，**禁止重复造轮子**
- 先列方案再实现
- 标记不确定上下文为"需查看文件"
- **发现明显缺陷时主动修复**（资源泄漏、空引用、逻辑错误）
- **用户要求优化时深入分析**，不做表面工作

### 鼓励

- 提取重复代码为公共方法
- 简化冗余的条件判断
- 使用现代 C# 语法改进可读性
- 补充缺失的资源释放逻辑
- 修正错误或过时的注释

### 禁止

- 虚构 API/文件/类型
- 伪造测试结果/性能数据
- 擅自删除公共/受保护成员
- 擅自删除已有代码注释（除非注释本身错误）
- **删除防御性注释**（带说明的注释代码，记录历史踩坑经验）
- 仅删除空白行制造"格式优化"提交
- 删除循环体的花括号
- 将 `<summary>` 拆成多行
- 将 `String`/`Int32` 改为 `string`/`int`
- 新增外部依赖（除非说明理由并给出权衡）
- 在热点路径添加未缓存反射/复杂 Linq
- 输出敏感凭据/内部地址
- **发现问题却视而不见**
- **用户要求优化时仅做注释/测试等表面工作**

---

## 13. 变更说明模板

提交或答复需包含：

```markdown
## 概述
做了什么 / 为什么

## 影响
- 公共 API：是/否
- 性能影响：无/有（说明）

## 兼容性
降级策略 / 条件编译点

## 风险
潜在回归 / 性能开销

## 后续
是否补测试 / 文档
```

---

## 14. 术语说明

| 术语 | 定义 |
|------|------|
| **热点路径** | 经性能分析或高频调用栈确认的关键执行段 |
| **基线** | 变更前的功能/性能参考数据 |
| **顺带修复** | 在完成主任务过程中，修复发现的相关问题 |
| **防御性注释** | 被注释掉的代码，前面带有说明，记录历史踩坑经验，用于警示后人 |

---

## 15. 代码优化检查清单

当进行代码优化时，按以下清单逐项检查：

### 架构与结构
- [ ] 代码架构是否清晰？是否需要重构？
- [ ] 类的职责是否单一？是否需要拆分？
- [ ] 是否有重复代码可以提取为公共方法？
- [ ] Region 组织是否符合规范（属性→静态→构造→方法→辅助→日志）？

### 语法现代化
- [ ] 是否可以使用更简洁的 C# 语法？（switch 表达式、模式匹配等）
- [ ] 集合初始化是否使用了集合表达式 `[]`？
- [ ] 是否可以使用 null 条件运算符 `?.` 简化代码？

### 健壮性
- [ ] 是否存在空引用风险？
- [ ] 资源是否正确释放？（IDisposable、流、连接等）
- [ ] 异常处理是否完善？
- [ ] 并发场景是否线程安全？

### 性能
- [ ] 是否存在可以缓存的重复计算？
- [ ] 是否有不必要的对象分配？
- [ ] 热点路径是否避免了反射和复杂 Linq？
- [ ] 是否使用了对象池/ArrayPool 等池化技术？

### 注释与文档
- [ ] 类、接口是否有 `<summary>` 注释？
- [ ] 公共方法是否有完整的参数和返回值注释？
- [ ] 方法内重要逻辑是否有注释说明？
- [ ] 防御性注释是否保留？

### 日志
- [ ] `ILog Log` 和 `WriteLog` 是否放在类的最后？
- [ ] 是否用名为"日志"的 region 包裹？

---

（完）
