using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using NewLife.Net.Sockets;
using NewLife.PeerToPeer.Messages;
using NewLife.Messaging;

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
            get
            {
                if (_Token == Guid.Empty) _Token = Guid.NewGuid();
                return _Token;
            }
            set { _Token = value; }
        }

        //private List<IPEndPoint> _Private;
        ///// <summary>我的私有地址</summary>
        //public List<IPEndPoint> Private
        //{
        //    get
        //    {
        //        if (_Private == null) _Private = new List<IPEndPoint>();
        //        return _Private;
        //    }
        //    set { _Private = value; }
        //}
        #endregion

        //#region 构造
        //static TrackerServer()
        //{
        //    P2PMessage.Init();
        //}

        //TrackerServer()
        //{
        //    Message.Received += new EventHandler<EventArgs<Message, Stream>>(Message_Received);
        //}

        ///// <summary>
        ///// 析构，取消事件注册
        ///// </summary>
        //~TrackerServer()
        //{
        //    Message.Received += new EventHandler<EventArgs<Message, Stream>>(Message_Received);
        //}
        //#endregion

        #region 服务器控制
        //private ITrackerServer _Tracker;
        ///// <summary>跟踪服务器</summary>
        //public ITrackerServer Tracker
        //{
        //    get { return _Tracker; }
        //    set { _Tracker = value; }
        //}

        ///// <summary>
        ///// 开始
        ///// </summary>
        //public void Start()
        //{
        //    Tracker.MessageArrived += new EventHandler<EventArgs<P2PMessage, Stream>>(OnMessageArrived);
        //    Tracker.Start();
        //}

        ///// <summary>
        ///// 停止
        ///// </summary>
        //public void Stop()
        //{
        //    Dispose();
        //}

        ///// <summary>
        ///// 已重载。
        ///// </summary>
        ///// <param name="disposing"></param>
        //protected override void OnDispose(bool disposing)
        //{
        //    Tracker.Stop();

        //    base.OnDispose(disposing);
        //}
        #endregion

        //#region 发送
        ///// <summary>
        ///// 发送信息
        ///// </summary>
        ///// <param name="msg"></param>
        ///// <param name="remoteEP"></param>
        //public void Send(P2PMessage msg, EndPoint remoteEP)
        //{
        //    msg.Token = Token;

        //    Tracker.Send(msg.ToArray(), remoteEP);
        //}
        //#endregion

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
                    break;
                case MessageTypes.TestResponse:
                    break;
                case MessageTypes.Invite:
                    break;
                case MessageTypes.InviteResponse:
                    break;
                case MessageTypes.Ping:
                    OnPing(sender as ITrackerServer, e.Arg1 as PingMessage, e.Arg2);
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

        #region 活跃
        void OnPing(ITrackerServer tracker, PingMessage msg, Stream stream)
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
        private Dictionary<Guid, Peer> _Friends;
        /// <summary>好友节点 TKey Public-Private</summary>
        public Dictionary<Guid, Peer> Friends
        {
            get
            {
                if (_Friends == null) _Friends = new Dictionary<Guid, Peer>();
                return _Friends;
            }
            set { _Friends = value; }
        }

        private DateTime _FriendUpdateTime;
        /// <summary>好友最后更新时间</summary>
        public DateTime FriendUpdateTime
        {
            get { return _FriendUpdateTime; }
            set { _FriendUpdateTime = value; }
        }

        void AddFriend(PingMessage msg, IPEndPoint publicEP)
        {
            if (!Friends.ContainsKey(msg.Token))
            {
                lock (Friends)
                {
                    if (!Friends.ContainsKey(msg.Token))
                    {
                        Peer peer = new Peer();
                        peer.Token = msg.Token;
                        peer.Private = msg.Private;
                        peer.Public = publicEP;
                        peer.InviteTime = DateTime.Now;

                        Friends.Add(msg.Token, peer);

                        FriendUpdateTime = DateTime.Now;

                        //WriteLog("添加好友：{0} {1}", publicEP, msg.Token);
                    }
                }
            }
        }

        /// <summary>
        /// 根据Ping消息取好友
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        List<Peer> GetFriends(PingMessage msg)
        {
            if (!Friends.ContainsKey(msg.Token)) return null;

            Peer peer = Friends[msg.Token];
            //// 上次的活跃时间之后没有好友更新
            //if (peer.ActiveTime > FriendUpdateTime) return null;

            lock (Friends)
            {
                List<Peer> list = new List<Peer>(Friends.Values);
                if (list.Contains(peer)) list.Remove(peer);
                if (list == null || list.Count < 1) return null;

                //WriteLog("取得好友：{0}", list.Count);
                return list;
            }
        }
        #endregion
    }
}