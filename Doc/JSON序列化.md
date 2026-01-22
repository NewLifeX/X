# JSON 序列化

## 概述

NewLife.Core 提供了轻量级的 JSON 序列化和反序列化功能，通过 `JsonHelper` 扩展方法可以方便地进行对象与 JSON 字符串的转换。内置 `FastJson` 实现，同时支持切换到 `System.Text.Json`。

**命名空间**：`NewLife.Serialization`  
**文档地址**：https://newlifex.com/core/json

## 核心特性

- **轻量级**：内置 `FastJson` 实现，无外部依赖
- **高性能**：针对常见场景优化，支持对象池
- **扩展方法**：`ToJson()` 和 `ToJsonEntity<T>()` 简洁易用
- **配置灵活**：支持驼峰命名、忽略空值、缩进格式化等
- **类型转换**：自动处理常见类型转换
- **可切换**：支持切换到 `System.Text.Json` 实现

## 快速开始

### 序列化

```csharp
using NewLife.Serialization;

// 简单对象序列化
var user = new { Id = 1, Name = "张三", Age = 25 };
var json = user.ToJson();
// {"Id":1,"Name":"张三","Age":25}

// 格式化输出
var jsonIndented = user.ToJson(true);
// {
//   "Id": 1,
//   "Name": "张三",
//   "Age": 25
// }

// 驼峰命名
var jsonCamel = user.ToJson(false, true, true);
// {"id":1,"name":"张三","age":25}
```

### 反序列化

```csharp
using NewLife.Serialization;

var json = """{"Id":1,"Name":"张三","Age":25}""";

// 反序列化为指定类型
var user = json.ToJsonEntity<User>();

// 反序列化为动态字典
var dict = json.DecodeJson();
var name = dict["Name"];  // "张三"
```

## API 参考

### ToJson - 序列化

```csharp
// 基础序列化
public static String ToJson(this Object value, Boolean indented = false)

// 完整参数
public static String ToJson(this Object value, Boolean indented, Boolean nullValue, Boolean camelCase)

// 使用配置对象
public static String ToJson(this Object value, JsonOptions jsonOptions)
```

**参数说明**：
- `indented`：是否缩进格式化，默认 false
- `nullValue`：是否输出空值，默认 true
- `camelCase`：是否使用驼峰命名，默认 false

**示例**：
```csharp
var obj = new
{
    Id = 1,
    Name = "测试",
    Description = (String?)null,
    CreateTime = DateTime.Now
};

// 默认输出
obj.ToJson();
// {"Id":1,"Name":"测试","Description":null,"CreateTime":"2025-01-07 12:00:00"}

// 忽略空值
obj.ToJson(false, false, false);
// {"Id":1,"Name":"测试","CreateTime":"2025-01-07 12:00:00"}

// 驼峰命名 + 格式化
obj.ToJson(true, true, true);
```

### ToJsonEntity - 反序列化

```csharp
// 泛型方法
public static T? ToJsonEntity<T>(this String json)

// 指定类型
public static Object? ToJsonEntity(this String json, Type type)
```

**示例**：
```csharp
var json = """{"id":1,"name":"张三","roles":["admin","user"]}""";

// 反序列化为类
public class User
{
    public Int32 Id { get; set; }
    public String Name { get; set; }
    public String[] Roles { get; set; }
}

var user = json.ToJsonEntity<User>();
Console.WriteLine(user.Name);  // 张三
Console.WriteLine(user.Roles[0]);  // admin
```

### DecodeJson - 解析为字典

```csharp
public static IDictionary<String, Object?>? DecodeJson(this String json)
```

将 JSON 字符串解析为字典，适用于动态访问场景。

**示例**：
```csharp
var json = """{"code":0,"data":{"id":1,"name":"test"},"message":"ok"}""";

var dict = json.DecodeJson();
var code = dict["code"].ToInt();  // 0
var data = dict["data"] as IDictionary<String, Object>;
var id = data["id"].ToInt();  // 1
```

### JsonOptions - 配置选项

```csharp
public class JsonOptions
{
    /// <summary>使用驼峰命名。默认false</summary>
    public Boolean CamelCase { get; set; }
    
    /// <summary>忽略空值。默认false</summary>
    public Boolean IgnoreNullValues { get; set; }
    
    /// <summary>忽略循环引用。默认false</summary>
    public Boolean IgnoreCycles { get; set; }
    
    /// <summary>缩进格式化。默认false</summary>
    public Boolean WriteIndented { get; set; }
    
    /// <summary>使用完整时间格式。默认false</summary>
    public Boolean FullTime { get; set; }
    
    /// <summary>枚举使用字符串。默认false使用数字</summary>
    public Boolean EnumString { get; set; }
    
    /// <summary>长整型作为字符串。避免JS精度丢失，默认false</summary>
    public Boolean Int64AsString { get; set; }
}
```

**示例**：
```csharp
var options = new JsonOptions
{
    CamelCase = true,
    IgnoreNullValues = true,
    WriteIndented = true,
    EnumString = true
};

var json = obj.ToJson(options);
```

### Format - 格式化 JSON

