# 插件框架 IPlugin

## 概述

`IPlugin` 是 NewLife.Core 中的通用插件接口，配合 `PluginManager` 插件管理器，可以快速构建一个简单通用的插件系统。支持插件发现、加载、初始化和资源释放等完整生命周期管理。

**命名空间**：`NewLife.Model`  
**文档地址**：https://newlifex.com/core/plugin

## 核心特性

- **自动发现**：扫描程序集自动发现 `IPlugin` 实现
- **宿主识别**：通过 `PluginAttribute` 标记插件所属宿主
- **依赖注入**：支持从 `IServiceProvider` 实例化插件
- **生命周期**：支持初始化和销毁回调
- **倒序销毁**：按加载的反向顺序释放资源

## 快速开始

### 定义插件

```csharp
using NewLife.Model;

// 插件实现
[Plugin("MyApp")]  // 标记支持的宿主
public class MyPlugin : IPlugin
{
    public Boolean Init(String? identity, IServiceProvider provider)
    {
        if (identity != "MyApp") return false;  // 非目标宿主
        
        Console.WriteLine("MyPlugin 初始化成功");
        return true;
    }
}
```

### 加载插件

```csharp
using NewLife.Model;

// 创建插件管理器
var manager = new PluginManager
{
    Identity = "MyApp",
    Provider = ObjectContainer.Provider
};

// 加载并初始化插件
manager.Load();
manager.Init();

// 使用插件
foreach (var plugin in manager.Plugins)
{
    Console.WriteLine($"已加载插件: {plugin.GetType().Name}");
}

// 释放资源
manager.Dispose();
```

## API 参考

### IPlugin 接口

```csharp
public interface IPlugin
{
    /// <summary>初始化</summary>
    /// <param name="identity">插件宿主标识</param>
    /// <param name="provider">服务提供者</param>
    /// <returns>返回初始化是否成功</returns>
    Boolean Init(String? identity, IServiceProvider provider);
}
```

**参数说明**：
- `identity`：宿主标识，用于插件判断是否为目标宿主
- `provider`：服务提供者，用于获取依赖服务

**返回值**：
- `true`：初始化成功，保留该插件
- `false`：非目标宿主或初始化失败，移除该插件

### PluginAttribute 特性

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class PluginAttribute : Attribute
{
    public String Identity { get; set; }
}
```

用于标记插件支持的宿主标识。

**示例**：
```csharp
// 支持单个宿主
[Plugin("WebServer")]
public class WebPlugin : IPlugin { }

// 支持多个宿主
[Plugin("WebServer")]
[Plugin("ApiServer")]
public class MultiHostPlugin : IPlugin { }
```

### PluginManager 类

#### 属性

```csharp
/// <summary>宿主标识</summary>
public String? Identity { get; set; }

/// <summary>宿主服务提供者</summary>
public IServiceProvider? Provider { get; set; }

/// <summary>插件集合</summary>
public IPlugin[]? Plugins { get; set; }

/// <summary>日志提供者</summary>
public ILog Log { get; set; }
```

#### Load - 加载插件

```csharp
public void Load()
```

扫描所有程序集，加载实现 `IPlugin` 接口的类型。

**加载规则**：
1. 扫描所有已加载的程序集
2. 查找实现 `IPlugin` 的非抽象类
3. 检查 `PluginAttribute`，过滤非当前宿主的插件
4. 通过服务提供者或反射实例化

#### Init - 初始化插件

```csharp
public void Init()
```

依次调用每个插件的 `Init` 方法，移除初始化失败的插件。

#### LoadPlugins - 获取插件类型

```csharp
public IEnumerable<Type> LoadPlugins()
```

仅获取插件类型，不实例化。适用于需要自定义实例化逻辑的场景。

## 插件生命周期

```
1. 宿主创建 PluginManager
2. 调用 Load() - 发现并实例化插件
3. 调用 Init() - 初始化插件
4. 插件运行期...
5. 调用 Dispose() - 倒序销毁插件
```

### 生命周期示例

```csharp
public class LifecyclePlugin : IPlugin, IDisposable
{
    private ILogger? _logger;
    
    // 构造函数：宿主加载插件时调用
    public LifecyclePlugin()
    {
        Console.WriteLine("1. 构造函数被调用");
    }
    
    // 初始化：宿主准备就绪后调用
    public Boolean Init(String? identity, IServiceProvider provider)
    {
        Console.WriteLine("2. Init 被调用");
        
        // 获取依赖
        _logger = provider.GetService<ILogger>();
        _logger?.Info("插件初始化");
        
        return true;
    }
    
    // 销毁：宿主释放时调用
    public void Dispose()
    {
        Console.WriteLine("3. Dispose 被调用");
        _logger?.Info("插件销毁");
    }
}
```

## 使用场景

### 1. 功能扩展插件

```csharp
// 定义扩展点接口
public interface IDataProcessor
{
    String Name { get; }
    void Process(Object data);
}

// 插件实现
[Plugin("DataPipeline")]
public class JsonProcessor : IPlugin, IDataProcessor
{
    public String Name => "JSON处理器";
    
    public Boolean Init(String? identity, IServiceProvider provider)
    {
        return identity == "DataPipeline";
    }
    
    public void Process(Object data)
    {
        var json = data.ToJson();
        Console.WriteLine($"处理JSON: {json}");
    }
}

// 宿主使用
var manager = new PluginManager { Identity = "DataPipeline" };
manager.Load();
manager.Init();

