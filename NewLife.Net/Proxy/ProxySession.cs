using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NewLife.Net.Sockets;

namespace NewLife.Net.Proxy
{
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
        public IProxy Proxy { get { return _Proxy; } set { _Proxy = value; } }

        //private IProxy _BaseProxy;
        ///// <summary>代理基类</summary>
        //protected virtual IProxy BaseProxy { get { return _BaseProxy; } set { _BaseProxy = value; } }

        private ISocketClient _Remote;
        /// <summary>远程服务端。跟目标服务端通讯的那个Socket，其实是客户端TcpClientX/UdpClientX</summary>
        public ISocketClient Remote { get { return _Remote; } set { _Remote = value; } }

        private EndPoint _RemoteEndPoint;
        /// <summary>服务端远程IP终结点</summary>
        public EndPoint RemoteEndPoint { get { return _RemoteEndPoint; } set { _RemoteEndPoint = value; } }

        private ProtocolType _RemoteProtocolType;
        /// <summary>服务端协议。默认与客户端协议相同</summary>
        public ProtocolType RemoteProtocolType { get { return _RemoteProtocolType; } set { _RemoteProtocolType = value; } }
        #endregion

        #region 方法
        /// <summary>子类重载实现资源释放逻辑时必须首先调用基类方法</summary>
        /// <param name="disposing">从Dispose调用（释放所有资源）还是析构函数调用（释放非托管资源）</param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            if (Remote == null)
            {
                Remote.Dispose();
                Remote = null;
            }
        }
        #endregion

        #region 数据交换
        //TODO: 这里应该如何设计，使得子类能够通过重载来改变数据流

        /// <summary>收到客户端发来的数据</summary>
        /// <param name="e"></param>
        protected override void OnReceive(NetEventArgs e)
        {
            if (Remote == null)
            {
                Remote = CreateRemote(e);
                if (Remote.ProtocolType == ProtocolType.Tcp && !Remote.Client.Connected) Remote.Connect(RemoteEndPoint);
                Remote.Received += new EventHandler<NetEventArgs>(Remote_Received);
                Remote.ReceiveAsync();
            }

            var stream = e.GetStream();
            if (stream != null) Remote.Send(stream, RemoteEndPoint);
        }

        /// <summary>为会话创建与远程服务器通讯的Socket。可以使用Socket池达到重用的目的。默认实现创建与服务器相同类型的客户端</summary>
        /// <param name="e"></param>
        /// <returns></returns>
        protected virtual ISocketClient CreateRemote(NetEventArgs e)
        {
            var client = NetService.Resolve<ISocketClient>(RemoteProtocolType);
            //if (client.ProtocolType == ProtocolType.Tcp && !client.Client.Connected) client.Connect(RemoteEndPoint);
            if (RemoteEndPoint != null) client.AddressFamily = RemoteEndPoint.AddressFamily;
            client.OnDisposed += (s, e2) => this.Dispose();
            return client;
        }

        void Remote_Received(object sender, NetEventArgs e) { OnReceiveRemote(e); }

        /// <summary>收到远程服务器返回的数据</summary>
        /// <param name="e"></param>
        protected virtual void OnReceiveRemote(NetEventArgs e)
        {
            var stream = e.GetStream();
            if (stream != null) Session.Send(stream, ClientEndPoint);

            //// UDP准备关闭
            //if (Session.ProtocolType == ProtocolType.Udp)
            //{
            //    // 等待未发送完成的数据
            //    Thread.Sleep(1000);
            //    Dispose();
            //}
        }
        #endregion

        #region 发送
        /// <summary>发送数据</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">位移</param>
        /// <param name="size">写入字节数</param>
        public virtual void SendRemote(byte[] buffer, int offset = 0, int size = 0) { Remote.Send(buffer, offset, size, RemoteEndPoint); }

        /// <summary>发送数据流</summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public virtual long SendRemote(Stream stream) { return Remote.Send(stream, RemoteEndPoint); }

        /// <summary>发送字符串</summary>
        /// <param name="msg"></param>
        /// <param name="encoding"></param>
        public virtual void SendRemote(string msg, Encoding encoding = null) { Remote.Send(msg, encoding, RemoteEndPoint); }
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString() { return base.ToString() + "=>" + Remote; }
        #endregion
    }
}