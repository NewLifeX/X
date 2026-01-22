# 并行模型 Actor

## 概述

`Actor` 是 NewLife.Core 中的无锁并行编程模型，基于消息队列实现线程安全的异步处理。每个 Actor 拥有独立的消息邮箱和处理线程，通过消息传递进行通信，避免了传统锁机制带来的复杂性和性能问题。

**命名空间**：`NewLife.Model`  
**文档地址**：https://newlifex.com/core/actor

## 核心特性

- **无锁设计**：通过消息队列隔离状态，无需显式加锁
- **独立线程**：每个 Actor 使用独立线程处理消息，不影响线程池
- **批量处理**：支持批量消费消息，提高吞吐量
- **容量限制**：支持设置消息队列最大容量，防止内存溢出
- **自动启动**：发送消息时自动启动 Actor
- **性能追踪**：集成 `ITracer` 支持链路追踪

## 快速开始

```csharp
using NewLife.Model;

// 定义 Actor
public class MyActor : Actor
{
    protected override Task ReceiveAsync(ActorContext context, CancellationToken cancellationToken)
    {
        var message = context.Message;
        Console.WriteLine($"收到消息: {message}");
        return Task.CompletedTask;
    }
}

// 使用 Actor
var actor = new MyActor();

// 发送消息
actor.Tell("Hello");
actor.Tell("World");

// 等待处理完成后停止
actor.Stop(5000);
```

## API 参考

### IActor 接口

```csharp
public interface IActor
{
    /// <summary>添加消息，驱动内部处理</summary>
    /// <param name="message">消息对象</param>
    /// <param name="sender">发送者Actor</param>
    /// <returns>返回待处理消息数</returns>
    Int32 Tell(Object message, IActor? sender = null);
}
```

### ActorContext 类

```csharp
public class ActorContext
{
    /// <summary>发送者</summary>
    public IActor? Sender { get; set; }
    
    /// <summary>消息</summary>
    public Object? Message { get; set; }
}
```

### Actor 基类

#### 属性

```csharp
/// <summary>名称</summary>
public String Name { get; set; }

/// <summary>是否启用</summary>
public Boolean Active { get; }

/// <summary>受限容量。最大可堆积的消息数，默认Int32.MaxValue</summary>
public Int32 BoundedCapacity { get; set; }

/// <summary>批大小。每次处理消息数，默认1</summary>
public Int32 BatchSize { get; set; }

/// <summary>是否长时间运行。默认true，使用独立线程</summary>
public Boolean LongRunning { get; set; }

/// <summary>当前队列长度</summary>
public Int32 QueueLength { get; }

/// <summary>性能追踪器</summary>
public ITracer? Tracer { get; set; }
```

#### Tell - 发送消息

```csharp
public virtual Int32 Tell(Object message, IActor? sender = null)
```

向 Actor 发送消息。如果 Actor 未启动，会自动启动。

**参数**：
- `message`：消息对象，可以是任意类型
- `sender`：发送者 Actor，用于回复消息

**返回值**：当前待处理的消息数

**示例**：
```csharp
var actor = new MyActor();

// 发送简单消息
actor.Tell("Hello");

// 发送复杂对象
actor.Tell(new { Id = 1, Name = "Test" });

// 带发送者
actor.Tell("Ping", senderActor);
```

#### Start - 启动 Actor

```csharp
public virtual Task? Start()
public virtual Task? Start(CancellationToken cancellationToken)
```

手动启动 Actor。通常不需要手动调用，`Tell` 会自动启动。

**示例**：
```csharp
var actor = new MyActor();

// 手动启动
actor.Start();

// 带取消令牌启动
using var cts = new CancellationTokenSource();
actor.Start(cts.Token);
```

#### Stop - 停止 Actor

```csharp
public virtual Boolean Stop(Int32 msTimeout = 0)
```

停止 Actor，不再接受新消息。

