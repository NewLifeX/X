# NewLife.Core AOT 兼容性现状与策略

> 版本基线：v12（2026-10-08）｜评估方式：**NativeAOT 真实编译 + 运行实测**（win-x64，.NET 10）
> 说明：上一版为 2026-03 v11.13 期静态分析产物，随 v12 重构已失效（IMP-22 文档漂移）；本版按 v12 代码基线全文重写，所有结论以实测为准。

---

## 1. 结论摘要

**一句话：咽喉修复已落地，库在 NativeAOT 下核心链路实测全通（21/21）；剩余工作集中在反射宽面注解治理与源生成器体系（档 C）。**

- 实测（21 场景冒烟）：JIT 基线 14/14 → **AOT 修复前 4/13 → 修复后 14/14**（2026-10-08 波次 1 落地）→ **源生成器后 17/17**（SG-2/SG-3/SG-4 新增"源生成器服务工厂""源生成器 Json 成员访问""源生成器配置绑定"场景，JIT/AOT 双通）→ **异步委托修复后 19/19**（新增"Http 异步委托返回值""Http 控制器异步操作方法"场景）→ **SG-6 后 21/21**（新增"Http 控制器静态分发""Http 静态分发优先级"场景）。
- 失败不是散点，而是三类机制（见 §2.3）：**反射构造成员在 AOT 裁剪后丢失**、**反射契约注解缺位**、**动态能力设计性不支持**。
- **咽喉修复已入代码**：`Pool<T>` 泛型工厂（`e561198fc`）+ 注解属性 Stub 垫片（`341bcc10a`）+ 主链保留契约注解（`1e2050372`/`229f71949`/`5d11e19ce`）+ 低成本反射替换（`4e9a59415`）+ 动态能力守卫（`277c9a85a`）→ 冒烟 14/14；冒烟工程与 CI job 已入仓（`c8ac1d228`）。改造杠杆集中、成本可控。
- 档位与推荐见 §5；剩余切片见 §5.1。

## 2. 实测数据（2026-10-08）

### 2.1 编译期：AOT 分析器全量警告（库自身，`IsAotCompatible=true`）

| TFM | IL 警告 | 码分布 |
|-----|---------|--------|
| net10.0 | 326（去重 163） | IL3050×96、IL2026×64、IL2075×56、IL2070×40、IL2057×28、IL3000×20、IL2067×14、其余×8 |
| net8.0 | 274 | 同分布 |

集中文件：`IReflect.cs` 68、`AssemblyX.cs` 38、`ExtendableConverter.cs` 28、`MachineInfo.Windows.cs` 26、`PluginHelper.cs` 20、`DbTable.cs` 16、`IJsonHost.cs` 14、`JsonReader.cs` 10。
按模块：Reflection 63、Serialization 46、Common 17、Web 10、Data 8、其余零星（去重后计）。

> 2026-10-08 波次 1 修复后重跑：net10.0 360 条——IL2091（8→16）/IL2067（14→26）/IL2090/IL2062 等**传导类警告显性化**（+34），即新注解把库内尚未传导的调用点暴露出来；`IReflect.cs` 70、`AssemblyX.cs` 38 仍为主战场。剩余面规划见 §5.1。

### 2.2 运行期：13 场景冒烟实测

| # | 场景 | JIT | AOT 修复前 | AOT 修复后 |
|---|------|:---:|:---:|:---:|
| 1 | 基础类型转换 Utility | ✅ | ✅ | ✅ |
| 2 | `Type.GetTypeEx`（类型被静态引用） | ✅ | ✅ | ✅ |
| 3 | 日志 XTrace 控制台 | ✅ | ❌ | ✅ |
| 4 | 日志 TextFileLog 文件 | ✅ | ✅ | ✅ |
| 5 | `ReceivedEventArgs.Rent` 直接调用 | ✅ | ❌ | ✅ |
| 6 | 配置 JsonConfigProvider 往返 | ✅ | ❌ | ✅ |
| 7 | JSON JsonHelper 往返 | ✅ | ❌ | ✅ |
| 8 | SpanSerializer 往返 | ✅ | ❌ | ✅ |
| 9 | Binary 写入 | ✅ | ✅ | ✅ |
| 10 | NetServer/NetClient TCP+SrmpCodec 回环 | ✅ | ❌ | ✅ |
| 11 | HttpServer+ApiHttpClient GET（含 `/obj` 对象反序列化断言） | ✅ | ❌ | ✅ |
| 12 | WebSocket 文本回显 | ✅ | ❌ | ✅ |
| 13 | ObjectContainer 服务解析 | ✅ | ❌ | ✅ |
| 14 | 动态能力守卫 PluginHelper（AOT 下抛明确异常） | ✅ | ❌ | ✅ |

