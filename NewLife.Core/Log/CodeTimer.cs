using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using NewLife.Reflection;

namespace NewLife.Log
{
    /// <summary>
    /// 代码性能计时器输出回调
    /// </summary>
    /// <param name="ts"></param>
    /// <param name="cpu"></param>
    /// <param name="gen"></param>
    public delegate void CodeTimerOutputCallback(TimeSpan ts, long cpu, Int32[] gen);

    /// <summary>
    /// 代码性能计数器
    /// </summary>
    /// <remarks>参考了老赵（http://www.cnblogs.com/jeffreyzhao/archive/2009/03/10/codetimer.html）和eaglet（http://www.cnblogs.com/eaglet/archive/2009/03/10/1407791.html）两位的作品</remarks>
    public static class CodeTimer
    {
        #region 初始化
        /// <summary>
        /// 初始化
        /// </summary>
        public static void Initialize()
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            //Time("", 1, () => { });
        }

        #endregion

        #region 计时
        /// <summary>
        /// 计时
        /// </summary>
        /// <param name="iteration"></param>
        /// <param name="action"></param>
        /// <param name="output"></param>
        public static void Time(Int32 iteration, Action<Int32> action, [Optional]CodeTimerOutputCallback output)
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            Int32[] gcCounts = new Int32[GC.MaxGeneration + 1];
            for (Int32 i = 0; i <= GC.MaxGeneration; i++)
            {
                gcCounts[i] = GC.CollectionCount(i);
            }

            Stopwatch watch = new Stopwatch();
            watch.Start();
            long cycleCount = GetCPU();
            for (Int32 i = 0; i < iteration; i++) action(i);
            long cpuCycles = GetCPU() - cycleCount;
            watch.Stop();

            List<Int32> list = new List<Int32>();
            for (Int32 i = 0; i <= GC.MaxGeneration; i++)
            {
                int count = GC.CollectionCount(i) - gcCounts[i];
                list.Add(count);
            }

            if (output == null)
            {
                output = delegate(TimeSpan ts2, long cpuCycles2, Int32[] gen2)
                {
                    ConsoleColor currentForeColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;

                    Console.WriteLine(Format(ts2, cpuCycles2, gen2));

                    Console.ForegroundColor = currentForeColor;
                };
            }
            output(watch.Elapsed, cpuCycles, list.ToArray());
        }

        /// <summary>
        /// 格式化
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="cpu"></param>
        /// <param name="gen"></param>
        /// <returns></returns>
        public static String Format(TimeSpan ts, long cpu, Int32[] gen)
        {
            return String.Format("{0:n0}ms {1:n0} {2}/{3}/{4}", ts.TotalMilliseconds, cpu, gen[0], gen[1], gen[2]);
        }

        private static long GetCPU()
        {
            //TODO 检查系统版本，新版本使用QueryThreadCycleTime，旧版本使用GetThreadTimes
            if (Environment.Version.Major >= 6)
                return (long)GetCycleCount();
            else
                return GetCurrentThreadTimes();
        }

        private static ulong GetCycleCount()
        {
            ulong cycleCount = 0;
            QueryThreadCycleTime(GetCurrentThread(), ref cycleCount);
            return cycleCount;
        }

        private static long GetCurrentThreadTimes()
        {
            long l;
            long kernelTime, userTimer;
            GetThreadTimes(GetCurrentThread(), out l, out l, out kernelTime, out userTimer);
            return kernelTime + userTimer;
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
        #endregion
    }
}