**参数**：
- `msTimeout`：等待毫秒数。0=不等待，-1=无限等待

**返回值**：是否在超时前完成所有消息处理

**示例**：
```csharp
// 立即停止，不等待
actor.Stop(0);

// 等待最多5秒
var completed = actor.Stop(5000);
if (!completed)
    Console.WriteLine("有消息未处理完成");

// 无限等待
actor.Stop(-1);
```

#### ReceiveAsync - 处理消息

```csharp
// 单条处理（BatchSize=1）
protected virtual Task ReceiveAsync(ActorContext context, CancellationToken cancellationToken)

// 批量处理（BatchSize>1）
protected virtual Task ReceiveAsync(ActorContext[] contexts, CancellationToken cancellationToken)
```

子类重写此方法实现消息处理逻辑。

## 使用场景

### 1. 日志收集器

```csharp
public class LogActor : Actor
{
    private readonly StreamWriter _writer;
    
    public LogActor(String filePath)
    {
        Name = "LogActor";
        BatchSize = 100;  // 批量写入
        BoundedCapacity = 10000;  // 限制队列
        
        _writer = new StreamWriter(filePath, true) { AutoFlush = false };
    }
    
    protected override async Task ReceiveAsync(ActorContext[] contexts, CancellationToken cancellationToken)
    {
        foreach (var ctx in contexts)
        {
            if (ctx.Message is String log)
            {
                await _writer.WriteLineAsync(log);
            }
        }
        await _writer.FlushAsync();
    }
    
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);
        _writer?.Dispose();
    }
}

// 使用
var logger = new LogActor("app.log");
logger.Tell($"[{DateTime.Now:HH:mm:ss}] 应用启动");
logger.Tell($"[{DateTime.Now:HH:mm:ss}] 处理请求");
```

### 2. 消息处理器

```csharp
public class MessageProcessor : Actor
{
    private readonly IMessageHandler _handler;
    
    public MessageProcessor(IMessageHandler handler)
    {
        Name = "MessageProcessor";
        _handler = handler;
    }
    
    protected override async Task ReceiveAsync(ActorContext context, CancellationToken cancellationToken)
    {
        if (context.Message is Message msg)
        {
            try
            {
                await _handler.HandleAsync(msg, cancellationToken);
                
                // 回复发送者
                context.Sender?.Tell(new Ack { MessageId = msg.Id });
            }
            catch (Exception ex)
            {
                context.Sender?.Tell(new Error { MessageId = msg.Id, Exception = ex });
            }
        }
    }
}
```

### 3. 数据聚合器

```csharp
public class DataAggregator : Actor
{
    private readonly Dictionary<String, Int32> _counts = new();
    private DateTime _lastFlush = DateTime.Now;
    
    public DataAggregator()
    {
        Name = "DataAggregator";
        BatchSize = 50;
    }
    
    protected override Task ReceiveAsync(ActorContext[] contexts, CancellationToken cancellationToken)
    {
        foreach (var ctx in contexts)
        {
            if (ctx.Message is String key)
            {
                _counts.TryGetValue(key, out var count);
                _counts[key] = count + 1;
            }
        }
        
        // 每分钟输出一次统计
        if ((DateTime.Now - _lastFlush).TotalMinutes >= 1)
        {
            foreach (var kv in _counts)
            {
                Console.WriteLine($"{kv.Key}: {kv.Value}");
            }
            _counts.Clear();
            _lastFlush = DateTime.Now;
        }
        
        return Task.CompletedTask;
    }
}
```

### 4. Actor 之间通信

