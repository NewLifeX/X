using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using NewLife.Log;
using NewLife.Threading;
using NewLife.Web;

namespace NewLife.IP
{
    /// <summary>IP搜索</summary>
    public static class Ip
    {
        private static readonly Object lockHelper = new Object();
        private static Zip zip;

        /// <summary>数据文件</summary>
        public static String DbFile { get; set; }

        static Ip()
        {
            var dir = Runtime.IsWeb ? "..\\Data" : ".";
            var ip = dir.CombinePath("ip.gz").GetFullPath();
            if (File.Exists(ip)) DbFile = ip;

            // 如果本地没有IP数据库，则从网络下载
            if (DbFile.IsNullOrWhiteSpace())
            {
                ThreadPoolX.QueueUserWorkItem(() =>
                {
                    var url = Setting.Current.PluginServer;
                    XTrace.WriteLine("没有找到IP数据库{0}，准备联网获取 {1}", ip, url);

                    var client = new WebClientX
                    {
                        Log = XTrace.Log
                    };
                    var file = client.DownloadLink(url, "ip.gz", dir.GetFullPath());

                    if (File.Exists(file))
                    {
                        DbFile = file.GetFullPath();
                        zip = null;
                        // 让它重新初始化
                        _inited = null;
                    }
                });
            }
        }

        static Boolean? _inited;
        static Boolean Init()
        {
            if (_inited != null) return _inited.Value;
            lock (typeof(Ip))
            {
                if (_inited != null) return _inited.Value;
                _inited = false;

                var z = new Zip();

                if (!File.Exists(DbFile))
                {
                    //throw new InvalidOperationException("无法找到IP数据库" + DbFile + "！");
                    XTrace.WriteLine("无法找到IP数据库{0}", DbFile);
                    return false;
                }
                XTrace.WriteLine("使用IP数据库{0}", DbFile);
                using (var fs = File.OpenRead(DbFile))
                {
                    try
                    {
                        z.SetStream(fs);
                    }
                    catch (Exception ex)
                    {
                        XTrace.WriteException(ex);

                        return false;
                    }
                }
                zip = z;
            }

            if (zip.Stream == null) throw new InvalidOperationException("无法打开IP数据库" + DbFile + "！");

            _inited = true;
            return true;
        }

        /// <summary>获取IP地址</summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static String GetAddress(String ip)
        {
            if (String.IsNullOrEmpty(ip)) return "";

            if (!Init()) return "";

            var ip2 = IPToUInt32(ip.Trim());
            lock (lockHelper)
            {
                return zip.GetAddress(ip2) + "";
            }
        }

        /// <summary>获取IP地址</summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public static String GetAddress(IPAddress addr)
        {
            if (addr == null) return "";

            if (!Init()) return "";

            var ip2 = (UInt32)addr.GetAddressBytes().Reverse().ToInt();
            lock (lockHelper)
            {
                return zip.GetAddress(ip2) + "";
            }
        }

        static UInt32 IPToUInt32(String IpValue)
        {
            var ss = IpValue.Split('.');
            //var buf = stackalloc Byte[4];
            var val = 0u;
            //var ptr = (Byte*)&val;
            for (var i = 0; i < 4; i++)
            {
                if (i < ss.Length && UInt32.TryParse(ss[i], out var n))
                {
                    //buf[3 - i] = (Byte)n;
                    val |= n << (3 - i);
                    //ptr[3 - i] = n;
                }
            }
            //return BitConverter.ToUInt32(buf, 0);
            return val;
        }
    }

    class MyIpProvider : NetHelper.IPProvider
    {
        public String GetAddress(IPAddress addr) => Ip.GetAddress(addr);
    }
}