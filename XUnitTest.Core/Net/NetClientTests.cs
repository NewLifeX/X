using System.Net.Sockets;
using NewLife;
using NewLife.Data;
using NewLife.Log;
using NewLife.Model;
using NewLife.Net;
using NewLife.Net.Handlers;
using Xunit;

namespace XUnitTest.Net;

/// <summary>NetClient 应用层网络客户端单元测试</summary>
[Collection("Net")]
public class NetClientTests
{
    #region 构造与属性

    [Fact]
    public void DefaultCtor_NameIsClassName()
    {
        using var client = new NetClient();
        Assert.Equal("NetClient", client.Name);
        Assert.Null(client.Remote);
        Assert.Null(client.Server);
        Assert.False(client.Active);
        Assert.Null(client.Client);
    }

    [Fact]
    public void StringCtor_ServerParsedToRemote()
    {
        using var client = new NetClient("tcp://127.0.0.1:8080");
        Assert.NotNull(client.Remote);
        Assert.Equal(NetType.Tcp, client.Remote!.Type);
        // IP 地址解析后存入 Address，域名才会写入 Host
        Assert.Equal("127.0.0.1", client.Remote.Address?.ToString());
        Assert.Equal(8080, client.Remote.Port);
    }

    [Fact]
    public void NetUriCtor_RemoteAssigned()
    {
        var uri = new NetUri("udp://127.0.0.1:9090");
        using var client = new NetClient(uri);
        Assert.Same(uri, client.Remote);
    }

    [Fact]
    public void Server_SetNull_ClearsRemote()
    {
        using var client = new NetClient("tcp://127.0.0.1:8080");
        Assert.NotNull(client.Remote);
        client.Server = null;
        Assert.Null(client.Remote);
        Assert.Null(client.Server);
    }

    [Fact]
    public void Server_SetEmpty_ClearsRemote()
    {
        using var client = new NetClient("tcp://127.0.0.1:8080");
        client.Server = "";
        Assert.Null(client.Remote);
    }

    [Fact]
    public void ToString_ReturnsRemoteOrName()
    {
        using var c1 = new NetClient();
        Assert.Equal("NetClient", c1.ToString());

        using var c2 = new NetClient("tcp://127.0.0.1:1234");
        Assert.Contains("1234", c2.ToString());
    }

    [Fact]
    public void DefaultPropertyValues()
    {
        using var client = new NetClient();
        Assert.Equal(3_000, client.Timeout);
        Assert.True(client.AutoReconnect);
        Assert.Equal(5_000, client.ReconnectDelay);
        Assert.Equal(0, client.MaxReconnect);
        Assert.Null(client.Pipeline);
        Assert.Null(client.Tracer);
    }

    #endregion

    #region 扩展数据

    [Fact]
    public void Items_LazyCreated()
    {
        using var client = new NetClient();
        var items = client.Items;
        Assert.NotNull(items);
        Assert.Same(items, client.Items);
    }

    [Fact]
    public void Indexer_GetSet()
    {
        using var client = new NetClient();
        Assert.Null(client["key1"]);
        client["key1"] = "value1";
        Assert.Equal("value1", client["key1"]);
        client["key1"] = null;
        Assert.Null(client["key1"]);
    }

    #endregion

    #region 日志

    [Fact]
    public void LogPrefix_DefaultFromName()
    {
        using var client = new NetClient();
        Assert.Equal("NetClient ", client.LogPrefix);
    }

    [Fact]
    public void LogPrefix_CustomSet()
    {
        using var client = new NetClient();
        client.LogPrefix = "[MyApp] ";
        Assert.Equal("[MyApp] ", client.LogPrefix);
    }

    [Fact]
    public void WriteLog_RoutesToLog()
    {
        var captured = new List<String>();
        var log = new ActionLog(msg => captured.Add(msg));

        using var client = new NetClient { Log = log };
        client.WriteLog("test {0}", 42);

        Assert.Single(captured);
        Assert.Contains("test 42", captured[0]);
    }

    #endregion

    #region 编解码器

    [Fact]
    public void Add_Instance_CreatesPipeline()
    {
        using var client = new NetClient();
        Assert.Null(client.Pipeline);
        var result = client.Add(new StandardCodec());
        Assert.NotNull(client.Pipeline);
        Assert.Same(client, result);
    }

    [Fact]
    public void Add_Generic_CreatesPipeline()
    {
        using var client = new NetClient();
        var result = client.Add<StandardCodec>();
        Assert.NotNull(client.Pipeline);
        Assert.Same(client, result);
    }

    [Fact]
    public void Add_Multiple_HandlersOrdered()
    {
        using var client = new NetClient();
        client.Add<StandardCodec>().Add<StandardCodec>();
        Assert.NotNull(client.Pipeline);
        Assert.Equal(2, ((Pipeline)client.Pipeline!).Handlers.Count);
    }