foreach (var plugin in manager.Plugins.OfType<IDataProcessor>())
{
    plugin.Process(myData);
}
```

### 2. 事件监听插件

```csharp
public interface IEventListener
{
    void OnEvent(String eventName, Object? args);
}

[Plugin("EventSystem")]
public class LoggingPlugin : IPlugin, IEventListener
{
    private ILog? _log;
    
    public Boolean Init(String? identity, IServiceProvider provider)
    {
        _log = provider.GetService<ILog>();
        return identity == "EventSystem";
    }
    
    public void OnEvent(String eventName, Object? args)
    {
        _log?.Info($"事件: {eventName}, 参数: {args}");
    }
}

// 事件分发
public class EventDispatcher
{
    private readonly IEventListener[] _listeners;
    
    public EventDispatcher(PluginManager manager)
    {
        _listeners = manager.Plugins?.OfType<IEventListener>().ToArray() ?? [];
    }
    
    public void Dispatch(String eventName, Object? args = null)
    {
        foreach (var listener in _listeners)
        {
            listener.OnEvent(eventName, args);
        }
    }
}
```

### 3. 模块化应用

```csharp
// 模块接口
public interface IModule
{
    String Name { get; }
    void Configure(IObjectContainer container);
    void Start();
    void Stop();
}

// 用户模块
[Plugin("MainApp")]
public class UserModule : IPlugin, IModule, IDisposable
{
    public String Name => "用户模块";
    
    public Boolean Init(String? identity, IServiceProvider provider)
    {
        return identity == "MainApp";
    }
    
    public void Configure(IObjectContainer container)
    {
        container.AddTransient<IUserService, UserService>();
        container.AddTransient<IUserRepository, UserRepository>();
    }
    
    public void Start()
    {
        XTrace.WriteLine($"{Name} 已启动");
    }
    
    public void Stop()
    {
        XTrace.WriteLine($"{Name} 已停止");
    }
    
    public void Dispose() => Stop();
}

// 主程序
class Program
{
    static void Main()
    {
        var ioc = ObjectContainer.Current;
        
        var manager = new PluginManager
        {
            Identity = "MainApp",
            Provider = ioc.BuildServiceProvider()
        };
        
        manager.Load();
        manager.Init();
        
        // 配置模块
        foreach (var module in manager.Plugins.OfType<IModule>())
        {
            module.Configure(ioc);
        }
        
        // 启动模块
        foreach (var module in manager.Plugins.OfType<IModule>())
        {
            module.Start();
        }
        
        Console.WriteLine("按任意键退出...");
        Console.ReadKey();
        
        manager.Dispose();
    }
}
```

### 4. 插件目录加载

```csharp
public class PluginLoader
{
    public void LoadFromDirectory(String path)
    {
        if (!Directory.Exists(path)) return;
        
        // 加载插件目录下的所有 DLL
        foreach (var file in Directory.GetFiles(path, "*.dll"))
        {
            try
            {
                Assembly.LoadFrom(file);
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
        }
    }
}

// 使用
var loader = new PluginLoader();
loader.LoadFromDirectory("plugins");

var manager = new PluginManager { Identity = "MyApp" };
manager.Load();  // 会发现新加载的程序集中的插件
manager.Init();
```

## 最佳实践

### 1. 插件接口设计

```csharp
// 定义清晰的扩展点接口
public interface IPlugin
{
    Boolean Init(String? identity, IServiceProvider provider);
}

// 功能接口与插件接口分离
public interface IDataExporter
{
    String Format { get; }
    Byte[] Export(Object data);
}

// 插件同时实现两个接口
[Plugin("ExportSystem")]
public class ExcelExporter : IPlugin, IDataExporter
{
    public String Format => "xlsx";
    
    public Boolean Init(String? identity, IServiceProvider provider) => true;
    
    public Byte[] Export(Object data) { /* ... */ }
}
```

### 2. 依赖注入

```csharp
[Plugin("MyApp")]
public class DatabasePlugin : IPlugin
{
    private IDbConnection? _connection;
    private ILogger? _logger;
    
    public Boolean Init(String? identity, IServiceProvider provider)
    {
        // 从容器获取依赖
        _connection = provider.GetService<IDbConnection>();
        _logger = provider.GetService<ILogger>();
        
        if (_connection == null)
        {
            _logger?.Error("未找到数据库连接");
            return false;
        }
        
        return true;
    }
}
```

### 3. 错误处理

```csharp
[Plugin("MyApp")]
public class SafePlugin : IPlugin
{
    public Boolean Init(String? identity, IServiceProvider provider)
    {
        try
        {
            // 初始化逻辑
            return true;
        }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);
            return false;  // 返回 false 而不是抛出异常
        }
    }
}
```

### 4. 资源释放

```csharp
[Plugin("MyApp")]
public class ResourcePlugin : IPlugin, IDisposable
{
    private FileStream? _file;
    private Boolean _disposed;
    
    public Boolean Init(String? identity, IServiceProvider provider)
    {
        _file = File.OpenWrite("plugin.log");
        return true;
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        _file?.Dispose();
    }
}
```

## 相关链接

- [对象容器 ObjectContainer](object_container-对象容器ObjectContainer.md)
- [反射扩展 Reflect](reflect-反射扩展Reflect.md)
- [轻量级应用主机 Host](host-轻量级应用主机Host.md)
