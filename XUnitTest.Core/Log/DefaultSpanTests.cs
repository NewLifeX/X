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
}