> 合计：JIT 14/14；AOT 修复前 4/13（场景 14 为修复后新增）；**修复后 14/14**（2026-10-08 波次 1：`e561198fc`、`341bcc10a`、`1e2050372`、`229f71949`、`5d11e19ce`、`4e9a59415`、`277c9a85a`；冒烟入仓 `c8ac1d228`）；**SG-4 后 17/17**（SG-1 `4b9f703ff`、主包内嵌分发、SG-2 扫描式 DI 生成器、SG-3 Json 成员访问生成器、SG-4 配置绑定生成器）；**异步委托修复后 19/19**（`TaskHelper` 开放泛型 `DynamicDependency` + `MapController` 契约注解，负向验证：移除修复后两场景均变红）；**SG-6 后 21/21**（控制器静态分发表 + 分发优先级锚定，负向验证：关闭生成器即变红）。AOT 产物 `IsDynamicCodeSupported=False`（真 NativeAOT，非 JIT）。

### 2.3 失败机制解析（附实证）

1. **反射构造成员丢失（致命咽喉）**
   `Pool<T>.OnCreate → typeof(T).CreateInstance() → Activator.CreateInstance(type, true)`。AOT 编译后这些类型的构造器不在反射元数据中，运行期抛 `No parameterless constructor defined for type 'NewLife.Net.ReceivedEventArgs'`（库日志实证，每一路 TCP/HTTP 连接均复现）。`ReceivedEventArgs` 在**每个入站数据包**上创建 → 网络栈整体不可用；`Pool<WriteLogEventArgs>` 同类 → 控制台日志不可用。异常还会被 session 默认空日志（`Logger.Null`）吞掉，表现为"连接能建立、消息永不交付"。
2. **反射式成员访问缺契约注解**
   消费者类型（自定义 POCO）的属性仅经反射读写时被 AOT 裁剪：序列化输出 `{}`（实测配置往返文件 8 字节空对象）、反序列化静默回默认值。库未声明 `DynamicallyAccessedMembers`，需求无法向调用方传导、无编译期提示。
3. **Type 参数反射路径**
   `ObjectContainer`（构造函数扫描 + 参数解析）、`DbTable`/`MemoryCache`（`Type.GetType("System."+x)`）、`XmlHelper`（`"To"+type.Name` 方法名拼接查找）、`TaskHelper`（`Task<T>.Result` 反射）等；AOT 下或失效或需等价替换。
   **注解位置实证（2026-10-08 SG-4）**：`DynamicallyAccessedMembers` 仅对 `Type`/`String` 参数与泛型参数生效，放在 `Object`/`Task` 参数上会被 ILC 直接忽略（IL2098）；`ConfigHelper.MapTo/MapFrom` 的 `Object` 参数曾据此误标（已移除）。配置绑定的保留要求只能经 `Load<T>`/`Bind<T>`/`Save<T>` 等泛型入口传导——直调 `MapTo(Object)` 若该类型未经上述入口使用，AOT 下**静默不绑定**（JIT 正常）。
   **动态运行时类型的处置（2026-10-08 同日修复）**：`TaskHelper.GetResult` 读取的是**运行时未知类型** `Task<T>`，无法用参数/泛型注解表达保留，改用开放泛型 `[DynamicDependency(PublicProperties, typeof(Task<>))]`（ILC 支持开放泛型，覆盖全部实例化）；`MapController<T>`/`MapController(Type)` 补 `PublicConstructors | PublicMethods` 注解。修复前 AOT 实测：异步委托返回值 `null`、控制器构造抛 `No parameterless constructor`。
4. **动态能力（设计性不支持）**
   `ScriptEngine`（CodeDom/Emit 运行时代码生成）、`PluginHelper`/`AssemblyX.LoadFrom`（动态加载程序集）、`DynamicXml`/`DynamicInternal`（DLR 动态绑定）、`SpeakProvider`（动态加载语音组件）。这些在 AOT 下**无法通过改造解决**，应编译期注解声明 + 运行期明确报错。

