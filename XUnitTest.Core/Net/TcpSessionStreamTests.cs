using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using NewLife.Data;
using NewLife.Messaging;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

/// <summary>TcpSession 流式收发（出站单出口/流式发送/关闭排空）回环测试</summary>
[Collection("Net")]
public class TcpSessionStreamTests
{
    #region 工具
    private static async Task WaitUntilAsync(Func<Boolean> condition, Int32 timeoutMs = 10_000)
    {
        var sw = Stopwatch.StartNew();
        while (!condition())
        {
            if (sw.ElapsedMilliseconds > timeoutMs) throw new TimeoutException("等待条件超时");

            await Task.Delay(10);
        }
    }

    /// <summary>建立回环连接并等待服务端会话</summary>
    private static async Task<(NetServer Server, TcpClient Client, TcpSession Session)> ConnectAsync()
    {
        var server = new NetServer { Port = 0 };
        server.Start();

        var wait = new ManualResetEventSlim();
        TcpSession? session = null;
        server.NewSession += (s, e) =>
        {
            session = e.Session.Session as TcpSession;
            wait.Set();
        };

        var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, server.Port);
        if (!wait.Wait(3_000) || session == null)
        {
            client.Dispose();
            server.Dispose();

            throw new TimeoutException("等待服务端会话超时");
        }

