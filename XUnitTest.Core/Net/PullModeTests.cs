using System.Net;
using System.Net.Sockets;
using NewLife;
using NewLife.Data;
using NewLife.Log;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

/// <summary>接收模式二选一（AutoReceive）测试：事件模式禁止拉取、拉取模式直读、手动开环、UdpServer.MaxAsync 并发数、UdpSession 会话级拉取明确拒绝</summary>
[Collection("Net.A")]
public class PullModeTests
{
    #region 事件模式禁止拉取
    /// <summary>默认事件模式（AutoReceive=true）下接收环已运行，同步拉取抛异常</summary>
    [Fact(DisplayName = "拉取_事件模式_同步拉取抛异常")]
    public void EventMode_Sync_Throws()
    {
        using var server = new NetServer { Port = 0, ProtocolType = NetType.Tcp, Log = XTrace.Log };
        server.Start();

        using var client = new NetUri($"tcp://127.0.0.1:{server.Port}").CreateRemote();
        client.Log = XTrace.Log;
        client.Open();

        Assert.True(client is SessionBase { AutoReceive: true });

        var ex = Assert.Throws<InvalidOperationException>(() => client.Receive());
        Assert.Contains("AutoReceive", ex.Message);
    }

    /// <summary>默认事件模式（AutoReceive=true）下接收环已运行，异步拉取抛异常</summary>
    [Fact(DisplayName = "拉取_事件模式_异步拉取抛异常")]
    public async Task EventMode_Async_Throws()
    {
        using var server = new NetServer { Port = 0, ProtocolType = NetType.Tcp, Log = XTrace.Log };
        server.Start();

        using var client = new NetUri($"tcp://127.0.0.1:{server.Port}").CreateRemote();
        client.Log = XTrace.Log;
        client.Open();

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.ReceiveAsync(default));
    }

    /// <summary>UdpServer 默认事件模式（AutoReceive=true）下接收环已运行，拉取抛异常</summary>
    [Fact(DisplayName = "拉取_事件模式_UDP拉取抛异常")]
    public void EventMode_Udp_Throws()
    {
        using var server = new UdpServer { Log = XTrace.Log };
        server.Open();

        Assert.True(server.Active);
        var ex = Assert.Throws<InvalidOperationException>(() => server.Receive());
        Assert.Contains("AutoReceive", ex.Message);
    }
    #endregion

    #region 拉取模式直读
    /// <summary>拉取模式（AutoReceive=false）：短读循环拼包，模拟 Modbus 逐个数据帧拉取的用法</summary>
    [Fact(DisplayName = "拉取_拉取模式_短读循环拼包")]
    public void PullMode_Tcp_ShortReadLoop()
    {
        // 服务端：每个连接先发 64 字节递增数据
        using var server = new TcpServer { Port = 0, Log = XTrace.Log };
        server.NewSession += (s, e) =>
        {
            if (e.Session is TcpSession session)
            {
                var data = new Byte[64];
                for (var i = 0; i < data.Length; i++) data[i] = (Byte)(0xA0 + i);
                session.Send(data);
            }
        };
        server.Start();

        using var client = new TcpSession
        {
            Remote = new NetUri($"tcp://127.0.0.1:{server.Port}"),
            AutoReceive = false,
            BufferSize = 16,    // 调小接收缓冲，载荷大于单轮容量，强制多轮短读
            Timeout = 5_000,
            Log = XTrace.Log,
        };
        client.Open();

        // 循环拉取直到收满 64 字节
        var total = new List<Byte>();
        var rounds = 0;
        while (total.Count < 64)
        {
            using var pk = client.Receive();
            Assert.NotNull(pk);
            rounds++;
            Assert.True(pk.Length > 0 && pk.Length <= 16, $"单轮长度应在 1~16 之间，实际 {pk.Length}");
            total.AddRange(pk.ToArray());
        }

        Assert.True(rounds >= 2, "应出现多轮短读");
        Assert.Equal(Enumerable.Range(0xA0, 64).Select(i => (Byte)i).ToArray(), total.ToArray());
    }

    /// <summary>拉取模式（AutoReceive=false）：读失败原样抛出异常，缓冲由归还路径处理而非吞掉异常</summary>
    [Fact(DisplayName = "拉取_拉取模式_读超时异常原样抛出")]
    public void PullMode_ReadFailure_Throws()
    {
        // 服务端：接受连接但不发数据，强制客户端拉到超时
        using var server = new TcpServer { Port = 0, Log = XTrace.Log };
        server.Start();

        using var client = new TcpSession
        {
            Remote = new NetUri($"tcp://127.0.0.1:{server.Port}"),
            AutoReceive = false,
            // 建连与读取共用同一超时：200ms 预算在满载并行跑测下连建连都会被取消（实测 ConnectAsync 取消），
            // 故建连用宽松预算，建连成功后再把套接字读超时收紧到 200ms
            Timeout = 10_000,
            Log = XTrace.Log,
        };
        client.Open();

        client.Client!.ReceiveTimeout = 200;

        // 失败路径归还池化缓冲，但异常必须原样上抛（归还不得替换或吞掉原始异常）
        Assert.ThrowsAny<SocketException>(() => client.Receive());
    }

    /// <summary>拉取模式（AutoReceive=false）：UDP 先发后收</summary>
    [Fact(DisplayName = "拉取_拉取模式_UDP先发后收")]
    public async Task PullMode_Udp_SendThenReceive()
    {
        // 服务端：收到数据后回发 echo:xxx
        using var server = new UdpServer { Log = XTrace.Log };
        server.Received += (s, e) =>
        {
            if (s is UdpSession session)
            {
                var text = e.GetBytes().ToStr();
                session.Send($"echo:{text}");
            }
        };
        server.Open();

        // 客户端：拉取模式
        using var client = new UdpServer { AutoReceive = false, Log = XTrace.Log };
        client.Remote = new NetUri($"udp://127.0.0.1:{server.Port}");
        client.Open();

        client.Send("hello");

        using var pk = await client.ReceiveAsync(default).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.NotNull(pk);
        Assert.Equal("echo:hello", pk!.ToStr());
    }

    /// <summary>拉取模式：UDP 服务器级直读连续数据报，一次拉取恰为一个完整数据报，大小与顺序完整</summary>
    [Fact(DisplayName = "拉取_UdpServer_连续多数据报顺序完整")]
    public void UdpServerPull_MultipleDatagrams()
    {
        // 直读走 Socket 接收超时（Timeout 默认 3 秒），满载下数据报到达可能被调度滞后，放宽等待窗口
        using var server = new UdpServer { AutoReceive = false, Timeout = 15_000, Log = XTrace.Log };
        server.Open();
        var serverEp = new IPEndPoint(IPAddress.Loopback, server.Port);

        using var sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        sock.Bind(new IPEndPoint(IPAddress.Loopback, 0));

        // 发送 5 个大小不一的数据报，包间小间隔确保到达顺序
        var expected = new List<Byte[]>();
        for (var i = 0; i < 5; i++)
        {
            var data = new Byte[96 + i * 53];
            for (var j = 0; j < data.Length; j++) data[j] = (Byte)(i * 31 + j);
            expected.Add(data);
            sock.SendTo(data, serverEp);
            Thread.Sleep(20);
        }

        // 逐个拉取：每次 Receive 恰为一个完整数据报，不粘不拆
        for (var i = 0; i < 5; i++)
        {
            using var pk = server.Receive();
            Assert.NotNull(pk);
            Assert.Equal(expected[i].Length, pk!.Length);
            Assert.Equal(expected[i], pk.ToArray());
        }
    }

    /// <summary>拉取模式可手动启动接收环，此后拉取被禁止（单向切换）</summary>
    [Fact(DisplayName = "拉取_手动开环后_拉取被禁止")]
    public void PullMode_ManualRingStart_ThenPullThrows()
    {
        using var server = new NetServer { Port = 0, ProtocolType = NetType.Tcp, Log = XTrace.Log };
        server.Start();

        using var client = new TcpSession
        {
            Remote = new NetUri($"tcp://127.0.0.1:{server.Port}"),
            AutoReceive = false,
            Log = XTrace.Log,
        };
        client.Open();

        // 拉取模式下可自行启动接收环（转事件模式）
        Assert.True(client.StartReceive());

        // 开环后拉取被禁止
        Assert.Throws<InvalidOperationException>(() => client.Receive());
    }
    #endregion

    #region NetClient 与 UdpServer.MaxAsync
    /// <summary>NetClient 透传 AutoReceive：拉取模式下 Receive/ReceiveAsync 可用</summary>
    [Fact(DisplayName = "拉取_NetClient透传_拉取可用")]
    public async Task NetClient_AutoReceiveFalse_Pull()
    {
        using var server = new NetServer { Port = 0, ProtocolType = NetType.Tcp, Log = XTrace.Log };
        server.Received += (s, e) =>
        {
            if (s is INetSession session && e.Packet != null) session.Send(e.Packet);
        };
        server.Start();

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}")
        {
            AutoReconnect = false,
            AutoReceive = false,
            Log = XTrace.Log,
        };

        Assert.True(client.Open());
        client.Send("pull-hello");

        using var pk = await client.ReceiveAsync(default).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.NotNull(pk);
        Assert.Equal("pull-hello", pk!.ToStr());

        client.Close("done");
    }

    /// <summary>UdpServer.MaxAsync 为并行接收数：默认处理器核心数×1.6（下限4），小于等于0归一为1</summary>
    [Fact(DisplayName = "拉取_UdpServer_MaxAsync读写与归一")]
    public void UdpServerMaxAsync_Normalize()
    {
        using var udp = new UdpServer();

        Assert.True(udp.AutoReceive);
        Assert.Equal(Math.Max(Environment.ProcessorCount * 16 / 10, 4), udp.MaxAsync);

        udp.MaxAsync = 10;
        Assert.Equal(10, udp.MaxAsync);

        udp.MaxAsync = 0;
        Assert.Equal(1, udp.MaxAsync);

        udp.MaxAsync = -3;
        Assert.Equal(1, udp.MaxAsync);
    }
    #endregion

    #region UdpSession 会话拉取收口（共享 Socket 不支持会话级拉取）
    /// <summary>UDP 会话共享服务器 Socket：会话级拉取一律明确拒绝，并指引替代入口</summary>
    [Fact(DisplayName = "拉取_UdpSession会话_明确拒绝并指引替代入口")]
    public async Task UdpSessionPull_NotSupported()
    {
        using var server = new UdpServer { AutoReceive = false, Log = XTrace.Log };
        server.Open();

        var remote = new IPEndPoint(IPAddress.Loopback, 12345);
        var session = Assert.IsType<UdpSession>(server.CreateSession(null, remote));

        // 拉取模式下同样拒绝：按会话过滤拉取会丢弃其他会话的数据报，且与服务器级拉取争抢同一 Socket
        var ex = Assert.Throws<NotSupportedException>(() => session.Receive());
        Assert.Contains("UdpServer", ex.Message);
        Assert.Contains("NetClient", ex.Message);

        var ex2 = await Assert.ThrowsAsync<NotSupportedException>(() => session.ReceiveAsync(default));
        Assert.Contains("Received", ex2.Message);
    }

    /// <summary>默认事件模式（服务器接收环运行）下，会话拉取同样明确拒绝</summary>
    [Fact(DisplayName = "拉取_UdpSession会话_接收环运行同样拒绝")]
    public void UdpSessionPull_NotSupportedWhenServerReceiving()
    {
        using var server = new UdpServer { Log = XTrace.Log };
        server.Open();
        var serverEp = new IPEndPoint(IPAddress.Loopback, server.Port);

        using var sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        sock.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        sock.SendTo("hello".GetBytes(), serverEp);

        // 等待服务器接收环创建会话
        UdpSession? session = null;
        for (var i = 0; i < 50 && session == null; i++)
        {
            Thread.Sleep(20);
            session = server.Sessions.Values.OfType<UdpSession>().FirstOrDefault();
        }
        Assert.NotNull(session);

        Assert.Throws<NotSupportedException>(() => session!.Receive());
    }

    #endregion

    #region 服务端会话强制事件模式
    /// <summary>服务端会话必须事件模式：子类在 CreateSession 里设 AutoReceive=false，启动时被收敛为 true</summary>
    [Fact(DisplayName = "拉取_服务端会话_强制事件模式")]
    public async Task ServerSession_ForceEventMode()
    {
        // 服务端故意把会话设为拉取模式：服务端会话不支持拉取，启动时必须回到事件模式
        using var server = new PullModeServer { Port = 0, Log = XTrace.Log };
        server.NewSession += (s, e) =>
        {
            if (e.Session is TcpSession session)
            {
                session.Received += (ss, ee) =>
                {
                    var pk = ee.Packet;
                    if (pk != null && pk.Length > 0) session.Send(pk);
                };
            }
        };
        server.Start();

        using var client = new TcpSession
        {
            Remote = new NetUri($"tcp://127.0.0.1:{server.Port}"),
            Log = XTrace.Log,
        };
        client.Open();

        // 回显成功说明服务端会话的接收环已启动
        var received = new TaskCompletionSource<String>();
        client.Received += (s, e) =>
        {
            var pk = e.Packet;
            if (pk != null && pk.Length > 0) received.TrySetResult(pk.ToStr());
        };
        client.Send("pull-server");

        var text = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("pull-server", text);

        // 服务端会话已收敛为事件模式：AutoReceive 为 true，且拉取被拒绝
        var session = Assert.IsType<TcpSession>(Assert.Single(server.Sessions.Values));
        Assert.True(session.AutoReceive, "服务端会话必须为事件模式");
        var ex = Assert.Throws<InvalidOperationException>(() => session.Receive());
        Assert.Contains("AutoReceive", ex.Message);
    }

    /// <summary>服务端会话：在 CreateSession 里关闭自动接收，用于验证服务端不支持拉取模式</summary>
    private sealed class PullModeServer : TcpServer
    {
        protected override TcpSession CreateSession(Socket client)
        {
            var session = base.CreateSession(client);
            session.AutoReceive = false;
            return session;
        }
    }
    #endregion
}
