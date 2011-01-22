using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using NewLife.Log;
using NewLife.Net.Udp;
using NewLife.Net.Sockets;
using NewLife.Net.Common;
using NewLife.Reflection;
using NewLife.Configuration;
using NewLife.Net.Tcp;

namespace NewLife.Net.UPnP
{
    /// <summary>
    /// 通用即插即用协议客户端
    /// </summary>
    public class UPnPClient : DisposeBase
    {
        #region 属性
        //public String Location = null;
        public InternetGatewayDevice IGD = null;
        //public String IGDXML;
        //映射前是否检查端口
        public static bool IsPortCheck = true;

        private UdpClientX _Udp;
        /// <summary>Udp客户端，用于发现网关设备</summary>
        private UdpClientX Udp
        {
            get { return _Udp; }
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
        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            try
            {
                if (Udp != null) Udp.Dispose();
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
            "ST: UPnPClient:rootdevice\r\n" +
            "\r\n\r\n";

        /// <summary>
        /// 开始
        /// </summary>
        public void StartDiscover()
        {
            Udp = new UdpClientX();
            Udp.Received += new EventHandler<NetEventArgs>(Udp_Received);
            Udp.ReceiveAsync();

            IPAddress address = NetHelper.ParseAddress("239.255.255.250");
            IPEndPoint remoteEP = new IPEndPoint(address, 1900);

            // 设置多播
            Socket socket = Udp.Client;
            //socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 4);
            //socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, 1);
            //MulticastOption optionValue = new MulticastOption(remoteEP.Address);
            //try
            //{
            //    socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, optionValue);
            //    //socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, (int)this.boundto.Address.Address);
            //}
            //catch (Exception)
            //{
            //    return;
            //}

            byte[] data = Encoding.ASCII.GetBytes(UPNP_DISCOVER);
            Udp.Client.EnableBroadcast = true;
            Udp.Send(data, remoteEP);

            //socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DropMembership, optionValue);

            if (CacheGateway) CheckCacheGateway();
        }

        void Udp_Received(object sender, NetEventArgs e)
        {
            String content = e.GetString();
            if (String.IsNullOrEmpty(content)) return;

            IPEndPoint remote = e.RemoteEndPoint as IPEndPoint;
            IPAddress address = remote.Address;

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

                Uri uri = new Uri(url);
                AddGateway(uri.Host, xml, false);

                if (CacheGateway) File.WriteAllText(GetCacheFile(uri.Host), xml);
            }
            catch (Exception ex)
            {
                XTrace.WriteLine(ex.Message + " 路径[" + url + "]");
                throw;
            }
        }

        void AddGateway(String address, String content, Boolean isCache)
        {
            //反序列化
            XmlSerializer serial = new XmlSerializer(typeof(InternetGatewayDevice));
            InternetGatewayDevice device = null;
            using (StringReader reader = new StringReader(content))
            {
                device = serial.Deserialize(reader) as InternetGatewayDevice;
                if (device == null) return;

                if (String.IsNullOrEmpty(device.URLBase)) device.URLBase = String.Format("http://{0}:1900", address);

                if (Gateways.ContainsKey(address))
                    Gateways[address] = device;
                else
                    Gateways.Add(address, device);
            }

            if (OnNewDevice != null) OnNewDevice(this, new EventArgs<InternetGatewayDevice, bool>(device, isCache));
        }

        /// <summary>
        /// 发现新设备时触发。参数（设备，是否来自缓存）
        /// </summary>
        public event EventHandler<EventArgs<InternetGatewayDevice, Boolean>> OnNewDevice;

        const String cacheKey = "InternetGatewayDevice_";

        /// <summary>
        /// 检查缓存的网关
        /// </summary>
        void CheckCacheGateway()
        {
            String p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "XCache");
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
            String fileName = String.Format(@"XCache\{0}{1}.xml", cacheKey, address);
            fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

            if (!Directory.Exists(Path.GetDirectoryName(fileName))) Directory.CreateDirectory(Path.GetDirectoryName(fileName));

            return fileName;
        }
        #endregion

        #region 操作
        ///// <summary>
        ///// 添加映射端口
        ///// </summary>
        ///// <param name="RemoteHost">远程主机</param>
        ///// <param name="ExternalPort">外部端口</param>
        ///// <param name="Protocol">TCP或UDP</param>
        ///// <param name="InternalPort">内部端口</param>
        ///// <param name="InternalClient">本地IP地址</param>
        ///// <param name="Enabled">是否启用[0,1]</param>
        ///// <param name="Description">端口映射的描述</param>
        ///// <param name="Duration">映射的持续时间，用0表示不永久</param>
        ///// <returns>bool</returns>
        //public bool Add(String RemoteHost, Int32 ExternalPort, String Protocol, Int32 InternalPort, String InternalClient, Int32 Enabled, String Description, int? Duration)
        //{
        //    if (IsPortCheck == true && GetMapByPortAndProtocol(null, ExternalPort, Protocol) != null)
        //    {
        //        XTrace.WriteLine(ExternalPort + "端口被占用");
        //        return false;
        //    }
        //    String Command = XMLCommand.Add(RemoteHost, ExternalPort, Protocol, InternalPort, InternalClient, Enabled, Description, Duration);
        //    return SOAPRequest(Command);
        //}

