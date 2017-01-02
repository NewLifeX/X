using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Log;

namespace NewLife.Threading
{
    /// <summary>定时器调度器</summary>
    public class TimerScheduler
    {
        #region 静态
        private TimerScheduler(String name) { Name = name; }

        private static Dictionary<String, TimerScheduler> _cache = new Dictionary<String, TimerScheduler>();

        /// <summary>创建指定名称的调度器</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static TimerScheduler Create(String name)
        {
            TimerScheduler ts = null;
            if (_cache.TryGetValue(name, out ts)) return ts;
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
        private static TimerScheduler _Current;
        /// <summary>当前调度器</summary>
        public static TimerScheduler Current { get { return _Current; } private set { _Current = value; } }
        #endregion

        #region 属性
        /// <summary>名称</summary>
        public String Name { get; private set; }

        /// <summary>定时器个数</summary>
        public Int32 Count { get; private set; }

        private Thread thread;

        private HashSet<TimerX> timers = new HashSet<TimerX>();
        #endregion

        /// <summary>把定时器加入队列</summary>
        /// <param name="timer"></param>
        public void Add(TimerX timer)
        {
            WriteLog("Timer.Add {0}ms {1}", timer.Period, timer);

            lock (timers)
            {
                timers.Add(timer);
                Count++;

                if (thread == null)
                {
                    thread = new Thread(Process);
                    //thread.Name = "TimerX";
                    thread.Name = Name == "Default" ? "T" : Name;
                    thread.IsBackground = true;
                    thread.Start();
                }

                var e = waitForTimer;
                if (e != null)
                {
                    var swh = e.SafeWaitHandle;
                    if (swh != null && !swh.IsClosed) e.Set();
                }
            }
        }

        /// <summary>从队列删除定时器</summary>
        /// <param name="timer"></param>
        public void Remove(TimerX timer)
        {
            if (timer == null) return;

            WriteLog("Timer.Remove {0}", timer);

            lock (timers)
            {
                if (timers.Contains(timer))
                {
                    timers.Remove(timer);
                    Count--;
                }
            }
        }

        private AutoResetEvent waitForTimer;
        private Int32 period = 10;

        /// <summary>调度主程序</summary>
        /// <param name="state"></param>
        private void Process(Object state)
        {
            Current = this;
            while (true)
            {
                // 准备好定时器列表
                TimerX[] arr = null;
                lock (timers)
                {
                    // 如果没有任务，则销毁线程
                    if (timers.Count == 0 && period == 60000)
                    {
                        WriteLog("没有可用任务，销毁线程");
                        var th = thread;
                        thread = null;
                        th.Abort();
                        break;
                    }

                    arr = timers.ToArray();
                }

                try
                {
                    var now = DateTime.Now;

                    // 设置一个较大的间隔，内部会根据处理情况调整该值为最合理值
                    period = 60000;
                    foreach (var timer in arr)
                    {
                        if (!timer.Calling && CheckTime(timer, now))
                        {
                            if (!timer.Async)
                                ProcessItem(timer);
                            else
                                Task.Factory.StartNew(() => ProcessItem(timer));
                        }
                    }
                }
                catch (ThreadAbortException) { break; }
                catch (ThreadInterruptedException) { break; }
                catch { }

                if (waitForTimer == null) waitForTimer = new AutoResetEvent(false);
                waitForTimer.WaitOne(period, false);
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
            var d = (Int32)ts.TotalMilliseconds;
            if (d > 0)
            {
                // 缩小间隔，便于快速调用
                if (d < period) period = d;

                return false;
            }

            return true;
        }

        /// <summary>处理每一个定时器</summary>
        /// <param name="timer"></param>
        private void ProcessItem(TimerX timer)
        {
            TimerX.Current = timer;

            var sw = new Stopwatch();
            sw.Start();

            try
            {
                timer.Calling = true;

                timer.Callback(timer.State ?? timer);
            }
            catch (ThreadAbortException) { throw; }
            catch (ThreadInterruptedException) { throw; }
            // 如果用户代码没有拦截错误，则这里拦截，避免出错了都不知道怎么回事
            catch (Exception ex) { XTrace.WriteException(ex); }
            finally
            {
                sw.Stop();

                var d = (Int32)sw.ElapsedMilliseconds;
                if (timer.Cost == 0)
                    timer.Cost = d;
                else
                    timer.Cost = (timer.Cost + d) / 2;

                if (d > 500 && !timer.Async) XTrace.WriteLine("任务 {0} 耗时过长 {1:n0}ms", timer, d);

                // 再次读取周期，因为任何函数可能会修改
                var p = timer.Period;

                timer.Timers++;
                timer.NextTime = DateTime.Now.AddMilliseconds(p);
                timer.Calling = false;

                // 清理一次性定时器
                if (p <= 0)
                    timer.Dispose();
                else if (p < period)
                    period = p;

                TimerX.Current = null;
            }
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString()
        {
            return Name;
        }

        #region 设置
        /// <summary>是否开启调试，输出更多信息</summary>
        public static Boolean Debug { get; set; }

        static void WriteLog(String format, params Object[] args)
        {
            if (Debug) XTrace.WriteLine(format, args);
        }
        #endregion
    }
}