using System.Buffers;
using System.ComponentModel;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using NewLife;
using NewLife.Data;
using NewLife.Messaging;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

// 协议交付契约：处理器返回后消息即收尾，处理器内需同步完成负载消费（慢路径等待为同连接串行语义），不适用 xUnit1031
#pragma warning disable xUnit1031

/// <summary>协议模式（Protocol 属性 + 消息泵）实网回环测试</summary>
[Collection("Net")]
public class MessageSessionTests
{
    #region 工具
    private static async Task<T> WithTimeout<T>(Task<T> task, Int32 timeoutMs = 10_000)
    {
        var completed = await Task.WhenAny(task, Task.Delay(timeoutMs)).ConfigureAwait(false);
        if (completed != task) throw new TimeoutException("等待消息超时");

        return await task.ConfigureAwait(false);
    }

    private static Byte[] MakePayload(Int32 count)
    {
        var buf = new Byte[count];
        for (var i = 0; i < count; i++) buf[i] = (Byte)(i * 31 + 7);

        return buf;
    }

    private static TaskCompletionSource<T> NewTcs<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>加载测试自签名证书（内嵌 pfx）</summary>
    private static X509Certificate2 LoadTestCert()
    {
        var pfx = typeof(MessageSessionTests).Assembly.GetManifestResourceStream("XUnitTest.certs.newlifex.com.pfx")!.ReadBytes(-1);
#if NET9_0_OR_GREATER
        return X509CertificateLoader.LoadPkcs12(pfx, "123456");
#else
        return new X509Certificate2(pfx, "123456", X509KeyStorageFlags.DefaultKeySet);
#endif
    }
    #endregion

