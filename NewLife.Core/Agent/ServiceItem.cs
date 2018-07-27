using System;
using System.Diagnostics;
using System.Threading;
using NewLife.Log;
using NewLife.Threading;

namespace NewLife.Agent
{
    /// <summary>服务工作项</summary>
    public class ServiceItem
    {
        #region 属性
        /// <summary>服务项索引</summary>
        public Int32 Index { get; private set; }

        /// <summary>线程名称</summary>
        public String Name { get; set; }

        /// <summary>任务委托</summary>
        public Func<Int32, Boolean> Callback { get; set; }

        /// <summary>工作任务</summary>
        public IJob Job { get; set; }

        /// <summary>线程</summary>
        public Thread Thread { get; private set; }

        /// <summary>间隔</summary>
        public Int32 Interval { get; set; }

        /// <summary>可用</summary>
        public Boolean Active { get; set; }

        /// <summary>最后活跃时间</summary>
        public DateTime LastActive { get; private set; }

        /// <summary>阻塞任务用的自动事件量</summary>
        public AutoResetEvent Event { get; private set; }
        #endregion

        #region 构造
        /// <summary>实例化一个服务工作项</summary>
        /// <param name="index"></param>
        /// <param name="name"></param>
        /// <param name="interval"></param>
        public ServiceItem(Int32 index, String name = null, Int32 interval = 0)
        {
            Index = index;
            Name = !name.IsNullOrEmpty() ? name : "A" + index;
            Interval = interval;
        }
        #endregion

        #region 方法
        /// <summary>启动工作项</summary>
        public void Start(String reason)
        {
            // 可以通过设置任务的时间间隔小于0来关闭指定任务
            var time = Interval;
            if (time < 0) return;

            WriteLine("启动线程[{0}/{1}] Interval={2} {3}", Index, Name, time, reason);

            var th = Thread = new Thread(WorkWaper);

            th.Name = Name;
            th.IsBackground = true;
            th.Priority = ThreadPriority.AboveNormal;
            th.Start(Index);

            //Active = true;
            //LastActive = DateTime.Now;
        }

        /// <summary>停止工作项</summary>
        public void Stop(String reason)
        {
            var th = Thread;
            if (th == null) return;

            WriteLine("停止线程[{0}/{1}] LastActive={2} {3}", Index, Name, LastActive, reason);

            Active = false;
            Event?.Set();

            var set = Setting.Current;
            try
            {
                if (th != null && th.IsAlive)
                {
                    // 等待线程退出
                    th.Join(set.WaitForExit);

                    if (th.IsAlive) th.Abort();
                }
            }
            catch (Exception ex)
            {
                WriteLine(ex.ToString());
            }
            Thread = null;
        }

        /// <summary>线程包装</summary>
        /// <param name="data">线程序号</param>
        private void WorkWaper(Object data)
        {
            var index = (Int32)data;
            var ev = Event = new AutoResetEvent(false);

            Active = true;
            var set = Setting.Current;

            var ctx = new JobContext();
            ctx["Worker"] = this;
            while (true)
            {
                var isContinute = false;
                LastActive = TimerX.Now;

                var sw = Stopwatch.StartNew();
                try
                {
                    if (Callback != null)
                        isContinute = Callback(Index);
                    else if (Job != null)
                        Job.Execute(ctx);
                }
                catch (ThreadAbortException)
                {
                    Active = false;
                    WriteLine("线程[{0}]被取消！", index);
                    break;
                }
                catch (ThreadInterruptedException)
                {
                    Active = false;
                    WriteLine("线程[{0}]中断错误！", index);
                    break;
                }
                catch (Exception ex)
                {
                    // 确保拦截了所有的异常，保证服务稳定运行
                    WriteLine(ex?.GetTrue().GetMessage());
                }
                sw.Stop();
                LastActive = TimerX.Now;

                if (set.Debug && set.WaitForExit > 0 && sw.ElapsedMilliseconds > set.WaitForExit) WriteLine("工作任务耗时较长 {0:n0}ms > {1:n0}ms，需要调整业务缩小耗时，以确保任务得到可靠保护", sw.ElapsedMilliseconds, set.WaitForExit);

                // 检查服务是否正在重启
                if (!Active)
                {
                    WriteLine("停止服务，线程[{0}]退出", index);
                    break;
                }

                var time = Interval;

                if (!isContinute) ev.WaitOne(time * 1000);

                if (!Active)
                {
                    WriteLine("停止服务，线程[{0}]退出", index);
                    break;
                }
            }

            ev.Dispose();
            Event = null;
        }
        #endregion

        #region 维护管理
        /// <summary>检查是否有工作线程死亡</summary>
        public void CheckActive()
        {
            // 如果工作线程没有启动，则不用检查
            if (!Active) return;

            var th = Thread;
            if (th != null && !th.IsAlive)
            {
                WriteLine(th.Name + "处于停止状态，准备重新启动！");

                Start("CheckActive");
            }

            // 是否检查最大活动时间
            var max = Setting.Current.MaxActive;
            if (max <= 0) return;

            var ts = TimerX.Now - LastActive;
            if (ts.TotalSeconds > max)
            {
                WriteLine("{0}已经{1:n0}秒没有活动了，准备重新启动！", Name, ts.TotalSeconds);

                Stop("MaxActive");
                // 等待线程结束
                Thread?.Join(100);
                Start("MaxActive");
            }
        }
        #endregion

        #region 日志
        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void WriteLine(String format, params Object[] args) => XTrace.WriteLine(format, args);
        #endregion
    }
}