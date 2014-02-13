using System;
using System.IO;
using System.Text;
using NewLife.Net.Common;
using NewLife.Net.Sockets;

namespace NewLife.Net.Proxy
{
    /// <summary>代理会话</summary>
    /// <typeparam name="TProxy">实际代理类型</typeparam>
    /// <typeparam name="TProxySession">代理会话类型</typeparam>
    public class ProxySession<TProxy, TProxySession> : ProxySession
        where TProxy : ProxyBase<TProxySession>
        where TProxySession : ProxySession, new()
    {
        /// <summary>代理对象</summary>
        public TProxy Proxy { get { return (this as IProxySession).Proxy as TProxy; } set { (this as IProxySession).Proxy = value; } }
    }

    /// <summary>代理会话。客户端的一次转发请求（或者Tcp连接），就是一个会话。转发的全部操作都在会话中完成。</summary>
    /// <remarks>
    /// 一个会话应该包含两端，两个Socket，服务端和客户端
    /// 客户端<see cref="INetSession.Session"/>发来的数据，在这里经过一系列过滤器后，转发给服务端<see cref="RemoteClientSession"/>；
    /// 服务端<see cref="RemoteClientSession"/>返回的数据，在这里经过过滤器后，转发给客户端<see cref="INetSession.Session"/>。
    /// </remarks>
    public class ProxySession : NetSession, IProxySession
    {
        #region 属性
        private IProxy _Proxy;
        /// <summary>代理对象</summary>
        IProxy IProxySession.Proxy { get { return _Proxy; } set { _Proxy = value; } }

        private ISocketSession _Remote;
        /// <summary>远程服务端。跟目标服务端通讯的那个Socket，其实是客户端TcpClientX/UdpClientX</summary>
        public ISocketSession RemoteClientSession { get { return _Remote; } set { _Remote = value; } } 

        //private IPEndPoint _RemoteEndPoint;
        ///// <summary>服务端远程IP终结点</summary>
        //public IPEndPoint RemoteEndPoint { get { return _RemoteEndPoint; } set { _RemoteEndPoint = value; } }

        //private ProtocolType _RemoteProtocolType;
        ///// <summary>服务端协议。默认与客户端协议相同</summary>
        //public ProtocolType RemoteProtocolType { get { return _RemoteProtocolType; } set { _RemoteProtocolType = value; } }

        private NetUri _RemoteUri;
        /// <summary>服务端地址</summary>
        public NetUri RemoteServerUri { get { return _RemoteUri ?? (_RemoteUri = new NetUri()); } }

        //private String _RemoteHost;
        ///// <summary>远程主机</summary>
        //public virtual String RemoteHost { get { return _RemoteHost; } set { _RemoteHost = value; } }
        #endregion

        #region 构造
        /// <summary>实例化一个代理会话</summary>
        public ProxySession()
        {
            DisposeWhenSendError = true;            
        }

        /// <summary>子类重载实现资源释放逻辑时必须首先调用基类方法</summary>
        /// <param name="disposing">从Dispose调用（释放所有资源）还是析构函数调用（释放非托管资源）</param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            var remote = RemoteClientSession;
            if (remote != null)
            {
                RemoteClientSession = null;
                remote.Dispose();
            }
        }
        #endregion

        #region 数据交换
        /// <summary>开始会话处理。参数e里面可能含有数据</summary>
        /// <param name="e"></param>
        public override void Start(ReceivedEventArgs e)
        {
            // 如果未指定远程协议，则与来源协议一致
            if (RemoteServerUri.ProtocolType == 0) RemoteServerUri.ProtocolType = Session.ProtocolType;

            base.Start(e);
        }

        /// <summary>收到客户端发来的数据</summary>
        /// <param name="e"></param>
        protected override void OnReceive(ReceivedEventArgs e)
        {
            WriteDebugLog("Proxy[{0}].OnReceive {1} => {2}", ID, ClientEndPoint, e.Stream.Length);

            if (e.Stream != null)
            {
                if (RemoteClientSession == null) StartRemote(e);

                //Remote.Send(stream, RemoteEndPoint);
                if (RemoteClientSession != null) SendRemote(e.Stream);
            }
        }

