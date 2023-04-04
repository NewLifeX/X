using System;
using System.Diagnostics;
using NewLife.Log;
using NewLife.Threading;
using NewLife;

namespace Test2
{
    internal class Program
    {
        static void Main(string[] args)
        {
            XTrace.UseConsole();

            TimerScheduler.Default.Log = XTrace.Log;

            while (true)
            {
                var sw = Stopwatch.StartNew();
#if !DEBUG
                try
                {
#endif
                    Test1();
#if !DEBUG
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex?.GetTrue());
                }
#endif

                sw.Stop();
                Console.WriteLine("OK! 耗时 {0}", sw.Elapsed);
                //Thread.Sleep(5000);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                var key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.C) break;
            }
        }

        static void Test1()
        {
            var str = $"{DateTime.Now:yyyy}年，学无先后达者为师！";
            str.SpeakAsync();
        }
    }
}
