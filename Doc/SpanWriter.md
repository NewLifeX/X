# SpanWriter 文档

## 概述

`SpanWriter` 是一个高性能的字节流写入器，提供零分配的二进制数据写入能力。它是一个 `ref struct`，专门设计用于构建网络协议帧、序列化二进制数据或生成自定义格式的字节流。

## 核心特性

- **零分配写入**：基于 `Span<byte>` 的直接内存写入，无 GC 压力
- **多数据类型支持**：内置基础类型（整数、浮点、字符串）的写入方法
- **7位压缩整数**：兼容 .NET 标准的压缩格式写入
- **结构体直接序列化**：利用内存布局直接写入结构体
- **字符串灵活写入**：支持定长、变长和长度前缀多种模式
- **字节序控制**：支持大端和小端字节序
- **边界检查**：自动进行缓冲区溢出检查

## 构造方式

```csharp
// 从 Span<byte> 构造
Span<byte> buffer = stackalloc byte[1024];
var writer = new SpanWriter(buffer);

// 从 IPacket 构造
IPacket packet = new OwnerPacket(1024);
var writer = new SpanWriter(packet);

// 从字节数组构造
var buffer = new byte[1024];
var writer = new SpanWriter(buffer);
```

## 主要属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Span` | `Span<byte>` | 目标缓冲区 |
| `Position` | `int` | 已写入字节数 |
| `Capacity` | `int` | 总容量 |
| `FreeCapacity` | `int` | 剩余可写字节数 |
| `WrittenSpan` | `ReadOnlySpan<byte>` | 已写入的数据片段 |
| `WrittenCount` | `int` | 已写入数据长度 |
| `IsLittleEndian` | `bool` | 是否小端字节序（默认 true） |

## 基础操作

### 位置控制

```csharp
var writer = new SpanWriter(buffer);

// 前进指定字节数（标记已写入）
writer.Advance(4);

// 获取可写入的缓冲区
Span<byte> writeBuffer = writer.GetSpan();
Span<byte> writeBufferWithHint = writer.GetSpan(sizeHint: 100);

// 获取已写入的数据
ReadOnlySpan<byte> written = writer.WrittenSpan;
int writtenLength = writer.WrittenCount;
```

## 数据类型写入

### 基础数值类型

```csharp
var writer = new SpanWriter(buffer);

// 写入基础类型
writer.Write((byte)0x01);
writer.WriteByte(0x02);           // 等价于 Write((byte)0x02)
writer.Write((short)1000);
writer.Write((ushort)2000);
writer.Write(12345);
writer.Write(67890U);
writer.Write(123456789L);
writer.Write(987654321UL);
writer.Write(3.14f);
writer.Write(2.718281828);

// 所有Write方法都返回写入的字节数
int bytesWritten = writer.Write(42); // 返回 4
```

### 字节序控制

```csharp
var writer = new SpanWriter(buffer);

// 使用大端字节序
writer.IsLittleEndian = false;
writer.Write(0x12345678); // 写入为 12 34 56 78

// 切换回小端字节序
writer.IsLittleEndian = true;
writer.Write(0x12345678); // 写入为 78 56 34 12
```

### 字符串写入

```csharp
var writer = new SpanWriter(buffer);

// 写入模式：
// length = -1: 写入全部字符串内容，不含长度信息
// length = 0:  先写入7位压缩长度前缀，再写入字符串内容
// length > 0:  写入固定长度（不足填0，超长截断）

// 写入全部内容
writer.Write("Hello World", -1);

// 写入带长度前缀的字符串
writer.Write("Hello World", 0);

// 写入固定长度字符串（20字节，不足补0）
writer.Write("Hello", 20);

// 指定编码
writer.Write("你好世界", 0, Encoding.UTF8);
writer.Write("Hello", 10, Encoding.ASCII);
```

### 字节数组和 Span 写入

```csharp
var writer = new SpanWriter(buffer);

// 写入字节数组
byte[] data = { 1, 2, 3, 4, 5 };
writer.Write(data);

// 写入 ReadOnlySpan<byte>
ReadOnlySpan<byte> span = stackalloc byte[] { 6, 7, 8 };
writer.Write(span);

// 写入 Span<byte>
Span<byte> spanData = stackalloc byte[] { 9, 10 };
writer.Write(spanData);
```

### 结构体写入

```csharp
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct MyHeader
{
    public int Magic;
    public short Version;
    public byte Flags;
}

var writer = new SpanWriter(buffer);
var header = new MyHeader 
{ 
    Magic = 0x12345678, 
    Version = 1, 
    Flags = 0xFF 
};

int bytesWritten = writer.Write(header); // 返回结构体大小
```

## 高级功能

### 7位压缩整数

```csharp
var writer = new SpanWriter(buffer);

// 写入7位压缩格式的32位整数（兼容 BinaryWriter）
int bytesWritten = writer.WriteEncodedInt(12345);

// 负数也支持（会占用5字节）
writer.WriteEncodedInt(-1);
```

