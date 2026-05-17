using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using NewLife;
using NewLife.Data;
using NewLife.Log;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Integration;

/// <summary>EchoServer 集成测试固定装置。TCP+UDP 同时监听，无编解码器，原样字节回显</summary>
public class EchoServerFixture : IDisposable
{
    /// <summary>回声服务端实例（强类型，便于访问 TotalBytesReceived）</summary>
    public EchoNetServer Server { get; }

    /// <summary>服务端日志捕获器，用于断言服务端回显行为</summary>
    public TestLog ServerLog { get; } = new();

    public EchoServerFixture()
    {
        var server = new EchoNetServer
        {
            Port = 0,
            AddressFamily = System.Net.Sockets.AddressFamily.InterNetwork,
            Log = ServerLog,
            SessionLog = ServerLog,
        };
        Server = server;
        Server.Start();
    }

    public void Dispose() => Server?.Stop("IntegrationTestDone");
}

/// <summary>回声服务端，TCP+UDP 同时监听，追踪服务端总接收字节数</summary>
public class EchoNetServer : NetServer<EchoSession>
{
    /// <summary>服务端总接收字节数（线程安全）</summary>
    public Int64 TotalBytesReceived;
}

/// <summary>回声会话，收到数据后记录字节数、写日志并原样返回</summary>
public class EchoSession : NetSession<EchoNetServer>
{
    /// <summary>收到客户端数据，记录并原样回显</summary>
    /// <param name="e">接收事件参数</param>
    protected override void OnReceive(ReceivedEventArgs e)
    {
        var packet = e.Packet;
        if (packet == null || packet.Length == 0) return;

        Interlocked.Add(ref Host.TotalBytesReceived, packet.Length);  // 服务端跟踪
        WriteLog("回显 {0} 字节", packet.Length);                            // 触发 TestLog
        Send(packet);
    }
}

/// <summary>EchoServer 集成测试：多种客户端类型（TCP/UDP）原样字节回显，正确性优先</summary>
[TestCaseOrderer("NewLife.UnitTest.DefaultOrderer", "NewLife.UnitTest")]
public class EchoServerIntegrationTests : IClassFixture<EchoServerFixture>
{
    private readonly EchoServerFixture _fixture;

    public EchoServerIntegrationTests(EchoServerFixture fixture) => _fixture = fixture;

    [Fact(DisplayName = "01-服务端已启动且端口已分配")]
    public void Test01_ServerStarted()
    {
        Assert.True(_fixture.Server.Active, "服务端应处于运行状态");
        Assert.True(_fixture.Server.Port > 0, "端口应已分配");
        XTrace.WriteLine("EchoServer 已在端口 {0} 上启动", _fixture.Server.Port);
    }

    [Fact(DisplayName = "02-NetClient TCP 16字节小包回显")]
    public async Task Test02_TcpEcho_NetClient_Small()
    {
        var port = _fixture.Server.Port;
        var beforeBytes = Interlocked.Read(ref _fixture.Server.TotalBytesReceived);
        var client = new NetClient($"tcp://127.0.0.1:{port}") { AutoReconnect = false };

        var payload = new Byte[16];
        Random.Shared.NextBytes(payload);

        var wait = new TaskCompletionSource<Byte[]>();
        client.Received += (s, e) => wait.TrySetResult(e.GetBytes());
        client.Open();
        client.Send(payload);

        var received = await wait.Task.WaitAsync(TimeSpan.FromSeconds(5));
        client.Close("done");

        // 客户端验证
        Assert.Equal(payload, received);

        // 服务端验证：确认服务端确实处理了该请求
        Assert.True(await _fixture.ServerLog.WaitForAsync("回显"), "服务端日志应包含回显记录");
        Assert.True(Interlocked.Read(ref _fixture.Server.TotalBytesReceived) >= beforeBytes + payload.Length,
            "服务端应已接收该包的字节");
    }

    [Fact(DisplayName = "03-ISocketRemote TCP 小包回显")]
    public async Task Test03_TcpEcho_ISocketRemote()
    {
        var port = _fixture.Server.Port;
        var beforeBytes = Interlocked.Read(ref _fixture.Server.TotalBytesReceived);
        var client = new NetUri($"tcp://127.0.0.1:{port}").CreateRemote();

        var payload = new Byte[32];
        Random.Shared.NextBytes(payload);

        var wait = new TaskCompletionSource<Byte[]>();
        client.Received += (s, e) => wait.TrySetResult(e.GetBytes());
        client.Open();
        client.Send(payload);

        var received = await wait.Task.WaitAsync(TimeSpan.FromSeconds(5));
        client.Close("done");

        Assert.Equal(payload, received);
        Assert.True(Interlocked.Read(ref _fixture.Server.TotalBytesReceived) >= beforeBytes + payload.Length,
            "服务端应已接收该包的字节");
    }

