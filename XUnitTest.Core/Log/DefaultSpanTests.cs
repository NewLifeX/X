using System.ComponentModel;
using NewLife;
using NewLife.Log;
using Xunit;

namespace XUnitTest.Log;

[DisplayName("片段标识惰性生成")]
public class DefaultSpanTests
{
    [Fact]
    [DisplayName("读取时才生成标识，重复读取返回同一实例")]
    public void LazyIdAndTraceId()
    {
        var span = new DefaultSpan();

        // 首次读取即生成，格式符合 W3C 约定
        var id = span.Id;
        Assert.False(id.IsNullOrEmpty());
        Assert.Equal(16, id.Length);
        Assert.Matches("^[0-9a-f]{16}$", id);

        var traceId = span.TraceId;
        Assert.False(traceId.IsNullOrEmpty());
        Assert.Equal(32, traceId.Length);
        Assert.Matches("^[0-9a-f]{32}$", traceId);

        // 缓存同一个实例，重复读取不再生成
        Assert.Same(id, span.Id);
        Assert.Same(traceId, span.TraceId);
    }

    [Fact]
    [DisplayName("显式写入的标识优先于惰性生成")]
    public void SetterWins()
    {
        var span = new DefaultSpan { Id = "1111111111111111", TraceId = "11111111111111111111111111111111" };

        Assert.Equal("1111111111111111", span.Id);
        Assert.Equal("11111111111111111111111111111111", span.TraceId);
        Assert.Same(span.Id, span.Id);
    }

    [Fact]
    [DisplayName("清空后重新惰性生成新标识")]
    public void ClearRegenerates()
    {
        var span = new DefaultSpan();
        var id = span.Id;
        var traceId = span.TraceId;

        span.Clear();

        Assert.NotNull(span.Id);
        Assert.NotNull(span.TraceId);
        Assert.NotEqual(id, span.Id);
        Assert.NotEqual(traceId, span.TraceId);
    }

    [Fact]
    [DisplayName("并发读取拿到同一个标识")]
    public void ConcurrentReadSameValue()
    {
        var span = new DefaultSpan();
        var ids = new String[64];
        var traceIds = new String[64];

        Parallel.For(0, ids.Length, i =>
        {
            ids[i] = span.Id;
            traceIds[i] = span.TraceId;
        });

        foreach (var item in ids)
        {
            Assert.Same(ids[0], item);
        }
        foreach (var item in traceIds)
        {
            Assert.Same(traceIds[0], item);
        }
    }

    [Fact]
    [DisplayName("标识按 W3C 布局分段，各字段偏移正确")]
    public void IdLayout()
    {
        var span = new DefaultSpan();
        var id = span.Id;
        var traceId = span.TraceId;

        // Id      = IPv4(8) + PID(4) + 顺序数(4)
        // TraceId = IPv4(8) + 毫秒(13) + 顺序数(6) + 标识位(1) + PID(4)
        Assert.Equal(id[..8], traceId[..8]);            // 同一进程的 IPv4 段
        Assert.Equal(id[8..12], traceId[28..32]);       // 同一进程的 PID 段
        Assert.Equal('e', traceId[27]);                 // 标识位固定为 e
        Assert.Matches("^[0-9]{13}$", traceId[8..21]);  // 毫秒时间戳为 13 位十进制

        // 毫秒段应接近当前时间
        var ms = traceId[8..21].ToLong();
        var now = Runtime.UtcNow.ToUnixTimeMilliseconds();
        Assert.True(ms > now - 60_000 && ms < now + 60_000, $"毫秒段时间戳偏移过大：{ms} vs {now}");
    }
}
