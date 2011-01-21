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
        #endregion

        #region 构造
        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);


        }
        #endregion

        #region 发现
        /// <summary>
        /// 开始
        /// </summary>
        public void Discover()
        {
            UdpClientX Udp = new UdpClientX();
            IPAddress address = NetHelper.ParseAddress("239.255.255.250");
            IPEndPoint remoteEP = new IPEndPoint(address, 1900);

            // 设置多播
            Socket socket = Udp.Client;
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 4);
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, 1);
            MulticastOption optionValue = new MulticastOption(remoteEP.Address);
            try
            {
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, optionValue);
                //socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, (int)this.boundto.Address.Address);
            }
            catch (Exception)
            {
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("M-SEARCH * HTTP/1.1");
            sb.AppendLine("HOST: 239.255.255.250:1900");
            sb.AppendLine("MAN: \"ssdp:discover\"");
            sb.AppendLine("MX: 3");
            sb.AppendLine("ST: UPnPClient:rootdevice");
            sb.AppendLine();
            sb.AppendLine();

            byte[] data = Encoding.ASCII.GetBytes(sb.ToString());
            Udp.Client.EnableBroadcast = true;
            Udp.Send(data, remoteEP);

            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DropMembership, optionValue);

            String xml = Udp.ReceiveString();
            if (String.IsNullOrEmpty(xml)) return;

            //分析数据并反序列化
            DownLoadAndSerializer(xml);
        }

        /// <summary>
        /// 返回信息分析,反序列化IGD.XML
        /// </summary>
        /// <param name="html"></param>
        void DownLoadAndSerializer(String html)
        {
            //取Location
            String sp = "LOCATION:";
            Int32 p = html.IndexOf(sp);
            if (p <= 0) return;

            String url = html.Substring(p + sp.Length);
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

                //反序列化
                XmlSerializer serial = new XmlSerializer(typeof(InternetGatewayDevice));
                StringReader reader = new StringReader(xml);
                IGD = serial.Deserialize(reader) as InternetGatewayDevice;
                //如果
                if (String.IsNullOrEmpty(IGD.URLBase)) IGD.URLBase = url;
            }
            catch (Exception e)
            {
                XTrace.WriteLine(e.Message + " 路径[" + url + "]");
                throw;
            }
        }
        #endregion

        ///// <summary>
        ///// 查找UPNP
        ///// </summary>
        ///// <returns></returns>
        //public static void Search()
        //{
        //    if (IGD != null) return;

        //    byte[] data = Encoding.ASCII.GetBytes(XMLCommand.UPnPSearch());
        //    byte[] buffer = null;
        //    UdpClient Client = new UdpClient(0);
        //    Client.EnableBroadcast = true;
        //    Client.Send(data, data.Length, "239.255.255.250", 1900);
        //    IPEndPoint ip = new IPEndPoint(IPAddress.Any, 0);
        //    for (Int32 i = 1; i <= 10; i++)
        //    {
        //        if (Client.Available > 0)
        //        {
        //            buffer = Client.Receive(ref ip);
        //            break;
        //        }
        //        //等待10毫秒
        //        Thread.Sleep(10);
        //    }

        //    Client.Close();

        //    if (buffer == null)
        //    {
        //        //XLog.XTrace.WriteLine("获取UPNP失败,没有接收到返回信息!");
        //        throw new Exception("获取UPNP失败,没有接收到返回信息!");
        //    }

        //    //分析数据并反序列化
        //    DownLoadAndSerializer(Encoding.UTF8.GetString(buffer));

        //}

        #region 操作
        /// <summary>
        /// 添加映射端口
        /// </summary>
        /// <param name="RemoteHost">远程主机</param>
        /// <param name="ExternalPort">外部端口</param>
        /// <param name="Protocol">TCP或UDP</param>
        /// <param name="InternalPort">内部端口</param>
        /// <param name="InternalClient">本地IP地址</param>
        /// <param name="Enabled">是否启用[0,1]</param>
        /// <param name="Description">端口映射的描述</param>
        /// <param name="Duration">映射的持续时间，用0表示不永久</param>
        /// <returns>bool</returns>
        public bool Add(String RemoteHost, Int32 ExternalPort, String Protocol, Int32 InternalPort, String InternalClient, Int32 Enabled, String Description, int? Duration)
        {
            if (IsPortCheck == true && GetMapByPortAndProtocol(null, ExternalPort, Protocol) != null)
            {
                XTrace.WriteLine(ExternalPort + "端口被占用");
                return false;
            }
            String Command = XMLCommand.Add(RemoteHost, ExternalPort, Protocol, InternalPort, InternalClient, Enabled, Description, Duration);
            return SOAPRequest(Command);
        }

        /// <summary>
        /// 删除端口映射
        /// </summary>
        /// <param name="RemoteHost">远程主机</param>
        /// <param name="ExternalPort">外部端口</param>
        /// <param name="Protocol">TCP或UDP</param>
        /// <returns></returns>
        public bool Del(String RemoteHost, Int32 ExternalPort, String Protocol)
        {
            String Command = XMLCommand.Del(RemoteHost, ExternalPort, Protocol);
            return SOAPRequest(Command);
        }
        #endregion

        #region 查找
        /// <summary>
        /// 获取端口映射信息
        /// </summary>
        /// <param name="RemoteHost">远程主机</param>
        /// <param name="ExternalPort">外部端口</param>
        /// <param name="Protocol">TCP/UDP</param>
        /// <returns></returns>
        public PortMappingEntry GetMapByPortAndProtocol(String RemoteHost, Int32 ExternalPort, String Protocol)
        {
            String header = null;
            String xml = null;
            String cmd = XMLCommand.GetMapByPortAndProtocol(RemoteHost, ExternalPort, Protocol);

            if (!SOAPRequest(cmd, out header, out xml)) return null;

            //转为XML
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            //设置命名空间
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("B", "http://schemas.xmlsoap.org/soap/envelope/");

            //查询Body节点
            XmlNode body = doc.SelectSingleNode("//B:Body", nsmgr);
            if (body == null) return null;

            Int32 ConvertInt;
            PortMappingEntry entity = new PortMappingEntry();
            entity.NewRemoteHost = RemoteHost;
            entity.NewExternalPort = ExternalPort;
            entity.NewProtocol = Protocol;

            XmlNode node = body.SelectSingleNode("//NewInternalPort");
            if (node != null && Int32.TryParse(node.InnerText, out ConvertInt)) entity.NewInternalPort = ConvertInt;

            node = body.SelectSingleNode("//NewInternalClient");
            if (node != null) entity.NewInternalClient = node.InnerText;

            node = body.SelectSingleNode("//NewEnabled");
            if (node != null && Int32.TryParse(node.InnerText, out ConvertInt)) entity.NewEnabled = ConvertInt;

            node = body.SelectSingleNode("//NewPortMappingDescription");
            if (node != null) entity.NewPortMappingDescription = node.InnerText;

            node = body.SelectSingleNode("//NewLeaseDuration");
            if (node != null && Int32.TryParse(node.InnerText, out ConvertInt)) entity.NewLeaseDuration = ConvertInt;

            //XmlSerializer serial = new XmlSerializer(typeof(Envelope));
            //StringReader reader = new StringReader(Document);
            //Envelope PME = serial.Deserialize(reader) as Envelope;
            return entity;
        }

        /// <summary>
        /// 获取端口映射信息
        /// </summary>
        /// <param name="index">索引</param>
        /// <returns></returns>
        public PortMappingEntry GetMapByIndex(Int32 index)
        {
            String Header = null;
            String Document = null;

            //String Command = XMLCommand.GetMapByIndex(PortMappingIndex);
            String Xmlns = IGD.device.deviceList[0].deviceList[0].serviceList[0].serviceType;
            //StringBuilder sb = new StringBuilder();
            //sb.AppendLine("<u:GetGenericPortMappingEntry xmlns:u= \"" + Xmlns + "\">");
            //sb.AppendLine("<NewPortMappingIndex>" + index + "</NewPortMappingIndex>");
            //sb.AppendLine("<NewRemoteHost></NewRemoteHost>");
            //sb.AppendLine("<NewExternalPort></NewExternalPort>");
            //sb.AppendLine("<NewProtocol></NewProtocol>");
            //sb.AppendLine("<NewInternalPort></NewInternalPort>");
            //sb.AppendLine("<NewInternalClient></NewInternalClient>");
            //sb.AppendLine("<NewEnabled></NewEnabled>");
            //sb.AppendLine("<NewPortMappingDescription></NewPortMappingDescription>");
            //sb.AppendLine("<NewLeaseDuration></NewLeaseDuration>");
            //sb.AppendLine("</u:GetGenericPortMappingEntry>");

            PortMappingEntryClient entry = new PortMappingEntryClient();
            entry.NewPortMappingIndex = index;
            String Command = SerialRequest(entry, "u", Xmlns);

            if (!SOAPRequest(Command, out Header, out Document)) return null;

            //转为XML
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(Document);

            //设置命名空间
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xml.NameTable);
            nsmgr.AddNamespace("B", "http://schemas.xmlsoap.org/soap/envelope/");

            //查询Body节点
            XmlNodeList BodyNode = xml.SelectNodes("//B:Body", nsmgr);

            if (BodyNode.Count == 0) return null;

            Int32 ConvertInt;
            XmlNodeList Child;
            PortMappingEntry PM = new PortMappingEntry();
            Child = BodyNode[0].SelectNodes("//NewRemoteHost");
            if (Child.Count > 0) PM.NewRemoteHost = Child[0].InnerText;
            Child = BodyNode[0].SelectNodes("//NewExternalPort");
            if (Child.Count > 0 && Int32.TryParse(Child[0].InnerText, out ConvertInt)) PM.NewExternalPort = ConvertInt;
            Child = BodyNode[0].SelectNodes("//NewProtocol");
            if (Child.Count > 0) PM.NewProtocol = Child[0].InnerText;
            Child = BodyNode[0].SelectNodes("//NewInternalPort");
            if (Child.Count > 0 && Int32.TryParse(Child[0].InnerText, out ConvertInt)) PM.NewInternalPort = ConvertInt;
            Child = BodyNode[0].SelectNodes("//NewInternalClient");
            if (Child.Count > 0) PM.NewInternalClient = Child[0].InnerText;
            Child = BodyNode[0].SelectNodes("//NewEnabled");
            if (Child.Count > 0 && Int32.TryParse(Child[0].InnerText, out ConvertInt)) PM.NewEnabled = ConvertInt;
            Child = BodyNode[0].SelectNodes("//NewPortMappingDescription");
            if (Child.Count > 0) PM.NewPortMappingDescription = Child[0].InnerText;
            Child = BodyNode[0].SelectNodes("//NewLeaseDuration");
            if (Child.Count > 0 && Int32.TryParse(Child[0].InnerText, out ConvertInt)) PM.NewLeaseDuration = ConvertInt;

            //XmlSerializer serial = new XmlSerializer(typeof(Envelope));
            //StringReader reader = new StringReader(Document);
            //Envelope PME = serial.Deserialize(reader) as Envelope;
            return PM;
        }

        //上次获取的时间
        private static DateTime GetAllMapDate;
        private static Int32 GetAllMaxSeconds = 60;
        private static List<PortMappingEntry> _GetMapByIndexAll;

        /// <summary>
        /// 获取所有端口映射信息
        /// </summary>
        /// <returns></returns>
        public List<PortMappingEntry> GetMapByIndexAll()
        {
            if (GetAllMapDate.AddSeconds(GetAllMaxSeconds) >= DateTime.Now) return _GetMapByIndexAll;
            GetAllMapDate = DateTime.Now;
            Int32 i = 0;
            List<PortMappingEntry> Return = new List<PortMappingEntry>();
            PortMappingEntry Item;
            while (true)
            {
                Item = GetMapByIndex(i);
                if (Item == null) break;
                Return.Add(Item);
                i++;
            }
            _GetMapByIndexAll = Return;
            return _GetMapByIndexAll;
        }

        public bool SOAPRequest(String Command)
        {
            String Header = null;
            String Document = null;
            return SOAPRequest(Command, out Header, out Document);
        }

        /// <summary>
        /// 发送请求
        /// </summary>
        /// <param name="Command">请求</param>
        /// <param name="Header">返回Header</param>
        /// <param name="Document">返回正文</param>
        /// <returns></returns>
        public bool SOAPRequest(String Command, out String Header, out String Document)
        {
            TcpClient Client = new TcpClient(IGD.ServerHOST, IGD.ServerPort);
            NetworkStream Stream = Client.GetStream();
            byte[] data = Encoding.ASCII.GetBytes(Command);
            Stream.Write(data, 0, data.Length);
            byte[] buffer = new byte[1024];
            Int32 Count;
            String Read;
            Header = null;
            Document = null;
            for (Int32 i = 1; i <= 10; i++)
            {
                if (Stream.DataAvailable == true)
                {
                    Count = Stream.Read(buffer, 0, buffer.Length);
                    Read = Encoding.ASCII.GetString(buffer, 0, Count);
                    if (Read.IndexOf("200 OK") > -1)
                    {
                        //out
                        Int32 IndexOf = Read.IndexOf("\r\n\r\n");
                        Header = Read.Substring(0, IndexOf);
                        Document = Read.Substring(IndexOf + 4, Read.Length - IndexOf - 4);
                        return true;
                    }
                    else
                        return false;
                }
                //等待数据
                Thread.Sleep(100);
            }

            //超时
            return false;
        }
        #endregion

        #region 辅助函数
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