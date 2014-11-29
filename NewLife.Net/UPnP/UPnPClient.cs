using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using NewLife.Configuration;
using NewLife.Log;
using NewLife.Net.Sockets;
using NewLife.Xml;

namespace NewLife.Net.UPnP
{
    /// <summary>通用即插即用协议客户端</summary>
    /// <remarks>
    /// UPnP 是各种各样的智能设备、无线设备和个人电脑等实现遍布全球的对等网络连接（P2P）的结构。UPnP 是一种分布式的，开放的网络架构。UPnP 是独立的媒介。
    /// 
    /// <a target="_blank" href="http://baike.baidu.com/view/27925.htm">UPnP</a>
    /// </remarks>
    /// <example>
    /// <code>
    /// UPnPClient client = new UPnPClient();
    /// client.OnNewDevice += new EventHandler&lt;NewLife.EventArgs&lt;InternetGatewayDevice, bool&gt;&gt;(client_OnNewDevice);
    /// client.StartDiscover();
    /// 
    /// static void client_OnNewDevice(object sender, EventArgs&lt;InternetGatewayDevice, bool&gt; e)
    /// {
    ///     Console.WriteLine("{0}{1}", e.Arg1, e.Arg2 ? " [缓存]" : "");
    ///     if (e.Arg2) return;
    /// 
    ///     foreach (var item in e.Arg1.GetMapByIndexAll())
    ///     {
    ///         Console.WriteLine(item);
    ///     }
    /// }
    /// </code>
    /// </example>
    public class UPnPClient : Netbase
    {
        #region 属性
        private UdpServer _Udp;
        /// <summary>Udp客户端，用于发现网关设备</summary>
        private UdpServer Udp
        {
            get
            {
                if (_Udp == null)
                {
                    _Udp = new UdpServer();
                    //_Udp.Name = "UPnPClient";
                    //_Udp.ProtocolType = ProtocolType.Udp;
                    _Udp.Received += Udp_Received;
                    //_Udp.Start();
                    _Udp.ReceiveAsync();
                }
                return _Udp;
            }
            set { _Udp = value; }
        }

        private SortedList<String, InternetGatewayDevice> _Gateways;
        /// <summary>网关设备</summary>
        public SortedList<String, InternetGatewayDevice> Gateways
        {
            get { return _Gateways ?? (_Gateways = new SortedList<String, InternetGatewayDevice>()); }
            //set { _Gateways = value; }
        }
        #endregion

        #region 构造
        /// <summary>释放资源</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            try
            {
                if (_Udp != null) _Udp.Dispose();
            }
            catch { }
        }
        #endregion

        #region 发现
        const String UPNP_DISCOVER = "" +
            "M-SEARCH * HTTP/1.1\r\n" +
            "HOST: 239.255.255.250:1900\r\n" +
            "MAN: \"ssdp:discover\"\r\n" +
            "MX: 3\r\n" +
            "ST: UPnP:rootdevice\r\n" + // 搜索目标，这里只要根设备
            "\r\n\r\n";

        /// <summary>开始</summary>
        public void StartDiscover()
        {
            if (CacheGateway) ThreadPool.QueueUserWorkItem(delegate(Object state) { CheckCacheGateway(); });

            IPAddress address = NetHelper.ParseAddress("239.255.255.250");

            Udp.Client.EnableBroadcast = true;
            Udp.Client.Send(UPNP_DISCOVER, Encoding.ASCII, new IPEndPoint(address, 1900));

            //Boolean hasDefault = false;
            //foreach (var item in NetHelper.GetMulticasts())
            //{
            //    foreach (var s in Udp.Servers)
            //    {
            //        if (s.AddressFamily == item.AddressFamily)
            //        {
            //            (s as UdpServer).Send(UPNP_DISCOVER, null, new IPEndPoint(item, 1900));
            //            if (item.Equals(address)) hasDefault = true;
            //            break;
            //        }
            //    }
            //}
            //if (!hasDefault)
            //{
            //    foreach (var s in Udp.Servers)
            //    {
            //        if (s.AddressFamily == address.AddressFamily)
            //        {
            //            (s as UdpServer).Send(UPNP_DISCOVER, null, new IPEndPoint(address, 1900));
            //            break;
            //        }
            //    }
            //}
        }

