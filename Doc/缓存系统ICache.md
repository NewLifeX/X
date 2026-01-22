# 缓存系统 ICache

## 概述

NewLife.Core 提供了统一的缓存接口 `ICache`，支持内存缓存、Redis 缓存等多种实现。通过统一接口，可以在不同环境下无缝切换缓存实现，同时支持过期时间、原子操作、批量操作等高级特性。

**命名空间**：`NewLife.Caching`  
**文档地址**：https://newlifex.com/core/icache

## 核心特性

- **统一接口**：`ICache` 接口定义标准缓存操作
- **高性能**：`MemoryCache` 基于 `ConcurrentDictionary`，峰值性能达 10 亿 ops
- **自动过期**：支持相对过期时间（TTL）
- **原子操作**：递增、递减、替换等原子操作
- **批量操作**：批量读写减少网络开销
- **LRU 淘汰**：内存缓存超容量时自动清理

## 快速开始

### 基本使用

```csharp
using NewLife.Caching;

// 使用默认内存缓存
var cache = MemoryCache.Instance;

// 设置缓存（60秒过期）
cache.Set("name", "张三", 60);

// 获取缓存
var name = cache.Get<String>("name");

// 删除缓存
cache.Remove("name");
```

### 常用操作

```csharp
var cache = MemoryCache.Instance;

// 检查是否存在
if (cache.ContainsKey("user:1"))
{
    var user = cache.Get<User>("user:1");
}

// 获取或添加（缓存穿透保护）
var data = cache.GetOrAdd("data:key", k =>
{
    // 缓存不存在时，执行此回调获取数据
    return LoadFromDatabase(k);
}, 300);

// 原子递增
var count = cache.Increment("visit:count", 1);
```

## API 参考

### ICache 接口

#### 基本属性

```csharp
/// <summary>缓存名称</summary>
String Name { get; }

/// <summary>默认过期时间（秒）</summary>
Int32 Expire { get; set; }

/// <summary>缓存项总数</summary>
Int32 Count { get; }

/// <summary>所有缓存键</summary>
ICollection<String> Keys { get; }
```

#### 基础操作

```csharp
/// <summary>检查是否存在</summary>
Boolean ContainsKey(String key);

/// <summary>设置缓存</summary>
/// <param name="expire">过期秒数。-1使用默认，0永不过期</param>
Boolean Set<T>(String key, T value, Int32 expire = -1);

/// <summary>设置缓存（TimeSpan）</summary>
Boolean Set<T>(String key, T value, TimeSpan expire);

/// <summary>获取缓存</summary>
T Get<T>(String key);

/// <summary>尝试获取（解决缓存穿透）</summary>
Boolean TryGetValue<T>(String key, out T value);

/// <summary>删除缓存</summary>
Int32 Remove(String key);

/// <summary>批量删除</summary>
Int32 Remove(params String[] keys);

/// <summary>清空所有缓存</summary>
void Clear();
```

#### 过期时间管理

```csharp
/// <summary>设置过期时间</summary>
Boolean SetExpire(String key, TimeSpan expire);

/// <summary>获取剩余过期时间</summary>
TimeSpan GetExpire(String key);
```

#### 批量操作

```csharp
/// <summary>批量获取</summary>
IDictionary<String, T?> GetAll<T>(IEnumerable<String> keys);

/// <summary>批量设置</summary>
void SetAll<T>(IDictionary<String, T> values, Int32 expire = -1);
```

#### 高级操作

```csharp
/// <summary>添加（已存在时不更新）</summary>
Boolean Add<T>(String key, T value, Int32 expire = -1);

/// <summary>替换并返回旧值</summary>
T Replace<T>(String key, T value);

/// <summary>获取或添加</summary>
T GetOrAdd<T>(String key, Func<String, T> callback, Int32 expire = -1);

/// <summary>原子递增</summary>
Int64 Increment(String key, Int64 value);
Double Increment(String key, Double value);

/// <summary>原子递减</summary>
Int64 Decrement(String key, Int64 value);
Double Decrement(String key, Double value);
```

### MemoryCache 类

```csharp
public class MemoryCache : Cache
{
    /// <summary>默认实例</summary>
    public static MemoryCache Instance { get; set; }
    
    /// <summary>容量。超标时LRU淘汰，默认100000</summary>
    public Int32 Capacity { get; set; }
    
    /// <summary>定时清理间隔（秒），默认60</summary>
    public Int32 Period { get; set; }
    
    /// <summary>缓存键过期事件</summary>
    public event EventHandler<KeyEventArgs>? KeyExpired;
}
```

## 使用场景

### 1. 数据缓存

```csharp
public class UserService
{
    private readonly ICache _cache;
    
    public UserService(ICache cache)
    {
        _cache = cache;
    }
    
    public User? GetUser(Int32 id)
    {
        var key = $"user:{id}";
        
        // 先查缓存
        if (_cache.TryGetValue<User>(key, out var user))
            return user;
        
        // 缓存未命中，查数据库
        user = LoadUserFromDb(id);
        
        if (user != null)
            _cache.Set(key, user, 300);  // 缓存5分钟
        
        return user;
    }
}
```

