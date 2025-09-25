using System;
using System.Linq;
using System.Threading;
using NewLife.Caching;
using Xunit;

namespace XUnitTest.Caching;

/// <summary>CacheProvider 测试</summary>
[TestCaseOrderer("NewLife.UnitTest.DefaultOrderer", "NewLife.UnitTest")]
public class CacheProviderTests
{
    /// <summary>获取 CacheProvider 实例</summary>
    public CacheProvider Provider { get; set; }

    public CacheProviderTests() => Provider = new CacheProvider();

    [Fact(DisplayName = "基础构造测试")]
    public void BasicConstructorTest()
    {
        var provider = new CacheProvider();
        Assert.NotNull(provider.Cache);
        Assert.NotNull(provider.InnerCache);

        // 默认情况下，Cache 和 InnerCache 指向同一实例
        Assert.True(ReferenceEquals(provider.Cache, provider.InnerCache));
    }

    [Fact(DisplayName = "缓存功能测试")]
    public void CacheFunctionalityTest()
    {
        var provider = Provider;

        // 测试全局缓存
        provider.Cache.Set("test:global", "全局缓存值");
        var globalValue = provider.Cache.Get<String>("test:global");
        Assert.Equal("全局缓存值", globalValue);

        // 测试内部缓存
        provider.InnerCache.Set("test:inner", "内部缓存值");
        var innerValue = provider.InnerCache.Get<String>("test:inner");
        Assert.Equal("内部缓存值", innerValue);

        // 验证缓存键存在
        Assert.True(provider.Cache.ContainsKey("test:global"));
        Assert.True(provider.InnerCache.ContainsKey("test:inner"));

        // 清理
        provider.Cache.Remove("test:global");
        provider.InnerCache.Remove("test:inner");
    }

    [Fact(DisplayName = "队列功能测试")]
    public void QueueFunctionalityTest()
    {
        var provider = Provider;

        // 测试全局队列
        var globalQueue = provider.GetQueue<String>("test-topic");
        Assert.NotNull(globalQueue);

        globalQueue.Add("消息1", "消息2");
        var messages = globalQueue.Take(2).ToArray();
        Assert.Equal(2, messages.Length);
        Assert.Contains("消息1", messages);
        Assert.Contains("消息2", messages);

        // 测试内部队列
        var innerQueue = provider.GetInnerQueue<String>("inner-topic");
        Assert.NotNull(innerQueue);

        innerQueue.Add("内部消息1", "内部消息2");
        var innerMessages = innerQueue.Take(2).ToArray();
        Assert.Equal(2, innerMessages.Length);
        Assert.Contains("内部消息1", innerMessages);
        Assert.Contains("内部消息2", innerMessages);
    }

    [Fact(DisplayName = "队列消费组参数测试")]
    public void QueueWithGroupTest()
    {
        var provider = Provider;

        // 测试不同消费组参数
        var queue1 = provider.GetQueue<String>("topic1");
        var queue2 = provider.GetQueue<String>("topic1", null);
        var queue3 = provider.GetQueue<String>("topic1", "group1");

        Assert.NotNull(queue1);
        Assert.NotNull(queue2);
        Assert.NotNull(queue3);

        // 由于当前实现忽略 group 参数，所以应该返回相同类型的队列
        Assert.Equal(queue1.GetType(), queue2.GetType());
        Assert.Equal(queue1.GetType(), queue3.GetType());
    }

    [Fact(DisplayName = "分布式锁功能测试")]
    public void DistributedLockTest()
    {
        var provider = Provider;

        // 测试成功获取锁
        using (var distributedLock = provider.AcquireLock("test:lock", 1000))
        {
            Assert.NotNull(distributedLock);

            // 验证锁在缓存中存在（MemoryCache 实现会创建锁键）
            // 注意：不同的缓存实现可能有不同的锁键格式
            var lockExists = provider.Cache.Keys.Any(k => k.Contains("lock"));
            Assert.True(lockExists, "分布式锁应在缓存中创建相应的键");
        }

        // 锁释放后，相关键应该被清理
        Thread.Sleep(10); // 给一点时间让锁完全释放
    }

    [Fact(DisplayName = "分布式锁超时测试")]
    public void DistributedLockTimeoutTest()
    {
        var provider = Provider;
        var lockKey = "test:timeout:lock";

        // 首先获取一个锁
        using var firstLock = provider.AcquireLock(lockKey, 500);
        Assert.NotNull(firstLock);

        // 尝试获取同一个锁，应该会抛出异常或超时
        Assert.Throws<InvalidOperationException>(() =>
        {
            using var secondLock = provider.AcquireLock(lockKey, 100);
        });
    }

    [Fact(DisplayName = "缓存实例替换测试")]
    public void CacheInstanceReplacementTest()
    {
        var provider = new CacheProvider();
        var originalCache = provider.Cache;
        var originalInnerCache = provider.InnerCache;

        // 替换缓存实例
        var newCache = new MemoryCache { Name = "NewCache" };
        var newInnerCache = new MemoryCache { Name = "NewInnerCache" };

        provider.Cache = newCache;
        provider.InnerCache = newInnerCache;

        Assert.NotEqual(originalCache, provider.Cache);
        Assert.NotEqual(originalInnerCache, provider.InnerCache);
        Assert.Equal("NewCache", provider.Cache.Name);
        Assert.Equal("NewInnerCache", provider.InnerCache.Name);

        // 验证功能正常
        provider.Cache.Set("test", "value");
        Assert.Equal("value", provider.Cache.Get<String>("test"));
    }

    [Fact(DisplayName = "复杂数据类型测试")]
    public void ComplexDataTypeTest()
    {
        var provider = Provider;
        var testData = new TestModel
        {
            Id = 123,
            Name = "测试数据",
            CreatedAt = DateTime.Now,
            Tags = new[] { "tag1", "tag2", "tag3" }
        };

        // 测试复杂对象缓存
        provider.Cache.Set("complex:data", testData);
        var retrievedData = provider.Cache.Get<TestModel>("complex:data");

        Assert.NotNull(retrievedData);
        Assert.Equal(testData.Id, retrievedData.Id);
        Assert.Equal(testData.Name, retrievedData.Name);
        Assert.Equal(testData.Tags.Length, retrievedData.Tags.Length);

        // 清理
        provider.Cache.Remove("complex:data");
    }

    /// <summary>测试数据模型</summary>
    private class TestModel
    {
        public Int32 Id { get; set; }
        public String Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public String[] Tags { get; set; }
    }
}