# 数据扩展 IOHelper

## 概述

`IOHelper` 是 NewLife.Core 中的 IO 操作工具类，提供高效的数据流操作、字节数组转换、压缩解压、编码转换等功能。针对 .NET 6+ 的 `Stream.Read` 语义变化进行了兼容性处理，确保在所有框架版本上行为一致。

**命名空间**：`NewLife`  
**文档地址**：https://newlifex.com/core/io_helper

## 核心特性

- **精确读取**：`ReadExactly` 确保读取指定字节数，解决 .NET 6+ 部分读取问题
- **压缩解压**：支持 Deflate 和 GZip 两种算法
- **字节序转换**：支持大端/小端字节序转换
- **十六进制编码**：高效的十六进制字符串转换
- **变长整数**：支持 7-bit 编码的压缩整数读写

## 快速开始

```csharp
using NewLife;

// 字节数组转十六进制字符串
var hex = new Byte[] { 0x12, 0xAB, 0xCD }.ToHex();  // "12ABCD"

// 十六进制字符串转字节数组
var data = "12ABCD".ToHex();  // [0x12, 0xAB, 0xCD]

// Base64 编解码
var base64 = data.ToBase64();
var bytes = base64.ToBase64();

// 压缩数据
var compressed = data.Compress();
var decompressed = compressed.Decompress();

// 流转字符串
using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Hello"));
var str = stream.ToStr();  // "Hello"
```

## API 参考

### 属性配置

#### MaxSafeArraySize

```csharp
public static Int32 MaxSafeArraySize { get; set; } = 1024 * 1024;
```

最大安全数组大小。超过该大小时，读取数据操作将强制失败。

**用途**：保护性设置，避免解码错误数据时读取超大数组导致应用崩溃。

**示例**：
```csharp
// 需要解码大型二进制数据时，适当放宽
IOHelper.MaxSafeArraySize = 10 * 1024 * 1024;  // 10MB
```

### 数据流读取

#### ReadExactly

```csharp
public static Int32 ReadExactly(this Stream stream, Byte[] buffer, Int32 offset, Int32 count)
public static Byte[] ReadExactly(this Stream stream, Int64 count)
```

精确读取指定字节数。若数据不足则抛出 `EndOfStreamException`。

**背景**：.NET 6 开始，`Stream.Read` 可能返回部分数据（partial read），本方法确保读取完整数据。

**示例**：
```csharp
using var fs = File.OpenRead("data.bin");

// 读取固定长度的协议头
var header = new Byte[16];
fs.ReadExactly(header, 0, 16);

// 读取并返回新数组
var data = fs.ReadExactly(1024);
```

#### ReadAtLeast

```csharp
public static Int32 ReadAtLeast(this Stream stream, Byte[] buffer, Int32 offset, Int32 count, Int32 minimumBytes, Boolean throwOnEndOfStream = true)
```

读取至少指定字节数，允许读取更多但不超过 `count`。

**参数说明**：
- `minimumBytes`：最少需要读取的字节数
- `throwOnEndOfStream`：数据不足时是否抛出异常

**示例**：
```csharp
var buffer = new Byte[1024];

// 至少读取 100 字节，最多 1024 字节
var read = stream.ReadAtLeast(buffer, 0, 1024, 100, throwOnEndOfStream: false);
if (read < 100)
{
    Console.WriteLine("数据不足");
}
```

#### ReadBytes

```csharp
public static Byte[] ReadBytes(this Stream stream, Int64 length)
```

从流中读取指定长度的字节数组。

**参数说明**：
- `length`：要读取的字节数，-1 表示读取到流末尾

**示例**：
```csharp
// 读取指定长度
var data = stream.ReadBytes(1024);

// 读取全部剩余数据
var all = stream.ReadBytes(-1);
```

### 数据流写入

#### Write

```csharp
public static Stream Write(this Stream des, params Byte[] src)
```

将字节数组写入数据流。

**示例**：
```csharp
using var ms = new MemoryStream();
ms.Write(new Byte[] { 1, 2, 3 });
ms.Write(new Byte[] { 4, 5, 6 });
```

#### WriteArray / ReadArray

```csharp
public static Stream WriteArray(this Stream des, params Byte[] src)
public static Byte[] ReadArray(this Stream des)
```

写入/读取带长度前缀的字节数组（使用 7-bit 编码整数作为长度）。

**示例**：
```csharp
using var ms = new MemoryStream();

// 写入带长度前缀的数据
ms.WriteArray(new Byte[] { 1, 2, 3, 4, 5 });

// 读取
ms.Position = 0;
var data = ms.ReadArray();  // [1, 2, 3, 4, 5]
```

### 压缩解压

#### Compress / Decompress（Deflate）

```csharp
public static Stream Compress(this Stream inStream, Stream? outStream = null)
public static Stream Decompress(this Stream inStream, Stream? outStream = null)
public static Byte[] Compress(this Byte[] data)
public static Byte[] Decompress(this Byte[] data)
```

使用 Deflate 算法压缩/解压数据。

