using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace NewLife.Threading
{
    /// <summary>
    /// 不可重入的定时器。为了避免系统的Timer可重入的问题，差别在于本地调用完成后才开始计算时间间隔。这实际上也是经常用到的。
    /// </summary>
    public class TimerX : DisposeBase
    {
        #region 属性
        private WeakAction<Object> _Callback;
        /// <summary>回调</summary>
        public WeakAction<Object> Callback
        {
            get { return _Callback; }
            set { _Callback = value; }
        }

        private Object _State;
        /// <summary>用户数据</summary>
        public Object State
        {
            get { return _State; }
            set { _State = value; }
        }

        private DateTime _NextTime;
        /// <summary>下一次调用时间</summary>
        public DateTime NextTime
        {
            get { return _NextTime; }
            set { _NextTime = value; }
        }

        private Int32 _Timers;
        /// <summary>调用次数</summary>
        public Int32 Timers
        {
            get { return _Timers; }
            set { _Timers = value; }
        }

        //private Int32 _DueTime;
        ///// <summary>开始间隔时间</summary>
        //public Int32 DueTime
        //{
        //    get { return _DueTime; }
        //    set { _DueTime = value; }
        //}

        private Int32 _Period;
        /// <summary>间隔周期</summary>
        public Int32 Period
        {
            get { return _Period; }
            set { _Period = value; }
        }

        private Boolean _Calling;
        /// <summary>调用中</summary>
        public Boolean Calling
        {
            get { return _Calling; }
            set { _Calling = value; }
        }
        #endregion

        #region 构造
        /// <summary>
        /// 实例化
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <param name="dueTime"></param>
        /// <param name="period"></param>
        public TimerX(WaitCallback callback, object state, int dueTime, int period)
        {
            if (callback == null) throw new ArgumentNullException("callback");
            if (dueTime < Timeout.Infinite) throw new ArgumentOutOfRangeException("dueTime");
            if (period < Timeout.Infinite) throw new ArgumentOutOfRangeException("period");

            Callback = new WeakAction<object>(callback);
            State = state;
            //DueTime = dueTime;
            Period = period;

            NextTime = DateTime.Now.AddMilliseconds(dueTime);

            TimerXHelper.Add(this);
        }

        private event EventHandler OnClose;
        /// <summary>
        /// 销毁时触发
        /// </summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            if (OnClose != null) OnClose(this, EventArgs.Empty);
        }
        #endregion

        #region 内部助手
        static class TimerXHelper
        {
            static Thread thread;

            static List<TimerX> timers = new List<TimerX>();

            public static void Add(TimerX timer)
            {
                lock (timers)
                {
                    timers.Add(timer);

                    timer.OnClose += new EventHandler(delegate(Object sender, EventArgs e)
                    {
                        TimerX tx = sender as TimerX;
                        if (tx == null) return;
                        lock (timers)
                        {
                            if (!timers.Contains(tx)) return;

                            timers.Remove(sender as TimerX);
                        }
                    });

                    if (thread == null)
                    {
                        thread = new Thread(Process);
                        thread.Name = "TimerX定时器";
                        thread.IsBackground = true;
                        thread.Start();
                    }

                    if (waitForTimer != null) waitForTimer.Set();
                }
            }

            static AutoResetEvent waitForTimer;
            static Int32 period = 10;

            static void Process(Object state)
            {
                while (true)
                {
                    try
                    {
                        #region 准备
                        if (timers == null || timers.Count < 1)
                        {
                            // 没有计时器，设置一个较大的休眠时间
                            //period = 60000;

                            // 使用事件量来控制线程
                            if (waitForTimer != null) waitForTimer.Close();

                            waitForTimer = new AutoResetEvent(false);
                            waitForTimer.WaitOne(Timeout.Infinite);
                        }

                        List<TimerX> list = null;
                        lock (timers)
                        {
                            list = new List<TimerX>(timers);
                        }
                        #endregion

                        #region 调度
                        // 记录本次循环有几个任务被处理
                        //Int32 count = 0;
                        // 设置一个较大的间隔，内部会根据处理情况调整该值为最合理值
                        period = 60000;
                        foreach (TimerX timer in list)
                        {
                            // 删除过期的
                            if (!timer.Callback.IsAlive)
                            {
                                lock (timers)
                                {
                                    //if (timers.Contains(timer)) timers.Remove(timer);
                                    Int32 index = timers.IndexOf(timer);
                                    if (index >= 0) timers.RemoveAt(index);
                                }
                                continue;
                            }

                            TimeSpan ts = timer.NextTime - DateTime.Now;
                            Int32 d = (Int32)ts.TotalMilliseconds;
                            if (d > 0)
                            {
                                // 缩小间隔，便于快速调用
                                if (d < period) period = d;

                                continue;
                            }

                            try
                            {
                                timer.Calling = true;

                                //timer.Callback(timer.State);
                                // 线程池调用
                                Action<Object> callback = timer.Callback;
                                ThreadPoolX.QueueUserWorkItem(delegate() { callback(timer.State); });
                            }
                            catch (ThreadAbortException) { throw; }
                            catch (ThreadInterruptedException) { throw; }
                            catch { }
                            finally
                            {
                                timer.Timers++;
                                timer.Calling = false;
                                timer.NextTime = DateTime.Now.AddMilliseconds(timer.Period);
                                if (timer.Period < period) period = timer.Period;
                            }
                            //count++;
                        }
                        #endregion
                    }
                    catch (ThreadAbortException) { break; }
                    catch (ThreadInterruptedException) { break; }
                    catch { }

                    Thread.Sleep(period);
                }
            }
        }
        #endregion
    }
}