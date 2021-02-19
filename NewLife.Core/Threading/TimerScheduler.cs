using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Log;
using NewLife.Reflection;

#nullable enable
namespace NewLife.Threading
{
    /// <summary>定时器调度器</summary>
    public class TimerScheduler
    {
        #region 静态
        private TimerScheduler(String name) => Name = name;

        private static readonly Dictionary<String, TimerScheduler> _cache = new Dictionary<String, TimerScheduler>();

        /// <summary>创建指定名称的调度器</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static TimerScheduler Create(String name)
        {
            if (_cache.TryGetValue(name, out var ts)) return ts;
            lock (_cache)
            {
                if (_cache.TryGetValue(name, out ts)) return ts;

                ts = new TimerScheduler(name);
                _cache[name] = ts;

                return ts;
            }
        }

        /// <summary>默认调度器</summary>
        public static TimerScheduler Default { get; } = Create("Default");

        [ThreadStatic]
        private static TimerScheduler? _Current;
        /// <summary>当前调度器</summary>
        public static TimerScheduler? Current { get => _Current; private set => _Current = value; }
        #endregion

        #region 属性
        /// <summary>名称</summary>
        public String Name { get; private set; }

        /// <summary>定时器个数</summary>
        public Int32 Count { get; private set; }

        /// <summary>最大耗时。超过时报警告日志，默认500ms</summary>
        public Int32 MaxCost { get; set; } = 500;

        private Thread? thread;
        private Int32 _tid;

        private TimerX[] Timers = new TimerX[0];
        #endregion

        /// <summary>把定时器加入队列</summary>
        /// <param name="timer"></param>
        public void Add(TimerX timer)
        {
            timer.Id = Interlocked.Increment(ref _tid);
            WriteLog("Timer.Add {0}", timer);

            lock (this)
            {
                var list = new List<TimerX>(Timers);
                if (list.Contains(timer)) return;
                list.Add(timer);

                Timers = list.ToArray();

                Count++;

                if (thread == null)
                {
                    thread = new Thread(Process)
                    {
                        Name = Name == "Default" ? "T" : Name,
                        IsBackground = true
                    };
                    thread.Start();

                    WriteLog("启动定时调度器：{0}", Name);
                }

                Wake();
            }
        }

        /// <summary>从队列删除定时器</summary>
        /// <param name="timer"></param>
        /// <param name="reason"></param>
        public void Remove(TimerX timer, String reason)
        {
            if (timer == null || timer.Id == 0) return;

            WriteLog("Timer.Remove {0} reason:{1}", timer, reason);

            lock (this)
            {
                timer.Id = 0;

                var list = new List<TimerX>(Timers);
                if (list.Contains(timer))
                {
                    list.Remove(timer);
                    Timers = list.ToArray();

                    Count--;
                }
            }
        }

        private AutoResetEvent? waitForTimer;
        private Int32 period = 10;

        /// <summary>唤醒处理</summary>
        public void Wake()
        {
            var e = waitForTimer;
            if (e != null)
            {
                var swh = e.SafeWaitHandle;
                if (swh != null && !swh.IsClosed) e.Set();
            }
        }

        /// <summary>调度主程序</summary>
        /// <param name="state"></param>
        private void Process(Object? state)
        {
            Current = this;
            while (true)
            {
                // 准备好定时器列表
                var arr = Timers;

                // 如果没有任务，则销毁线程
                if (arr.Length == 0 && period == 60_000)
                {
                    WriteLog("没有可用任务，销毁线程");

                    var th = thread;
                    thread = null;
#if !__CORE__
                    th?.Abort();
#endif

                    break;
                }

                try
                {
                    var now = DateTime.Now;

                    // 设置一个较大的间隔，内部会根据处理情况调整该值为最合理值
                    period = 60_000;
                    foreach (var timer in arr)
                    {
                        if (!timer.Calling && CheckTime(timer, now))
                        {
                            // 是否能够执行
                            if (timer.CanExecute == null || timer.CanExecute())
                            {
                                // 必须在主线程设置状态，否则可能异步线程还没来得及设置开始状态，主线程又开始了新的一轮调度
                                timer.Calling = true;
                                if (timer.IsAsyncTask)
                                    Task.Factory.StartNew(ExecuteAsync, timer);
                                else if (!timer.Async)
                                    Execute(timer);
                                else
                                    //Task.Factory.StartNew(() => ProcessItem(timer));
                                    // 不需要上下文流动
                                    ThreadPool.UnsafeQueueUserWorkItem(Execute, timer);
                                // 内部线程池，让异步任务有公平竞争CPU的机会
                                //ThreadPoolX.QueueUserWorkItem(Execute, timer);
                            }
                            // 即使不能执行，也要设置下一次的时间
                            else
                            {
                                OnFinish(timer);
                            }
                        }
                    }
                }
                catch (ThreadAbortException) { break; }
                catch (ThreadInterruptedException) { break; }
                catch { }

                if (waitForTimer == null) waitForTimer = new AutoResetEvent(false);
                if (period > 0) waitForTimer.WaitOne(period, true);
            }
        }

