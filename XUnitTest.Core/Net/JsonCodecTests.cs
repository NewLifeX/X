using NewLife;
using NewLife.Data;
using NewLife.Net.Handlers;
using Xunit;

namespace XUnitTest.Net;

/// <summary>JsonCodec 链式帧负载解析测试</summary>
public class JsonCodecTests
{
    [Fact(DisplayName = "Read：链式帧负载跨段解析（只取首段会截断）")]
    public void Read_ChainedPayload_Decoded()
    {
        var text = new String('x', 10_000);
        var json = "{\"name\":\"NewLife\",\"data\":\"" + text + "\"}";
        var bytes = json.GetBytes();

        // 模拟大帧跨接收轮：按 4KB 分段组链
        IPacket? head = null;
        OwnerPacket? tail = null;
        var pos = 0;
        while (pos < bytes.Length)
        {
            var count = Math.Min(4096, bytes.Length - pos);
            var seg = new OwnerPacket(count);
            bytes.AsSpan(pos, count).CopyTo(seg.GetSpan());
            if (head == null) head = seg; else tail!.Next = seg;
            tail = seg;
            pos += count;
        }

        Object? result = null;
        try
        {
            var codec = new JsonCodec();
            result = codec.Read(null!, head!);
        }
        finally
        {
            head!.TryDispose();
        }

        Assert.NotNull(result);
        var dic = result as System.Collections.IDictionary;
        Assert.NotNull(dic);
        Assert.Equal("NewLife", dic!["name"]?.ToString());
        Assert.Equal(10_000, dic["data"]?.ToString()?.Length);
    }

    [Fact(DisplayName = "Read：Memory 输入直接 UTF8 解码")]
    public void Read_MemoryInput_Decoded()
    {
        var json = "{\"name\":\"NewLife\"}";
        var codec = new JsonCodec();

        var result = codec.Read(null!, new Memory<Byte>(json.GetBytes()));

        var dic = result as System.Collections.IDictionary;
        Assert.NotNull(dic);
        Assert.Equal("NewLife", dic!["name"]?.ToString());
    }

    [Fact(DisplayName = "Read：Byte[] 输入直接 UTF8 解码")]
    public void Read_ByteArrayInput_Decoded()
    {
        var json = "{\"name\":\"NewLife\",\"count\":123}";
        var codec = new JsonCodec();

        var result = codec.Read(null!, json.GetBytes());

        var dic = result as System.Collections.IDictionary;
        Assert.NotNull(dic);
        Assert.Equal("NewLife", dic!["name"]?.ToString());
        Assert.Equal("123", dic["count"]?.ToString());
    }
}
