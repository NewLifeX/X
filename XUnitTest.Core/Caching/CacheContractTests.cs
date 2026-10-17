using System.Reflection;
using NewLife.Caching;
using NewLife.Reflection;
using Xunit;

namespace XUnitTest.Caching;

/// <summary>Cache 基类默认实现契约测试，使用最小字典缓存子类避免依赖具体实现</summary>
public class CacheContractTests
{
    private sealed class SimpleCache : Cache
    {
        private readonly Dictionary<String, Object?> _items = [];

        public override Int32 Count => _items.Count;
        public override ICollection<String> Keys => _items.Keys;

        public override Boolean ContainsKey(String key) => _items.ContainsKey(key);

        public override Boolean Set<T>(String key, T value, Int32 expire = -1)
        {
            _items[key] = value;
            return true;
        }

        public override T Get<T>(String key)
        {
            if (!_items.TryGetValue(key, out var value) || value == null) return default!;

            return value is T t ? t : value.ChangeType<T>();
        }

        public override Int32 Remove(String key) => _items.Remove(key) ? 1 : 0;

        public override Int32 Remove(params String[] keys)
        {
            var count = 0;
            foreach (var key in keys)
            {
                if (_items.Remove(key)) count++;
            }

            return count;
        }

        public override Boolean SetExpire(String key, TimeSpan expire) => ContainsKey(key);

        public override TimeSpan GetExpire(String key) => ContainsKey(key) ? TimeSpan.Zero : TimeSpan.FromSeconds(-1);
    }

    [Fact(DisplayName = "基类 Add 默认实现仅在键不存在时写入")]
    public void Add_Default_OnlyWhenMissing()
    {
        var cache = new SimpleCache();

        Assert.True(cache.Add("key", "first"));
        Assert.Equal("first", cache.Get<String>("key"));

        Assert.False(cache.Add("key", "second"));
        Assert.Equal("first", cache.Get<String>("key"));
    }

    [Fact(DisplayName = "基类 Replace 默认实现返回旧值并写入新值")]
    public void Replace_Default_ReturnsOld()
    {
        var cache = new SimpleCache();
        cache.Set("key", "old");

        Assert.Equal("old", cache.Replace("key", "new"));
        Assert.Equal("new", cache.Get<String>("key"));

        // 不存在的键返回默认值，并写入新值
        Assert.Null(cache.Replace<String>("missing", "created"));
        Assert.Equal("created", cache.Get<String>("missing"));
    }

    [Fact(DisplayName = "基类 TryGetValue 默认实现区分默认值与不存在")]
    public void TryGetValue_Default_DistinguishesDefaultValue()
    {
        var cache = new SimpleCache();
        cache.Set("zero", 0);

        Assert.True(cache.TryGetValue<Int32>("zero", out var zero));
        Assert.Equal(0, zero);

        Assert.False(cache.TryGetValue<Int32>("missing", out var missing));
        Assert.Equal(0, missing);
    }

    [Fact(DisplayName = "基类 Increment/Decrement 默认实现覆盖 Int64 与 Double")]
    public void IncrementDecrement_Default()
    {
        var cache = new SimpleCache();

        Assert.Equal(5L, cache.Increment("count", 5L));
        Assert.Equal(8L, cache.Increment("count", 3L));
        Assert.Equal(6L, cache.Decrement("count", 2L));

        Assert.Equal(1.5d, cache.Increment("ratio", 1.5d), 6);
        Assert.Equal(0.25d, cache.Decrement("ratio", 1.25d), 6);
    }

    [Fact(DisplayName = "基类 GetOrAdd 默认实现命中不回调且支持缓存默认值")]
    public void GetOrAdd_Default_HitAndDefaultValue()
    {
        var cache = new SimpleCache();
        var times = 0;

        var value = cache.GetOrAdd("key", _ => { times++; return 42; });
        Assert.Equal(42, value);
        Assert.Equal(1, times);

        value = cache.GetOrAdd("key", _ => { times++; return 99; });
        Assert.Equal(42, value);
        Assert.Equal(1, times);

        // 缓存值为类型默认值 0 时，仍应视为命中而不是再次回调
        var zero = cache.GetOrAdd("zero", _ => { times++; return 0; });
        Assert.Equal(0, zero);
        Assert.Equal(2, times);

        zero = cache.GetOrAdd("zero", _ => { times++; return 1; });
        Assert.Equal(0, zero);
        Assert.Equal(2, times);
    }
}

/// <summary>MemoryCache 取或加与过期淘汰契约测试</summary>
public class MemoryCacheContractTests
{
    [Fact(DisplayName = "GetOrAdd 值重载：未命中写入、命中返回旧值、过期后重建")]
    public async Task GetOrAdd_Value_HitMissAndExpire()
    {
        var cache = new MemoryCache();

        Assert.Equal("first", cache.GetOrAdd("key", "first"));
        Assert.Equal("first", cache.Get<String>("key"));

        Assert.Equal("first", cache.GetOrAdd("key", "second"));
        Assert.Equal("first", cache.Get<String>("key"));

        cache.Set("expired", "old", 1);
        await Task.Delay(1100);

        Assert.Equal("new", cache.GetOrAdd("expired", "new"));
        Assert.Equal("new", cache.Get<String>("expired"));
    }

