using System;
using System.Net;
using System.Net.Sockets;
using NewLife.Net.Sockets;
using System.Threading;

namespace NewLife.Net.Proxy
{
    /// <summary>代理会话。客户端的一次转发请求（或者Tcp连接），就是一个会话。转发的全部操作都在会话中完成。</summary>
    /// <remarks>
    /// 一个会话应该包含两端，两个Socket，服务端和客户端
    /// </remarks>
    class ProxySession : Netbase, IProxySession
    {
        #region 属性
        private IProxy _Proxy;
        /// <summary>代理对象</summary>
        public IProxy Proxy { get { return _Proxy; } set { _Proxy = value; } }

        private ISocketSession _Client;
        /// <summary>客户端。跟客户端通讯的那个Socket，其实是服务端TcpSession/UdpServer</summary>
        public ISocketSession Session { get { return _Client; } set { _Client = value; } }

        private ISocketServer _Server;
        /// <summary>服务端。跟目标服务端通讯的那个Socket，其实是客户端TcpClientX/UdpClientX</summary>
        public ISocketServer Server { get { return _Server; } set { _Server = value; } }

        private ISocketClient _Remote;
        /// <summary>远程服务端。跟目标服务端通讯的那个Socket，其实是客户端TcpClientX/UdpClientX</summary>
        public ISocketClient Remote { get { return _Remote; } set { _Remote = value; } }

        private EndPoint _ClientEndPoint;
        /// <summary>客户端远程IP终结点</summary>
        public EndPoint ClientEndPoint { get { return _ClientEndPoint; } set { _ClientEndPoint = value; } }

        private EndPoint _RemoteEndPoint;
        /// <summary>客户端远程IP终结点</summary>
        public EndPoint RemoteEndPoint { get { return _RemoteEndPoint; } set { _RemoteEndPoint = value; } }

        IProxyFilter Filter { get { return Proxy.MainFilter; } }
        #endregion

        #region 构造
        /// <summary>实例化一个代理会话</summary>
        public ProxySession() { }

        /// <summary>通过指定的Socket对象实例化一个代理会话</summary>
        /// <param name="client"></param>
        public ProxySession(ISocketSession client) { Session = client; }
        #endregion

        #region 方法
        /// <summary>开始会话处理。参数e里面可能含有数据</summary>
        /// <param name="e"></param>
        public void Start(NetEventArgs e)
        {
            // Tcp挂接事件，Udp直接处理数据
            if (Session.ProtocolType == ProtocolType.Tcp)
            {
                Session.Received += new EventHandler<NetEventArgs>(Session_Received);
                Session.OnDisposed += (s, e2) => this.Dispose();
            }
            else
                OnReceive(e);
        }

        void Session_Received(object sender, NetEventArgs e)
        {
            OnReceive(e);
        }

        /// <summary>子类重载实现资源释放逻辑时必须首先调用基类方法</summary>
        /// <param name="disposing">从Dispose调用（释放所有资源）还是析构函数调用（释放非托管资源）</param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            if (Session.ProtocolType == ProtocolType.Tcp)
            {
                Session.Received -= new EventHandler<NetEventArgs>(Session_Received);
                Session.Disconnect();
                Session.Dispose();
            }

            Server = null;
            Session = null;
            Remote.Dispose();
        }
        #endregion

        #region 数据交换
        void OnReceive(NetEventArgs e)
        {
            if (Remote == null)
            {
                Remote = Filter.CreateRemote(this, e);
                Remote.Received += new EventHandler<NetEventArgs>(Remote_Received);
                Remote.ReceiveAsync();
            }

            var stream = e.GetStream();
            Console.WriteLine("{0} => {1} {2}字节", ClientEndPoint, Session.LocalEndPoint, stream.Length);
            stream = Filter.OnClientToServer(this, stream, e);
            if (stream != null) Remote.Send(stream, RemoteEndPoint);
        }

        void Remote_Received(object sender, NetEventArgs e)
        {
            var stream = e.GetStream();
            Console.WriteLine("{0} {1}字节", Remote, stream.Length);
            stream = Filter.OnServerToClient(this, stream, e);
            if (stream != null) Session.Send(stream, ClientEndPoint);

            // UDP准备关闭
            if (Session.ProtocolType == ProtocolType.Udp)
            {
                // 等待未发送完成的数据
                Thread.Sleep(1000);
                Dispose();
            }
        }
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString() { return Session + "," + Remote; }
        #endregion
    }
}