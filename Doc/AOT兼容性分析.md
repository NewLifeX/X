# NewLife.Core AOT 兼容性现状与边界

> 基线：v12｜评估方式：**NativeAOT 真实编译 + 运行实测**（win-x64，.NET 10）｜冒烟工程：`AotSmoke/`

---

## 1. 现状摘要

- **核心链路实测全通**：24 场景冒烟 JIT/AOT 双通（§2.1），覆盖日志、网络（TCP/HTTP/WebSocket）、序列化（JSON/Binary/Span/STJ 源生成上下文）、配置、IoC、Http 控制器、CsvDb/DbTable。
- **支持方式**：主链路以「泛型/Type 入口的保留注解 + 源生成器静态表」达成——生成器承接 DI 注册、Json 成员访问、配置绑定、Http 分发四条通道；生成物未覆盖的场景自动回退原反射路径，运行语义不变（§4）。
- **已知边界**：动态能力（脚本/插件/DLR/表达式编译）设计性不支持；部分只能经 `Object`/运行时类型进入的成员枚举路径无法静态表达保留要求（§5 收口记录）。
- 编译期分析器警告是**已知面**（§2.2 分类），不以清零为目标：动不了的部分显式声明，能传导的按需传导。

---

## 2. 实测数据

### 2.1 运行期：24 场景冒烟（JIT/AOT 双通）

| # | 场景 | # | 场景 |
|---|------|---|------|
| 1 | 基础类型转换 Utility | 13 | ObjectContainer 服务解析 |
| 2 | 类型被静态引用时 `Type.GetTypeEx` | 14 | 动态能力守卫 `PluginHelper`（AOT 下抛明确异常） |
| 3 | 日志 XTrace 控制台 | 15 | 源生成器服务工厂 |
| 4 | 日志 TextFileLog 文件 | 16 | 源生成器 Json 成员访问 |
| 5 | `ReceivedEventArgs.Rent` 直接调用 | 17 | 源生成器配置绑定 |
| 6 | 配置 JsonConfigProvider 往返 | 18 | Http 异步委托返回值 |
| 7 | JSON JsonHelper 往返 | 19 | Http 控制器异步操作方法 |
| 8 | SpanSerializer 往返 | 20 | Http 控制器静态分发 |
| 9 | Binary 写入 | 21 | Http 静态分发优先级 |
| 10 | NetServer/NetClient TCP+SrmpCodec 回环 | 22 | CsvDb 读写往返 |
| 11 | HttpServer+ApiHttpClient GET（含对象反序列化） | 23 | DbTable 模型读写往返 |
| 12 | WebSocket 文本回显 | 24 | SystemJson 上下文透传 |

> AOT 产物 `IsDynamicCodeSupported=False`（真 NativeAOT，非 JIT 自欺）。

### 2.2 编译期：AOT 分析器（`IsAotCompatible=true`，net10.0）

- 当前 **328 条**（原始行数，未去重）｜`IL2098`（非法注解放置）**0 条**。
- 三类含义：
  - **动态能力**（IL3050/IL2026/IL3000）：设计性不支持，已按 §5 声明与守卫；
  - **宽面反射入口**（IL2075/IL2070/IL2057）：未做注解治理，由调用方按需治理（§5）；
  - **传导提示**（IL2067/IL2091/IL2095）：调用方按提示保留元数据即可，无需改库。
- 集中文件：`IReflect.cs`、`AssemblyX.cs`、`ExtendableConverter.cs`、`MachineInfo*.cs`、`PluginHelper.cs`。

---

## 3. 关键机制与生成器体系

### 3.1 关键机制备忘

1. **`DynamicallyAccessedMembers` 只对 `Type`/`String` 参数与泛型参数生效**；放在 `Object`/`Task` 等参数上会被 ILC 直接忽略（IL2098），属无效注解。
2. **保留要求必须经「泛型/Type 入口」传导**：`Load<T>`/`Bind<T>`/`Save<T>`、`CsvDb<T>`、`DbTable.WriteModels<T>`、`MapController<T>` 等入口按下发要求；`Object` 参数入口（如直调 `MapTo`）表达不了，只能由其他入口先保留（§5）。
3. **运行时未知泛型**用开放泛型依赖声明覆盖全部实例化：`[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Task<>))]`（`TaskHelper.GetResult` 即此例）。低版本 TFM 由 `Stub` 中的 `DynamicDependencyAttribute` 垫片补齐。
4. **动态能力不可改造**：以 `RequiresDynamicCode`/`RequiresUnreferencedCode` 声明 + `RuntimeFeature.IsDynamicCodeSupported` 守卫，运行期抛明确异常而非静默错。

### 3.2 生成器体系（静态化通道）

| 生成器 | 承接通道 | 生成物 |
|---|---|---|
| `ServiceRegistrarGenerator` | DI 注册（`AddTransient<TService,TImpl>` 等，限唯一公开无参构造） | `ServiceFactoryTable` 工厂注册 |
| `JsonSerializableGenerator` | `JsonHelper` 泛型/Type 入口的成员访问 | `MemberAccessorTable` 读写委托 |
| `ConfigModelGenerator` | `Config<T>` 子类配置绑定 | `ConfigProvider.DataRetrieval` 绑定器 |
| `HttpControllerDispatchGenerator` | `MapController<T>` 控制器动作分发 | `HttpDispatchTable` 方法名→强类型调用 |

