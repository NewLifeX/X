using System;
using System.Net;
using System.Threading;
using NewLife.Net.Sockets;
using NewLife.Net.Tcp;
using NewLife.Net.Udp;
using System.Net.Sockets;

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
    /// 
    /// 经鉴定，我认为网络上所有关于TCP穿透的文章，全部都是在胡扯
    /// 不外乎几种可能：
    /// 1，双方都在同一个内网
    /// 2，通过服务器中转所有数据
    /// 3，臆断，认为那样子就可行。包括许多论文也是这个说法，我中的这招，不经过NAT会成功，经过最流行的TP-LINK就无法成功
    /// </remarks>
    public class P2PClient : Netbase
    {
        #region 属性
        private ISocketServer _Server;
        /// <summary>客户端</summary>
        public ISocketServer Server { get { return _Server; } set { _Server = value; } }

        private IPEndPoint _HoleServer;
        /// <summary>打洞服务器地址</summary>
        public IPEndPoint HoleServer { get { return _HoleServer; } set { _HoleServer = value; } }

        private ISocketClient _Client;
        /// <summary>客户端</summary>
        public ISocketClient Client { get { return _Client; } set { _Client = value; } }

        private ProtocolType _ProtocolType = ProtocolType.Udp;
        /// <summary>协议</summary>
        public ProtocolType ProtocolType { get { return _ProtocolType; } set { _ProtocolType = value; } }

        private IPEndPoint _ParterAddress;
        /// <summary>目标伙伴地址</summary>
        public IPEndPoint ParterAddress { get { return _ParterAddress; } set { _ParterAddress = value; } }

        private Boolean _Success;
        /// <summary>是否成功</summary>
        public Boolean Success { get { return _Success; } set { _Success = value; } }
        #endregion

        #region 方法
        /// <summary>
        /// 
        /// </summary>
        public void EnsureServer()
        {
            if (Server == null)
            {
                if (ProtocolType == ProtocolType.Tcp)
                {
                    var server = new TcpServer();
                    Server = server;
                    server.ReuseAddress = true;
                    server.Accepted += new EventHandler<NetEventArgs>(server_Accepted);
                }
                else
                {
                    var server = new UdpServer();
                    Server = server;
                    //server.ReuseAddress = true;
                    server.Received += new EventHandler<NetEventArgs>(server_Received);
                }

                Server.Start();

                WriteLog("监听：{0}", Server);
            }
        }

        void server_Received(object sender, NetEventArgs e)
        {
            var remote = "" + e.RemoteIPEndPoint;
            if (remote == "" + HoleServer)
            {
                WriteLog("HoleServer数据到来：{0} {1}", e.RemoteIPEndPoint, e.GetString());

                var ss = e.GetString().Split(":");
                if (ss == null || ss.Length < 2) return;

                IPAddress address = null;
                if (!IPAddress.TryParse(ss[0], out address)) return;
                Int32 port = 0;
                if (!Int32.TryParse(ss[1], out port)) return;
                var ep = new IPEndPoint(address, port);
                ParterAddress = ep;

                //Thread.Sleep(1000);

                Console.WriteLine("准备连接对方：{0}", ep);
                while (!Success)
                {
                    (Server as UdpServer).Send("Hello!", null, ep);

                    Thread.Sleep(100);
                    if (Success) break;
                    Thread.Sleep(3000);
                }
            }
            else if (remote == "" + ParterAddress)
            {
                WriteLog("Parter数据到来：{0} {1}", e.RemoteIPEndPoint, e.GetString());
                Success = true;

                var session = e.Socket as ISocketSession;
                if (session != null)
                {
                    session.Send("P2P连接已建立！", null, e.RemoteIPEndPoint);
                    WriteLog("P2P连接已建立！");
                    session.Send("我与" + e.RemoteIPEndPoint + "的P2P连接已建立！", null, HoleServer);

                    while (true)
                    {
                        Console.Write("请输入要说的话：");
                        String line = Console.ReadLine();
                        if (String.IsNullOrEmpty(line)) continue;
                        if (line == "exit") break;

                        session.Send(line, null, e.RemoteIPEndPoint);
                        Console.WriteLine("已发送！");
                    }
                }
            }
            else
            {
                WriteLog("未识别的数据到来：{0} {1}", e.RemoteIPEndPoint, e.GetString());
            }
        }

        void server_Accepted(object sender, NetEventArgs e)
        {
            WriteLog("连接到来：{0}", e.RemoteIPEndPoint);

            var session = e.Socket as ISocketSession;
            if (session != null)
            {
                session.Received += new EventHandler<NetEventArgs>(client_Received);
                session.Send("P2P连接已建立！");
                WriteLog("P2P连接已建立！");
            }
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

            //Random rnd = new Random((Int32)DateTime.Now.Ticks);
            //Thread.Sleep(rnd.Next(0, 2000));

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
        /// <summary>开始处理</summary>
        /// <param name="name"></param>
        public void Start(String name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            if (ProtocolType == ProtocolType.Tcp) EnsureClient();

            SendToHole("reg:" + name);

            //while (ParterAddress == null)
            //{
            //    Server.Send("reg:" + name, null, HoleServer);
            //    var ep = new IPEndPoint(HoleServer.Address, HoleServer.Port + 1);
            //    Server.Send("checknat", null, ep);

            //    Thread.Sleep(100);
            //    if (ParterAddress != null) break;
            //    Thread.Sleep(19000);
            //}
        }

        void SendToHole(String msg)
        {
            var server = Server as UdpServer;
            if (server != null)
            {
                server.Send(msg, null, HoleServer);
                if (msg.StartsWith("reg"))
                {
                    var ep = new IPEndPoint(HoleServer.Address, HoleServer.Port + 1);
                    server.Send("checknat", null, ep);
                }
            }
            else
            {
                Client.Send(msg, null);
            }
        }
        #endregion
    }
}