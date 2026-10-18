using System.ComponentModel;
using System.Net.Sockets;
using NewLife.Messaging;
using NewLife.Net;
using Xunit;
using XUnitTest.Integration;

namespace XUnitTest.Net;

/// <summary>协议编码器日志测试：客户端打开输出一行、服务端启动输出一行、服务端接入会话不逐会话输出</summary>
[Collection("Net.C")]
public class ProtocolLogTests
{
    #region 客户端
    [Fact]
    [DisplayName("协议日志_客户端打开_输出一行协议编码器")]
    public void ClientOpenLogsProtocol()
    {
        using var server = new TcpServer { Port = 0 };
        server.Start();

        var log = new TestLog();
        using var client = new TcpSession
        {
            Remote = new NetUri($"tcp://127.0.0.1:{server.Port}"),
            Protocol = new SrmpCodec(),
            Log = log,
        };

        Assert.True(client.Open());
        Assert.True(log.Contains("Protocol codec"));
        Assert.True(log.Contains(nameof(SrmpCodec)));
        Assert.Equal(1, log.Count("Protocol codec"));
    }

    [Fact]
    [DisplayName("协议日志_未配置协议_客户端打开不输出")]
    public void ClientOpenWithoutProtocolNoLog()
    {
        using var server = new TcpServer { Port = 0 };
        server.Start();

        var log = new TestLog();
        using var client = new TcpSession
        {
            Remote = new NetUri($"tcp://127.0.0.1:{server.Port}"),
            Log = log,
        };

        Assert.True(client.Open());
        Assert.Equal(0, log.Count("Protocol codec"));
    }

    [Fact]
    [DisplayName("协议日志_UDP客户端打开_输出一行协议编码器")]
    public void UdpClientOpenLogsProtocol()
    {
        var log = new TestLog();
        using var client = new UdpServer
        {
            Remote = new NetUri("udp://127.0.0.1:55555"),
            Protocol = new SrmpCodec(),
            Log = log,
        };

        Assert.True(client.Open());
        Assert.True(log.Contains("Protocol codec"));
        Assert.Equal(1, log.Count("Protocol codec"));
    }
    #endregion

    #region 服务端
    [Fact]
    [DisplayName("协议日志_服务端启动_输出一行协议编码器")]
    public void ServerStartLogsProtocolOnce()
    {
        var log = new TestLog();
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            Protocol = new SrmpCodec(),
            Log = log,
        };

        server.Start();
        Assert.Equal(1, log.Count("Protocol codec"));

        // 未配置协议时不输出
        var log2 = new TestLog();
        using var server2 = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            Log = log2,
        };

        server2.Start();
        Assert.Equal(0, log2.Count("Protocol codec"));
    }

    [Fact]
    [DisplayName("协议日志_服务端接入会话_不逐会话输出编码器")]
    public void ServerSessionDoesNotLogProtocol()
    {
        var log = new TestLog();
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            Protocol = new SrmpCodec(),
            Log = log,
            SocketLog = log,
        };

        server.Start();
        Assert.Equal(1, log.Count("Protocol codec"));

        // 客户端接入并保持连接，触发服务端会话启动
        using var client = new TcpSession
        {
            Remote = new NetUri($"tcp://127.0.0.1:{server.Port}"),
            Protocol = new SrmpCodec(),
        };
        Assert.True(client.Open());

        // 等待服务端会话创建（新会话先写 New 行，随后注册进 NetServer.Sessions）
        var end = Environment.TickCount64 + 10_000;
        while (server.Sessions.Count == 0 && Environment.TickCount64 < end) Thread.Sleep(10);

        Assert.True(server.Sessions.Count > 0);
        Assert.True(log.Contains("]New "));

        // 接入会话不输出协议编码器，也不再输出每会话的消息泵日志
        Assert.Equal(1, log.Count("Protocol codec"));
        Assert.Equal(0, log.Count("启动消息泵"));
    }
    #endregion
}

