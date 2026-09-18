using System.ComponentModel;
using System.Diagnostics;
using NewLife.Data;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

/// <summary>发送泵（SendPump 出站管道：追加/泵送/背压/排空/中止/流式发送）组件测试</summary>
public class SendPumpTests
{
    #region 工具
    private static async Task WaitUntilAsync(Func<Boolean> condition, Int32 timeoutMs = 10_000)
    {
        var sw = Stopwatch.StartNew();
        while (!condition())
        {
            if (sw.ElapsedMilliseconds > timeoutMs) throw new TimeoutException("等待条件超时");

            await Task.Delay(10);
        }
    }

    /// <summary>测试假泵：捕获发送数据，可选择闸门阻塞、部分发送与失败</summary>
    private sealed class FakePump
    {
        /// <summary>已发送数据段</summary>
        public List<Byte[]> Sent { get; } = [];

        /// <summary>已发送段数</summary>
        public Int32 SendCount { get { lock (Sent) return Sent.Count; } }

        /// <summary>发送闸门。非空时每次发送前等待（模拟慢速对端）</summary>
        public ManualResetEventSlim? Gate { get; set; }

        /// <summary>置位后发送返回 -1（模拟连接故障）</summary>
        public Boolean FailOnSend { get; set; }

        /// <summary>部分发送：大于 0 时每次只接受这么多字节（模拟内核缓冲只收一部分）</summary>
        public Int32 PartialChunk { get; set; }

        /// <summary>错误回调记录的动作名</summary>
        public List<String> Errors { get; } = [];

        /// <summary>发送泵</summary>
        public SendPump Pump { get; }

        public FakePump()
        {
            Pump = new SendPump(new Pipe(), OnSend, (action, ex) => { lock (Errors) Errors.Add(action); }, (format, args) => { });
        }

        private ValueTask<Int32> OnSend(ReadOnlyMemory<Byte> data)
        {
            Gate?.Wait(10_000);
            if (FailOnSend) return new(-1);

            var chunk = data;
            if (PartialChunk > 0 && data.Length > PartialChunk) chunk = data[..PartialChunk];

            lock (Sent) Sent.Add(chunk.ToArray());

            return new(chunk.Length);
        }
    }
    #endregion

    #region 基本
    [Fact]
    [DisplayName("发送泵_追加_按序送出")]
    public async Task Append_PumpSendsInOrder()
    {
        var fake = new FakePump();
        var pipe = fake.Pump.Pipe;

        var p1 = new Byte[] { 1, 2, 3 };
        var p2 = new Byte[] { 4, 5 };
        var p3 = new Byte[] { 6 };
        fake.Pump.Append(new ArrayPacket(p1));
        fake.Pump.Append(new ArrayPacket(p2));
        fake.Pump.Append(new ArrayPacket(p3));

        // 等待泵把三包全部送出并消费指针推进到位（两个条件非原子，需一起等待）
        await WaitUntilAsync(() => fake.SendCount == 3 && pipe.UnconsumedLength == 0);

        Assert.Equal(p1, fake.Sent[0]);
        Assert.Equal(p2, fake.Sent[1]);
        Assert.Equal(p3, fake.Sent[2]);
        Assert.Equal(0, pipe.UnconsumedLength);
    }

    [Fact]
    [DisplayName("发送泵_借阅视图入管道_已转自有拷贝")]
    public async Task Append_BorrowedView_ClonesOnQueue()
    {
        var fake = new FakePump();
        var pipe = fake.Pump.Pipe;

        var src = new Byte[] { 10, 20, 30 };
        var rs = fake.Pump.Append(new ArrayPacket(src));
        Assert.Equal(3, rs);

        // 借阅视图在入管道时已转自有拷贝：立即改写原缓冲不影响已排队数据
        src[0] = 99;

        await WaitUntilAsync(() => fake.SendCount == 1);
        Assert.Equal(new Byte[] { 10, 20, 30 }, fake.Sent[0]);
        Assert.Equal(0, pipe.UnconsumedLength);
    }

