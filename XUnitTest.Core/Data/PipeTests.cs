using System.Buffers;
using System.ComponentModel;
using NewLife;
using NewLife.Data;
using Xunit;

namespace XUnitTest.Data;

/// <summary>数据包管道（Pipe 与 Reader/Writer 句柄）测试</summary>
public class PipeTests
{
    #region 工具
    /// <summary>快速构建字节数组</summary>
    private static Byte[] B(params Int32[] values)
    {
        var buf = new Byte[values.Length];
        for (var i = 0; i < values.Length; i++) buf[i] = (Byte)values[i];

        return buf;
    }
    #endregion

    #region 读取
    [Fact]
    [DisplayName("Pipe_有数据_立即返回窗口")]
    public async Task ReadAsync_HasData_ReturnsImmediately()
    {
        using var pipe = new Pipe();
        pipe.Writer.Append(new ArrayPacket(B(1, 2, 3)));

        var vt = pipe.Reader.ReadAsync();

        Assert.True(vt.IsCompleted);
        var rr = await vt;
        Assert.Equal(B(1, 2, 3), rr.Buffer.ToArray());
        Assert.False(rr.IsCompleted);
        Assert.False(rr.IsCanceled);
    }

    [Fact]
    [DisplayName("Pipe_无数据_追加后唤醒挂起读取")]
    public async Task ReadAsync_NoData_AwakenedByAppend()
    {
        using var pipe = new Pipe();

        var vt = pipe.Reader.ReadAsync();
        Assert.False(vt.IsCompleted);

        pipe.Writer.Append(new ArrayPacket(B(1, 2, 3)));

        var rr = await vt;
        Assert.Equal(B(1, 2, 3), rr.Buffer.ToArray());
        Assert.False(rr.IsCompleted);
    }

    [Fact]
    [DisplayName("Pipe_多轮追加_窗口顺序一致")]
    public async Task ReadAsync_MultiAppend_WindowContinuous()
    {
        using var pipe = new Pipe();
        pipe.Writer.Append(new ArrayPacket(B(1, 2, 3)));

        var rr = await pipe.Reader.ReadAsync();
        Assert.Equal(B(1, 2, 3), rr.Buffer.ToArray());

        // 部分消费后追加新数据
        pipe.Reader.AdvanceTo(1);
        Assert.Equal(2, pipe.UnconsumedLength);
        Assert.Equal(B(2, 3), pipe.Reader.Buffer.ToArray());

        pipe.Writer.Append(new ArrayPacket(B(4, 5)));
        var rr2 = await pipe.Reader.ReadAsync();
        Assert.Equal(B(2, 3, 4, 5), rr2.Buffer.ToArray());

        pipe.Reader.AdvanceTo(4);
        Assert.Equal(0, pipe.UnconsumedLength);
        Assert.True(pipe.Reader.Buffer.IsEmpty);
    }

    [Fact]
    [DisplayName("Pipe_链式追加_跨段窗口可读")]
    public async Task ReadAsync_ChainedAppend_CrossSegmentWindow()
    {
        using var pipe = new Pipe();
        IPacket pk = new ArrayPacket(B(1, 2));
        pk.Append(new ArrayPacket(B(3, 4, 5)));
        pipe.Writer.Append(pk);

        var rr = await pipe.Reader.ReadAsync();
        Assert.False(rr.Buffer.IsSingleSegment);
        Assert.Equal(B(1, 2, 3, 4, 5), rr.Buffer.ToArray());

        // 消费第一段整段+第二段1字节
        pipe.Reader.AdvanceTo(3);
        Assert.Equal(B(4, 5), pipe.Reader.Buffer.ToArray());

        pipe.Reader.AdvanceTo(2);
        Assert.Equal(0, pipe.UnconsumedLength);
    }
    #endregion

    #region 消费与所有权
    [Fact]
    [DisplayName("Pipe_整链消费_归还缓冲引用")]
    public void AdvanceTo_AllConsumed_ReleasesRefs()
    {
        using var pipe = new Pipe();
        var owner = new OwnerPacket(6);
        using var extra = owner.Slice(0, 2);

        Assert.Equal(2, owner.RefCount);
        pipe.Writer.Append(owner);
        Assert.Equal(2, owner.RefCount);

        pipe.Reader.AdvanceTo(6);

        Assert.Equal(1, extra.RefCount);   // 管道节点已归还，剩余切片持有
        Assert.Equal(0, owner.RefCount);   // 交给管道的句柄已释放
        Assert.Equal(0, pipe.UnconsumedLength);
    }

    [Fact]
    [DisplayName("Pipe_读侧结束_释放未消费数据")]
    public void ReaderComplete_ReleasesUnconsumed()
    {
        using var pipe = new Pipe();
        var owner = new OwnerPacket(8);
        using var extra = owner.Slice(0, 1);

        pipe.Writer.Append(owner);
        pipe.Reader.Complete();

        Assert.Equal(0, pipe.UnconsumedLength);
        Assert.Equal(1, extra.RefCount);

        // 读侧结束后，追加的数据直接被释放
        var late = new OwnerPacket(4);
        using var lateExtra = late.Slice(0, 1);
        pipe.Writer.Append(late);
        Assert.Equal(1, lateExtra.RefCount);
    }

    [Fact]
    [DisplayName("Pipe_越界消费_抛参数异常")]
    public void AdvanceTo_OutOfRange_Throws()
    {
        using var pipe = new Pipe();
        pipe.Writer.Append(new ArrayPacket(B(1)));

        Assert.Throws<ArgumentOutOfRangeException>(() => pipe.Reader.AdvanceTo(2));
        Assert.Throws<ArgumentOutOfRangeException>(() => pipe.Reader.AdvanceTo(-1));
    }
    #endregion

