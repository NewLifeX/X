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
    /// <summary>裸 Socket 层基础收发冒烟测试</summary>
    [Collection("Net")]
    public class NetSeverTests
    {
        [Fact(DisplayName = "裸Tcp_客户端发字节_服务端回显一致")]
        public void TcpEmptyData()
        {
            var server = new NetServer
            {
                Port = 0,

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

            try
            {
                var uri = new NetUri($"tcp://127.0.0.1:{server.Port}");
                using (var client = new TcpClient())
                {
                    // 设置接收超时，避免阻塞导致测试挂起
                    client.ReceiveTimeout = 3000;

                    client.Connect(uri.EndPoint);

                    using var ns = client.GetStream();
                    var payload = "Stone@NewLife.com".GetBytes();
                    ns.Write(payload);

                    // 回显读满（NetworkStream 可能短读）
                    var buf = new Byte[1024];
                    var total = 0;
                    while (total < payload.Length)
                    {
                        var rs = ns.Read(buf, total, buf.Length - total);
                        Assert.True(rs > 0, "服务端未回显数据");
                        total += rs;
                    }

                    Assert.Equal(payload, buf[..total]);
                }
            }
            finally
            {
                // 确保停止并释放服务器，避免长期占用端口和后台资源
                server.Stop("UnitTest");
                server.TryDispose();
            }
        }
    }
}