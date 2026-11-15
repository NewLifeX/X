using System.Net;
using System.Net.Http;
using NewLife.Http;
using Xunit;

namespace XUnitTest.Http;

/// <summary>下载重试策略：只重试网络类错误</summary>
public class HttpDownloadRetryTests
{
    private sealed class QueueHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;

        public QueueHandler(params HttpResponseMessage[] responses) => _responses = new Queue<HttpResponseMessage>(responses);

        public Int32 Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;

            return Task.FromResult(_responses.Count > 1 ? _responses.Dequeue() : _responses.Peek());
        }
    }

    private static String TempFile() => Path.Combine(Path.GetTempPath(), "dl-" + Guid.NewGuid().ToString("N") + ".bin");

    [Fact(DisplayName = "404 不重试")]
    public async Task NotFoundNotRetried()
    {
        using var handler = new QueueHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        using var client = new HttpClient(handler);
        var file = TempFile();
        var options = new HttpDownloadOptions { RetryCount = 3, RetryDelay = TimeSpan.Zero };

        await Assert.ThrowsAsync<HttpRequestException>(() => client.DownloadFileAsync("http://127.0.0.1/x", file, options));
        Assert.Equal(1, handler.Calls);
    }

    [Fact(DisplayName = "503 会重试")]
    public async Task ServiceUnavailableRetried()
    {
        var ok = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("data") };
        using var handler = new QueueHandler(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable), ok);
        using var client = new HttpClient(handler);
        var file = TempFile();
        var options = new HttpDownloadOptions { RetryCount = 3, RetryDelay = TimeSpan.Zero };
        try
        {
            await client.DownloadFileAsync("http://127.0.0.1/x", file, options);

            Assert.Equal(2, handler.Calls);
        }
        finally
        {
            if (File.Exists(file)) File.Delete(file);
        }
    }

    [Fact(DisplayName = "哈希校验失败不重试")]
    public async Task HashMismatchNotRetried()
    {
        using var handler = new QueueHandler(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("data") });
        using var client = new HttpClient(handler);
        var file = TempFile();
        var options = new HttpDownloadOptions
        {
            RetryCount = 3,
            RetryDelay = TimeSpan.Zero,
            ExpectedHash = "md5$00000000000000000000000000000000",
        };

        await Assert.ThrowsAsync<InvalidDataException>(() => client.DownloadFileAsync("http://127.0.0.1/x", file, options));
        Assert.Equal(1, handler.Calls);
    }
}
