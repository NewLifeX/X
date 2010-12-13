using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using NewLife.Net.Common;
using NewLife.Net.Sockets;
using NewLife.Net.Udp;
//using NewLife.PeerToPeer.Common;
using NewLife.PeerToPeer.Messages;

namespace NewLife.PeerToPeer.Client
{
    /// <summary>
    /// 对等应用程序
    /// </summary>
    public class P2PClient : NetServer
    {
        #region 属性
        private Guid _Token;
        /// <summary>唯一本地标识</summary>
        public Guid Token
        {
            get { return _Token; }
            set { _Token = value; }
        }

        private List<IPEndPoint> _Trackers;
        /// <summary>跟踪服务器地址</summary>
        public List<IPEndPoint> Trackers
        {
            get { return _Trackers; }
            set { _Trackers = value; }
        }

        private List<IPAddress> _Private;
        /// <summary>我的私有地址</summary>
        public List<IPAddress> Private
        {
            get { return _Private; }
            set { _Private = value; }
        }

        private IPEndPoint _Public;
        /// <summary>我的公有地址</summary>
        public IPEndPoint Public
        {
            get { return _Public; }
            set { _Public = value; }
        }

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

        private Thread _PingThread;
        /// <summary>Ping线程</summary>
        public Thread PingThread
        {
            get
            {
                if (_PingThread == null)
                    _PingThread = new Thread(new ThreadStart(Ping));
                return _PingThread;
            }
            set { _PingThread = value; }
        }
        #endregion

