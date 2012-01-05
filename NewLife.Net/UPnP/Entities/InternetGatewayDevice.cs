using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Text.RegularExpressions;
using System.IO;
using NewLife.Net.Tcp;

namespace NewLife.Net.UPnP
{
    /// <summary>因特网网关设备</summary>
    [Serializable, XmlRoot("root", Namespace = "urn:schemas-upnp-org:device-1-0")]
    public class InternetGatewayDevice
    {
        #region 属性
        private SpecVersion _specVersion;
        /// <summary>UPnP 设备架构版本</summary>
        [XmlElement("specVersion")]
        public SpecVersion specVersion
        {
            get { return _specVersion; }
            set { _specVersion = value; }
        }

        private String _URLBase;
        /// <summary>URL</summary>
        [XmlElement("URLBase")]
        public String URLBase
        {
            get { return _URLBase; }
            set { _URLBase = value; }
        }

        private Device _device;
        /// <summary>设备</summary>
        [XmlElement("device")]
        public Device device
        {
            get { return _device; }
            set { _device = value; }
        }
        #endregion

        #region 扩展属性
        private String _ServerHost;
        /// <summary>UPNP设备IP</summary>
        [XmlIgnore]
        public String ServerHost
        {
            get
            {
                if (String.IsNullOrEmpty(_ServerHost)) GetHOSTAndPort();
                return _ServerHost;

            }
            set { _ServerHost = value; }
        }

        private Int32 _ServerPort;
        /// <summary>UPNP设备端口</summary>
        [XmlIgnore]
        public Int32 ServerPort
        {
            get
            {
                if (_ServerPort == 0) GetHOSTAndPort();
                return _ServerPort;
            }
            set { _ServerPort = value; }
        }
        #endregion

        #region 方法
        /// <summary>
        /// 分析URLBase 并赋值HOST和Port
        /// </summary>
        public void GetHOSTAndPort()
        {
            //Regex Regex = new Regex(@"\d{1,3}.\d{1,3}.\d{1,3}.\d{1,3}:\d{1,5}");
            //String HostAndPortStr = Regex.Match(URLBase).Value;
            //if (!String.IsNullOrEmpty(HostAndPortStr))
            //{
            //    String[] Arr = HostAndPortStr.Split(new Char[] { ':' });
            //    ServerHOST = Arr[0];
            //    ServerPort = Convert.ToInt32(Arr[1]);
            //}
            //else
            //    throw new Exception("UPNP设备IP与端口获取出错!");

            if (String.IsNullOrEmpty(URLBase)) return;

            Uri uri = new Uri(URLBase);
            ServerHost = uri.Host;
            ServerPort = uri.Port;
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            Device d = device;
            if (d != null)
                return String.Format("{0} {1}", d, ServerHost);
            else
                return ServerHost;
        }
        #endregion

        #region 操作
        /// <summary>添加映射端口</summary>
        /// <param name="remoteHost">远程主机</param>
        /// <param name="externalPort">外部端口</param>
        /// <param name="protocol">TCP或UDP</param>
        /// <param name="internalPort">内部端口</param>
        /// <param name="internalClient">本地IP地址</param>
        /// <param name="enabled">是否启用[0,1]</param>
        /// <param name="description">端口映射的描述</param>
        /// <param name="duration">映射的持续时间，用0表示永久</param>
        /// <returns>bool</returns>
        public Boolean Add(String remoteHost, Int32 externalPort, String protocol, Int32 internalPort, String internalClient, Int32 enabled, String description, Int32 duration)
        {
            var entity = new PortMappingEntry();
            entity.Name = "AddPortMapping";
            entity.RemoteHost = remoteHost;
            entity.ExternalPort = externalPort;
            entity.Protocol = protocol;
            entity.InternalClient = internalClient;
            entity.InternalPort = internalPort;
            entity.Enabled = enabled;
            entity.Description = description;
            entity.LeaseDuration = duration;

            var response = Request<PortMappingEntry>(entity);
            return response != null;
        }

