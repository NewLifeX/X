using System.Buffers;
using System.ComponentModel;
using System.Net.Sockets;
using NewLife;
using NewLife.Data;
using NewLife.Http;
using NewLife.Log;
using NewLife.Net;
using NewLife.Messaging;
using Xunit;

namespace XUnitTest.Net;

/// <summary>Unix域套接字传输测试。依赖操作系统的AF_UNIX支持（Linux/macOS，Windows 10 1803+），不支持时静默跳过</summary>
[Collection("Net")]
[DisplayName("Unix域套接字测试")]
public class UdsNetServerTests
{
    #region 辅助
    private static String NewTempPath() => Path.Combine(Path.GetTempPath(), $"nl_uds_{Guid.NewGuid():N}.sock");

    /// <summary>探测当前系统是否支持Unix域套接字</summary>
    private static Boolean UnixSupported()
    {
        var path = NewTempPath();
        try
        {
            using var sock = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            sock.Bind(new UnixDomainSocketEndPoint(path));

            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            try { File.Delete(path); } catch { }
        }
    }
    #endregion

    [Fact]
    [DisplayName("Unix域套接字_Echo回环")]
    public void Echo()
    {
        if (!UnixSupported())
        {
            XTrace.WriteLine("当前系统不支持Unix域套接字，跳过测试");
            return;
        }

        var path = NewTempPath();
        try
        {
            using var server = new UdsEchoServer { Local = new NetUri($"unix://{path}") };
            server.Start();

            Assert.True(server.Active);
            Assert.True(File.Exists(path));

            using var client = new NetUri($"unix://{path}").CreateRemote();

            var wait = new ManualResetEventSlim();
            Byte[]? received = null;
            client.Received += (s, e) =>
            {
                received = e.GetBytes();
                wait.Set();
            };

            client.Open();

            var payload = new Byte[32];
            Random.Shared.NextBytes(payload);
            _ = client.Send(payload);

            Assert.True(wait.Wait(3_000));
            Assert.NotNull(received);
            Assert.Equal(payload, received);
        }
        finally
        {
            try { File.Delete(path); } catch { }
        }
    }

    [Fact]
    [DisplayName("Unix域套接字_编解码器Echo")]
    public async Task CodecEcho()
    {
        if (!UnixSupported()) return;

        var path = NewTempPath();
        try
        {
            using var server = new NetServer
            {
                Local = new NetUri($"unix://{path}"),
                Protocol = new SrmpCodec(),
            };
            server.Received += async (s, e) =>
            {
                if (s is not INetSession session) return;
                if (e.Message is not DefaultMessage m) return;

                // 回显解码后的消息体
                var reply = m.CreateReply();
                if (m.Body != null) reply.SetBody(await m.Body.ReadAllAsync());
                session.SendReply(reply, e);
            };
            server.Start();

            Assert.True(server.Active);

            using var client = new NetUri($"unix://{path}").CreateRemote();
            ((SessionBase)client).Protocol = new SrmpCodec();
            client.Open();

            var request = new DefaultMessage();
            request.SetBody(new ArrayPacket("Hello UDS"u8.ToArray()));
            var response = await client.SendMessageAsync(request);

            Assert.NotNull(response);
            var respMsg = Assert.IsAssignableFrom<IMessage>(response);
            var body = await respMsg.Body!.ReadAllAsync();
            Assert.Equal("Hello UDS"u8.ToArray(), body.AsReadOnlySequence().ToArray());
            body.TryDispose();
        }
        finally
        {
            try { File.Delete(path); } catch { }
        }
    }

    [Fact]
    [DisplayName("Unix域套接字_停止后删除套接字文件")]
    public void StopDeletesFile()
    {
        if (!UnixSupported()) return;

        var path = NewTempPath();
        var server = new NetServer { Local = new NetUri($"unix://{path}") };
        server.Start();

        Assert.True(File.Exists(path));

        server.Stop("Test");
        server.Dispose();

        Assert.False(File.Exists(path));
    }

