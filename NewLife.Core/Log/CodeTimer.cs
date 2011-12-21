using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using NewLife.Exceptions;
using System.Text;

namespace NewLife.Log
{
    /// <summary>
    /// 代码性能计时器
    /// </summary>
    /// <remarks>参考了老赵（http://www.cnblogs.com/jeffreyzhao/archive/2009/03/10/codetimer.html）和eaglet（http://www.cnblogs.com/eaglet/archive/2009/03/10/1407791.html）两位的作品</remarks>
    /// <remarks>为了保证性能比较的公平性，采用了多种指标，并使用计时器重写等手段来避免各种不必要的损耗</remarks>
    public class CodeTimer
    {
        #region 静态快速计时
        /// <summary>
        /// 计时
        /// </summary>
        /// <param name="times">次数</param>
        /// <param name="action">需要计时的委托</param>
        /// <param name="needTimeOne">是否需要预热</param>
        /// <returns></returns>
        public static CodeTimer Time(Int32 times, Action<Int32> action, Boolean needTimeOne = true)
        {
            CodeTimer timer = new CodeTimer();
            timer.Times = times;
            timer.Action = action;

            if (needTimeOne) timer.TimeOne();
            timer.Time();

            return timer;
        }

        /// <summary>
        /// 计时，并用控制台输出行
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="times">次数</param>
        /// <param name="action">需要计时的委托</param>
        /// <param name="needTimeOne">是否需要预热</param>
        public static void TimeLine(String title, Int32 times, Action<Int32> action, Boolean needTimeOne = true)
        {
            Console.Write("{0}{1}：", new String(' ', 16 - Encoding.Default.GetByteCount(title)), title);

            CodeTimer timer = new CodeTimer();
            timer.Times = times;
            timer.Action = action;
            timer.ShowProgress = true;

            ConsoleColor currentForeColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;

            if (needTimeOne) timer.TimeOne();
            timer.Time();

            Console.WriteLine(timer.ToString());

            Console.ForegroundColor = currentForeColor;
        }

        /// <summary>显示头部</summary>
        /// <param name="title"></param>
        public static void ShowHeader(String title = "指标")
        {
            Write(title, 16);
            Console.Write("：");

            ConsoleColor currentForeColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;

            Write("执行时间", 9);
            Console.Write(" ");
            Write("CPU时间", 9);
            Console.Write(" ");
            Write("指令周期", 15);
            Console.WriteLine("   GC(0/1/2)");

            Console.ForegroundColor = currentForeColor;
        }

        static void Write(String name, Int32 max)
        {
            Int32 len = Encoding.Default.GetByteCount(name);
            if (len < max) Console.Write(new String(' ', max - len));
            Console.Write(name);
        }
        #endregion

        #region PInvoke
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool QueryThreadCycleTime(IntPtr threadHandle, ref ulong cycleTime);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetCurrentThread();

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetThreadTimes(IntPtr hThread, out long lpCreationTime, out long lpExitTime, out long lpKernelTime, out long lpUserTime);

        static Boolean supportCycle = true;
        private static ulong GetCycleCount()
        {
            //if (Environment.Version.Major < 6) return 0;

            if (!supportCycle) return 0;

            try
            {
                ulong cycleCount = 0;
                QueryThreadCycleTime(GetCurrentThread(), ref cycleCount);
                return cycleCount;
            }
            catch
            {
                supportCycle = false;
                return 0;
            }
        }

        private static long GetCurrentThreadTimes()
        {
            long l;
            long kernelTime, userTimer;
            GetThreadTimes(GetCurrentThread(), out l, out l, out kernelTime, out userTimer);
            return kernelTime + userTimer;
        }
        #endregion

        #region 私有字段
        ulong cpuCycles = 0;
        long threadTime = 0;
        int[] gen;
        #endregion

        #region 属性
        private Int32 _Times;
        /// <summary>次数</summary>
        public Int32 Times
        {
            get { return _Times; }
            set { _Times = value; }
        }

        private Action<Int32> _Action;
        /// <summary>迭代方法，如不指定，则使用Time(int index)</summary>
        public Action<Int32> Action
        {
            get { return _Action; }
            set { _Action = value; }
        }

        private Boolean _ShowProgress;
        /// <summary>是否显示控制台进度</summary>
        public Boolean ShowProgress
        {
            get { return _ShowProgress; }
            set { _ShowProgress = value; }
        }