    [Fact]
    [DisplayName("发送泵_字节跨度入管道_按副本不锁原缓冲")]
    public async Task Append_Span_CopiesIntoPipe()
    {
        var fake = new FakePump();
        _ = fake.Pump.Pipe;

        var buf = new Byte[] { 1, 2, 3, 4 };
        var rs = fake.Pump.Append(new ReadOnlySpan<Byte>(buf, 1, 2));
        Assert.Equal(2, rs);

        // 立即复用原缓冲
        buf[1] = 99;
        buf[2] = 99;

        await WaitUntilAsync(() => fake.SendCount == 1);
        Assert.Equal(new Byte[] { 2, 3 }, fake.Sent[0]);
    }

    [Fact]
    [DisplayName("发送泵_部分发送_自动续发直至发完")]
    public async Task PartialSend_ContinuesUntilDone()
    {
        var fake = new FakePump { PartialChunk = 2 };
        var pipe = fake.Pump.Pipe;

        var payload = new Byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        var rs = fake.Pump.Append(new ReadOnlySpan<Byte>(payload));
        Assert.Equal(10, rs);

        await WaitUntilAsync(() => pipe.UnconsumedLength == 0 && fake.Sent.Sum(e => e.Length) == 10);

        // 段内部分发送续发：每次只接受 2 字节，共 5 段
        Assert.Equal(5, fake.SendCount);
        Assert.All(fake.Sent, e => Assert.Equal(2, e.Length));
        Assert.Equal(payload, fake.Sent.SelectMany(e => e).ToArray());
    }
    #endregion

    #region 回压与生命周期
    [Fact]
    [DisplayName("发送泵_写侧回压_暂停挂起提交_消费恢复唤醒")]
    public async Task Backpressure_PauseSuspendsFlush_ResumeWakes()
    {
        var fake = new FakePump();
        var pipe = fake.Pump.Pipe;
        pipe.PauseThreshold = 64;
        pipe.ResumeThreshold = 32;

        var gate = new ManualResetEventSlim(false);
        fake.Gate = gate;

        // 先放 100 字节：泵被闸门挡住，未发送量达到暂停水位
        fake.Pump.Append(new ArrayPacket(new Byte[100]));
        await WaitUntilAsync(() => pipe.IsPaused);

        // 写侧提交挂起等待恢复
        var vt = pipe.Writer.FlushAsync(true);
        Assert.False(vt.IsCompleted);

        // 放行：泵发完后未消费量降到恢复水位以下，挂起提交被唤醒
        gate.Set();
        var fr = await vt.AsTask().WaitAsync(TimeSpan.FromSeconds(3));
        Assert.False(fr.IsCanceled);
        Assert.False(pipe.IsPaused);
        Assert.Equal(0, pipe.UnconsumedLength);
        Assert.Equal(1, fake.SendCount);
    }

    [Fact]
    [DisplayName("发送泵_关闭_先排空排队数据再完成")]
    public async Task Flush_DrainsPendingBeforeClosing()
    {
        var fake = new FakePump();
        var pipe = fake.Pump.Pipe;

        var p1 = new Byte[] { 1, 2, 3 };
        var p2 = new Byte[] { 4, 5 };
        fake.Pump.Append(new ArrayPacket(p1));
        fake.Pump.Append(new ArrayPacket(p2));

        // 完成写入并限时等待泵发完已排队数据
        await fake.Pump.FlushAsync(3_000);

        Assert.True(pipe.IsCompleted);
        Assert.Equal(2, fake.SendCount);
        Assert.Equal(p1, fake.Sent[0]);
        Assert.Equal(p2, fake.Sent[1]);
        Assert.Equal(0, pipe.UnconsumedLength);
    }

