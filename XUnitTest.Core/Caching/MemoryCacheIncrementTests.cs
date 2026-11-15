using NewLife.Caching;
using Xunit;

namespace XUnitTest.Caching;

/// <summary>缓存小数自增</summary>
public class MemoryCacheIncrementTests
{
    [Fact(DisplayName = "整数缓存上做小数自增不丢结果")]
    public void IncrementDoubleOnInteger()
    {
        var cache = new MemoryCache();
        cache.Set("k", 1);

        var v = cache.Increment("k", 0.5);
        Assert.Equal(1.5, v);

        var got = cache.Get<Double>("k");
        Assert.Equal(1.5, got);
    }
}