        private Int32 _Index;
        /// <summary>进度</summary>
        public Int32 Index
        {
            get { return _Index; }
            set { _Index = value; }
        }

        private long _CpuCycles;
        /// <summary>CPU周期</summary>
        public long CpuCycles
        {
            get { return _CpuCycles; }
            set { _CpuCycles = value; }
        }

        private long _ThreadTime;
        /// <summary>线程时间，单位是100ns，除以10000转为ms</summary>
        public long ThreadTime
        {
            get { return _ThreadTime; }
            set { _ThreadTime = value; }
        }

        private Int32[] _Gen = new Int32[] { 0, 0, 0 };
        /// <summary>GC代数</summary>
        public Int32[] Gen
        {
            get { return _Gen; }
            set { _Gen = value; }
        }

        private TimeSpan _Elapsed;
        /// <summary>执行时间</summary>
        public TimeSpan Elapsed
        {
            get { return _Elapsed; }
            set { _Elapsed = value; }
        }
        #endregion

        #region 方法
        /// <summary>
        /// 计时核心方法，处理进程和线程优先级
        /// </summary>
        public virtual void Time()
        {
            if (Times <= 0) throw new XException("非法迭代次数！");

            // 设定进程、线程优先级，并在完成时还原
            ProcessPriorityClass pp = Process.GetCurrentProcess().PriorityClass;
            ThreadPriority tp = Thread.CurrentThread.Priority;
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

        /// <summary>
        /// 真正的计时
        /// </summary>
        protected virtual void TimeTrue()
        {
            if (Times <= 0) throw new XException("非法迭代次数！");

            // 统计GC代数
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            gen = new Int32[GC.MaxGeneration + 1];
            for (Int32 i = 0; i <= GC.MaxGeneration; i++)
            {
                gen[i] = GC.CollectionCount(i);
            }

            Stopwatch watch = new Stopwatch();
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

            for (Int32 i = 0; i < Times; i++)
            {
                Index = i;

                action(i);
            }
            if (Action == null)
            {
                // 结束
                Finish();
            }

            CpuCycles = (long)(GetCycleCount() - cpuCycles);
            ThreadTime = GetCurrentThreadTimes() - threadTime;

            watch.Stop();
            Elapsed = watch.Elapsed;

            // 统计GC代数
            List<Int32> list = new List<Int32>();
            for (Int32 i = 0; i <= GC.MaxGeneration; i++)
            {
                int count = GC.CollectionCount(i) - gen[i];
                list.Add(count);
            }
            Gen = list.ToArray();
        }

        /// <summary>
        /// 执行一次迭代，预热所有方法
        /// </summary>
        public void TimeOne()
        {
            Int32 n = Times;

            try
            {
                Times = 1;
                Time();
            }
            finally { Times = n; }
        }

        /// <summary>
        /// 迭代前执行，计算时间
        /// </summary>
        public virtual void Init() { }

        /// <summary>
        /// 每一次迭代，计算时间
        /// </summary>
        /// <param name="index"></param>
        public virtual void Time(Int32 index) { }

        /// <summary>
        /// 迭代后执行，计算时间
        /// </summary>
        public virtual void Finish() { }
        #endregion

        #region 进度
        Thread thread;

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
            Int32 left = Console.CursorLeft;

            // 设置光标不可见
            Boolean cursorVisible = Console.CursorVisible;
            Console.CursorVisible = false;

            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (true)
            {
                try
                {
                    Int32 i = Index;
                    if (i >= Times) break;

                    if (i > 0 && sw.Elapsed.TotalMilliseconds > 10)
                    {
                        Double d = (Double)i / Times;
                        Double ms = sw.Elapsed.TotalMilliseconds;
                        TimeSpan ts = new TimeSpan(0, 0, 0, 0, (Int32)(ms * Times / i));
                        Console.Write("{0,7:n0}ms {1:p} Total=>{2}", ms, d, ts);
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
        /// <summary>
        /// 已重载。输出依次分别是：执行时间、CPU线程时间、时钟周期、GC代数
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("{0,7:n0}ms {1,7:n0}ms {2,15:n0} {3,3}/{4}/{5}", Elapsed.TotalMilliseconds, ThreadTime / 10000, CpuCycles, Gen[0], Gen[1], Gen[2]);
        }
        #endregion
    }
}