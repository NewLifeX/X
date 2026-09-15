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

        var pk2 = pk.Slice(7, 70) as OwnerPacket;
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

        // 扩展头部：接管切片引用（构造后切片句柄作废，源句柄不受影响）
        var pk3 = pk2.ExpandHeader(3) as OwnerPacket;
        Assert.NotNull(pk3);
        Assert.Equal(pk.Buffer, pk3.Buffer);
        Assert.Equal(7 - 3, pk3.Offset);
        Assert.Equal(70 + 3, pk3.Length);

        pk3.TryDispose();
        Assert.NotNull(pk.GetValue("_owner"));

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

        // 扩展头部
        var pk3 = (OwnerPacket)pk2.ExpandHeader(3);
        //Assert.Equal(pk.Memory, pk3.Memory);
        Assert.Equal(70 + 3, pk3.Total);
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

    [Fact]
    public void SliceCompat_StructPackets_IgnoreTransferOwner()
    {
#pragma warning disable CS0618 // 三参重载为兼容旧版二进制保留，此处验证其转发行为
        // 结构体无所有权，三参重载与两参行为一致（兼容旧版二进制接口调用）
        var buf = Rand.NextBytes(64);

        IPacket ap = new ArrayPacket(buf, 4, 32);
        var a1 = ap.Slice(6, 10);
        var a2 = ap.Slice(6, 10, true);
        Assert.Equal(a1.Length, a2.Length);
        Assert.Equal(a1.ToArray(), a2.ToArray());

        IPacket mp = new MemoryPacket(buf, 32);
        var m1 = mp.Slice(6, 10);
        var m2 = mp.Slice(6, 10, false);
        Assert.Equal(m1.ToArray(), m2.ToArray());

        IPacket rp = new ReadOnlyPacket(buf, 4, 32);
        var r1 = rp.Slice(6, 10);
        var r2 = rp.Slice(6, 10, true);
        Assert.Equal(r1.ToArray(), r2.ToArray());

        // 链式 ArrayPacket：三参沿链转发
        var f3 = new ArrayPacket(Rand.NextBytes(8), 0, 4);
        var f2 = new ArrayPacket(Rand.NextBytes(8)) { Next = f3 };
        var f1 = new ArrayPacket(Rand.NextBytes(8)) { Next = f2 };

        var all = f1.ToArray();
        var c1 = ((IPacket)f1).Slice(6, 10, true);
        Assert.Equal(10, c1.Total);

        var expected = new Byte[10];
        Array.Copy(all, 6, expected, 0, 10);
        Assert.Equal(expected, c1.ToArray());
#pragma warning restore CS0618
    }
}
