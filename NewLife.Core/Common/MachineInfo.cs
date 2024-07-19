using System.ComponentModel;
using System.Diagnostics;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.VisualBasic.Devices;
using Microsoft.Win32;
using NewLife.Log;
using NewLife.Model;
using NewLife.Serialization;
using NewLife.Windows;
using NewLife.Data;

namespace NewLife;

/// <summary>机器信息接口</summary>
/// <remarks>用于扩展MachineInfo功能，具体应用自定义各字段获取方式</remarks>
public interface IMachineInfo
{
    /// <summary>初始化静态数据</summary>
    void Init(MachineInfo info);

    /// <summary>刷新动态数据</summary>
    void Refresh(MachineInfo info);
}

/// <summary>机器信息</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/machine_info
/// 
/// 刷新信息成本较高，建议采用单例模式
/// </remarks>
public class MachineInfo : IExtend
{
    #region 属性
    /// <summary>系统名称</summary>
    [DisplayName("系统名称")]
    public String OSName { get; set; }

    /// <summary>系统版本</summary>
    [DisplayName("系统版本")]
    public String OSVersion { get; set; }

    /// <summary>产品名称</summary>
    [DisplayName("产品名称")]
    public String Product { get; set; }

    /// <summary>制造商</summary>
    [DisplayName("制造商")]
    public String Vendor { get; set; }

    /// <summary>处理器型号</summary>
    [DisplayName("处理器型号")]
    public String Processor { get; set; }

    ///// <summary>处理器序列号</summary>
    //public String CpuID { get; set; }

    /// <summary>硬件唯一标识。取主板编码，部分品牌存在重复</summary>
    [DisplayName("硬件唯一标识")]
    public String UUID { get; set; }

    /// <summary>软件唯一标识。系统标识，操作系统重装后更新，Linux系统的machine_id，Android的android_id，Ghost系统存在重复</summary>
    [DisplayName("软件唯一标识")]
    public String Guid { get; set; }

    /// <summary>计算机序列号。适用于品牌机，跟笔记本标签显示一致</summary>
    [DisplayName("计算机序列号")]
    public String Serial { get; set; }

    /// <summary>主板。序列号或家族信息</summary>
    [DisplayName("主板")]
    public String Board { get; set; }

    /// <summary>磁盘序列号</summary>
    [DisplayName("磁盘序列号")]
    public String DiskID { get; set; }

    /// <summary>内存总量。单位Byte</summary>
    [DisplayName("内存总量")]
    public UInt64 Memory { get; set; }

    /// <summary>可用内存。单位Byte</summary>
    [DisplayName("可用内存")]
    public UInt64 AvailableMemory { get; set; }

    /// <summary>CPU占用率</summary>
    [DisplayName("CPU占用率")]
    public Double CpuRate { get; set; }

    /// <summary>网络上行速度。字节每秒，初始化后首次读取为0</summary>
    [DisplayName("网络上行速度")]
    public UInt64 UplinkSpeed { get; set; }

    /// <summary>网络下行速度。字节每秒，初始化后首次读取为0</summary>
    [DisplayName("网络下行速度")]
    public UInt64 DownlinkSpeed { get; set; }

    /// <summary>温度。单位度</summary>
    [DisplayName("温度")]
    public Double Temperature { get; set; }

    /// <summary>电池剩余。小于1的小数，常用百分比表示</summary>
    [DisplayName("电池剩余")]
    public Double Battery { get; set; }

    private readonly Dictionary<String, Object> _items = [];
    IDictionary<String, Object> IExtend.Items => _items;

    /// <summary>获取 或 设置 扩展属性数据</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public Object this[String key] { get => _items.TryGetValue(key, out var obj) ? obj : null; set => _items[key] = value; }
    #endregion

    #region 全局静态
    /// <summary>当前机器信息。默认null，在RegisterAsync后才能使用</summary>
    public static MachineInfo Current { get; set; }

    /// <summary>机器信息提供者。外部实现可修改部分行为</summary>
    public static IMachineInfo Provider { get; set; }

