using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading.Tasks;
using NewLife.Collections;
using NewLife.Log;
using NewLife.Model;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife
{
    /// <summary>机器信息</summary>
    /// <remarks>
    /// 文档 https://newlifex.com/core/machine_info
    /// 
    /// 刷新信息成本较高，建议采用单例模式
    /// </remarks>
    public class MachineInfo
    {
        #region 属性
        /// <summary>系统名称</summary>
        public String OSName { get; set; }

        /// <summary>系统版本</summary>
        public String OSVersion { get; set; }

        /// <summary>产品名称。制造商</summary>
        public String Product { get; set; }

        /// <summary>处理器型号</summary>
        public String Processor { get; set; }

        /// <summary>处理器序列号。PC处理器序列号绝大部分重复，实际存储处理器的其它信息</summary>
        public String CpuID { get; set; }

        /// <summary>硬件唯一标识。取主板编码，部分品牌存在重复</summary>
        public String UUID { get; set; }

        /// <summary>系统标识。操作系统重装后更新，Linux系统的machine_id，Android的android_id，Ghost系统存在重复</summary>
        public String Guid { get; set; }

        /// <summary>磁盘序列号</summary>
        public String DiskID { get; set; }

        /// <summary>内存总量</summary>
        public UInt64 Memory { get; set; }

        /// <summary>可用内存</summary>
        public UInt64 AvailableMemory { get; private set; }

        /// <summary>CPU占用率</summary>
        public Single CpuRate { get; private set; }

        /// <summary>网络上行速度。字节每秒，初始化后首次读取为0</summary>
        public UInt64 UplinkSpeed { get; set; }

        /// <summary>网络下行速度。字节每秒，初始化后首次读取为0</summary>
        public UInt64 DownlinkSpeed { get; set; }

        /// <summary>温度</summary>
        public Double Temperature { get; set; }

        /// <summary>电池剩余</summary>
        public Double Battery { get; set; }
        #endregion

        #region 构造
        /// <summary>当前机器信息。默认null，在RegisterAsync后才能使用</summary>
        public static MachineInfo Current { get; set; }

        static MachineInfo() => RegisterAsync();

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
                if (Current == null)
                {
                    var f = file;
                    if (!File.Exists(f)) f = file2;
                    if (File.Exists(f))
                    {
                        try
                        {
                            //XTrace.WriteLine("Load MachineInfo {0}", f);
                            Current = File.ReadAllText(f).ToJsonEntity<MachineInfo>();
                        }
                        catch { }
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
                    var json = mi.ToJson(true);
                    File.WriteAllText(file.EnsureDirectory(true), json);
                    File.WriteAllText(file2.EnsureDirectory(true), json);
                }
                catch { }

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
        /// <summary>刷新</summary>
        public void Init()
        {
            var osv = Environment.OSVersion;
            if (OSVersion.IsNullOrEmpty()) OSVersion = osv.Version + "";
            if (OSName.IsNullOrEmpty()) OSName = (osv + "").TrimStart("Microsoft").TrimEnd(OSVersion).Trim();
            if (Guid.IsNullOrEmpty()) Guid = "";

            try
            {
                if (Runtime.Windows)
                    LoadWindowsInfo();
                else if (Runtime.Linux)
                    LoadLinuxInfo();
            }
            catch (Exception ex)
            {
                if (XTrace.Log.Level <= LogLevel.Debug) XTrace.WriteException(ex);
            }

            // window+netcore 不方便读取注册表，随机生成一个guid，借助文件缓存确保其不变
            if (Guid.IsNullOrEmpty()) Guid = "0-" + System.Guid.NewGuid().ToString();
            if (UUID.IsNullOrEmpty()) UUID = "0-" + System.Guid.NewGuid().ToString();

            try
            {
                Refresh();
            }
#if DEBUG
            catch (Exception ex) { XTrace.WriteException(ex); }
#else
            catch { }
#endif
        }

        private void LoadWindowsInfo()
        {
            var str = "";

            var os = ReadWmic("os", "Caption", "Version");
            if (os != null)
            {
                if (os.TryGetValue("Caption", out str)) OSName = str.TrimStart("Microsoft").Trim();
                if (os.TryGetValue("Version", out str)) OSVersion = str;
            }

            var csproduct = ReadWmic("csproduct", "Name", "UUID");
            if (csproduct != null)
            {
                if (csproduct.TryGetValue("Name", out str)) Product = str;
                if (csproduct.TryGetValue("UUID", out str)) UUID = str;
            }

            var disk = ReadWmic("diskdrive", "serialnumber");
            if (disk != null)
            {
                if (disk.TryGetValue("serialnumber", out str)) DiskID = str?.Trim();
            }

            // 不要在刷新里面取CPU负载，因为运行wmic会导致CPU负载很不准确，影响测量
            var cpu = ReadWmic("cpu", "Name", "ProcessorId", "LoadPercentage");
            if (cpu != null)
            {
                if (cpu.TryGetValue("Name", out str)) Processor = str;
                if (cpu.TryGetValue("ProcessorId", out str)) CpuID = str;
                if (cpu.TryGetValue("LoadPercentage", out str)) CpuRate = (Single)(str.ToDouble() / 100);
            }

            // 从注册表读取 MachineGuid
            str = Execute("reg", @"query HKLM\SOFTWARE\Microsoft\Cryptography /v MachineGuid");
            if (!str.IsNullOrEmpty() && str.Contains("REG_SZ")) Guid = str.Substring("REG_SZ", null).Trim();
        }

        private void LoadLinuxInfo()
        {
            var str = GetLinuxName();
            if (!str.IsNullOrEmpty()) OSName = str;

            var device = ReadDeviceInfo();
            var js = device.ToJson(true);

            if (device.TryGetValue("Platform", out str))
                OSName = str;
            if (device.TryGetValue("Version", out str))
                OSVersion = str;

            // 树莓派的Hardware无法区分P0/P4
            var dic = ReadInfo("/proc/cpuinfo");
            if (dic != null)
            {
                if (dic.TryGetValue("Hardware", out str) ||
                    dic.TryGetValue("cpu model", out str) ||
                    dic.TryGetValue("model name", out str))
                    Processor = str?.TrimStart("vendor ");

                if (device.TryGetValue("Product", out str))
                    Product = str;
                else if (dic.TryGetValue("vendor_id", out str))
                    Product = str;
                else if (dic.TryGetValue("Model", out str))
                    Product = str;

                if (device.TryGetValue("Fingerprint", out str) && !str.IsNullOrEmpty())
                    CpuID = str;
                if (dic.TryGetValue("Serial", out str))
                    CpuID = str;
            }

            var mid = "/etc/machine-id";
            if (!File.Exists(mid)) mid = "/var/lib/dbus/machine-id";
            if (TryRead(mid, out var value))
                Guid = value;
            else if (device.TryGetValue("android_id", out str) && !str.IsNullOrEmpty() && str != "unknown")
                Guid = str;
            //else if (android.TryGetValue("Id", out str))
            //    Guid = str;

            var file = "/sys/class/dmi/id/product_uuid";
            if (!File.Exists(file)) file = "/proc/serial_num";  // miui12支持/proc/serial_num
            if (TryRead(file, out value))
                UUID = value;
            else if (device.TryGetValue("Serial", out str) && str != "unknown")
                UUID = str;

            file = "/sys/class/dmi/id/product_name";
            if (TryRead(file, out value)) Product = value;

            var disks = GetFiles("/dev/disk/by-id", true);
            if (disks.Count == 0) disks = GetFiles("/dev/disk/by-uuid", false);
            if (disks.Count > 0) DiskID = disks.Join(",");

            var dmi = Execute("dmidecode")?.SplitAsDictionary(":", "\n");
            if (dmi != null)
            {
                if (dmi.TryGetValue("ID", out str)) CpuID = str.Replace(" ", null);
                if (dmi.TryGetValue("UUID", out str)) UUID = str;
                if (dmi.TryGetValue("Product Name", out str)) Product = str;
                //if (TryFind(dmi, new[] { "Serial Number" }, out str)) Guid = str;
            }

            // 从release文件读取产品
            var prd = GetProductByRelease();
            if (!prd.IsNullOrEmpty()) Product = prd;

            //// 电池剩余
            //if (TryRead("/sys/class/power_supply/BAT0/energy_now", out var energy_now) &&
            //    TryRead("/sys/class/power_supply/BAT0/energy_full", out var energy_full))
            //{
            //    Battery = energy_now.ToDouble() / energy_full.ToDouble();
            //}
            //else if (TryRead("/sys/class/power_supply/battery/capacity", out var capacity))
            //{
            //    Battery = capacity.ToDouble() / 100.0;
            //}
        }

        private readonly ICollection<String> _excludes = new List<String>();

        /// <summary>获取实时数据，如CPU、内存、温度</summary>
        public void Refresh()
        {
            if (Runtime.Windows)
            {
                MEMORYSTATUSEX ms = default;
                ms.Init();
                if (GlobalMemoryStatusEx(ref ms))
                {
                    Memory = ms.ullTotalPhys;
                    AvailableMemory = ms.ullAvailPhys;
                }
            }
            // 特别识别Linux发行版
            else if (Runtime.Linux)
            {
                var dic = ReadInfo("/proc/meminfo");
                if (dic != null)
                {
                    if (dic.TryGetValue("MemTotal", out var str))
                        Memory = (UInt64)str.TrimEnd(" kB").ToInt() * 1024;

                    if (dic.TryGetValue("MemAvailable", out str))
                        AvailableMemory = (UInt64)str.TrimEnd(" kB").ToInt() * 1024;
                    else if (dic.TryGetValue("MemFree", out str))
                        AvailableMemory = (UInt64)(str.TrimEnd(" kB").ToInt() + dic["Buffers"].TrimEnd(" kB").ToInt() + dic["Cached"].TrimEnd(" kB").ToInt()) * 1024;
                }

                // respberrypi + fedora
                if (TryRead("/sys/class/thermal/thermal_zone0/temp", out var value) ||
                    TryRead("/sys/class/hwmon/hwmon0/temp1_input", out value) ||
                    TryRead("/sys/class/hwmon/hwmon0/temp2_input", out value) ||
                    TryRead("/sys/class/hwmon/hwmon0/device/hwmon/hwmon0/temp2_input", out value) ||
                    TryRead("/sys/devices/virtual/thermal/thermal_zone0/temp", out value))
                    Temperature = value.ToDouble() / 1000;
                // A2温度获取，Ubuntu 16.04 LTS， Linux 3.4.39
                else if (TryRead("/sys/class/hwmon/hwmon0/device/temp_value", out value))
                    Temperature = value.Substring(null, ":").ToDouble();

                // 电池剩余
                if (TryRead("/sys/class/power_supply/BAT0/energy_now", out var energy_now) &&
                    TryRead("/sys/class/power_supply/BAT0/energy_full", out var energy_full))
                {
                    Battery = energy_now.ToDouble() / energy_full.ToDouble();
                }
                else if (TryRead("/sys/class/power_supply/battery/capacity", out var capacity))
                {
                    Battery = capacity.ToDouble() / 100.0;
                }
                else if (Runtime.Mono)
                {
                    var battery = ReadDeviceBattery();
                    if (battery.TryGetValue("ChargeLevel", out var obj)) Battery = obj.ToDouble();
                }

                //var upt = Execute("uptime");
                //if (!upt.IsNullOrEmpty())
                //{
                //    str = upt.Substring("load average:");
                //    if (!str.IsNullOrEmpty()) CpuRate = (Single)str.Split(",")[0].ToDouble();
                //}

                //file = "/proc/loadavg";
                //if (File.Exists(file)) CpuRate = (Single)File.ReadAllText(file).Substring(null, " ").ToDouble() / Environment.ProcessorCount;

                var file = "/proc/stat";
                if (!_excludes.Contains(nameof(CpuRate)) && File.Exists(file))
                {
                    // CPU指标：user，nice, system, idle, iowait, irq, softirq
                    // cpu  57057 0 14420 1554816 0 443 0 0 0 0
                    try
                    {
                        using var reader = new StreamReader(file);
                        var line = reader.ReadLine();
                        if (!line.IsNullOrEmpty() && line.StartsWith("cpu"))
                        {
                            var vs = line.TrimStart("cpu").Trim().Split(' ');
                            var current = new SystemTime
                            {
                                IdleTime = vs[3].ToLong(),
                                TotalTime = vs.Take(7).Select(e => e.ToLong()).Sum().ToLong(),
                            };

                            var idle = current.IdleTime - (_systemTime?.IdleTime ?? 0);
                            var total = current.TotalTime - (_systemTime?.TotalTime ?? 0);
                            _systemTime = current;

                            CpuRate = total == 0 ? 0 : ((Single)(total - idle) / total);
                        }
                    }
                    catch { _excludes.Add(nameof(_excludes)); }
                }
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

                CpuRate = total == 0 ? 0 : ((Single)(total - idle) / total);

                if (!_excludes.Contains(nameof(Temperature)))
                {
                    var temp = ReadWmic(@"/namespace:\\root\wmi path MSAcpi_ThermalZoneTemperature", "CurrentTemperature");
                    if (temp != null && temp.Count > 0)
                    {
                        if (temp.TryGetValue("CurrentTemperature", out var str)) Temperature = (str.SplitAsInt().Average() - 2732) / 10.0;
                    }
                    else
                        _excludes.Add(nameof(Temperature));
                }

                if (!_excludes.Contains(nameof(Battery)))
                {
                    var battery = ReadWmic("path win32_battery", "EstimatedChargeRemaining");
                    if (battery != null && battery.Count > 0)
                    {
                        if (battery.TryGetValue("EstimatedChargeRemaining", out var str)) Battery = str.SplitAsInt().Average() / 100.0;
                    }
                    else
                        _excludes.Add(nameof(Battery));
                }
            }

            RefreshSpeed();
        }

        private Int64 _lastTime;
        private Int64 _lastSent;
        private Int64 _lastReceived;
        /// <summary>刷新网络速度</summary>
        public void RefreshSpeed()
        {
            var sent = 0L;
            var received = 0L;
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                try
                {
                    var st = ni.GetIPStatistics();
                    sent += st.BytesSent;
                    received += st.BytesReceived;
                }
                catch { }
            }

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
        /// <summary>获取Linux发行版名称</summary>
        /// <returns></returns>
        public static String GetLinuxName()
        {
            var fr = "/etc/redhat-release";
            if (TryRead(fr, out var value)) return value;

            var dr = "/etc/debian-release";
            if (TryRead(dr, out value)) return value;

            var sr = "/etc/os-release";
            if (TryRead(sr, out value)) return value?.SplitAsDictionary("=", "\n", true)["PRETTY_NAME"].Trim();

            var uname = Execute("uname", "-sr")?.Trim();
            if (!uname.IsNullOrEmpty())
            {
                // 支持Android系统名
                var ss = uname.Split('-');
                foreach (var item in ss)
                {
                    if (!item.IsNullOrEmpty() && item.StartsWithIgnoreCase("Android")) return item;
                }

                return uname;
            }

            return null;
        }

        private static String GetProductByRelease()
        {
            var di = "/etc/".AsDirectory();
            if (!di.Exists) return null;

            foreach (var fi in di.GetFiles("*-release"))
            {
                if (!fi.Name.EqualIgnoreCase("redhat-release", "debian-release", "os-release", "system-release"))
                {
                    var dic = File.ReadAllText(fi.FullName).SplitAsDictionary("=", "\n", true);
                    if (dic.TryGetValue("BOARD", out var str)) return str;
                    if (dic.TryGetValue("BOARD_NAME", out str)) return str;
                }
            }

            return null;
        }

        private static Boolean TryRead(String fileName, out String value)
        {
            value = null;

            if (!File.Exists(fileName)) return false;

            try
            {
                value = File.ReadAllText(fileName)?.Trim();
                if (value.IsNullOrEmpty()) return false;
            }
            catch { return false; }

            return true;
        }

        private static IDictionary<String, String> ReadInfo(String file, Char separate = ':')
        {
            if (file.IsNullOrEmpty() || !File.Exists(file)) return null;

            var dic = new NullableDictionary<String, String>(StringComparer.OrdinalIgnoreCase);

            using var reader = new StreamReader(file);
            while (!reader.EndOfStream)
            {
                // 按行读取
                var line = reader.ReadLine();
                if (line != null)
                {
                    // 分割
                    var p = line.IndexOf(separate);
                    if (p > 0)
                    {
                        var key = line[..p].Trim();
                        var value = line[(p + 1)..].Trim();
                        dic[key] = value;
                    }
                }
            }

            return dic;
        }

        private static String Execute(String cmd, String arguments = null)
        {
            try
            {
                var psi = new ProcessStartInfo(cmd, arguments)
                {
                    // UseShellExecute 必须 false，以便于后续重定向输出流
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    //RedirectStandardError = true,
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
            var dic = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);

            var args = $"{type} get {keys.Join(",")} /format:list";
            var str = Execute("wmic", args)?.Trim();
            if (str.IsNullOrEmpty()) return dic;

            var ss = str.Split(Environment.NewLine);
            foreach (var item in ss)
            {
                var ks = item?.Split('=');
                if (ks != null && ks.Length >= 2)
                {
                    var k = ks[0].Trim();
                    var v = ks[1].Trim();
                    if (dic.TryGetValue(k, out var val))
                        dic[k] = val + "," + v;
                    else
                        dic[k] = v;
                }
            }

            // 排序，避免多个磁盘序列号时，顺序变动
            //foreach (var item in dic)
            //{
            //    if (item.Value.Contains(','))
            //        dic[item.Key] = item.Value.Split(',').OrderBy(e => e).Join();
            //}
            var dic2 = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in dic)
            {
                if (item.Value.Contains(','))
                    dic2[item.Key] = item.Value.Split(',').OrderBy(e => e).Join();
                else
                    dic2[item.Key] = item.Value;
            }

            return dic2;
        }

        /// <summary>
        /// 获取设备信息。用于Xamarin
        /// </summary>
        /// <returns></returns>
        public static IDictionary<String, String> ReadDeviceInfo()
        {
            var dic = new Dictionary<String, String>();
            if (!Runtime.Mono) return dic;

            {
                var type = "Android.OS.Build".GetTypeEx();
                if (type != null)
                {
                    foreach (var item in type.GetProperties(BindingFlags.Public | BindingFlags.Static))
                    {
                        try
                        {
                            dic[item.Name] = item.GetValue(null) + "";
                        }
                        catch { }
                    }
                }
            }
            {
                var type = "Xamarin.Essentials.DeviceInfo".GetTypeEx();
                if (type != null)
                {
                    foreach (var item in type.GetProperties(BindingFlags.Public | BindingFlags.Static))
                    {
                        try
                        {
                            dic[item.Name] = item.GetValue(null) + "";
                        }
                        catch { }
                    }
                }
            }
            {
                var type = "Android.Provider.Settings".GetTypeEx()?.GetNestedType("Secure");
                if (type != null)
                {
                    var resolver = "Android.App.Application".GetTypeEx()?.GetValue("Context")?.GetValue("ContentResolver");
                    if (resolver != null)
                    {
                        var name = "android_id";
                        dic[name] = type.Invoke("GetString", resolver, name) as String;
                    }
                }
            }

            return dic;
        }

        /// <summary>
        /// 获取设备电量。用于 Xamarin
        /// </summary>
        /// <returns></returns>
        public static IDictionary<String, Object> ReadDeviceBattery()
        {
            var dic = new Dictionary<String, Object>();
            if (!Runtime.Mono) return dic;

            var type = "Xamarin.Essentials.Battery".GetTypeEx();
            if (type == null) return dic;

            foreach (var item in type.GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                try
                {
                    dic[item.Name] = item.GetValue(null);
                }
                catch { }
            }

            return dic;
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
        #endregion
    }
}