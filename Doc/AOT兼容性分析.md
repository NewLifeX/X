# NewLife.Core AOT 兼容性现状与边界

> 基线：v12｜评估方式：**NativeAOT 真实编译 + 运行实测**（win-x64，.NET 10）｜冒烟工程：`AotSmoke/`

---

## 1. 现状摘要

- **核心链路实测全通**：31 场景冒烟 JIT/AOT 双通（§2.1），覆盖日志、网络（TCP/HTTP/WebSocket）、序列化（JSON/Binary/Span/STJ 源生成上下文）、配置、IoC、Http 控制器、CsvDb/DbTable、反射泛型入口保留、平台探测（Runtime/MachineInfo/PathHelper/ProcessHelper）、多语言词表查表与全部内置语言。
- **支持方式**：主链路以「泛型/Type 入口的保留注解 + 源生成器静态表」达成——生成器承接 DI 注册、Json 成员访问、配置绑定、Http 分发四条通道；生成物未覆盖的场景自动回退原反射路径，运行语义不变（§4）。
- **已知边界**：动态能力（脚本/插件/DLR/表达式编译）设计性不支持；部分只能经 `Object`/运行时类型进入的成员枚举路径无法静态表达保留要求（§5 收口记录）。
- **告警面已收敛**：编译期分析器与发布期 ILC 均 **0 条**告警（§2.2）。动态能力以声明与守卫承接，能静态表达的一律传导。

---

## 2. 实测数据

### 2.1 运行期：31 场景冒烟（JIT/AOT 双通）

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
| | | 25 | Reflect 泛型入口保留（`GetPropertyEx<T>`） |
| 26 | 运行时平台探测 Runtime | 27 | 机器信息 MachineInfo 平台分支 |
| 28 | 临时路径与文件IO PathHelper | 29 | 进程信息 ProcessHelper |
| 30 | 多语言 Localization 静态表（含 XException / 运行时注册） | 31 | 多语言全部内置语言表在 AOT 下可用（31 种） |

> AOT 判真：原生发布件入口 `Assembly.Location` 为空，而 JIT 基线为磁盘路径；因冒烟工程固定“严格 AOT 姿态”，两种运行的 `IsDynamicCodeSupported` 均为 False，该值不作判别依据。

### 2.2 编译期：分析器与 ILC 告警

- **当前均为 0 条**：`IsAotCompatible` 已在 csproj 对 net8.0+（含 `-windows` 形态）常驻启用，分析器告警 0 条（net8.0/net9.0/net10.0 及对应 `-windows` 形态实测）；AOT 发布的 ILC 告警 0 条（冒烟工程实测）。基线（改造前）为 164 条唯一告警。
- 遗留告警的三类含义与处置方式：
  - **动态能力**（IL3050/IL2026/IL3000）：设计性不支持 → 按 §5 用 `RequiresDynamicCode`/`RequiresUnreferencedCode` 声明 + 运行期守卫，或在边界方法上显式声明裁剪语义；
  - **宽面反射入口**（IL2075/IL2070/IL2057）：运行时类型无法静态表达 → 提供泛型入口承载保留要求，非泛型重载显式声明；
  - **传导提示**（IL2067/IL2091/IL2095）：能静态表达的通过泛型入口/注解传导，调用方按提示保留元数据即可。
- 说明：ILC 只认 `UnconditionalSuppressMessage`，**不认 `#pragma warning disable`**——库内声明一律用特性，才能同时消掉分析器与发布期告警。

---

## 3. 关键机制与生成器体系

### 3.1 关键机制备忘

