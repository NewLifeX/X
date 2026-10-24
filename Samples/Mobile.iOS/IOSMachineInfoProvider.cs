using Foundation;
using NewLife;
using UIKit;

namespace Mobile.iOSHost;

/// <summary>iOS 宿主机器信息补充</summary>
/// <remarks>设备名称、稳定标识与电池信息无法由 NewLife.Core 直接读取，由宿主补充。</remarks>
public sealed class IOSMachineInfoProvider : IMachineInfo
{
    /// <summary>补充静态信息</summary>
    /// <param name="info">机器信息</param>
    public void Init(MachineInfo info)
    {
        var device = UIDevice.CurrentDevice;

        var name = device.Name;
        if (!name.IsNullOrEmpty()) info["DeviceName"] = name;

        var id = GetIdentifier();
        if (!id.IsNullOrEmpty())
        {
            info.UUID = id;
            if (info.Guid.IsNullOrEmpty() || info.Guid.StartsWith("0-")) info.Guid = id;
        }

        try
        {
            device.BatteryMonitoringEnabled = true;
        }
        catch { }
    }

    /// <summary>刷新实时信息</summary>
    /// <param name="info">机器信息</param>
    public void Refresh(MachineInfo info)
    {
        try
        {
            var device = UIDevice.CurrentDevice;
            if (!device.BatteryMonitoringEnabled) device.BatteryMonitoringEnabled = true;

            var level = device.BatteryLevel;
            if (level >= 0) info.Battery = level;
        }
        catch { }
    }

    private static String? GetIdentifier()
    {
        try
        {
            var id = UIDevice.CurrentDevice.IdentifierForVendor?.ToString();
            if (!id.IsNullOrEmpty()) return id;
        }
        catch { }

        try
        {
            var defaults = NSUserDefaults.StandardUserDefaults;
            var id = defaults.StringForKey("device_id");
            if (id.IsNullOrEmpty())
            {
                id = Guid.NewGuid().ToString("N");
                defaults.SetString(id, "device_id");
            }

            return id;
        }
        catch
        {
            return null;
        }
    }
}
