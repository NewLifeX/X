using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Threading;

namespace NewLife.Collections
{
    /// <summary>资源池。主要用于数据库连接池和网络连接池</summary>
    /// <typeparam name="T"></typeparam>
    public class Pool<T> : DisposeBase where T : class
    {
        #region 属性
        /// <summary>名称</summary>
        public String Name { get; set; }

        private Int32 _FreeCount;
        /// <summary>空闲个数</summary>
        public Int32 FreeCount { get => _FreeCount; }

        private Int32 _BusyCount;
        /// <summary>繁忙个数</summary>
        public Int32 BusyCount { get => _BusyCount; }

        /// <summary>最大个数。默认100</summary>
        public Int32 Max { get; set; } = 100;

        /// <summary>最小个数。默认1</summary>
        public Int32 Min { get; set; } = 1;

        /// <summary>空闲清理时间。最小个数之上的资源超过空闲时间时被清理，默认10s</summary>
        public Int32 IdleTime { get; set; } = 10;

        /// <summary>完全空闲清理时间。最小个数之下的资源超过空闲时间时被清理，默认0s永不清理</summary>
        public Int32 AllIdleTime { get; set; } = 0;

        /// <summary>申请等待时间。池满时等待的时间，默认1000ms</summary>
        public Int32 WaitTime { get; set; } = 1000;

        /// <summary>基础空闲集合。只保存最小个数，最热部分</summary>
        private ConcurrentStack<Item> _free = new ConcurrentStack<Item>();

        /// <summary>扩展空闲集合。保存最小个数以外部分</summary>
        private ConcurrentQueue<Item> _free2 = new ConcurrentQueue<Item>();

        /// <summary>借出去的放在这</summary>
        private ConcurrentDictionary<T, Item> _busy = new ConcurrentDictionary<T, Item>();

        private Object SyncRoot = new Object();
        #endregion

        #region 构造
        /// <summary>实例化一个资源池</summary>
        public Pool()
        {
            var str = GetType().Name;
            if (str.Contains("`")) str = str.Substring(null, "`");
            if (str != "Pool")
                Name = str;
            else
                Name = $"Pool<{typeof(T).Name}>";
        }

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

        private volatile Boolean _inited;
        private void Init()
        {
            if (_inited) return;

            lock (SyncRoot)
            {
                if (_inited) return;
                _inited = true;

                WriteLog($"Init {typeof(T).FullName} Min={Min} Max={Max} IdleTime={IdleTime}s AllIdleTime={AllIdleTime}s WaitTime={WaitTime}ms");
            }
        }
        #endregion

        #region 内嵌
        class Item
        {
            /// <summary>数值</summary>
            public T Value { get; set; }

            /// <summary>过期时间</summary>
            public DateTime LastTime { get; set; }
        }
        #endregion

        #region 主方法
        /// <summary>申请</summary>
        /// <param name="msTimeout">池满时等待的最大超时时间。超时后仍无法得到资源将抛出异常</param>
        /// <returns></returns>
        public T Acquire(Int32 msTimeout = 0)
        {
            var pi = OnAcquire(msTimeout);
            if (pi == null) return default(T);

            return pi.Value;
        }

        /// <summary>申请资源包装项，Dispose时自动归还到池中</summary>
        /// <param name="msTimeout">池满时等待的最大超时时间。超时后仍无法得到资源将抛出异常</param>
        /// <returns></returns>
        public PoolItem<T> AcquireItem(Int32 msTimeout = 0)
        {
            var pi = OnAcquire(msTimeout);
            if (pi == null) return null;

            return new PoolItem<T>(this, pi.Value);
        }

        /// <summary>申请</summary>
        /// <param name="msTimeout">池满时等待的最大超时时间。超时后仍无法得到资源将抛出异常</param>
        /// <returns></returns>
        private Item OnAcquire(Int32 msTimeout = 0)
        {
            var sw = Log == null || Log == Logger.Null ? null : Stopwatch.StartNew();
            Interlocked.Increment(ref _Total);

            if (msTimeout <= 0) msTimeout = WaitTime;
            var end = TimerX.Now.AddMilliseconds(msTimeout);

            Item pi = null;
            var flag = false;
            while (true)
            {
                flag = false;
                // 从空闲集合借一个
                if (_free.TryPop(out pi) || _free2.TryDequeue(out pi))
                {
                    //FreeCount = _free.Count + _free2.Count;
                    Interlocked.Decrement(ref _FreeCount);
                    flag = true;
                }
                else
                {
                    // 超出最大值后，抛出异常
                    //var count = _busy.Count;
                    var count = BusyCount;
                    if (count >= Max)
                    {
                        // 如果超时时间未到，等一会重试
                        if (msTimeout > 0 && TimerX.Now < end)
                        {
                            Thread.Sleep(10);
                            continue;
                        }

                        var msg = $"申请失败，已有 {count:n0} 达到或超过最大值 {Max:n0}";

                        WriteLog("Acquire Max " + msg);

                        throw new Exception(Name + " " + msg);
                    }

                    // 借不到，增加
                    pi = new Item
                    {
                        Value = Create(),
                    };

                    if (count == 0) Init();
                    WriteLog("Acquire Create Free={0} Busy={1}", FreeCount, count + 1);
                }

                // 抛弃无效资源
                if (OnAcquire(pi.Value)) break;
            }

            if (flag) Interlocked.Increment(ref _Success);

            // 最后时间
            pi.LastTime = TimerX.Now;

            // 加入繁忙集合
            _busy.TryAdd(pi.Value, pi);

            //BusyCount = _busy.Count;
            Interlocked.Increment(ref _BusyCount);

#if DEBUG
            //WriteLog("Acquire Free={0} Busy={1}", FreeCount, BusyCount);
#endif
            if (sw != null)
            {
                sw.Stop();
                var ms = sw.Elapsed.TotalMilliseconds;

                if (Cost < 0.001)
                    Cost = ms;
                else
                    Cost = (Cost * 3 + ms) / 4;
            }

            return pi;
        }

