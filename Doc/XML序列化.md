# XML 序列化

## 概述

NewLife.Core 提供了灵活的 XML 序列化和反序列化功能，通过 `XmlHelper` 扩展方法可以方便地进行对象与 XML 的转换。支持自动添加注释、属性模式输出等特性，特别适合配置文件场景。

**命名空间**：`NewLife.Xml`（扩展方法）、`NewLife.Serialization`（核心实现）  
**文档地址**：https://newlifex.com/core/xml

## 核心特性

- **简洁 API**：`ToXml()` 和 `ToXmlEntity<T>()` 扩展方法
- **注释支持**：自动附加 `Description` 和 `DisplayName` 特性作为注释
- **属性模式**：可选择将属性序列化为 XML 属性而非元素
- **编码控制**：支持指定编码格式
- **文件操作**：直接序列化到文件或从文件反序列化

## 快速开始

### 序列化

```csharp
using NewLife.Xml;

public class AppConfig
{
    public String Name { get; set; }
    public Int32 Port { get; set; }
    public Boolean Debug { get; set; }
}

var config = new AppConfig
{
    Name = "MyApp",
    Port = 8080,
    Debug = true
};

// 序列化为 XML 字符串
var xml = config.ToXml();
```

**输出**：
```xml
<?xml version="1.0" encoding="utf-8"?>
<AppConfig>
  <Name>MyApp</Name>
  <Port>8080</Port>
  <Debug>true</Debug>
</AppConfig>
```

### 反序列化

```csharp
using NewLife.Xml;

var xml = """
<?xml version="1.0" encoding="utf-8"?>
<AppConfig>
  <Name>MyApp</Name>
  <Port>8080</Port>
  <Debug>true</Debug>
</AppConfig>
""";

var config = xml.ToXmlEntity<AppConfig>();
Console.WriteLine(config.Name);  // MyApp
```

## API 参考

### ToXml - 序列化

```csharp
// 基础序列化
public static String ToXml(this Object obj, Encoding? encoding = null, 
    Boolean attachComment = false, Boolean useAttribute = false)

// 完整参数
public static String ToXml(this Object obj, Encoding encoding, 
    Boolean attachComment, Boolean useAttribute, Boolean omitXmlDeclaration)

// 序列化到流
public static void ToXml(this Object obj, Stream stream, Encoding? encoding = null, 
    Boolean attachComment = false, Boolean useAttribute = false)

// 序列化到文件
public static void ToXmlFile(this Object obj, String file, Encoding? encoding = null, 
    Boolean attachComment = true)
```

**参数说明**：
- `encoding`：编码格式，默认 UTF-8
- `attachComment`：是否附加注释（使用 Description/DisplayName）
- `useAttribute`：是否使用 XML 属性模式
- `omitXmlDeclaration`：是否省略 XML 声明

### ToXmlEntity - 反序列化

```csharp
// 从字符串反序列化
public static TEntity? ToXmlEntity<TEntity>(this String xml) where TEntity : class

// 从流反序列化
public static TEntity? ToXmlEntity<TEntity>(this Stream stream, Encoding? encoding = null)

// 从文件反序列化
public static TEntity? ToXmlFileEntity<TEntity>(this String file, Encoding? encoding = null)
```

## 使用场景

### 1. 配置文件

```csharp
using System.ComponentModel;
using NewLife.Xml;

public class DatabaseConfig
{
    [Description("数据库服务器地址")]
    public String Server { get; set; } = "localhost";
    
    [Description("数据库端口")]
    public Int32 Port { get; set; } = 3306;
    
    [Description("数据库名称")]
    public String Database { get; set; } = "mydb";
    
    [Description("用户名")]
    public String User { get; set; } = "root";
    
    [Description("连接超时（秒）")]
    public Int32 Timeout { get; set; } = 30;
}

// 保存配置（带注释）
var config = new DatabaseConfig();
config.ToXmlFile("db.config", attachComment: true);

// 加载配置
var loaded = "db.config".ToXmlFileEntity<DatabaseConfig>();
```

**生成的 XML**：
```xml
<?xml version="1.0" encoding="utf-8"?>
<DatabaseConfig>
  <!--数据库服务器地址-->
  <Server>localhost</Server>
  <!--数据库端口-->
  <Port>3306</Port>
  <!--数据库名称-->
  <Database>mydb</Database>
  <!--用户名-->
  <User>root</User>
  <!--连接超时（秒）-->
  <Timeout>30</Timeout>
</DatabaseConfig>
```

### 2. 属性模式输出

