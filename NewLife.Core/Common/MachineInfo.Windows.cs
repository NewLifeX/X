using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using NewLife.Log;
using NewLife.Windows;
using NewLife.Serialization;
using System.Runtime.Versioning;

// 本文件为 Windows 系统信息探测（注册表/WMI/性能计数器/COM），属动态能力边界：
// AOT 下这些分支不受支持并按空值降级；相关 AOT 语义在每个方法上显式声明

#if NETFRAMEWORK || NET5_0_OR_GREATER
using Microsoft.Win32;
#endif

namespace NewLife;

/// <summary>机器信息 Windows 部分</summary>
public partial class MachineInfo
{
    #region 公共基础设施
    [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [SecurityCritical]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern Boolean GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    internal struct MEMORYSTATUSEX
    {
        internal UInt32 dwLength;

        internal UInt32 dwMemoryLoad;

        internal UInt64 ullTotalPhys;

        internal UInt64 ullAvailPhys;

        internal UInt64 ullTotalPageFile;

        internal UInt64 ullAvailPageFile;

        internal UInt64 ullTotalVirtual;

        internal UInt64 ullAvailVirtual;

        internal UInt64 ullAvailExtendedVirtual;

        internal void Init() =>
#if NET45
            dwLength = checked((UInt32)Marshal.SizeOf(typeof(MEMORYSTATUSEX)));
#else
            dwLength = checked((UInt32)Marshal.SizeOf<MEMORYSTATUSEX>());
#endif
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern Boolean GetSystemTimes(out FILETIME idleTime, out FILETIME kernelTime, out FILETIME userTime);

    private struct FILETIME(Int64 time)
    {
        public UInt32 Low = (UInt32)time;

        public UInt32 High = (UInt32)(time >> 32);

        public readonly Int64 ToLong() => (Int64)(((UInt64)High << 32) | Low);
    }

    /// <summary>刷新内存和CPU数据（Windows/Linux共用 SystemTime）</summary>
    private void RefreshMemoryAndCpu()
    {
        MEMORYSTATUSEX ms = default;
        ms.Init();
        if (GlobalMemoryStatusEx(ref ms))
        {
            Memory = ms.ullTotalPhys;
            AvailableMemory = ms.ullAvailPhys;
            FreeMemory = ms.ullAvailPhys;
        }

        GetSystemTimes(out var idleTime, out var kernelTime, out var userTime);

        var current = new SystemTime
        {
            IdleTime = idleTime.ToLong(),
            TotalTime = kernelTime.ToLong() + userTime.ToLong(),
        };

        var idle = current.IdleTime - (_systemTime?.IdleTime ?? 0);
        var total = current.TotalTime - (_systemTime?.TotalTime ?? 0);
        _systemTime = current;

        CpuRate = total == 0 ? 0 : Math.Round((Double)(total - idle) / total, 4);
    }

#if NETFRAMEWORK || NET6_0_OR_GREATER
    /// <summary>从注册表读取硬件信息（GUID/UUID/Vendor/Product/Processor）并做 csproduct 兜底</summary>
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
#endif
    private void LoadHardwareFromRegistry()
    {
        var str = "";

        var reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
        if (reg != null) str = reg.GetValue("MachineGuid") + "";
        if (str.IsNullOrEmpty())
        {
            reg = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            reg = reg?.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
            if (reg != null) str = reg.GetValue("MachineGuid") + "";
        }

        if (!str.IsNullOrEmpty()) Guid = str;

        reg = Registry.LocalMachine.OpenSubKey(@"SYSTEM\HardwareConfig");
        if (reg != null)
        {
            str = (reg.GetValue("LastConfig") + "")?.Trim('{', '}').ToUpper();

            // UUID取不到时返回 FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF
            if (!str.IsNullOrEmpty() && !str.EqualIgnoreCase("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF")) UUID = str;
        }

        reg = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS");
        reg ??= Registry.LocalMachine.OpenSubKey(@"SYSTEM\HardwareConfig\Current");
        if (reg != null)
        {
            Product = (reg.GetValue("SystemProductName") + "").Replace("System Product Name", null);
            if (Product.IsNullOrEmpty()) Product = reg.GetValue("BaseBoardProduct") + "";

            Vendor = reg.GetValue("SystemManufacturer") + "";
            if (Vendor.IsNullOrEmpty()) Vendor = reg.GetValue("ASUSTeK COMPUTER INC.") + "";
        }

        reg = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
        if (reg != null) Processor = reg.GetValue("ProcessorNameString") + "";

        // 旧版系统（如win2008）没有UUID的注册表项，需要用WMI查询。也可能因为过去的某个BUG，导致GUID跟UUID相等
        if (UUID.IsNullOrEmpty() || UUID == Guid || Vendor.IsNullOrEmpty())
        {
            // 优先进程内 WMI（System.Management/COM），wmic 进程兜底；wmic 自 Windows 11 24H2/Server 2025 起默认不再安装
            var csproduct = ReadWmiComMulti("csproduct", "Name", "UUID", "Vendor");
            if (csproduct.Count == 0) csproduct = ReadWmic("csproduct", "Name", "UUID", "Vendor");
            if (csproduct.Count > 0)
            {
                if (csproduct.TryGetValue("Name", out str) && !str.IsNullOrEmpty() && Product.IsNullOrEmpty()) Product = str;
                if (csproduct.TryGetValue("UUID", out str) && !str.IsNullOrEmpty()) UUID = str;
                if (csproduct.TryGetValue("Vendor", out str) && !str.IsNullOrEmpty()) Vendor = str;
            }
        }
    }
#endif
    #endregion

    #region Windows WMI辅助
    /// <summary>通过 PowerShell 命令读取信息</summary>
    /// <param name="command">PowerShell命令</param>
    /// <returns>解析后的字典</returns>
    public static IDictionary<String, String> ReadPowerShell(String command)
    {
        var dic = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);

        var args = $"-Command \"{command}\"";
        var str = "powershell.exe".Execute(args, 3_000) ?? String.Empty;
        if (!String.IsNullOrWhiteSpace(str))
        {
            foreach (var item in str.DecodeJson()!)
            {
                dic[item.Key] = item.Value?.ToString() ?? String.Empty;
            }
        }
        return dic;
    }

    /// <summary>通过 WMIC 命令读取信息（末位兜底）</summary>
    /// <remarks>
    /// wmic 自 Windows 11 24H2 / Server 2025 起默认不再随系统安装（可作按需功能手动恢复）。
    /// 命令缺失时返回空字典并记录调试日志，不抛异常；获取系统信息建议优先使用 ReadWmiComMulti/GetInfo（含 System.Management、WMI COM 多级降级）。
    /// </remarks>
    /// <param name="type">WMI类型</param>
    /// <param name="keys">查询字段</param>
    /// <returns>解析后的字典；命令缺失或查询失败时为空字典</returns>
    public static IDictionary<String, String> ReadWmic(String type, params String[] keys)
    {
        var dic = new Dictionary<String, IList<String>>(StringComparer.OrdinalIgnoreCase);
        var dic2 = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);

        var args = $"{type} get {keys.Join(",")} /format:list";
        var str = "wmic".Execute(args, 0, false)?.Trim();
        if (str.IsNullOrEmpty()) return dic2;

        var ss = str.Split("\r\n");
        foreach (var item in ss)
        {
            var ks = item?.Split('=');
            if (ks != null && ks.Length >= 2)
            {
                var k = ks[0].Trim();
                var v = ks[1].Trim().TrimInvisible();
                if (!k.IsNullOrEmpty() && !v.IsNullOrEmpty())
                {
                    if (!dic.TryGetValue(k, out var list))
                        dic[k] = list = [];

                    list.Add(v);
                }
            }
        }

        // 排序，避免多个磁盘序列号时，顺序变动
        foreach (var item in dic)
        {
            dic2[item.Key] = item.Value.OrderBy(e => e).Join();
        }

        return dic2;
    }