    #endregion

    #region 连接管理（不依赖真实服务端）

    [Fact]
    public void Open_NoRemote_Throws()
    {
        using var client = new NetClient();
        Assert.Throws<InvalidOperationException>(() => client.Open());
    }

    [Fact]
    public async Task OpenAsync_NoRemote_Throws()
    {
        using var client = new NetClient();
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.OpenAsync());
    }

    [Fact]
    public void Open_AfterDispose_ReturnsFalse()
    {
        var client = new NetClient("tcp://127.0.0.1:1");
        client.Dispose();
        Assert.False(client.Open());
    }

    [Fact]
    public void Close_WhenNotConnected_ReturnsTrueIdempotent()
    {
        using var client = new NetClient("tcp://127.0.0.1:1");
        Assert.True(client.Close("test"));
    }

    [Fact]
    public async Task CloseAsync_WhenNotConnected_ReturnsTrueIdempotent()
    {
        using var client = new NetClient("tcp://127.0.0.1:1");
        Assert.True(await client.CloseAsync("test"));
    }

    [Fact]
    public void Open_UnreachableHost_ReturnsFalse()
    {
        using var client = new NetClient("tcp://127.0.0.1:1") { Timeout = 500, AutoReconnect = false };
        Assert.False(client.Open());
        Assert.False(client.Active);
        Assert.Null(client.Client);
    }

    [Fact]
    public void Send_NotConnected_Throws()
    {
        using var client = new NetClient("tcp://127.0.0.1:1") { Timeout = 200, AutoReconnect = false };
        Assert.Throws<InvalidOperationException>(() => client.Send(new Byte[] { 1, 2, 3 }));
    }

    #endregion

    #region 真实 TCP 连接（Echo 服务端）

    [Fact]
    public void TcpOpenClose()
    {
        using var server = CreateEchoServer();
        using var client = CreateTcpClient(server.Port);

        Assert.True(client.Open());
        Assert.True(client.Active);
        Assert.NotNull(client.Client);

        Assert.True(client.Close("done"));
        Assert.False(client.Active);
    }

    [Fact]
    public void Open_WhenAlreadyActive_ReturnsTrueNoRebuild()
    {
        using var server = CreateEchoServer();
        using var client = CreateTcpClient(server.Port);

        client.Open();
        var firstInner = client.Client;

        Assert.True(client.Open());
        Assert.Same(firstInner, client.Client);
    }

    [Fact]
    public async Task OpenAsync_Success()
    {
        using var server = CreateEchoServer();
        using var client = CreateTcpClient(server.Port);

        Assert.True(await client.OpenAsync());
        Assert.True(client.Active);
    }

    [Fact]
    public async Task CloseAsync_Success()
    {
        using var server = CreateEchoServer();
        using var client = CreateTcpClient(server.Port);

        client.Open();
        Assert.True(await client.CloseAsync("done"));
        Assert.False(client.Active);
    }

    [Fact]
    public void Dispose_CleansUp()
    {
        using var server = CreateEchoServer();
        var client = CreateTcpClient(server.Port);
        client.Open();
        client.Dispose();

        Assert.False(client.Active);
        Assert.Null(client.Client);
        Assert.True(client.Disposed);
    }

    [Fact]
    public void SendReceive_ByteArray_Echo()
    {
        using var server = CreateEchoServer();
        using var client = CreateTcpClient(server.Port);

        var wait = new ManualResetEventSlim();
        Byte[]? received = null;
        client.Received += (s, e) => { received = e.GetBytes(); wait.Set(); };

        client.Open();

        var payload = new Byte[] { 0x01, 0x02, 0x03, 0x04 };
        client.Send(payload);

        Assert.True(wait.Wait(3_000), "超时未收到回声");
        Assert.Equal(payload, received);
    }

    [Fact]
    public void Send_ArraySegment_Echo()
    {
        using var server = CreateEchoServer();
        using var client = CreateTcpClient(server.Port);

        var wait = new ManualResetEventSlim();
        Byte[]? received = null;
        client.Received += (s, e) => { received = e.GetBytes(); wait.Set(); };

        client.Open();

        var buf = new Byte[] { 0xAA, 0x01, 0x02, 0x03, 0xBB };
        client.Send(new ArraySegment<Byte>(buf, 1, 3));

        Assert.True(wait.Wait(3_000), "超时未收到回声");
        Assert.Equal(new Byte[] { 0x01, 0x02, 0x03 }, received);
    }

    [Fact]
    public void Send_ReadOnlySpan_Echo()
    {
        using var server = CreateEchoServer();
        using var client = CreateTcpClient(server.Port);

        var wait = new ManualResetEventSlim();
        Byte[]? received = null;
        client.Received += (s, e) => { received = e.GetBytes(); wait.Set(); };

        client.Open();

        ReadOnlySpan<Byte> span = new Byte[] { 0x10, 0x20, 0x30 };
        client.Send(span);

        Assert.True(wait.Wait(3_000), "超时未收到回声");
        Assert.Equal(new Byte[] { 0x10, 0x20, 0x30 }, received);
    }

    [Fact]
    public async Task ReceiveAsync_EchoData()
    {
        using var server = CreateEchoServer();
        using var client = CreateTcpClient(server.Port);

        var tcs = new TaskCompletionSource<Byte[]?>();
        client.Received += (s, e) => tcs.TrySetResult(e.GetBytes());

        client.Open();

        var payload = new Byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        client.Send(payload);

        using var cts = new CancellationTokenSource(3_000);
        cts.Token.Register(() => tcs.TrySetCanceled());
        var result = await tcs.Task;
        Assert.Equal(payload, result);
    }

    [Fact]
    public void Received_EventDriven_Echo()
    {
        using var server = CreateEchoServer();
        using var client = CreateTcpClient(server.Port);

        var wait = new ManualResetEventSlim();
        Byte[]? received = null;

        client.Received += (s, e) =>
        {
            received = e.GetBytes();
            wait.Set();
        };

        client.Open();
        client.Send(new Byte[] { 1, 2, 3, 4, 5 });

        Assert.True(wait.Wait(3_000), "Received 事件超时未触发");
        Assert.NotNull(received);
        Assert.Equal(new Byte[] { 1, 2, 3, 4, 5 }, received);
    }

    [Fact]
    public void Received_Sender_IsNetClient()
    {
        using var server = CreateEchoServer();
        using var client = CreateTcpClient(server.Port);

        Object? capturedSender = null;
        var wait = new ManualResetEventSlim();

        client.Received += (s, e) => { capturedSender = s; wait.Set(); };

        client.Open();
        client.Send(new Byte[] { 0xFF });

        wait.Wait(3_000);
        Assert.Same(client, capturedSender);
    }

    [Fact]
    public void Opened_Event_FiredOnConnect()
    {
        using var server = CreateEchoServer();
        using var client = CreateTcpClient(server.Port);

        Object? capturedSender = null;
        var wait = new ManualResetEventSlim();

        client.Opened += (s, e) => { capturedSender = s; wait.Set(); };
        client.Open();

        Assert.True(wait.Wait(2_000), "Opened 事件未触发");
        Assert.Same(client, capturedSender);
    }

    [Fact]
    public void Closed_Event_FiredOnClose()
    {
        using var server = CreateEchoServer();
        using var client = CreateTcpClient(server.Port);

        var wait = new ManualResetEventSlim();
        Object? capturedSender = null;

        client.Closed += (s, e) => { capturedSender = s; wait.Set(); };
        client.Open();
        client.Close("test");

        Assert.True(wait.Wait(2_000), "Closed 事件未触发");
        Assert.Same(client, capturedSender);
    }

    [Fact]
    public void Pipeline_PassedToInnerClient()
    {
        using var server = CreateEchoServer();
        using var client = CreateTcpClient(server.Port);
        client.Add<StandardCodec>();

        client.Open();

        Assert.NotNull(client.Pipeline);
        Assert.Same(client.Pipeline, client.Client!.Pipeline);
    }

    [Fact]
    public void Tracer_PassedToInnerClient()
    {
        using var server = CreateEchoServer();
        using var client = CreateTcpClient(server.Port);

        var tracer = new DefaultTracer();
        client.Tracer = tracer;
        client.Open();

        Assert.Same(tracer, client.Client!.Tracer);
    }

    [Fact]
    public void Name_And_Timeout_PassedToInnerClient()
    {
        using var server = CreateEchoServer();
        using var client = CreateTcpClient(server.Port);
        client.Name = "MyTestClient";
        client.Timeout = 2_000;

        client.Open();

        Assert.Equal("MyTestClient", client.Client!.Name);
        Assert.Equal(2_000, client.Client.Timeout);
    }

    #endregion

    #region 断线重连

    [Fact]
    public void Close_NoAutoReconnect_AfterUserClose()
    {
        using var server = CreateEchoServer();
        using var client = CreateTcpClient(server.Port);
        client.AutoReconnect = true;
        client.ReconnectDelay = 200;

        client.Open();

        var openCount = 0;
        var reconnected = false;
        client.Opened += (s, e) =>
        {
            if (Interlocked.Increment(ref openCount) > 1) reconnected = true;
        };

        client.Close("user close");
        Thread.Sleep(500);

        Assert.False(reconnected, "主动关闭后不应触发重连");
    }

    [Fact]
    public void AutoReconnect_Disabled_NoReconnect()
    {
        using var server = CreateEchoServer();
        using var client = CreateTcpClient(server.Port);
        client.AutoReconnect = false;
        client.ReconnectDelay = 200;

        client.Open();

        var openCount = 0;
        var reconnectCount = 0;
        client.Opened += (s, e) =>
        {
            if (Interlocked.Increment(ref openCount) > 1) Interlocked.Increment(ref reconnectCount);
        };

        server.Stop("test");
        Thread.Sleep(500);

        Assert.Equal(0, reconnectCount);
    }

    [Fact]
    public void MaxReconnect_StopsAfterLimit()
    {
        using var client = new FakeReconnectClient("tcp://127.0.0.1:1")
        {
            AutoReconnect = true,
            MaxReconnect = 2,
        };
        client.SimulateReconnect(2);
        Assert.False(client.CanScheduleReconnect(), "超过最大重连次数后不应再调度");
    }

    [Fact]
    public void MaxReconnect_Zero_AlwaysAllowed()
    {
        using var client = new FakeReconnectClient("tcp://127.0.0.1:1")
        {
            AutoReconnect = true,
            MaxReconnect = 0,
        };
        client.SimulateReconnect(100);
        Assert.True(client.CanScheduleReconnect(), "MaxReconnect=0 时始终允许重连");
    }

    [Fact]
    public void Dispose_PreventsReconnect()
    {
        using var server = CreateEchoServer();
        var client = CreateTcpClient(server.Port);
        client.AutoReconnect = true;
        client.ReconnectDelay = 100;

        client.Open();

        var openCount = 0;
        var reconnected = false;
        client.Opened += (s, e) =>
        {
            if (Interlocked.Increment(ref openCount) > 1) reconnected = true;
        };

        client.Dispose();
        Thread.Sleep(300);

        Assert.False(reconnected, "Dispose 后不应触发重连");
    }

    [Fact]
    public void AutoReconnect_ReconnectsAfterServerRestart()
    {
        var server = CreateEchoServer();
        var port = server.Port;

        using var client = new NetClient($"tcp://127.0.0.1:{port}")
        {
            Timeout = 3_000,
            AutoReconnect = true,
            ReconnectDelay = 300,
        };

        client.Open();
        Assert.True(client.Active);

        var openCount = 0;
        var reconnectWait = new ManualResetEventSlim();
        client.Opened += (s, e) =>
        {
            // 注册 handler 在首次 Open 之后，所以任意触发即为重连
            if (Interlocked.Increment(ref openCount) > 0) reconnectWait.Set();
        };

        server.Stop("restart test");
        server.Dispose();
        Thread.Sleep(300);

        using var server2 = new NetServer
        {
            Port = port,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            ReuseAddress = true,
        };
        server2.Start();

        var ok = reconnectWait.Wait(5_000);
        server2.Stop("done");

        Assert.True(ok, "未能在超时时间内成功重连");
    }

    #endregion

    #region CreateClient 扩展点

    [Fact]
    public void CreateClient_Overridable()
    {
        using var server = CreateEchoServer();
        using var custom = new CustomNetClient(server.Port);

        custom.Open();

        Assert.True(custom.CustomCreateClientCalled);
        Assert.True(custom.Active);
    }

    #endregion

    #region 辅助类型

    private static NetServer CreateEchoServer()
    {
        var server = new NetServer<EchoSession>
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            UseSession = false,
        };
        server.Start();
        return server;
    }

    private static NetClient CreateTcpClient(Int32 port) =>
        new($"tcp://127.0.0.1:{port}") { Timeout = 3_000, AutoReconnect = false };

    private class EchoSession : NetSession<NetServer<EchoSession>>
    {
        protected override void OnReceive(ReceivedEventArgs e)
        {
            var pk = e.Packet;
            if (pk != null && pk.Length > 0) Send(pk);
        }
    }

    private class ActionLog : Logger
    {
        private readonly Action<String> _action;

        public ActionLog(Action<String> action) => _action = action;

        protected override void OnWrite(LogLevel level, String format, params Object?[] args)
        {
            var msg = args == null || args.Length == 0 ? format : String.Format(format, args);
            _action(msg);
        }
    }

    private class FakeReconnectClient : NetClient
    {
        private Int32 _count;

        public FakeReconnectClient(String server) : base(server) { }

        public void SimulateReconnect(Int32 count) => _count = count;

        public Boolean CanScheduleReconnect()
        {
            if (!AutoReconnect || Disposed) return false;
            if (MaxReconnect > 0 && _count >= MaxReconnect) return false;
            return true;
        }
    }

    private class CustomNetClient : NetClient
    {
        public Boolean CustomCreateClientCalled { get; private set; }

        public CustomNetClient(Int32 port) : base($"tcp://127.0.0.1:{port}") { }

        protected override ISocketClient CreateClient()
        {
            CustomCreateClientCalled = true;
            return base.CreateClient();
        }
    }

    #endregion
}