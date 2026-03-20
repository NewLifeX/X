---
name: configuration
description: 使用 NewLife 配置系统管理应用配置，支持本地文件、配置中心和命令行参数
---

# NewLife 配置系统使用指南

## 适用场景

- 应用程序配置管理（替代 appsettings.json）
- 强类型配置绑定
- 远程配置中心集成（Stardust / Apollo）
- 配置热更新和变更通知
- 命令行参数解析

## 强类型配置（Config\<T\>）

### 定义配置类

```csharp
public class AppConfig : Config<AppConfig>
{
    /// <summary>应用名称</summary>
    public String AppName { get; set; } = "MyApp";

    /// <summary>数据库连接字符串</summary>
    public String ConnectionString { get; set; } = "Data Source=app.db";

    /// <summary>服务端口</summary>
    public Int32 Port { get; set; } = 8080;

    /// <summary>启用调试模式</summary>
    public Boolean Debug { get; set; }

    /// <summary>加载后校验</summary>
    protected override void OnLoaded()
    {
        if (Port <= 0 || Port > 65535) Port = 8080;
    }
}
```

### 使用配置

```csharp
// 首次访问自动从文件加载（默认 AppConfig.json）
var config = AppConfig.Current;

var name = config.AppName;
var port = config.Port;

// 修改并保存
config.Debug = true;
config.Save();

// 首次运行生成默认配置文件
if (config.IsNew)
    XTrace.WriteLine("已生成默认配置文件，请修改后重启");
```

## IConfigProvider 直接使用

### JSON 配置文件

```csharp
var provider = new JsonConfigProvider { FileName = "config.json" };
provider.LoadAll();

// 读取值（冒号分隔多级 key）
var connStr = provider["Database:ConnectionString"];

// 绑定到对象
var dbConfig = provider.Load<DatabaseConfig>("Database");

// 监听变更
provider.Changed += (s, e) => XTrace.WriteLine("配置已变更");
```

### XML / INI 配置

```csharp
// XML
var provider = new XmlConfigProvider { FileName = "config.xml" };

// INI
var provider = new IniConfigProvider { FileName = "config.ini" };
```

## 远程配置中心

### Stardust 配置中心

```csharp
var provider = new HttpConfigProvider
{
    Server = "http://star.newlifex.com:6600",
    AppId = "MyApp",
    Secret = "xxx",
    Scope = "production",    // 环境标识
    Period = 60,             // 轮询间隔秒数
};
provider.LoadAll();

// 替换全局 Config 提供者
AppConfig.Provider = provider;

// 之后通过 AppConfig.Current 自动从配置中心获取
```

### Apollo 配置中心

```csharp
var provider = new ApolloConfigProvider
{
    Server = "http://apollo-server:8080",
    AppId = "MyApp",
};
```

## 自动绑定与热更新

```csharp
var provider = new JsonConfigProvider { FileName = "app.json" };
provider.LoadAll();

var config = new AppConfig();
provider.Bind(config, autoReload: true);  // 文件变更时自动更新对象

// 或带变更回调
provider.Bind(config, "AppConfig", section =>
{
    XTrace.WriteLine("配置已更新：{0}", section["AppName"]);
});
```

## 命令行参数

```csharp
var parser = new CommandParser { IgnoreCase = true };
var args = parser.Parse(Environment.GetCommandLineArgs());

// 命令行: --port 8080 --debug -v
var port = args["port"].ToInt();     // 8080
var debug = args.ContainsKey("debug"); // true
var verbose = args.ContainsKey("v"); // true

// 字符串分割为参数数组
var parts = CommandParser.Split("--port 8080 --name \"My App\"");
```

## 内置全局配置

```csharp
// Setting 类（全局应用设置，对应 Config/Core.config）
var setting = Setting.Current;
setting.LogPath = "Logs";
setting.TempPath = "Temp";
setting.PluginPath = "Plugins";
setting.Save();

// SysConfig（系统配置，应用名/版本/显示名等元数据）
var sys = SysConfig.Current;
sys.Name = "MyApp";
sys.DisplayName = "我的应用";
```

## 注意事项

- `Config<T>.Current` 是线程安全的单例
- 默认使用 JSON 格式，文件名为 `Config/{类名}.json`
- `OnLoaded()` 在每次加载后调用，适合做校验和默认值修正
- `IsNew = true` 表示配置文件首次创建，可提示用户修改
- `HttpConfigProvider` 会本地缓存（加密），即使配置中心不可用也能启动
- 不要在静态构造函数中访问 `Config<T>.Current`（可能死锁）
