using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using NewLife.Net.Common;
using NewLife.Net.Sockets;
#if NET4
using System.Linq;
#else
using NewLife.Linq;
#endif

namespace NewLife.Net.Tcp
{
    /// <summary>TCP服务器</summary>
    /// <remarks>
    /// 核心工作：启动服务<see cref="OnStart"/>时，监听端口，并启用多个（逻辑处理器数的10倍）异步接受操作<see cref="AcceptAsync"/>。
    /// 服务器只处理<see cref="SocketAsyncOperation.Accept"/>操作，并创建<see cref="ISocketSession"/>后，
    /// 将其赋值在事件参数的<see cref="NetEventArgs.Session"/>中，传递给<see cref="Accepted"/>。
    /// 
    /// 服务器完全处于异步工作状态，任何操作都不可能被阻塞。
    /// 
    /// 注意：服务器接受连接请求后，不会开始处理数据，而是由<see cref="Accepted"/>事件订阅者决定何时开始处理数据<see cref="TcpClientX.Start"/>。
    /// </remarks>
    public class TcpServer : SocketServer
    {
        #region 属性
        /// <summary>已重载。</summary>
        public override ProtocolType ProtocolType { get { return ProtocolType.Tcp; } }

        private Int32 _MaxNotActive = 30;
        /// <summary>最大不活动时间。
        /// 对于每一个会话连接，如果超过该时间仍然没有收到任何数据，则断开会话连接。
        /// 单位秒，默认30秒。时间不是太准确，建议15秒的倍数。为0表示不检查。</summary>
        public Int32 MaxNotActive { get { return _MaxNotActive; } set { _MaxNotActive = value; } }

        private Boolean _AutoReceiveAsync = true;
        /// <summary>自动开始会话的异步接收。
        /// 接受连接请求后，自动开始会话的异步接收，默认打开，如果会话需要同步接收数据，需要关闭该选项。</summary>
        public Boolean AutoReceiveAsync { get { return _AutoReceiveAsync; } set { _AutoReceiveAsync = value; } }

        /// <summary>异步操作计数</summary>
        public override int AsyncCount
        {
            get
            {
                Int32 n = base.AsyncCount;
                foreach (var item in Sessions.Values.ToArray())
                {
                    if (item.Host != null) n += item.Host.AsyncCount;
                }
                return n;
            }
        }

        private Boolean _UseSession;
        /// <summary>使用会话</summary>
        public Boolean UseSession { get { return _UseSession; } set { _UseSession = value; } }
        #endregion

        #region 构造
        /// <summary>构造TCP服务器对象</summary>
        public TcpServer() : base(IPAddress.Any, 0) { }

        /// <summary>构造TCP服务器对象</summary>
        /// <param name="port"></param>
        public TcpServer(Int32 port) : base(IPAddress.Any, port) { }

        /// <summary>构造TCP服务器对象</summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        public TcpServer(IPAddress address, Int32 port) : base(address, port) { }

        /// <summary>构造TCP服务器对象</summary>
        /// <param name="hostname"></param>
        /// <param name="port"></param>
        public TcpServer(String hostname, Int32 port) : base(hostname, port) { }
        #endregion

        #region 开始停止
        /// <summary>开始</summary>
        protected override void OnStart()
        {
            // 开始监听
            base.OnStart();

            // 三次握手之后，Accept之前的总连接个数，队列满之后，新连接将得到主动拒绝ConnectionRefused错误
            // 在我（大石头）的开发机器上，实际上这里的最大值只能是200，大于200跟200一个样
            Server.Listen(Int32.MaxValue);
            //Server.Listen(200);

            AcceptAsync();
        }

        void AcceptAsync()
        {
            StartAsync(e =>
            {
                var server = Server;
                if (server == null || Disposed) { e.Cancel = true; return false; }

                e.AcceptSocket = null;
                return server.AcceptAsync(e);
            });
        }

        /// <summary>确定是否有挂起的连接请求。</summary>
        /// <returns>如果连接正挂起，则为 true；否则为 false。</returns>
        public Boolean Pending() { return Server.Poll(0, SelectMode.SelectRead); }
        #endregion

