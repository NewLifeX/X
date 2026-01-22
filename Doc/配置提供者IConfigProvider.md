# IConfigProvider 配置体系使用说明

本文档介绍 NewLife.Configuration 配置体系的架构、用法与扩展点，适用于 NewLife.Core 仓库。

## 1. 架构概览

- 核心抽象
  - `IConfigProvider`：配置提供者统一接口，暴露键值访问、树形段访问、模型 Load/Save/Bind、变更通知。
  - `IConfigSection`：配置段（节点），键/值/注释与子级（树形）结构。
  - `GetConfigCallback`：获取配置的委托，可用于注入到其它模块中进行按键获取。
- 抽象基类
  - `ConfigProvider`：实现 `IConfigProvider` 的基类，提供懒加载、键路径访问、模型映射、绑定与变更通知、默认提供者注册与工厂方法。
  - `FileConfigProvider`：文件型提供者基类，封装文件读写、轮询热加载（`TimerX`）。
- 具体实现
  - `XmlConfigProvider`：XML 文件。
  - `InIConfigProvider`：INI 文件。
  - `JsonConfigProvider`：JSON 文件（支持注释的预处理）。
  - `HttpConfigProvider`：配置中心（星尘等），带本地缓存、版本与增量上报，支持定时刷新。
  - `ApolloConfigProvider`：针对 Apollo 的适配（命名空间聚合读取）。
  - `CompositeConfigProvider`：复合提供者，聚合多个提供者，读取优先、保存逐个尝试。
- 辅助与模型
  - `Config<T>`：配置模型基类，`Current` 通过标注 `ConfigAttribute` 自动选择提供者并加载/绑定。
  - `ConfigHelper`：在模型与配置树之间进行映射（`MapTo/MapFrom`），支持数组、`IList<T>`、复杂对象、字典。
  - `IConfigMapping`：自定义映射接口，用户可在模型中实现以获得完全控制的映射逻辑。

## 2. 典型用法

- 定义模型

```csharp
[Config("core", provider: null)] // 使用默认提供者（可全局切换）
public class CoreConfig : Config<CoreConfig>
{
    [Description("全局调试。XTrace.Debug")] public bool Debug { get; set; }
    public string? LogPath { get; set; }
    public SysConfig Sys { get; set; } = new();
}

public class SysConfig
{
    [Description("用于标识系统的英文名，不能有空格")] public string Name { get; set; } = "";
    public string? DisplayName { get; set; }
}
```

- 加载/保存

```csharp
var cfg = CoreConfig.Current;      // 首次会绑定并自动落盘（若新建）
cfg.Debug = true;
cfg.Save();
```

- 直接使用提供者

```csharp
var prv = ConfigProvider.Create("config")!; // xml 默认 .config
prv.Init("core");
prv.LoadAll();
var debug = prv["Debug"];           // 键路径支持冒号分隔："Sys:Name"
var section = prv.GetSection("Sys:Name");
```

- 绑定热更新

```csharp
var cfg = new CoreConfig();
var prv = new JsonConfigProvider { FileName = "Config/core.json" };
prv.Bind(cfg, autoReload: true); // 文件变更/远端推送将刷新 cfg
```

- 复合提供者

```csharp
var local = new JsonConfigProvider { FileName = "Config/appsettings.json" };
var remote = new HttpConfigProvider { Server = "http://conf", AppId = "Demo" };
var composite = new CompositeConfigProvider(local, remote);
var name = composite["Sys:Name"]; // 读取时按顺序查找
```

## 3. 扩展点

- 新建文件提供者：继承 `FileConfigProvider`，重写 `OnRead` 与 `GetString/OnWrite` 即可。
- 新建远端提供者：继承 `ConfigProvider`，实现 `LoadAll/SaveAll` 与定时刷新（可选）。
- 注册为默认：`ConfigProvider.Register<MyProvider>("my");`，随后通过 `ConfigProvider.Create("my")` 使用。

## 4. 行为与约定

- 键路径语法：`A:B:C` 表示树形下钻。
- `Keys` 默认只返回根下第一层键；具体实现可覆盖返回深层键集合。
- `IsNew` 用于指示配置源是否首次创建，`Config<T>.Current` 会据此决定是否持久化默认值。
- 注释：模型属性上的 `DescriptionAttribute/DisplayNameAttribute` 会写入配置项注释。
- 列表与数组：`ConfigHelper` 支持 `T[]` 与 `IList<T>`；基元类型元素会进行类型转换。

## 5. 本仓库优化点（已处理）

- 修复：`ConfigHelper.MapToList` 对基元类型元素进行 `ChangeType` 转换，避免 `List<int>` 被写成字符串列表。
- 健壮性：`ConfigProvider.Keys` 访问前调用 `EnsureLoad()`，避免未加载即访问导致的空集合误判。
- 并发安全：`ConfigProvider` 的绑定集合改为 `ConcurrentDictionary`，防止变更通知期间枚举异常。

## 6. 注意事项

- 性能：反射与树形映射不应放在热点高频路径；如需频繁访问，请缓存配置或使用强类型绑定。
- 线程安全：模型绑定与刷新回调不要执行耗时操作，避免阻塞通知链路。
- 兼容性：针对 .NET Framework 与 .NET Standard 均已适配，无需使用平台专属 API。

---
以上。
