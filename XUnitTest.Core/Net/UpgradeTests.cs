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

        // Upgrade.Extract 把更新包解压到系统临时目录下的同名子目录（staragent50），
        // 多目标框架并行运行时会相互占用同一个解压目录，故用跨进程互斥串行执行
        using var mutex = new Mutex(false, @"Global\NewLife.Core.UpgradeTests.CopyAndReplace");
        var acquired = false;
        try
        {
            try
            {
                acquired = mutex.WaitOne(TimeSpan.FromMinutes(5));
            }
            catch (AbandonedMutexException)
            {
                // 上一个持有者进程异常退出，互斥量已归本线程
                acquired = true;
            }

            if (!acquired) return;

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
        finally
        {
            if (acquired) mutex.ReleaseMutex();
        }
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

    /// <summary>启动一个接受连接但不应答的本地服务，供下载超时用例使用</summary>
    private static TcpListener StartHangServer(out Int32 port)
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

                _ = Task.Run(async () =>
                {
                    using (client)
                    using (var ns = client.GetStream())
                    {
                        try
                        {
                            // 读走请求但不应答，保持连接直到用例结束
                            await ns.ReadAsync(new Byte[1024], 0, 1024).ConfigureAwait(false);
                            await Task.Delay(5_000).ConfigureAwait(false);
                        }
                        catch { }
                    }
                });
            }
        });

        return listener;
    }

    [Fact(DisplayName = "下载超时_按失败处理并抛超时")]
    public void Download_Timeout_Throws()
    {
        using var listener = StartHangServer(out var port);

        var ug = new Upgrade
        {
            DownloadTimeout = 500,
            UpdatePath = Path.GetTempPath().CombinePath("upgrade_timeout_" + Guid.NewGuid().ToString("N")),
            Log = XTrace.Log,
        };

        var url = $"http://127.0.0.1:{port}/App_v9.zip";
        var ex = Assert.Throws<TimeoutException>(() => ug.Download(url, "App_v9.zip"));
        Assert.Contains("timeout", ex.Message, StringComparison.OrdinalIgnoreCase);

        // 超时后不得把未完成的文件当成下载结果（旧实现继续执行并回写 SourceFile）
        Assert.Null(ug.SourceFile);
    }

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

    [Fact(DisplayName = "重启目标：apphost形态直接启动进程路径")]
    public void RestartTarget_Apphost_NoArgs()
    {
        var app = Path.Combine("app", "MyApp.exe");
        var dll = Path.Combine("app", "MyApp.dll");

        var (exe, args) = Upgrade.ResolveRestartTarget(app, dll);

        // 进程路径本身就是应用，不需要带参数
        Assert.Equal(app, exe);
        Assert.Null(args);
    }

    [Fact(DisplayName = "重启目标：框架依赖形态以主机加入口程序集启动")]
    public void RestartTarget_FrameworkDependent_HostWithEntry()
    {
        var host = Path.Combine("dotnet-root", "DOTNET.EXE");
        var dll = Path.Combine("app", "MyApp.dll");

        var (exe, args) = Upgrade.ResolveRestartTarget(host, dll);

        // 直接启动主机只会起一个无参数的裸主机（打印用法后退出），必须带上入口程序集；主机名判定不区分大小写与扩展名
        Assert.Equal(host, exe);
        Assert.Equal(dll, args);
    }

    [Fact(DisplayName = "重启目标：主机形态但入口缺失时保持现状")]
    public void RestartTarget_HostWithoutEntry_NoTarget()
    {
        var (exe, args) = Upgrade.ResolveRestartTarget(Path.Combine("dotnet-root", "dotnet"), null);

        // 宁可不动（不启动也不退出），也不启动一个无参数的裸主机
        Assert.Null(exe);
        Assert.Null(args);
    }

    [Fact(DisplayName = "重启目标：进程路径缺失时无法重启")]
    public void RestartTarget_NoProcessPath_NoTarget()
    {
        var (exe, args) = Upgrade.ResolveRestartTarget(null, Path.Combine("app", "MyApp.dll"));

        Assert.Null(exe);
        Assert.Null(args);
    }
}
