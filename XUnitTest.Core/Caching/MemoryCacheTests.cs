using System.Diagnostics;
using NewLife;
using NewLife.Caching;
using NewLife.Data;
using NewLife.Log;
using NewLife.Security;
using NewLife.Serialization;
using Xunit;

namespace XUnitTest.Caching;

[TestCaseOrderer("NewLife.UnitTest.DefaultOrderer", "NewLife.UnitTest")]
public class MemoryCacheTests
{
    public MemoryCache Cache { get; set; }

    public MemoryCacheTests() => Cache = new MemoryCache();

    [Fact(DisplayName = "基础测试")]
    public void Test1()
    {
        var ic = Cache;
        var key = "Name";
        var key2 = "Company";

        ic.Set(key, "大石头");
        ic.Set(key2, "新生命");
        Assert.Equal("大石头", ic.Get<String>(key));
        Assert.Equal("新生命", ic.Get<String>(key2));

        var count = ic.Count;
        Assert.True(count >= 2);

        // Keys
        var keys = ic.Keys;
        Assert.True(keys.Contains(key));

        // 过期时间
        ic.SetExpire(key, TimeSpan.FromSeconds(1));
        var ts = ic.GetExpire(key);
        Assert.True(ts.TotalSeconds is > 0 and < 2, "过期时间");

        var rs = ic.Remove(key2);
        Assert.Equal(1, rs);

        Assert.False(ic.ContainsKey(key2));

        ic.Clear();
        Assert.True(ic.Count == 0);
    }

    [Fact(DisplayName = "集合测试")]
    public void DictionaryTest()
    {
        var ic = Cache;

        var dic = new Dictionary<String, String>
        {
            ["111"] = "123",
            ["222"] = "abc",
            ["大石头"] = "学无先后达者为师"
        };

        ic.SetAll(dic);
        var dic2 = ic.GetAll<String>(dic.Keys);

        Assert.Equal(dic.Count, dic2.Count);
        foreach (var item in dic)
        {
            Assert.Equal(item.Value, dic2[item.Key]);
        }
    }

    [Fact(DisplayName = "高级添加")]
    public void AddReplace()
    {
        var ic = Cache;
        var key = "Name";

        ic.Set(key, Environment.UserName, 2);
        var rs = ic.Add(key, Environment.MachineName, 2);
        Assert.False(rs);

        var name = ic.Get<String>(key);
        Assert.Equal(Environment.UserName, name);
        Assert.NotEqual(Environment.MachineName, name);

        var old = ic.Replace(key, Environment.MachineName);
        Assert.Equal(Environment.UserName, old);
        ic.SetExpire(key, TimeSpan.FromSeconds(2));

        name = ic.Get<String>(key);
        Assert.Equal(Environment.MachineName, name);
        Assert.NotEqual(Environment.UserName, name);

        Thread.Sleep(2000);
        rs = ic.Add(key, Environment.MachineName, 2);
        Assert.True(rs);
    }

    [Fact]
    public void TryGet()
    {
        var ic = Cache;
        var key = "TryGetName";

        ic.Set(key, Environment.UserName, 1);
        var v1 = ic.Get<String>(key);
        Assert.NotNull(v1);

        var rs1 = ic.TryGetValue<String>(key, out var v2);
        Assert.True(rs1);
        Assert.Equal(v1, v2);

        Thread.Sleep(1100);

        var v3 = ic.Get<String>(key);
        Assert.Null(v3);

        var rs2 = ic.TryGetValue<String>(key, out var v4);
        Assert.False(rs2);
        Assert.Equal(v1, v4);
    }

    [Fact(DisplayName = "累加累减")]
    public void IncDec()
    {
        var ic = Cache;
        var key = "CostInt";
        var key2 = "CostDouble";

        ic.Set(key, 123);
        ic.Increment(key, 22);
        Assert.Equal(123 + 22, ic.Get<Int32>(key));

        ic.Set(key2, 456d);
        ic.Increment(key2, 22d);
        Assert.Equal(456d + 22d, ic.Get<Double>(key2));

        ic.Set("cc", 3.14);
        ic.Increment("cc", 0.3);
        Assert.Equal(3.14 + 0.3, ic.Get<Double>("cc"));
    }

    [Fact(DisplayName = "复杂对象")]
    public void TestObject()
    {
        var obj = new User
        {
            Name = "大石头",
            Company = "NewLife",
            Age = 24,
            Roles = new[] { "管理员", "游客" },
            UpdateTime = DateTime.Now,
        };

        var ic = Cache;
        var key = "user";

        ic.Set(key, obj);
        var obj2 = ic.Get<User>(key);

        Assert.Equal(obj.ToJson(), obj2.ToJson());
    }

