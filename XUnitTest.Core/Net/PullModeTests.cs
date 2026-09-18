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
        Assert.True(client.ReceiveAsync());

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
}