## 3. 模块级能力矩阵

> 2026-10-08 波次 1 修复后实测：Reflection（`CreateInstance` 工厂路径）、Serialization（JSON/配置/Span）、Net/Http/WebSocket、Log、Model（容器解析）核心路径已通过（§2.2）；下表为评估基线快照，剩余未冒烟模块待 §5.1 清单推进。

| 模块 | AOT 现状 | 主要阻塞 | 修复类别 | 成本 |
|------|---------|---------|---------|------|
| Reflection（`IReflect`/`Reflect`/`AssemblyX`） | ❌ 静默失效 | 反射构造/成员访问/类型查找 | 核心改造：AOT 安全工厂 + 注解 | 中 |
| Serialization（Json/配置/Span/Binary/XML） | ❌ 静默失效 | 属性元数据 + DAM 缺位 + `MakeGenericType` | 泛型注解 + 少量替换 | 中 |
| Net/Http/Messaging（网络栈） | ❌ 不可用 | `Pool<T>` 接收链路（单点） | 1 行工厂修复 + 注解（PoC 已证） | 低（杠杆最大） |
| Log（控制台/文件/网络日志） | 控制台 ❌，文件 ✅ | `Pool<WriteLogEventArgs>` | 随 `Pool` 修复 | 低 |
| Model（`ObjectContainer`） | ❌ 无法解析 | 构造函数扫描无 DAM | 注册/解析路径 DAM（或工厂入口） | 中 |
| Configuration（`ConfigHelper`） | ❌ 静默回默认 | 属性映射反射 | 注解 | 低-中 |
| Common（`MachineInfo` 等） | 未冒烟（警告集中） | `Marshal` Type 重载/属性扫描 | 泛型重载 + 注解 | 低 |
| Web（`PluginHelper`/JWT） | 插件 ❌（设计性） | 动态加载 | 注解声明 | 低 |
| Data（`DbTable` 等） | 未冒烟（警告中） | 类型字符串还原 | `TypeCode` 映射 | 低 |
| Extension（`ProcessHelper`/`EnumHelper`） | 未冒烟 | `Marshal`/枚举反射 | 泛型重载 + 注解 | 低 |
| `ScriptEngine`/`DynamicXml`/插件加载 | 设计性冲突 | 运行时代码生成/动态加载 | **注解排除（声明不支持）** | — |

## 4. 支持边界声明（面向下游）

- **目标可用（按推荐档落地后实测验收）**：日志（全量）、TCP/UDP/HTTP/WebSocket 服务端与客户端、JSON（泛型路径）、配置（泛型路径）、二进制/Span 序列化、基础工具与运行时信息。
- **需要调用方配合**：使用泛型 API 时，编译期分析器会提示需要保留的类型元数据（按提示处理即可）；Type 参数路径按警告提示选择替代或显式保留。
- **明确不支持**：`ScriptEngine`（运行时编译 C#）、`PluginHelper`/`Assembly.LoadFrom` 插件加载、`DynamicXml`/`DynamicObject`（DLR）、语音组件动态加载——已提供编译期注解提示（`RequiresDynamicCode`/`RequiresUnreferencedCode`）与运行期明确异常（`RuntimeFeature.IsDynamicCodeSupported` 守卫）。

## 5. 改进路线（三档投资）

| 档 | 内容 | 成本 | 收益 | 风险/遗憾 |
|----|------|------|------|-----------|
| A：最小·诚实声明 | 全库 `Requires*`/DAM 注解 + 不支持清单 + 文档 | 中 | 消费者编译期可见需求，不再静默踩雷；核心路径仍不可用 | 下游仍无法在 AOT 下使用网络/序列化 |
| **B：推荐·声明 + 咽喉修复** | A + `Pool<T>`/反射入口/序列化注解 + 低成本替换 + CI 冒烟 | 中 | **核心路径实测可用**（PoC 已验证等效路径）；服务器/客户端/工业边缘下游获得 AOT 准入 | 注解面广；CI 时长增加；需 CI 兜底防回退 |
| C：激进·全面 AOT 化 | B + 源生成器体系（IoC/JSON/插件静态注册） | 高 | 极端裁剪场景更稳 | 成本高、下游需求未证实；现阶段不建议 |

> **决策（2026-10-08）**：采用**档 C**（档 B + 源生成器体系），分两波落地——波次 1：咽喉修复、注解体系与 CI 防回归（IMP-23~27）；波次 2：源生成器体系设计与切片。波次 1 已完成咽喉修复与主链注解，冒烟 13/13（§2.2）。

