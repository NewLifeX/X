using System;
using System.Collections;
using System.Collections.Generic;
using NewLife.Threading;
using NewLife.Net.Sockets;

namespace NewLife.Net.Tcp
{
    /// <summary>会话集合。带有自动清理不活动会话的功能</summary>
    class TcpSessionCollection : DisposeBase, ICollection<ISocketSession>
    {
        //List<ISocketSession> _list = new List<ISocketSession>();
        Dictionary<Int32, ISocketSession> _list = new Dictionary<Int32, ISocketSession>();

        private Int32 sessionID = 0;

        /// <summary>添加新会话，并设置会话编号</summary>
        /// <param name="client"></param>
        public void Add(ISocketSession client)
        {
            lock (_list)
            {
                Int32 id = ++sessionID;
                client.OnDisposed += (s, e) => _list.Remove(id);
                _list.Add(id, client);

                if (clearTimer == null) clearTimer = new TimerX(e => RemoveNotAlive(), null, ClearPeriod, ClearPeriod);
            }
        }

        private Int32 _ClearPeriod = 5000;
        /// <summary>清理周期。单位毫秒，默认5000毫秒。</summary>
        public Int32 ClearPeriod { get { return _ClearPeriod; } set { _ClearPeriod = value; } }

        /// <summary>关闭所有</summary>
        public void CloseAll()
        {
            if (clearTimer != null) clearTimer.Dispose();

            if (_list.Count < 1) return;
            lock (_list)
            {
                if (_list.Count < 1) return;

                foreach (var item in _list.Values)
                {
                    if (item == null || item.Disposed || item.Socket == null) continue;

                    item.Close();
                }

                _list.Clear();
            }
        }

        /// <summary>清理会话计时器</summary>
        private TimerX clearTimer;

        /// <summary>移除不活动的会话</summary>
        void RemoveNotAlive()
        {
            if (_list.Count < 1) return;

            lock (_list)
            {
                if (_list.Count < 1) return;

                //for (int i = _list.Count - 1; i >= 0; i--)
                //{
                //    var item = _list[i];
                //    if (item == null || item.Disposed || item.Socket == null) _list.RemoveAt(i);
                //}
                var list = new List<Int32>();
                // 这里可能有问题，曾经见到，_list有元素，但是value为null，这里居然没有进行遍历而直接跳过
                foreach (var elm in _list)
                {
                    var item = elm.Value;
                    if (item == null || item.Disposed || item.Socket == null) list.Add(elm.Key);
                }
                foreach (var item in list)
                {
                    _list.Remove(item);
                }
            }
        }

        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            CloseAll();
        }

        #region 成员
        /// <summary>从集合中移除项</summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Boolean Remove(ISocketSession item) { throw new NetException("不支持！请直接销毁会话对象！"); }

        public void Clear() { _list.Clear(); }

        public bool Contains(ISocketSession item) { return _list.ContainsValue(item); }

        public void CopyTo(ISocketSession[] array, int arrayIndex) { _list.Values.CopyTo(array, arrayIndex); }

        public int Count { get { return _list.Count; } }

        public bool IsReadOnly { get { return (_list as ICollection<ISocketSession>).IsReadOnly; } }

        public IEnumerator<ISocketSession> GetEnumerator() { return _list.Values.GetEnumerator() as IEnumerator<ISocketSession>; }

        IEnumerator IEnumerable.GetEnumerator() { return _list.GetEnumerator(); }
        #endregion
    }
}