    //static MachineInfo() => RegisterAsync();

    private static Task<MachineInfo> _task;
    /// <summary>异步注册一个初始化后的机器信息实例</summary>
    /// <returns></returns>
    public static Task<MachineInfo> RegisterAsync()
    {
        if (_task != null) return _task;

        return _task = Task.Factory.StartNew(() =>
        {
            var set = Setting.Current;
            var dataPath = set.DataPath;
            if (dataPath.IsNullOrEmpty()) dataPath = "Data";

            // 文件缓存，加快机器信息获取
            var file = Path.GetTempPath().CombinePath("machine_info.json");
            var file2 = dataPath.CombinePath("machine_info.json").GetBasePath();
            var json = "";
            if (Current == null)
            {
                var f = file;
                if (!File.Exists(f)) f = file2;
                if (File.Exists(f))
                {
                    try
                    {
                        //XTrace.WriteLine("Load MachineInfo {0}", f);
                        json = File.ReadAllText(f);
                        Current = json.ToJsonEntity<MachineInfo>();
                    }
                    catch (Exception ex)
                    {
                        if (XTrace.Log.Level <= LogLevel.Debug) XTrace.WriteException(ex);
                    }
                }
            }

            var mi = Current ?? new MachineInfo();

            mi.Init();
            Current = mi;

            //// 定时刷新
            //if (msRefresh > 0) mi._timer = new TimerX(s => mi.Refresh(), null, msRefresh, msRefresh) { Async = true };

            // 注册到对象容器
            ObjectContainer.Current.AddSingleton(mi);

            try
            {
                var json2 = mi.ToJson(true);
                if (json != json2)
                {
                    File.WriteAllText(file2.EnsureDirectory(true), json2);
                    File.WriteAllText(file.EnsureDirectory(true), json2);
                }
            }
            catch (Exception ex)
            {
                if (XTrace.Log.Level <= LogLevel.Debug) XTrace.WriteException(ex);
            }

            return mi;
        });
    }

    /// <summary>获取当前信息，如果未设置则等待异步注册结果</summary>
    /// <returns></returns>
    public static MachineInfo GetCurrent() => Current ?? RegisterAsync().Result;

    /// <summary>从对象容器中获取一个已注册机器信息实例</summary>
    /// <returns></returns>
    public static MachineInfo Resolve() => ObjectContainer.Current.Resolve<MachineInfo>();
    #endregion

    #region 方法
    /// <summary>初始化静态数据。可能是实例化后执行，也可能是Json反序列化后执行</summary>
    public void Init()
    {
        var osv = Environment.OSVersion;
        if (OSVersion.IsNullOrEmpty()) OSVersion = osv.Version + "";
        if (OSName.IsNullOrEmpty()) OSName = (osv + "").TrimStart("Microsoft").TrimEnd(OSVersion).Trim();
        if (Guid.IsNullOrEmpty()) Guid = "";

        try
        {
            //if (Runtime.Windows)
            LoadWindowsInfo();

            Provider?.Init(this);
        }
        catch (Exception ex)
        {
            if (XTrace.Log.Level <= LogLevel.Debug) XTrace.WriteException(ex);
        }

        // 裁剪不可见字符，顺带去掉两头空白
        OSName = OSName.TrimInvisible()?.Trim();
        OSVersion = OSVersion.TrimInvisible()?.Trim();
        Product = Product.TrimInvisible()?.Trim();
        Processor = Processor.TrimInvisible()?.Trim();
        UUID = UUID.TrimInvisible()?.Trim();
        Guid = Guid.TrimInvisible()?.Trim();
        Serial = Serial.TrimInvisible()?.Trim();
        Board = Board.TrimInvisible()?.Trim();
        DiskID = DiskID.TrimInvisible()?.Trim();

        // 无法读取系统标识时，随机生成一个guid，借助文件缓存确保其不变
        if (Guid.IsNullOrEmpty()) Guid = "0-" + System.Guid.NewGuid().ToString();
        if (UUID.IsNullOrEmpty()) UUID = "0-" + System.Guid.NewGuid().ToString();

        try
        {
            Refresh();
        }
        catch (Exception ex)
        {
            if (XTrace.Log.Level <= LogLevel.Debug) XTrace.WriteException(ex);
        }
    }

