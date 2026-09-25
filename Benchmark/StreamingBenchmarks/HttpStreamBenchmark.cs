using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using BenchmarkDotNet.Attributes;
using NewLife;
using NewLife.Http;

namespace Benchmark.StreamingBenchmarks;

/// <summary>HTTP 流式响应基准：BodyStream 流式发送 vs 整段物化响应（回环 TCP，直接读取到 Content-Length）</summary>
/// <remarks>
/// 命令：dotnet run --project Benchmark/Benchmark.csproj -c Release -- --filter "*HttpStreamBenchmark*"
/// 服务端为内置 HttpServer；客户端为裸 TcpClient（避免 HttpClient 自身开销干扰）。
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 2, iterationCount: 5)]
public class HttpStreamBenchmark : IDisposable
{
    private HttpServer _server = null!;
    private Byte[] _payload = null!;
    private Byte[] _buf = null!;

    /// <summary>响应体大小（字节）</summary>
    [Params(1024 * 1024, 8 * 1024 * 1024)]
    public Int32 Size { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _payload = new Byte[Size];
        Random.Shared.NextBytes(_payload);
        _buf = new Byte[64 * 1024];

        _server = new HttpServer { Port = 0 };
        _server.Map("/stream", new StreamBodyHandler { Payload = _payload });
        _server.Map("/buffer", new BufferBodyHandler { Payload = _payload });
        _server.Start();
    }

    [GlobalCleanup]
    public void Cleanup() => _server?.Dispose();

    /// <summary>清理</summary>
    public void Dispose() => Cleanup();

    /// <summary>流式响应：BodyStream 分块发送（大文件不物化）</summary>
    [Benchmark]
    public Int64 StreamBody()
    {
        var total = Get("/stream");

        return total;
    }

    /// <summary>整段响应：Body 物化后一次 Build 发送</summary>
    [Benchmark]
    public Int64 BufferedBody()
    {
        var total = Get("/buffer");

        return total;
    }

    /// <summary>发起 GET 并读取完整响应，返回收到字节数</summary>
    private Int64 Get(String path)
    {
        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, _server.Port);

        using var ns = client.GetStream();
        ns.ReadTimeout = 30_000;

        var req = $"GET {path} HTTP/1.1\r\nHost: 127.0.0.1\r\nConnection: close\r\n\r\n".GetBytes();
        ns.Write(req, 0, req.Length);

        var received = 0L;
        var buf = _buf;
        Int32 n;
        while ((n = ns.Read(buf, 0, buf.Length)) > 0)
        {
            received += n;
        }

        return received;
    }

    /// <summary>流式响应处理器（BodyStream）</summary>
    class StreamBodyHandler : IHttpHandler
    {
        public Byte[] Payload { get; set; } = [];

        public void ProcessRequest(IHttpContext context)
        {
            context.Response.ContentType = "application/octet-stream";
            context.Response.BodyStream = new MemoryStream(Payload);
        }
    }

    /// <summary>整段响应处理器（Body 物化）</summary>
    class BufferBodyHandler : IHttpHandler
    {
        public Byte[] Payload { get; set; } = [];

        public void ProcessRequest(IHttpContext context)
        {
            context.Response.SetResult(Payload);
        }
    }
}
