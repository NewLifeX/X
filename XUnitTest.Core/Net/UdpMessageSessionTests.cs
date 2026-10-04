using System.Buffers;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NewLife;
using NewLife.Data;
using NewLife.Messaging;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

/// <summary>UDP 协议模式（数据报定界 + 消息分发）实网测试</summary>
[Collection("Net.C")]
public class UdpMessageSessionTests
{
    #region 宿主
    /// <summary>UDP 消息宿主：收集收到的行内容</summary>
    public class UdpMessageServer : NetServer<UdpMessageSession>
    {
        /// <summary>收到的消息内容，线程安全</summary>
        public ConcurrentQueue<String> ReceivedLines { get; } = new();
    }

    /// <summary>UDP 消息会话：读出行内容并回显</summary>
    public class UdpMessageSession : NetSession<UdpMessageServer>
    {
        protected override void OnReceive(ReceivedEventArgs e)
        {
            if (e.Message is not Message msg) return;

            var line = ReadBody(msg);
            if (line == null) return;

            Host.ReceivedLines.Enqueue(line);

            // 回显：协议构建后经底层会话发送
            var reply = new Message();
            reply.SetBody(new ArrayPacket(Encoding.UTF8.GetBytes("echo:" + line)));
            (Session as UdpSession)?.SendMessage(reply);
        }

        internal static String? ReadBody(Message msg)
        {
            var body = msg.Body;
            if (body == null) return null;

            // UDP 消息体为内存模式，读满立即完成
            var data = body.ReadAllAsync().AsTask().GetAwaiter().GetResult();
            var line = Encoding.UTF8.GetString(data.AsReadOnlySequence().ToArray());
            data.TryDispose();

            return line;
        }
    }
    #endregion

    #region 工具
    private static UdpMessageServer NewServer()
    {
        var server = new UdpMessageServer
        {
            Port = 0,
            ProtocolType = NetType.Udp,
            AddressFamily = AddressFamily.InterNetwork,
        };
        server.Protocol = new SplitDataCodec();
        server.Start();

        return server;
    }

    private static NetClient NewClient(UdpMessageServer server)
    {
        var client = new NetClient($"udp://127.0.0.1:{server.Port}")
        {
            Protocol = new SplitDataCodec(),
            AutoReconnect = false,
        };
        client.Open();

        return client;
    }

    /// <summary>轮询等待条件成立，必要时重复投递（UDP 在并行满载时可能丢包）</summary>
    private static async Task<Boolean> WaitUntilAsync(Func<Boolean> condition, Action? resend = null, Int32 timeoutMs = 10_000)
    {
        var start = Environment.TickCount64;
        while (Environment.TickCount64 - start < timeoutMs)
        {
            if (condition()) return true;

            resend?.Invoke();

            await Task.Delay(50);
        }

        return condition();
    }
    #endregion

    [Fact]
    [DisplayName("UDP协议_客户端到服务端_行协议消息")]
    public async Task ClientToServer()
    {
        using var server = NewServer();
        using var client = NewClient(server);

        var payload = Encoding.UTF8.GetBytes("hello");
        void Send()
        {
            var msg = new Message();
            msg.SetBody(new ArrayPacket(payload));
            client.SendMessage(msg);
        }
        Send();

        Assert.True(await WaitUntilAsync(() => server.ReceivedLines.Contains("hello"), Send));
    }

    [Fact]
    [DisplayName("UDP协议_服务端回显_客户端收到消息")]
    public async Task ServerToClient()
    {
        using var server = NewServer();
        using var client = NewClient(server);

        var received = new TaskCompletionSource<String>();
        client.Received += (s, e) =>
        {
            if (e.Message is not Message msg) return;

            var line = UdpMessageSession.ReadBody(msg);
            if (line != null) received.TrySetResult(line);
        };

        var payload = Encoding.UTF8.GetBytes("ping");
        void Send()
        {
            var msg = new Message();
            msg.SetBody(new ArrayPacket(payload));
            client.SendMessage(msg);
        }
        Send();

        // UDP 可能丢包，重发直到收到回显或超时
        var end = Environment.TickCount64 + 10_000;
        while (!received.Task.IsCompleted && Environment.TickCount64 < end)
        {
            var task = await Task.WhenAny(received.Task, Task.Delay(500));
            if (task == received.Task) break;

            Send();
        }

        Assert.True(received.Task.IsCompleted, "客户端应在超时内收到回显消息");
        Assert.Equal("echo:ping", await received.Task);
    }

    [Fact]
    [DisplayName("UDP协议_单数据报多帧_逐条分发")]
    public async Task MultiFramesInOneDatagram()
    {
        using var server = NewServer();
        using var client = NewClient(server);

        // 原始字节发送：一个数据报内两帧行协议
        var datagram = Encoding.UTF8.GetBytes("one\r\ntwo\r\n");
        client.Send(datagram);

        Assert.True(await WaitUntilAsync(() => server.ReceivedLines.Contains("one") && server.ReceivedLines.Contains("two"), () => client.Send(datagram)));
    }

    [Fact]
    [DisplayName("UDP协议_请求响应等待_明确拒绝")]
    public async Task RequestResponse_NotSupported()
    {
        using var server = NewServer();
        using var client = NewClient(server);

        var msg = new Message();
        msg.SetBody(new ArrayPacket(Encoding.UTF8.GetBytes("rpc")));

        await Assert.ThrowsAsync<NotSupportedException>(() => client.SendMessageAsync(msg).AsTask());
    }

    [Fact]
    [DisplayName("UDP协议_压缩协议_解压结果不被线上压缩字节覆盖")]
    public async Task CompressedCodec_BodyNotOverwritten()
    {
        // 压缩协议要求整帧（UDP 数据报自带完整帧），可验证“协议预绑定体不被二次绑定覆盖”
        using var server = new UdpMessageServer
        {
            Port = 0,
            ProtocolType = NetType.Udp,
            AddressFamily = AddressFamily.InterNetwork,
        };
        server.Protocol = new CompressedCodec(new SrmpCodec());
        server.Start();

        using var client = new NetClient($"udp://127.0.0.1:{server.Port}")
        {
            Protocol = new CompressedCodec(new SrmpCodec()),
            AutoReconnect = false,
        };
        client.Open();

        var payload = Encoding.UTF8.GetBytes("compressed-body");
        void Send()
        {
            var msg = new DefaultMessage { Sequence = 1 };
            msg.SetBody(new ArrayPacket(payload));
            client.SendMessage(msg);
        }
        Send();

        // 服务端应读到解压后的原文；
        // 修复前会被线上压缩字节覆盖（乱码），Contains 永不成立
        Assert.True(await WaitUntilAsync(() => server.ReceivedLines.Contains("compressed-body"), Send));
    }
}
