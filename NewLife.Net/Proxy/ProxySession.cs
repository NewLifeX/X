using System;
using System.IO;
using System.Text;
using NewLife.Log;
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
    /// 客户端<see cref="INetSession.Session"/>发来的数据，在这里经过一系列过滤器后，转发给服务端<see cref="RemoteServer"/>；
    /// 服务端<see cref="RemoteServer"/>返回的数据，在这里经过过滤器后，转发给客户端<see cref="INetSession.Session"/>。
    /// </remarks>
    public class ProxySession : NetSession, IProxySession
    {
        #region 属性
        private IProxy _Proxy;
        /// <summary>代理对象</summary>
        IProxy IProxySession.Proxy { get { return _Proxy; } set { _Proxy = value; } }

        private ISocketClient _RemoteServer;
        /// <summary>远程服务端。跟目标服务端通讯的那个Socket，其实是客户端TcpSession/UdpServer</summary>
        public ISocketClient RemoteServer { get { return _RemoteServer; } set { _RemoteServer = value; } }

        private NetUri _RemoteServerUri = new NetUri();
        /// <summary>服务端地址</summary>
        public NetUri RemoteServerUri { get { return _RemoteServerUri; } set { _RemoteServerUri = value; } }

        private Boolean _ExchangeEmptyData = true;
        /// <summary>是否中转空数据包。默认true</summary>
        public Boolean ExchangeEmptyData { get { return _ExchangeEmptyData; } set { _ExchangeEmptyData = value; } }
        #endregion

        #region 构造
        /// <summary>实例化一个代理会话</summary>
        public ProxySession() { }

        /// <summary>子类重载实现资源释放逻辑时必须首先调用基类方法</summary>
        /// <param name="disposing">从Dispose调用（释放所有资源）还是析构函数调用（释放非托管资源）</param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            var remote = RemoteServer;
            if (remote != null)
            {
                RemoteServer = null;
                remote.Dispose();
            }
        }
        #endregion

        #region 数据交换
        /// <summary>开始会话处理。</summary>
        public override void Start()
        {
            // 如果未指定远程协议，则与来源协议一致
            if (RemoteServerUri.ProtocolType == 0) RemoteServerUri.ProtocolType = Session.Local.ProtocolType;
            // 如果是Tcp，收到空数据时不要断开。为了稳定可靠，默认设置
            if (Session is TcpSession) (Session as TcpSession).DisconnectWhenEmptyData = false;

            base.Start();
        }

        /// <summary>收到客户端发来的数据</summary>
        /// <param name="e"></param>
        protected override void OnReceive(ReceivedEventArgs e)
        {
            WriteLog("客户端[{0}] {1}", e.Length, e.ToHex(16));

            if (e.Length > 0 || e.Length == 0 && ExchangeEmptyData)
            {
                // 如果未建立到远程服务器链接，则建立
                if (RemoteServer == null) StartRemote(e);

                // 如果已存在到远程服务器的链接，则把数据发向远程服务器
                if (RemoteServer != null) SendRemote(e.Stream);
            }
        }

        /// <summary>开始远程连接</summary>
        /// <param name="e"></param>
        protected virtual void StartRemote(ReceivedEventArgs e)
        {
            var start = DateTime.Now;
            ISocketClient session = null;
            try
            {
                WriteLog("连接远程服务器 {0} 解析 {1}", RemoteServerUri, RemoteServerUri.Address);

                session = CreateRemote(e);
                session.Log = new ActionLog(WriteLog);
                session.OnDisposed += (s, e2) =>
                {
                    // 这个是必须清空的，是否需要保持会话呢，由OnRemoteDispose决定
                    _RemoteServer = null;
                    OnRemoteDispose(s as ISocketClient);
                };
                session.Received += Remote_Received;
                session.ReceiveAsync();

                RemoteServer = session;
            }
            catch (Exception ex)
            {
                var ts = DateTime.Now - start;
                WriteError("无法为{0}连接远程服务器{1}！耗时{2}！{3}", Remote, RemoteServerUri, ts, ex.Message);

                if (session != null) session.Dispose();
                this.Dispose();
            }
        }

        /// <summary>为会话创建与远程服务器通讯的Socket。可以使用Socket池达到重用的目的。</summary>
        /// <param name="e"></param>
        /// <returns></returns>
        protected virtual ISocketClient CreateRemote(ReceivedEventArgs e)
        {
            return NetService.CreateClient(RemoteServerUri);
        }

        /// <summary>远程连接断开时触发。默认销毁整个会话，子类可根据业务情况决定客户端与代理的链接是否重用。</summary>
        /// <param name="client"></param>
        protected virtual void OnRemoteDispose(ISocketClient client) { this.Dispose(); }

        void Remote_Received(object sender, ReceivedEventArgs e)
        {
            try
            {
                OnReceiveRemote(e);
            }
            catch (Exception ex)
            {
                WriteError(ex.Message);
                this.Dispose();
            }
        }

        /// <summary>收到远程服务器返回的数据</summary>
        /// <param name="e"></param>
        protected virtual void OnReceiveRemote(ReceivedEventArgs e)
        {
            WriteLog("服务端[{0}] {1}", e.Length, e.ToHex(16));

            if (e.Length > 0 || e.Length == 0 && ExchangeEmptyData)
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
                    catch (Exception ex)
                    {
                        WriteError("转发给客户端出错，{0}", ex.Message);

                        this.Dispose();
                        throw;
                    }
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
                RemoteServer.Send(buffer, offset, size);
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
                RemoteServer.Send(stream);
            }
            catch (Exception ex)
            {
                WriteError("转发给服务端出错，{0}", ex.Message);

                this.Dispose();
                throw;
            }

            return this;
        }

        /// <summary>发送字符串</summary>
        /// <param name="msg"></param>
        /// <param name="encoding"></param>
        public virtual IProxySession SendRemote(String msg, Encoding encoding = null)
        {
            try
            {
                RemoteServer.Send(msg, encoding);
            }
            catch { this.Dispose(); throw; }

            return this;
        }
        #endregion

        #region 错误处理
        /// <summary></summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnError(object sender, ExceptionEventArgs e)
        {
            if (e.Exception != null) Dispose();
        }
        #endregion

        #region 辅助
        private String _LogPrefix;
        /// <summary>日志前缀</summary>
        public override String LogPrefix
        {
            get
            {
                if (_LogPrefix == null)
                {
                    var session = this as INetSession;
                    var name = session.Host == null ? "" : session.Host.Name.TrimEnd("Proxy");
                    _LogPrefix = "{0}[{1}] ".F(name, ID);
                }
                return _LogPrefix;
            }
            set { _LogPrefix = value; }
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString() { return base.ToString() + "=>" + RemoteServerUri; }
        #endregion
    }
}