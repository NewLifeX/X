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
            var timer = new CodeTimer
            {
                Times = times,
                Action = action
            };

            if (needTimeOne) timer.TimeOne();
            timer.Time();

            return timer;
        }

        /// <summary>计时，并用控制台输出行</summary>
        /// <param name="title">标题</param>
        /// <param name="times">次数</param>
        /// <param name="action">需要计时的委托</param>
        /// <param name="needTimeOne">是否需要预热</param>
        public static CodeTimer TimeLine(String title, Int32 times, Action<Int32> action, Boolean needTimeOne = true)
        {
            var n = Encoding.UTF8.GetByteCount(title);
            Console.Write("{0}{1}：", n >= 16 ? "" : new String(' ', 16 - n), title);

            var timer = new CodeTimer
            {
                Times = times,
                Action = action,
                ShowProgress = true
            };
            var currentForeColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            var left = Console.CursorLeft;
            if (needTimeOne) timer.TimeOne();
            timer.Time();

            // 等一会，让进度那边先输出
            Thread.Sleep(10);
            Console.CursorLeft = left;
            Console.WriteLine(timer.ToString());
            Console.ForegroundColor = currentForeColor;

            return timer;
        }

        /// <summary>显示头部</summary>
        /// <param name="title"></param>
        public static void ShowHeader(String title = "指标")
        {
            Write(title, 16);
            Console.Write("：");
            var currentForeColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Write("执行时间", 9);
            Console.Write(" ");
            Write("CPU时间", 9);
            Console.Write(" ");
            Write("指令周期", 15);
            Write("GC(0/1/2)", 9);
            Console.WriteLine("   百分比");

            msBase = 0;
            Console.ForegroundColor = currentForeColor;
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
            GetThreadTimes(GetCurrentThread(), out var ct, out var et, out var kernelTime, out var userTimer);
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

        /// <summary>线程时间，单位是ms</summary>
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

            var watch = Stopwatch.StartNew();
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
            // 线程时间，单位是100ns，除以10000转为ms
            ThreadTime = (GetCurrentThreadTimes() - threadTime) / 10_000;

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
            thread = new Thread(Progress)
            {
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal
            };
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
            var left = Console.CursorLeft;

            // 设置光标不可见
            var cursorVisible = Console.CursorVisible;
            Console.CursorVisible = false;
            var sw = Stopwatch.StartNew();
            while (true)
            {
                try
                {
                    var i = Index;
                    if (i >= Times) break;

                    if (i > 0 && sw.Elapsed.TotalMilliseconds > 10)
                    {
                        var prog = (Double)i / Times;
                        var ms = sw.Elapsed.TotalMilliseconds;

                        // 预计总时间
                        var ts = new TimeSpan(0, 0, 0, 0, (Int32)(ms * Times / i));

                        var speed = i / ms;
                        var cost = ms / i;

                        Console.Write($"{ms,7:n0}ms {prog:p2} Total=>{ts}");
                        Console.CursorLeft = left;
                    }
                }
                catch (ThreadAbortException) { break; }
                catch { break; }

                Thread.Sleep(500);
            }
            sw.Stop();
            Console.CursorLeft = left;
            Console.CursorVisible = cursorVisible;
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

            var speed = ms == 0 ? 0 : Times / ms;
            var cost = Times == 0 ? 0 : ms / Times;
            return $"{ms,7:n0}ms {ThreadTime,7:n0}ms {CpuCycles,15:n0} {Gen[0],3}/{Gen[1]}/{Gen[2]}\t{pc,8:p2}";
        }
        #endregion
    }
}