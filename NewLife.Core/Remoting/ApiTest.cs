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
        public async static void Main()
        {
            var svr = new ApiServer(3344);
            svr.Add("http://*:888/");
            svr.Log = XTrace.Log;
            svr.Register<HelloController>();
            //svr.Encode=new Json();
            //svr.Encode = new ProtocolBuffer();
            svr.Start();


            var client = new ApiClient("udp://127.0.0.1:3344");
            client.Log = XTrace.Log;
            //client.Encode=new Json();
            //client.Encode = new ProtocolBuffer();
            //client.Compress = new SevenZip();
            client.Login("admin", "password");

            var msg = "NewLifeX";
            var rs = await client.Invoke<String>("Hello/SayHello", new { msg });
            Console.WriteLine(rs);

            Console.ReadKey();

            client.Dispose();
            svr.Dispose();
        }

        class HelloController : IApi
        {
            public IApiSession Session { get; set; }

            public String SayHello(String msg)
            {
                return "收到：" + msg;
            }
        }
    }
#endif
}