```csharp
public class Item
{
    public Int32 Id { get; set; }
    public String Name { get; set; }
    public Decimal Price { get; set; }
}

var item = new Item { Id = 1, Name = "商品A", Price = 99.9M };

// 元素模式（默认）
var xml1 = item.ToXml();
// <Item><Id>1</Id><Name>商品A</Name><Price>99.9</Price></Item>

// 属性模式
var xml2 = item.ToXml(useAttribute: true);
// <Item Id="1" Name="商品A" Price="99.9" />
```

### 3. 复杂对象

```csharp
public class Order
{
    public Int32 Id { get; set; }
    public DateTime CreateTime { get; set; }
    public Customer Customer { get; set; }
    public List<OrderItem> Items { get; set; }
}

public class Customer
{
    public String Name { get; set; }
    public String Phone { get; set; }
}

public class OrderItem
{
    public String ProductName { get; set; }
    public Int32 Quantity { get; set; }
    public Decimal Price { get; set; }
}

var order = new Order
{
    Id = 1001,
    CreateTime = DateTime.Now,
    Customer = new Customer { Name = "张三", Phone = "13800138000" },
    Items = new List<OrderItem>
    {
        new() { ProductName = "商品A", Quantity = 2, Price = 50 },
        new() { ProductName = "商品B", Quantity = 1, Price = 100 }
    }
};

var xml = order.ToXml();
```

### 4. 省略 XML 声明

```csharp
// 省略 <?xml version="1.0" encoding="utf-8"?>
var xml = obj.ToXml(Encoding.UTF8, false, false, true);
```

### 5. 字典序列化

```csharp
// 字符串字典可以直接序列化
var dict = new Dictionary<String, String>
{
    ["Key1"] = "Value1",
    ["Key2"] = "Value2"
};

dict.ToXmlFile("settings.xml");
```

## Xml 类（高级用法）

对于需要更精细控制的场景，可以直接使用 `Xml` 类：

```csharp
using NewLife.Serialization;

// 序列化
var xml = new Xml
{
    Stream = stream,
    Encoding = Encoding.UTF8,
    UseAttribute = false,
    UseComment = true,
    EnumString = true  // 枚举使用字符串
};
xml.Write(obj);

// 反序列化
var xml = new Xml
{
    Stream = stream,
    Encoding = Encoding.UTF8
};
var result = xml.Read(typeof(MyClass));
```

### Xml 类属性

```csharp
public class Xml
{
    /// <summary>使用特性输出</summary>
    public Boolean UseAttribute { get; set; }
    
    /// <summary>使用注释</summary>
    public Boolean UseComment { get; set; }
    
    /// <summary>枚举使用字符串。默认true</summary>
    public Boolean EnumString { get; set; }
    
    /// <summary>XML写入设置</summary>
    public XmlWriterSettings Setting { get; set; }
}
```

## 特性支持

### XmlRoot - 根元素名称

```csharp
[XmlRoot("config")]
public class AppConfig
{
    public String Name { get; set; }
}

// 输出 <config><Name>...</Name></config>
```

### XmlElement - 元素名称

```csharp
public class User
{
    [XmlElement("user_name")]
    public String Name { get; set; }
}
```

### XmlAttribute - 输出为属性

```csharp
public class Item
{
    [XmlAttribute]
    public Int32 Id { get; set; }
    
    public String Name { get; set; }
}

// 输出 <Item Id="1"><Name>...</Name></Item>
```

### XmlIgnore - 忽略字段

```csharp
public class User
{
    public String Name { get; set; }
    
    [XmlIgnore]
    public String Password { get; set; }  // 不序列化
}
```

## 最佳实践

### 1. 配置文件使用注释

```csharp
// 保存时启用注释
config.ToXmlFile("app.config", attachComment: true);

// 使用 Description 特性添加说明
[Description("应用名称，用于日志标识")]
public String AppName { get; set; }
```

### 2. 文件操作注意事项

```csharp
// ToXmlFile 会自动创建目录
config.ToXmlFile("Config/app.xml");

// 检查文件是否存在
if (File.Exists(file))
{
    var config = file.ToXmlFileEntity<AppConfig>();
}
```

### 3. 编码一致性

```csharp
// 保存和加载使用相同编码
var encoding = Encoding.UTF8;
config.ToXmlFile("config.xml", encoding);
var loaded = "config.xml".ToXmlFileEntity<AppConfig>(encoding);
```

## 与 JSON 对比

| 特性 | XML | JSON |
|------|-----|------|
| 可读性 | 带注释更清晰 | 更紧凑 |
| 体积 | 较大 | 较小 |
| 注释支持 | 原生支持 | 不支持 |
| 配置文件 | ? 推荐 | ? 适用 |
| API 数据 | ? 不推荐 | ? 推荐 |

## 相关链接

- [JSON 序列化](json-JSON序列化.md)
- [配置系统 Config](config-配置系统Config.md)
- [二进制序列化 Binary](binary-二进制序列化Binary.md)
