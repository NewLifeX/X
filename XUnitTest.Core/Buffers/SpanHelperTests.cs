using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife;
using Xunit;

namespace XUnitTest.Buffers;

public class SpanHelperTests
{
    [Fact]
    public void ToStr()
    {
        var str = "Hello NewLife";
        var buf = Encoding.UTF8.GetBytes(str);

        var span = new Span<Byte>(buf);
        var str2 = span.ToStr();
        Assert.Equal(str, str2);

        var span3 = new ReadOnlySpan<Byte>(buf);
        var str3 = span3.ToStr();
        Assert.Equal(str, str3);
    }

    [Fact]
    public void GetBytes()
    {
        var str = "Hello NewLife";
        var buf = Encoding.UTF8.GetBytes(str);

        Span<Byte> span = stackalloc Byte[buf.Length];
        var count = Encoding.UTF8.GetBytes(str.AsSpan(), span);
        Assert.Equal(str.Length, count);
        Assert.Equal('H', (Char)span[0]);
        Assert.Equal('e', (Char)span[1]);
        Assert.Equal('l', (Char)span[2]);
        Assert.Equal('l', (Char)span[3]);
        Assert.Equal('o', (Char)span[4]);

        var str3 = Encoding.UTF8.GetString(span);
        Assert.Equal(str, str3);
    }

    [Fact]
    public void ToHex()
    {
        var str = "Hello NewLife";
        var buf = Encoding.UTF8.GetBytes(str);

        Span<Byte> span = stackalloc Byte[buf.Length];
        var count = Encoding.UTF8.GetBytes(str.AsSpan(), span);

        Assert.Equal(buf.ToHex(), span.ToHex());
        Assert.Equal(buf.ToHex(null, 0, 8), span.ToHex(8));
        Assert.Equal(buf.ToHex("-", 0, 8), span.ToHex(8, "-"));
        Assert.Equal(buf.ToHex("+&", 5, 8), span.ToHex(8, "+&", 5));

        ReadOnlySpan<Byte> span2 = span;
        Assert.Equal(buf.ToHex(), span2.ToHex());
    }
}
