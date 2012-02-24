using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NewLife.Exceptions;
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
    /// 客户端<see cref="INetSession.Session"/>发来的数据，在这里经过一系列过滤器后，转发给服务端<see cref="Remote"/>；
    /// 服务端<see cref="Remote"/>返回的数据，在这里经过过滤器后，转发给客户端<see cref="INetSession.Session"/>。
    /// </remarks>
    public class ProxySession : NetSession, IProxySession
    {
        #region 属性
        private IProxy _Proxy;
        /// <summary>代理对象</summary>
        IProxy IProxySession.Proxy { get { return _Proxy; } set { _Proxy = value; } }

        private ISocketClient _Remote;
        /// <summary>远程服务端。跟目标服务端通讯的那个Socket，其实是客户端TcpClientX/UdpClientX</summary>
        public ISocketClient Remote { get { return _Remote; } set { _Remote = value; } }

        private IPEndPoint _RemoteEndPoint;
        /// <summary>服务端远程IP终结点</summary>
        public IPEndPoint RemoteEndPoint { get { return _RemoteEndPoint; } set { _RemoteEndPoint = value; } }

        private ProtocolType _RemoteProtocolType;
        /// <summary>服务端协议。默认与客户端协议相同</summary>
        public ProtocolType RemoteProtocolType { get { return _RemoteProtocolType; } set { _RemoteProtocolType = value; } }

        /// <summary>服务端地址</summary>
        public NetUri RemoteUri { get { return new NetUri(Remote != null ? Remote.ProtocolType : RemoteProtocolType, RemoteEndPoint); } }
        #endregion

        #region 方法
        /// <summary>子类重载实现资源释放逻辑时必须首先调用基类方法</summary>
        /// <param name="disposing">从Dispose调用（释放所有资源）还是析构函数调用（释放非托管资源）</param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            var remote = Remote;
            if (remote != null)
            {
                Remote = null;
                remote.Dispose();
            }
        }
        #endregion

        #region 数据交换
        /// <summary>收到客户端发来的数据</summary>
        /// <param name="e"></param>
        protected override void OnReceive(NetEventArgs e)
        {
            var stream = e.GetStream();
            stream = OnReceive(e, stream);
            if (stream != null)
            {
                if (Remote == null) StartRemote(e);

                //Remote.Send(stream, RemoteEndPoint);
                SendRemote(stream);
            }
        }

        /// <summary>收到客户端发来的数据。子类可通过重载该方法来修改数据</summary>
        /// <param name="e"></param>
        /// <param name="stream">数据</param>
        /// <returns>修改后的数据</returns>
        protected virtual Stream OnReceive(NetEventArgs e, Stream stream)
        {
            WriteLog("{0}客户数据：{1}", ID, stream.Length);

            return stream;
        }

        /// <summary>开始远程连接</summary>
        /// <param name="e"></param>
        protected virtual void StartRemote(NetEventArgs e)
        {
            var start = DateTime.Now;
            try
            {
                var client = CreateRemote(e);
                if (client.ProtocolType == ProtocolType.Tcp && !client.Client.Connected) client.Connect(RemoteEndPoint);
                client.Received += new EventHandler<NetEventArgs>(Remote_Received);
                client.ReceiveAsync();
                Remote = client;
            }
            catch (Exception ex)
            {
                this.Dispose();

                var ts = DateTime.Now - start;
                throw new XException(ex, "无法连接远程服务器{0}！耗时{1}！", RemoteEndPoint, ts);
            }
        }

        /// <summary>为会话创建与远程服务器通讯的Socket。可以使用Socket池达到重用的目的。默认实现创建与服务器相同类型的客户端</summary>
        /// <param name="e"></param>
        /// <returns></returns>
        protected virtual ISocketClient CreateRemote(NetEventArgs e)
        {
            var client = NetService.Resolve<ISocketClient>(RemoteProtocolType);
            if (RemoteEndPoint != null) client.AddressFamily = RemoteEndPoint.AddressFamily;
            return client;
        }

        void Remote_Received(object sender, NetEventArgs e)
        {
            try
            {
                OnReceiveRemote(e);
            }
            catch { this.Dispose(); throw; }
        }

        /// <summary>收到远程服务器返回的数据</summary>
        /// <param name="e"></param>
        protected virtual void OnReceiveRemote(NetEventArgs e)
        {
            var stream = e.GetStream();
            stream = OnReceiveRemote(e, stream);
            if (stream != null)
            {
                var session = Session;
                if (session == null || session.Disposed)
                    this.Dispose();
                else
                {
                    try
                    {
                        session.Send(stream, ClientEndPoint);
                    }
                    catch { this.Dispose(); throw; }
                }
            }
        }

        /// <summary>收到客户端发来的数据。子类可通过重载该方法来修改数据</summary>
        /// <param name="e"></param>
        /// <param name="stream">数据</param>
        /// <returns>修改后的数据</returns>
        protected virtual Stream OnReceiveRemote(NetEventArgs e, Stream stream)
        {
            WriteLog("{0}远程数据：{1}", ID, stream.Length);

            return stream;
        }
        #endregion

        #region 发送
        /// <summary>发送数据</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">位移</param>
        /// <param name="size">写入字节数</param>
        public virtual void SendRemote(byte[] buffer, int offset = 0, int size = 0)
        {
            try
            {
                Remote.Send(buffer, offset, size, RemoteEndPoint);
            }
            catch { this.Dispose(); throw; }
        }

        /// <summary>发送数据流</summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public virtual long SendRemote(Stream stream)
        {
            try
            {
                return Remote.Send(stream, RemoteEndPoint);
            }
            catch { this.Dispose(); throw; }
        }

        /// <summary>发送字符串</summary>
        /// <param name="msg"></param>
        /// <param name="encoding"></param>
        public virtual void SendRemote(string msg, Encoding encoding = null)
        {
            try
            {
                Remote.Send(msg, encoding, RemoteEndPoint);
            }
            catch { this.Dispose(); throw; }
        }
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString() { return base.ToString() + "=>" + Remote; }
        #endregion
    }
}