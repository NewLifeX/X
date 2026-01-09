# EventBus 使用手册

本文档面向 NewLife.Core 的事件总线能力，涵盖接口模型、默认实现 `EventBus<TEvent>` 的进程内分发、基于主题的 `EventHub<TEvent>` 路由，以及基于队列的 `QueueEventBus<TEvent>`。

> 代码位置：`NewLife.Core\Messaging\IEventBus.cs`、`NewLife.Core\Messaging\EventHub.cs`、`NewLife.Core\Caching\QueueEventBus.cs`

---

## 1. 设计概览

### 1.1 适用场景

- **进程内事件**：同一进程内发布/订阅，低延迟、无持久化（`EventBus<TEvent>`）。
- **多主题事件路由**：把带 `topic` 的网络消息路由到对应事件总线或回调（`EventHub<TEvent>`）。
- **借助缓存队列的“跨进程”投递**：发布进队列、由后台消费循环拉取并分发到本地订阅者（`QueueEventBus<TEvent>`）。

### 1.2 核心特点

- **发布异步、订阅同步**：发布/处理使用 `PublishAsync`/`HandleAsync`；订阅与取消使用 `Subscribe`/`Unsubscribe`。
- **幂等订阅**：同一 `clientId` 重复订阅会覆盖上一次。
- **最佳努力分发**：默认单个处理器异常不会影响其它处理器；可通过 `ThrowOnHandlerError` 改为严格模式。
- **上下文传递**：通过 `IEventContext`（默认实现 `EventContext`）在发布者、订阅者及中间层之间传递数据。
- **链路追踪**：若事件实现 `ITraceMessage` 且 `TraceId` 为空，`EventBus<TEvent>` 发布时会自动写入当前埋点的 TraceId。

---

## 2. 接口模型

### 2.1 `IEventBus`（非泛型）

用于统一持有不同事件类型的总线引用（例如放入 `IEventContext.EventBus`）。

- `Task<Int32> PublishAsync(Object event, IEventContext? context = null, CancellationToken cancellationToken = default)`

> 说明：默认实现通常会把 `Object` 强转为实际的 `TEvent`。

### 2.2 `IEventBus<TEvent>`（泛型总线）

- `Task<Int32> PublishAsync(TEvent event, IEventContext? context = null, CancellationToken cancellationToken = default)`
- `Boolean Subscribe(IEventHandler<TEvent> handler, String clientId = "")`
- `Boolean Unsubscribe(String clientId = "")`

#### `clientId` 的语义

- 用于识别订阅者。
- 相同 `clientId` 重复订阅会覆盖旧订阅（幂等）。
- 在某些实现里可用于“消费组/分组”语义。

### 2.3 `IAsyncEventBus<TEvent>`（异步订阅/取消）

适用于订阅需要网络往返或其它异步动作的场景。

- `Task<Boolean> SubscribeAsync(IEventHandler<TEvent> handler, String clientId = "", CancellationToken cancellationToken = default)`
- `Task<Boolean> UnsubscribeAsync(String clientId = "", CancellationToken cancellationToken = default)`

### 2.4 `IEventHandler<TEvent>`（事件处理器）

- `Task HandleAsync(TEvent event, IEventContext? context, CancellationToken cancellationToken)`

建议：处理器尽量幂等，并尊重 `cancellationToken`。

### 2.5 `IEventContext` / `EventContext`

- `IEventBus EventBus { get; }`

`EventContext` 还提供：

- `String? Topic`：多层次事件架构（例如 EventHub）中使用。
- `String? ClientId`：发送方标识，用于分发时“不要分发给自己”。
- `IDictionary<String, Object?> Items` 与索引器：携带扩展数据。

> `EventBus<TEvent>` 内部会池化 `EventContext`：当发布时未传入 `context`，会从对象池获取并在分发后归还。

---

## 3. 默认事件总线 `EventBus<TEvent>`

### 3.1 行为语义

- **即时分发，不存储**：不在线的订阅者收不到历史消息。
- **顺序调用处理器**：对当前订阅快照逐个调用 `HandleAsync`。
- **异常策略**：
  - `ThrowOnHandlerError = false`（默认）：记录错误日志，继续分发。
  - `ThrowOnHandlerError = true`：遇到第一个处理器异常立即抛出，中断分发。
- **排除发送方**：如果 `context` 是 `EventContext` 且设置了 `ClientId`，分发时会跳过 `clientId` 相同的订阅者。

### 3.2 快速开始（进程内）

