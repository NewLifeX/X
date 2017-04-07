using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace NewLife.Log
{
    /// <summary>代码性能计时器</summary>
    /// <remarks>参考了老赵（http://www.cnblogs.com/jeffreyzhao/archive/2009/03/10/codetimer.html）和eaglet（http://www.cnblogs.com/eaglet/archive/2009/03/10/1407791.html）两位的作品</remarks>
    /// <remarks>为了保证性能比较的公平性，采用了多种指标，并使用计时器重写等手段来避免各种不必要的损耗</remarks>
    public class CodeTimer
    {
        #region 静态快速计时
        /// <summary>计时</summary>
        /// <param name="times">次数</param>
        /// <param name="action">需要计时的委托</param>
        /// <param name="needTimeOne">是否需要预热</param>
        /// <returns></returns>
        public static CodeTimer Time(Int32 times, Action<Int32> action, Boolean needTimeOne = true)
        {
            var timer = new CodeTimer();
            timer.Times = times;
            timer.Action = action;

            if (needTimeOne) timer.TimeOne();
            timer.Time();

            return timer;
        }

        /// <summary>计时，并用控制台输出行</summary>
        /// <param name="title">标题</param>
        /// <param name="times">次数</param>
        /// <param name="action">需要计时的委托</param>
        /// <param name="needTimeOne">是否需要预热</param>
        public static void TimeLine(String title, Int32 times, Action<Int32> action, Boolean needTimeOne = true)
        {
            var n = Encoding.UTF8.GetByteCount(title);
            Console.Write("{0}{1}：", n >= 16 ? "" : new String(' ', 16 - n), title);

            var timer = new CodeTimer();
            timer.Times = times;
            timer.Action = action;
            timer.ShowProgress = true;
#if !__MOBILE__
            var currentForeColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            var left = Console.CursorLeft;
#endif
            if (needTimeOne) timer.TimeOne();
            timer.Time();

            // 等一会，让进度那边先输出
            Thread.Sleep(10);
#if !__MOBILE__
            Console.CursorLeft = left;
#endif
            Console.WriteLine(timer.ToString());
#if !__MOBILE__
            Console.ForegroundColor = currentForeColor;
#endif
        }

        /// <summary>显示头部</summary>
        /// <param name="title"></param>
        public static void ShowHeader(String title = "指标")
        {
            Write(title, 16);
            Console.Write("：");
#if !__MOBILE__
            var currentForeColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
#endif
            Write("执行时间", 9);
            Console.Write(" ");
            Write("CPU时间", 9);
            Console.Write(" ");
            Write("指令周期", 15);
            Write("GC(0/1/2)", 9);
            Console.WriteLine("   百分比");

            msBase = 0;
#if !__MOBILE__
            Console.ForegroundColor = currentForeColor;
#endif
        }

        static void Write(String name, Int32 max)
        {
            var len = Encoding.UTF8.GetByteCount(name);
            if (len < max) Console.Write(new String(' ', max - len));
            Console.Write(name);
        }
        #endregion

        #region PInvoke
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern Boolean QueryThreadCycleTime(IntPtr threadHandle, ref UInt64 cycleTime);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetCurrentThread();

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern Boolean GetThreadTimes(IntPtr hThread, out Int64 lpCreationTime, out Int64 lpExitTime, out Int64 lpKernelTime, out Int64 lpUserTime);

        static Boolean supportCycle = true;
        private static UInt64 GetCycleCount()
        {
            //if (Environment.Version.Major < 6) return 0;

            if (!supportCycle) return 0;

            try
            {
                UInt64 cycleCount = 0;
                QueryThreadCycleTime(GetCurrentThread(), ref cycleCount);
                return cycleCount;
            }
            catch
            {
                supportCycle = false;
                return 0;
            }
        }

        private static Int64 GetCurrentThreadTimes()
        {
            Int64 l;
            Int64 kernelTime, userTimer;
            GetThreadTimes(GetCurrentThread(), out l, out l, out kernelTime, out userTimer);
            return kernelTime + userTimer;
        }
        #endregion

        #region 私有字段
        UInt64 cpuCycles = 0;
        Int64 threadTime = 0;
        Int32[] gen;
        #endregion

        #region 属性
        /// <summary>次数</summary>
        public Int32 Times { get; set; }

        /// <summary>迭代方法，如不指定，则使用Time(int index)</summary>
        public Action<Int32> Action { get; set; }

        /// <summary>是否显示控制台进度</summary>
        public Boolean ShowProgress { get; set; }

        /// <summary>进度</summary>
        public Int32 Index { get; set; }

        /// <summary>CPU周期</summary>
        public Int64 CpuCycles { get; set; }

        /// <summary>线程时间，单位是100ns，除以10000转为ms</summary>
        public Int64 ThreadTime { get; set; }

        /// <summary>GC代数</summary>
        public Int32[] Gen { get; set; }

        /// <summary>执行时间</summary>
        public TimeSpan Elapsed { get; set; }
        #endregion

        #region 构造
        /// <summary>实例化一个代码计时器</summary>
        public CodeTimer()
        {
            Gen = new Int32[] { 0, 0, 0 };
        }
        #endregion

        #region 方法
        /// <summary>计时核心方法，处理进程和线程优先级</summary>
        public virtual void Time()
        {
            if (Times <= 0) throw new XException("非法迭代次数！");

            // 设定进程、线程优先级，并在完成时还原
            var pp = Process.GetCurrentProcess().PriorityClass;
            var tp = Thread.CurrentThread.Priority;
            try
            {
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
                Thread.CurrentThread.Priority = ThreadPriority.Highest;

                StartProgress();

                TimeTrue();
            }
            finally
            {
                StopProgress();

                Thread.CurrentThread.Priority = tp;
                Process.GetCurrentProcess().PriorityClass = pp;
            }
        }

        /// <summary>真正的计时</summary>
        protected virtual void TimeTrue()
        {
            if (Times <= 0) throw new XException("非法迭代次数！");

            // 统计GC代数
            GC.Collect(GC.MaxGeneration);

            gen = new Int32[GC.MaxGeneration + 1];
            for (var i = 0; i <= GC.MaxGeneration; i++)
            {
                gen[i] = GC.CollectionCount(i);
            }

            var watch = new Stopwatch();
            watch.Start();
            cpuCycles = GetCycleCount();
            threadTime = GetCurrentThreadTimes();

            // 如果未指定迭代方法，则使用内部的Time
            Action<Int32> action = Action;
            if (action == null)
            {
                action = Time;

                // 初始化
                Init();
            }

            for (var i = 0; i < Times; i++)
            {
                Index = i;

                action(i);
            }
            if (Action == null)
            {
                // 结束
                Finish();
            }

            CpuCycles = (Int64)(GetCycleCount() - cpuCycles);
            ThreadTime = GetCurrentThreadTimes() - threadTime;

            watch.Stop();
            Elapsed = watch.Elapsed;

            // 统计GC代数
            var list = new List<Int32>();
            for (var i = 0; i <= GC.MaxGeneration; i++)
            {
                var count = GC.CollectionCount(i) - gen[i];
                list.Add(count);
            }
            Gen = list.ToArray();
        }

        /// <summary>执行一次迭代，预热所有方法</summary>
        public void TimeOne()
        {
            var n = Times;

            try
            {
                Times = 1;
                Time();
            }
            finally { Times = n; }
        }

        /// <summary>迭代前执行，计算时间</summary>
        public virtual void Init() { }

        /// <summary>每一次迭代，计算时间</summary>
        /// <param name="index"></param>
        public virtual void Time(Int32 index) { }

        /// <summary>迭代后执行，计算时间</summary>
        public virtual void Finish() { }
        #endregion

        #region 进度
        Thread thread;
        /// <summary>基准时间</summary>
        static Double msBase;

        void StartProgress()
        {
            if (!ShowProgress) return;

            // 使用低优先级线程显示进度
            thread = new Thread(new ParameterizedThreadStart(Progress));
            thread.IsBackground = true;
            thread.Priority = ThreadPriority.BelowNormal;
            thread.Start();
        }

        void StopProgress()
        {
            if (thread != null && thread.IsAlive)
            {
                thread.Abort();
                thread.Join(3000);
            }
        }

        void Progress(Object state)
        {
#if !__MOBILE__
            var left = Console.CursorLeft;

            // 设置光标不可见
            var cursorVisible = Console.CursorVisible;
            Console.CursorVisible = false;
#endif
            var sw = new Stopwatch();
            sw.Start();
            while (true)
            {
                try
                {
                    var i = Index;
                    if (i >= Times) break;

                    if (i > 0 && sw.Elapsed.TotalMilliseconds > 10)
                    {
                        var d = (Double)i / Times;
                        var ms = sw.Elapsed.TotalMilliseconds;
                        var ts = new TimeSpan(0, 0, 0, 0, (Int32)(ms * Times / i));
                        Console.Write("{0,7:n0}ms {1:p} Total=>{2}", ms, d, ts);
#if !__MOBILE__
                        Console.CursorLeft = left;
#endif
                    }
                }
                catch (ThreadAbortException) { break; }
                catch { break; }

                Thread.Sleep(500);
            }
            sw.Stop();
#if !__MOBILE__
            Console.CursorLeft = left;
            Console.CursorVisible = cursorVisible;
#endif
        }
        #endregion

        #region 重载
        /// <summary>已重载。输出依次分别是：执行时间、CPU线程时间、时钟周期、GC代数</summary>
        /// <returns></returns>
        public override String ToString()
        {
            var ms = Elapsed.TotalMilliseconds;
            if (msBase == 0) msBase = ms;
            var pc = ms / msBase;

            return String.Format("{0,7:n0}ms {1,7:n0}ms {2,15:n0} {3,3}/{4}/{5}\t{6,8:p2}", ms, ThreadTime / 10000, CpuCycles, Gen[0], Gen[1], Gen[2], pc);
        }
        #endregion
    }
}