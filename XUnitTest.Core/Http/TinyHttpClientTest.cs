using System.Net.Sockets;
using NewLife;
using NewLife.Data;
using NewLife.Http;
using NewLife.Log;
using Xunit;

namespace XUnitTest.Http;

public class TinyHttpClientTest
{
    private readonly TinyHttpClient _Client;

    public TinyHttpClientTest()
    {
        _Client = new TinyHttpClient();
    }

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

    //[Fact(DisplayName = "同步请求")]
    //public void SendTest()
    //{
    //    var uri = new Uri("http://newlifex.com");
    //    var client = new TinyHttpClient { Timeout = TimeSpan.FromSeconds(3), Log = XTrace.Log };
    //    var html = client.Send(uri, null)?.ToStr();

    //    Assert.True(!html.IsNullOrEmpty() && html.Length > 500);
    //    Assert.Equal(uri, client.BaseAddress);
    //}

    [Fact(DisplayName = "异步请求")]
    public async Task SendAsyncTest()
    {
        // 在 CI 环境中跳过，因为目标服务器可能不可达
        if (!IsServerReachable("newlifex.com", 80)) return;

        var uri = new Uri("http://newlifex.com");
        var req = new HttpRequest { RequestUri = uri };
        var client = new TinyHttpClient { Timeout = TimeSpan.FromSeconds(3), Log = XTrace.Log };
        var html = (await client.SendAsync(req))?.Body.ToStr();

        Assert.True(!html.IsNullOrEmpty() && html.Length > 500);
        Assert.Equal(uri, client.BaseAddress);
    }

    //[Fact(DisplayName = "同步字符串")]
    //public void GetString()
    //{
    //    var url = "http://x.newlifex.com";
    //    var client = new TinyHttpClient();
    //    var html = client.GetString(url);

    //    Assert.True(!html.IsNullOrEmpty() && html.Length > 500);
    //}

    [Fact(DisplayName = "异步字符串")]
    public async Task GetStringAsync()
    {
        // 在 CI 环境中跳过，因为目标服务器可能不可达
        if (!IsServerReachable("x.newlifex.com", 80)) return;

        var url = "http://x.newlifex.com";
        var client = new TinyHttpClient();
        var html = await client.GetStringAsync(url);

        Assert.True(!html.IsNullOrEmpty() && html.Length > 500);
    }

    [Fact(DisplayName = "https")]
    public async Task GetStringHttps()
    {
        // 在 CI 环境中跳过，因为目标服务器可能不可达
        if (!IsServerReachable("newlifex.com", 443)) return;

        var url = "https://newlifex.com";
        var client = new TinyHttpClient();
        var html = await client.GetStringAsync(url);

        Assert.True(!html.IsNullOrEmpty() && html.Length > 500);
    }

    #region 分块传输
    /// <summary>分块解析测试替身：按队列供给后续数据包，每个元素模拟一次接收</summary>
    private sealed class ChunkFeedClient : TinyHttpClient
    {
        public Queue<Byte[]> Packets { get; } = new();

        public Task<IPacket> ReadChunk(IPacket body) => ReadChunkAsync(body);

        protected override Task<IOwnerPacket> SendDataAsync(Uri? uri, IPacket? request)
        {
            var bytes = Packets.Count > 0 ? Packets.Dequeue() : null;
            if (bytes == null) return Task.FromResult<IOwnerPacket>(new OwnerPacket(0));

            var pk = new OwnerPacket(bytes.Length);
            bytes.CopyTo(pk.GetSpan());

            return Task.FromResult<IOwnerPacket>(pk);
        }
    }

    private static async Task<IPacket> ReadChunkAsync(ChunkFeedClient client, Byte[] first, params Byte[][] more)
    {
        foreach (var item in more) client.Packets.Enqueue(item);

        var pk = await client.ReadChunk(new ArrayPacket(first));

        return pk;
    }

    [Fact(DisplayName = "分块传输_长度行跨接收包_后续分块不丢失")]
    public async Task Chunk_LengthLineSplitAcrossPackets()
    {
        var client = new ChunkFeedClient();

        // 长度行 "5\r\n" 被拆到两个接收包：旧实现 ParseChunk 找不到 CRLF 即整体跳出，后续分块全部丢失（仍返回“成功”）
        var body = await ReadChunkAsync(client, "5".GetBytes(), "\r\nhello\r\n0\r\n\r\n".GetBytes());

        Assert.Equal("hello", body.ToStr());
    }

    [Fact(DisplayName = "分块传输_多分块跨包_逐块还原")]
    public async Task Chunk_MultipleChunksAcrossPackets()
    {
        var client = new ChunkFeedClient();

        var body = await ReadChunkAsync(client, "3\r\nab".GetBytes(), "c\r\n".GetBytes(), "5\r\nhello\r\n0\r\n\r\n".GetBytes());

        Assert.Equal("abchello", body.ToStr());
    }

    [Fact(DisplayName = "分块传输_分块扩展_忽略分号后内容")]
    public async Task Chunk_Extension_Ignored()
    {
        var client = new ChunkFeedClient();

        // 1ba;ext=1 这类分块扩展：旧实现直接 Int32.Parse 会抛 FormatException 穿透到调用方
        var body = await ReadChunkAsync(client, "3;ext=1\r\nabc\r\n0\r\n\r\n".GetBytes());

        Assert.Equal("abc", body.ToStr());
    }

    [Fact(DisplayName = "分块传输_非法长度行_抛异常")]
    public async Task Chunk_InvalidLength_Throws()
    {
        var client = new ChunkFeedClient();

        await Assert.ThrowsAsync<InvalidDataException>(() => ReadChunkAsync(client, "xyz\r\nabc\r\n0\r\n\r\n".GetBytes()));
    }

    [Fact(DisplayName = "分块传输_单块超上限_抛异常")]
    public async Task Chunk_ExceedsMaxChunkSize_Throws()
    {
        var client = new ChunkFeedClient { MaxChunkSize = 4 };

        await Assert.ThrowsAsync<InvalidDataException>(() => ReadChunkAsync(client, "5\r\nhello\r\n0\r\n\r\n".GetBytes()));
    }

    [Fact(DisplayName = "分块传输_总长超上限_抛异常")]
    public async Task Chunk_ExceedsMaxBodySize_Throws()
    {
        var client = new ChunkFeedClient { MaxBodySize = 4 };

        await Assert.ThrowsAsync<InvalidDataException>(() => ReadChunkAsync(client, "3\r\nabc\r\n3\r\ndef\r\n0\r\n\r\n".GetBytes()));
    }
    #endregion
}