    [Fact]
    [DisplayName("Unix域套接字_自动清理残留文件后重启")]
    public void CleanResidualFile()
    {
        if (!UnixSupported()) return;

        var path = NewTempPath();
        try
        {
            // 制造残留：绑定原始套接字后关闭，不删除文件（模拟进程异常退出）
            using (var raw = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified))
            {
                raw.Bind(new UnixDomainSocketEndPoint(path));
            }

            // 平台自动删除文件时，本用例退化为普通启动场景
            using var server = new NetServer { Local = new NetUri($"unix://{path}") };
            server.Start();

            Assert.True(server.Active);
        }
        finally
        {
            try { File.Delete(path); } catch { }
        }
    }

    [Fact]
    [DisplayName("Unix域套接字_已被监听时拒绝启动")]
    public void RejectWhenInUse()
    {
        if (!UnixSupported()) return;

        var path = NewTempPath();
        try
        {
            using var server1 = new NetServer { Local = new NetUri($"unix://{path}") };
            server1.Start();
            Assert.True(server1.Active);

            // 第二个实例使用相同路径时，应拒绝启动而不是抢占
            using var server2 = new NetServer { Local = new NetUri($"unix://{path}") };
            var ex = Assert.Throws<InvalidOperationException>(() => server2.Start());
            Assert.Contains(path, ex.Message);
        }
        finally
        {
            try { File.Delete(path); } catch { }
        }
    }

    [Fact]
    [DisplayName("Unix域套接字_HttpServer冒烟")]
    public void HttpOverUds()
    {
        if (!UnixSupported()) return;

        var path = NewTempPath();
        try
        {
            using var server = new HttpServer { Local = new NetUri($"unix://{path}") };
            server.MapGet("/ping", () => "pong");
            server.Start();

            Assert.True(server.Active);

            using var client = new NetUri($"unix://{path}").CreateRemote();

            // HTTP响应可能分片到达，用事件收集；同步Receive会与后台接收环竞争同一连接
            var wait = new ManualResetEventSlim();
            var text = "";
            client.Received += (s, e) =>
            {
                var bytes = e.GetBytes();
                if (bytes != null) text += bytes.ToStr();

                if (text.Contains("pong")) wait.Set();
            };

            client.Open();

            var request = "GET /ping HTTP/1.1\r\nHost: localhost\r\nConnection: close\r\n\r\n".GetBytes();
            _ = client.Send(request);

            Assert.True(wait.Wait(3_000), "未收到HTTP响应");

            Assert.Contains("200", text);
            Assert.Contains("pong", text);
        }
        finally
        {
            try { File.Delete(path); } catch { }
        }
    }

    [Fact]
    [DisplayName("Unix域套接字_拉取模式收发")]
    public async Task PullModeEcho()
    {
        if (!UnixSupported()) return;

        var path = NewTempPath();
        try
        {
            using var server = new NetServer { Local = new NetUri($"unix://{path}") };
            server.Received += (s, e) =>
            {
                if (s is INetSession session && e.Packet != null) session.Send(e.Packet);
            };
            server.Start();

            // 拉取模式客户端：打开前关闭自动接收环
            using var client = new TcpSession
            {
                Remote = new NetUri($"unix://{path}"),
                AutoReceive = false,
                Log = XTrace.Log,
            };
            client.Open();

            _ = client.Send("uds-pull");

            using var pk = await client.ReceiveAsync(default).WaitAsync(TimeSpan.FromSeconds(5));
            Assert.NotNull(pk);
            Assert.Equal("uds-pull", pk!.ToStr());
        }
        finally
        {
            try { File.Delete(path); } catch { }
        }
    }

    [Fact]
    [DisplayName("Unix域套接字_拉取时对端关闭_不挂死")]
    public async Task PullModeRemoteClose()
    {
        if (!UnixSupported()) return;

        var path = NewTempPath();
        try
        {
            // 服务端：收到数据后主动关闭会话
            using var server = new NetServer { Local = new NetUri($"unix://{path}") };
            server.Received += (s, e) =>
            {
                if (s is NetSession ns) ns.Close("server-close");
            };
            server.Start();

            using var client = new TcpSession
            {
                Remote = new NetUri($"unix://{path}"),
                AutoReceive = false,
                Log = XTrace.Log,
            };
            client.Open();

            _ = client.Send("bye");

            // 对端关闭后应结束拉取（空引用/空包或连接级异常），而不是永久挂起
            try
            {
                using var pk = await client.ReceiveAsync(default).WaitAsync(TimeSpan.FromSeconds(5));
                Assert.True(pk == null || pk.Length == 0, $"对端关闭后拉取应结束，实际收到 {pk?.Length} 字节");
            }
            catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException)
            {
                // 连接级异常亦为有效断开语义
            }
        }
        finally
        {
            try { File.Delete(path); } catch { }
        }
    }

    #region 服务端
    class UdsEchoServer : NetServer<UdsEchoSession>
    {
    }

    class UdsEchoSession : NetSession<UdsEchoServer>
    {
        protected override void OnReceive(ReceivedEventArgs e)
        {
            var packet = e.Packet;
            if (packet == null || packet.Length == 0) return;

            Send(packet);
        }
    }
    #endregion
}
