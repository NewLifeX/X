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

    [Fact(DisplayName = "并发创建客户端只保留一个实例")]
    public void EnsureCreate_Concurrent()
    {
        using var client = new WebClientX();

        var results = new HttpClient?[16];
        Parallel.For(0, results.Length, i => results[i] = client.EnsureCreate());

        var first = results[0];
        Assert.All(results, e => Assert.Same(first, e));
    }

    #region 资源释放
    /// <summary>带释放标记的内容</summary>
    private class TrackingContent : StringContent
    {
        public Boolean Disposed { get; private set; }

        public TrackingContent(String content) : base(content) { }

        protected override void Dispose(Boolean disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    /// <summary>不发起真实请求，直接返回可控内容</summary>
    private class TestWebClientX : WebClientX
    {
        public TrackingContent? Last;

        public override Task<HttpContent> SendAsync(String address, HttpContent? content = null, CancellationToken cancellationToken = default)
        {
            Last = new TrackingContent("web-ok");
            return Task.FromResult<HttpContent>(Last);
        }
    }

    [Fact(DisplayName = "下载字符串_内容被释放")]
    public async Task DownloadString_DisposesContent()
    {
        using var client = new TestWebClientX();

        var rs = await client.DownloadStringAsync("http://x/y");

        // SendAsync 返回的内容所有权在调用方，下载封装读取后必须释放
        Assert.Equal("web-ok", rs);
        Assert.True(client.Last!.Disposed);
    }
    #endregion
}
