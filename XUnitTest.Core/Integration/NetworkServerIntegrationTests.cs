using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NewLife;
using NewLife.Data;
using NewLife.Log;
using NewLife.Messaging;
using NewLife.Net;
using NewLife.Net.Handlers;
using Xunit;

namespace XUnitTest.Integration;

/// <summary>NetworkServer 集成测试固定装置，复现 Samples/Zero.Server 的逻辑</summary>
public class NetworkServerFixture : IDisposable
{
    /// <summary>网络服务端实例（强类型，便于访问 ReceivedMessages 与 ConnectionCount）</summary>
    public MyNetServer Server { get; }

    /// <summary>服务端日志捕获器，用于断言连接/接收/断开等服务端行为</summary>
    public TestLog ServerLog { get; } = new();

    public NetworkServerFixture()
    {
        var server = new MyNetServer
        {
            Port = 0,
            AddressFamily = AddressFamily.InterNetwork,
            Log = ServerLog,
            SessionLog = ServerLog,
        };
        Server = server;
        Server.Start();
    }

    public void Dispose() => Server?.Stop("IntegrationTestDone");
}

/// <summary>网络服务端，追踪累计连接数与服务端收到的消息内容</summary>
public class MyNetServer : NetServer<MyNetSession>
{
    /// <summary>已建立的累计连接数（线程安全）</summary>
    public Int32 ConnectionCount;

    /// <summary>服务端收到的原始消息内容，线程安全</summary>
    public ConcurrentQueue<String> ReceivedMessages { get; } = new();
}

/// <summary>定义会话。连接时发欢迎语，收到数据后：记录到服务端队列、写日志、返回反转字符串</summary>
public class MyNetSession : NetSession<MyNetServer>
{
    protected override void OnConnected()
    {
        Interlocked.Increment(ref Host.ConnectionCount);
        Send($"Welcome to visit {Environment.MachineName}!  [{Remote}]\r\n");
        base.OnConnected();
    }

    protected override void OnDisconnected(String reason)
    {
        WriteLog("客户端 {0} 已断开连接。{1}", Remote, reason);
        base.OnDisconnected(reason);
    }

    protected override void OnReceive(ReceivedEventArgs e)
    {
        var packet = e.Packet;
        if (packet == null || packet.Length == 0) return;

        var msg = packet.ToStr();
        Host.ReceivedMessages.Enqueue(msg);     // 服务端跟踪，供测试断言
        WriteLog("收到：{0}", msg);              // 触发 TestLog 记录
        Send(msg.Reverse().Join(null));
    }

    protected override void OnError(Object sender, ExceptionEventArgs e)
    {
        WriteLog("[{0}] 错误：{1}", e.Action, e.Exception?.GetTrue().Message);
        base.OnError(sender, e);
    }
}

/// <summary>TCP StandardCodec 集成测试固定装置</summary>
public class TcpCodecServerFixture : IDisposable
{
    public TcpCodecNetServer Server { get; }

    public TcpCodecServerFixture()
    {
        var server = new TcpCodecNetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            Log = XTrace.Log,
#if DEBUG
            SessionLog = XTrace.Log,
#endif
        };
        Server = server;
        Server.Start();
    }

    public void Dispose() => Server?.Stop("IntegrationTestDone");
}

public class TcpCodecNetServer : NetServer<TcpCodecSession>
{
    /// <summary>服务端解码后收到的原始载荷，线程安全</summary>
    public ConcurrentQueue<Byte[]> ReceivedPayloads { get; } = new();

    public TcpCodecNetServer()
    {
        Add<StandardCodec>();
    }
}

public class TcpCodecSession : NetSession<TcpCodecNetServer>
{
    private Int32 _count;

