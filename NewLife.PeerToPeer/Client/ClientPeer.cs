using System;
using System.Collections.Generic;
using System.Text;
using NewLife.PeerToPeer.Common;
using NewLife.PeerToPeer.Messages;
using System.IO;
using System.Net.Sockets;
using System.Net;

namespace NewLife.PeerToPeer.Client
{
    /// <summary>
    /// 客户端对等方
    /// </summary>
    public class ClientPeer : Peer
    {
        #region 构造
        private P2PClient _Application;
        /// <summary>应用</summary>
        public P2PClient Application
        {
            get { return _Application; }
            private set { _Application = value; }
        }

        //private Peer() { }

        ///// <summary>
        ///// 为指定应用程序创建一个对等方
        ///// </summary>
        ///// <param name="app"></param>
        ///// <returns></returns>
        //public static Peer Create(PeerApplication app)
        //{
        //    if (app == null) throw new ArgumentNullException("app");

        //    Peer peer = new Peer();
        //    peer.Application = app;

        //    return peer;
        //}

        /// <summary>
        /// 通过指定应用程序构造对等方
        /// </summary>
        /// <param name="app"></param>
        public ClientPeer(P2PClient app)
        {
            if (app == null) throw new ArgumentNullException("app");

            Application = app;
        }
        #endregion

        #region 收发消息
        //public override void Send(Message message, bool isResponse)
        //{
        //    PeerMessage msg = PeerMessage.Create(message);
        //    msg.IsResponse = isResponse;
        //    msg.Token = Application.Token;

        //    MemoryStream stream = new MemoryStream();
        //    msg.Serialize(stream);
        //    // 把指针移到开头，否则无法读取任何数据
        //    stream.Position = 0;

        //    if (Application.ProtocolType == ProtocolType.Tcp)
        //    {
        //        EnsureConnection();

        //        Connection.Send(stream);
        //    }
        //    else
        //    {
        //        //// 如果是UDP协议，则借用PeerApplication来发数据
        //        //Application.Server.Send(Public, stream.ToArray());
        //    }
        //}

        ///// <summary>
        ///// 消息到达时
        ///// </summary>
        ///// <param name="socket"></param>
        ///// <param name="remote"></param>
        ///// <param name="state"></param>
        ///// <param name="msg"></param>
        ///// <returns></returns>
        //protected override bool OnMessageArrived(Socket socket, IPEndPoint remote, object state, Stream stream, PeerMessage msg)
        //{
        //    if (Application != null) Application.OnCommand(msg, socket, remote, stream, null);

        //    return base.OnMessageArrived(socket, remote, state, stream, msg);
        //}
        #endregion
    }
}