        return (server, client, session);
    }

    /// <summary>并发排空：边发边收，不依赖内核缓冲容量</summary>
    private static Task<Int32> DrainAsync(TcpClient client, Byte[] received)
    {
        var stream = client.GetStream();
        stream.ReadTimeout = 30_000;

        return Task.Run(() =>
        {
            var n = 0;
            while (n < received.Length)
            {
                var c = stream.Read(received, n, received.Length - n);
                if (c <= 0) break;

                n += c;
            }

            return n;
        });
    }
    #endregion

    #region 会话级行为
    [Fact]
    [DisplayName("流式会话_发送管道创建后Send统一入管道_借阅视图转自有拷贝")]
    public async Task SendPipeCreated_RoutesAndClones()
    {
        var (server, client, session) = await ConnectAsync();
        using (server)
        using (client)
        {
            // 创建发送管道后，Send 系列统一入管道（单出口）
            var pipe = session.SendPipe;
            Assert.Same(pipe, session.GetSendPipe());

            var src = new Byte[] { 10, 20, 30 };
            var rs = session.Send(new ArrayPacket(src));
            Assert.Equal(3, rs);

            // 借阅视图在入管道时已转自有拷贝：立即改写原缓冲不影响已排队数据
            src[0] = 99;

            await WaitUntilAsync(() => pipe.UnconsumedLength == 0);

            var received = new Byte[3];
            Assert.Equal(3, await DrainAsync(client, received));
            Assert.Equal(new Byte[] { 10, 20, 30 }, received);
        }
    }

    [Fact]
    [DisplayName("流式会话_关闭_先排空排队数据再关闭")]
    public async Task Close_DrainsPendingBeforeClosing()
    {
        var (server, client, session) = await ConnectAsync();
        using (server)
        using (client)
        {
            var pipe = session.SendPipe;

            var p1 = new Byte[] { 1, 2, 3 };
            var p2 = new Byte[] { 4, 5 };
            pipe.Writer.Append(new ArrayPacket(p1));
            pipe.Writer.Append(new ArrayPacket(p2));

            var received = new Byte[p1.Length + p2.Length];
            var reader = DrainAsync(client, received);

            // 同步关闭：完成写入并限时等待泵发完已排队数据
            var ok = session.Close("test");
            Assert.True(ok);

            Assert.True(pipe.IsCompleted);

            Assert.Equal(received.Length, await reader.WaitAsync(TimeSpan.FromSeconds(5)));
            Assert.Equal(p1.Concat(p2).ToArray(), received);

            // 服务端会话关闭即销毁：后续发送抛 ObjectDisposedException
            Assert.True(session.Disposed);
            Assert.Throws<ObjectDisposedException>(() => session.Send(new ArrayPacket(new Byte[] { 6 })));
        }
    }

    [Fact]
    [DisplayName("流式会话_销毁_入站管道残余归还")]
    public async Task InboundPipe_Close_ReleasesResidue()
    {
        var (server, client, session) = await ConnectAsync();
        using (server)
        using (client)
        {
            // 触发入站管道创建：此后接收数据同时投递到管道（共享切片）
            var pipe = session.Pipe;

            await client.GetStream().WriteAsync(new Byte[512]);
            await WaitUntilAsync(() => pipe.UnconsumedLength > 0);

            // 关闭并销毁会话：入站管道残余必须归还。
            // 旧实现只 _pipe.Writer.Complete()，读侧链上的池缓冲一挂到会话对象不可达，由终结器兜底
            session.Close("test");

            Assert.Equal(0, pipe.UnconsumedLength);
        }
    }
    #endregion

    #region 回环
    [Fact]
    [DisplayName("发送管道_回环_批量下发完整到达")]
    public async Task Loopback_BatchSend_Arrives()
    {
        var (server, client, session) = await ConnectAsync();
        using (server)
        using (client)
        {
            // 创建发送管道后经 Send 批量下发（单出口：管道接管发送）
            _ = session.SendPipe;

            var payload = new Byte[100_000];
            Random.Shared.NextBytes(payload);

            // 对端并发排空：边发边收，不依赖内核缓冲容量
            var received = new Byte[payload.Length];
            var reader = DrainAsync(client, received);

            var rs = session.Send(payload);
            Assert.Equal(payload.Length, rs);

            await WaitUntilAsync(() => session.GetSendPipe()!.UnconsumedLength == 0);

            // 对端完整收到且内容一致
            var read = await reader.WaitAsync(TimeSpan.FromSeconds(30));
            Assert.Equal(payload.Length, read);
            Assert.Equal(payload, received);
        }
    }

    [Fact]
    [DisplayName("流式发送_回环_大文件完整到达")]
    public async Task SendStream_Loopback_LargeArrives()
    {
        var (server, client, session) = await ConnectAsync();
        using (server)
        using (client)
        {
            // 1MB 数据流式发送：分块入管道 + 写侧回压，泵顺序送出
            var payload = new Byte[1_000_000];
            Random.Shared.NextBytes(payload);

            var pipe = session.SendPipe;
            pipe.PauseThreshold = 64 * 1024;
            pipe.ResumeThreshold = 32 * 1024;

            // 对端并发排空：发送受阻于内核缓冲容量时，若客户端不读将永远阻塞（并行负载下必须边发边收）
            var received = new Byte[payload.Length];
            var reader = DrainAsync(client, received);

            var total = await session.SendAsync(new MemoryStream(payload));
            Assert.Equal(payload.Length, total);

            // 对端完整收到且内容一致
            var read = await reader.WaitAsync(TimeSpan.FromSeconds(30));
            Assert.Equal(payload.Length, read);
            Assert.Equal(payload, received);
        }
    }

    [Fact]
    [DisplayName("流式发送_SRMP头+流式体_单条消息完整到达")]
    public async Task SendStream_SrmpHeaderBody_OneMessage()
    {
        var (server, client, session) = await ConnectAsync();
        using (server)
        using (client)
        {
            // 发送侧：头声明长度 + 流式体（等价 SRMP 大消息流式发送）；单出口保证头与体无交错
            var payload = new Byte[256 * 1024];
            Random.Shared.NextBytes(payload);

            _ = session.SendPipe;

            // 对端并发排空：边发边收，不依赖内核缓冲容量
            var frameLen = 8 + payload.Length;
            var received = new Byte[frameLen];
            var reader = DrainAsync(client, received);

            var msg = new DefaultMessage { Sequence = 0x3C };
            var codec = new SrmpCodec();
            var rs = session.Send(codec.BuildHeader(msg, payload.Length));
            Assert.Equal(8, rs);

            var total = await session.SendAsync(new MemoryStream(payload), payload.Length);
            Assert.Equal(payload.Length, total);

            // 接收侧：按消息自解析定界，完整负载与发送一致
            var read = await reader.WaitAsync(TimeSpan.FromSeconds(30));
            Assert.Equal(frameLen, read);

            var pr = codec.TryParse(new ArrayPacket(received).AsReadOnlySequence());
            Assert.NotNull(pr);
            Assert.Equal(8, pr.Value.HeaderSize);
            Assert.Equal(payload.Length, pr.Value.BodyLength);
            Assert.Equal(0x3C, ((DefaultMessage)pr.Value.Message!).Sequence);
            Assert.Equal(payload, received[8..]);
        }
    }
    #endregion
}