        ///// <summary>
        ///// 删除端口映射
        ///// </summary>
        ///// <param name="RemoteHost">远程主机</param>
        ///// <param name="ExternalPort">外部端口</param>
        ///// <param name="Protocol">TCP或UDP</param>
        ///// <returns></returns>
        //public bool Del(String RemoteHost, Int32 ExternalPort, String Protocol)
        //{
        //    String Command = XMLCommand.Del(RemoteHost, ExternalPort, Protocol);
        //    return SOAPRequest(Command);
        //}
        #endregion

        #region 查找
        /// <summary>
        /// 获取端口映射信息
        /// </summary>
        /// <param name="RemoteHost">远程主机</param>
        /// <param name="ExternalPort">外部端口</param>
        /// <param name="Protocol">TCP/UDP</param>
        /// <returns></returns>
        public static PortMappingEntry GetMapByPortAndProtocol(InternetGatewayDevice device, String RemoteHost, Int32 ExternalPort, String Protocol)
        {
            PortMappingEntry entity = new PortMappingEntry();
            entity.Name = "GetSpecificPortMappingEntry";
            entity.NewRemoteHost = RemoteHost;
            entity.NewExternalPort = ExternalPort;
            entity.NewProtocol = Protocol;

            PortMappingEntry response = Request<PortMappingEntry>(device, entity);
            return response;
        }

        /// <summary>
        /// 获取端口映射信息
        /// </summary>
        /// <param name="index">索引</param>
        /// <returns></returns>
        public static PortMappingEntry GetMapByIndex(InternetGatewayDevice device, Int32 index)
        {
            PortMappingEntryRequest entity = new PortMappingEntryRequest();
            entity.Name = "GetGenericPortMappingEntry";
            entity.NewPortMappingIndex = index;

            PortMappingEntry response = Request<PortMappingEntry>(device, entity);
            return response;
        }

        /// <summary>
        /// 获取所有端口映射信息
        /// </summary>
        /// <returns></returns>
        public static List<PortMappingEntry> GetMapByIndexAll(InternetGatewayDevice device)
        {
            List<PortMappingEntry> list = new List<PortMappingEntry>();
            PortMappingEntry item;
            while (true)
            {
                item = GetMapByIndex(device, list.Count);
                if (item == null) break;
                list.Add(item);
            }
            return list;
        }
        #endregion

        #region 设备/服务
        /// <summary>
        /// 获取指定设备指定类型的服务
        /// </summary>
        /// <param name="device"></param>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public static Service GetService(Device device, String serviceType)
        {
            if (device == null || device.serviceList == null || device.serviceList.Count < 1) return null;

            foreach (Service item in device.serviceList)
            {
                if (String.Equals(item.serviceType, serviceType, StringComparison.OrdinalIgnoreCase))
                {
                    return item;
                }
            }

            if (device.deviceList == null || device.deviceList.Count < 1) return null;

            foreach (Device item in device.deviceList)
            {
                Service service = GetService(item, serviceType);
                if (service != null) return service;
            }

            return null;
        }

        /// <summary>
        /// 取得广域网IP连接设备
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        public static Service GetWANIPService(Device device)
        {
            return GetService(device, "urn:schemas-upnp-org:service:WANIPConnection:1");
        }
        #endregion

        #region SOAP
        static TResponse Request<TResponse>(InternetGatewayDevice device, UPnPAction action) where TResponse : UPnPAction<TResponse>, new()
        {
            if (device == null || device.device == null || action == null) return null;

            Service service = GetWANIPService(device.device);
            if (service == null) return null;

            Uri uri = new Uri(String.Format("http://{0}:{1}{2}", device.ServerHost, device.ServerPort, service.controlURL));

            String xml = action.ToSoap(service.serviceType);
            xml = SOAPRequest(uri, service.serviceType + "#" + action.Name, xml);

            TResponse response = UPnPAction<TResponse>.FromXml(xml);

            return response;
        }

