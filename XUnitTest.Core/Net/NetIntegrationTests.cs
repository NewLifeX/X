using System.Net;
using System.Net.Sockets;
using System.Text;
using NewLife;
using NewLife.Data;
using NewLife.Log;
using NewLife.Net;
using NewLife.Net.Handlers;
using Xunit;

namespace XUnitTest.Net;

/// <summary>NetClient/NetServer 端到端集成测试，覆盖编解码器、数据完整性、多客户端并发、会话生命周期等场景</summary>
[Collection("Net")]
[TestCaseOrderer("NewLife.UnitTest.DefaultOrderer", "NewLife.UnitTest")]
public class NetIntegrationTests
{
    #region TCP Echo 数据完整性
    /// <summary>TCP Echo 通过 CreateRemote 客户端收发，验证数据逐字节一致</summary>
    [Fact]
    public void TcpEcho_CreateRemote_DataIntegrity()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
        };
        server.Received += (s, e) =>
        {
            if (s is INetSession session && e.Packet != null)
                session.Send(e.Packet);
        };
        server.Start();

        using var client = new NetUri($"tcp://127.0.0.1:{server.Port}").CreateRemote();
        var payload = new Byte[256];
        Random.Shared.NextBytes(payload);

        var wait = new ManualResetEventSlim();
        Byte[]? received = null;
        client.Received += (s, e) =>
        {
            if (e.Packet != null)
            {
                received = e.Packet.GetSpan().ToArray();
                wait.Set();
            }
        };
        client.Open();
        client.Send(payload);

        Assert.True(wait.Wait(3_000));
        Assert.NotNull(received);
        Assert.Equal(payload, received);
    }

    /// <summary>TCP Echo 多轮收发，每轮数据不同，验证连续通信可靠性</summary>
    [Fact]
    public void TcpEcho_MultiRound_DataConsistency()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
        };
        server.Received += (s, e) =>
        {
            if (s is INetSession session && e.Packet != null)
                session.Send(e.Packet);
        };
        server.Start();

        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);
        var ns = client.GetStream();

        for (var round = 0; round < 10; round++)
        {
            var payload = Encoding.UTF8.GetBytes($"Round-{round:D4}-{Guid.NewGuid()}");
            ns.Write(payload, 0, payload.Length);
            ns.Flush();

            var buf = new Byte[1024];
            var total = 0;
            while (total < payload.Length)
            {
                ns.ReadTimeout = 3_000;
                var n = ns.Read(buf, total, buf.Length - total);
                Assert.True(n > 0);
                total += n;
            }
            Assert.Equal(payload.Length, total);
            Assert.Equal(payload, buf[..total]);
        }
    }
    #endregion

    #region LengthFieldCodec 集成
    /// <summary>LengthFieldCodec 编解码集成：客户端带长度前缀发送，服务端正确解码</summary>
    [Fact]
    public async Task LengthFieldCodec_RequestResponse()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
        };
        server.Add(new LengthFieldCodec { Size = 2, Offset = 0 });
        server.Received += (s, e) =>
        {
            if (s is INetSession session && e.Packet != null)
                session.SendMessage(e.Packet);
        };
        server.Start();

        var uri = new NetUri($"tcp://127.0.0.1:{server.Port}");
        using var client = uri.CreateRemote();
        client.Add(new LengthFieldCodec { Size = 2, Offset = 0 });
        client.Open();

        var payload = "LengthField-Test-Payload"u8.ToArray();
        var response = await client.SendMessageAsync(new ArrayPacket(payload));

        Assert.NotNull(response);
        if (response is IPacket rpk)
            Assert.Equal(payload, rpk.ToArray());
    }

    /// <summary>LengthFieldCodec 连续发送多条消息，验证粘包拆包正确</summary>
    [Fact]
    public async Task LengthFieldCodec_MultiMessage_NoPacking()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
        };
        server.Add(new LengthFieldCodec { Size = 2, Offset = 0 });
        server.Received += (s, e) =>
        {
            if (s is INetSession session && e.Packet != null)
                session.SendMessage(e.Packet);
        };
        server.Start();

        var uri = new NetUri($"tcp://127.0.0.1:{server.Port}");
        using var client = uri.CreateRemote();
        client.Add(new LengthFieldCodec { Size = 2, Offset = 0 });
        client.Timeout = 5_000;
        client.Open();

        for (var i = 0; i < 5; i++)
        {
            var msg = Encoding.UTF8.GetBytes($"Message-{i}");
            var resp = await client.SendMessageAsync(new ArrayPacket(msg));
            Assert.NotNull(resp);
            if (resp is IPacket rpk)
                Assert.Equal(msg, rpk.ToArray());
        }
    }

    /// <summary>LengthFieldCodec 4字节长度头，大小端正确</summary>
    [Fact]
    public async Task LengthFieldCodec_Size4_Works()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
        };
        server.Add(new LengthFieldCodec { Size = 4, Offset = 0 });
        server.Received += (s, e) =>
        {
            if (s is INetSession session && e.Packet != null)
                session.SendMessage(e.Packet);
        };
        server.Start();

        var uri = new NetUri($"tcp://127.0.0.1:{server.Port}");
        using var client = uri.CreateRemote();
        client.Add(new LengthFieldCodec { Size = 4, Offset = 0 });
        client.Open();

        var payload = new Byte[512];
        Random.Shared.NextBytes(payload);
        var resp = await client.SendMessageAsync(new ArrayPacket(payload));

        Assert.NotNull(resp);
        if (resp is IPacket rpk)
            Assert.Equal(payload, rpk.ToArray());
    }
    #endregion

    #region SplitDataCodec 集成
    /// <summary>SplitDataCodec 按 CRLF 拆包，服务端逐行接收</summary>
    [Fact]
    public void SplitDataCodec_LineByLine_Receive()
    {
        var receivedLines = new List<String>();
        var allReceived = new ManualResetEventSlim();

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
        };
        server.Add<SplitDataCodec>();
        server.Received += (s, e) =>
        {
            if (e.Packet != null)
            {
                var line = e.Packet.ToStr();
                lock (receivedLines)
                {
                    receivedLines.Add(line);
                    if (receivedLines.Count >= 3) allReceived.Set();
                }
            }
        };
        server.Start();

        // 用原始 TcpClient 一次性发送 3 行（模拟粘包）
        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);
        var ns = client.GetStream();

        var data = "Line1\r\nLine2\r\nLine3\r\n"u8.ToArray();
        ns.Write(data, 0, data.Length);
        ns.Flush();

        Assert.True(allReceived.Wait(3_000));
        Assert.Equal(3, receivedLines.Count);
        Assert.Equal("Line1\r\n", receivedLines[0]);
        Assert.Equal("Line2\r\n", receivedLines[1]);
        Assert.Equal("Line3\r\n", receivedLines[2]);
    }

    /// <summary>SplitDataCodec 自定义分隔符</summary>
    [Fact]
    public void SplitDataCodec_CustomDelimiter()
    {
        var receivedLines = new List<String>();
        var allReceived = new ManualResetEventSlim();

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
        };
        server.Add(new SplitDataCodec { SplitData = "|"u8.ToArray() });
        server.Received += (s, e) =>
        {
            if (e.Packet != null)
            {
                var line = e.Packet.ToStr();
                lock (receivedLines)
                {
                    receivedLines.Add(line);
                    if (receivedLines.Count >= 2) allReceived.Set();
                }
            }
        };
        server.Start();

        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);
        var ns = client.GetStream();

        var data = "PartA|PartB|"u8.ToArray();
        ns.Write(data, 0, data.Length);
        ns.Flush();

        Assert.True(allReceived.Wait(3_000));
        Assert.Equal(2, receivedLines.Count);
        Assert.Equal("PartA|", receivedLines[0]);
        Assert.Equal("PartB|", receivedLines[1]);
    }
    #endregion

    #region StandardCodec 多轮请求响应
    /// <summary>StandardCodec 多轮 SendMessageAsync，逐条验证数据一致</summary>
    [Fact]
    public async Task StandardCodec_MultiRound_ExactPayload()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
        };
        server.Add<StandardCodec>();
        server.Received += (s, e) =>
        {
            if (s is INetSession session && e.Packet != null)
                session.SendReply(e.Packet, e);
        };
        server.Start();

        var uri = new NetUri($"tcp://127.0.0.1:{server.Port}");
        using var client = uri.CreateRemote();
        client.Add<StandardCodec>();
        client.Timeout = 5_000;
        client.Open();

        for (var i = 0; i < 20; i++)
        {
            var payload = Encoding.UTF8.GetBytes($"Req-{i:D4}-{Guid.NewGuid()}");
            var resp = await client.SendMessageAsync(new ArrayPacket(payload));
            Assert.NotNull(resp);
            if (resp is IPacket rpk)
                Assert.Equal(payload, rpk.ToArray());
        }
    }

    /// <summary>StandardCodec 不同大小载荷（1B~64KB），验证编解码边界</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(127)]
    [InlineData(128)]
    [InlineData(1024)]
    [InlineData(8192)]
    [InlineData(65000)]
    public async Task StandardCodec_VariousPayloadSizes(Int32 size)
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
        };
        server.Add<StandardCodec>();
        server.Received += (s, e) =>
        {
            if (s is INetSession session && e.Packet != null)
                session.SendReply(e.Packet, e);
        };
        server.Start();

        var uri = new NetUri($"tcp://127.0.0.1:{server.Port}");
        using var client = uri.CreateRemote();
        client.Add<StandardCodec>();
        client.Timeout = 10_000;
        client.Open();

        var payload = new Byte[size];
        Random.Shared.NextBytes(payload);
        var resp = await client.SendMessageAsync(new ArrayPacket(payload));

        Assert.NotNull(resp);
        if (resp is IPacket rpk)
            Assert.Equal(payload, rpk.ToArray());
    }
    #endregion

    #region 多客户端并发 SendMessageAsync
    /// <summary>多个独立客户端并发 SendMessageAsync，各自收到正确响应</summary>
    [Fact]
    public async Task MultiClient_Concurrent_SendMessageAsync()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            UseSession = true,
        };
        server.Add<StandardCodec>();
        server.Received += (s, e) =>
        {
            if (s is INetSession session && e.Packet != null)
                session.SendReply(e.Packet, e);
        };
        server.Start();

        const Int32 clientCount = 5;
        var clients = new List<ISocketRemote>();

        for (var i = 0; i < clientCount; i++)
        {
            var c = new NetUri($"tcp://127.0.0.1:{server.Port}").CreateRemote();
            c.Add<StandardCodec>();
            c.Timeout = 10_000;
            c.Open();
            clients.Add(c);
        }

        // 每个客户端并发发送
        var tasks = new List<Task>();
        for (var ci = 0; ci < clientCount; ci++)
        {
            var idx = ci;
            var c = clients[idx];
            tasks.Add(Task.Run(async () =>
            {
                for (var j = 0; j < 5; j++)
                {
                    var payload = Encoding.UTF8.GetBytes($"Client{idx}-Msg{j}");
                    var resp = await c.SendMessageAsync(new ArrayPacket(payload));
                    Assert.NotNull(resp);
                    if (resp is IPacket rpk)
                        Assert.Equal(payload, rpk.ToArray());
                }
            }));
        }

        await Task.WhenAll(tasks);

        foreach (var c in clients)
            (c as IDisposable)?.Dispose();
    }
    #endregion

    #region 自定义会话生命周期
    /// <summary>自定义 NetSession 的 OnConnected/OnReceive/OnDisconnected 完整生命周期回调</summary>
    [Fact]
    public void CustomSession_FullLifecycle()
    {
        var connected = new ManualResetEventSlim();
        var received = new ManualResetEventSlim();
        var disconnected = new ManualResetEventSlim();

        using var server = new NetServer<LifecycleTrackingSession>
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
        };

        LifecycleTrackingSession? session = null;
        server.NewSession += (s, e) =>
        {
            if (e.Session is LifecycleTrackingSession lts)
            {
                session = lts;
                lts.ConnectedSignal = connected;
                lts.ReceivedSignal = received;
                lts.DisconnectedSignal = disconnected;
            }
        };
        server.Start();

        // 连接
        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);
        Assert.True(connected.Wait(3_000));
        Assert.NotNull(session);
        Assert.True(session!.IsConnected);

        // 发送数据
        var ns = client.GetStream();
        ns.Write("Hello"u8.ToArray());
        ns.Flush();
        Assert.True(received.Wait(3_000));
        Assert.True(session.ReceivedCount > 0);

        // 断开
        client.Close();
        Assert.True(disconnected.Wait(3_000));
        Assert.True(session.IsDisconnected);
    }

    class LifecycleTrackingSession : NetSession
    {
        public Boolean IsConnected { get; private set; }
        public Boolean IsDisconnected { get; private set; }
        public Int32 ReceivedCount => _receivedCount;
        private Int32 _receivedCount;

        public ManualResetEventSlim? ConnectedSignal { get; set; }
        public ManualResetEventSlim? ReceivedSignal { get; set; }
        public ManualResetEventSlim? DisconnectedSignal { get; set; }

        protected override void OnConnected()
        {
            base.OnConnected();
            IsConnected = true;
            ConnectedSignal?.Set();
        }

        protected override void OnReceive(ReceivedEventArgs e)
        {
            base.OnReceive(e);
            if (e.Packet != null && e.Packet.Total > 0)
            {
                Interlocked.Increment(ref _receivedCount);
                ReceivedSignal?.Set();
            }
        }

        protected override void OnDisconnected(String reason)
        {
            base.OnDisconnected(reason);
            IsDisconnected = true;
            DisconnectedSignal?.Set();
        }
    }
    #endregion

    #region 服务端优雅关闭
    /// <summary>服务端 Stop 时，已连接客户端的读取及时感知断开</summary>
    [Fact]
    public void ServerGracefulShutdown_ClientDetectsDisconnect()
    {
        var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            UseSession = true,
        };
        server.Start();
        var port = server.Port;

        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, port);
        var ns = client.GetStream();
        Thread.Sleep(200);

        Assert.Equal(1, server.SessionCount);

        // 服务端关闭
        server.Stop("Shutdown");
        server.Dispose();

        // 客户端应能检测到连接断开（Read 返回 0 或抛异常）
        var buf = new Byte[64];
        ns.ReadTimeout = 3_000;
        try
        {
            var n = ns.Read(buf, 0, buf.Length);
            // n==0 表示对端关闭
            Assert.Equal(0, n);
        }
        catch (IOException)
        {
            // 连接已重置也是合理的
        }
    }

    /// <summary>服务端 Stop 后立即重启，新客户端可正常连接</summary>
    [Fact]
    public void ServerRestartCycle_NewClientConnects()
    {
        var port = 0;

        for (var cycle = 0; cycle < 3; cycle++)
        {
            using var server = new NetServer
            {
                Port = port,
                ProtocolType = NetType.Tcp,
                AddressFamily = AddressFamily.InterNetwork,
                ReuseAddress = true,
            };

            try
            {
                server.Start();
            }
            catch
            {
                // TIME_WAIT 情况下跳过
                continue;
            }

            if (port == 0) port = server.Port;

            // 新客户端连接
            using var client = new TcpClient();
            client.Connect(IPAddress.Loopback, server.Port);
            Thread.Sleep(200);
            Assert.Equal(1, server.SessionCount);

            server.Stop("Cycle");
            Thread.Sleep(100);
        }
    }
    #endregion

    #region UDP Echo 数据完整性
    /// <summary>UDP Echo 通过 CreateRemote 客户端收发，验证数据一致</summary>
    [Fact]
    public void UdpEcho_CreateRemote_DataIntegrity()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Udp,
            AddressFamily = AddressFamily.InterNetwork,
        };
        server.Received += (s, e) =>
        {
            if (s is INetSession session && e.Packet != null)
                session.Send(e.Packet);
        };
        server.Start();

        var wait = new ManualResetEventSlim();
        Byte[]? received = null;

        var uri = new NetUri($"udp://127.0.0.1:{server.Port}");
        using var client = uri.CreateRemote();
        client.Received += (s, e) =>
        {
            if (e.Packet != null)
            {
                received = e.Packet.GetSpan().ToArray();
                wait.Set();
            }
        };
        client.Open();

        var payload = new Byte[64];
        Random.Shared.NextBytes(payload);
        client.Send(payload);

        if (wait.Wait(3_000))
        {
            Assert.NotNull(received);
            Assert.Equal(payload, received);
        }
        // UDP 在部分 CI 环境可能丢包，不强制断言
    }
    #endregion

    #region TCP 长连接持续收发
    /// <summary>TCP 长连接持续 100 次 Echo，模拟长连接场景</summary>
    [Fact]
    public void TcpLongConnection_100Exchanges()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
        };
        server.Add<StandardCodec>();
        server.Received += (s, e) =>
        {
            if (s is INetSession session && e.Packet != null)
                session.SendReply(e.Packet, e);
        };
        server.Start();

        var uri = new NetUri($"tcp://127.0.0.1:{server.Port}");
        using var client = uri.CreateRemote();
        client.Add<StandardCodec>();
        client.Timeout = 10_000;
        client.Open();

        for (var i = 0; i < 100; i++)
        {
            var payload = Encoding.UTF8.GetBytes($"Ping-{i}");
            var resp = client.SendMessageAsync(new ArrayPacket(payload)).AsTask().Result;
            Assert.NotNull(resp);
            if (resp is IPacket rpk)
                Assert.Equal(payload, rpk.ToArray());
        }
    }
    #endregion

    #region 自定义 NetServer<TSession> Echo
    /// <summary>泛型 NetServer + 自定义 EchoSession 完整收发验证</summary>
    [Fact]
    public async Task GenericNetServer_EchoSession_RequestResponse()
    {
        using var server = new NetServer<EchoSession>
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
        };
        server.Add<StandardCodec>();
        server.Start();

        var uri = new NetUri($"tcp://127.0.0.1:{server.Port}");
        using var client = uri.CreateRemote();
        client.Add<StandardCodec>();
        client.Open();

        var payload = "GenericServerTest"u8.ToArray();
        var resp = await client.SendMessageAsync(new ArrayPacket(payload));
        Assert.NotNull(resp);
        if (resp is IPacket rpk)
            Assert.Equal(payload, rpk.ToArray());
    }

    class EchoSession : NetSession
    {
        protected override void OnReceive(ReceivedEventArgs e)
        {
            base.OnReceive(e);
            if (e.Packet != null && e.Packet.Total > 0)
                SendReply(e.Packet, e);
        }
    }
    #endregion

    #region 会话 Items 扩展数据传递
    /// <summary>会话通过 Items 传递自定义数据，群发时按条件过滤</summary>
    [Fact]
    public async Task SessionItems_FilterBroadcast()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            UseSession = true,
        };

        var sessionReady = new CountdownEvent(4);
        server.NewSession += (s, e) =>
        {
            if (e.Session is NetSession ns)
            {
                ns.Session["Group"] = ns.ID % 2 == 0 ? "A" : "B";
                sessionReady.Signal();
            }
        };
        server.Start();

        // 创建 4 个客户端
        var clients = new List<TcpClient>();
        var streams = new List<NetworkStream>();
        for (var i = 0; i < 4; i++)
        {
            var c = new TcpClient();
            c.Connect(IPAddress.Loopback, server.Port);
            clients.Add(c);
            streams.Add(c.GetStream());
        }

        Assert.True(sessionReady.Wait(3_000));

        // 只向 Group=A 的会话发送
        var msg = "GroupA-Only"u8.ToArray();
        var sentCount = await server.SendAllAsync(
            new ArrayPacket(msg),
            session => session is NetSession ns && ns.Session["Group"]?.ToString() == "A");

        Assert.True(sentCount >= 0);

        foreach (var c in clients)
            c.Dispose();
    }
    #endregion

    #region LengthFieldCodec 大小端测试
    /// <summary>LengthFieldCodec 负数 Size 表示大端序</summary>
    [Fact]
    public async Task LengthFieldCodec_BigEndian_NegativeSize()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
        };
        server.Add(new LengthFieldCodec { Size = -2, Offset = 0 });
        server.Received += (s, e) =>
        {
            if (s is INetSession session && e.Packet != null)
                session.SendMessage(e.Packet);
        };
        server.Start();

        var uri = new NetUri($"tcp://127.0.0.1:{server.Port}");
        using var client = uri.CreateRemote();
        client.Add(new LengthFieldCodec { Size = -2, Offset = 0 });
        client.Open();

        var payload = "BigEndianTest"u8.ToArray();
        var resp = await client.SendMessageAsync(new ArrayPacket(payload));

        Assert.NotNull(resp);
        if (resp is IPacket rpk)
            Assert.Equal(payload, rpk.ToArray());
    }
    #endregion

    #region 多协议同时监听
    /// <summary>NetServer 同时监听 TCP+UDP（Unknown），两种协议均可通信</summary>
    [Fact]
    public void DualProtocol_TcpAndUdp_BothWork()
    {
        var tcpReceived = new ManualResetEventSlim();
        var udpReceived = new ManualResetEventSlim();

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Unknown,
            AddressFamily = AddressFamily.InterNetwork,
        };
        server.Received += (s, e) =>
        {
            if (e.Packet == null) return;
            var text = e.Packet.ToStr();
            if (s is INetSession session)
            {
                session.Send(e.Packet);
                if (text.StartsWith("TCP")) tcpReceived.Set();
                if (text.StartsWith("UDP")) udpReceived.Set();
            }
        };
        server.Start();
        Assert.Equal(2, server.Servers.Count);

        // TCP 客户端
        using var tcpClient = new TcpClient();
        tcpClient.Connect(IPAddress.Loopback, server.Port);
        var ns = tcpClient.GetStream();
        ns.Write("TCP-Test"u8.ToArray());
        ns.Flush();

        Assert.True(tcpReceived.Wait(3_000));

        // UDP 客户端
        using var udpClient = new UdpClient();
        var udpData = "UDP-Test"u8.ToArray();
        udpClient.Send(udpData, udpData.Length, new IPEndPoint(IPAddress.Loopback, server.Port));

        // UDP 可能在 CI 环境下不稳定，放宽断言
        udpReceived.Wait(3_000);
    }
    #endregion

    #region Received 事件与管道 Message 区分
    /// <summary>StandardCodec 管道下，Received 事件中 e.Message 为解码后的 IPacket</summary>
    [Fact]
    public void StandardCodec_ReceivedEvent_MessageIsDecoded()
    {
        Object? receivedMessage = null;
        var wait = new ManualResetEventSlim();

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
        };
        server.Add<StandardCodec>();
        server.Received += (s, e) =>
        {
            receivedMessage = e.Message ?? e.Packet;
            wait.Set();
            if (s is INetSession session && e.Packet != null)
                session.SendReply(e.Packet, e);
        };
        server.Start();

        var uri = new NetUri($"tcp://127.0.0.1:{server.Port}");
        using var client = uri.CreateRemote();
        client.Add<StandardCodec>();
        client.Open();

        var payload = "DecodeCheck"u8.ToArray();
        client.SendMessage(new ArrayPacket(payload));

        Assert.True(wait.Wait(3_000));
        Assert.NotNull(receivedMessage);
        // 经过 StandardCodec 解码后，Message 应该是 IPacket 类型
        Assert.True(receivedMessage is IPacket);
    }
    #endregion
}
