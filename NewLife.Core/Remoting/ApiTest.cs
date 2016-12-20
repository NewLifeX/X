using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Log;

namespace NewLife.Remoting
{
#if DEBUG
    /// <summary>Rpc测试</summary>
    public class ApiTest
    {
        /// <summary>测试主函数</summary>
        public static async void Main()
        {
            var svr = new ApiServer(3344);
            svr.Add("http://*:888/");
            //svr.Log = XTrace.Log;
            svr.Register<HelloController>();
            svr.Encoder = new JsonEncoder();
            //svr.Encoder = new ProtocolBuffer();
            svr.Start();


            var client = new ApiClient("udp://127.0.0.1:3344") { Encoder = new JsonEncoder() };
            //client.Log = XTrace.Log;
            //client.Encoder = new ProtocolBuffer();
            //client.Compress = new SevenZip();
            client.Open();
            client.Login("admin", "password");

            const string msg = "NewLifeX";
            var rs = await client.Invoke<string>("Hello/Say", new { msg });
            Console.WriteLine(rs);

            Console.ReadKey();

            client.Dispose();
            svr.Dispose();
        }

        private class HelloController : IApi
        {
            public IApiSession Session { get; set; }

            public string Say(string msg)
            {
                return "收到：" + msg;
            }
        }
    }
#endif
}
