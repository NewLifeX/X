using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using NewLife.Threading;

namespace NewLife.Net
{
    /// <summary>会话集合。带有自动清理不活动会话的功能</summary>
    class TcpSessionCollection : DisposeBase, IDictionary<String, ISocketSession>
    {
        Dictionary<String, ISocketSession> _dic = new Dictionary<String, ISocketSession>();

        //private Int32 sessionID = 0;

        /// <summary>添加新会话，并设置会话编号</summary>
        /// <param name="session"></param>
        public void Add(ISocketSession session)
        {
            lock (_dic)
            {
                //session.ID = ++sessionID;
                session.OnDisposed += (s, e) => { lock (_dic) { _dic.Remove((s as ISocketSession).Remote.EndPoint + ""); } };
                _dic.Add(session.Remote.EndPoint + "", session);

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

                    //item.Close();
                    item.Dispose();
                }
            }
        }

        /// <summary>清理会话计时器</summary>
        private TimerX clearTimer;

        /// <summary>移除不活动的会话</summary>
        void RemoveNotAlive(Object state)
        {
            if (_dic.Count < 1) return;

            var keys = new List<String>();
            var values = new List<ISocketSession>();
            lock (_dic)
            {
                if (_dic.Count < 1) return;

                Int32 notactive = Server != null ? Server.MaxNotActive : 0;
                // 这里可能有问题，曾经见到，_list有元素，但是value为null，这里居然没有进行遍历而直接跳过
                // 操作这个字典的时候，必须加锁，否则就会数据错乱，成了这个样子，无法枚举
                foreach (var elm in _dic)
                {
                    var item = elm.Value;
                    //if (item == null || item.Disposed || item.Socket == null) list.Add(elm.Key);
                    if (item == null || item.Disposed /*|| !item.Active || notactive > 0 && item.Host.Statistics.Last.AddSeconds(notactive) < DateTime.Now*/)
                    {
                        keys.Add(elm.Key);
                        values.Add(elm.Value);
                    }
                }
                foreach (var item in keys)
                {
                    _dic.Remove(item);
                }
            }
            foreach (var item in values)
            {
                item.Dispose();
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

        #region IDictionary<String,ISocketSession> 成员

        void IDictionary<String, ISocketSession>.Add(String key, ISocketSession value) { Add(value); }

        bool IDictionary<String, ISocketSession>.ContainsKey(String key) { return _dic.ContainsKey(key); }

        ICollection<String> IDictionary<String, ISocketSession>.Keys { get { return _dic.Keys; } }

        bool IDictionary<String, ISocketSession>.Remove(String key)
        {
            ISocketSession session;
            if (!_dic.TryGetValue(key, out session)) return false;

            //session.Close();
            session.Dispose();

            return _dic.Remove(key);
        }

        bool IDictionary<String, ISocketSession>.TryGetValue(String key, out ISocketSession value) { return _dic.TryGetValue(key, out value); }

        ICollection<ISocketSession> IDictionary<String, ISocketSession>.Values { get { return _dic.Values; } }

        ISocketSession IDictionary<String, ISocketSession>.this[String key] { get { return _dic[key]; } set { _dic[key] = value; } }

        #endregion

        #region ICollection<KeyValuePair<String,ISocketSession>> 成员

        void ICollection<KeyValuePair<String, ISocketSession>>.Add(KeyValuePair<String, ISocketSession> item)
        {
            throw new XException("不支持！请使用Add(ISocketSession session)方法！");
        }

        bool ICollection<KeyValuePair<String, ISocketSession>>.Contains(KeyValuePair<String, ISocketSession> item) { throw new NotImplementedException(); }

        void ICollection<KeyValuePair<String, ISocketSession>>.CopyTo(KeyValuePair<String, ISocketSession>[] array, int arrayIndex) { throw new NotImplementedException(); }

        bool ICollection<KeyValuePair<String, ISocketSession>>.Remove(KeyValuePair<String, ISocketSession> item) { throw new XException("不支持！请直接销毁会话对象！"); }

        #endregion

        #region IEnumerable<KeyValuePair<String,ISocketSession>> 成员
        IEnumerator<KeyValuePair<String, ISocketSession>> IEnumerable<KeyValuePair<String, ISocketSession>>.GetEnumerator()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}