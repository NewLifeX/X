using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using NewLife;
using NewLife.Http;
using Xunit;

namespace XUnitTest.Http;

/// <summary>HttpDownloadOptions 与分阶段下载测试</summary>
public class HttpDownloadOptionsTests
{
    [Fact]
    [DisplayName("下载选项默认值")]
    public void Options_DefaultValues()
    {
        var options = new HttpDownloadOptions();

        Assert.Equal(TimeSpan.FromSeconds(15), options.HeaderTimeout);
        Assert.Equal(TimeSpan.FromSeconds(300), options.DownloadTimeout);
        Assert.Null(options.Progress);
        Assert.Null(options.ExpectedHash);
        Assert.Equal(0, options.BufferSize);
        Assert.True(options.UseContentDispositionFileName);
    }

    [Fact]
    [DisplayName("下载_进度回调按缓冲区多次触发")]
    public async Task Download_ProgressAndBuffer()
    {
        var data = new Byte[256 * 1024];
        new Random(12345).NextBytes(data);
        var file = NewTempFile("progress.bin");
        try
        {
            var reports = new List<(Int64 Received, Int64 Total)>();
            var options = new HttpDownloadOptions
            {
                HeaderTimeout = TimeSpan.FromSeconds(2),
                DownloadTimeout = TimeSpan.FromSeconds(10),
                BufferSize = 1024,
                Progress = (received, total) => reports.Add((received, total)),
            };

            var rs = await CreateClient((_, _) => Task.FromResult(CreateResponse(data))).DownloadFileAsync("http://x/file.bin", file, options);

            Assert.Equal(file.GetFullPath(), rs);
            Assert.Equal(data, File.ReadAllBytes(rs));
            Assert.NotEmpty(reports);
            Assert.True(reports.Count > 1);
            Assert.Equal(data.Length, reports[^1].Received);
            Assert.All(reports, item => Assert.Equal(data.Length, item.Total));
            for (var i = 1; i < reports.Count; i++)
            {
                Assert.True(reports[i].Received > reports[i - 1].Received);
            }
        }
        finally
        {
            DeleteFile(file);
        }
    }

    [Fact]
    [DisplayName("下载_使用Content-Disposition文件名")]
    public async Task Download_UsesContentDispositionFileName()
    {
        var data = "content-disposition"u8.ToArray();
        var file = NewTempFile("original.bin");
        try
        {
            var options = new HttpDownloadOptions
            {
                HeaderTimeout = TimeSpan.FromSeconds(2),
                DownloadTimeout = TimeSpan.FromSeconds(10),
            };

            var rs = await CreateClient((_, _) => Task.FromResult(CreateResponse(data, "attachment; filename=\"actual.bin\"")))
                .DownloadFileAsync("http://x/file.bin", file, options);

            Assert.Equal("actual.bin", Path.GetFileName(rs));
            Assert.Equal(data, File.ReadAllBytes(rs));
            Assert.False(File.Exists(file));
        }
        finally
        {
            DeleteFile(file);
            DeleteFile(Path.Combine(Path.GetDirectoryName(file)!, "actual.bin"));
        }
    }

    [Fact]
    [DisplayName("下载_关闭Content-Disposition文件名")]
    public async Task Download_IgnoresContentDispositionFileName()
    {
        var data = "keep-name"u8.ToArray();
        var file = NewTempFile("original.bin");
        try
        {
            var options = new HttpDownloadOptions
            {
                HeaderTimeout = TimeSpan.FromSeconds(2),
                DownloadTimeout = TimeSpan.FromSeconds(10),
                UseContentDispositionFileName = false,
            };

            var rs = await CreateClient((_, _) => Task.FromResult(CreateResponse(data, "attachment; filename=\"actual.bin\"")))
                .DownloadFileAsync("http://x/file.bin", file, options);

            Assert.Equal(file.GetFullPath(), rs);
            Assert.Equal(data, File.ReadAllBytes(rs));
        }
        finally
        {
            DeleteFile(file);
        }
    }

