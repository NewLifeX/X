# 配置系统 Config

## 概述

NewLife.Core 提供了强大灵活的配置系统，支持 XML、JSON、INI 等多种格式，以及本地文件和远程配置中心。通过 `Config<T>` 基类可以快速创建强类型配置，支持热更新和自动保存。

**命名空间**：`NewLife.Configuration`  
**文档地址**：https://newlifex.com/core/config

## 核心特性

- **强类型配置**：继承 `Config<T>` 自动管理配置文件
- **多格式支持**：XML、JSON、INI、HTTP 等配置提供者
- **热更新**：配置文件变化时自动重载
- **注释支持**：XML 格式支持自动生成注释
- **分层配置**：支持多级嵌套配置结构
- **配置中心**：支持远程配置中心（如星尘）

## 快速开始

### 定义配置类

```csharp
using NewLife.Configuration;
using System.ComponentModel;

/// <summary>应用配置</summary>
[Config("App")]  // 配置文件名为 App.config
public class AppConfig : Config<AppConfig>
{
    [Description("应用名称")]
    public String Name { get; set; } = "MyApp";
    
    [Description("服务端口")]
    public Int32 Port { get; set; } = 8080;
    
    [Description("调试模式")]
    public Boolean Debug { get; set; }
    
    [Description("数据库连接")]
    public String ConnectionString { get; set; } = "Server=.;Database=test";
}
```

### 使用配置

```csharp
// 读取配置（自动加载/创建配置文件）
var config = AppConfig.Current;

Console.WriteLine($"应用: {config.Name}");
Console.WriteLine($"端口: {config.Port}");

// 修改并保存
config.Debug = true;
config.Save();
```

**自动生成的配置文件** (`App.config`)：
```xml
<?xml version="1.0" encoding="utf-8"?>
<App>
  <!--应用名称-->
  <Name>MyApp</Name>
  <!--服务端口-->
  <Port>8080</Port>
  <!--调试模式-->
  <Debug>false</Debug>
  <!--数据库连接-->
  <ConnectionString>Server=.;Database=test</ConnectionString>
</App>
```

## API 参考

### Config&lt;T&gt; 基类

```csharp
public class Config<TConfig> where TConfig : Config<TConfig>, new()
{
    /// <summary>当前使用的提供者</summary>
    public static IConfigProvider? Provider { get; set; }
    
    /// <summary>当前实例</summary>
    public static TConfig Current { get; }
    
    /// <summary>加载配置</summary>
    public virtual Boolean Load();
    
    /// <summary>保存配置</summary>
    public virtual Boolean Save();
    
    /// <summary>配置加载后触发</summary>
    protected virtual void OnLoaded() { }
}
```

### ConfigAttribute 特性

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class ConfigAttribute : Attribute
{
    /// <summary>配置名（文件名，不含扩展名）</summary>
    public String Name { get; set; }
    
    /// <summary>配置提供者类型</summary>
    public Type? Provider { get; set; }
}
```

### IConfigProvider 接口

```csharp
public interface IConfigProvider
{
    /// <summary>名称</summary>
    String Name { get; set; }
    
    /// <summary>根元素</summary>
    IConfigSection Root { get; set; }
    
    /// <summary>是否新配置</summary>
    Boolean IsNew { get; set; }
    
    /// <summary>获取/设置配置值</summary>
    String? this[String key] { get; set; }
    
    /// <summary>加载配置</summary>
    Boolean LoadAll();
    
    /// <summary>保存配置</summary>
    Boolean SaveAll();
    
    /// <summary>加载到模型</summary>
    T? Load<T>(String? path = null) where T : new();
    
    /// <summary>绑定模型（热更新）</summary>
    void Bind<T>(T model, Boolean autoReload = true, String? path = null);
    
    /// <summary>配置改变事件</summary>
    event EventHandler? Changed;
}
```

## 配置提供者

### XmlConfigProvider（默认）

```csharp
// 自动使用 XML 格式
[Config("Database")]
public class DbConfig : Config<DbConfig> { }

// 或显式指定
[Config("Database", Provider = typeof(XmlConfigProvider))]
public class DbConfig : Config<DbConfig> { }
```

### JsonConfigProvider

```csharp
[Config("appsettings", Provider = typeof(JsonConfigProvider))]
public class AppSettings : Config<AppSettings>
{
    public String Name { get; set; }
    public LoggingConfig Logging { get; set; } = new();
}

