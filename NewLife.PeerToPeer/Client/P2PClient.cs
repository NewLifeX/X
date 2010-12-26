//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Net;
//using NewLife.Messaging;
//using NewLife.Net.Common;
//using NewLife.PeerToPeer.Messages;

//namespace NewLife.PeerToPeer.Client
//{
//    /// <summary>
//    /// 对等应用程序
//    /// </summary>
//    public class P2PClient : MessageServer
//    {
//        #region 属性
//        private Guid _Token;
//        /// <summary>唯一本地标识</summary>
//        public Guid Token
//        {
//            get { return _Token; }
//            set { _Token = value; }
//        }

//        private List<IPEndPoint> _Trackers;
//        /// <summary>跟踪服务器地址</summary>
//        public List<IPEndPoint> Trackers
//        {
//            get { return _Trackers; }
//            set { _Trackers = value; }
//        }

//        private List<IPAddress> _Private;
//        /// <summary>我的私有地址</summary>
//        public List<IPAddress> Private
//        {
//            get { return _Private; }
//            set { _Private = value; }
//        }

//        private IPEndPoint _Public;
//        /// <summary>我的公有地址</summary>
//        public IPEndPoint Public
//        {
//            get { return _Public; }
//            set { _Public = value; }
//        }

//        private Dictionary<Guid, Peer> _Friends;
//        /// <summary>好友节点 TKey Public-Private</summary>
//        public Dictionary<Guid, Peer> Friends
//        {
//            get
//            {
//                if (_Friends == null) _Friends = new Dictionary<Guid, Peer>();
//                return _Friends;
//            }
//            set { _Friends = value; }
//        }

//        private DateTime _FriendUpdateTime;
//        /// <summary>好友最后更新时间</summary>
//        public DateTime FriendUpdateTime
//        {
//            get { return _FriendUpdateTime; }
//            set { _FriendUpdateTime = value; }
//        }

//        //private Thread _PingThread;
//        ///// <summary>Ping线程</summary>
//        //public Thread PingThread
//        //{
//        //    get
//        //    {
//        //        if (_PingThread == null)
//        //            _PingThread = new Thread(new ThreadStart(Ping));
//        //        return _PingThread;
//        //    }
//        //    set { _PingThread = value; }
//        //}
//        #endregion

//        #region 开始/停止
//        /// <summary>
//        /// 开始
//        /// </summary>
//        public void Start()
//        {
//            Private = NetHelper.GetIPV4();
//        }

//        /// <summary>
//        /// 停止
//        /// </summary>
//        public void Stop()
//        {
//            ////结束Ping
//            //if (_PingThread != null) _PingThread.Abort();
//        }
//        #endregion

//        #region 收发消息
//        /// <summary>
//        /// 消息到达时
//        /// </summary>
//        /// <param name="sender"></param>
//        /// <param name="e"></param>
//        protected override void OnReceived(object sender, EventArgs<Message, Stream> e)
//        {
//            P2PMessage msg = e.Arg1 as P2PMessage;
//            if (msg == null) return;

//            switch (msg.MessageType)
//            {
//                case MessageTypes.Unkown:
//                    break;
//                //case MessageTypes.Test:
//                //    OnTest(e.Arg1 as TestMessage, e.Arg2);
//                //    break;
//                //case MessageTypes.Ping:
//                //    OnPing(e.Arg1 as PingMessage, e.Arg2);
//                //    break;
//                //case MessageTypes.Track:
//                //    OnTrack(e.Arg1 as TrackMessage, e.Arg2);
//                //    break;
//                default:
//                    //TestMessage.Response response = new TestMessage.Response();
//                    //response.Str = "无法识别该消息！";
//                    //response.WritePacket(e.Arg2);

//                    WriteLog("未处理的消息：{0}", msg.MessageType);
//                    break;
//            }
//        }
//        #endregion

//        #region 邀请
//        /// <summary>
//        /// 邀请指定地址成为我的好友
//        /// </summary>
//        /// <param name="ep"></param>
//        public void Invite(IPEndPoint ep)
//        {
//            //Peer peer = new ClientPeer(this);
//            //peer.Public = ep;
//            //peer.MessageArrived += delegate(Object sender, MessageArrivedEventArgs e)
//            //{
//            //    Console.WriteLine("收到消息！");

//            //    if (e.Message is InviteMessage.Response)
//            //    {
//            //        InviteMessage.Response message = e.Message as InviteMessage.Response;
//            //        peer.Token = e.Token;
//            //        peer.Private = message.Private;
//            //        AddFriend(peer);
//            //        AddFriends(message.Friends);
//            //        Console.WriteLine("邀请好友：" + e.Token);
//            //    }
//            //};
//            //peer.Public = ep;
//            //Invite(peer);
//        }

//        /// <summary>
//        /// 邀请
//        /// </summary>
//        /// <param name="peer"></param>
//        public void Invite(Peer peer)
//        {
//            InviteMessage msg = new InviteMessage();
//            //msg.Private = Private;

//            //peer.Send(msg, false);
//        }
//        #endregion

//        #region 好友
//        /// <summary>
//        /// 查找好友，关加入好友列表
//        /// </summary>
//        /// <returns></returns>
//        public bool AddFriend(Peer Friend)
//        {
//            if (Friend == null) return false;
//            //String TKey = Friend.Public.Address.ToString();
//            //if (null == Friends.Find(delegate(Peer item)
//            //{
//            //    if (Friend.Public == item.Public && Friend.Private == item.Private)
//            //        return true;
//            //    return false;
//            //}))
//            if (Find(Friend.Token) == null)
//            {
//                Friends.Add(Friend.Token, Friend);
//                return true;
//            }
//            return false;
//        }

//        /// <summary>
//        /// 添加远程好友列表
//        /// </summary>
//        /// <param name="friends"></param>
//        public void AddFriends(Dictionary<Guid, Peer> friends)
//        {
//            if (friends == null || friends.Count == 0) return;
//            foreach (Peer item in friends.Values)
//            {
//                AddFriend(item);
//            }
//        }

//        /// <summary>
//        /// 根据Guid找好友
//        /// </summary>
//        /// <param name="guid">Guid</param>
//        /// <returns></returns>
//        public Peer Find(Guid guid)
//        {
//            if (Friends == null || Friends.Count == 0) return null;
//            if (Friends.ContainsKey(guid))
//                return Friends[guid];

//            return null;
//        }

//        /// <summary>
//        /// 移除连接超时的好友
//        /// </summary>
//        /// <returns></returns>
//        public bool RemoveTimeoutFriend()
//        {
//            return true;
//        }
//        #endregion

//        #region 重载
//        ///// <summary>
//        ///// 已重载。
//        ///// </summary>
//        ///// <returns></returns>
//        //public override string ToString()
//        //{
//        //    return Name;
//        //}
//        #endregion
//    }
//}