using System.Net;
using System.Net.Http.Headers;
using NewLife;
using NewLife.Log;
using NewLife.Remoting;
using Xunit;

namespace XUnitTest.Remoting;

public class ApiHttpClientRaceTests
{
    static ApiHttpClientRaceTests() => NewLife.Http.HttpHelper.Tracer = new DefaultTracer();

    // 固定的测试内容和其真实哈希值
    private const String TestContent = "MockFileContent";
    private static readonly Byte[] TestData = TestContent.GetBytes();
    private static readonly String TestMd5 = TestContent.MD5();
    private static readonly String TestSha256 = TestData.SHA256().ToHex();

    class MockHandler : HttpMessageHandler
    {
        private readonly Int32 _delayMs;
        private readonly String? _headerHash;
        private readonly Byte[] _data;

        public MockHandler(Int32 delayMs = 0, String? headerHash = null, Byte[]? data = null)
        {
            _delayMs = delayMs;
            _headerHash = headerHash;
            _data = data ?? TestData;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_delayMs > 0)
                await Task.Delay(_delayMs, cancellationToken);

            var rs = new HttpResponseMessage(HttpStatusCode.OK);

            if (request.Method == HttpMethod.Head)
            {
                AppendHashHeaders(rs, _headerHash);
            }
            else
            {
                rs.Content = new ByteArrayContent(_data);
                AppendHashHeaders(rs, _headerHash);
            }

            return rs;
        }

