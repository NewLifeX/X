using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using NewLife.Log;
using NewLife.Threading;

namespace NewLife.Net
{
    /// <summary>会话集合。带有自动清理不活动会话的功能</summary>
    class SessionCollection : DisposeBase, IDictionary<String, ISocketSession>
    {
        #region 属性
        ConcurrentDictionary<String, ISocketSession> _dic = new ConcurrentDictionary<String, ISocketSession>();

        /// <summary>服务端</summary>
        public ISocketServer Server { get; private set; }

        /// <summary>清理周期。单位毫秒，默认10秒。</summary>
        public Int32 ClearPeriod { get; set; } = 10;

        /// <summary>清理会话计时器</summary>
        private TimerX clearTimer;
        #endregion

        #region 构造
        public SessionCollection(ISocketServer server)
        {
            Server = server;

            var p = ClearPeriod * 1000;
            clearTimer = new TimerX(RemoveNotAlive, null, p, p)
            {
                Async = true,
                CanExecute = () => _dic.Any(),
            };
        }

        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            clearTimer.TryDispose();

            CloseAll();
        }
        #endregion

        #region 主要方法
        /// <summary>添加新会话，并设置会话编号</summary>
        /// <param name="session"></param>
        /// <returns>返回添加新会话是否成功</returns>
        public Boolean Add(ISocketSession session)
        {
            var key = session.Remote.EndPoint + "";
            if (_dic.ContainsKey(key)) return false;

            session.OnDisposed += (s, e) => { _dic.Remove((s as ISocketSession).Remote.EndPoint + ""); };
            _dic.TryAdd(key, session);

            return true;
        }

        /// <summary>获取会话，加锁</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public ISocketSession Get(String key)
        {
            if (!_dic.TryGetValue(key, out var session)) return null;

            return session;
        }

        /// <summary>关闭所有</summary>
        public void CloseAll()
        {
            if (!_dic.Any()) return;

            foreach (var item in _dic.ToValueArray())
            {
                if (item != null && !item.Disposed) item.TryDispose();
            }
        }

        /// <summary>移除不活动的会话</summary>
        void RemoveNotAlive(Object state)
        {
            if (!_dic.Any()) return;

            var timeout = 30;
            if (Server != null) timeout = Server.SessionTimeout;
            var keys = new List<String>();
            var values = new List<ISocketSession>();
            // 估算完成时间，执行过长时提示
            using (var tc = new TimeCost("{0}.RemoveNotAlive".F(GetType().Name), 100))
            {
                tc.Log = Server.Log;

                foreach (var elm in _dic)
                {
                    var item = elm.Value;
                    // 判断是否已超过最大不活跃时间
                    if (item == null || item.Disposed || timeout > 0 && IsNotAlive(item, timeout))
                    {
                        keys.Add(elm.Key);
                        values.Add(elm.Value);
                    }
                }
                // 从会话集合里删除这些键值，现在在锁内部，操作安全
                foreach (var item in keys)
                {
                    _dic.Remove(item);
                }
            }
            // 已经离开了锁，慢慢释放各个会话
            foreach (var item in values)
            {
                item.WriteLog("超过{0}秒不活跃销毁 {1}", timeout, item);

                //item.Dispose();
                item.TryDispose();
            }
        }

        Boolean IsNotAlive(ISocketSession session, Int32 timeout)
        {
            // 如果有最后时间则判断最后时间，否则判断开始时间
            var time = session.LastTime > DateTime.MinValue ? session.LastTime : session.StartTime;
            return time.AddSeconds(timeout) < DateTime.Now;
        }
        #endregion

        #region 成员
        public void Clear() => _dic.Clear();

        public Int32 Count => _dic.Count;

        public Boolean IsReadOnly => (_dic as IDictionary<Int32, ISocketSession>).IsReadOnly;

        public IEnumerator<ISocketSession> GetEnumerator() => _dic.Values.GetEnumerator() as IEnumerator<ISocketSession>;

        IEnumerator IEnumerable.GetEnumerator() => _dic.GetEnumerator();
        #endregion

        #region IDictionary<String,ISocketSession> 成员

        void IDictionary<String, ISocketSession>.Add(String key, ISocketSession value) => Add(value);

        Boolean IDictionary<String, ISocketSession>.ContainsKey(String key) => _dic.ContainsKey(key);

        ICollection<String> IDictionary<String, ISocketSession>.Keys => _dic.Keys;

        Boolean IDictionary<String, ISocketSession>.Remove(String key)
        {
            if (!_dic.TryGetValue(key, out var session)) return false;

            //session.Close();
            session.Dispose();

            return _dic.Remove(key);
        }

        Boolean IDictionary<String, ISocketSession>.TryGetValue(String key, out ISocketSession value) => _dic.TryGetValue(key, out value);

        ICollection<ISocketSession> IDictionary<String, ISocketSession>.Values => _dic.Values;

        ISocketSession IDictionary<String, ISocketSession>.this[String key] { get { return _dic[key]; } set { _dic[key] = value; } }

        #endregion

        #region ICollection<KeyValuePair<String,ISocketSession>> 成员

        void ICollection<KeyValuePair<String, ISocketSession>>.Add(KeyValuePair<String, ISocketSession> item) => throw new XException("不支持！请使用Add(ISocketSession session)方法！");

        Boolean ICollection<KeyValuePair<String, ISocketSession>>.Contains(KeyValuePair<String, ISocketSession> item) => throw new NotImplementedException();

        void ICollection<KeyValuePair<String, ISocketSession>>.CopyTo(KeyValuePair<String, ISocketSession>[] array, Int32 arrayIndex) => throw new NotImplementedException();

        Boolean ICollection<KeyValuePair<String, ISocketSession>>.Remove(KeyValuePair<String, ISocketSession> item) => throw new XException("不支持！请直接销毁会话对象！");

        #endregion

        #region IEnumerable<KeyValuePair<String,ISocketSession>> 成员
        IEnumerator<KeyValuePair<String, ISocketSession>> IEnumerable<KeyValuePair<String, ISocketSession>>.GetEnumerator() => throw new NotImplementedException();
        #endregion
    }
}