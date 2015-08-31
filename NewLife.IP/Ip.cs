using System;
using System.Linq;
using System.IO;
using System.IO.Compression;
using NewLife.Threading;
using NewLife.Web;
using NewLife.Log;
using System.Diagnostics;
using System.Net;

namespace NewLife.IP
{
    /// <summary>IP搜索</summary>
    public static class Ip
    {
        private static object lockHelper = new object();
        private static Zip zip;

        private static String _DbFile;
        /// <summary>数据文件</summary>
        public static String DbFile { get { return _DbFile; } set { _DbFile = value; zip = null; } }

        static Ip()
        {
            var ns = new String[] { "qqwry.dat", "qqwry.gz", "ip.gz", "ip.gz.config", "ipdata.config" };
            foreach (var item in ns)
            {
                var fs = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, item, SearchOption.AllDirectories);
                if (fs != null && fs.Length > 0)
                {
                    _DbFile = Path.GetFullPath(fs[0]);
                    break;
                }
            }

            // 如果本地没有IP数据库，则从网络下载
            if (_DbFile.IsNullOrWhiteSpace())
            {
                ThreadPoolX.QueueUserWorkItem(() =>
                {
                    var url = "http://www.newlifex.com/showtopic-51.aspx";
                    XTrace.WriteLine("没有找到IP数据库，准备联网获取 {0}", url);

                    var client = new WebClientX();
                    client.Log = XTrace.Log;
                    var dir = Runtime.IsWeb ? "App_Data" : "Data";
                    var file = client.DownloadLink(url, "ip.gz", dir.GetFullPath());

                    if (File.Exists(file))
                    {
                        _DbFile = file.GetFullPath();
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

                if (!File.Exists(_DbFile))
                {
                    //throw new InvalidOperationException("无法找到IP数据库" + _DbFile + "！");
                    XTrace.WriteLine("无法找到IP数据库{0}", _DbFile);
                    return false;
                }
                XTrace.WriteLine("使用IP数据库{0}", _DbFile);
                using (var fs = File.OpenRead(_DbFile))
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

            if (zip.Stream == null) throw new InvalidOperationException("无法打开IP数据库" + _DbFile + "！");

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

        static uint IPToUInt32(String IpValue)
        {
            var ss = IpValue.Split('.');
            var buf = new Byte[4];
            for (int i = 0; i < 4; i++)
            {
                var n = 0;
                if (i < ss.Length && Int32.TryParse(ss[i], out n))
                {
                    buf[3 - i] = (Byte)n;
                }
            }
            return BitConverter.ToUInt32(buf, 0);
        }
    }

    class MyIpProvider : NetHelper.IpProvider
    {
        public string GetAddress(IPAddress addr)
        {
            return Ip.GetAddress(addr);
        }
    }
}