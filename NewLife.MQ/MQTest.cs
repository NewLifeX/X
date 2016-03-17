using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NewLife.Log;

namespace NewLife.MessageQueue
{
    /// <summary>测试用例</summary>
    public class MQTest
    {
        /// <summary>基础测试</summary>
        public static void TestBase()
        {
            var svr = new MQServer();
            svr.Start();

            var client = new MQClient();
            client.Name = "user1";
            client.Public("test");

            var user = new MQClient();
            user.Name = "user2";
            user.Received += (s, e) =>
            {
                XTrace.WriteLine("user.收到推送 {0}", e.Arg);
            };
            //user.Open();
            user.Subscribe("test");

            for (int i = 0; i < 3; i++)
            {
                client.Send("test", "测试{0}".F(i + 1));
            }

            Console.ReadKey(true);

            client.Dispose();
            user.Dispose();
            svr.Dispose();
        }
    }
}