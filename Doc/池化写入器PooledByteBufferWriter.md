# PooledByteBufferWriter 文档

## 概述

`PooledByteBufferWriter` 是一个基于 `ArrayPool<byte>` 的高性能缓冲区写入器，实现了 `IBufferWriter<byte>` 接口。它专为需要动态扩容、避免频繁内存分配的大块连续写入场景而设计，如网络协议构建、序列化操作等。

## 核心特性

- **池化内存管理**：基于 `ArrayPool<byte>.Shared` 进行数组租借与归还，减少 GC 压力
- **动态扩容**：支持自动扩容，近似2倍增长策略
- **IBufferWriter接口**：与 .NET 标准序列化库完全兼容
- **零拷贝操作**：提供直接的内存视图，避免不必要的数据复制
- **流操作支持**：直接写入到 `Stream`，支持同步和异步
- **安全边界检查**：防止缓冲区溢出和内存泄露

## 构造和生命周期

### 基础构造

```csharp
// 指定初始容量（从数组池租用）
using var writer = new PooledByteBufferWriter(1024);

// 也可以不使用 using，但必须手动调用 Dispose
var writer2 = new PooledByteBufferWriter(4096);
// ... 使用 writer2 ...
writer2.Dispose(); // 必须调用以归还内存到池中
```

### 重用实例

```csharp
var writer = new PooledByteBufferWriter(1024);

// 第一次使用
WriteData1(writer);
var result1 = writer.WrittenMemory.ToArray();
writer.Clear(); // 清空内容，但保持内存不归还

// 第二次使用（复用相同的内存）
WriteData2(writer);
var result2 = writer.WrittenMemory.ToArray();

// 使用完毕，归还内存
writer.Dispose();
```

## 主要属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `WrittenMemory` | `ReadOnlyMemory<byte>` | 已写入的内存（只读） |
| `WrittenSpan` | `ReadOnlySpan<byte>` | 已写入的数据段 |
| `WrittenCount` | `int` | 已写入字节数 |
| `Capacity` | `int` | 当前缓冲区总容量 |
| `FreeCapacity` | `int` | 剩余可写容量 |

## IBufferWriter<byte> 接口实现

### 获取写入缓冲区

```csharp
var writer = new PooledByteBufferWriter(512);

// 获取至少256字节的可写内存
Memory<byte> buffer = writer.GetMemory(256);

// 获取至少256字节的可写Span
Span<byte> span = writer.GetSpan(256);

// 写入一些数据到span
span[0] = 0x01;
span[1] = 0x02;
span[2] = 0x03;

// 通知已写入3字节
writer.Advance(3);
```

### 与序列化库配合使用

```csharp
// 与 System.Text.Json 配合
var writer = new PooledByteBufferWriter(1024);
using var jsonWriter = new Utf8JsonWriter(writer);

jsonWriter.WriteStartObject();
jsonWriter.WriteString("name", "张三");
jsonWriter.WriteNumber("age", 25);
jsonWriter.WriteEndObject();
jsonWriter.Flush();

// 获取JSON字节
ReadOnlyMemory<byte> jsonBytes = writer.WrittenMemory;
string json = Encoding.UTF8.GetString(jsonBytes.Span);

writer.Dispose();
```

### 与 MessagePack 配合

```csharp
var writer = new PooledByteBufferWriter(1024);

// 使用 MessagePack 序列化
MessagePackSerializer.Serialize(writer, new { Name = "张三", Age = 25 });

// 获取序列化结果
ReadOnlyMemory<byte> msgPackBytes = writer.WrittenMemory;

writer.Dispose();
```

## 直接操作方法

### 清空操作

```csharp
var writer = new PooledByteBufferWriter(1024);

// 写入一些数据
var span = writer.GetSpan(100);
span.Fill(0x42);
writer.Advance(100);

Console.WriteLine(writer.WrittenCount); // 输出: 100

// 仅清空内容（不归还内存，可继续使用）
writer.Clear();
Console.WriteLine(writer.WrittenCount); // 输出: 0

// 清空并归还内存到池中（之后不能再使用）
writer.ClearAndReturnBuffers();
```

### 重新初始化

```csharp
var writer = new PooledByteBufferWriter(1024);
// ... 使用writer ...

// 清空并归还原有内存
writer.ClearAndReturnBuffers();

// 重新初始化为新的实例
writer.InitializeEmptyInstance(2048);
// 现在可以继续使用writer
```

## 流操作

### 写入到流（同步）

