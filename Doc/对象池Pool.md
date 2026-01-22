# 对象池 Pool

## 概述

`Pool<T>` 是 NewLife.Core 中的轻量级对象池实现，采用数组无锁设计，通过 CAS 操作实现高性能的对象复用。对象池可以显著减少频繁创建销毁对象带来的 GC 压力，特别适合高频场景。

**命名空间**：`NewLife.Collections`  
**文档地址**：https://newlifex.com/core/object_pool

## 核心特性

- **无锁设计**：使用 `Interlocked.CompareExchange` 实现无锁并发
- **热点槽位**：单独维护最热对象，提升访问速度
- **GC 友好**：支持二代 GC 时自动清理
- **高性能**：O(N) 扫描，结构体持有引用避免额外分配
- **内置池**：提供 `StringBuilder`、`MemoryStream` 等常用池

## 快速开始

### 基本使用

```csharp
using NewLife.Collections;

// 创建对象池
var pool = new Pool<MyObject>();

// 获取对象
var obj = pool.Get();

try
{
    // 使用对象...
    obj.DoSomething();
}
finally
{
    // 归还对象
    pool.Return(obj);
}
```

### 使用内置 StringBuilder 池

```csharp
using NewLife.Collections;

// 从池中获取 StringBuilder
var sb = Pool.StringBuilder.Get();

sb.Append("Hello ");
sb.Append("World");

// 归还并获取结果
var result = sb.Return(true);  // 返回 "Hello World"

// 或不需要结果时
sb.Return(false);
```

## API 参考

### IPool&lt;T&gt; 接口

```csharp
public interface IPool<T>
{
    /// <summary>对象池大小</summary>
    Int32 Max { get; set; }
    
    /// <summary>获取对象</summary>
    T Get();
    
    /// <summary>归还对象</summary>
    Boolean Return(T value);
    
    /// <summary>清空对象池</summary>
    Int32 Clear();
}
```

### Pool&lt;T&gt; 类

```csharp
public class Pool<T> : IPool<T> where T : class
{
    /// <summary>对象池大小。默认 CPU*2，最小8</summary>
    public Int32 Max { get; set; }
    
    /// <summary>获取对象，池空时创建新实例</summary>
    public virtual T Get();
    
    /// <summary>归还对象</summary>
    public virtual Boolean Return(T value);
    
    /// <summary>清空对象池</summary>
    public virtual Int32 Clear();
    
    /// <summary>创建对象（可重写）</summary>
    protected virtual T? OnCreate();
}
```

#### 构造函数

```csharp
// 默认构造，大小为 CPU*2
var pool = new Pool<MyObject>();

// 指定大小
var pool = new Pool<MyObject>(100);

// 启用 GC 清理（protected）
protected Pool(Int32 max, Boolean useGcClear)
```

## 内置对象池

### StringBuilder 池

```csharp
public static class Pool
{
    /// <summary>字符串构建器池</summary>
    public static IPool<StringBuilder> StringBuilder { get; set; }
}
```

**使用示例**：
```csharp
var sb = Pool.StringBuilder.Get();
sb.Append("Name: ");
sb.Append(name);
sb.AppendLine();

// 方式1：归还并获取结果
var result = sb.Return(true);

// 方式2：仅归还
sb.Return(false);
```

### StringBuilderPool 类

```csharp
public class StringBuilderPool : Pool<StringBuilder>
{
    /// <summary>初始容量。默认100</summary>
    public Int32 InitialCapacity { get; set; }
    
    /// <summary>最大容量。超过不入池，默认4K</summary>
    public Int32 MaximumCapacity { get; set; }
}
```

归还时自动清空内容，超过最大容量的不放入池中。

## 使用场景

### 1. 高频对象复用

```csharp
public class MessageProcessor
{
    private readonly Pool<Message> _pool = new();
    
    public void Process(Byte[] data)
    {
        var msg = _pool.Get();
        try
        {
            msg.Parse(data);
            HandleMessage(msg);
        }
        finally
        {
            msg.Reset();  // 重置状态
            _pool.Return(msg);
        }
    }
}
```

### 2. 缓冲区池

