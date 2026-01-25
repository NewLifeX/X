using System.Net.Sockets;
using NewLife;
using NewLife.Web;
using Xunit;

namespace XUnitTest.Web;

public class WebClientTests
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
    public void GetLinks()
    {
        // 在 CI 环境中跳过，因为目标服务器可能不可达
        if (!IsServerReachable("x.newlifex.com", 80)) return;

        var client = new WebClientX();
        var links = client.GetLinks("http://x.newlifex.com");
        Assert.NotEmpty(links);

        var names = "System.Data.SQLite.win-x64,System.Data.SQLite.win,System.Data.SQLite_net80,System.Data.SQLite_netstandard21,System.Data.SQLite_netstandard20,System.Data.SQLite".Split(",", ";");

        links = links.Where(e => e.Name.EqualIgnoreCase(names) || e.FullName.EqualIgnoreCase(names)).ToArray();
        var link = links.OrderByDescending(e => e.Version).ThenByDescending(e => e.Time).FirstOrDefault();

        Assert.NotNull(link);
        Assert.Equal("System.Data.SQLite.win-x64", link.Name);
        Assert.True(link.Time >= "2024-05-14".ToDateTime());
        Assert.Null(link.Version);
    }
}