1. **`DynamicallyAccessedMembers` 只对 `Type`/`String` 参数与泛型参数生效**；放在 `Object`/`Task` 等参数上会被 ILC 直接忽略（IL2098），属无效注解。
2. **保留要求必须经「泛型/Type 入口」传导**：`Load<T>`/`Bind<T>`/`Save<T>`、`CsvDb<T>`、`DbTable.WriteModels<T>`、`MapController<T>` 等入口按下发要求；`Object` 参数入口（如直调 `MapTo`）表达不了，只能由其他入口先保留（§5）。
   - 反射门面同理：`Reflect.GetPropertyEx<T>` / `GetMethodEx<T>` / `CreateInstance<T>` 把保留要求放在泛型参数上（`typeof(T)` 调用点天然保留）；对应的非泛型 `Type` 重载保持零注解（**给宽面 `Type` 参数加注解会让全库 60+ 文件、300+ 调用点弥散传导告警**），动态语义在该处显式声明。
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
- **Type 参数 API 的使用指引**：容器注册、`Config<T>`、`CsvDb<T>` 等主链已由生成器与泛型入口覆盖；直接使用 `typeof(X).CreateInstance()`、`Reflect.GetPropertyEx/GetMethodEx`、`IReflect` 宽面成员访问等运行时类型 API 时，元数据能否保留取决于调用方——先用泛型入口触达目标类型（如 `Load<T>`、`GetService<T>`、`Reflect.GetPropertyEx<T>`），或用 `DynamicDependency` 显式声明保留。
- **配置绑定（源生成器 + 标准入口）**：配置类继承 `Config<T>` 后自动注册入 `ConfigProvider.DataRetrieval`（绑定器编译期生成）；`Config<T>.Current` 与 `IConfigProvider.Load<T>()` 均经泛型入口，AOT 下可正确绑定；**只有**显式用 `ConfigSetting`（`Object` 参数入口）绑定才依赖保留注解，建议改用泛型入口或 `provider.Load<T>()`。
- **控制器静态分发（源生成器，自动生效）**：生成器扫描 `MapController<T>` 注册调用点，为控制器生成"方法名 → 强类型调用"委托并注册进 `HttpDispatchTable`；`ControllerHandler` 命中分发表即零反射调用；未命中（如手工装配的探针控制器）自动回退原反射路径。
- **遇到 "不支持 AOT/裁剪" 警告**：1) 确认该 API 是否在主链路；2) 需要就用泛型入口/保留注解处理；3) 动态能力 API（脚本、插件、表达式编译）在 AOT 下必须关闭或规避（`RuntimeFeature.IsDynamicCodeSupported`）。
- **遇到 `RequiresDynamicCode` 警告**：说明调用路径含运行时生成代码能力，AOT 下运行到会抛异常；按场景改用静态路径或加开关判断。
- **排障提示**：`IsDynamicCodeSupported=False` 表示真 AOT；`Assembly.Location` 为空是 AOT 正常表现，不要用它判断目录。

---

## 5. 已知边界与收口记录（只增不删）

| 项 | 位置 | 现状与理由 |
|---|---|---|
| 实例成员的运行时枚举（`IReflect` 拷贝/填充等宽面目标） | `Reflection/IReflect.cs` | 经 `Object` 参数进入，运行时才知目标是任意类型，**无法静态表达保留要求**；泛型入口（`Copy<T>` 等）已覆盖主场景；边界方法以特性显式声明裁剪语义。 |
| 程序集与类型发现面（`AssemblyX` 全部方法、`PluginHelper.LoadPlugin`） | `Reflection/AssemblyX.cs`、`Web/PluginHelper.cs` | 插件发现是 AOT 设计性不支持（`PluginHelper.LoadPlugin` 已加 `RequiresDynamicCode`/`RequiresUnreferencedCode`）；`AssemblyX` 按方法声明动态加载语义，AOT 下按空值降级。 |
| Windows 系统信息探测（WMI/注册表/COM/性能计数器） | `Common/MachineInfo*.cs` | 依赖 COM 与运行时反射绑定，AOT 下不受支持；探测失败按空值降级，不参与业务主链；可静态表达的（`Marshal.SizeOf<T>`）已改用泛型入口。 |
| 动态语义边界（DLR 动态成员、运行时特性读取、服务定位、调试用内存读取等） | `Reflection/DynamicInternal.cs`、`AttributeX.cs`、`Model/ObjectContainer.cs`、`Extension/ProcessHelper.cs` 等 | 语义本身即运行时类型，逐方法显式声明裁剪语义（含替代入口指引），不逐点改造。 |
| 控制器分发参数绑定（`ref/out/params`/`ValueTask` 等） | `HttpControllerDispatchGenerator` | 生成器主动跳过无法安全生成的签名（回退反射），未求穷尽。 |
| 低版本 TFM 的注解元数据体积 | `Stub/CodeAnalysisAttributes.cs` | 实测注解使用点在每个低版本 DLL 约占 9.5~10 KB（以 `UnconditionalSuppressMessage` 的中文说明串为主，Roslyn 已共享相同 blob）；已用 `[Conditional]` 镜像符号在低版本编译期整体消除（net45 −10,752 B、netstandard2.0 −10,240 B、netcoreapp3.1 −10,752 B；net6.0 仅 RDC 一处）。注解类型定义本身（约 4 KB）保留——编译期必须可解析。边界：低版本资产不再携带这些注解，链接器场景如需恢复，删除对应 Conditional 行即可。 |

---

## 6. 复现方法

