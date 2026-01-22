# 高性能缓冲区操作类库文档

本文档库包含了 NewLife.Core 中四个核心的高性能缓冲区操作类的详细说明和使用指南。

## 类库概述

| 类名 | 类型 | 主要用途 | 特点 |
|------|------|----------|------|
| [`SpanReader`](./SpanReader.md) | `ref struct` | 高性能二进制数据读取 | 零分配读取、自动流扩展、支持多种数据类型 |
| [`SpanWriter`](./SpanWriter.md) | `ref struct` | 高性能二进制数据写入 | 零分配写入、固定缓冲区、支持多种数据类型 |
| [`SpanHelper`](./SpanHelperDoc.md) | `static class` | Span操作辅助工具 | 编码转换、十六进制、边界搜索、数据修剪 |
| [`PooledByteBufferWriter`](./PooledByteBufferWriter.md) | `sealed class` | 池化动态缓冲区写入 | 基于数组池、动态扩容、IBufferWriter接口 |

## 核心设计理念

### 高性能优先
- **零分配操作**：基于 `Span<T>` 和 `Memory<T>` 的现代 .NET 内存模型
- **池化内存管理**：使用 `ArrayPool<T>` 减少 GC 压力
- **直接内存访问**：避免不必要的数据复制和转换

### 易用性设计
- **类型安全**：泛型支持和强类型 API
- **一致性接口**：遵循 .NET 标准约定
- **丰富的重载**：支持多种使用场景

### 跨平台兼容
- **多框架支持**：.NET Framework 4.5 到 .NET 9
- **条件编译优化**：针对不同平台的特定优化

## 典型使用场景

### 网络协议处理
```csharp
// 解析网络数据包
public void ProcessPacket(ReadOnlySpan<byte> packetData)
{
    var reader = new SpanReader(packetData);
    
    var header = reader.Read<PacketHeader>();
    var messageId = reader.ReadEncodedInt();
    var payload = reader.ReadBytes(header.PayloadLength);
    
    ProcessMessage(messageId, payload);
}

// 构建响应数据包
public ReadOnlyMemory<byte> BuildResponse(int messageId, string content)
{
    using var writer = new PooledByteBufferWriter(1024);
    var spanWriter = new SpanWriter(writer.GetSpan());
    
    spanWriter.Write(0x12345678); // Magic
    spanWriter.WriteEncodedInt(messageId);
    spanWriter.Write(content, 0); // 带长度前缀
    
    writer.Advance(spanWriter.WrittenCount);
    return writer.WrittenMemory;
}
```

### 二进制序列化
```csharp
public byte[] SerializeObject<T>(T obj) where T : struct
{
    using var writer = new PooledByteBufferWriter(1024);
    var spanWriter = new SpanWriter(writer.GetSpan());
    
    // 写入类型信息和版本
    spanWriter.Write(typeof(T).Name, 0);
    spanWriter.Write((short)1);
    
    // 直接写入结构体
    spanWriter.Write(obj);
    
    writer.Advance(spanWriter.WrittenCount);
    return writer.WrittenMemory.ToArray();
}
```

### 数据转换和格式化
```csharp
public string FormatBinaryData(ReadOnlySpan<byte> data)
{
    // 使用 SpanHelper 进行格式化
    var hex = data.ToHex(" ", groupSize: 4, maxLength: 64);
    var text = data.ToStr().Replace('\0', '.');
    
    return $"Hex: {hex}\nText: {text}";
}
```

## 性能特点

### 内存分配对比
```
传统方式：                   新方式：
┌─────────────────┐         ┌─────────────────┐
│ byte[] → string │ (GC)    │ Span → string   │ (GC减少)
│ MemoryStream    │ (GC)    │ PooledWriter    │ (池化)
│ BinaryReader    │ (GC)    │ SpanReader      │ (栈上)
│ StringBuilder   │ (GC)    │ SpanWriter      │ (栈上)
└─────────────────┘         └─────────────────┘
```

### 性能基准（示例）
- **SpanReader**: 比 `BinaryReader` 快 2-3 倍，零额外分配
- **SpanWriter**: 比 `BinaryWriter` 快 2-4 倍，零额外分配  
- **PooledByteBufferWriter**: 比 `MemoryStream` 减少 60-80% 的 GC 分配
- **SpanHelper**: 十六进制转换比 `BitConverter.ToString` 快 3-5 倍

## 使用注意事项

### ref struct 限制
`SpanReader` 和 `SpanWriter` 是 `ref struct`，有以下限制：
- 不能存储在堆上（类字段、装箱）
- 不能在异步方法中跨 `await` 使用
- 不能在 LINQ 查询中使用
- 不能作为泛型类型参数

### 生命周期管理
```csharp
// ? 正确使用
void ProcessData(ReadOnlySpan<byte> data)
{
    var reader = new SpanReader(data);
    // 使用 reader...
} // reader 在方法结束时自动失效

// ? 错误使用
SpanReader? savedReader = null;
void BadUsage(ReadOnlySpan<byte> data)
{
    savedReader = new SpanReader(data); // 编译错误
}
```

### 资源释放
```csharp
// ? 使用 using 确保释放
using var writer = new PooledByteBufferWriter(1024);
// 使用 writer...

// ? 手动管理也可以
var writer = new PooledByteBufferWriter(1024);
try
{
    // 使用 writer...
}
finally
{
    writer.Dispose();
}
```

## 最佳实践总结

1. **选择合适的类型**
   - 读取固定大小数据 → `SpanReader`
   - 写入固定大小数据 → `SpanWriter` 
   - 动态大小写入 → `PooledByteBufferWriter`
   - 格式转换辅助 → `SpanHelper`

2. **性能优化**
   - 预估合适的缓冲区大小
   - 批量操作减少调用次数
   - 复用对象实例

3. **内存管理**
   - 及时释放池化资源
   - 避免持有 Span 引用超出生命周期
   - 在高并发场景监控内存池使用

4. **错误处理**
   - 检查边界和容量限制
   - 处理编码转换异常
   - 确保异常安全的资源释放

## 相关资源

- [NewLife.Core 官方文档](https://newlifex.com/core)
- [.NET Span<T> 官方文档](https://docs.microsoft.com/en-us/dotnet/api/system.span-1)
- [IBufferWriter<T> 接口文档](https://docs.microsoft.com/en-us/dotnet/api/system.buffers.ibufferwriter-1)

---

?? **文档版本**: 1.0  
?? **更新时间**: 2024年12月  
?? **维护团队**: NewLife 开发团队