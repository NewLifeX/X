using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using NewLife;
using NewLife.Log;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

/// <summary>会话资源回收测试：不活动会话超时后由会话集合自动清理，连接资源得到释放</summary>
/// <remarks>通过把 ClearPeriod 缩小到 1 秒缩短等待，避免默认 10 秒清理周期拖慢用例</remarks>
[Collection("Net")]
public class NetTimeoutCleanupTests
{
    /// <summary>UDP：客户端停止发包后，超时不活动会话应从会话集合清除</summary>
    [Fact(DisplayName = "资源回收_UDP会话超时_集合自动清理归零")]
    public void UdpSessionTimeout_Cleaned()
    {
        using var server = new UdpServer { Port = 0, SessionTimeout = 1, Log = XTrace.Log };
        ((SessionCollection)server.Sessions).ClearPeriod = 1;
        server.Open();

        using var sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        sock.SendTo("hello".GetBytes(), new IPEndPoint(IPAddress.Loopback, server.Port));

        // 等待会话建立
        var sw = Stopwatch.StartNew();
        while (server.Sessions.Count == 0 && sw.ElapsedMilliseconds < 5000) Thread.Sleep(20);
        Assert.Single(server.Sessions);

        // 静默等待：超时1秒+清理周期1秒，数秒内自动移除
        sw.Restart();
        while (server.Sessions.Count > 0 && sw.ElapsedMilliseconds < 15000) Thread.Sleep(50);
        Assert.Empty(server.Sessions);
    }

    /// <summary>TCP：空闲连接超时后服务端清理会话，客户端同步感知断开</summary>
    [Fact(DisplayName = "资源回收_TCP空闲会话超时_服务端清理且客户端感知")]
    public void TcpIdleSessionTimeout_CleanedAndDetected()
    {
        using var server = new TcpServer { Port = 0, SessionTimeout = 1, Log = XTrace.Log };
        ((SessionCollection)server.Sessions).ClearPeriod = 1;
        server.Start();

        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);

        // 等待服务端会话建立（LastTime 为建立时刻，空闲即不活跃）
        var sw = Stopwatch.StartNew();
        while (server.Sessions.Count == 0 && sw.ElapsedMilliseconds < 5000) Thread.Sleep(20);
        Assert.Single(server.Sessions);

        // 静默等待超时清理
        sw.Restart();
        while (server.Sessions.Count > 0 && sw.ElapsedMilliseconds < 15000) Thread.Sleep(50);
        Assert.Empty(server.Sessions);

        // 客户端应感知断开：读取返回0或连接级异常
        client.ReceiveTimeout = 5000;
        using var ns = client.GetStream();
        try
        {
            var n = ns.Read(new Byte[16], 0, 16);
            Assert.Equal(0, n);
        }
        catch (IOException)
        {
            // 连接被服务端关闭/重置亦为有效断开语义
        }
    }
}
