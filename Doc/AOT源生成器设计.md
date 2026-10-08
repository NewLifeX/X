# NewLife.Core AOT 源生成器体系设计

> 定位：**随 `NewLife.Core` 包分发的编译期源生成器**（`analyzers/dotnet/cs`），把"运行期反射"改为"编译期生成静态指向代码"，为 NativeAOT 提供零反射链路；JIT 下同样生效（免反射、免启动暖机）。
> 当前实测与边界见 [《AOT兼容性分析》](AOT兼容性分析.md)（冒烟 23/23、使用指引 §4、收口记录 §5）。

---

## 1. 定位与分发

| 项 | 说明 |
|---|---|
| 交付形态 | 生成器打进 `NewLife.Core` 主包 `analyzers/dotnet/cs`，应用装包即自动运行，无额外包 |
| 目标框架 | 生成器本体 netstandard2.0（Roslyn 要求），与库主目标框架无关 |
| 作用面 | 仅扫描**应用侧调用点**（`Register<T>`、`ToJson`、`Config<T>`、`MapController`），生成静态表注册代码；不改写库二进制 |
| 兼容底线 | 生成器可缺席：生成物缺失时 `ServiceFactoryTable`/`MemberAccessorTable`/`HttpDispatchTable` 查表落空，自动回退原反射路径，语义不变 |
| 启动顺序 | 生成物以 `[ModuleInitializer]` 注册——**先于**任何静态构造，库侧查表兜底，无时序陷阱 |

---

## 2. 生成器清单

| 生成器 | 承接通道 | 生成物 | 关键文件 |
|---|---|---|---|
| `ServiceFactoryGenerator` | IoC 注册（`Register<TService,TImpl>` 等泛型/Type 调用点） | 每个实现类型一个工厂 lambda，写入 `ServiceFactoryTable` | `NewLife.Core.SourceGenerator/ServiceFactoryGenerator.cs` |
| `JsonMemberAccessorGenerator` | `JsonHelper.ToJson`/反序列化（编译期已知泛型/`Type` 入口） | 每个模型类型的"读/写成员"委托集合，写入 `MemberAccessorTable` | `JsonMemberAccessorGenerator.cs` |
| `ConfigBindingGenerator` | 配置绑定（`Config<T>` 子类自动注册 + `Load<T>`/`Current`） | `ConfigProvider.DataRetrieval` 强类型绑定器 | `ConfigBindingGenerator.cs` |
| `HttpControllerDispatchGenerator` | `MapController<TController>` 控制器动作分发 | "方法名 → 强类型调用"委托表，写入 `HttpDispatchTable` | `HttpControllerDispatchGenerator.cs` |

> 支撑基础设施：`AnalyzerReleases`、`MemberLineBuilder`（共用片段构建）、生成物表类型（`*Table.cs`，库侧普通类型，无反射）。

---

## 3. 机制要点

- **运行期表查询、编译期填表**：库侧保留原反射路径作为兜底，生成物只做"加一条更快/可裁剪的记录"；**未覆盖≠失败**，只是回退。
- **参数绑定语义镜像**：生成的分发委托按库内 `ParameterBinder` 的规则生成（Query/Form/Route/body 取值、类型转换、可空性），保证 JIT/AOT 行为一致。
- **无法安全生成的签名自动跳过**：泛型方法、含 `ref/out/params`、含 `ValueTask`/元组/指针/`ref struct`、不可访问成员等直接跳过（回退），不强行生成。
- **片段模型增量友好**：生成片段以字符串（含分隔符）作为增量缓存键，去重后拼接；避免语法树等不稳定键导致无意义重生成。
- **不接管按 API 面**：只按"功能入口"（调用点）承接；返回 `PropertyInfo` 之类的反射 API 无法被生成器接管，仍属边界（见分析稿 §5）。

---

## 4. 新增生成器的接入步骤（4 步）

1. **定义运行期表**（库侧）：`xxxTable.cs`——静态 `Register` + 查询方法，公开面最小化（仅生成器与库内需要）。
2. **写生成器**：`IIncrementalGenerator`，`SyntaxProvider.CreateSyntaxProvider` 过滤候选调用点 → 收集符号信息 → `RegisterSourceOutput` 输出注册代码。
3. **注册生成物**：`[ModuleInitializer]` 提交静态表；生成代码用最简语法（无泛型推断依赖），避免 IL 兼容面问题。
4. **接入消费点**：库内在原反射调用点前**先查表**、未命中回退，保持行为不变。

> 测试要求：`XUnitTest.SourceGenerator` 校验生成物文本；`Test/AotSmoke` 增加"生成路径 + 回退路径"两个场景，AOT 真编译运行双验证（当前 23/23）。

---

## 5. 边界与风险

- **仅覆盖编译期已知类型**：运行时才知的类型（`MapTo(Object)`、开放泛型实例化、反射枚举成员）无法静态生成；由保留注解/开放泛型依赖兜底（见分析稿 §3）。
- **生成器不能消除"库内部"反射**：它只把**应用侧调用点**静态化；库内部按 `Object` 参数进入的宽面反射（`IReflect` 拷贝等）仍需注解或入口约束。
- **维护成本**：每新增一条通道 = 表类型 + 生成器 + 单测 + 冒烟场景四件套；只在"真实主链路 + AOT 必须"时新增，不为理论场景铺面。
- **生成物兼容**：生成器版本升级可能改变生成物文本（无 API 契约），下游无需感知；缺省行为始终是回退反射，因此**生成器异常不破坏运行**。
