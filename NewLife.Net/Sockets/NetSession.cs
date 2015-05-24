using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace NewLife.Net.Sockets
{
    /// <summary>网络服务的会话</summary>
    /// <typeparam name="TServer">网络服务类型</typeparam>
    public class NetSession<TServer> : NetSession where TServer : NetServer
    {
        /// <summary>主服务</summary>
        public virtual TServer Host { get { return (this as INetSession).Host as TServer; } set { (this as INetSession).Host = value; } }
    }

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

        private NetServer _Host;
        /// <summary>主服务</summary>
        NetServer INetSession.Host { get { return _Host; } set { _Host = value; } }

        private ISocketSession _Session;
        /// <summary>客户端。跟客户端通讯的那个Socket，其实是服务端TcpSession/UdpServer</summary>
        public ISocketSession Session { get { return _Session; } set { _Session = value; } }

        private ISocketServer _Server;
        /// <summary>服务端</summary>
        public ISocketServer Server { get { return _Server; } set { _Server = value; } }

        /// <summary>客户端地址</summary>
        public NetUri Remote { get { return Session.Remote; } }
        #endregion

        #region 方法
        /// <summary>开始会话处理。</summary>
        public virtual void Start()
        {
            ShowSession();

            Session.Received += (s, e2) => OnReceive(e2);
            Session.OnDisposed += (s, e2) => this.Dispose();
            Session.Error += OnError;
        }

        [Conditional("DEBUG")]
        void ShowSession()
        {
            WriteLog("{0}", Session);
        }

        /// <summary>子类重载实现资源释放逻辑时必须首先调用基类方法</summary>
        /// <param name="disposing">从Dispose调用（释放所有资源）还是析构函数调用（释放非托管资源）</param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            Session.Dispose();

            Server = null;
            Session = null;
        }
        #endregion

        #region 业务核心
        /// <summary>收到客户端发来的数据，触发<seealso cref="Received"/>事件，重载者可直接处理数据</summary>
        /// <param name="e"></param>
        protected virtual void OnReceive(ReceivedEventArgs e)
        {
            if (Received != null) Received(this, e);
        }

        /// <summary>数据到达事件</summary>
        public event EventHandler<ReceivedEventArgs> Received;
        #endregion

        #region 收发
        /// <summary>发送数据</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">位移</param>
        /// <param name="size">写入字节数</param>
        public virtual INetSession Send(byte[] buffer, int offset = 0, int size = 0)
        {
            Session.Send(buffer, offset, size);

            return this;
        }

        /// <summary>发送数据流</summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public virtual INetSession Send(Stream stream)
        {
            Session.Send(stream);

            return this;
        }

        /// <summary>发送字符串</summary>
        /// <param name="msg"></param>
        /// <param name="encoding"></param>
        public virtual INetSession Send(string msg, Encoding encoding = null)
        {
            Session.Send(msg, encoding);

            return this;
        }
        #endregion

        #region 异常处理
        /// <summary>错误处理</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnError(object sender, ExceptionEventArgs e) { }
        #endregion

        #region 辅助
        private String _LogPrefix;
        /// <summary>日志前缀</summary>
        public virtual String LogPrefix
        {
            get
            {
                if (_LogPrefix == null)
                {
                    var name = _Host == null ? "" : _Host.Name;
                    _LogPrefix = "{0}[{1}] ".F(name, ID);
                }
                return _LogPrefix;
            }
            set { _LogPrefix = value; }
        }

        /// <summary>已重载。日志加上前缀</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public override void WriteLog(string format, params object[] args)
        {
            base.WriteLog(LogPrefix + format, args);
        }

        /// <summary>输出错误日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public virtual void WriteError(String format, params Object[] args)
        {
            var name = _Host == null ? "" : _Host.Name;
            Log.Error(LogPrefix + format, args);
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            //return Session == null ? base.ToString() : Session.ToString();
            return String.Format("{0}[{1}] {2}", _Host == null ? "" : _Host.Name, ID, Session);
        }
        #endregion
    }
}