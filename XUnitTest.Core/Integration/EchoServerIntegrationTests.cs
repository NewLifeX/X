using System.Collections.Concurrent;
using System.Diagnostics;
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
            AddressFamily = AddressFamily.InterNetwork,
            Log = ServerLog,
            SessionLog = ServerLog,
        };
        Server = server;
        Server.Start();
        // 增大 UDP 收发缓冲区，防止高并发吞吐测试中操作系统层面丢包
        foreach (var srv in Server.Servers)
        {
            if (srv.Client != null)
            {
                srv.Client.ReceiveBufferSize = 8 << 20;
                srv.Client.SendBufferSize = 8 << 20;
            }
        }
    }

    public void Dispose() => Server?.Stop("IntegrationTestDone");
}

/// <summary>回声服务端，TCP+UDP 同时监听，追踪服务端总接收字节数与包数</summary>
public class EchoNetServer : NetServer<EchoSession>
{
    /// <summary>服务端总接收字节数（线程安全）</summary>
    public Int64 TotalBytesReceived;

    /// <summary>服务端总接收包数（线程安全）</summary>
    public Int64 TotalPacketsReceived;
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

        Interlocked.Add(ref Host.TotalBytesReceived, packet.Length);        // 服务端跟踪
        Interlocked.Increment(ref Host.TotalPacketsReceived);               // 包计数
        WriteLog("回显 {0} 字节", packet.Length);                             // 触发 TestLog
        Send(packet);
    }
}