**示例**：
```csharp
// 压缩字节数组
var data = Encoding.UTF8.GetBytes("Hello World!");
var compressed = data.Compress();
var decompressed = compressed.Decompress();

// 压缩数据流
using var input = new MemoryStream(data);
using var output = new MemoryStream();
input.Compress(output);
```

#### CompressGZip / DecompressGZip

```csharp
public static Stream CompressGZip(this Stream inStream, Stream? outStream = null)
public static Stream DecompressGZip(this Stream inStream, Stream? outStream = null)
public static Byte[] CompressGZip(this Byte[] data)
public static Byte[] DecompressGZip(this Byte[] data)
```

使用 GZip 算法压缩/解压数据。GZip 格式包含文件头信息，兼容性更好。

**示例**：
```csharp
var data = File.ReadAllBytes("large-file.txt");
var gzipped = data.CompressGZip();
File.WriteAllBytes("large-file.txt.gz", gzipped);
```

### 字节序转换

#### ToUInt16 / ToUInt32 / ToUInt64

```csharp
public static UInt16 ToUInt16(this Byte[] data, Int32 offset = 0, Boolean isLittleEndian = true)
public static UInt32 ToUInt32(this Byte[] data, Int32 offset = 0, Boolean isLittleEndian = true)
public static UInt64 ToUInt64(this Byte[] data, Int32 offset = 0, Boolean isLittleEndian = true)
```

从字节数组读取整数，支持大端/小端字节序。

**示例**：
```csharp
var data = new Byte[] { 0x01, 0x00, 0x00, 0x00 };

// 小端序（默认）
var value1 = data.ToUInt32();                    // 1
// 大端序
var value2 = data.ToUInt32(isLittleEndian: false);  // 16777216
```

#### GetBytes

```csharp
public static Byte[] GetBytes(this Int16 value, Boolean isLittleEndian = true)
public static Byte[] GetBytes(this Int32 value, Boolean isLittleEndian = true)
public static Byte[] GetBytes(this Int64 value, Boolean isLittleEndian = true)
// ... 更多重载
```

将整数转换为字节数组。

**示例**：
```csharp
var bytes1 = 12345.GetBytes();                      // 小端序
var bytes2 = 12345.GetBytes(isLittleEndian: false); // 大端序
```

### 十六进制编码

#### ToHex

```csharp
public static String ToHex(this Byte[] data, Int32 offset = 0, Int32 count = -1)
public static String ToHex(this Byte[] data, String? separate, Int32 lineSize = 0)
```

将字节数组转换为十六进制字符串。

**示例**：
```csharp
var data = new Byte[] { 0x12, 0xAB, 0xCD, 0xEF };

// 基本转换
data.ToHex()                         // "12ABCDEF"

// 带分隔符
data.ToHex("-")                      // "12-AB-CD-EF"
data.ToHex(" ")                      // "12 AB CD EF"

// 分行显示
var largeData = new Byte[32];
largeData.ToHex(" ", lineSize: 16)   // 每 16 字节一行
```

#### ToHex（字符串转字节数组）

```csharp
public static Byte[] ToHex(this String? data)
```

将十六进制字符串转换为字节数组。

**示例**：
```csharp
"12ABCDEF".ToHex()           // [0x12, 0xAB, 0xCD, 0xEF]
"12-AB-CD-EF".ToHex()        // [0x12, 0xAB, 0xCD, 0xEF]（自动忽略分隔符）
"12 AB CD EF".ToHex()        // [0x12, 0xAB, 0xCD, 0xEF]
```

### Base64 编码

#### ToBase64

```csharp
public static String ToBase64(this Byte[] data)
public static Byte[] ToBase64(this String? data)
```

Base64 编解码。

**示例**：
```csharp
// 编码
var data = Encoding.UTF8.GetBytes("Hello");
var base64 = data.ToBase64();        // "SGVsbG8="

// 解码
var bytes = base64.ToBase64();       // [72, 101, 108, 108, 111]
```

### 字符串转换

#### ToStr

```csharp
public static String ToStr(this Stream stream, Encoding? encoding = null)
public static String ToStr(this Byte[] buf, Encoding? encoding = null, Int32 offset = 0, Int32 count = -1)
```

将流或字节数组转换为字符串，自动处理 BOM。

**示例**：
```csharp
// 流转字符串
using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Hello"));
var str = stream.ToStr();  // "Hello"

// 字节数组转字符串
var data = Encoding.UTF8.GetBytes("世界");
var text = data.ToStr(Encoding.UTF8);  // "世界"
```

### 变长整数编码

#### WriteEncodedInt / ReadEncodedInt

```csharp
public static Stream WriteEncodedInt(this Stream stream, Int32 value)
public static Int32 ReadEncodedInt(this Stream stream)
```

使用 7-bit 编码读写变长整数，小值占用更少字节。

**编码规则**：
- 0-127：1 字节
- 128-16383：2 字节
- 16384-2097151：3 字节
- 以此类推

