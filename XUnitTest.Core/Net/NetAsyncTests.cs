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

/// <summary>网络异步操作测试</summary>
[TestCaseOrderer("NewLife.UnitTest.DefaultOrderer", "NewLife.UnitTest")]
public class NetAsyncTests
{
    #region 异步接收测试
    /// <summary>测试异步接收数据</summary>
    [Fact]
    public async Task AsyncReceiveTest()
    {
        var receivedData = new List<Byte[]>();

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
        };

        server.Received += (s, e) =>
        {
            if (s is INetSession session && e.Packet != null)
            {
                lock (receivedData)
                {
                    receivedData.Add(e.Packet.ToArray());
                }
                // Echo
                session.Send(e.Packet);
            }
        };

        server.Start();

        // 客户端使用TcpSession进行异步通信
        var uri = new NetUri($"tcp://127.0.0.1:{server.Port}");
        var client = uri.CreateRemote();
        client.Log = XTrace.Log;
        if (client is TcpSession tcp) tcp.MaxAsync = 0;
        client.Open();

        // 发送数据
        var sendData = "Hello Async"u8.ToArray();
        client.Send(sendData);

        // 异步接收
        using var cts = new CancellationTokenSource(5000);
        var pk = await client.ReceiveAsync(cts.Token);

        Assert.NotNull(pk);
        Assert.Equal(sendData.Length, pk.Length);
        Assert.Equal(sendData, pk.ToArray());