### 批量操作示例

```csharp
public byte[] BuildProtocolFrame(int messageId, string content)
{
    Span<byte> buffer = stackalloc byte[1024];
    var writer = new SpanWriter(buffer);
    
    // 写入协议头
    writer.Write((byte)0x01);           // 版本
    writer.WriteEncodedInt(messageId);  // 消息ID（压缩格式）
    writer.Write(content, 0);           // 内容（带长度前缀）
    
    // 返回实际数据
    return writer.WrittenSpan.ToArray();
}
```

## 错误处理

所有写入操作在空间不足时会抛出异常：

```csharp
Span<byte> smallBuffer = stackalloc byte[2];
var writer = new SpanWriter(smallBuffer);

try
{
    writer.Write(12345); // 需要4字节，但缓冲区只有2字节
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"缓冲区空间不足: {ex.Message}");
}
```

## 使用场景

### 网络协议构建

```csharp
public ReadOnlySpan<byte> BuildHttpResponse(int statusCode, string body)
{
    Span<byte> buffer = stackalloc byte[4096];
    var writer = new SpanWriter(buffer);
    
    // HTTP状态行
    writer.Write($"HTTP/1.1 {statusCode} OK\r\n", -1, Encoding.ASCII);
    
    // 头部
    writer.Write("Content-Type: text/plain\r\n", -1, Encoding.ASCII);
    writer.Write($"Content-Length: {Encoding.UTF8.GetByteCount(body)}\r\n", -1, Encoding.ASCII);
    writer.Write("\r\n", -1, Encoding.ASCII);
    
    // 正文
    writer.Write(body, -1, Encoding.UTF8);
    
    return writer.WrittenSpan;
}
```

### 二进制数据序列化

```csharp
public void SerializeBinaryData(Span<byte> output, MyDataStructure data)
{
    var writer = new SpanWriter(output);
    
    // 魔数和版本
    writer.Write(0x12345678);
    writer.Write((short)1);
    
    // 数据项数量
    writer.WriteEncodedInt(data.Items.Count);
    
    // 序列化每个数据项
    foreach (var item in data.Items)
    {
        writer.Write(item.Id);
        writer.Write(item.Name, 0); // 带长度前缀
        writer.Write(item.Timestamp.ToBinary());
    }
}
```

### 性能关键场景

```csharp
public unsafe void HighPerformanceWrite(Span<byte> buffer, int[] values)
{
    var writer = new SpanWriter(buffer);
    
    // 写入数组长度
    writer.WriteEncodedInt(values.Length);
    
    // 批量写入整数（避免循环开销）
    foreach (int value in values)
    {
        writer.Write(value);
    }
}
```

## 字符串写入详解

### 定长字符串处理

```csharp
var writer = new SpanWriter(buffer);

// 固定20字节，不足补0
writer.Write("Hello", 20);        // "Hello" + 15个0字节

// 超长截断
writer.Write("This is a very long string", 10); // 只写入前10字节
```

### 编码处理

```csharp
var writer = new SpanWriter(buffer);

// UTF8编码（默认）
writer.Write("中文测试", 0);

// GBK编码
var gbk = Encoding.GetEncoding("GBK");
writer.Write("中文测试", 0, gbk);

// ASCII编码（非ASCII字符会被替换）
writer.Write("Hello World", 0, Encoding.ASCII);
```

## 性能说明

- `SpanWriter` 是 `ref struct`，只能在栈上分配，无 GC 压力
- 所有数值写入都是直接内存访问，性能接近不安全代码
- 字符串写入会涉及编码转换，建议复用编码器实例
- 大端字节序写入会有轻微性能开销
- 7位压缩整数写入针对小数值优化

## 限制与注意事项

1. **ref struct 限制**：不能存储在堆上，不能作为字段，不能在异步方法中跨await使用
2. **生命周期**：依赖底层缓冲区的生命周期，使用期间缓冲区不能被释放
3. **边界检查**：会自动进行溢出检查，但不会自动扩容
4. **线程安全**：非线程安全，不能跨线程使用
5. **缓冲区大小**：需要预先分配足够的缓冲区，写入超出容量会抛出异常

## 最佳实践

1. **合理估算缓冲区大小**：预留足够空间避免溢出异常
2. **复用编码器**：避免重复创建 `Encoding` 实例
3. **使用 stackalloc**：小缓冲区优先使用栈分配
4. **批量写入**：合并多个小写入操作提高性能
5. **错误处理**：妥善处理缓冲区溢出异常

## 相关类型

- [`SpanReader`](./SpanReader.md) - 对应的读取器
- [`SpanHelper`](./SpanHelper.md) - Span相关的辅助方法
- [`PooledByteBufferWriter`](./PooledByteBufferWriter.md) - 池化的动态缓冲区写入器