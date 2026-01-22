# 二进制序列化 Binary

## 概述

`Binary` 是 NewLife.Core 中的高性能二进制序列化器，用于将对象序列化为紧凑的二进制格式或从二进制数据反序列化为对象。特别适合网络通信、协议解析、数据存储等对性能和体积有较高要求的场景。

**命名空间**：`NewLife.Serialization`  
**文档地址**：https://newlifex.com/core/binary

## 核心特性

- **高性能**：直接操作字节流，无中间格式转换
- **紧凑格式**：支持7位变长编码整数，减少数据体积
- **字节序控制**：支持大端/小端字节序
- **协议支持**：支持 `FieldSize` 特性定义字段大小
- **版本兼容**：支持协议版本控制
- **扩展性强**：可添加自定义处理器

## 快速开始

### 序列化

```csharp
using NewLife.Serialization;

public class User
{
    public Int32 Id { get; set; }
    public String Name { get; set; }
    public Int32 Age { get; set; }
}

var user = new User { Id = 1, Name = "张三", Age = 25 };

// 快速序列化
var packet = Binary.FastWrite(user);
var bytes = packet.ToArray();

// 或使用流
using var ms = new MemoryStream();
Binary.FastWrite(user, ms);
```

### 反序列化

```csharp
using NewLife.Serialization;

var bytes = /* 二进制数据 */;

// 快速反序列化
using var ms = new MemoryStream(bytes);
var user = Binary.FastRead<User>(ms);
```

## API 参考

### Binary 类

#### 属性

```csharp
/// <summary>使用7位编码整数。默认false不使用</summary>
public Boolean EncodeInt { get; set; }

/// <summary>小端字节序。默认false大端</summary>
public Boolean IsLittleEndian { get; set; }

/// <summary>使用指定大小的FieldSizeAttribute特性。默认false</summary>
public Boolean UseFieldSize { get; set; }

/// <summary>大小宽度。可选0/1/2/4，默认0表示压缩编码整数</summary>
public Int32 SizeWidth { get; set; }

/// <summary>解析字符串时，是否清空两头的0字节。默认false</summary>
public Boolean TrimZero { get; set; }

/// <summary>协议版本。用于支持多版本协议序列化</summary>
public String? Version { get; set; }

/// <summary>使用完整的时间格式。完整格式使用8个字节保存毫秒数，默认false</summary>
public Boolean FullTime { get; set; }

/// <summary>总的字节数。读取或写入</summary>
public Int64 Total { get; set; }
```

#### FastWrite - 快速序列化

```csharp
// 序列化为数据包
public static IPacket FastWrite(Object value, Boolean encodeInt = true)

// 序列化到流
public static Int64 FastWrite(Object value, Stream stream, Boolean encodeInt = true)
```

**参数**：
- `value`：要序列化的对象
- `encodeInt`：是否使用7位变长编码整数
- `stream`：目标流

**示例**：
```csharp
// 返回 IPacket
var packet = Binary.FastWrite(obj);
var bytes = packet.ToArray();

// 写入流
using var ms = new MemoryStream();
var length = Binary.FastWrite(obj, ms);
```

#### FastRead - 快速反序列化

```csharp
public static T? FastRead<T>(Stream stream, Boolean encodeInt = true)
```

**示例**：
```csharp
using var ms = new MemoryStream(bytes);
var obj = Binary.FastRead<MyClass>(ms);
```

### 完整用法

```csharp
// 创建序列化器
var bn = new Binary
{
    EncodeInt = true,           // 使用7位编码
    IsLittleEndian = true,      // 小端字节序
    UseFieldSize = true         // 启用 FieldSize 特性
};

// 设置流
bn.Stream = new MemoryStream();

// 写入数据
bn.Write(obj);

// 获取结果
var bytes = bn.GetBytes();
```

```csharp
// 反序列化
var bn = new Binary
{
    Stream = new MemoryStream(bytes),
    EncodeInt = true
};

var obj = bn.Read<MyClass>();
```

## IAccessor 接口

对于需要自定义序列化逻辑的类型，可以实现 `IAccessor` 接口：

```csharp
public interface IAccessor
{
    /// <summary>从数据流读取</summary>
    Boolean Read(Stream stream, Object context);
    
    /// <summary>写入数据流</summary>
    Boolean Write(Stream stream, Object context);
}
```

**示例**：
```csharp
public class CustomPacket : IAccessor
{
    public Byte Header { get; set; }
    public Int16 Length { get; set; }
    public Byte[] Data { get; set; }
    public Byte Checksum { get; set; }
    
    public Boolean Read(Stream stream, Object context)
    {
        var bn = context as Binary;
        
        Header = bn.ReadByte();
        Length = bn.Read<Int16>();
        Data = bn.ReadBytes(Length);
        Checksum = bn.ReadByte();
        
        return true;
    }
    
    public Boolean Write(Stream stream, Object context)
    {
        var bn = context as Binary;
        
        bn.Write(Header);
        bn.Write((Int16)Data.Length);
        bn.Write(Data);
        bn.Write(Checksum);
        
        return true;
    }
}
```

## FieldSize 特性