    protected override void OnReceive(ReceivedEventArgs e)
    {
        var msg = e.Message;
        if (msg == null) return;

        // 提取解码后的原始载荷，记录到服务端，供测试断言
        var payload = ExtractSessionPayload(msg);
        Host.ReceivedPayloads.Enqueue(payload);
        WriteLog("收到解码数据：{0} 字节", payload.Length);

        var n = Interlocked.Increment(ref _count);
        if (n % 10 == 0) Thread.Sleep(Random.Shared.Next(10, 30));

        // 复用接收链路上下文，StandardCodec.Write 可通过 GetRequest 取到原始请求，自动构造 Reply=true+相同 Sequence 的回复
        SendReply(msg, e);
    }

    private static Byte[] ExtractSessionPayload(Object msg)
    {
        if (msg is IMessage imsg) return imsg.Payload?.ToArray() ?? [];
        if (msg is IPacket pk) return pk.ToArray();
        if (msg is Byte[] buf) return buf;
        return [];
    }
}

/// <summary>UDP StandardCodec 集成测试固定装置</summary>
public class UdpCodecServerFixture : IDisposable
{
    public UdpCodecNetServer Server { get; }

    public UdpCodecServerFixture()
    {
        var server = new UdpCodecNetServer
        {
            Port = 0,
            ProtocolType = NetType.Udp,
            AddressFamily = AddressFamily.InterNetwork,
            Log = XTrace.Log,
#if DEBUG
            SessionLog = XTrace.Log,
#endif
        };
        Server = server;
        Server.Start();
    }

    public void Dispose() => Server?.Stop("IntegrationTestDone");
}

public class UdpCodecNetServer : NetServer<UdpCodecSession>
{
    /// <summary>服务端解码后收到的原始载荷，线程安全</summary>
    public ConcurrentQueue<Byte[]> ReceivedPayloads { get; } = new();

    public UdpCodecNetServer()
    {
        Add<StandardCodec>();
    }
}

public class UdpCodecSession : NetSession<UdpCodecNetServer>
{
    protected override void OnReceive(ReceivedEventArgs e)
    {
        var msg = e.Message;
        if (msg == null) return;

        // 记录服务端解码后收到的原始载荷
        var payload = ExtractSessionPayload(msg);
        Host.ReceivedPayloads.Enqueue(payload);

        // 复用接收链路上下文，StandardCodec.Write 可通过 GetRequest 取到原始请求，自动构造 Reply=true+相同 Sequence 的回复
        SendReply(msg, e);
    }

    private static Byte[] ExtractSessionPayload(Object msg)
    {
        if (msg is IMessage imsg) return imsg.Payload?.ToArray() ?? [];
        if (msg is IPacket pk) return pk.ToArray();
        if (msg is Byte[] buf) return buf;
        return [];
    }
}

/// <summary>TCP JsonCodec+StandardCodec 集成测试固定装置</summary>
public class JsonCodecServerFixture : IDisposable
{
    public JsonCodecNetServer Server { get; }

    public JsonCodecServerFixture()
    {
        var server = new JsonCodecNetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            Log = XTrace.Log,
#if DEBUG
            SessionLog = XTrace.Log,
#endif
        };
        Server = server;
        Server.Start();
    }

    public void Dispose() => Server?.Stop("IntegrationTestDone");
}

public class JsonCodecNetServer : NetServer<JsonCodecSession>
{
    /// <summary>服务端解码后收到的对象（IDictionary），线程安全</summary>
    public ConcurrentQueue<Object> ReceivedObjects { get; } = new();

    public JsonCodecNetServer()
    {
        Add<JsonCodec>();
        Add<StandardCodec>();
    }
}

public class JsonCodecSession : NetSession<JsonCodecNetServer>
{
    protected override void OnReceive(ReceivedEventArgs e)
    {
        var msg = e.Message;
        if (msg == null) return;

        Host.ReceivedObjects.Enqueue(msg);
        WriteLog("收到JSON对象：{0} 类型={1}", (msg as System.Collections.IDictionary)?.Count, msg.GetType().Name);

        SendMessage(msg);
    }
}

