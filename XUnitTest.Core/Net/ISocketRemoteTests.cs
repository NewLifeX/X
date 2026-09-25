using NewLife;
using NewLife.Data;
using NewLife.Log;
using NewLife.Messaging;
using NewLife.Net;
using NewLife.Security;
using NewLife.Serialization;
using Xunit;

namespace XUnitTest.Net;

[Collection("Net")]
public class ISocketRemoteTests
{
    [Fact(DisplayName = "发送流_短读流_不截断且完整到达")]
    public void SendStream_ShortRead_NoTruncate()
    {
        // 数据源每次最多返回 997 字节（模拟网络流/压缩流的短读行为），旧实现会把短读误判为流结束而截断
        var payload = Rand.NextBytes(100_000);
        using var src = new ShortReadStream(payload, 997);

        using var svr = new NetServer { Port = 0 };
        var received = new MemoryStream();
        var total = 0L;
        var wait = new ManualResetEventSlim();
        svr.Received += (s, e) =>
        {
            e.Packet?.CopyTo(received);
            var n = Interlocked.Add(ref total, e.Packet?.Total ?? 0);
            if (n >= payload.Length) wait.Set();
        };
        svr.Start();
        WaitForServerReady(svr);

        var uri = new NetUri($"tcp://127.0.0.1:{svr.Port}");
        var client = uri.CreateRemote();
        client.Open();
        try
        {
            var sent = client.Send(src);
            Assert.Equal(payload.Length, sent);

            Assert.True(wait.Wait(5_000), "未在超时内收到全部数据");
            Assert.Equal(payload.Length, received.Length);
            Assert.Equal(payload, received.ToArray());
        }
        finally
        {
            client.Close("test");
        }
    }

    /// <summary>短读流：每次读取最多返回指定字节数，模拟网络流等短读行为</summary>
    private sealed class ShortReadStream(Byte[] data, Int32 maxChunk) : Stream
    {
        private Int32 _offset;

        public override Boolean CanRead => true;
        public override Boolean CanSeek => false;
        public override Boolean CanWrite => false;
        public override Int64 Length => data.Length;
        public override Int64 Position { get => _offset; set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override Int32 Read(Byte[] buffer, Int32 offset, Int32 count)
        {
            var n = Math.Min(Math.Min(count, maxChunk), data.Length - _offset);
            if (n <= 0) return 0;

            Array.Copy(data, _offset, buffer, offset, n);
            _offset += n;

            return n;
        }
        public override Int64 Seek(Int64 offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(Int64 value) => throw new NotSupportedException();
        public override void Write(Byte[] buffer, Int32 offset, Int32 count) => throw new NotSupportedException();
    }


    /// <summary>等待服务器就绪</summary>
    private static void WaitForServerReady(NetServer server)
    {
        var timeout = DateTime.Now.AddSeconds(10);
        while (DateTime.Now < timeout)
        {
            if (server.Active && server.Port > 0) break;
            Thread.Sleep(50);
        }
        // 额外等待一点时间确保服务器完全就绪
        Thread.Sleep(100);
    }

}
