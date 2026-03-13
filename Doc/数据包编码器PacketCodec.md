# 数据包编码器PacketCodec

## 概述

`PacketCodec` 是 NewLife.Core 处理 TCP 粘包拆包的核心组件，广泛用于长连接协议的原始数据流解析。它通过"快速路径"和"慢速路径"双模式保证高性能：无缓存时直接从原始字节切片完整帧（零拷贝），有缓存时合并到内部 `MemoryStream` 后继续尝试解析。

**命名空间**：`NewLife.Messaging`  
**文档地址**：https://newlifex.com/core/packet_codec

## 核心特性

- **快速路径**：无残余缓存时，`Parse` 直接在原始 `IPacket` 上切片，零内存分配
- **慢速路径**：有残余缓存时，合并新数据后重新解析
- **超大包支持**：支持超过 64KB 的数据包
- **缓存过期**：超过 `Expire` 时间仍未凑齐完整包时自动丢弃，防止内存泄漏
- **上限保护**：`MaxCache` 限制缓存最大字节数，防止恶意数据导致内存耗尽
- **线程安全**：内部对慢速路径加锁，快速路径无锁

## 快速开始

```csharp
using NewLife.Messaging;

// 每个 TCP 连接创建一个独立实例
var codec = new PacketCodec
{
    // 从数据头读取完整包长度的委托（返回0/负值表示数据不足）
    GetLength2 = span =>
    {
        if (span.Length < 4) return 0;
        return span[0] | (span[1] << 8) | (span[2] << 16) | (span[3] << 24);
    },
    MaxCache = 4 * 1024 * 1024,  // 4MB 上限
};

// 每次收到网络数据调用（receivedPacket 可能是残包或多包）
var packets = codec.Parse(receivedPacket);
foreach (var pk in packets)
{
    // pk 是一个完整的业务包，可直接解析
    ProcessPacket(pk);
}
```

## API 参考

### 属性

```csharp
/// <summary>内部缓存流。用于存储不完整数据包，一般无需直接操作</summary>
public MemoryStream? Stream { get; set; }

/// <summary>获取包长度的委托（新接口，性能更优）。返回0/负值表示数据不足</summary>
public GetLengthDelegate? GetLength2 { get; set; }

/// <summary>最后一次解包成功时间</summary>
public DateTime Last { get; set; }

/// <summary>缓存有效期（毫秒）。超时后丢弃残余缓存，默认 5_000</summary>
public Int32 Expire { get; set; } = 5_000;

/// <summary>最大缓存字节数。默认 1MB</summary>
public Int32 MaxCache { get; set; } = 1024 * 1024;

/// <summary>APM 性能追踪器</summary>
public ITracer? Tracer { get; set; }
```

### 方法

#### Parse - 解析数据包

```csharp
public virtual IList<IPacket> Parse(IPacket pk)
```

**参数**：
- `pk`：本次收到的原始数据包（可以是残包、完整包或多个包）

**返回**：完整业务数据包列表（可能为空列表，表示数据不足需等待后续数据）

**工作流程**：

1. 若内部缓存为空，直接在 `pk` 上切片解析（快速路径）
2. 若已有缓存或快速路径有剩余，将新数据追加到 `MemoryStream`（慢速路径）
3. 调用 `GetLength2` 判断当前缓存是否已有完整包
4. 检查缓存过期（超过 `Expire` 且已有新数据时清空旧缓存）
5. 检查 `MaxCache` 上限，超出时丢弃并记录追踪

#### Dispose - 释放资源

```csharp
public void Dispose()
```

释放内部 `MemoryStream`，连接关闭时应调用（通常通过 `using` 自动管理）。

## GetLengthDelegate 委托

```csharp
/// <summary>获取完整包长度的委托</summary>
/// <param name="span">待解析的数据片段（从当前位置开始）</param>
/// <returns>完整包的字节数；返回 0 或负值表示数据不足，继续等待</returns>
public delegate Int32 GetLengthDelegate(ReadOnlySpan<Byte> span);
```

### 常见协议的 GetLength2 实现

```csharp
// 1. 2字节小端长度字段（包长度不含头）
GetLength2 = span =>
{
    if (span.Length < 2) return 0;
    return (span[0] | (span[1] << 8)) + 2;  // 加上头长度
};

// 2. 4字节大端长度字段
GetLength2 = span =>
{
    if (span.Length < 4) return 0;
    var bodyLen = (span[0] << 24) | (span[1] << 16) | (span[2] << 8) | span[3];
    return bodyLen + 4;
};

// 3. NewLife 默认消息格式（DefaultMessage）
GetLength2 = DefaultMessage.GetLength;

// 4. 固定包长（如 Modbus RTU）
GetLength2 = span => 8;
```

## 使用场景

### 在自定义协议中使用

```csharp
// 为每条连接创建独立的 PacketCodec 实例
public class MySession : NetSession
{
    private PacketCodec _codec = null!;

    protected override void OnConnected()
    {
        base.OnConnected();
        _codec = new PacketCodec
        {
            GetLength2 = span =>
            {
                // 协议头: [SOF=0xAA][LEN 2字节][DATA][CRC]
                if (span.Length < 3) return 0;
                if (span[0] != 0xAA) return -1;  // 非法包头，触发清空
                return (span[1] | (span[2] << 8)) + 3 + 1;  // 头3字节 + 数据 + CRC1字节
            },
            MaxCache = 256 * 1024,
        };
    }

    protected override void OnReceive(ReceivedEventArgs e)
    {
        var packets = _codec.Parse(e.Packet);
        foreach (var pk in packets)
        {
            // 处理完整的业务包
            ProcessFrame(pk);
        }
    }

    protected override void OnDisconnected(String reason)
    {
        base.OnDisconnected(reason);
        _codec.Dispose();
    }
}
```

### 在 LengthFieldCodec 中的集成方式

`LengthFieldCodec` 内部正是基于 `PacketCodec` 实现的，通过把 `PacketCodec` 实例存储在会话上下文（`ss["Codec"]`）中，实现按连接隔离：

```csharp
// LengthFieldCodec 内部实现参考（简化）
protected override IEnumerable<IPacket>? Decode(IHandlerContext context, IPacket pk)
{
    if (context.Owner is not IExtend ss) yield break;
    if (ss["Codec"] is not PacketCodec pc)
    {
        ss["Codec"] = pc = new PacketCodec
        {
            GetLength2 = BuildGetLength(),
            MaxCache   = MaxCache,
            Expire     = Expire,
        };
    }
    foreach (var item in pc.Parse(pk))
        yield return item;
}
```

## 注意事项

- **每条连接独立实例**：`PacketCodec` 有内部状态（缓存流），绝对不能跨连接共享。
- **`GetLength2` 先检查长度**：委托内必须先判断 `span.Length` 是否满足最小头长度，不足则返回 0，否则会读取越界。
- **返回负值触发清空**：`GetLength2` 返回负值时 `PacketCodec` 会丢弃当前所有缓存，用于遇到非法包头时的自恢复。
- **关闭连接时 Dispose**：释放内部 `MemoryStream`，防止内存泄漏。
- **`Expire` 防止内存积压**：设置合理的超时时间，避免因网络问题导致残包长期占用内存。
