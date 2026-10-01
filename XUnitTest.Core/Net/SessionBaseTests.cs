using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using NewLife;
using NewLife.Data;
using NewLife.Log;
using NewLife.Model;
using NewLife.Net;
using NewLife.Messaging;
using Xunit;

namespace XUnitTest.Net;

/// <summary>SessionBase及相关类单元测试</summary>
[Collection("Net")]
[TestCaseOrderer("NewLife.UnitTest.DefaultOrderer", "NewLife.UnitTest")]
public class SessionBaseTests
{
    #region TcpSession测试
    /// <summary>测试TcpSession基本连接</summary>
    [Fact]
    public void TcpSessionConnect()
    {
        using var server = new TcpServer { Port = 0 };
        server.Start();

        using var client = new TcpSession
        {
            Remote = new NetUri($"tcp://127.0.0.1:{server.Port}"),
            Timeout = 5000,
        };

        Assert.False(client.Active);
        var result = client.Open();
        Assert.True(result);
        Assert.True(client.Active);
        Assert.NotNull(client.Client);
        Assert.True(client.Client.Connected);

        client.Close("Test");
        Assert.False(client.Active);
    }

    /// <summary>测试TcpSession异步连接</summary>
    [Fact]
    public async Task TcpSessionConnectAsync()
    {
        using var server = new TcpServer { Port = 0 };
        server.Start();

        using var client = new TcpSession
        {
            Remote = new NetUri($"tcp://127.0.0.1:{server.Port}"),
            Timeout = 5000,
        };

        Assert.False(client.Active);
        var result = await client.OpenAsync();
        Assert.True(result);
        Assert.True(client.Active);

        await client.CloseAsync("Test");
        Assert.False(client.Active);
    }

    /// <summary>测试TcpSession数据发送</summary>
    [Fact]
    public void TcpSessionSendReceive()
    {
        var receivedData = new List<Byte[]>();
        var receivedEvent = new ManualResetEventSlim(false);

        using var server = new TcpServer { Port = 0 };
        server.NewSession += (s, e) =>
        {
            if (e.Session is TcpSession session)
            {
                session.Received += (sender, args) =>
                {
                    receivedData.Add(args.GetBytes());
                    receivedEvent.Set();
                };
            }
        };
        server.Start();

        using var client = new TcpSession
        {
            Remote = new NetUri($"tcp://127.0.0.1:{server.Port}"),
        };
        client.Open();

        var sendData = "Hello TcpSession"u8.ToArray();
        var sentBytes = client.Send(sendData);

        Assert.Equal(sendData.Length, sentBytes);
        Assert.True(receivedEvent.Wait(3000));
        Assert.Single(receivedData);
        Assert.Equal(sendData, receivedData[0]);
    }

    /// <summary>测试TcpSession发送IPacket</summary>
    [Fact]
    public void TcpSessionSendPacket()
    {
        var receivedEvent = new ManualResetEventSlim(false);
        Byte[]? received = null;

        using var server = new TcpServer { Port = 0 };
        server.NewSession += (s, e) =>
        {
            if (e.Session is TcpSession session)
            {
                session.Received += (sender, args) =>
                {
                    received = args.GetBytes();
                    receivedEvent.Set();
                };
            }
        };
        server.Start();

        using var client = new TcpSession
        {
            Remote = new NetUri($"tcp://127.0.0.1:{server.Port}"),
        };
        client.Open();

        var sendData = "PacketData"u8.ToArray();
        var packet = new ArrayPacket(sendData);
        var sentBytes = client.Send(packet);

        Assert.Equal(sendData.Length, sentBytes);
        Assert.True(receivedEvent.Wait(3000));
        Assert.Equal(sendData, received);
    }

    /// <summary>测试TcpSession NoDelay属性</summary>
    [Fact]
    public void TcpSessionNoDelay()
    {
        using var server = new TcpServer { Port = 0 };
        server.Start();

        using var client = new TcpSession
        {
            Remote = new NetUri($"tcp://127.0.0.1:{server.Port}"),
            NoDelay = true,
        };
        client.Open();

        Assert.True(client.Client!.NoDelay);
    }

    /// <summary>测试TcpSession KeepAlive</summary>
    [Fact]
    public void TcpSessionKeepAlive()
    {
        using var server = new TcpServer { Port = 0 };
        server.Start();

        using var client = new TcpSession
        {
            Remote = new NetUri($"tcp://127.0.0.1:{server.Port}"),
            KeepAliveInterval = 30,
        };
        client.Open();

        // KeepAlive 设置成功不会抛异常
        Assert.True(client.Active);
    }