        /// <summary>
        /// SOAP头部
        /// </summary>
        const String SOAP_HEADER = "" +
            "POST {0} HTTP/1.1\r\n" +
            "HOST: {1}\r\n" +
            "SOAPACTION: \"{2}\"\r\n" +
            "CONTENT-TYPE: text/xml ; charset=\"utf-8\"\r\n" +
            "Content-Length: {3}" +
            "\r\n\r\n";
        ///// <summary>
        ///// SOAP主体
        ///// </summary>
        //const String SOAP_BODY = "" +
        //    "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
        //    "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">\r\n" +
        //    "<s:Body>\r\n" +
        //    "{0}\r\n" +
        //    "</s:Body>\r\n" +
        //    "</s:Envelope>\r\n";

        /// <summary>
        /// 发送SOAP请求，发送xml，返回xml
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="action"></param>
        /// <param name="xml"></param>
        /// <returns></returns>
        static String SOAPRequest(Uri uri, String action, String xml)
        {
            //String body = String.Format(SOAP_BODY, xml);
            String header = String.Format(SOAP_HEADER, uri.PathAndQuery, uri.Host + ":" + uri.Port, action, Encoding.UTF8.GetByteCount(xml));

            TcpClientX client = new TcpClientX();
            try
            {
                client.Connect(uri.Host, uri.Port);
                client.Send(header + xml);

                String response = client.ReceiveString();
                if (String.IsNullOrEmpty(response)) return null;

                Int32 p = response.IndexOf("\r\n\r\n");
                if (p < 0) return null;

                response = response.Substring(p).Trim();
                if (String.IsNullOrEmpty(response)) response = client.ReceiveString();

                Envelope env = null;
                XmlSerializer serial = new XmlSerializer(typeof(Envelope));
                using (StringReader reader = new StringReader(response))
                {
                    env = serial.Deserialize(reader) as Envelope;
                }
                if (env == null || env.Body == null) return null;

                if (!String.IsNullOrEmpty(env.Body.Fault)) throw env.Body.ThrowException();

                return env.Body.Xml;
            }
            finally { client.Dispose(); }
        }
        #endregion

        #region 辅助函数
        /// <summary>
        /// 是否缓存网关。缓存网关可以加速UPnP的发现过程
        /// </summary>
        public static Boolean CacheGateway
        {
            get
            {
                return Config.GetConfig<Boolean>("NewLife.Net.UPnP.CacheGateway");
            }
        }

        /// <summary>
        /// 序列化请求
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="prefix"></param>
        /// <param name="ns"></param>
        /// <returns></returns>
        public static String SerialRequest(Object obj, String prefix, String ns)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement(prefix, obj.GetType().Name, ns);
            doc.AppendChild(root);

            TypeX tx = TypeX.Create(obj.GetType());
            foreach (PropertyInfoX item in tx.Properties)
            {
                XmlElement elm = doc.CreateElement(item.Property.Name);
                Object v = item.GetValue(obj);
                String str = v == null ? "" : v.ToString();

                XmlText text = doc.CreateTextNode(str);
                elm.AppendChild(text);

                root.AppendChild(elm);
            }

            return doc.InnerXml;

            //XmlRootAttribute att = new XmlRootAttribute();
            //att.Namespace = ns;

            //XmlAttributes atts = new XmlAttributes();
            //atts.XmlRoot = att;

            //XmlAttributeOverrides ovs = new XmlAttributeOverrides();
            //ovs.Add(obj.GetType(), atts);

            ////atts = new XmlAttributes();
            ////XmlElementAttribute att2 = new XmlElementAttribute();
            ////att2.Namespace = null;
            ////atts.XmlElements.Add(att2);
            ////ovs.Add(typeof(Int32), atts);

            ////atts = new XmlAttributes();
            ////att2 = new XmlElementAttribute();
            ////att2.Namespace = null;
            ////atts.XmlElements.Add(att2);
            ////ovs.Add(typeof(String), atts);

            //XmlSerializer serial = new XmlSerializer(obj.GetType(), ovs);
            //using (MemoryStream stream = new MemoryStream())
            //{
            //    XmlWriterSettings setting = new XmlWriterSettings();
            //    setting.Encoding = Encoding.UTF8;
            //    // 去掉开头 <?xml version="1.0" encoding="utf-8"?>
            //    setting.OmitXmlDeclaration = true;

            //    using (XmlWriter writer = XmlWriter.Create(stream, setting))
            //    {
            //        XmlSerializerNamespaces xsns = new XmlSerializerNamespaces();
            //        xsns.Add(prefix, ns);
            //        serial.Serialize(writer, obj, xsns);

            //        byte[] bts = stream.ToArray();
            //        String xml = Encoding.UTF8.GetString(bts);

            //        if (!String.IsNullOrEmpty(xml)) xml = xml.Trim();

            //        return xml;
            //    }
            //}
        }
        #endregion
    }
}