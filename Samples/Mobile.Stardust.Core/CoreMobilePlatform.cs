using System.Runtime.InteropServices;
using NewLife;
using Stardust.Models;

namespace Mobile.Stardust;

/// <summary>基于 NewLife.Core MachineInfo 的移动平台适配。Android/iOS 字段由 Core 平台分支采集</summary>
public class CoreMobilePlatform : IMobilePlatform
{
    private readonly MachineInfo _info;

    /// <summary>实例化。使用当前机器信息</summary>
    public CoreMobilePlatform() => _info = MachineInfo.GetCurrent();

    /// <summary>平台名称</summary>
    public String Name => _info.OSName.IsNullOrEmpty() ? "Mobile" : _info.OSName!;

    /// <summary>设备唯一标识</summary>
    public String DeviceId => !_info.UUID.IsNullOrEmpty() ? _info.UUID! : _info.Guid ?? "";

    /// <summary>设备名称。优先系统设置中的设备名称（如 FeiFan），否则使用产品型号</summary>
    public String DeviceName => _info["DeviceName"] as String ?? (!_info.Product.IsNullOrEmpty() ? _info.Product! : Environment.MachineName);

    /// <summary>操作系统名称</summary>
    public String OSName => _info.OSName ?? Name;

    /// <summary>操作系统版本</summary>
    public String OSVersion => _info.OSVersion ?? "";

    /// <summary>处理器架构</summary>
    public String Architecture => RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();

    /// <summary>获取应用私有数据目录</summary>
    /// <returns>可写目录</returns>
    public String GetDataPath()
    {
        var dir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (dir.IsNullOrEmpty()) dir = Path.GetTempPath();

        dir = Path.Combine(dir, "Mobile");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        return dir;
    }

    /// <summary>填充节点信息</summary>
    /// <param name="info">节点信息</param>
    public void FillNodeInfo(NodeInfo info)
    {
        var mi = _info;
        try { mi.Refresh(); } catch { }

        info.OSName = mi.OSName;
        info.OSVersion = mi.OSVersion;
        info.Architecture = Architecture;
        info.MachineName = DeviceName;
        info.Product = mi.Product;
        info.Vendor = mi.Vendor;
        info.Processor = mi.Processor;
        info.Memory = mi.Memory;
        info.AvailableMemory = mi.AvailableMemory;
        info.FreeMemory = mi.FreeMemory;
        info.UUID = mi.UUID;
        info.MachineGuid = mi.Guid;
        info.SerialNumber = mi.Serial;
        info.Board = mi.Board;
        info.DiskID = mi.DiskID;
        info.ProcessorCount = Environment.ProcessorCount;
        info.Runtime = Environment.Version + "";
        info.Framework = RuntimeInformation.FrameworkDescription;

        try { info.TimeZone = TimeZoneInfo.Local.Id; } catch { }
    }

    /// <summary>填充心跳信息</summary>
    /// <param name="info">心跳信息</param>
    public void FillPingInfo(PingInfo info)
    {
        var mi = _info;
        try { mi.Refresh(); } catch { }

        info.Memory = mi.Memory;
        info.AvailableMemory = mi.AvailableMemory;
        info.FreeMemory = mi.FreeMemory;
        info.CpuRate = mi.CpuRate;
        info.Temperature = mi.Temperature;

        // MachineInfo.Battery 为 0~1，星尘平台使用百分比
        info.Battery = mi.Battery > 0 ? Math.Round(mi.Battery * 100, 1) : 0;
    }

    /// <summary>重启当前应用</summary>
    public void RestartApp() => Environment.Exit(0);
}