```csharp
public static String Format(String json)
```

将压缩的 JSON 字符串格式化为易读格式。

**示例**：
```csharp
var json = """{"id":1,"name":"test","items":[1,2,3]}""";
var formatted = JsonHelper.Format(json);
// {
//   "id":  1,
//   "name":  "test",
//   "items":  [
//     1,
//     2,
//     3
//   ]
// }
```

## IJsonHost 接口

`IJsonHost` 是 JSON 序列化的核心接口，可以切换不同的实现：

```csharp
public interface IJsonHost
{
    IServiceProvider ServiceProvider { get; set; }
    JsonOptions Options { get; set; }
    
    String Write(Object value, Boolean indented = false, Boolean nullValue = true, Boolean camelCase = false);
    String Write(Object value, JsonOptions jsonOptions);
    Object? Read(String json, Type type);
    Object? Convert(Object obj, Type targetType);
    Object? Parse(String json);
    IDictionary<String, Object?>? Decode(String json);
}
```

### 切换实现

```csharp
// 默认使用 FastJson
JsonHelper.Default = new FastJson();

// 切换到 System.Text.Json（.NET 5+）
JsonHelper.Default = new SystemJson();
```

## 使用场景

### 1. Web API 数据交换

```csharp
// 序列化响应
public class ApiResult<T>
{
    public Int32 Code { get; set; }
    public String? Message { get; set; }
    public T? Data { get; set; }
}

var result = new ApiResult<User>
{
    Code = 0,
    Message = "success",
    Data = new User { Id = 1, Name = "test" }
};

// 使用驼峰命名（前端友好）
var json = result.ToJson(false, true, true);

// 解析响应
var response = json.ToJsonEntity<ApiResult<User>>();
```

### 2. 配置文件处理

```csharp
// 读取 JSON 配置
var json = File.ReadAllText("config.json");
var config = json.ToJsonEntity<AppConfig>();

// 保存配置
var newJson = config.ToJson(true);  // 格式化便于阅读
File.WriteAllText("config.json", newJson);
```

### 3. 日志记录

```csharp
public void LogRequest(Object request)
{
    // 序列化请求参数用于日志
    var json = request.ToJson();
    XTrace.WriteLine($"Request: {json}");
}
```

### 4. 动态数据处理

```csharp
// 处理不确定结构的 JSON
var json = await httpClient.GetStringAsync(url);
var dict = json.DecodeJson();

if (dict.TryGetValue("error", out var error))
{
    throw new Exception(error?.ToString());
}

var data = dict["data"] as IDictionary<String, Object>;
// 动态访问字段...
```

### 5. 处理长整型精度问题

```csharp
// JavaScript 无法精确表示超过 2^53 的整数
var options = new JsonOptions { Int64AsString = true };

var obj = new { Id = 9007199254740993L };
var json = obj.ToJson(options);
// {"Id":"9007199254740993"}  // 字符串形式，避免精度丢失
```

## 特殊类型处理

### 日期时间

```csharp
var obj = new { Time = DateTime.Now };

// 默认格式
obj.ToJson();
// {"Time":"2025-01-07 12:00:00"}

// 完整 ISO 格式
var options = new JsonOptions { FullTime = true };
obj.ToJson(options);
// {"Time":"2025-01-07T12:00:00.0000000+08:00"}
```

### 枚举

```csharp
public enum Status { Pending, Active, Closed }

var obj = new { Status = Status.Active };

// 默认使用数字
obj.ToJson();
// {"Status":1}

// 使用字符串
var options = new JsonOptions { EnumString = true };
obj.ToJson(options);
// {"Status":"Active"}
```

### 字节数组

```csharp
var obj = new { Data = new Byte[] { 1, 2, 3, 4 } };

// 默认 Base64
obj.ToJson();
// {"Data":"AQIDBA=="}
```

## 最佳实践

### 1. 复用配置对象

```csharp
// 定义全局配置
public static class JsonConfig
{
    public static readonly JsonOptions Api = new()
    {
        CamelCase = true,
        IgnoreNullValues = true
    };
    
    public static readonly JsonOptions Log = new()
    {
        WriteIndented = false,
        IgnoreNullValues = true
    };
}

// 使用
var json = data.ToJson(JsonConfig.Api);
```

### 2. 处理空值

```csharp
// 反序列化时注意空值
var user = json.ToJsonEntity<User>();
if (user == null)
{
    // JSON 为 null 或解析失败
}

// 或使用空字符串检查
if (json.IsNullOrEmpty()) return;
var user = json.ToJsonEntity<User>();
```

### 3. 大数据量处理

```csharp
// 对于大量数据，考虑分批处理
// 避免一次性序列化/反序列化超大对象
```

## 性能说明

- `FastJson` 针对常见场景优化，适合大多数应用
- 对于高性能要求，可切换到 `System.Text.Json`
- 避免频繁创建 `JsonOptions`，建议复用
- 字符串操作使用对象池优化

## 相关链接

- [二进制序列化 Binary](binary-二进制序列化Binary.md)
- [XML 序列化](xml-XML序列化Xml.md)
- [配置系统 Config](config-配置系统Config.md)