    private class User
    {
        public String Name { get; set; }
        public String Company { get; set; }
        public Int32 Age { get; set; }
        public String[] Roles { get; set; }
        public DateTime UpdateTime { get; set; }
    }

    [Fact(DisplayName = "字节数组")]
    public void TestBuffer()
    {
        var ic = Cache;
        var key = "buf";

        var str = "学无先后达者为师";
        var buf = str.GetBytes();

        ic.Set(key, buf);
        var buf2 = ic.Get<Byte[]>(key);

        Assert.Equal(buf.ToHex(), buf2.ToHex());
    }

    [Fact(DisplayName = "数据包")]
    public void TestPacket()
    {
        var ic = Cache;
        var key = "buf";

        var str = "学无先后达者为师";
        var pk = new Packet(str.GetBytes());

        ic.Set(key, pk);
        var pk2 = ic.Get<Packet>(key);

        Assert.Equal(pk.ToHex(), pk2.ToHex());
    }

#if NET6_0_OR_GREATER
    [Fact(DisplayName = "正常锁")]
    public void TestLock1()
    {
        var ic = Cache;

        using var ck = ic.AcquireLock("lock:TestLock1", 3000);
        var k2 = ck as CacheLock;

        Assert.NotNull(k2);
        Assert.Equal("lock:TestLock1", k2.Key);

        // 实际上存在这个key
        Assert.True(ic.ContainsKey(k2.Key));

        // 取有效期
        var exp = ic.GetExpire(k2.Key);
        Assert.True(exp.TotalMilliseconds <= 3000);

        // 释放锁
        ck.Dispose();

        // 这个key已经不存在
        Assert.False(ic.ContainsKey(k2.Key));
    }

    [Fact(DisplayName = "抢锁失败")]
    public void TestLock2()
    {
        var ic = Cache;

        var ck1 = ic.AcquireLock("lock:TestLock2", 2000);
        // 故意不用using，验证GC是否能回收
        //using var ck1 = ic.AcquireLock("TestLock2", 3000);

        var sw = Stopwatch.StartNew();

        // 抢相同锁，不可能成功。超时时间必须小于3000，否则前面的锁过期后，这里还是可以抢到的
        Assert.Throws<InvalidOperationException>(() => ic.AcquireLock("lock:TestLock2", 1000));
        var ck2 = ic.AcquireLock("lock:TestLock2", 1000, 1000, false);
        //Assert.Null(ck2);

        // 耗时必须超过有效期
        sw.Stop();
        XTrace.WriteLine("TestLock2 ElapsedMilliseconds={0}ms", sw.ElapsedMilliseconds);
        Assert.True(sw.ElapsedMilliseconds >= 1000);

        Thread.Sleep(2000 - 1000 + 1);

        // 那个锁其实已经不在了，缓存应该把它干掉
        Assert.False(ic.ContainsKey("lock:TestLock2"));
    }

