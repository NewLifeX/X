using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Text;
using NewLife.Data;
using NewLife.Http;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Http;

/// <summary>分块响应体在「非发送队列出口」下的句柄归还测试</summary>
/// <remarks>
/// <para>分块响应体默认走发送队列出口（承载会话是活动 TcpSession 时），句柄所有权随入队转移给管道。</para>
/// <para>其余情形退化为借用语义的直发：句柄不转移，必须由本层归还，否则每块泄漏一个池缓冲。UDP 承载的 HttpSession 即走此路（UdpSession 不是 TcpSession）。</para>
/// </remarks>
[Collection("Net")]
public class HttpChunkedSendTests
{
    #region 辅助
    /// <summary>不可寻址数据流：长度未知，迫使响应体走分块传输</summary>
    private sealed class NonSeekableStream : Stream
    {
        private readonly Byte[] _data;
        private Int32 _pos;

        /// <summary>创建不可寻址数据流</summary>
        /// <param name="data">数据内容</param>
        public NonSeekableStream(Byte[] data) => _data = data;

        /// <summary>是否可读</summary>
        public override Boolean CanRead => true;

        /// <summary>是否可寻址</summary>
        public override Boolean CanSeek => false;

        /// <summary>是否可写</summary>
        public override Boolean CanWrite => false;

        /// <summary>长度。不可寻址，取长度即不支持</summary>
        public override Int64 Length => throw new NotSupportedException();

        /// <summary>当前位置。只读推进，不支持设置</summary>
        public override Int64 Position
        {
            get => _pos;
            set => throw new NotSupportedException();
        }

        /// <summary>无缓冲，空实现</summary>
        public override void Flush() { }

        /// <summary>读取数据</summary>
        /// <param name="buffer">目标缓冲</param>
        /// <param name="offset">起始偏移</param>
        /// <param name="count">最大读取量</param>
        /// <returns>实际读取字节数</returns>
        public override Int32 Read(Byte[] buffer, Int32 offset, Int32 count)
        {
            var n = Math.Min(count, _data.Length - _pos);
            if (n <= 0) return 0;

            Array.Copy(_data, _pos, buffer, offset, n);
            _pos += n;

            return n;
        }

        /// <summary>不可寻址</summary>
        public override Int64 Seek(Int64 offset, SeekOrigin origin) => throw new NotSupportedException();

        /// <summary>只读</summary>
        public override void SetLength(Int64 value) => throw new NotSupportedException();

        /// <summary>只读</summary>
        public override void Write(Byte[] buffer, Int32 offset, Int32 count) => throw new NotSupportedException();
    }

    /// <summary>回分块响应体的处理器</summary>
    private sealed class ChunkedHandler : HttpSession
    {
        /// <summary>响应体长度。取 64K 读块的整数倍之外，确保分出多块</summary>
        public Int32 BodySize { get; set; } = 200 * 1024;

        /// <summary>处理请求，返回长度未知的流式响应体</summary>
        /// <param name="request">请求</param>
        /// <param name="data">数据帧</param>
        /// <returns>响应</returns>
        protected override HttpResponse ProcessRequest(HttpRequest request, IData data)
            => new() { BodyStream = new NonSeekableStream(new Byte[BodySize]) };
    }

    /// <summary>捕获直发句柄的网络会话。只记录不实际发送，便于断言句柄是否归还</summary>
    private sealed class CapturingSession : NetSession
    {
        /// <summary>经 Send(IPacket) 直发（借用语义）的句柄</summary>
        public readonly List<IPacket> Packets = [];

        /// <summary>重写发送，记录句柄后不再下发</summary>
        /// <param name="data">要发送的数据包</param>
        /// <returns>当前会话实例</returns>
        public override INetSession Send(IPacket data)
        {
            lock (Packets) Packets.Add(data);

            return this;
        }

        /// <summary>已捕获句柄数</summary>
        public Int32 Count { get { lock (Packets) return Packets.Count; } }

        /// <summary>取已捕获句柄的快照</summary>
        /// <returns>句柄数组</returns>
        public IPacket[] Snapshot() { lock (Packets) return [.. Packets]; }
    }

    /// <summary>以 UDP 承载 Http 的服务端。UDP 承载会话不是 TcpSession，故分块响应体走借用直发</summary>
    private sealed class UdpHttpServer : HttpServer
    {
        /// <summary>本服务端创建并捕获的会话</summary>
        public volatile CapturingSession? Captured;

        /// <summary>创建会话，换用可捕获句柄的会话类型</summary>
        /// <param name="session">底层Socket会话</param>
        /// <returns>创建的网络会话实例</returns>
        protected override INetSession CreateSession(ISocketSession session)
        {
            // Host 只作为 INetSession 成员暴露，须按接口赋值
            var ns = new CapturingSession { Server = session.Server, Session = session };
            ((INetSession)ns).Host = this;
            Captured = ns;

            return ns;
        }

        /// <summary>创建处理器，返回分块响应处理器</summary>
        /// <param name="session">网络会话</param>
        /// <returns>网络处理器</returns>
        public override INetHandler? CreateHandler(INetSession session) => new ChunkedHandler();
    }
    #endregion

    [Fact]
    [DisplayName("HTTP_分块响应体_非发送队列出口_每块句柄已归还")]
    public async Task ChunkedBody_DirectSend_ReleasesChunkHandles()
    {
        using var server = new UdpHttpServer { Port = 0, ProtocolType = NetType.Udp };
        server.Start();

        var request = Encoding.ASCII.GetBytes("GET /chunked HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n");
        using (var udp = new System.Net.Sockets.UdpClient())
        {
            udp.Send(request, request.Length, new IPEndPoint(IPAddress.Loopback, server.Port));
        }

        var sw = Stopwatch.StartNew();

        // 等 UDP 数据报被服务端处理并建出会话（接收在服务端线程上异步进行）
        while (server.Captured == null)
        {
            Assert.True(sw.ElapsedMilliseconds < 10_000, $"等待 UDP 会话创建超时。Port={server.Port}；Servers={server.Servers.Count}");
            await Task.Delay(20);
        }

        var session = server.Captured!;

        // 分块直发是同步的，句柄按序进入捕获列表；连续两次采样不再增长即视为整条响应已写完
        Int32 last;
        do
        {
            last = session.Count;
            await Task.Delay(20);

            Assert.True(sw.ElapsedMilliseconds < 20_000, "等待分块响应写出超时");
        } while (session.Count != last);

        // 响应头 1 个 + 分块若干个（每块由 BuildChunk 借出池化句柄）
        var owners = session.Snapshot().OfType<OwnerPacket>().ToList();
        Assert.True(owners.Count >= 2, $"应捕获到响应头与分块句柄，实际只有 {owners.Count} 个");

        // 直发为借用语义：句柄不转移，本层必须归还（引用计数归零即已归还池缓冲）
        Assert.All(owners, pk => Assert.Equal(0, pk.RefCount));
    }
}
