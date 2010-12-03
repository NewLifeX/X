using System;
using System.Collections.Generic;
using System.Text;
using System.Management;
using System.Runtime.InteropServices;
using System.IO;

namespace XCode.XLicense
{
    /// <summary>
    /// 硬件信息
    /// </summary>
    internal class HardInfo
    {
        /// <summary>
        /// 机器名
        /// </summary>
        public static String MachineName
        {
            get
            {
                return Environment.MachineName;
            }
        }

        private static String _BaseBoard;
        /// <summary>
        /// 主板序列号
        /// </summary>
        public static String BaseBoard
        {
            get
            {
                if (_BaseBoard == null)
                {
                    _BaseBoard = GetInfo("Win32_BaseBoard", "SerialNumber");
                    if (String.IsNullOrEmpty(_BaseBoard)) _BaseBoard = GetInfo("Win32_BaseBoard", "Product");
                }
                return _BaseBoard;
            }
        }

        private static String _Processors;
        /// <summary>
        /// 处理器序列号
        /// </summary>
        public static String Processors
        {
            get
            {
                if (_Processors == null) _Processors = GetInfo("Win32_Processor", "ProcessorId");
                return _Processors;
            }
        }

        private static String _DiskDrives;
        /// <summary>
        /// 磁盘序列号
        /// </summary>
        public static String DiskDrives
        {
            get
            {
                //if (_DiskDrives == null) _DiskDrives = GetInfo("Win32_DiskDrive", "Model");
                //磁盘序列号不够明显，故使用驱动器序列号代替
                String id = AppDomain.CurrentDomain.BaseDirectory.Substring(0, 2);
                if (_DiskDrives == null) _DiskDrives = GetInfo("Win32_LogicalDisk Where DeviceID=\"" + id+"\"", "VolumeSerialNumber");
                return _DiskDrives;
                //上面的方式取驱动器序列号会取得包括U盘和网络映射驱动器的序列号，实际只要当前所在盘就可以了
                //return Volume;
            }
        }

        private static String _Volume = String.Empty;
        /// <summary>
        /// 驱动器序列号
        /// </summary>
        public static String Volume
        {
            get
            {
                //if (_Volume == null) _Volume = GetInfo("Win32_LogicalDisk", "VolumeSerialNumber");
                //return _Volume;
                if (_Volume == null)
                {
                    //DriveInfo di = new DriveInfo(AppDomain.CurrentDomain.BaseDirectory.Substring(0, 2));
                    //_Volume = di.VolumeLabel;
                    //String s1 = null;
                    //String s2 = null;
                    //Int32 c1 = 0;
                    //Int32 c2 = 0;
                    //Int32 c3 = 0;
                    //GetVolumeInformation(AppDomain.CurrentDomain.BaseDirectory.Substring(0, 2), s1, 256, ref c1, c2, c3, s2, 256);
                    //_Volume = c1.ToString("X");
                }
                return _Volume;
            }
        }

 //       [DllImport("kernel32.dll")]
 //       public static extern Int32 GetVolumeInformation(
 //string lpRootPathName,
 //string lpVolumeNameBuffer,
 //Int32 nVolumeNameSize,
 //ref Int32 lpVolumeSerialNumber,
 //Int32 lpMaximumComponentLength,
 //Int32 lpFileSystemFlags,
 //string lpFileSystemNameBuffer,
 //Int32 nFileSystemNameSize
 //);

        private static String _Macs;
        /// <summary>
        /// 网卡地址序列号
        /// </summary>
        public static String Macs
        {
            get
            {
                if (_Macs != null) return _Macs;
                //return GetInfo("Win32_NetworkAdapterConfiguration", "MacAddress");
                ManagementClass cimobject = new ManagementClass("Win32_NetworkAdapterConfiguration");
                ManagementObjectCollection moc = cimobject.GetInstances();
                List<String> bbs = new List<string>();
                foreach (ManagementObject mo in moc)
                {
                    if (mo != null &&
                        mo.Properties != null &&
                        mo.Properties["MacAddress"] != null &&
                        mo.Properties["MacAddress"].Value != null &&
                        mo.Properties["IPEnabled"] != null &&
                        (bool)mo.Properties["IPEnabled"].Value)
                        bbs.Add(mo.Properties["MacAddress"].Value.ToString());
                }
                bbs.Sort();
                StringBuilder sb = new StringBuilder();
                foreach (String s in bbs)
                {
                    sb.Append(s);
                    sb.Append(" ");
                }
                _Macs = sb.ToString().Trim();
                return _Macs;
            }
        }

        private static String _IPs;
        /// <summary>
        /// IP地址
        /// </summary>
        public static String IPs
        {
            get
            {
                if (_IPs != null) return _IPs;
                //return null;
                ManagementClass cimobject = new ManagementClass("Win32_NetworkAdapterConfiguration");
                ManagementObjectCollection moc = cimobject.GetInstances();
                List<String> bbs = new List<string>();
                foreach (ManagementObject mo in moc)
                {
                    if (mo != null &&
                        mo.Properties != null &&
                        mo.Properties["IPAddress"] != null &&
                        mo.Properties["IPAddress"].Value != null &&
                        mo.Properties["IPEnabled"] != null &&
                        (bool)mo.Properties["IPEnabled"].Value)
                    {
                        String[] ss = (String[])mo.Properties["IPAddress"].Value;
                        if (ss != null)
                        {
                            foreach (String s in ss)
                                bbs.Add(s);
                        }
                        //bbs.Add(mo.Properties["IPAddress"].Value.ToString());
                    }
                }
                bbs.Sort();
                StringBuilder sb = new StringBuilder();
                foreach (String s in bbs)
                {
                    sb.Append(s);
                    sb.Append(" ");
                }
                _IPs = sb.ToString().Trim();
                return _IPs;
            }
        }

        private static String GetInfo(String path, String property)
        {
            String wql = String.Format("Select {0} From {1}", property, path);
            ManagementObjectSearcher cimobject = new ManagementObjectSearcher(wql);
            ManagementObjectCollection moc = cimobject.Get();
            List<String> bbs = new List<string>();
            foreach (ManagementObject mo in moc)
            {
                if (mo != null &&
                    mo.Properties != null &&
                    mo.Properties[property] != null &&
                    mo.Properties[property].Value != null)
                    bbs.Add(mo.Properties[property].Value.ToString());
            }
            bbs.Sort();
            StringBuilder sb = new StringBuilder();
            foreach (String s in bbs)
            {
                sb.Append(s);
                sb.Append(" ");
            }
            return sb.ToString().Trim();
        }
    }
}