`FieldSizeAttribute` 用于指定字段的固定大小或关联长度字段：

```csharp
public class Protocol
{
    public Byte Header { get; set; }
    
    [FieldSize(2)]  // 固定2字节
    public Int16 Length { get; set; }
    
    [FieldSize("Length")]  // 大小由 Length 字段决定
    public Byte[] Body { get; set; }
    
    [FieldSize(4)]  // 固定4字节字符串
    public String Code { get; set; }
}
```

## 使用场景

### 1. 网络协议解析

```csharp
public class TcpMessage
{
    public Byte Start { get; set; } = 0x7E;
    public UInt16 MessageId { get; set; }
    public UInt16 BodyLength { get; set; }
    
    [FieldSize("BodyLength")]
    public Byte[] Body { get; set; }
    
    public Byte Checksum { get; set; }
    public Byte End { get; set; } = 0x7E;
}

// 解析
var bn = new Binary(stream) { IsLittleEndian = false, UseFieldSize = true };
var msg = bn.Read<TcpMessage>();

// 构建
var msg = new TcpMessage { MessageId = 0x0001, Body = data };
msg.BodyLength = (UInt16)data.Length;
msg.Checksum = CalculateChecksum(msg);
var packet = Binary.FastWrite(msg);
```

### 2. JT/T808 协议

```csharp
public class JT808Message
{
    public UInt16 MsgId { get; set; }
    public UInt16 MsgAttr { get; set; }
    
    [FieldSize(6)]  // 2011版6字节，2019版10字节
    public String Phone { get; set; }
    
    public UInt16 SeqNo { get; set; }
    public Byte[] Body { get; set; }
}

// 2011版本
var bn = new Binary { UseFieldSize = true, Version = "2011" };
var msg = bn.Read<JT808Message>();

// 2019版本
var bn = new Binary { UseFieldSize = true, Version = "2019" };
```

### 3. 数据存储

```csharp
public class Record
{
    public Int64 Id { get; set; }
    public DateTime CreateTime { get; set; }
    public Byte[] Data { get; set; }
}

// 保存到文件
using var fs = File.Create("data.bin");
foreach (var record in records)
{
    Binary.FastWrite(record, fs);
}

// 从文件加载
using var fs = File.OpenRead("data.bin");
var list = new List<Record>();
while (fs.Position < fs.Length)
{
    var record = Binary.FastRead<Record>(fs);
    list.Add(record);
}
```

### 4. 高性能序列化

```csharp
// 复用 Binary 实例避免频繁创建
var bn = new Binary { EncodeInt = true };

foreach (var item in items)
{
    bn.Stream = new MemoryStream();
    bn.Write(item);
    var bytes = bn.GetBytes();
    // 处理 bytes...
}
```

## 7位变长编码

`EncodeInt = true` 时，整数使用7位变长编码，可显著减少小数值的存储空间：

| 值范围 | 字节数 |
|--------|--------|
| 0 ~ 127 | 1 字节 |
| 128 ~ 16383 | 2 字节 |
| 16384 ~ 2097151 | 3 字节 |
| 2097152 ~ 268435455 | 4 字节 |
| 更大 | 5 字节 |

```csharp
// 启用变长编码
var bn = new Binary { EncodeInt = true };
bn.Stream = new MemoryStream();
bn.Write(100);      // 1字节
bn.Write(1000);     // 2字节
bn.Write(100000);   // 3字节
```

## 字节序

```csharp
// 大端字节序（网络字节序，默认）
var bn = new Binary { IsLittleEndian = false };
bn.Write((Int32)0x12345678);  // 输出: 12 34 56 78

// 小端字节序（Intel x86）
var bn = new Binary { IsLittleEndian = true };
bn.Write((Int32)0x12345678);  // 输出: 78 56 34 12
```

## 最佳实践

### 1. 协议解析统一配置

```csharp
public static class BinaryConfig
{
    public static Binary CreateReader(Stream stream) => new Binary(stream)
    {
        EncodeInt = false,
        IsLittleEndian = false,
        UseFieldSize = true
    };
    
    public static Binary CreateWriter() => new Binary
    {
        EncodeInt = false,
        IsLittleEndian = false,
        UseFieldSize = true
    };
}
```

### 2. 错误处理

```csharp
var bn = new Binary(stream);
try
{
    var obj = bn.Read<MyClass>();
}
catch (EndOfStreamException)
{
    // 数据不完整
}
```

### 3. 检查剩余数据

```csharp
var bn = new Binary(stream);

// 检查是否有足够数据
if (bn.CheckRemain(10))  // 至少需要10字节
{
    var data = bn.ReadBytes(10);
}
```

## 性能对比

| 序列化方式 | 体积 | 速度 | 适用场景 |
|-----------|------|------|---------|
| Binary | 最小 | 最快 | 协议、存储 |
| JSON | 中等 | 中等 | API、配置 |
| XML | 最大 | 较慢 | 配置、文档 |

## 相关链接

- [JSON 序列化](json-JSON序列化.md)
- [XML 序列化](xml-XML序列化.md)
- [数据包 IPacket](packet-数据包IPacket.md)
