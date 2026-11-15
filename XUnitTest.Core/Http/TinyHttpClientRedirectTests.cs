using System.Net;
using System.Net.Sockets;
using System.Text;
using NewLife.Data;
using NewLife.Http;
using Xunit;

namespace XUnitTest.Http;

/// <summary>TinyHttpClient 跳转次数用尽必须报错</summary>
public class TinyHttpClientRedirectTests
{
    [Fact(DisplayName = "无限重定向时报错而不是返回 3xx")]
    public async Task TooManyRedirectsThrows()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var server = Task.Run(() =>
        {
            while (true)
            {
                TcpClient tcp;
                try { tcp = listener.AcceptTcpClient(); }
                catch { return; }

                using var _ = tcp;
                var ns = tcp.GetStream();
                using var reader = new StreamReader(ns, Encoding.ASCII, false, 1024, true);

                // 同一条连接上可以连续处理多个请求头
                while (true)
                {
                    String? line;
                    var any = false;
                    while (!String.IsNullOrEmpty(line = reader.ReadLine())) any = true;
                    if (!any) break;

                    var head = "HTTP/1.1 302 Found\r\nLocation: /loop\r\nContent-Length: 0\r\n\r\n";
                    var buf = Encoding.ASCII.GetBytes(head);
                    ns.Write(buf, 0, buf.Length);
                    ns.Flush();
                }
            }
        });

        try
        {
            using var tiny = new TinyHttpClient { Timeout = TimeSpan.FromSeconds(15) };
            var request = new HttpRequest { RequestUri = new Uri($"http://127.0.0.1:{port}/a") };

            await Assert.ThrowsAsync<InvalidDataException>(() => tiny.SendAsync(request));
        }
        finally
        {
            listener.Stop();
            await server;
        }
    }

    [Fact(DisplayName = "跳转到同主机另一端口时重建连接")]
    public async Task RedirectToOtherPortReconnects()
    {
        // 目标端口：只有真正发到这里的请求才拿到 OK-B
        var target = new TcpListener(IPAddress.Loopback, 0);
        target.Start();
        var targetPort = ((IPEndPoint)target.LocalEndpoint).Port;
        var targetServer = Task.Run(() =>
        {
            try
            {
                using var tcp = target.AcceptTcpClient();
                using var ns = tcp.GetStream();
                ReadHead(ns);
                WriteAll(ns, "HTTP/1.1 200 OK\r\nContent-Length: 5\r\n\r\nOK-B!");
            }
            catch
            {
                // 监听已停止，测试用服务器直接退出
            }
        });

        // 跳转端口：同主机不同端口，连接保持打开；复用旧连接会把 /b 写到这里，再次拿到 302
        var redirect = new TcpListener(IPAddress.Loopback, 0);
        redirect.Start();
        var redirectPort = ((IPEndPoint)redirect.LocalEndpoint).Port;
        var redirectServer = Task.Run(() =>
        {
            try
            {
                using var tcp = redirect.AcceptTcpClient();
                using var ns = tcp.GetStream();
                while (true)
                {
                    if (!ReadHead(ns)) return;

                    WriteAll(ns, $"HTTP/1.1 302 Found\r\nLocation: http://127.0.0.1:{targetPort}/b\r\nContent-Length: 0\r\n\r\n");
                }
            }
            catch
            {
                // 连接被客户端关闭或监听已停止
            }
        });

        try
        {
            using var tiny = new TinyHttpClient { Timeout = TimeSpan.FromSeconds(15) };
            var request = new HttpRequest { RequestUri = new Uri($"http://127.0.0.1:{redirectPort}/a") };

            var res = await tiny.SendAsync(request);

            Assert.Equal("OK-B!", res?.Body?.ToStr());
        }
        finally
        {
            redirect.Stop();
            target.Stop();
            await Task.WhenAll(redirectServer, targetServer);
        }
    }

    /// <summary>读完一个请求头块，返回是否读到有效请求</summary>
    private static Boolean ReadHead(NetworkStream ns)
    {
        using var reader = new StreamReader(ns, Encoding.ASCII, false, 1024, true);

        String? line;
        var any = false;
        while (!String.IsNullOrEmpty(line = reader.ReadLine())) any = true;

        return any;
    }

    private static void WriteAll(NetworkStream ns, String text)
    {
        var buf = Encoding.ASCII.GetBytes(text);
        ns.Write(buf, 0, buf.Length);
        ns.Flush();
    }
}
