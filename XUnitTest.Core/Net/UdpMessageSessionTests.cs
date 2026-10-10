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

        internal static String? ReadBody(IMessage msg)
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
    /// <summary>UDP 消息宿主（SRMP）：请求-响应回显</summary>
    public class UdpSrmpServer : NetServer<UdpSrmpSession>
    {
        /// <summary>收到的消息内容，线程安全</summary>
        public ConcurrentQueue<String> ReceivedLines { get; } = new();

        /// <summary>是否应答。置 false 时静默丢弃请求，用于验证等待超时</summary>
        public Boolean Echo { get; set; } = true;
    }

    /// <summary>UDP 消息会话（SRMP）：按序列号应答</summary>
    public class UdpSrmpSession : NetSession<UdpSrmpServer>
    {
        protected override void OnReceive(ReceivedEventArgs e)
        {
            if (e.Message is not DefaultMessage msg || msg.Reply) return;

            var line = UdpMessageSession.ReadBody(msg);
            if (line == null) return;

            Host.ReceivedLines.Enqueue(line);

            if (!Host.Echo) return;

            var session = Session as UdpSession;
            var seq = msg.Sequence;

            // slow 开头延迟应答：让应答到达顺序与请求发出顺序相反，验证按序列号配对而非先到先得
            if (line.StartsWith("slow", StringComparison.Ordinal))
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(300).ConfigureAwait(false);

                    try
                    {
                        var slow = new DefaultMessage { Reply = true, Sequence = seq };
                        slow.SetBody(new ArrayPacket(Encoding.UTF8.GetBytes("echo:" + line)));
                        session?.SendMessage(slow);
                    }
                    catch (ObjectDisposedException) { }
                });

                return;
            }

            var reply = new DefaultMessage { Reply = true, Sequence = seq };
            reply.SetBody(new ArrayPacket(Encoding.UTF8.GetBytes("echo:" + line)));
            session?.SendMessage(reply);
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

    private static UdpSrmpServer NewSrmpServer(UdpSrmpServer? server = null)
    {
        server ??= new UdpSrmpServer
        {
            Port = 0,
            ProtocolType = NetType.Udp,
            AddressFamily = AddressFamily.InterNetwork,
        };
        server.Protocol = new SrmpCodec();
        server.Start();

        return server;
    }

    private static NetClient NewSrmpClient(UdpSrmpServer server, Int32 matchTimeout = 2_000)
    {
        var client = new NetClient($"udp://127.0.0.1:{server.Port}")
        {
            Protocol = new SrmpCodec(),
            MatchTimeout = matchTimeout,
            AutoReconnect = false,
        };
        client.Open();

        return client;
    }

    /// <summary>发起一次请求-响应调用。UDP 不可靠，等待超时后重试</summary>
    private static async Task<String?> RpcAsync(NetClient client, Int32 sequence, String text, Int32 retries = 3)
    {
        for (var i = 0; ; i++)
        {
            try
            {
                var req = new DefaultMessage { Sequence = sequence };
                req.SetBody(new ArrayPacket(Encoding.UTF8.GetBytes(text)));

                var resp = await client.SendMessageAsync(req);
                try
                {
                    return UdpMessageSession.ReadBody(resp);
                }
                finally
                {
                    resp.TryDispose();
                }
            }
            catch (OperationCanceledException) when (i < retries - 1) { }
        }
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
    [DisplayName("UDP协议_请求响应_按序列号匹配并交付")]
    public async Task RequestResponse_RoundTrip()
    {
        using var server = NewSrmpServer();
        using var client = NewSrmpClient(server);

        var line = await RpcAsync(client, 1, "rpc");

        Assert.Equal("echo:rpc", line);
        Assert.Contains("rpc", server.ReceivedLines);
    }

    [Fact]
    [DisplayName("UDP协议_请求响应_应答乱序到达仍各归其主")]
    public async Task RequestResponse_MatchBySequence()
    {
        using var server = NewSrmpServer();
        using var client = NewSrmpClient(server);

        // 慢请求先发出、应答延迟300ms；快请求随后发出、立即应答。
        // 应答到达顺序与请求发出顺序相反，只有按序列号配对才能各自取回自己的响应（先到先得的配对会张冠李戴）
        var slowTask = RpcAsync(client, 1, "slow-1");
        await Task.Delay(50);
        var fastTask = RpcAsync(client, 2, "fast-2");

        Assert.Equal("echo:fast-2", await fastTask);
        Assert.Equal("echo:slow-1", await slowTask);
    }

    [Fact]
    [DisplayName("UDP协议_请求响应_应答先经事件链再交付等待方")]
    public async Task RequestResponse_DeliveryAfterEventChain()
    {
        using var server = NewSrmpServer();
        using var client = NewSrmpClient(server);

        // 事件链内先读空响应体，等待方仍须拿到完整响应（交付前体复位）
        var observed = new ConcurrentQueue<String>();
        client.Received += (s, e) =>
        {
            if (e.Message is not IMessage msg || !msg.Reply) return;

            var text = UdpMessageSession.ReadBody(msg);
            if (text != null) observed.Enqueue(text);
        };

        var line = await RpcAsync(client, 7, "visible");

        Assert.Equal("echo:visible", line);
        Assert.Contains("echo:visible", observed);
    }

    [Fact]
    [DisplayName("UDP协议_请求响应_无应答时超时")]
    public async Task RequestResponse_Timeout()
    {
        using var server = NewSrmpServer();
        server.Echo = false;
        using var client = NewSrmpClient(server, matchTimeout: 300);

        var msg = new DefaultMessage { Sequence = 1 };
        msg.SetBody(new ArrayPacket(Encoding.UTF8.GetBytes("no-reply")));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.SendMessageAsync(msg).AsTask());
    }

    [Fact]
    [DisplayName("UDP协议_请求响应_协议未实现配对_明确拒绝")]
    public async Task RequestResponse_ProtocolWithoutMatcher_NotSupported()
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
