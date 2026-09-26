using System.Net;
using System.Net.Sockets;
using NewLife;
using NewLife.Log;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

public class UpgradeTests
{
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

    [Fact]
    public void CopyAndReplace()
    {
        // 在 CI 环境中跳过，因为目标服务器可能不可达
        if (!IsServerReachable("x.newlifex.com", 80))
        {
            return; // Skip test when server is not reachable
        }

        //Directory.Delete("./Update", true);

        var url = "http://x.newlifex.com/star/staragent50.zip";
        var fileName = Path.GetFileName(url);
        //fileName = "Update".CombinePath(fileName).EnsureDirectory(true);

        var ug = new Upgrade { Log = XTrace.Log };
        ug.Download(url, fileName);

        // 解压
        var source = ug.Extract(ug.SourceFile);

        // 覆盖
        var dest = "./updateTest";
        ug.CopyAndReplace(source, dest);
    }

    /// <summary>启动一个固定返回指定 HTML 的本地 HTTP 服务，供检查更新用例使用</summary>
    private static TcpListener StartHtmlServer(String html, out Int32 port)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        port = ((IPEndPoint)listener.LocalEndpoint).Port;

        _ = Task.Run(async () =>
        {
            while (true)
            {
                TcpClient client;
                try
                {
                    client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                }
                catch
                {
                    break;
                }

                using (client)
                using (var ns = client.GetStream())
                {
                    try
                    {
                        // 读走请求行与头部即可，响应固定内容
                        await ns.ReadAsync(new Byte[1024], 0, 1024).ConfigureAwait(false);

                        var body = html.GetBytes();
                        var head = $"HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n".GetBytes();
                        await ns.WriteAsync(head, 0, head.Length).ConfigureAwait(false);
                        await ns.WriteAsync(body, 0, body.Length).ConfigureAwait(false);
                        await ns.FlushAsync().ConfigureAwait(false);
                    }
                    catch { }
                }
            }
        });

        return listener;
    }

    /// <summary>构造含指定资源包链接的目录页 HTML</summary>
    private static String PackageHtml(String fileName) => $"<table><tr><td>2026-01-01 00:00:00</td><td>1K</td><td><a href=\"{fileName}\">{fileName}</a></td><td>-</td></tr></table>";

    [Fact(DisplayName = "检查更新：首个地址无较新版本时继续检查后续镜像")]
    public void Check_FallsThroughToNextServer()
    {
        // 两个镜像各返回一份资源包目录；第一个只有旧版本，更新版本只在第二个镜像上
        using var s1 = StartHtmlServer(PackageHtml("App_v1.0.0.0.zip"), out var port1);
        using var s2 = StartHtmlServer(PackageHtml("App_v2.0.0.0.zip"), out var port2);

        var ug = new Upgrade
        {
            Name = "App",
            Server = $"http://127.0.0.1:{port1}/,http://127.0.0.1:{port2}/",
            Version = new Version(1, 5),
            DestinationPath = Path.GetTempPath().CombinePath("upgrade_test_" + Guid.NewGuid().ToString("N")),
            Log = XTrace.Log,
        };

        Assert.True(ug.Check());
        Assert.NotNull(ug.Link);
        Assert.StartsWith($"http://127.0.0.1:{port2}/", ug.Link!.Url);
        Assert.Equal(new Version(2, 0, 0, 0), ug.Link.Version);
    }
}
