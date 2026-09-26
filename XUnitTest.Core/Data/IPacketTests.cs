using NewLife;
using NewLife.Data;
using NewLife.Reflection;
using NewLife.Security;
using Xunit;

namespace XUnitTest.Data;

public class IPacketTests
{
    [Fact]
    public void OwnerPacketTest()
    {
        var pk = new OwnerPacket(123);

        Assert.NotNull(pk.Buffer);
        Assert.Equal(128, pk.Buffer.Length);
        Assert.Equal(0, pk.Offset);
        Assert.Equal(123, pk.Length);
        Assert.Equal(123, pk.Total);
        Assert.Null(pk.Next);
        Assert.NotNull(pk.GetValue("_owner"));

        pk[77] = (Byte)'A';
        Assert.Equal('A', (Char)pk[77]);

        var span = pk.GetSpan();
        Assert.Equal('A', (Char)span[77]);

        var memory = pk.GetMemory();
        Assert.Equal(123, memory.Length);
        Assert.Equal('A', (Char)memory.Span[77]);

        var gcmemory = GC.GetAllocatedBytesForCurrentThread();
        pk.Resize(127);

        var pk2 = pk.Slice(7, 70);
        Assert.Equal(gcmemory + 48, GC.GetAllocatedBytesForCurrentThread());
        Assert.NotNull(pk2);
        Assert.Equal(70, pk2.Length);
        Assert.Equal(7, pk2.Offset);
        Assert.Same(pk.GetValue("_owner"), pk2.GetValue("_owner"));
        Assert.NotNull(pk.GetValue("_owner"));
        Assert.Equal(2, pk.RefCount);

        var rs = pk2.TryGetArray(out var segment);
        Assert.True(rs);
        Assert.Equal(pk.Buffer, segment.Array);
        Assert.Equal(7, segment.Offset);
        Assert.Equal(70, segment.Count);

        // 扩展头部：共享借位（引用计数各自释放，源句柄不受影响）
        var pk3 = pk2.ExpandHeader(3) as OwnerPacket;
        Assert.NotNull(pk3);
        Assert.Equal(pk.Buffer, pk3.Buffer);
        Assert.Equal(7 - 3, pk3.Offset);
        Assert.Equal(70 + 3, pk3.Length);
        Assert.Equal(3, pk.RefCount);       // pk 切片、pk2 切片、借位头各持一份引用

        pk3.TryDispose();
        Assert.NotNull(pk.GetValue("_owner"));
        Assert.Equal(2, pk.RefCount);
        Assert.Equal(70, pk2.Length);       // 源切片仍然有效

        pk.TryDispose();
    }

    [Fact]
    public void MemoryPacketTest()
    {
        var buf = Rand.NextBytes(125);
        var gcmemory = GC.GetAllocatedBytesForCurrentThread();
        var pk = new MemoryPacket(buf, 123);
        Assert.Equal(gcmemory, GC.GetAllocatedBytesForCurrentThread());

        Assert.Equal(125, pk.Memory.Length);
        Assert.Equal(123, pk.Length);
        Assert.Equal(123, pk.Total);
        Assert.Null(pk.Next);

        pk[77] = (Byte)'A';
        Assert.Equal('A', (Char)pk[77]);

        var span = pk.GetSpan();
        Assert.Equal('A', (Char)span[77]);

        var memory = pk.GetMemory();
        Assert.Equal(123, memory.Length);
        Assert.Equal('A', (Char)memory.Span[77]);

        var pk2 = (MemoryPacket)pk.Slice(7, 70);
        Assert.Equal(70, pk2.Length);

        var rs = (pk2 as IPacket).TryGetArray(out var segment);
        Assert.True(rs);
        Assert.Equal(buf, segment.Array);
        Assert.Equal(7, segment.Offset);
        Assert.Equal(70, segment.Count);

        pk2.TryDispose();

        // 头部准备：MemoryPacket 为视图，新头节点挂接负载（零拷贝单段头 + 负载链）
        var pk3 = (OwnerPacket)pk2.PrepareHeader(3);
        Assert.Equal(3, pk3.Length);
        Assert.NotNull(pk3.Next);
        Assert.Equal(70 + 3, pk3.Total);
        pk3.TryDispose();
    }