**推荐 B 的四点论证**：

1. **为什么这样设计**：阻塞点集中在少数咽喉（PoC 实测 9 失败 → 1），先用泛型工厂/注解把"真实支持边界"变成编译期契约，符合核心库"约 90% 覆盖 + 扩展点"的取舍哲学。
2. **优劣/风险**：修复语义等价（工厂替换/注解为纯元数据），对 JIT 用户零行为影响；主要风险是注解面较广与 CI 增时；不进 CI 则反射引入会静默回退（今日失败模式复现）。
3. **与替代方案的差异**：档 A 不解决"下游要用"；档 C 用高成本换边缘场景且需求未证实；档 B 是收益/成本最优点。
4. **同行对照**：ASP.NET Core 系（Kestrel/最小 API/System.Text.Json source-gen）已验证 AOT/Trim 支持；本库《网络库竞品分析》已把 AOT/Trim 列为 P2 未来缺口且路线（渐进标注）与本推荐一致。SuperSocket 等未核实。

### 5.1 咽喉点工作清单（改进候选切片）

1. `Pool<T>` 泛型工厂（1 行级 + 语义对齐）→ 网络栈/控制台日志/事件总线全通（PoC 已证）。**→ ✅ 已落地（`e561198fc`）**
2. 反射入口注解化：`Reflect`/`IReflect`/`ObjectContainer`/`JsonHelper`/`ConfigHelper`/`ApiHttpClient` 泛型路径加 DAM、Type 参数路径加 `Requires*`；低 TFM 用 Stub 垫片补齐 3 个注解属性。**→ 主链已完成（`341bcc10a`/`1e2050372`/`229f71949`/`5d11e19ce`：JSON/配置/Span/API/容器注册/`IReflect` 工厂）；`MapController` 与 `HttpDispatchTable.RegisterController` 入口补 `PublicConstructors|PublicMethods` 注解（2026-10-08，控制器 AOT 实测可用）；控制器方法的参数绑定与调用已由 SG-6 源生成器静态化（`ParameterBinder` 仅余委托路径，由开放泛型 `DynamicDependency` 保障）；`IReflect`/`AssemblyX`/`TypeX` 宽面成员访问等 Type 参数路径待做（需先设计 RUC 传染治理，避免下游警告海啸）**
3. 低成本等价替换：`XmlHelper` → `switch(TypeCode)`；`MemoryCache`/`DbTable` → TypeCode 映射；`ExcelWriter`/`ProcessHelper` → 泛型重载；`TaskHelper` → 强类型。**→ ✅ 已落地（`4e9a59415`：TypeCodeHelper 六处替换、XmlConvert 显式分发、Marshal 泛型化）；`TaskHelper` 的"契约注解"经 SG-4 复查为非法放置（IL2098，AOT 下不生效），2026-10-08 已改用开放泛型 `DynamicDependency(PublicProperties, typeof(Task<>))` 并实测通过（冒烟 18/19 场景）**
4. 动态能力声明 + 运行期守卫：`ScriptEngine`/`PluginHelper`/`AssemblyX.LoadFrom`/`DynamicXml`/`SpeakProvider`。**→ ✅ 已落地（`277c9a85a`：类/方法级声明 + DynamicCodeHelper 明确异常；AssemblyX 无公开入口、SpeakProvider 已有优雅降级，保持现状）**
5. CI 防回归：冒烟工程（13 场景）入库 + `test.yml` 增加 win-x64 `PublishAot` 冒烟 job；net8+ TFM 开关分析器并清零/登记基线。**→ ✅ 已落地（`c8ac1d228`：冒烟扩至 14 场景入仓 `Test/AotSmoke`，`test.yml` aot job 含分析器报告（不阻断）与 AOT 冒烟断言）**

> 波次 2（源生成器体系）设计见[《AOT源生成器设计》](AOT源生成器设计.md)。**进展（2026-10-08）**：SG-1（骨架 + 主包内嵌分发）与 SG-2（扫描式 DI 注册生成器）已落地——分析器随 `NewLife.Core` 主包分发（`analyzers/dotnet/cs`，不新增包）；编译期扫描容器注册调用点，为"唯一公开无参构造"的实现类型生成静态工厂，容器解析优先命中；下游注册代码零改动，未生成场景自动回退原路径（语义不变）。

