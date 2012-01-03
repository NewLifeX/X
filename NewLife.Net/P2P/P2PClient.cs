using System;
using System.Net;
using NewLife.Net.Sockets;
using NewLife.Net.Tcp;

namespace NewLife.Net.P2P
{
    /// <summary>P2P客户端</summary>
    /// <remarks>
    /// Tcp打洞流程（A想连接B）：
    /// 1，客户端A通过路由器NAT-A连接打洞服务器S
    /// 2，A向S发送标识，异步等待响应
    /// 3，S记录A的标识和会话<see cref="ISocketSession"/>
    /// 3，客户端B，从业务通道拿到标识
    /// 4，B通过路由器NAT-B连接打洞服务器S，异步等待响应
    /// 5，B向S发送标识
    /// 6，S找到匹配标识，同时向AB会话响应对方的外网地址，会话结束
    /// 7，AB收到响应，B先连接A，A暂停一会后连接B
    /// </remarks>
    public class P2PClient : Netbase
    {
        #region 属性
        //private String _Identity;
        ///// <summary>标识。打洞服务器以此来标识邀请者与被邀请者，相同即为同一个会话</summary>
        //public String Identity { get { return _Identity; } set { _Identity = value; } }

        private ISocketServer _Server;
        /// <summary>监听服务器</summary>
        public ISocketServer Server { get { return _Server; } set { _Server = value; } }

        private ISocketClient _Client;
        /// <summary>客户端</summary>
        public ISocketClient Client { get { return _Client; } set { _Client = value; } }

        //private ProtocolType _ProtocolType = ProtocolType.Tcp;
        ///// <summary>协议</summary>
        //public ProtocolType ProtocolType { get { return _ProtocolType; } set { _ProtocolType = value; } }

        private IPEndPoint _HoleServer;
        /// <summary>打洞服务器地址</summary>
        public IPEndPoint HoleServer { get { return _HoleServer; } set { _HoleServer = value; } }

        private IPEndPoint _ParterAddress;
        /// <summary>目标伙伴地址</summary>
        public IPEndPoint ParterAddress { get { return _ParterAddress; } set { _ParterAddress = value; } }
        #endregion

        #region 方法
        void EnsureServer()
        {
            if (Server == null)
            {
                var server = new TcpServer();
                Server = server;
                server.ReuseAddress = true;
                server.Accepted += new EventHandler<NetEventArgs>(server_Accepted);

                server.Start();

                WriteLog("监听：{0}", server.LocalEndPoint);
            }
        }

        void server_Accepted(object sender, NetEventArgs e)
        {
            WriteLog("连接到来：{0}", e.RemoteIPEndPoint);

            var session = e.Socket as ISocketSession;
            if (session != null)
            {
                session.Received += new EventHandler<NetEventArgs>(session_Received);
                session.Send("P2P连接已建立！");
                WriteLog("P2P连接已建立！");
            }
        }

        void session_Received(object sender, NetEventArgs e)
        {
            WriteLog("会话数据到来：{0} {1}", e.RemoteIPEndPoint, e.GetString());
        }

        void EnsureClient()
        {
            EnsureServer();
            if (Client == null)
            {
                var server = Server;

                var client = new TcpClientX();
                Client = client;
                client.Address = server.LocalEndPoint.Address;
                client.Port = server.LocalEndPoint.Port;
                client.ReuseAddress = true;
                client.Connect(HoleServer);
                client.Received += new EventHandler<NetEventArgs>(client_Received);
                client.ReceiveAsync();
            }
        }

        void client_Received(object sender, NetEventArgs e)
        {
            WriteLog("数据到来：{0} {1}", e.RemoteIPEndPoint, e.GetString());

            var ss = e.GetString().Split(":");
            if (ss == null || ss.Length < 2) return;

            var ep = new IPEndPoint(IPAddress.Parse(ss[0]), Int32.Parse(ss[1]));
            ParterAddress = ep;

            Client.Dispose();
            var server = Server;

            var client = new TcpClientX();
            Client = client;
            client.Address = server.LocalEndPoint.Address;
            client.Port = server.LocalEndPoint.Port;
            client.ReuseAddress = true;
            Console.WriteLine("准备连接对方：{0}", ep);
            client.Connect(ep);
            client.Received += new EventHandler<NetEventArgs>(client_Received2);
            client.ReceiveAsync();

            client.Send("Hello!");
        }

        void client_Received2(object sender, NetEventArgs e)
        {
            WriteLog("数据到来2：{0} {1}", e.RemoteIPEndPoint, e.GetString());
        }
        #endregion

        #region 业务
        public void Start(String name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            EnsureClient();

            Client.Send("reg:" + name);
        }
        #endregion
    }
}