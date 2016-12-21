using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Log;
using NewLife.Net;
using NewLife.Queue.Center;
using NewLife.Remoting;

namespace CenterService
{
    class Program
    {
        static void Main(string[] args)
        {
            XTrace.UseConsole();
            XTrace.Debug = true;
            Test();
            Console.ReadLine();
        }

        static async void Test()
        {

            var svr = new CenterServer(3344);
            svr.Start();
            var client = new ApiClient("tcp://127.0.0.1:3344") { Encoder = new JsonEncoder() };
            client.Log = XTrace.Log;
            //client.Encoder = new ProtocolBuffer();
            //client.Compress = new SevenZip();
            client.Open();
            client.Login("admin", "password");

            const string msg = "NewLifeX";
            var rs = await client.InvokeAsync<string>("Demo/Say", new { msg });
            Console.WriteLine(rs);

            client.Dispose();
            svr.Dispose();
        }

       
    }
}
