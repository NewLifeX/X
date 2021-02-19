using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace NewLife.Threading
{
    /// <summary>不可重入的定时器，支持Cron</summary>
    /// <remarks>
    /// 文档 https://www.yuque.com/smartstone/nx/timerx
    /// 
    /// 为了避免系统的Timer可重入的问题，差别在于本地调用完成后才开始计算时间间隔。这实际上也是经常用到的。
    /// 
    /// 因为挂载在静态列表上，必须从外部主动调用<see cref="IDisposable.Dispose"/>才能销毁定时器。
    /// 但是要注意GC回收定时器实例。
    /// 
    /// 该定时器不能放入太多任务，否则适得其反！
    /// 
    /// TimerX必须维持对象，否则Scheduler也没有维持对象时，大家很容易一起被GC回收。
    /// </remarks>
    public class TimerX : IDisposable
    {
        #region 属性
        /// <summary>编号</summary>
        public Int32 Id { get; internal set; }

        /// <summary>所属调度器</summary>
        public TimerScheduler Scheduler { get; private set; }

        /// <summary>目标对象。弱引用，使得调用方对象可以被GC回收</summary>
        internal readonly WeakReference Target;

        /// <summary>委托方法</summary>
        internal readonly MethodInfo Method;

        internal readonly Boolean IsAsyncTask;

        /// <summary>获取/设置 用户数据</summary>
        public Object? State { get; set; }

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
        public Func<Boolean>? CanExecute { get; set; }

        private DateTime _AbsolutelyNext;
        private Cron? _cron;
        #endregion

        #region 静态
        [ThreadStatic]
        private static TimerX? _Current;
        /// <summary>当前定时器</summary>
        public static TimerX? Current { get => _Current; internal set => _Current = value; }
        #endregion

        #region 构造
        private TimerX(Object? target, MethodInfo method, Object? state, String? scheduler = null)
        {
            Target = new WeakReference(target);
            Method = method;
            State = state;

            Scheduler = (scheduler == null || scheduler.IsNullOrEmpty()) ? TimerScheduler.Default : TimerScheduler.Create(scheduler);
            //Scheduler.Add(this);
        }

        private void Init(DateTime nextTime)
        {
            NextTime = nextTime;

            Scheduler.Add(this);
        }

        /// <summary>实例化一个不可重入的定时器</summary>
        /// <param name="callback">委托</param>
        /// <param name="state">用户数据</param>
        /// <param name="dueTime">多久之后开始。毫秒</param>
        /// <param name="period">间隔周期。毫秒</param>
        /// <param name="scheduler">调度器</param>
        public TimerX(TimerCallback callback, Object? state, Int32 dueTime, Int32 period, String? scheduler = null) : this(callback.Target, callback.Method, state, scheduler)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            if (dueTime < 0) throw new ArgumentOutOfRangeException(nameof(dueTime));

            Period = period;

            Init(DateTime.Now.AddMilliseconds(dueTime));
        }

        /// <summary>实例化一个不可重入的定时器</summary>
        /// <param name="callback">委托</param>
        /// <param name="state">用户数据</param>
        /// <param name="dueTime">多久之后开始。毫秒</param>
        /// <param name="period">间隔周期。毫秒</param>
        /// <param name="scheduler">调度器</param>
        public TimerX(Func<Object, Task> callback, Object? state, Int32 dueTime, Int32 period, String? scheduler = null) : this(callback.Target, callback.Method, state, scheduler)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            if (dueTime < 0) throw new ArgumentOutOfRangeException(nameof(dueTime));

            IsAsyncTask = true;
            Async = true;
            Period = period;

            Init(DateTime.Now.AddMilliseconds(dueTime));
        }

        /// <summary>实例化一个绝对定时器，指定时刻执行，跟当前时间和SetNext无关</summary>
        /// <param name="callback">委托</param>
        /// <param name="state">用户数据</param>
        /// <param name="startTime">绝对开始时间</param>
        /// <param name="period">间隔周期。毫秒</param>
        /// <param name="scheduler">调度器</param>
        public TimerX(TimerCallback callback, Object state, DateTime startTime, Int32 period, String? scheduler = null) : this(callback.Target, callback.Method, state, scheduler)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            if (startTime <= DateTime.MinValue) throw new ArgumentOutOfRangeException(nameof(startTime));

            Period = period;
            Absolutely = true;

            var now = DateTime.Now;
            var next = startTime;
            while (next < now) next = next.AddMilliseconds(period);

            Init(_AbsolutelyNext = next);
        }

        /// <summary>实例化一个绝对定时器，指定时刻执行，跟当前时间和SetNext无关</summary>
        /// <param name="callback">委托</param>
        /// <param name="state">用户数据</param>
        /// <param name="startTime">绝对开始时间</param>
        /// <param name="period">间隔周期。毫秒</param>
        /// <param name="scheduler">调度器</param>
        public TimerX(Func<Object, Task> callback, Object state, DateTime startTime, Int32 period, String? scheduler = null) : this(callback.Target, callback.Method, state, scheduler)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            if (startTime <= DateTime.MinValue) throw new ArgumentOutOfRangeException(nameof(startTime));

            IsAsyncTask = true;
            Async = true;
            Period = period;
            Absolutely = true;

            var now = DateTime.Now;
            var next = startTime;
            while (next < now) next = next.AddMilliseconds(period);

            Init(_AbsolutelyNext = next);
        }

        /// <summary>实例化一个Cron定时器</summary>
        /// <param name="callback">委托</param>
        /// <param name="state">用户数据</param>
        /// <param name="cronExpression">Cron表达式</param>
        /// <param name="scheduler">调度器</param>
        public TimerX(TimerCallback callback, Object state, String cronExpression, String? scheduler = null) : this(callback.Target, callback.Method, state, scheduler)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            if (cronExpression.IsNullOrEmpty()) throw new ArgumentNullException(nameof(cronExpression));

            _cron = new Cron();
            if (!_cron.Parse(cronExpression)) throw new ArgumentException("无效的Cron表达式", nameof(cronExpression));

            Absolutely = true;

            Init(_AbsolutelyNext = _cron.GetNext(DateTime.Now));
        }

        /// <summary>实例化一个Cron定时器</summary>
        /// <param name="callback">委托</param>
        /// <param name="state">用户数据</param>
        /// <param name="cronExpression">Cron表达式</param>
        /// <param name="scheduler">调度器</param>
        public TimerX(Func<Object, Task> callback, Object state, String cronExpression, String? scheduler = null) : this(callback.Target, callback.Method, state, scheduler)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            if (cronExpression.IsNullOrEmpty()) throw new ArgumentNullException(nameof(cronExpression));

            _cron = new Cron();
            if (!_cron.Parse(cronExpression)) throw new ArgumentException("无效的Cron表达式", nameof(cronExpression));

            IsAsyncTask = true;
            Async = true;
            Absolutely = true;

            Init(_AbsolutelyNext = _cron.GetNext(DateTime.Now));
        }

        /// <summary>销毁定时器</summary>
        public void Dispose()
        {
            Dispose(true);

            // 告诉GC，不要调用析构函数
            GC.SuppressFinalize(this);
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(Boolean disposing)
        {
            if (disposing)
            {
                // 释放托管资源
            }

            // 释放非托管资源
            Scheduler?.Remove(this, disposing ? "Dispose" : "GC");
        }
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

        /// <summary>设置下一次执行时间，并获取间隔</summary>
        /// <returns>返回下一次执行的间隔时间，不能小于等于0，否则定时器被销毁</returns>
        internal Int32 SetAndGetNextTime()
        {
            // 如果已设置
            var period = Period;
            if (hasSetNext)
            {
                var ts = (Int32)(NextTime - DateTime.Now).TotalMilliseconds;
                return ts > 0 ? ts : period;
            }

            if (Absolutely)
            {
                if (_cron != null)
                    NextTime = _AbsolutelyNext = _cron.GetNext(_AbsolutelyNext);
                else
                    NextTime = _AbsolutelyNext = _AbsolutelyNext.AddMilliseconds(period);
                var ts = (Int32)(NextTime - DateTime.Now).TotalMilliseconds;
                return ts > 0 ? ts : period;
            }
            else
            {
                NextTime = DateTime.Now.AddMilliseconds(period);
                return period;
            }
        }
        #endregion

        #region 静态方法
        /// <summary>延迟执行一个委托。特别要小心，很可能委托还没被执行，对象就被gc回收了</summary>
        /// <param name="callback"></param>
        /// <param name="ms"></param>
        /// <returns></returns>
        public static TimerX Delay(TimerCallback callback, Int32 ms) => new TimerX(callback, null, ms, 0) { Async = true };

        private static TimerX? _NowTimer;
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
                            // 多线程下首次访问Now可能取得空时间
                            _Now = DateTime.Now;

                            _NowTimer = new TimerX(CopyNow, null, 0, 500);
                        }
                    }
                }

                return _Now;
            }
        }

        private static void CopyNow(Object? state) => _Now = DateTime.Now;
        #endregion

        #region 辅助
        /// <summary>已重载</summary>
        /// <returns></returns>
        public override String ToString() => $"[{Id}]{Method.DeclaringType.Name}.{Method.Name} ({(_cron != null ? _cron.ToString() : (Period + "ms"))})";
        #endregion
    }
}
#nullable restore