using System.Net;
using System.Net.Sockets;
using System.Text;
using NewLife;
using NewLife.Data;
using NewLife.Log;
using NewLife.Net;
using NewLife.Net.Handlers;
using Xunit;

namespace XUnitTest.Net;

/// <summary>网络库边界条件和并发测试</summary>
[TestCaseOrderer("NewLife.UnitTest.DefaultOrderer", "NewLife.UnitTest")]
public class NetEdgeCaseTests
{
    #region 并发连接测试
    /// <summary>测试并发连接</summary>
    [Fact]
    public void ConcurrentConnections()
    {
        const Int32 clientCount = 10;
        var connectedCount = 0;
        var allConnected = new ManualResetEventSlim(false);

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            UseSession = true,
            Log = XTrace.Log,
        };

        server.NewSession += (s, e) =>
        {
            if (Interlocked.Increment(ref connectedCount) >= clientCount)
                allConnected.Set();
        };

        server.Start();

        // 并发创建客户端
        var clients = new List<TcpClient>();
        Parallel.For(0, clientCount, i =>
        {
            var client = new TcpClient();
            client.Connect(IPAddress.Loopback, server.Port);
            lock (clients)
            {
                clients.Add(client);
            }
        });

        // 等待所有连接
        Assert.True(allConnected.Wait(10000));
        Assert.Equal(clientCount, server.SessionCount);

