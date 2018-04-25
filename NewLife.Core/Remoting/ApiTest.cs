using System;
using System.Linq;
using NewLife.Log;
using NewLife.Net;

namespace NewLife.Remoting
{
#if DEBUG
    /// <summary>Rpc测试</summary>
    [Api(null)]
    public class ApiTest
    {
        /// <summary>测试主函数</summary>
        public static void Main()
        {
            Console.WriteLine("模式（1服务端、2客户端）");
            var cki = Console.ReadKey(true);
            if (cki.KeyChar == '1')
                TestServer();
            else if (cki.KeyChar == '2')
                TestClient();
        }

        private static void TestServer()
        {
            var svr = new ApiServer(3344)
            {
                Log = XTrace.Log,
                EncoderLog = XTrace.Log
            };
            svr.Register<HelloController>();

            //var ns = svr.Server as NetServer;
            //ns.LogSend = true;
            //ns.LogReceive = true;

            svr.Start();

            Console.ReadKey();
        }

        private static async void TestClient()
        {
            var client = new ApiClient("tcp://127.0.0.1:3344")
            {
                Log = XTrace.Log,
                EncoderLog = XTrace.Log
            };

            //var sc = client.Client;
            //sc.LogSend = true;
            //sc.LogReceive = true;

            client.Open();

            var msg = "NewLifeX";
            Console.WriteLine();
            var rs = await client.InvokeAsync<String>("Say", new { msg });
            XTrace.WriteLine(rs);

            Console.WriteLine();
            rs = await client.InvokeAsync<String>("Hello/Eat", new { msg });
            XTrace.WriteLine(rs);

            Console.WriteLine();
            rs = await client.InvokeAsync<String>("Sleep", new { msg });
            XTrace.WriteLine(rs);

            Console.WriteLine();
            try
            {
                msg = "报错";
                rs = await client.InvokeAsync<String>("Say", new { msg });
            }
            catch (ApiException ex)
            {
                XTrace.WriteLine("服务端发生 {0} 错误：{1}", ex.Code, ex.Message);
            }

            Console.WriteLine();
            var apis = await client.InvokeAsync<String[]>("Api/All");
            Console.WriteLine(apis.Join(Environment.NewLine));

            Console.WriteLine("完成");
            Console.ReadKey();
        }

        [Api(null)]
        private class HelloController : IApi
        {
            public IApiSession Session { get; set; }

            [Api("Say")]
            public String Say(String msg)
            {
                if (msg == "报错") throw new Exception("出错，上一次 " + Session["Last"]);

                Session["Last"] = msg;

                var ss = Session.AllSessions;

                return "收到：{0} 在线：{1}".F(msg, ss.Length);
            }

            [Api("Hello/*")]
            public String Local()
            {
                var ctx = ControllerContext.Current;

                return "本地执行：{0}({1})".F(ctx.Action.Name, ctx.Parameters.Select(e => e.Key).Join(", "));
            }

            [Api("*")]
            public String Global()
            {
                var ctx = ControllerContext.Current;

                return "全局执行：{0}({1})".F(ctx.Action.Name, ctx.Parameters.Select(e => e.Key).Join(", "));
            }
        }
    }
#endif
}