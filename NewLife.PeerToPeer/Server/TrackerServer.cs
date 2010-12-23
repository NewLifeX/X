using System;
using System.IO;
using System.Net;
using System.Threading;
using NewLife.Messaging;
using NewLife.Net.Sockets;
using NewLife.PeerToPeer.Messages;
using NewLife.Web;

namespace NewLife.PeerToPeer.Server
{
    /// <summary>
    /// 跟踪服务器
    /// </summary>
    public class TrackerServer : MessageServer
    {
        #region 属性
        private Guid _Token;
        /// <summary>标识</summary>
        public Guid Token
        {
            get { return _Token; }
            set { _Token = value; }
        }
        #endregion

        #region 构造
        /// <summary>
        /// 根据标识实例化Tracker服务器
        /// </summary>
        /// <param name="token"></param>
        public TrackerServer(Guid token)
        {
            Token = token;

            MessageHandler.Error += new EventHandler<EventArgs<Message, Stream>>(MessageHandler_Error);
            MessageHandler.Null += new EventHandler<EventArgs<Message, Stream>>(MessageHandler_Null);
        }

        /// <summary>
        /// 析构，取消事件注册
        /// </summary>
        ~TrackerServer()
        {
            MessageHandler.Error -= new EventHandler<EventArgs<Message, Stream>>(MessageHandler_Error);
            MessageHandler.Null -= new EventHandler<EventArgs<Message, Stream>>(MessageHandler_Null);
        }

        private static Int32 _Inited = 0;
        /// <summary>
        /// 初始化
        /// </summary>
        public static void Init()
        {
            // 只执行一次，防止多线程冲突
            if (Interlocked.CompareExchange(ref _Inited, 1, 0) != 0) return;

        }

        static void MessageHandler_Null(object sender, EventArgs<Message, Stream> e)
        {
            NullMessage message = e.Arg1 as NullMessage;
            Stream stream = e.Arg2;

            WriteLog("空数据！");

            message.Serialize(stream);
        }

        static void MessageHandler_Error(object sender, EventArgs<Message, Stream> e)
        {
            ExceptionMessage message = e.Arg1 as ExceptionMessage;
            Stream stream = e.Arg2;

            WriteLog("出错！" + message.Error);
        }
        #endregion

        #region 处理
        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnReceived(object sender, EventArgs<Message, Stream> e)
        {

            P2PMessage msg = e.Arg1 as P2PMessage;
            if (msg == null) return;

            switch (msg.MessageType)
            {
                case MessageTypes.Unkown:
                    break;
                case MessageTypes.Test:
                    OnTest(e.Arg1 as TestMessage, e.Arg2);
                    break;
                case MessageTypes.TestResponse:
                    break;
                case MessageTypes.Invite:
                    break;
                case MessageTypes.InviteResponse:
                    break;
                case MessageTypes.Ping:
                    OnPing(e.Arg1 as PingMessage, e.Arg2);
                    break;
                case MessageTypes.PingResponse:
                    break;
                case MessageTypes.FindTorrent:
                    break;
                case MessageTypes.FindTorrentResponse:
                    break;
                case MessageTypes.TranFile:
                    break;
                case MessageTypes.TranFileResponse:
                    break;
                case MessageTypes.Text:
                    break;
                case MessageTypes.TextResponse:
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region 测试
        void OnTest(TestMessage msg, Stream stream)
        {
            WriteLog("收到{0}的测试消息：{1}", GetEndPoint(stream), msg.Str);

            TestMessage.Response response = new TestMessage.Response();
            response.Str = "消息收到！";

            // 必须一次性写入到流中去，否则使用UDP时将会分多次发送
            //response.Serialize(stream);
            Byte[] buffer = response.ToArray();
            stream.Write(buffer, 0, buffer.Length);
        }
        #endregion

        #region 活跃
        void OnPing(PingMessage msg, Stream stream)
        {
            //WriteLog("{0} Ping Private={1}", remoteEP, msg.Private == null ? 0 : msg.Private.Count);

            //AddFriend(msg, remoteEP);

            //// 响应
            //PingMessage.Response response = new PingMessage.Response();
            //response.Public = remoteEP;
            //response.Friends = GetFriends(msg);
            //Send(tracker, response, remoteEP);
        }
        #endregion

        #region 好友
        //private Dictionary<Guid, Peer> _Friends;
        ///// <summary>好友节点 TKey Public-Private</summary>
        //public Dictionary<Guid, Peer> Friends
        //{
        //    get
        //    {
        //        if (_Friends == null) _Friends = new Dictionary<Guid, Peer>();
        //        return _Friends;
        //    }
        //    set { _Friends = value; }
        //}

        //private DateTime _FriendUpdateTime;
        ///// <summary>好友最后更新时间</summary>
        //public DateTime FriendUpdateTime
        //{
        //    get { return _FriendUpdateTime; }
        //    set { _FriendUpdateTime = value; }
        //}

        //void AddFriend(PingMessage msg, IPEndPoint publicEP)
        //{
        //    if (!Friends.ContainsKey(msg.Token))
        //    {
        //        lock (Friends)
        //        {
        //            if (!Friends.ContainsKey(msg.Token))
        //            {
        //                Peer peer = new Peer();
        //                peer.Token = msg.Token;
        //                peer.Private = msg.Private;
        //                peer.Public = publicEP;
        //                peer.InviteTime = DateTime.Now;

        //                Friends.Add(msg.Token, peer);

        //                FriendUpdateTime = DateTime.Now;

        //                //WriteLog("添加好友：{0} {1}", publicEP, msg.Token);
        //            }
        //        }
        //    }
        //}

        ///// <summary>
        ///// 根据Ping消息取好友
        ///// </summary>
        ///// <param name="msg"></param>
        ///// <returns></returns>
        //List<Peer> GetFriends(PingMessage msg)
        //{
        //    if (!Friends.ContainsKey(msg.Token)) return null;

        //    Peer peer = Friends[msg.Token];
        //    //// 上次的活跃时间之后没有好友更新
        //    //if (peer.ActiveTime > FriendUpdateTime) return null;

        //    lock (Friends)
        //    {
        //        List<Peer> list = new List<Peer>(Friends.Values);
        //        if (list.Contains(peer)) list.Remove(peer);
        //        if (list == null || list.Count < 1) return null;

        //        //WriteLog("取得好友：{0}", list.Count);
        //        return list;
        //    }
        //}
        #endregion

        #region 辅助方法
        static IPEndPoint GetEndPoint(Stream stream)
        {
            IPAddress address = IPAddress.Any;
            if (stream is HttpStream)
            {
                //String ip = (stream as HttpStream).Context.Request.UserHostAddress;

                //IPAddress.TryParse(ip, out address);

                //return new IPEndPoint(address, 0);

                return (stream as HttpStream).RemoteEndPoint;
            }
            else if (stream is SocketStream)
            {
                return (stream as SocketStream).RemoteEndPoint;
            }

            return new IPEndPoint(address, 0);
        }
        #endregion
    }
}