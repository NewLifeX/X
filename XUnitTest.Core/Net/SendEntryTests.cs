using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

/// <summary>发送出口测试：直发与发送泵首次接管出口的交错，两条路径不得并发写同一 Socket</summary>
/// <remarks>
/// <para>交错窗口（“无锁快照读到无泵”与“泵发布”之间）转瞬即逝、无法直接观测，但互斥性会外显为一件事：
/// 直发在途时创建发送泵（首次访问 <see cref="TcpSession.SendPipe"/>）必须等它写完才发布泵。</para>
/// <para>本用例用“对端不读 + 收缩收发缓冲”把直发钉在内核写阻塞上：断言此刻创建发送泵不立即完成，
/// 且直发之后入队的数据必须整段排在直发数据后面（逐块填充不同字节，错位即可定位）。</para>
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
    [DisplayName("发送出口_在途直发期间创建发送泵_必须等待且数据不交错")]
    public async Task InFlightDirectSend_PumpCreation_WaitsAndKeepsOrder()
    {
        const Int32 chunkSize = 64 * 1024;
        const Int32 chunkCount = 64;                        // 合计 4MB，远超内核收发缓冲
        var body = new Byte[chunkSize * chunkCount];
        for (var i = 0; i < chunkCount; i++) body.AsSpan(i * chunkSize, chunkSize).Fill((Byte)(i + 1));

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

            // 逐块直发：发送泵尚未创建，全部走直发路径并持发送锁；对端不读，几十 KB 后必然阻塞在写
            var sending = Task.Factory.StartNew(() =>
            {
                var total = 0;
                for (var i = 0; i < chunkCount; i++)
                {
                    var rs = client.Send(body, i * chunkSize, chunkSize);
                    if (rs <= 0) break;

                    total += rs;
                }

                return total;
            }, TaskCreationOptions.LongRunning);
            Assert.False(await CompletedWithinAsync(sending, 300), "前置条件不成立：直发应被内核缓冲阻塞");

            // 关键断言：直发在途时创建发送泵必须等待（发布点持同一把发送锁），否则两条路径会并发写同一 Socket
            var creating = Task.Factory.StartNew(() => client.SendPipe, TaskCreationOptions.LongRunning);
            Assert.False(await CompletedWithinAsync(creating, 300), "在途直发未结束时，创建发送泵不应立即完成");

            // 对端开始读取：直发写完 → 发送泵获锁并发布 → 之后的数据统一入队
            var received = new MemoryStream();
            var reading = Task.Factory.StartNew(() =>
            {
                var buf = new Byte[64 * 1024];
                while (received.Length < body.Length + 3)
                {
                    var n = peer.Receive(buf);
                    if (n <= 0) break;

                    received.Write(buf, 0, n);
                }
            }, TaskCreationOptions.LongRunning);

            var pipe = await creating.WaitAsync(TimeSpan.FromSeconds(30));
            Assert.Same(pipe, client.GetSendPipe());

            // 直发全部结束（含泵发布后仍在直发的余量），此后入队的数据必须整段排在直发数据之后
            Assert.Equal(body.Length, await sending.WaitAsync(TimeSpan.FromSeconds(30)));

            client.Send(new Byte[] { 0xAA, 0xBB, 0xCC });
            await WaitUntilAsync(() => pipe.UnconsumedLength == 0);

            Assert.True(client.Close("test"));
            await reading.WaitAsync(TimeSpan.FromSeconds(30));

            var bytes = received.ToArray();
            Assert.Equal(body.Length + 3, bytes.Length);
            Assert.Equal(body, bytes[..body.Length]);
            Assert.Equal(new Byte[] { 0xAA, 0xBB, 0xCC }, bytes[^3..]);
        }
        finally
        {
            listener.Dispose();
        }
    }
}