public class LoggingConfig
{
    public String Level { get; set; } = "Information";
    public Boolean Console { get; set; } = true;
}
```

**生成的 JSON 文件**：
```json
{
  "Name": "MyApp",
  "Logging": {
    "Level": "Information",
    "Console": true
  }
}
```

### InIConfigProvider

```csharp
[Config("settings", Provider = typeof(InIConfigProvider))]
public class IniConfig : Config<IniConfig>
{
    public String Server { get; set; }
    public Int32 Port { get; set; }
}
```

### HttpConfigProvider

用于连接远程配置中心：

```csharp
[HttpConfig("http://config.server.com", AppId = "myapp", Secret = "xxx")]
public class RemoteConfig : Config<RemoteConfig>
{
    public String Setting1 { get; set; }
}
```

## 使用场景

### 1. 数据库配置

```csharp
[Config("Database")]
public class DatabaseConfig : Config<DatabaseConfig>
{
    [Description("数据库类型")]
    public String DbType { get; set; } = "MySql";
    
    [Description("连接字符串")]
    public String ConnectionString { get; set; }
    
    [Description("最大连接数")]
    public Int32 MaxPoolSize { get; set; } = 100;
    
    [Description("命令超时（秒）")]
    public Int32 CommandTimeout { get; set; } = 30;
}

// 使用
var db = DatabaseConfig.Current;
var connStr = db.ConnectionString;
```

### 2. 嵌套配置

```csharp
[Config("Service")]
public class ServiceConfig : Config<ServiceConfig>
{
    public String Name { get; set; } = "MyService";
    public HttpConfig Http { get; set; } = new();
    public CacheConfig Cache { get; set; } = new();
}

public class HttpConfig
{
    public Int32 Port { get; set; } = 8080;
    public Int32 Timeout { get; set; } = 30;
    public Boolean Ssl { get; set; }
}

public class CacheConfig
{
    public String Type { get; set; } = "Memory";
    public Int32 Expire { get; set; } = 3600;
}
```

### 3. 数组配置

```csharp
[Config("Servers")]
public class ServersConfig : Config<ServersConfig>
{
    public String[] Hosts { get; set; } = ["localhost"];
    public List<EndpointConfig> Endpoints { get; set; } = new();
}

public class EndpointConfig
{
    public String Host { get; set; }
    public Int32 Port { get; set; }
}
```

### 4. 配置验证

```csharp
[Config("App")]
public class AppConfig : Config<AppConfig>
{
    public Int32 Port { get; set; } = 8080;
    
    protected override void OnLoaded()
    {
        // 验证配置
        if (Port <= 0 || Port > 65535)
        {
            Port = 8080;
            XTrace.WriteLine("端口配置无效，使用默认值 8080");
        }
    }
}
```

### 5. 配置变更监听

```csharp
var config = AppConfig.Current;
AppConfig.Provider.Changed += (s, e) =>
{
    XTrace.WriteLine("配置已更新");
    // 重新读取配置
    config = AppConfig.Current;
};
```

### 6. 直接使用提供者

```csharp
// 不继承 Config<T>，直接使用提供者
var provider = new JsonConfigProvider { FileName = "custom.json" };
provider.LoadAll();

// 读取值
var name = provider["Name"];
var port = provider["Server:Port"].ToInt();

// 设置值
provider["Debug"] = "true";
provider.SaveAll();
```

## 配置文件路径

默认配置文件存放在应用程序目录，可通过以下方式自定义：

```csharp
// 相对路径
[Config("Config/App")]
public class AppConfig : Config<AppConfig> { }

// 配置提供者初始化
var provider = new XmlConfigProvider();
provider.Init("Config/App.config");
```

## 最佳实践

### 1. 使用 Description 特性

```csharp
[Description("日志级别：Debug/Info/Warn/Error")]
public String LogLevel { get; set; } = "Info";
```

### 2. 提供默认值

```csharp
public Int32 MaxRetry { get; set; } = 3;
public String[] AllowedHosts { get; set; } = ["*"];
```

### 3. 敏感信息处理

```csharp
[Config("Secrets")]
public class SecretsConfig : Config<SecretsConfig>
{
    // 建议从环境变量读取
    public String ApiKey { get; set; } = 
        Environment.GetEnvironmentVariable("API_KEY") ?? "";
}
```

### 4. 配置重载

```csharp
// 强制重新加载配置
AppConfig._Current = null;
var fresh = AppConfig.Current;
```

## 与 appsettings.json 集成

```csharp
// 加载 ASP.NET Core 风格的配置
var provider = JsonConfigProvider.LoadAppSettings();
var connectionString = provider["ConnectionStrings:Default"];
var logLevel = provider["Logging:LogLevel:Default"];
```

## 配置继承

```csharp
public abstract class BaseConfig<T> : Config<T> where T : BaseConfig<T>, new()
{
    [Description("应用版本")]
    public String Version { get; set; } = "1.0.0";
    
    [Description("启用调试")]
    public Boolean Debug { get; set; }
}

[Config("MyApp")]
public class MyAppConfig : BaseConfig<MyAppConfig>
{
    [Description("特定设置")]
    public String CustomSetting { get; set; }
}
```

## 相关链接

- [JSON 序列化](json-JSON序列化.md)
- [XML 序列化](xml-XML序列化.md)
- [日志系统 ILog](log-日志ILog.md)