## 6. 下游使用指引

- **启用方式**：`<PublishAot>true</PublishAot>` 应写在**应用工程**属性组内；**不要**用命令行全局属性传入（`-p:PublishAot=true` 会波及多 TFM 依赖项目的 net45 等目标，报 `NETSDK1207`）。
- **IoC 注册（源生成器，自动生效）**：引用 `NewLife.Core` 即自动获得源生成器。`AddTransient<TService, TImpl>()` / `AddSingleton<TService, TImpl>()` / `AddScoped<TService, TImpl>()`（含 `TryAdd` 与 `typeof(...)` 字面量形式）会在编译期被扫描，实现类型若为"唯一公开无参构造"，则自动注册静态工厂，容器解析零反射；有参构造注入、实例/工厂注册保持原路径，**注册代码无需改动**。动态注册场景可手动 `ServiceFactoryTable.Register(typeof(TImpl), () => new TImpl())` 补充。
- **Json 序列化（源生成器，自动生效）**：在任意类上标记 `[JsonSerializable(typeof(T))]`（惯例放 `JsonSerializerContext` 派生类；net5+ 共享框架自带该特性），编译期自动为 T 的公开实例属性/字段生成静态读写委托。JsonHelper 序列化命中委托时零反射零表达式编译；未标记类型、init-only/元组/指针/ref struct 成员自动回退原路径，行为不变。不必为标记类型额外声明 DAM。
- **配置绑定（源生成器 + 标准入口）**：继承 `Config<T>` 的配置模型，其公开实例成员读写会命中编译期生成的委托（`MemberAccessorTable`）。AOT 下请经**标准入口**触发绑定——`IConfigProvider.Load<T>()` / `Bind<T>()` / `Save<T>()`、`Config<T>.Current`，其泛型参数带 DAM 注解，会保留模型公开成员元数据。**直接调用 `section.MapTo(model, provider)` / `MapFrom(model)` 无法传导保留要求**（`Object` 参数上 DAM 无效，IL2098）；若该类型未经上述任一入口使用，AOT 下将静默不绑定（JIT 正常、AOT 无效果），建议绑定后校验关键字段。
- **控制器静态分发（源生成器，自动生效）**：`server.MapController<TController>()` / `MapController(typeof(TController))` 会被编译期扫描，为"公开、非泛型、参数可静态化"的方法生成"方法名 → 强类型调用"委托（参数按名绑定并 `ChangeType` 转换、显式默认值、`IHttpContext`/`IServiceProvider` 注入、异步与值类型结果强类型写回），请求分发时优先命中，**AOT 下零反射且不依赖 `MethodInfo` 元数据**；重载歧义、泛型方法、`ref`/`out`/`params`、`ValueTask` 返回值等方法自动回退原反射路径，**注册代码无需改动**。手工装配 `ControllerHandler` 时可用 `HttpDispatchTable.RegisterController(typeof(T), 表)` 补充（该入口同样带保留注解）。
- **遇到 "不支持 AOT/裁剪" 警告**：按提示保留类型元数据（泛型 API 多数自动处理），或改用建议替代路径。
- **遇到 `RequiresDynamicCode` 警告**：该功能（脚本/插件/动态对象）不可用于 AOT，改用静态注册或替代实现。
- **排障提示**：网络/日志异常默认走 `Log`/`SessionLog`（默认 `Logger.Null` 静默）；AOT 排障时务必先接上日志（文件日志在 AOT 下可用）。

## 附录：复现方法

```powershell
# 1) 分析器全量警告（库自身编译）
dotnet build NewLife.Core\NewLife.Core.csproj -f net10.0 -c Release -p:IsAotCompatible=true

# 2) 冒烟实测（13 场景工程；PublishAot 写在工程文件内）
dotnet run -c Release --project <冒烟工程>                  # JIT 基线（应 13/13）
dotnet publish <冒烟工程> -c Release -r win-x64 -o out      # NativeAOT 真编译
.\out\<App>.exe                                              # AOT 运行，逐场景 PASS/FAIL
```

> 2026-10-08 实测证据（分析器日志、JIT/AOT 运行记录、咽喉点 PoC 补丁）存于评估会话工作区；冒烟工程已入仓（`Test/AotSmoke`，CI 见 `.github/workflows/test.yml` 的 `aot` job）。
