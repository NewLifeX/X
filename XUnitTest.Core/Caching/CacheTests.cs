using NewLife.Caching;
using Xunit;

namespace XUnitTest.Caching;

public class CacheTests
{
    private readonly MemoryCache _cache;

    public CacheTests()
    {
        _cache = new MemoryCache();
    }

    [Fact]
    public void SetGet_Basic()
    {
        _cache.Set("key1", "value1");
        var result = _cache.Get<String>("key1");

        Assert.Equal("value1", result);
    }

    [Fact]
    public void Indexer_SetGet()
    {
        _cache["key2"] = 42;
        var result = _cache.Get<Int32>("key2");

        Assert.Equal(42, result);
    }

    [Fact]
    public void Count()
    {
        Assert.Equal(0, _cache.Count);

        _cache.Set("a", 1);
        _cache.Set("b", 2);
        _cache.Set("c", 3);

        Assert.Equal(3, _cache.Count);
    }

    [Fact]
    public void ContainsKey()
    {
        _cache.Set("exist", "yes");

        Assert.True(_cache.ContainsKey("exist"));
        Assert.False(_cache.ContainsKey("notexist"));
    }

    [Fact]
    public void Remove()
    {
        _cache.Set("todelete", "bye");
        Assert.Equal(1, _cache.Count);

        _cache.Remove("todelete");
        Assert.Equal(0, _cache.Count);
        Assert.False(_cache.ContainsKey("todelete"));
    }

    [Fact]
    public void Get_MissingKey()
    {
        var result = _cache.Get<String>("noexist");
        Assert.Null(result);

        var intResult = _cache.Get<Int32>("noexist");
        Assert.Equal(0, intResult);
    }

    [Fact]
    public void Expire_Default()
    {
        Assert.Equal(0, _cache.Expire);
    }

    [Fact]
    public void Name_Property()
    {
        Assert.NotNull(_cache.Name);
    }

    [Fact]
    public void Keys_ReturnsAllKeys()
    {
        _cache.Set("x", 1);
        _cache.Set("y", 2);
        _cache.Set("z", 3);

        var keys = _cache.Keys;
        Assert.NotNull(keys);
        Assert.Equal(3, keys.Count);
        Assert.Contains("x", keys);
        Assert.Contains("y", keys);
        Assert.Contains("z", keys);
    }

    [Fact]
    public void Clear_All()
    {
        _cache.Set("a", 1);
        _cache.Set("b", 2);
        Assert.Equal(2, _cache.Count);

        _cache.Clear();
        Assert.Equal(0, _cache.Count);
    }

    [Fact]
    public void Default_NotNull()
    {
        Assert.NotNull(Cache.Default);
    }
}
