using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Log;
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
        public String OSName { get; private set; }

        /// <summary>系统版本</summary>
        public String OSVersion { get; private set; }

        /// <summary>处理器序列号</summary>
        public String Processor { get; private set; }

        /// <summary>处理器序列号</summary>
        public String CpuID { get; private set; }

        /// <summary>唯一标识</summary>
        public String UUID { get; private set; }

        /// <summary>机器标识</summary>
        public String Guid { get; private set; }

        /// <summary>内存总量</summary>
        public UInt64 Memory { get; private set; }

#if __WIN__
        private ComputerInfo _cinfo;
        /// <summary>可用内存</summary>
        public UInt64 AvailableMemory => _cinfo.AvailablePhysicalMemory;

        private PerformanceCounter _cpuCounter;
        /// <summary>CPU占用率</summary>
        public Single CpuRate => _cpuCounter == null ? 0 : (_cpuCounter.NextValue() / 100);
#else
        /// <summary>可用内存</summary>
        public UInt64 AvailableMemory { get; private set; }

        /// <summary>CPU占用率</summary>
        public Single CpuRate { get; private set; }
#endif

        /// <summary>温度</summary>
        public Double Temperature { get; }
        #endregion

        #region 构造
        /// <summary>实例化机器信息</summary>
        public MachineInfo()
        {
            Refresh();
        }
        #endregion

        #region 方法
        /// <summary>刷新</summary>
        public void Refresh()
        {
#if __WIN__
            var reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
            if (reg != null)
            {
                Guid = reg.GetValue("MachineGuid") + "";
            }

            // 性能计数器的初始化非常耗时
            Task.Factory.StartNew(() =>
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total")
                {
                    MachineName = "."
                };
                _cpuCounter.NextValue();
            });

            var ci = new ComputerInfo();
            OSName = ci.OSFullName;
            OSVersion = ci.OSVersion;

            Memory = ci.TotalPhysicalMemory;
            //AvailableMemory = ci.AvailablePhysicalMemory;

            _cinfo = ci;

            Processor = GetInfo("Win32_Processor", "Name");
            CpuID = GetInfo("Win32_Processor", "ProcessorId");
            UUID = GetInfo("Win32_ComputerSystemProduct", "UUID");
#endif
        }
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