/// <summary>NetworkServer 基础集成测试，验证 TCP/UDP 连接与收发功能</summary>
[TestCaseOrderer("NewLife.UnitTest.DefaultOrderer", "NewLife.UnitTest")]
public class NetworkServerIntegrationTests(NetworkServerFixture fixture) : IClassFixture<NetworkServerFixture>
{
    [Fact(DisplayName = "01-服务端已启动且端口已分配")]
    public void Test01_ServerStarted()
    {
        Assert.True(fixture.Server.Active, "服务端应处于运行状态");
        Assert.True(fixture.Server.Port > 0, "端口应已分配");

        XTrace.WriteLine("NetworkServer 已在端口 {0} 上启动", fixture.Server.Port);
    }

    [Fact(DisplayName = "02-TcpClient 全流程：连接→欢迎→发送→精确回显→服务端确认")]
    public async Task Test02_TcpClient_FullFlow()
    {
        var port = fixture.Server.Port;
        var priorCount = fixture.Server.ReceivedMessages.Count;

        using var client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", port);
        var ns = client.GetStream();
        ns.ReadTimeout = 5_000;

        // 1. 接收欢迎消息，验证包含机器名
        var buf = new Byte[1024];
        var count = await ns.ReadAsync(buf);
        var welcome = Encoding.UTF8.GetString(buf, 0, count);
        Assert.Contains("Welcome", welcome);
        Assert.Contains(Environment.MachineName, welcome);

        // 2. 发送消息
        const String msg = "Hello NewLife";
        await ns.WriteAsync(Encoding.UTF8.GetBytes(msg));

        // 3. 接收回显，精确验证反转结果
        count = await ns.ReadAsync(buf);
        var reply = Encoding.UTF8.GetString(buf, 0, count);
        Assert.Equal("efiLweN olleH", reply);

        // 4. 服务端日志确认：包含接收记录
        Assert.True(await fixture.ServerLog.WaitForAsync("收到"), "服务端日志应包含接收记录");

        // 5. 服务端 ReceivedMessages 队列确实记录了该消息
        Assert.True(fixture.Server.ReceivedMessages.Count > priorCount, "服务端应已记录收到的消息");
        Assert.Contains(msg, fixture.Server.ReceivedMessages);
    }

    [Fact(DisplayName = "03-UdpClient 全流程：发包→欢迎→精确回显→服务端确认")]
    public async Task Test03_UdpClient_FullFlow()
    {
        var port = fixture.Server.Port;
        var endpoint = new IPEndPoint(IPAddress.Loopback, port);
        const String msg = "Hello NewLife";
        var msgBytes = Encoding.UTF8.GetBytes(msg);

        using var udp = new UdpClient();
        udp.Client.ReceiveTimeout = 5_000;

        // 1. 发送数据，触发服务端建立 UDP 会话并推送欢迎消息
        await udp.SendAsync(msgBytes, msgBytes.Length, endpoint);

        // 2. 收欢迎消息，验证包含机器名
        var result1 = await udp.ReceiveAsync().WaitAsync(TimeSpan.FromSeconds(5));
        var welcome = Encoding.UTF8.GetString(result1.Buffer);
        Assert.Contains("Welcome", welcome);
        Assert.Contains(Environment.MachineName, welcome);

        // 3. 收回显，精确验证反转结果
        var result2 = await udp.ReceiveAsync().WaitAsync(TimeSpan.FromSeconds(5));
        var reply = Encoding.UTF8.GetString(result2.Buffer);
        Assert.Equal("efiLweN olleH", reply);

        // 4. 验证服务端收到了原始消息内容
        await Task.Delay(50);
        Assert.Contains(msg, fixture.Server.ReceivedMessages);

        // 5. 服务端日志有接收记录
        Assert.True(fixture.ServerLog.Contains("收到"), "服务端日志应有接收记录");

        // 发送空包触发服务端断开会话
        await udp.SendAsync([], 0, endpoint);
    }

