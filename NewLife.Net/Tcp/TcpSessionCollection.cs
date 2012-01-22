using System;
using System.Collections;
using System.Collections.Generic;
using NewLife.Threading;
using NewLife.Net.Sockets;

namespace NewLife.Net.Tcp
{
    /// <summary>会话集合。带有自动清理不活动会话的功能</summary>
    class TcpSessionCollection : DisposeBase, IDictionary<Int32, ISocketSession>
    {
        Dictionary<Int32, ISocketSession> _dic = new Dictionary<Int32, ISocketSession>();

        private Int32 sessionID = 0;

        /// <summary>添加新会话，并设置会话编号</summary>
        /// <param name="session"></param>
        public void Add(ISocketSession session)
        {
            lock (_dic)
            {
                session.ID = ++sessionID;
                session.OnDisposed += (s, e) => { lock (_dic) { _dic.Remove((s as ISocketSession).ID); } };
                _dic.Add(session.ID, session);

                if (clearTimer == null) clearTimer = new TimerX(RemoveNotAlive, null, ClearPeriod, ClearPeriod);
            }
        }

        private TcpServer _Server;
        /// <summary>服务端</summary>
        public TcpServer Server { get { return _Server; } set { _Server = value; } }

        private Int32 _ClearPeriod = 15000;
        /// <summary>清理周期。单位毫秒，默认15000毫秒。</summary>
        public Int32 ClearPeriod { get { return _ClearPeriod; } set { _ClearPeriod = value; } }

        /// <summary>关闭所有</summary>
        public void CloseAll()
        {
            if (clearTimer != null) clearTimer.Dispose();

            if (_dic.Count < 1) return;
            lock (_dic)
            {
                if (_dic.Count < 1) return;

                // 必须先复制到数组，因为会话销毁的时候，会自动从集合中删除，从而引起集合枚举失败
                var ns = new ISocketSession[_dic.Count];
                _dic.Values.CopyTo(ns, 0);
                _dic.Clear();
                foreach (var item in ns)
                {
                    //if (item == null || item.Disposed || item.Socket == null) continue;
                    if (item == null || item.Disposed) continue;

                    item.Close();
                }
            }
        }

        /// <summary>清理会话计时器</summary>
        private TimerX clearTimer;

        /// <summary>移除不活动的会话</summary>
        void RemoveNotAlive(Object state)
        {
            if (_dic.Count < 1) return;

            lock (_dic)
            {
                if (_dic.Count < 1) return;

                Int32 notactive = Server != null ? Server.MaxNotActive : 0;
                var list = new List<Int32>();
                // 这里可能有问题，曾经见到，_list有元素，但是value为null，这里居然没有进行遍历而直接跳过
                // 操作这个字典的时候，必须加锁，否则就会数据错乱，成了这个样子，无法枚举
                foreach (var elm in _dic)
                {
                    var item = elm.Value;
                    //if (item == null || item.Disposed || item.Socket == null) list.Add(elm.Key);
                    if (item == null || item.Disposed || notactive > 0 && item.Statistics.Last.AddSeconds(notactive) < DateTime.Now) list.Add(elm.Key);
                }
                foreach (var item in list)
                {
                    _dic.Remove(item);
                }
            }
        }

        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            CloseAll();
        }

        #region 成员
        public void Clear() { _dic.Clear(); }

        public int Count { get { return _dic.Count; } }

        public bool IsReadOnly { get { return (_dic as IDictionary<Int32, ISocketSession>).IsReadOnly; } }

        public IEnumerator<ISocketSession> GetEnumerator() { return _dic.Values.GetEnumerator() as IEnumerator<ISocketSession>; }

        IEnumerator IEnumerable.GetEnumerator() { return _dic.GetEnumerator(); }
        #endregion

        #region IDictionary<int,ISocketSession> 成员

        void IDictionary<int, ISocketSession>.Add(int key, ISocketSession value) { Add(value); }

        bool IDictionary<int, ISocketSession>.ContainsKey(int key) { return _dic.ContainsKey(key); }

        ICollection<int> IDictionary<int, ISocketSession>.Keys { get { return _dic.Keys; } }

        bool IDictionary<int, ISocketSession>.Remove(int key)
        {
            ISocketSession session;
            if (!_dic.TryGetValue(key, out session)) return false;

            session.Close();

            return _dic.Remove(key);
        }

        bool IDictionary<int, ISocketSession>.TryGetValue(int key, out ISocketSession value) { return _dic.TryGetValue(key, out value); }

        ICollection<ISocketSession> IDictionary<int, ISocketSession>.Values { get { return _dic.Values; } }

        ISocketSession IDictionary<int, ISocketSession>.this[int key] { get { return _dic[key]; } set { _dic[key] = value; } }

        #endregion

        #region ICollection<KeyValuePair<int,ISocketSession>> 成员

        void ICollection<KeyValuePair<int, ISocketSession>>.Add(KeyValuePair<int, ISocketSession> item)
        {
            throw new NetException("不支持！请使用Add(ISocketSession session)方法！");
        }

        bool ICollection<KeyValuePair<int, ISocketSession>>.Contains(KeyValuePair<int, ISocketSession> item) { throw new NotImplementedException(); }

        void ICollection<KeyValuePair<int, ISocketSession>>.CopyTo(KeyValuePair<int, ISocketSession>[] array, int arrayIndex) { throw new NotImplementedException(); }

        bool ICollection<KeyValuePair<int, ISocketSession>>.Remove(KeyValuePair<int, ISocketSession> item) { throw new NetException("不支持！请直接销毁会话对象！"); }

        #endregion

        #region IEnumerable<KeyValuePair<int,ISocketSession>> 成员
        IEnumerator<KeyValuePair<int, ISocketSession>> IEnumerable<KeyValuePair<int, ISocketSession>>.GetEnumerator()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}