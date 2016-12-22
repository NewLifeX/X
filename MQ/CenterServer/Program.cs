using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Log;
using NewLife.Net;
using NewLife.Queue.Broker;
using NewLife.Queue.Center;
using NewLife.Queue.Clients;
using NewLife.Remoting;

namespace CenterService
{
    class Program
    {
        private static BrokerService svr;
        static void Main(string[] args)
        {
            XTrace.UseConsole();
            XTrace.Debug = true;
            svr = new BrokerService { ProducerServer = { Log = XTrace.Log } };
            Test();
            System.Threading.Thread.Sleep(1000);
            Client();
            Console.ReadLine();
        }

        static void Test()
        {
           svr.Start();
        }

        static async void Client()
        {
            var client = new ProducerClient {Log = XTrace.Log};
            await client.RegisterProducer();
        }
    }
}