```csharp
public class BufferPool : Pool<Byte[]>
{
    public Int32 BufferSize { get; set; } = 4096;
    
    protected override Byte[] OnCreate() => new Byte[BufferSize];
    
    public override Boolean Return(Byte[] value)
    {
        // 大小不匹配不入池
        if (value.Length != BufferSize) return false;
        
        // 清空数据
        Array.Clear(value, 0, value.Length);
        
        return base.Return(value);
    }
}

// 使用
var bufferPool = new BufferPool { BufferSize = 8192 };
var buffer = bufferPool.Get();
try
{
    var read = stream.Read(buffer, 0, buffer.Length);
    // 处理数据...
}
finally
{
    bufferPool.Return(buffer);
}
```

### 3. 数据库连接池

```csharp
public class ConnectionPool : Pool<DbConnection>
{
    public String ConnectionString { get; set; }
    
    protected override DbConnection OnCreate()
    {
        var conn = new MySqlConnection(ConnectionString);
        conn.Open();
        return conn;
    }
    
    public override Boolean Return(DbConnection value)
    {
        // 连接已断开则不入池
        if (value.State != ConnectionState.Open) return false;
        
        return base.Return(value);
    }
}
```

### 4. 临时集合

```csharp
public class ListPool<T> : Pool<List<T>>
{
    public Int32 MaxCapacity { get; set; } = 1000;
    
    protected override List<T> OnCreate() => new(16);
    
    public override Boolean Return(List<T> value)
    {
        if (value.Capacity > MaxCapacity) return false;
        
        value.Clear();
        return base.Return(value);
    }
}

// 使用
var listPool = new ListPool<Int32>();
var list = listPool.Get();
try
{
    list.Add(1);
    list.Add(2);
    // 处理...
}
finally
{
    listPool.Return(list);
}
```

### 5. 结合 using 模式

```csharp
public class PooledObject<T> : IDisposable where T : class
{
    private readonly IPool<T> _pool;
    public T Value { get; }
    
    public PooledObject(IPool<T> pool)
    {
        _pool = pool;
        Value = pool.Get();
    }
    
    public void Dispose()
    {
        _pool.Return(Value);
    }
}

// 使用
using (var pooled = new PooledObject<StringBuilder>(Pool.StringBuilder))
{
    pooled.Value.Append("Hello");
    var result = pooled.Value.ToString();
}
```

## 自定义对象池

```csharp
public class MyObjectPool : Pool<MyObject>
{
    public MyObjectPool() : base(100) { }  // 池大小100
    
    protected override MyObject OnCreate()
    {
        // 自定义创建逻辑
        return new MyObject
        {
            Id = Guid.NewGuid(),
            CreateTime = DateTime.Now
        };
    }
    
    public override Boolean Return(MyObject value)
    {
        // 归还前重置对象
        value.Reset();
        
        // 验证对象状态
        if (!value.IsValid) return false;
        
        return base.Return(value);
    }
}
```

## 性能优化

### 1. 预热对象池

```csharp
var pool = new Pool<MyObject>(50);

// 预热：创建一批对象放入池中
for (var i = 0; i < 50; i++)
{
    var obj = new MyObject();
    pool.Return(obj);
}
```

### 2. 合理设置大小

```csharp
// 根据并发量设置
var pool = new Pool<MyObject>(Environment.ProcessorCount * 4);
```

### 3. 避免大对象

```csharp
// 大对象可能影响 GC，考虑使用 ArrayPool
var buffer = ArrayPool<Byte>.Shared.Rent(1024 * 1024);
try
{
    // 使用缓冲区
}
finally
{
    ArrayPool<Byte>.Shared.Return(buffer);
}
```

## 与 ArrayPool 对比

| 特性 | Pool&lt;T&gt; | ArrayPool&lt;T&gt; |
|------|--------------|-------------------|
| 目标类型 | 引用类型 | 数组 |
| 线程安全 | 是（CAS） | 是 |
| GC 清理 | 可选 | 自动 |
| 大小调整 | 固定 | 动态 |
| 适用场景 | 对象复用 | 缓冲区 |

## 最佳实践

1. **及时归还**：使用 try-finally 确保对象归还
2. **重置状态**：归还前清理对象内部状态
3. **验证对象**：归还时检查对象有效性
4. **合理大小**：根据并发量设置池大小
5. **避免泄漏**：确保异常路径也能归还对象

## 相关链接

- [缓存系统 ICache](cache-缓存系统ICache.md)
- [字符串扩展 StringHelper](string_helper-字符串扩展StringHelper.md)
- [数据扩展 IOHelper](io_helper-数据扩展IOHelper.md)
