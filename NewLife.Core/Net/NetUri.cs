using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Xml.Serialization;

namespace NewLife.Net
{
    /// <summary>协议类型</summary>
    public enum NetType : Byte
    {
        /// <summary>未知协议</summary>
        Unknown = 0,

        /// <summary>传输控制协议</summary>
        Tcp = 6,

        /// <summary>用户数据报协议</summary>
        Udp = 17,

        /// <summary>Http协议</summary>
        Http = 80,

        /// <summary>Https协议</summary>
        Https = 43,

        /// <summary>WebSocket协议</summary>
        WebSocket = 81
    }

    /// <summary>网络资源标识，指定协议、地址、端口、地址族（IPv4/IPv6）</summary>
    /// <remarks>
    /// 仅序列化<see cref="Type"/>和<see cref="EndPoint"/>，其它均是配角！
    /// 有可能<see cref="Host"/>代表主机域名，而<see cref="Address"/>指定主机IP地址。
    /// </remarks>
    public class NetUri
    {
        #region 属性
        /// <summary>协议类型</summary>
        public NetType Type { get; set; }

        /// <summary>主机</summary>
        public String Host { get; set; }

        /// <summary>地址</summary>
        [XmlIgnore]
        public IPAddress Address { get { return EndPoint.Address; } set { EndPoint.Address = value; } }

        /// <summary>端口</summary>
        public Int32 Port { get { return EndPoint.Port; } set { EndPoint.Port = value; } }

        [NonSerialized]
        private IPEndPoint _EndPoint;
        /// <summary>终结点</summary>
        [XmlIgnore]
        public IPEndPoint EndPoint
        {
            get
            {
                var ep = _EndPoint;
                if (ep == null) ep = _EndPoint = new IPEndPoint(IPAddress.Any, 0);
                if ((ep.Address == null || ep.Address.IsAny()) && !Host.IsNullOrEmpty()) ep.Address = NetHelper.ParseAddress(Host) ?? IPAddress.Any;

                return ep;
            }
            set
            {
                var ep = _EndPoint = value;
                if (ep != null)
                    Host = ep.Address + "";
                else
                    Host = null;
            }
        }
        #endregion

        #region 扩展属性
        /// <summary>是否Tcp协议</summary>
        [XmlIgnore]
        public Boolean IsTcp => Type == NetType.Tcp;

        /// <summary>是否Udp协议</summary>
        [XmlIgnore]
        public Boolean IsUdp => Type == NetType.Udp;
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public NetUri() { }

        /// <summary>实例化</summary>
        /// <param name="uri"></param>
        public NetUri(String uri) => Parse(uri);

        /// <summary>实例化</summary>
        /// <param name="protocol"></param>
        /// <param name="endpoint"></param>
        public NetUri(NetType protocol, IPEndPoint endpoint)
        {
            Type = protocol;
            _EndPoint = endpoint;
        }

        /// <summary>实例化</summary>
        /// <param name="protocol"></param>
        /// <param name="address"></param>
        /// <param name="port"></param>
        public NetUri(NetType protocol, IPAddress address, Int32 port)
        {
            Type = protocol;
            Address = address;
            Port = port;
        }

        /// <summary>实例化</summary>
        /// <param name="protocol"></param>
        /// <param name="host"></param>
        /// <param name="port"></param>
        public NetUri(NetType protocol, String host, Int32 port)
        {
            Type = protocol;
            Host = host;
            Port = port;
        }
        #endregion

        #region 方法
        static readonly String Sep = "://";

        /// <summary>分析</summary>
        /// <param name="uri"></param>
        public NetUri Parse(String uri)
        {
            if (uri.IsNullOrWhiteSpace()) return this;

            // 分析协议
            var protocol = "";
            var p = uri.IndexOf(Sep);
            if (p >= 0)
            {
                protocol = uri.Substring(0, p);
                Type = ParseType(protocol);
                uri = uri.Substring(p + Sep.Length);
            }

            _EndPoint = null;

            // 特殊协议端口
            switch (protocol.ToLower())
            {
                case "http":
                case "ws":
                    Port = 80;
                    break;
                case "https":
                case "wss":
                    Port = 443;
                    break;
            }

            // 这个可能是一个Uri，去掉尾部
            p = uri.IndexOf('/');
            if (p < 0) p = uri.IndexOf('\\');
            if (p < 0) p = uri.IndexOf('?');
            if (p >= 0) uri = uri.Substring(0, p);

            // 分析端口
            p = uri.LastIndexOf(":");
            if (p >= 0)
            {
                var pt = uri.Substring(p + 1);
                if (Int32.TryParse(pt, out var port))
                {
                    Port = port;
                    uri = uri.Substring(0, p);
                }
            }

            Host = uri;

            return this;
        }

        private static NetType ParseType(String value)
        {
            if (value.IsNullOrEmpty()) return NetType.Unknown;

            try
            {
                if (value.EqualIgnoreCase("Http", "Https")) return NetType.Http;
                if (value.EqualIgnoreCase("ws", "wss")) return NetType.WebSocket;

                return (NetType)(Int32)Enum.Parse(typeof(ProtocolType), value, true);
            }
            catch { return NetType.Unknown; }
        }
        #endregion

        #region 辅助
        /// <summary>分析地址</summary>
        /// <param name="hostname">主机地址</param>
        /// <returns></returns>
        public static IPAddress ParseAddress(String hostname)
        {
            if (hostname.IsNullOrEmpty()) return null;
            if (hostname == "*") return null;

            try
            {
                if (IPAddress.TryParse(hostname, out var addr)) return addr;

                var hostAddresses = Dns.GetHostAddresses(hostname);
                if (hostAddresses == null || hostAddresses.Length < 1) return null;

                return hostAddresses.FirstOrDefault(d => d.AddressFamily == AddressFamily.InterNetwork || d.AddressFamily == AddressFamily.InterNetworkV6);
            }
            catch (SocketException ex)
            {
                throw new XException("解析主机" + hostname + "的地址失败！" + ex.Message, ex);
            }
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString()
        {
            var p = Type + "";
            switch (Type)
            {
                case NetType.Unknown:
                    p = "";
                    break;
                case NetType.WebSocket:
                    p = Port == 443 ? "wss" : "ws";
                    break;
            }
            var host = Host;
            if (host.IsNullOrEmpty()) host = Address + "";

            if (Port > 0)
                return String.Format("{0}://{1}:{2}", p, host, Port);
            else
                return String.Format("{0}://{1}", p, host);
        }
        #endregion

        #region 重载运算符
        /// <summary>重载类型转换，字符串直接转为NetUri对象</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static implicit operator NetUri(String value) => new NetUri(value);
        #endregion
    }
}