    [Fact(DisplayName = "04-System.TcpClient TCP 小包回显")]
    public async Task Test04_TcpEcho_SystemTcpClient()
    {
        var port = _fixture.Server.Port;
        var beforeBytes = Interlocked.Read(ref _fixture.Server.TotalBytesReceived);
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);
        var ns = client.GetStream();
        ns.ReadTimeout = 5_000;

        var payload = new Byte[32];
        Random.Shared.NextBytes(payload);
        await ns.WriteAsync(payload);

        var received = new Byte[payload.Length];
        var total = 0;
        while (total < received.Length)
        {
            var n = await ns.ReadAsync(received.AsMemory(total));
            if (n == 0) break;
            total += n;
        }

        Assert.Equal(payload.Length, total);
        Assert.Equal(payload, received[..total]);
        Assert.True(Interlocked.Read(ref _fixture.Server.TotalBytesReceived) >= beforeBytes + payload.Length,
            "服务端应已接收该包的字节");
    }

    [Fact(DisplayName = "05-System.Socket TCP 小包回显")]
    public async Task Test05_TcpEcho_SystemSocket()
    {
        var port = _fixture.Server.Port;
        var beforeBytes = Interlocked.Read(ref _fixture.Server.TotalBytesReceived);
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.ReceiveTimeout = 5_000;
        await socket.ConnectAsync(IPAddress.Loopback, port);

        var payload = new Byte[32];
        Random.Shared.NextBytes(payload);
        await socket.SendAsync(payload, SocketFlags.None);

        var received = new Byte[payload.Length];
        var total = 0;
        while (total < received.Length)
        {
            var n = await socket.ReceiveAsync(received.AsMemory(total), SocketFlags.None);
            if (n == 0) break;
            total += n;
        }

        Assert.Equal(payload.Length, total);
        Assert.Equal(payload, received[..total]);
        Assert.True(Interlocked.Read(ref _fixture.Server.TotalBytesReceived) >= beforeBytes + payload.Length,
            "服务端应已接收该包的字节");
    }

    [Fact(DisplayName = "06-NetClient UDP 32字节回显")]
    public async Task Test06_UdpEcho_NetClient()
    {
        var port = _fixture.Server.Port;
        var beforeBytes = Interlocked.Read(ref _fixture.Server.TotalBytesReceived);
        var client = new NetClient($"udp://127.0.0.1:{port}") { AutoReconnect = false };

        var payload = new Byte[32];
        Random.Shared.NextBytes(payload);

        var wait = new TaskCompletionSource<Byte[]>();
        client.Received += (s, e) => wait.TrySetResult(e.GetBytes());
        client.Open();
        client.Send(payload);

        var received = await wait.Task.WaitAsync(TimeSpan.FromSeconds(5));
        client.Close("done");

        Assert.Equal(payload, received);
        await Task.Delay(50);
        Assert.True(Interlocked.Read(ref _fixture.Server.TotalBytesReceived) >= beforeBytes + payload.Length,
            "服务端应已接收UDP包的字节");
    }

    [Fact(DisplayName = "07-System.UdpClient UDP 回显")]
    public async Task Test07_UdpEcho_SystemUdpClient()
    {
        var port = _fixture.Server.Port;
        var beforeBytes = Interlocked.Read(ref _fixture.Server.TotalBytesReceived);
        using var udp = new UdpClient();
        udp.Client.ReceiveTimeout = 5_000;

        var payload = new Byte[32];
        Random.Shared.NextBytes(payload);
        var endpoint = new IPEndPoint(IPAddress.Loopback, port);
        await udp.SendAsync(payload, payload.Length, endpoint);

        var result = await udp.ReceiveAsync().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(payload, result.Buffer);

        await Task.Delay(50);
        Assert.True(Interlocked.Read(ref _fixture.Server.TotalBytesReceived) >= beforeBytes + payload.Length,
            "服务端应已接收UDP包的字节");
    }

    [Fact(DisplayName = "08-System.Socket UDP 回显")]
    public async Task Test08_UdpEcho_SystemSocket()
    {
        var port = _fixture.Server.Port;
        var beforeBytes = Interlocked.Read(ref _fixture.Server.TotalBytesReceived);
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.ReceiveTimeout = 5_000;
        var endpoint = new IPEndPoint(IPAddress.Loopback, port);
        EndPoint remote = endpoint;

        var payload = new Byte[32];
        Random.Shared.NextBytes(payload);
        await socket.SendToAsync(payload, SocketFlags.None, endpoint);

        var received = new Byte[256];
        var n = socket.ReceiveFrom(received, ref remote);
        Assert.Equal(payload, received[..n]);

        await Task.Delay(50);
        Assert.True(Interlocked.Read(ref _fixture.Server.TotalBytesReceived) >= beforeBytes + payload.Length,
            "服务端应已接收UDP包的字节");
    }

    [Fact(DisplayName = "09-NetClient TCP 1MB大包完整性回显")]
    public async Task Test09_TcpEcho_LargePacket()
    {
        var port = _fixture.Server.Port;
        var beforeBytes = Interlocked.Read(ref _fixture.Server.TotalBytesReceived);
        // 使用原生 TcpClient 直接字节流接收大包，避免 NetClient 粘包拆包干扰
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);
        var ns = client.GetStream();
        ns.ReadTimeout = 30_000;

        var payload = new Byte[1024 * 1024]; // 1MB
        Random.Shared.NextBytes(payload);
        await ns.WriteAsync(payload);

        // 循环读取直到收齐全部字节
        var received = new Byte[payload.Length];
        var total = 0;
        while (total < received.Length)
        {
            var n = await ns.ReadAsync(received.AsMemory(total));
            if (n == 0) break;
            total += n;
        }

        Assert.Equal(payload.Length, total);
        Assert.Equal(payload, received);

        // 服务端确认：收到的字节应 >= 1MB（TCP 分块接收，累加之和）
        Assert.True(Interlocked.Read(ref _fixture.Server.TotalBytesReceived) >= beforeBytes + payload.Length,
            $"服务端应已接收 1MB 大包，实际增量={Interlocked.Read(ref _fixture.Server.TotalBytesReceived) - beforeBytes}");
    }

    [Fact(DisplayName = "10-TcpClient TCP 连续10次请求，每次回显逐包验证")]
    public async Task Test10_TcpEcho_MultiMessage_Order()
    {
        var port = _fixture.Server.Port;
        const Int32 count = 10;
        const Int32 size = 64;
        var beforeBytes = Interlocked.Read(ref _fixture.Server.TotalBytesReceived);

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);
        var ns = client.GetStream();
        ns.ReadTimeout = 5_000;

        for (var i = 0; i < count; i++)
        {
            var payload = new Byte[size];
            payload[0] = (Byte)i;
            Random.Shared.NextBytes(payload.AsSpan(1));

            await ns.WriteAsync(payload);

            var received = new Byte[size];
            var total = 0;
            while (total < size)
            {
                var n = await ns.ReadAsync(received.AsMemory(total));
                if (n == 0) break;
                total += n;
            }

            Assert.Equal(size, total);
            Assert.Equal(payload, received);
        }

        // 服务端确认：应收到 count * size 字节
        Assert.True(Interlocked.Read(ref _fixture.Server.TotalBytesReceived) >= beforeBytes + count * size,
            $"服务端应接收 {count * size} 字节，实际增量={Interlocked.Read(ref _fixture.Server.TotalBytesReceived) - beforeBytes}");
    }

    [Fact(DisplayName = "11-NetClient TCP 10客户端并发各发1包，全部正确回显")]
    public async Task Test11_TcpEcho_ConcurrentClients()
    {
        var port = _fixture.Server.Port;
        const Int32 clientCount = 10;
        const Int32 packetSize = 32;
        var beforeBytes = Interlocked.Read(ref _fixture.Server.TotalBytesReceived);

        var tasks = Enumerable.Range(0, clientCount).Select(async idx =>
        {
            var payload = new Byte[packetSize];
            payload[0] = (Byte)idx;
            Random.Shared.NextBytes(payload.AsSpan(1));

            var wait = new TaskCompletionSource<Byte[]>();
            var client = new NetClient($"tcp://127.0.0.1:{port}") { AutoReconnect = false };
            client.Received += (s, e) => wait.TrySetResult(e.GetBytes());
            client.Open();
            client.Send(payload);

            var received = await wait.Task.WaitAsync(TimeSpan.FromSeconds(10));
            client.Close("done");
            return (payload, received);
        }).ToArray();

        var results = await Task.WhenAll(tasks);

        foreach (var (payload, received) in results)
            Assert.Equal(payload, received);

        // 服务端确认：应收到 clientCount * packetSize 字节
        await Task.Delay(50);
        Assert.True(Interlocked.Read(ref _fixture.Server.TotalBytesReceived) >= beforeBytes + clientCount * packetSize,
            $"服务端应接收 {clientCount * packetSize} 字节，实际增量={Interlocked.Read(ref _fixture.Server.TotalBytesReceived) - beforeBytes}");
    }
}