        client.Close("Test");
    }

    /// <summary>测试异步接收超时</summary>
    [Fact]
    public async Task AsyncReceiveTimeoutTest()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
        };

        // 服务端不回复数据
        server.Received += (s, e) => { };

        server.Start();

        var uri = new NetUri($"tcp://127.0.0.1:{server.Port}");
        var client = uri.CreateRemote();
        client.Timeout = 1000; // 1秒超时
        client.Log = XTrace.Log;
        client.Open();

        // 发送数据
        client.Send("test"u8.ToArray());

        // 异步接收应该超时
        using var cts = new CancellationTokenSource(500);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await client.ReceiveAsync(cts.Token);
        });

        client.Close("Test");
    }
    #endregion

    #region SendMessageAsync测试
    /// <summary>测试SendMessageAsync异步发送消息</summary>
    [Fact]
    public async Task SendMessageAsyncTest()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
        };

        server.Add<StandardCodec>();
        server.Received += (s, e) =>
        {
            if (s is INetSession session && e.Packet != null)
            {
                // 回复消息
                session.SendReply(e.Packet, e);
            }
        };

        server.Start();

        var uri = new NetUri($"tcp://127.0.0.1:{server.Port}");
        var client = uri.CreateRemote();
        client.Add<StandardCodec>();
        client.Log = XTrace.Log;
        client.Open();

        // 发送消息并等待响应
        var sendData = new ArrayPacket("Test Message"u8.ToArray());
        var response = await client.SendMessageAsync(sendData);

        Assert.NotNull(response);

        client.Close("Test");
    }

    /// <summary>测试SendMessageAsync带取消令牌（取消时抛出异常）</summary>
    [Fact]
    public async Task SendMessageAsyncWithCancellationTest()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
        };

        server.Add<StandardCodec>();
        // 服务端不回复，让客户端超时
        server.Received += (s, e) => { };

        server.Start();

        var uri = new NetUri($"tcp://127.0.0.1:{server.Port}");
        var client = uri.CreateRemote();
        client.Add<StandardCodec>();
        client.Log = XTrace.Log;
        client.Open();

        try
        {
            // 发送消息，但很快取消
            var sendData = new ArrayPacket("Cancel Test"u8.ToArray());
            using var cts = new CancellationTokenSource(500);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await client.SendMessageAsync(sendData, cts.Token);
            });
        }
        finally
        {
            client.Close("Test");
        }
    }
    #endregion

    #region 同步发送测试
    /// <summary>测试SendMessage同步发送</summary>
    [Fact]
    public void SendMessageSyncTest()
    {
        var messageReceived = new ManualResetEventSlim(false);
        IPacket? receivedPacket = null;

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
        };

        server.Add<StandardCodec>();
        server.Received += (s, e) =>
        {
            if (e.Packet != null)
            {
                receivedPacket = e.Packet;
                messageReceived.Set();
            }
        };

        server.Start();

        var uri = new NetUri($"tcp://127.0.0.1:{server.Port}");
        var client = uri.CreateRemote();
        client.Add<StandardCodec>();
        client.Log = XTrace.Log;
        client.Open();

        // 同步发送消息
        var sendData = new ArrayPacket("Sync Message"u8.ToArray());
        var result = client.SendMessage(sendData);

        Assert.True(result > 0);
        Assert.True(messageReceived.Wait(3000));
        Assert.NotNull(receivedPacket);

        client.Close("Test");
    }
    #endregion

    #region 多次异步通信测试
    /// <summary>测试多次异步请求响应</summary>
    [Fact]
    public async Task MultipleAsyncRequestsTest()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
        };

        server.Add<StandardCodec>();
        server.Received += (s, e) =>
        {
            if (s is INetSession session && e.Packet != null)
            {
                // 简单Echo
                session.SendReply(e.Packet, e);
            }
        };

        server.Start();

        var uri = new NetUri($"tcp://127.0.0.1:{server.Port}");
        var client = uri.CreateRemote();
        client.Add<StandardCodec>();
        client.Log = XTrace.Log;
        client.Open();

        // 连续发送多个请求
        for (var i = 0; i < 5; i++)
        {
            var msg = $"Request {i}";
            var sendData = new ArrayPacket(Encoding.UTF8.GetBytes(msg));
            var response = await client.SendMessageAsync(sendData);

            Assert.NotNull(response);
        }

        client.Close("Test");
    }

    /// <summary>测试并发异步请求</summary>
    [Fact]
    public async Task ConcurrentAsyncRequestsTest()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
        };

        server.Add<StandardCodec>();
        //server.Add(new StandardCodec { UserPacket = false });
        server.Received += (s, e) =>
        {
            if (s is INetSession session && e.Message is IPacket pk)
            {
                XTrace.WriteLine("收到：{0}", pk.ToStr());
                session.SendReply(pk, e);
            }
        };

        server.Start();

        var uri = new NetUri($"tcp://127.0.0.1:{server.Port}");
        var client = uri.CreateRemote();
        client.Add<StandardCodec>();
        client.Log = XTrace.Log;
        client.LogSend = true;
        client.LogReceive = true;
        client.Timeout = 3_000;
        client.Open();

        // 并发发送多个请求
        var tasks = new List<Task<Object>>();
        for (var i = 0; i < 3; i++)
        {
            var idx = i;
            var msg = $"Concurrent {idx}";
            var sendData = new ArrayPacket(Encoding.UTF8.GetBytes(msg));
            tasks.Add(client.SendMessageAsync(sendData));
        }

        var rs = await Task.WhenAll(tasks);
        foreach (var response in rs)
        {
            Assert.NotNull(response);
        }

        client.Close("Test");
    }
    #endregion

    #region UDP异步测试
    /// <summary>测试UDP异步收发</summary>
    [Fact]
    public async Task UdpAsyncTest()
    {
        var receivedEvent = new ManualResetEventSlim(false);

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Udp,
            Log = XTrace.Log,
        };

        server.Received += (s, e) =>
        {
            if (s is INetSession session && e.Packet != null)
            {
                session.Send(e.Packet);
                receivedEvent.Set();
            }
        };

        server.Start();

        var uri = new NetUri($"udp://127.0.0.1:{server.Port}");
        var client = uri.CreateRemote();
        client.Log = XTrace.Log;
        client.Open();

        // 发送数据
        var sendData = "UDP Async Test"u8.ToArray();
        client.Send(sendData);

        Assert.True(receivedEvent.Wait(3000));

        client.Close("Test");
    }
    #endregion

    #region 打开关闭测试
    /// <summary>测试异步打开连接</summary>
    [Fact]
    public async Task AsyncOpenTest()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
        };

        server.Start();

        var uri = new NetUri($"tcp://127.0.0.1:{server.Port}");
        var client = uri.CreateRemote() as TcpSession;
        Assert.NotNull(client);
        client.Log = XTrace.Log;

        // 异步打开
        using var cts = new CancellationTokenSource(5000);
        var result = await client.OpenAsync(cts.Token);

        Assert.True(result);
        Assert.True(client.Active);

        client.Close("Test");
    }

    /// <summary>测试异步关闭连接</summary>
    [Fact]
    public async Task AsyncCloseTest()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
        };

        server.Start();

        var uri = new NetUri($"tcp://127.0.0.1:{server.Port}");
        var client = uri.CreateRemote() as TcpSession;
        Assert.NotNull(client);
        client.Log = XTrace.Log;
        client.Open();

        Assert.True(client.Active);

        // 异步关闭
        using var cts = new CancellationTokenSource(5000);
        var result = await client.CloseAsync("AsyncClose", cts.Token);

        Assert.True(result);
        Assert.False(client.Active);
    }
    #endregion

    #region Received事件异步处理
    /// <summary>测试Received事件中异步处理</summary>
    [Fact]
    public async Task AsyncReceivedHandlerTest()
    {
        var processCompleted = new ManualResetEventSlim(false);
        var processedData = String.Empty;

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            Log = XTrace.Log,
        };

        server.Received += async (s, e) =>
        {
            if (s is INetSession session && e.Packet != null)
            {
                // 模拟异步处理
                await Task.Delay(100);
                processedData = e.Packet.ToStr();

                // 异步处理后回复
                session.Send($"Processed: {processedData}");
                processCompleted.Set();
            }
        };

        server.Start();

        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);
        var ns = client.GetStream();

        // 发送数据
        ns.Write("Async Handler Test"u8.ToArray());

        Assert.True(processCompleted.Wait(5000));
        Assert.Equal("Async Handler Test", processedData);

        // 验证收到响应
        var buf = new Byte[1024];
        var len = ns.Read(buf, 0, buf.Length);
        var response = Encoding.UTF8.GetString(buf, 0, len);
        Assert.Contains("Processed:", response);
    }
    #endregion

    #region 群发异步测试
    /// <summary>测试异步群发</summary>
    [Fact]
    public async Task AsyncBroadcastTest()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            UseSession = true,
            Log = XTrace.Log,
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

        Thread.Sleep(500);

        // 异步群发
        var sendData = new ArrayPacket("Async Broadcast"u8.ToArray());
        var count = await server.SendAllAsync(sendData);

        Assert.Equal(3, count);

        // 清理
        foreach (var client in clients)
            client.Dispose();
    }
    #endregion
}