**示例**：
```csharp
using var ms = new MemoryStream();

// 写入小值只需 1 字节
ms.WriteEncodedInt(100);   // 1 字节

// 写入大值需要更多字节
ms.WriteEncodedInt(10000); // 2 字节

// 读取
ms.Position = 0;
var v1 = ms.ReadEncodedInt();  // 100
var v2 = ms.ReadEncodedInt();  // 10000
```

### 时间读写

#### WriteDateTime / ReadDateTime

```csharp
public static Stream WriteDateTime(this Stream stream, DateTime dt)
public static DateTime ReadDateTime(this Stream stream)
```

以 Unix 时间戳（秒）格式读写时间，4 字节存储。

**示例**：
```csharp
using var ms = new MemoryStream();

ms.WriteDateTime(DateTime.Now);

ms.Position = 0;
var dt = ms.ReadDateTime();
```

### 字节数组操作

#### ReadBytes

```csharp
public static Byte[] ReadBytes(this Byte[] src, Int32 offset, Int32 count)
```

从字节数组中复制指定范围的数据。

**示例**：
```csharp
var data = new Byte[] { 1, 2, 3, 4, 5 };
var part = data.ReadBytes(1, 3);  // [2, 3, 4]
var rest = data.ReadBytes(2, -1); // [3, 4, 5]（-1 表示到末尾）
```

#### Write

```csharp
public static Int32 Write(this Byte[] dst, Int32 dstOffset, Byte[] src, Int32 srcOffset = 0, Int32 count = -1)
```

向字节数组写入数据。

**示例**：
```csharp
var buffer = new Byte[10];
var data = new Byte[] { 1, 2, 3 };
var written = buffer.Write(0, data);  // 写入 3 字节
```

#### CopyTo（指定长度）

```csharp
public static void CopyTo(this Stream source, Stream destination, Int64 length, Int32 bufferSize)
```

从源流复制指定长度数据到目标流。

**示例**：
```csharp
using var source = File.OpenRead("large-file.bin");
using var dest = File.Create("part.bin");

// 只复制前 1MB
source.CopyTo(dest, 1024 * 1024, bufferSize: 81920);
```

## 使用场景

### 1. 网络协议解析

```csharp
public class ProtocolParser
{
    public Message Parse(Stream stream)
    {
        // 读取固定长度的协议头
        var header = stream.ReadExactly(8);
        
        var magic = header.ToUInt32(0);
        var length = header.ToUInt32(4);
        
        // 读取消息体
        var body = stream.ReadExactly(length);
        
        return new Message { Header = header, Body = body };
    }
}
```

### 2. 二进制序列化

```csharp
public class BinarySerializer
{
    public void Serialize(Stream stream, Object obj)
    {
        var json = JsonSerializer.Serialize(obj);
        var data = Encoding.UTF8.GetBytes(json);
        
        // 写入长度和数据
        stream.WriteArray(data);
    }
    
    public T Deserialize<T>(Stream stream)
    {
        var data = stream.ReadArray();
        var json = data.ToStr(Encoding.UTF8);
        return JsonSerializer.Deserialize<T>(json);
    }
}
```

### 3. 数据压缩传输

```csharp
public class CompressedTransport
{
    public Byte[] Send(Byte[] data)
    {
        // 压缩数据
        var compressed = data.CompressGZip();
        
        // 构建传输包：[压缩标志][原始长度][压缩数据]
        using var ms = new MemoryStream();
        ms.WriteByte(1);  // 压缩标志
        ms.WriteEncodedInt(data.Length);  // 原始长度
        ms.Write(compressed);
        
        return ms.ToArray();
    }
    
    public Byte[] Receive(Byte[] packet)
    {
        using var ms = new MemoryStream(packet);
        var compressed = ms.ReadByte() == 1;
        var originalLength = ms.ReadEncodedInt();
        var data = ms.ReadBytes(-1);
        
        return compressed ? data.DecompressGZip() : data;
    }
}
```

## 最佳实践

### 1. 使用 ReadExactly 替代 Read

```csharp
// 推荐：确保读取完整数据
var data = stream.ReadExactly(100);

// 不推荐：可能只读取部分数据
var buffer = new Byte[100];
stream.Read(buffer, 0, 100);  // .NET 6+ 可能返回小于 100
```

### 2. 注意字节序

```csharp
// 与其他系统交互时注意字节序
// 网络协议通常使用大端序
var networkValue = data.ToUInt32(isLittleEndian: false);

// 本地存储通常使用小端序（x86/x64 默认）
var localValue = data.ToUInt32();
```

### 3. 使用对象池减少内存分配

```csharp
using NewLife.Collections;

// 使用内存流池
var ms = Pool.MemoryStream.Get();
try
{
    // 使用流...
}
finally
{
    ms.Return(true);  // 归还并清空
}
```

## 相关链接

- [路径扩展 PathHelper](path_helper-路径扩展PathHelper.md)
- [安全扩展 SecurityHelper](security_helper-安全扩展SecurityHelper.md)
- [数据包 IPacket](packet-数据包IPacket.md)