        /// <summary>检查定时器是否到期</summary>
        /// <param name="timer"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        private Boolean CheckTime(TimerX timer, DateTime now)
        {
            // 删除过期的，为了避免占用过多CPU资源，TimerX禁止小于10ms的任务调度
            var p = timer.Period;
            if (p < 10 && p > 0)
            {
                // 周期0表示只执行一次
                if (p < 10 && p > 0) XTrace.WriteLine("为了避免占用过多CPU资源，TimerX禁止小于{1}ms<10ms的任务调度，关闭任务{0}", timer, p);
                timer.Dispose();
                return false;
            }

            var ts = timer.NextTime - now;
            //var d = (Int64)ts.TotalMilliseconds;
            //var d = Math.Ceiling(ts.TotalMilliseconds);
            var d = ts.TotalMilliseconds;
            if (d > 0)
            {
                // 缩小间隔，便于快速调用
                if (d < period) period = (Int32)d;

                return false;
            }
            //XTrace.WriteLine("d={0}", ts.TotalMilliseconds);

            return true;
        }

        /// <summary>处理每一个定时器</summary>
        /// <param name="state"></param>
        private void Execute(Object? state)
        {
            if (state is not TimerX timer) return;

            TimerX.Current = timer;

            // 控制日志显示
            WriteLogEventArgs.CurrentThreadName = Name == "Default" ? "T" : Name;

            timer.hasSetNext = false;

            var sw = Stopwatch.StartNew();

            try
            {
                // 弱引用判断
                var target = timer.Target.Target;
                if (target == null && !timer.Method.IsStatic)
                {
                    Remove(timer, "委托已不存在（GC回收委托所在对象）");
                    timer.Dispose();
                    return;
                }

                //timer.Calling = true;

                //target.Invoke(timer.State ?? timer);

                if (timer.IsAsyncTask)
                {
                    var func = timer.Method.As<Func<Object?, Task>>(target);
                    func(timer.State).Wait();
                }
                else
                {
                    var func = timer.Method.As<TimerCallback>(target);
                    func(timer.State);
                }
            }
            catch (ThreadAbortException) { throw; }
            catch (ThreadInterruptedException) { throw; }
            // 如果用户代码没有拦截错误，则这里拦截，避免出错了都不知道怎么回事
            catch (Exception ex) { XTrace.WriteException(ex); }
            finally
            {
                sw.Stop();

                OnExecuted(timer, (Int32)sw.ElapsedMilliseconds);
            }
        }

        /// <summary>处理每一个定时器</summary>
        /// <param name="state"></param>
        private async void ExecuteAsync(Object? state)
        {
            if (state is not TimerX timer) return;

            TimerX.Current = timer;

            // 控制日志显示
            WriteLogEventArgs.CurrentThreadName = Name == "Default" ? "T" : Name;

            timer.hasSetNext = false;

            var sw = Stopwatch.StartNew();

            try
            {
                // 弱引用判断
                var target = timer.Target.Target;
                if (target == null && !timer.Method.IsStatic)
                {
                    Remove(timer, "委托已不存在（GC回收委托所在对象）");
                    timer.Dispose();
                    return;
                }

                var func = timer.Method.As<Func<Object?, Task>>(target);
                await func(timer.State);
            }
            catch (ThreadAbortException) { throw; }
            catch (ThreadInterruptedException) { throw; }
            // 如果用户代码没有拦截错误，则这里拦截，避免出错了都不知道怎么回事
            catch (Exception ex) { XTrace.WriteException(ex); }
            finally
            {
                sw.Stop();

                OnExecuted(timer, (Int32)sw.ElapsedMilliseconds);
            }
        }

        private void OnExecuted(TimerX timer, Int32 ms)
        {
            if (timer.Cost == 0)
                timer.Cost = ms;
            else
                timer.Cost = (timer.Cost + ms) / 2;

            if (ms > MaxCost && !timer.Async && !timer.IsAsyncTask) XTrace.WriteLine("任务 {0} 耗时过长 {1:n0}ms，建议使用异步任务Async=true", timer, ms);

            timer.Timers++;
            OnFinish(timer);

            timer.Calling = false;

            TimerX.Current = null;

            // 控制日志显示
            WriteLogEventArgs.CurrentThreadName = null;

            // 调度线程可能在等待，需要唤醒
            Wake();
        }

        private void OnFinish(TimerX timer)
        {
            // 如果内部设置了下一次时间，则不再递加周期
            var p = timer.SetAndGetNextTime();

            // 清理一次性定时器
            if (p <= 0)
            {
                Remove(timer, "Period<=0");
                timer.Dispose();
            }
            else if (p < period)
                period = p;
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString() => Name;

        #region 设置
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;

        private void WriteLog(String format, params Object[] args) => Log?.Info(Name + format, args);
        #endregion
    }
}
#nullable restore