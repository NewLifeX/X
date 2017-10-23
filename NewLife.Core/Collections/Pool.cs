using System;
using System.Linq;
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

        /// <summary>空闲对象过期清理时间。默认60s</summary>
        public Int32 Expire { get; set; } = 60;

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

        ///// <summary>销毁</summary>
        ///// <param name="disposing"></param>
        //protected override void OnDispose(Boolean disposing)
        //{
        //    base.OnDispose(disposing);

        //    _timer.TryDispose();
        //}
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
            // 从空闲集合借一个
            if (!_free.TryPop(out var pi) && !_free2.TryDequeue(out pi))
            {
                // 借不到，增加
                if (_busy.Count >= Max)
                {
                    WriteLog("Acquire Max");

                    return default(T);
                }

                pi = new Item
                {
                    Value = Create(),
                };

                WriteLog("Acquire Create");
            }
            else
            {
                FreeCount = _free.Count + _free2.Count;
            }

            // 附加业务
            OnAcquire(pi.Value);

            // 更新过期时间
            pi.ExpireTime = DateTime.Now.AddSeconds(Expire);

            // 加入繁忙集合
            _busy.TryAdd(pi.Value, pi);

            BusyCount = _busy.Count;

            WriteLog("Acquire Free={0} Busy={1}", FreeCount, BusyCount);

            // 启动定期清理的定时器
            StartTimer();

            return pi.Value;
        }

        /// <summary>释放</summary>
        /// <param name="value"></param>
        public void Release(T value)
        {
            if (!_busy.TryRemove(value, out var pi))
            {
                WriteLog("Release Error");

                return;
            }

            BusyCount = _busy.Count;

            // 附加业务
            OnRelease(pi.Value);

            // 更新过期时间
            pi.ExpireTime = DateTime.Now.AddSeconds(Expire);

            // 如果空闲数不足最小值，则返回到基础空闲集合
            if (FreeCount < Min)
                _free.Push(pi);
            else
                _free2.Enqueue(pi);

            FreeCount = _free.Count + _free2.Count;

            WriteLog("Release Free={0} Busy={1}", FreeCount, BusyCount);
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
            if (_free.IsEmpty) return;

            // 遍历并干掉过期项
            var now = DateTime.Now;
            var count = 0;
            while (_free2.TryPeek(out var pi) && pi.ExpireTime < now)
            {
                _free2.TryDequeue(out pi);
                count++;
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
}