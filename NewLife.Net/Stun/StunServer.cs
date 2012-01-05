using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using NewLife.Net.Sockets;
using NewLife.Reflection;

namespace NewLife.Net.Stun
{
    /// <summary>Stun服务端。Simple Traversal of UDP over NATs，NAT 的UDP简单穿越。RFC 3489</summary>
    /// <remarks>
    /// <a href="http://baike.baidu.com/view/884586.htm">STUN</a>
    /// </remarks>
    public class StunServer : NetServer
    {
        #region 属性
        private IDictionary<Int32, IPEndPoint> _Public;
        /// <summary>我的公网地址。因为当前服务器可能在内网中，需要调用StunClient拿公网地址</summary>
        public IDictionary<Int32, IPEndPoint> Public { get { return _Public; } private set { _Public = value; } }

        private IPEndPoint _Partner;
        /// <summary>伙伴地址。需要改变地址时，向该伙伴地址发送信息</summary>
        public IPEndPoint Partner { get { return _Partner; } set { _Partner = value; } }

        private Int32 _Port2;
        /// <summary>第二端口</summary>
        public Int32 Port2 { get { return _Port2; } set { _Port2 = value; } }
        #endregion

        #region 构造
        /// <summary>
        /// 实例化
        /// </summary>
        public StunServer()
        {
            Port = 3478;
            Port2 = Port + 1;

            //// 同时两个端口
            //AddServer(IPAddress.Any, 3478, ProtocolType.Udp, AddressFamily.InterNetwork);
            //AddServer(IPAddress.Any, 3479, ProtocolType.Udp, AddressFamily.InterNetwork);
        }

        /// <summary>确保建立服务器</summary>
        protected override void EnsureCreateServer()
        {
            if (Servers.Count <= 0)
            {
                // 同时两个端口
                AddServer(IPAddress.Any, Port, ProtocolType.Unknown, AddressFamily.InterNetwork);
                AddServer(IPAddress.Any, Port2, ProtocolType.Unknown, AddressFamily.InterNetwork);

                var dic = new Dictionary<Int32, IPEndPoint>();
                WriteLog("获取公网地址……");
                IPAddress pub = null;
                foreach (var item in Servers)
                {
                    if (item.ProtocolType == ProtocolType.Tcp) continue;

                    var ep = StunClient.GetPublic(item.ProtocolType, item.Port, 2000);
                    pub = ep.Address;
                    WriteLog("{0}://{1}:{2}的公网地址：{3}", item.ProtocolType, item.Address, item.Port, ep);
                    dic.Add(item.Port, ep);
                }
                // Tcp没办法获取公网地址，只能通过Udp获取到的公网地址加上端口形成，所以如果要使用Tcp，服务器必须拥有独立公网地址
                foreach (var item in Servers)
                {
                    if (item.ProtocolType != ProtocolType.Tcp) continue;

                    var ep = new IPEndPoint(pub, item.Port);
                    WriteLog("{0}://{1}:{2}的公网地址：{3}", item.ProtocolType, item.Address, item.Port, ep);
                    dic.Add(item.Port + 100000, ep);
                }
                //var ep = StunClient.GetPublic(Port, 2000);
                //WriteLog("端口{0}的公网地址：{1}", Port, ep);
                //dic.Add(Port, ep);
                //ep = StunClient.GetPublic(Port2, 2000);
                //WriteLog("端口{0}的公网地址：{1}", Port2, ep);
                //dic.Add(Port2, ep);
                WriteLog("成功获取公网地址！");
                Public = dic;
            }
        }
        #endregion

        #region 方法
        /// <summary>接收到数据时</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnReceived(object sender, NetEventArgs e)
        {
            base.OnReceived(sender, e);

            if (e.BytesTransferred > 0)
            {
                var session = e.Socket as ISocketSession;
                IPEndPoint remote = e.RemoteIPEndPoint;

                var request = StunMessage.Read(e.GetStream());
                WriteLog("{0}://{1} {2}{3}{4}", session.ProtocolType, remote, request.Type, request.ChangeIP ? " ChangeIP" : "", request.ChangePort ? " ChangePort" : "");

                // 如果是响应，说明是兄弟服务器发过来的，需要重新修改为请求
                switch (request.Type)
                {
                    case StunMessageType.BindingResponse:
                        request.Type = StunMessageType.BindingRequest;
                        remote = request.ResponseAddress;
                        break;
                    case StunMessageType.SharedSecretResponse:
                        request.Type = StunMessageType.SharedSecretRequest;
                        remote = request.ResponseAddress;
                        break;
                    default:
                        break;
                }

                // 是否需要发给伙伴
                if (request.ChangeIP)
                {
                    if (Partner != null && !Partner.Equals(session.GetRelativeEndPoint(Partner.Address)))
                    {
                        // 发给伙伴
                        request.ChangeIP = false;
                        // 记住对方的地址
                        request.ResponseAddress = remote;
                        session.Send(request.GetStream(), Partner);
                        return;
                    }
                    // 如果没有伙伴地址，采用不同端口代替
                    request.ChangePort = true;
                }

                // 开始分流处理
                switch (request.Type)
                {
                    case StunMessageType.BindingRequest:
                        OnBind(request, session, remote);
                        break;
                    case StunMessageType.SharedSecretRequest:
                        break;
                    default:
                        break;
                }
            }
        }
        #endregion

        #region 绑定
        /// <summary>绑定</summary>
        /// <param name="request"></param>
        /// <param name="session"></param>
        /// <param name="remote"></param>
        /// <returns></returns>
        protected void OnBind(StunMessage request, ISocketSession session, IPEndPoint remote)
        {
            var rs = new StunMessage();
            rs.Type = StunMessageType.BindingResponse;
            rs.TransactionID = request.TransactionID;
            rs.MappedAddress = remote;
            //rs.SourceAddress = session.GetRelativeEndPoint(remote.Address);
            if (session.ProtocolType == ProtocolType.Tcp)
                rs.SourceAddress = Public[session.Port + 100000];
            else
                rs.SourceAddress = Public[session.Port];

            // 找另一个
            ISocketSession session2 = null;
            Int32 anotherPort = 0;
            for (int i = 0; i < Servers.Count; i++)
            {
                var server = Servers[i];
                if (server.ProtocolType == session.ProtocolType && server.LocalEndPoint.Port != session.LocalEndPoint.Port)
                {
                    anotherPort = server.Port;
                    if (server.ProtocolType == ProtocolType.Tcp)
                    {
                        break;
                    }
                    else
                    {
                        session2 = server as ISocketSession;
                        if (session2 != null) break;
                    }
                }
            }
            //rs.ChangedAddress = Partner ?? session2.GetRelativeEndPoint(remote.Address);
            if (session.ProtocolType == ProtocolType.Tcp)
                rs.ChangedAddress = Partner ?? Public[anotherPort + 100000];
            else
                rs.ChangedAddress = Partner ?? Public[anotherPort];

            String name = Name;
            if (name == this.GetType().Name) name = this.GetType().FullName;
            rs.ServerName = String.Format("{0} v{1}", name, AssemblyX.Create(Assembly.GetExecutingAssembly()).CompileVersion);

            // 换成另一个
            if (request.ChangePort) session = session2;

            session.Send(rs.GetStream(), remote);
        }
        #endregion

        #region 辅助
        static Boolean IsResponse(StunMessageType type)
        {
            return ((UInt16)type & 0x0100) != 0;
        }
        #endregion
    }
}