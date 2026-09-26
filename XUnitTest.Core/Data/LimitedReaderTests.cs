using System;
using System.Buffers;
using System.ComponentModel;
using System.Threading.Tasks;
using NewLife;
using NewLife.Data;
using Xunit;

namespace XUnitTest.Data;

/// <summary>限长读取器测试。管道流式模式由 PipeTests 的 Limit 用例覆盖，此处补内存模式（整帧解析）的窗口、预算、复位与取值语义</summary>
public class LimitedReaderTests
{
    private static Byte[] B(params Int32[] values)
    {
        var buf = new Byte[values.Length];
        for (var i = 0; i < values.Length; i++) buf[i] = (Byte)values[i];

        return buf;
    }

    [Fact]
    [DisplayName("限长读取_内存模式_窗口裁剪到预算内")]
    public async Task MemoryMode_BufferWithinBudget()
    {
        var packet = new ArrayPacket(B(1, 2, 3, 4, 5));
        var body = new LimitedReader(packet, 1, 3);

        Assert.False(body.IsStreaming);
        Assert.Equal(3, body.Remaining);
        Assert.Equal(B(2, 3, 4), body.Buffer.ToArray());

        // 数据已在内存：读取立即完成，且带“结束”标记
        var rr = await body.ReadAsync();
        Assert.True(rr.IsCompleted);
        Assert.False(rr.IsCanceled);
        Assert.Equal(B(2, 3, 4), rr.Buffer.ToArray());

        // 读取不消费：预算不变
        Assert.Equal(3, body.Remaining);
    }

    [Fact]
    [DisplayName("限长读取_内存模式_推进扣减预算")]
    public async Task MemoryMode_AdvanceDecrementsBudget()
    {
        var packet = new ArrayPacket(B(1, 2, 3, 4, 5));
        var body = new LimitedReader(packet, 1, 3);

        body.AdvanceTo(1);
        Assert.Equal(2, body.Remaining);
        Assert.Equal(B(3, 4), body.Buffer.ToArray());

        var rr = await body.ReadAsync();
        Assert.Equal(B(3, 4), rr.Buffer.ToArray());
    }

    [Fact]
    [DisplayName("限长读取_推进超出预算_抛参数异常")]
    public void Advance_BeyondBudget_Throws()
    {
        var packet = new ArrayPacket(B(1, 2, 3, 4, 5));
        var body = new LimitedReader(packet, 1, 2);

        Assert.Throws<ArgumentOutOfRangeException>(() => body.AdvanceTo(3));
        Assert.Throws<ArgumentOutOfRangeException>(() => body.AdvanceTo(-1));
    }

    [Fact]
    [DisplayName("限长读取_预算耗尽_读取返回已结束空结果")]
    public async Task BudgetExhausted_ReturnsCompletedEmpty()
    {
        var packet = new ArrayPacket(B(1, 2, 3));
        var body = new LimitedReader(packet, 0, 2);

        body.AdvanceTo(2);
        Assert.Equal(0, body.Remaining);
        Assert.Null(body.AsPacket());

        var rr = await body.ReadAsync();
        Assert.True(rr.IsCompleted);
        Assert.True(rr.Buffer.IsEmpty);

        // 同步形态与异步一致
        Assert.True(body.TryRead(out var sync));
        Assert.True(sync.IsCompleted);
        Assert.True(sync.Buffer.IsEmpty);
    }

    [Fact]
    [DisplayName("限长读取_内存模式_AsPacket为剩余体视图且不消费")]
    public void MemoryMode_AsPacketView()
    {
        var packet = new ArrayPacket(B(1, 2, 3, 4, 5));
        var body = new LimitedReader(packet, 1, 4);

        var view = body.AsPacket();
        Assert.NotNull(view);
        Assert.Equal(B(2, 3, 4, 5), view!.ToArray());

        // 取视图不消费
        body.AdvanceTo(2);
        Assert.Equal(2, body.Remaining);
        Assert.Equal(B(4, 5), body.AsPacket()!.ToArray());
    }

    [Fact]
    [DisplayName("限长读取_内存模式_ReadAllAsync读满并推进到帧尾")]
    public async Task MemoryMode_ReadAll()
    {
        var packet = new ArrayPacket(B(1, 2, 3, 4, 5));
        var body = new LimitedReader(packet, 1, 3);

        var all = await body.ReadAllAsync();
        Assert.Equal(B(2, 3, 4), all.ToArray());
        Assert.Equal(0, body.Remaining);
    }

    [Fact]
    [DisplayName("限长读取_内存模式_ReadAllAsync后不可重复读")]
    public async Task MemoryMode_ReadAllConsumes()
    {
        var packet = new ArrayPacket(B(1, 2, 3));
        var body = new LimitedReader(packet, 0, 3);

        var first = await body.ReadAllAsync();
        Assert.Equal(B(1, 2, 3), first.ToArray());

        // 已推进到帧尾：再读只得空包
        var second = await body.ReadAllAsync();
        Assert.Equal(0, second.Length);
    }

    [Fact]
    [DisplayName("限长读取_内存模式_复位后可重复读")]
    public async Task MemoryMode_Reset()
    {
        var packet = new ArrayPacket(B(1, 2, 3, 4, 5));
        var body = new LimitedReader(packet, 1, 3);

        var all = await body.ReadAllAsync();
        Assert.Equal(B(2, 3, 4), all.ToArray());

        // 事件链消费完消息体后、交付等待方前复位：内容可被重复读取
        body.Reset();
        Assert.Equal(3, body.Remaining);

        var again = await body.ReadAllAsync();
        Assert.Equal(B(2, 3, 4), again.ToArray());
    }

    [Fact]
    [DisplayName("限长读取_内存模式_Drain丢弃余量")]
    public async Task MemoryMode_Drain()
    {
        var packet = new ArrayPacket(B(1, 2, 3, 4, 5));
        var body = new LimitedReader(packet, 1, 4);

        await body.DrainAsync();

        Assert.Equal(0, body.Remaining);
        Assert.Null(body.AsPacket());
    }

    [Fact]
    [DisplayName("限长读取_流式模式_取包与复位被拒绝")]
    public void StreamingMode_AsPacketAndResetRejected()
    {
        using var pipe = new Pipe();
        var body = pipe.Reader.Limit(4);

        Assert.True(body.IsStreaming);
        Assert.Equal(4, body.Remaining);

        // 流式体的数据由管道承载、不可重放，也不允许直接取包
        Assert.Throws<InvalidOperationException>(() => body.AsPacket());
        Assert.Throws<InvalidOperationException>(() => body.Reset());
    }

    [Fact]
    [DisplayName("限长读取_构造函数_非法参数被拒绝")]
    public void Ctor_InvalidArguments_Throws()
    {
        var packet = new ArrayPacket(B(1, 2, 3));

        Assert.Throws<ArgumentNullException>(() => new LimitedReader((IPacket)null!, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new LimitedReader(packet, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new LimitedReader(packet, 0, -1));
    }
}
