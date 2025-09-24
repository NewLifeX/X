# IPacket 数据包设计文档

## 概述

IPacket 是 NewLife.Core 中的核心数据包接口，用于高性能网络编程和协议解析。它采用内存共享理念，通过对象池和链式结构实现零拷贝数据处理，显著降低内存分配和 GC 压力。

## 设计理念

### 核心原则
1. **性能优先**：零拷贝设计，减少内存分配
2. **内存友好**：使用对象池复用内存，降低 GC 压力  
3. **链式结构**：支持多段数据包链接，避免大块内存拷贝
4. **所有权管理**：明确内存管理职责，防止内存泄漏

### 设计目标
- 网络库零拷贝数据传输
- 协议解析中的高效数据切片
- 减少网络编程中的内存开销
- 提供类似 .NET Core Span/Memory 的功能（兼容 .NET Framework）

## 接口定义

```csharp
public interface IPacket
{
    Int32 Length { get; }                    // 当前段长度
    IPacket? Next { get; set; }              // 链式后续包
    Int32 Total { get; }                     // 总长度（含链式）
    Byte this[Int32 index] { get; set; }     // 跨链索引访问
    
    Span<Byte> GetSpan();                    // 获取内存视图
    Memory<Byte> GetMemory();                // 获取内存块
    IPacket Slice(Int32 offset, Int32 count = -1);
    IPacket Slice(Int32 offset, Int32 count, Boolean transferOwner);
    Boolean TryGetArray(out ArraySegment<Byte> segment);
}
```

## 实现类型

### 1. ArrayPacket（结构体）
- **用途**：基于字节数组的数据包，无内存管理开销
- **特性**：值类型，无 GC 分配，适合频繁创建
- **场景**：已有数组的数据切片，协议解析

```csharp
var packet = new ArrayPacket(buffer, offset, length);
var slice = packet.Slice(10, 20);  // 零分配切片
```

### 2. OwnerPacket（类）
- **用途**：具有所有权管理的数据包，使用 ArrayPool
- **特性**：支持内存池，自动释放，可转移所有权  
- **场景**：需要申请新缓冲区的场景

```csharp
using var packet = new OwnerPacket(1024);  // 从池中申请
var slice = packet.Slice(0, 512, transferOwner: true);  // 转移所有权
```

### 3. MemoryPacket（结构体）
- **用途**：基于 Memory&lt;Byte&gt; 的数据包
- **特性**：可能来自内存池，无内存管理责任
- **场景**：与现有 Memory 系统集成

## 扩展方法 (PacketHelper)

### 链式操作
```csharp
IPacket chain = packet1
    .Append(packet2)
    .Append(dataBytes);
```

### 数据转换
```csharp
// 字符串转换（支持多包链）
string text = packet.ToStr(Encoding.UTF8, offset: 10, count: 100);

// 十六进制显示（调试友好）
string hex = packet.ToHex(maxLength: 32, separator: " ", groupSize: 4);
```

### 流操作
```csharp
// 同步复制
packet.CopyTo(stream);

// 异步复制  
await packet.CopyToAsync(stream, cancellationToken);

// 获取独立流
using var stream = packet.GetStream();
```

### 数据读取
```csharp
// 读取字节数组
byte[] data = packet.ReadBytes(offset: 10, count: 20);

// 深度克隆
IPacket clone = packet.Clone();

// 转换为数组段
ArraySegment<byte> segment = packet.ToSegment();
IList<ArraySegment<byte>> segments = packet.ToSegments();
```

### 头部扩展
```csharp
// 协议头填充（优先复用现有缓冲区）
IPacket withHeader = packet.ExpandHeader(headerSize: 4);
```

## 内存管理模式

### 所有权转移规则
1. **接收方负责释放**：获得数据包的一方负责最终释放
2. **单次转移**：所有权只能转移一次，避免重复释放
3. **链式继承**：转移所有权时，Next 链一同转移

### 生命周期管理
```csharp
// 申请与自动释放
using var packet = new OwnerPacket(size);

// 手动释放
if (packet is IOwnerPacket owner)
    owner.Dispose();

// 扩展方法释放
packet.TryDispose();
```

## 性能优化策略

### 单包快速路径
- 单段数据直接操作，避免链表遍历
- Span 零拷贝视图访问
- 直接内存编码转换

### 多包链式处理  
- StringBuilder 池化拼接
- 全局偏移量计算
- 延迟内存分配

### 防护机制
- 环路检测避免死循环
- 边界校验防止越界
- 参数规范化处理异常输入

## 使用场景

### 网络编程
```csharp
// 接收数据
var packet = await socket.ReceivePacketAsync();

// 协议解析
var header = packet.Slice(0, 4);
var payload = packet.Slice(4);

// 响应数据
var response = headerPacket.Append(bodyPacket);
await socket.SendPacketAsync(response);
```

### 协议实现
```csharp
// HTTP 协议解析
var lines = packet.ToStr().Split('\n');
var bodyStart = packet.IndexOf("\r\n\r\n".GetBytes()) + 4;
var body = packet.Slice(bodyStart);
```

### 数据处理管道
```csharp
// 链式处理
var result = inputPacket
    .Decrypt()           // 解密
    .Decompress()        // 解压
    .ParseProtocol()     // 协议解析
    .Process();          // 业务处理
```

## 注意事项

### 安全使用
1. **短期持有**：Span/Memory 仅在所有权生命周期内使用
2. **禁止缓存**：不要将 Span/Memory 存储到异步结构中
3. **及时释放**：OwnerPacket 使用后及时释放或使用 using

### 性能考虑
1. **避免长链**：过长的包链会影响遍历性能
2. **合理切片**：频繁切片会增加对象创建开销
3. **复用缓冲区**：优先使用 ExpandHeader 复用现有缓冲区

### 兼容性
- 支持 .NET Framework 4.5+ 到 .NET 9
- 旧版 Packet 类实现相同接口，便于渐进式升级
- 与现有 Stream/ArraySegment API 良好集成

## 最佳实践

### 创建数据包
```csharp
// 推荐：使用结构体避免分配
IPacket packet = new ArrayPacket(buffer, offset, length);

// 需要所有权管理时
using var packet = new OwnerPacket(size);
```

### 链式操作
```csharp
// 构建复合数据包
var message = headerPacket
    .Append(bodyBytes)
    .Append(footerPacket);
```

### 错误处理
```csharp
try
{
    var data = packet.ReadBytes(offset, count);
}
catch (IndexOutOfRangeException)
{
    // 处理越界访问
}
finally
{
    packet.TryDispose();  // 安全释放
}
```

## 总结

IPacket 接口提供了高性能、内存友好的数据包处理能力，特别适合网络编程和协议解析场景。通过链式结构和零拷贝设计，它能够显著降低内存开销和 GC 压力，是构建高性能网络应用的重要基础设施。

合理使用 IPacket 的各种实现类型和扩展方法，能够在保证性能的同时，提供清晰的内存管理语义和良好的开发体验。