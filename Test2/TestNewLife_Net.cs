using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Security.Policy;
using System.Text;
using System.Threading;
using NewLife.Log;
using NewLife.Net;
using NewLife.Net.Sockets;

namespace Test2
{
    public class TestNewLife_Net
    {
        /// <summary>
        /// 测试网络端口号
        /// </summary>
        private int testPort = 65530;

        #region 测试管理
        public void StartTest()
        {
            XTrace.WriteLine("测试结束");
            StartServer();

            TestSend();
            TestSendAndReceive();
            TestSendButNoResponse();
            TestSendButNoResponse10Times();
            TestAsyncReceive();
        }

        public void StopTest()
        {
            _Server.Stop();
            XTrace.WriteLine("开始测试");
        }
        #endregion

        #region 服务端
        private NetServer _Server;
        public void StartServer(int port = 65530)
        {
            _Server = new NetServer();
            _Server.Port = port;
            _Server.NewSession += _Server_NewSession;
            _Server.Start();
        }

        void _Server_NewSession(object sender, SessionEventArgs e)
        {

            e.Session.Received += ServerSession_Received;
        }

        void ServerSession_Received(object sender, ReceivedEventArgs e)
        {
            var str = e.ToStr();
            XTrace.WriteLine("----------服务端接收：" + str);
            if (str.StartsWith("echo:"))
            {
                var session = sender as ISocketSession;
                session.Send(str);
            }
        }
        #endregion

        #region 客户端
        private ISocketSession CreateSession(string host, int port = 65530, ProtocolType protocolType = ProtocolType.Tcp)
        {
            var ip = NetHelper.ParseAddress(host);
            var session = NetService.CreateSession(new NetUri(protocolType, ip, port));
            //session.Received += ClientSession_Received;
            return session;
        }

        void ClientSession_Received(object sender, ReceivedEventArgs e)
        {
            XTrace.WriteLine("==========客户端接收：" + e.ToStr());
        }

        public void TestSend()
        {
            XTrace.WriteLine("TestSend");
            string str =
                "中国11个连片特困区大多数处于气候、水文、土地使用、森林等多种系统边缘，对气候变化的影响异常敏感脆弱。这些地区的扶贫减贫工作需要与气候变化适应、粮食安全保障、贫困人群生计安全以及贫困地区生态保护等有机结合起来。”";

            var session = CreateSession("127.0.0.1", testPort);
            session.Send(str);
            session.Dispose();
        }

        public void TestSendAndReceive()
        {
            XTrace.WriteLine("TestSendAndReceive");
            string str =
                "echo:中国11个连片特困区大多数处于气候、水文、土地使用、森林等多种系统边缘，对气候变化的影响异常敏感脆弱。这些地区的扶贫减贫工作需要与气候变化适应、粮食安全保障、贫困人群生计安全以及贫困地区生态保护等有机结合起来。”";

            var session = CreateSession("127.0.0.1", testPort);
            session.Send(str);
#if DEBUG
            Stopwatch sw = new Stopwatch();
            sw.Start();
#endif
            var bs = session.Receive();
            XTrace.WriteLine(bs.ToStr());
#if DEBUG
            sw.Stop();
            XTrace.WriteLine("+++++接收数据耗时：" + sw.ElapsedMilliseconds + " ms");
#endif
            session.Dispose();
        }

        public void TestSendButNoResponse()
        {
            XTrace.WriteLine("TestSendButNoResponse");
            string str =
                "中国11个连片特困区大多数处于气候、水文、土地使用、森林等多种系统边缘，对气候变化的影响异常敏感脆弱。这些地区的扶贫减贫工作需要与气候变化适应、粮食安全保障、贫困人群生计安全以及贫困地区生态保护等有机结合起来。”";

            var session = CreateSession("127.0.0.1", testPort);
            session.Send(str);
#if DEBUG
            Stopwatch sw = new Stopwatch();
            sw.Start();
#endif
            var bs = session.Receive();
            XTrace.WriteLine(bs.ToStr());
#if DEBUG
            sw.Stop();
            XTrace.WriteLine("+++++接收数据耗时：" + sw.ElapsedMilliseconds + " ms");
#endif
            session.Dispose();
        }

        public void TestSendButNoResponse10Times()
        {
            XTrace.WriteLine("TestSendButNoResponse10Times");
            string str =
                "中国11个连片特困区大多数处于气候、水文、土地使用、森林等多种系统边缘，对气候变化的影响异常敏感脆弱。这些地区的扶贫减贫工作需要与气候变化适应、粮食安全保障、贫困人群生计安全以及贫困地区生态保护等有机结合起来。”";

            var session = CreateSession("127.0.0.1", testPort);
            for (int i = 0; i < 10; i++)
            {
                XTrace.WriteLine("第{0}次", i + 1);
                session.Send(str);
#if DEBUG
                Stopwatch sw = new Stopwatch();
                sw.Start();
#endif
                var bs = session.Receive();
                XTrace.WriteLine(bs.ToStr());
#if DEBUG
                sw.Stop();
                XTrace.WriteLine("+++++接收数据耗时：" + sw.ElapsedMilliseconds + " ms");
#endif
            }

            session.Dispose();
        }

        public void TestAsyncReceive()
        {
            XTrace.WriteLine("TestSendAndReceive");
            string str =
                "echo:中国11个连片特困区大多数处于气候、水文、土地使用、森林等多种系统边缘，对气候变化的影响异常敏感脆弱。这些地区的扶贫减贫工作需要与气候变化适应、粮食安全保障、贫困人群生计安全以及贫困地区生态保护等有机结合起来。”";

            var session = CreateSession("127.0.0.1", testPort);
            session.Received += ClientSession_Received;
            session.ReceiveAsync();
            session.Send(str);
        }
        #endregion
    }
}
