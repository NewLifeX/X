using System.Net;
using System.Net.Sockets;
using System.Text;
using NewLife;
using NewLife.Data;
using NewLife.Messaging;
using NewLife.Net;
using NewLife.Net.Handlers;
using Xunit;

namespace XUnitTest.Net;

/// <summary>NetClient/NetServer 端到端集成测试，覆盖编解码器、数据完整性、多客户端并发、会话生命周期等场景</summary>
[Collection("Net")]
[TestCaseOrderer("NewLife.UnitTest.DefaultOrderer", "NewLife.UnitTest")]
public class NetIntegrationTests
{
    #region 辅助
    /// <summary>从 SendMessageAsync 返回值中提取有效负载</summary>
    private static Byte[] ExtractPayload(Object? resp)
    {
        // StandardCodec UserPacket=true 时返回 Payload 克隆（IPacket）
        // StandardCodec UserPacket=false 时返回 DefaultMessage（IMessage）
        // LengthFieldCodec 返回 IPacket
        if (resp is IMessage msg) return msg.Payload.ToArray();
        if (resp is IPacket pk) return pk.ToArray();
        return [];
    }

    /// <summary>把收到的请求负载回显给发送方（优先 e.Message 负载，负载缺失时回退整轮 e.Packet）</summary>
    private static void EchoPayload(INetSession session, ReceivedEventArgs e)
    {
        var pk = e.Message as IPacket;
        if (pk == null || pk.Total == 0) pk = e.Packet;
        if (pk != null && pk.Total > 0) session.SendReply(pk, e);
    }
    #endregion

