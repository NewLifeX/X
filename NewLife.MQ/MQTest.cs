using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            client.Public("test");

            var user = new MQClient();
            user.Subscribe("test");
            user.Received += (s, e) =>
            {

            };

            for (int i = 0; i < 10; i++)
            {
                client.Send("test", "测试 {0}".F(i + 1));
            }
        }
    }
}