/// <summary>EchoServer 集成测试：多种客户端类型（TCP/UDP）原样字节回显，正确性优先</summary>
[Collection("Integration")]
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

    [Fact(DisplayName = "12-TCP 100K TPS：热身+50客户端×10000流水线并发=500000条，硬断言TPS≥100000")]
    public async Task Test12_TcpEcho_100K_TPS()
    {
        const Int32 msgSize = 64;
        var port = _fixture.Server.Port;
        _fixture.ServerLog.Enable = false;

        // 热身：5客户端各发200包，预热 JIT 与 TCP 连接池
        {
            const Int32 warmupClients = 5;
            const Int32 warmupPerClient = 200;
            var warmupTasks = Enumerable.Range(0, warmupClients).Select(async _ =>
            {
                using var tcp = new TcpClient { NoDelay = true };
                await tcp.ConnectAsync(IPAddress.Loopback, port);
                var stream = tcp.GetStream();
                var toReceive = msgSize * warmupPerClient;
                var warmupReceived = 0;
                var readTask = Task.Run(async () =>
                {
                    var buf = new Byte[8192];
                    while (warmupReceived < toReceive)
                    {
                        var remaining = toReceive - warmupReceived;
                        var n = await stream.ReadAsync(buf.AsMemory(0, Math.Min(buf.Length, remaining)));
                        if (n == 0) break;
                        warmupReceived += n;
                    }
                });
                var send = new Byte[msgSize];
                for (var i = 0; i < warmupPerClient; i++)
                {
                    send[0] = (Byte)(i & 0xFF);
                    await stream.WriteAsync(send);
                }
                await readTask.WaitAsync(TimeSpan.FromSeconds(15));
            }).ToArray();
            await Task.WhenAll(warmupTasks).WaitAsync(TimeSpan.FromSeconds(30));
        }

        const Int32 clientCount = 50;
        const Int32 perClient = 10_000;
        const Int32 total = clientCount * perClient;

        var sw = Stopwatch.StartNew();

        var tasks = Enumerable.Range(0, clientCount).Select(async clientIdx =>
        {
            using var tcp = new TcpClient { NoDelay = true };
            await tcp.ConnectAsync(IPAddress.Loopback, port);
            var stream = tcp.GetStream();
            var toReceive = msgSize * perClient;
            var received = 0;

            // 后台并行读取，不阻塞发送，实现流水线
            var readTask = Task.Run(async () =>
            {
                var buf = new Byte[8192];
                while (received < toReceive)
                {
                    var remaining = toReceive - received;
                    var n = await stream.ReadAsync(buf.AsMemory(0, Math.Min(buf.Length, remaining)));
                    if (n == 0) break;
                    received += n;
                }
                return received;
            });

            // 持续发送，首字节带序号便于问题定位
            var send = new Byte[msgSize];
            for (var i = 0; i < perClient; i++)
            {
                send[0] = (Byte)(i & 0xFF);
                await stream.WriteAsync(send);
            }

            var totalReceived = await readTask.WaitAsync(TimeSpan.FromSeconds(30));
            return totalReceived == toReceive ? perClient : 0;
        }).ToArray();

        var counts = await Task.WhenAll(tasks);
        sw.Stop();
        _fixture.ServerLog.Enable = true;

        var completed = counts.Sum();
        var tps = total / sw.Elapsed.TotalSeconds;
        XTrace.WriteLine("TCP 100K TPS（热身后）：{0}条/{1}ms，TPS={2:N0}", total, sw.ElapsedMilliseconds, tps);

        Assert.Equal(total, completed);
        Assert.True(tps >= 100_000, $"TPS={tps:N0}，低于100000，耗时={sw.ElapsedMilliseconds}ms");
    }

    [Fact(DisplayName = "13-UDP 并发吞吐：热身+5客户端×10000包=50000条，收到率≥90%，TPS≥30000")]
    public async Task Test13_UdpEcho_100K_TPS()
    {
        const Int32 msgSize = 32;
        var port = _fixture.Server.Port;
        var ep = new IPEndPoint(IPAddress.Loopback, port);

        // 热身：1客户端并发发200包，预热 UDP Socket 与服务端
        {
            var warmupCount = 0;
            var warmupDone = new TaskCompletionSource<Boolean>();
            using var wu = new UdpClient();
            wu.Client.ReceiveBufferSize = 1 << 20;
            // 显式绑定本地端口，避免 ReceiveAsync 与 SendAsync 并发时竞争调用 Socket.Bind 导致 WSAEINVAL
            wu.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
            var warmupMsg = new Byte[msgSize];
            _ = Task.Run(async () =>
            {
                while (warmupCount < 200)
                {
                    try
                    {
                        await wu.ReceiveAsync().WaitAsync(TimeSpan.FromMilliseconds(500));
                        Interlocked.Increment(ref warmupCount);
                    }
                    catch { break; }
                }
                warmupDone.TrySetResult(true);
            });
            for (var i = 0; i < 200; i++) await wu.SendAsync(warmupMsg, msgSize, ep);
            await warmupDone.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }

        const Int32 clientCount = 5;
        const Int32 perClient = 10_000;
        const Int32 total = clientCount * perClient;

        var totalReceived = 0;
        var tcs = new TaskCompletionSource<Boolean>();

        // 创建所有 UDP 客户端并启动接收循环
        var udpClients = Enumerable.Range(0, clientCount).Select(_ =>
        {
            var udp = new UdpClient();
            udp.Client.ReceiveBufferSize = 1 << 20;
            // 显式绑定本地端口，避免 ReceiveAsync 与 SendAsync 并发时竞争调用 Socket.Bind 导致 WSAEINVAL
            udp.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
            return udp;
        }).ToArray();

        var recvCts = new CancellationTokenSource();
        var recvTasks = udpClients.Select(udp => Task.Run(async () =>
        {
            while (!recvCts.IsCancellationRequested)
            {
                try
                {
                    await udp.ReceiveAsync().WaitAsync(TimeSpan.FromMilliseconds(200), recvCts.Token);
                    var n = Interlocked.Increment(ref totalReceived);
                    if (n >= total * 9 / 10) tcs.TrySetResult(true);
                }
                catch { break; }
            }
        })).ToArray();

        // 关闭每包 WriteLog，减少单线程 UDP 接收循环开销，测量结束后还原
        _fixture.ServerLog.Enable = false;
        var sw = Stopwatch.StartNew();

        // 并发发送（fire-and-forget，不等回包）
        var sendTasks = udpClients.Select((udp, idx) => Task.Run(async () =>
        {
            var send = new Byte[msgSize];
            send[0] = (Byte)(idx & 0xFF);
            for (var i = 0; i < perClient; i++)
            {
                send[1] = (Byte)(i & 0xFF);
                await udp.SendAsync(send, msgSize, ep);
                if (i % 100 == 0) await Task.Yield();
            }
        })).ToArray();

        await Task.WhenAll(sendTasks);
        // 等待90%包到达或超时
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(30));
        sw.Stop();

        recvCts.Cancel();
        await Task.WhenAll(recvTasks).WaitAsync(TimeSpan.FromSeconds(2));
        foreach (var udp in udpClients) udp.Dispose();
        _fixture.ServerLog.Enable = true;

        var tps = (Int64)(total / sw.Elapsed.TotalSeconds);
        XTrace.WriteLine("UDP 并发吞吐（热身后）：{0}条/{1}ms，TPS={2:N0}，收到={3}/{4}", total, sw.ElapsedMilliseconds, tps, totalReceived, total);

        Assert.True(totalReceived >= total * 9 / 10, $"UDP 收到率低于90%：{totalReceived}/{total}");
        // UDP 单线程接收循环实测约 40-51K pps（禁用日志后），保守阈值 30K
        Assert.True(tps >= 30_000, $"TPS={tps:N0}，低于30000，耗时={sw.ElapsedMilliseconds}ms");
    }
}
