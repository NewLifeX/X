using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NewLife.Net.Sockets
{
    /// <summary>网络服务的会话</summary>
    /// <remarks>
    /// 实际应用可通过重载<see cref="OnReceive"/>实现收到数据时的业务逻辑。
    /// </remarks>
    public class NetSession : Netbase, INetSession
    {
        #region 属性
        private Int32 _ID;
        /// <summary>编号</summary>
        public virtual Int32 ID { get { return _ID; } set { if (_ID > 0)throw new NetException("禁止修改会话编号！"); _ID = value; } }
        //Int32 INetSession.ID { get { return _ID; } set { if (_ID > 0)throw new NetException("禁止修改会话编号！"); _ID = value; } }

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

        #region 方法
        /// <summary>开始会话处理。参数e里面可能含有数据</summary>
        /// <param name="e"></param>
        public virtual void Start(NetEventArgs e)
        {
            WriteLog("新会话：{0}", this);

            // Tcp挂接事件，Udp直接处理数据
            if (Session.ProtocolType == ProtocolType.Tcp)
            {
                Session.Received += new EventHandler<NetEventArgs>(Session_Received);
                Session.OnDisposed += (s, e2) => this.Dispose();

                // 这里不需要再次Start，因为TcpServer在处理完成Accepted事件后，会调用Start
                //(Session as TcpClientX).Start(e);
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

            var session = Session;
            if (session.ProtocolType == ProtocolType.Tcp)
            {
                session.Received -= new EventHandler<NetEventArgs>(Session_Received);
                session.Disconnect();
                session.Dispose();
            }

            Server = null;
            Session = null;
        }
        #endregion

        #region 业务核心
        /// <summary>收到客户端发来的数据</summary>
        /// <param name="e"></param>
        protected virtual void OnReceive(NetEventArgs e) { }
        #endregion

        #region 发送
        /// <summary>发送数据</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">位移</param>
        /// <param name="size">写入字节数</param>
        public virtual void Send(byte[] buffer, int offset = 0, int size = 0) { Session.Send(buffer, offset, size, ClientEndPoint); }

        /// <summary>发送数据流</summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public virtual long Send(Stream stream) { return Session.Send(stream, ClientEndPoint); }

        /// <summary>发送字符串</summary>
        /// <param name="msg"></param>
        /// <param name="encoding"></param>
        public virtual void Send(string msg, Encoding encoding = null) { Session.Send(msg, encoding, ClientEndPoint); }
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString() { return Session == null ? base.ToString() : String.Format("{0}://{1}", Session.ProtocolType, ClientEndPoint); }
        #endregion
    }
}