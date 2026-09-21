using System.Buffers;
using System.ComponentModel;
using NewLife;
using NewLife.Data;
using NewLife.Messaging;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

/// <summary>会话数据管道（IStreamSession.Pipe 流式接收）测试</summary>
[Collection("Net")]
public class SessionPipeTests
{
    [Fact]
    [DisplayName("会话管道_接收投递_帧泵流式读取")]
    public async Task SessionPipe_Frames()
    {
        using var server = new NetServer { Port = 0 };
        server.Start();

        var wait = new ManualResetEventSlim();
        Pipe? pipe = null;
        server.NewSession += (s, e) =>
        {
            pipe = (e.Session.Session as IStreamSession)?.Pipe;
            wait.Set();
        };

        using var client = new NetUri($"tcp://127.0.0.1:{server.Port}").CreateRemote();
        client.Open();

        // 会话建立且管道就绪后再发送，避免首轮数据早于管道创建
        Assert.True(wait.Wait(3_000));
        Assert.NotNull(pipe);

        // 两帧（标准 SRMP）：第二帧分两次发送，验证跨轮组帧
        var f1 = BuildFrame(Fill(10, 1));
        var f2 = BuildFrame(Fill(300, 2));
        _ = client.Send(f1);
        _ = client.Send(f2[..100]);
        await Task.Delay(20);
        _ = client.Send(f2[100..]);

        var parser = new DefaultMessage();
        var framer = new PacketFramer { GetFrameLength = buffer => parser.TryParse(buffer, out _) };
        var p1 = await framer.ReadFrameAsync(pipe!.Reader).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.NotNull(p1);
        Assert.Equal(f1, p1!.AsReadOnlySequence().ToArray());

        var p2 = await framer.ReadFrameAsync(pipe!.Reader).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.NotNull(p2);
        Assert.Equal(f2, p2!.AsReadOnlySequence().ToArray());

        p1.TryDispose();
        p2.TryDispose();
    }

    [Fact]
    [DisplayName("会话管道_关闭_通知消费方流结束")]
    public async Task SessionPipe_Close_CompletesReader()
    {
        using var server = new NetServer { Port = 0 };
        server.Start();

        var wait = new ManualResetEventSlim();
        Pipe? pipe = null;
        server.NewSession += (s, e) =>
        {
            pipe = (e.Session.Session as IStreamSession)?.Pipe;
            wait.Set();
        };

        using var client = new NetUri($"tcp://127.0.0.1:{server.Port}").CreateRemote();
        client.Open();

        Assert.True(wait.Wait(3_000));
        Assert.NotNull(pipe);

        // 挂起读取，客户端断开后服务端会话关闭，读取应立即完成（IsCompleted）
        var vt = pipe!.Reader.ReadAsync();
        Assert.False(vt.IsCompleted);

        client.Close("test");

        var rr = await vt.AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(rr.IsCompleted);
        Assert.True(rr.Buffer.IsEmpty);
    }

    [Fact]
    [DisplayName("会话管道_背压_达到暂停水位后恢复")]
    public async Task SessionPipe_Backpressure_PauseResume()
    {
        using var server = new NetServer { Port = 0 };
        server.Start();

        var wait = new ManualResetEventSlim();
        Pipe? pipe = null;
        server.NewSession += (s, e) =>
        {
            pipe = (e.Session.Session as IStreamSession)?.Pipe;
            wait.Set();
        };

        using var client = new NetUri($"tcp://127.0.0.1:{server.Port}").CreateRemote();
        client.Open();

        Assert.True(wait.Wait(3_000));
        Assert.NotNull(pipe);

        // 自定义低水位，避免真发 1MB 才能触发
        pipe!.PauseThreshold = 32 * 1024;
        pipe.ResumeThreshold = 16 * 1024;

        var resumed = 0;
        pipe.Resumed += (s, e) => Interlocked.Increment(ref resumed);

        // 发送端后台推送 40 帧 × 4KB：接收端不消费时管道应达到暂停水位并停止接收
        const Int32 frameCount = 40;
        var totalSent = 0L;
        var frames = new List<Byte[]>();
        for (var i = 0; i < frameCount; i++)
        {
            var f = BuildFrame(Fill(4 * 1024, (Byte)i));
            frames.Add(f);
            totalSent += f.Length;
        }

        var sender = Task.Run(() =>
        {
            foreach (var f in frames) client.Send(f);
        });

        // 等待管道达到暂停水位（接收方未消费，数据持续累积）
        var sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        while (!pipe.IsPaused && sw.Elapsed < TimeSpan.FromSeconds(10)) await Task.Delay(10);
        Assert.True(pipe.IsPaused, "接收数据未触发暂停水位");

        // 持续消费：降到恢复水位以下应触发 Resumed 并恢复接收，最终收完所有帧
        var parser = new DefaultMessage();
        var framer = new PacketFramer { GetFrameLength = buffer => parser.TryParse(buffer, out _) };
        var received = 0;
        var total = 0L;
        while (received < frameCount)
        {
            var frame = await framer.ReadFrameAsync(pipe.Reader).AsTask().WaitAsync(TimeSpan.FromSeconds(10));
            Assert.NotNull(frame);

            total += frame!.Total;
            received++;
            frame.TryDispose();
        }

        await sender.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.True(resumed > 0, "消费降压后未触发恢复事件");
        Assert.Equal(totalSent, total);
        Assert.False(pipe.IsPaused);
    }