```csharp
var writer = new PooledByteBufferWriter(1024);

// 写入一些数据
var span = writer.GetSpan(20);
Encoding.UTF8.GetBytes("Hello World".AsSpan(), span);
writer.Advance(11);

// 直接写入到流
using var fileStream = File.Create("output.txt");
writer.WriteToStream(fileStream);

writer.Dispose();
```

### 写入到流（异步）

```csharp
var writer = new PooledByteBufferWriter(1024);

// 写入数据...
WriteData(writer);

// 异步写入到流
using var networkStream = GetNetworkStream();
await writer.WriteToStreamAsync(networkStream);

writer.Dispose();
```

### 完整的异步示例

```csharp
public async Task SerializeAndSendAsync<T>(T data, Stream stream)
{
    using var writer = new PooledByteBufferWriter(4096);
    
    // 使用JSON序列化
    using var jsonWriter = new Utf8JsonWriter(writer);
    JsonSerializer.Serialize(jsonWriter, data);
    jsonWriter.Flush();
    
    // 异步发送到网络
    await writer.WriteToStreamAsync(stream);
}
```

## 扩容机制

### 扩容策略

```csharp
// 内部扩容逻辑（简化版）：
// 1. 当剩余空间不足时，计算新大小 = 当前长度 + max(请求大小, 当前长度)
// 2. 从数组池租用新数组
// 3. 复制已有数据到新数组
// 4. 清理并归还旧数组

var writer = new PooledByteBufferWriter(100);
Console.WriteLine(writer.Capacity); // 可能输出: 128 (数组池分配的实际大小)

// 写入数据触发扩容
var largeSpan = writer.GetSpan(500); // 需要500字节，但当前容量不足
// 内部会自动扩容到合适大小（通常是当前大小的2倍左右）

Console.WriteLine(writer.Capacity); // 可能输出: 512 或更大
```

### 容量限制

```csharp
// 最大容量接近 Array.MaxLength (约2GB)
const int MaxArrayLength = 0x7FFFFFC7; // 2,147,483,591 字节

// 当接近上限时，扩容策略会更保守
var writer = new PooledByteBufferWriter(1024);
try
{
    // 对于超大数据，可能会抛出 OutOfMemoryException
    var hugeSpan = writer.GetSpan(int.MaxValue / 2);
}
catch (OutOfMemoryException ex)
{
    Console.WriteLine($"内存不足: {ex.Message}");
}
```

## 性能优化

### 预分配合适的初始容量

```csharp
// 不好的做法：频繁扩容
var writer1 = new PooledByteBufferWriter(16); // 初始容量太小
for (int i = 0; i < 1000; i++)
{
    var span = writer1.GetSpan(100);
    // 每次都可能触发扩容...
    writer1.Advance(100);
}

// 好的做法：预估合适的初始容量
var writer2 = new PooledByteBufferWriter(100 * 1000); // 预分配足够空间
for (int i = 0; i < 1000; i++)
{
    var span = writer2.GetSpan(100);
    // 不会触发扩容
    writer2.Advance(100);
}
```

### 批量写入

```csharp
public void EfficientWrite(PooledByteBufferWriter writer, byte[][] dataChunks)
{
    // 计算总大小
    int totalSize = dataChunks.Sum(chunk => chunk.Length);
    
    // 一次性获取足够空间
    var span = writer.GetSpan(totalSize);
    
    // 批量复制
    int offset = 0;
    foreach (var chunk in dataChunks)
    {
        chunk.CopyTo(span.Slice(offset));
        offset += chunk.Length;
    }
    
    writer.Advance(totalSize);
}
```

## 错误处理

### 常见异常

```csharp
var writer = new PooledByteBufferWriter(100);

try
{
    // ArgumentOutOfRangeException: 负数advance
    writer.Advance(-1);
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"无效的advance值: {ex.Message}");
}

try
{
    // ArgumentOutOfRangeException: advance超出容量
    writer.Advance(writer.Capacity + 1);
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"advance超出容量: {ex.Message}");
}

try
{
    // OutOfMemoryException: 请求超大空间
    writer.GetSpan(int.MaxValue);
}
catch (OutOfMemoryException ex)
{
    Console.WriteLine($"内存不足: {ex.Message}");
}
```

### 资源泄露防护

```csharp
// 使用using确保资源释放
public void SafeUsage()
{
    using var writer = new PooledByteBufferWriter(1024);
    
    // 即使发生异常，Dispose也会被调用
    DoSomeWork(writer);
    
} // 自动调用 Dispose，归还内存到池中

// 手动管理（需要确保异常安全）
public void ManualUsage()
{
    var writer = new PooledByteBufferWriter(1024);
    try
    {
        DoSomeWork(writer);
    }
    finally
    {
        writer.Dispose(); // 必须在finally中调用
    }
}
```

