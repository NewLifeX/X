using System;
using System.Diagnostics;
using NewLife.Log;

namespace TestST
{
    class Program
    {
        static void Main(string[] args)
        {
            XTrace.UseConsole();

            var sw = Stopwatch.StartNew();

            Test1();

            sw.Stop();
            Console.WriteLine("OK! {0:n0}ms", sw.ElapsedMilliseconds);

            Console.ReadKey();
        }

        static void Test1()
        {
            XTrace.WriteLine("学无先后达者为师！");
        }
    }
}