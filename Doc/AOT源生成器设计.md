# NewLife.Core AOT 源生成器体系设计

> 基线：v12（2026-10-08）｜状态：**设计稿**（波次 2 第一步）｜前置：波次 1（IMP-23~27）已完成，冒烟 14/14
> 实测证据：本设计的最小链路 PoC 已完成 JIT 与 NativeAOT 双环境验证（见 §3.3）

---

## 1. 背景与定位

档 C 决策（2026-10-08）的第二波：在波次 1「咽喉修复 + 注解契约」之上，为下游提供**无反射、AOT 原生**的通道。

- 现状：核心链路（日志/网络/JSON/配置/序列化/IoC 主链）在 AOT 下实测可用（14/14），靠「DAM 保留契约 + 少量替换」达成——**依赖裁剪器保留元数据**，属于"可以跑"级别。
- 缺口：反射宽面（`IReflect` 成员访问、`ParameterBinder`、`TaskHelper` 等）无法用单一 DAM 精确表达（IMP-24 剩余面），下游（XCode/Cube）的深度场景需要**编译期确定性**。
- 源生成器路线：把"运行期反射发现"前移到"编译期代码生成"，生成物为纯静态代码（`new` / 直接成员访问），**天然 AOT 安全且更快**。

## 2. 场景清单与价值排序

| # | 场景 | 波次 1 后现状 | 源生成器方案 | 价值 | 归属 |
|---|------|--------------|--------------|------|------|
| 1 | IoC 服务注册/解析 | 泛型注册 DAM 保构造器，主链可用；实例构造仍走运行期构造器扫描 | 扫描容器注册调用点（`AddTransient<TService, TImpl>()` 等）→ 按唯一公开无参构造生成静态工厂，容器解析优先命中（**用户注册代码零改动**） | **高** | 本库 `ObjectContainer` |
| 2 | JSON 序列化 | DAM 保成员可用；极端裁剪下成员仍可能缺失 | `[JsonSerializable]` → 生成成员读写处理器，FastJson 内核可插拔 | 中-高 | 本库 `JsonHelper` |
| 3 | 配置模型绑定 | 泛型入口 DAM 可用 | 生成 `MapTo` 绑定代码 | 中 | 本库 `ConfigHelper` |
| 4 | HTTP handler 参数绑定 | `ParameterBinder` 反射（ILC 警告遗留） | 生成参数绑定代码 | 中 | 本库 `HttpServer` |
| 5 | XCode 数据实体 | （下游）反射 + 缓存 | 下游自建生成器，本库提供特性与约定参考 | 由下游 | XCode 仓 |

**排序原则**：先做收益/成本比最高的 SG-DI（场景 1）——它同时解决 IMP-28 提出的"工厂委托注册路径"，且形态最简单。

## 3. 技术架构

### 3.1 项目与分发

- 分析器项目 **`NewLife.Core.SourceGenerator`**（`netstandard2.0` + `Microsoft.CodeAnalysis.CSharp`，`IsRoslynComponent`），**随主包 `NewLife.Core` 分发**：打包时嵌入 `analyzers/dotnet/cs`，不新增独立 NuGet 包；主库通过自定义 MSBuild 目标保证构建顺序（避免跨 TFM 引用协商与包依赖）
- **运行时主包零新增依赖**（分析器仅编译期生效，不进依赖列表）；下游引用 `NewLife.Core` 即自动获得生成器，未使用生成器特性/标记时零产出、零影响（JIT 行为完全不变）
- 采用 `IIncrementalGenerator`：SG-2（DI 注册）用 `CreateSyntaxProvider` 扫描注册调用点；后续特性驱动切片（如 SG-3）用 `ForAttributeWithMetadataName`（增量语义，编辑器内性能可接受）

### 3.2 生成物策略

- 生成**纯静态代码**：无任何反射调用，`new T(...)` / 属性直接访问 → 天然 AOT 安全（无需 DAM）
- 生成物与运行时的关系：SG-2 通过**运行时工厂表**接入（`NewLife.Model.ServiceFactoryTable.Register`），容器 `CreateInstance` 在显式工厂之后、构造注入之前查表命中 → 不改变既有注册语义与公共调用方式
- SG-2 **不新增特性**（扫描式，见 §6 决策 4）；后续切片如需特性约定，新增在运行时包并仅承载元数据，JIT 下可忽略

### 3.3 PoC 实测（2026-10-08）

最小链路（本仓结构下）已实测通过：

