#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Versioning;
using NewLife.Reflection;

namespace NewLife;

/// <summary>机器信息 Android 部分</summary>
public partial class MachineInfo
{
    /// <summary>加载 Android 静态信息</summary>
    [SupportedOSPlatform("android")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Android Build 信息经运行时反射读取，读取失败降级为空")]
    private void LoadAndroidInfo()
    {
        var device = ReadDeviceInfo();
        if (device.TryGetValue("Manufacturer", out var str) && !str.IsNullOrEmpty()) Vendor = str;
        if (device.TryGetValue("Model", out str) && !str.IsNullOrEmpty()) Product = str;
        if (device.TryGetValue("SocModel", out str) && !str.IsNullOrEmpty())
            Processor = PrettifyProcessor(str);
        else if (device.TryGetValue("Hardware", out str) && !str.IsNullOrEmpty())
            Processor = PrettifyProcessor(str);
        if (device.TryGetValue("Board", out str) && !str.IsNullOrEmpty())
            Board = str;
        else if (device.TryGetValue("Device", out str) && !str.IsNullOrEmpty())
            Board = str;

        // Xamarin.Essentials 的 Name 通常是系统设置里的设备名称，如“FeiFan”
        if (device.TryGetValue("Name", out str) && !str.IsNullOrEmpty() && !str.EqualIgnoreCase("unknown"))
            this["DeviceName"] = str;

        if (device.TryGetValue("Serial", out str) && !str.IsNullOrEmpty() && !str.EqualIgnoreCase("unknown"))
        {
            Serial = str;
            UUID = str;
        }

        // Android 8.0 起普通应用读不到 Serial，只能退化为 android_id（按应用签名和用户隔离，恢复出厂设置后重置）。
        // 不能用 Build.Fingerprint，它是 ROM 构建标识，同型号同版本的机器完全相同，既会跨设备重复又会随系统升级改变。
        if (device.TryGetValue("android_id", out str) && !str.IsNullOrEmpty() && !str.EqualIgnoreCase("unknown"))
        {
            Guid = str;
            if (UUID.IsNullOrEmpty()) UUID = str;
        }

        var release = GetAndroidBuildVersion("Release");
        if (!release.IsNullOrEmpty())
        {
            OSVersion = release;
            OSName = "Android " + release;
        }
        else
        {
            OSName = "Android";
            if (GetAndroidBuildVersion("SdkInt") is { } sdk) OSVersion = sdk;
        }
    }

    /// <summary>把 Android 硬件代号转换为更易读的处理器名称</summary>
    /// <param name="value">Build.SocModel 或 Build.Hardware，如 kirin980</param>
    /// <returns>友好名称，如 Huawei Kirin 980</returns>
    private static String PrettifyProcessor(String value)
    {
        var name = value.Trim();
        if (name.IsNullOrEmpty()) return name;

        if (name.StartsWithIgnoreCase("kirin"))
        {
            var version = name[5..].Trim();
            return version.IsNullOrEmpty() ? "Huawei Kirin" : "Huawei Kirin " + version;
        }

        if (name.StartsWithIgnoreCase("exynos"))
        {
            var version = name[6..].Trim();
            return version.IsNullOrEmpty() ? "Samsung Exynos" : "Samsung Exynos " + version;
        }

        return name;
    }

    /// <summary>刷新 Android 实时信息</summary>
    [SupportedOSPlatform("android")]
    private void RefreshAndroid()
    {
        // Android 与 Linux 共用 /proc/meminfo、/proc/stat 以及 thermal/power_supply 文件
        RefreshLinux();
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Android Build.VERSION 经运行时反射读取，读取失败降级为空")]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, "Android.OS.Build/VERSION", "Mono.Android")]
    private static String? GetAndroidBuildVersion(String name)
    {
        var type = "Android.OS.Build".GetTypeEx();
        var versionType = type?.GetNestedType("VERSION");
        if (versionType == null) return null;

        var property = versionType.GetProperty(name, BindingFlags.Public | BindingFlags.Static);
        if (property == null) return null;

        try
        {
            var value = property.GetValue(null);
            if (value == null) return null;

            return value is Enum ? Convert.ToInt32(value).ToString() : value.ToString();
        }
        catch
        {
            return null;
        }
    }
}
#endif
