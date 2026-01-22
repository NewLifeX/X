# SpanHelper 文档

## 概述

`SpanHelper` 是一个静态帮助类，提供了 `Span<T>` 和 `ReadOnlySpan<T>` 的常用扩展方法。它专注于高性能、零分配的操作，包括字符串编码转换、十六进制编码、边界搜索、流操作和数据修剪等功能。

## 核心特性

- **零分配转换**：提供字节数组到字符串的高效转换
- **十六进制编码**：支持多种格式的十六进制字符串生成
- **边界搜索**：在字节流中查找特定的开始和结束边界
- **流操作扩展**：扩展 `Stream` 以支持 `Memory<byte>` 写入
- **数据修剪**：去除前后指定的元素
- **跨平台兼容**：兼容 .NET Framework 4.5 到 .NET 9

## 字符串编码转换

### 基础转换

```csharp
using NewLife;

// ReadOnlySpan<byte> 到字符串
ReadOnlySpan<byte> data = stackalloc byte[] { 72, 101, 108, 108, 111 }; // "Hello"
string text = data.ToStr(); // 使用 UTF8 编码
string gbkText = data.ToStr(Encoding.GetEncoding("GBK"));

// Span<byte> 到字符串
Span<byte> buffer = stackalloc byte[] { 87, 111, 114, 108, 100 }; // "World"
string text2 = buffer.ToStr();
```

### 高性能编码操作

```csharp
// 字符串编码到 Span<byte>（避免中间数组分配）
string text = "Hello World";
Span<byte> buffer = stackalloc byte[100];
int bytesWritten = Encoding.UTF8.GetBytes(text.AsSpan(), buffer);

// 字节解码为字符串（指针路径，.NET Framework 4.5+ 兼容）
ReadOnlySpan<byte> utf8Bytes = stackalloc byte[] { 72, 101, 108, 108, 111 };
string decoded = Encoding.UTF8.GetString(utf8Bytes);
```

## 十六进制编码

### 基础十六进制转换

```csharp
// 基础十六进制编码（大写，无分隔符）
ReadOnlySpan<byte> data = stackalloc byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF };
string hex = data.ToHex(); // "0123456789ABCDEF"

// Span<byte> 版本
Span<byte> buffer = stackalloc byte[] { 0xFF, 0x00, 0x80 };
string hex2 = buffer.ToHex(); // "FF0080"
```

### 限制长度的十六进制

```csharp
byte[] largeData = new byte[1000];
// ... 填充数据 ...

// 只显示前16字节的十六进制
string hex = largeData.AsSpan().ToHex(16); // 只转换前16字节

// 如果数据少于指定长度，显示全部
byte[] smallData = { 0x01, 0x02 };
string hex2 = smallData.AsSpan().ToHex(10); // "0102"
```

### 带分隔符和分组的十六进制

```csharp
ReadOnlySpan<byte> data = stackalloc byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB };

// 每个字节用空格分隔
string hex1 = data.ToHex(" "); // "01 23 45 67 89 AB"

// 每2字节用连字符分隔
string hex2 = data.ToHex("-", groupSize: 2); // "0123-4567-89AB"

// 每4字节用下划线分隔
string hex3 = data.ToHex("_", groupSize: 4); // "01234567_89AB"

// 限制显示长度并分组
string hex4 = data.ToHex(" ", groupSize: 0, maxLength: 4); // "01 23 45 67"
```

## 边界搜索

### 基础边界搜索

```csharp
// 查找边界之间的内容
ReadOnlySpan<byte> html = "<div>Hello World</div>"u8;
ReadOnlySpan<byte> startTag = "<div>"u8;
ReadOnlySpan<byte> endTag = "</div>"u8;

// 提取边界之间的内容
ReadOnlySpan<byte> content = html.Substring(startTag, endTag); // "Hello World"

// Span<byte> 版本
Span<byte> buffer = stackalloc byte[100];
// ... 填充 HTML 内容 ...
Span<byte> extracted = buffer.Substring(startTag, endTag);
```

### 获取边界位置信息

```csharp
ReadOnlySpan<byte> data = "prefix[content]suffix"u8;
ReadOnlySpan<byte> start = "["u8;
ReadOnlySpan<byte> end = "]"u8;

// 获取详细位置信息
var (offset, count) = data.IndexOf(start, end);
if (offset >= 0 && count >= 0)
{
    // offset: 内容开始位置
    // count: 内容长度
    var content = data.Slice(offset, count); // "content"
}
else if (offset >= 0 && count == -1)
{
    // 找到开始边界但未找到结束边界
    Console.WriteLine("未找到结束标记");
}
else
{
    // 未找到开始边界
    Console.WriteLine("未找到开始标记");
}
```

## 流操作扩展

### Memory 写入流

```csharp
// 同步写入 ReadOnlyMemory<byte> 到流
ReadOnlyMemory<byte> data = new byte[] { 1, 2, 3, 4, 5 };
using var stream = new MemoryStream();

stream.Write(data); // 扩展方法，自动处理数组池回退

// 异步写入
await stream.WriteAsync(data);
await stream.WriteAsync(data, CancellationToken.None);
```

## 数据修剪

### 基础修剪操作

```csharp
// 去除前后的零字节
ReadOnlySpan<byte> data = stackalloc byte[] { 0, 0, 1, 2, 3, 0, 0 };
ReadOnlySpan<byte> trimmed = data.Trim((byte)0); // [1, 2, 3]

// 去除前后的空格字符
ReadOnlySpan<char> text = "  Hello World  ".AsSpan();
ReadOnlySpan<char> trimmedText = text.Trim(' '); // "Hello World"

// Span<T> 版本
Span<byte> buffer = stackalloc byte[] { 0xFF, 1, 2, 3, 0xFF, 0xFF };
Span<byte> trimmedBuffer = buffer.Trim((byte)0xFF); // [1, 2, 3]
```

## 最佳实践

1. **优先使用 ReadOnlySpan**：除非需要修改数据，否则使用只读版本
2. **栈分配小缓冲区**：256字节以下优先使用 `stackalloc`
3. **复用编码器实例**：避免重复创建 `Encoding` 对象
4. **合理使用修剪**：避免在大数据上频繁修剪操作
5. **错误处理**：边界搜索可能返回空结果，需要检查
6. **性能测试**：在性能关键路径进行基准测试

## 相关类型

- [`SpanReader`](./SpanReader.md) - 高性能字节流读取器
- [`SpanWriter`](./SpanWriter.md) - 高性能字节流写入器
- [`PooledByteBufferWriter`](./PooledByteBufferWriter.md) - 池化的动态缓冲区写入器