| 验证 | 方式 | 结果 |
|------|------|------|
| 生成器项目（ns2.0 + Roslyn 4.8.0）被 net10 消费项目以 Analyzer 引用 | `ProjectReference OutputItemType="Analyzer"` | ✅ 编译通过、代码生成 |
| 生成注册代码接入 `ObjectContainer` 并被解析 | `dotnet run -c Release` | ✅ `resolved=Foo`、`SG PoC PASS` |
| **NativeAOT 真机运行生成代码路径** | `dotnet publish -r win-x64` + 运行 | ✅ `SG PoC PASS`（exit 0） |

PoC 形态：`[SgService(typeof(IFoo))] class Foo` → 生成 `GeneratedServices.RegisterAll(container)` → `container.AddTransient<IFoo, Foo>()`；生成器源码与实测工程存于评估会话工作区。

## 4. 切片计划

| 切片 | 内容 | 验收 |
|------|------|------|
| SG-1 | 分析器项目骨架 + 打包规范 + 生成器单测工程 + 特性定义（运行时包） | 骨架包可被消费、CI 通过。**→ ✅ 已落地：分析器项目 + GeneratorTestHost 测试基建 + sln/CI 接入 + 主包内嵌分发（打包校验断言 analyzers/dotnet/cs 与无依赖泄漏）** |
| SG-2 | DI 注册生成器：扫描容器注册调用点（`AddTransient<TService, TImpl>()` 等六类 + `typeof` 字面量形式）→ 唯一公开无参构造的实现类型生成静态工厂 | JIT + AOT 双环境实测 + 单测。**→ ✅ 已落地：`ServiceFactoryTable` + 容器接入 + 扫描生成器 + 生成器单测 3 项；冒烟 15 场景 JIT/AOT 15/15（含"源生成器服务工厂"场景断言工厂表命中）** |
| SG-3 | JSON 处理器生成器：`[JsonSerializable]` → 生成读写处理器；FastJson 接入 | 往返一致性 + AOT 实测 |
| SG-4 | 配置绑定生成器 | 绑定往返 + AOT 实测 |
| SG-5 | 收口：IMP-24 剩余面处置决策（哪些改源生成器、哪些保留注解/静音）、文档与使用指引 | 文档 + 冒烟回归 15/15 |

每个切片独立提交、独立验收（JIT + AOT + 单测三证）。

## 5. 风险与遗憾

| 风险 | 说明 | 缓解 |
|------|------|------|
| SDK 兼容 | Roslyn 4.8 要求消费方 SDK ≥ 8.0 | 分析器包声明最低 SDK；低 SDK 下优雅禁用 |
| 生成器调试成本 | 分析器测试需专用测试基建（`Microsoft.CodeAnalysis.CSharp.Analyzer.Testing`） | SG-1 一并建立 |
| 特性命名冲突 | `[Service]`/`[JsonSerializable]` 可能与下游现有特性重名 | 命名评审；必要时前缀命名。SG-2 采用扫描式后该项仅余后续切片 |
| 扫描覆盖面 | 仅覆盖字面量注册调用点（泛型/`typeof`）；变量中转、运行期反射注册不生成 | 未生成时自动回退原运行期路径，语义不变（《AOT兼容性分析》边界一致） |
| 覆盖面有限 | 源生成器只解决"编译期已知类型"；动态场景（脚本/插件）仍不可用 | 与《AOT兼容性分析》的"明确不支持"边界一致 |
| 维护成本 | 生成器是长期资产（版本跟随 Roslyn） | 按切片渐进投入，SG-5 复评 |

## 6. 待决策（用户拍板）

1. ~~是否启动实现（SG-1~SG-5）？~~ **已启动**：SG-1 ✅（骨架 + 测试基建 + 主包内嵌分发）、SG-2 ✅（扫描式 DI 注册生成器）。
2. ~~分发形态：独立 NuGet 包 vs 并入主包？~~ **已决策（2026-10-08）：并入主包 `NewLife.Core` 分发**（主包打包时嵌入 `analyzers/dotnet/cs`，不新增包）。
3. ~~特性命名：短名（如 `[Service]`，需排查冲突）vs 前缀命名？~~ **不再适用（2026-10-08）**：SG-2 采用调用点扫描，不新增特性。
4. SG-2 形态选择：**已决策（2026-10-08）采用扫描式（B）**——自动分析 `AddTransient<TService, TImpl>()` 等注册调用点提取实现类型清单，用户注册代码零改动；生成条件限定"唯一公开无参构造"（与容器原构造语义一致），其余场景回退原路径。

---

（完）
