using System;
using System.Collections.Concurrent;
using NewLife.Reflection;

namespace NewLife.Collections
{
    /// <summary>对象池。主要用于数据库连接池和网络连接池</summary>
    /// <typeparam name="T"></typeparam>
    public class Pool<T> where T : class
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
        #endregion

        #region 内嵌
        private BlockingCollection<Item> _free = new BlockingCollection<Item>();
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
    }
}