1) 定义事件类型：

- 建议使用轻量 DTO（class/record 均可）。

2) 订阅：

- 通过实现 `IEventHandler<TEvent>`，或使用扩展方法直接订阅委托。

3) 发布：

- 调用 `PublishAsync`，返回已成功处理该事件的处理器数量。

示例（委托订阅）：

- `bus.Subscribe(e => Console.WriteLine(e));`
- `await bus.PublishAsync(myEvent);`

> 扩展方法位于 `EventBusExtensions`，会把委托包装为 `DelegateEventHandler<TEvent>`。

### 3.3 使用上下文（传递附加数据）

你可以在发布时传入 `EventContext`，用于：

- 设置 `Topic`/`ClientId`（尤其在 `EventHub<TEvent>` 场景）；
- 通过 `Items` 保存自定义数据（例如 `ext["Raw"]` 带原始报文）；
- 在处理器中读取上下文以实现协作逻辑。

注意事项：

- 若传入的 `context` 为 `null`，`EventBus<TEvent>` 可能从对象池创建上下文，分发后会调用 `Reset()` 并归还；处理器不应保存该上下文引用到异步生命周期之外。

### 3.4 订阅/取消订阅

- `Subscribe(handler, clientId)`：覆盖同 `clientId` 的旧处理器。
- `Unsubscribe(clientId)`：移除对应订阅。

建议：

- 为长期订阅者指定稳定的 `clientId`，便于重连覆盖与取消订阅。

---

## 4. `EventHub<TEvent>`：按主题路由的事件枢纽

`EventHub<TEvent>` 的职责是将带主题的输入消息分发到对应的事件总线或回调。

### 4.1 消息格式

仅处理以 `event#` 开头的消息：

- `event#topic#clientId#message`

字段说明：

- `topic`：主题名称。
- `clientId`：发送方标识/订阅分组。
- `message`：
  - 事件 JSON（通常为 `TEvent` 的 JSON）；
  - 或控制指令：`subscribe` / `unsubscribe`。

### 4.2 注册方式

- `Add(topic, IEventBus<TEvent> bus)`：把某个总线固定绑定到主题。
- `Add(topic, IEventHandler<TEvent> dispatcher)`：把某个处理器/回调绑到主题（不经过总线）。
- `GetEventBus(topic, clientId)`：通过 `Factory` 延迟创建并缓存主题总线；如果未设置 `Factory`，默认创建 `EventBus<TEvent>`。

### 4.3 订阅/取消订阅（控制指令）

当收到：

- `event#topic#clientId#subscribe`

`EventHub<TEvent>` 会：

- 要求 `context` 中提供 `Handler`：`(context as IExtend)?["Handler"] is IEventHandler<TEvent>`。
- `GetEventBus(topic, clientId)` 获取主题总线。
- `bus.Subscribe(handler, clientId)` 绑定订阅。

当收到：

- `event#topic#clientId#unsubscribe`

会：

- 找到主题总线并 `Unsubscribe(clientId)`。
- 若总线为 `EventBus<TEvent>` 且没有任何订阅者，则从枢纽中移除该主题的总线与分发器（避免主题长期占用内存）。

### 4.4 分发路径与返回值

- 命中主题总线：`bus.PublishAsync(event, context)`，返回该总线的处理器计数。
- 未命中总线但命中分发器：调用 `dispatcher.HandleAsync`，返回 `1`。
- 不匹配/解析失败/未注册：返回 `0`。

### 4.5 上下文写入

在 `DispatchAsync(topic, clientId, ...)` 中：

- 如果 `context` 是 `EventContext`：写入 `Topic` / `ClientId`。
- 否则若 `context` 支持 `IExtend`：写入 `ext["Topic"]` / `ext["ClientId"]`。

在 `HandleAsync` 收到网络消息时：

- 会把原始输入保存到 `context["Raw"]`（若 `context` 支持 `IExtend`），便于订阅者零拷贝转发/诊断。

---

## 5. `QueueEventBus<TEvent>`：基于队列的事件总线

`QueueEventBus<TEvent>` 继承自 `EventBus<TEvent>`，但改变了“发布”的语义：

- `PublishAsync` 不再进程内直接分发，而是 **写入队列**。
- 订阅时启动一个后台消费循环，从队列拉取消息并调用基类的 `DispatchAsync` 分发到本地订阅者。

### 5.1 使用方式