### 2. 防止缓存穿透

```csharp
// 使用 GetOrAdd 防止缓存穿透
var user = cache.GetOrAdd($"user:{id}", key =>
{
    // 即使数据库返回 null，也会被缓存
    return LoadUserFromDb(id);
}, 60);

// 使用 TryGetValue 区分空值和不存在
if (cache.TryGetValue<User?>($"user:{id}", out var user))
{
    // 键存在（可能为 null）
    return user;
}
else
{
    // 键不存在，需要查询数据库
}
```

### 3. 计数器

```csharp
// 访问计数
var count = cache.Increment("page:home:views", 1);

// 限流计数
var requests = cache.Increment($"rate:{userId}", 1);
if (requests == 1)
{
    // 首次访问，设置过期时间
    cache.SetExpire($"rate:{userId}", TimeSpan.FromMinutes(1));
}
if (requests > 100)
{
    throw new Exception("请求过于频繁");
}
```

### 4. 分布式锁简易实现

```csharp
public class SimpleLock
{
    private readonly ICache _cache;
    
    public Boolean TryLock(String key, Int32 seconds = 30)
    {
        // Add 只在不存在时成功
        return _cache.Add($"lock:{key}", DateTime.Now, seconds);
    }
    
    public void Unlock(String key)
    {
        _cache.Remove($"lock:{key}");
    }
}

// 使用
var locker = new SimpleLock(cache);
if (locker.TryLock("order:create"))
{
    try
    {
        // 执行业务
    }
    finally
    {
        locker.Unlock("order:create");
    }
}
```

### 5. 会话缓存

```csharp
public class SessionCache
{
    private readonly ICache _cache;
    private readonly Int32 _expire = 1800;  // 30分钟
    
    public void Set(String sessionId, Object data)
    {
        _cache.Set($"session:{sessionId}", data, _expire);
    }
    
    public T? Get<T>(String sessionId)
    {
        var key = $"session:{sessionId}";
        var data = _cache.Get<T>(key);
        
        if (data != null)
        {
            // 续期
            _cache.SetExpire(key, TimeSpan.FromSeconds(_expire));
        }
        
        return data;
    }
}
```

### 6. 批量操作优化

```csharp
// 批量获取
var keys = new[] { "user:1", "user:2", "user:3" };
var users = cache.GetAll<User>(keys);

foreach (var kv in users)
{
    Console.WriteLine($"{kv.Key}: {kv.Value?.Name}");
}

// 批量设置
var items = new Dictionary<String, User>
{
    ["user:1"] = new User { Id = 1, Name = "张三" },
    ["user:2"] = new User { Id = 2, Name = "李四" }
};
cache.SetAll(items, 300);
```

## 过期时间说明

| expire 值 | 含义 |
|-----------|------|
| < 0 | 使用默认过期时间 `Expire` |
| = 0 | 永不过期 |
| > 0 | 从现在起 N 秒后过期 |

```csharp
// 使用默认过期时间
cache.Set("key1", value);

// 永不过期
cache.Set("key2", value, 0);

// 1小时后过期
cache.Set("key3", value, 3600);

// 使用 TimeSpan
cache.Set("key4", value, TimeSpan.FromHours(1));
```

## 依赖注入

```csharp
// 注册缓存服务
services.AddSingleton<ICache>(MemoryCache.Instance);

// 或创建新实例
services.AddSingleton<ICache>(sp => new MemoryCache
{
    Capacity = 50000,
    Expire = 600
});
```

## 缓存键规范

建议使用冒号分隔的层级键名：

```
类型:标识[:子类型]
user:123
user:123:profile
order:2024:001
config:app:debug
```

## 最佳实践

### 1. 合理设置容量

```csharp
var cache = new MemoryCache
{
    Capacity = 100000,  // 根据内存调整
    Period = 60         // 清理间隔
};
```

### 2. 监听过期事件

```csharp
var cache = new MemoryCache();
cache.KeyExpired += (s, e) =>
{
    XTrace.WriteLine($"缓存过期: {e.Key}");
    // 可以在这里触发数据预热
};
```

### 3. 避免大 Key

```csharp
// 不推荐：存储大对象
cache.Set("bigdata", hugeList);

// 推荐：拆分存储
foreach (var item in hugeList)
{
    cache.Set($"item:{item.Id}", item);
}
```

### 4. 使用前缀隔离

```csharp
// 不同模块使用不同前缀
cache.Set("user:session:abc", data);
cache.Set("order:temp:123", data);

// 按前缀批量删除
cache.Remove("user:session:*");
```

## 相关链接

- [对象池 Pool](pool-对象池Pool.md)
- [字典缓存 DictionaryCache](dictionary_cache-字典缓存.md)
- [雪花算法 Snowflake](snowflake-雪花算法Snowflake.md)
