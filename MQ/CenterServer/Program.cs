using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Log;

namespace CenterServer
{
    class Program
    {
        static void Main(string[] args)
        {
            XTrace.UseConsole();
            new NewLife.Queue.Center.CenterServer().Start();
            Console.ReadLine();
        }
    }
}
