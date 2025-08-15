# NewLife 开源项目统一 Copilot 指令说明

本文件适用于新生命团队（NewLife）全部开源/内部项目：核心基础库、通用组件、中间件适配、物联网协议、产品平台、工具/客户端、示例与脚手架、嵌入式/边缘相关项目。复制后无需修改，应保持一致性。

---
## 1. 目标与原则
- 约束/指引智能助手在仓库中的行为：理解项目生态、编码规范、兼容性与性能诉求。
- 所有修改需兼顾“广泛兼容 ( multi‑TFM ) + 高性能 + 长期维护 (20+ 年历史)”三要素。
- 优先复用 NewLife 既有能力（日志、配置、缓存、序列化、网络、对象容器、追踪等），避免盲目新增外部依赖。

---
## 2. 典型多目标框架 (Multi‑Target Frameworks)
绝大多数库或产品同时面向：
- .NET Framework: net45 / net461 / net462
- .NET Standard: netstandard2.0 / netstandard2.1
- .NET Core / .NET: netcoreapp3.1 / net5.0 / net6.0 / net7.0 / net8.0 / net9.0
- Windows 桌面特性：net5.0-windows → net9.0-windows（WinForms/WPF/特定 API）

要求：
1. 新增目标框架前评估必要性与 API 可用性；不要轻易移除旧框架（保持二进制兼容）。
2. 使用条件编译区分功能：`#if NET5_0_OR_GREATER`、`#if NETSTANDARD2_0`、`#if NETFRAMEWORK`、`#if __WIN__` 等。
3. 避免在公共 API 中暴露仅某平台存在的类型；若必须，使用抽象/接口+局部实现或分部类。

---
## 3. 目录/项目约定 (可能按项目裁剪)
- {ProjectRoot}/ProjectName*: 主功能源码（命名与仓库一致）。
- Samples/ 或 Sample*/：示例、演示、脚手架模板示例。
- Test / Test2：轻量可执行测试（控制台/手工验证）。
- XUnitTest.*：xUnit 单元测试工程。
- Doc/：文档、图标、签名证书 newlife.snk。
- .github/：CI、Issue/PR 模板、Copilot 指令（本文件）。

---
## 4. 强命名与签名
- 全部正式库使用已公开的 `newlife.snk` 进行强命名，方便下游反射/插件体系与企业内私有构建。
- 不新增未签名的并行程序集；私有更名需保持 StrongName 不变或明确版本策略。

---
## 5. 编码规范 (统一风格)
1. 语言特性：使用“最新 C#” (`<LangVersion>latest</LangVersion>`)，可用表达式体、模式匹配、Span / Memory、`using var`、`ref struct` 等。
2. 基础类型：使用 .NET 类型名（`String`/`Int32`/`Boolean`/`Object` 等），不使用 C# 关键字别名。
3. 命名：PascalCase (类型/公共成员)；camelCase (参数/局部变量)；私有字段 `_xxx` 或前缀 `_` + camelCase；常量 PascalCase。
4. if 单行：`if (x > 0) return;` （同一行无花括号）。多行使用花括号并换行。
5. 文件组织：一个文件一个主要公共类型；必要时使用 `partial` 拆分平台相关实现。
6. 禁止无意义的空白 & 尾随空格；保持 UTF-8 无 BOM。
7. 不随意删除代码注释，可根据代码上下文修改注释；保留历史注释（如 `// TODO`、`// FIXME` 等）。
8. 不随意改动现有公共签名（破坏兼容需记录变更说明/迁移策略）。

---
## 6. 文档注释
- 所有 `public` / `protected` 成员必须具备 XML 注释。
- `<summary>` 单行简洁；详细描述放 `<remarks>`；多语言可优先中文，必要时附英文简述。
- 使用 `<param>` `<returns>` `<example>`；示例需可编译（省略不相关部分可用 `// ...`）。
示例：
```csharp
/// <summary>获取或设置配置名称</summary>
/// <remarks>标识不同配置实例，支持多副本并存。</remarks>
public String Name { get; set; }

/// <summary>异步保存到指定路径</summary>
/// <param name="path">保存路径，空则使用默认路径</param>
/// <returns>是否成功</returns>
public async Task<Boolean> SaveAsync(String? path = null)
```

