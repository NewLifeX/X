using NewLife.Caching;
using Xunit;

namespace XUnitTest.Caching;

/// <summary>ICache 接口契约测试，以 MemoryCache 为实现验证缓存接口基本语义</summary>
public class ICacheContractTests
{
    private static MemoryCache CreateCache()
    {
        var cache = new MemoryCache();
        cache.Clear();
        return cache;
    }

    [Fact(DisplayName = "Set/Get 基本类型存储和读取")]
    public void Set_Get_BasicTypes()
    {
        var cache = CreateCache();
        const String key = "test_int";

        // Int32
        cache.Set(key, 42);
        Assert.Equal(42, cache.Get<Int32>(key));

        // String
        cache.Set("test_str", "hello");
        Assert.Equal("hello", cache.Get<String>("test_str"));

        // Boolean
        cache.Set("test_bool", true);
        Assert.True(cache.Get<Boolean>("test_bool"));

        // Double
        cache.Set("test_double", 3.14);
        Assert.Equal(3.14, cache.Get<Double>("test_double"));
    }

    [Fact(DisplayName = "ContainsKey 检查键存在")]
    public void ContainsKey()
    {
        var cache = CreateCache();
        cache.Set("exists", 1);
        Assert.True(cache.ContainsKey("exists"));
        Assert.False(cache.ContainsKey("not_exists"));
    }

    [Fact(DisplayName = "Remove 移除缓存项")]
    public void Remove()
    {
        var cache = CreateCache();
        cache.Set("to_remove", "value");
        Assert.True(cache.ContainsKey("to_remove"));

        cache.Remove("to_remove");
        Assert.False(cache.ContainsKey("to_remove"));
    }

    [Fact(DisplayName = "索引器 Get/Set 永不过期")]
    public void Indexer()
    {
        var cache = CreateCache();
        cache["idx"] = "indexer_value";
        Assert.Equal("indexer_value", cache["idx"]?.ToString());

        cache["idx"] = null;
        Assert.Null(cache["idx"]);
    }

    [Fact(DisplayName = "过期时间：Set 后等待过期则 Get 返回默认值")]
    public async Task Expiry()
    {
        var cache = CreateCache();
        const String key = "expire_key";
        cache.Set(key, "will_expire", 1); // 1 秒过期
        Assert.Equal("will_expire", cache.Get<String>(key));

        await Task.Delay(1500);
        // 已过期，不应返回值
        var val = cache.Get<String>(key);
        Assert.Null(val);
    }

    [Fact(DisplayName = "Set/Get 引用类型对象")]
    public void Set_Get_Object()
    {
        var cache = CreateCache();
        var obj = new MyData { Id = 10, Name = "test" };

        cache.Set("obj", obj);
        var result = cache.Get<MyData>("obj");

        Assert.NotNull(result);
        Assert.Equal(10, result.Id);
        Assert.Equal("test", result.Name);
    }

    [Fact(DisplayName = "TryGetValue 区分存在与不存在")]
    public void TryGetValue()
    {
        var cache = CreateCache();
        cache.Set("exists", 99);

        Assert.True(cache.TryGetValue<Int32>("exists", out var val));
        Assert.Equal(99, val);

        Assert.False(cache.TryGetValue<Int32>("not_exists", out _));
    }

    [Fact(DisplayName = "Count/Keys 基础统计")]
    public void Count_Keys()
    {
        var cache = CreateCache();
        Assert.Equal(0, cache.Count);

        cache.Set("a", 1);
        cache.Set("b", 2);
        cache.Set("c", 3);

        Assert.Equal(3, cache.Count);
        Assert.Contains("a", cache.Keys);
        Assert.Contains("b", cache.Keys);
        Assert.Contains("c", cache.Keys);
    }

    [Fact(DisplayName = "Set 相同键覆盖")]
    public void Overwrite()
    {
        var cache = CreateCache();
        cache.Set("key", "old");
        cache.Set("key", "new");

        Assert.Equal("new", cache.Get<String>("key"));
    }

    class MyData
    {
        public Int32 Id { get; set; }
        public String? Name { get; set; }
    }
}
