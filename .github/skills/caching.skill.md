---
name: caching
description: 使用 NewLife 统一缓存接口实现内存缓存、分布式缓存、缓存锁和队列
---

# NewLife 缓存系统使用指南

## 适用场景

- 需要高性能键值缓存（内存或 Redis）
- 需要分布式锁
- 需要生产者-消费者队列
- 需要原子递增/递减计数器

## 核心 API

### 内存缓存

```csharp
// 全局单例（推荐）
var cache = MemoryCache.Instance;

// 或自定义实例
var cache = new MemoryCache { Capacity = 100_000, Expire = 60 };
```

### 基础操作

```csharp
ICache cache = MemoryCache.Instance;

// 设置（60秒过期）
cache.Set("key", "value", 60);
cache.Set("user:1", new User { Name = "test" }, 300);

// 获取
var val = cache.Get<String>("key");
var user = cache.Get<User>("user:1");

// 带默认值的获取
if (cache.TryGetValue<User>("user:1", out var u)) { /* 命中 */ }

// 删除
cache.Remove("key");
cache.Remove("user:*");  // 通配符删除

// 批量操作
var dict = cache.GetAll<String>(["key1", "key2", "key3"]);
cache.SetAll(new Dictionary<String, Object> { ["a"] = 1, ["b"] = 2 }, 60);
```

### GetOrAdd 模式

```csharp
// 缓存未命中时执行回调加载
var user = cache.GetOrAdd("user:1", k => LoadUserFromDb(k), 300);
```

### 原子计数器

```csharp
// 原子递增，适合计数、限流
var count = cache.Increment("api:calls", 1);
var (value, ttl) = cache.IncrementWithTtl("api:calls", 1);  // 同时获取剩余过期时间
```

### 分布式锁

```csharp
using var lck = cache.AcquireLock("lock:order:123", 3000);
if (lck != null)
{
    // 获取锁成功，执行业务
    ProcessOrder(123);
}
else
{
    // 获取锁失败（超时），降级处理
}
```

### 队列

```csharp
var queue = cache.GetQueue<String>("task:queue");
queue.Add("task1");
queue.Add("task2");

// 消费
if (queue.TryTake(out var task)) { /* 处理 task */ }

// 批量消费
var tasks = queue.Take(10).ToList();
```

## ICacheProvider 服务化

```csharp
// 注册到 DI
services.AddSingleton<ICacheProvider, CacheProvider>();

// 使用
public class OrderService(ICacheProvider cacheProvider)
{
    public void Process()
    {
        var cache = cacheProvider.Cache;     // 跨进程缓存
        var local = cacheProvider.InnerCache; // 本地缓存
        using var lck = cacheProvider.AcquireLock("key", 3000);
    }
}
```

## 切换到 Redis

```csharp
// 安装 NewLife.Redis 包后
var redis = new FullRedis("server=127.0.0.1:6379;password=pass;db=0");

// ICache 接口完全兼容，业务代码无需修改
ICache cache = redis;
cache.Set("key", "value", 60);
```

## 注意事项

- `MemoryCache.Instance` 是全局单例，整个应用共享
- `expire = -1` 使用缓存的默认过期时间，`expire = 0` 表示立即过期
- `Remove` 支持 `*` 通配符，但大量 key 时性能较差
- Redis 实现在独立包 `NewLife.Redis`，接口完全兼容
