# 类型转换 Utility

## 概述

`Utility` 是 NewLife.Core 中最基础的工具类，提供高效、安全的类型转换扩展方法。所有转换方法均支持默认值，在转换失败时返回默认值而不抛出异常，极大简化了日常开发中的类型转换操作。

**命名空间**：`NewLife`  
**文档地址**：https://newlifex.com/core/utility

## 核心特性

- **安全转换**：所有转换失败时返回默认值，不抛出异常
- **扩展方法**：直接在对象上调用 `.ToInt()`、`.ToDateTime()` 等
- **多类型支持**：支持字符串、全角字符、字节数组、时间戳等多种输入
- **高性能**：针对常见场景深度优化，避免不必要的内存分配
- **可扩展**：通过 `DefaultConvert` 类支持自定义转换逻辑

## 快速开始

```csharp
using NewLife;

// 字符串转整数
var num = "123".ToInt();           // 123
var num2 = "abc".ToInt(-1);        // -1（转换失败返回默认值）

// 字符串转时间
var dt = "2024-01-15".ToDateTime();
var dt2 = "invalid".ToDateTime();  // DateTime.MinValue

// 对象转布尔
var flag = "true".ToBoolean();     // true
var flag2 = "1".ToBoolean();       // true
var flag3 = "yes".ToBoolean();     // true
```

## API 参考

### 整数转换

#### ToInt

```csharp
public static Int32 ToInt(this Object? value, Int32 defaultValue = 0)
```

将对象转换为32位整数。

**支持的输入类型**：
- 字符串（含全角数字）
- 字节数组（小端序，1-4字节）
- DateTime（转为Unix秒，不含时区转换）
- DateTimeOffset（转为Unix秒）
- 实现 `IConvertible` 的类型

**示例**：
```csharp
// 基本转换
"123".ToInt()                    // 123
"  456  ".ToInt()                // 456（自动去除空格）
"１２３".ToInt()                 // 123（支持全角数字）
"1,234,567".ToInt()              // 1234567（支持千分位）

// 字节数组转换（小端序）
new Byte[] { 0x01 }.ToInt()                    // 1
new Byte[] { 0x01, 0x00 }.ToInt()              // 1
new Byte[] { 0x01, 0x00, 0x00, 0x00 }.ToInt()  // 1

// 时间转Unix秒
DateTime.Now.ToInt()             // 当前Unix时间戳（秒）

// 转换失败返回默认值
"abc".ToInt()                    // 0
"abc".ToInt(-1)                  // -1
((Object?)null).ToInt()          // 0
```

#### ToLong

```csharp
public static Int64 ToLong(this Object? value, Int64 defaultValue = 0)
```

将对象转换为64位长整数。

**特殊处理**：
- DateTime 转为 Unix 毫秒（不含时区转换）
- 字节数组支持 1-8 字节

**示例**：
```csharp
"9223372036854775807".ToLong()   // Int64.MaxValue
DateTime.Now.ToLong()            // 当前Unix时间戳（毫秒）
```

### 浮点数转换

#### ToDouble

```csharp
public static Double ToDouble(this Object? value, Double defaultValue = 0)
```

将对象转换为双精度浮点数。

**示例**：
```csharp
"3.14".ToDouble()                // 3.14
"3.14E+10".ToDouble()            // 31400000000（支持科学计数法）
"1,234.56".ToDouble()            // 1234.56（支持千分位）
```

#### ToDecimal

```csharp
public static Decimal ToDecimal(this Object? value, Decimal defaultValue = 0)
```

将对象转换为高精度浮点数，适用于金融计算等精度要求高的场景。

**示例**：
```csharp
"123456789.123456789".ToDecimal()  // 精确保留小数
```

### 布尔值转换

#### ToBoolean

```csharp
public static Boolean ToBoolean(this Object? value, Boolean defaultValue = false)
```

将对象转换为布尔值。

**支持的真值**：`true`、`True`、`1`、`y`、`yes`、`on`、`enable`、`enabled`  
**支持的假值**：`false`、`False`、`0`、`n`、`no`、`off`、`disable`、`disabled`

**示例**：
```csharp
"true".ToBoolean()               // true
"True".ToBoolean()               // true
"1".ToBoolean()                  // true
"yes".ToBoolean()                // true
"on".ToBoolean()                 // true
"enable".ToBoolean()             // true

"false".ToBoolean()              // false
"0".ToBoolean()                  // false
"no".ToBoolean()                 // false
"off".ToBoolean()                // false

"invalid".ToBoolean()            // false（默认值）
"invalid".ToBoolean(true)        // true（指定默认值）
```

### 时间转换

#### ToDateTime

```csharp
public static DateTime ToDateTime(this Object? value)
public static DateTime ToDateTime(this Object? value, DateTime defaultValue)
```

将对象转换为时间日期。

**支持的格式**：
- 标准日期时间字符串
- `yyyy-M-d` 格式
- `yyyy/M/d` 格式
- `yyyyMMddHHmmss` 格式
- `yyyyMMdd` 格式
- Unix 秒（Int32）
- Unix 毫秒（Int64，自动判断）
- UTC 标记（末尾 `Z` 或 ` UTC`）