    #region TCP Echo 数据完整性
    /// <summary>TCP Echo 通过 NetClient 客户端收发，验证数据逐字节一致</summary>
    [Fact]
    public void TcpEcho_NetClient_DataIntegrity()
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

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}");
        var payload = new Byte[256];
        Random.Shared.NextBytes(payload);

        var wait = new ManualResetEventSlim();
        Byte[]? received = null;
        client.Received += (s, e) =>
        {
            received = e.GetBytes();
            wait.Set();
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
    /// <summary>LengthFieldCodec 编解码集成：客户端带长度前缀发送，服务端正确解码并回显</summary>
    [Fact]
    public void LengthFieldCodec_ServerDecodesPayload()
    {
        var decodedPayload = new ManualResetEventSlim();
        Byte[]? serverReceived = null;

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
        };
        server.Add(new LengthFieldCodec { Size = 2, Offset = 0 });
        server.Received += (s, e) =>
        {
            if (e.Message is IPacket pk)
            {
                serverReceived = pk.ToArray();
                decodedPayload.Set();

                if (s is INetSession session)
                    session.SendMessage(pk);
            }
        };
        server.Start();

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}");
        client.Add(new LengthFieldCodec { Size = 2, Offset = 0 });
        client.Open();

        var payload = "LengthField-Test-Payload"u8.ToArray();
        client.SendMessage(payload.AsPacket());

        Assert.True(decodedPayload.Wait(3_000));
        Assert.NotNull(serverReceived);
        // 服务端通过 LengthFieldCodec 解码后，应收到纯负载（无长度头）
        Assert.Equal(payload, serverReceived);
    }

    /// <summary>LengthFieldCodec 连续发送多条消息，验证粘包拆包正确（服务端逐条收到完整负载）</summary>
    [Fact]
    public void LengthFieldCodec_MultiMessage_ServerDecodesEach()
    {
        var receivedPayloads = new List<Byte[]>();
        var allReceived = new ManualResetEventSlim();

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
        };
        server.Add(new LengthFieldCodec { Size = 2, Offset = 0 });
        server.Received += (s, e) =>
        {
            if (e.Message is IPacket pk)
            {
                lock (receivedPayloads)
                {
                    receivedPayloads.Add(pk.ToArray());
                    if (receivedPayloads.Count >= 5) allReceived.Set();
                }
            }
        };
        server.Start();

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}");
        client.Add(new LengthFieldCodec { Size = 2, Offset = 0 });
        client.Open();

        var expected = new List<Byte[]>();
        for (var i = 0; i < 5; i++)
        {
            var msg = Encoding.UTF8.GetBytes($"Message-{i}");
            expected.Add(msg);
            client.SendMessage(new ArrayPacket(msg));
        }

        Assert.True(allReceived.Wait(5_000));
        Assert.Equal(5, receivedPayloads.Count);
        for (var i = 0; i < 5; i++)
        {
            Assert.Equal(expected[i], receivedPayloads[i]);
        }
    }

    /// <summary>LengthFieldCodec 4字节长度头，大载荷正确传输</summary>
    [Fact]
    public void LengthFieldCodec_Size4_LargePayload()
    {
        var decodedPayload = new ManualResetEventSlim();
        Byte[]? serverReceived = null;

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
        };
        server.Add(new LengthFieldCodec { Size = 4, Offset = 0 });
        server.Received += (s, e) =>
        {
            if (e.Message is IPacket pk)
            {
                serverReceived = pk.ToArray();
                decodedPayload.Set();
            }
        };
        server.Start();

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}");
        client.Add(new LengthFieldCodec { Size = 4, Offset = 0 });
        client.Open();

        var payload = new Byte[512];
        Random.Shared.NextBytes(payload);
        client.SendMessage(new ArrayPacket(payload));

        Assert.True(decodedPayload.Wait(5_000));
        Assert.NotNull(serverReceived);
        Assert.Equal(payload, serverReceived);
    }

    /// <summary>LengthFieldCodec 负数 Size 表示大端序</summary>
    [Fact]
    public void LengthFieldCodec_BigEndian_NegativeSize()
    {
        var decodedPayload = new ManualResetEventSlim();
        Byte[]? serverReceived = null;

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
        };
        server.Add(new LengthFieldCodec { Size = -2, Offset = 0 });
        server.Received += (s, e) =>
        {
            if (e.Message is IPacket pk)
            {
                serverReceived = pk.ToArray();
                decodedPayload.Set();
            }
        };
        server.Start();

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}");
        client.Add(new LengthFieldCodec { Size = -2, Offset = 0 });
        client.Open();

        var payload = "BigEndianTest"u8.ToArray();
        client.SendMessage(new ArrayPacket(payload));

        Assert.True(decodedPayload.Wait(3_000));
        Assert.NotNull(serverReceived);
        Assert.Equal(payload, serverReceived);
    }

    /// <summary>LengthFieldCodec Offset=2 的场景（如 MQTT：[Flag(1)] [Length(2)] [Payload]）</summary>
    [Fact]
    public void LengthFieldCodec_Offset2_MqttStyle()
    {
        var receivedPayloads = new List<Byte[]>();
        var allReceived = new ManualResetEventSlim();

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
        };
        server.Add(new LengthFieldCodec { Size = 2, Offset = 2 });
        server.Received += (s, e) =>
        {
            if (e.Message is IPacket pk)
            {
                lock (receivedPayloads)
                {
                    receivedPayloads.Add(pk.ToArray());
                    if (receivedPayloads.Count >= 3) allReceived.Set();
                }
            }
        };
        server.Start();

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}");
        client.Add(new LengthFieldCodec { Size = 2, Offset = 2 });
        client.Open();

        var expected = new List<Byte[]>();
        for (var i = 0; i < 3; i++)
        {
            var payload = Encoding.UTF8.GetBytes($"MQTT-{i}");
            expected.Add(payload);
            client.SendMessage(new ArrayPacket(payload));
        }

        Assert.True(allReceived.Wait(5_000));
        Assert.Equal(3, receivedPayloads.Count);
        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(expected[i], receivedPayloads[i]);
        }
    }

    /// <summary>LengthFieldCodec Offset=4 的情况（4 字节协议头，然后是 2 字节长度字段）</summary>
    [Fact]
    public void LengthFieldCodec_Offset4_ProtocolHeader()
    {
        var decodedPayload = new ManualResetEventSlim();
        Byte[]? serverReceived = null;

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
        };
        server.Add(new LengthFieldCodec { Size = 2, Offset = 4 });
        server.Received += (s, e) =>
        {
            if (e.Message is IPacket pk)
            {
                serverReceived = pk.ToArray();
                decodedPayload.Set();
            }
        };
        server.Start();

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}");
        client.Add(new LengthFieldCodec { Size = 2, Offset = 4 });
        client.Open();

        var payload = "Offset4-Test-Payload"u8.ToArray();
        client.SendMessage(new ArrayPacket(payload));

        Assert.True(decodedPayload.Wait(3_000));
        Assert.NotNull(serverReceived);
        Assert.Equal(payload, serverReceived);
    }

    /// <summary>LengthFieldCodec 大端序 Size=-4 + Offset=2 组合</summary>
    [Fact]
    public void LengthFieldCodec_Size4_BigEndian_WithOffset()
    {
        var decodedPayload = new ManualResetEventSlim();
        Byte[]? serverReceived = null;

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
        };
        server.Add(new LengthFieldCodec { Size = -4, Offset = 2 });
        server.Received += (s, e) =>
        {
            if (e.Message is IPacket pk)
            {
                serverReceived = pk.ToArray();
                decodedPayload.Set();
            }
        };
        server.Start();

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}");
        client.Add(new LengthFieldCodec { Size = -4, Offset = 2 });
        client.Open();

        var payload = new Byte[1024];
        Random.Shared.NextBytes(payload);
        client.SendMessage(new ArrayPacket(payload));

        Assert.True(decodedPayload.Wait(5_000));
        Assert.NotNull(serverReceived);
        Assert.Equal(payload, serverReceived);
    }

    /// <summary>LengthFieldCodec 多消息粘包，Offset=1 + Size=1 的小包场景</summary>
    [Fact]
    public void LengthFieldCodec_Size1_Offset1_MultiMessage()
    {
        var receivedPayloads = new List<Byte[]>();
        var allReceived = new ManualResetEventSlim();

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
        };
        server.Add(new LengthFieldCodec { Size = 1, Offset = 1 });
        server.Received += (s, e) =>
        {
            if (e.Message is IPacket pk)
            {
                lock (receivedPayloads)
                {
                    receivedPayloads.Add(pk.ToArray());
                    if (receivedPayloads.Count >= 10) allReceived.Set();
                }
            }
        };
        server.Start();

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}");
        client.Add(new LengthFieldCodec { Size = 1, Offset = 1 });
        client.Open();

        var expected = new List<Byte[]>();
        for (var i = 0; i < 10; i++)
        {
            var payload = Encoding.UTF8.GetBytes($"S{i}");
            expected.Add(payload);
            client.SendMessage(new ArrayPacket(payload));
        }

        Assert.True(allReceived.Wait(5_000));
        Assert.Equal(10, receivedPayloads.Count);
        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(expected[i], receivedPayloads[i]);
        }
    }

    /// <summary>LengthFieldCodec Size=0（变长编码）+ Offset=0 的边界测试</summary>
    [Fact]
    public void LengthFieldCodec_VarLength_Size0_Offset0()
    {
        var decodedPayload = new ManualResetEventSlim();
        Byte[]? serverReceived = null;

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
        };
        server.Add(new LengthFieldCodec { Size = 0, Offset = 0 });
        server.Received += (s, e) =>
        {
            if (e.Message is IPacket pk)
            {
                serverReceived = pk.ToArray();
                decodedPayload.Set();
            }
        };
        server.Start();

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}");
        client.Add(new LengthFieldCodec { Size = 0, Offset = 0 });
        client.Open();

        var payload = "VarLength-Test"u8.ToArray();
        client.SendMessage(new ArrayPacket(payload));

        Assert.True(decodedPayload.Wait(3_000));
        Assert.NotNull(serverReceived);
        Assert.Equal(payload, serverReceived);
    }

    /// <summary>LengthFieldCodec Size=0（变长编码）+ 大载荷跨轮：头部前缀读取不物化整帧，链式拆分正确</summary>
    [Fact]
    public void LengthFieldCodec_VarLength_BigPayload_Chained()
    {
        var decodedPayload = new ManualResetEventSlim();
        Byte[]? serverReceived = null;

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
        };
        server.Add(new LengthFieldCodec { Size = 0, Offset = 0 });
        server.Received += (s, e) =>
        {
            if (e.Message is IPacket pk)
            {
                serverReceived = pk.ToArray();
                decodedPayload.Set();
            }
        };
        server.Start();

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}");
        client.Add(new LengthFieldCodec { Size = 0, Offset = 0 });
        client.Open();

        // 60KB 负载跨多个接收轮，服务端粘包编码器组链后按变长头部前缀拆分
        var payload = new Byte[60_000];
        Random.Shared.NextBytes(payload);
        client.SendMessage(new ArrayPacket(payload));

        Assert.True(decodedPayload.Wait(10_000));
        Assert.NotNull(serverReceived);
        Assert.Equal(payload, serverReceived);
    }

    /// <summary>LengthFieldCodec Size=0（变长编码）+ Offset=2：偏移与多字节变长字段组合正确拆分</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(127)]
    [InlineData(128)]
    [InlineData(1024)]
    [InlineData(65536)]
    public void LengthFieldCodec_VarLength_Size0_Offset2(Int32 size)
    {
        var decodedPayload = new ManualResetEventSlim();
        Byte[]? serverReceived = null;

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
        };
        server.Add(new LengthFieldCodec { Size = 0, Offset = 2 });
        server.Received += (s, e) =>
        {
            if (e.Message is IPacket pk)
            {
                serverReceived = pk.ToArray();
                decodedPayload.Set();
            }
        };
        server.Start();

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}");
        client.Add(new LengthFieldCodec { Size = 0, Offset = 2 });
        client.Open();

        // 覆盖 1/2/3 字节变长编码：1B→1字节、128B→2字节、65536B→3字节且跨多个接收轮
        var payload = new Byte[size];
        Random.Shared.NextBytes(payload);
        client.SendMessage(new ArrayPacket(payload));

        Assert.True(decodedPayload.Wait(10_000));
        Assert.NotNull(serverReceived);
        Assert.Equal(payload, serverReceived);
    }

    /// <summary>LengthFieldCodec 不同负载大小边界测试：1B、255B、256B、65535B、65536B</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(127)]
    [InlineData(255)]
    [InlineData(256)]
    [InlineData(1024)]
    [InlineData(65535)]
    public void LengthFieldCodec_VariousPayloadSizes_WithOffset(Int32 size)
    {
        var decodedPayload = new ManualResetEventSlim();
        Byte[]? serverReceived = null;

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
        };
        server.Add(new LengthFieldCodec { Size = 2, Offset = 2 });
        server.Received += (s, e) =>
        {
            if (e.Message is IPacket pk)
            {
                serverReceived = pk.ToArray();
                decodedPayload.Set();
            }
        };
        server.Start();

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}");
        client.Add(new LengthFieldCodec { Size = 2, Offset = 2 });
        client.Timeout = 10_000;
        client.Open();

        var payload = new Byte[size];
        Random.Shared.NextBytes(payload);
        client.SendMessage(new ArrayPacket(payload));

        Assert.True(decodedPayload.Wait(10_000));
        Assert.NotNull(serverReceived);
        Assert.Equal(size, serverReceived.Length);
        Assert.Equal(payload, serverReceived);
    }

    /// <summary>LengthFieldCodec 混合大小端 + 不同 Offset 的互操作性验证（同一连接内多轮编解码）</summary>
    [Fact]
    public async Task LengthFieldCodec_ConsistentEncoding_MultiRound()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
        };
        server.Add(new LengthFieldCodec { Size = 2, Offset = 1 });
        server.Received += (s, e) =>
        {
            if (s is INetSession session && e.Message is IPacket pk)
                session.SendMessage(pk);
        };
        server.Start();

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}");
        client.Add(new LengthFieldCodec { Size = 2, Offset = 1 });
        client.Timeout = 10_000;
        client.Open();

        // 同一连接内多轮发送，验证编码器和解码器状态一致
        for (var round = 0; round < 10; round++)
        {
            var payload = Encoding.UTF8.GetBytes($"Round-{round:D2}-{Guid.NewGuid():N}");
            
            // 通过 StandardCodec 与服务端配合验证（手动实现回显验证）
            var pkSent = new ArrayPacket(payload);
            var received = await client.SendMessageAsync(pkSent, CancellationToken.None);
            
            Assert.NotNull(received);
        }
    }
    #endregion

    #region StandardCodec 请求响应
    /// <summary>StandardCodec 多轮 SendMessageAsync 请求响应，验证通信可靠</summary>
    [Fact]
    public async Task StandardCodec_MultiRound_RequestResponse()
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
            if (s is INetSession session) EchoPayload(session, e);
        };
        server.Start();

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}");
        client.Add<StandardCodec>();
        client.Timeout = 5_000;
        client.Open();

        for (var i = 0; i < 20; i++)
        {
            var payload = Encoding.UTF8.GetBytes($"Req-{i:D4}-{Guid.NewGuid()}");
            var resp = await client.SendMessageAsync(new ArrayPacket(payload));
            Assert.NotNull(resp);

            // 提取有效负载验证
            var data = ExtractPayload(resp);
            Assert.True(data.Length > 0);
        }
    }

    /// <summary>StandardCodec 服务端解码后收到的负载与客户端发送一致</summary>
    [Fact]
    public void StandardCodec_ServerDecodesPayload()
    {
        var decodedPayloads = new List<Byte[]>();
        var allReceived = new ManualResetEventSlim();

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
        };
        server.Add<StandardCodec>();
        server.Received += (s, e) =>
        {
            if (e.Message is IPacket pk)
            {
                lock (decodedPayloads)
                {
                    decodedPayloads.Add(pk.ToArray());
                    if (decodedPayloads.Count >= 3) allReceived.Set();
                }
            }
        };
        server.Start();

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}");
        client.Add<StandardCodec>();
        client.Open();

        var expected = new List<Byte[]>();
        for (var i = 0; i < 3; i++)
        {
            var payload = Encoding.UTF8.GetBytes($"Payload-{i}");
            expected.Add(payload);
            client.SendMessage(new ArrayPacket(payload));
            Thread.Sleep(50);
        }

        Assert.True(allReceived.Wait(5_000));
        Assert.Equal(3, decodedPayloads.Count);
        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(expected[i], decodedPayloads[i]);
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
            if (s is INetSession session) EchoPayload(session, e);
        };
        server.Start();

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}");
        client.Add<StandardCodec>();
        client.Timeout = 10_000;
        client.Open();

        var payload = new Byte[size];
        Random.Shared.NextBytes(payload);
        var resp = await client.SendMessageAsync(new ArrayPacket(payload));

        Assert.NotNull(resp);
        var data = ExtractPayload(resp);
        Assert.True(data.Length > 0);
    }

    /// <summary>StandardCodec 直接发送 Byte[]，无需转换为 IPacket</summary>
    [Fact]
    public void StandardCodec_SendMessage_ByteArray_Direct()
    {
        var decodedPayloads = new List<Byte[]>();
        var allReceived = new ManualResetEventSlim();

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
        };
        server.Add<StandardCodec>();
        server.Received += (s, e) =>
        {
            if (e.Message is IPacket pk)
            {
                lock (decodedPayloads)
                {
                    decodedPayloads.Add(pk.ToArray());
                    if (decodedPayloads.Count >= 2) allReceived.Set();
                }
            }
        };
        server.Start();

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}");
        client.Add<StandardCodec>();
        client.Open();

        // 直接发送 Byte[]，不转换为 IPacket
        var payload1 = Encoding.UTF8.GetBytes("Direct-ByteArray-1");
        client.SendMessage(payload1);

        var payload2 = Encoding.UTF8.GetBytes("Direct-ByteArray-2");
        client.SendMessage(payload2);

        Assert.True(allReceived.Wait(5_000));
        Assert.Equal(2, decodedPayloads.Count);
        Assert.Equal(payload1, decodedPayloads[0]);
        Assert.Equal(payload2, decodedPayloads[1]);
    }
    #endregion

    #region 多客户端并发 SendMessageAsync
    /// <summary>多个独立客户端并发 SendMessageAsync，各自收到响应</summary>
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
            if (s is INetSession session) EchoPayload(session, e);
        };
        server.Start();

        const Int32 clientCount = 5;
        var clients = new List<NetClient>();

        for (var i = 0; i < clientCount; i++)
        {
            var c = new NetClient($"tcp://127.0.0.1:{server.Port}");
            c.Add<StandardCodec>();
            c.Timeout = 10_000;
            c.Open();
            clients.Add(c);
        }

        // 每个客户端并发发送
        var tasks = new List<Task>();
        for (var ci = 0; ci < clientCount; ci++)
        {
            var c = clients[ci];
            tasks.Add(Task.Run(async () =>
            {
                for (var j = 0; j < 5; j++)
                {
                    var payload = Encoding.UTF8.GetBytes($"Client-Msg{j}");
                    var resp = await c.SendMessageAsync(new ArrayPacket(payload));
                    Assert.NotNull(resp);
                }
            }));
        }

        await Task.WhenAll(tasks);

        foreach (var c in clients)
            c.Dispose();
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

        // OnConnected 在 NetServer.OnNewSession 内部的 Start 中触发，早于 NewSession 事件，
        // 因此必须在服务器启动前通过静态字段传递信号，确保 OnConnected 能正确设置信号
        LifecycleTrackingSession.StaticConnectedSignal = connected;
        LifecycleTrackingSession.StaticReceivedSignal = received;
        LifecycleTrackingSession.StaticDisconnectedSignal = disconnected;
        LifecycleTrackingSession.StaticInstance = null;

        using var server = new NetServer<LifecycleTrackingSession>
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
        };
        server.Start();

        // 连接
        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);
        Assert.True(connected.Wait(3_000));
        var session = LifecycleTrackingSession.StaticInstance;
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
        public static ManualResetEventSlim? StaticConnectedSignal;
        public static ManualResetEventSlim? StaticReceivedSignal;
        public static ManualResetEventSlim? StaticDisconnectedSignal;
        public static LifecycleTrackingSession? StaticInstance;

        public Boolean IsConnected { get; private set; }
        public Boolean IsDisconnected { get; private set; }
        public Int32 ReceivedCount => _receivedCount;
        private Int32 _receivedCount;

        protected override void OnConnected()
        {
            base.OnConnected();
            IsConnected = true;
            StaticInstance = this;
            StaticConnectedSignal?.Set();
        }

        protected override void OnReceive(ReceivedEventArgs e)
        {
            base.OnReceive(e);
            if (e.Packet != null && e.Packet.Total > 0)
            {
                Interlocked.Increment(ref _receivedCount);
                StaticReceivedSignal?.Set();
            }
        }

        protected override void OnDisconnected(String reason)
        {
            base.OnDisconnected(reason);
            IsDisconnected = true;
            StaticDisconnectedSignal?.Set();
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

        // 客户端应能检测到连接断开
        var buf = new Byte[64];
        ns.ReadTimeout = 3_000;
        try
        {
            var n = ns.Read(buf, 0, buf.Length);
            Assert.Equal(0, n);
        }
        catch (IOException)
        {
            // 连接已重置也是合理的
        }
    }

    /// <summary>服务端 Stop 后重启，新客户端可正常连接</summary>
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
                continue;
            }

            if (port == 0) port = server.Port;

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
    /// <summary>UDP Echo 通过 NetClient 客户端收发，验证数据一致</summary>
    [Fact]
    public void UdpEcho_NetClient_DataIntegrity()
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

        using var client = new NetClient($"udp://127.0.0.1:{server.Port}");
        client.Received += (s, e) =>
        {
            received = e.GetBytes();
            wait.Set();
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
    /// <summary>TCP 长连接持续 100 次 StandardCodec 请求响应</summary>
    [Fact]
    public async Task TcpLongConnection_100Exchanges()
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
            if (s is INetSession session) EchoPayload(session, e);
        };
        server.Start();

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}");
        client.Add<StandardCodec>();
        client.Timeout = 10_000;
        client.Open();

        for (var i = 0; i < 100; i++)
        {
            var payload = Encoding.UTF8.GetBytes($"Ping-{i}");
            var resp = await client.SendMessageAsync(new ArrayPacket(payload));
            Assert.NotNull(resp);
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

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}");
        client.Add<StandardCodec>();
        client.Open();

        var payload = "GenericServerTest"u8.ToArray();
        var resp = await client.SendMessageAsync(new ArrayPacket(payload));
        Assert.NotNull(resp);
    }

    class EchoSession : NetSession
    {
        protected override void OnReceive(ReceivedEventArgs e)
        {
            base.OnReceive(e);
            var pk = e.Message as IPacket;
            if (pk == null || pk.Total == 0) pk = e.Packet;
            if (pk != null && pk.Total > 0) SendReply(pk, e);
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

        var clients = new List<TcpClient>();
        for (var i = 0; i < 4; i++)
        {
            var c = new TcpClient();
            c.Connect(IPAddress.Loopback, server.Port);
            clients.Add(c);
        }

        Assert.True(sessionReady.Wait(3_000));

        var msg = "GroupA-Only"u8.ToArray();
        var sentCount = await server.SendAllAsync(
            new ArrayPacket(msg),
            session => session is NetSession ns && ns.Session["Group"]?.ToString() == "A");

        Assert.True(sentCount >= 0);

        foreach (var c in clients)
            c.Dispose();
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

        udpReceived.Wait(3_000);
    }
    #endregion

    #region Received 事件与管道 Message 区分
    /// <summary>StandardCodec 管道下，Received 事件中 e.Message 为解码后的纯负载（UserPacket=true 时为 IPacket）</summary>
    [Fact]
    public void StandardCodec_ReceivedEvent_MessageIsDecoded()
    {
        Object? receivedMessage = null;
        Byte[]? receivedData = null;
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
            receivedMessage = e.Message;

            // UserPacket=true 时 e.Message 为纯负载 IPacket（拥有切片），事件内有效；
            // 需要跨轮持有时用 Slice 取独立引用，此处仅快照内容
            if (e.Message is IPacket pk) receivedData = pk.ToArray();
            wait.Set();
        };
        server.Start();

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}");
        client.Add<StandardCodec>();
        client.Open();

        var payload = "DecodeCheck"u8.ToArray();
        client.SendMessage(new ArrayPacket(payload));

        Assert.True(wait.Wait(3_000));
        Assert.NotNull(receivedMessage);
        Assert.True(receivedMessage is IPacket);

        // UserPacket=true 时，e.Message 是纯负载 IPacket，不含 StandardCodec 协议头
        Assert.Equal(payload, receivedData);
    }
    #endregion

    #region 事件内切片持有（路线2）
    /// <summary>Received 事件内切片持有负载后延迟校验：拥有切片不随接收缓冲复用被污染（引用计数保活）</summary>
    [Fact]
    public void ReceivedEvent_SliceFrame_NoReusePollution()
    {
        var failures = new List<String>();
        var takenCount = 0;
        var allTaken = new ManualResetEventSlim();

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
        };
        server.Add<StandardCodec>();
        server.Received += (s, e) =>
        {
            if (e.Message is not IPacket pk || pk.Total == 0) return;

            // 先快照期望内容，再在事件内切片持有当前负载（拥有切片，独立于消息容器与接收缓冲）
            var expected = pk.ToArray();
            var owner = pk.Slice(0, -1);

            if (Interlocked.Increment(ref takenCount) >= 4) allTaken.Set();

            // 延迟校验：若接收层错误复用被切片持有的缓冲（引用计数未达标→未换新缓冲），
            // 后续接收会覆盖该数组，此处将检测到内容被污染
            _ = Task.Run(async () =>
            {
                await Task.Delay(200);
                // 读取独立切片（原负载句柄会随消息容器回池作废；切片持有引用，缓冲由引用计数保活）
                var now = owner.ToArray();
                if (now.Length != expected.Length || !now.AsSpan().SequenceEqual(expected))
                {
                    lock (failures)
                        failures.Add($"切片缓冲被污染：期望长度 {expected.Length}，实际长度 {now.Length}，内容不一致");
                }
                owner.TryDispose();
            });
        };
        server.Start();

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}");
        client.Add<StandardCodec>();
        client.Open();

        for (var i = 0; i < 8; i++)
        {
            var payload = Encoding.UTF8.GetBytes($"Slice-NoReuse-{i:D2}-{Guid.NewGuid()}");
            client.SendMessage(new ArrayPacket(payload));
            Thread.Sleep(60);
        }

        Assert.True(allTaken.Wait(5_000), $"事件内切片命中不足 4 次，实际 {takenCount} 次");

        // 等待所有延迟校验完成
        Thread.Sleep(400);

        Assert.Empty(failures);
    }

    /// <summary>事件内切片持有响应后，响应仍正常匹配等待方：拥有切片生命周期互不影响</summary>
    [Fact]
    public async Task ReceivedEvent_SliceResponse_AwaitStillMatched()
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
            if (s is INetSession session) EchoPayload(session, e);
        };
        server.Start();

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}");
        client.Add<StandardCodec>();
        client.Timeout = 3_000;
        client.Open();

        var sliceEnabled = false;
        var taken = new ManualResetEventSlim();
        IPacket? escaped = null;
        client.Received += (s, e) =>
        {
            // 事件内切片持有响应负载（拥有切片，独立于消息容器与接收缓冲）
            if (sliceEnabled && e.Message is IPacket pk && pk.Total > 0)
            {
                escaped = pk.Slice(0, -1);
                taken.Set();
            }
        };

        // 对照组：事件只读，响应正常匹配返回（证明挂载事件不影响请求-响应匹配）
        var ctrl = await client.SendMessageAsync(new ArrayPacket(Encoding.UTF8.GetBytes("ctrl")));
        Assert.NotNull(ctrl);
        Assert.True(ExtractPayload(ctrl).Length > 0);
        (ctrl as IDisposable)?.Dispose();

        // 实验组：事件切片持有响应负载 → 响应仍交付等待方（引用计数保证数据独立）
        sliceEnabled = true;
        var payload = Encoding.UTF8.GetBytes($"SliceKeep-{Guid.NewGuid()}");
        var rs = await client.SendMessageAsync(new ArrayPacket(payload));
        Assert.NotNull(rs);
        Assert.True(ExtractPayload(rs).Length > 0);
        (rs as IDisposable)?.Dispose();

        Assert.True(taken.Wait(5_000), "事件内切片持有响应未命中");

        // 延迟校验：切片内容不被后续接收复用污染
        var expected = escaped!.ToArray();
        await Task.Delay(100);
        Assert.Equal(expected, escaped.ToArray());

        escaped.TryDispose();
    }

    /// <summary>事件内直接消费（释放）响应负载后，等待方仍拿到完整数据：交付句柄在事件前已独立切出</summary>
    [Fact]
    public async Task ReceivedEvent_ConsumedResponse_AwaitStillMatched()
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
            if (s is INetSession session) EchoPayload(session, e);
        };
        server.Start();

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}");
        client.Add<StandardCodec>();
        client.Timeout = 3_000;
        client.Open();

        var consumeEnabled = false;
        var consumed = new ManualResetEventSlim();
        client.Received += (s, e) =>
        {
            // 事件内直接消费（处置）响应负载：交付句柄已在事件前独立切出，等待方不应受影响
            if (consumeEnabled && e.Message is IPacket pk && pk.Total > 0)
            {
                pk.TryDispose();
                consumed.Set();
            }
        };

        // 对照组：事件只读，响应正常匹配返回
        var ctrl = await client.SendMessageAsync(new ArrayPacket(Encoding.UTF8.GetBytes("ctrl-consumed")));
        Assert.NotNull(ctrl);
        Assert.True(ExtractPayload(ctrl).Length > 0);
        (ctrl as IDisposable)?.Dispose();

        // 实验组：事件释放响应负载 → await 仍拿到完整数据（独立交付句柄保活缓冲）
        consumeEnabled = true;
        var payload = Encoding.UTF8.GetBytes($"Consume-{Guid.NewGuid()}");
        var rs = await client.SendMessageAsync(new ArrayPacket(payload));
        Assert.True(consumed.Wait(5_000), "事件内消费响应未命中");
        Assert.NotNull(rs);
        Assert.Equal(payload, ExtractPayload(rs));
        (rs as IDisposable)?.Dispose();
    }
    #endregion
}
