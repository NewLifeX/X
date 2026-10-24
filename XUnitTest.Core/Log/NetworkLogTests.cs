using System.Net;
using System.Net.Sockets;
using NewLife.Log;
using Xunit;

namespace XUnitTest.Log;

public class NetworkLogTests
{
    [Fact]
    public void UdpLog()
    {
        using var netLog = new NetworkLog();
        netLog.Write(LogLevel.Info, "This is {0}", Environment.MachineName);
        netLog.Write(LogLevel.Info, "I am {0}", Environment.UserName);
    }

    [Fact]
    public void TcpLog()
    {
        using var netLog = new NetworkLog("tcp://127.0.0.1:514");
        netLog.Write(LogLevel.Info, "This is {0}", Environment.MachineName);
        netLog.Write(LogLevel.Info, "I am {0}", Environment.UserName);
    }

    [Fact(DisplayName = "HTTP日志_本地服务接收")]
    public void HttpLog()
    {
        // 本地化：由本地服务接收日志写入，不依赖公网站点
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var received = 0;
        _ = Task.Run(async () =>
        {
            try
            {
                while (true)
                {
                    using var client = await listener.AcceptTcpClientAsync();
                    using var ns = client.GetStream();

                    var buf = new Byte[1024];
                    var n = await ns.ReadAsync(buf, 0, buf.Length);
                    if (n > 0) Interlocked.Increment(ref received);
                }
            }
            catch { }
        });

        using (var netLog = new NetworkLog($"http://127.0.0.1:{port}/log"))
        {
            netLog.Write(LogLevel.Info, "This is {0}", Environment.MachineName);
            netLog.Write(LogLevel.Info, "I am {0}", Environment.UserName);

            for (var i = 0; i < 10; i++)
            {
                netLog.Write(LogLevel.Info, "Hello +" + i);
            }

            Assert.True(TestWait.Until(() => Volatile.Read(ref received) >= 1, 10_000), "本地服务未在超时内收到日志请求");
        }

        listener.Stop();
    }
}