**示例**：
```csharp
// 字符串转换
"2024-01-15".ToDateTime()
"2024-1-5".ToDateTime()          // 支持单位数月日
"2024/01/15".ToDateTime()
"20240115".ToDateTime()
"20240115120000".ToDateTime()
"2024-01-15 12:30:45".ToDateTime()
"2024-01-15T12:30:45Z".ToDateTime()  // UTC 时间

// Unix 时间戳转换
1705276800.ToDateTime()          // Unix 秒
1705276800000L.ToDateTime()      // Unix 毫秒（自动判断）
```

> **注意**：整数转时间时不进行 UTC 与本地时间转换。在物联网场景中，设备可能位于不同时区，建议统一使用 UTC 时间传输后再转换。

#### ToDateTimeOffset

```csharp
public static DateTimeOffset ToDateTimeOffset(this Object? value)
public static DateTimeOffset ToDateTimeOffset(this Object? value, DateTimeOffset defaultValue)
```

将对象转换为带时区的时间日期。

### 时间格式化

#### ToFullString

```csharp
public static String ToFullString(this DateTime value, String? emptyValue = null)
public static String ToFullString(this DateTime value, Boolean useMillisecond, String? emptyValue = null)
```

将时间格式化为 `yyyy-MM-dd HH:mm:ss` 标准格式。

**参数说明**：
- `useMillisecond`：是否包含毫秒，格式为 `yyyy-MM-dd HH:mm:ss.fff`
- `emptyValue`：当时间为 `MinValue` 时显示的替代字符串

**示例**：
```csharp
DateTime.Now.ToFullString()                    // "2024-01-15 12:30:45"
DateTime.Now.ToFullString(true)                // "2024-01-15 12:30:45.123"
DateTime.MinValue.ToFullString("")             // ""
DateTime.MinValue.ToFullString("N/A")          // "N/A"
```

#### Trim

```csharp
public static DateTime Trim(this DateTime value, String format = "s")
```

截断时间精度。

**格式参数**：
- `ns`：纳秒精度（实际为 100ns，即 1 tick）
- `us`：微秒精度
- `ms`：毫秒精度
- `s`：秒精度（默认）
- `m`：分钟精度
- `h`：小时精度

**示例**：
```csharp
var dt = new DateTime(2024, 1, 15, 12, 30, 45, 123);
dt.Trim("s")                     // 2024-01-15 12:30:45.000
dt.Trim("m")                     // 2024-01-15 12:30:00.000
dt.Trim("h")                     // 2024-01-15 12:00:00.000
dt.Trim("ms")                    // 保留毫秒
```

### 字节单位格式化

#### ToGMK

```csharp
public static String ToGMK(this Int64 value, String? format = null)
public static String ToGMK(this UInt64 value, String? format = null)
```

将字节数格式化为可读的单位字符串。

**示例**：
```csharp
1024L.ToGMK()                    // "1.00K"
1048576L.ToGMK()                 // "1.00M"
1073741824L.ToGMK()              // "1.00G"
1099511627776L.ToGMK()           // "1.00T"

// 自定义格式
1536L.ToGMK("n1")                // "1.5K"
1536L.ToGMK("n0")                // "2K"
```

### 异常处理

#### GetTrue

```csharp
public static Exception GetTrue(this Exception ex)
```

获取异常的真实内部异常，自动解包 `AggregateException`、`TargetInvocationException`、`TypeInitializationException` 等包装异常。

**示例**：
```csharp
try
{
    // 可能抛出包装异常的代码
}
catch (Exception ex)
{
    var realEx = ex.GetTrue();
    Console.WriteLine(realEx.Message);
}
```

#### GetMessage

```csharp
public static String GetMessage(this Exception ex)
```

获取格式化的异常消息，过滤掉不必要的堆栈信息（如 `System.Runtime.ExceptionServices` 等）。

## 自定义转换

通过替换 `Utility.Convert` 可以自定义所有类型转换的行为：

```csharp
public class MyConvert : DefaultConvert
{
    public override Int32 ToInt(Object? value, Int32 defaultValue)
    {
        // 自定义转换逻辑
        if (value is MyCustomType mct)
            return mct.Value;
            
        return base.ToInt(value, defaultValue);
    }
}

// 全局替换
Utility.Convert = new MyConvert();
```

## 最佳实践

### 1. 始终提供有意义的默认值

```csharp
// 推荐：明确指定默认值
var port = config["Port"].ToInt(8080);
var timeout = config["Timeout"].ToInt(30);

// 不推荐：使用隐式默认值 0 可能导致问题
var port = config["Port"].ToInt();  // 如果配置缺失，端口为 0
```

### 2. 时间戳转换注意时区

```csharp
// 物联网场景：设备上报 UTC 时间戳
var deviceTime = timestamp.ToDateTime();      // 不含时区转换
var localTime = deviceTime.ToLocalTime();     // 转为本地时间

// 或者使用 DateTimeOffset
var dto = timestamp.ToDateTimeOffset();
```

### 3. 利用链式调用简化代码

```csharp
// 传统写法
Int32 value;
if (!Int32.TryParse(str, out value))
    value = defaultValue;

// NewLife 写法
var value = str.ToInt(defaultValue);
```

## 性能说明

- 所有转换方法针对常见类型（String、Int32 等）进行了快速路径优化
- 字符串转换使用 `Span<T>` 避免不必要的内存分配
- 时间格式化避免使用 `ToString()` 格式化，采用手动拼接提升性能
- 字节数组转换直接使用 `BitConverter`，无额外开销

## 相关链接

- [字符串扩展 StringHelper](string_helper-字符串扩展StringHelper.md)
- [数据扩展 IOHelper](io_helper-数据扩展IOHelper.md)
