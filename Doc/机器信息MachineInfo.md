# 机器信息MachineInfo

## 概述

`NewLife.MachineInfo` 用于获取机器的硬件和系统信息，支持Windows、Linux、Mac等多种操作系统。

**主要功能**：
- 获取操作系统信息（名称、版本）
- 获取硬件信息（CPU、内存、磁盘）
- 获取唯一标识（UUID、GUID、序列号）
- 获取动态信息（CPU占用率、内存使用、网络速度）

**命名空间**: `NewLife`  
**源码**: [NewLife.Core/Common/MachineInfo.cs](https://github.com/NewLifeX/X/blob/master/NewLife.Core/Common/MachineInfo.cs)  
**文档**: https://newlifex.com/core/machine_info

---

## 快速入门

### 基础用法

```csharp
using NewLife;

// 获取当前机器信息（首次调用会初始化）
var machine = MachineInfo.GetCurrent();

Console.WriteLine($"操作系统：{machine.OSName} {machine.OSVersion}");
Console.WriteLine($"处理器：{machine.Processor}");
Console.WriteLine($"内存总量：{machine.Memory / 1024 / 1024 / 1024}GB");
Console.WriteLine($"硬件标识：{machine.UUID}");
Console.WriteLine($"系统标识：{machine.Guid}");
```

### 异步初始化

```csharp
// 异步注册机器信息（推荐在应用启动时调用）
var machine = await MachineInfo.RegisterAsync();

Console.WriteLine($"CPU占用：{machine.CpuRate:P2}");
Console.WriteLine($"可用内存：{machine.AvailableMemory / 1024 / 1024}MB");
```

### 刷新动态数据

```csharp
var machine = MachineInfo.GetCurrent();

// 刷新动态数据（CPU、内存、网络等）
machine.Refresh();

Console.WriteLine($"CPU占用：{machine.CpuRate:P2}");
Console.WriteLine($"空闲内存：{machine.FreeMemory / 1024 / 1024}MB");
Console.WriteLine($"下载速度：{machine.DownlinkSpeed / 1024}KB/s");
Console.WriteLine($"上传速度：{machine.UplinkSpeed / 1024}KB/s");
```

---

## 核心属性

### 静态信息（初始化后不变）

| 属性 | 类型 | 说明 | 示例 |
|-----|------|------|------|
| `OSName` | String | 操作系统名称 | "Windows 11", "Ubuntu 22.04" |
| `OSVersion` | String | 系统版本号 | "10.0.22000", "5.15.0" |
| `Product` | String | 产品名称 | "ThinkPad X1 Carbon" |
| `Vendor` | String | 制造商 | "Lenovo", "Dell" |
| `Processor` | String | 处理器型号 | "Intel Core i7-1165G7" |
| `UUID` | String | 硬件唯一标识（主板序列号） | "xxxx-xxxx-xxxx" |
| `Guid` | String | 软件唯一标识（系统ID） | "xxxx-xxxx-xxxx" |
| `Serial` | String | 计算机序列号 | "PF2ABCDE" |
| `Board` | String | 主板信息 | "20XWCTO1WW" |
| `DiskID` | String | 磁盘序列号 | "1234567890" |
| `Memory` | UInt64 | 内存总量（字节） | 17179869184 (16GB) |

### 动态信息（需要刷新）

| 属性 | 类型 | 说明 |
|-----|------|------|
| `AvailableMemory` | UInt64 | 可用内存（字节） |
| `FreeMemory` | UInt64 | 空闲内存（字节） |
| `CpuRate` | Double | CPU占用率（0-1） |
| `UplinkSpeed` | UInt64 | 网络上行速度（字节/秒） |
| `DownlinkSpeed` | UInt64 | 网络下行速度（字节/秒） |
| `Temperature` | Double | 温度（度） |
| `Battery` | Double | 电池剩余（0-1） |

---

## 核心方法

### RegisterAsync - 异步注册

```csharp
/// <summary>异步注册一个初始化后的机器信息实例</summary>
public static Task<MachineInfo> RegisterAsync()
```

**特点**：
- 异步执行，不阻塞主线程
- 首次调用时初始化，后续直接返回缓存结果
- 自动缓存到文件（`machine_info.json`），加快后续启动速度
- 注册到对象容器 `ObjectContainer`

**示例**：
```csharp
// 应用启动时异步注册
await MachineInfo.RegisterAsync();

// 后续直接使用
var machine = MachineInfo.Current;
```

### GetCurrent - 获取当前实例

```csharp
/// <summary>获取当前信息，如果未设置则等待异步注册结果</summary>
public static MachineInfo GetCurrent()
```

**示例**：
```csharp
var machine = MachineInfo.GetCurrent();
Console.WriteLine(machine.OSName);
```

### Refresh - 刷新动态数据

```csharp
/// <summary>刷新动态数据（CPU、内存、网络等）</summary>
public void Refresh()
```

**示例**：
```csharp
var machine = MachineInfo.GetCurrent();
machine.Refresh();  // 更新CPU占用、内存使用等

Console.WriteLine($"CPU: {machine.CpuRate:P}");
```

---

## 唯一标识说明

### UUID（硬件标识）

- **来源**：主板序列号
- **特点**：与硬件绑定，更换主板后变化
- **注意**：部分品牌（如某些白牌机）可能重复

```csharp
var uuid = machine.UUID;  // 如 "A1B2C3D4-E5F6-..."
```

### Guid（系统标识）

- **来源**：
  - Windows：注册表 `MachineGuid`
  - Linux：`/etc/machine-id`
  - Android：`android_id`
- **特点**：与操作系统安装绑定，重装系统后变化
- **注意**：Ghost系统可能重复

```csharp
var guid = machine.Guid;  // 如 "B1C2D3E4-F5A6-..."
```

### Serial（序列号）

- **来源**：计算机序列号（BIOS）
- **特点**：品牌机独有，与笔记本标签一致
- **注意**：组装机通常为空

```csharp
var serial = machine.Serial;  // 如 "PF2ABCDE"
```

### DiskID（磁盘序列号）

- **来源**：系统盘序列号
- **特点**：与磁盘硬件绑定
- **注意**：更换硬盘后变化

---

## 内存信息详解

### AvailableMemory（可用内存）

**推荐用于应用自我保护和监控告警**

- **Linux**：`MemAvailable`（内核评估可安全分配的内存）
- **Windows**：`ullAvailPhys`（当前可用物理内存）

```csharp
if (machine.AvailableMemory < 100 * 1024 * 1024)  // 小于100MB
{
    Console.WriteLine("内存不足，拒绝新任务");
}
```

### FreeMemory（空闲内存）

**适合用于监控展示和人工分析**

- **Linux**：`MemFree + Buffers + Cached + SReclaimable - Shmem`
- **Windows**：与 `AvailableMemory` 一致

```csharp
Console.WriteLine($"空闲内存：{machine.FreeMemory / 1024 / 1024}MB");
```

---

## 使用场景

### 1. 应用监控

```csharp
var timer = new TimerX(async _ =>
{
    var machine = MachineInfo.GetCurrent();
    machine.Refresh();
    
    // 上报监控数据
    await ReportMetrics(new
    {
        CpuRate = machine.CpuRate,
        AvailableMemory = machine.AvailableMemory,
        DownlinkSpeed = machine.DownlinkSpeed,
        UplinkSpeed = machine.UplinkSpeed
    });
}, null, 0, 60000);  // 每分钟上报
```

### 2. 设备注册

```csharp
var machine = await MachineInfo.RegisterAsync();

var device = new Device
{
    UUID = machine.UUID,
    Guid = machine.Guid,
    OSName = machine.OSName,
    OSVersion = machine.OSVersion,
    Processor = machine.Processor,
    Memory = machine.Memory
};

await RegisterDevice(device);
```

### 3. 授权验证

```csharp
var machine = MachineInfo.GetCurrent();

// 基于硬件标识验证授权
if (!IsLicenseValid(machine.UUID))
{
    throw new UnauthorizedAccessException("未授权的设备");
}
```

### 4. 自适应资源分配

```csharp
var machine = MachineInfo.GetCurrent();
var cpuCount = Environment.ProcessorCount;
var memoryGB = machine.Memory / 1024 / 1024 / 1024;

// 根据机器配置调整线程池大小
ThreadPool.SetMinThreads(cpuCount * 2, cpuCount * 2);

// 根据内存大小调整缓存容量
var cacheSize = (Int32)(memoryGB * 0.1 * 1024 * 1024 * 1024);  // 10%内存
```

### 5. 性能告警

```csharp
var machine = MachineInfo.GetCurrent();
machine.Refresh();

if (machine.CpuRate > 0.9)
{
    SendAlert("CPU使用率过高：" + machine.CpuRate.ToString("P"));
}

if (machine.AvailableMemory < 100 * 1024 * 1024)
{
    SendAlert("可用内存不足：" + machine.AvailableMemory / 1024 / 1024 + "MB");
}
```

---

## 最佳实践

### 1. 应用启动时异步注册

```csharp
class Program
{
    static async Task Main(String[] args)
    {
        // 异步注册机器信息（不阻塞启动）
        _ = MachineInfo.RegisterAsync();
        
        // 继续应用初始化
        await StartApplication();
    }
}
```

### 2. 使用单例模式

```csharp
// MachineInfo 内部已实现单例
var machine = MachineInfo.Current;  // 使用已注册的实例
```

### 3. 定期刷新动态数据

```csharp
// 不要频繁刷新，建议间隔至少1秒
var timer = new TimerX(_ =>
{
    MachineInfo.Current?.Refresh();
}, null, 0, 1000);
```

### 4. 利用文件缓存

```csharp
// 机器信息会自动缓存到：
// - {Temp}/machine_info.json
// - {DataPath}/machine_info.json

// 下次启动时自动加载缓存，加快初始化速度
```

---

## 扩展功能

### 自定义机器信息提供者

```csharp
public class CustomMachineInfo : IMachineInfo
{
    public void Init(MachineInfo info)
    {
        // 自定义初始化逻辑
        info["CustomField"] = "CustomValue";
    }
    
    public void Refresh(MachineInfo info)
    {
        // 自定义刷新逻辑
        info["Timestamp"] = DateTime.Now;
    }
}

// 注册自定义提供者
MachineInfo.Provider = new CustomMachineInfo();
await MachineInfo.RegisterAsync();
```

### 使用扩展属性

```csharp
var machine = MachineInfo.GetCurrent();

// 设置扩展属性
machine["AppVersion"] = "1.0.0";
machine["DeployTime"] = DateTime.Now;

// 获取扩展属性
var version = machine["AppVersion"] as String;
```

---

## 注意事项

### 1. 异步初始化

```csharp
// 推荐：异步注册
await MachineInfo.RegisterAsync();

// 不推荐：同步等待
var machine = MachineInfo.GetCurrent();  // 可能阻塞
```

### 2. 权限要求

某些信息需要特定权限：
- **Windows**：读取注册表需要管理员权限（部分键）
- **Linux**：读取 `/sys` 和 `/proc` 通常需要 root 权限
- **建议**：以普通用户运行，读取失败时使用默认值

### 3. 唯一标识可能重复

- **UUID**：部分白牌机/虚拟机可能重复
- **Guid**：Ghost系统可能重复
- **建议**：组合多个标识生成唯一ID

```csharp
var uniqueId = $"{machine.UUID}_{machine.Guid}_{machine.DiskID}".MD5();
```

### 4. 性能考虑

- **初始化**：首次执行较慢（100-500ms），后续使用缓存
- **刷新**：每次调用有性能开销，避免高频调用
- **建议**：定时刷新（如每秒一次）而非实时刷新

---

## 跨平台支持

### Windows

支持：
- ? OSName, OSVersion
- ? Processor, Memory
- ? UUID（主板序列号）
- ? Guid（MachineGuid）
- ? Serial, Product, Vendor
- ? CpuRate, AvailableMemory
- ? UplinkSpeed, DownlinkSpeed

### Linux

支持：
- ? OSName, OSVersion
- ? Processor, Memory
- ? UUID（DMI）
- ? Guid（/etc/machine-id）
- ? CpuRate, AvailableMemory
- ? UplinkSpeed, DownlinkSpeed
- ?? Serial, Product（部分设备不支持）

### macOS

支持：
- ? OSName, OSVersion
- ? Processor, Memory
- ? UUID（Hardware UUID）
- ?? 其他信息支持有限

---

## 常见问题

### 1. UUID 为什么是空？

可能原因：
- 虚拟机环境
- 白牌机没有主板序列号
- 权限不足

解决：使用 `Guid` 或组合多个标识。

### 2. Guid 为 `0-xxxx` 格式？

表示无法读取系统标识，自动生成的随机GUID。

### 3. 刷新后数据不变？

检查：
- 是否有权限读取系统信息
- 刷新间隔是否过短（建议≥1秒）

### 4. 如何获取所有网卡速度？

```csharp
var interfaces = NetworkInterface.GetAllNetworkInterfaces();
foreach (var ni in interfaces)
{
    var stats = ni.GetIPv4Statistics();
    Console.WriteLine($"{ni.Name}: {stats.BytesReceived} / {stats.BytesSent}");
}
```

---

## 参考资料

- **在线文档**: https://newlifex.com/core/machine_info
- **源码**: https://github.com/NewLifeX/X/blob/master/NewLife.Core/Common/MachineInfo.cs
- **配置**: [setting-核心配置Setting.md](setting-核心配置Setting.md)

---

## 更新日志

- **2025-01**: 完善文档，补充详细说明
- **2024**: 支持 .NET 9.0，优化跨平台支持
- **2023**: 区分 AvailableMemory 和 FreeMemory
- **2022**: 增加网络速度、温度、电池等动态信息
- **2020**: 初始版本，支持基础硬件信息获取
