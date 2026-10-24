using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using NewLife;
using NewLife.Data;
using NewLife.Http;
using Xunit;

namespace XUnitTest.Http;

public class TinyHttpClientTest
{
    /// <summary>启动本地一次性 HTTP 服务器，返回端口与完成任务</summary>
    private static (Int32 Port, Task Server) StartLocalServer(Func<NetworkStream, Task> handler)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var task = Task.Run(async () =>
        {
            try
            {
                using var client = await listener.AcceptTcpClientAsync();
                client.NoDelay = true;
                using var ns = client.GetStream();

                // 读掉请求
                var buf = new Byte[1024];
                await ns.ReadAtLeastAsync(buf, 1);

                await handler(ns);
            }
            finally
            {
                listener.Stop();
            }
        });

        return (port, task);
    }

    /// <summary>分块/多元数据测试替身：按队列供给后续数据包，每个元素模拟一次接收</summary>
    private sealed class FeedClient : TinyHttpClient
    {
        public Queue<Byte[]> Packets { get; } = new();

        protected override Task<IOwnerPacket> SendDataAsync(Uri? uri, IPacket? request)
        {
            var bytes = Packets.Count > 0 ? Packets.Dequeue() : null;
            if (bytes == null) return Task.FromResult<IOwnerPacket>(new OwnerPacket(0));

            var pk = new OwnerPacket(bytes.Length);
            bytes.CopyTo(pk.GetSpan());

            return Task.FromResult<IOwnerPacket>(pk);
        }
    }

    private static async Task<HttpResponse?> FeedAsync(params Byte[][] packets)
    {
        var client = new FeedClient();
        foreach (var item in packets) client.Packets.Enqueue(item);

        return await client.SendAsync(new HttpRequest { RequestUri = new Uri("http://127.0.0.1/") });
    }

    [Fact(DisplayName = "异步请求")]
    public async Task SendAsyncTest()
    {
        // 本地化：由本地服务提供超过 500 字符的页面，验证请求/基地址/正文读取链路，不触网
        using var server = new HttpServer { Port = 0 };
        server.Map("/", () => "<html><body>" + new String('x', 600) + "</body></html>");
        server.Start();

        var uri = new Uri($"http://127.0.0.1:{server.Port}/");
        var req = new HttpRequest { RequestUri = uri };
        var client = new TinyHttpClient { Timeout = TimeSpan.FromSeconds(3) };

        var html = (await client.SendAsync(req))?.Body.ToStr();

        Assert.True(!html.IsNullOrEmpty() && html.Length > 500);
        Assert.Equal(uri, client.BaseAddress);
    }

    [Fact(DisplayName = "异步字符串")]
    public async Task GetStringAsyncTest()
    {
        // 本地化：同样以超过 500 字符的本地页面校验字符串接口，不触网
        using var server = new HttpServer { Port = 0 };
        server.Map("/", () => "<html><body>" + new String('y', 600) + "</body></html>");
        server.Start();

        var client = new TinyHttpClient();
        var html = await client.GetStringAsync($"http://127.0.0.1:{server.Port}/");

        Assert.True(!html.IsNullOrEmpty() && html.Length > 500);
    }

    [Fact(DisplayName = "https_自签名证书_严格校验拒绝")]
    public async Task GetStringHttpsTest()
    {
        // 本地化说明：无法为公网域名在本机取得受系统信任的证书，故 https 以"自签名证书被拒绝"作为确定性验收口径。
        // TinyHttpClient 按系统信任链严格校验服务器证书，本地自签证书必须被拒绝（含主机名不匹配）
        var pfx = typeof(TinyHttpClientTest).Assembly.GetManifestResourceStream("XUnitTest.certs.newlifex.com.pfx")!.ReadBytes(-1);
#if NET9_0_OR_GREATER
        using var cert = X509CertificateLoader.LoadPkcs12(pfx, "123456");
#else
        using var cert = new X509Certificate2(pfx, "123456", X509KeyStorageFlags.DefaultKeySet);
#endif

        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        _ = Task.Run(async () =>
        {
            try
            {
                using var client = await listener.AcceptTcpClientAsync();
                using var ssl = new SslStream(client.GetStream(), false);
                await ssl.AuthenticateAsServerAsync(cert, false, SslProtocols.Tls12, false);
                var head = "HTTP/1.1 200 OK\r\nContent-Length: 2\r\n\r\nok".GetBytes();
                await ssl.WriteAsync(head, 0, head.Length);
                await ssl.FlushAsync();
            }
            catch { }
            finally
            {
                listener.Stop();
            }
        });

        var http = new TinyHttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var ex = await Record.ExceptionAsync(() => http.GetStringAsync($"https://127.0.0.1:{port}/"));

        Assert.NotNull(ex);
        Assert.True(ex is AuthenticationException || ex.InnerException is AuthenticationException,
            $"应为证书校验失败，实际 {ex.GetType().Name}: {ex.Message}");
    }

    [Theory(DisplayName = "HTTP客户端_跟随303/307/308重定向与相对Location")]
    [InlineData(303)]
    [InlineData(307)]
    [InlineData(308)]
    public async Task Redirect_303_307_308_Followed(Int32 code)
    {
        using var server = new HttpServer { Port = 0 };
        server.Map("/a", ctx =>
        {
            ctx.Response.StatusCode = (HttpStatusCode)code;
            // 相对 Location：真实服务器常见写法
            ctx.Response.Headers["Location"] = "/b";
        });
        server.Map("/b", () => "B");
        server.Start();

        var client = new TinyHttpClient();
        var res = await client.SendAsync(new HttpRequest { RequestUri = new Uri($"http://127.0.0.1:{server.Port}/a") });

        Assert.NotNull(res);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("B", res.Body?.ToStr());
    }

    [Fact(DisplayName = "HTTP客户端_响应头跨接收块_累积读取成功")]
    public async Task SendAsync_SplitResponseHead()
    {
        var head = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 5\r\n\r\n");

        // 先发半截头，停顿后再发余下头与体：模拟服务端头部稍慢，首个数据块拿不到完整头
        var (port, server) = StartLocalServer(async ns =>
        {
            await ns.WriteAsync(head.AsMemory(0, 10));
            await ns.FlushAsync();
            await Task.Delay(50);
            await ns.WriteAsync(head.AsMemory(10));
            await ns.WriteAsync(Encoding.ASCII.GetBytes("hello"));
            await ns.FlushAsync();
        });

        using var client = new TinyHttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var res = await client.SendAsync(new HttpRequest { RequestUri = new Uri($"http://127.0.0.1:{port}/") });

        Assert.NotNull(res);
        Assert.Equal(5, res!.BodyLength);
        Assert.Equal("hello", res.Body!.ToStr());

        await server;
    }

    [Fact(DisplayName = "HTTP客户端_响应体跨接收块_按Content-Length补读")]
    public async Task SendAsync_BodySplitAcrossReads()
    {
        // 定长体分两次到达：必须补读到声明长度，否则调用方拿到被截断的“成功”结果
        var head = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 10\r\n\r\n");

        var (port, server) = StartLocalServer(async ns =>
        {
            await ns.WriteAsync(head);
            await ns.WriteAsync(Encoding.ASCII.GetBytes("hello"));
            await ns.FlushAsync();
            await Task.Delay(50);
            await ns.WriteAsync(Encoding.ASCII.GetBytes("world"));
            await ns.FlushAsync();
        });

        using var client = new TinyHttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var res = await client.SendAsync(new HttpRequest { RequestUri = new Uri($"http://127.0.0.1:{port}/") });

        Assert.NotNull(res);
        Assert.Equal("helloworld", res!.Body!.ToStr());

        await server;
    }

    [Fact(DisplayName = "HTTP客户端_慢滴漏服务端_整次请求超时生效不被无限挂住")]
    public async Task SendAsync_SlowDrip_TotalTimeout()
    {
        // 服务端每隔 50ms 只回 1 个字节，响应头永不补全。
        // 超时若按“单次读取”计，每轮读取都在预算内拿到字节，调用方被无限挂住
        var (port, _) = StartLocalServer(async ns =>
        {
            var one = new Byte[] { (Byte)'X' };
            try
            {
                for (var i = 0; i < 300; i++)
                {
                    await ns.WriteAsync(one);
                    await Task.Delay(50);
                }
            }
            catch { }
        });

        // 超时预算 1 秒：服务端永远补不全响应头，客户端必须在预算内失败而非被逐字节读取无限拖住。
        // 满载下建连本身也可能先行超时（TimeoutException），两者都是“未挂住”的合法失败形态；同时校验耗时确实在预算量级
        using var client = new TinyHttpClient { Timeout = TimeSpan.FromSeconds(1) };

        var sw = Stopwatch.StartNew();
        var ex = await Record.ExceptionAsync(() =>
            client.SendAsync(new HttpRequest { RequestUri = new Uri($"http://127.0.0.1:{port}/") }).WaitAsync(TimeSpan.FromSeconds(10)));
        sw.Stop();

        Assert.NotNull(ex);
        Assert.True(ex is OperationCanceledException or TimeoutException, $"应为超时类异常，实际 {ex.GetType().Name}: {ex.Message}");
        Assert.True(sw.ElapsedMilliseconds < 5_000, $"请求未被超时预算限制，耗时 {sw.ElapsedMilliseconds}ms");
    }

    [Fact(DisplayName = "HTTP客户端_错误状态码_抛异常")]
    public async Task SendAsync_ErrorStatus_Throws()
    {
        var resp = Encoding.ASCII.GetBytes("HTTP/1.1 404 Not Found\r\nContent-Length: 0\r\n\r\n");

        var (port, server) = StartLocalServer(async ns =>
        {
            await ns.WriteAsync(resp);
            await ns.FlushAsync();
        });

        using var client = new TinyHttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            client.SendAsync(new HttpRequest { RequestUri = new Uri($"http://127.0.0.1:{port}/") }));

        Assert.Contains("404", ex.Message);

        await server;
    }

    [Fact(DisplayName = "HTTP客户端_204无内容_按成功返回")]
    public async Task SendAsync_NoContent_Succeeds()
    {
        var resp = Encoding.ASCII.GetBytes("HTTP/1.1 204 No Content\r\nContent-Length: 0\r\n\r\n");

        var (port, server) = StartLocalServer(async ns =>
        {
            await ns.WriteAsync(resp);
            await ns.FlushAsync();
        });

        using var client = new TinyHttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var res = await client.SendAsync(new HttpRequest { RequestUri = new Uri($"http://127.0.0.1:{port}/") });

        Assert.NotNull(res);
        Assert.Equal(HttpStatusCode.NoContent, res!.StatusCode);

        await server;
    }

    [Fact(DisplayName = "HTTP客户端_响应重复或非法Content-Length_报错拒绝")]
    public async Task SendAsync_InvalidContentLength_Throws()
    {
        // 重复（或无法解析）的长度会让本端与中间代理对实体长度产生分歧：本端按最后一个值读、代理按第一个值转发，
        // 多出的字节被当成下一条响应（响应队列投毒）。故按协议错误直接报出，不静默取一个值
        foreach (var resp in new[]
        {
            "HTTP/1.1 200 OK\r\nContent-Length: 5\r\nContent-Length: 10\r\n\r\nhello",
            "HTTP/1.1 200 OK\r\nContent-Length: abc\r\n\r\nhello",
        })
        {
            var (port, server) = StartLocalServer(async ns =>
            {
                await ns.WriteAsync(Encoding.ASCII.GetBytes(resp));
                await ns.FlushAsync();
            });

            using var client = new TinyHttpClient { Timeout = TimeSpan.FromSeconds(10) };
            await Assert.ThrowsAsync<InvalidDataException>(() =>
                client.SendAsync(new HttpRequest { RequestUri = new Uri($"http://127.0.0.1:{port}/") }));

            await server;
        }
    }

    [Fact(DisplayName = "HTTP客户端_响应头始终无法解析_报错而非静默返回null")]
    public async Task SendAsync_UnparsableHead_Throws()
    {
        // 服务端回了不成形的字节后断开：若不报错就只能静默返回 null，
        // 调用方无法区分“服务端没响应”与“响应不成形”
        var resp = Encoding.ASCII.GetBytes("garbage-response\r\n\r\n");

        var (port, server) = StartLocalServer(async ns =>
        {
            await ns.WriteAsync(resp);
            await ns.FlushAsync();
        });

        using var client = new TinyHttpClient { Timeout = TimeSpan.FromSeconds(10) };
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            client.SendAsync(new HttpRequest { RequestUri = new Uri($"http://127.0.0.1:{port}/") }));

        await server;
    }

    [Fact(DisplayName = "HTTP客户端_1xx临时响应_显式报错不支持")]
    public async Task SendAsync_InterimResponse_Throws()
    {
        // 本端从不发 Expect，1xx 属不支持形态；显式报错，避免调用方拿到 1xx 的空体却误判成功
        var resp = Encoding.ASCII.GetBytes("HTTP/1.1 100 Continue\r\n\r\n");

        var (port, server) = StartLocalServer(async ns =>
        {
            await ns.WriteAsync(resp);
            await ns.FlushAsync();
        });

        using var client = new TinyHttpClient { Timeout = TimeSpan.FromSeconds(10) };
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            client.SendAsync(new HttpRequest { RequestUri = new Uri($"http://127.0.0.1:{port}/") }));

        await server;
    }

    [Fact(DisplayName = "HTTP客户端_传输编码含压缩_显式报错不支持")]
    public async Task SendAsync_TransferEncodingWithGzip_Throws()
    {
        // 含 gzip 等本端不解的传输编码时，原样交出会让调用方拿到未解压却以为成功的字节流，故显式拒绝
        var resp = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nTransfer-Encoding: gzip, chunked\r\n\r\n5\r\nhello\r\n0\r\n\r\n");

        var (port, server) = StartLocalServer(async ns =>
        {
            await ns.WriteAsync(resp);
            await ns.FlushAsync();
        });

        using var client = new TinyHttpClient { Timeout = TimeSpan.FromSeconds(10) };
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            client.SendAsync(new HttpRequest { RequestUri = new Uri($"http://127.0.0.1:{port}/") }));

        await server;
    }

    [Fact(DisplayName = "HTTP客户端_分块与Content-Length同现_以分块为准")]
    public async Task SendAsync_ChunkedTakesPrecedenceOverContentLength()
    {
        // RFC 9112 §6.1：Transfer-Encoding 覆盖 Content-Length。按 CL 先补读会一直等不到剩余字节，直到整次超时
        var head = "HTTP/1.1 200 OK\r\nContent-Length: 100\r\nTransfer-Encoding: chunked\r\n\r\n".GetBytes();

        var (port, server) = StartLocalServer(async ns =>
        {
            await ns.WriteAsync(head);
            await ns.WriteAsync("5\r\nhello\r\n0\r\n\r\n".GetBytes());
            await ns.FlushAsync();

            // 保持连接不关
            await Task.Delay(1500);
        });

        using var client = new TinyHttpClient { Timeout = TimeSpan.FromSeconds(1) };
        var res = await client.SendAsync(new HttpRequest { RequestUri = new Uri($"http://127.0.0.1:{port}/") });

        Assert.NotNull(res);
        Assert.Equal(HttpStatusCode.OK, res!.StatusCode);
        Assert.Equal("hello", res.Body?.ToStr());

        await server;
    }

    [Fact(DisplayName = "HTTP客户端_重定向跨跳_上一跳的Transfer-Encoding不残留")]
    public async Task Redirect_HeaderResidue_NotReused()
    {
        // 解析只覆盖本次出现的头部，跨跳复用同一响应对象会把上一跳的 Transfer-Encoding 残留到第二跳，
        // 让带 Content-Length 的最终响应被当成分块解析
        var hop1 = "HTTP/1.1 302 Found\r\nLocation: /b\r\nContent-Length: 0\r\nTransfer-Encoding: chunked\r\n\r\n".GetBytes();
        var hop2 = "HTTP/1.1 200 OK\r\nContent-Length: 5\r\n\r\nhello".GetBytes();

        var (port, server) = StartLocalServer(async ns =>
        {
            await ns.WriteAsync(hop1);
            await ns.FlushAsync();

            // 第二跳请求（同一连接复用）
            var buf = new Byte[1024];
            var count = await ns.ReadAsync(buf);
            Assert.True(count > 0);

            await ns.WriteAsync(hop2);
            await ns.FlushAsync();

            // 保持连接不关
            await Task.Delay(1500);
        });

        // 超时只作为兜底（本地服务端回调即时响应），满载下连接与读取可能滞后秒级，1 秒会误伤
        using var client = new TinyHttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var res = await client.SendAsync(new HttpRequest { RequestUri = new Uri($"http://127.0.0.1:{port}/a") });

        Assert.NotNull(res);
        Assert.Equal(HttpStatusCode.OK, res!.StatusCode);
        Assert.Equal("hello", res.Body?.ToStr());

        await server;
    }

    #region 分块传输
    private static Byte[] Head => "HTTP/1.1 200 OK\r\nTransfer-Encoding: chunked\r\n\r\n".GetBytes();

    [Fact(DisplayName = "分块传输_长度行跨接收包_后续分块不丢失")]
    public async Task Chunk_LengthLineSplitAcrossPackets()
    {
        // 长度行 "5\r\n" 被拆到两个接收包
        var res = await FeedAsync(
            [.. Head, .. "5".GetBytes()],
            "\r\nhello\r\n0\r\n\r\n".GetBytes());

        Assert.NotNull(res);
        Assert.Equal("hello", res!.Body!.ToStr());
    }

    [Fact(DisplayName = "分块传输_多分块跨包_逐块还原")]
    public async Task Chunk_MultipleChunksAcrossPackets()
    {
        var res = await FeedAsync(
            [.. Head, .. "3\r\nab".GetBytes()],
            "c\r\n".GetBytes(),
            "5\r\nhello\r\n0\r\n\r\n".GetBytes());

        Assert.NotNull(res);
        Assert.Equal("abchello", res!.Body!.ToStr());
    }

    [Fact(DisplayName = "分块传输_分块扩展_忽略分号后内容")]
    public async Task Chunk_Extension_Ignored()
    {
        var res = await FeedAsync([.. Head, .. "3;ext=1\r\nabc\r\n0\r\n\r\n".GetBytes()]);

        Assert.NotNull(res);
        Assert.Equal("abc", res!.Body!.ToStr());
    }

    [Fact(DisplayName = "分块传输_末块后带trailers_一并消费且可跨包")]
    public async Task Chunk_Trailers_Consumed()
    {
        // trailers 与终止空行同包
        var res = await FeedAsync([.. Head, .. "5\r\nhello\r\n0\r\nX-A: 1\r\n\r\n".GetBytes()]);
        Assert.Equal("hello", res!.Body!.ToStr());

        // trailers 终止空行跨接收包：未收到时不能误判为已完成
        var res2 = await FeedAsync(
            [.. Head, .. "5\r\nhello\r\n0\r\n".GetBytes()],
            "X-A: 1\r\n\r\n".GetBytes());
        Assert.Equal("hello", res2!.Body!.ToStr());
    }

    [Fact(DisplayName = "分块传输_非法长度行_抛异常")]
    public async Task Chunk_InvalidLength_Throws()
    {
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            FeedAsync([.. Head, .. "xyz\r\nabc\r\n0\r\n\r\n".GetBytes()]));
    }

    [Fact(DisplayName = "分块传输_未收到终止块且对端关闭_抛异常")]
    public async Task Chunk_Incomplete_Throws()
    {
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            FeedAsync([.. Head, .. "5\r\nhel".GetBytes()]));
    }
    #endregion

    [Fact(DisplayName = "建连_半开地址_受整次超时约束")]
    public async Task Connect_Blackhole_BoundedByTimeout()
    {
        // 本机黑洞（accept 队列打满的半开连接）：不依赖任何外部网络。
        // 本机 backlog 语义不支持构造黑洞时跳过（属本机环境差异，与外部依赖无关）
        using var blackhole = TestBlackhole.TryCreate();
        if (blackhole == null) return;

        // 预算必须明显小于黑洞探测窗口（1 秒），确保探测通过后建连仍处于半开状态
        using var client = new TinyHttpClient { Timeout = TimeSpan.FromMilliseconds(500) };

        var sw = Stopwatch.StartNew();
        // 没有超时约束时，建连要等操作系统默认 TCP 超时（Windows 约 21 秒）
        var ex = await Assert.ThrowsAnyAsync<Exception>(() =>
            client.SendAsync(new HttpRequest { RequestUri = new Uri($"http://127.0.0.1:{blackhole.Port}/") }).WaitAsync(TimeSpan.FromSeconds(10)));
        sw.Stop();

        Assert.IsType<TimeoutException>(ex);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(5), $"建连耗时 {sw.Elapsed}，未受整次超时约束");
    }
}
