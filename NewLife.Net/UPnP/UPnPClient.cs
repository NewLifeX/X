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

namespace NewLife.Net.UPnP
{
    public class UPnPClient
    {
        public static String Location = null;
        public static IGD IGD = null;
        public static Int32 SleepTime = 0;
        public static String IGDXML;
        //映射前是否检查端口
        public static bool IsPortCheck = true;


        /// <summary>
        /// 查找UPNP
        /// </summary>
        /// <returns></returns>
        public static void Search()
        {
            //Thread thread = new Thread(new ThreadStart(UdpAction));

            if (IGD != null) return;

            byte[] data = Encoding.ASCII.GetBytes(XMLCommand.UPnPSearch());
            byte[] buffer = null;
            UdpClient Client = new UdpClient(0);
            Client.EnableBroadcast = true;
            Client.Send(data, data.Length, "239.255.255.250", 1900);
            IPEndPoint ip = new IPEndPoint(IPAddress.Any, 0);
            for (Int32 i = 1; i <= 10; i++)
            {
                if (Client.Available > 0)
                {
                    buffer = Client.Receive(ref ip);
                    break;
                }
                //等待10毫秒
                Thread.Sleep(10);
            }

            Client.Close();

            if (buffer == null)
            {
                //XLog.XTrace.WriteLine("获取UPNP失败,没有接收到返回信息!");
                throw new Exception("获取UPNP失败,没有接收到返回信息!");
            }

            //分析数据并反序列化
            DownLoadAndSerializer(Encoding.UTF8.GetString(buffer));

        }

        /// <summary>
        /// 添加映射端口
        /// </summary>
        /// <param name="RemoteHost">远程主机</param>
        /// <param name="ExternalPort">外部端口</param>
        /// <param name="Protocol">TCP或UDP</param>
        /// <param name="InternalPort">内部端口</param>
        /// <param name="NewInternalClient">本地IP地址</param>
        /// <param name="NewEnabled">是否启用[0,1]</param>
        /// <param name="Description">端口映射的描述</param>
        /// <param name="Duration">映射的持续时间，用0表示不永久</param>
        /// <returns>bool</returns>
        public static bool Add(String RemoteHost, Int32 ExternalPort, String Protocol, Int32 InternalPort, String InternalClient, Int32 Enabled, String Description, int? Duration)
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
        public static bool Del(String RemoteHost, Int32 ExternalPort, String Protocol)
        {
            String Command = XMLCommand.Del(RemoteHost, ExternalPort, Protocol);
            return SOAPRequest(Command);
        }

        /// <summary>
        /// 获取端口映射信息
        /// </summary>
        /// <param name="RemoteHost">远程主机</param>
        /// <param name="ExternalPort">外部端口</param>
        /// <param name="Protocol">TCP/UDP</param>
        /// <returns></returns>
        public static PortMappingEntry GetMapByPortAndProtocol(String RemoteHost, Int32 ExternalPort, String Protocol)
        {

            String Header = null;
            String Document = null;

            String Command = XMLCommand.GetMapByPortAndProtocol(RemoteHost, ExternalPort, Protocol);

            if (SOAPRequest(Command, out Header, out Document) == false)
                return null;

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
            PM.NewRemoteHost = RemoteHost;
            PM.NewExternalPort = ExternalPort;
            PM.NewProtocol = Protocol;
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

        /// <summary>
        /// 获取端口映射信息
        /// </summary>
        /// <param name="PortMappingIndex">索引</param>
        /// <returns></returns>
        public static PortMappingEntry GetMapByIndex(Int32 PortMappingIndex)
        {

            String Header = null;
            String Document = null;

            String Command = XMLCommand.GetMapByIndex(PortMappingIndex);

            if (SOAPRequest(Command, out Header, out Document) == false)
                return null;

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
        public static List<PortMappingEntry> GetMapByIndexAll()
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

        public static bool SOAPRequest(String Command)
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
        public static bool SOAPRequest(String Command, out String Header, out String Document)
        //public static bool SOAPRequest(String Command)
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


        /// <summary>
        /// 测试,无查找,直接下载
        /// </summary>
        public static void Test_DownLoadAndSerializer()
        {
            try
            {

                //下载IGD.XML
                WebClient client = new WebClient();
                IGDXML = client.DownloadString("http://192.168.1.1:1900/igd.xml");

                //反序列化
                XmlSerializer serial = new XmlSerializer(typeof(IGD));
                StringReader reader = new StringReader(IGDXML);
                IGD = serial.Deserialize(reader) as IGD;
                //如果
                if (String.IsNullOrEmpty(IGD.URLBase)) IGD.URLBase = "http://192.168.1.1:1900/igd.xml";
            }
            catch (Exception e)
            {
                XTrace.WriteLine(e.Message + "路径[" + Location + "]");
                throw e;
            }
        }

        /// <summary>
        /// 返回信息分析,反序列化IGD.XML
        /// </summary>
        /// <param name="state"></param>
        public static void DownLoadAndSerializer(String buffer)
        {

            try
            {
                //取Location
                String sp = "LOCATION:";
                Int32 p = buffer.IndexOf(sp);
                Location = buffer.Substring(p + sp.Length);
                Location = Location.Substring(0, Location.IndexOf(Environment.NewLine));

                //下载IGD.XML
                WebClient client = new WebClient();
                IGDXML = client.DownloadString(Location);

                //反序列化
                XmlSerializer serial = new XmlSerializer(typeof(IGD));
                StringReader reader = new StringReader(IGDXML);
                IGD = serial.Deserialize(reader) as IGD;
                //如果
                if (String.IsNullOrEmpty(IGD.URLBase)) IGD.URLBase = Location;
            }
            catch (Exception e)
            {
                XTrace.WriteLine(e.Message + "路径[" + Location + "]");
                throw e;
            }
        }
    }
}
