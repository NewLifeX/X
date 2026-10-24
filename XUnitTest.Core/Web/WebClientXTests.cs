using System.IO.Compression;
using System.Net;
using NewLife;
using NewLife.Data;
using NewLife.Http;
using NewLife.Web;
using Xunit;

namespace XUnitTest.Web;

/// <summary>WebClientX 下载与 CDN 鉴权契约测试</summary>
/// <remarks>
/// 全程本地化：由 <see cref="CdnStub"/> 在动态端口复刻 CDN 目录页与文件下载，目标目录使用临时目录。
/// auth_key 签名规则与实现一致：MD5("{path}-{timestamp}-{rand}-0-{secret}")，
/// 服务端校验时间戳新鲜度与哈希，过期/错误哈希/畸形参数均返回 403。
/// </remarks>
public class WebClientXTests : IDisposable
{
    private readonly String _root;
    private readonly CdnStub _stub = new();

    public WebClientXTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "NewLife_WebClientX_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        _stub.Dispose();
        try
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }
        catch { }
    }

    #region 下载
    [Fact(DisplayName = "普通下载：目录页解析链接并按名称下载落盘")]
    public void DownloadLink_Downloads()
    {
        var content = "hello-webclientx".GetBytes();
        _stub.PageHtml = CreatePage("app_20240501120000.zip", "");
        _stub.FileBytes = content;

        using var client = new WebClientX { Timeout = 10_000 };

        // 目录页解析
        var links = client.GetLinks(_stub.BaseUrl + "/");
        var link = Assert.Single(links);
        Assert.Equal("app", link.Name);
        Assert.Equal("app_20240501120000.zip", link.FullName);
        Assert.Equal(2024, link.Time.Year);

        // 下载
        var file = client.DownloadLink(_stub.BaseUrl + "/", "app", _root);

        Assert.False(file.IsNullOrEmpty());
        Assert.Equal(Path.Combine(_root, "app_20240501120000.zip"), file);
        Assert.True(File.Exists(file));
        var fi = new FileInfo(file);
        Assert.Equal(content.Length, fi.Length);
        Assert.Equal(content, File.ReadAllBytes(file));
        Assert.Equal(1, _stub.FileHits);
        Assert.NotNull(client.LastLink);
    }

    [Fact(DisplayName = "已存在目标文件：无哈希时按本地文件跳过下载")]
    public void DownloadLink_SkipsExistingLocal()
    {
        _stub.PageHtml = CreatePage("app_20240501120000.zip", "");
        _stub.FileBytes = "server-content".GetBytes();

        var local = Path.Combine(_root, "app_20240501120000.zip");
        File.WriteAllText(local, "local-content");

        using var client = new WebClientX { Timeout = 10_000 };
        var file = client.DownloadLink(_stub.BaseUrl + "/", "app", _root);

        Assert.Equal(local, file);
        Assert.Equal("local-content", File.ReadAllText(local));
        // 服务端一次都没被下载，说明 ValidLocal 提前放行
        Assert.Equal(0, _stub.FileHits);
    }

    [Fact(DisplayName = "已存在目标文件：MD5 哈希一致时跳过下载")]
    public void DownloadLink_SkipsExistingByHash()
    {
        var localContent = "local-content";
        _stub.PageHtml = CreatePage("app_20240501120000.zip", localContent.MD5());
        _stub.FileBytes = "server-content".GetBytes();

        var local = Path.Combine(_root, "app_20240501120000.zip");
        File.WriteAllText(local, localContent);

        using var client = new WebClientX { Timeout = 10_000 };
        var file = client.DownloadLink(_stub.BaseUrl + "/", "app", _root);

        Assert.Equal(local, file);
        Assert.Equal(localContent, File.ReadAllText(local));
        Assert.Equal(0, _stub.FileHits);
    }

    [Fact(DisplayName = "GetLinksInDirectory：扫描目录内压缩包")]
    public void GetLinksInDirectory_Local()
    {
        var dir = Path.Combine(_root, "local");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "pkg_20240501120000.zip"), "x");
        File.WriteAllText(Path.Combine(dir, "archive.gz"), "x");
        File.WriteAllText(Path.Combine(dir, "readme.txt"), "x");

        using var client = new WebClientX();
        var links = client.GetLinksInDirectory(dir);

        Assert.Equal(2, links.Length);
        Assert.Contains(links, e => e.FullName == "pkg_20240501120000.zip" && e.Name == "pkg");
        Assert.Contains(links, e => e.FullName == "archive.gz" && e.Name == "archive");

        Assert.Empty(client.GetLinksInDirectory(""));
        Assert.Empty(client.GetLinksInDirectory(Path.Combine(_root, "missing")));
    }

    [Fact(DisplayName = "DownloadLinkAndExtract：下载 zip 并解压到目标目录")]
    public void DownloadLinkAndExtract_Zip()
    {
        var payload = "zipped-content".GetBytes();
        _stub.PageHtml = CreatePage("pkg_20240501120000.zip", "");
        _stub.FileBytes = CreateZip("sub/data.txt", payload);

        using var client = new WebClientX { Timeout = 10_000 };
        var file = client.DownloadLinkAndExtract(_stub.BaseUrl + "/", "pkg", _root);

        Assert.NotNull(file);
        Assert.Equal(Path.Combine(_root, "pkg_20240501120000.zip"), file);
        Assert.True(File.Exists(file));

        var extracted = Path.Combine(_root, "sub", "data.txt");
        Assert.True(File.Exists(extracted));
        Assert.Equal(payload, File.ReadAllBytes(extracted));
    }
    #endregion

    #region CDN 鉴权
    [Fact(DisplayName = "auth_key 签名：时间戳+随机数+hash 通过服务端校验，已有 auth_key 不重复追加")]
    public async Task CheckAuth_SignaturePasses()
    {
        using var client = new WebClientX { AuthKey = _stub.Secret };

        var text = await client.DownloadStringAsync(_stub.BaseUrl + "/auth/ok?x=1");

        Assert.Equal("auth ok", text);
        Assert.Equal(1, _stub.AuthPass);

        var ss = _stub.LastAuthKey!.Split('-');
        Assert.Equal(4, ss.Length);
        Assert.Equal("0", ss[2]);
        // 时间戳为当前 Unix 秒
        Assert.True(Math.Abs(Runtime.UtcNow.ToInt() - ss[0].ToLong()) < 60);

        // 地址里已有 auth_key 时原样使用，不再追加
        var preset = BuildAuthKey("/auth/ok", Runtime.UtcNow.ToInt(), 654321, _stub.Secret);
        text = await client.DownloadStringAsync($"{_stub.BaseUrl}/auth/ok?auth_key={preset}");

        Assert.Equal("auth ok", text);
        Assert.Equal(preset, _stub.LastAuthKey);
        Assert.Equal(2, _stub.AuthPass);
    }

    [Theory(DisplayName = "auth_key 校验失败：过期时间戳/错误 hash/畸形参数均被拒绝")]
    [InlineData("expired", "expired")]
    [InlineData("badhash", "bad hash")]
    [InlineData("malformed", "malformed auth_key")]
    public async Task CheckAuth_Rejected(String mode, String expected)
    {
        const String path = "/auth/ok";
        var now = Runtime.UtcNow.ToInt();
        var key = mode switch
        {
            "expired" => BuildAuthKey(path, now - 3600, 123456, _stub.Secret),
            "badhash" => $"{now}-123456-0-{new String('a', 32)}",
            _ => "abc",
        };

        using var client = new WebClientX { AuthKey = _stub.Secret };
        var text = await client.DownloadStringAsync($"{_stub.BaseUrl}{path}?auth_key={key}");

        Assert.Equal(expected, text);
        Assert.Equal(0, _stub.AuthPass);
        Assert.Equal(1, _stub.AuthFail);
    }

    [Fact(DisplayName = "auth_key 未配置：不追加签名参数")]
    public async Task CheckAuth_NotConfigured()
    {
        using var client = new WebClientX();

        var text = await client.DownloadStringAsync(_stub.BaseUrl + "/auth/ok");

        Assert.Equal("missing auth_key", text);
        Assert.Null(_stub.LastAuthKey);
        Assert.Equal(1, _stub.AuthFail);
    }
    #endregion

    #region 辅助
    private static String CreatePage(String fileName, String hash) => $"""
        <table>
        <tr><td>2024-05-01 12:00:00</td><td>1 KB</td><td><a href="/files/{fileName}">{fileName}</a></td><td>{hash}</td></tr>
        </table>
        """;

    private static String BuildAuthKey(String path, Int64 time, Int32 rand, String secret)
    {
        var hash = $"{path}-{time}-{rand}-0-{secret}".MD5().ToLower();
        return $"{time}-{rand}-0-{hash}";
    }

    private static Byte[] CreateZip(String entryName, Byte[] content)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            var entry = zip.CreateEntry(entryName);
            using var es = entry.Open();
            es.Write(content, 0, content.Length);
        }

        return ms.ToArray();
    }

    /// <summary>本地 CDN 桩：目录页 + 文件字节流 + auth_key 校验</summary>
    private sealed class CdnStub : IDisposable
    {
        private readonly HttpServer _server;
        private Int32 _fileHits;
        private Int32 _authPass;
        private Int32 _authFail;

        /// <summary>服务地址</summary>
        public String BaseUrl { get; }

        /// <summary>CDN 鉴权密钥</summary>
        public String Secret { get; } = "unit-test-secret";

        /// <summary>目录页 Html</summary>
        public String PageHtml { get; set; } = "";

        /// <summary>文件字节流</summary>
        public Byte[] FileBytes { get; set; } = [];

        /// <summary>文件下载次数</summary>
        public Int32 FileHits => _fileHits;

        /// <summary>鉴权通过次数</summary>
        public Int32 AuthPass => _authPass;

        /// <summary>鉴权失败次数</summary>
        public Int32 AuthFail => _authFail;

        /// <summary>最近一次收到的 auth_key</summary>
        public String? LastAuthKey { get; private set; }

        public CdnStub()
        {
            // 动态端口由系统分配
            _server = new HttpServer { Port = 0 };
            _server.Map("/", Page);
            _server.Map("/files/*", Download);
            _server.Map("/auth/ok", Auth);
            _server.Start();

            BaseUrl = $"http://127.0.0.1:{_server.Port}";
        }

        public void Dispose() => _server.TryDispose();

        private void Page(IHttpContext context) => context.WriteHtml(PageHtml);

        private void Download(IHttpContext context)
        {
            Interlocked.Increment(ref _fileHits);
            context.Response.ContentType = "application/octet-stream";
            context.Response.Body = (ArrayPacket)FileBytes;
        }

        private void Auth(IHttpContext context)
        {
            var url = context.Request.RequestUri!.ToString();
            var p = url.IndexOf('?');
            var path = p >= 0 ? url[..p] : url;
            var key = GetQuery(context, "auth_key");
            LastAuthKey = key;

            if (key.IsNullOrEmpty())
            {
                Interlocked.Increment(ref _authFail);
                context.WriteText("missing auth_key", HttpStatusCode.Forbidden);
                return;
            }

            var ss = key.Split('-');
            if (ss == null || ss.Length != 4 || !Int64.TryParse(ss[0], out var time) || !Int32.TryParse(ss[1], out var rand) || ss[2] != "0")
            {
                Interlocked.Increment(ref _authFail);
                context.WriteText("malformed auth_key", HttpStatusCode.Forbidden);
                return;
            }

            // 时间戳超过 5 分钟视为过期
            if (Math.Abs(Runtime.UtcNow.ToInt() - time) > 300)
            {
                Interlocked.Increment(ref _authFail);
                context.WriteText("expired", HttpStatusCode.Forbidden);
                return;
            }

            var hash = $"{path}-{time}-{rand}-0-{Secret}".MD5().ToLower();
            if (!hash.EqualIgnoreCase(ss[3]))
            {
                Interlocked.Increment(ref _authFail);
                context.WriteText("bad hash", HttpStatusCode.Forbidden);
                return;
            }

            Interlocked.Increment(ref _authPass);
            context.WriteText("auth ok");
        }

        private static String? GetQuery(IHttpContext context, String name)
        {
            // 服务端解析出的 RequestUri 为相对地址，Uri.Query/AbsolutePath 均不可用，直接从字符串解析
            var url = context.Request.RequestUri?.ToString();
            if (url.IsNullOrEmpty()) return null;

            var p = url.IndexOf('?');
            if (p < 0) return null;

            foreach (var item in url[(p + 1)..].Split('&'))
            {
                var p2 = item.IndexOf('=');
                if (p2 <= 0) continue;

                if (item[..p2].EqualIgnoreCase(name)) return Uri.UnescapeDataString(item[(p2 + 1)..]);
            }

            return null;
        }
    }
    #endregion
}
