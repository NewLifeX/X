using System.Net.Sockets;
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
}