    private void LoadWindowsInfo()
    {
        var str = "";

        var reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
        if (reg != null) str = reg.GetValue("MachineGuid") + "";

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

        // 旧版系统（如win2008）没有UUID的注册表项，需要用wmic查询。也可能因为过去的某个BUG，导致GUID跟UUID相等
        if (UUID.IsNullOrEmpty() || UUID == Guid || Vendor.IsNullOrEmpty())
        {
            var csproduct = ReadWmic("csproduct", "Name", "UUID", "Vendor");
            if (csproduct != null)
            {
                if (csproduct.TryGetValue("Name", out str) && !str.IsNullOrEmpty() && Product.IsNullOrEmpty()) Product = str;
                if (csproduct.TryGetValue("UUID", out str) && !str.IsNullOrEmpty()) UUID = str;
                if (csproduct.TryGetValue("Vendor", out str) && !str.IsNullOrEmpty()) Vendor = str;
            }
        }
        var ci = new ComputerInfo();
        try
        {
            Memory = ci.TotalPhysicalMemory;

            // 系统名取WMI可能出错
            OSName = ci.OSFullName?.Replace("®", null).TrimStart("Microsoft").Trim();
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

        //Processor = GetInfo("Win32_Processor", "Name");
        //CpuID = GetInfo("Win32_Processor", "ProcessorId");
        //var uuid = GetInfo("Win32_ComputerSystemProduct", "UUID");
        //Product = GetInfo("Win32_ComputerSystemProduct", "Name");
        DiskID = GetInfo("Win32_DiskDrive where mediatype=\"Fixed hard disk media\"", "SerialNumber");

        var sn = GetInfo("Win32_BIOS", "SerialNumber");
        if (!sn.IsNullOrEmpty() && !sn.EqualIgnoreCase("System Serial Number")) Serial = sn;
        Board = GetInfo("Win32_BaseBoard", "SerialNumber");
    }

    private readonly ICollection<String> _excludes = [];

    /// <summary>获取实时数据，如CPU、内存、温度</summary>
    public void Refresh()
    {
        if (Runtime.Windows)
            RefreshWindows();

        RefreshSpeed();

        Provider?.Refresh(this);
    }

    private void RefreshWindows()
    {
        MEMORYSTATUSEX ms = default;
        ms.Init();
        if (GlobalMemoryStatusEx(ref ms))
        {
            Memory = ms.ullTotalPhys;
            AvailableMemory = ms.ullAvailPhys;
        }

        if (Runtime.Windows)
        {
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
                        if (XTrace.Log.Level <= LogLevel.Debug) XTrace.WriteLine("Temperature信息无法读取");
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
                    if (XTrace.Log.Level <= LogLevel.Debug) XTrace.WriteLine("Battery信息无法读取");
                    _excludes.Add(nameof(Battery));
                    Battery = 0;
                }
            }
        }
    }

    private Int64 _lastTime;
    private Int64 _lastSent;
    private Int64 _lastReceived;
    /// <summary>刷新网络速度</summary>
    public void RefreshSpeed()
    {
        var sent = 0L;
        var received = 0L;
        try
        {
            // 包含本地环回和隧道网卡
            // WSL获取网络列表时可能报错
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                try
                {
                    var st = ni.GetIPv4Statistics();
                    sent += st.BytesSent;
                    received += st.BytesReceived;
                }
                catch { }
            }
        }
        catch { }

        var now = Runtime.TickCount64;
        if (_lastTime > 0)
        {
            var interval = now - _lastTime;
            if (interval > 0)
            {
                var s1 = (sent - _lastSent) * 1000 / interval;
                var s2 = (received - _lastReceived) * 1000 / interval;
                if (s1 >= 0) UplinkSpeed = (UInt64)s1;
                if (s2 >= 0) DownlinkSpeed = (UInt64)s2;
            }
        }

        _lastSent = sent;
        _lastReceived = received;
        _lastTime = now;
    }
    #endregion

    #region 辅助
    private static String Execute(String cmd, String arguments = null)
    {
        try
        {
#if DEBUG
            if (XTrace.Log.Level <= LogLevel.Debug) XTrace.WriteLine("Execute({0} {1})", cmd, arguments);
#endif

            var psi = new ProcessStartInfo(cmd, arguments)
            {
                // UseShellExecute 必须 false，以便于后续重定向输出流
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };
            var process = Process.Start(psi);
            if (!process.WaitForExit(3_000))
            {
                process.Kill();
                return null;
            }

            return process.StandardOutput.ReadToEnd();
        }
        catch { return null; }
    }

    /// <summary>通过WMIC命令读取信息</summary>
    /// <param name="type"></param>
    /// <param name="keys"></param>
    /// <returns></returns>
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
    #endregion

    #region 内存
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

        internal void Init() => dwLength = checked((UInt32)Marshal.SizeOf(typeof(MEMORYSTATUSEX)));
    }
    #endregion

    #region 磁盘
    /// <summary>获取指定目录所在盘可用空间，默认当前目录</summary>
    /// <param name="path"></param>
    /// <returns>返回可用空间，字节，获取失败返回-1</returns>
    public static Int64 GetFreeSpace(String path = null)
    {
        if (path.IsNullOrEmpty()) path = ".";

        var driveInfo = new DriveInfo(Path.GetPathRoot(path.GetFullPath()));
        if (driveInfo == null || !driveInfo.IsReady) return -1;

        try
        {
            return driveInfo.AvailableFreeSpace;
        }
        catch { return -1; }
    }

    /// <summary>获取指定目录下文件名，支持去掉后缀的去重，主要用于Linux</summary>
    /// <param name="path"></param>
    /// <param name="trimSuffix"></param>
    /// <returns></returns>
    public static ICollection<String> GetFiles(String path, Boolean trimSuffix = false)
    {
        var list = new List<String>();
        if (path.IsNullOrEmpty()) return list;

        var di = path.AsDirectory();
        if (!di.Exists) return list;

        var list2 = di.GetFiles().Select(e => e.Name).ToList();
        foreach (var item in list2)
        {
            var line = item?.Trim();
            if (!line.IsNullOrEmpty())
            {
                if (trimSuffix)
                {
                    if (!list2.Any(e => e != line && line.StartsWith(e))) list.Add(line);
                }
                else
                {
                    list.Add(line);
                }
            }
        }

        return list;
    }
    #endregion

    #region Windows辅助
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern Boolean GetSystemTimes(out FILETIME idleTime, out FILETIME kernelTime, out FILETIME userTime);

    private struct FILETIME
    {
        public UInt32 Low;

        public UInt32 High;

        public FILETIME(Int64 time)
        {
            Low = (UInt32)time;
            High = (UInt32)(time >> 32);
        }

        public Int64 ToLong() => (Int64)(((UInt64)High << 32) | Low);
    }

    private class SystemTime
    {
        public Int64 IdleTime;
        public Int64 TotalTime;
    }

    private SystemTime _systemTime;

    /// <summary>获取WMI信息</summary>
    /// <param name="path"></param>
    /// <param name="property"></param>
    /// <param name="nameSpace"></param>
    /// <returns></returns>
    public static String GetInfo(String path, String property, String nameSpace = null)
    {
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
                if (val != null) bbs.Add(val.ToString().TrimInvisible().Trim());
            }
        }
        catch (Exception ex)
        {
            if (XTrace.Log.Level <= LogLevel.Debug) XTrace.WriteLine("WMI.GetInfo({0})失败！{1}", path, ex.Message);
            return "";
        }

        bbs.Sort();

        return bbs.Distinct().Join();
    }
    #endregion
}