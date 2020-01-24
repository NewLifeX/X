using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading.Tasks;
using NewLife.Collections;
using NewLife.Log;
using NewLife.Model;
using NewLife.Serialization;
using NewLife.Threading;
#if __WIN__
using System.Management;
using Microsoft.VisualBasic.Devices;
using Microsoft.Win32;
#endif

namespace NewLife
{
    /// <summary>机器信息</summary>
    /// <remarks>
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

        /// <summary>处理器序列号</summary>
        public String Processor { get; set; }

        /// <summary>处理器序列号</summary>
        public String CpuID { get; set; }

        /// <summary>唯一标识</summary>
        public String UUID { get; set; }

        /// <summary>机器标识</summary>
        public String Guid { get; set; }

        /// <summary>内存总量</summary>
        public UInt64 Memory { get; set; }
        /// <summary>可用内存</summary>
        public UInt64 AvailableMemory { get; private set; }

        /// <summary>CPU占用率</summary>
        public Single CpuRate { get; private set; }

        /// <summary>温度</summary>
        public Double Temperature { get; set; }

#if __WIN__
        private ComputerInfo _cinfo;
        private PerformanceCounter _cpuCounter;
#endif

        private TimerX _timer;
        #endregion

        #region 构造
        /// <summary>实例化机器信息</summary>
        public MachineInfo() { }

        /// <summary>当前机器信息</summary>
        public static MachineInfo Current { get; set; }

        /// <summary>异步注册一个初始化后的机器信息实例</summary>
        /// <param name="msRefresh">定时刷新实时数据，默认0ms不刷新</param>
        /// <returns></returns>
        public static Task<MachineInfo> RegisterAsync(Int32 msRefresh = 0)
        {
            return Task.Factory.StartNew(() =>
            {
                // 文件缓存，加快机器信息获取
                var file = XTrace.TempPath.CombinePath("machine.info").GetBasePath();
                if (Current == null && File.Exists(file))
                {
                    try
                    {
                        Current = File.ReadAllText(file).ToJsonEntity<MachineInfo>();
                    }
                    finally { }
                }

                var mi = Current ?? new MachineInfo();

                mi.Init();
                File.WriteAllText(file.EnsureDirectory(true), mi.ToJson(true));

                // 定时刷新
                if (msRefresh > 0) mi._timer = new TimerX(s => mi.Refresh(), null, msRefresh, msRefresh) { Async = true };

                Current = mi;

                // 注册到对象容器
                ObjectContainer.Current.Register<MachineInfo>(mi);

                return mi;
            });
        }

        /// <summary>从对象容器中获取一个已注册机器信息实例</summary>
        /// <returns></returns>
        public static MachineInfo Resolve() => ObjectContainer.Current.ResolveInstance<MachineInfo>();
        #endregion

        #region 方法
        /// <summary>刷新</summary>
        public void Init()
        {
#if __CORE__
            var osv = Environment.OSVersion;
            if (OSVersion.IsNullOrEmpty()) OSVersion = osv.Version + "";
            if (OSName.IsNullOrEmpty()) OSName = (osv + "").TrimStart("Microsoft").TrimEnd(OSVersion).Trim();
            if (Guid.IsNullOrEmpty()) Guid = "";

            if (Runtime.Windows)
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
            }
            // 特别识别Linux发行版
            else if (Runtime.Linux)
            {
                var str = GetLinuxName();
                if (!str.IsNullOrEmpty()) OSName = str;

                // 树莓派优先 Model
                var dic = ReadInfo("/proc/cpuinfo");
                if (dic != null)
                {
                    if (dic.TryGetValue("Model", out str) ||
                        dic.TryGetValue("Hardware", out str) ||
                        dic.TryGetValue("cpu model", out str) ||
                        dic.TryGetValue("model name", out str))
                        Processor = str;

                    if (dic.TryGetValue("Serial", out str)) CpuID = str;
                }

                var mid = "/etc/machine-id";
                if (!File.Exists(mid)) mid = "/var/lib/dbus/machine-id";
                if (File.Exists(mid)) Guid = File.ReadAllText(mid).Trim();

                var file = "/sys/class/dmi/id/product_uuid";
                if (File.Exists(file)) UUID = File.ReadAllText(file).Trim();
                file = "/sys/class/dmi/id/product_name";
                if (File.Exists(file)) Product = File.ReadAllText(file).Trim();

                var dmi = Execute("dmidecode")?.SplitAsDictionary(":", "\n");
                if (dmi != null)
                {
                    if (dmi.TryGetValue("ID", out str)) CpuID = str.Replace(" ", null);
                    if (dmi.TryGetValue("UUID", out str)) UUID = str;
                    if (dmi.TryGetValue("Product Name", out str)) Product = str;
                    //if (TryFind(dmi, new[] { "Serial Number" }, out str)) Guid = str;
                }
            }
#else
            // 性能计数器的初始化非常耗时
            Task.Factory.StartNew(() =>
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total")
                {
                    MachineName = "."
                };
                _cpuCounter.NextValue();
            });

            var reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
            if (reg != null) Guid = reg.GetValue("MachineGuid") + "";
            if (Guid.IsNullOrEmpty())
            {
                reg = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                if (reg != null) Guid = reg.GetValue("MachineGuid") + "";
            }

            var ci = new ComputerInfo();
            OSName = ci.OSFullName.TrimStart("Microsoft").Trim();
            OSVersion = ci.OSVersion;
            Memory = ci.TotalPhysicalMemory;

            _cinfo = ci;

            Processor = GetInfo("Win32_Processor", "Name");
            CpuID = GetInfo("Win32_Processor", "ProcessorId");
            UUID = GetInfo("Win32_ComputerSystemProduct", "UUID");
            Product = GetInfo("Win32_ComputerSystemProduct", "Name");

            // 读取主板温度，不太准。标准方案是ring0通过IOPort读取CPU温度，太难在基础类库实现
            var str = GetInfo("MSAcpi_ThermalZoneTemperature", "CurrentTemperature");
            if (!str.IsNullOrEmpty()) Temperature = (str.ToDouble() - 2732) / 10.0;