        //List<String> process = new List<String>();
        void Udp_Received(object sender, ReceivedEventArgs e)
        {
            var content = e.Stream.ToStr();
            if (String.IsNullOrEmpty(content)) return;

            var udp = e as UdpReceivedEventArgs;

            var remote = udp.Remote;
            var address = remote.Address;
            WriteLog("发现UPnP设备：{0}", remote);

            //分析数据并反序列化
            String sp = "LOCATION:";
            Int32 p = content.IndexOf(sp);
            if (p <= 0) return;

            String url = content.Substring(p + sp.Length);
            p = url.IndexOf(Environment.NewLine);
            if (p <= 0) return;

            url = url.Substring(0, p);
            url = url.Trim();
            if (String.IsNullOrEmpty(url)) return;

            try
            {
                //下载IGD.XML
                WebClient client = new WebClient();
                String xml = client.DownloadString(url);
                if (xml != null) xml = xml.Trim();

                Uri uri = new Uri(url);
                if (CacheGateway) File.WriteAllText(GetCacheFile(uri.Host), xml);

                AddGateway(uri.Host, xml, false);
            }
            catch (Exception ex)
            {
                WriteLog(ex.Message + " 路径[" + url + "]");
                throw;
            }
        }

        void AddGateway(String address, String content, Boolean isCache)
        {
            //反序列化
            var device = content.Trim().ToXmlEntity<InternetGatewayDevice>();
            //XmlSerializer serial = new XmlSerializer(typeof(InternetGatewayDevice));
            //InternetGatewayDevice device = null;
            //using (StringReader reader = new StringReader(content.Trim()))
            //{
            //    device = serial.Deserialize(reader) as InternetGatewayDevice;
            if (device == null) return;

            if (String.IsNullOrEmpty(device.URLBase)) device.URLBase = String.Format("http://{0}:1900", address);

            lock (Gateways)
            {
                Gateways[address] = device;
            }
            //}

            if (OnNewDevice != null) OnNewDevice(this, new EventArgs<InternetGatewayDevice, bool>(device, isCache));
        }

        /// <summary>发现新设备时触发。参数（设备，是否来自缓存）</summary>
        public event EventHandler<EventArgs<InternetGatewayDevice, Boolean>> OnNewDevice;

        const String cacheKey = "InternetGatewayDevice_";

        /// <summary>检查缓存的网关</summary>
        void CheckCacheGateway()
        {
            String p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, XTrace.TempPath);
            p = Path.Combine(p, "UPnP");
            if (!Directory.Exists(p)) return;

            String[] ss = Directory.GetFiles(p, cacheKey + "*.xml", SearchOption.TopDirectoryOnly);
            if (ss == null || ss.Length < 1) return;

            foreach (String item in ss)
            {
                String ip = Path.GetFileNameWithoutExtension(item).Substring(cacheKey.Length).Trim(new Char[] { '_' });

                AddGateway(ip, File.ReadAllText(item), true);
            }
        }

        static String GetCacheFile(String address)
        {
            var fileName = Path.Combine(Path.Combine(XTrace.TempPath, "UPnP"), String.Format(@"{0}{1}.xml", cacheKey, address));
            fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

            var dir = Path.GetDirectoryName(fileName);
            if (!String.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            return fileName;
        }
        #endregion

        #region 辅助函数
        /// <summary>是否缓存网关。缓存网关可以加速UPnP的发现过程</summary>
        public static Boolean CacheGateway { get { return Config.GetConfig<Boolean>("NewLife.Net.UPnP.CacheGateway"); } }
        #endregion
    }
}