    #region 完成与取消
    [Fact]
    [DisplayName("Pipe_写侧完成_唤醒挂起读且可排空余量")]
    public async Task Complete_AwakesPendingRead_DrainsRemaining()
    {
        using var pipe = new Pipe();

        var vt = pipe.Reader.ReadAsync();
        Assert.False(vt.IsCompleted);

        pipe.Writer.Complete(new InvalidOperationException("closed"));

        var rr = await vt;
        Assert.True(rr.IsCompleted);
        Assert.NotNull(pipe.Error);

        // 完成后取剩余数据：读完即空
        var rr2 = await pipe.Reader.ReadAsync();
        Assert.True(rr2.IsCompleted);
        Assert.True(rr2.Buffer.IsEmpty);
    }

    [Fact]
    [DisplayName("Pipe_完成前余量_读取带完成标记")]
    public async Task Complete_WithBufferedData_ReadReturnsCompletedFlag()
    {
        using var pipe = new Pipe();
        pipe.Writer.Append(new ArrayPacket(B(7, 8)));
        pipe.Writer.Complete();

        var rr = await pipe.Reader.ReadAsync();
        Assert.True(rr.IsCompleted);
        Assert.Equal(B(7, 8), rr.Buffer.ToArray());

        pipe.Reader.AdvanceTo(2);
        var rr2 = await pipe.Reader.ReadAsync();
        Assert.True(rr2.IsCompleted);
        Assert.True(rr2.Buffer.IsEmpty);
    }

    [Fact]
    [DisplayName("Pipe_完成后追加_数据被释放")]
    public void AppendAfterComplete_Released()
    {
        using var pipe = new Pipe();
        pipe.Writer.Complete();

        var owner = new OwnerPacket(5);
        using var extra = owner.Slice(0, 1);
        pipe.Writer.Append(owner);

        Assert.Equal(1, extra.RefCount);
        Assert.Equal(0, pipe.UnconsumedLength);
    }

    [Fact]
    [DisplayName("Pipe_完成前已推进未提交_数据仍交付读侧")]
    public async Task Complete_CommitsAdvancedData()
    {
        using var pipe = new Pipe();

        // 只走 GetSpan/Advance，不 Flush
        var span = pipe.Writer.GetSpan(4);
        span[0] = 0x2A;
        pipe.Writer.Advance(1);

        pipe.Writer.Complete();

        // 对齐 BCL：Complete 先提交再结束，已 Advance 的数据不得静默丢弃
        var rr = await pipe.Reader.ReadAsync();
        Assert.True(rr.IsCompleted);
        Assert.Equal(1, rr.Buffer.Length);
        Assert.Equal(0x2A, rr.Buffer.First.Span[0]);
        pipe.Reader.AdvanceTo(rr.Buffer.Length);
    }

    [Fact]
    [DisplayName("Pipe_读侧结束后写入_返回已完成而不抛异常")]
    public async Task ReaderCompleted_WriteAsync_ReturnsCompleted()
    {
        using var pipe = new Pipe();

        // 读侧先结束（对端关闭／接收侧收尾）
        pipe.Reader.Complete();

        // 对齐 BCL：取窗口与写入不再抛异常，由返回结果告知已完成，调用方据此停止写入
        var span = pipe.Writer.GetSpan(4);
        Assert.True(span.Length >= 4);

        var fr = await pipe.Writer.WriteAsync(new Byte[] { 1, 2 });
        Assert.True(fr.IsCompleted);

        // 无人消费的数据直接释放
        Assert.Equal(0, pipe.UnconsumedLength);
    }