        /// <summary>释放</summary>
        /// <param name="value"></param>
        public void Release(T value)
        {
            if (value == null) return;

            // 从繁忙队列找到并移除缓存项
            if (!_busy.TryRemove(value, out var pi))
            {
                WriteLog("Release Error");

                return;
            }

            //BusyCount = _busy.Count;
            Interlocked.Decrement(ref _BusyCount);

            // 抛弃无效资源
            if (!OnRelease(pi.Value)) return;

            //// 确保空闲队列个数最少是CPU个数
            //var min = Environment.ProcessorCount;
            //if (min < Min) min = Min;
            var min = Min;

            // 如果空闲数不足最小值，则返回到基础空闲集合
            if (_FreeCount < min)
                _free.Push(pi);
            else
                _free2.Enqueue(pi);

            // 最后时间
            pi.LastTime = TimerX.Now;

            //FreeCount = _free.Count + _free2.Count;
            Interlocked.Increment(ref _FreeCount);

            // 启动定期清理的定时器
            StartTimer();

#if DEBUG
            //WriteLog("Release Free={0} Busy={1}", FreeCount, BusyCount);
#endif
        }
        #endregion

        #region 重载
        /// <summary>创建实例时触发。外部可用于自定义创建对象</summary>
        public event EventHandler<EventArgs<T>> OnCreate;

        /// <summary>创建实例</summary>
        /// <returns></returns>
        protected virtual T Create()
        {
            if (OnCreate != null)
            {
                var e = new EventArgs<T>(default(T));
                OnCreate(this, e);
                if (e.Arg != null) return e.Arg;
            }
            return typeof(T).CreateInstance() as T;
        }

        /// <summary>申请时，返回是否有效。无效资源将会被抛弃并重新申请</summary>
        /// <param name="value"></param>
        protected virtual Boolean OnAcquire(T value) { return true; }

        /// <summary>释放时，返回是否有效。无效资源将会被抛弃，不再加入空闲队列</summary>
        /// <param name="value"></param>
        protected virtual Boolean OnRelease(T value)
        {
            var db = value as DisposeBase;
            if (db != null && db.Disposed) return false;

            return true;
        }

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
            //// 总数小于等于最小个数时不处理
            //if (FreeCount + BusyCount <= Min) return;

            // 遍历并干掉过期项
            var count = 0;

            // 总数小于等于最小个数时不处理
            if (IdleTime > 0 && !_free2.IsEmpty && FreeCount + BusyCount > Min)
            {
                var exp = TimerX.Now.AddSeconds(-IdleTime);
                // 移除扩展空闲集合里面的超时项
                while (_free2.TryPeek(out var pi) && pi.LastTime < exp)
                {
                    // 取出来销毁
                    if (_free2.TryDequeue(out pi))
                    {
                        OnDestroy(pi.Value);

                        count++;
                        Interlocked.Decrement(ref _FreeCount);
                    }
                }
            }

            if (AllIdleTime > 0 && !_free.IsEmpty)
            {
                var exp = TimerX.Now.AddSeconds(-AllIdleTime);
                // 移除基础空闲集合里面的超时项
                while (_free.TryPeek(out var pi) && pi.LastTime < exp)
                {
                    // 取出来销毁
                    if (_free.TryPop(out pi))
                    {
                        OnDestroy(pi.Value);

                        count++;
                        Interlocked.Decrement(ref _FreeCount);
                    }
                }
            }

            if (count > 0)
            {
                //_FreeCount = _free.Count + _free2.Count;
                //_BusyCount = _busy.Count;

                var p = Total == 0 ? 0 : (Double)Success / Total;

                WriteLog("Release Free={0} Busy={1} 清除过期资源 {2:n0} 项。总请求 {3:n0} 次，命中 {4:p2}，平均 {5:n2}us", FreeCount, BusyCount, count, Total, p, Cost * 1000);
            }
        }
        #endregion

        #region 统计
        private Int32 _Total;
        /// <summary>总请求数</summary>
        public Int32 Total { get => _Total; }

        private Int32 _Success;
        /// <summary>成功数</summary>
        public Int32 Success { get => _Success; }

        /// <summary>平均耗时。单位ms</summary>
        private Double Cost;
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args)
        {
            if (Log == null) return;

            Log.Info(Name + "." + format, args);
        }
        #endregion
    }

    /// <summary>资源池包装项，自动归还资源到池中</summary>
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