- 创建：`new QueueEventBus<TEvent>(cache, topic)`
- 订阅：首次订阅会启动后台 LongRunning 消费任务。
- 发布：写入队列；返回值为队列 `Add` 的结果（通常为 1）。
- 释放：调用 `Dispose()` 会取消消费循环并等待后台任务退出。

### 5.2 取消与关闭

- `Dispose()` 会：
  - 取消内部 `CancellationTokenSource`；
  - 等待后台任务最多约 3 秒；
  - 然后释放 CTS。

注意：

- 释放后再发布消息，会继续写入队列（队列属于外部 `ICache`），但本实例不再消费。

---

## 6. 委托订阅：`EventBusExtensions` 与 `DelegateEventHandler<TEvent>`

### 6.1 常用订阅形式

`EventBusExtensions` 提供多种 `Subscribe`/`SubscribeAsync` 便捷扩展：

- `Action<TEvent>`
- `Action<TEvent, IEventContext>`
- `Func<TEvent, Task>`
- `Func<TEvent, IEventContext, CancellationToken, Task>`

注意：

- 内部通过 `DelegateEventHandler<TEvent>` 适配到 `IEventHandler<TEvent>`。

### 6.2 取消令牌

只有最后一种委托签名可以直接拿到 `CancellationToken`。

---

## 7. 线程安全与并发语义

- `EventBus<TEvent>`：
  - 订阅集合使用 `ConcurrentDictionary<String, IEventHandler<TEvent>>`。
  - 分发时枚举字典是快照语义：分发过程中订阅变化不保证实时可见。
- `EventHub<TEvent>`：
  - `_eventBuses` / `_dispatchers` 均为 `ConcurrentDictionary`。
  - `GetEventBus` 并发下可能多次创建，但最终仅缓存一份实例。

---

## 8. 错误处理与最佳实践

### 8.1 处理器异常

- 默认：记录日志并继续。
- 需要强一致/严格失败：设置 `EventBus<TEvent>.ThrowOnHandlerError = true`。

### 8.2 幂等性

- 事件处理器建议幂等，避免重复投递带来的副作用。

### 8.3 不要持有池化上下文

- 当 `context` 由 `EventBus<TEvent>` 自动创建时，它来自对象池，分发结束会被重置并复用。
- 处理器内如需长期保存信息，应复制所需字段/数据，而不是保存 `context` 引用。

### 8.4 `clientId` 的使用建议

- 客户端订阅：使用稳定的 `clientId`，便于覆盖旧订阅。
- 发布者：在 `EventHub<TEvent>` 场景里，`clientId` 会被用于避免“分发给自己”。

---

## 9. 常见用法组合

### 9.1 进程内：一个发布者 + 多个订阅者

- 使用 `EventBus<TEvent>`。
- 不需要 `EventHub<TEvent>`。

### 9.2 网络场景：按 topic 订阅/发布

- 使用 `EventHub<TEvent>` 作为统一入口：
  - 输入：收到网络字符串或 `IPacket`。
  - 输出：分发到 topic 对应的 `IEventBus<TEvent>` 或回调。
- 若需要按 topic 创建总线：提供 `IEventBusFactory`，让枢纽按需创建。

### 9.3 类 MQ 场景：使用缓存队列

- 使用 `QueueEventBus<TEvent>`：
  - 发布写入队列；
  - 本地订阅者由后台消费循环拉取队列再分发。

---

## 10. 相关测试用例（可参考）

- `XUnitTest.Core\Messaging\EventBusTests.cs`
- `XUnitTest.Core\Messaging\EventHubTests.cs`
- `XUnitTest.Core\Caching\QueueEventBusTests.cs`

---

## 11. FAQ

### Q1：`PublishAsync` 返回值代表什么？

默认实现（`EventBus<TEvent>`）：返回成功执行 `HandleAsync` 的处理器数量（处理器抛异常且 `ThrowOnHandlerError=false` 则不计入成功）。

### Q2：为什么 `EventBus<TEvent>` 有非泛型 `IEventBus`？

用于在不关心事件具体类型时（例如统一上下文或中间件管道）持有一个总线引用。

### Q3：如何在 `EventHub<TEvent>` 的 subscribe 指令中提供处理器？

构造 `EventContext` 并写入 `context["Handler"] = myHandler`，然后调用 `HandleAsync("event#...#subscribe", context)`。

---

## 12. 版本与兼容性

- 本模块面向多目标框架（`net45` 至 `net10`）并使用异步 API。
- 在较老框架下，部分 `Task` 相关 API 会使用兼容实现（例如 `TaskEx`）。
