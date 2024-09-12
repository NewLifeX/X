using System;
using NewLife.Buffers;
using Xunit;

namespace XUnitTest.Buffers;

public class SpanWriterTests
{
    [Fact]
    public void CtorTest()
    {
        Span<Byte> span = stackalloc Byte[100];
        var writer = new SpanWriter(span);

        Assert.Equal(0, writer.Position);
        Assert.Equal(span.Length, writer.Capacity);
        Assert.Equal(span.Length, writer.FreeCapacity);
        Assert.Equal(span.Length, writer.GetSpan().Length);

        writer.Advance(33);

        Assert.Equal(33, writer.Position);
        Assert.Equal(span.Length, writer.Capacity);
        Assert.Equal(span.Length - 33, writer.FreeCapacity);
        Assert.Equal(span.Length - 33, writer.GetSpan().Length);

        //Assert.Throws<ArgumentOutOfRangeException>(() => writer.GetSpan(100));
    }
}