#endif

            Refresh();
        }

        /// <summary>获取实时数据，如CPU、内存、温度</summary>
        public void Refresh()
        {
#if __CORE__
            if (Runtime.Windows)
            {
                var str = "";

                var cpu = ReadWmic("cpu", "Name", "ProcessorId", "LoadPercentage");
                if (cpu != null)
                {
                    if (cpu.TryGetValue("Name", out str)) Processor = str;
                    if (cpu.TryGetValue("ProcessorId", out str)) CpuID = str;
                    if (cpu.TryGetValue("LoadPercentage", out str)) CpuRate = (Single)(str.ToDouble() / 100);
                }

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
                var str = "";

                var dic = ReadInfo("/proc/meminfo");
                if (dic != null)
                {
                    if (dic.TryGetValue("MemTotal", out str))
                        Memory = (UInt64)str.TrimEnd(" kB").ToInt() * 1024;

                    if (dic.TryGetValue("MemAvailable", out str) ||
                        dic.TryGetValue("MemFree", out str))
                        AvailableMemory = (UInt64)str.TrimEnd(" kB").ToInt() * 1024;
                }

                var file = "/sys/class/thermal/thermal_zone0/temp";
                if (File.Exists(file))
                    Temperature = File.ReadAllText(file).Trim().ToDouble() / 1000;
                else
                {
                    // A2温度获取，Ubuntu 16.04 LTS， Linux 3.4.39
                    file = "/sys/class/hwmon/hwmon0/device/temp_value";
                    if (File.Exists(file)) Temperature = File.ReadAllText(file).Trim().Substring(null, ":").ToDouble();
                }

                var upt = Execute("uptime");
                if (!upt.IsNullOrEmpty())
                {
                    str = upt.Substring("load average:");
                    if (!str.IsNullOrEmpty()) CpuRate = (Single)str.Split(",")[0].ToDouble();
                }
            }
#else
            AvailableMemory = _cinfo.AvailablePhysicalMemory;
            CpuRate = _cpuCounter == null ? 0 : (_cpuCounter.NextValue() / 100);
#endif
        }
        #endregion

        #region 辅助
        /// <summary>获取Linux发行版名称</summary>
        /// <returns></returns>
        public static String GetLinuxName()
        {
            var fr = "/etc/redhat-release";
            var dr = "/etc/debian-release";
            if (File.Exists(fr))
                return File.ReadAllText(fr).Trim();
            else if (File.Exists(dr))
                return File.ReadAllText(dr).Trim();
            else
            {
                var sr = "/etc/os-release";
                if (File.Exists(sr)) return File.ReadAllText(sr).SplitAsDictionary("=", "\n", true)["PRETTY_NAME"].Trim();
            }

            var uname = Execute("uname", "-sr")?.Trim();
            if (!uname.IsNullOrEmpty()) return uname;

            return null;
        }

        private static IDictionary<String, String> ReadInfo(String file, Char separate = ':')
        {
            if (file.IsNullOrEmpty() || !File.Exists(file)) return null;

            var dic = new NullableDictionary<String, String>();

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
                        var key = line.Substring(0, p).Trim();
                        var value = line.Substring(p + 1).Trim();
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
                var psi = new ProcessStartInfo(cmd, arguments) { RedirectStandardOutput = true };
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
            var str = Execute("wmic", args);
            if (str.IsNullOrEmpty()) return dic;

            return str.SplitAsDictionary("=", Environment.NewLine);
        }
        #endregion

        #region 内存
#if __CORE__
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
#endif
        #endregion

        #region WMI辅助
#if __WIN__
        /// <summary>获取WMI信息</summary>
        /// <param name="path"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        public static String GetInfo(String path, String property)
        {
            // Linux Mono不支持WMI
            if (Runtime.Mono) return "";

            var bbs = new List<String>();
            try
            {
                var wql = String.Format("Select {0} From {1}", property, path);
                var cimobject = new ManagementObjectSearcher(wql);
                var moc = cimobject.Get();
                foreach (var mo in moc)
                {
                    if (mo != null &&
                        mo.Properties != null &&
                        mo.Properties[property] != null &&
                        mo.Properties[property].Value != null)
                        bbs.Add(mo.Properties[property].Value.ToString());
                }
            }
            catch (Exception ex)
            {
                //XTrace.WriteException(ex);
                XTrace.WriteLine("WMI.GetInfo({0})失败！{1}", path, ex.Message);
                return "";
            }

            bbs.Sort();

            return bbs.Distinct().Join();
        }
#endif
        #endregion
    }
}