```powershell
# 1) 分析器全量警告（库自身编译，期望 0 条；IsAotCompatible 已随 csproj 对 net8.0+ 常驻启用）
dotnet build NewLife.Core\NewLife.Core.csproj -f net10.0 -c Release

# 2) 冒烟实测（31 场景工程；PublishAot 写在工程文件内）
dotnet run -c Release --project AotSmoke\AotSmoke.csproj                # JIT 基线
dotnet publish AotSmoke\AotSmoke.csproj -c Release -r win-x64 -o out    # NativeAOT 真编译（期望 0 条 ILC 告警）
.\out\AotSmoke.exe                                                      # AOT 运行，逐场景 PASS/FAIL（非 0 退出码即失败）
```

> CI 防回归：`.github/workflows/aot.yml` + `.github/workflows/test.yml`（push/PR + 人工手动触发，无夜间任务）—— 分析器 0 告警硬约束（阻断）+ NativeAOT 冒烟矩阵（win-x64 / linux-x64 / linux-arm64 阻断门禁；win-arm64、osx 双架构、musl 容器观察期，稳定后逐项转阻断）；边缘 RID（win-x86 / linux-arm）与 LA64（linux-loongarch64，loong64 容器 + QEMU 仿真）已由原夜间 `edge.yml` 并入常规任务，其中 LA64 的单测与 AOT 冒烟自 2026-10-17 起均为观察期（仅人工 `workflow_dispatch` 触发、失败不阻塞）。指令集矩阵（linux-riscv64 / linux-s390x / linux-ppc64le）自 2026-10-18 起接入：push/PR 只跑 smoke 子集（`XUnitTest.Buffers/Algorithms/Collections`，字节序与位运算确定性用例），全量仅人工 `workflow_dispatch` 触发，均 `continue-on-error` 不阻塞。

---

## 7. 多 RID 支持矩阵与 CI

> 目标：win-x64 / linux-x64 / linux-arm64 单测必选（阻断门禁）；linux-la64 单测与 AOT 冒烟为观察期（仅人工触发、失败不阻塞，QEMU 下整套单测以小时计），扩展 win-arm64、linux-musl、osx 双架构与边缘 RID（win-x86、linux-arm，观察期容错）；linux-riscv64 / linux-s390x / linux-ppc64le 按 LA64 的容器化 QEMU 配方接入（观察期，push/PR 跑 smoke 子集、全量人工触发）。
> 前提：**NativeAOT 官方不支持跨 OS 编译**（Windows→Linux 需 WSL/VM），支持同 OS 内 x64↔arm64 交叉；因此每个 RID 均使用原生 runner 发布运行。

### 7.1 覆盖分层

| 层级 | 目标 RID | 单测 | AOT 冒烟 | runner / 载体 | 状态 |
|---|---|---|---|---|---|
| T1 主流 | win-x64 | ✔（既有 10 个 job） | ✔ 阻断 | windows-latest | 门禁 |
| T1 主流 | win-arm64 | ✔ Core net8/net10 | ✔ | windows-11-arm（自带 ARM64 MSVC 工具链） | 观察期 |
| T1 主流 | linux-x64 | ✔ Core 双 TFM + Compat + 源生成器 | ✔ 阻断 | ubuntu-latest | 门禁 |
| T1 主流 | linux-arm64 | ✔ Core net8/net10 | ✔ 阻断 | ubuntu-24.04-arm | 门禁 |
| T1 主流 | linux-musl-x64 / arm64 | ✔ Core net10（alpine 容器） | ✔ | ubuntu(-arm) + alpine:3.20 容器 | 观察期 |
| T1 主流 | osx-arm64 / osx-x64 | ✔ Core net8/net10 | ✔ | macos-latest / macos-15-intel（官方称可用至 2027-08） | 观察期 |
| T2 边缘 | win-x86 | — | ✔ | windows-latest 交叉发布 | 观察期容错（aot.yml） |
| T2 边缘 | linux-arm（32 位） | — | 探索 | QEMU 容器 JIT 冒烟 + 交叉发布尝试 | 观察期容错（aot.yml） |
| T2 边缘 | linux-riscv64 | ✔ smoke（push/PR）+ 全量（人工） | JIT 冒烟观察期 + 发布探索 | docker + riscv64 容器（QEMU 用户态）+ 社区 SDK（test.yml / aot.yml） | 观察期（不阻塞） |
| T2 边缘 | linux-s390x | ✔ smoke（push/PR）+ 全量（人工） | —（ILCompiler 无 s390x 后端） | docker + s390x 容器（QEMU 用户态）+ Ubuntu `dotnet-runtime-10.0` | 观察期（不阻塞） |
| T2 边缘 | linux-ppc64le | ✔ smoke（push/PR）+ 全量（人工） | —（ILCompiler 无 ppc64le 后端） | docker + ppc64le 容器（QEMU 用户态）+ Ubuntu `dotnet-runtime-10.0` | 观察期（不阻塞） |
| T3 LA64 | linux-loongarch64 | ✔ 全量 net10.0（观察期，人工触发） | JIT 冒烟观察期 + 发布探索 | docker + loong64 容器（QEMU 用户态）+ 龙芯 SDK（test.yml / aot.yml） | 观察期（手动触发，不阻塞） |

