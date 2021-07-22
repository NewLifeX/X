using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using NewLife;
using NewLife.Log;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net
{
    public class NetSeverTests
    {
        [Fact]
        public void TcpEmptyData()
        {
            var server = new NetServer
            {
                Port = 7777,

                Log = XTrace.Log,
                SessionLog = XTrace.Log,
                SocketLog = XTrace.Log,
                LogSend = true,
                LogReceive = true,
            };

            server.Received += (s, e) =>
            {
                var ss = s as INetSession;
                ss.Send(e.Packet);
            };

            server.Start();

            {
                var uri = new NetUri($"tcp://127.0.0.1:{server.Port}");
                var client = new TcpClient();
                client.Connect(uri.EndPoint);

                var ns = client.GetStream();
                ns.Write("Stone@NewLife.com".GetBytes());

                var buf = new Byte[1024];
                var rs = ns.Read(buf, 0, buf.Length);
            }

            Thread.Sleep(10_000);
        }
    }
}