    [Fact]
    public void ArrayPacketTest()
    {
        var buf = Rand.NextBytes(125);
        var pk = new ArrayPacket(buf, 2, 123);

        Assert.NotNull(pk.Buffer);
        Assert.Equal(125, pk.Buffer.Length);
        Assert.Equal(2, pk.Offset);
        Assert.Equal(123, pk.Length);
        Assert.Equal(123, pk.Total);
        Assert.Null(pk.Next);

        pk[77] = (Byte)'A';
        Assert.Equal('A', (Char)pk[77]);

        var span = pk.GetSpan();
        Assert.Equal('A', (Char)span[77]);

        var memory = pk.GetMemory();
        Assert.Equal(123, memory.Length);
        Assert.Equal('A', (Char)memory.Span[77]);

        var pk2 = pk.Slice(7, 70);
        Assert.Equal(70, pk2.Length);
        Assert.Equal(2 + 7, pk2.Offset);

        var rs = (pk2 as IPacket).TryGetArray(out var segment);
        Assert.True(rs);
        Assert.Equal(pk.Buffer, segment.Array);
        Assert.Equal(2 + 7, segment.Offset);
        Assert.Equal(70, segment.Count);

        pk2.TryDispose();

        // 扩展头部
        var pk3 = (ArrayPacket)pk2.ExpandHeader(3);
        Assert.Equal(pk.Buffer, pk3.Buffer);
        Assert.Equal(2 + 7 - 3, pk3.Offset);
        Assert.Equal(70 + 3, pk3.Length);
    }

    [Fact(DisplayName = "ExpandHeader：未预留头部空间时抛异常")]
    public void ExpandHeader_NoReserve_Throws()
    {
        using var op = new OwnerPacket(64);
        op.GetSpan().Fill(0x51);

        var ex = Assert.Throws<InvalidOperationException>(() => op.ExpandHeader(8));
        Assert.Contains("预留", ex.Message);
    }

    [Fact(DisplayName = "ExpandHeader：带链句柄切片后前移链头，源句柄与源链均不受影响")]
    public void ExpandHeader_Chained_SharedSlice()
    {
        var part1 = new OwnerPacket(8, 8);
        var part2 = new OwnerPacket(8);
        part1.Next = part2;
        part1.GetSpan().Fill(0x11);
        part2.GetSpan().Fill(0x22);
        var buffer1 = part1.GetValue("_buffer");
        var buffer2 = part2.GetValue("_buffer");

        // 带链：切片得到独占的共享节点链后前移链头
        var pk = part1.ExpandHeader(4);
        Assert.Same(buffer1, pk.Buffer);
        Assert.Equal(8 - 4, pk.Offset);
        Assert.Equal(4 + 8, pk.Length);
        Assert.NotNull(pk.Next);
        Assert.Same(buffer2, ((OwnerPacket)pk.Next!).Buffer);
        Assert.Equal(0x11, pk[4]);
        Assert.Equal(0x22, pk[12]);

        // 源链完整可用，仅多出一份共享引用
        Assert.Equal(0x11, part1[0]);
        Assert.Equal(0x22, part2[0]);
        Assert.Equal(16, part1.Total);
        Assert.Equal(2, part1.RefCount);
        Assert.Equal(2, part2.RefCount);

        pk.TryDispose();
        Assert.Equal(1, part1.RefCount);
        Assert.Equal(1, part2.RefCount);
        Assert.Equal(0x11, part1[0]);
    }

    [Fact(DisplayName = "ExpandHeader：空首节点带链时保留预留区前移，不产生负偏移")]
    public void ExpandHeader_EmptyHeadChain_KeepsReserve()
    {
        // 首节点为空但持有预留区：切片会跳过长度 0 的节点，预留区随之丢失，必须改为保留首节点自身前移窗口
        var part1 = new OwnerPacket(0, 8);
        var part2 = new OwnerPacket(8);
        part1.Next = part2;
        part2.GetSpan().Fill(0x33);
        var buffer1 = part1.GetValue("_buffer");
        var buffer2 = part2.GetValue("_buffer");

        var pk = part1.ExpandHeader(4);
        Assert.Same(buffer1, pk.Buffer);
        Assert.Equal(8 - 4, pk.Offset);
        Assert.Equal(4, pk.Length);
        Assert.NotNull(pk.Next);
        Assert.Same(buffer2, ((OwnerPacket)pk.Next!).Buffer);

        // 新头窗口落在原预留区（与负载同缓冲），负载从第 4 字节起可读
        pk.GetSpan().Fill(0x44);
        Assert.Equal(0x44, pk[0]);
        Assert.Equal(0x33, pk[4]);

        // 源句柄与源链保持有效，仅多出共享引用
        Assert.Equal(0, part1.Length);
        Assert.Equal(8, part1.Offset);
        Assert.Equal(2, part1.RefCount);
        Assert.Equal(2, part2.RefCount);

        pk.TryDispose();
        Assert.Equal(1, part1.RefCount);
        Assert.Equal(1, part2.RefCount);
        Assert.Equal(0x33, part2[0]);
    }

