using System;
using System.Buffers;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using NewLife.Buffers;
using NewLife.Data;
using Xunit;

namespace XUnitTest.Buffers;

/// <summary>SpanReader 生命周期测试：流式扩容借出的池缓冲必须能归还、且不影响调用方句柄</summary>
public class SpanReaderLifecycleTests
{
    /// <summary>构造一个需要扩容的流式读取器并读一格，触发从池借缓冲</summary>
    private static SpanReader OpenStreamReader(MemoryStream ms)
    {
        var reader = new SpanReader(ms, null, 1024);
        _ = reader.ReadByte();

        return reader;
    }

    [Fact]
    [DisplayName("流式读取器_释放后缓冲归还池_不再持续借出")]
    public void Dispose_ReturnsBufferToPool()
    {
        var ms = new MemoryStream(new Byte[1024]);

        // 预热：先把池里该桶的数组消耗掉
        for (var i = 0; i < 8; i++)
        {
            var warm = OpenStreamReader(ms);
            warm.Dispose();
            ms.Position = 0;
        }

        const Int32 N = 16;
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < N; i++)
        {
            var reader = OpenStreamReader(ms);
            reader.Dispose();
            ms.Position = 0;
        }
        var used = (GC.GetAllocatedBytesForCurrentThread() - before) / N;

        // 归还到位时只剩 OwnerPacket/ArrayOwner 两个小对象（约 80 B）；未归还则会每轮再漏一块 1KB 池数组（约 1128 B）
        Assert.True(used < 256, $"每轮分配 {used} 字节，池缓冲疑似未归还（归还到位时约 80 字节）");
    }

    [Fact]
    [DisplayName("流式读取器_using模式可用")]
    public void Dispose_UsingPattern()
    {
        var ms = new MemoryStream(new Byte[64]);

        // ref struct 支持模式化 using：出作用域自动归还扩容借出的池缓冲
        {
            using var reader = new SpanReader(ms, null, 256);
            Assert.Equal(0x00, reader.ReadByte());
        }

        // 二次使用（缓冲来自池复用）不受前一次释放影响
        ms.Position = 0;
        {
            using var reader = new SpanReader(ms, null, 256);
            Assert.Equal(0x00, reader.ReadByte());
        }
    }

    [Fact]
    [DisplayName("视图读取器_释放不影响调用方数据包")]
    public void Dispose_ViewReader_KeepsCallerPacket()
    {
        var packet = new OwnerPacket(8);
        var reader = new SpanReader((IPacket)packet);
        _ = reader.ReadByte();

        reader.Dispose();

        // 构造时传入的数据包不属于读取器：释放只归还自己借的缓冲，不动调用方句柄
        Assert.Equal(1, packet.RefCount);
        Assert.Equal(8, packet.Length);

        packet.Dispose();
    }

    [Fact]
    [DisplayName("释放_重复调用幂等")]
    public void Dispose_Twice_IsIdempotent()
    {
        var ms = new MemoryStream(new Byte[1024]);
        var reader = OpenStreamReader(ms);

        reader.Dispose();
        reader.Dispose();
        reader.Dispose();

        // 重复释放不抛异常；此时读取器已作废
        Assert.True(true);
    }

    [Fact]
    [DisplayName("流式读取器_不释放也不会抛异常_但会漏一块池缓冲")]
    public void WithoutDispose_StillFunctional()
    {
        // 保留旧用法（不显式释放）不抛异常、读取结果正确——只是那块池缓冲回不到池，
        // 故 Dispose 属"建议但非强制"的纪律：走流式扩容的读取器应使用 using
        var ms = new MemoryStream(new Byte[] { 1, 2, 3 });
        var reader = new SpanReader(ms, null, 1024);

        Assert.Equal(1, reader.ReadByte());
        Assert.Equal(2, reader.ReadByte());
        Assert.Equal(3, reader.ReadByte());
    }

    [Fact]
    [DisplayName("流式扩容_初始包可访问数组_未读数据不丢失")]
    public void EnsureSpace_ArrayBackedPacket_KeepsUnreadData()
    {
        // 初始包只给 3 字节，读掉 1 字节后还剩 2 字节；再读 8 字节触发扩容，
        // 剩下的 2 字节必须先搬进新缓冲，否则数据丢失
        using var ms = new MemoryStream(new Byte[] { 4, 5, 6, 7, 8, 9 });
        var head = new ArrayPacket(new Byte[] { 1, 2, 3 });

        using var reader = new SpanReader(ms, head, 8);
        Assert.Equal(1, reader.ReadByte());

        var rest = reader.ReadBytes(8);
        Assert.Equal(new Byte[] { 2, 3, 4, 5, 6, 7, 8, 9 }, rest.ToArray());
    }

    [Fact]
    [DisplayName("流式扩容_初始包不可访问数组_抛异常不漏池缓冲")]
    public void EnsureSpace_UnsupportedArrayAccess_DoesNotLeakPoolBuffer()
    {
        // 非数组内存的数据包让 TryGetArray 返回 false，扩容时走"旧数据无法搬运"的异常分支。
        // 该判定必须排在租借新池缓冲之前，否则每抛一次就漏一块缓冲（读取器不持有它，谁也还不回去）
        IPacket data = new MemoryPacket(new NonArrayMemoryManager(4).Memory, 4);
        using var ms = new MemoryStream(new Byte[1024]);

        const Int32 BufferSize = 65536;
        const Int32 N = 16;

        // 预热：先让池备好该尺寸的数组，之后每轮若能归还就不会再产生数组分配
        for (var i = 0; i < 4; i++)
        {
            var warm = new SpanReader(ms, data, BufferSize);
            try { _ = warm.ReadInt64(); } catch (NotSupportedException) { }
            warm.Dispose();
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < N; i++)
        {
            var reader = new SpanReader(ms, data, BufferSize);
            try { _ = reader.ReadInt64(); } catch (NotSupportedException) { }
            reader.Dispose();
        }
        var used = (GC.GetAllocatedBytesForCurrentThread() - before) / N;

        // 正常情况下每轮只有异常对象的开销；若漏归还池缓冲，这里还会多出 64KB 的数组分配
        Assert.True(used < 16 * 1024, $"每轮分配 {used} 字节，异常路径疑似漏归还池缓冲");
    }

    /// <summary>内存管理器：包装普通数组但让 Memory 不暴露数组视图，用于模拟 TryGetArray 返回 false 的数据包</summary>
    private sealed class NonArrayMemoryManager : MemoryManager<Byte>
    {
        private readonly Byte[] _buffer;

        public NonArrayMemoryManager(Int32 length) => _buffer = new Byte[length];

        public override Span<Byte> GetSpan() => _buffer;

        public override MemoryHandle Pin(Int32 elementIndex = 0) => default;

        public override void Unpin() { }

        protected override void Dispose(Boolean disposing) { }
    }
}
