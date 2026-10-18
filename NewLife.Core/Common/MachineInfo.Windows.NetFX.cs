#if NETFRAMEWORK
using System.Management;
using Microsoft.VisualBasic.Devices;
using Microsoft.Win32;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Windows;

namespace NewLife;

/// <summary>机器信息 Windows NETFRAMEWORK 兼容部分。覆盖 Windows.cs 中的同名方法</summary>
public partial class MachineInfo
{
    /// <summary>加载 Windows 静态信息（NETFRAMEWORK 路径）</summary>
    private void LoadWindowsInfo()
    {
        // 从注册表读取硬件信息
        LoadHardwareFromRegistry();

        // 获取内存大小
        {
            var ci = new ComputerInfo();
            Memory = ci.TotalPhysicalMemory;
        }

        // 获取操作系统名称和版本
        try
        {
            var ci = new ComputerInfo();

            // 系统名取WMI可能出错
            OSName = ci.OSFullName?.Replace("®", null).TrimPrefix("Microsoft").Trim();
            OSVersion = ci.OSVersion;
        }
        catch
        {
            try
            {
                var reg2 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                if (reg2 != null)
                {
                    OSName = reg2.GetValue("ProductName") + "";
                    OSVersion = reg2.GetValue("ReleaseId") + "";
                }
            }
            catch (Exception ex)
            {
                if (XTrace.Log.Level <= LogLevel.Debug) XTrace.WriteException(ex);
            }
        }

        // 磁盘和BIOS序列号
        //Processor = GetInfo("Win32_Processor", "Name");
        //CpuID = GetInfo("Win32_Processor", "ProcessorId");
        //var uuid = GetInfo("Win32_ComputerSystemProduct", "UUID");
        //Product = GetInfo("Win32_ComputerSystemProduct", "Name");
        DiskID = GetInfo("Win32_DiskDrive where mediatype=\"Fixed hard disk media\"", "SerialNumber");

        var sn = GetInfo("Win32_BIOS", "SerialNumber");
        if (!sn.IsNullOrEmpty() && !sn.EqualIgnoreCase("System Serial Number")) Serial = sn;
        Board = GetInfo("Win32_BaseBoard", "SerialNumber");
    }

    /// <summary>刷新 Windows 动态数据（NETFRAMEWORK 路径）</summary>
    private void RefreshWindows()
    {
        RefreshMemoryAndCpu();

        var power = new PowerStatus();

        if (!_excludes.Contains(nameof(Temperature)))
        {
            // 读取主板温度，不太准。标准方案是ring0通过IOPort读取CPU温度，太难在基础类库实现
            var str = GetInfo("Win32_TemperatureProbe", "CurrentReading");
            if (!str.IsNullOrEmpty())
            {
                Temperature = str.SplitAsInt().Average();
            }
            else
            {
                str = GetInfo("MSAcpi_ThermalZoneTemperature", "CurrentTemperature", "root/wmi");
                if (!str.IsNullOrEmpty())
                    Temperature = (str.SplitAsInt().Average() - 2732) / 10.0;
                else
                {
                    if (XTrace.Log.Level <= LogLevel.Debug) XTrace.WriteLine("Temperature information is unavailable.");
                    _excludes.Add(nameof(Temperature));
                    Temperature = 0;
                }
            }
        }

        if (power.BatteryLifePercent > 0)
            Battery = power.BatteryLifePercent;
        else if (!_excludes.Contains(nameof(Battery)))
        {
            // 电池剩余
            var str = GetInfo("Win32_Battery", "EstimatedChargeRemaining");
            if (!str.IsNullOrEmpty())
                Battery = str.SplitAsInt().Average() / 100.0;
            else
            {
                if (XTrace.Log.Level <= LogLevel.Debug) XTrace.WriteLine("Battery information is unavailable.");
                _excludes.Add(nameof(Battery));
                Battery = 0;
            }
        }
    }

    /// <summary>获取WMI信息（NETFRAMEWORK 路径，直接使用 ManagementObjectSearcher）</summary>
    /// <param name="path">WMI路径</param>
    /// <param name="property">属性名</param>
    /// <param name="nameSpace">命名空间，默认 root\cimv2</param>
    /// <returns>查询结果</returns>
    public static String GetInfo(String path, String property, String? nameSpace = null)
    {
        // 规范化命名空间：WMI 要求反斜杠分隔，调用者可能传入 root/wmi 等正斜杠格式
        if (nameSpace != null) nameSpace = nameSpace.Replace("/", "\\");

        // Linux Mono不支持WMI
        if (Runtime.Mono) return "";

        var bbs = new List<String>();
        try
        {
            var wql = $"Select {property} From {path}";
            var cimobject = new ManagementObjectSearcher(nameSpace, wql);
            var moc = cimobject.Get();
            foreach (var mo in moc)
            {
                var val = mo?.Properties?[property]?.Value;
                if (val != null)
                {
                    var v = val.ToString().TrimInvisible()?.Trim();
                    if (v != null) bbs.Add(v);
                }
            }
        }
        catch (Exception ex)
        {
            if (XTrace.Log.Level <= LogLevel.Debug) XTrace.WriteLine("WMI.GetInfo({0}) failed: {1}", path, ex.Message);
            return "";
        }

        bbs.Sort();

        return bbs.Distinct().Join();
    }
}
#endif
