using System;
using System.Net.Sockets;
using NewLife.Net.Tcp;
using NewLife.Net.Udp;

namespace NewLife.Net.Sockets
{
    /// <summary>网络应用服务</summary>
    public class NetAppServer : NetServer
    {
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
                svr.Received += new EventHandler<NetEventArgs>(OnReceived);
            }
            else
            {
                throw new Exception("不支持的协议类型" + server.ProtocolType + "！");
            }

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
            TcpClientX session = e.Socket as TcpClientX;
            if (session != null)
            {
                session.Received += OnReceived;
                session.Error += new EventHandler<NetEventArgs>(OnError);
            }
        }

        /// <summary>收到数据时</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnReceived(Object sender, NetEventArgs e) { }

        ///// <summary>把数据发送给客户端</summary>
        ///// <param name="sender"></param>
        ///// <param name="buffer"></param>
        ///// <param name="offset"></param>
        ///// <param name="size"></param>
        ///// <param name="remoteEP"></param>
        //protected virtual void Send(ISocketSession sender, Byte[] buffer, Int32 offset, Int32 size, EndPoint remoteEP)
        //{
        //    if (sender is TcpClientX)
        //    {
        //        TcpClientX tc = sender as TcpClientX;
        //        if (tc != null && tc.Client.Connected) tc.Send(buffer, offset, size);
        //    }
        //    else if (sender is UdpServer)
        //    {
        //        //if ((remoteEP as IPEndPoint).Address != IPAddress.Any)
        //        // 兼容IPV6
        //        IPEndPoint remote = remoteEP as IPEndPoint;
        //        if (remote != null && !remote.Address.IsAny())
        //        {
        //            UdpServer us = sender as UdpServer;
        //            us.Send(buffer, offset, size, remoteEP);
        //        }
        //    }
        //}

        ///// <summary>断开客户端连接</summary>
        ///// <param name="client"></param>
        //protected virtual void Disconnect(ISocketSession client)
        //{
        //    if (client is TcpClientX)
        //    {
        //        TcpClientX tc = client as TcpClientX;
        //        if (tc != null && tc.Client.Connected) tc.Close();
        //    }
        //}

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
    }
}