# SpanReader 文档

## 概述

`SpanReader` 是一个高性能的只读字节流解析器，用于零分配地从内存缓冲区或数据流中读取二进制数据。它是一个 `ref struct`，专为解析网络协议帧（如 Redis、MySQL 协议）或自定义二进制格式而设计。

## 核心特性

- **零分配读取**：基于 `Span<byte>` 和 `ReadOnlySpan<byte>`，避免不必要的内存分配
- **自动流扩展**：支持从底层 `Stream` 动态读取更多数据
- **多数据类型支持**：内置基础类型（整数、浮点、字符串）的读取方法
- **7位压缩整数**：兼容 .NET 标准的压缩格式
- **结构体直接反序列化**：利用内存布局直接读取结构体
- **字节序控制**：支持大端和小端字节序
- **数据包切片**：支持零拷贝的数据包切片操作

## 构造方式

### 基础构造

```csharp
// 从 ReadOnlySpan<byte> 构造
var data = new byte[] { 1, 2, 3, 4 };
var reader = new SpanReader(data.AsSpan());

// 从 Span<byte> 构造
Span<byte> buffer = stackalloc byte[100];
var reader = new SpanReader(buffer);

// 从 IPacket 构造
IPacket packet = GetPacket();
var reader = new SpanReader(packet);
```

### 流扩展构造

```csharp
// 支持从数据流读取更多数据
var stream = new NetworkStream(socket);
var reader = new SpanReader(stream, initialPacket, bufferSize: 8192);
reader.MaxCapacity = 1024 * 1024; // 限制最大读取 1MB
```

## 主要属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Span` | `ReadOnlySpan<byte>` | 当前数据片段 |
| `Position` | `int` | 已读取字节数 |
| `Capacity` | `int` | 总容量 |
| `FreeCapacity` | `int` | 尚未读取的剩余字节数 |
| `IsLittleEndian` | `bool` | 是否小端字节序（默认 true） |
| `MaxCapacity` | `int` | 最大容量限制（0 表示不限制） |

## 基础操作

### 位置控制

```csharp
var reader = new SpanReader(data);

// 前进指定字节数
reader.Advance(4);

// 获取剩余数据片段
var remaining = reader.GetSpan();
var remainingWithHint = reader.GetSpan(sizeHint: 10);
```

### 确保数据可用

```csharp
// 确保至少有 10 字节可读（必要时从流中补齐）
reader.EnsureSpace(10);
```

## 数据类型读取

### 基础数值类型

```csharp
var reader = new SpanReader(data);

// 读取基础类型
byte b = reader.ReadByte();
short s = reader.ReadInt16();
ushort us = reader.ReadUInt16();
int i = reader.ReadInt32();
uint ui = reader.ReadUInt32();
long l = reader.ReadInt64();
ulong ul = reader.ReadUInt64();
float f = reader.ReadSingle();
double d = reader.ReadDouble();
```

### 字节序控制

```csharp
var reader = new SpanReader(data);

// 使用大端字节序
reader.IsLittleEndian = false;
int bigEndianValue = reader.ReadInt32();

// 切换回小端字节序
reader.IsLittleEndian = true;
int littleEndianValue = reader.ReadInt32();
```

### 字符串读取

```csharp
var reader = new SpanReader(data);

// 读取方式：
// length = -1: 读取剩余全部字节
// length = 0:  先读取7位压缩长度前缀，再读取相应字节
// length > 0:  读取固定长度

// 读取剩余全部数据为字符串
string allText = reader.ReadString(-1);

// 读取带长度前缀的字符串（7位压缩格式）
string prefixedText = reader.ReadString(0);

// 读取固定长度字符串
string fixedText = reader.ReadString(10);

// 指定编码
string gbkText = reader.ReadString(10, Encoding.GetEncoding("GBK"));
```

### 字节数组读取

```csharp
var reader = new SpanReader(data);

// 读取指定长度的字节片段
ReadOnlySpan<byte> bytes = reader.ReadBytes(5);

// 读取到目标缓冲区
Span<byte> buffer = stackalloc byte[10];
int bytesRead = reader.Read(buffer);
```

### 结构体读取

```csharp
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct MyHeader
{
    public int Magic;
    public short Version;
    public byte Flags;
}

var reader = new SpanReader(data);
MyHeader header = reader.Read<MyHeader>();
```

## 高级功能

### 7位压缩整数

```csharp
var reader = new SpanReader(data);

// 读取7位压缩格式的32位整数（兼容 BinaryReader）
int compressedValue = reader.ReadEncodedInt();
```

### 数据包切片

```csharp
// 从IPacket构造的reader支持零拷贝切片
var reader = new SpanReader(packet);

// 切片10字节的数据包（不会复制数据）
IPacket slice = reader.ReadPacket(10);
```

## 错误处理

所有读取操作在数据不足时会抛出异常：

```csharp
var reader = new SpanReader(smallData);

try
{
    int value = reader.ReadInt32(); // 如果数据不足4字节会抛出异常
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"数据不足: {ex.Message}");
}
```

## 使用场景

### 网络协议解析

```csharp
public ParsedMessage ParseProtocolFrame(ReadOnlySpan<byte> frameData)
{
    var reader = new SpanReader(frameData);
    
    var header = reader.Read<ProtocolHeader>();
    var bodyLength = reader.ReadEncodedInt();
    var body = reader.ReadBytes(bodyLength);
    
    return new ParsedMessage(header, body);
}
```

### 流式数据解析

```csharp
public async Task<List<Record>> ParseStreamAsync(Stream stream)
{
    var records = new List<Record>();
    var reader = new SpanReader(stream, bufferSize: 4096);
    reader.MaxCapacity = 1024 * 1024; // 限制1MB
    
    while (reader.FreeCapacity > 0)
    {
        try
        {
            var recordType = reader.ReadByte();
            var recordLength = reader.ReadEncodedInt();
            var recordData = reader.ReadBytes(recordLength);
            
            records.Add(new Record(recordType, recordData));
        }
        catch (InvalidOperationException)
        {
            break; // 数据读取完毕
        }
    }
    
    return records;
}
```

## 性能说明

- `SpanReader` 是 `ref struct`，只能在栈上分配，无 GC 压力
- 所有数值读取都是直接内存访问，性能接近不安全代码
- 支持从流扩展时，会使用 `OwnerPacket` 管理缓冲区生命周期
- 大端字节序读取会有轻微性能开销

## 限制与注意事项

1. **ref struct 限制**：不能存储在堆上，不能作为字段，不能在异步方法中跨await使用
2. **生命周期**：依赖底层数据的生命周期，使用期间底层数据不能被释放
3. **线程安全**：非线程安全，不能跨线程使用
4. **流扩展模式**：与数据包切片模式不兼容，需要选择合适的构造方式

## 相关类型

- [`SpanWriter`](./SpanWriter.md) - 对应的写入器
- [`SpanHelper`](./SpanHelper.md) - Span相关的辅助方法
- [`PooledByteBufferWriter`](./PooledByteBufferWriter.md) - 池化的动态缓冲区写入器