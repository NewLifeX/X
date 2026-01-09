# MachineInfo 使用手册

## 概述
`MachineInfo` 提供跨平台（Windows / Linux / macOS / Xamarin）的一组机器/运行时信息采集方法，包含静态硬件/系统标识以及运行时指标（内存、CPU、网络速率、温度、电池等）。类实现了 `IExtend`，并支持通过 `IMachineInfo` 扩展定制行为。

该类设计为单例使用，获取成本较高（会访问文件系统、执行系统命令或读取操作系统 API），建议通过 `MachineInfo.RegisterAsync()` 在应用启动时初始化一次并复用。

## 快速入门

- 注册并获取单例：

```c#
var mi = await MachineInfo.RegisterAsync();
// 或者阻塞获取
var current = MachineInfo.GetCurrent();
```

- 从对象容器解析（如果已注册）：

```C#
var mi = MachineInfo.Resolve();
```

- 手动刷新运行时数据：

```C#
mi.Refresh();
```

## 主要属性（摘要）

- `OSName`：操作系统名称。
- `OSVersion`：操作系统版本字符串。
- `Product`：产品/机器型号（如云厂商产品名、笔记本型号等）。
- `Vendor`：制造商（如 Dell、Apple、Tencent 等）。
- `Processor`：处理器型号描述字符串。
- `UUID`：硬件唯一标识（通常来自主板或 DMI）。
- `Guid`：软件/系统唯一标识（Windows MachineGuid、Linux machine-id、Android android_id 等）。
- `Serial`：计算机序列号（品牌机显示的序列号）。
- `Board`：主板序列号或家族信息。
- `DiskID`：磁盘序列号或磁盘标识列表（逗号分隔）。
- `Memory`：总物理内存（字节）。
- `AvailableMemory`：可用物理内存（字节），Linux 优先使用 MemAvailable，Windows 使用 GlobalMemoryStatusEx.ullAvailPhys。
- `FreeMemory`：空闲内存（字节），Linux 采用 free 命令口径融合 Buffers/Cached/SReclaimable 等。
- `CpuRate`：CPU 占用率（0~1）。
- `UplinkSpeed` / `DownlinkSpeed`：网络上行/下行速率，单位字节/秒（首次读取为0，需要两次采样才能计算）。
- `Temperature`：温度（摄氏度）。
- `Battery`：电池剩余（小于1 的小数，如 0.8 表示 80%）。

另外，`MachineInfo` 实现 `IExtend`，可通过索引器保存/读取扩展键值：

```C#
mi["CustomKey"] = "Value";
var v = mi["CustomKey"];
```

## 静态方法与辅助

- `MachineInfo.RegisterAsync()`：异步初始化并注册单例实例，内部会尝试从缓存文件（`Path.GetTempPath()/machine_info.json` 或 DataPath）读取历史信息以加速初始化。
- `MachineInfo.GetCurrent()`：同步获取当前单例（如果未初始化会阻塞等待 `RegisterAsync()` 结果）。
- `MachineInfo.Resolve()`：从对象容器解析已注册实例（若已注册）。
- `MachineInfo.GetFreeSpace(string? path = null)`：获取指定路径所在盘符的可用空间（字节），失败返回 `-1`。
- `MachineInfo.GetFiles(string path, bool trimSuffix = false)`：枚举目录下文件名并返回名称列表（用于读取 `/dev/disk` 等）。
- `MachineInfo.IsInContainer`：判断是否运行在容器环境（依据环境变量、/.dockerenv 或 /proc/1/cgroup 内容）。
- `MachineInfo.GetContainerLimits()`：在 Linux 上读取 cgroup v1/v2 的内存与 CPU 配额限制，返回 `(MemoryLimit, CpuLimit)`。
- `MachineInfo.GetContainerMemoryUsage()`：读取容器当前内存使用（字节），优先尝试 cgroup v2（memory.current）。

## 扩展与定制

- 自定义提供者：实现 `IMachineInfo`，并在应用启动时设置 `MachineInfo.Provider`。

```C#
public class MyProvider : IMachineInfo
{
    public void Init(MachineInfo info) { /* 初始化静态字段 */ }
    public void Refresh(MachineInfo info) { /* 刷新运行时字段 */ }
}

MachineInfo.Provider = new MyProvider();
```

此机制允许在不同平台或运行环境中修改字段采集逻辑或补充额外信息。

## 平台行为差异

- Windows：优先从注册表读取 `MachineGuid`、DMI 与 BIOS 信息；使用 `GlobalMemoryStatusEx` 获取内存；部分信息通过 WMIC 或 WMI 获取（对 .NET Framework 提供更丰富支持）。
- Linux：读取 `/proc`、`/sys/class/dmi`、`/etc/*-release`、`/etc/machine-id` 等文件来填充信息；CPU 与内存指标来自 `/proc/stat` 与 `/proc/meminfo`；温度来自 `/sys/class/thermal` 或 hwmon 等。
- macOS：通过 `sw_vers`、`system_profiler`、`diskutil` 等命令读取信息。
- Xamarin/Mono：尝试从 `Android.OS.Build`、`Xamarin.Essentials` 等读取设备信息与电池状态。

## 使用建议与注意事项

- 单例模式：由于采集成本较高，建议在应用启动阶段调用 `RegisterAsync()` 并复用 `MachineInfo.Current`。
- 刷新频率：`Refresh()` 会读取系统文件与执行命令，应避免高频调用（例如每秒多次）。网络速度测量需要两次采样间隔，建议至少间隔 1 秒以上。
- 可见性与清洗：类会自动裁剪不可见字符并填充默认值（若无法读取 Guid/UUID，会生成以 `0-` 前缀的随机 GUID 并借助文件缓存维持稳定）。
- 权限：部分读取操作需要较高权限（例如读取某些 `/sys` 或 WMI），在容器或受限环境下可能无法获取所有信息。
- 可靠性：方法内部捕获异常并在调试级别记录，通常不会抛出异常，但获取不到的字段会保持空或默认值。

## 示例

```csharp
// 启动时注册
await MachineInfo.RegisterAsync();
var mi = MachineInfo.Current;
Console.WriteLine($"OS: {mi.OSName} {mi.OSVersion}");
Console.WriteLine($"CPU: {mi.Processor}");
Console.WriteLine($"Memory: {mi.Memory / 1024 / 1024} MB");

// 定时刷新监控指标
var timer = new System.Threading.Timer(_ => {
    mi.Refresh();
    Console.WriteLine($"CPU: {mi.CpuRate:P}, Up:{mi.UplinkSpeed}B/s, Down:{mi.DownlinkSpeed}B/s");
}, null, 0, 5000);
```

## 常见问题

- Q：首次读取网络速率为什么为 0？
  A：速度使用两次样本差计算，初始化后首次读取没有历史样本，结果为 0，下一次刷新后会得到有效值。

- Q：为什么在容器中看到的内存/CPU 与宿主不同？
  A：容器运行时可能被 cgroup 限制，`GetContainerLimits()` 以及 `GetContainerMemoryUsage()` 能读取这些配额。

- Q：如何在单元测试中模拟 MachineInfo？
  A：为 `MachineInfo.Provider` 提供一个测试实现，或在测试中直接构造 `MachineInfo` 并赋值需要的字段。

## 版本与兼容性

- 该类跨多个目标框架（包括 .NET Framework、.NET Core、.NET 5+ 与 Mono）使用条件编译处理平台差异。

## 参考

- 类源文件：`NewLife.Core/Common/MachineInfo.cs`
- 文档站点：https://newlifex.com/core/machine_info
