---
name: serialization
description: 使用 NewLife 进行 JSON/XML/Binary/CSV 多格式序列化和反序列化
---

# NewLife 序列化使用指南

## 适用场景

- JSON 序列化/反序列化（API 通信、配置文件）
- 二进制序列化（自定义协议、IoT 数据帧）
- XML 序列化（配置/数据交换）
- CSV 数据导入导出
- 高性能零分配序列化（Span 序列化）

## JSON 序列化

### 基础用法

```csharp
// 序列化
var json = user.ToJson();
var json = user.ToJson(indented: true);  // 美化输出
var json = user.ToJson(indented: false, nullValue: false, camelCase: true);

// 反序列化
var user = json.ToJsonEntity<User>();
var obj = json.ToJsonEntity(typeof(User));

// 解析为字典（不需要类型）
var dict = json.DecodeJson();

// 格式化美化
var pretty = JsonHelper.Format(json);
```

### JsonOptions 精细控制

```csharp
var options = new JsonOptions
{
    Indented = true,
    NullValue = false,      // 不输出 null 字段
    CamelCase = true,       // 驼峰命名
    EnumString = true,      // 枚举输出字符串
    IgnoreDefault = true,   // 忽略默认值
};
var json = user.ToJson(options);
```

### 切换 JSON 引擎

```csharp
// 默认使用 FastJson（NewLife 内置）
// 切换为 System.Text.Json
JsonHelper.Default = new SystemJson();
```

## Binary 序列化

### 读写操作

```csharp
// 序列化到流
var bn = new Binary();
bn.Write(myObj);
var data = bn.GetBytes();

// 从流反序列化
var bn = new Binary(new MemoryStream(data));
var obj = bn.Read<MyClass>();
```

### 协议配置

```csharp
var bn = new Binary
{
    IsLittleEndian = false,  // 大端（网络字节序）
    EncodeInt = true,        // 7位变长编码整数
    SizeWidth = 2,           // 集合长度用 2 字节
    FullTime = true,         // DateTime 完整 8 字节
};
```

### 自定义处理器

```csharp
var bn = new Binary();
bn.AddHandler<MyCustomHandler>(priority: 10);
```

## Span 序列化（零分配）

适用于 IoT 协议帧等需要零分配的高性能场景。

### 定义可序列化类型

```csharp
public class MyFrame : ISpanSerializable
{
    public Byte Command { get; set; }
    public UInt16 Length { get; set; }
    public Byte[] Data { get; set; }

    public Int32 Read(ReadOnlySpan<Byte> buffer)
    {
        var reader = new SpanReader(buffer);
        Command = reader.ReadByte();
        Length = reader.ReadUInt16();
        Data = reader.ReadBytes(Length);
        return reader.Position;
    }

    public Int32 Write(Span<Byte> buffer)
    {
        var writer = new SpanWriter(buffer);
        writer.WriteByte(Command);
        writer.WriteUInt16((UInt16)Data.Length);
        writer.Write(Data);
        return writer.Position;
    }
}
```

### 使用

```csharp
// 序列化
using var pk = SpanSerializer.Serialize(frame);
var bytes = pk.GetSpan().ToArray();

// 反序列化
var frame = SpanSerializer.Deserialize<MyFrame>(data);

// 或直接用扩展方法
using var pk = frame.ToPacket();
frame.FromSpan(data);
```

## CSV 读写

```csharp
// 写入 CSV
var csv = new CsvFile(filePath, true);
csv.WriteLine("Name", "Age", "City");
csv.WriteLine("张三", "25", "北京");
csv.Dispose();

// 读取 CSV
var csv = new CsvFile(filePath, false);
while (csv.ReadLine() is String[] line)
{
    var name = line[0];
    var age = line[1].ToInt();
}

// CSV 数据库（增删改查）
var db = new CsvDb<User>("users.csv");
db.Add(new User { Name = "test" });
var users = db.FindAll();
```

## Excel 读写

```csharp
// 读取 Excel（无需 Office）
using var reader = new ExcelReader(filePath);
var table = reader.ReadAll();  // 返回 DbTable

// 写入 Excel
using var writer = new ExcelWriter(filePath);
writer.WriteHeader("Name", "Age");
writer.WriteRow("张三", 25);
```

## 注意事项

- `ToJson()` 默认不美化、包含 null、PascalCase
- `ToJsonEntity<T>()` 返回可空，务必检查 null
- Binary 序列化双方必须统一 `IsLittleEndian`/`SizeWidth`/`EncodeInt`
- `SpanSerializer.Serialize` 返回 `IOwnerPacket`，**必须 Dispose** 归还缓冲区
- CSV/Excel 读写器实现 `IDisposable`，配合 `using` 使用
