using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NewLife;
using NewLife.Data;
using NewLife.Log;
using NewLife.Net;
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

/// <summary>NetworkServer 基础集成测试，验证 TCP/UDP 连接与收发功能</summary>
[Collection("Integration")]
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
        var sessionsBefore = fixture.Server.SessionCount;
        var maxBefore = fixture.Server.MaxSessionCount;

        var client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", port);
        var ns = client.GetStream();
        ns.ReadTimeout = 5_000;

        // 等待服务端完成会话建立（轮询避免偶发时序问题）
        for (var i = 0; i < 20 && fixture.Server.SessionCount < sessionsBefore + 1; i++)
            await Task.Delay(50);

        // 连接后：SessionCount 精确 +1；MaxSessionCount 同步更新（只增不减）
        Assert.Equal(sessionsBefore + 1, fixture.Server.SessionCount);
        var expectedMax = Math.Max(maxBefore, sessionsBefore + 1);
        Assert.Equal(expectedMax, fixture.Server.MaxSessionCount);

        // 1. 接收欢迎消息，精确验证整句结构（不多不少）
        //    格式：$"Welcome to visit {MachineName}!  [tcp://{ip}:{port}]\r\n"
        var buf = new Byte[1024];
        var n = await ns.ReadAsync(buf);
        var welcome = Encoding.UTF8.GetString(buf, 0, n);
        var expectedStart = $"Welcome to visit {Environment.MachineName}!  [tcp://127.0.0.1:";
        Assert.True(welcome.StartsWith(expectedStart, StringComparison.Ordinal),
            $"欢迎词应以 '{expectedStart}' 开头，实际：'{welcome}'");
        Assert.True(welcome.EndsWith("]\r\n", StringComparison.Ordinal),
            $"欢迎词应以 ']\\r\\n' 结尾，实际：'{welcome}'");
        var portPart = welcome[expectedStart.Length..welcome.IndexOf("]\r\n")];
        Assert.True(Int32.TryParse(portPart, out var clientPort) && clientPort is > 0 and <= 65535,
            $"欢迎词中端口号应为有效端口（1-65535），实际：'{portPart}'");

        // 2. 发送消息
        const String msg = "Hello NewLife";
        await ns.WriteAsync(Encoding.UTF8.GetBytes(msg));

        // 3. 接收回显，精确验证反转结果
        n = await ns.ReadAsync(buf);
        var reply = Encoding.UTF8.GetString(buf, 0, n);
        Assert.Equal("efiLweN olleH", reply);

        // 4. 服务端日志确认：包含接收记录
        Assert.True(await fixture.ServerLog.WaitForAsync("收到"), "服务端日志应包含接收记录");

        // 5. 服务端队列：精确多了1条（不是「大于」）
        Assert.Equal(priorCount + 1, fixture.Server.ReceivedMessages.Count);
        Assert.Contains(msg, fixture.Server.ReceivedMessages);

        // 主动关闭连接，等待服务端清理会话
        client.Close();
        client.Dispose();
        await Task.Delay(150);

        // 断开后：SessionCount 精确回到原值；MaxSessionCount 保持不减
        Assert.Equal(sessionsBefore, fixture.Server.SessionCount);
        Assert.Equal(expectedMax, fixture.Server.MaxSessionCount);
    }

    [Fact(DisplayName = "03-UdpClient 全流程：发包→欢迎→精确回显→服务端确认")]
    public async Task Test03_UdpClient_FullFlow()
    {
        var port = fixture.Server.Port;
        var endpoint = new IPEndPoint(IPAddress.Loopback, port);
        const String msg = "Hello NewLife";
        var msgBytes = Encoding.UTF8.GetBytes(msg);
        var sessionsBefore = fixture.Server.SessionCount;
        var maxBefore = fixture.Server.MaxSessionCount;

        using var udp = new UdpClient();
        udp.Client.ReceiveTimeout = 5_000;

        // 1. 发送数据，触发服务端建立 UDP 会话并推送欢迎消息
        await udp.SendAsync(msgBytes, msgBytes.Length, endpoint);

        // 等待服务端完成 UDP 会话建立（轮询避免偶发时序问题）
        for (var i = 0; i < 20 && fixture.Server.SessionCount < sessionsBefore + 1; i++)
            await Task.Delay(50);

        // 首包触发 UDP 会话建立：SessionCount 精确 +1；MaxSessionCount 同步更新
        Assert.Equal(sessionsBefore + 1, fixture.Server.SessionCount);
        var expectedMax = Math.Max(maxBefore, sessionsBefore + 1);
        Assert.Equal(expectedMax, fixture.Server.MaxSessionCount);

        // 2. 收欢迎消息，精确验证整句结构（不多不少）
        //    格式：$"Welcome to visit {MachineName}!  [udp://{ip}:{port}]\r\n"
        var result1 = await udp.ReceiveAsync().WaitAsync(TimeSpan.FromSeconds(5));
        var welcome = Encoding.UTF8.GetString(result1.Buffer);
        var expectedStart = $"Welcome to visit {Environment.MachineName}!  [udp://127.0.0.1:";
        Assert.True(welcome.StartsWith(expectedStart, StringComparison.Ordinal),
            $"欢迎词应以 '{expectedStart}' 开头，实际：'{welcome}'");
        Assert.True(welcome.EndsWith("]\r\n", StringComparison.Ordinal),
            $"欢迎词应以 ']\\r\\n' 结尾，实际：'{welcome}'");
        var portPart = welcome[expectedStart.Length..welcome.IndexOf("]\r\n")];
        Assert.True(Int32.TryParse(portPart, out var clientPort) && clientPort is > 0 and <= 65535,
            $"欢迎词中端口号应为有效端口（1-65535），实际：'{portPart}'");

        // 3. 收回显，精确验证反转结果
        var result2 = await udp.ReceiveAsync().WaitAsync(TimeSpan.FromSeconds(5));
        var reply = Encoding.UTF8.GetString(result2.Buffer);
        Assert.Equal("efiLweN olleH", reply);

        // 4. 服务端队列：精确包含该消息（UDP 会话无断开语义，不检查 -1）
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
        var sessionsBefore = fixture.Server.SessionCount;
        var maxBefore = fixture.Server.MaxSessionCount;

        var uri = new NetUri($"tcp://127.0.0.1:{port}");
        var client = uri.CreateRemote();
        client.Name = "集成测试Tcp客户";
        client.Log = XTrace.Log;

        if (client is TcpSession tcp) tcp.MaxAsync = 0;
        client.Open();  // 明确触发 TCP 连接，确保服务端在断言前建立会话

        // 等待服务端完成会话建立
        await Task.Delay(50);
        Assert.Equal(sessionsBefore + 1, fixture.Server.SessionCount);
        var expectedMax = Math.Max(maxBefore, sessionsBefore + 1);
        Assert.Equal(expectedMax, fixture.Server.MaxSessionCount);

        // 1. 接收欢迎消息，精确验证整句结构（不多不少）
        //    格式：$"Welcome to visit {MachineName}!  [tcp://{ip}:{port}]\r\n"
        using var welcome = await client.ReceiveAsync(default).WaitAsync(TimeSpan.FromSeconds(5));
        var welcomeStr = welcome.ToStr();
        var expectedStart = $"Welcome to visit {Environment.MachineName}!  [tcp://127.0.0.1:";
        Assert.True(welcomeStr.StartsWith(expectedStart, StringComparison.Ordinal),
            $"欢迎词应以 '{expectedStart}' 开头，实际：'{welcomeStr}'");
        Assert.True(welcomeStr.EndsWith("]\r\n", StringComparison.Ordinal),
            $"欢迎词应以 ']\\r\\n' 结尾，实际：'{welcomeStr}'");
        var portPart = welcomeStr[expectedStart.Length..welcomeStr.IndexOf("]\r\n")];
        Assert.True(Int32.TryParse(portPart, out var clientPort) && clientPort is > 0 and <= 65535,
            $"欢迎词中端口号应为有效端口（1-65535），实际：'{portPart}'");

        // 2. 发送消息
        const String msg = "Hello NewLife";
        client.Send(msg);

        // 3. 接收回显，精确验证反转结果
        using var reply = await client.ReceiveAsync(default).WaitAsync(TimeSpan.FromSeconds(5));
        var replyStr = reply.ToStr();
        Assert.Equal("efiLweN olleH", replyStr);

        client.Close("Test04Done");

        // 4. 服务端队列：精确多了1条（不是「大于」）
        await Task.Delay(50);
        Assert.Equal(priorCount + 1, fixture.Server.ReceivedMessages.Count);
        Assert.Contains(msg, fixture.Server.ReceivedMessages);

        // 5. 服务端日志确认断开连接
        Assert.True(await fixture.ServerLog.WaitForAsync("断开"), "服务端日志应包含断开连接记录");

        // 断开后：SessionCount 精确回到原值；MaxSessionCount 保持不减
        await Task.Delay(150);
        Assert.Equal(sessionsBefore, fixture.Server.SessionCount);
        Assert.Equal(expectedMax, fixture.Server.MaxSessionCount);
    }
}


