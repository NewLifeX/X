using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using NewLife.Data;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

/// <summary>发送出口测试：直发（无发送队列）——同步写完才返回，多入口共用一把写锁不交错</summary>
/// <remarks>
/// <para>架构：所有发送入口（Send/SendAsync/SendAsync(Stream)/SendFileAsync）共用一把写锁，任一时刻只有一位写者。</para>
/// <para>观测手段：对端不读且收缩收发缓冲时，直发会堵在内核写阻塞上（同步 Send 不返回）；
/// 对端开始读后数据完整到达。并发用例用“块内字节一致”检出交错。</para>
/// </remarks>
[Collection("Net")]
public class SendEntryTests
{
    #region 工具
    /// <summary>指定时间内是否已完成</summary>
    private static async Task<Boolean> CompletedWithinAsync(Task task, Int32 timeoutMs)
    {
        var finished = await Task.WhenAny(task, Task.Delay(timeoutMs));
        return ReferenceEquals(finished, task);
    }

    private static async Task WaitUntilAsync(Func<Boolean> condition, Int32 timeoutMs = 10_000)
    {
        var sw = Stopwatch.StartNew();
        while (!condition())
        {
            if (sw.ElapsedMilliseconds > timeoutMs) throw new TimeoutException("等待条件超时");

            await Task.Delay(10);
        }
    }
    #endregion

    [Fact]
    [DisplayName("直发_Send 同步写出_慢对端阻塞_读后续达")]
    public async Task Send_SyncWriteBlocksOnSlowPeer()
    {
        const Int32 chunkSize = 64 * 1024;
        const Int32 chunkCount = 64;                        // 合计 4MB，超过内核收发缓冲
        var body = new Byte[chunkSize];
        body.AsSpan().Fill(0x5A);

        // 裸监听套接字：接受连接但不读取；收缩收发缓冲，让直发必然堵在内核写阻塞上
        var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        listener.ReceiveBufferSize = 8 * 1024;
        listener.Listen(1);
        var port = ((IPEndPoint)listener.LocalEndPoint!).Port;

        using var client = new TcpSession { Remote = new NetUri($"tcp://127.0.0.1:{port}") };
        try
        {
            client.Open();
            client.Client!.SendBufferSize = 16 * 1024;

            using var peer = listener.Accept();
            peer.ReceiveBufferSize = 8 * 1024;
            peer.ReceiveTimeout = 30_000;

            // 直发：对端不读时同步 Send 必然阻塞（内存由内核缓冲天然限住，不需要发送队列）
            var sending = Task.Factory.StartNew(() =>
            {
                var total = 0;
                for (var i = 0; i < chunkCount; i++)
                {
                    var rs = client.Send(body, 0, chunkSize);
                    if (rs <= 0) break;

                    total += rs;
                }

                return total;
            }, TaskCreationOptions.LongRunning);
            Assert.False(await CompletedWithinAsync(sending, 300), "前置条件不成立：直发应被内核缓冲阻塞");

            // 对端开始读：直发跑到写完，数据完整到达
            var received = new MemoryStream();
            var reading = Task.Factory.StartNew(() =>
            {
                var buf = new Byte[64 * 1024];
                while (received.Length < chunkSize * chunkCount)
                {
                    var n = peer.Receive(buf);
                    if (n <= 0) break;

                    received.Write(buf, 0, n);
                }
            }, TaskCreationOptions.LongRunning);

            Assert.Equal(chunkSize * chunkCount, await sending.WaitAsync(TimeSpan.FromSeconds(30)));
            await reading.WaitAsync(TimeSpan.FromSeconds(30));

            var bytes = received.ToArray();
            Assert.Equal(chunkSize * chunkCount, bytes.Length);
            Assert.DoesNotContain(bytes, b => b != 0x5A);
        }
        finally
        {
            listener.Dispose();
        }
    }