    [Fact(DisplayName = "04-ISocketClient(TCP) 全流程：连接→欢迎→发送→精确回显→服务端确认断开")]
    public async Task Test04_ISocketClient_FullFlow()
    {
        var port = fixture.Server.Port;
        var priorCount = fixture.Server.ReceivedMessages.Count;

        var uri = new NetUri($"tcp://127.0.0.1:{port}");
        var client = uri.CreateRemote();
        client.Name = "集成测试Tcp客户";
        client.Log = XTrace.Log;

        if (client is TcpSession tcp) tcp.MaxAsync = 0;

        // 1. 接收欢迎消息，验证包含机器名
        using var welcome = await client.ReceiveAsync(default).WaitAsync(TimeSpan.FromSeconds(5));
        var welcomeStr = welcome.ToStr();
        Assert.Contains("Welcome", welcomeStr);
        Assert.Contains(Environment.MachineName, welcomeStr);

        // 2. 发送消息
        const String msg = "Hello NewLife";
        client.Send(msg);

        // 3. 接收回显，精确验证反转结果
        using var reply = await client.ReceiveAsync(default).WaitAsync(TimeSpan.FromSeconds(5));
        var replyStr = reply.ToStr();
        Assert.Equal("efiLweN olleH", replyStr);

        client.Close("Test04Done");

        // 4. 服务端确认记录了该消息
        await Task.Delay(50);
        Assert.True(fixture.Server.ReceivedMessages.Count > priorCount, "服务端应已记录收到的消息");
        Assert.Contains(msg, fixture.Server.ReceivedMessages);

        // 5. 服务端日志确认断开连接
        Assert.True(await fixture.ServerLog.WaitForAsync("断开"), "服务端日志应包含断开连接记录");
    }
}

/// <summary>NetServer+NetClient 挂 StandardCodec：同步异步收发、大包小包、粘包、并发与吞吐</summary>
[TestCaseOrderer("NewLife.UnitTest.DefaultOrderer", "NewLife.UnitTest")]
public class CodecIntegrationTests(TcpCodecServerFixture tcpFixture, UdpCodecServerFixture udpFixture) : IClassFixture<TcpCodecServerFixture>, IClassFixture<UdpCodecServerFixture>
{
    private NetClient CreateTcpClient()
    {
        var client = new NetClient($"tcp://127.0.0.1:{tcpFixture.Server.Port}") { AutoReconnect = false };
        client.Add<StandardCodec>();
        return client;
    }

    private NetClient CreateUdpClient()
    {
        var client = new NetClient($"udp://127.0.0.1:{udpFixture.Server.Port}") { AutoReconnect = false };
        client.Add<StandardCodec>();
        return client;
    }

    private static Byte[] ExtractPayload(Object? message, Byte[] fallback)
    {
        if (message is IMessage imsg) return imsg.Payload?.ToArray() ?? [];
        if (message is IPacket pk) return pk.ToArray();
        if (message is Byte[] buf) return buf;
        return fallback;
    }

    [Fact(DisplayName = "06-TCP+StandardCodec 完整流程：SendMessageAsync→精确回显→服务端确认解码内容")]
    public async Task Test06_Tcp_StandardCodec_Full()
    {
        var payload = new Byte[64];
        Random.Shared.NextBytes(payload);
        var serverBefore = tcpFixture.Server.ReceivedPayloads.Count;

        using var client = CreateTcpClient();
        client.Open();

        // SendMessageAsync：StandardCodec 通过 Sequence 号匹配请求响应，返回解码前的原始载荷
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var response = await client.SendMessageAsync(payload, cts.Token);
        var received = ExtractPayload(response, []);
        client.Close("done");

        // 客户端回显精确等于发送内容
        Assert.Equal(payload, received);

        // 服务端确认收到了解码后的原始载荷
        await Task.Delay(50);
        Assert.True(tcpFixture.Server.ReceivedPayloads.Count > serverBefore, "服务端应已记录解码后的载荷");
        Assert.True(tcpFixture.Server.ReceivedPayloads.Any(p => p.SequenceEqual(payload)),
            "服务端收到的载荷应与客户端发送的完全一致");
    }