---
## 7. 异步 & 并发
- 异步方法名以 `Async` 结尾；不要为纯同步实现强行使用异步。
- 内部库方法默认 `ConfigureAwait(false)`，UI / WinForms 层除外。
- 频繁调用路径注意减少任务分配，可用 `ValueTask`、`ArrayPool`、`ObjectPool`、`Pool.StringBuilder`、`Pool.MemoryStream`。
- 并发数据结构优先使用框架内高性能容器或 `Concurrent*`；热点路径避免锁竞争，可采用`Interlocked`/无锁/分片锁/读写锁。

---
## 8. 性能优化
- 关注 GC：重用缓冲区（`Span<T>`、`Memory<T>`、`ArrayPool<T>`、对象池）。
- 日志判级：`Log?.Debug` 或使用框架提供的判级接口，避免字符串拼接开销。
- 序列化/网络热路径避免 LINQ/装箱/反射频用；可用表达式缓存、委托缓存。
- 大型循环内避免多次 `DateTime.Now`，改用时间戳缓存`TimerX.Now`。
- 需 Unsafe 时：局部最小化、加注释说明收益与风险。

---
## 9. 日志 & 追踪
- 使用 `ILog` / `XTrace`：公共组件提供可注入 `Log` 属性；服务器/长生命周期对象提供 `WriteLog` 帮助方法。
- 分布式链路：可选 `ITracer` 注入（名称规范：`Tracer`）。执行入口创建 span / 埋点；错误捕获写入标签。
- 仅在必要场景打印 Debug/Trace 级日志，避免噪声。

---
## 10. 错误处理
- 精准异常类型，避免裸 `catch {}`。无特殊处理直接 `throw;`。
- 业务/协议层使用 `ApiException` + 合适 `ApiCode`（或所在项目自定义的错误码枚举）。
- 参数校验：公共 API 早失败（`ArgumentNullException` 等）。内部私有方法不重复校验。
- 不将控制流建立在异常上；使用 TryParse / TryXXX 模式。
- 常见基础类型转换使用 `ToInt`、`ToLong`、`ToDouble`、`ToBoolean` 、`ToDecimal`、`ToDateTime`等扩展方法。

---
## 11. 配置 & 序列化
- 优先使用 NewLife 内置 Json / Xml / 二进制 / Csv 组件；禁止随意引入重复功能三方库。
- 配置系统：`IConfigProvider` / `Setting` / `SysConfig`；支持热加载需订阅变更事件。
- 序列化版本兼容：增加字段保持向后兼容；必要时使用自定义版本头或扩展标记。

---
## 12. 安全实践
- 不提交密钥/密码/令牌；示例使用占位符（`<YOUR_KEY>`）。
- 加密/哈希优先使用 NewLife.Security 或 BCL；禁用弱算法（MD5 仅做一致性校验，不做安全签名）。
- 网络输入先长度/格式校验再解码。避免 DoS 风险（限制最大包/最大循环次数）。

---
## 13. 外部依赖策略
- 保持核心库零或极少第三方依赖；功能优先内聚于 NewLife 生态。
- 新增依赖需满足：活跃、兼容多框架、明确许可（MIT/Apache2 等）。
- 添加前评估是否可用已有模块替代；在 PR 说明理由/影响。

---
## 14. 测试规范
- 单元测试：xUnit (`[Fact]` / `[Theory]`)，命名 `{ClassName}Tests`。
- `DisplayName` 中文描述；断言清晰表达意图。
- 涉及 IO：使用临时目录并清理；网络端口随机分配或使用 0 让系统选择。
- 性能敏感场景可添加基准（BenchmarkDotNet）但不默认引入为生产依赖。
- 关键协议 / 解析 / 序列化需覆盖：正常 + 边界 + 异常。

---
## 15. 兼容性与 API 稳定
- 公共类型/成员删除或签名变更需提供迁移策略（Obsolete → 移除）。
- 条件 API：为差异平台提供最小补偿层（shim / partial）。
- 注意旧框架缺失 API（ValueTask、Span、Task.WaitAsync 等）时的替代实现。

---
## 16. 版本与发布
- 语义化版本：MAJOR 破坏性 / MINOR 新功能兼容 / PATCH 修复。
- 多框架打包：单一 NuGet 包含全部 TFM；保证功能语义一致。
- 发布流程：更新变更日志要点（重点性能/兼容/安全修复）。

---
## 17. 示例 / 演示
- Samples 中展示典型最小可运行场景；避免过度复杂；添加简短 README / 注释。
- 产品级示例（服务器/代理/控制台）展示：配置加载 → 初始化日志/追踪 → 组件注册 → 运行 → 优雅退出。

