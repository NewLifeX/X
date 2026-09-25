using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NewLife;
using NewLife.Data;
using NewLife.Log;
using NewLife.Model;
using NewLife.Net;
using NewLife.Messaging;
using Xunit;

namespace XUnitTest.Net;

/// <summary>NetServer网络服务器单元测试</summary>
[Collection("Net")]
[TestCaseOrderer("NewLife.UnitTest.DefaultOrderer", "NewLife.UnitTest")]
public class NetServerTests
{
    #region 基础功能测试
    /// <summary>测试TCP服务器基本启动停止</summary>
    [Fact]
    public void TcpServerStartStop()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork, // 仅IPv4
            Log = XTrace.Log,
        };

        server.Start();

        Assert.True(server.Active);
        Assert.True(server.Port > 0);
        Assert.Single(server.Servers);

        server.Stop("Test");

        Assert.False(server.Active);
    }

    /// <summary>测试UDP服务器基本启动停止</summary>
    [Fact]
    public void UdpServerStartStop()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Udp,
            AddressFamily = AddressFamily.InterNetwork, // 仅IPv4
            Log = XTrace.Log,
        };

        server.Start();

        // UDP服务器可能在某些环境下行为不同，验证基本属性
        Assert.True(server.Port > 0);
        Assert.NotEmpty(server.Servers);

        server.Stop("Test");
    }

    /// <summary>测试同时监听TCP和UDP</summary>
    [Fact]
    public void TcpUdpServerStartStop()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Unknown,
            AddressFamily = AddressFamily.InterNetwork,
            Log = XTrace.Log,
        };

        server.Start();

        Assert.True(server.Active);
        Assert.True(server.Port > 0);
        Assert.Equal(2, server.Servers.Count); // TCP + UDP

        server.Stop("Test");

        Assert.False(server.Active);
    }

    /// <summary>测试服务器配置属性</summary>
    [Fact]
    public void ServerConfiguration()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            SessionTimeout = 120,
            UseSession = true,
            ReuseAddress = true,
            StatPeriod = 0,
            Log = XTrace.Log,
        };

        Assert.Equal(120, server.SessionTimeout);
        Assert.True(server.UseSession);
        Assert.True(server.ReuseAddress);
        Assert.Equal(0, server.StatPeriod);
    }

    /// <summary>测试服务器名称自动生成</summary>
    [Fact]
    public void ServerNameGeneration()
    {
        using var server = new NetServer();

        // 默认名称应该是类名去掉Server后缀
        Assert.Equal("Net", server.Name);

        server.Name = "MyServer";
        Assert.Equal("MyServer", server.Name);
    }
    #endregion

    #region 数据收发测试
    /// <summary>测试TCP数据收发</summary>
    [Fact]
    public void TcpDataTransfer()
    {
        var receivedData = new List<Byte[]>();
        var receivedEvent = new ManualResetEventSlim(false);

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
            SessionLog = XTrace.Log,
        };

        server.Received += (s, e) =>
        {
            if (e.Packet != null)
            {
                receivedData.Add(e.GetBytes());
                receivedEvent.Set();

                // Echo回复
                if (s is INetSession session)
                    session.Send(e.Packet);
            }
        };

        server.Start();

        // 客户端连接
        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);

        var ns = client.GetStream();
        var sendData = "Hello NewLife.Net"u8.ToArray();
        ns.Write(sendData, 0, sendData.Length);

        // 等待接收
        Assert.True(receivedEvent.Wait(3000));
        Assert.Single(receivedData);
        Assert.Equal(sendData, receivedData[0]);

        // 接收Echo回复
        var buf = new Byte[1024];
        var len = ns.Read(buf, 0, buf.Length);
        Assert.Equal(sendData.Length, len);
        Assert.Equal(sendData, buf[..len]);
    }

    /// <summary>测试UDP数据收发</summary>
    [Fact(DisplayName = "UDP_数据收发双向字节一致")]
    public void UdpDataTransfer()
    {
        var receivedData = new List<Byte[]>();
        var receivedEvent = new ManualResetEventSlim(false);

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Udp,
            AddressFamily = AddressFamily.InterNetwork,
            Log = XTrace.Log,
            SessionLog = XTrace.Log,
        };

        server.Received += (s, e) =>
        {
            if (e.Packet != null)
            {
                receivedData.Add(e.GetBytes());
                receivedEvent.Set();

                // Echo回复
                if (s is INetSession session)
                    session.Send(e.Packet);
            }
        };

        server.Start();

        // 确保服务器已启动
        Assert.NotEmpty(server.Servers);
        Assert.True(server.Port > 0);

        // 客户端发送
        using var client = new UdpClient();
        var serverEp = new IPEndPoint(IPAddress.Loopback, server.Port);
        var sendData = "Hello UDP"u8.ToArray();
        client.Send(sendData, sendData.Length, serverEp);

        // 回环UDP必须收到，未收到即失败
        Assert.True(receivedEvent.Wait(5000), "未在超时内收到UDP数据");
        Assert.Single(receivedData);
        Assert.Equal(sendData, receivedData[0]);

        // 客户端读取Echo回复，验证双向
        client.Client.ReceiveTimeout = 5000;
        var echoEp = new IPEndPoint(IPAddress.Any, 0);
        var echo = client.Receive(ref echoEp);
        Assert.Equal(sendData, echo);
    }

    /// <summary>测试多次数据发送</summary>
    [Fact]
    public void MultipleDataTransfer()
    {
        var receivedCount = 0;
        var allReceived = new ManualResetEventSlim(false);

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
        };

        server.Received += (s, e) =>
        {
            if (e.Packet != null && e.Packet.Total > 0)
            {
                if (Interlocked.Increment(ref receivedCount) >= 3)
                    allReceived.Set();
            }
        };

        server.Start();

        // 客户端连接并多次发送
        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);
        var ns = client.GetStream();

        for (var i = 0; i < 3; i++)
        {
            var data = Encoding.UTF8.GetBytes($"Message {i}\n");
            ns.Write(data, 0, data.Length);
            ns.Flush();
            Thread.Sleep(100);
        }

        // 等待接收（可能由于粘包合并为较少次数接收）
        Thread.Sleep(1000);
        Assert.True(receivedCount >= 1);
    }
    #endregion

    #region 会话管理测试
    /// <summary>测试会话创建和管理</summary>
    [Fact]
    public void SessionManagement()
    {
        var sessionCreated = new ManualResetEventSlim(false);
        INetSession? createdSession = null;

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            UseSession = true,
            Log = XTrace.Log,
        };

        server.NewSession += (s, e) =>
        {
            createdSession = e.Session;
            sessionCreated.Set();
        };

        server.Start();

        // 客户端连接
        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);

        // 等待会话创建
        Assert.True(sessionCreated.Wait(3000));
        Assert.NotNull(createdSession);
        Assert.True(createdSession.ID > 0);
        Assert.Equal(1, server.SessionCount);

        // 通过ID获取会话
        var session = server.GetSession(createdSession.ID);
        Assert.NotNull(session);
        Assert.Same(createdSession, session);

        // 关闭客户端
        client.Close();
        Thread.Sleep(500);

        // 会话应该已清理（可能需要等待超时）
        Assert.True(server.SessionCount <= 1);
    }

    /// <summary>测试多客户端会话</summary>
    [Fact]
    public void MultipleClientSessions()
    {
        var sessionCount = 0;
        var sessionEvent = new ManualResetEventSlim(false);

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            UseSession = true,
            Log = XTrace.Log,
        };

        server.NewSession += (s, e) =>
        {
            Interlocked.Increment(ref sessionCount);
            if (sessionCount >= 3) sessionEvent.Set();
        };

        server.Start();

        // 创建多个客户端
        var clients = new List<TcpClient>();
        for (var i = 0; i < 3; i++)
        {
            var client = new TcpClient();
            client.Connect(IPAddress.Loopback, server.Port);
            clients.Add(client);
        }

        // 等待所有会话创建
        Assert.True(sessionEvent.Wait(5000));
        Assert.Equal(3, server.SessionCount);

        // 清理
        foreach (var client in clients)
            client.Dispose();
    }

    /// <summary>测试会话ID唯一性</summary>
    [Fact]
    public void SessionIdUniqueness()
    {
        var sessionIds = new List<Int32>();

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            UseSession = true,
            Log = XTrace.Log,
        };

        server.NewSession += (s, e) =>
        {
            lock (sessionIds)
            {
                sessionIds.Add(e.Session!.ID);
            }
        };

        server.Start();

        // 创建多个客户端
        var clients = new List<TcpClient>();
        for (var i = 0; i < 5; i++)
        {
            var client = new TcpClient();
            client.Connect(IPAddress.Loopback, server.Port);
            clients.Add(client);
            Thread.Sleep(50);
        }

        Thread.Sleep(500);

        // 验证ID唯一性
        Assert.Equal(5, sessionIds.Count);
        Assert.Equal(sessionIds.Count, sessionIds.Distinct().Count());

        // 清理
        foreach (var client in clients)
            client.Dispose();
    }

    /// <summary>测试禁用会话集合</summary>
    [Fact]
    public void DisableSessionCollection()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            UseSession = false,
            Log = XTrace.Log,
        };

        server.Start();

        // 客户端连接
        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);
        Thread.Sleep(200);

        // 会话集合应该为空
        Assert.Empty(server.Sessions);
    }
    #endregion

    #region 管道测试
    /// <summary>测试标准编解码器</summary>
    [Fact]
    public void StandardCodecTest()
    {
        var receivedMessage = new ManualResetEventSlim(false);
        Object? received = null;

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
        };

        server.Protocol = new SrmpCodec();
        server.Received += async (s, e) =>
        {
            received = e.Message ?? e.Packet;
            receivedMessage.Set();

            if (s is INetSession session && e.Message is DefaultMessage m)
            {
                var reply = m.CreateReply();
                if (m.Body != null) reply.SetBody(await m.Body.ReadAllAsync());
                session.SendMessage(reply);
            }
        };

        server.Start();

        // 客户端
        var uri = new NetUri($"tcp://127.0.0.1:{server.Port}");
        var client = uri.CreateRemote();
        ((SessionBase)client).Protocol = new SrmpCodec();
        client.Open();

        var request = new DefaultMessage();
        request.SetBody(new ArrayPacket("Hello StandardCodec"u8.ToArray()));
        client.SendMessage(request);

        // 等待接收
        Assert.True(receivedMessage.Wait(3000));
        Assert.NotNull(received);

        client.Close("Test");
    }

    /// <summary>测试协议属性设置</summary>
    [Fact]
    public void ProtocolPropertySet()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
        };

        // 初始无协议
        Assert.Null(server.Protocol);

        // 设置协议编解码器
        server.Protocol = new SrmpCodec();

        Assert.NotNull(server.Protocol);
    }

    /// <summary>测试SplitDataCodec发送数据时追加分割字节</summary>
    [Fact]
    public void SplitDataCodecSendAppendsSplitBytes()
    {
        var serverReceived = new ManualResetEventSlim(false);

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            Log = XTrace.Log,
        };
        server.Protocol = new SplitDataCodec();
        server.Received += async (s, e) =>
        {
            if (s is INetSession session && e.Message is Message m)
            {
                // 服务端收到消息后回送，SplitDataCodec.Write 会追加分割字节
                var reply = new Message();
                if (m.Body != null) reply.SetBody(await m.Body.ReadAllAsync());
                session.SendMessage(reply);
                serverReceived.Set();
            }
        };
        server.Start();

        // 客户端直接接收原始字节
        using var client = new TcpClient();
        client.ReceiveTimeout = 3000;
        client.Connect(IPAddress.Loopback, server.Port);
        var stream = client.GetStream();

        // 向服务端发送带分割字节的消息（SplitDataCodec 解码时需要分割字节）
        var payload = "Hello"u8.ToArray();
        var splitData = new Byte[] { 0x0D, 0x0A };
        var toSend = payload.Concat(splitData).ToArray();
        stream.Write(toSend, 0, toSend.Length);

        // 等待服务端处理完成后再读取回送数据
        Assert.True(serverReceived.Wait(3000));

        var buf = new Byte[64];
        var n = stream.Read(buf, 0, buf.Length);
        var received = buf[..n];

        // 服务端回送的数据末尾应包含分割字节
        Assert.True(received.Length >= splitData.Length);
        Assert.Equal(splitData, received[^splitData.Length..]);
    }

    /// <summary>SplitDataCodec 行帧交付：同步消费直接可用；跨轮带出经 Slice 后帧释放仍可读</summary>
    [Fact]
    public void SplitDataCodec_FrameDeliveredSyncSliceOutlives()
    {
        var all = new ManualResetEventSlim(false);
        var lines = new List<String>();
        IPacket? escaped = null;

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
        };
        server.Protocol = new SplitDataCodec();
        server.Received += (s, e) =>
        {
            if (e.Message is not IMessage msg || msg.Payload == null) return;
            var pk = msg.Payload;

            // 同步消费：帧在本轮同步链路内直接可用（消息体为行内容，不含分隔符）
            var text = pk.ToStr();
            // 跨轮带出：切出共享切片（引用计数），帧同步归还后切片仍保活
            if (text.StartsWith("keep")) escaped = pk.Slice(0, -1);

            lock (lines)
            {
                lines.Add(text.TrimEnd('\r', '\n'));
                if (lines.Count >= 2) all.Set();
            }
        };
        server.Start();

        // 裸 TCP 客户端发送两行（避免 SplitDataCodec.Write 追加分隔符影响输入侧）
        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);
        var stream = client.GetStream();
        var data = "hello\r\nkeep\r\n"u8.ToArray();
        stream.Write(data, 0, data.Length);

        Assert.True(all.Wait(3000));

        lock (lines)
        {
            Assert.Equal(new[] { "hello", "keep" }, lines);
        }

        // 帧已由 Read 在同步消费后归还池引用；跨轮切片独立保活数据
        Assert.NotNull(escaped);
        Assert.Equal("keep", escaped!.ToStr());
        escaped.TryDispose();
    }
    #endregion

    #region 泛型会话测试
    /// <summary>测试自定义会话类型</summary>
    [Fact]
    public void CustomSessionType()
    {
        var sessionCreated = new ManualResetEventSlim(false);

        using var server = new NetServer<MySession>
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
        };

        server.NewSession += (s, e) =>
        {
            sessionCreated.Set();
        };

        server.Start();

        // 客户端连接
        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);

        Assert.True(sessionCreated.Wait(3000));

        // 获取自定义类型会话
        var sessions = server.Sessions;
        Assert.NotEmpty(sessions);

        var session = server.GetSession(sessions.First().Key);
        Assert.NotNull(session);
        Assert.IsType<MySession>(session);
    }

    /// <summary>测试自定义会话属性</summary>
    [Fact]
    public void CustomSessionProperty()
    {
        var sessionCreated = new ManualResetEventSlim(false);
        MySession? createdSession = null;

        using var server = new NetServer<MySession>
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
        };

        server.NewSession += (s, e) =>
        {
            createdSession = e.Session as MySession;
            sessionCreated.Set();
        };

        server.Start();

        // 客户端连接
        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);

        Assert.True(sessionCreated.Wait(3000));
        Assert.NotNull(createdSession);
        Assert.Equal("Test", createdSession.CustomProperty);
    }

    class MySession : NetSession
    {
        public String CustomProperty { get; set; } = "Test";

        protected override void OnReceive(ReceivedEventArgs e)
        {
            base.OnReceive(e);
        }
    }
    #endregion

    #region 群发测试
    /// <summary>测试群发消息</summary>
    [Fact(DisplayName = "群发_全部客户端收到相同字节")]
    public async Task BroadcastMessage()
    {
        var sessionEvent = new ManualResetEventSlim(false);
        var sessionCount = 0;

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            UseSession = true,
            Log = XTrace.Log,
        };
        server.NewSession += (s, e) => { if (Interlocked.Increment(ref sessionCount) >= 3) sessionEvent.Set(); };

        server.Start();

        // 创建多个客户端
        var clients = new List<TcpClient>();
        var streams = new List<NetworkStream>();

        for (var i = 0; i < 3; i++)
        {
            var client = new TcpClient();
            client.Connect(IPAddress.Loopback, server.Port);
            clients.Add(client);
            streams.Add(client.GetStream());
        }

        // 等待所有会话创建
        Assert.True(sessionEvent.Wait(5000), "未创建 3 个会话");

        // 群发数据：3 个会话都应发送成功
        var sendData = "Broadcast Message"u8.ToArray();
        var count = await server.SendAllAsync(new ArrayPacket(sendData));
        Assert.Equal(3, count);

        // 逐个客户端限时读满：3 个都必须收到且内容一致
        for (var i = 0; i < 3; i++)
        {
            streams[i].ReadTimeout = 5000;
            var buf = new Byte[64];
            var total = 0;
            while (total < sendData.Length)
            {
                var len = streams[i].Read(buf, total, buf.Length - total);
                Assert.True(len > 0, $"客户端{i}未收到群发数据");
                total += len;
            }
            Assert.Equal(sendData, buf[..total]);
        }

        // 清理
        foreach (var client in clients)
            client.Dispose();
    }

    /// <summary>测试带条件的群发</summary>
    [Fact(DisplayName = "群发_条件筛选_精确计数")]
    public async Task BroadcastWithPredicate()
    {
        var sessionEvent = new ManualResetEventSlim(false);
        var sessions = new List<INetSession>();

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            UseSession = true,
            Log = XTrace.Log,
        };

        server.NewSession += (s, e) =>
        {
            // 为会话设置标记，通过Session的Items属性
            if (e.Session is NetSession ns)
            {
                ns.Session["Tag"] = ns.ID % 2 == 0 ? "Even" : "Odd";
                lock (sessions)
                {
                    sessions.Add(ns);
                    if (sessions.Count >= 4) sessionEvent.Set();
                }
            }
        };

        server.Start();

        // 创建多个客户端
        var clients = new List<TcpClient>();
        for (var i = 0; i < 4; i++)
        {
            var client = new TcpClient();
            client.Connect(IPAddress.Loopback, server.Port);
            clients.Add(client);
        }

        // 等待 4 个会话全部创建
        Assert.True(sessionEvent.Wait(5000), "未创建 4 个会话");

        // 只向偶数会话发送，发送数应精确等于偶数会话数
        var sendData = "Even Only"u8.ToArray();
        Int32 evenCount;
        lock (sessions) evenCount = sessions.Count(s => s is NetSession ns && ns.Session["Tag"]?.ToString() == "Even");
        Assert.InRange(evenCount, 1, 4);
        var count = await server.SendAllAsync(
            new ArrayPacket(sendData),
            session => session is NetSession ns && ns.Session["Tag"]?.ToString() == "Even");

        Assert.Equal(evenCount, count);

        // 清理
        foreach (var client in clients)
            client.Dispose();
    }

    /// <summary>强断言群发：每个存活客户端都收到完全相同的字节流；已踢出会话不干扰其余会话</summary>
    [Fact(DisplayName = "群发_多会话_逐客户端字节一致_踢出会话容错")]
    public async Task SendAllAsync_StrongAssert_AliveClientsReceiveExact()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            UseSession = true,
            Log = XTrace.Log,
        };

        var sessions = new List<INetSession>();
        server.NewSession += (s, e) => { lock (sessions) sessions.Add(e.Session); };
        server.Start();

        // 4 个客户端顺序接入
        var clients = new List<TcpClient>();
        for (var i = 0; i < 4; i++)
        {
            var client = new TcpClient();
            client.Connect(IPAddress.Loopback, server.Port);
            client.ReceiveTimeout = 3_000;
            clients.Add(client);
        }

        // 等待全部会话建立
        for (var i = 0; i < 100; i++)
        {
            lock (sessions) { if (sessions.Count >= 4) break; }
            Thread.Sleep(20);
        }
        lock (sessions) Assert.True(sessions.Count >= 4, $"应建立4个会话，实际 {sessions.Count}");

        // 踢出最后一个会话（Close 应断开底层连接），按其远端端口与客户端对应
        INetSession kicked;
        lock (sessions) kicked = sessions[^1];
        kicked.Close("kick");
        Thread.Sleep(200);
        var kickedPort = ((IPEndPoint)kicked.Session.Remote.EndPoint).Port;

        // 群发随机报价，逐字节校验
        var payload = new Byte[256];
        Random.Shared.NextBytes(payload);
        var count = await server.SendAllAsync(new ArrayPacket(payload));

        // 存活会话全部送达；已断开会话的 Send 返回 -1 仍计入（既有语义）
        Assert.InRange(count, 3, 4);

        foreach (var client in clients)
        {
            var stream = client.GetStream();
            var isKicked = ((IPEndPoint)client.Client.LocalEndPoint!).Port == kickedPort;
            if (isKicked)
            {
                // 被踢出的客户端不应收到群发数据
                Thread.Sleep(500);
                Assert.False(stream.DataAvailable, "被踢出的客户端不应收到群发数据");
                continue;
            }

            // 等待数据完整到达
            var buf = new Byte[1024];
            var total = 0;
            for (var k = 0; k < 100 && total < payload.Length; k++)
            {
                while (total < payload.Length && stream.DataAvailable)
                {
                    var len = stream.Read(buf, total, buf.Length - total);
                    if (len <= 0) break;
                    total += len;
                }
                if (total < payload.Length) Thread.Sleep(20);
            }

            Assert.Equal(payload.Length, total);
            Assert.Equal(payload, buf[..total]);
        }

        foreach (var client in clients) client.Dispose();
    }
    #endregion

    #region SSL测试
    /// <summary>测试SSL配置</summary>
    [Fact]
    public void SslConfiguration()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            SslProtocol = System.Security.Authentication.SslProtocols.Tls12,
            Log = XTrace.Log,
        };

        Assert.Equal(System.Security.Authentication.SslProtocols.Tls12, server.SslProtocol);
    }
    #endregion

    #region 地址重用测试
    /// <summary>测试地址重用</summary>
    [Fact(DisplayName = "地址重用_监听器停止后同端口立即重启")]
    public void ReuseAddressTest()
    {
        var port = 0;

        // 第一个服务器
        using (var server1 = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            ReuseAddress = true,
            Log = XTrace.Log,
        })
        {
            server1.Start();
            port = server1.Port;
            server1.Stop("Test");
        }

        // 立即启动第二个服务器在相同端口（启用地址重用）
        using var server2 = new NetServer
        {
            Port = port,
            ProtocolType = NetType.Tcp,
            ReuseAddress = true,
            Log = XTrace.Log,
        };

        // 启用地址重用后，监听器停止后可立即在同端口重启
        server2.Start();
        Assert.True(server2.Active);
    }
    #endregion

    #region 错误处理测试
    /// <summary>测试错误事件</summary>
    [Fact]
    public void ErrorEventTest()
    {
        var errorReceived = false;

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
        };

        server.Error += (s, e) =>
        {
            errorReceived = true;
        };

        server.Start();

        // 正常关闭不应触发错误
        server.Stop("Test");

        Assert.False(errorReceived);
    }
    #endregion

    #region 会话生命周期测试
    /// <summary>测试会话连接事件</summary>
    [Fact]
    public void SessionLifecycleEvents()
    {
        var sessionCreated = new ManualResetEventSlim(false);

        using var server = new NetServer<LifecycleSession>
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            Log = XTrace.Log,
        };

        server.NewSession += (s, e) =>
        {
            sessionCreated.Set();
        };

        server.Start();

        // 客户端连接
        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);
        Thread.Sleep(300);

        // 验证会话创建
        Assert.True(sessionCreated.Wait(3000));

        // 验证会话存在
        Assert.True(server.Sessions.Count >= 1);
    }

    class LifecycleSession : NetSession
    {
        public Action? OnConnectedCallback { get; set; }
        public Action? OnDisconnectedCallback { get; set; }

        protected override void OnConnected()
        {
            base.OnConnected();
            OnConnectedCallback?.Invoke();
        }

        protected override void OnDisconnected(String reason)
        {
            base.OnDisconnected(reason);
            OnDisconnectedCallback?.Invoke();
        }
    }
    #endregion

    #region 服务提供者测试
    /// <summary>测试服务提供者</summary>
    [Fact]
    public void ServiceProviderTest()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
        };

        // 设置服务提供者
        var services = ObjectContainer.Current;
        services.AddSingleton<ITestService, TestService>();
        server.ServiceProvider = services.BuildServiceProvider();

        server.Start();

        Assert.NotNull(server.ServiceProvider);
    }

    interface ITestService
    {
        void DoWork();
    }

    class TestService : ITestService
    {
        public void DoWork() { }
    }
    #endregion

    #region 统计信息测试
    /// <summary>测试统计信息获取</summary>
    [Fact]
    public void GetStatTest()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            UseSession = true,
            StatPeriod = 0,
            Log = XTrace.Log,
        };

        server.Start();

        // 创建连接
        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);
        Thread.Sleep(200);

        // 获取统计
        var stat = server.GetStat();
        Assert.NotEmpty(stat);
        Assert.Contains("在线", stat);
    }
    #endregion

    #region Items扩展数据测试
    /// <summary>测试服务器扩展数据</summary>
    [Fact]
    public void ServerItemsTest()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
        };

        // 设置扩展数据
        server["Key1"] = "Value1";
        server["Key2"] = 123;

        Assert.Equal("Value1", server["Key1"]);
        Assert.Equal(123, server["Key2"]);
        Assert.Null(server["NonExistent"]);
    }
    #endregion

    #region 端口随机分配测试
    /// <summary>测试端口为0时自动分配</summary>
    [Fact]
    public void RandomPortAllocation()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
        };

        Assert.Equal(0, server.Port);

        server.Start();

        // 启动后应该分配了实际端口
        Assert.True(server.Port > 0);
        Assert.True(server.Port < 65536);
    }

    /// <summary>反复随机端口启停，验证端口分配与回收稳定</summary>
    [Fact]
    public void RandomPortRepeatedStartStop()
    {
        // 随机端口可能落入系统保留段，OnStart 内置有界重试，反复启停不应失败
        for (var i = 0; i < 5; i++)
        {
            using var server = new NetServer
            {
                Port = 0,
                ProtocolType = NetType.Tcp,
                Log = XTrace.Log,
            };

            server.Start();
            Assert.True(server.Port > 0);

            server.Stop("测试重启");
        }
    }
    #endregion

    #region ToString测试
    /// <summary>测试服务器ToString</summary>
    [Fact]
    public void ServerToStringTest()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
        };

        var str1 = server.ToString();
        Assert.NotEmpty(str1);

        server.Start();

        var str2 = server.ToString();
        Assert.NotEmpty(str2);
        Assert.Contains(server.Port.ToString(), str2);
    }
    #endregion

    #region 构造函数测试
    /// <summary>测试无参构造函数</summary>
    [Fact]
    public void ConstructorDefault()
    {
        using var server = new NetServer();

        Assert.Equal("Net", server.Name);
        Assert.Equal(0, server.Port);
        Assert.Equal(NetType.Unknown, server.ProtocolType);
        Assert.Equal(AddressFamily.Unspecified, server.AddressFamily);
        Assert.True(server.UseSession);
        Assert.False(server.ReuseAddress);
        Assert.Equal(600, server.StatPeriod);
    }

    /// <summary>测试端口构造函数</summary>
    [Fact]
    public void ConstructorWithPort()
    {
        using var server = new NetServer(8080);

        Assert.Equal(8080, server.Port);
        Assert.Equal(IPAddress.Any, server.Local.Address);
    }

    /// <summary>测试地址端口构造函数</summary>
    [Fact]
    public void ConstructorWithAddressPort()
    {
        using var server = new NetServer(IPAddress.Loopback, 9090);

        Assert.Equal(9090, server.Port);
        Assert.Equal(IPAddress.Loopback, server.Local.Address);
    }

    /// <summary>测试完整构造函数</summary>
    [Fact]
    public void ConstructorWithProtocol()
    {
        using var server = new NetServer(IPAddress.Any, 7070, NetType.Tcp);

        Assert.Equal(7070, server.Port);
        Assert.Equal(NetType.Tcp, server.ProtocolType);
    }
    #endregion

    #region Local属性测试
    /// <summary>测试Local属性自动推断地址族</summary>
    [Fact]
    public void LocalPropertyAddressFamilyInference()
    {
        using var server = new NetServer();

        // 设置IPv4地址
        server.Local = new NetUri("tcp://192.168.1.1:8080");
        Assert.Equal(AddressFamily.InterNetwork, server.AddressFamily);

        // 设置通配符地址不改变地址族
        using var server2 = new NetServer();
        server2.Local = new NetUri("tcp://*:8080");
        Assert.Equal(AddressFamily.Unspecified, server2.AddressFamily);
    }
    #endregion

    #region Server属性测试
    /// <summary>测试Server属性自动创建</summary>
    [Fact]
    public void ServerPropertyAutoCreate()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
        };

        // 访问Server属性会自动创建
        var socketServer = server.Server;

        Assert.NotNull(socketServer);
        Assert.Single(server.Servers);
    }

    /// <summary>测试Server属性设置为null清空集合</summary>
    [Fact]
    public void ServerPropertySetNull()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
        };

        // 先触发自动创建
        var _ = server.Server;
        Assert.NotEmpty(server.Servers);

        // 设置为null清空集合
        server.Server = null;
        Assert.Empty(server.Servers);
    }
    #endregion

    #region AddServer方法测试
    /// <summary>测试AddServer方法</summary>
    [Fact]
    public void AddServerMethod()
    {
        using var server = new NetServer
        {
            Log = XTrace.Log,
        };

        var count = server.AddServer(IPAddress.Any, 0, NetType.Tcp, AddressFamily.InterNetwork);

        Assert.Equal(1, count);
        Assert.Single(server.Servers);
    }

    /// <summary>测试AddServer添加多种协议</summary>
    [Fact]
    public void AddServerMultipleProtocols()
    {
        using var server = new NetServer
        {
            Log = XTrace.Log,
        };

        var count = server.AddServer(IPAddress.Any, 0, NetType.Unknown, AddressFamily.InterNetwork);

        // Unknown协议会同时添加TCP和UDP
        Assert.Equal(2, count);
        Assert.Equal(2, server.Servers.Count);
    }
    #endregion

    #region AttachServer方法测试
    /// <summary>测试AttachServer防止重复添加</summary>
    [Fact]
    public void AttachServerPreventDuplicate()
    {
        using var server = new NetServer
        {
            Log = XTrace.Log,
        };

        var tcpServer = new TcpServer { Port = 0 };

        var result1 = server.AttachServer(tcpServer);
        var result2 = server.AttachServer(tcpServer);

        Assert.True(result1);
        Assert.False(result2);
        Assert.Single(server.Servers);
    }
    #endregion

    #region MaxSessionCount测试
    /// <summary>测试最高会话数记录</summary>
    [Fact]
    public void MaxSessionCountTracking()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            UseSession = true,
            Log = XTrace.Log,
        };

        server.Start();

        // 创建多个客户端
        var clients = new List<TcpClient>();
        for (var i = 0; i < 5; i++)
        {
            var client = new TcpClient();
            client.Connect(IPAddress.Loopback, server.Port);
            clients.Add(client);
        }

        Thread.Sleep(500);

        // 记录最高会话数
        var maxCount = server.MaxSessionCount;
        Assert.True(maxCount >= 5);

        // 关闭部分客户端
        clients[0].Dispose();
        clients[1].Dispose();
        Thread.Sleep(500);

        // MaxSessionCount不应减少
        Assert.True(server.MaxSessionCount >= maxCount);

        // 清理
        foreach (var client in clients)
            client.Dispose();
    }
    #endregion

    #region SendAllMessage测试
    /// <summary>测试SendAllMessage群发</summary>
    [Fact]
    public void SendAllMessageTest()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            UseSession = true,
            Log = XTrace.Log,
        };

        server.Start();

        // 创建客户端
        var clients = new List<TcpClient>();
        for (var i = 0; i < 3; i++)
        {
            var client = new TcpClient();
            client.Connect(IPAddress.Loopback, server.Port);
            clients.Add(client);
        }

        Thread.Sleep(500);

        // 群发消息（无协议模式：直接字节群发）
        var count = server.SendAllMessage(new ArrayPacket("Broadcast"u8.ToArray()));

        Assert.True(count >= 0);

        // 清理
        foreach (var client in clients)
            client.Dispose();
    }

    /// <summary>测试UseSession为false时群发抛异常</summary>
    [Fact]
    public async Task SendAllAsyncThrowsWhenUseSessionFalse()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            UseSession = false,
            Log = XTrace.Log,
        };

        server.Start();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
        {
            await server.SendAllAsync(new ArrayPacket("Test"u8.ToArray()));
        });
    }
    #endregion

    #region CreateHandler测试
    /// <summary>测试CreateHandler默认返回null</summary>
    [Fact]
    public void CreateHandlerReturnsNull()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            Log = XTrace.Log,
        };

        var sessionCreated = new ManualResetEventSlim(false);
        INetSession? createdSession = null;

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

        if (createdSession is NetSession ns)
        {
            // 默认Handler为null
            Assert.Null(ns.Handler);
        }
    }

    /// <summary>测试自定义CreateHandler</summary>
    [Fact]
    public void CustomCreateHandler()
    {
        var sessionCreated = new ManualResetEventSlim(false);
        INetSession? createdSession = null;

        using var server = new HandlerServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            Log = XTrace.Log,
        };

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

        if (createdSession is NetSession ns)
        {
            Assert.NotNull(ns.Handler);
            Assert.IsType<CustomHandler>(ns.Handler);
        }
    }

    class HandlerServer : NetServer
    {
        public override INetHandler? CreateHandler(INetSession session) => new CustomHandler();
    }

    class CustomHandler : INetHandler
    {
        public void Init(INetSession session) { }
        public void Process(IData data) { }
    }
    #endregion

    #region Tracer测试
    /// <summary>测试Tracer属性配置</summary>
    [Fact]
    public void TracerConfiguration()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
        };

        // 默认为null
        Assert.Null(server.Tracer);
        Assert.Null(server.SocketTracer);

        // 可以设置不同的Tracer
        var tracer1 = new DefaultTracer();
        var tracer2 = new DefaultTracer();

        server.Tracer = tracer1;
        server.SocketTracer = tracer2;

        Assert.Same(tracer1, server.Tracer);
        Assert.Same(tracer2, server.SocketTracer);
    }
    #endregion

    #region 日志属性测试
    /// <summary>测试日志属性</summary>
    [Fact]
    public void LogProperties()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
        };

        // 设置不同的日志
        server.Log = XTrace.Log;
        server.SocketLog = XTrace.Log;
        server.SessionLog = XTrace.Log;

        Assert.Same(XTrace.Log, server.Log);
        Assert.Same(XTrace.Log, server.SocketLog);
        Assert.Same(XTrace.Log, server.SessionLog);
    }

    /// <summary>测试LogPrefix属性</summary>
    [Fact]
    public void LogPrefixProperty()
    {
        using var server = new NetServer
        {
            Name = "TestServer",
        };

        Assert.Equal("TestServer", server.LogPrefix);

        server.LogPrefix = "CustomPrefix";
        Assert.Equal("CustomPrefix", server.LogPrefix);
    }

    /// <summary>测试LogSend和LogReceive属性</summary>
    [Fact]
    public void LogSendReceiveProperties()
    {
        using var server = new NetServer();

        Assert.False(server.LogSend);
        Assert.False(server.LogReceive);

        server.LogSend = true;
        server.LogReceive = true;

        Assert.True(server.LogSend);
        Assert.True(server.LogReceive);
    }
    #endregion
}