    [Fact]
    [DisplayName("发送泵_发送失败_中止管道并唤醒")]
    public async Task SendFailure_AbortsPipe()
    {
        var fake = new FakePump { FailOnSend = true };
        var pipe = fake.Pump.Pipe;

        fake.Pump.Append(new ArrayPacket(new Byte[] { 1, 2, 3 }));

        await WaitUntilAsync(() => pipe.IsCompleted);
        Assert.NotNull(pipe.Error);

        // 中止后：追加的数据由管道直接释放，Append 返回失败
        pipe.Writer.Append(new ArrayPacket(new Byte[] { 4 }));
        Assert.Equal(-1, fake.Pump.Append(new ArrayPacket(new Byte[] { 5 })));
    }
    #endregion

    #region 流式发送
    [Fact]
    [DisplayName("流式发送_按期望长度发送")]
    public async Task SendStream_Length_SendsExactly()
    {
        var fake = new FakePump();
        var pipe = fake.Pump.Pipe;

        var data = new Byte[300];
        Random.Shared.NextBytes(data);

        var total = await fake.Pump.SendAsync(new MemoryStream(data), 200);
        Assert.Equal(200, total);

        await WaitUntilAsync(() => pipe.UnconsumedLength == 0);
        Assert.Single(fake.Sent);
        Assert.Equal(data[..200], fake.Sent[0]);
    }

    [Fact]
    [DisplayName("流式发送_负数长度_读到流尾")]
    public async Task SendStream_ToEnd()
    {
        var fake = new FakePump();
        var pipe = fake.Pump.Pipe;

        var data = new Byte[300];
        Random.Shared.NextBytes(data);

        var total = await fake.Pump.SendAsync(new MemoryStream(data));
        Assert.Equal(300, total);

        await WaitUntilAsync(() => pipe.UnconsumedLength == 0);
        Assert.Single(fake.Sent);
        Assert.Equal(data, fake.Sent[0]);
    }

    [Fact]
    [DisplayName("流式发送_流提前结束_抛出异常")]
    public async Task SendStream_EarlyEnd_Throws()
    {
        var fake = new FakePump();
        var pipe = fake.Pump.Pipe;

        var data = new Byte[300];
        await Assert.ThrowsAsync<InvalidDataException>(async () => await fake.Pump.SendAsync(new MemoryStream(data), 500));

        // 已入管道的部分照常送出
        await WaitUntilAsync(() => pipe.UnconsumedLength == 0);
        Assert.Equal(300, fake.Sent.Sum(e => e.Length));
    }

    [Fact]
    [DisplayName("流式发送_写侧回压_挂起等待_恢复后完成")]
    public async Task SendStream_Backpressure_SuspendsAndResumes()
    {
        var fake = new FakePump();
        var pipe = fake.Pump.Pipe;
        pipe.PauseThreshold = 8 * 1024;
        pipe.ResumeThreshold = 4 * 1024;

        // 泵被闸门挡住：队列无法消化，生产者应在暂停水位附近挂起
        var gate = new ManualResetEventSlim(false);
        fake.Gate = gate;

        var data = new Byte[256 * 1024];
        var task = fake.Pump.SendAsync(new MemoryStream(data)).AsTask();

        await WaitUntilAsync(() => pipe.IsPaused);
        await Task.Delay(50);
        Assert.False(task.IsCompleted);
        Assert.True(pipe.UnconsumedLength <= 64 * 1024, $"挂起期间积压应有界，实际 {pipe.UnconsumedLength}");

        // 放行：泵消化后生产者继续，直至全部发完
        gate.Set();
        var total = await task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(data.Length, total);
        await WaitUntilAsync(() => pipe.UnconsumedLength == 0);
        Assert.Equal(data.Length, fake.Sent.Sum(e => e.Length));
    }

    [Fact]
    [DisplayName("流式发送_取消_立即抛出")]
    public async Task SendStream_Canceled_Throws()
    {
        var fake = new FakePump();
        _ = fake.Pump.Pipe;

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await fake.Pump.SendAsync(new MemoryStream(new Byte[100]), -1, cts.Token));
        Assert.Equal(0, fake.SendCount);
    }
    #endregion
}