        #region 事件
        /// <summary>连接完成。在事件处理代码中，事件参数不得另作他用，套接字事件池将会将其回收。</summary>
        /// <remarks>这里一定不需要再次ReceiveAsync，因为TcpServer在处理完成Accepted事件后，会调用Start->ReceiveAsync</remarks>
        public event EventHandler<NetEventArgs> Accepted;

        /// <summary>新客户端到达</summary>
        /// <param name="e"></param>
        protected virtual void OnAccept(NetEventArgs e)
        {
            // 没有接收事件时，马上开始处理重建委托
            if (Accepted == null)
            {
                AcceptAsync();
                return;
            }

            // Session的Start也就可能处理一下参数里面的数据，而不可能使用参数，因为参数的完成事件是挂载在TcpServer上的。
            Process(e, AcceptAsync, ProcessAccept);
        }

        private IStatistics _AcceptStatistics;
        /// <summary>连接数统计信息。</summary>
        public IStatistics AcceptStatistics { get { return _AcceptStatistics ?? (_AcceptStatistics = NetService.Resolve<IStatistics>()); } }

        void ProcessAccept(NetEventArgs e)
        {
            // 统计连接数
            AcceptStatistics.Increment();

            // 建立会话
            var session = CreateSession(e);
            //session.NoDelay = this.NoDelay;
            //e.Socket = session;
            e.Socket = session.Host;
            e.Session = session;
            session.LocalUri.CopyFrom(LocalUri);
            if (Accepted != null)
            {
                e.Cancel = false;
                Accepted(this, e);
                if (e.Cancel || session.Disposed || session.Host.Disposed) return;
            }

            // 不需要指定Key，内部会计算
            if (UseSession) Sessions.Add(0, session);

            // 设置接收事件，统计异步接收的数据包，不包括同步接收
            session.Received += (s, e2) => Statistics.Increment();

            // 设置心跳时间
            e.AcceptSocket.SetTcpKeepAlive(true);

            // 来自这里的事件参数没有远程地址
            if (AutoReceiveAsync) (session as TcpClientX).Start(e);
        }

        /// <summary>已重载。</summary>
        /// <param name="e"></param>
        protected override void OnComplete(NetEventArgs e)
        {
            if (e.LastOperation == SocketAsyncOperation.Accept)
            {
                OnAccept(e);
                return;
            }
            base.OnComplete(e);
        }
        #endregion

        #region 会话
        private Object _Sessions_lock = new object();
        private IDictionary<Int32, ISocketSession> _Sessions;
        /// <summary>会话集合。用自增的数字ID作为标识，业务应用自己维持ID与业务主键的对应关系。</summary>
        public IDictionary<Int32, ISocketSession> Sessions
        {
            get
            {
                if (_Sessions != null) return _Sessions;
                lock (_Sessions_lock)
                {
                    if (_Sessions != null) return _Sessions;

                    return _Sessions = new TcpSessionCollection() { Server = this };
                }
            }
        }

        /// <summary>创建会话</summary>
        /// <param name="e"></param>
        /// <returns></returns>
        protected virtual ISocketSession CreateSession(NetEventArgs e)
        {
            var session = new TcpClientX();
            session.Socket = e.AcceptSocket;
            //session.RemoteEndPoint = e.AcceptSocket.RemoteEndPoint as IPEndPoint;
            //if (e.RemoteEndPoint == null) e.RemoteEndPoint = session.RemoteEndPoint;
            session.SetRemote(e);
            // 对于服务器中的会话来说，收到空数据表示断开连接
            // session.DisconnectWhenEmptyData = true;

            return session;
        }
        #endregion

        #region 释放资源
        /// <summary>已重载。释放会话集合等资源</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            // 释放托管资源
            if (disposing)
            {
                var sessions = _Sessions;
                if (sessions != null)
                {
                    _Sessions = null;

                    WriteLog("准备释放Tcp会话{0}个！", sessions.Count);
                    if (sessions is IDisposable)
                        (sessions as IDisposable).Dispose();
                    else
                    {
                        foreach (var item in sessions.Values.ToArray())
                        {
                            item.Dispose();
                        }
                        sessions.Clear();
                    }
                }
            }
        }
        #endregion
    }
}