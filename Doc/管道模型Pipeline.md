# 管道模型Pipeline

## 概述

`IPipeline`/`Pipeline` 是 NewLife.Core 网络与消息处理的核心扩展机制，基于责任链模式将编解码、压缩、加密等逻辑拆分为独立的处理器节点，通过双向链表串联。每条连接对应独立的管道实例，收包（`Read`）沿头部到尾部正向传递，发包（`Write`）沿尾部到头部逆向传递。

**命名空间**：`NewLife.Model`  
**文档地址**：/core/pipeline

## 核心特性

- **双向链路**：同一管道同时支持解码（`Read`）和编码（`Write`）两个方向
- **链式传递**：前一个处理器的输出自动成为下一个的输入
- **生命周期事件**：`Open`（正向）/ `Close`（逆向）/ `Error`（正向）与网络会话同步
- **线程安全**：`Add`/`Remove`/`Clear` 内部加锁，链表修改安全
- **可组合**：按需叠加拆包、解密、序列化等处理器，无需修改核心逻辑

## 数据流方向

```
收包（Read，正向 Head → Tail）:
  Socket → Handler[0].Read → Handler[1].Read → ... → Handler[n].FireRead → 业务层

发包（Write，逆向 Tail → Head）:
  业务层 → Handler[n].Write → ... → Handler[1].Write → Handler[0].FireWrite → Socket

Open/Error（正向）   Close（逆向）
```

## 快速开始

```csharp
using NewLife.Model;
using NewLife.Net.Handlers;

// 1. 创建管道并注册处理器（顺序即正向处理顺序）
var pipeline = new Pipeline();
pipeline.Add(new LengthFieldCodec { Size = 2 });  // 粘包拆包
pipeline.Add(new StandardCodec());                 // 消息编解码

// 2. 创建上下文（通常由 NetServer/NetClient 自动完成）
var ctx = pipeline.CreateContext(session);

// 3. 收包：将原始 IPacket 传入管道，返回业务消息
var msg = pipeline.Read(ctx, receivedPacket);

// 4. 发包：将业务消息传入管道，返回编码后的 IPacket
var pk = pipeline.Write(ctx, myMessage) as IPacket;
session.Send(pk);
```

## API 参考

### IPipeline 接口

#### 管理处理器

```csharp
/// <summary>添加处理器到末尾</summary>
void Add(IPipelineHandler handler);

/// <summary>移除处理器</summary>
Boolean Remove(IPipelineHandler handler);

/// <summary>清空所有处理器</summary>
void Clear();
```

#### 执行操作

```csharp
/// <summary>读取（收包），正向 Head→Tail。多消息时返回 null 并多次回调 FireRead</summary>
Object? Read(IHandlerContext context, Object message);

/// <summary>写入（发包），逆向 Tail→Head</summary>
Object? Write(IHandlerContext context, Object message);

/// <summary>打开连接，正向传播</summary>
Boolean Open(IHandlerContext context);

/// <summary>关闭连接，逆向传播</summary>
Boolean Close(IHandlerContext context, String reason);

/// <summary>发生错误，正向传播</summary>
Boolean Error(IHandlerContext context, Exception exception);
```

### Pipeline 类属性

```csharp
public class Pipeline : IPipeline
{
    /// <summary>处理器集合（顺序）</summary>
    public IList<IPipelineHandler> Handlers { get; }

    /// <summary>头部处理器（正向起点）</summary>
    public IPipelineHandler? Head { get; }

    /// <summary>尾部处理器（逆向起点）</summary>
    public IPipelineHandler? Tail { get; }
}
```

### IPipelineHandler / Handler

```csharp
public interface IPipelineHandler
{
    /// <summary>前驱节点（逆向下一跳）</summary>
    IPipelineHandler? Prev { get; set; }

    /// <summary>后继节点（正向下一跳）</summary>
    IPipelineHandler? Next { get; set; }

    Object? Read(IHandlerContext context, Object message);
    Object? Write(IHandlerContext context, Object message);
    Boolean Open(IHandlerContext context);
    Boolean Close(IHandlerContext context, String reason);
    Boolean Error(IHandlerContext context, Exception exception);
}
```

`Handler` 是 `IPipelineHandler` 的抽象基类，默认实现是将消息传递给链表中的下一个节点，业务实现只需覆写关心的方法。

## 自定义处理器

### 示例：记录收发日志

```csharp
public class LogHandler : Handler
{
    public override Object? Read(IHandlerContext context, Object message)
    {
        XTrace.WriteLine("收到: {0}", message);
        return base.Read(context, message);  // 必须传递给下一节点
    }

    public override Object? Write(IHandlerContext context, Object message)
    {
        XTrace.WriteLine("发送: {0}", message);
        return base.Write(context, message);
    }
}

// 注册到管道
pipeline.Add(new LogHandler());
```

### 示例：加密/解密处理器

```csharp
public class EncryptHandler : Handler
{
    private readonly Byte[] _key;
    public EncryptHandler(Byte[] key) => _key = key;

    public override Object? Read(IHandlerContext context, Object message)
    {
        if (message is IPacket pk)
        {
            var decrypted = Decrypt(pk.GetSpan(), _key);
            return base.Read(context, new ArrayPacket(decrypted));
        }
        return base.Read(context, message);
    }

    public override Object? Write(IHandlerContext context, Object message)
    {
        if (message is IPacket pk)
        {
            var encrypted = Encrypt(pk.GetSpan(), _key);
            return base.Write(context, new ArrayPacket(encrypted));
        }
        return base.Write(context, message);
    }
}
```

## 与 NetServer/NetClient 集成

NetServer 和 NetClient 自动为每条连接创建管道，可在 `NewSession` 事件中注册编解码器：

```csharp
var server = new NetServer { Port = 12345 };

server.NewSession += (sender, e) =>
{
    var pipeline = e.Session.Host.Pipeline;
    pipeline.Add(new LengthFieldCodec { Size = 4 });
    pipeline.Add(new StandardCodec());
};

server.Start();
```

## 最佳实践

- **顺序设计**：拆包器放在协议解码器前面；错误顺序会导致收到残包。
- **按连接隔离**：有内部状态的处理器（如拆包缓存）必须为每条连接新建实例。
- **提前 return 时释放**：截断链路时必须释放持有的 `IPacket`，防止内存泄漏。
- **不要吞异常**：调用 `base.Error` 或 `context.FireError` 向下传递，交由根处理器统一处理。
- **继承 Handler 而非接口**：避免忘记传递消息给下一节点。
