using System;
using System.Text;
using NewLife;
using NewLife.Data;
using Xunit;

namespace XUnitTest.Data;

public class RingBufferTests
{
    [Fact]
    public void EnsureCapacity()
    {
        var rb = new RingBuffer();
        Assert.Equal(1024, rb.Capacity);

        rb = new RingBuffer(333);
        Assert.Equal(333, rb.Capacity);

        rb.EnsureCapacity(2048);
        Assert.Equal(2048, rb.Capacity);
    }

    [Fact]
    public void WriteReadRead()
    {
        var rb = new RingBuffer(128);

        Assert.Equal(128, rb.Capacity);
        Assert.Equal(0, rb.Length);
        Assert.Equal(0, rb.Head);
        Assert.Equal(0, rb.Tail);

        // 120个字符
        var sb = new StringBuilder();
        for (var i = 0; i < 10; i++)
        {
            sb.Append("HelloNewLife");
        }
        var buf = sb.ToString().GetBytes();

        // 写入数据
        rb.Write(buf);
        Assert.Equal(128, rb.Capacity);
        Assert.Equal(buf.Length, rb.Length);
        Assert.Equal(buf.Length, rb.Head);
        Assert.Equal(0, rb.Tail);

        // 读取
        var buf2 = new Byte[70];
        var count = rb.Read(buf2);
        Assert.Equal(buf2.Length, count);
        Assert.Equal(128, rb.Capacity);
        Assert.Equal(buf.Length - buf2.Length, rb.Length);
        Assert.Equal(buf.Length, rb.Head);
        Assert.Equal(buf2.Length, rb.Tail);
        Assert.Equal(sb.ToString()[..buf2.Length], buf2.ToStr());

        // 读取
        count = rb.Read(buf2);
        Assert.Equal(buf.Length - buf2.Length, count);
        Assert.Equal(128, rb.Capacity);
        Assert.Equal(0, rb.Length);
        Assert.Equal(buf.Length, rb.Head);
        Assert.Equal(buf.Length, rb.Tail);
    }

    [Fact]
    public void WriteWriteReadRead()
    {
        var rb = new RingBuffer(128);

        // 120个字符
        var sb = new StringBuilder();
        for (var i = 0; i < 10; i++)
        {
            sb.Append("HelloNewLife");
        }
        var buf = sb.ToString().GetBytes();

        // 写入数据
        rb.Write(buf);
        Assert.Equal(128, rb.Capacity);
        Assert.Equal(buf.Length, rb.Length);
        Assert.Equal(buf.Length, rb.Head);
        Assert.Equal(0, rb.Tail);

        // 再写，扩容
        rb.Write(buf);
        Assert.Equal(256, rb.Capacity);
        Assert.Equal(buf.Length * 2, rb.Length);
        Assert.Equal(buf.Length * 2, rb.Head);
        Assert.Equal(0, rb.Tail);

        // 读取
        var buf2 = new Byte[70];
        var count = rb.Read(buf2);
        Assert.Equal(buf2.Length, count);
        Assert.Equal(buf.Length * 2 - buf2.Length, rb.Length);
        Assert.Equal(buf2.Length, rb.Tail);
        Assert.Equal(sb.ToString()[..buf2.Length], buf2.ToStr());

        // 读取
        count = rb.Read(buf2);
        Assert.Equal(buf2.Length, count);
        Assert.Equal(buf.Length * 2 - buf2.Length * 2, rb.Length);
        Assert.Equal(buf2.Length * 2, rb.Tail);
    }

    [Fact]
    public void WriteRead3()
    {
        var rb = new RingBuffer(128);

        Assert.Equal(128, rb.Capacity);
        Assert.Equal(0, rb.Length);
        Assert.Equal(0, rb.Head);
        Assert.Equal(0, rb.Tail);

        // 120个字符
        var sb = new StringBuilder();
        for (var i = 0; i < 10; i++)
        {
            sb.Append("HelloNewLife");
        }
        var buf = sb.ToString().GetBytes();

        // 写入数据
        rb.Write(buf);
        Assert.Equal(128, rb.Capacity);
        Assert.Equal(buf.Length, rb.Length);
        Assert.Equal(buf.Length, rb.Head);
        Assert.Equal(0, rb.Tail);

        // 读取
        var buf2 = new Byte[115];
        var count = rb.Read(buf2);
        Assert.Equal(buf2.Length, count);
        Assert.Equal(buf.Length - buf2.Length, rb.Length);
        Assert.Equal(buf2.Length, rb.Tail);
        Assert.Equal(sb.ToString()[..buf2.Length], buf2.ToStr());

        // 再写，扩容
        rb.Write(buf);
        Assert.Equal(128, rb.Capacity);
        Assert.Equal(buf.Length * 2 - buf2.Length, rb.Length);
        Assert.Equal(buf.Length * 2 - rb.Capacity, rb.Head);
        Assert.Equal(buf2.Length, rb.Tail);
    }
}
