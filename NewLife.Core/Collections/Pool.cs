using System;
using System.Collections.Concurrent;
using System.Linq;
using NewLife.Reflection;
using NewLife.Threading;

namespace NewLife.Collections
{
    /// <summary>对象池。主要用于数据库连接池和网络连接池</summary>
    /// <typeparam name="T"></typeparam>
    public class Pool<T> : DisposeBase where T : class
    {
        #region 属性
        /// <summary>空闲个数</summary>
        public Int32 FreeCount { get; private set; }

        /// <summary>繁忙个数</summary>
        public Int32 BusyCount { get; private set; }

        /// <summary>最大个数。默认100</summary>
        public Int32 Max { get; set; } = 100;

        /// <summary>最小个数。默认0</summary>
        public Int32 Min { get; set; }

        /// <summary>空闲对象过期清理时间。默认60s</summary>
        public Int32 Expire { get; set; } = 60;
        #endregion

        #region 构造
        /// <summary>实例化一个对象池</summary>
        public Pool()
        {
            _free = new BlockingCollection<Item>(_stack);
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            _timer.TryDispose();
        }
        #endregion

        #region 内嵌
        private ConcurrentStack<Item> _stack = new ConcurrentStack<Item>();
        private BlockingCollection<Item> _free;
        private ConcurrentDictionary<T, Item> _busy = new ConcurrentDictionary<T, Item>();

        class Item
        {
            /// <summary>数值</summary>
            public T Value { get; set; }

            /// <summary>过期时间</summary>
            public DateTime ExpireTime { get; set; }
        }
        #endregion

        #region 主方法
        /// <summary>申请</summary>
        /// <param name="msTimerout">超时时间。默认1000ms</param>
        /// <returns></returns>
        public T Acquire(Int32 msTimerout = 1000)
        {
            // 从空闲集合借一个
            if (!_free.TryTake(out var pi))
            {
                // 借不到，增加
                if (_busy.Count < Max)
                {
                    pi = new Item
                    {
                        Value = Create(),
                    };
                }
                else
                {
                    // 池满，等待
                    if (!_free.TryTake(out pi, msTimerout)) return default(T);
                }
            }

            // 附加业务
            OnAcquire(pi.Value);

            // 更新过期时间
            pi.ExpireTime = DateTime.Now.AddSeconds(Expire);

            // 加入繁忙集合
            _busy.TryAdd(pi.Value, pi);

            FreeCount = _free.Count;
            BusyCount = _busy.Count;

            // 启动定期清理的定时器
            StartTimer();

            return pi.Value;
        }

        /// <summary>释放</summary>
        /// <param name="value"></param>
        public void Release(T value)
        {
            if (!_busy.TryRemove(value, out var pi)) return;

            // 附加业务
            OnRelease(pi.Value);

            // 更新过期时间
            pi.ExpireTime = DateTime.Now.AddSeconds(Expire);

            _free.TryAdd(pi);

            FreeCount = _free.Count;
            BusyCount = _busy.Count;
        }
        #endregion

        #region 重载
        /// <summary>创建实例</summary>
        /// <returns></returns>
        protected virtual T Create() { return typeof(T).CreateInstance() as T; }

        /// <summary>申请时</summary>
        /// <param name="value"></param>
        protected virtual void OnAcquire(T value) { }

        /// <summary>释放时</summary>
        /// <param name="value"></param>
        protected virtual void OnRelease(T value) { }
        #endregion

        #region 定期清理
        private TimerX _timer;

        private void StartTimer()
        {
            if (_timer != null) return;
            lock (this)
            {
                if (_timer != null) return;

                _timer = new TimerX(Work, null, 10000, 10000);
            }
        }

        private void Work(Object state)
        {
            // 总数小于等于最小个数时不处理
            if (FreeCount + BusyCount <= Min) return;

            // 没有空闲时也不处理
            var st = _stack;
            if (st.Count == 0) return;

            // 遍历并干掉过期项
            var now = DateTime.Now;
            // 栈是FILO结构，过期的都在前面
            while (st.TryPeek(out var pi) && pi.ExpireTime < now)
            {
                st.TryPop(out pi);
            }
        }
        #endregion
    }
}