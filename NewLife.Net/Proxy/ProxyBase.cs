using System;
using System.Collections.Generic;
using System.Net.Sockets;
using NewLife.Linq;
using NewLife.Net.Sockets;
using NewLife.Net.Tcp;
using NewLife.Net.Udp;
using System.IO;

namespace NewLife.Net.Proxy
{
    /// <summary>网络数据转发代理基类</summary>
    public abstract class ProxyBase : NetServer, IProxy
    {
        #region 属性
        private ICollection<IProxySession> _Sessions;
        /// <summary>会话集合。</summary>
        public ICollection<IProxySession> Sessions { get { return _Sessions ?? (_Sessions = new List<IProxySession>()); } }

        private ICollection<IProxyFilter> _Filters;
        /// <summary>过滤器集合。</summary>
        public ICollection<IProxyFilter> Filters { get { return _Filters ?? (_Filters = new List<IProxyFilter>()); } }
        #endregion

        #region 构造
        /// <summary>子类重载实现资源释放逻辑</summary>
        /// <param name="disposing">从Dispose调用（释放所有资源）还是析构函数调用（释放非托管资源）</param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            lock (Sessions)
            {
                Sessions.ForEach(e => e.Dispose());
            }

            lock (Filters)
            {
                Filters.ForEach(e => e.Dispose());
            }
        }
        #endregion

        #region 创建
        /// <summary>添加Socket服务器</summary>
        /// <param name="server"></param>
        /// <returns>添加是否成功</returns>
        public override Boolean AttachServer(ISocketServer server)
        {
            if (!base.AttachServer(server)) return false;

            if (server.ProtocolType == ProtocolType.Tcp)
            {
                var svr = server as TcpServer;
                svr.Accepted += new EventHandler<NetEventArgs>(OnAccepted);
            }
            else if (server.ProtocolType == ProtocolType.Udp)
            {
                var svr = server as UdpServer;
                svr.Received += new EventHandler<NetEventArgs>(OnAccepted);
            }
            else
            {
                throw new Exception("不支持的协议类型" + server.ProtocolType + "！");
            }

            // 使用线程池处理事件，因为代理会话可能直接在事件中进行数据转发
            server.UseThreadPool = true;
            server.Error += new EventHandler<NetEventArgs>(OnError);

            return true;
        }
        #endregion

        #region 业务
        /// <summary>接受连接时，对于Udp是收到数据时（同时触发OnReceived）</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnAccepted(Object sender, NetEventArgs e)
        {
            WriteLog("新客户：{0}", e.RemoteEndPoint);

            var session = NetService.Resolve<IProxySession>();
            session.Proxy = this;
            session.Server = sender as ISocketServer;
            session.Session = e.Socket as ISocketSession;

            Sessions.Add(session);
            session.OnDisposed += (s, ev) => Sessions.Remove(session);

            session.Start(e);
        }

        /// <summary>断开连接/发生错误</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnError(object sender, NetEventArgs e)
        {
            if (e.SocketError != SocketError.Success || e.Error != null)
                WriteLog("{2}错误 {0} {1}", e.SocketError, e.Error, e.LastOperation);
            else
                WriteLog("{0}断开！", e.LastOperation);
        }
        #endregion

        #region 接口方法
        /// <summary>为会话创建与远程服务器通讯的Socket</summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public virtual ISocketClient CreateRemote(IProxySession session, NetEventArgs e)
        {
            // 转发给过滤器，由过滤器负责创建
            foreach (var item in Filters)
            {
                var client = item.CreateRemote(session, e);
                if (client != null) return client;
            }

            // return null;
            throw new NetException("没有任何过滤器为会话创建远程Socket！");
        }

        /// <summary>客户端发数据往服务端时</summary>
        /// <param name="session"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        Stream IProxy.OnClientToServer(IProxySession session, Stream stream, NetEventArgs e)
        {
            foreach (var item in Filters)
            {
                if ((stream = item.OnClientToServer(session, stream, e)) == null) return null;
            }
            return stream;
        }

        /// <summary>服务端发数据往客户端时</summary>
        /// <param name="session"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        Stream IProxy.OnServerToClient(IProxySession session, Stream stream, NetEventArgs e)
        {
            foreach (var item in Filters)
            {
                if ((stream = item.OnServerToClient(session, stream, e)) == null) return null;
            }
            return stream;
        }
        #endregion
    }
}