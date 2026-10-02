using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NewLife;
using NewLife.Data;
using NewLife.Http;
using NewLife.Log;
using Xunit;
#if NET6_0_OR_GREATER
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
#endif

namespace XUnitTest.Http;

public class TinyHttpClientTest
{
    private readonly TinyHttpClient _Client;

    public TinyHttpClientTest()
    {
        _Client = new TinyHttpClient();
    }

    /// <summary>检查目标服务器是否可达</summary>
    private static Boolean IsServerReachable(String host, Int32 port, Int32 timeoutMs = 2000)
    {
        try
        {
            using var client = new TcpClient();
            var result = client.BeginConnect(host, port, null, null);
            var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(timeoutMs));
            if (!success) return false;
            client.EndConnect(result);
            return true;
        }
        catch
        {
            return false;
        }
    }

    //[Fact(DisplayName = "同步请求")]
    //public void SendTest()
    //{
    //    var uri = new Uri("http://newlifex.com");
    //    var client = new TinyHttpClient { Timeout = TimeSpan.FromSeconds(3), Log = XTrace.Log };
    //    var html = client.Send(uri, null)?.ToStr();

    //    Assert.True(!html.IsNullOrEmpty() && html.Length > 500);
    //    Assert.Equal(uri, client.BaseAddress);
    //}

    [Theory(DisplayName = "HTTP客户端_跟随303/307/308重定向与相对Location")]
    [InlineData(303)]
    [InlineData(307)]
    [InlineData(308)]
    public async Task Redirect_303_307_308_Followed(Int32 code)
    {
        // 只认 301/302 时，303/307/308 会被当成成功响应原样返回（调用方拿到重定向页面却以为成功）
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

    [Fact(DisplayName = "异步请求")]
    public async Task SendAsyncTest()
    {
        // 在 CI 环境中跳过，因为目标服务器可能不可达
        if (!IsServerReachable("newlifex.com", 80)) return;

        var uri = new Uri("http://newlifex.com");
        var req = new HttpRequest { RequestUri = uri };
        var client = new TinyHttpClient { Timeout = TimeSpan.FromSeconds(3), Log = XTrace.Log };
        var html = (await client.SendAsync(req))?.Body.ToStr();

        Assert.True(!html.IsNullOrEmpty() && html.Length > 500);
        Assert.Equal(uri, client.BaseAddress);
    }

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
                await ns.ReadAsync(buf);

                await handler(ns);
            }
            finally
            {
                listener.Stop();
            }
        });

        return (port, task);
    }

    [Fact(DisplayName = "异步请求_响应头跨接收块_累积读取成功")]
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

        using var client = new TinyHttpClient { Timeout = TimeSpan.FromMilliseconds(200) };

        // 整次预算耗尽即取消；旧实现每轮重新计时，永远读不完，只会撞上外层 10s 兜底（TimeoutException）
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.SendAsync(new HttpRequest { RequestUri = new Uri($"http://127.0.0.1:{port}/") }).WaitAsync(TimeSpan.FromSeconds(10)));
    }

    [Fact(DisplayName = "异步请求_错误状态码_抛异常")]
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

    [Fact(DisplayName = "HTTP客户端_响应重复Content-Length_报错拒绝")]
    public async Task SendAsync_DuplicateContentLength_Throws()
    {
        // 同名头部就地覆盖只会剩最后一个值，与中间代理（通常取第一个）理解不一致，构成实体长度分歧：
        // 本端按 10 读、代理按 5 转发，多出的字节被当成下一条响应（响应队列投毒）。请求侧已拒绝，响应侧同口径
        var resp = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 5\r\nContent-Length: 10\r\n\r\nhello");

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

    [Fact(DisplayName = "HTTP客户端_响应非法Content-Length_报错拒绝")]
    public async Task SendAsync_InvalidContentLength_Throws()
    {
        // 无法解析的长度值旧实现退化成“无体”（ContentLength=-1），实体随首个接收包原样截断，边界不可预期；
        // 与请求侧一致按协议错误报错
        var resp = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: abc\r\n\r\nhello");

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

    [Fact(DisplayName = "异步请求_204无内容_按成功返回")]
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

    [Fact(DisplayName = "证书校验_默认严格")]
    public void IgnoreServerCertificate_DefaultFalse()
    {
        using var client = new TinyHttpClient();
        Assert.False(client.IgnoreServerCertificate);
    }

#if NET6_0_OR_GREATER
    /// <summary>生成自签服务器证书（含私钥）</summary>
    private static X509Certificate2 CreateSelfSignedCert()
    {
        var rsa = RSA.Create(2048);
        var req = new CertificateRequest("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var cert = req.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddDays(1));

        // Windows 的 Schannel 服务端需要可用的持久化私钥，CreateSelfSigned 的临时密钥不足以取凭据。
        // 导出为 PFX 再以 UserKeySet 导入，得到 Schannel 可用的证书
        var pfx = cert.Export(X509ContentType.Pfx);
#if NET9_0_OR_GREATER
        var loaded = X509CertificateLoader.LoadPkcs12(pfx, null, X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable);
#else
        var loaded = new X509Certificate2(pfx, (String?)null, X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable);
#endif

        Assert.True(loaded.HasPrivateKey, "自签证书应包含私钥");

        return loaded;
    }

    /// <summary>启动一次性 TLS 服务器：接受一次连接，读掉请求后回一个 200 响应</summary>
    /// <param name="cert">服务器证书</param>
    /// <param name="expectSuccess">是否预期握手成功。false 时吞掉服务端握手异常（客户端证书校验失败属预期）</param>
    private static (Int32 Port, Task Server) StartTlsServer(X509Certificate2 cert, Boolean expectSuccess = false)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var task = Task.Run(async () =>
        {
            try
            {
                using var tc = await listener.AcceptTcpClientAsync();
                tc.NoDelay = true;
                using var ns = tc.GetStream();
                using var ssl = new SslStream(ns, false);
                await ssl.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
                {
                    ServerCertificate = cert,
                    EnabledSslProtocols = SslProtocols.None,
                });

                var buf = new Byte[1024];
                await ssl.ReadAsync(buf);
                await ssl.WriteAsync(Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 2\r\n\r\nok"));
                await ssl.FlushAsync();
            }
            catch (Exception) when (!expectSuccess)
            {
                // 客户端证书校验失败时，服务端握手报错属预期
            }
            finally
            {
                listener.Stop();
            }
        });

        return (port, task);
    }

    [Fact(DisplayName = "证书校验_自签默认被拒绝")]
    public async Task SelfSigned_RejectedByDefault()
    {
        using var cert = CreateSelfSignedCert();
        var (port, server) = StartTlsServer(cert);

        using var client = new TinyHttpClient { Timeout = TimeSpan.FromSeconds(10) };
        await Assert.ThrowsAnyAsync<Exception>(() =>
            client.SendAsync(new HttpRequest { RequestUri = new Uri($"https://127.0.0.1:{port}/") }));

        await server;
    }

    [Fact(DisplayName = "证书校验_显式忽略后放行自签")]
    public async Task SelfSigned_AcceptedWhenIgnored()
    {
        using var cert = CreateSelfSignedCert();
        var (port, server) = StartTlsServer(cert, true);

        using var client = new TinyHttpClient { Timeout = TimeSpan.FromSeconds(10), IgnoreServerCertificate = true };
        var clientTask = client.SendAsync(new HttpRequest { RequestUri = new Uri($"https://127.0.0.1:{port}/") });

        // 先等服务端任务：服务端若握手失败，这里会直接抛出真实异常，便于定位
        await server;

        var res = await clientTask;
        Assert.NotNull(res);
        Assert.Equal("ok", res!.Body!.ToStr());
    }
#endif

    //[Fact(DisplayName = "同步字符串")]
    //public void GetString()
    //{
    //    var url = "http://x.newlifex.com";
    //    var client = new TinyHttpClient();
    //    var html = client.GetString(url);

    //    Assert.True(!html.IsNullOrEmpty() && html.Length > 500);
    //}

    [Fact(DisplayName = "异步字符串")]
    public async Task GetStringAsync()
    {
        // 在 CI 环境中跳过，因为目标服务器可能不可达
        if (!IsServerReachable("x.newlifex.com", 80)) return;

        var url = "http://x.newlifex.com";
        var client = new TinyHttpClient();
        var html = await client.GetStringAsync(url);

        Assert.True(!html.IsNullOrEmpty() && html.Length > 500);
    }

    [Fact(DisplayName = "https")]
    public async Task GetStringHttps()
    {
        // 在 CI 环境中跳过，因为目标服务器可能不可达
        if (!IsServerReachable("newlifex.com", 443)) return;

        var url = "https://newlifex.com";
        var client = new TinyHttpClient();
        var html = await client.GetStringAsync(url);

        Assert.True(!html.IsNullOrEmpty() && html.Length > 500);
    }

    #region 分块传输
    /// <summary>分块解析测试替身：按队列供给后续数据包，每个元素模拟一次接收</summary>
    private sealed class ChunkFeedClient : TinyHttpClient
    {
        public Queue<Byte[]> Packets { get; } = new();

        public Task<IPacket> ReadChunk(IPacket body) => ReadChunkAsync(body);

        protected override Task<IOwnerPacket> SendDataAsync(Uri? uri, IPacket? request)
        {
            var bytes = Packets.Count > 0 ? Packets.Dequeue() : null;
            if (bytes == null) return Task.FromResult<IOwnerPacket>(new OwnerPacket(0));

            var pk = new OwnerPacket(bytes.Length);
            bytes.CopyTo(pk.GetSpan());

            return Task.FromResult<IOwnerPacket>(pk);
        }
    }

    private static async Task<IPacket> ReadChunkAsync(ChunkFeedClient client, Byte[] first, params Byte[][] more)
    {
        foreach (var item in more) client.Packets.Enqueue(item);

        var pk = await client.ReadChunk(new ArrayPacket(first));

        return pk;
    }

    [Fact(DisplayName = "分块传输_长度行跨接收包_后续分块不丢失")]
    public async Task Chunk_LengthLineSplitAcrossPackets()
    {
        var client = new ChunkFeedClient();

        // 长度行 "5\r\n" 被拆到两个接收包：旧实现 ParseChunk 找不到 CRLF 即整体跳出，后续分块全部丢失（仍返回“成功”）
        var body = await ReadChunkAsync(client, "5".GetBytes(), "\r\nhello\r\n0\r\n\r\n".GetBytes());

        Assert.Equal("hello", body.ToStr());
    }

    [Fact(DisplayName = "分块传输_多分块跨包_逐块还原")]
    public async Task Chunk_MultipleChunksAcrossPackets()
    {
        var client = new ChunkFeedClient();

        var body = await ReadChunkAsync(client, "3\r\nab".GetBytes(), "c\r\n".GetBytes(), "5\r\nhello\r\n0\r\n\r\n".GetBytes());

        Assert.Equal("abchello", body.ToStr());
    }

    [Fact(DisplayName = "分块传输_分块扩展_忽略分号后内容")]
    public async Task Chunk_Extension_Ignored()
    {
        var client = new ChunkFeedClient();

        // 1ba;ext=1 这类分块扩展：旧实现直接 Int32.Parse 会抛 FormatException 穿透到调用方
        var body = await ReadChunkAsync(client, "3;ext=1\r\nabc\r\n0\r\n\r\n".GetBytes());

        Assert.Equal("abc", body.ToStr());
    }

    [Fact(DisplayName = "分块传输_非法长度行_抛异常")]
    public async Task Chunk_InvalidLength_Throws()
    {
        var client = new ChunkFeedClient();

        await Assert.ThrowsAsync<InvalidDataException>(() => ReadChunkAsync(client, "xyz\r\nabc\r\n0\r\n\r\n".GetBytes()));
    }

    [Fact(DisplayName = "分块传输_单块超上限_抛异常")]
    public async Task Chunk_ExceedsMaxChunkSize_Throws()
    {
        var client = new ChunkFeedClient { MaxChunkSize = 4 };

        await Assert.ThrowsAsync<InvalidDataException>(() => ReadChunkAsync(client, "5\r\nhello\r\n0\r\n\r\n".GetBytes()));
    }

    [Fact(DisplayName = "分块传输_总长超上限_抛异常")]
    public async Task Chunk_ExceedsMaxBodySize_Throws()
    {
        var client = new ChunkFeedClient { MaxBodySize = 4 };

        await Assert.ThrowsAsync<InvalidDataException>(() => ReadChunkAsync(client, "3\r\nabc\r\n3\r\ndef\r\n0\r\n\r\n".GetBytes()));
    }

    [Fact(DisplayName = "分块传输_末块后带trailers_一并消费不残留")]
    public async Task Chunk_Trailers_Consumed()
    {
        var client = new ChunkFeedClient();

        // 末块后的 trailers 段必须一并消费：残留在连接上会让复用连接的下一个响应错位
        var body = await ReadChunkAsync(client, "3\r\nabc\r\n0\r\nX-Trace: 1\r\n\r\n".GetBytes());

        Assert.Equal("abc", body.ToStr());
    }

    [Fact(DisplayName = "分块传输_trailers终止空行跨接收包_仍正确结束")]
    public async Task Chunk_TrailerSplitAcrossPackets()
    {
        var client = new ChunkFeedClient();

        var body = await ReadChunkAsync(client, "3\r\nabc\r\n0\r\n".GetBytes(), "\r\n".GetBytes());

        Assert.Equal("abc", body.ToStr());
    }

    [Fact(DisplayName = "分块传输_长度行迟迟不到_残留超上限抛异常")]
    public async Task Chunk_UnterminatedData_ExceedsCap_Throws()
    {
        var client = new ChunkFeedClient { MaxChunkSize = 8 };

        // 对端持续发送不含 CRLF 的字节：未解析残留必须有上限，不能无界累积
        await Assert.ThrowsAsync<InvalidDataException>(() => ReadChunkAsync(
            client, "abcdefghij".GetBytes(), "abcdefghij".GetBytes(), "abcdefghij".GetBytes()));
    }
    #endregion

    #region 响应分帧与建连超时
    [Fact(DisplayName = "分块与Content-Length同现_以分块为准_不按CL补读")]
    public async Task Chunked_TakesPrecedenceOverContentLength()
    {
        // RFC 9112 §6.1：Transfer-Encoding 覆盖 Content-Length。
        // 旧实现先按 Content-Length 补读再判分块：声明长度大于实际分块体时，一直等不到剩余字节，直到整次超时才失败
        var head = "HTTP/1.1 200 OK\r\nContent-Length: 100\r\nTransfer-Encoding: chunked\r\n\r\n".GetBytes();

        var (port, server) = StartLocalServer(async ns =>
        {
            await ns.WriteAsync(head);
            await ns.WriteAsync("5\r\nhello\r\n0\r\n\r\n".GetBytes());
            await ns.FlushAsync();

            // 保持连接不关：旧实现会一直等 Content-Length 声明的剩余 95 字节
            await Task.Delay(1500);
        });

        using var client = new TinyHttpClient { Timeout = TimeSpan.FromSeconds(1) };
        var res = await client.SendAsync(new HttpRequest { RequestUri = new Uri($"http://127.0.0.1:{port}/") });

        Assert.NotNull(res);
        Assert.Equal(HttpStatusCode.OK, res!.StatusCode);
        Assert.Equal("hello", res.Body?.ToStr());

        await server;
    }

    [Fact(DisplayName = "1xx临时响应_单独到达_跳过并返回最终响应")]
    public async Task InterimResponse_SeparatePacket()
    {
        // 网关与 Expect 场景会先回 100 Continue。当成最终响应返回会让调用方拿到 1xx 的空体并误判成功
        var (port, server) = StartLocalServer(async ns =>
        {
            await ns.WriteAsync("HTTP/1.1 100 Continue\r\n\r\n".GetBytes());
            await ns.FlushAsync();
            await Task.Delay(50);

            await ns.WriteAsync("HTTP/1.1 200 OK\r\nContent-Length: 2\r\n\r\nOK".GetBytes());
            await ns.FlushAsync();
            await Task.Delay(50);
        });

        using var client = new TinyHttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var res = await client.SendAsync(new HttpRequest { RequestUri = new Uri($"http://127.0.0.1:{port}/") });

        Assert.NotNull(res);
        Assert.Equal(HttpStatusCode.OK, res!.StatusCode);
        Assert.Equal("OK", res.Body?.ToStr());

        await server;
    }

    [Fact(DisplayName = "1xx与最终响应同包到达_残留字节不丢弃")]
    public async Task InterimResponse_SamePacket()
    {
        // 服务端两次写入常被合并到同一个接收包：1xx 之后的字节就是最终响应，丢弃即丢失响应并挂到超时
        var (port, server) = StartLocalServer(async ns =>
        {
            await ns.WriteAsync("HTTP/1.1 100 Continue\r\n\r\nHTTP/1.1 200 OK\r\nContent-Length: 5\r\n\r\nhello".GetBytes());
            await ns.FlushAsync();
            await Task.Delay(50);
        });

        using var client = new TinyHttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var res = await client.SendAsync(new HttpRequest { RequestUri = new Uri($"http://127.0.0.1:{port}/") });

        Assert.NotNull(res);
        Assert.Equal(HttpStatusCode.OK, res!.StatusCode);
        Assert.Equal("hello", res.Body?.ToStr());

        await server;
    }

    [Fact(DisplayName = "重定向跨跳_上一跳的Transfer-Encoding不残留")]
    public async Task Redirect_HeaderResidue_NotReused()
    {
        // 解析只覆盖本次出现的头部，跨跳复用同一响应对象会把上一跳的 Transfer-Encoding 残留到第二跳，
        // 让带 Content-Length 的最终响应被当成分块解析（旧实现下等不到分块长度行，直到超时）
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

            // 保持连接不关：陈旧 TE 会让旧实现一直等分块数据
            await Task.Delay(1500);
        });

        using var client = new TinyHttpClient { Timeout = TimeSpan.FromSeconds(1) };
        var res = await client.SendAsync(new HttpRequest { RequestUri = new Uri($"http://127.0.0.1:{port}/a") });

        Assert.NotNull(res);
        Assert.Equal(HttpStatusCode.OK, res!.StatusCode);
        Assert.Equal("hello", res.Body?.ToStr());

        await server;
    }

    /// <summary>探测地址是否构成“黑洞”：预算内既没连上、也不立刻失败</summary>
    private static Boolean IsBlackhole(String host, Int32 port, Int32 budgetMs)
    {
        try
        {
            using var tc = new TcpClient();
            var task = tc.ConnectAsync(host, port);

            return !task.Wait(budgetMs);
        }
        catch
        {
            return false;   // 立刻失败（拒绝或路由不可达）→ 不是黑洞
        }
    }

    [Fact(DisplayName = "建连_黑洞地址_受整次超时约束")]
    public async Task Connect_Blackhole_BoundedByTimeout()
    {
        // 192.0.2.0/24 为文档保留网段，正常不会被路由。若本机对该地址立刻拒绝（ICMP 或路由不可达），
        // 就无从验证建连超时约束，跳过
        if (!IsBlackhole("192.0.2.1", 80, 1000)) return;

        using var client = new TinyHttpClient { Timeout = TimeSpan.FromMilliseconds(500) };

        var sw = Stopwatch.StartNew();
        // 没有超时约束时，建连要等操作系统默认 TCP 超时（Windows 约 21 秒）
        var ex = await Assert.ThrowsAnyAsync<Exception>(() =>
            client.SendAsync(new HttpRequest { RequestUri = new Uri("http://192.0.2.1/") }).WaitAsync(TimeSpan.FromSeconds(10)));
        sw.Stop();

        Assert.IsType<TimeoutException>(ex);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(5), $"建连耗时 {sw.Elapsed}，未受整次超时约束");
    }
    #endregion
}