    [Fact(DisplayName = "07-TCP+StandardCodec 64KB 大包：SendMessageAsync→精确回显→服务端确认")]
    public async Task Test07_Tcp_StandardCodec_Large()
    {
        var payload = new Byte[64 * 1024];
        Random.Shared.NextBytes(payload);
        var serverBefore = tcpFixture.Server.ReceivedPayloads.Count;

        using var client = CreateTcpClient();
        client.Open();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var response = await client.SendMessageAsync(payload, cts.Token);
        var received = ExtractPayload(response, []);
        client.Close("done");

        Assert.Equal(payload, received);

        await Task.Delay(100);
        Assert.True(tcpFixture.Server.ReceivedPayloads.Count > serverBefore, "服务端应已记录大包载荷");
        Assert.True(tcpFixture.Server.ReceivedPayloads.Any(p => p.SequenceEqual(payload)),
            "服务端收到的大包应与客户端发送的完全一致");
    }

    [Fact(DisplayName = "08-TCP+StandardCodec 粘包测试：快速连发100包全部收回")]
    public async Task Test08_Tcp_StandardCodec_Sticky()
    {
        const Int32 total = 100;
        var received = 0;
        var tcs = new TaskCompletionSource<Boolean>();

        using var client = CreateTcpClient();
        client.Received += (s, e) =>
        {
            var n = Interlocked.Increment(ref received);
            if (n >= total) tcs.TrySetResult(true);
        };

        client.Open();
        for (var i = 0; i < total; i++)
        {
            var payload = new Byte[16];
            payload[0] = (Byte)i;
            client.SendMessage(payload);
        }

        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(30));
        client.Close("done");

        Assert.Equal(total, received);
    }

    [Fact(DisplayName = "09-UDP+StandardCodec 小包：SendMessageAsync→精确回显→服务端确认")]
    public async Task Test09_Udp_StandardCodec_Small()
    {
        var payload = new Byte[32];
        Random.Shared.NextBytes(payload);
        var serverBefore = udpFixture.Server.ReceivedPayloads.Count;

        using var client = CreateUdpClient();
        client.Open();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var response = await client.SendMessageAsync(payload, cts.Token);
        var received = ExtractPayload(response, []);
        client.Close("done");

        Assert.Equal(payload, received);

        await Task.Delay(50);
        Assert.True(udpFixture.Server.ReceivedPayloads.Count > serverBefore, "服务端应已记录UDP解码载荷");
        Assert.True(udpFixture.Server.ReceivedPayloads.Any(p => p.SequenceEqual(payload)),
            "服务端收到的UDP载荷应与客户端发送的完全一致");
    }

    [Fact(DisplayName = "10-UDP+StandardCodec 多包收发")]
    public async Task Test10_Udp_StandardCodec_Multi()
    {
        const Int32 total = 10;
        var bag = new ConcurrentBag<Byte[]>();
        var tcs = new TaskCompletionSource<Boolean>();

        using var client = CreateUdpClient();
        client.Received += (s, e) =>
        {
            bag.Add(ExtractPayload(e.Message, e.GetBytes()));
            if (bag.Count >= total) tcs.TrySetResult(true);
        };

        client.Open();

        var payloads = new Byte[total][];
        for (var i = 0; i < total; i++)
        {
            payloads[i] = new Byte[16];
            payloads[i][0] = (Byte)i;
            Random.Shared.NextBytes(payloads[i].AsSpan(1));
            client.SendMessage(payloads[i]);
            await Task.Delay(5);
        }

        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        client.Close("done");

        foreach (var p in payloads)
            Assert.Contains(bag, x => x.SequenceEqual(p));
    }

    [Fact(DisplayName = "11-TCP 并发吞吐：50客户端×200消息=10000条，零丢包且TPS>=10000")]
    public async Task Test11_Tcp_Throughput_10K()
    {
        const Int32 clientCount = 50;
        const Int32 perClient = 200;
        const Int32 total = clientCount * perClient;

        var received = 0;
        var tcs = new TaskCompletionSource<Boolean>();

        var clients = new List<NetClient>(clientCount);
        for (var i = 0; i < clientCount; i++)
        {
            var c = CreateTcpClient();
            c.Received += (s, e) =>
            {
                var n = Interlocked.Increment(ref received);
                if (n >= total) tcs.TrySetResult(true);
            };
            c.Open();
            clients.Add(c);
        }

        var sw = Stopwatch.StartNew();
        var sendTasks = clients.Select(async c =>
        {
            for (var i = 0; i < perClient; i++)
            {
                var payload = new Byte[32];
                payload[0] = (Byte)i;
                c.SendMessage(payload);
                if (i % 50 == 0) await Task.Yield();
            }
        }).ToArray();

        await Task.WhenAll(sendTasks);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(60));
        sw.Stop();

        foreach (var c in clients) c.Close("done");

        var tps = (Int64)(total / sw.Elapsed.TotalSeconds);
        XTrace.WriteLine("吞吐测试结果：{0}条/{1}ms，TPS={2}", total, sw.ElapsedMilliseconds, tps);

        Assert.Equal(total, received);
        Assert.True(tps >= 10_000, $"TPS={tps}，低于10000");
    }
}

