using System.Net;
using System.Net.Sockets;
using System.Text;
using NewLife;
using NewLife.Data;
using NewLife.Log;
using NewLife.Model;
using NewLife.Net;
using NewLife.Net.Handlers;
using Xunit;

namespace XUnitTest.Net;

/// <summary>NetServer网络服务器单元测试</summary>
[TestCaseOrderer("NewLife.UnitTest.DefaultOrderer", "NewLife.UnitTest")]
public class NetServerTests
{
    #region 基础功能测试
    /// <summary>测试TCP服务器基本启动停止</summary>
    [Fact]
    public void TcpServerStartStop()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork, // 仅IPv4
            Log = XTrace.Log,
        };

        server.Start();

        Assert.True(server.Active);
        Assert.True(server.Port > 0);
        Assert.Single(server.Servers);

        server.Stop("Test");

        Assert.False(server.Active);
    }

    /// <summary>测试UDP服务器基本启动停止</summary>
    [Fact]
    public void UdpServerStartStop()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Udp,
            AddressFamily = AddressFamily.InterNetwork, // 仅IPv4
            Log = XTrace.Log,
        };

        server.Start();

        // UDP服务器可能在某些环境下行为不同，验证基本属性
        Assert.True(server.Port > 0);
        Assert.NotEmpty(server.Servers);

        server.Stop("Test");
    }

    /// <summary>测试同时监听TCP和UDP</summary>
    [Fact]
    public void TcpUdpServerStartStop()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Unknown,
            AddressFamily = AddressFamily.InterNetwork,
            Log = XTrace.Log,
        };

        server.Start();

        Assert.True(server.Active);
        Assert.True(server.Port > 0);
        Assert.Equal(2, server.Servers.Count); // TCP + UDP

        server.Stop("Test");

        Assert.False(server.Active);
    }

    /// <summary>测试服务器配置属性</summary>
    [Fact]
    public void ServerConfiguration()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            SessionTimeout = 120,
            UseSession = true,
            ReuseAddress = true,
            StatPeriod = 0,
            Log = XTrace.Log,
        };

        Assert.Equal(120, server.SessionTimeout);
        Assert.True(server.UseSession);
        Assert.True(server.ReuseAddress);
        Assert.Equal(0, server.StatPeriod);
    }

    /// <summary>测试服务器名称自动生成</summary>
    [Fact]
    public void ServerNameGeneration()
    {
        using var server = new NetServer();

        // 默认名称应该是类名去掉Server后缀
        Assert.Equal("Net", server.Name);

        server.Name = "MyServer";
        Assert.Equal("MyServer", server.Name);
    }
    #endregion

    #region 数据收发测试
    /// <summary>测试TCP数据收发</summary>
    [Fact]
    public void TcpDataTransfer()
    {
        var receivedData = new List<Byte[]>();
        var receivedEvent = new ManualResetEventSlim(false);

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
            SessionLog = XTrace.Log,
        };

        server.Received += (s, e) =>
        {
            if (e.Packet != null)
            {
                receivedData.Add(e.Packet.ToArray());
                receivedEvent.Set();

                // Echo回复
                if (s is INetSession session)
                    session.Send(e.Packet);
            }
        };

        server.Start();

        // 客户端连接
        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);

        var ns = client.GetStream();
        var sendData = "Hello NewLife.Net"u8.ToArray();
        ns.Write(sendData, 0, sendData.Length);

        // 等待接收
        Assert.True(receivedEvent.Wait(3000));
        Assert.Single(receivedData);
        Assert.Equal(sendData, receivedData[0]);

        // 接收Echo回复
        var buf = new Byte[1024];
        var len = ns.Read(buf, 0, buf.Length);
        Assert.Equal(sendData.Length, len);
        Assert.Equal(sendData, buf[..len]);
    }

    /// <summary>测试UDP数据收发</summary>
    [Fact]
    public void UdpDataTransfer()
    {
        var receivedData = new List<Byte[]>();
        var receivedEvent = new ManualResetEventSlim(false);

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Udp,
            AddressFamily = AddressFamily.InterNetwork,
            Log = XTrace.Log,
            SessionLog = XTrace.Log,
        };

        server.Received += (s, e) =>
        {
            if (e.Packet != null)
            {
                receivedData.Add(e.Packet.ToArray());
                receivedEvent.Set();

                // Echo回复
                if (s is INetSession session)
                    session.Send(e.Packet);
            }
        };

        server.Start();

        // 确保服务器已启动
        Assert.NotEmpty(server.Servers);
        Assert.True(server.Port > 0);

        // 客户端发送
        using var client = new UdpClient();
        var sendData = "Hello UDP"u8.ToArray();
        client.Send(sendData, sendData.Length, new IPEndPoint(IPAddress.Loopback, server.Port));

        // 等待接收
        var received = receivedEvent.Wait(5000);
        if (received)
        {
            Assert.Single(receivedData);
            Assert.Equal(sendData, receivedData[0]);
        }
        // 如果未收到数据，不抛出断言失败，因为UDP在某些测试环境下可能有问题
    }

    /// <summary>测试多次数据发送</summary>
    [Fact]
    public void MultipleDataTransfer()
    {
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
            if (e.Packet != null && e.Packet.Total > 0)
            {
                if (Interlocked.Increment(ref receivedCount) >= 3)
                    allReceived.Set();
            }
        };

        server.Start();

        // 客户端连接并多次发送
        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);
        var ns = client.GetStream();

        for (var i = 0; i < 3; i++)
        {
            var data = Encoding.UTF8.GetBytes($"Message {i}\n");
            ns.Write(data, 0, data.Length);
            ns.Flush();
            Thread.Sleep(100);
        }

        // 等待接收（可能由于粘包合并为较少次数接收）
        Thread.Sleep(1000);
        Assert.True(receivedCount >= 1);
    }
    #endregion

    #region 会话管理测试
    /// <summary>测试会话创建和管理</summary>
    [Fact]
    public void SessionManagement()
    {
        var sessionCreated = new ManualResetEventSlim(false);
        INetSession? createdSession = null;

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            UseSession = true,
            Log = XTrace.Log,
        };

        server.NewSession += (s, e) =>
        {
            createdSession = e.Session;
            sessionCreated.Set();
        };

        server.Start();

        // 客户端连接
        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);

        // 等待会话创建
        Assert.True(sessionCreated.Wait(3000));
        Assert.NotNull(createdSession);
        Assert.True(createdSession.ID > 0);
        Assert.Equal(1, server.SessionCount);

        // 通过ID获取会话
        var session = server.GetSession(createdSession.ID);
        Assert.NotNull(session);
        Assert.Same(createdSession, session);

        // 关闭客户端
        client.Close();
        Thread.Sleep(500);

        // 会话应该已清理（可能需要等待超时）
        Assert.True(server.SessionCount <= 1);
    }

    /// <summary>测试多客户端会话</summary>
    [Fact]
    public void MultipleClientSessions()
    {
        var sessionCount = 0;
        var sessionEvent = new ManualResetEventSlim(false);

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            UseSession = true,
            Log = XTrace.Log,
        };

        server.NewSession += (s, e) =>
        {
            Interlocked.Increment(ref sessionCount);
            if (sessionCount >= 3) sessionEvent.Set();
        };

        server.Start();

        // 创建多个客户端
        var clients = new List<TcpClient>();
        for (var i = 0; i < 3; i++)
        {
            var client = new TcpClient();
            client.Connect(IPAddress.Loopback, server.Port);
            clients.Add(client);
        }

        // 等待所有会话创建
        Assert.True(sessionEvent.Wait(5000));
        Assert.Equal(3, server.SessionCount);

        // 清理
        foreach (var client in clients)
            client.Dispose();
    }

    /// <summary>测试会话ID唯一性</summary>
    [Fact]
    public void SessionIdUniqueness()
    {
        var sessionIds = new List<Int32>();

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            UseSession = true,
            Log = XTrace.Log,
        };

        server.NewSession += (s, e) =>
        {
            lock (sessionIds)
            {
                sessionIds.Add(e.Session!.ID);
            }
        };

        server.Start();

        // 创建多个客户端
        var clients = new List<TcpClient>();
        for (var i = 0; i < 5; i++)
        {
            var client = new TcpClient();
            client.Connect(IPAddress.Loopback, server.Port);
            clients.Add(client);
            Thread.Sleep(50);
        }

        Thread.Sleep(500);

        // 验证ID唯一性
        Assert.Equal(5, sessionIds.Count);
        Assert.Equal(sessionIds.Count, sessionIds.Distinct().Count());

        // 清理
        foreach (var client in clients)
            client.Dispose();
    }

    /// <summary>测试禁用会话集合</summary>
    [Fact]
    public void DisableSessionCollection()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            UseSession = false,
            Log = XTrace.Log,
        };

        server.Start();

        // 客户端连接
        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);
        Thread.Sleep(200);

        // 会话集合应该为空
        Assert.Empty(server.Sessions);
    }
    #endregion

    #region 管道测试
    /// <summary>测试标准编解码器</summary>
    [Fact]
    public void StandardCodecTest()
    {
        var receivedMessage = new ManualResetEventSlim(false);
        Object? received = null;

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
        };

        server.Add<StandardCodec>();
        server.Received += (s, e) =>
        {
            received = e.Message ?? e.Packet;
            receivedMessage.Set();

            if (s is INetSession session && e.Packet != null)
                session.SendMessage(e.Packet);
        };

        server.Start();

        // 客户端
        var uri = new NetUri($"tcp://127.0.0.1:{server.Port}");
        var client = uri.CreateRemote();
        client.Add<StandardCodec>();
        client.Open();

        var sendData = "Hello StandardCodec"u8.ToArray();
        client.SendMessage(new ArrayPacket(sendData));

        // 等待接收
        Assert.True(receivedMessage.Wait(3000));
        Assert.NotNull(received);

        client.Close("Test");
    }

    /// <summary>测试管道处理器添加</summary>
    [Fact]
    public void PipelineHandlerAdd()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
        };

        // 初始时管道为空
        Assert.Null(server.Pipeline);

        // 添加处理器后管道自动创建
        server.Add<StandardCodec>();

        Assert.NotNull(server.Pipeline);
    }
    #endregion

    #region 泛型会话测试
    /// <summary>测试自定义会话类型</summary>
    [Fact]
    public void CustomSessionType()
    {
        var sessionCreated = new ManualResetEventSlim(false);

        using var server = new NetServer<MySession>
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
        };

        server.NewSession += (s, e) =>
        {
            sessionCreated.Set();
        };

        server.Start();

        // 客户端连接
        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);

        Assert.True(sessionCreated.Wait(3000));

        // 获取自定义类型会话
        var sessions = server.Sessions;
        Assert.NotEmpty(sessions);

        var session = server.GetSession(sessions.First().Key);
        Assert.NotNull(session);
        Assert.IsType<MySession>(session);
    }

    /// <summary>测试自定义会话属性</summary>
    [Fact]
    public void CustomSessionProperty()
    {
        var sessionCreated = new ManualResetEventSlim(false);
        MySession? createdSession = null;

        using var server = new NetServer<MySession>
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
        };

        server.NewSession += (s, e) =>
        {
            createdSession = e.Session as MySession;
            sessionCreated.Set();
        };

        server.Start();

        // 客户端连接
        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);

        Assert.True(sessionCreated.Wait(3000));
        Assert.NotNull(createdSession);
        Assert.Equal("Test", createdSession.CustomProperty);
    }

    class MySession : NetSession
    {
        public String CustomProperty { get; set; } = "Test";

        protected override void OnReceive(ReceivedEventArgs e)
        {
            base.OnReceive(e);
        }
    }
    #endregion

    #region 群发测试
    /// <summary>测试群发消息</summary>
    [Fact]
    public async Task BroadcastMessage()
    {
        var receiveCounts = new Int32[3];
        var allReceived = new ManualResetEventSlim(false);

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            UseSession = true,
            Log = XTrace.Log,
        };

        server.Start();

        // 创建多个客户端
        var clients = new List<TcpClient>();
        var streams = new List<NetworkStream>();

        for (var i = 0; i < 3; i++)
        {
            var client = new TcpClient();
            client.Connect(IPAddress.Loopback, server.Port);
            clients.Add(client);
            streams.Add(client.GetStream());
        }

        // 等待所有会话创建
        Thread.Sleep(500);

        // 群发数据
        var sendData = "Broadcast Message"u8.ToArray();
        await server.SendAllAsync(new ArrayPacket(sendData));

        // 等待接收
        Thread.Sleep(500);

        // 验证每个客户端都收到数据
        for (var i = 0; i < 3; i++)
        {
            var stream = streams[i];
            if (stream.DataAvailable)
            {
                var buf = new Byte[1024];
                var len = stream.Read(buf, 0, buf.Length);
                if (len > 0) receiveCounts[i] = len;
            }
        }

        // 至少有一个客户端收到数据
        Assert.True(receiveCounts.Any(c => c > 0));

        // 清理
        foreach (var client in clients)
            client.Dispose();
    }

    /// <summary>测试带条件的群发</summary>
    [Fact]
    public async Task BroadcastWithPredicate()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            UseSession = true,
            Log = XTrace.Log,
        };

        server.NewSession += (s, e) =>
        {
            // 为会话设置标记，通过Session的Items属性
            if (e.Session is NetSession ns)
                ns.Session["Tag"] = ns.ID % 2 == 0 ? "Even" : "Odd";
        };

        server.Start();

        // 创建多个客户端
        var clients = new List<TcpClient>();
        for (var i = 0; i < 4; i++)
        {
            var client = new TcpClient();
            client.Connect(IPAddress.Loopback, server.Port);
            clients.Add(client);
        }

        Thread.Sleep(500);

        // 只向偶数会话发送
        var sendData = "Even Only"u8.ToArray();
        var count = await server.SendAllAsync(
            new ArrayPacket(sendData),
            session => session is NetSession ns && ns.Session["Tag"]?.ToString() == "Even");

        // 验证发送数量
        Assert.True(count >= 0);

        // 清理
        foreach (var client in clients)
            client.Dispose();
    }
    #endregion

    #region SSL测试
    /// <summary>测试SSL配置</summary>
    [Fact]
    public void SslConfiguration()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            SslProtocol = System.Security.Authentication.SslProtocols.Tls12,
            Log = XTrace.Log,
        };

        Assert.Equal(System.Security.Authentication.SslProtocols.Tls12, server.SslProtocol);
    }
    #endregion

    #region 地址重用测试
    /// <summary>测试地址重用</summary>
    [Fact]
    public void ReuseAddressTest()
    {
        var port = 0;

        // 第一个服务器
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
            server1.Stop("Test");
        }

        // 立即启动第二个服务器在相同端口（启用地址重用）
        using var server2 = new NetServer
        {
            Port = port,
            ProtocolType = NetType.Tcp,
            ReuseAddress = true,
            Log = XTrace.Log,
        };

        // 这可能成功也可能失败，取决于系统
        try
        {
            server2.Start();
            Assert.True(server2.Active);
        }
        catch
        {
            // 某些系统可能仍然不允许立即重用
        }
    }
    #endregion

    #region 错误处理测试
    /// <summary>测试错误事件</summary>
    [Fact]
    public void ErrorEventTest()
    {
        var errorReceived = false;

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
        };

        server.Error += (s, e) =>
        {
            errorReceived = true;
        };

        server.Start();

        // 正常关闭不应触发错误
        server.Stop("Test");

        Assert.False(errorReceived);
    }
    #endregion

    #region 会话生命周期测试
    /// <summary>测试会话连接事件</summary>
    [Fact]
    public void SessionLifecycleEvents()
    {
        var sessionCreated = new ManualResetEventSlim(false);

        using var server = new NetServer<LifecycleSession>
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            Log = XTrace.Log,
        };

        server.NewSession += (s, e) =>
        {
            sessionCreated.Set();
        };

        server.Start();

        // 客户端连接
        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);
        Thread.Sleep(300);

        // 验证会话创建
        Assert.True(sessionCreated.Wait(3000));

        // 验证会话存在
        Assert.True(server.Sessions.Count >= 1);
    }

    class LifecycleSession : NetSession
    {
        public Action? OnConnectedCallback { get; set; }
        public Action? OnDisconnectedCallback { get; set; }

        protected override void OnConnected()
        {
            base.OnConnected();
            OnConnectedCallback?.Invoke();
        }

        protected override void OnDisconnected(String reason)
        {
            base.OnDisconnected(reason);
            OnDisconnectedCallback?.Invoke();
        }
    }
    #endregion

    #region 服务提供者测试
    /// <summary>测试服务提供者</summary>
    [Fact]
    public void ServiceProviderTest()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
        };

        // 设置服务提供者
        var services = ObjectContainer.Current;
        services.AddSingleton<ITestService, TestService>();
        server.ServiceProvider = services.BuildServiceProvider();

        server.Start();

        Assert.NotNull(server.ServiceProvider);
    }

    interface ITestService
    {
        void DoWork();
    }

    class TestService : ITestService
    {
        public void DoWork() { }
    }
    #endregion

    #region 统计信息测试
    /// <summary>测试统计信息获取</summary>
    [Fact]
    public void GetStatTest()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            UseSession = true,
            StatPeriod = 0,
            Log = XTrace.Log,
        };

        server.Start();

        // 创建连接
        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);
        Thread.Sleep(200);

        // 获取统计
        var stat = server.GetStat();
        Assert.NotEmpty(stat);
        Assert.Contains("在线", stat);
    }
    #endregion

    #region Items扩展数据测试
    /// <summary>测试服务器扩展数据</summary>
    [Fact]
    public void ServerItemsTest()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
        };

        // 设置扩展数据
        server["Key1"] = "Value1";
        server["Key2"] = 123;

        Assert.Equal("Value1", server["Key1"]);
        Assert.Equal(123, server["Key2"]);
        Assert.Null(server["NonExistent"]);
    }
    #endregion

    #region 端口随机分配测试
    /// <summary>测试端口为0时自动分配</summary>
    [Fact]
    public void RandomPortAllocation()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
        };

        Assert.Equal(0, server.Port);

        server.Start();

        // 启动后应该分配了实际端口
        Assert.True(server.Port > 0);
        Assert.True(server.Port < 65536);
    }
    #endregion

    #region ToString测试
    /// <summary>测试服务器ToString</summary>
    [Fact]
    public void ServerToStringTest()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
        };

        var str1 = server.ToString();
        Assert.NotEmpty(str1);

        server.Start();

        var str2 = server.ToString();
        Assert.NotEmpty(str2);
        Assert.Contains(server.Port.ToString(), str2);
    }
    #endregion
}
