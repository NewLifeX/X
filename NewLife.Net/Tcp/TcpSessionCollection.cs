using System;
using System.Collections;
using System.Collections.Generic;
using NewLife.Threading;

namespace NewLife.Net.Tcp
{
    /// <summary>Tcp会话集合。其中Tcp会话使用弱引用，允许GC及时回收不再使用的会话。</summary>
    class TcpSessionCollection : DisposeBase, ICollection<TcpClientX>
    {
        List<TcpClientX> _list = new List<TcpClientX>();

        private Int32 sessionID = 0;
        /// <summary>添加新会话，并设置会话编号</summary>
        /// <param name="client"></param>
        public void Add(TcpClientX client)
        {
            lock (_list)
            {
                client.OnDisposed += (s, e) => Remove(client);

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

                foreach (var item in _list)
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

                for (int i = _list.Count - 1; i >= 0; i--)
                {
                    var item = _list[i];
                    if (item == null || item.Disposed || item.Socket == null) _list.RemoveAt(i);
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
        /// <param name="key"></param>
        /// <returns></returns>
        public Boolean Remove(TcpClientX item)
        {
            lock (_list) { return _list.Remove(item); }
        }

        public void Clear() { _list.Clear(); }

        public bool Contains(TcpClientX item) { return _list.Contains(item); }

        public void CopyTo(TcpClientX[] array, int arrayIndex) { _list.CopyTo(array, arrayIndex); }

        public int Count { get { return _list.Count; } }

        public bool IsReadOnly { get { return (_list as ICollection<TcpClientX>).IsReadOnly; } }

        public IEnumerator<TcpClientX> GetEnumerator() { return _list.GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator() { return _list.GetEnumerator(); }
        #endregion
    }
}