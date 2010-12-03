using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;

namespace NewLife.PeerToPeer
{
    /// <summary>
    /// 在线用户
    /// </summary>
    public class PeerOnline
    {
        private Dictionary<Guid, Peer> _OnlineUser;
        /// <summary>在线对等方</summary>
        public Dictionary<Guid, Peer> OnlineUser
        {
            get { return _OnlineUser; }
        }

        private Timer _OnlineTimer;
        /// <summary>属性说明</summary>
        public Timer OnlineTimer
        {
            get
            {
                if (_OnlineTimer == null)
                {
                    _OnlineTimer = new Timer(TimerInterval);
                    _OnlineTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
                    _OnlineTimer.Enabled = false;
                }
                return _OnlineTimer;
            }
            set { _OnlineTimer = value; }
        }

        private Int32 _TimerInterval = 60 * 1000;
        /// <summary>Timer执行间隔 默认60秒钟</summary>
        public Int32 TimerInterval
        {
            get { return _TimerInterval; }
            set { _TimerInterval = value; }
        }

        private Object lockObj = new Object();

        /// <summary>
        /// 
        /// </summary>
        public PeerOnline()
        {
            OnlineTimer.Start();
        }

        /// <summary>
        /// 添加在线用户
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public Boolean OnlinePeerAdd(Guid guid, Peer peer)
        {
            if (OnlineUser.ContainsKey(guid)) return false;
            lock (lockObj)
            {
                if (OnlineUser.ContainsKey(guid)) return false;
                OnlineUser.Add(guid, peer);
            }
            return true;
        }

        /// <summary>
        /// 删除在线用户
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public Boolean OnlinePeerDelete(Guid guid)
        {
            if (!OnlineUser.ContainsKey(guid)) return false;
            lock (lockObj)
            {
                if (!OnlineUser.ContainsKey(guid)) return false;
                OnlineUser.Remove(guid);
            }
            return true;
        }

        ///// <summary>
        ///// 维护在线列表，超离线
        ///// </summary>
        ///// <returns></returns>
        //public Boolean OnlineTimer()
        //{
        //    return false;
        //}

        /// <summary>
        /// 维护在线列表，Timer回调方法
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            //Console.WriteLine("Hello World!");
        }


    }
}