    #region 匹配队列
    [Fact]
    [DisplayName("匹配队列_超时_取消池化等待源")]
    public async Task MatchQueue_Timeout_CancelsSource()
    {
        var queue = new DefaultMatchQueue();
        var source = PooledValueTaskSource<Message>.Rent();
        queue.Add(this, new DefaultMessage { Sequence = 1 }, 300, source);

        var task = source.ValueTask.AsTask();

        // 超时取消由共享的 1 秒周期 TimerX 驱动，回调还要经线程池调度；并行跑测试时抖动可能远超 1 秒。
        // 本用例只验证“到期必然取消”这一契约，故给足余量——曾因 10 秒上限在与网络用例同批跑时被拖超时
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => WithTimeout(task, 60_000));
    }

    [Fact]
    [DisplayName("匹配队列_清空_取消池化等待源")]
    public async Task MatchQueue_Clear_CancelsSource()
    {
        var queue = new DefaultMatchQueue();
        var source = PooledValueTaskSource<Message>.Rent();
        queue.Add(this, new DefaultMessage { Sequence = 1 }, 300, source);

        var task = source.ValueTask.AsTask();

        // 与超时路径共用同一套取消逻辑，但同步触发、不依赖定时器调度，因此不会因机器负载而飘
        queue.Clear();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
    }

    [Fact]
    [DisplayName("匹配队列_回调命中_结果交付池化源")]
    public void MatchQueue_Match_DeliversSource()
    {
        var queue = new DefaultMatchQueue();
        var source = PooledValueTaskSource<Message>.Rent();
        var matcher = new SrmpCodec();

        var req = new DefaultMessage { Sequence = 1 };
        queue.Add(this, req, 10_000, source);

        var resp = new DefaultMessage { Sequence = 1, Kind = MessageKinds.Response };
        var ok = queue.Match(this, resp, resp, (rq, rs) => rq is Message a && rs is Message b && matcher.Match(a, b));

        Assert.True(ok);
        Assert.True(source.ValueTask.IsCompleted);
        Assert.Same(resp, source.ValueTask.Result);
    }

    [Fact]
    [DisplayName("协议匹配_序列号超过255_按低8位配对")]
    public void SrmpMatch_LargeSequence_MatchesLowByte()
    {
        var matcher = new SrmpCodec();

        var req = new DefaultMessage { Sequence = 300 };

        // 线格式只带 1 字节序列号，对端回显的是低 8 位
        var resp = new DefaultMessage { Sequence = 300 & 0xFF, Kind = MessageKinds.Response };

        Assert.NotEqual(req.Sequence, resp.Sequence);
        Assert.True(matcher.Match(req, resp));

        // 低 8 位不同则不配对
        resp.Sequence = (300 & 0xFF) + 1;
        Assert.False(matcher.Match(req, resp));
    }

    [Fact]
    [DisplayName("匹配队列_等待方已取消_完成失败按未命中返回")]
    public void MatchQueue_WaiterCanceled_ReturnsFalse()
    {
        var queue = new DefaultMatchQueue();
        var source = PooledValueTaskSource<Message>.Rent();
        var matcher = new SrmpCodec();

        var req = new DefaultMessage { Sequence = 8 };
        queue.Add(this, req, 10_000, source);

        // 等待方先行放弃（取消），而队列项仍在；此时迟到响应到达
        Assert.True(source.TrySetCanceled());

        var resp = new DefaultMessage { Sequence = 8, Kind = MessageKinds.Response };
        var ok = queue.Match(this, resp, resp, (rq, rs) => rq is Message a && rs is Message b && matcher.Match(a, b));

        // 完成失败必须按“未命中”返回：调用方据此丢弃负载并释放消息，否则消息与池化缓冲泄漏
        Assert.False(ok);
    }

    [Fact]
    [DisplayName("匹配队列_池化源回收复用_残留项不得完成新请求")]
    public async Task MatchQueue_StaleItem_DoesNotCompleteReusedSource()
    {
        var queue = new DefaultMatchQueue();
        var matcher = new SrmpCodec();

        // 第一次等待：入队后取消，等待方结束并归还池
        var first = PooledValueTaskSource<Message>.Rent();
        var oldRequest = new DefaultMessage { Sequence = 0x21 };
        queue.Add(this, oldRequest, 10_000, first);

        var oldTask = first.ValueTask.AsTask();
        Assert.True(first.TrySetCanceled());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => oldTask);

        // 归还后同一实例通常被下一个请求借出复用（池为 LIFO）。不硬断言 Same：xUnit 并行下
        // 可能被其它用例抢走，断言失败会变成与产品缺陷无关的假红；两种情形都必须“不得完成新等待源”
        var second = PooledValueTaskSource<Message>.Rent();

        var newRequest = new DefaultMessage { Sequence = 0x22 };
        queue.Add(this, newRequest, 10_000, second);

        // 迟到响应只能匹配上残留的旧队列项：完成必须失败，且不得完成复用后的新请求
        var lateResponse = new DefaultMessage { Sequence = 0x21, Kind = MessageKinds.Response };
        var ok = queue.Match(this, lateResponse, lateResponse, (rq, rs) => rq is Message a && rs is Message b && matcher.Match(a, b));

        Assert.False(ok);
        Assert.False(second.ValueTask.IsCompleted);
    }
    #endregion

    [Fact]
    [DisplayName("协议模式_小消息往返_服务端应答")]
    public async Task SmallMessage_RoundTrip()
    {
        using var server = new NetServer { Port = 0, Protocol = new SrmpCodec() };
        server.Start();

        var serverGot = NewTcs<(Int32 Sequence, Boolean OneWay, Byte[] Body)>();
        server.Received += (s, e) =>
        {
            if (e.Message is not DefaultMessage req) return;

            // 交付契约：处理器返回后消息收尾（未读体丢弃、消息释放）；
            // 需要负载时在本方法内读完（数据未到齐会等待——同连接消息串行）
            var all = req.Body!.ReadAllAsync().AsTask().GetAwaiter().GetResult();
            var body = all.AsReadOnlySequence().ToArray();
            all.TryDispose();

            serverGot.TrySetResult((req.Sequence, req.Kind == MessageKinds.OneWay, body));

            // 服务端应答（经协议构建整帧发送）
            var reply = (DefaultMessage)req.CreateReply();
            reply.SetBody(new ArrayPacket(new Byte[] { 0x0B, 0x0C }));
            ((INetSession)s!).SendMessage(reply);
        };

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}") { Protocol = new SrmpCodec() };
        var clientGot = NewTcs<(Int32 Sequence, Boolean Reply, Byte[] Body)>();
        client.Received += (s, e) =>
        {
            if (e.Message is not DefaultMessage msg || msg.Kind < MessageKinds.Response) return;

            // 处理器内读完负载（交付契约：返回后消息收尾）
            var all = msg.Body!.ReadAllAsync().AsTask().GetAwaiter().GetResult();
            var body = all.AsReadOnlySequence().ToArray();
            all.TryDispose();

            clientGot.TrySetResult((msg.Sequence, msg.Kind >= MessageKinds.Response, body));
        };
        Assert.True(client.Open());

        var reqMsg = new DefaultMessage { Sequence = 0x35 };
        reqMsg.SetBody(new ArrayPacket(new Byte[] { 0x01, 0x02, 0x03 }));
        client.SendMessage(reqMsg);

        // 服务端收到请求：字段与负载完整
        var (seq, oneWay, body) = await WithTimeout(serverGot.Task);
        Assert.Equal(0x35, seq);
        Assert.False(oneWay);
        Assert.Equal(new Byte[] { 0x01, 0x02, 0x03 }, body);

        // 客户端收到应答：Reply 且序列号配对
        var (rseq, replyFlag, replyBody) = await WithTimeout(clientGot.Task);
        Assert.True(replyFlag);
        Assert.Equal(0x35, rseq);
        Assert.Equal(new Byte[] { 0x0B, 0x0C }, replyBody);
    }

    [Fact]
    [DisplayName("协议模式_大帧_流式读取完整")]
    public async Task LargeMessage_Streaming()
    {
        var payload = MakePayload(300_000);

        using var server = new NetServer { Port = 0, Protocol = new SrmpCodec() };
        server.Start();

        var got = NewTcs<Byte[]>();
        server.Received += (s, e) =>
        {
            if (e.Message is not DefaultMessage req) return;

            try
            {
                // 头部到齐即已交付，负载流式读取；大帧数据未到齐时等待（同连接消息串行）
                var all = req.Body!.ReadAllAsync().AsTask().GetAwaiter().GetResult();
                var body = all.AsReadOnlySequence().ToArray();
                all.TryDispose();

                got.TrySetResult(body);
            }
            catch (Exception ex)
            {
                got.TrySetException(ex);
            }
        };

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}") { Protocol = new SrmpCodec() };
        Assert.True(client.Open());

        var reqMsg = new DefaultMessage { Sequence = 0x11 };
        reqMsg.SetBody(new ArrayPacket(payload));
        client.SendMessage(reqMsg);

        var body = await WithTimeout(got.Task);
        Assert.Equal(payload, body);
    }

    [Fact]
    [DisplayName("协议模式_单向消息_不等待响应")]
    public async Task OneWayMessage_NoReply()
    {
        using var server = new NetServer { Port = 0, Protocol = new SrmpCodec() };
        server.Start();

        var got = NewTcs<Boolean>();
        server.Received += (s, e) =>
        {
            if (e.Message is not DefaultMessage req) return;

            // 仅头部语义即可处理：不读负载，未读体由消息泵在处理器返回后丢弃对齐
            got.TrySetResult(req.Kind == MessageKinds.OneWay);
        };

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}") { Protocol = new SrmpCodec() };
        Assert.True(client.Open());

        var reqMsg = new DefaultMessage { Sequence = 0x22, Kind = MessageKinds.OneWay };
        reqMsg.SetBody(new ArrayPacket(new Byte[] { 1 }));
        client.SendMessage(reqMsg);

        Assert.True(await WithTimeout(got.Task));
    }

    [Fact]
    [DisplayName("协议模式_请求响应_客户端等待匹配")]
    public async Task RequestResponse_ClientAwaits()
    {
        using var server = new NetServer { Port = 0, Protocol = new SrmpCodec() };
        server.Start();

        var serverGot = NewTcs<Int32>();
        server.Received += (s, e) =>
        {
            if (e.Message is not DefaultMessage req) return;

            // 回显应答：物化请求负载作为响应体（所有权转移）
            var all = req.Body!.ReadAllAsync().AsTask().GetAwaiter().GetResult();
            var reply = (DefaultMessage)req.CreateReply();
            reply.SetBody(all);
            serverGot.TrySetResult(((INetSession)s!).SendMessage(reply));
        };

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}") { Protocol = new SrmpCodec() };
        var pushed = 0;
        client.Received += (s, e) => Interlocked.Increment(ref pushed);
        Assert.True(client.Open());

        var req = new DefaultMessage { Sequence = 0x42 };
        req.SetBody(new ArrayPacket(new Byte[] { 5, 6, 7 }));

        var reqTask = client.SendMessageAsync(req).AsTask();

        // 服务端已收到请求并完成应答发送
        var sent = await WithTimeout(serverGot.Task, 5_000);
        Assert.True(sent > 0, "服务端应答发送失败");

        var resp = (DefaultMessage)await WithTimeout(reqTask, 8_000);
        Assert.Equal(MessageKinds.Response, resp.Kind);
        Assert.Equal(0x42, resp.Sequence);

        // 响应先进入事件链（可观测），随后匹配交付等待方；交付后跳过收尾，消息由等待方释放
        Assert.Equal(1, pushed);

        // 交付消息体为内存模式（流式负载已物化），等待方可异步消费
        Assert.False(resp.Body!.IsStreaming);
        var body = await resp.Body.ReadAllAsync();
        Assert.Equal(new Byte[] { 5, 6, 7 }, body.AsReadOnlySequence().ToArray());
        body.TryDispose();

        // 等待方负责释放响应消息
        resp.Dispose();
    }

    [Fact]
    [DisplayName("协议模式_请求响应_序列号超过255仍能配对")]
    public async Task RequestResponse_SequenceOver255_StillMatches()
    {
        using var server = new NetServer { Port = 0, Protocol = new SrmpCodec() };
        server.Start();

        var serverGot = NewTcs<Int32>();
        server.Received += (s, e) =>
        {
            if (e.Message is not DefaultMessage req) return;

            // 回显应答：物化请求负载作为响应体（所有权转移）
            var all = req.Body!.ReadAllAsync().AsTask().GetAwaiter().GetResult();
            var reply = (DefaultMessage)req.CreateReply();
            reply.SetBody(all);
            serverGot.TrySetResult(((INetSession)s!).SendMessage(reply));
        };

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}")
        {
            Protocol = new SrmpCodec(),
            MatchTimeout = 3_000,
        };
        Assert.True(client.Open());

        // 客户端用自增计数器：序列号超过 255 时线格式只保留低 8 位。
        // 若按完整 Int32 比较则恒不配对，只能等 MatchTimeout 超时取消
        var req = new DefaultMessage { Sequence = 300 };
        req.SetBody(new ArrayPacket(new Byte[] { 7, 8 }));

        var reqTask = client.SendMessageAsync(req).AsTask();

        var sent = await WithTimeout(serverGot.Task, 5_000);
        Assert.True(sent > 0, "服务端应答发送失败");

        // 配对超时内完成交付：修复前此处必然抛超时取消
        var resp = (DefaultMessage)await WithTimeout(reqTask, 8_000);
        Assert.Equal(MessageKinds.Response, resp.Kind);
        Assert.Equal(300 & 0xFF, resp.Sequence);

        var body = await resp.Body!.ReadAllAsync();
        Assert.Equal(new Byte[] { 7, 8 }, body.AsReadOnlySequence().ToArray());
        body.TryDispose();
        resp.Dispose();
    }

    [Fact]
    [DisplayName("协议模式_请求响应_无应答超时取消")]
    public async Task RequestResponse_Timeout()
    {
        using var server = new NetServer { Port = 0, Protocol = new SrmpCodec() };
        server.Start();

        // 服务端收到请求但不作答
        var got = NewTcs<Boolean>();
        server.Received += (s, e) => got.TrySetResult(true);

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}")
        {
            Protocol = new SrmpCodec(),
            MatchTimeout = 300,
        };
        Assert.True(client.Open());

        var req = new DefaultMessage { Sequence = 0x77 };
        req.SetBody(new ArrayPacket(new Byte[] { 1 }));

        var reqTask = client.SendMessageAsync(req).AsTask();

        // 服务端应能收到请求
        Assert.True(await WithTimeout(got.Task, 5_000), "服务端未收到请求");

        // 匹配队列超时（定时器秒级精度）取消等待方
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => WithTimeout(reqTask, 10_000));
    }

    [Fact]
    [DisplayName("协议模式_超限残余_会话被关闭")]
    public async Task GarbageData_ExceedsMaxCache_ClosesConnection()
    {
        using var server = new NetServer { Port = 0, Protocol = new SrmpCodec(), MaxCache = 256 };
        server.Start();

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}") { AutoReconnect = false };
        client.Open();

        var closed = NewTcs<Boolean>();
        client.Closed += (s, e) => closed.TrySetResult(true);

        // 原始字节发送：0xFFFF 扩展头声明负数长度，永远无法定界
        var garbage = new Byte[1024];
        for (var i = 0; i < garbage.Length; i++) garbage[i] = 0xFF;
        client.Send(garbage);

        // 服务端泵判定协议错误并关闭会话，客户端感知断开
        var task = await Task.WhenAny(closed.Task, Task.Delay(5_000));
        Assert.True(task == closed.Task, "客户端应在超时内感知服务端关闭连接");
        Assert.True(await closed.Task);
    }

    [Fact]
    [DisplayName("协议模式_流式发送_服务端收流式体")]
    public async Task StreamingSend_RoundTrip()
    {
        using var server = new NetServer { Port = 0, Protocol = new SrmpCodec() };
        server.Start();

        var serverGot = NewTcs<Byte[]>();
        server.Received += (s, e) =>
        {
            if (e.Message is not DefaultMessage req) return;

            var all = req.Body!.ReadAllAsync().AsTask().GetAwaiter().GetResult();
            var body = all.AsReadOnlySequence().ToArray();
            all.TryDispose();
            serverGot.TrySetResult(body);
        };

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}") { Protocol = new SrmpCodec() };
        client.Open();

        // 流式发送 300KB：头部先行 + 流内容分块（内容不整载）
        var payload = MakePayload(300_000);
        using var stream = new MemoryStream(payload);
        var sent = await client.SendMessageAsync(new DefaultMessage { Sequence = 0x33 }, stream, payload.Length);

        Assert.Equal(payload.Length, sent);
        Assert.Equal(payload, await WithTimeout(serverGot.Task));
    }

    #region 会话处理器
    /// <summary>协议宿主：走 INetHandler 处理器分发（验证处理器可经事件参数取得消息）</summary>
    public class HandlerServer : NetServer<HandlerSession>
    {
        /// <summary>处理器收到的消息体</summary>
        public TaskCompletionSource<String> Got { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override INetHandler? CreateHandler(INetSession session) => new MessageNetHandler(this);
    }

    /// <summary>会话处理器：从事件参数转型取消息（协议模式契约）</summary>
    public class MessageNetHandler : INetHandler
    {
        private readonly HandlerServer _server;

        public MessageNetHandler(HandlerServer server) => _server = server;

        public void Init(INetSession session) { }

        public void Process(IData data)
        {
            // 契约：data 实际为 ReceivedEventArgs，协议模式下消息在 Message
            if (data is not ReceivedEventArgs e || e.Message is not DefaultMessage msg) return;

            var all = msg.Body!.ReadAllAsync().AsTask().GetAwaiter().GetResult();
            var body = all.AsReadOnlySequence().ToArray();
            all.TryDispose();
            _server.Got.TrySetResult(Encoding.UTF8.GetString(body));
        }
    }

    public class HandlerSession : NetSession<HandlerServer> { }
    #endregion

    [Fact]
    [DisplayName("协议模式_会话处理器_收到带消息的事件参数")]
    public async Task Protocol_NetHandler()
    {
        using var server = new HandlerServer { Port = 0, Protocol = new SrmpCodec() };
        server.Start();

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}") { Protocol = new SrmpCodec() };
        client.Open();

        var msg = new DefaultMessage { Sequence = 1 };
        msg.SetBody(new ArrayPacket(Encoding.UTF8.GetBytes("via-handler")));
        client.SendMessage(msg);

        Assert.Equal("via-handler", await WithTimeout(server.Got.Task));
    }

    [Fact]
    [DisplayName("协议模式_SSL回环_流式消息完整")]
    public async Task SslProtocol_RoundTrip()
    {
        using var cert = LoadTestCert();

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            SslProtocol = SslProtocols.Tls12,
            Certificate = cert,
            Protocol = new SrmpCodec(),
        };
        server.Start();

        var serverGot = NewTcs<Byte[]>();
        server.Received += (s, e) =>
        {
            if (e.Message is not DefaultMessage req) return;

            var all = req.Body!.ReadAllAsync().AsTask().GetAwaiter().GetResult();
            var body = all.AsReadOnlySequence().ToArray();
            all.TryDispose();
            serverGot.TrySetResult(body);
        };

        using var client = new TcpSession
        {
            Remote = new NetUri($"tcp://127.0.0.1:{server.Port}"),
            SslProtocol = SslProtocols.Tls12,
            Protocol = new SrmpCodec(),
        };
        client.Open();

        // 大帧（300KB）经 SSL 流：服务端泵按流式体读取
        var payload = MakePayload(300_000);
        var msg = new DefaultMessage { Sequence = 0x61 };
        msg.SetBody(new ArrayPacket(payload));
        client.SendMessage(msg);

        Assert.Equal(payload, await WithTimeout(serverGot.Task, 15_000));
    }

    #region 服务端并行
    [Fact]
    [DisplayName("协议模式_服务端并行_慢请求不阻塞快请求")]
    public async Task Server_Parallel_SlowRequest_DoesNotBlock()
    {
        using var server = new NetServer { Port = 0, Protocol = new SrmpCodec(), MaxConcurrency = 8 };
        server.Start();

        var slowIn = NewTcs<Boolean>();
        using var slowGate = new ManualResetEventSlim();
        server.Received += (s, e) =>
        {
            if (s is not INetSession session || e.Message is not DefaultMessage req) return;

            var reply = req.CreateReply();
            switch (req.Sequence)
            {
                case 0x51:
                    // 慢请求：在处理链内阻塞，直到测试放行
                    slowIn.TrySetResult(true);
                    slowGate.Wait(10_000);
                    reply.SetBody(new ArrayPacket("slow"u8.ToArray()));
                    session.SendMessage(reply);
                    break;
                case 0x52:
                    reply.SetBody(new ArrayPacket("fast"u8.ToArray()));
                    session.SendMessage(reply);
                    break;
            }
        };

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}") { Protocol = new SrmpCodec() };
        Assert.True(client.Open());

        // 多路复用：并发发出慢请求（Seq 0x51）与快请求（Seq 0x52）
        var slow = client.SendMessageAsync(new DefaultMessage { Sequence = 0x51 }).AsTask();
        await WithTimeout(slowIn.Task);
        var fast = client.SendMessageAsync(new DefaultMessage { Sequence = 0x52 }).AsTask();

        // 并行证据：慢请求仍被阻塞时，快请求先完成；串行实现下 3 秒内必超时
        var fastResp = await WithTimeout(fast, 3_000);
        Assert.Equal("fast", fastResp.Payload!.ToStr());

        // 放行慢请求，双请求均完成且各自配对
        slowGate.Set();
        var slowResp = await WithTimeout(slow, 5_000);
        Assert.Equal("slow", slowResp.Payload!.ToStr());
        var slowMsg = Assert.IsType<DefaultMessage>(slowResp);
        Assert.Equal(MessageKinds.Response, slowMsg.Kind);
        Assert.Equal(0x51, slowMsg.Sequence);
    }
    #endregion
}
#pragma warning restore xUnit1031