    [Fact]
    [DisplayName("直发_并发写入_各块不交错（写锁契约）")]
    public async Task Send_ConcurrentWriters_NoInterleave()
    {
        const Int32 writers = 8;
        const Int32 blocks = 200;
        const Int32 blockSize = 512;

        var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        listener.Listen(1);
        var port = ((IPEndPoint)listener.LocalEndPoint!).Port;

        using var client = new TcpSession { Remote = new NetUri($"tcp://127.0.0.1:{port}") };
        try
        {
            client.Open();

            using var peer = listener.Accept();
            peer.ReceiveTimeout = 30_000;

            var total = writers * blocks * blockSize;
            var received = new MemoryStream();
            var reading = Task.Factory.StartNew(() =>
            {
                var buf = new Byte[64 * 1024];
                while (received.Length < total)
                {
                    var n = peer.Receive(buf);
                    if (n <= 0) break;

                    received.Write(buf, 0, n);
                }
            }, TaskCreationOptions.LongRunning);

            // 多线程同时发送：写锁保证任一时刻只有一位写者，所以每个 512 字节块内部必然同字节
            var tasks = new Task[writers];
            for (var w = 0; w < writers; w++)
            {
                var id = (Byte)(w + 1);
                var block = new Byte[blockSize];
                block.AsSpan().Fill(id);

                tasks[w] = Task.Factory.StartNew(() =>
                {
                    for (var i = 0; i < blocks; i++) client.Send(block, 0, blockSize);
                }, TaskCreationOptions.LongRunning);
            }

            await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(30));
            await reading.WaitAsync(TimeSpan.FromSeconds(30));

            var bytes = received.ToArray();
            Assert.Equal(total, bytes.Length);

            // 交错会把别的写者的字节混进块内（块内出现两种字节）
            for (var i = 0; i < total; i += blockSize)
            {
                var first = bytes[i];
                for (var j = 1; j < blockSize; j++)
                {
                    Assert.Equal(first, bytes[i + j]);
                }
            }
        }
        finally
        {
            listener.Dispose();
        }
    }

    [Fact]
    [DisplayName("直发_Send与SendAsync保序送达")]
    public async Task SendAndSendAsync_DeliverInOrder()
    {
        using var server = new NetServer { Port = 0 };
        server.Start();

        var wait = new ManualResetEventSlim();
        var received = new List<Byte>();
        server.NewSession += (s, e) =>
        {
            e.Session.Session.Received += (ss, ee) =>
            {
                var pk = ee.Packet;
                if (pk != null) lock (received) received.AddRange(pk.GetSpan().ToArray());
            };
            wait.Set();
        };

        using var client = new TcpSession { Remote = new NetUri($"tcp://127.0.0.1:{server.Port}") };
        client.Open();

        // 会话建立且订阅就绪后再发送，避免首轮数据早于订阅
        Assert.True(wait.Wait(3_000));

        // 同步直发与异步直发交替，共用写锁 → 前缀严格保序
        Assert.Equal(4, client.Send(new Byte[] { 1, 2, 3, 4 }));
        Assert.Equal(3, await client.SendAsync(new ArrayPacket(new Byte[] { 5, 6, 7 })));
        Assert.Equal(2, client.Send(new Byte[] { 8, 9 }));

        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < TimeSpan.FromSeconds(5))
        {
            lock (received) { if (received.Count >= 9) break; }
            await Task.Delay(10);
        }

        lock (received) Assert.Equal(new Byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, received);
    }

    [Fact]
    [DisplayName("文件发送_非SSL_内核零拷贝_内容逐字节一致")]
    public async Task SendFile_ZeroCopy_ContentMatches()
    {
        const Int32 size = 300 * 1024;
        var payload = new Byte[size];
        Random.Shared.NextBytes(payload);

        var file = Path.Combine(Path.GetTempPath(), "nl_sendfile_" + Guid.NewGuid().ToString("N") + ".bin");
        await File.WriteAllBytesAsync(file, payload);

        var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        listener.Listen(1);
        var port = ((IPEndPoint)listener.LocalEndPoint!).Port;

        using var client = new TcpSession { Remote = new NetUri($"tcp://127.0.0.1:{port}") };
        try
        {
            client.Open();

            using var peer = listener.Accept();
            peer.ReceiveTimeout = 30_000;

            var received = new MemoryStream();
            var reading = Task.Factory.StartNew(() =>
            {
                var buf = new Byte[64 * 1024];
                while (received.Length < size)
                {
                    var n = peer.Receive(buf);
                    if (n <= 0) break;

                    received.Write(buf, 0, n);
                }
            }, TaskCreationOptions.LongRunning);

            // 非 SSL会话：整个文件交给内核推送（sendfile/TransmitFile），应用层不经读块
            var rs = await client.SendFileAsync(file);
            Assert.Equal(size, rs);

            await reading.WaitAsync(TimeSpan.FromSeconds(30));
            Assert.Equal(payload, received.ToArray());
        }
        finally
        {
            listener.Dispose();
            File.Delete(file);
        }
    }

    [Fact]
    [DisplayName("发送失败_错误回调内再次 Send_不自死锁")]
    public async Task SendFailed_ErrorCallbackSendsAgain_NoDeadlock()
    {
        var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        listener.Listen(1);
        var port = ((IPEndPoint)listener.LocalEndPoint!).Port;

        using var client = new TcpSession { Remote = new NetUri($"tcp://127.0.0.1:{port}") };
        try
        {
            client.Open();

            using var peer = listener.Accept();
            peer.ReceiveTimeout = 30_000;

            // 确定性制造发送失败：关掉发送方向后任何 Send 都立刻失败，
            // 而套接字仍处已绑定（Open 仍为真）、接收方向不受影响，不会顺带把会话关掉
            client.Client!.Shutdown(SocketShutdown.Send);

            // 错误回调内同步再次 Send：上报若发生在写锁内，这里会二次等待同一把 SemaphoreSlim 而自死锁
            var innerDone = new ManualResetEventSlim();
            var fired = 0;
            client.Error += (s, e) =>
            {
                // 只在内层发一次，避免“失败 → 上报 → 再失败”的无限递归
                if (Interlocked.Increment(ref fired) > 1) return;

                client.Send(new Byte[] { 9, 9 });
                innerDone.Set();
            };

            var rs = 0;
            Exception? error = null;
            var sending = Task.Factory.StartNew(() =>
            {
                try { rs = client.SendAsync(new ArrayPacket(new Byte[64])).AsTask().GetAwaiter().GetResult(); }
                catch (Exception ex) { error = ex; }
            }, TaskCreationOptions.LongRunning);

            Assert.True(await CompletedWithinAsync(sending, 10_000), "发送失败时，错误回调内再次 Send 发生了自死锁");
            Assert.True(innerDone.IsSet, $"发送失败没有触发错误回调（rs={rs}, error={error?.GetType().Name}: {error?.Message}）");
        }
        finally
        {
            listener.Dispose();
        }
    }
}
