using NewLife;
using NewLife.Http;
using NewLife.Web;
using Xunit;

namespace XUnitTest.Web;

public class WebClientTests
{
    [Fact]
    public void GetLinks()
    {
        // 本地化：与软件站同构的表格页面（时间/大小/链接/哈希四列），解析语义不变，全程不触网
        using var server = new HttpServer { Port = 0 };
        server.Map("/", () => """
            <table>
            <tr><td>2024-05-14 10:00:00</td><td>1 MB</td><td><a href="/files/System.Data.SQLite.win-x64.zip">System.Data.SQLite.win-x64.zip</a></td><td>-</td></tr>
            <tr><td>2024-05-10 10:00:00</td><td>1 MB</td><td><a href="/files/System.Data.SQLite.win.zip">System.Data.SQLite.win.zip</a></td><td>-</td></tr>
            <tr><td>2024-04-01 10:00:00</td><td>1 MB</td><td><a href="/files/System.Data.SQLite_net80.zip">System.Data.SQLite_net80.zip</a></td><td>-</td></tr>
            <tr><td>2024-04-01 10:00:00</td><td>1 MB</td><td><a href="/files/System.Data.SQLite_netstandard21.zip">System.Data.SQLite_netstandard21.zip</a></td><td>-</td></tr>
            <tr><td>2024-04-01 10:00:00</td><td>1 MB</td><td><a href="/files/System.Data.SQLite_netstandard20.zip">System.Data.SQLite_netstandard20.zip</a></td><td>-</td></tr>
            <tr><td>2024-04-01 10:00:00</td><td>1 MB</td><td><a href="/files/System.Data.SQLite.zip">System.Data.SQLite.zip</a></td><td>-</td></tr>
            </table>
            """);
        server.Start();

        var client = new WebClientX();
        var links = client.GetLinks($"http://127.0.0.1:{server.Port}/");

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
