---
name: type-conversion
description: 使用 NewLife 类型转换扩展方法进行安全高效的类型转换
---

# NewLife 类型转换与基础工具使用指南

## 适用场景

- 字符串转数值/日期等基础类型转换
- 安全的类型转换（不抛异常，返回默认值）
- 字符串操作与格式化
- 池化 StringBuilder
- 时间戳与运行时信息

## 类型转换扩展

### 核心方法

```csharp
// 字符串 → 数值（转换失败返回 0）
var n = "123".ToInt();             // 123
var n = "abc".ToInt();             // 0
var n = "abc".ToInt(-1);           // -1（指定默认值）

// 字符串 → 布尔
var b = "true".ToBoolean();        // true
var b = "1".ToBoolean();           // true
var b = "yes".ToBoolean();         // true

// 字符串 → 浮点
var d = "3.14".ToDouble();         // 3.14

// 字符串 → Int64
var l = "9999999999".ToLong();     // 9999999999L

// 字符串 → DateTime
var dt = "2026-03-19".ToDateTime();
var dt = "2026-03-19 10:30:00".ToDateTime();

// 字符串 → 十六进制字节数组
var bytes = "48656C6C6F".ToHex();

// 字节数组 → 十六进制字符串
var hex = bytes.ToHex();
var hex = bytes.ToHex("-");        // 带分隔符
```

### 对象转换

```csharp
// 通用转换
var val = obj.ChangeType<Int32>();
var val = obj.ChangeType(typeof(Int32));

// 枚举转换
var level = "Info".ToEnum<LogLevel>();
```

### 与标准库对比

| NewLife | 标准库 | 区别 |
| ------ | ------ | ---- |
| `"123".ToInt()` | `Int32.Parse("123")` | 不抛异常，返回默认值 |
| `"abc".ToInt(0)` | `Int32.TryParse(...)` | 更简洁 |
| `"true".ToBoolean()` | `Boolean.Parse("true")` | 支持 "1"/"yes"/"on" |
| `obj.ChangeType<T>()` | `Convert.ChangeType()` | 更安全，支持更多类型 |

## 字符串工具

### 常用扩展

```csharp
// 空值判断
if (str.IsNullOrEmpty()) { }
if (str.IsNullOrWhiteSpace()) { }

// 截取
var first10 = str.Cut(10);          // 截取前 10 字符
var sub = str.Substring(5, 10);

// 编码
var base64 = str.ToBase64();
var original = base64.FromBase64();

// URL 编码
var encoded = str.UrlEncode();
var decoded = encoded.UrlDecode();

// 格式化（F 扩展方法）
var msg = "Hello {0}, you are {1}".F("test", 25);

// HTML 编码
var safe = str.HtmlEncode();
```

## Pool.StringBuilder

```csharp
// NewLife 池化 StringBuilder（替代 new StringBuilder()）
var sb = Pool.StringBuilder.Get();
sb.Append("Hello ");
sb.Append("World");
var result = sb.Put(true);  // 返回字符串并归还到池

// 也可手动归还
var sb = Pool.StringBuilder.Get();
try
{
    sb.Append("...");
    return sb.ToString();
}
finally
{
    sb.Put();  // 归还（不取字符串）
}
```

## 时间戳

```csharp
// 高精度时间戳（兼容 .NET 4.5，替代 Environment.TickCount64）
var tick = Runtime.TickCount64;

// 计算耗时
var start = Runtime.TickCount64;
DoWork();
var elapsed = Runtime.TickCount64 - start;
XTrace.WriteLine("耗时 {0}ms", elapsed);
```

## 运行时信息

```csharp
// 机器信息
var mi = MachineInfo.Current;
mi.CpuID         // CPU 标识
mi.Memory        // 总内存（字节）
mi.AvailableMemory // 可用内存
mi.CpuRate       // CPU 使用率
mi.Temperature   // CPU 温度
mi.Board         // 主板信息

// 运行时
Runtime.IsWindows   // 是否 Windows
Runtime.IsLinux     // 是否 Linux
Runtime.ProcessId   // 当前进程 ID
Runtime.CachePath   // 缓存目录
```

## 注意事项

- 所有 `ToXxx()` 扩展方法**不会抛异常**，转换失败返回默认值
- `ToInt()` / `ToLong()` / `ToDouble()` 第一个参数为失败时的默认值
- `Pool.StringBuilder.Get()` 获取的 StringBuilder 必须调用 `Put()` 归还
- `Runtime.TickCount64` 在所有 .NET 版本可用，`Environment.TickCount64` 仅 .NET 5+
- 这些扩展方法定义在 `NewLife` 命名空间，引用 NewLife.Core 后自动可用