/// <summary>NetServer+NetClient 至少双编码器（JsonCodec + StandardCodec）集成测试</summary>
[TestCaseOrderer("NewLife.UnitTest.DefaultOrderer", "NewLife.UnitTest")]
public class JsonCodecIntegrationTests(JsonCodecServerFixture fixture) : IClassFixture<JsonCodecServerFixture>
{
    private NetClient CreateJsonClient()
    {
        var client = new NetClient($"tcp://127.0.0.1:{fixture.Server.Port}") { AutoReconnect = false };
        client.Add<JsonCodec>();
        client.Add<StandardCodec>();
        return client;
    }

    [Fact(DisplayName = "12-TCP+JsonCodec+StandardCodec 对象全流程：发送→内容回显→服务端确认解码")]
    public async Task Test12_JsonAndStandard_ObjectRoundtrip()
    {
        var obj = new Dictionary<String, Object>
        {
            ["name"] = "NewLife",
            ["count"] = 123,
            ["ok"] = true,
        };

        var serverBefore = fixture.Server.ReceivedObjects.Count;

        var wait = new TaskCompletionSource<Object>();
        using var client = CreateJsonClient();
        client.Received += (s, e) => wait.TrySetResult(e.Message!);

        client.Open();
        client.SendMessage(obj);
        var result = await wait.Task.WaitAsync(TimeSpan.FromSeconds(5));
        client.Close("done");

        // 客户端回显类型验证
        Assert.NotNull(result);
        Assert.True(result is IDictionary<String, Object?> || result is IDictionary<String, Object>,
            $"返回类型应为字典，实际：{result.GetType().FullName}");

        // 验证关键字段内容
        if (result is IDictionary<String, Object?> clientDict)
            Assert.Equal("NewLife", clientDict["name"]?.ToString());

        // 服务端确认收到了解码后的 JSON 对象
        await Task.Delay(50);
        Assert.True(fixture.Server.ReceivedObjects.Count > serverBefore, "服务端应已记录收到的 JSON 对象");
        var serverObj = fixture.Server.ReceivedObjects.LastOrDefault();
        Assert.NotNull(serverObj);
        Assert.True(serverObj is IDictionary<String, Object?> || serverObj is IDictionary<String, Object>,
            $"服务端接收对象应为字典，实际：{serverObj!.GetType().FullName}");

        if (serverObj is IDictionary<String, Object?> serverDict)
            Assert.Equal("NewLife", serverDict["name"]?.ToString());
    }
}