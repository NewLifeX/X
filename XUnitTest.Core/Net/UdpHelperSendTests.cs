using System.Net;
using System.Net.Sockets;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

/// <summary>UDP 发送数据流不因短读截断</summary>
public class UdpHelperSendTests
{
    /// <summary>每次只返回固定少量字节的流，模拟网络流/压缩流的短读</summary>
    private sealed class ChunkedStream : Stream
    {
        private readonly Byte[] _data;
        private readonly Int32 _chunk;
        private Int32 _pos;

        public ChunkedStream(Byte[] data, Int32 chunk)
        {
            _data = data;
            _chunk = chunk;
        }

        public override Boolean CanRead => true;
        public override Boolean CanSeek => false;
        public override Boolean CanWrite => false;
        public override Int64 Length => _data.Length;
        public override Int64 Position { get => _pos; set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override Int64 Seek(Int64 offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(Int64 value) => throw new NotSupportedException();
        public override void Write(Byte[] buffer, Int32 offset, Int32 count) => throw new NotSupportedException();

        public override Int32 Read(Byte[] buffer, Int32 offset, Int32 count)
        {
            var n = Math.Min(Math.Min(_chunk, count), _data.Length - _pos);
            if (n <= 0) return 0;

            Array.Copy(_data, _pos, buffer, offset, n);
            _pos += n;

            return n;
        }
    }

    [Fact(DisplayName = "短读流的数据必须全部发出")]
    public void SendChunkedStreamSendsAll()
    {
        var data = new Byte[5000];
        new Random(1234).NextBytes(data);

        using var server = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        var port = ((IPEndPoint)server.Client.LocalEndPoint!).Port;
        using var client = new UdpClient();
        var remote = new IPEndPoint(IPAddress.Loopback, port);

        using (var stream = new ChunkedStream(data, 100))
        {
            client.Send(stream, remote);
        }

        // 收齐所有分片，旧实现只发出第一片（100 字节）
        var total = 0;
        var buffer = new Byte[2048];
        var deadline = DateTime.Now.AddSeconds(10);
        while (total < data.Length && DateTime.Now < deadline)
        {
            if (!server.Client.Poll(1_000_000, SelectMode.SelectRead)) continue;

            if (server.Client.Available <= 0) continue;
            var n = server.Client.Receive(buffer);
            if (n <= 0) break;

            total += n;
        }

        Assert.Equal(data.Length, total);
    }
}
