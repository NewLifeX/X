using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using NewLife.Data;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

/// <summary>发送队列出口测试：与直发并存的可选批量出口</summary>
/// <remarks>
/// <para>架构：<c>Send</c> 系列始终直发；<c>SendQueue</c> 是第二出口，入队数据由发送泵批量写出（多条合并为一次散列写）。</para>
/// <para>两条出口共用同一把写锁——只会损失消息之间的先后顺序，不会出现“块内字节交错”。</para>
/// <para>观测手段：裸监听套接字 + 收缩收发缓冲，迫使内核缓冲吃满、队列积压到水位，从而验证入队等待与自动恢复。</para>
/// </remarks>
[Collection("Net")]
public class TcpSessionSendQueueTests
{
    #region 工具
    /// <summary>指定时间内是否已完成</summary>
    private static async Task<Boolean> CompletedWithinAsync(Task task, Int32 timeoutMs)
    {
        var finished = await Task.WhenAny(task, Task.Delay(timeoutMs));
        return ReferenceEquals(finished, task);
    }

    /// <summary>创建裸监听套接字，返回端口</summary>
    private static Socket CreateListener(out Int32 port)
    {
        var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        listener.ReceiveBufferSize = 8 * 1024;
        listener.Listen(4);

        port = ((IPEndPoint)listener.LocalEndPoint!).Port;

        return listener;
    }

    /// <summary>持续读取到期望字节数</summary>
    private static Task StartReading(Socket peer, MemoryStream received, Int64 expect)
        => Task.Factory.StartNew(() =>
        {
            var buf = new Byte[64 * 1024];
            while (received.Length < expect)
            {
                var n = peer.Receive(buf);
                if (n <= 0) break;

                received.Write(buf, 0, n);
            }
        }, TaskCreationOptions.LongRunning);

    /// <summary>小水位会话：把出站暂停水位压到 8K，便于在测试里快速触发背压</summary>
    private sealed class SmallQueueSession : TcpSession
    {
        /// <summary>创建小水位发送队列</summary>
        /// <returns>发送队列</returns>
        protected override Pipe CreateSendQueue() => new()
        {
            PauseThreshold = 8 * 1024,
            ResumeThreshold = 4 * 1024,
        };
    }
    #endregion

    [Fact]
    [DisplayName("发送队列_多次入队_按序完整到达对端")]
    public async Task SendQueued_ReachesPeerInOrder()
    {
        const Int32 blockSize = 256;
        const Int32 blockCount = 200;

        var listener = CreateListener(out var port);
        using var client = new TcpSession { Remote = new NetUri($"tcp://127.0.0.1:{port}") };
        try
        {
            client.Open();
            using var peer = listener.Accept();
            peer.ReceiveTimeout = 30_000;

            var expect = (Int64)blockSize * blockCount;
            var received = new MemoryStream();
            var reading = StartReading(peer, received, expect);

            // 逐条入队（所有权转移给队列，句柄由发送泵释放）
            for (var i = 0; i < blockCount; i++)
            {
                var body = new Byte[blockSize];
                body.AsSpan().Fill((Byte)(i & 0xFF));

                Assert.True(await client.SendQueuedAsync(new ArrayPacket(body)));
            }

            await reading.WaitAsync(TimeSpan.FromSeconds(30));

            var bytes = received.ToArray();
            Assert.Equal(expect, bytes.Length);

            // 队列内部有序：每块字节一致且块序与入队序一致
            for (var i = 0; i < blockCount; i++)
            {
                for (var j = 0; j < blockSize; j++)
                {
                    Assert.Equal((Byte)(i & 0xFF), bytes[i * blockSize + j]);
                }
            }
        }
        finally
        {
            listener.Dispose();
        }
    }

