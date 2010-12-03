//using System;
//using System.IO;
//using System.Net;
//using NewLife.Net.Sockets;
//using NewLife.Net.Udp;
//using NewLife.PeerToPeer.Messages;
//using NewLife.Messaging;
//using NewLife.Log;

//namespace NewLife.PeerToPeer.Server
//{
//    /// <summary>
//    /// Udp协议跟踪服务器
//    /// </summary>
//    public class UdpTrackerServer : NetServer, ITrackerServer
//    {
//        #region 构造
//        static UdpTrackerServer()
//        {
//            P2PMessage.Init();
//        }

//        /// <summary>
//        /// 初始化
//        /// </summary>
//        public UdpTrackerServer()
//        {
//            MessageHandler.Instance.MessageReceived += new EventHandler<EventArgs<Message, Stream>>(Instance_MessageReceived);
//        }

//        /// <summary>
//        /// 析构，取消事件注册
//        /// </summary>
//        ~UdpTrackerServer()
//        {
//            MessageHandler.Instance.MessageReceived -= new EventHandler<EventArgs<Message, Stream>>(Instance_MessageReceived);
//        }
//        #endregion

//        #region 开始/停止
//        /// <summary>
//        /// 已重载。
//        /// </summary>
//        protected override void EnsureCreateServer()
//        {
//            Name = "P2P跟踪服务器（Udp）";

//            UdpServer svr = new UdpServer(Address, Port);
//            svr.Received += new EventHandler<NetEventArgs>(UdpServer_Received);
//            // 允许同时处理多个数据包
//            svr.NoDelay = true;
//            // 使用线程池来处理事件
//            svr.UseThreadPool = true;

//            Server = svr;

//            //// 初始化消息
//            //P2PMessage.Init();
//        }
//        #endregion

//        #region 收发消息
//        void UdpServer_Received(object sender, NetEventArgs e)
//        {
//            if (e.BytesTransferred <= 0) return;

//            try
//            {
//                Message.Process(e.GetStream(), MessageExceptionOption.Throw);
//            }
//            catch (Exception ex)
//            {
//                XTrace.WriteLine(ex.ToString());
//            }

//            //WriteLog("{0} {1}", e.RemoteEndPoint, Encoding.UTF8.GetString(e.Buffer, e.Offset, e.BytesTransferred));
//            //WriteLog("{0} [{1}] {2}", e.RemoteEndPoint, e.BytesTransferred, msg);

//            //OnCommand(msg, e);
//            //OnMessageArrived(msg, e, stream);
//        }

//        void Instance_MessageReceived(object sender, EventArgs<Message, Stream> e)
//        {
//            P2PMessage msg = e.Arg1 as P2PMessage;
//            if (msg == null) return;

//            //switch (msg.MessageType)
//            //{
//            //    case MessageTypes.Test:
//            //        if (Test != null) Test(sender, e);
//            //        break;
//            //    case MessageTypes.Ping:
//            //        if (Ping != null) Ping(sender, e);
//            //        break;
//            //    case MessageTypes.FindTorrent:
//            //        if (FindTorrent != null) FindTorrent(sender, e);
//            //        break;
//            //    case MessageTypes.Text:
//            //        if (Text != null) Text(sender, e);
//            //        break;
//            //    default:
//            //        break;
//            //}

//            if (MessageArrived != null) MessageArrived(this, new EventArgs<P2PMessage, Stream>(msg, e.Arg2));
//        }

//        /// <summary>
//        /// 消息到达时触发
//        /// </summary>
//        public event EventHandler<EventArgs<P2PMessage, Stream>> MessageArrived;

//        ///// <summary>
//        ///// 数据到达时
//        ///// </summary>
//        ///// <param name="msg"></param>
//        ///// <param name="e"></param>
//        ///// <param name="stream"></param>
//        //protected virtual void OnMessageArrived(P2PMessage msg, Stream stream)
//        //{
//        //    if (MessageArrived != null) MessageArrived(this, new EventArgs<P2PMessage, Stream>(msg, stream));
//        //}

//        ///// <summary>
//        ///// 发送数据
//        ///// </summary>
//        ///// <param name="buffer"></param>
//        ///// <param name="remoteEP"></param>
//        //public void Send(Byte[] buffer, EndPoint remoteEP)
//        //{
//        //    (Server as UdpServer).Send(buffer, remoteEP);
//        //}
//        #endregion

//        #region 命令
//        ///// <summary>
//        ///// 命令处理中心
//        ///// </summary>
//        ///// <param name="msg"></param>
//        ///// <param name="e"></param>
//        //protected virtual void OnCommand(P2PMessage msg, NetEventArgs e)
//        //{
//        //    P2PMessage result = msg.Process(e.RemoteEndPoint);
//        //    if (result != null)
//        //    {
//        //        MemoryStream stream = new MemoryStream();
//        //        result.Serialize(stream);
//        //        Byte[] buffer = stream.ToArray();

//        //        UdpServer us = Server as UdpServer;
//        //        us.Send(buffer, 0, buffer.Length, e.RemoteEndPoint);
//        //    }
//        //}
//        #endregion
//    }
//}