## 使用场景

### HTTP响应构建

```csharp
public async Task<byte[]> BuildHttpResponseAsync(object data)
{
    using var writer = new PooledByteBufferWriter(4096);
    
    // 写入HTTP头部
    var headerSpan = writer.GetSpan(256);
    var headerBytes = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: application/json\r\n\r\n");
    headerBytes.CopyTo(headerSpan);
    writer.Advance(headerBytes.Length);
    
    // 写入JSON正文
    using var jsonWriter = new Utf8JsonWriter(writer);
    JsonSerializer.Serialize(jsonWriter, data);
    jsonWriter.Flush();
    
    return writer.WrittenMemory.ToArray();
}
```

### 网络数据包构建

```csharp
public ReadOnlyMemory<byte> BuildNetworkPacket(int messageId, byte[] payload)
{
    using var writer = new PooledByteBufferWriter(payload.Length + 64);
    
    // 写入包头
    var headerSpan = writer.GetSpan(16);
    BinaryPrimitives.WriteInt32LittleEndian(headerSpan, 0x12345678); // Magic
    BinaryPrimitives.WriteInt32LittleEndian(headerSpan[4..], messageId);
    BinaryPrimitives.WriteInt32LittleEndian(headerSpan[8..], payload.Length);
    writer.Advance(12);
    
    // 写入负载
    var payloadSpan = writer.GetSpan(payload.Length);
    payload.CopyTo(payloadSpan);
    writer.Advance(payload.Length);
    
    return writer.WrittenMemory;
}
```

### 文件格式生成

```csharp
public void GenerateBinaryFile(string filePath, IEnumerable<Record> records)
{
    using var writer = new PooledByteBufferWriter(8192);
    using var fileStream = File.Create(filePath);
    
    // 写入文件头
    var magic = "MYFORMAT"u8;
    var headerSpan = writer.GetSpan(magic.Length + 4);
    magic.CopyTo(headerSpan);
    BinaryPrimitives.WriteInt32LittleEndian(headerSpan[magic.Length..], records.Count());
    writer.Advance(magic.Length + 4);
    
    // 写入记录
    foreach (var record in records)
    {
        WriteRecord(writer, record);
        
        // 当缓冲区积累足够数据时，刷新到文件
        if (writer.WrittenCount > 4096)
        {
            writer.WriteToStream(fileStream);
            writer.Clear(); // 重置缓冲区继续使用
        }
    }
    
    // 写入剩余数据
    if (writer.WrittenCount > 0)
    {
        writer.WriteToStream(fileStream);
    }
}
```

## 与其他缓冲区类型对比

### vs. MemoryStream

```csharp
// MemoryStream: 更高级但有GC压力
using var ms = new MemoryStream();
// 写入操作...
byte[] result1 = ms.ToArray(); // 需要复制数据

// PooledByteBufferWriter: 更底层但零拷贝
using var writer = new PooledByteBufferWriter(1024);
// 写入操作...
ReadOnlyMemory<byte> result2 = writer.WrittenMemory; // 无复制
```

### vs. ArrayBufferWriter<byte>

```csharp
// ArrayBufferWriter: 使用托管数组
var arrayWriter = new ArrayBufferWriter<byte>();
// 写入操作...
// 内存不会自动归还，依赖GC回收

// PooledByteBufferWriter: 使用数组池
using var pooledWriter = new PooledByteBufferWriter(1024);
// 写入操作...
// 使用完毕自动归还到池中，减少GC压力
```

## 最佳实践

1. **合理预估初始容量**：避免频繁扩容影响性能
2. **及时释放资源**：使用 `using` 语句或确保调用 `Dispose`
3. **批量操作**：尽量减少 `GetSpan`/`Advance` 调用次数
4. **异常安全**：在 `finally` 块中确保资源释放
5. **复用实例**：对于频繁操作，可以使用 `Clear()` 复用同一实例
6. **监控内存使用**：在高并发场景下监控数组池的使用情况

## 限制与注意事项

1. **非线程安全**：单个实例不能跨线程并发使用
2. **生命周期管理**：必须正确调用 `Dispose` 以归还内存
3. **最大容量限制**：接近 2GB 的理论上限
4. **数组池依赖**：依赖 `ArrayPool<byte>.Shared` 的实现质量
5. **内存清理**：归还前会清理敏感数据，可能有轻微性能开销

## 相关类型

- [`SpanReader`](./SpanReader.md) - 高性能字节流读取器
- [`SpanWriter`](./SpanWriter.md) - 高性能字节流写入器（固定缓冲区）
- [`SpanHelper`](./SpanHelper.md) - Span相关的辅助方法