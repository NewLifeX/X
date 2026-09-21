using System.Net;
using System.Net.Sockets;
using NewLife;
using NewLife.Data;
using NewLife.Log;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

/// <summary>接收模式二选一（AutoReceive）测试：事件模式禁止拉取、拉取模式直读、手动开环、UdpServer.MaxAsync 并发数</summary>
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
        using var server = new UdpServer { AutoReceive = false, Log = XTrace.Log };
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

    /// <summary>UdpServer.MaxAsync 为并行接收数：默认CPU*1.6，小于等于0归一为1</summary>
    [Fact(DisplayName = "拉取_UdpServer_MaxAsync读写与归一")]
    public void UdpServerMaxAsync_Normalize()
    {
        using var udp = new UdpServer();

        Assert.True(udp.AutoReceive);
        Assert.Equal(Environment.ProcessorCount * 16 / 10, udp.MaxAsync);

        udp.MaxAsync = 10;
        Assert.Equal(10, udp.MaxAsync);

        udp.MaxAsync = 0;
        Assert.Equal(1, udp.MaxAsync);

        udp.MaxAsync = -3;
        Assert.Equal(1, udp.MaxAsync);
    }
    #endregion

    #region UdpSession 会话拉取（来源过滤与接收环互斥）
    /// <summary>拉取模式下，会话只接受本会话远端的数据报，其它对端的数据报被丢弃</summary>
    [Fact(DisplayName = "拉取_UdpSession会话_过滤其它对端数据报")]
    public void UdpSessionPull_FilterForeignDatagram()
    {
        // 服务器拉取模式：不会启动接收环，可手动创建会话直接拉取
        using var server = new UdpServer { AutoReceive = false, Log = XTrace.Log };
        server.Open();
        var serverEp = new IPEndPoint(IPAddress.Loopback, server.Port);

        using var sockA = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        sockA.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        var epA = (IPEndPoint)sockA.LocalEndPoint!;

        using var sockB = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        sockB.Bind(new IPEndPoint(IPAddress.Loopback, 0));

        var session = Assert.IsType<UdpSession>(server.CreateSession(null, epA));
        session.Timeout = 3_000;

        // B 先发：应被会话丢弃；A 后发：应被取到
        sockB.SendTo("foreign-bbb".GetBytes(), serverEp);
        Thread.Sleep(50);
        sockA.SendTo("own-aaa".GetBytes(), serverEp);

        using var pk = session.Receive();
        Assert.Equal("own-aaa", pk.ToStr());
    }

    /// <summary>拉取模式下，仅有其它对端数据时全部被丢弃，本会话收不到数据（接收超时）</summary>
    [Fact(DisplayName = "拉取_UdpSession会话_仅其它对端数据_丢弃后超时")]
    public void UdpSessionPull_ForeignOnly_TimesOut()
    {
        using var server = new UdpServer { AutoReceive = false, Log = XTrace.Log };
        server.Open();
        var serverEp = new IPEndPoint(IPAddress.Loopback, server.Port);

        using var sockA = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        sockA.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        var epA = (IPEndPoint)sockA.LocalEndPoint!;

        using var sockB = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        sockB.Bind(new IPEndPoint(IPAddress.Loopback, 0));

        var session = Assert.IsType<UdpSession>(server.CreateSession(null, epA));
        session.Timeout = 300;

        sockB.SendTo("foreign-bbb".GetBytes(), serverEp);
        Thread.Sleep(50);

        // 非本会话来源的数据被丢弃，直到接收超时（未修复前会直接返回 foreign-bbb）
        var ex = Assert.Throws<SocketException>(() => session.Receive());
        Assert.Equal(SocketError.TimedOut, ex.SocketErrorCode);
    }

    /// <summary>默认事件模式（服务器接收环运行）下，会话拉取直接抛异常</summary>
    [Fact(DisplayName = "拉取_UdpSession会话_服务器接收环运行时抛异常")]
    public async Task UdpSessionPull_RefusedWhenServerReceiving()
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

        var ex = Assert.Throws<InvalidOperationException>(() => session!.Receive());
        Assert.Contains("AutoReceive", ex.Message);
        await Assert.ThrowsAsync<InvalidOperationException>(() => session!.ReceiveAsync(default));
    }

    /// <summary>拉取模式：UdpSession 会话级连续拉取多个数据报（同步异步交替），内容与顺序正确</summary>
    [Fact(DisplayName = "拉取_UdpSession_连续多数据报同步异步交替")]
    public async Task UdpSessionPull_MultipleDatagrams_Mixed()
    {
        using var server = new UdpServer { AutoReceive = false, Log = XTrace.Log };
        server.Open();
        var serverEp = new IPEndPoint(IPAddress.Loopback, server.Port);

        using var sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        sock.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        var localEp = (IPEndPoint)sock.LocalEndPoint!;

        var session = Assert.IsType<UdpSession>(server.CreateSession(null, localEp));
        session.Timeout = 5_000;

        // 发送 5 个大小不一的文本数据报
        var expected = new List<String>();
        for (var i = 0; i < 5; i++)
        {
            var text = $"pkt-{i}-" + new String('x', i * 64);
            expected.Add(text);
            sock.SendTo(text.GetBytes(), serverEp);
            Thread.Sleep(20);
        }

        // 同步与异步交替拉取，逐个校验内容
        for (var i = 0; i < 5; i++)
        {
            if (i % 2 == 0)
            {
                using var pk = session.Receive();
                Assert.NotNull(pk);
                Assert.Equal(expected[i], pk!.ToStr());
            }
            else
            {
                using var pk = await session.ReceiveAsync(default).WaitAsync(TimeSpan.FromSeconds(5));
                Assert.NotNull(pk);
                Assert.Equal(expected[i], pk!.ToStr());
            }
        }
    }
    #endregion
}
