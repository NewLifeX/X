using System;
using System.Collections.Concurrent;
using NewLife.Log;
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

        /// <summary>最小个数。默认1</summary>
        public Int32 Min { get; set; } = 1;

        /// <summary>空闲对象过期清理时间。默认10s</summary>
        public Int32 Expire { get; set; } = 10;

        /// <summary>基础空闲集合。只保存最小个数，最热部分</summary>
        private ConcurrentStack<Item> _free = new ConcurrentStack<Item>();

        /// <summary>扩展空闲集合。保存最小个数以外部分</summary>
        private ConcurrentQueue<Item> _free2 = new ConcurrentQueue<Item>();

        /// <summary>借出去的放在这</summary>
        private ConcurrentDictionary<T, Item> _busy = new ConcurrentDictionary<T, Item>();
        #endregion

        #region 构造
        ///// <summary>实例化一个对象池</summary>
        //public Pool() { }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            _timer.TryDispose();

            while (_free.TryPop(out var pi)) OnDestroy(pi.Value);
            while (_free2.TryDequeue(out var pi)) OnDestroy(pi.Value);
            _busy.Clear();
        }
        #endregion

        #region 内嵌
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
        /// <returns></returns>
        public T Acquire()
        {
            var pi = OnAcquire();
            if (pi == null) return default(T);

            return pi.Value;
        }

        /// <summary>申请对象包装项，Dispose时自动归还到池中</summary>
        /// <returns></returns>
        public PoolItem<T> AcquireItem()
        {
            var pi = OnAcquire();
            if (pi == null) return null;

            return new PoolItem<T>(this, pi.Value);
        }

        /// <summary>申请</summary>
        /// <returns></returns>
        private Item OnAcquire()
        {
            Item pi = null;
            while (true)
            {
                // 从空闲集合借一个
                if (!_free.TryPop(out pi) && !_free2.TryDequeue(out pi))
                {
                    // 超出最大值后，抛出异常
                    var count = _busy.Count;
                    if (count >= Max)
                    {
                        var msg = $"申请失败，已有 {count:n0} 达到或超过最大值 {Max:n0}";

                        WriteLog("Acquire Max " + msg);

                        throw new Exception($"Pool<{typeof(T).Name}>{msg}");
                    }

                    // 借不到，增加
                    pi = new Item
                    {
                        Value = OnCreate(),
                    };

                    WriteLog("Acquire Create");
                }
                else
                {
                    FreeCount = _free.Count + _free2.Count;
                }

                // 抛弃无效对象
                if (OnAcquire(pi.Value)) break;
            }

            // 更新过期时间
            pi.ExpireTime = DateTime.Now.AddSeconds(Expire);

            // 加入繁忙集合
            _busy.TryAdd(pi.Value, pi);

            BusyCount = _busy.Count;

#if DEBUG
            WriteLog("Acquire Free={0} Busy={1}", FreeCount, BusyCount);
#endif

            return pi;
        }

        /// <summary>释放</summary>
        /// <param name="value"></param>
        public void Release(T value)
        {
            if (value == null) return;

            if (!_busy.TryRemove(value, out var pi))
            {
                WriteLog("Release Error");

                return;
            }

            BusyCount = _busy.Count;

            // 抛弃无效对象
            if (!OnRelease(pi.Value)) return;

            var exp = Expire;

            // 确保空闲队列个数最少是CPU个数
            var min = Environment.ProcessorCount;
            if (min < Min) min = Min;

            // 如果空闲数不足最小值，则返回到基础空闲集合
            if (FreeCount < min)
            {
                _free.Push(pi);
                // 基础空闲集合，有效期翻倍
                exp *= 2;
            }
            else
                _free2.Enqueue(pi);

            // 更新过期时间
            pi.ExpireTime = DateTime.Now.AddSeconds(exp);

            FreeCount = _free.Count + _free2.Count;

            // 启动定期清理的定时器
            StartTimer();

#if DEBUG
            WriteLog("Release Free={0} Busy={1}", FreeCount, BusyCount);
#endif
        }
        #endregion

        #region 重载
        /// <summary>创建实例</summary>
        /// <returns></returns>
        protected virtual T OnCreate() { return typeof(T).CreateInstance() as T; }

        /// <summary>申请时，返回是否有效。无效对象将会被抛弃</summary>
        /// <param name="value"></param>
        protected virtual Boolean OnAcquire(T value) { return true; }

        /// <summary>释放时，返回是否有效。无效对象将会被抛弃</summary>
        /// <param name="value"></param>
        protected virtual Boolean OnRelease(T value) { return true; }

        /// <summary>销毁时触发</summary>
        /// <param name="value"></param>
        protected virtual void OnDestroy(T value) { value.TryDispose(); }
        #endregion

        #region 定期清理
        private TimerX _timer;

        private void StartTimer()
        {
            if (_timer != null) return;
            lock (this)
            {
                if (_timer != null) return;

                _timer = new TimerX(Work, null, 5000, 5000);
            }
        }

        private void Work(Object state)
        {
            // 总数小于等于最小个数时不处理
            if (FreeCount + BusyCount <= Min) return;

            // 遍历并干掉过期项
            var now = DateTime.Now;
            var count = 0;

            if (!_free2.IsEmpty)
            {
                // 移除扩展空闲集合里面的超时项
                while (_free2.TryPeek(out var pi) && pi.ExpireTime < now)
                {
                    // 取出来销毁
                    if (_free2.TryDequeue(out pi)) OnDestroy(pi.Value);

                    count++;
                }
            }

            if (!_free.IsEmpty)
            {
                // 基础空闲集合
                while (_free.Count > Min && _free.TryPeek(out var pi) && pi.ExpireTime < now)
                {
                    // 取出来销毁
                    if (_free.TryPop(out pi)) OnDestroy(pi.Value);

                    count++;
                }
            }

            if (count > 0)
            {
                FreeCount = _free.Count + _free2.Count;
                WriteLog("Release Free={0} Busy={1} 清除过期对象 {2:n0} 项", FreeCount, BusyCount, count);
            }
        }
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;

        private String _Prefix;
        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args)
        {
            if (Log == null) return;

            if (_Prefix == null)
            {
                _Prefix = $"Pool<{typeof(T).Name}>.";
            }

            Log.Info(_Prefix + format, args);
        }
        #endregion
    }

    /// <summary>对象池包装项，自动归还对象到池中</summary>
    /// <typeparam name="T"></typeparam>
    public class PoolItem<T> : DisposeBase where T : class
    {
        #region 属性
        /// <summary>数值</summary>
        public T Value { get; }

        /// <summary>池</summary>
        public Pool<T> Pool { get; }
        #endregion

        #region 构造
        /// <summary>包装项</summary>
        /// <param name="pool"></param>
        /// <param name="value"></param>
        public PoolItem(Pool<T> pool, T value)
        {
            Pool = pool;
            Value = value;
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            Pool.Release(Value);
        }
        #endregion
    }
}