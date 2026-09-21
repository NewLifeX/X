using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using NewLife.Data;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

/// <summary>发送背压（TrySend 暂停拒绝 / SendAsync(IPacket) 挂起等待）测试</summary>
[Collection("Net")]
public class SendBackpressureTests
{
    [Fact]
    [DisplayName("背压发送_TrySend与SendAsync_暂停拒绝_恢复完成")]
    public async Task Pause_RejectsTrySend_SuspendsSendAsync()
    {
        // 裸监听套接字：接受连接但不读取，使内核缓冲写满后发送泵阻塞、管道积压达到暂停水位
        var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        listener.Listen(1);
        var port = ((IPEndPoint)listener.LocalEndPoint!).Port;

        using var client = new TcpSession { Remote = new NetUri($"tcp://127.0.0.1:{port}") };
        try
        {
            client.Open();

            // 收缩客户端发送缓冲，加快内核缓冲饱和
            client.Client!.SendBufferSize = 16 * 1024;

            var pipe = client.SendPipe;
            pipe.PauseThreshold = 32 * 1024;
            pipe.ResumeThreshold = 16 * 1024;

            // 推入数据：泵写入内核的部分有限，应用层管道积压很快达到暂停水位（同步 Send 走管道为入队，不阻塞）
            var payload = new Byte[64 * 1024];
            var sw = Stopwatch.StartNew();
            while (!pipe.IsPaused && sw.Elapsed < TimeSpan.FromSeconds(10))
            {
                client.Send(payload);
                await Task.Delay(5);
            }
            Assert.True(pipe.IsPaused, "未达到暂停水位");

            // 超量灌注，确保积压远超恢复水位，对端不读时泵无法消化
            for (var i = 0; i < 8; i++) client.Send(payload);

            // 暂停水位：TrySend 拒绝；SendAsync 挂起等待
            Assert.False(client.TrySend(new ArrayPacket(new Byte[128])));

            var sending = client.SendAsync(new ArrayPacket(new Byte[128]));
            await Task.Delay(200);
            Assert.False(sending.IsCompleted, "暂停水位下 SendAsync 不应完成");

            // 对端开始读取：积压消化到恢复水位以下，挂起提交被唤醒
            var conn = listener.Accept();
            _ = Task.Run(() =>
            {
                try
                {
                    var buf = new Byte[64 * 1024];
                    var total = 0L;
                    while (total < 4L * 1024 * 1024)
                    {
                        var n = conn.Receive(buf);
                        if (n <= 0) break;

                        total += n;
                    }
                }
                catch
                {
                    // 测试收尾关闭连接时正常退出
                }
            });

            var rs = await sending.AsTask().WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal(128, rs);
            Assert.False(pipe.IsPaused);

            conn.Dispose();
        }
        finally
        {
            listener.Dispose();
        }
    }

    [Fact]
    [DisplayName("背压发送_正常路径_TrySend与SendAsync送达")]
    public async Task Normal_SendDeliversInOrder()
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
                if (pk != null) lock (received) received.AddRange(pk.ToArray());
            };
            wait.Set();
        };

        using var client = new TcpSession { Remote = new NetUri($"tcp://127.0.0.1:{server.Port}") };
        client.Open();

        // 会话建立且订阅就绪后再发送，避免首轮数据早于订阅
        Assert.True(wait.Wait(3_000));

        // 正常水位：TrySend 接受
        Assert.True(client.TrySend(new ArrayPacket(new Byte[] { 1, 2, 3, 4 })));

        // SendAsync(IPacket)：未暂停时立即完成
        var rs = await client.SendAsync(new ArrayPacket(new Byte[] { 5, 6, 7 }));
        Assert.Equal(3, rs);

        // 送达且保序
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < TimeSpan.FromSeconds(5))
        {
            lock (received) { if (received.Count >= 7) break; }
            await Task.Delay(10);
        }

        lock (received) Assert.Equal(new Byte[] { 1, 2, 3, 4, 5, 6, 7 }, received);
    }

    [Fact]
    [DisplayName("背压发送_挂起期间关闭_发送被唤醒而不挂死")]
    public async Task Suspend_ThenClose_WakesUp()
    {
        // 裸监听套接字：接受连接但不读取，使发送泵阻塞、管道积压达到暂停水位
        var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        listener.Listen(1);
        var port = ((IPEndPoint)listener.LocalEndPoint!).Port;

        try
        {
            using var client = new TcpSession { Remote = new NetUri($"tcp://127.0.0.1:{port}") };
            client.Open();
            client.Client!.SendBufferSize = 16 * 1024;

            var pipe = client.SendPipe;
            pipe.PauseThreshold = 32 * 1024;
            pipe.ResumeThreshold = 16 * 1024;

            // 灌入数据直到达到暂停水位
            var payload = new Byte[64 * 1024];
            var sw = Stopwatch.StartNew();
            while (!pipe.IsPaused && sw.Elapsed < TimeSpan.FromSeconds(10))
            {
                client.Send(payload);
                await Task.Delay(5);
            }
            Assert.True(pipe.IsPaused, "未达到暂停水位");
            for (var i = 0; i < 8; i++) client.Send(payload);

            // 暂停水位下 SendAsync 挂起
            var sending = client.SendAsync(new ArrayPacket(new Byte[128]));
            await Task.Delay(200);
            Assert.False(sending.IsCompleted, "暂停水位下 SendAsync 不应完成");

            // 挂起期间关闭会话：关闭不应被背压阻塞，挂起发送应被唤醒并失败
            var closeTask = Task.Run(() => client.Close("close-while-paused"));
            var closeCompleted = await Task.WhenAny(closeTask, Task.Delay(8_000)) == closeTask;
            Assert.True(closeCompleted, "关闭会话不应因背压挂起而被阻塞");

            var task = sending.AsTask();
            var sendCompleted = await Task.WhenAny(task, Task.Delay(5_000)) == task;
            Assert.True(sendCompleted, "关闭会话后挂起的 SendAsync 应被唤醒，而不是永久挂起");

            try
            {
                var rs = await task;
                Assert.True(rs <= 0, $"关闭后发送不应报告成功：{rs}");
            }
            catch
            {
                // 明确异常亦可：挂起者被唤醒并失败即达成目标
            }
        }
        finally
        {
            listener.Dispose();
        }
    }
}
