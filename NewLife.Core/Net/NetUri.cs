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
        private NetType _Type;
        /// <summary>协议类型</summary>
        public NetType Type { get { return _Type; } set { _Type = value; _Protocol = value.ToString(); } }

        [NonSerialized]
        private String _Protocol;
        /// <summary>协议</summary>
        [XmlIgnore]
        public String Protocol
        {
            get { return _Protocol; }
            set
            {
                _Protocol = value;
                if (String.IsNullOrEmpty(value))
                    _Type = NetType.Unknown;
                else
                {
                    try
                    {
                        if (value.EqualIgnoreCase("Http", "Https"))
                            _Type = NetType.Http;
                        else if (value.EqualIgnoreCase("ws", "wss"))
                            _Type = NetType.WebSocket;
                        else
                        {
                            _Type = (NetType)(Int32)Enum.Parse(typeof(ProtocolType), value, true);
                            // 规范化名字
                            _Protocol = _Type.ToString();
                        }
                    }
                    catch { _Type = NetType.Unknown; }
                }
            }
        }

        /// <summary>地址</summary>
        [XmlIgnore]
        public IPAddress Address { get { return EndPoint.Address; } set { EndPoint.Address = value; _Host = value + ""; } }

        private String _Host;
        /// <summary>主机</summary>
        public String Host { get { return _Host; } set { _Host = value; _EndPoint = null; /*只清空，避免这里耗时 try { EndPoint.Address = ParseAddress(value); } catch { }*/ } }

        private Int32 _Port;
        /// <summary>端口</summary>
        public Int32 Port { get { return _Port = EndPoint.Port; } set { _Port = EndPoint.Port = value; } }

        [NonSerialized]
        private IPEndPoint _EndPoint;
        /// <summary>终结点</summary>
        [XmlIgnore]
        public IPEndPoint EndPoint
        {
            get
            {
                // Host每次改变都会清空
                if (_EndPoint == null) _EndPoint = new IPEndPoint(ParseAddress(_Host) ?? IPAddress.Any, _Port);
                return _EndPoint;
            }
            set
            {
                // 考虑到序列化问题，Host可能是域名，而Address只是地址
                _EndPoint = value;
                if (value != null)
                {
                    _Host = _EndPoint.Address + "";
                    _Port = _EndPoint.Port;
                }
                else
                {
                    _Host = null;
                    _Port = 0;
                }
            }
        }
        #endregion

        #region 扩展属性
        /// <summary>是否Tcp协议</summary>
        [XmlIgnore]
        public Boolean IsTcp { get { return Type == NetType.Tcp; } }

        /// <summary>是否Udp协议</summary>
        [XmlIgnore]
        public Boolean IsUdp { get { return Type == NetType.Udp; } }
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public NetUri() { }

        /// <summary>实例化</summary>
        /// <param name="uri"></param>
        public NetUri(String uri) { Parse(uri); }

        /// <summary>实例化</summary>
        /// <param name="protocol"></param>
        /// <param name="endpoint"></param>
        public NetUri(NetType protocol, IPEndPoint endpoint)
        {
            Type = protocol;
            EndPoint = endpoint;
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
            var p = uri.IndexOf(Sep);
            if (p >= 0)
            {
                Protocol = uri.Substring(0, p);
                uri = uri.Substring(p + Sep.Length);
            }

            // 特殊协议端口
            switch (Protocol.ToLower())
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
                var port = 0;
                if (Int32.TryParse(pt, out port))
                {
                    Port = port;
                    uri = uri.Substring(0, p);
                }
            }

            Host = uri;

            return this;
        }
        #endregion

        #region 辅助
        /// <summary>分析地址</summary>
        /// <param name="hostname">主机地址</param>
        /// <returns></returns>
        public static IPAddress ParseAddress(String hostname)
        {
            if (String.IsNullOrEmpty(hostname)) return null;

            try
            {
                IPAddress addr = null;
                if (IPAddress.TryParse(hostname, out addr)) return addr;

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
            var p = Protocol;
            switch (Type)
            {
                case NetType.Unknown:
                    p = "";
                    break;
                case NetType.WebSocket:
                    p = Port == 443 ? "wss" : "ws";
                    break;
            }
            if (Port > 0)
                return String.Format("{0}://{1}:{2}", p, Host, Port);
            else
                return String.Format("{0}://{1}", p, Host);
        }
        #endregion

        #region 重载运算符
        /// <summary>重载类型转换，字符串直接转为NetUri对象</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static implicit operator NetUri(String value)
        {
            return new NetUri(value);
        }
        #endregion
    }
}