    [Fact]
    [DisplayName("发送队列_积压达水位_入队等待_对端读取后自动恢复")]
    public async Task SendQueued_Backpressure_BlocksThenResumes()
    {
        const Int32 chunkSize = 4 * 1024;
        const Int32 chunkCount = 64;                    // 合计 256KB，远超收缩后的内核缓冲

        var listener = CreateListener(out var port);
        using var client = new SmallQueueSession { Remote = new NetUri($"tcp://127.0.0.1:{port}") };
        try
        {
            client.Open();
            client.Client!.SendBufferSize = 8 * 1024;

            using var peer = listener.Accept();
            peer.ReceiveBufferSize = 8 * 1024;
            peer.ReceiveTimeout = 30_000;

            // 对端不读：内核缓冲吃满后泵阻塞，队列积压到水位，入队必须等待
            var sending = Task.Factory.StartNew(async () =>
            {
                for (var i = 0; i < chunkCount; i++)
                {
                    var body = new Byte[chunkSize];
                    body.AsSpan().Fill(0x7E);

                    await client.SendQueuedAsync(new ArrayPacket(body)).ConfigureAwait(false);
                }
            }, TaskCreationOptions.LongRunning).Unwrap();

            Assert.False(await CompletedWithinAsync(sending, 500), "前置条件不成立：队列积压应让入队等待");

            // 对端开始读：泵持续写出，水位回落，入队自动恢复
            var expect = (Int64)chunkSize * chunkCount;
            var received = new MemoryStream();
            var reading = StartReading(peer, received, expect);

            await sending.WaitAsync(TimeSpan.FromSeconds(30));
            await reading.WaitAsync(TimeSpan.FromSeconds(30));

            var bytes = received.ToArray();
            Assert.Equal(expect, bytes.Length);
            Assert.DoesNotContain(bytes, b => b != 0x7E);
        }
        finally
        {
            listener.Dispose();
        }
    }

