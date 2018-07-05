using System;
using System.Text;
using System.Threading;
using NewLife.Log;

namespace NewLife.Threading
{
    /// <summary>不可重入的定时器。</summary>
    /// <remarks>
    /// 为了避免系统的Timer可重入的问题，差别在于本地调用完成后才开始计算时间间隔。这实际上也是经常用到的。
    /// 
    /// 因为挂载在静态列表上，必须从外部主动调用<see cref="IDisposable.Dispose"/>才能销毁定时器。
    /// 
    /// 该定时器不能放入太多任务，否则适得其反！
    /// 
    /// TimerX必须维持对象，否则很容易被GC回收。
    /// </remarks>
    public class TimerX : /*DisposeBase*/IDisposable
    {
        #region 属性
        /// <summary>所属调度器</summary>
        public TimerScheduler Scheduler { get; private set; }

        /// <summary>获取/设置 回调</summary>
        public WeakAction<Object> Callback { get; set; }

        /// <summary>获取/设置 用户数据</summary>
        public Object State { get; set; }

        /// <summary>获取/设置 下一次调用时间</summary>
        public DateTime NextTime { get; set; }

        /// <summary>获取/设置 调用次数</summary>
        public Int32 Timers { get; internal set; }

        /// <summary>获取/设置 间隔周期。毫秒，设为0或-1则只调用一次</summary>
        public Int32 Period { get; set; }

        /// <summary>获取/设置 异步执行任务。默认false</summary>
        public Boolean Async { get; set; }

        /// <summary>获取/设置 绝对精确时间执行。默认false</summary>
        public Boolean Absolutely { get; set; }

        /// <summary>调用中</summary>
        public Boolean Calling { get; internal set; }

        /// <summary>平均耗时。毫秒</summary>
        public Int32 Cost { get; internal set; }

        /// <summary>判断任务是否执行的委托。一般跟异步配合使用，避免频繁从线程池借出线程</summary>
        public Func<Boolean> CanExecute { get; set; }
        #endregion

        #region 静态
        [ThreadStatic]
        private static TimerX _Current;
        /// <summary>当前定时器</summary>
        public static TimerX Current { get { return _Current; } internal set { _Current = value; } }
        #endregion

        #region 构造
        /// <summary>实例化一个不可重入的定时器</summary>
        /// <param name="callback">委托</param>
        /// <param name="state">用户数据</param>
        /// <param name="dueTime">多久之后开始。毫秒</param>
        /// <param name="period">间隔周期。毫秒</param>
        /// <param name="scheduler">调度器</param>
        public TimerX(WaitCallback callback, Object state, Int32 dueTime, Int32 period, String scheduler = null)
        {
            if (dueTime < 0) throw new ArgumentOutOfRangeException(nameof(dueTime));
            //if (period < 0) throw new ArgumentOutOfRangeException("period");

            Callback = new WeakAction<Object>(callback) ?? throw new ArgumentNullException(nameof(callback));
            State = state;
            Period = period;

            NextTime = DateTime.Now.AddMilliseconds(dueTime);

            Scheduler = scheduler.IsNullOrEmpty() ? TimerScheduler.Default : TimerScheduler.Create(scheduler);
            Scheduler.Add(this);
        }

        /// <summary>实例化一个绝对定时器</summary>
        /// <param name="callback">委托</param>
        /// <param name="state">用户数据</param>
        /// <param name="startTime">绝对开始时间</param>
        /// <param name="period">间隔周期。毫秒</param>
        /// <param name="scheduler">调度器</param>
        public TimerX(WaitCallback callback, Object state, DateTime startTime, Int32 period, String scheduler = null)
        {
            if (startTime <= DateTime.MinValue) throw new ArgumentOutOfRangeException(nameof(startTime));
            //if (period < 0) throw new ArgumentOutOfRangeException("period");

            Callback = new WeakAction<Object>(callback) ?? throw new ArgumentNullException(nameof(callback));
            State = state;
            Period = period;
            Absolutely = true;

            var now = DateTime.Now;
            var next = startTime;
            if (period % 1000 == 0)
            {
                var s = period / 1000;
                while (next < now) next = next.AddSeconds(s);
            }
            else
            {
                while (next < now) next = next.AddMilliseconds(period);
            }
            NextTime = next;

            Scheduler = scheduler.IsNullOrEmpty() ? TimerScheduler.Default : TimerScheduler.Create(scheduler);
            Scheduler.Add(this);
        }

        /// <summary>销毁定时器</summary>
        public void Dispose() => Scheduler?.Remove(this);
        #endregion

        #region 方法
        /// <summary>是否已设置下一次时间</summary>
        internal Boolean hasSetNext;

        /// <summary>设置下一次运行时间</summary>
        /// <param name="ms">小于等于0表示马上调度</param>
        public void SetNext(Int32 ms)
        {
            NextTime = DateTime.Now.AddMilliseconds(ms);

            hasSetNext = true;

            Scheduler.Wake();
        }
        #endregion

        #region 静态方法
        /// <summary>延迟执行一个委托</summary>
        /// <param name="callback"></param>
        /// <param name="ms"></param>
        /// <returns></returns>
        public static TimerX Delay(WaitCallback callback, Int32 ms) => new TimerX(callback, null, ms, 0) { Async = true };

        private static TimerX _NowTimer;
        private static DateTime _Now;
        /// <summary>当前时间。定时读取系统时间，避免频繁读取系统时间造成性能瓶颈</summary>
        public static DateTime Now
        {
            get
            {
                if (_NowTimer == null)
                {
                    lock (TimerScheduler.Default)
                    {
                        if (_NowTimer == null)
                        {
                            _NowTimer = new TimerX(CopyNow, null, 0, 500);
                        }
                    }
                }

                return _Now;
            }
        }

        private static void CopyNow(Object state) => _Now = DateTime.Now;
        #endregion

        #region 辅助
        /// <summary>已重载</summary>
        /// <returns></returns>
        public override String ToString() => Callback + "";
        #endregion
    }
}