- **非 AOT 条件编译**：生成物在 JIT 下同样生效（免反射、免表达式编译与暖机）；生成器缺席或未覆盖场景自动回退反射，语义不变。
- **启动顺序**：生成物以 `[ModuleInitializer]` 注册，先于任何静态构造；分发形态为随主包 `analyzers/dotnet/cs` 分发，下游零配置。
- **新增通道四件套**：运行期表 + 生成器 + 生成物单测（`XUnitTest.SourceGenerator`）+ 冒烟"生成/回退"双场景。

---

## 4. 下游使用指引

- **启用方式**：`<PublishAot>true</PublishAot>` 写在**应用工程**属性组内；**不要**用命令行全局属性传入（`-p:PublishAot=true` 会波及多 TFM 依赖项目的 net45 等目标，报 `NETSDK1207`）。
- **IoC 注册（源生成器，自动生效）**：`container.AddTransient<TService, TImpl>()` / `AddSingleton<...>` 等扩展方法注册调用点会自动生成服务工厂并注册进 `ServiceFactoryTable`（限"唯一公开无参构造"的实现类型）；运行期先查表、未命中再回退反射构造。生成器随 `NewLife.Core` 包自动分发（`analyzers/dotnet/cs`），无需应用侧额外引包。
- **Json 序列化（源生成器，自动生效）**：类型经编译期已知泛型/`Type` 入口进入 `JsonHelper`（`ToJson`/`ToJsonEntity<T>`/反序列化泛型）时，成员访问器走 `MemberAccessorTable`，避免对 `PropertyInfo` 的引用被裁剪。
- **System.Text.Json 运行时类型路径（上下文透传）**：`SystemJson` 的 `Write(Object)`/`Read(json, Type)` 经运行时类型解析；把应用的 `JsonSerializerContext` 传入构造即可让这些类型由源生成元数据解析——`new SystemJson(MyContext.Default)`（可传多个，未声明类型自动回退默认解析器）。上下文只提供成员元数据，命名策略等行为以 `SystemJson` 自身选项为准。
- **Type 参数 API 的使用指引**：容器注册、`Config<T>`、`CsvDb<T>` 等主链已由生成器与泛型入口覆盖；直接使用 `typeof(X).CreateInstance()`、`Reflect.GetPropertyEx/GetMethodEx`、`IReflect` 宽面成员访问等运行时类型 API 时，元数据能否保留取决于调用方——先用泛型入口触达目标类型（如 `Load<T>`、`GetService<T>`），或用 `DynamicDependency` 显式声明保留。
- **配置绑定（源生成器 + 标准入口）**：配置类继承 `Config<T>` 后自动注册入 `ConfigProvider.DataRetrieval`（绑定器编译期生成）；`Config<T>.Current` 与 `IConfigProvider.Load<T>()` 均经泛型入口，AOT 下可正确绑定；**只有**显式用 `ConfigSetting`（`Object` 参数入口）绑定才依赖保留注解，建议改用泛型入口或 `provider.Load<T>()`。
- **控制器静态分发（源生成器，自动生效）**：生成器扫描 `MapController<T>` 注册调用点，为控制器生成"方法名 → 强类型调用"委托并注册进 `HttpDispatchTable`；`ControllerHandler` 命中分发表即零反射调用；未命中（如手工装配的探针控制器）自动回退原反射路径。
- **遇到 "不支持 AOT/裁剪" 警告**：1) 确认该 API 是否在主链路；2) 需要就用泛型入口/保留注解处理；3) 动态能力 API（脚本、插件、表达式编译）在 AOT 下必须关闭或规避（`RuntimeFeature.IsDynamicCodeSupported`）。
- **遇到 `RequiresDynamicCode` 警告**：说明调用路径含运行时生成代码能力，AOT 下运行到会抛异常；按场景改用静态路径或加开关判断。
- **排障提示**：`IsDynamicCodeSupported=False` 表示真 AOT；`Assembly.Location` 为空是 AOT 正常表现，不要用它判断目录。

---

## 5. 已知边界与收口记录（只增不删）

| 项 | 位置 | 现状与理由 |
|---|---|---|
| 实例成员的运行时枚举（`IReflect` 拷贝/填充等宽面目标） | `Reflection/IReflect.cs` | 经 `Object` 参数进入，运行时才知目标是任意类型，**无法静态表达保留要求**；泛型入口（`Copy<T>` 等）已覆盖主场景。 |
| 程序集与类型发现面（`AssemblyX.GetTypes` / `PluginHelper.Load`） | `Reflection/AssemblyX.cs`、`PluginHelper.cs` | 插件发现是 AOT 设计性不支持；`GetTypes` 已加 `RequiresUnreferencedCode` 声明，消费方按需裁剪。 |
| 控制器分发参数绑定（`ref/out/params`/`ValueTask` 等） | `HttpControllerDispatchGenerator` | 生成器主动跳过无法安全生成的签名（回退反射），未求穷尽。 |

---

## 6. 复现方法

```powershell
# 1) 分析器全量警告（库自身编译）
dotnet build NewLife.Core\NewLife.Core.csproj -f net10.0 -c Release -p:IsAotCompatible=true

# 2) 冒烟实测（24 场景工程；PublishAot 写在工程文件内）
dotnet run -c Release --project AotSmoke\AotSmoke.csproj                # JIT 基线
dotnet publish AotSmoke\AotSmoke.csproj -c Release -r win-x64 -o out    # NativeAOT 真编译
.\out\AotSmoke.exe                                                      # AOT 运行，逐场景 PASS/FAIL（非 0 退出码即失败）
```
