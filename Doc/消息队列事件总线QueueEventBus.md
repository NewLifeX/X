# 消息队列事件总线QueueEventBus

## 概述

`QueueEventBus<TEvent>` 是基于消息队列的分布式事件总线，继承自 `EventBus<TEvent>`，利用 `ICache` 的后端队列（如 `MemoryQueue` 或 Redis 队列）实现跨进程的事件发布与订阅。它结合了事件总线的本地订阅分发和消息队列的异步持久化能力。

**命名空间**：`NewLife.Caching`  
**文档地址**：https://newlifex.com/core/queue_event_bus

## 核心特性

- **分布式事件**：事件先进入后端队列，再由后台消费循环分发给本地订阅者
- **本地订阅**：订阅/取消订阅机制与传统 `EventBus` 一致
- **跨进程投递**：使用 `ICache.GetQueue<T>(topic)` 获取队列，多进程共享
- **优雅停止**：支持 `CancellationToken` 取消后台消费循环
- **链路追踪**：实现 `ITracerFeature`，消费时可自动埋点

## 快速开始

```csharp
using NewLife.Caching;

// 创建基于内存队列的事件总线
var cache = MemoryCache.Instance;
var bus = new QueueEventBus<String>(cache, "myTopic");

// 订阅事件
bus.Subscribe(msg =>
{
    Console.WriteLine($"收到: {msg}");
    return Task.CompletedTask;
}, "consumer1");

// 发布事件（进入队列）
await bus.PublishAsync("Hello, QueueEventBus!");

// 稍等片刻，后台消费线程会处理
await Task.Delay(100);
```

## 工作原理

```
发布者 ──PublishAsync──→ QueueEventBus ──Add──→ ICache队列
                                                    │
                                        后台消费线程 ←── TakeOneAsync
                                                    │
                                              分发给本地订阅者
                                                    ↓
                                              订阅者处理器
```

- 发布时：`PublishAsync` 将事件写入后端队列
- 订阅时：首次订阅自动启动后台消费任务
- 消费时：后台循环 `TakeOneAsync` 拉取消息，调用所有订阅者处理器
- 销毁时：取消消费任务，等待现有处理完成

## 与 EventBus 的区别

| 特性 | `EventBus<TEvent>` | `QueueEventBus<TEvent>` |
|------|-------------------|------------------------|
| 事件范围 | 进程内 | 跨进程（基于缓存队列） |
| 持久化 | 无 | 依赖后端队列（内存/Redis） |
| 消费模型 | 同步广播 | 后台拉取 + 广播 |
| 启动方式 | 即时 | 首次订阅时启动后台任务 |

## 注意事项

- 确保 `ICache` 实例支持 `IProducerConsumer<T>`（`GetQueue<T>` 方法）
- 销毁实例时应调用 `Dispose` 以停止后台消费循环
- 异常不会阻止后续消息消费（`ThrowOnHandlerError` 可配置）
