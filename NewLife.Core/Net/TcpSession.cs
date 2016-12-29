using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NewLife.Data;

namespace NewLife.Net
{
    /// <summary>增强TCP客户端</summary>
    public class TcpSession : SessionBase, ISocketSession
    {
        #region 属性
        /// <summary>会话编号</summary>
        public Int32 ID { get; internal set; }

        /// <summary>收到空数据时抛出异常并断开连接。默认true</summary>
        public Boolean DisconnectWhenEmptyData { get; set; }

        ///// <summary>会话数据流，供用户程序使用。可用于解决Tcp粘包的问题。</summary>
        //public Stream Stream { get; set; }

        ISocketServer _Server;
        /// <summary>Socket服务器。当前通讯所在的Socket服务器，其实是TcpServer/UdpServer。该属性决定本会话是客户端会话还是服务的会话</summary>
        ISocketServer ISocketSession.Server { get { return _Server; } }

        /// <summary>自动重连次数，默认3。发生异常断开连接时，自动重连服务端。</summary>
        public Int32 AutoReconnect { get; set; }
        #endregion

        #region 构造
        /// <summary>实例化增强TCP</summary>
        public TcpSession()
        {
            Name = GetType().Name;
            Local = new NetUri(NetType.Tcp, IPAddress.Any, 0);
            Remote = new NetUri(NetType.Tcp, IPAddress.Any, 0);

            DisconnectWhenEmptyData = true;
            AutoReconnect = 3;
        }

        /// <summary>使用监听口初始化</summary>
        /// <param name="listenPort"></param>
        public TcpSession(Int32 listenPort)
            : this()
        {
            Port = listenPort;
        }

        /// <summary>用TCP客户端初始化</summary>
        /// <param name="client"></param>
        public TcpSession(Socket client)
            : this()
        {
            if (client == null) return;

            Client = client;
            var socket = client;
            if (socket.LocalEndPoint != null) Local.EndPoint = (IPEndPoint)socket.LocalEndPoint;
            if (socket.RemoteEndPoint != null) Remote.EndPoint = (IPEndPoint)socket.RemoteEndPoint;
        }

        internal TcpSession(ISocketServer server, Socket client)
            : this(client)
        {
            Active = true;
            _Server = server;
            Name = server.Name;
        }
        #endregion

        #region 方法
        /// <summary>打开</summary>
        protected override Boolean OnOpen()
        {
            // 服务端会话没有打开
            if (_Server != null) return false;

            if (Client == null || !Client.IsBound)
            {
                // 根据目标地址适配本地IPv4/IPv6
                if (Remote != null && !Remote.Address.IsAny())
                {
                    Local.Address = Local.Address.GetRightAny(Remote.Address.AddressFamily);
                }

                Client = NetHelper.CreateTcp(Local.EndPoint.Address.IsIPv4());
                //Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
                Client.Bind(Local.EndPoint);
                CheckDynamic();

                WriteLog("Open {0}", this);
            }

            // 打开端口前如果已设定远程地址，则自动连接
            if (Remote == null || Remote.EndPoint.IsAny()) return false;

            try
            {
                Client.Connect(Remote.EndPoint);
            }
            catch (Exception ex)
            {
                if (!Disposed && !ex.IsDisposed()) OnError("Connect", ex);
                if (ThrowException) throw;

                return false;
            }

            _Reconnect = 0;

            return true;
        }

        /// <summary>关闭</summary>
        /// <param name="reason">关闭原因。便于日志分析</param>
        protected override Boolean OnClose(String reason)
        {
            var client = Client;
            if (client != null)
            {
                Client = null;
                WriteLog("Close {0} {1}", reason, this);

                // 提前关闭这个标识，否则Close时可能触发自动重连机制
                Active = false;
                try
                {
                    // 温和一点关闭连接
                    //Client.Shutdown();
                    client.Close();

                    // 如果是服务端，这个时候就是销毁
                    if (_Server != null) Dispose();
                }
                catch (Exception ex)
                {
                    if (!ex.IsDisposed()) OnError("Close", ex);
                    if (ThrowException) throw;

                    return false;
                }
            }
            //Stream = null;

            return true;
        }
        #endregion

