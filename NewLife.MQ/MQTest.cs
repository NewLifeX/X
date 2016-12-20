using System;
using NewLife.Log;

namespace NewLife.MessageQueue
{
    /// <summary>测试用例</summary>
    public class MQTest
    {
        /// <summary>基础测试</summary>
        public static async void TestBase()
        {
            var svr = new MQServer();
            //svr.Server.Log = XTrace.Log;
            svr.Start();

            var client = new MQClient();
            client.Log = XTrace.Log;
            client.Name = "user1";
            await client.CreateTopic("test");

            var user = new MQClient();
            user.Log = XTrace.Log;
            user.Name = "user2";
            user.Received += (s, e) =>
            {
                XTrace.WriteLine("user.收到推送 {0}", e.Arg);
            };
            //user.Open();
            await user.Subscribe("test");

            for (int i = 0; i < 3; i++)
            {
                await client.Public("test", "测试{0}".F(i + 1));
            }

            Console.ReadKey(true);

            client.Dispose();
            user.Dispose();
            svr.Dispose();
        }
    }
}