        /// <summary>开始远程连接</summary>
        /// <param name="e"></param>
        protected virtual void StartRemote(ReceivedEventArgs e)
        {
            var start = DateTime.Now;
            ISocketSession session = null;
            try
            {
                WriteDebugLog("Proxy[{0}].StartRemote {1}", ID, RemoteServerUri);

                session = CreateRemote(e);
                //if (client.ProtocolType == ProtocolType.Tcp && !client.Client.Connected) client.Connect(RemoteEndPoint);
                session.OnDisposed += (s, e2) =>
                {
                    // 这个是必须清空的，是否需要保持会话呢，由OnRemoteDispose决定
                    _Remote = null;
                    OnRemoteDispose(s as ISocketSession);
                };
                session.Received += new EventHandler<ReceivedEventArgs>(Remote_Received);
                session.ReceiveAsync();

                //Debug.Assert(session.Client.Connected);
                RemoteClientSession = session;
            }
            catch (Exception ex)
            {
                if (session != null) session.Dispose();
                this.Dispose();

                var ts = DateTime.Now - start;
                //var host = String.IsNullOrEmpty(RemoteHost) ? "" + RemoteEndPoint : RemoteHost;
                //throw new XException(ex, "无法连接远程服务器{0}！耗时{1}！", RemoteUri, ts);
                WriteLog("无法为{0}连接远程服务器{1}！耗时{2}！{3}", ClientUri, RemoteServerUri, ts, ex.Message);
            }
        }

        ///// <summary>用于记录不可连接的地址，带超时时间。</summary>
        //static Dictionary<String, DateTime> _NotConnected = new Dictionary<string, DateTime>();

        /// <summary>为会话创建与远程服务器通讯的Socket。可以使用Socket池达到重用的目的。默认实现创建与服务器相同类型的客户端</summary>
        /// <param name="e"></param>
        /// <returns></returns>
        protected virtual ISocketSession CreateRemote(ReceivedEventArgs e)
        {
            //var key = "" + RemoteEndPoint;
            //if (String.IsNullOrEmpty(key)) throw new ArgumentNullException("RemoteEndPoint");

            //DateTime dt;
            //if (_NotConnected.TryGetValue(key, out dt))
            //{
            //    if (dt > DateTime.Now) throw new NetException("指定时间内连接{0}超时，稍候再试！", key);
            //    _NotConnected.Remove(key);
            //}

            var client = NetService.Container.Resolve<ISocketClient>(RemoteServerUri.ProtocolType);
            var rep = RemoteServerUri.EndPoint;
            if (rep != null)
            {
                client.AddressFamily = rep.AddressFamily;
                client.Connect(rep);
            }
            return client.CreateSession();

            //return NetService.CreateSession(new NetUri(RemoteProtocolType, RemoteEndPoint));
        }

        /// <summary>远程连接断开时触发。默认销毁整个会话，子类可根据业务情况决定客户端与代理的链接是否重用。</summary>
        /// <param name="client"></param>
        protected virtual void OnRemoteDispose(ISocketSession client) { this.Dispose(); }

        void Remote_Received(object sender, ReceivedEventArgs e)
        {
            try
            {
                OnReceiveRemote(e);
            }
            catch (Exception ex) { this.Dispose(); WriteLog(ex.Message); throw; }
        }

        /// <summary>收到远程服务器返回的数据</summary>
        /// <param name="e"></param>
        protected virtual void OnReceiveRemote(ReceivedEventArgs e)
        {
            WriteDebugLog("Proxy[{0}].OnReceiveRemote {1} <= {2}", ID, RemoteServerUri.EndPoint, e.Stream.Length);

            if (e.Stream != null)
            {
                var session = Session;
                if (session == null || session.Disposed)
                    this.Dispose();
                else
                {
                    try
                    {
                        Send(e.Stream);
                    }
                    catch { this.Dispose(); throw; }
                }
            }
        }
        #endregion

        #region 发送
        /// <summary>发送数据</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">位移</param>
        /// <param name="size">写入字节数</param>
        public virtual IProxySession SendRemote(byte[] buffer, int offset = 0, int size = 0)
        {
            try
            {
                RemoteClientSession.Send(buffer, offset, size);
            }
            catch { this.Dispose(); throw; }

            return this;
        }

        /// <summary>发送数据流</summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public virtual IProxySession SendRemote(Stream stream)
        {
            try
            {
                RemoteClientSession.Send(stream);
            }
            catch { this.Dispose(); throw; }

            return this;
        }

        /// <summary>发送字符串</summary>
        /// <param name="msg"></param>
        /// <param name="encoding"></param>
        public virtual IProxySession SendRemote(string msg, Encoding encoding = null)
        {
            try
            {
                RemoteClientSession.Send(msg, encoding);
            }
            catch { this.Dispose(); throw; }

            return this;
        }
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString() { return base.ToString() + "=>" + RemoteClientSession; }
        #endregion
    }
}