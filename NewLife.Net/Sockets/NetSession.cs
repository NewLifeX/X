using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace NewLife.Net.Sockets
{
    /// <summary>网络服务的会话</summary>
    class NetSession : Netbase, INetSession
    {
        #region 属性
        private Int32 _ID;
        /// <summary>编号</summary>
        Int32 INetSession.ID { get { return _ID; } set { if (_ID > 0)throw new NetException("禁止修改会话编号！"); _ID = value; } }

        private ISocketSession _Session;
        /// <summary>客户端。跟客户端通讯的那个Socket，其实是服务端TcpSession/UdpServer</summary>
        public ISocketSession Session { get { return _Session; } set { _Session = value; } }

        private ISocketServer _Server;
        /// <summary>服务端。跟目标服务端通讯的那个Socket，其实是客户端TcpClientX/UdpClientX</summary>
        public ISocketServer Server { get { return _Server; } set { _Server = value; } }

        private EndPoint _ClientEndPoint;
        /// <summary>客户端远程IP终结点</summary>
        public EndPoint ClientEndPoint { get { return _ClientEndPoint; } set { _ClientEndPoint = value; } }
        #endregion

        #region 构造
        /// <summary>实例化一个网络服务会话</summary>
        public NetSession() { }

        /// <summary>通过指定的Socket对象实例化一个网络服务会话</summary>
        /// <param name="client"></param>
        public NetSession(ISocketSession client) { Session = client; }
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
        }
        #endregion

        #region 数据交换
        /// <summary>收到客户端发来的数据</summary>
        /// <param name="e"></param>
        protected virtual void OnReceive(NetEventArgs e)
        {
            //var stream = e.GetStream();
            //Console.WriteLine("{0} => {1} {2}字节", ClientEndPoint, Session.LocalEndPoint, stream.Length);
        }
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString() { return "" + Session; }
        #endregion
    }
}