using System;
using System.Collections.Generic;
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
        /// <summary>回调</summary>
        public Action<Object> Callback { get; set; }

        /// <summary>用户数据</summary>
        public Object State { get; set; }

        /// <summary>下一次调用时间</summary>
        public DateTime NextTime { get; set; }

        /// <summary>调用次数</summary>
        public Int32 Timers { get; private set; }

        /// <summary>间隔周期。毫秒，设为0则只调用一次</summary>
        public Int32 Period { get; set; }

        /// <summary>调用中</summary>
        public Boolean Calling { get; private set; }
        #endregion

        #region 构造
        /// <summary>实例化一个不可重入的定时器</summary>
        /// <param name="callback">委托</param>
        /// <param name="state">用户数据</param>
        /// <param name="dueTime">多久之后开始。毫秒</param>
        /// <param name="period">间隔周期。毫秒</param>
        public TimerX(WaitCallback callback, Object state, Int32 dueTime, Int32 period)
        {
            if (callback == null) throw new ArgumentNullException("callback");
            if (dueTime < 0) throw new ArgumentOutOfRangeException("dueTime");
            if (period < 0) throw new ArgumentOutOfRangeException("period");

            Callback = new Action<Object>(callback);
            State = state;
            Period = period;

            NextTime = DateTime.Now.AddMilliseconds(dueTime);

            TimerXHelper.Add(this);
        }

        /// <summary>销毁定时器</summary>
        public void Dispose()
        {
            TimerXHelper.Remove(this);
        }
        #endregion

        #region 静态方法
        /// <summary>延迟执行一个委托</summary>
        /// <param name="callback"></param>
        /// <param name="ms"></param>
        /// <returns></returns>
        public static TimerX Delay(WaitCallback callback, Int32 ms)
        {
            var timer = new TimerX(callback, null, ms, 0);
            return timer;
        }
        #endregion

        #region 辅助
        /// <summary>已重载</summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Callback != null ? "" + Callback : base.ToString();
        }
        #endregion

        #region 设置
        /// <summary>是否开启调试，输出更多信息</summary>
        public static Boolean Debug { get; set; }

        static void WriteLog(String format, params Object[] args)
        {
            if (Debug) XTrace.WriteLine(format, args);
        }
        #endregion

        #region 内部助手
        static class TimerXHelper
        {
            static Thread thread;

            static HashSet<TimerX> timers = new HashSet<TimerX>();

            /// <summary>把定时器加入队列</summary>
            /// <param name="timer"></param>
            public static void Add(TimerX timer)
            {
                WriteLog("TimerX.Add {0}ms {1}", timer.Period, timer);

                lock (timers)
                {
                    timers.Add(timer);

                    if (thread == null)
                    {
                        thread = new Thread(Process);
                        //thread.Name = "TimerX";
                        thread.Name = "T";
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

            public static void Remove(TimerX timer)
            {
                if (timer == null) return;

                WriteLog("TimerX.Remove {0}", timer);

                lock (timers)
                {
                    if (timers.Contains(timer)) timers.Remove(timer);
                }
            }

            static AutoResetEvent waitForTimer;
            static Int32 period = 10;

            /// <summary>调度主程序</summary>
            /// <param name="state"></param>
            static void Process(Object state)
            {
                while (true)
                {
                    try
                    {
                        var arr = GetTimers();

                        var now = DateTime.Now;

                        // 设置一个较大的间隔，内部会根据处理情况调整该值为最合理值
                        period = 60000;
                        foreach (var timer in arr)
                        {
                            if (CheckTime(timer, now)) ProcessItem(timer);
                        }
                    }
                    catch (ThreadAbortException) { break; }
                    catch (ThreadInterruptedException) { break; }
                    catch { }

                    if (waitForTimer == null) waitForTimer = new AutoResetEvent(false);
                    waitForTimer.WaitOne(period, false);
                }
            }

            /// <summary>准备好定时器列表</summary>
            /// <returns></returns>
            static TimerX[] GetTimers()
            {
                if (timers == null || timers.Count < 1)
                {
                    // 使用事件量来控制线程
                    if (waitForTimer != null) waitForTimer.Close();

                    // 没有任务，无线等待
                    waitForTimer = new AutoResetEvent(false);
                    waitForTimer.WaitOne(Timeout.Infinite, false);
                }

                lock (timers)
                {
                    return timers.ToArray();
                }
            }

            /// <summary>检查定时器是否到期</summary>
            /// <param name="timer"></param>
            /// <param name="now"></param>
            /// <returns></returns>
            static Boolean CheckTime(TimerX timer, DateTime now)
            {
                // 删除过期的，为了避免占用过多CPU资源，TimerX禁止小于10ms的任务调度
                var p = timer.Period;
                if (p < 10 && p > 0)
                {
                    // 周期0表示只执行一次
                    if (p < 10 && p > 0) XTrace.WriteLine("为了避免占用过多CPU资源，TimerX禁止小于{1}ms<10ms的任务调度，关闭任务{0}", timer, p);
                    lock (timers)
                    {
                        timers.Remove(timer);
                        timer.Dispose();
                    }
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
            static void ProcessItem(TimerX timer)
            {
                try
                {
                    timer.Calling = true;

                    //Action<Object> callback = timer.Callback;
                    timer.Callback(timer.State ?? timer);
                }
                catch (ThreadAbortException) { throw; }
                catch (ThreadInterruptedException) { throw; }
                // 如果用户代码没有拦截错误，则这里拦截，避免出错了都不知道怎么回事
                catch (Exception ex) { XTrace.WriteException(ex); }
                finally
                {
                    // 再次读取周期，因为任何函数可能会修改
                    var p = timer.Period;

                    timer.Timers++;
                    timer.NextTime = DateTime.Now.AddMilliseconds(p);
                    timer.Calling = false;

                    // 清理一次性定时器
                    if (p <= 0)
                    {
                        lock (timers)
                        {
                            timers.Remove(timer);
                            timer.Dispose();
                        }
                    }
                    if (p < period) period = p;
                }
            }
        }
        #endregion
    }
}