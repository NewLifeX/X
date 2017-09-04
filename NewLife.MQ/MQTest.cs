using System;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Log;
using NewLife.Net;
using NewLife.Security;
#if !NET4
using TaskEx = System.Threading.Tasks.Task;
#endif

namespace NewLife.MessageQueue
{
#if DEBUG
    /// <summary>测试用例</summary>
    public class MQTest
    {
        ///// <summary>基础测试</summary>
        //public static async void TestBase()
        //{
        //    var svr = new MQServer();
        //    //svr.Server.Log = XTrace.Log;
        //    svr.Start();

        //    var client = new MQClient();
        //    client.Log = XTrace.Log;
        //    client.Name = "张三";
        //    client.Open();

        //    var user = new MQClient();
        //    user.Log = XTrace.Log;
        //    user.Name = "李四";
        //    user.Open();

        //    //user.Received += (s, e) =>
        //    //{
        //    //    XTrace.WriteLine("user.收到推送 {0}", e.Arg);
        //    //};
        //    await user.Subscribe("新生命团队");

        //    for (var i = 0; i < 3; i++)
        //    {
        //        await client.Public("测试{0}".F(i + 1));
        //    }

        //    Console.ReadKey(true);

        //    client.Dispose();
        //    user.Dispose();
        //    svr.Dispose();
        //}

        ///// <summary>分离式</summary>
        //public static void Main()
        //{
        //    Console.Write("选择模式 客户端=1，服务端=2 ：");
        //    var mode = Console.ReadKey().KeyChar.ToString().ToInt();
        //    Console.WriteLine();

        //    if (mode == 1)
        //    {
        //        Console.Write("用户名：");
        //        var user = Console.ReadLine();
        //        Console.Write("主题：");
        //        var topic = Console.ReadLine();

        //        if (user.IsNullOrEmpty()) user = "test";
        //        if (topic.IsNullOrEmpty()) topic = "新生命团队";

        //        // 创建MQ客户端
        //        var client = new MQClient();
        //        client.Log = XTrace.Log;
        //        if (user.Contains("@"))
        //        {
        //            client.Remote.Host = user.Substring("@");
        //            user = user.Substring(null, "@");
        //        }
        //        client.Name = user;
        //        client.EnsureCreate();
        //        client.Client.UserName = "test";
        //        client.Client.Password = "test";
        //        client.Open();

        //        //client.Received += (s, e) =>
        //        //{
        //        //    XTrace.WriteLine("user.收到推送 {0}", e.Arg);
        //        //};

        //        var task = Task.Run(async () =>
        //        {
        //            await client.Subscribe(topic);
        //        });
        //        task.Wait();

        //        while (true)
        //        {
        //            Console.Write("发布消息：");
        //            var str = Console.ReadLine();
        //            if (!str.IsNullOrEmpty())
        //            {
        //                if (str.EqualIgnoreCase("exit", "quit")) break;

        //                Task.Run(() => client.Public(str));
        //            }
        //        }
        //    }
        //    else
        //    {
        //        var svr = new MQServer();
        //        svr.Server.Log = XTrace.Log;
        //        //svr.Server.Anonymous = true;
        //        svr.Start();

        //        var ns = svr.Server.Servers[0] as NetServer;
        //        while (true)
        //        {
        //            Console.Title = ns.GetStat();
        //            Thread.Sleep(500);
        //        }

        //        //Console.ReadKey();
        //    }
        //}

        /// <summary>进程型消息队列</summary>
        public static void Main2()
        {
            var host = new MQHost();
            host.Log = XTrace.Log;
            host.Tip = true;

            host.Subscribe("aaa", "ttt", null, OnMessage);
            host.Subscribe("bbb", "ttt", "t1||t2", OnMessage);
            host.Subscribe("ccc", "ttt", "t1||t3", OnMessage, 111);
            host.Subscribe("ccc", "ttt", "t1||t3", OnMessage, 222);

            var tags = "t1,t2,t3,t4".Split(",");
            for (int i = 0; i < 1000; i++)
            {
                Console.WriteLine();
                host.Send("大石头", "ttt", tags[Rand.Next(tags.Length)], Rand.NextString(16));
                Thread.Sleep(1000);
            }
        }

        static async Task OnMessage(Subscriber sb, Message m)
        {
            await TaskEx.Delay(Rand.Next(200));
            XTrace.WriteLine("{0}=>{3} [{1}]: {2} {4}", m.Sender, m.Tag, m.Content, sb.Host.User, sb.User);
        }
    }
#endif
}