    private static readonly Dictionary<String, String> _wmiAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        { "csproduct", "Win32_ComputerSystemProduct" },
        { "os", "Win32_OperatingSystem" },
        { "diskdrive", "Win32_DiskDrive" },
        { "bios", "Win32_BIOS" },
        { "baseboard", "Win32_BaseBoard" },
        { "win32_battery", "Win32_Battery" },
    };

    /// <summary>通过 WMI 查询多属性。优先进程内 WMI（System.Management/COM），极端场景回退 wmic 进程</summary>
    /// <param name="type">WMI 类型，支持 wmic 格式如 "csproduct"、"/namespace:\\root\wmi path MSAcpi_ThermalZoneTemperature"</param>
    /// <param name="keys">查询字段</param>
    /// <returns>属性字典</returns>
    public static IDictionary<String, String> ReadWmiComMulti(String type, params String[] keys)
    {
        // 解析命名空间和类名
        var nameSpace = "root\\cimv2";
        var wmiClass = type;

        if (type.StartsWith("/namespace:"))
        {
            var p = type.IndexOf(" path ", StringComparison.Ordinal);
            if (p > 0)
            {
                nameSpace = type["/namespace:".Length..p].Trim('\\');
                wmiClass = type[(p + 6)..].Trim();
            }
        }
        else if (type.StartsWith("path "))
        {
            wmiClass = type[5..].Trim();
        }

        // 映射 wmic 别名到 WMI 类名
        var alias = wmiClass;
        var whereIdx = wmiClass.IndexOf(" where ", StringComparison.OrdinalIgnoreCase);
        if (whereIdx > 0) alias = wmiClass[..whereIdx];

        if (_wmiAliases.TryGetValue(alias, out var fullClass))
        {
            wmiClass = whereIdx > 0 ? $"{fullClass}{wmiClass[whereIdx..]}" : fullClass;
        }

        // 逐属性查询
        var dic = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in keys)
        {
            var value = GetInfo(wmiClass, key, nameSpace);
            if (!value.IsNullOrEmpty()) dic[key] = value;
        }

        return dic;
    }

    /// <summary>通过 WMI 查询单个属性（内部经 ReadWmiComMulti 的多级降级链）</summary>
    /// <param name="type">WMI 类型</param>
    /// <param name="property">属性名</param>
    /// <returns>属性值，失败返回 null</returns>
    private static String? ReadWmiComSingle(String type, String property)
    {
        var dic = ReadWmiComMulti(type, property);
        return dic.TryGetValue(property, out var v) && !v.IsNullOrEmpty() ? v : null;
    }
    #endregion

