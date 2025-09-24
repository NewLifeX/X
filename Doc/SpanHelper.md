# SpanHelper - Span 帮助类

## 概述

`SpanHelper` 是 NewLife.Core 提供的 Span/Memory 扩展工具类，专为高性能场景设计，提供常用的 Span 操作扩展方法，避免额外内存分配。

## 主要功能

- **字符串转换**：高效的字节数组与字符串相互转换
- **十六进制编码**：支持分隔符和分组的十六进制字符串生成
- **边界截取**：基于边界标记的数据切片操作
- **流操作**：Memory 到 Stream 的高效写入
- **数据修剪**：去除前后指定元素的操作

## API 参考

### 字符串扩展

#### ToStr 方法

将字节 Span 转换为字符串。

```csharp
// 基本用法 - 使用 UTF8 编码
ReadOnlySpan<byte> data = [72, 101, 108, 108, 111]; // "Hello"
string result = data.ToStr();

// 指定编码
string result2 = data.ToStr(Encoding.ASCII);

// Span<byte> 重载
Span<byte> spanData = stackalloc byte[] { 72, 101, 108, 108, 111 };
string result3 = spanData.ToStr();
```

**参数：**
- `span`：字节数据
- `encoding`：编码格式，默认 UTF8

**返回值：**解码后的字符串

#### GetBytes 方法

获取字符串的字节表示（指针路径，避免中间数组分配）。

```csharp
string text = "Hello World";
Span<byte> buffer = stackalloc byte[100];

// 高效编码，避免额外分配
int bytesWritten = Encoding.UTF8.GetBytes(text.AsSpan(), buffer);
Console.WriteLine($"写入了 {bytesWritten} 个字节");
```

**参数：**
- `encoding`：编码对象
- `chars`：字符序列
- `bytes`：目标字节序列

**返回值：**实际写入的字节数

#### GetString 方法

获取字节数组的字符串（指针路径，避免额外拷贝）。

```csharp
ReadOnlySpan<byte> data = [72, 101, 108, 108, 111];
string result = Encoding.UTF8.GetString(data);
```

### 十六进制编码

#### ToHex 方法

将字节数据编码为十六进制字符串。

```csharp
// 基本用法
ReadOnlySpan<byte> data = [0x48, 0x65, 0x6C, 0x6C, 0x6F];
string hex = data.ToHex(); // "48656C6C6F"

// 限制长度
string shortHex = data.ToHex(3); // "48656C"

// 添加分隔符
string separated = data.ToHex("-"); // "48-65-6C-6C-6F"

// 分组显示
string grouped = data.ToHex(" ", 2); // "4865 6C6C 6F"

// 限制长度并分组
string limited = data.ToHex("-", 2, 4); // "48-65-6C-6C"
```

**重载版本：**

1. `ToHex(ReadOnlySpan<byte>)` - 基本十六进制转换
2. `ToHex(ReadOnlySpan<byte>, int)` - 限制最大长度
3. `ToHex(ReadOnlySpan<byte>, string?, int, int)` - 完整功能版本
4. `ToHex(Span<byte>, ...)` - 对应的 Span 重载

**参数：**
- `data`：要转换的字节数据
- `separate`：分隔符字符串
- `groupSize`：分组大小，0 表示每个字节前都插入分隔符
- `maxLength`：最大显示字节数，-1 表示全部

### 边界截取扩展

#### Substring 方法

通过指定开始与结束边界来截取数据源。

```csharp
// 字节数组示例
ReadOnlySpan<byte> source = "Hello[World]End"u8;
ReadOnlySpan<byte> start = "["u8;
ReadOnlySpan<byte> end = "]"u8;

ReadOnlySpan<byte> result = source.Substring(start, end);
string extracted = Encoding.UTF8.GetString(result); // "World"

// 字符示例
ReadOnlySpan<char> text = "Hello[World]End";
ReadOnlySpan<char> startChar = "[";
ReadOnlySpan<char> endChar = "]";

ReadOnlySpan<char> charResult = text.Substring(startChar, endChar); // "World"
```

**返回值：**位于边界之间的切片；未匹配返回空切片

#### IndexOf 方法

查找开始与结束边界的位置信息。

```csharp
ReadOnlySpan<byte> source = "Hello[World]End"u8;
ReadOnlySpan<byte> start = "["u8;
ReadOnlySpan<byte> end = "]"u8;

var (offset, count) = source.IndexOf(start, end);
if (offset != -1 && count != -1)
{
    var result = source.Slice(offset, count);
    // 处理提取的数据
}
```

