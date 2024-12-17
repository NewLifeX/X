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
        Assert.True((Boolean)pk.GetValue("_hasOwner"));

        pk[77] = (Byte)'A';
        Assert.Equal('A', (Char)pk[77]);

        var span = pk.GetSpan();
        Assert.Equal('A', (Char)span[77]);

        var memory = pk.GetMemory();
        Assert.Equal(123, memory.Length);
        Assert.Equal('A', (Char)memory.Span[77]);

        var gcmemory = GC.GetAllocatedBytesForCurrentThread();
        pk.Resize(127);

        var pk2 = pk.Slice(7, 70, false) as OwnerPacket;
        Assert.Equal(gcmemory + 48, GC.GetAllocatedBytesForCurrentThread());
        Assert.NotNull(pk2);
        Assert.Equal(70, pk2.Length);
        Assert.Equal(7, pk2.Offset);
        Assert.False((Boolean)pk2.GetValue("_hasOwner"));
        Assert.True((Boolean)pk.GetValue("_hasOwner"));

        var rs = (pk2 as IPacket).TryGetArray(out var segment);
        Assert.True(rs);
        Assert.Equal(pk.Buffer, segment.Array);
        Assert.Equal(7, segment.Offset);
        Assert.Equal(70, segment.Count);

        pk2.TryDispose();
        Assert.False((Boolean)pk2.GetValue("_hasOwner"));
        Assert.True((Boolean)pk.GetValue("_hasOwner"));

        // 扩展头部
        var pk3 = pk2.ExpandHeader(3) as OwnerPacket;
        Assert.NotNull(pk3);
        Assert.Equal(pk.Buffer, pk3.Buffer);
        Assert.Equal(7 - 3, pk3.Offset);
        Assert.Equal(70 + 3, pk3.Length);
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

        var pk2 = (MemoryPacket)pk.Slice(7, 70, false);
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

        var pk2 = (ArrayPacket)pk.Slice(7, 70, false);
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
}