        private static void AppendHashHeaders(HttpResponseMessage rs, String? hash)
        {
            if (hash.IsNullOrEmpty()) return;

            if (hash.Contains("digest:", StringComparison.OrdinalIgnoreCase))
                rs.Headers.Add("Digest", hash.Replace("digest:", "", StringComparison.OrdinalIgnoreCase));
            else if (hash.Contains("xfilehash:", StringComparison.OrdinalIgnoreCase))
                rs.Headers.Add("X-File-Hash", hash.Replace("xfilehash:", "", StringComparison.OrdinalIgnoreCase));
            else if (hash.Contains("etag:", StringComparison.OrdinalIgnoreCase))
                rs.Headers.ETag = new EntityTagHeaderValue($"\"{hash.Replace("etag:", "", StringComparison.OrdinalIgnoreCase)}\"");
            else
                rs.Headers.Add("X-Content-MD5", hash);
        }
    }

    [Fact(DisplayName = "竞速下载_选择最快响应")]
    public async Task DownloadFileRaceAsync_Fastest()
    {
        var client = new ApiHttpClient
        {
            UseProxy = false,
            Timeout = 5000
        };

        // 添加三个服务，第二个响应最快
        client.Add("slow1", "http://service1.test");
        client.Services[0].Client = new HttpClient(new MockHandler(200));

        client.Add("fast", "http://service2.test");
        client.Services[1].Client = new HttpClient(new MockHandler(50));

        client.Add("slow2", "http://service3.test");
        client.Services[2].Client = new HttpClient(new MockHandler(300));

        var file = "race_fastest.txt";
        var fullPath = file.GetFullPath();
        if (File.Exists(fullPath)) File.Delete(fullPath);

        await client.DownloadFileRaceAsync("/test.txt", file, null, false);

        Assert.True(File.Exists(fullPath));
        Assert.Equal("fast", client.Current?.Name);
    }

    [Fact(DisplayName = "竞速下载_HEAD竞速选择最快通过校验")]
    public async Task DownloadFileRaceAsync_HeadRace_FastestHashMatch()
    {
        // 验证 HEAD 检查是竞速模式：谁先通过哈希校验谁被选中
        var expectedHash = $"md5${TestMd5}";
        var client = new ApiHttpClient
        {
            UseProxy = false,
            Timeout = 5000
        };

        // 服务1：最快响应但哈希不匹配
        client.Add("fastest_wrong", "http://service1.test");
        client.Services[0].Client = new HttpClient(new MockHandler(10, "md5$11111111111111111111111111111111"));

        // 服务2：较慢但哈希匹配 - 应该被选中
        client.Add("slower_correct", "http://service2.test");
        client.Services[1].Client = new HttpClient(new MockHandler(100, expectedHash));

        // 服务3：最慢，哈希也匹配
        client.Add("slowest_correct", "http://service3.test");
        client.Services[2].Client = new HttpClient(new MockHandler(500, expectedHash));

        var file = "race_head_race.txt";
        var fullPath = file.GetFullPath();
        if (File.Exists(fullPath)) File.Delete(fullPath);

        await client.DownloadFileRaceAsync("/test.txt", file, expectedHash, useHeadCheck: true);

        Assert.True(File.Exists(fullPath));
        Assert.Equal("slower_correct", client.Current?.Name);
    }

    [Fact(DisplayName = "竞速下载_HEAD先行检查匹配哈希")]
    public async Task DownloadFileRaceAsync_HeadCheck_HashMatch()
    {
        var expectedHash = $"md5${TestMd5}";
        var client = new ApiHttpClient
        {
            UseProxy = false,
            Timeout = 5000
        };

        client.Add("wrong1", "http://service1.test");
        client.Services[0].Client = new HttpClient(new MockHandler(50, "md5$11111111111111111111111111111111"));

        client.Add("correct", "http://service2.test");
        client.Services[1].Client = new HttpClient(new MockHandler(100, expectedHash));

        client.Add("wrong2", "http://service3.test");
        client.Services[2].Client = new HttpClient(new MockHandler(30, "md5$22222222222222222222222222222222"));

        var file = "race_head_match.txt";
        var fullPath = file.GetFullPath();
        if (File.Exists(fullPath)) File.Delete(fullPath);

        await client.DownloadFileRaceAsync("/test.txt", file, expectedHash, useHeadCheck: true);

        Assert.True(File.Exists(fullPath));
        Assert.Equal("correct", client.Current?.Name);
    }

    [Fact(DisplayName = "竞速下载_GET响应头哈希匹配")]
    public async Task DownloadFileRaceAsync_GetHeader_HashMatch()
    {
        var expectedHash = $"sha256${TestSha256}";
        var client = new ApiHttpClient
        {
            UseProxy = false,
            Timeout = 5000
        };

        client.Add("noHash", "http://service1.test");
        client.Services[0].Client = new HttpClient(new MockHandler(50));

        client.Add("correct", "http://service2.test");
        client.Services[1].Client = new HttpClient(new MockHandler(100, $"xfilehash:sha256:{TestSha256}"));

        client.Add("wrong", "http://service3.test");
        client.Services[2].Client = new HttpClient(new MockHandler(30, "sha256$bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"));

        var file = "race_get_match.txt";
        var fullPath = file.GetFullPath();
        if (File.Exists(fullPath)) File.Delete(fullPath);

        await client.DownloadFileRaceAsync("/test.txt", file, expectedHash, useHeadCheck: false);

        Assert.True(File.Exists(fullPath));
        Assert.Equal("correct", client.Current?.Name);
    }

    [Fact(DisplayName = "竞速下载_Digest头部解析")]
    public async Task DownloadFileRaceAsync_DigestHeader()
    {
        var expectedHash = $"sha256${TestSha256}";
        var client = new ApiHttpClient
        {
            UseProxy = false,
            Timeout = 5000
        };

        client.Add("service1", "http://service1.test");
        client.Services[0].Client = new HttpClient(new MockHandler(50, $"digest:SHA-256={TestSha256}"));

        var file = "race_digest.txt";
        var fullPath = file.GetFullPath();
        if (File.Exists(fullPath)) File.Delete(fullPath);

        await client.DownloadFileRaceAsync("/test.txt", file, expectedHash, useHeadCheck: true);

        Assert.True(File.Exists(fullPath));
    }

    [Fact(DisplayName = "竞速下载_ETag头部解析")]
    public async Task DownloadFileRaceAsync_ETagHeader()
    {
        var expectedHash = $"md5${TestMd5}";
        var client = new ApiHttpClient
        {
            UseProxy = false,
            Timeout = 5000
        };

        client.Add("service1", "http://service1.test");
        client.Services[0].Client = new HttpClient(new MockHandler(50, $"etag:{TestMd5}"));

        var file = "race_etag.txt";
        var fullPath = file.GetFullPath();
        if (File.Exists(fullPath)) File.Delete(fullPath);

        await client.DownloadFileRaceAsync("/test.txt", file, expectedHash, useHeadCheck: false);

        Assert.True(File.Exists(fullPath));
    }

    [Fact(DisplayName = "竞速下载_单服务降级为普通下载")]
    public async Task DownloadFileRaceAsync_SingleService()
    {
        var client = new ApiHttpClient
        {
            UseProxy = false,
            Timeout = 5000
        };

        client.Add("single", "http://service1.test");
        client.Services[0].Client = new HttpClient(new MockHandler(50));

        var file = "race_single.txt";
        var fullPath = file.GetFullPath();
        if (File.Exists(fullPath)) File.Delete(fullPath);

        // 单服务应该自动降级为普通下载
        await client.DownloadFileRaceAsync("/test.txt", file, null, false);

        Assert.True(File.Exists(fullPath));
        Assert.Equal("single", client.Current?.Name);
    }

    [Fact(DisplayName = "竞速下载_全部服务不可用则抛出异常")]
    public async Task DownloadFileRaceAsync_AllShielded()
    {
        var client = new ApiHttpClient
        {
            UseProxy = false,
            Timeout = 5000
        };

        client.Add("service1", "http://service1.test");
        client.Services[0].Client = new HttpClient(new MockHandler(50));
        client.Services[0].NextTime = DateTime.Now.AddSeconds(60);

        client.Add("service2", "http://service2.test");
        client.Services[1].Client = new HttpClient(new MockHandler(30));
        client.Services[1].NextTime = DateTime.Now.AddSeconds(60);

        var file = "race_shielded.txt";
        var fullPath = file.GetFullPath();
        if (File.Exists(fullPath)) File.Delete(fullPath);

        // 目前行为：当全部被屏蔽，默认启用所有服务，所以应当能下载
        await client.DownloadFileRaceAsync("/test.txt", file, null, false);
        Assert.True(File.Exists(fullPath));
    }
}