        #region 开始/停止
        /// <summary>
        /// 已重载。
        /// </summary>
        protected override void EnsureCreateServer()
        {
            Name = "P2P客户端（Udp）";

            UdpServer svr = new UdpServer(Address, Port);
            svr.Received += new EventHandler<NetEventArgs>(UdpServer_Received);
            // 允许同时处理多个数据包
            svr.NoDelay = true;
            // 使用线程池来处理事件
            svr.UseThreadPool = true;

            Server = svr;

            // 初始化消息
            P2PMessage.Init();
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        protected override void OnStart()
        {
            base.OnStart();

            Private = NetHelper.GetIPV4();

            //开始Ping
            PingThread.Start();
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        protected override void OnStop()
        {
            //结束Ping
            if (_PingThread != null) _PingThread.Abort();

            base.OnStop();
        }
        #endregion

        #region 收发消息
        void UdpServer_Received(object sender, NetEventArgs e)
        {
            if (e.BytesTransferred <= 0) return;

            P2PMessage msg = P2PMessage.Deserialize(e.GetStream()) as P2PMessage;
            if (msg == null) return;

            //WriteLog("{0} {1}", e.RemoteEndPoint, Encoding.UTF8.GetString(e.Buffer, e.Offset, e.BytesTransferred));
            WriteLog("{0} [{1}] {2}", e.RemoteEndPoint, e.BytesTransferred, msg);

            OnMessageArrived(msg, e);
        }

        /// <summary>
        /// 消息到达时触发
        /// </summary>
        public event EventHandler<EventArgs<P2PMessage, IPEndPoint>> MessageArrived;

        /// <summary>
        /// 数据到达时
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="e"></param>
        protected virtual void OnMessageArrived(P2PMessage msg, NetEventArgs e)
        {
            IPEndPoint ip = e.RemoteEndPoint as IPEndPoint;
            if (MessageArrived != null) MessageArrived(this, new EventArgs<P2PMessage, IPEndPoint>(msg, ip));

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
                    break;
                case MessageTypes.PingResponse:
                    OnPingResponse(msg as PingMessage.Response, ip);
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

        /// <summary>
        /// 发送信息
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="remoteEP"></param>
        public void Send(P2PMessage msg, EndPoint remoteEP)
        {
            msg.Token = Token;

            (Server as UdpServer).Send(msg.ToArray(), remoteEP);
        }
        #endregion

        #region 测试
        /// <summary>
        /// 测试
        /// </summary>
        /// <param name="ep"></param>
        /// <param name="str"></param>
        public void Test(IPEndPoint ep, String str)
        {
            //Peer peer = new ClientPeer(this);
            //peer.Public = ep;

            //peer.MessageArrived += delegate(Object sender, MessageArrivedEventArgs e)
            //{
            //    Console.WriteLine("收到消息！");

            //    if (e.Message is TestMessage.Response)
            //    {
            //        TestMessage.Response message = e.Message as TestMessage.Response;
            //        Console.WriteLine(message.Str);
            //    }
            //};

            TestMessage msg = new TestMessage();
            msg.Str = str;

            //peer.Send(msg, false);
        }
        #endregion

        #region 邀请
        /// <summary>
        /// 邀请指定地址成为我的好友
        /// </summary>
        /// <param name="ep"></param>
        public void Invite(IPEndPoint ep)
        {
            //Peer peer = new ClientPeer(this);
            //peer.Public = ep;
            //peer.MessageArrived += delegate(Object sender, MessageArrivedEventArgs e)
            //{
            //    Console.WriteLine("收到消息！");

            //    if (e.Message is InviteMessage.Response)
            //    {
            //        InviteMessage.Response message = e.Message as InviteMessage.Response;
            //        peer.Token = e.Token;
            //        peer.Private = message.Private;
            //        AddFriend(peer);
            //        AddFriends(message.Friends);
            //        Console.WriteLine("邀请好友：" + e.Token);
            //    }
            //};
            //peer.Public = ep;
            //Invite(peer);
        }

        /// <summary>
        /// 邀请
        /// </summary>
        /// <param name="peer"></param>
        public void Invite(Peer peer)
        {
            InviteMessage msg = new InviteMessage();
            //msg.Private = Private;

            //peer.Send(msg, false);
        }
        #endregion

        #region Ping
        /// <summary>
        /// 检测好友
        /// </summary>
        public void Ping()
        {
            while (true)
            {
                try
                {
                    //// Ping跟踪服务器
                    //Ping(TrackerServer);

                    // Ping好友
                    if (Friends != null && Friends.Count > 0)
                    {
                        foreach (Peer item in Friends.Values)
                        {
                            if (item.Public == null || item.Public.Address == IPAddress.Any || item.Public.Port <= 0) continue;

                            // 先尝试私有地址
                            if (item.Private != null && item.Private.Count > 0)
                            {
                                foreach (IPAddress elm in item.Private)
                                {
                                    Ping(new IPEndPoint(elm, item.Public.Port));
                                }
                            }

                            Ping(item.Public);
                        }
                    }
                }
                catch (ThreadAbortException) { break; }
                catch (ThreadInterruptedException) { break; }
                catch (Exception ex)
                {
                    WriteLog(ex.ToString());
                }

                Thread.Sleep(3000);
            }
        }

        void Ping(IPEndPoint remoteEP)
        {
            PingMessage msg = new PingMessage();
            msg.Private = Private;
            Send(msg, remoteEP);
        }

        void OnPingResponse(PingMessage.Response msg, IPEndPoint remoteEP)
        {
            if ((Public == null || Public.Address == IPAddress.Any) && msg.Public != null)
            {
                Public = msg.Public;
                WriteLog("我的公有地址改变为：{0}", Public);
            }
            if (msg.Friends != null && msg.Friends.Count > 0)
            {
                foreach (Peer item in msg.Friends)
                {
                    //if (Friends.ContainsKey(item.Token))
                    Friends[item.Token] = item;
                }
            }
        }
        #endregion

        #region 好友
        /// <summary>
        /// 查找好友，关加入好友列表
        /// </summary>
        /// <returns></returns>
        public bool AddFriend(Peer Friend)
        {
            if (Friend == null) return false;
            //String TKey = Friend.Public.Address.ToString();
            //if (null == Friends.Find(delegate(Peer item)
            //{
            //    if (Friend.Public == item.Public && Friend.Private == item.Private)
            //        return true;
            //    return false;
            //}))
            if (Find(Friend.Token) == null)
            {
                Friends.Add(Friend.Token, Friend);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 添加远程好友列表
        /// </summary>
        /// <param name="friends"></param>
        public void AddFriends(Dictionary<Guid, Peer> friends)
        {
            if (friends == null || friends.Count == 0) return;
            foreach (Peer item in friends.Values)
            {
                AddFriend(item);
            }
        }

        /// <summary>
        /// 根据Guid找好友
        /// </summary>
        /// <param name="guid">Guid</param>
        /// <returns></returns>
        public Peer Find(Guid guid)
        {
            if (Friends == null || Friends.Count == 0) return null;
            if (Friends.ContainsKey(guid))
                return Friends[guid];

            return null;
        }

        /// <summary>
        /// 移除连接超时的好友
        /// </summary>
        /// <returns></returns>
        public bool RemoveTimeoutFriend()
        {
            return true;
        }
        #endregion

        #region 重载
        /// <summary>
        /// 已重载。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Name;
        }
        #endregion
    }
}