    [Fact]
    [DisplayName("会话管道_整帧路径_超大帧凑齐不受暂停阻挡（读饥饿让位）")]
    public async Task SessionPipe_WholeFrameExceedsPauseThreshold_ReaderStarvationReleases()
    {
        using var server = new NetServer { Port = 0 };
        server.Start();

        var wait = new ManualResetEventSlim();
        Pipe? pipe = null;
        server.NewSession += (s, e) =>
        {
            pipe = (e.Session.Session as TcpSession)?.Pipe;
            wait.Set();
        };

        using var client = new NetUri($"tcp://127.0.0.1:{server.Port}").CreateRemote();
        client.Open();

        Assert.True(wait.Wait(3_000));
        Assert.NotNull(pipe);

        // 低水位（64K 暂停 / 32K 恢复）；单帧声明 256K，远超暂停水位
        pipe!.PauseThreshold = 64 * 1024;
        pipe.ResumeThreshold = 32 * 1024;

        var resumed = 0;
        pipe.Resumed += (s, e) => Interlocked.Increment(ref resumed);

        var frame = BuildFrame(Fill(256 * 1024, 0x5A));

        // 后台分块推送（模拟网络分片）；远超水位后 send 可能停顿于内核缓冲，属预期
        var sender = Task.Run(() =>
        {
            try
            {
                for (var i = 0; i < frame.Length; i += 8 * 1024)
                {
                    var count = Math.Min(8 * 1024, frame.Length - i);
                    client.Send(frame, i, count);
                }
            }
            catch { }
        });

        // 整帧路径：帧不完整不消费，暂停先触发；但读侧挂起等待（饥饿）时让位放行，帧应完整到达
        var parser = new DefaultMessage();
        var framer = new PacketFramer { GetFrameLength = buffer => parser.TryParse(buffer, out _) };
        var pk = await framer.ReadFrameAsync(pipe.Reader).AsTask().WaitAsync(TimeSpan.FromSeconds(15));

        Assert.NotNull(pk);
        Assert.Equal(frame.Length, pk!.Total);
        Assert.Equal(frame, pk.AsReadOnlySequence().ToArray());
        pk.TryDispose();

        // 读饥饿让位至少发生一次：暂停已触发，但读者等待的数据被放行
        Assert.True(resumed > 0, "超大帧等待期间未发生读饥饿让位");

        await Task.WhenAny(sender, Task.Delay(2_000));
    }

    [Fact]
    [DisplayName("会话管道_背压_高频暂停恢复_多轮无丢唤醒")]
    public async Task SessionPipe_Backpressure_HighFrequencyPauseResume()
    {
        using var server = new NetServer { Port = 0 };
        server.Start();

        var wait = new ManualResetEventSlim();
        Pipe? pipe = null;
        server.NewSession += (s, e) =>
        {
            pipe = (e.Session.Session as TcpSession)?.Pipe;
            wait.Set();
        };

        using var client = new NetUri($"tcp://127.0.0.1:{server.Port}").CreateRemote();
        client.Open();

        Assert.True(wait.Wait(3_000));
        Assert.NotNull(pipe);

        // 小水位 + 快消费：制造高频暂停/恢复循环，覆盖“暂存晚于消费恢复”的丢唤醒窗口
        pipe!.PauseThreshold = 16 * 1024;
        pipe.ResumeThreshold = 8 * 1024;

        var parser = new DefaultMessage();
        var framer = new PacketFramer { GetFrameLength = buffer => parser.TryParse(buffer, out _) };

        // 多轮发送：跨轮复现“前轮正常、后轮接收停摆”的场景（对齐基准规模，制造高频暂停/恢复）
        for (var round = 0; round < 2; round++)
        {
            const Int32 frameCount = 1024;
            var frame = BuildFrame(Fill(16 * 1024, (Byte)round));

            var sender = Task.Run(() =>
            {
                try
                {
                    for (var i = 0; i < frameCount; i++) client.Send(frame);
                }
                catch { }
            });

            var received = 0;
            try
            {
                while (received < frameCount)
                {
                    var f = await framer.ReadFrameAsync(pipe.Reader).AsTask().WaitAsync(TimeSpan.FromSeconds(30));
                    Assert.NotNull(f);
                    f!.TryDispose();
                    received++;
                }
            }
            catch (TimeoutException)
            {
                throw new TimeoutException($"第 {round} 轮接收停摆：已收 {received}/{frameCount}，暂停={pipe.IsPaused}，未消费={pipe.UnconsumedLength}");
            }

            try
            {
                await sender.WaitAsync(TimeSpan.FromSeconds(30));
            }
            catch (TimeoutException)
            {
                throw new TimeoutException($"第 {round} 轮发送未完成：暂停={pipe.IsPaused}，未消费={pipe.UnconsumedLength}");
            }
        }

        client.Close("test");
    }

    #region 工具
    private static Byte[] Fill(Int32 count, Byte value)
    {
        var buf = new Byte[count];
        for (var i = 0; i < count; i++) buf[i] = value;

        return buf;
    }

    /// <summary>构造标准消息帧（4/8字节头 + 负载）</summary>
    private static Byte[] BuildFrame(Byte[] payload)
    {
        var headerSize = payload.Length < 0xFFFF ? 4 : 8;
        var buf = new Byte[headerSize + payload.Length];
        buf[0] = 0x01;
        buf[1] = 0x02;
        if (headerSize == 4)
        {
            buf[2] = (Byte)(payload.Length & 0xFF);
            buf[3] = (Byte)(payload.Length >> 8);
        }
        else
        {
            buf[2] = 0xFF;
            buf[3] = 0xFF;
            buf[4] = (Byte)(payload.Length & 0xFF);
            buf[5] = (Byte)((payload.Length >> 8) & 0xFF);
            buf[6] = (Byte)((payload.Length >> 16) & 0xFF);
            buf[7] = (Byte)((payload.Length >> 24) & 0xFF);
        }
        payload.CopyTo(buf, headerSize);

        return buf;
    }
    #endregion
}