    [Fact]
    [DisplayName("下载_响应头超时快速失败")]
    public async Task Download_HeaderTimeout()
    {
        var file = NewTempFile("header-timeout.bin");
        try
        {
            var options = new HttpDownloadOptions
            {
                HeaderTimeout = TimeSpan.FromMilliseconds(100),
                DownloadTimeout = TimeSpan.FromSeconds(10),
            };

            var client = CreateClient(async (_, cancellationToken) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                return CreateResponse([1, 2, 3]);
            });

            await Assert.ThrowsAsync<TimeoutException>(() => client.DownloadFileAsync("http://x/file.bin", file, options));

            Assert.False(File.Exists(file));
        }
        finally
        {
            DeleteFile(file);
        }
    }

    [Fact]
    [DisplayName("下载_响应体超时快速失败")]
    public async Task Download_DownloadTimeout()
    {
        var data = new Byte[1024 * 1024];
        var file = NewTempFile("body-timeout.bin");
        try
        {
            var options = new HttpDownloadOptions
            {
                HeaderTimeout = TimeSpan.FromSeconds(2),
                DownloadTimeout = TimeSpan.FromMilliseconds(200),
            };

            var client = CreateClient((_, _) =>
            {
                var content = new StreamContent(new SlowStream(data, TimeSpan.Zero, TimeSpan.FromSeconds(5)));
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
            });

            await Assert.ThrowsAsync<TimeoutException>(() => client.DownloadFileAsync("http://x/file.bin", file, options));

            Assert.False(File.Exists(file));
        }
        finally
        {
            DeleteFile(file);
        }
    }

    [Fact]
    [DisplayName("下载_响应头与响应体超时互不影响")]
    public async Task Download_PhaseTimeoutSeparated()
    {
        var data = new Byte[2048];
        new Random(2468).NextBytes(data);
        var file = NewTempFile("phase.bin");
        try
        {
            var options = new HttpDownloadOptions
            {
                HeaderTimeout = TimeSpan.FromSeconds(1),
                DownloadTimeout = TimeSpan.FromSeconds(5),
            };

            var client = CreateClient(async (_, cancellationToken) =>
            {
                await Task.Delay(400, cancellationToken);
                var content = new StreamContent(new SlowStream(data, TimeSpan.Zero, TimeSpan.FromMilliseconds(1500)));
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
            });

            var rs = await client.DownloadFileAsync("http://x/file.bin", file, options);

            Assert.Equal(data, File.ReadAllBytes(rs));
        }
        finally
        {
            DeleteFile(file);
        }
    }

    [Fact]
    [DisplayName("下载_调用方取消不重试")]
    public async Task Download_CallerCancellation()
    {
        var file = NewTempFile("cancel.bin");
        try
        {
            var options = new HttpDownloadOptions
            {
                HeaderTimeout = TimeSpan.FromSeconds(10),
                DownloadTimeout = TimeSpan.FromSeconds(10),
            };

            var client = CreateClient(async (_, cancellationToken) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                return CreateResponse([1, 2, 3]);
            });
            using var cts = new CancellationTokenSource(200);

            var ex = await Record.ExceptionAsync(() => client.DownloadFileAsync("http://x/file.bin", file, options, cts.Token));

            Assert.IsAssignableFrom<OperationCanceledException>(ex);
            Assert.False(File.Exists(file));
        }
        finally
        {
            DeleteFile(file);
        }
    }

    [Fact]
    [DisplayName("下载_Content-Length不完整时失败")]
    public async Task Download_IncompleteContentLength()
    {
        var data = new Byte[1024];
        var file = NewTempFile("incomplete.bin");
        try
        {
            var options = new HttpDownloadOptions
            {
                HeaderTimeout = TimeSpan.FromSeconds(2),
                DownloadTimeout = TimeSpan.FromSeconds(10),
            };

            var client = CreateClient((_, _) =>
            {
                var content = new ByteArrayContent(data);
                content.Headers.ContentLength = data.Length + 10;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
            });

            await Assert.ThrowsAsync<IOException>(() => client.DownloadFileAsync("http://x/file.bin", file, options));

            Assert.False(File.Exists(file));
        }
        finally
        {
            DeleteFile(file);
        }
    }

    [Fact]
    [DisplayName("下载_哈希校验成功")]
    public async Task Download_HashSuccess()
    {
        var data = "hash-ok"u8.ToArray();
        var file = NewTempFile("hash-ok.bin");
        try
        {
            var options = new HttpDownloadOptions
            {
                HeaderTimeout = TimeSpan.FromSeconds(2),
                DownloadTimeout = TimeSpan.FromSeconds(10),
                ExpectedHash = "md5$" + data.MD5().ToHex(),
            };

            var rs = await CreateClient((_, _) => Task.FromResult(CreateResponse(data))).DownloadFileAsync("http://x/file.bin", file, options);

            Assert.Equal(data, File.ReadAllBytes(rs));
            Assert.False(File.Exists(file + ".tmp"));
        }
        finally
        {
            DeleteFile(file);
        }
    }

    [Fact]
    [DisplayName("下载_哈希校验失败")]
    public async Task Download_HashFailed()
    {
        var data = "hash-fail"u8.ToArray();
        var file = NewTempFile("hash-fail.bin");
        try
        {
            var options = new HttpDownloadOptions
            {
                HeaderTimeout = TimeSpan.FromSeconds(2),
                DownloadTimeout = TimeSpan.FromSeconds(10),
                ExpectedHash = "md5$" + new String('0', 32),
            };

            await Assert.ThrowsAsync<IOException>(() => CreateClient((_, _) => Task.FromResult(CreateResponse(data)))
                .DownloadFileAsync("http://x/file.bin", file, options));

            Assert.False(File.Exists(file));
            Assert.False(File.Exists(file + ".tmp"));
        }
        finally
        {
            DeleteFile(file);
        }
    }

    private static HttpClient CreateClient(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        => new(new DelegateHandler(handler));

    private static HttpResponseMessage CreateResponse(Byte[] data, String? contentDisposition = null)
    {
        var content = new ByteArrayContent(data);
        if (!contentDisposition.IsNullOrEmpty())
            content.Headers.ContentDisposition = ContentDispositionHeaderValue.Parse(contentDisposition);

        return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
    }

    private static String NewTempFile(String name)
    {
        var dir = Path.Combine(Path.GetTempPath(), "HttpDownloadTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, name);
    }

    private static void DeleteFile(String file)
    {
        try
        {
            var dir = Path.GetDirectoryName(file);
            if (!dir.IsNullOrEmpty() && Directory.Exists(dir)) Directory.Delete(dir, true);
        }
        catch { }
    }

    private sealed class DelegateHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => handler(request, cancellationToken);
    }

    /// <summary>首次读取立即返回一小段，后续读取延迟，用于模拟慢速响应体</summary>
    private sealed class SlowStream(Byte[] data, TimeSpan firstDelay, TimeSpan nextDelay) : Stream
    {
        private Int32 _position;
        private Boolean _firstRead = true;

        public override Boolean CanRead => true;

        public override Boolean CanSeek => false;

        public override Boolean CanWrite => false;

        public override Int64 Length => data.Length;

        public override Int64 Position { get => _position; set => throw new NotSupportedException(); }

        public override void Flush() { }

        public override Int32 Read(Byte[] buffer, Int32 offset, Int32 count) => throw new NotSupportedException();

        public override async Task<Int32> ReadAsync(Byte[] buffer, Int32 offset, Int32 count, CancellationToken cancellationToken)
        {
            if (_firstRead)
            {
                _firstRead = false;
                if (firstDelay > TimeSpan.Zero) await Task.Delay(firstDelay, cancellationToken).ConfigureAwait(false);

                var n = Math.Min(Math.Min(count, 1024), data.Length - _position);
                Array.Copy(data, _position, buffer, offset, n);
                _position += n;

                return n;
            }

            if (_position >= data.Length) return 0;

            if (nextDelay > TimeSpan.Zero) await Task.Delay(nextDelay, cancellationToken).ConfigureAwait(false);

            var m = Math.Min(count, data.Length - _position);
            Array.Copy(data, _position, buffer, offset, m);
            _position += m;

            return m;
        }

        public override Int64 Seek(Int64 offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(Int64 value) => throw new NotSupportedException();

        public override void Write(Byte[] buffer, Int32 offset, Int32 count) => throw new NotSupportedException();
    }
}