```csharp
public class PingActor : Actor
{
    protected override Task ReceiveAsync(ActorContext context, CancellationToken cancellationToken)
    {
        if (context.Message is String msg && msg == "Ping")
        {
            Console.WriteLine("PingActor 收到 Ping，发送 Pong");
            context.Sender?.Tell("Pong", this);
        }
        return Task.CompletedTask;
    }
}

public class PongActor : Actor
{
    protected override Task ReceiveAsync(ActorContext context, CancellationToken cancellationToken)
    {
        if (context.Message is String msg && msg == "Pong")
        {
            Console.WriteLine("PongActor 收到 Pong");
        }
        return Task.CompletedTask;
    }
}

// 使用
var ping = new PingActor();
var pong = new PongActor();

// pong 发送 Ping 给 ping，ping 会回复 Pong
ping.Tell("Ping", pong);
```

### 5. 限流处理器

```csharp
public class RateLimitedActor : Actor
{
    private readonly SemaphoreSlim _semaphore;
    private readonly Int32 _maxConcurrency;
    
    public RateLimitedActor(Int32 maxConcurrency = 10)
    {
        _maxConcurrency = maxConcurrency;
        _semaphore = new SemaphoreSlim(maxConcurrency);
        BatchSize = maxConcurrency;
    }
    
    protected override async Task ReceiveAsync(ActorContext[] contexts, CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();
        
        foreach (var ctx in contexts)
        {
            await _semaphore.WaitAsync(cancellationToken);
            
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await ProcessAsync(ctx.Message, cancellationToken);
                }
                finally
                {
                    _semaphore.Release();
                }
            }, cancellationToken));
        }
        
        await Task.WhenAll(tasks);
    }
    
    private async Task ProcessAsync(Object? message, CancellationToken cancellationToken)
    {
        // 处理逻辑
        await Task.Delay(100, cancellationToken);
    }
}
```

## 最佳实践

### 1. 合理设置批大小

```csharp
// IO密集型：较大批次
var ioActor = new IoActor { BatchSize = 100 };

// CPU密集型：较小批次
var cpuActor = new CpuActor { BatchSize = 10 };

// 实时性要求高：单条处理
var realtimeActor = new RealtimeActor { BatchSize = 1 };
```

### 2. 设置队列容量

```csharp
// 防止内存溢出
var actor = new MyActor
{
    BoundedCapacity = 10000  // 最多堆积1万条消息
};

// 检查队列长度
if (actor.QueueLength > 5000)
{
    Console.WriteLine("警告：消息积压");
}
```

### 3. 优雅停止

```csharp
// 停止接收新消息，等待现有消息处理完成
var completed = actor.Stop(30_000);  // 最多等30秒

if (!completed)
{
    Console.WriteLine($"有 {actor.QueueLength} 条消息未处理");
}
```

### 4. 异常处理

```csharp
public class SafeActor : Actor
{
    protected override async Task ReceiveAsync(ActorContext context, CancellationToken cancellationToken)
    {
        try
        {
            await ProcessAsync(context.Message);
        }
        catch (Exception ex)
        {
            // 记录日志，不抛出异常
            XTrace.WriteException(ex);
            
            // 可选：发送到死信队列
            DeadLetterActor?.Tell(new DeadLetter
            {
                Message = context.Message,
                Exception = ex
            });
        }
    }
}
```

### 5. 性能追踪

```csharp
var actor = new MyActor
{
    Tracer = new DefaultTracer()  // 或使用星尘追踪
};

// 追踪信息会自动记录：
// - actor:Start
// - actor:Loop
// - actor:Stop
```

## 与其他并发模型对比

| 特性 | Actor | Task/async | 锁 |
|------|-------|------------|-----|
| 线程安全 | 天然安全 | 需要注意 | 需要显式加锁 |
| 编程复杂度 | 中等 | 低 | 高 |
| 适用场景 | IO密集 | 通用 | 共享状态 |
| 背压处理 | 支持 | 不支持 | 不支持 |
| 消息顺序 | 保证 | 不保证 | 不适用 |

## 相关链接

- [高级定时器 TimerX](timerx-高级定时器TimerX.md)
- [轻量级应用主机 Host](host-轻量级应用主机Host.md)
- [链路追踪 ITracer](tracer-链路追踪ITracer.md)