---
## 18. Pull Request 检查清单 (作者 & 审核)
- [ ] 编码风格与命名符合规范
- [ ] 添加/更新必要的 XML 注释 & 文档
- [ ] 多目标框架均编译通过（CI）
- [ ] 无不必要外部依赖
- [ ] 性能敏感更改给出基准或说明
- [ ] 兼容性评估（若涉及公共 API）
- [ ] 新增/修改逻辑具备测试或说明测试方式
- [ ] 未包含敏感信息 / 临时调试代码 / 大体积二进制

---
## 19. Copilot / 智能助手 使用指引
当该文件存在时，智能助手应：
1. 回答聚焦软件开发相关内容；与项目无关的其它领域问题提示不在支持范围。
2. 在建议代码前先检索现有实现（避免重复造轮子）。
3. 修改文件前先读取原文件，最小化 diff，保留历史风格。
4. 输出示例使用 .NET 类型名（`String` 等），遵循 if 单行写法规范。
5. 提供增量改动：只编辑必要文件；不散布功能碎片。
6. 生成跨框架代码时用条件编译；不要因为某高版本 API 直接放弃旧目标。
7. 性能相关答复强调：对象池、Span、缓存、日志判级、避免多余分配。
8. 异步示例避免 `.Result` / `.GetAwaiter().GetResult()`；可演示同步包装。
9. 涉及日志使用 `ILog` / `XTrace`；涉及追踪使用 `ITracer`。
10. 需新增依赖时先建议是否可复用现有模块，并说明权衡。
11. 版权/许可：保持 MIT 头部（若仓库已有格式）。
12. 不输出超大整文件代码块（>300 行）除非用户明确要求；使用省略注释 `// ...` 表示未改动部分。

---
## 20. 常用宏/条件速查
- 平台：`NETFRAMEWORK` / `NETSTANDARD2_0` / `NETSTANDARD2_1` / `NETCOREAPP` / `NET5_0_OR_GREATER`
- Windows 特性：`__WIN__`（项目中定义）
- 调试：`DEBUG` / `TRACE`
- 版本分支：`NET8_0_OR_GREATER` 等

---
## 21. 提示模式 (对话中的行为)
- 若用户要求非开发/与项目无关内容：简洁提示“我是编程助手”。
- 回答保持客观、简洁、可操作；避免人称口语化与主观情绪。
- 未知信息不要臆测：可提示需要查看文件或代码位置。

---
## 22. 资源与生态
- 官网：https://newlifex.com
- GitHub 组织：https://github.com/NewLifeX
- 文档（核心库示例）：https://newlifex.com/core
- 典型组件：Core / XCode / Redis / Remoting / Net / MQTT / Modbus / Agent / Cube / Stardust / AntJob 等。

---
## 23. 许可证
所有项目默认 MIT（除非仓库另有声明）。贡献代码即表示接受该许可及其再授权要求。

---
## 24. 历史与稳定性
核心项目拥有 10~20 年演进历史；保持公共 API 稳定与回溯兼容是首要目标之一。新增功能需避免破坏既有生态（插件、脚本、动态加载、分布式节点等）。

---
## 25. 快速示例（标准结构示意）
```csharp
public class DemoServer : DisposeBase
{
    /// <summary>日志接口</summary>
    public ILog? Log { get; set; } = XTrace.Log;

    /// <summary>链路追踪</summary>
    public ITracer? Tracer { get; set; }

    private TimerX? _timer;

    /// <summary>启动</summary>
    public void Start()
    {
        _timer ??= new TimerX(DoWork, null, 0, 5_000) { Async = true };
        WriteLog("DemoServer started");
    }

    private void DoWork(Object? state)
    {
        using var span = Tracer?.NewSpan("demo:tick");
        // ... 业务逻辑 ...
        WriteLog("Tick at {0}", DateTime.Now.ToFullString());
    }

    /// <summary>写日志</summary>
    protected void WriteLog(String format, params Object?[] args) => Log?.Info(format, args);

    /// <inheritdoc />
    protected override void OnDispose(Boolean disposing)
    {
        _timer.TryDispose();
        base.OnDispose(disposing);
    }
}
```

---
本文件更新需保持“通用 + 精简 + 可执行”三原则；修改后建议同步至所有仓库。