    [Fact]
    [DisplayName("Pipe_读侧结束后再读_抛无效操作异常")]
    public async Task ReaderCompleted_Read_Throws()
    {
        using var pipe = new Pipe();

        Assert.False(pipe.Reader.IsReaderCompleted);
        Assert.Null(pipe.Reader.Error);

        pipe.Reader.Complete();

        // 对齐 BCL：结束读取后再读是误用，不能静默降级成"流结束"
        Assert.True(pipe.Reader.IsReaderCompleted);
        Assert.Throws<InvalidOperationException>(() => pipe.Reader.TryRead(out _));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await pipe.Reader.ReadAsync());
    }

    [Fact]
    [DisplayName("Pipe_取消挂起读取_返回取消结果")]
    public async Task CancelPendingRead_ReturnsCanceled()
    {
        using var pipe = new Pipe();

        var vt = pipe.Reader.ReadAsync();
        pipe.Reader.CancelPendingRead();

        var rr = await vt;
        Assert.True(rr.IsCanceled);
        Assert.False(rr.IsCompleted);

        // 无挂起读取时的取消：下一次读取立即返回取消结果
        pipe.Reader.CancelPendingRead();
        var rr2 = await pipe.Reader.ReadAsync();
        Assert.True(rr2.IsCanceled);
    }

    [Fact]
    [DisplayName("Pipe_令牌取消_抛出取消异常")]
    public async Task ReadAsync_TokenCanceled_Throws()
    {
        using var pipe = new Pipe();
        using var cts = new CancellationTokenSource();

        var vt = pipe.Reader.ReadAsync(cts.Token);
        Assert.False(vt.IsCompleted);

        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await vt);
    }

    [Fact]
    [DisplayName("Pipe_读侧完成带异常_记录到管道错误且不被空异常覆盖")]
    public void ReaderComplete_WithError_RecordsError()
    {
        using var pipe = new Pipe();
        var ex = new InvalidOperationException("boom");

        pipe.Reader.CompleteAsync(ex).GetAwaiter().GetResult();

        Assert.Same(ex, pipe.Error);

        // 首个带异常的完成方胜出：写侧随后的空异常完成不覆盖已有错误
        pipe.Writer.Complete();
        Assert.Same(ex, pipe.Error);
    }

    [Fact]
    [DisplayName("Pipe_写侧完成带异常_读侧可查管道错误")]
    public async Task WriterComplete_WithError_ExposedOnPipe()
    {
        using var pipe = new Pipe();
        var ex = new InvalidOperationException("broken");

        pipe.Writer.Complete(ex);

        // 读侧只拿到“已结束”信号；故障原因从 Pipe.Error 取，调用方据此区分故障结束与优雅结束
        var rr = await pipe.Reader.ReadAsync();
        Assert.True(rr.IsCompleted);
        Assert.Same(ex, pipe.Error);
    }
    #endregion

    #region 背压
    [Fact]
    [DisplayName("Pipe_水位_暂停与恢复事件")]
    public void Backpressure_PauseAndResume()
    {
        using var pipe = new Pipe { PauseThreshold = 100, ResumeThreshold = 50 };
        var resumed = 0;
        pipe.Resumed += (s, e) => resumed++;

        pipe.Writer.Append(new ArrayPacket(new Byte[150]));
        Assert.True(pipe.IsPaused);

        // 消费到恢复水位以下：触发恢复
        pipe.Reader.AdvanceTo(120);
        Assert.False(pipe.IsPaused);
        Assert.Equal(1, resumed);
        Assert.Equal(30, pipe.UnconsumedLength);

        // 非暂停状态下继续消费：不再触发
        pipe.Reader.AdvanceTo(30);
        Assert.Equal(1, resumed);
        Assert.Equal(0, pipe.UnconsumedLength);
    }

    [Fact]
    [DisplayName("Pipe_水位_多次小步消费后恢复")]
    public void Backpressure_MultiStepDrain()
    {
        using var pipe = new Pipe { PauseThreshold = 3000, ResumeThreshold = 1000 };
        var resumed = 0;
        pipe.Resumed += (s, e) => resumed++;

        pipe.Writer.Append(new ArrayPacket(new Byte[4000]));
        Assert.True(pipe.IsPaused);

        // 多次小步消费：跨过恢复水位（<1000）的那一步起始长度已低于暂停水位，也必须触发恢复
        for (var i = 0; i < 7; i++)
        {
            pipe.Reader.AdvanceTo(500);
        }

        Assert.Equal(1, resumed);
        Assert.False(pipe.IsPaused);
        Assert.Equal(500, pipe.UnconsumedLength);

        // 再次涨到暂停水位：第二次暂停后小步消费仍应恢复
        pipe.Writer.Append(new ArrayPacket(new Byte[4000]));
        Assert.True(pipe.IsPaused);

        for (var i = 0; i < 9; i++)
        {
            pipe.Reader.AdvanceTo(500);
        }

        Assert.Equal(2, resumed);
        Assert.False(pipe.IsPaused);
    }

    [Fact]
    [DisplayName("Pipe_写侧回压_提交挂起_消费恢复后完成")]
    public async Task FlushAsync_WaitForResume_SuspendsUntilDrained()
    {
        using var pipe = new Pipe { PauseThreshold = 100, ResumeThreshold = 50 };

        var span = pipe.Writer.GetSpan(150);
        span.Clear();
        pipe.Writer.Advance(150);

        // 提交触发暂停水位：waitForResume 提交挂起
        var vt = pipe.Writer.FlushAsync(true);
        Assert.False(vt.IsCompleted);
        Assert.True(pipe.IsPaused);

        // 小步消费未达恢复水位：继续挂起
        pipe.Reader.AdvanceTo(80);
        Assert.True(pipe.IsPaused);
        Assert.False(vt.IsCompleted);
        Assert.Equal(70, pipe.UnconsumedLength);

        // 消费到恢复水位以下：恢复并完成提交
        pipe.Reader.AdvanceTo(40);
        var fr = await vt;
        Assert.False(fr.IsCompleted);
        Assert.False(fr.IsCanceled);
        Assert.False(pipe.IsPaused);
        Assert.Equal(30, pipe.UnconsumedLength);
    }

    [Fact]
    [DisplayName("Pipe_写侧回压_默认提交挂起_对齐BCL")]
    public async Task FlushAsync_Default_Suspended()
    {
        using var pipe = new Pipe { PauseThreshold = 100, ResumeThreshold = 50 };

        var span = pipe.Writer.GetSpan(150);
        span.Clear();
        pipe.Writer.Advance(150);

        // 默认提交对齐 BCL：达到暂停水位时挂起
        var vt = pipe.Writer.FlushAsync();
        Assert.False(vt.IsCompleted);
        Assert.True(pipe.IsPaused);

        // 消费降至恢复水位以下：提交完成
        pipe.Reader.AdvanceTo(120);
        var fr = await vt;
        Assert.False(fr.IsCompleted);
        Assert.False(fr.IsCanceled);
        Assert.False(pipe.IsPaused);
        Assert.Equal(30, pipe.UnconsumedLength);
    }

    [Fact]
    [DisplayName("Pipe_写侧回压_取消挂起提交_返回取消结果")]
    public async Task CancelPendingFlush_WakesWithCanceled()
    {
        using var pipe = new Pipe { PauseThreshold = 100, ResumeThreshold = 50 };

        pipe.Writer.Append(new ArrayPacket(new Byte[150]));
        var vt = pipe.Writer.FlushAsync(true);
        Assert.False(vt.IsCompleted);

        pipe.Writer.CancelPendingFlush();

        var fr = await vt;
        Assert.True(fr.IsCanceled);
        Assert.False(fr.IsCompleted);

        // 取消只影响等待，不影响已提交数据
        Assert.Equal(150, pipe.UnconsumedLength);

        // 取消后可继续使用：排空后新提交正常完成
        pipe.Reader.AdvanceTo(150);
        var fr2 = await pipe.Writer.FlushAsync(true);
        Assert.False(fr2.IsCanceled);
    }

    [Fact]
    [DisplayName("Pipe_写侧回压_无挂起提交时取消_下一次提交标记取消一次")]
    public async Task CancelPendingFlush_NoPending_NextSubmitCanceledOnce()
    {
        using var pipe = new Pipe { PauseThreshold = 100, ResumeThreshold = 50 };

        // 无挂起提交时取消：暂存，由下一次提交消费（对齐 BCL）
        pipe.Writer.CancelPendingFlush();

        pipe.Writer.Append(new ArrayPacket(new Byte[150]));
        var fr = await pipe.Writer.FlushAsync(true).AsTask().WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(fr.IsCanceled);

        // 数据照常提交（只是不再等待水位）
        Assert.Equal(150, pipe.UnconsumedLength);
        pipe.Reader.AdvanceTo(150);

        // 只生效一次：下一次提交不再被取消
        pipe.Writer.Append(new ArrayPacket(new Byte[10]));
        var fr2 = await pipe.Writer.FlushAsync(true).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.False(fr2.IsCanceled);
        Assert.Equal(10, pipe.UnconsumedLength);
    }

    [Fact]
    [DisplayName("Pipe_写侧回压_令牌取消_抛出取消异常")]
    public async Task FlushAsync_TokenCanceled_Throws()
    {
        using var pipe = new Pipe { PauseThreshold = 100, ResumeThreshold = 50 };
        using var cts = new CancellationTokenSource();

        pipe.Writer.Append(new ArrayPacket(new Byte[150]));
        var vt = pipe.Writer.FlushAsync(true, cts.Token);
        Assert.False(vt.IsCompleted);

        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await vt);
    }

    [Fact]
    [DisplayName("Pipe_写侧回压_读侧结束_唤醒挂起提交")]
    public async Task FlushAsync_ReaderComplete_WakesPending()
    {
        using var pipe = new Pipe { PauseThreshold = 100, ResumeThreshold = 50 };

        pipe.Writer.Append(new ArrayPacket(new Byte[150]));
        var vt = pipe.Writer.FlushAsync(true);
        Assert.False(vt.IsCompleted);

        pipe.Reader.Complete();

        var fr = await vt;
        Assert.True(fr.IsCompleted);
        Assert.False(fr.IsCanceled);
    }

    [Fact]
    [DisplayName("Pipe_写侧回压_写侧结束_唤醒挂起提交")]
    public async Task FlushAsync_WriterComplete_WakesPending()
    {
        using var pipe = new Pipe { PauseThreshold = 100, ResumeThreshold = 50 };

        pipe.Writer.Append(new ArrayPacket(new Byte[150]));
        var vt = pipe.Writer.FlushAsync(true);
        Assert.False(vt.IsCompleted);

        pipe.Writer.Complete();

        var fr = await vt;
        Assert.True(fr.IsCompleted);
        Assert.False(fr.IsCanceled);
    }

    [Fact]
    [DisplayName("Pipe_写侧回压_重复挂起提交_抛无效操作异常")]
    public async Task FlushAsync_SecondPending_Throws()
    {
        using var pipe = new Pipe { PauseThreshold = 100, ResumeThreshold = 50 };

        pipe.Writer.Append(new ArrayPacket(new Byte[150]));
        var vt = pipe.Writer.FlushAsync(true);
        Assert.False(vt.IsCompleted);

        Assert.Throws<InvalidOperationException>(() => { pipe.Writer.FlushAsync(true); });

        // 排空后原挂起提交正常完成
        pipe.Reader.AdvanceTo(150);
        var fr = await vt;
        Assert.False(fr.IsCanceled);
    }

    [Fact]
    [DisplayName("Pipe_只检查不消费_写侧提交不被回压锁死")]
    public async Task Backpressure_ExaminedWithoutConsumed_SubmitProceeds()
    {
        using var pipe = new Pipe { PauseThreshold = 1024, ResumeThreshold = 512 };
        pipe.Writer.Append(new ArrayPacket(new Byte[2048]));
        Assert.True(pipe.IsPaused);

        // 只检查不消费：帧未凑齐时上层只看一眼就等更多数据（MessagePump 的文档化用法）
        var rr = await pipe.Reader.ReadAsync();
        pipe.Reader.AdvanceTo(0, rr.Buffer.Length);

        // 已检查的字节不再计入背压，写侧提交立即完成（超时保护：锁死则失败的用例不会被挂死）
        Assert.False(pipe.IsPaused);
        var fr = await pipe.Writer.FlushAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.False(fr.IsCanceled);
    }
    #endregion

    #region 写入缓冲
    [Fact]
    [DisplayName("Pipe_写入缓冲_Span推进提交后按序可读")]
    public async Task Writer_SpanAdvanceFlush()
    {
        using var pipe = new Pipe();

        // 第一次写入 8 字节并提交
        var span = pipe.Writer.GetSpan(8);
        "01234567".GetBytes().CopyTo(span);
        pipe.Writer.Advance(8);
        Assert.Equal(8, pipe.Writer.UnflushedBytes);

        var fr = await pipe.Writer.FlushAsync();
        Assert.False(fr.IsCompleted);
        Assert.Equal(0, pipe.Writer.UnflushedBytes);

        // 第二次写入 12 字节（sizeHint=0 任意窗口）
        span = pipe.Writer.GetSpan();
        "abcdefghijkl".GetBytes().CopyTo(span);
        pipe.Writer.Advance(12);
        await pipe.Writer.FlushAsync();

        var rr = await pipe.Reader.ReadAsync();
        Assert.Equal(20, rr.Buffer.Length);
        Assert.Equal("01234567abcdefghijkl", rr.Buffer.ToArray().ToStr());
        pipe.Reader.AdvanceTo(20);
    }

    [Fact]
    [DisplayName("Pipe_写入缓冲_IBufferWriter形态_多次提交成链")]
    public async Task Writer_AsBufferWriter()
    {
        using var pipe = new Pipe();
        IBufferWriter<Byte> writer = pipe.Writer;

        // 超过默认段大小，触发扩租
        var payload = new Byte[9000];
        Random.Shared.NextBytes(payload);
        payload.CopyTo(writer.GetSpan(payload.Length));
        writer.Advance(payload.Length);
        Assert.Equal(9000, pipe.Writer.UnflushedBytes);
        await pipe.Writer.FlushAsync();

        // 第二段小数据，形成链式窗口
        var tail = B(7, 8, 9);
        tail.CopyTo(writer.GetSpan(tail.Length));
        writer.Advance(tail.Length);
        await pipe.Writer.FlushAsync();

        var rr = await pipe.Reader.ReadAsync();
        Assert.Equal(9003, rr.Buffer.Length);
        Assert.False(rr.Buffer.IsSingleSegment);

        var all = rr.Buffer.ToArray();
        Assert.Equal(payload, all[..9000]);
        Assert.Equal(tail, all[9000..]);
        pipe.Reader.AdvanceTo(9003);
    }

    [Fact]
    [DisplayName("Pipe_写入缓冲_读侧结束_提交已完成且写入不再抛错")]
    public async Task Writer_CompletedPipe()
    {
        using var pipe = new Pipe();

        // 读侧结束（对端关闭／接收侧收尾）
        pipe.Reader.Complete();

        var fr = await pipe.Writer.FlushAsync();
        Assert.True(fr.IsCompleted);

        // 对齐 BCL：读侧结束后仍允许取窗口写入，由提交结果告知已完成，调用方据此停止写入
        Assert.True(pipe.Writer.GetSpan(4).Length >= 4);
        Assert.True((await pipe.Writer.WriteAsync(new Byte[] { 1 })).IsCompleted);
    }

    [Fact]
    [DisplayName("Pipe_写入缓冲_写侧自己结束后写入抛错")]
    public void Writer_OwnCompleted_WriteThrows()
    {
        using var pipe = new Pipe();

        // 写侧自己结束（读侧仍活着）：再取窗口属误用，抛异常（对齐 BCL）
        pipe.Writer.Complete();

        Assert.Throws<InvalidOperationException>(() => pipe.Writer.GetSpan(4));
    }

    [Fact]
    [DisplayName("Pipe_写入缓冲_递进越界_抛参数异常")]
    public void Writer_AdvanceOutOfRange()
    {
        using var pipe = new Pipe();
        pipe.Writer.GetSpan(4);

        // 超过最近一次返回窗口长度
        Assert.Throws<ArgumentOutOfRangeException>(() => pipe.Writer.Advance(5000));
    }

    [Fact]
    [DisplayName("Pipe_写入缓冲_WriteAsync_写入即提交")]
    public async Task Writer_WriteAsync()
    {
        using var pipe = new Pipe();

        // 写入即提交（等价 GetSpan + 拷贝 + Advance + FlushAsync）
        var data = "hello-pipe".GetBytes();
        var fr = await pipe.Writer.WriteAsync(data);
        Assert.False(fr.IsCompleted);
        Assert.Equal(0, pipe.Writer.UnflushedBytes);

        var rr = await pipe.Reader.ReadAsync();
        Assert.Equal(data, rr.Buffer.ToArray());
        pipe.Reader.AdvanceTo(rr.Buffer.Length);

        // 空数据：只提交，不产生数据
        await pipe.Writer.WriteAsync(ReadOnlyMemory<Byte>.Empty);
        Assert.Equal(0, pipe.UnconsumedLength);
    }
    #endregion

    #region 位置推进
    [Fact]
    [DisplayName("PipeReader_TryRead_无数据false_取消与完成态true")]
    public void Reader_TryRead()
    {
        using var pipe = new Pipe();

        Assert.False(pipe.Reader.TryRead(out _));

        pipe.Writer.Append(new ArrayPacket(B(1, 2, 3)));
        Assert.True(pipe.Reader.TryRead(out var rr));
        Assert.Equal(3, rr.Buffer.Length);
        Assert.False(rr.IsCompleted);
        pipe.Reader.AdvanceTo(3);

        // 取消态：返回 IsCanceled 并清除标志
        pipe.Reader.CancelPendingRead();
        Assert.True(pipe.Reader.TryRead(out rr));
        Assert.True(rr.IsCanceled);
        Assert.False(pipe.Reader.TryRead(out _));

        // 完成态：返回残余窗口且 IsCompleted
        pipe.Writer.Append(new ArrayPacket(B(4, 5)));
        pipe.Writer.Complete();
        Assert.True(pipe.Reader.TryRead(out rr));
        Assert.Equal(2, rr.Buffer.Length);
        Assert.True(rr.IsCompleted);
    }

    [Fact]
    [DisplayName("PipeReader_位置推进_与字节计数等价_过期位置抛错")]
    public async Task Reader_AdvanceToPosition()
    {
        using var pipe = new Pipe();
        pipe.Writer.Append(new ArrayPacket(B(1, 2, 3, 4, 5)));

        var rr = await pipe.Reader.ReadAsync();
        Assert.Equal(5, rr.Buffer.Length);

        // 消费 2 字节（位置取自最近窗口）
        pipe.Reader.AdvanceTo(rr.Buffer.GetPosition(2));
        Assert.Equal(3, pipe.UnconsumedLength);

        // examined 位置版：只标记不消费，下一次读取挂起等追加
        rr = await pipe.Reader.ReadAsync();
        Assert.Equal(3, rr.Buffer.Length);
        pipe.Reader.AdvanceTo(rr.Buffer.GetPosition(0), rr.Buffer.GetPosition(3));
        var vt = pipe.Reader.ReadAsync();
        Assert.False(vt.IsCompleted);

        // 追加后唤醒，共 4 字节
        pipe.Writer.Append(new ArrayPacket(B(6)));
        rr = await vt.AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(4, rr.Buffer.Length);
        Assert.Equal(B(3, 4, 5, 6), rr.Buffer.ToArray());

        // 消费完后旧位置（窗口已重建）抛 InvalidOperationException
        var stale = rr.Buffer.GetPosition(0);
        pipe.Reader.AdvanceTo(rr.Buffer.GetPosition(4));
        Assert.Throws<InvalidOperationException>(() => pipe.Reader.AdvanceTo(stale));
    }
    #endregion

    #region 切帧与限长
    [Fact]
    [DisplayName("Pipe_两参推进_已检查部分等待新数据")]
    public async Task AdvanceTo_ExaminedWaitsForMore()
    {
        using var pipe = new Pipe();
        pipe.Writer.Append(new ArrayPacket(B(1, 2, 3)));

        var rr = await pipe.Reader.ReadAsync();
        Assert.Equal(B(1, 2, 3), rr.Buffer.ToArray());

        // 全部检查过但不足使用：不消费，等待追加
        pipe.Reader.AdvanceTo(0, 3);
        var vt = pipe.Reader.ReadAsync();
        Assert.False(vt.IsCompleted);

        pipe.Writer.Append(new ArrayPacket(B(4)));
        var rr2 = await vt;
        Assert.Equal(B(1, 2, 3, 4), rr2.Buffer.ToArray());

        pipe.Reader.AdvanceTo(4);
        Assert.Equal(0, pipe.UnconsumedLength);
    }

    [Fact]
    [DisplayName("Pipe_切帧_拥有切片独立存活")]
    public void TakeFrame_OwnedSliceOutlivesPipe()
    {
        using var pipe = new Pipe();
        var owner = new OwnerPacket(5);
        var span = owner.GetSpan();
        for (var i = 0; i < span.Length; i++) span[i] = (Byte)(i + 1);

        pipe.Writer.Append(owner);

        var frame = pipe.Reader.TakeFrame(3);
        Assert.Equal(3, frame.Total);
        Assert.Equal(2, pipe.UnconsumedLength);

        // 结束读取释放管道全部引用后，帧（拥有切片）仍可读
        pipe.Reader.Complete();
        Assert.Equal(B(1, 2, 3), frame.AsReadOnlySequence().ToArray());

        frame.TryDispose();
    }

    [Fact]
    [DisplayName("Pipe_切帧越界_抛参数异常")]
    public void TakeFrame_OutOfRange_Throws()
    {
        using var pipe = new Pipe();
        pipe.Writer.Append(new ArrayPacket(B(1, 2)));

        Assert.Throws<ArgumentOutOfRangeException>(() => pipe.Reader.TakeFrame(3));
        Assert.Throws<ArgumentOutOfRangeException>(() => pipe.Reader.TakeFrame(-1));
    }

    [Fact]
    [DisplayName("Pipe_限长读取_窗口裁剪与预算扣减")]
    public async Task Limit_ClipsWindowToBudget()
    {
        using var pipe = new Pipe();
        pipe.Writer.Append(new ArrayPacket(B(1, 2, 3, 4, 5, 6, 7, 8)));

        var body = pipe.Reader.Limit(3);
        var rr = await body.ReadAsync();
        Assert.Equal(B(1, 2, 3), rr.Buffer.ToArray());

        body.AdvanceTo(3);
        Assert.Equal(0, body.Remaining);

        // 预算耗尽：读取返回已结束的空结果；超出预算的推进抛异常
        var rr2 = await body.ReadAsync();
        Assert.True(rr2.IsCompleted);
        Assert.True(rr2.Buffer.IsEmpty);
        Assert.Throws<ArgumentOutOfRangeException>(() => body.AdvanceTo(1));

        // 主读取器仅前移 3 字节
        Assert.Equal(5, pipe.UnconsumedLength);
    }

    [Fact]
    [DisplayName("Pipe_限长读取_Drain丢弃余量对齐")]
    public async Task Limit_DrainSkipsRemainder()
    {
        using var pipe = new Pipe();
        pipe.Writer.Append(new ArrayPacket(B(1, 2, 3, 4, 5, 6, 7, 8, 9, 10)));

        var body = pipe.Reader.Limit(4);
        var rr = await body.ReadAsync();
        Assert.Equal(B(1, 2, 3, 4), rr.Buffer.ToArray());

        // 只读 2 字节，其余丢弃
        body.AdvanceTo(2);
        await body.DrainAsync();

        Assert.Equal(0, body.Remaining);
        Assert.Equal(6, pipe.UnconsumedLength);
        Assert.Equal(B(5, 6, 7, 8, 9, 10), pipe.Reader.Buffer.ToArray());
    }
    #endregion

    #region 取消
    [Fact]
    [DisplayName("Pipe_取消令牌结束挂起读_读取器仍可用")]
    public async Task ReadAsync_TokenCanceled_ReaderReusable()
    {
        using var pipe = new Pipe();
        using var cts = new CancellationTokenSource();

        var task = pipe.Reader.ReadAsync(cts.Token).AsTask();
        Assert.False(task.IsCompleted);

        // 取消令牌触发：挂起读以取消结束（抛 OperationCanceledException）
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);

        // 取消后读取器仍可用：新读取正常挂起，追加数据后完成
        var vt = pipe.Reader.ReadAsync();
        Assert.False(vt.IsCompleted);

        pipe.Writer.Append(new ArrayPacket(B(1, 2, 3)));
        var rr = await vt;
        Assert.Equal(B(1, 2, 3), rr.Buffer.ToArray());
    }

    [Fact]
    [DisplayName("Pipe_已取消令牌_立即取消且读取器可用")]
    public async Task ReadAsync_PreCanceledToken_ImmediateCancel()
    {
        using var pipe = new Pipe();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // 令牌已取消：注册回调同步触发，任务立即以取消结束
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pipe.Reader.ReadAsync(cts.Token).AsTask());

        // 读取器保持可用，且不残留挂起状态
        pipe.Writer.Append(new ArrayPacket(B(7)));
        var rr = await pipe.Reader.ReadAsync();
        Assert.Equal(B(7), rr.Buffer.ToArray());
    }

    [Fact]
    [DisplayName("Pipe_取消挂起读_返回取消结果")]
    public async Task CancelPendingRead_PendingRead_ReturnsCanceled()
    {
        using var pipe = new Pipe();

        var vt = pipe.Reader.ReadAsync();
        Assert.False(vt.IsCompleted);

        pipe.Reader.CancelPendingRead();

        var rr = await vt;
        Assert.True(rr.IsCanceled);
        Assert.True(rr.Buffer.IsEmpty);
    }

    [Fact]
    [DisplayName("Pipe_无挂起读时取消_下一次读取立即取消且只生效一次")]
    public async Task CancelPendingRead_NoPending_NextReadCanceled()
    {
        using var pipe = new Pipe();

        pipe.Reader.CancelPendingRead();

        var rr = await pipe.Reader.ReadAsync();
        Assert.True(rr.IsCanceled);

        // 取消暂存只生效一次：随后的读取恢复正常
        pipe.Writer.Append(new ArrayPacket(B(9)));
        var rr2 = await pipe.Reader.ReadAsync();
        Assert.False(rr2.IsCanceled);
        Assert.Equal(B(9), rr2.Buffer.ToArray());
    }

    [Fact]
    [DisplayName("Pipe_令牌取消后再取消挂起_下一次读取仍立即取消")]
    public async Task CancelPendingRead_AfterTokenCanceled_NextReadCanceled()
    {
        using var pipe = new Pipe();
        using var cts = new CancellationTokenSource();

        // 挂起读先被取消令牌定案
        var task = pipe.Reader.ReadAsync(cts.Token).AsTask();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);

        // 残留等待者已被取消定案，取消信号不得就此丢失：下一次读取仍应立即返回取消结果
        pipe.Reader.CancelPendingRead();

        var rr = await pipe.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(rr.IsCanceled);
    }
    #endregion

    #region 对齐 BCL 增强
    [Fact]
    [DisplayName("Pipe_ReadAtLeastAsync_不足时等待追加直到满足")]
    public async Task ReadAtLeastAsync_WaitsUntilEnough()
    {
        using var pipe = new Pipe();

        pipe.Writer.Append(new ArrayPacket(B(1, 2, 3)));

        var vt = pipe.Reader.ReadAtLeastAsync(5);
        Assert.False(vt.IsCompleted);

        // 追加足量：唤醒并返回完整窗口（不消费）
        pipe.Writer.Append(new ArrayPacket(B(4, 5, 6, 7)));
        var rr = await vt;
        Assert.Equal(7, rr.Buffer.Length);
        Assert.False(rr.IsCompleted);
        Assert.False(rr.IsCanceled);
        pipe.Reader.AdvanceTo(7);
    }

    [Fact]
    [DisplayName("Pipe_ReadAtLeastAsync_结束未足量_返回完成")]
    public async Task ReadAtLeastAsync_CompletedBelowMin()
    {
        using var pipe = new Pipe();

        pipe.Writer.Append(new ArrayPacket(B(1, 2)));
        pipe.Writer.Complete();

        var rr = await pipe.Reader.ReadAtLeastAsync(10);
        Assert.True(rr.IsCompleted);
        Assert.Equal(2, rr.Buffer.Length);
        pipe.Reader.AdvanceTo(2);
    }

    [Fact]
    [DisplayName("Pipe_ReadAtLeastAsync_负数_抛参数异常")]
    public void ReadAtLeastAsync_Negative_Throws()
    {
        using var pipe = new Pipe();

        Assert.Throws<ArgumentOutOfRangeException>(() => pipe.Reader.ReadAtLeastAsync(-1));
    }

    [Fact]
    [DisplayName("Pipe_限长读取_TryRead_同步与预算语义")]
    public void Limit_TryRead()
    {
        using var pipe = new Pipe();
        pipe.Writer.Append(new ArrayPacket(B(1, 2, 3, 4, 5, 6, 7, 8)));

        var body = pipe.Reader.Limit(3);

        // 流式模式：窗口内有数据，同步尝试立即可得（不等待）
        Assert.True(body.TryRead(out var rr));
        Assert.Equal(B(1, 2, 3), rr.Buffer.ToArray());

        body.AdvanceTo(3);
        Assert.Equal(0, body.Remaining);

        // 预算耗尽：返回完成空结果
        Assert.True(body.TryRead(out var rr2));
        Assert.True(rr2.IsCompleted);
        Assert.True(rr2.Buffer.IsEmpty);

        // 无数据且未结束：返回 false
        using var pipe2 = new Pipe();
        var body2 = pipe2.Reader.Limit(2);
        Assert.False(body2.TryRead(out _));
    }

    [Fact]
    [DisplayName("Pipe_AsStream_写入提交读取回环")]
    public async Task AsStream_RoundTrip()
    {
        using var pipe = new Pipe();

        using var ws = pipe.Writer.AsStream();
        using var rs = pipe.Reader.AsStream();

        await ws.WriteAsync(B(11, 22, 33), 0, 3);
        ws.Flush();

        var buf = new Byte[5];
        var n = await rs.ReadAsync(buf, 0, buf.Length);
        Assert.Equal(3, n);
        Assert.Equal(B(11, 22, 33), buf[..3]);
    }

    [Fact]
    [DisplayName("Pipe_AsStream_Dispose结束管道_leaveOpen保留")]
    public void AsStream_DisposeSemantics()
    {
        using var pipe1 = new Pipe();
        var w1 = pipe1.Writer.AsStream(leaveOpen: true);
        w1.Dispose();
        Assert.False(pipe1.IsCompleted);

        using var pipe2 = new Pipe();
        var w2 = pipe2.Writer.AsStream();
        w2.Dispose();
        Assert.True(pipe2.IsCompleted);

        // 读侧流 Dispose 结束读取：残余未消费数据释放、管道收尾
        using var pipe3 = new Pipe();
        pipe3.Writer.Append(new ArrayPacket(B(1, 2, 3)));
        var r3 = pipe3.Reader.AsStream();
        r3.Dispose();
        Assert.True(pipe3.IsCompleted);
    }

    [Fact]
    [DisplayName("Pipe_Reset_未完成_抛无效操作")]
    public void Reset_NotCompleted_Throws()
    {
        using var pipe = new Pipe();

        Assert.Throws<InvalidOperationException>(() => pipe.Reset());
    }

    [Fact]
    [DisplayName("Pipe_Reset_两端完成后复位可复用")]
    public async Task Reset_Reuse()
    {
        using var pipe = new Pipe();

        // 第一轮：写入并消费，两端结束
        pipe.Writer.Append(new ArrayPacket(B(1, 2, 3)));
        var rr = await pipe.Reader.ReadAsync();
        Assert.Equal(3, rr.Buffer.Length);
        pipe.Reader.AdvanceTo(3);

        pipe.Writer.Complete();
        pipe.Reader.Complete();
        Assert.True(pipe.IsCompleted);

        // 复位后：完成状态清零，读写句柄可复用
        pipe.Reset();
        Assert.False(pipe.IsCompleted);
        Assert.False(pipe.IsPaused);

        pipe.Writer.Append(new ArrayPacket(B(9, 9)));
        var rr2 = await pipe.Reader.ReadAsync();
        Assert.Equal(2, rr2.Buffer.Length);
        pipe.Reader.AdvanceTo(2);
    }

    [Fact]
    [DisplayName("Pipe_复位_清空残留取消暂存")]
    public async Task Reset_ClearsPendingCancel()
    {
        using var pipe = new Pipe();

        // 无挂起读取时的取消暂存，随后两端结束并复位
        pipe.Reader.CancelPendingRead();
        pipe.Writer.Complete();
        pipe.Reader.Complete();
        pipe.Reset();

        // 复位后第一次读取不得立即返回取消结果（否则复用管道会凭空取消一次读取）
        var vt = pipe.Reader.ReadAsync();
        Assert.False(vt.IsCompleted);

        pipe.Writer.Append(new ArrayPacket(B(1, 2, 3)));
        var rr = await vt;
        Assert.False(rr.IsCanceled);
        Assert.Equal(B(1, 2, 3), rr.Buffer.ToArray());
    }

    [Fact]
    [DisplayName("Pipe_复位_清空已检查游标_新数据可被 TryRead 看到")]
    public async Task Reset_ClearsExaminedCursor()
    {
        using var pipe = new Pipe();

        // 制造大值已检查游标：只检查不消费
        pipe.Writer.Append(new ArrayPacket(new Byte[150]));
        var rr = await pipe.Reader.ReadAsync();
        pipe.Reader.AdvanceTo(0, rr.Buffer.Length);

        pipe.Writer.Complete();
        pipe.Reader.Complete();
        pipe.Reset();

        // 复位后复用：新追加的数据必须能被 TryRead 看到（不得因复用的游标/窗口状态误判无数据）
        pipe.Writer.Append(new ArrayPacket(B(7, 8)));
        Assert.True(pipe.Reader.TryRead(out var tr));
        Assert.Equal(B(7, 8), tr.Buffer.ToArray());
    }

    [Fact]
    [DisplayName("Pipe_复位_读侧结束标记与错误一并清除")]
    public async Task Reset_ClearsReaderCompletedAndError()
    {
        using var pipe = new Pipe();

        var error = new InvalidOperationException("boom");
        pipe.Writer.Append(new ArrayPacket(B(1)));
        var rr = await pipe.Reader.ReadAsync();
        pipe.Reader.AdvanceTo(1);

        pipe.Writer.Complete(error);
        pipe.Reader.Complete();
        Assert.True(pipe.Reader.IsReaderCompleted);
        Assert.Same(error, pipe.Reader.Error);

        pipe.Reset();

        // 读侧结束标记必须清零：否则复用后的第一次读取会判定"读侧已结束"而抛出无效操作
        Assert.False(pipe.Reader.IsReaderCompleted);
        Assert.Null(pipe.Reader.Error);

        // 复用管道照常工作，且不再受上一轮错误影响
        pipe.Writer.Append(new ArrayPacket(B(4, 5)));
        var rr2 = await pipe.Reader.ReadAsync();
        Assert.Equal(B(4, 5), rr2.Buffer.ToArray());
        pipe.Reader.AdvanceTo(2);
    }
    #endregion
}