#if !NETFRAMEWORK
    #region 现代方法（非 NETFRAMEWORK）
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
#endif
    private void LoadWindowsInfo()
    {
        var str = "";

        // 从注册表读取 MachineGuid（NET6+ 用 Registry，低版本用 reg.exe）
#if NET6_0_OR_GREATER
        LoadHardwareFromRegistry();
#else
        str = "reg".Execute(@"query HKLM\SOFTWARE\Microsoft\Cryptography /v MachineGuid", 0, false);
        if (!str.IsNullOrEmpty() && str.Contains("REG_SZ")) Guid = str.Substring("REG_SZ", null).Trim();

        var csproduct = ReadWmiComMulti("csproduct", "Name", "UUID", "Vendor");
        if (csproduct != null)
        {
            if (csproduct.TryGetValue("Name", out str)) Product = str;
            if (csproduct.TryGetValue("UUID", out str)) UUID = str;
            if (csproduct.TryGetValue("Vendor", out str)) Vendor = str;
        }
#endif

        // 获取操作系统名称和版本
        var os = ReadWmiComMulti("os", "Caption", "Version");
        if (os == null || os.Count == 0)
        {
            os = ReadPowerShell("Get-WmiObject Win32_OperatingSystem | Select-Object Caption, Version | ConvertTo-Json");
        }
        if (os is { Count: > 0 })
        {
            if (os.TryGetValue("Caption", out str)) OSName = str.TrimPrefix("Microsoft").Trim();
            if (os.TryGetValue("Version", out str)) OSVersion = str;
        }

        // 磁盘和BIOS序列号
        var diskStr = ReadWmiComSingle("diskdrive where mediatype=\"Fixed hard disk media\"", "serialnumber");
        if (!diskStr.IsNullOrEmpty()) DiskID = diskStr?.Trim();

        var sn = ReadWmiComSingle("bios", "serialnumber");
        if (!sn.IsNullOrEmpty() && !sn.EqualIgnoreCase("System Serial Number")) Serial = sn?.Trim();

        var boardStr = ReadWmiComSingle("baseboard", "serialnumber");
        if (!boardStr.IsNullOrEmpty()) Board = boardStr?.Trim();

        //// 不要在刷新里面取CPU负载，因为运行wmic会导致CPU负载很不准确，影响测量
        //var cpu = ReadWmic("cpu", "Name", "ProcessorId", "LoadPercentage");
        //if (cpu != null)
        //{
        //    if (cpu.TryGetValue("Name", out str)) Processor = str;
        //    //if (cpu.TryGetValue("ProcessorId", out str)) CpuID = str;
        //    if (cpu.TryGetValue("LoadPercentage", out str)) CpuRate = (Single)(str.ToDouble() / 100);
        //}

        // OS 名称和版本的兜底
        if (OSName.IsNullOrEmpty())
            OSName = RuntimeInformation.OSDescription.TrimPrefix("Microsoft").Trim();
        if (OSVersion.IsNullOrEmpty())
            OSVersion = Environment.OSVersion.Version.ToString();
    }

    /// <summary>刷新 Windows 动态数据（现代路径）</summary>
    private void RefreshWindows()
    {
        RefreshMemoryAndCpu();

        var power = new PowerStatus();

        if (!_excludes.Contains(nameof(Temperature)))
        {
            var str = ReadWmiComSingle(@"/namespace:\\root\wmi path MSAcpi_ThermalZoneTemperature", "CurrentTemperature");
            if (!str.IsNullOrEmpty())
            {
                Temperature = (str.SplitAsInt().Average() - 2732) / 10.0;
            }
            else
            {
                if (XTrace.Log.Level <= LogLevel.Debug) XTrace.WriteLine("Temperature信息无法读取");
                _excludes.Add(nameof(Temperature));
                Temperature = 0;
            }
        }

        if (power.BatteryLifePercent > 0)
            Battery = power.BatteryLifePercent;
        else if (!_excludes.Contains(nameof(Battery)))
        {
            var str = ReadWmiComSingle("path win32_battery", "EstimatedChargeRemaining");
            if (!str.IsNullOrEmpty())
            {
                Battery = str.SplitAsInt().Average() / 100.0;
            }
            else
            {
                if (XTrace.Log.Level <= LogLevel.Debug) XTrace.WriteLine("Battery信息无法读取");
                _excludes.Add(nameof(Battery));
                Battery = 0;
            }
        }
    }

    /// <summary>获取WMI信息。net*-windows 优先反射 System.Management 再回退 WMI COM；其它运行时用 WMI COM；wmic 进程为末位兜底（自 Windows 11 24H2 / Server 2025 起默认不再安装）</summary>
    /// <param name="path">WMI路径</param>
    /// <param name="property">属性名</param>
    /// <param name="nameSpace">命名空间，默认 root\cimv2</param>
    /// <returns>查询结果</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "运行时类型反射/动态加载边界；AOT 下不受支持或需改用泛型入口（见分析稿 §4/§5）")]
    public static String GetInfo(String path, String property, String? nameSpace = null)
    {
        // 规范化命名空间：WMI 要求反斜杠分隔，调用者可能传入 root/wmi 等正斜杠格式
        if (nameSpace != null) nameSpace = nameSpace.Replace("/", "\\");

        var ns = nameSpace ?? "root\\cimv2";

#if __WIN__
        // net*-windows：优先反射 System.Management 走标准 WMI，失败回退 COM；两者均失败降级 wmic 兜底
        {
            var value = QueryWmiSystemManagement(path, property, ns);
            if (value != null) return value;

            value = QueryWmiCom(path, property, ns);
            if (value != null) return value;

            // 两种 WMI 路径均失败（如 WMI 服务异常），降级到 wmic 进程兜底
            if (XTrace.Log.Level <= LogLevel.Debug)
                XTrace.WriteLine("WMI 均失败，回退 wmic: {0}.{1}", path, property);
        }
#else
        // 其它运行时（netstandard/net*非windows）：WMI 仅 Windows 主机支持，其它平台直接返回空
#if NET5_0_OR_GREATER
        if (!OperatingSystem.IsWindows()) return "";
#else
        if (!Runtime.Windows) return "";
#endif

        var value = QueryWmiCom(path, property, ns);
        if (value != null) return value;
#endif

        // wmic 兜底（__WIN__ 降级 + 其它运行时）。wmic 自 Windows 11 24H2/Server 2025 起默认不再安装，此时快速返回空结果
        // 不带命名空间时也要用 path 前缀以支持完整类名
        var type = !ns.EqualIgnoreCase("root\\cimv2") ? $"/namespace:\\\\{ns} path {path}" : $"path {path}";
        var dic = ReadWmic(type, property);
        return dic.TryGetValue(property, out var v) ? v : "";
    }

    /// <summary>通过反射 System.Management 查询 WMI 属性。用于 net*-windows，不新增编译期依赖，不开进程</summary>
    /// <remarks>
    /// System.Management.dll 随 Windows 桌面运行时提供（net*-windows），通过反射调用避免核心库直接引用该程序集。
    /// 内部使用 ManagementObjectSearcher(nameSpace, wql).Get() 枚举结果并读取属性。
    /// </remarks>
    /// <param name="wmiClass">WMI 类名，如 Win32_OperatingSystem</param>
    /// <param name="property">属性名</param>
    /// <param name="nameSpace">命名空间，如 root\cimv2</param>
    /// <param name="throwOnError">失败时是否抛出异常，默认false吞掉异常返回null</param>
    /// <returns>查询结果，失败返回null</returns>
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
#endif
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "运行时类型反射/动态加载边界；AOT 下不受支持或需改用泛型入口（见分析稿 §4/§5）")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "运行时类型反射/动态加载边界；AOT 下不受支持或需改用泛型入口（见分析稿 §4/§5）")]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "运行时类型反射/动态加载边界；AOT 下不受支持或需改用泛型入口（见分析稿 §4/§5）")]
    internal static String? QueryWmiSystemManagement(String wmiClass, String property, String nameSpace, Boolean throwOnError = false)
    {
        try
        {
            // 反射加载 System.Management（Windows桌面运行时内置，无编译期引用）
            var asm = Assembly.Load("System.Management");
            var searcherType = asm.GetType("System.Management.ManagementObjectSearcher")!;
            var moType = asm.GetType("System.Management.ManagementObject")!;
            var enumType = asm.GetType("System.Management.ManagementObjectCollection")!;

            // new ManagementObjectSearcher(nameSpace, wql)
            var wql = $"SELECT {property} FROM {wmiClass}";
            var searcher = Activator.CreateInstance(searcherType, nameSpace, wql);
            if (searcher == null) return null;

            // .Get() → ManagementObjectCollection
            var getMethod = searcherType.GetMethod("Get", Type.EmptyTypes)!;
            var results = getMethod.Invoke(searcher, null);
            if (results == null) return null;

            // foreach (ManagementObject obj in results)
            var bbs = new List<String>();
            var getEnumeratorMethod = enumType.GetMethod("GetEnumerator")!;
            var enumerator = getEnumeratorMethod.Invoke(results, null);
            var moveNextMethod = enumerator!.GetType().GetMethod("MoveNext")!;
            var currentProperty = enumerator.GetType().GetProperty("Current")!;

            var itemProp = moType.GetProperty("Item", [typeof(String)])!;
            var getValueMethod = itemProp.GetGetMethod()!;

            while ((Boolean)moveNextMethod.Invoke(enumerator, null)!)
            {
                var obj = currentProperty.GetValue(enumerator);
                if (obj == null) continue;

                var val = getValueMethod.Invoke(obj, [property]);
                if (val != null)
                {
                    var v = val.ToString()?.TrimInvisible()?.Trim();
                    if (!v.IsNullOrEmpty()) bbs.Add(v);
                }
            }

            // 清理 COM 引用
            if (results is IDisposable d1) d1.Dispose();
            if (searcher is IDisposable d2) d2.Dispose();

            if (bbs.Count > 0)
            {
                bbs.Sort();
                return bbs.Distinct().Join();
            }
        }
        catch (Exception ex)
        {
            if (throwOnError) throw;
            if (XTrace.Log.Level <= LogLevel.Debug)
            {
                XTrace.WriteLine("QueryWmiSystemManagement 失败: {0}.{1} @ {2}", wmiClass, property, nameSpace);
                XTrace.WriteException(ex is TargetInvocationException tie ? tie.InnerException! : ex);
            }
        }

        return null;
    }

    /// <summary>通过 COM 查询 WMI 属性。使用 Windows 内置 winmgmts 组件，不新增依赖，不开进程</summary>
    /// <remarks>
    /// 通过 winmgmts moniker 绑定 SWbemServices，执行 WQL 查询并枚举结果，SWbemObject 按名暴露 WMI 属性。
    /// 相比 System.Management 无需该程序集，适用于 netstandard/net*非windows 运行时下的 Windows 主机。
    /// COM 查询在部分环境可能失败，此时调用方应回退 wmic。
    /// </remarks>
    /// <param name="wmiClass">WMI 类名，如 Win32_OperatingSystem</param>
    /// <param name="property">属性名</param>
    /// <param name="nameSpace">命名空间，如 root\cimv2</param>
    /// <param name="throwOnError">失败时是否抛出异常，默认false吞掉异常返回null</param>
    /// <returns>查询结果，失败返回null</returns>
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
#endif
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "运行时类型反射/动态加载边界；AOT 下不受支持或需改用泛型入口（见分析稿 §4/§5）")]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "经 WMI moniker 动态绑定 COM 服务；AOT 下不受支持并降级为空值（见分析稿 §5）")]
    internal static String? QueryWmiCom(String wmiClass, String property, String nameSpace, Boolean throwOnError = false)
    {
        Object? services = null;
        Object? results = null;
        try
        {
            // 通过 winmgmts moniker 绑定 WMI 服务，规避 SWbemLocator.ConnectServer 的 IDispatch 参数编组问题
            var moniker = $"winmgmts:\\\\.\\{nameSpace}";
            services = Marshal.BindToMoniker(moniker);
            if (services == null) return null;

            // ExecQuery 执行 WQL 查询，返回 SWbemObjectSet
            var wql = $"SELECT {property} FROM {wmiClass}";
            results = services.GetType().InvokeMember("ExecQuery", BindingFlags.InvokeMethod, null, services, new Object[] { wql });
            if (results == null) return null;

            // SWbemObjectSet 实现 IEnumVARIANT，可直接 foreach；SWbemObject 通过 IDispatch 按名暴露 WMI 属性
            var bbs = new List<String>();
            foreach (var obj in (System.Collections.IEnumerable)results)
            {
                if (obj == null) continue;
                try
                {
                    var val = obj.GetType().InvokeMember(property, BindingFlags.GetProperty, null, obj, null);
                    if (val != null)
                    {
                        var v = val.ToString()?.TrimInvisible()?.Trim();
                        if (!v.IsNullOrEmpty()) bbs.Add(v);
                    }
                }
                finally { try { Marshal.FinalReleaseComObject(obj); } catch { } }
            }

            if (bbs.Count > 0)
            {
                bbs.Sort();
                return bbs.Distinct().Join();
            }
        }
        catch (Exception ex)
        {
            if (throwOnError) throw;
            if (XTrace.Log.Level <= LogLevel.Debug)
            {
                XTrace.WriteLine("QueryWmiCom 失败: {0}.{1} @ {2}", wmiClass, property, nameSpace);
                XTrace.WriteException(ex is TargetInvocationException tie ? tie.InnerException! : ex);
            }
        }
        finally
        {
            if (results != null) try { Marshal.FinalReleaseComObject(results); } catch { }
            if (services != null) try { Marshal.FinalReleaseComObject(services); } catch { }
        }

        return null;
    }
    #endregion
#endif
}
