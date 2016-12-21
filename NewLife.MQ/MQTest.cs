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
            client.Name = "张三";
            await client.Login();
            await client.CreateTopic("新生命团队");

            var user = new MQClient();
            user.Log = XTrace.Log;
            user.Name = "李四";
            await user.Login();
            user.Received += (s, e) =>
            {
                XTrace.WriteLine("user.收到推送 {0}", e.Arg);
            };
            await user.Subscribe("新生命团队");

            for (int i = 0; i < 3; i++)
            {
                await client.Public("测试{0}".F(i + 1));
            }

            Console.ReadKey(true);

            client.Dispose();
            user.Dispose();
            svr.Dispose();
        }

        /// <summary>分离式</summary>
        public static async void Main()
        {
            Console.Write("选择模式 客户端=0，服务端=1 ：");
            var mode = Console.ReadKey().KeyChar.ToString().ToInt();
            Console.WriteLine();

            if (mode == 0)
            {
                Console.Write("用户名：");
                var user = Console.ReadLine();
                Console.Write("主题：");
                var topic = Console.ReadLine();

                if (user.IsNullOrEmpty()) user = "test";
                if (topic.IsNullOrEmpty()) topic = "新生命团队";

                // 创建MQ客户端
                var client = new MQClient();
                client.Log = XTrace.Log;
                client.Name = user;
                await client.Login();
                await client.CreateTopic(topic);

                client.Received += (s, e) =>
                {
                    XTrace.WriteLine("user.收到推送 {0}", e.Arg);
                };
                await client.Subscribe(topic);

                while (true)
                {
                    var str = Console.ReadLine();
                    if (!str.IsNullOrEmpty())
                    {
                        if (str.EqualIgnoreCase("exit", "quit")) break;

                        await client.Public(str);
                    }
                }
            }
            else
            {
                var svr = new MQServer();
                svr.Server.Log = XTrace.Log;
                svr.Start();
            }
        }
    }
}