        #region 发送
        /// <summary>发送数据</summary>
        /// <remarks>
        /// 目标地址由<seealso cref="SessionBase.Remote"/>决定
        /// </remarks>
        /// <param name="pk">数据包</param>
        /// <returns>是否成功</returns>
        protected override Boolean OnSend(Packet pk)
        {
            if (StatSend != null) StatSend.Increment(pk.Count);
            if (Log != null && Log.Enable && LogSend) WriteLog("Send [{0}]: {1}", pk.Count, pk.ToHex());

            try
            {
                // 修改发送缓冲区
                if (Client.SendBufferSize < pk.Count) Client.SendBufferSize = pk.Count;

                if (pk.Count == 0)
                    Client.Send(new Byte[0]);
                else
                    Client.Send(pk.Data, pk.Offset, pk.Count, SocketFlags.None);
            }
            catch (Exception ex)
            {
                if (!ex.IsDisposed())
                {
                    OnError("Send", ex);

                    // 发送异常可能是连接出了问题，需要关闭
                    Close("发送出错");
                    Reconnect();

                    if (ThrowException) throw;
                }

                return false;
            }

            LastTime = DateTime.Now;

            return true;
        }

        internal override bool OnSendAsync(SocketAsyncEventArgs se) { return Client.SendAsync(se); }
        #endregion

        #region 接收
        ///// <summary>接收数据</summary>
        ///// <returns>收到的数据。如果没有数据返回0长度数组，如果出错返回null</returns>
        //public override Byte[] Receive()
        //{
        //    if (!Open()) return null;

        //    var task = SendAsync(null, null);
        //    if (Timeout > 0 && !task.Wait(Timeout)) return null;

        //    return task.Result;
        //}

        internal override bool OnReceiveAsync(SocketAsyncEventArgs se)
        {
            var client = Client;
            if (client == null || !Active || Disposed) throw new ObjectDisposedException(GetType().Name);

            return client.ReceiveAsync(se);
        }

        /// <summary>处理收到的数据</summary>
        /// <param name="pk"></param>
        /// <param name="remote"></param>
        internal override void OnReceive(Packet pk, IPEndPoint remote)
        {
            if (pk.Count== 0 && DisconnectWhenEmptyData)
            {
                Close("收到空数据");
                Dispose();

                return;
            }

            base.OnReceive(pk, remote);

            OnReceive(pk);
        }

        /// <summary>处理收到的数据</summary>
        /// <param name="pk"></param>
        protected virtual void OnReceive(Packet pk)
        {
#if !__MOBILE__
            // 更新全局远程IP地址
            NewLife.Web.WebHelper.UserHost = Remote.EndPoint.ToString();
#endif
            // 分析处理
            var e = new ReceivedEventArgs(pk);
            e.UserState = Remote.EndPoint;

            if (Log.Enable && LogReceive) WriteLog("Recv [{0}]: {1}", e.Length, e.ToHex(32, null));

            RaiseReceive(this, e);
        }
        #endregion

        #region 自动重连
        /// <summary>重连次数</summary>
        private Int32 _Reconnect;
        void Reconnect()
        {
            if (Disposed) return;
            // 如果重连次数达到最大重连次数，则退出
            if (Interlocked.Increment(ref _Reconnect) > AutoReconnect) return;

            WriteLog("Reconnect {0}", this);

            Open();
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
                    var name = _Server == null ? "" : _Server.Name;
                    _LogPrefix = "{0}[{1}].".F(name, ID);
                }
                return _LogPrefix;
            }
            set { _LogPrefix = value; }
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (Remote != null && !Remote.EndPoint.IsAny())
            {
                if (_Server == null)
                    return String.Format("{0}=>{1}", Local, Remote.EndPoint);
                else
                    return String.Format("{0}<={1}", Local, Remote.EndPoint);
            }
            else
                return Local.ToString();
        }
        #endregion
    }
}