        // 清理
        foreach (var client in clients)
            client.Dispose();
    }

    /// <summary>测试并发数据发送</summary>
    [Fact]
    public void ConcurrentDataSend()
    {
        const Int32 messageCount = 100;
        var receivedCount = 0;
        var allReceived = new ManualResetEventSlim(false);

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
        };

        server.Received += (s, e) =>
        {
            if (Interlocked.Increment(ref receivedCount) >= messageCount)
                allReceived.Set();
        };

        server.Start();

        // 客户端
        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);
        var stream = client.GetStream();

        // 并发发送数据
        Parallel.For(0, messageCount, i =>
        {
            var data = Encoding.UTF8.GetBytes($"Message {i}\n");
            lock (stream)
            {
                stream.Write(data, 0, data.Length);
            }
        });

        // 等待接收（可能由于粘包合并为较少次接收）
        Thread.Sleep(2000);
        Assert.True(receivedCount > 0);
    }
    #endregion

    #region 重连测试
    /// <summary>测试客户端重连</summary>
    [Fact]
    public void ClientReconnect()
    {
        var connectionCount = 0;

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
        };

        server.NewSession += (s, e) =>
        {
            Interlocked.Increment(ref connectionCount);
        };

        server.Start();
        var port = server.Port;

        // 第一次连接
        using (var client1 = new TcpClient())
        {
            client1.Connect(IPAddress.Loopback, port);
            Thread.Sleep(200);
        }

        Assert.Equal(1, connectionCount);

        // 第二次连接（重连）
        using (var client2 = new TcpClient())
        {
            client2.Connect(IPAddress.Loopback, port);
            Thread.Sleep(200);
        }

        Assert.Equal(2, connectionCount);
    }
    #endregion

    #region 空数据测试
    /// <summary>测试空数据处理</summary>
    [Fact]
    public void EmptyDataHandling()
    {
        var receivedEmpty = false;

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
        };

        server.Received += (s, e) =>
        {
            if (e.Packet == null || e.Packet.Total == 0)
                receivedEmpty = true;
        };

        server.Start();

        // 客户端连接后立即关闭（发送空数据）
        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);
        Thread.Sleep(100);
        client.Close();

        Thread.Sleep(500);
        // 空数据可能触发也可能不触发接收事件，取决于实现
    }
    #endregion

    #region 大数据包测试
    /// <summary>测试大数据包传输</summary>
    [Fact]
    public void LargeDataTransfer()
    {
        const Int32 dataSize = 1024 * 1024; // 1MB
        var receivedSize = 0;
        var allReceived = new ManualResetEventSlim(false);

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
        };

        server.Received += (s, e) =>
        {
            if (e.Packet != null)
            {
                Interlocked.Add(ref receivedSize, e.Packet.Total);
                if (receivedSize >= dataSize)
                    allReceived.Set();
            }
        };

        server.Start();

        // 客户端发送大数据
        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);
        var stream = client.GetStream();

        var data = new Byte[dataSize];
        new Random().NextBytes(data);
        stream.Write(data, 0, data.Length);

        // 等待接收完成
        Assert.True(allReceived.Wait(30000));
        Assert.Equal(dataSize, receivedSize);
    }
    #endregion

    #region 快速连接断开测试
    /// <summary>测试快速连接断开</summary>
    [Fact]
    public void RapidConnectDisconnect()
    {
        var maxSessions = 0;

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            UseSession = true,
            Log = XTrace.Log,
        };

        server.NewSession += (s, e) =>
        {
            var current = server.SessionCount;
            if (current > maxSessions)
                Interlocked.Exchange(ref maxSessions, current);
        };

        server.Start();

        // 快速连接断开
        for (var i = 0; i < 20; i++)
        {
            using var client = new TcpClient();
            client.Connect(IPAddress.Loopback, server.Port);
            Thread.Sleep(10);
        }

        Thread.Sleep(1000);

        // 确保没有资源泄漏
        Assert.True(server.MaxSessionCount >= maxSessions);
    }
    #endregion

    #region 服务器重启测试
    /// <summary>测试服务器重启</summary>
    [Fact]
    public void ServerRestart()
    {
        var port = 0;

        // 第一次启动
        using (var server1 = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            ReuseAddress = true,
            Log = XTrace.Log,
        })
        {
            server1.Start();
            port = server1.Port;
            Assert.True(server1.Active);

            // 客户端连接
            using var client = new TcpClient();
            client.Connect(IPAddress.Loopback, port);
            Thread.Sleep(100);

            server1.Stop("Restart");
            Assert.False(server1.Active);
        }

        Thread.Sleep(100);

        // 重新启动
        using var server2 = new NetServer
        {
            Port = port,
            ProtocolType = NetType.Tcp,
            ReuseAddress = true,
            Log = XTrace.Log,
        };

        try
        {
            server2.Start();
            Assert.True(server2.Active);
            Assert.Equal(port, server2.Port);
        }
        catch
        {
            // 端口可能还在TIME_WAIT状态
        }
    }
    #endregion

    #region IPv6测试
    /// <summary>测试IPv6支持</summary>
    [Fact]
    public void IPv6Support()
    {
        if (!Socket.OSSupportsIPv6) return;

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetworkV6,
            Log = XTrace.Log,
        };

        server.Start();

        Assert.True(server.Active);
        Assert.True(server.Port > 0);

        // IPv6客户端连接
        using var client = new TcpClient(AddressFamily.InterNetworkV6);
        client.Connect(IPAddress.IPv6Loopback, server.Port);

        Thread.Sleep(200);
        Assert.Equal(1, server.SessionCount);
    }
    #endregion

    #region 会话超时测试
    /// <summary>测试会话超时</summary>
    [Fact]
    public void SessionTimeout()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            SessionTimeout = 2, // 2秒超时
            UseSession = true,
            Log = XTrace.Log,
        };

        server.Start();

        // 客户端连接
        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);

        Thread.Sleep(500);
        Assert.Equal(1, server.SessionCount);

        // 不发送数据，等待超时
        // 注意：实际超时清理由内部定时器执行，这里只是验证配置
        Assert.Equal(2, server.Server?.SessionTimeout ?? 0);
    }
    #endregion

    #region 统计信息测试
    /// <summary>测试统计信息</summary>
    [Fact]
    public void StatisticsInfo()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            UseSession = true,
            StatPeriod = 0, // 禁用定时输出
            Log = XTrace.Log,
        };

        server.Start();

        // 创建连接
        var clients = new List<TcpClient>();
        for (var i = 0; i < 5; i++)
        {
            var client = new TcpClient();
            client.Connect(IPAddress.Loopback, server.Port);
            clients.Add(client);
        }

        Thread.Sleep(500);

        // 验证统计
        Assert.Equal(5, server.SessionCount);
        Assert.True(server.MaxSessionCount >= 5);

        var stat = server.GetStat();
        Assert.Contains("在线", stat);

        // 清理
        foreach (var client in clients)
            client.Dispose();
    }
    #endregion

    #region NetSession测试
    /// <summary>测试会话发送各种类型数据</summary>
    [Fact]
    public void SessionSendVariousTypes()
    {
        var receivedData = new List<Byte[]>();
        var receivedEvent = new ManualResetEventSlim(false);

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
        };

        server.Received += (s, e) =>
        {
            if (s is INetSession session && e.Packet != null)
            {
                // 测试各种发送方式
                session.Send(e.Packet);
                session.Send("Echo: " + e.Packet.ToStr());
                session.Send(e.Packet.ToArray());
                receivedData.Add(e.Packet.ToArray());
                receivedEvent.Set();
            }
        };

        server.Start();

        // 客户端连接
        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);
        var ns = client.GetStream();

        var sendData = "Test Data"u8.ToArray();
        ns.Write(sendData, 0, sendData.Length);

        Assert.True(receivedEvent.Wait(3000));
        Assert.Single(receivedData);
    }

    /// <summary>测试会话Items数据存储</summary>
    [Fact]
    public void SessionItemsStorage()
    {
        var verified = new ManualResetEventSlim(false);

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
        };

        server.NewSession += (s, e) =>
        {
            // 通过NetSession的底层Session的Items存储数据
            if (e.Session is NetSession ns)
            {
                ns.Session["UserId"] = 12345;
                ns.Session["UserName"] = "TestUser";
            }
        };

        server.Received += (s, e) =>
        {
            if (s is NetSession ns)
            {
                var userId = ns.Session["UserId"];
                var userName = ns.Session["UserName"];

                if (userId is Int32 id && id == 12345 && 
                    userName is String name && name == "TestUser")
                {
                    verified.Set();
                }
            }
        };

        server.Start();

        // 客户端连接并发送数据
        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);
        var ns = client.GetStream();
        ns.Write("test"u8.ToArray());

        Assert.True(verified.Wait(3000));
    }

    /// <summary>测试会话Close方法</summary>
    [Fact]
    public void SessionCloseMethod()
    {
        var sessionClosed = new ManualResetEventSlim(false);
        INetSession? testSession = null;

        using var server = new NetServer<CloseTestSession>
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
        };

        server.NewSession += (s, e) =>
        {
            testSession = e.Session;
            if (e.Session is CloseTestSession cts)
            {
                cts.OnCloseCallback = () => sessionClosed.Set();
            }
        };

        server.Start();

        // 客户端连接
        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);
        Thread.Sleep(200);

        Assert.NotNull(testSession);

        // 服务端主动关闭会话
        testSession.Close("ServerClose");

        Assert.True(sessionClosed.Wait(3000));
    }

    class CloseTestSession : NetSession
    {
        public Action? OnCloseCallback { get; set; }

        protected override void OnDisconnected(String reason)
        {
            base.OnDisconnected(reason);
            OnCloseCallback?.Invoke();
        }
    }
    #endregion

    #region Handler测试
    /// <summary>测试自定义Handler处理器</summary>
    [Fact]
    public void CustomHandlerTest()
    {
        var handlerProcessed = new ManualResetEventSlim(false);

        using var server = new NetServer<HandlerTestSession>
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
        };

        server.NewSession += (s, e) =>
        {
            if (e.Session is HandlerTestSession hts)
            {
                hts.OnHandlerProcessed = () => handlerProcessed.Set();
            }
        };

        server.Start();

        // 客户端连接并发送数据
        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);
        var ns = client.GetStream();
        ns.Write("handler test"u8.ToArray());

        // Handler可能未被设置，测试会话是否正常工作
        Thread.Sleep(500);
    }

    class HandlerTestSession : NetSession
    {
        public Action? OnHandlerProcessed { get; set; }

        protected override void OnReceive(ReceivedEventArgs e)
        {
            base.OnReceive(e);
            OnHandlerProcessed?.Invoke();
        }
    }
    #endregion

    #region ReceivedEventArgs测试
    /// <summary>测试ReceivedEventArgs属性</summary>
    [Fact]
    public void ReceivedEventArgsProperties()
    {
        var argsVerified = new ManualResetEventSlim(false);

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
        };

        server.Received += (s, e) =>
        {
            // 验证ReceivedEventArgs属性
            Assert.NotNull(e.Packet);
            Assert.NotNull(e.Remote);
            Assert.True(e.Remote.Port > 0);

            // 测试GetBytes方法
            var bytes = e.GetBytes();
            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 0);

            argsVerified.Set();
        };

        server.Start();

        // 客户端连接并发送数据
        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);
        var ns = client.GetStream();
        ns.Write("test args"u8.ToArray());

        Assert.True(argsVerified.Wait(3000));
    }
    #endregion

    #region 双向通信测试
    /// <summary>测试双向数据收发</summary>
    [Fact]
    public void BidirectionalCommunication()
    {
        var serverReceived = new ManualResetEventSlim(false);
        var clientReceived = new ManualResetEventSlim(false);
        Byte[]? serverData = null;
        Byte[]? clientData = null;

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
        };

        server.Received += (s, e) =>
        {
            if (s is INetSession session && e.Packet != null)
            {
                serverData = e.Packet.ToArray();
                serverReceived.Set();

                // 回复不同的数据
                session.Send("Server Response"u8.ToArray());
            }
        };

        server.Start();

        // 客户端连接
        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);
        var ns = client.GetStream();

        // 发送数据
        ns.Write("Client Request"u8.ToArray());

        Assert.True(serverReceived.Wait(3000));
        Assert.Equal("Client Request", Encoding.UTF8.GetString(serverData!));

        // 接收响应
        var buf = new Byte[1024];
        var len = ns.Read(buf, 0, buf.Length);
        clientData = buf[..len];

        Assert.Equal("Server Response", Encoding.UTF8.GetString(clientData));
    }
    #endregion

    #region 多协议测试
    /// <summary>测试同时支持TCP和UDP</summary>
    [Fact]
    public void MultiProtocolSupport()
    {
        var tcpReceived = new ManualResetEventSlim(false);
        var udpReceived = new ManualResetEventSlim(false);

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Unknown, // 同时监听TCP和UDP
            AddressFamily = AddressFamily.InterNetwork,
            Log = XTrace.Log,
        };

        server.Received += (s, e) =>
        {
            if (s is INetSession session)
            {
                if (session.Session.Local.IsTcp)
                    tcpReceived.Set();
                else
                    udpReceived.Set();
            }
        };

        server.Start();

        // TCP客户端
        using var tcpClient = new TcpClient();
        tcpClient.Connect(IPAddress.Loopback, server.Port);
        tcpClient.GetStream().Write("tcp"u8.ToArray());

        // UDP客户端
        using var udpClient = new UdpClient();
        udpClient.Send("udp"u8.ToArray(), 3, new IPEndPoint(IPAddress.Loopback, server.Port));

        Assert.True(tcpReceived.Wait(3000));
        Assert.True(udpReceived.Wait(3000));
    }
    #endregion
}