**返回值：**
- `(offset, count)` 元组
- 未匹配返回 `(-1, -1)`
- 只匹配起始返回 `(startOffset, -1)`

### 流扩展

#### Write 方法

将 ReadOnlyMemory 写入到数据流，支持内存池优化。

```csharp
using var stream = new MemoryStream();
ReadOnlyMemory<byte> data = "Hello World"u8.ToArray();

// 同步写入
stream.Write(data);

// 异步写入
await stream.WriteAsync(data, cancellationToken);
```

**特点：**
- 优先尝试直接访问底层数组
- 无法直接访问时使用 ArrayPool 进行缓冲
- 异步版本支持取消令牌

### 修剪扩展

#### Trim 方法

去除 Span 前后的指定元素。

```csharp
// 去除前后的零值
ReadOnlySpan<byte> data = stackalloc byte[] { 0, 0, 1, 2, 3, 0, 0 };
ReadOnlySpan<byte> trimmed = data.Trim((byte)0); // [1, 2, 3]

// 去除前后的空格字符
ReadOnlySpan<char> text = "  Hello World  ";
ReadOnlySpan<char> trimmedText = text.Trim(' '); // "Hello World"

// 支持任意 IEquatable<T> 类型
ReadOnlySpan<int> numbers = stackalloc int[] { -1, -1, 5, 10, 15, -1 };
ReadOnlySpan<int> result = numbers.Trim(-1); // [5, 10, 15]
```

## 性能特点

### 零分配设计

- 使用 `stackalloc` 进行栈上分配
- 基于指针的字符串操作避免临时数组
- 利用 `ArrayPool<T>` 进行缓冲区重用

### 高效实现

```csharp
// 十六进制转换使用查表法，避免计算开销
private static char GetHexValue(int i) => "0123456789ABCDEF"[i & 0xF];

// 字符串编码使用 unsafe 指针操作
fixed (char* charsPtr = &MemoryMarshal.GetReference(chars))
fixed (byte* bytesPtr = &MemoryMarshal.GetReference(bytes))
{
    return encoding.GetBytes(charsPtr, chars.Length, bytesPtr, bytes.Length);
}
```

### 多平台兼容

- 支持 .NET Framework 4.5 到 .NET 9
- 针对旧版本提供降级实现
- 条件编译确保最佳性能

## 使用示例

### 协议解析场景

```csharp
// 解析网络协议数据包
ReadOnlySpan<byte> packet = receivedData;
ReadOnlySpan<byte> header = "PKT:"u8;
ReadOnlySpan<byte> footer = "\r\n"u8;

// 提取数据负载
ReadOnlySpan<byte> payload = packet.Substring(header, footer);
if (!payload.IsEmpty)
{
    string content = payload.ToStr();
    Console.WriteLine($"接收到：{content}");
}
```

### 日志格式化场景

```csharp
// 高效的日志数据格式化
ReadOnlySpan<byte> logData = GetLogBytes();

// 生成十六进制转储，便于调试
string hexDump = logData.ToHex(" ", 16, 256); // 16字节一组，最多256字节
Console.WriteLine($"数据转储：{hexDump}");
```

### 数据清理场景

```csharp
// 清理数据中的填充字节
ReadOnlySpan<byte> rawData = ReadFromSensor();
ReadOnlySpan<byte> cleanData = rawData.Trim((byte)0x00);

// 转换为可读格式
string result = cleanData.ToHex("-");
```

## 注意事项

### 内存安全

- 所有方法都是内存安全的，不会访问越界
- `stackalloc` 使用需要注意栈空间大小
- 返回的 Span 引用原始数据，需注意生命周期

### 性能考虑

- 适用于高频调用场景
- 避免在 Span 上进行装箱操作
- 大数据量建议分批处理

### 兼容性

- 完全兼容现有的字节数组操作
- 可与 `Memory<T>` 和 `ReadOnlyMemory<T>` 无缝配合
- 支持所有实现 `IEquatable<T>` 的类型

## 相关类型

- `System.Span<T>` - 基础 Span 类型
- `System.ReadOnlySpan<T>` - 只读 Span 类型
- `System.Memory<T>` - 托管内存抽象
- `System.Buffers.ArrayPool<T>` - 数组池
- `System.Text.Encoding` - 字符编码

## 更多信息

- [NewLife.Core 文档](https://newlifex.com/core)
- [Span<T> 官方文档](https://docs.microsoft.com/zh-cn/dotnet/api/system.span-1)
- [高性能 .NET 编程指南](https://newlifex.com/core/span_helper)