    [Fact(DisplayName = "GetOrAdd 回调重载：未命中执行一次、命中不回调")]
    public void GetOrAdd_Callback_HitMiss()
    {
        var cache = new MemoryCache();
        var times = 0;

        var value = cache.GetOrAdd("key", key =>
        {
            Assert.Equal("key", key);
            times++;
            return 42;
        });
        Assert.Equal(42, value);
        Assert.Equal(1, times);

        value = cache.GetOrAdd("key", _ => { times++; return 99; });
        Assert.Equal(42, value);
        Assert.Equal(1, times);
    }

    [Fact(DisplayName = "GetOrAdd 回调重载：工厂返回 null 也会缓存且不重复回调")]
    public void GetOrAdd_Callback_NullCached()
    {
        var cache = new MemoryCache();
        var times = 0;

        var value = cache.GetOrAdd<String?>("key", _ => { times++; return null; });
        Assert.Null(value);
        Assert.Equal(1, times);

        value = cache.GetOrAdd<String?>("key", _ => { times++; return "value"; });
        Assert.Null(value);
        Assert.Equal(1, times);
    }

    [Fact(DisplayName = "GetOrAdd 回调重载：工厂异常向上传播且不写入缓存")]
    public void GetOrAdd_Callback_Exception()
    {
        var cache = new MemoryCache();
        var times = 0;

        Assert.Throws<InvalidOperationException>(() => cache.GetOrAdd<String>("key", _ =>
        {
            times++;
            throw new InvalidOperationException("factory");
        }));
        Assert.Equal(1, times);
        Assert.False(cache.ContainsKey("key"));

        // 异常未缓存，下一次仍会重新回调
        Assert.Equal("ok", cache.GetOrAdd("key", _ => { times++; return "ok"; }));
        Assert.Equal(2, times);
    }

    [Fact(DisplayName = "GetOrAdd 回调重载：命中但已过期时重建并回填")]
    public async Task GetOrAdd_Callback_ExpiredRebuild()
    {
        var cache = new MemoryCache();
        var times = 0;

        Assert.Equal("first", cache.GetOrAdd("key", _ => { times++; return "first"; }, 1));
        Assert.Equal(1, times);

        // 未过期时不重复回调
        Assert.Equal("first", cache.GetOrAdd("key", _ => { times++; return "second"; }, 1));
        Assert.Equal(1, times);

        await Task.Delay(1100);

        // 已过期：执行工厂重建并回填，返回新值
        Assert.Equal("second", cache.GetOrAdd("key", _ => { times++; return "second"; }, 60));
        Assert.Equal(2, times);
        Assert.Equal("second", cache.Get<String>("key"));

        // 回填后未过期，不再回调
        Assert.Equal("second", cache.GetOrAdd("key", _ => { times++; return "third"; }, 60));
        Assert.Equal(2, times);
    }

    [Fact(DisplayName = "GetOrAdd 回调重载：过期重建时工厂异常向上传播且不返回旧值")]
    public async Task GetOrAdd_Callback_ExpiredFactoryException()
    {
        var cache = new MemoryCache();
        cache.Set("key", "old", 1);
        await Task.Delay(1100);

        Assert.Throws<InvalidOperationException>(() => cache.GetOrAdd<String>("key", _ => throw new InvalidOperationException("factory")));

        // 过期旧值不再可读
        Assert.Null(cache.Get<String>("key"));

        // 异常未缓存，下一次重新回调
        Assert.Equal("new", cache.GetOrAdd("key", _ => "new"));
        Assert.Equal("new", cache.Get<String>("key"));
    }

    [Fact(DisplayName = "RemoveNotAlive 清理已过期缓存项并修正计数")]
    public async Task RemoveNotAlive_RemovesExpired()
    {
        var cache = new MemoryCache();
        cache.Set("keep", "alive");
        cache.Set("expired", "dead", 1);

        await Task.Delay(1100);

        InvokeRemoveNotAlive(cache);

        Assert.True(cache.ContainsKey("keep"));
        Assert.False(cache.Keys.Contains("expired"));
        Assert.Equal(1, cache.Count);
    }

    // Bench() 默认每个线程 1 万次并按 CPU 数扩展，整体耗时较长；其内部按耗时毫秒计算速度，
    // 极小负载下 ElapsedMilliseconds 可能为 0 导致除零，不适合作确定性冒烟。此处直接覆盖公开的
    // BenchOne 单线程顺序路径（赋值/读取/删除/累加），负载足够大以保证计时非零。
    [Fact(DisplayName = "BenchOne 顺序模式冒烟：读写删与累加路径可执行")]
    public void BenchOne_Smoke()
    {
        var cache = new MemoryCache();
        var keys = new[] { "bench:1" };

        var rs = cache.BenchOne(keys, 1_000_000, 1, false, 0);

        Assert.True(rs >= 3);
        Assert.True(cache.Get<Int64>("bench:1") > 0);
    }

    private static void InvokeRemoveNotAlive(MemoryCache cache)
    {
        var method = typeof(MemoryCache).GetMethod("RemoveNotAlive", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method.Invoke(cache, [null]);
    }
}
