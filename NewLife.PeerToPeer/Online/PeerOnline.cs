//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Threading;
//using System.Net;

//namespace NewLife.PeerToPeer.Online
//{
//    /// <summary>
//    /// 在线用户
//    /// </summary>
//    public class PeerOnline
//    {
//        private Dictionary<Guid, Peer> _Peers = new Dictionary<Guid, Peer>();
//        /// <summary>在线对等方</summary>
//        public Dictionary<Guid, Peer> Peers
//        {
//            get { return _Peers; }
//            set { _Peers = value; }
//        }

//        private Timer _OnlineTimer;
//        /// <summary>Timer</summary>
//        public Timer OnlineTimer
//        {
//            get { return _OnlineTimer; }
//            set { _OnlineTimer = value; }
//        }

//        private Int32 _TimerInterval = 60;
//        /// <summary>Timer执行间隔 默认60秒钟 单位秒</summary>
//        public Int32 TimerInterval
//        {
//            get { return _TimerInterval; }
//            set { _TimerInterval = value; }
//        }

//        private Int32 _TimeOut = 60;
//        /// <summary>超时间隔　单位秒</summary>
//        public Int32 TimeOut
//        {
//            get { return _TimeOut; }
//            set { _TimeOut = value; }
//        }

//        private AutoResetEvent TimeAutoReset = new AutoResetEvent(false);

//        /// <summary>
//        /// 检查状态
//        /// </summary>
//        private Boolean AutoResetEventStaticState = true;

//        private Object lockObj = new Object();

//        /// <summary>
//        /// 
//        /// </summary>
//        public PeerOnline()
//        {
//            //TimeStart();
//        }

//        /// <summary>
//        /// 开始检查
//        /// </summary>
//        public void TimeStart()
//        {
//            if (OnlineTimer == null)
//                OnlineTimer = new Timer(new TimerCallback(OnTimedEvent), this, TimerInterval * 1000, TimerInterval * 1000);
//            else
//            {
//                AutoResetEventStaticState = true;
//                TimeAutoReset.Reset();
//            }

//        }

//        /// <summary>
//        /// 停止检查
//        /// </summary>
//        public void TimeStop()
//        {
//            AutoResetEventStaticState = false;
//        }

//        /// <summary>
//        /// 创建在线列表
//        /// </summary>
//        public void CreateOnlineUser()
//        {
//            if (Peers == null)
//                Peers = new Dictionary<Guid, Peer>();
//        }

//        /// <summary>
//        /// 添加在线用户
//        /// </summary>
//        /// <param name="guid"></param>
//        /// <param name="peer"></param>
//        /// <returns></returns>
//        public Boolean Add(Peer peer)
//        {
//            if (Peers.ContainsKey(peer.Token)) return false;
//            lock (lockObj)
//            {
//                if (Peers.ContainsKey(peer.Token)) return false;
//                Peers.Add(peer.Token, peer);
//            }
//            return true;
//        }

//        /// <summary>
//        /// 删除在线用户
//        /// </summary>
//        /// <param name="guid"></param>
//        /// <returns></returns>
//        public Boolean Delete(Guid guid)
//        {
//            if (!Peers.ContainsKey(guid)) return false;
//            lock (lockObj)
//            {
//                if (!Peers.ContainsKey(guid)) return false;
//                Peers.Remove(guid);
//            }
//            return true;
//        }

//        /// <summary>
//        /// 保存用户
//        /// </summary>
//        /// <param name="peer"></param>
//        /// <returns></returns>
//        public Boolean Save(Peer peer)
//        {
//            if (!Peers.ContainsKey(peer.Token)) return Add(peer);
//            Boolean modify = false;
//            lock (lockObj)
//            {
//                if (!Peers.ContainsKey(peer.Token)) return false;
//                Peers[peer.Token] = peer;
//            }
//            return true;
//        }

//        public Boolean Save(Guid token, List<IPAddress> privateIP, String publicIP)
//        {
//            if (Peers.ContainsKey(token)) return false;
//            Peer p = new Peer();
//            p.Token = token;
//            p.Private = privateIP;
//            p.Public = new IPEndPoint(IPAddress.Parse(publicIP), 0);
//            p.ActiveTime = DateTime.Now;

//            return Add(p);
//        }

//        ///// <summary>
//        ///// 维护在线列表，超离线
//        ///// </summary>
//        ///// <returns></returns>
//        //public Boolean OnlineTimer()
//        //{
//        //    return false;
//        //}

//        /// <summary>
//        /// 维护在线列表，Timer回调方法
//        /// </summary>
//        /// <param name="peerOnline"></param>
//        private static void OnTimedEvent(Object peerOnline)
//        {
//            //AutoResetEvent autoEvent = new AutoResetEvent(false);

//            //Console.WriteLine("Hello World!");

//            PeerOnline po = peerOnline as PeerOnline;

//            //if(!po.AutoResetEventStaticState)
//            //   po.

//            if (po.Peers == null || po.Peers.Count == 0)
//            {
//                Console.WriteLine("空！");

//                return;
//            }
//            lock (po.lockObj)
//            {
//                if (po.Peers == null || po.Peers.Count == 0) return;
//                Dictionary<Guid, Peer> rt = new Dictionary<Guid, Peer>();
//                foreach (Peer item in po.Peers.Values)
//                {
//                    if (item.ActiveTime.AddSeconds(po.TimeOut) > DateTime.Now)
//                        rt.Add(item.Token, item);
//                }
//                po.Peers = rt;

//            }

//            Console.WriteLine("检查在线超时－" + po.Peers.Count + "-" + DateTime.Now.ToString());


//        }

//    }

//    ///// <summary>
//    ///// 回调参数
//    ///// </summary>
//    //class TimeCallBackStruct
//    //{
//    //    private Object _lockObj;
//    //    /// <summary>锁定对像</summary>
//    //    public Object lockObj
//    //    {
//    //        get { return _lockObj; }
//    //        set { _lockObj = value; }
//    //    }

//    //    private Dictionary<Guid, Peer> _OnlineUser;
//    //    /// <summary>在线用户</summary>
//    //    public Dictionary<Guid, Peer> OnlineUser
//    //    {
//    //        get { return _OnlineUser; }
//    //        set { _OnlineUser = value; }
//    //    }

//    //    /// <summary>
//    //    /// 
//    //    /// </summary>
//    //    /// <param name="obj"></param>
//    //    /// <param name="onlineUser"></param>
//    //    public TimeCallBackStruct(Object obj, Dictionary<Guid, Peer> onlineUser)
//    //    {
//    //        lockObj = obj;
//    //        OnlineUser = onlineUser;
//    //    }
//    //}
//}