- **观察期约定**：win-x86 / linux-arm 及 win-arm64 / osx / musl 等未转正平台 job 带 `continue-on-error`，连续稳定全绿后逐项转阻断门禁；win-x64 / linux-x64 / linux-arm64 为阻断门禁；LA64 的单测与 AOT 冒烟为观察期（`workflow_dispatch` 人工触发，push/PR 不参与、失败不阻塞）；linux-riscv64 / s390x / ppc64le 的 smoke 与全量同样带 `continue-on-error`，全量仅人工触发，smoke 稳定后再评估是否进 push/PR 门禁。
- **测试并行度**：Windows/Linux CI 使用 `test.ci.runsettings`（4 线程，匹配 4 vCPU runner）；macOS 3 vCPU 使用专用 `test.ci.macos.runsettings`（3 线程）。
- **本地 Linux 预跑**：WSL/容器内 `dotnet test XUnitTest.Core -f net8.0/net10.0`（无需任何平台开关）。

### 7.2 LA64（linux-loongarch64，社区支持平台）

- **现状**：官方不发 runtime pack、无 NativeAOT 支持（ILCompiler 源码含 LoongArch64 后端但不构成承诺）。龙芯 [loongson/dotnet](https://github.com/loongson/dotnet) 提供 .NET 8/9/10 SDK（当前 v10.0.112），pack 走 `https://lnuget.loongnix.cn/v3/index.json`。
- **CI 方案（观察期，人工 `workflow_dispatch` 触发，失败不阻塞）**：docker + QEMU 用户态仿真（loong64 binfmt）+ 龙芯 SDK 在 loong64 容器内执行 —— 裸宿主缺 LA64 动态加载器（`Could not open /lib64/ld-linux-loongarch-lp64d.so.1`），LA64 二进制必须在容器内运行；基础镜像 `lcr.loongnix.cn/debian:14-slim`（Debian unstable 的 loong64 端口，自带 loader，apt 指向 deb.debian.org 可用），容器内补装 libicu/libssl，使全球化/加密用例与其它 Linux job 行为一致。LA64 上 vstest 不可用（龙芯 SDK 版不认识 LoongArch64 架构、宿主版为 x64 R2R 复合镜像加载即 BadImageFormatException），单测改为 `xunit.runner.utility` 构建的自宿主执行器：宿主管还原与构建（RID 无关 IL），容器内以 LA64 运行时直接执行测试程序集；另设 `DOTNET_EnableHWIntrinsic=0` 规避仿真层非法指令。QEMU 下整套单测实测 ≥2 小时仍未跑完，故 2026-10-17 起转观察期：`test.yml` 与 `aot.yml` 的 LA64 job 仅人工触发、push/PR 不参与、失败不阻塞（`continue-on-error`）。
- **自托管接入（有真机后升级为阻断级）**：
  1. 龙芯 Linux 机器接入 GitHub Actions self-hosted runner（社区已有 runner 容器镜像 `kubernetes-loong64/actions-runner-loong64`）；
  2. 在 `aot.yml` / `test.yml` 增加 `runs-on: [self-hosted, loongarch64]` 的 job，复用 QEMU job 的步骤（去掉 qemu 安装与仿真环境变量）；
  3. 真机转绿后把状态同步进本节表格并考虑转阻断。

### 7.3 明确排除（避免反复讨论）

| 目标 | 排除理由 |
|---|---|
| Android / iOS | 不并入 `dotnet test` 矩阵（应用模型目标，测试宿主不同）；已由独立 `.github/workflows/mobile.yml` 覆盖：Android API 36 模拟器（`reactivecircus/android-emulator-runner`）+ iOS `macos-26` 模拟器，跑 `Samples/Mobile.Core` 共享冒烟（21 用例），路径触发含 `NewLife.Core/**`、`NewLife.Core.SourceGenerator/**` 与 `Directory.Build.props`；详见 `Doc/移动端技术架构.md` §5.1 |
| tvOS / macCatalyst / WASM | 应用模型类目标，本仓无对应冒烟宿主，不验证 |
| FreeBSD / OpenBSD | 无托管 runner，无官方/社区 runtime pack 渠道；纯托管库验证价值低，需要时按 LA64 的 QEMU 配方另立专项 |
| MIPS64 | .NET（Core/5+）从未支持、无 SDK 渠道；龙芯已从 MIPS64 迁移到 LoongArch（见 §7.4），本仓历史也从未有过 MIPS CI，不再纳入矩阵 |
| Windows x64→Linux 交叉 NativeAOT | 官方不支持跨 OS 编译 |

### 7.4 指令集取舍（为什么是这三家）

| 平台 | 运行时渠道（已核实） | 独特价值 | 结论 |
|---|---|---|---|
| linux-riscv64 | 社区构建 [filipnavara/dotnet-riscv](https://github.com/filipnavara/dotnet-riscv) 跟踪 dotnet VMR 全量构建，已发 `10.0.113`（glibc / musl 两档）；官方不发 runtime pack | 新 JIT 后端 + 弱内存模型；国产硬件主线；版本与仓库 net10 对齐 | 已接入（观察期） |
| linux-s390x | .NET 10 官方支持清单在 RHEL / CentOS Stream 明确列出 s390x；Ubuntu 24.04 官方源有 `dotnet-runtime-10.0` | **大端**：仓库近百处字节序判断（`SpanReader/Writer`、`BitConverter`、`BinaryPrimitives`，默认 `IsLittleEndian=true`）只有大端能验证 | 已接入（观察期），价值最高 |
| linux-ppc64le | 同 s390x（官方清单 + Ubuntu 包） | 64 位小端 + 弱内存模型；无大端差异 | 已接入（观察期），成本低 |
| linux-armv6 | 无官方 runtime pack / 主流渠道 | arm32 已由 `linux-arm`（QEMU JIT 冒烟）覆盖 | 不做 |
| MIPS64 | 无（Mono 时代遗留，龙芯已转 LoongArch） | 无新增覆盖面 | **不做，不再讨论** |
| FreeBSD / OpenBSD | runtime 有 crossrootfs，但无托管 runner、无 runtime pack | 纯托管库价值低 | 不做 |

- **自宿主执行器**：`Tools/SelfHostRunner`（`--namespace A,B` / `--class X` / `--list`，过滤无匹配返回 3）由宿主构建、容器内执行；LA64 仍用 job 内联版，后续统一。
- **为什么要测指令集**：本仓没有 `System.Runtime.Intrinsics` / `Vector128/256` 代码，也没有 Linux 侧 P/Invoke（仅 Windows/iOS/macOS）。矩阵验证的是 JIT/AOT 在新后端上的正确性、**字节序假设**（s390x 是唯一硬证据）、弱内存模型下的并发原语（RISC-V / ppc64le）以及 BCL 行为差异（`MachineInfo`、OpenSSL 加密、端序相关序列化）。
- **运行时来源约束**：NuGet 上没有 `Microsoft.NETCore.App.Runtime.linux-{riscv64,s390x,ppc64le,loongarch64}` 包，因此只能框架依赖运行（宿主构建 RID 无关 IL + 目标架构运行时执行），不能 self-contained / 交叉发布。
- **版本锁定与替换预案**：riscv64 固定 `dotnet-sdk-10.0.113-linux-riscv64.tar.gz`；上游断供时改用 [riscv-forks/dotnet-riscv](https://github.com/riscv-forks/dotnet-riscv)；若 Ubuntu 后续在 riscv64 端口提供 `dotnet-*` 包，则改走 apt，与 s390x/ppc64le 同配方。
- **s390x 首轮失败分流**：大端会同时暴露"用例假设小端"与真实字节序缺陷——前者按《测试稳定性规约》§1 规则 7/8 显式跳过并留痕，后者记入《功能清单》待修复项后修产品；不得为转绿而放宽断言。
- **README 徽章**：支持矩阵转绿后再把 `RISC-V / s390x / ppc64le` 加入 `arch` 徽章（当前仅 `Doc` 声明，避免未经验证的宣称）。

> 事实依据（2026-10 联网核实）：GitHub 托管 runner 标准表（公开仓库标准 runner 免费且不限量）；NativeAOT 交叉编译文档（learn.microsoft.com/dotnet/core/deploying/native-aot/cross-compile）；社区平台清单（dotnet/runtime `docs/workflow/building/coreclr/cross-building.md`）；龙芯 SDK 发布页与 lnuget 源。