    [Fact]
    [DisplayName("发送队列_与直发混用_块内字节不交错")]
    public async Task SendQueued_MixedWithDirectSend_NoInterleave()
    {
        const Int32 writers = 4;
        const Int32 blocks = 50;
        const Int32 blockSize = 512;

        var listener = CreateListener(out var port);
        using var client = new TcpSession { Remote = new NetUri($"tcp://127.0.0.1:{port}") };
        try
        {
            client.Open();
            using var peer = listener.Accept();
            peer.ReceiveTimeout = 30_000;

            var expect = (Int64)writers * 2 * blocks * blockSize;
            var received = new MemoryStream();
            var reading = StartReading(peer, received, expect);

            var tasks = new List<Task>();
            for (var w = 0; w < writers; w++)
            {
                var queuedByte = (Byte)(0x10 + w);
                var directByte = (Byte)(0x80 + w);

                // 队列出口
                tasks.Add(Task.Factory.StartNew(async () =>
                {
                    for (var i = 0; i < blocks; i++)
                    {
                        var body = new Byte[blockSize];
                        body.AsSpan().Fill(queuedByte);

                        await client.SendQueuedAsync(new ArrayPacket(body)).ConfigureAwait(false);
                    }
                }, TaskCreationOptions.LongRunning).Unwrap());

                // 直发出口
                tasks.Add(Task.Factory.StartNew(() =>
                {
                    for (var i = 0; i < blocks; i++)
                    {
                        var body = new Byte[blockSize];
                        body.AsSpan().Fill(directByte);

                        Assert.True(client.Send(body) > 0);
                    }
                }, TaskCreationOptions.LongRunning));
            }

            await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(60));
            await reading.WaitAsync(TimeSpan.FromSeconds(60));

            var bytes = received.ToArray();
            Assert.Equal(expect, bytes.Length);

            // 两条出口可交错在块边界，但共用写锁 → 块内不得混入其它写者的字节
            for (var i = 0; i < expect; i += blockSize)
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
    [DisplayName("发送队列_关闭重开_队列与发送泵重建")]
    public async Task SendQueue_RebuiltAfterCloseAndReopen()
    {
        var listener = CreateListener(out var port);
        using var client = new TcpSession { Remote = new NetUri($"tcp://127.0.0.1:{port}") };
        try
        {
            client.Open();

            Pipe queue1;
            using (var peer1 = listener.Accept())
            {
                peer1.ReceiveTimeout = 30_000;

                queue1 = client.SendQueue;
                Assert.Same(queue1, client.GetSendQueue());
                Assert.True(await client.SendQueuedAsync(new ArrayPacket(new Byte[] { 1, 2, 3 })));

                var buf = new Byte[16];
                Assert.Equal(3, peer1.Receive(buf));
                Assert.Equal(new Byte[] { 1, 2, 3 }, buf[..3]);
            }

            client.Close("test");

            // 等关闭收尾完成（入站管道与出站队列均复位）
            var sw = Stopwatch.StartNew();
            while (client.GetSendQueue() != null || client.Active)
            {
                Assert.True(sw.ElapsedMilliseconds < 10_000, "关闭收尾超时");
                await Task.Delay(10);
            }

            // 客户端会话同一实例可重开：再次访问得到新队列（新发送泵）
            var queue2 = client.SendQueue;
            Assert.NotNull(queue2);
            Assert.NotSame(queue1, queue2);

            // 入队会按需重连，数据送达新连接
            var sending = Task.Run(() => client.SendQueuedAsync(new ArrayPacket(new Byte[] { 4, 5, 6 })).AsTask());
            using var peer2 = listener.Accept();
            peer2.ReceiveTimeout = 30_000;

            Assert.True(await sending.WaitAsync(TimeSpan.FromSeconds(10)));

            var buf2 = new Byte[16];
            Assert.Equal(3, peer2.Receive(buf2));
            Assert.Equal(new Byte[] { 4, 5, 6 }, buf2[..3]);
        }
        finally
        {
            listener.Dispose();
        }
    }

    [Fact]
    [DisplayName("发送队列_空数据包_抛出ArgumentNullException")]
    public async Task SendQueued_NullData_Throws()
    {
        var listener = CreateListener(out var port);
        using var client = new TcpSession { Remote = new NetUri($"tcp://127.0.0.1:{port}") };
        try
        {
            client.Open();

            await Assert.ThrowsAsync<ArgumentNullException>(() => client.SendQueuedAsync(null!).AsTask());
        }
        finally
        {
            listener.Dispose();
        }
    }

    [Fact]
    [DisplayName("发送队列_等待水位时关闭_入队方被唤醒不挂起")]
    public async Task SendQueued_ClosedWhileWaitingResume_DoesNotHang()
    {
        const Int32 chunkSize = 4 * 1024;

        var listener = CreateListener(out var port);
        using var client = new SmallQueueSession { Remote = new NetUri($"tcp://127.0.0.1:{port}") };
        try
        {
            client.Open();
            client.Client!.SendBufferSize = 8 * 1024;

            using var peer = listener.Accept();
            peer.ReceiveBufferSize = 8 * 1024;
            peer.ReceiveTimeout = 30_000;

            // 对端不读：内核缓冲吃满后泵阻塞，队列积压到水位，入队方挂在“等待恢复”上
            var sending = Task.Factory.StartNew(async () =>
            {
                for (var i = 0; i < 4096; i++)
                {
                    var body = new Byte[chunkSize];
                    body.AsSpan().Fill(0x33);

                    if (!await client.SendQueuedAsync(new ArrayPacket(body)).ConfigureAwait(false)) return i;
                }

                return -1;
            }, TaskCreationOptions.LongRunning).Unwrap();

            Assert.False(await CompletedWithinAsync(sending, 500), "前置条件不成立：入队方应挂在等待水位上");

            // 关闭：完成写侧不触发 Resumed，必须显式唤醒等待中的入队方，否则永久挂起
            client.Close("test");

            Assert.True(await CompletedWithinAsync(sending, 10_000), "关闭后入队方仍未结束：等水位的入队方没有被唤醒");
        }
        finally
        {
            listener.Dispose();
        }
    }
}