    [Fact(DisplayName = "PrepareHeader：预留借位共享零拷贝，未预留新头节点挂接负载链")]
    public void PrepareHeader_BorrowOrChain()
    {
        // 预留 8 字节：借位共享零拷贝，共用同一缓冲（源句柄保持有效）
        var op = new OwnerPacket(16, 8);
        op.GetSpan().Fill(0x42);
        var buffer = op.GetValue("_buffer");

        var pk = op.PrepareHeader(4);
        var head = Assert.IsType<OwnerPacket>(pk);
        Assert.Same(buffer, head.GetValue("_buffer"));
        Assert.Equal(4, head.Offset);
        Assert.Equal(4 + 16, head.Length);
        Assert.Null(head.Next);
        Assert.Equal(0x42, head[4]);
        Assert.Equal(2, op.RefCount);       // 源句柄与帧头各持一份

        head.TryDispose();
        Assert.Equal(1, op.RefCount);
        Assert.Equal(0x42, op[0]);          // 源句柄始终可用
        op.TryDispose();

        // 未预留：新头节点挂接共享负载链（零拷贝），源句柄保持有效
        using var raw = new OwnerPacket(16);
        raw.GetSpan().Fill(0x42);

        var pk2 = raw.PrepareHeader(4);
        var head2 = Assert.IsType<OwnerPacket>(pk2);
        Assert.Equal(0, head2.FreeHeader);                          // 新头节点无前置空间
        Assert.Equal(4, head2.Length);
        Assert.Same(raw.Buffer, ((OwnerPacket)head2.Next!).Buffer);  // 负载共享同一缓冲
        Assert.Equal(4 + 16, head2.Total);
        Assert.Equal(0x42, head2[4]);
        Assert.Equal(0x42, head2[19]);
        Assert.NotNull(raw.GetValue("_owner"));                      // 源句柄仍有效
        Assert.Equal(2, raw.RefCount);

        head2.TryDispose();
        Assert.Equal(1, raw.RefCount);
        Assert.Equal(0x42, raw[0]);
    }

    [Fact]
    public void ReadBytesToSpan()
    {
        // 单节点：直接拷贝，返回实读长度
        var pk = new ArrayPacket(Rand.NextBytes(16), 3, 10);
        var buf = new Byte[16];
        var n = pk.ReadBytes(buf);
        Assert.Equal(10, n);
        Assert.Equal(pk.ToArray(), buf[..n]);

        // 链式：跨节点续接（ArrayPacket 是结构体，自后向前组装链）
        var f3 = new ArrayPacket(Rand.NextBytes(8), 0, 4);
        var f2 = new ArrayPacket(Rand.NextBytes(8)) { Next = f3 };
        var f1 = new ArrayPacket(Rand.NextBytes(8)) { Next = f2 };

        var all = f1.ToArray();
        Assert.Equal(20, all.Length);

        var buf2 = new Byte[64];
        var n2 = f1.ReadBytes(buf2);
        Assert.Equal(20, n2);
        Assert.Equal(all, buf2[..n2]);

        // 缓冲不足：只读缓冲长度
        var buf3 = new Byte[6];
        var n3 = f1.ReadBytes(buf3);
        Assert.Equal(6, n3);
        Assert.Equal(all[..6], buf3);

        // 空缓冲
        Assert.Equal(0, f1.ReadBytes(Span<Byte>.Empty));
    }

    [Fact(DisplayName = "IndexOf：单段、跨段、跨多段与空段查找均返回最早匹配的全局偏移")]
    public void IndexOfTest()
    {
        // 单段
        var pk = new ArrayPacket("hello\r\nworld"u8.ToArray());
        Assert.Equal(5, pk.IndexOf("\r\n"u8));
        Assert.Equal(-1, pk.IndexOf("xyz"u8));

        // 链式：目标跨两段（结构体链自后向前组装）
        IPacket f2 = new ArrayPacket("hello\r"u8.ToArray()) { Next = new ArrayPacket("\nworld"u8.ToArray()) };
        Assert.Equal(5, f2.IndexOf("\r\n"u8));

        // 链式：目标跨三段（连续短段）
        IPacket f3 = new ArrayPacket("xa"u8.ToArray())
        {
            Next = new ArrayPacket("b"u8.ToArray())
            {
                Next = new ArrayPacket("c"u8.ToArray())
            }
        };
        Assert.Equal(1, f3.IndexOf("abc"u8));

        // 链式：中间夹空段
        IPacket f4 = new ArrayPacket("x"u8.ToArray())
        {
            Next = new ArrayPacket([])
            {
                Next = new ArrayPacket("yz"u8.ToArray())
            }
        };
        Assert.Equal(0, f4.IndexOf("xyz"u8));

        // 链式：跨段匹配与段内匹配同时存在时，返回更早的跨段偏移
        IPacket f5 = new ArrayPacket("A"u8.ToArray())
        {
            Next = new ArrayPacket("BxAB"u8.ToArray())
        };
        Assert.Equal(0, f5.IndexOf("AB"u8));

        // 未命中、超过总长、空目标
        Assert.Equal(-1, f4.IndexOf("xyz!"u8));
        Assert.Equal(-1, f4.IndexOf(ReadOnlySpan<Byte>.Empty));
    }
}