        /// <summary>添加映射端口</summary>
        /// <param name="host">本地主机</param>
        /// <param name="port">端口（内外一致）</param>
        /// <param name="protocol">协议</param>
        /// <param name="description">描述</param>
        /// <returns></returns>
        public Boolean Add(String host, Int32 port, String protocol, String description)
        {
            return Add(null, port, protocol, port, host, 1, description, 0);
        }

        /// <summary>删除端口映射</summary>
        /// <param name="remoteHost">远程主机</param>
        /// <param name="externalPort">外部端口</param>
        /// <param name="protocol">TCP或UDP</param>
        /// <returns></returns>
        public Boolean Delete(String remoteHost, Int32 externalPort, String protocol)
        {
            var entity = new PortMappingEntry();
            entity.Name = "DeletePortMapping";
            entity.RemoteHost = remoteHost;
            entity.ExternalPort = externalPort;
            entity.Protocol = protocol;

            var response = Request<PortMappingEntry>(entity);
            return response != null;
        }
        #endregion

        #region 查找
        /// <summary>获取指定设备的端口映射信息</summary>
        /// <param name="remoteHost">远程主机</param>
        /// <param name="externalPort">外部端口</param>
        /// <param name="protocol">TCP/UDP</param>
        /// <returns></returns>
        public PortMappingEntry GetMapByPortAndProtocol(String remoteHost, Int32 externalPort, String protocol)
        {
            PortMappingEntry entity = new PortMappingEntry();
            entity.Name = "GetSpecificPortMappingEntry";
            entity.RemoteHost = remoteHost;
            entity.ExternalPort = externalPort;
            entity.Protocol = protocol;

            PortMappingEntry response = Request<PortMappingEntry>(entity);
            return response;
        }

        /// <summary>获取指定设备的端口映射信息</summary>
        /// <param name="index">索引</param>
        /// <returns></returns>
        public PortMappingEntry GetMapByIndex(Int32 index)
        {
            PortMappingEntryRequest entity = new PortMappingEntryRequest();
            entity.Name = "GetGenericPortMappingEntry";
            entity.NewPortMappingIndex = index;

            PortMappingEntry response = Request<PortMappingEntry>(entity);
            return response;
        }

        /// <summary>获取指定设备的所有端口映射信息</summary>
        /// <returns></returns>
        public IEnumerable<PortMappingEntry> GetMapByIndexAll()
        {
            Int32 n = 0;
            while (true)
            {
                PortMappingEntry item;
                try
                {
                    item = GetMapByIndex(n++);
                }
                catch { break; }

                if (item == null) break;

                yield return item;
            }
        }
        #endregion

        #region SOAP
        /// <summary>向设备发送指令</summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="action"></param>
        /// <returns></returns>
        TResponse Request<TResponse>(UPnPAction action) where TResponse : UPnPAction<TResponse>, new()
        {
            if (device == null || action == null) return null;

            Service service = device.GetWANIPService();
            if (service == null) return null;

            Uri uri = new Uri(String.Format("http://{0}:{1}{2}", ServerHost, ServerPort, service.controlURL));

            String xml = action.ToSoap(service.serviceType);
            xml = SOAPRequest(uri, service.serviceType + "#" + action.Name, xml);

            TResponse response = UPnPAction<TResponse>.FromXml(xml);

            return response;
        }

        /// <summary>SOAP头部</summary>
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

        /// <summary>发送SOAP请求，发送xml，返回xml</summary>
        /// <param name="uri"></param>
        /// <param name="action"></param>
        /// <param name="xml"></param>
        /// <returns></returns>
        static String SOAPRequest(Uri uri, String action, String xml)
        {
            //String body = String.Format(SOAP_BODY, xml);
            String header = String.Format(SOAP_HEADER, uri.PathAndQuery, uri.Host + ":" + uri.Port, action, Encoding.UTF8.GetByteCount(xml));

            var client = new TcpClientX();
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
                using (var reader = new StringReader(response))
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
    }
}