    [Fact(DisplayName = "抢死锁")]
    public void TestLock3()
    {
        var ic = Cache;

        XTrace.WriteLine("抢死锁");

        using var ck = ic.AcquireLock("TestLock3", 1000);

        // 已经过了一点时间
        Thread.Sleep(500);

        XTrace.WriteLine("抢死锁 Start");
        var sw = Stopwatch.StartNew();

        // 循环多次后，可以抢到
        using var ck2 = ic.AcquireLock("TestLock3", 1000);
        Assert.NotNull(ck2);
        XTrace.WriteLine("抢死锁 End");

        // 耗时必须超过有效期
        sw.Stop();
        XTrace.WriteLine("TestLock3 ElapsedMilliseconds={0}ms", sw.ElapsedMilliseconds);
        //Assert.True(sw.ElapsedMilliseconds >= 500);
        //Assert.True(sw.ElapsedMilliseconds <= 1000);
    }
#endif

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SaveAndLoad(Boolean compressed)
    {
        var mc = new MemoryCache();

        mc.Set("Name", "大石头");
        mc.Set("Age", 24, 3600);
        mc.Set("Ext", new GeoArea { Code = 1234, Name = "NewLife" }, 86400);

        var file = compressed ?
            "data/memoryCache.gz".GetFullPath() :
            "data/memoryCache.dat".GetFullPath();
        if (File.Exists(file)) File.Delete(file);

        mc.Save(file, compressed);

        Assert.True(File.Exists(file));

        var type = Type.GetType("NewLife.Data.GeoArea");

        var mc2 = new MemoryCache();
        mc2.Load(file, compressed);

        Assert.Equal("大石头", mc2.Get<String>("Name"));
        Assert.Equal(24, mc2.Get<Int32>("Age"));

        var ga = mc2.Get<GeoArea>("Ext");
        Assert.NotNull(ga);
        Assert.Equal(1234, ga.Code);
        Assert.Equal("NewLife", ga.Name);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BigSave(Boolean compressed)
    {
        var mc = new MemoryCache();

        for (var i = 0; i < 500_000; i++)
        {
            var ga = new GeoArea { Code = Rand.Next(100000, 999999), Name = Rand.NextString(8) };
            mc.Set(ga.Name, ga);
        }

        if (compressed)
            mc.Save("data/bigsave.gz", true);
        else
            mc.Save("data/bigsave.cache", false);
    }

    [Fact]
    public void GetQueue()
    {
        var mc = new MemoryCache();
        var queue = mc.GetQueue<TimePoint>("queue");

        var ex = Assert.Throws<InvalidCastException>(() => mc.GetQueue<String>("queue"));
        Assert.StartsWith("Unable to convert the value of [queue]", ex.Message);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(24)]
    [InlineData(25)]
    [InlineData(3650)]
    public void LongExpireSet(Int32 days)
    {
        var mc = new MemoryCache();

        mc.Set("name", "Stone", TimeSpan.FromDays(days));

        var rs = mc.ContainsKey("name");
        Assert.True(rs);

        var val = mc.Get<String>("name");
        Assert.Equal("Stone", val);

        var exp = mc.GetExpire("name");
        Assert.Equal(days, exp.Days);
    }

    [Fact(DisplayName = "移除-单键与通配")]
    public void Remove_Single_And_Wildcard()
    {
        var mc = new MemoryCache();
        mc.Set("user:1", 1);
        mc.Set("user:2", 2);
        mc.Set("role:1", 3);

        // 按通配删除
        var removedByWildcard = mc.Remove("user:*");
        Assert.Equal(2, removedByWildcard);
        Assert.False(mc.ContainsKey("user:1"));
        Assert.False(mc.ContainsKey("user:2"));
        Assert.True(mc.ContainsKey("role:1"));

        // 精确删除
        var removedExact = mc.Remove("role:1");
        Assert.Equal(1, removedExact);
        Assert.False(mc.ContainsKey("role:1"));

        // 删除不存在
        var removedNone = mc.Remove("not-exist");
        Assert.Equal(0, removedNone);
    }

    [Fact(DisplayName = "移除-批量与通配")]
    public void Remove_Batch_With_Wildcards()
    {
        var mc = new MemoryCache();
        mc.Set("user:1", 1);
        mc.Set("user:2", 2);
        mc.Set("user:3", 3);
        mc.Set("role:1", 10);
        mc.Set("role:2", 20);
        mc.Set("menu:1", 100);

        // 同时传入通配与精确键
        var removed = mc.Remove("user:*", "role:2");
        Assert.Equal(4, removed); // user:1, user:2, user:3, role:2

        // 剩余键校验
        Assert.False(mc.ContainsKey("user:1"));
        Assert.False(mc.ContainsKey("user:2"));
        Assert.False(mc.ContainsKey("user:3"));
        Assert.False(mc.ContainsKey("role:2"));
        Assert.True(mc.ContainsKey("role:1"));
        Assert.True(mc.ContainsKey("menu:1"));

        // 再次移除相同模式应为0
        var removedAgain = mc.Remove("user:*", "role:2");
        Assert.Equal(0, removedAgain);
    }

    [Fact(DisplayName = "搜索-匹配与分页")]
    public void Search_With_Pattern_Offset_Count()
    {
        var mc = new MemoryCache();
        var keys = new[] { "s:001", "s:002", "s:003", "a:001", "b:001" };
        foreach (var k in keys) mc.Set(k, 1);

        // 模式匹配
        var matched = mc.Search("s:*").ToList();
        var expectedSet = new HashSet<String>(new[] { "s:001", "s:002", "s:003" });
        Assert.Equal(expectedSet.Count, matched.Count);
        Assert.True(matched.All(expectedSet.Contains));

        // 偏移量（不关心顺序，只验证数量）
        var matchedWithOffset = mc.Search("s:*", offset: 1).ToList();
        Assert.Equal(expectedSet.Count - 1, matchedWithOffset.Count);
        Assert.True(matchedWithOffset.All(expectedSet.Contains));

        // 限制返回数量
        var limited = mc.Search("s:*", count: 2).ToList();
        Assert.Equal(2, limited.Count);
        Assert.True(limited.All(expectedSet.Contains));

        // 空/空字符串应返回全部键
        var all1 = mc.Search(null).ToList();
        var all2 = mc.Search(String.Empty).ToList();
        Assert.Equal(mc.Count, all1.Count);
        Assert.Equal(mc.Count, all2.Count);
    }
}