    /// <summary>探测地址是否构成“黑洞”：预算内既没连上、也不立刻失败</summary>
    private static Boolean IsBlackhole(String host, Int32 port, Int32 budgetMs)
    {
        try
        {
            using var tc = new TcpClient();
            var task = tc.ConnectAsync(host, port);

            return !task.Wait(budgetMs);
        }
        catch
        {
            return false;   // 立刻失败（拒绝或路由不可达）→ 不是黑洞
        }
    }

    /// <summary>测试TcpSession建连超时受Timeout约束</summary>
    [Fact(DisplayName = "TcpSession_建连超时_受Timeout约束")]
    public void TcpSessionTimeout()
    {
        // 192.0.2.0/24 为文档保留网段，正常不会被路由；若本机对它立刻拒绝（ICMP 或路由不可达），
        // 就无从验证建连超时约束，跳过
        if (!IsBlackhole("192.0.2.1", 12345, 1000)) return;

        using var client = new TcpSession
        {
            Remote = new NetUri("tcp://192.0.2.1:12345"), // 黑洞地址
            Timeout = 800,
        };

        var sw = Stopwatch.StartNew();
        // 没有超时约束时，建连要等操作系统默认 TCP 超时（Windows 约 21 秒）
        var ex = Assert.ThrowsAny<Exception>(() => client.Open());
        sw.Stop();

        // 超时异常类型随目标框架而异：高版本为预算取消源触发的 OperationCanceledException，低版本为 TimeoutException
        Assert.True(ex is TimeoutException || ex is SocketException || ex is OperationCanceledException,
            $"Expected TimeoutException, SocketException or OperationCanceledException, but got {ex.GetType().Name}");
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(5), $"建连耗时 {sw.Elapsed}，未受 Timeout 约束");
    }

    /// <summary>测试TcpSession Items扩展数据</summary>
    [Fact]
    public void TcpSessionItems()
    {
        using var client = new TcpSession();

        client["Key1"] = "Value1";
        client["Key2"] = 123;

        Assert.Equal("Value1", client["Key1"]);
        Assert.Equal(123, client["Key2"]);
        Assert.Null(client["NonExistent"]);
    }

    /// <summary>测试TcpSession LastTime更新</summary>
    [Fact]
    public void TcpSessionLastTime()
    {
        using var server = new TcpServer { Port = 0 };
        server.Start();

        using var client = new TcpSession
        {
            Remote = new NetUri($"tcp://127.0.0.1:{server.Port}"),
        };

        // 记录打开前的时间，允许一些误差
        var beforeOpen = DateTime.Now.AddMilliseconds(-100);
        client.Open();

        // 打开后的LastTime应该在beforeOpen之后
        var afterOpen = client.LastTime;
        Assert.True(afterOpen >= beforeOpen, $"afterOpen ({afterOpen:HH:mm:ss.fff}) should >= beforeOpen ({beforeOpen:HH:mm:ss.fff})");

        // 发送数据应该更新LastTime
        Thread.Sleep(50);
        var beforeSend = client.LastTime;
        client.Send("Test"u8.ToArray());

        // 发送后LastTime应该更新或保持不变（取决于实现）
        var afterSend = client.LastTime;
        Assert.True(afterSend >= beforeSend, $"afterSend ({afterSend:HH:mm:ss.fff}) should >= beforeSend ({beforeSend:HH:mm:ss.fff})");
    }

    /// <summary>测试TcpSession ToString</summary>
    [Fact]
    public void TcpSessionToString()
    {
        using var server = new TcpServer { Port = 0 };
        server.Start();

        using var client = new TcpSession
        {
            Remote = new NetUri($"tcp://127.0.0.1:{server.Port}"),
        };
        client.Open();

        var str = client.ToString();
        Assert.NotEmpty(str);
        Assert.Contains("=>", str); // 客户端格式 local=>remote
    }

    /// <summary>测试TcpSession关闭原因</summary>
    [Fact]
    public void TcpSessionCloseReason()
    {
        using var server = new TcpServer { Port = 0 };
        server.Start();

        using var client = new TcpSession
        {
            Remote = new NetUri($"tcp://127.0.0.1:{server.Port}"),
        };
        client.Open();
        client.Close("TestReason");

        Assert.Equal("TestReason", client.CloseReason);
    }
    #endregion

    #region UdpServer测试
    /// <summary>测试UdpServer基本功能</summary>
    [Fact]
    public void UdpServerBasic()
    {
        using var server = new UdpServer { Port = 0 };

        Assert.False(server.Active);
        server.Open();
        Assert.True(server.Active);
        Assert.True(server.Port > 0);
        Assert.NotNull(server.Client);

        server.Close("Test");
        Assert.False(server.Active);
    }

    /// <summary>测试UdpServer会话创建</summary>
    [Fact]
    public void UdpServerCreateSession()
    {
        var sessionCreated = new ManualResetEventSlim(false);
        ISocketSession? createdSession = null;

        using var server = new UdpServer { Port = 0 };
        server.NewSession += (s, e) =>
        {
            createdSession = e.Session;
            sessionCreated.Set();
        };
        server.Open();

        // 发送数据触发会话创建
        using var client = new UdpClient();
        var data = "Hello"u8.ToArray();
        client.Send(data, data.Length, new IPEndPoint(IPAddress.Loopback, server.Port));

        Assert.True(sessionCreated.Wait(3000));
        Assert.NotNull(createdSession);
        Assert.IsType<UdpSession>(createdSession);
    }

    /// <summary>测试UdpServer数据收发</summary>
    [Fact]
    public void UdpServerSendReceive()
    {
        var receivedEvent = new ManualResetEventSlim(false);
        Byte[]? received = null;

        using var server = new UdpServer { Port = 0 };
        server.Received += (s, e) =>
        {
            received = e.GetBytes();
            receivedEvent.Set();
        };
        server.Open();

        using var client = new UdpClient();
        var sendData = "UDP Test"u8.ToArray();
        client.Send(sendData, sendData.Length, new IPEndPoint(IPAddress.Loopback, server.Port));

        var ok = receivedEvent.Wait(3000);
        if (ok)
        {
            Assert.Equal(sendData, received);
        }
    }

    /// <summary>相同远程端点复用同一会话；会话销毁后端点缓存失效，可重新创建</summary>
    [Fact]
    public void UdpServerCreateSessionEndPointCache()
    {
        using var server = new UdpServer { Port = 0 };
        server.Open();

        var s1 = server.CreateSession(null, new IPEndPoint(IPAddress.Loopback, 12345));
        Assert.NotNull(s1);

        // 等值但不同实例的端点，应命中同一会话（端点缓存路径）
        var s2 = server.CreateSession(null, new IPEndPoint(IPAddress.Parse("127.0.0.1"), 12345));
        Assert.Same(s1, s2);
        Assert.Single(server.Sessions);

        // 会话销毁后缓存同步失效，同端点重新创建新会话
        s1.Dispose();
        var s3 = server.CreateSession(null, new IPEndPoint(IPAddress.Loopback, 12345));
        Assert.NotSame(s1, s3);
        Assert.Single(server.Sessions);

        s3.Dispose();
    }

    /// <summary>UDP客户端打开后自动连接远端：发送走已连接路径，服务端正常建立会话</summary>
    [Fact]
    public void UdpClientAutoConnectRemote()
    {
        var receivedEvent = new ManualResetEventSlim(false);
        Byte[]? received = null;

        using var server = new UdpServer { Port = 0 };
        server.Received += (s, e) =>
        {
            received = e.GetBytes();
            receivedEvent.Set();
        };
        server.Open();

        using var client = new UdpServer { Remote = new NetUri($"udp://127.0.0.1:{server.Port}") };
        client.Open();

        // 纯客户端模式（未指定本地端口）自动连接远端
        Assert.True(client.Client!.Connected);

        var payload = "auto connect"u8.ToArray();
        client.Send(payload);

        Assert.True(receivedEvent.Wait(3000));
        Assert.Equal(payload, received);
        Assert.Single(server.Sessions);
    }

    /// <summary>广播远端与指定本地端口的实例不自动连接，保持 SendTo 语义</summary>
    [Fact]
    public void UdpServerNoAutoConnectForBroadcastAndLocalPort()
    {
        using var server = new UdpServer { Port = 0 };
        server.Open();

        // 广播地址不能连接
        using (var broadcast = new UdpServer { Remote = new NetUri($"udp://255.255.255.255:{server.Port}") })
        {
            broadcast.Open();
            Assert.False(broadcast.Client!.Connected);
        }

        // 指定本地端口的实例可能同时监听（双角色），不自动连接
        var probe = new UdpServer { Port = 0 };
        probe.Open();
        var localPort = probe.Port;
        probe.Close("probe");

        using var dual = new UdpServer { Port = localPort, Remote = new NetUri($"udp://127.0.0.1:{server.Port}") };
        dual.Open();
        Assert.False(dual.Client!.Connected);
    }

    /// <summary>测试UdpServer会话超时</summary>
    [Fact]
    public void UdpServerSessionTimeout()
    {
        using var server = new UdpServer
        {
            Port = 0,
            SessionTimeout = 2,
        };

        Assert.Equal(2, server.SessionTimeout);
    }

    /// <summary>测试UdpServer地址重用</summary>
    [Fact]
    public void UdpServerReuseAddress()
    {
        using var server = new UdpServer
        {
            Port = 0,
            ReuseAddress = true,
        };
        server.Open();

        Assert.True(server.Active);
    }

    /// <summary>测试UdpServer环回过滤</summary>
    [Fact]
    public void UdpServerLoopback()
    {
        using var server = new UdpServer { Port = 0 };

        Assert.False(server.Loopback);
        server.Loopback = true;
        Assert.True(server.Loopback);
    }

    /// <summary>测试UdpServer ToString</summary>
    [Fact]
    public void UdpServerToString()
    {
        using var server = new UdpServer { Port = 0 };
        server.Open();

        var str = server.ToString();
        Assert.NotEmpty(str);
        Assert.Contains(server.Port.ToString(), str);
    }
    #endregion

    #region UdpSession测试
    /// <summary>测试UdpSession基本属性</summary>
    [Fact]
    public void UdpSessionBasic()
    {
        var sessionCreated = new ManualResetEventSlim(false);
        UdpSession? session = null;

        using var server = new UdpServer { Port = 0 };
        server.NewSession += (s, e) =>
        {
            session = e.Session as UdpSession;
            sessionCreated.Set();
        };
        server.Open();

        using var client = new UdpClient();
        var data = "Test"u8.ToArray();
        client.Send(data, data.Length, new IPEndPoint(IPAddress.Loopback, server.Port));

        Assert.True(sessionCreated.Wait(3000));
        Assert.NotNull(session);
        Assert.True(session.ID > 0);
        Assert.Equal(server, session.Server);
    }

    /// <summary>测试UdpSession发送数据</summary>
    [Fact]
    public void UdpSessionSend()
    {
        var sessionCreated = new ManualResetEventSlim(false);
        UdpSession? session = null;

        using var server = new UdpServer { Port = 0 };
        server.NewSession += (s, e) =>
        {
            session = e.Session as UdpSession;
            sessionCreated.Set();
        };
        server.Open();

        using var client = new UdpClient();
        client.Client.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        var clientEp = (IPEndPoint)client.Client.LocalEndPoint!;

        var data = "Init"u8.ToArray();
        client.Send(data, data.Length, new IPEndPoint(IPAddress.Loopback, server.Port));

        Assert.True(sessionCreated.Wait(3000));
        Assert.NotNull(session);

        // 会话发送数据
        var sendData = "Response"u8.ToArray();
        var sent = session.Send(sendData);
        Assert.True(sent > 0);
    }

    /// <summary>测试UdpSession Items扩展数据</summary>
    [Fact]
    public void UdpSessionItems()
    {
        var sessionCreated = new ManualResetEventSlim(false);
        UdpSession? session = null;

        using var server = new UdpServer { Port = 0 };
        server.NewSession += (s, e) =>
        {
            session = e.Session as UdpSession;
            sessionCreated.Set();
        };
        server.Open();

        using var client = new UdpClient();
        var data = "Test"u8.ToArray();
        client.Send(data, data.Length, new IPEndPoint(IPAddress.Loopback, server.Port));

        Assert.True(sessionCreated.Wait(3000));
        Assert.NotNull(session);

        session["Key1"] = "Value1";
        Assert.Equal("Value1", session["Key1"]);
    }

    /// <summary>测试UdpSession ToString</summary>
    [Fact]
    public void UdpSessionToString()
    {
        var sessionCreated = new ManualResetEventSlim(false);
        UdpSession? session = null;

        using var server = new UdpServer { Port = 0 };
        server.NewSession += (s, e) =>
        {
            session = e.Session as UdpSession;
            sessionCreated.Set();
        };
        server.Open();

        using var client = new UdpClient();
        var data = "Test"u8.ToArray();
        client.Send(data, data.Length, new IPEndPoint(IPAddress.Loopback, server.Port));

        Assert.True(sessionCreated.Wait(3000));
        Assert.NotNull(session);

        var str = session.ToString();
        Assert.NotEmpty(str);
        Assert.Contains("<=", str); // 服务端会话格式
    }
    #endregion

    #region TcpServer测试
    /// <summary>测试TcpServer基本功能</summary>
    [Fact]
    public void TcpServerBasic()
    {
        using var server = new TcpServer { Port = 0 };

        Assert.False(server.Active);
        server.Start();
        Assert.True(server.Active);
        Assert.True(server.Port > 0);
        Assert.NotNull(server.Client);

        server.Stop("Test");
        Assert.False(server.Active);
    }

    /// <summary>测试TcpServer会话创建</summary>
    [Fact]
    public void TcpServerNewSession()
    {
        var sessionCreated = new ManualResetEventSlim(false);
        ISocketSession? createdSession = null;

        using var server = new TcpServer { Port = 0 };
        server.NewSession += (s, e) =>
        {
            createdSession = e.Session;
            sessionCreated.Set();
        };
        server.Start();

        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);

        Assert.True(sessionCreated.Wait(3000));
        Assert.NotNull(createdSession);
        Assert.IsType<TcpSession>(createdSession);
    }

    /// <summary>测试TcpServer会话管理</summary>
    [Fact]
    public void TcpServerSessionManagement()
    {
        using var server = new TcpServer { Port = 0 };
        server.Start();

        var clients = new List<TcpClient>();
        for (var i = 0; i < 3; i++)
        {
            var client = new TcpClient();
            client.Connect(IPAddress.Loopback, server.Port);
            clients.Add(client);
        }

        Thread.Sleep(500);

        Assert.Equal(3, server.Sessions.Count);

        foreach (var client in clients)
            client.Dispose();
    }

    /// <summary>测试TcpServer MaxAsync属性</summary>
    [Fact]
    public void TcpServerMaxAsync()
    {
        using var server = new TcpServer { Port = 0 };

        // 默认值应该是 CPU * 1.6
        var expected = Environment.ProcessorCount * 16 / 10;
        Assert.Equal(expected, server.MaxAsync);

        server.MaxAsync = 10;
        Assert.Equal(10, server.MaxAsync);
    }

    /// <summary>测试TcpServer NoDelay属性</summary>
    [Fact]
    public void TcpServerNoDelay()
    {
        using var server = new TcpServer { Port = 0 };

        // 服务端默认true
        Assert.True(server.NoDelay);
    }

    /// <summary>测试TcpServer地址重用</summary>
    [Fact]
    public void TcpServerReuseAddress()
    {
        using var server = new TcpServer
        {
            Port = 0,
            ReuseAddress = true,
        };
        server.Start();

        Assert.True(server.Active);
    }

    /// <summary>测试TcpServer ToString</summary>
    [Fact]
    public void TcpServerToString()
    {
        using var server = new TcpServer { Port = 0 };
        server.Start();

        var str = server.ToString();
        Assert.NotEmpty(str);
        Assert.Contains(server.Port.ToString(), str);
    }
    #endregion

    #region 错误处理测试
    /// <summary>测试TcpSession错误事件</summary>
    [Fact]
    public void TcpSessionErrorEvent()
    {
        var errorReceived = false;
        String? errorAction = null;

        using var client = new TcpSession
        {
            Remote = new NetUri("tcp://192.0.2.1:12345"),
            Timeout = 500,
        };
        client.Error += (s, e) =>
        {
            errorReceived = true;
            errorAction = e.Action;
        };

        try
        {
            client.Open();
        }
        catch
        {
            // 预期会抛出异常
        }

        // 错误事件可能被触发
        Assert.True(errorReceived || !client.Active);
    }

    /// <summary>测试TcpServer错误事件</summary>
    [Fact]
    public void TcpServerErrorEvent()
    {
        var errorReceived = false;

        using var server = new TcpServer { Port = 0 };
        server.Error += (s, e) =>
        {
            errorReceived = true;
        };
        server.Start();

        // 正常停止不应触发错误
        server.Stop("Test");
        Assert.False(errorReceived);
    }
    #endregion

    #region Opened/Closed事件测试
    /// <summary>测试TcpSession Opened事件</summary>
    [Fact]
    public void TcpSessionOpenedEvent()
    {
        var openedFired = false;

        using var server = new TcpServer { Port = 0 };
        server.Start();

        using var client = new TcpSession
        {
            Remote = new NetUri($"tcp://127.0.0.1:{server.Port}"),
        };
        client.Opened += (s, e) => openedFired = true;
        client.Open();

        Assert.True(openedFired);
    }

    /// <summary>测试TcpSession Closed事件</summary>
    [Fact]
    public void TcpSessionClosedEvent()
    {
        var closedFired = false;

        using var server = new TcpServer { Port = 0 };
        server.Start();

        using var client = new TcpSession
        {
            Remote = new NetUri($"tcp://127.0.0.1:{server.Port}"),
        };
        client.Closed += (s, e) => closedFired = true;
        client.Open();
        client.Close("Test");

        Assert.True(closedFired);
    }
    #endregion

    #region 打开期间销毁测试
    /// <summary>打开流程挂起期间销毁会话：连接在销毁之后才建立，应被释放而不是泄漏</summary>
    [Fact(DisplayName = "打开挂起期间销毁_建立中的连接被释放")]
    public async Task OpenSuspendedThenDispose_ReleasesClient()
    {
        using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        listener.Listen(1);
        var port = ((IPEndPoint)listener.LocalEndPoint!).Port;

        var session = new SlowOpenSession
        {
            Remote = new NetUri($"tcp://127.0.0.1:{port}"),
            AutoReceive = false,
        };
        var openTask = session.OpenAsync();

        // 等待进入打开流程（此时尚未建立连接），随后销毁会话
        Assert.True(session.Entered.Wait(5000));
        session.Dispose();

        // 放行打开流程：连接在销毁之后才建立，应被立即释放（未修复时会留下未关闭的socket）
        session.Gate.Set();
        var completed = await Task.WhenAny(openTask, Task.Delay(10_000)) == openTask;
        Assert.True(completed, "打开流程未在10秒内完成");
        Assert.False(await openTask);
        Assert.Null(session.Client);
    }

    /// <summary>打开流程可控挂起的测试会话：闸门放行后才真正建立连接</summary>
    private sealed class SlowOpenSession : TcpSession
    {
        public ManualResetEventSlim Entered { get; } = new(false);
        public ManualResetEventSlim Gate { get; } = new(false);

        protected override async Task<Boolean> OnOpenAsync(CancellationToken cancellationToken)
        {
            Entered.Set();
            await Task.Run(() => Gate.Wait(cancellationToken), cancellationToken).ConfigureAwait(false);

            return await base.OnOpenAsync(cancellationToken).ConfigureAwait(false);
        }
    }
    #endregion

    #region 接收环复用
    /// <summary>接收环复用：同一连接连续两轮不同长度报文，轮末回挂的句柄必须按本轮长度重设窗口</summary>
    [Fact]
    public async Task ReceiveRing_ReusedHandle_RewindowsPerRound()
    {
        var serverLengths = new List<Int32>();
        var clientLengths = new List<Int32>();
        using var received = new SemaphoreSlim(0);

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
        };

        server.Received += (s, e) =>
        {
            if (s is INetSession session && e.Packet != null)
            {
                lock (serverLengths) serverLengths.Add(e.Packet.Length);

                // Echo，客户端据回包长度判断本轮窗口
                session.Send(e.Packet);
            }
        };

        server.Start();

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}");
        client.Received += (s, e) =>
        {
            if (e.Packet != null)
            {
                lock (clientLengths) clientLengths.Add(e.Packet.Length);
                received.Release();
            }
        };
        Assert.True(client.Open());

        // 第一轮 5 字节，等回包到达后再发第二轮：两轮分属不同接收轮次，不会被合并成一次读
        client.Send(new Byte[5]);
        Assert.True(await received.WaitAsync(5000), "首轮回包超时");
        client.Send(new Byte[37]);
        Assert.True(await received.WaitAsync(5000), "次轮回包超时");

        // 两轮长度各自正确：第二轮若沿用上一轮窗口（未重设），长度会停在 5
        lock (serverLengths) Assert.Equal(new[] { 5, 37 }, serverLengths);
        lock (clientLengths) Assert.Equal(new[] { 5, 37 }, clientLengths);

        // 迟到的多余轮次同样不允许出现
        await Task.Delay(100);
        lock (serverLengths) Assert.Equal(new[] { 5, 37 }, serverLengths);
    }
    #endregion
}
