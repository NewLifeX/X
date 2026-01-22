# 对象容器 ObjectContainer

## 概述

`ObjectContainer` 是 NewLife.Core 中的轻量级依赖注入（DI）容器，提供简单而强大的 IoC 功能。支持单例、瞬态和作用域三种生命周期，兼容 `IServiceProvider` 接口，可作为 ASP.NET Core 内置 DI 的补充或替代。

**命名空间**：`NewLife.Model`  
**文档地址**：https://newlifex.com/core/object_container

## 核心特性

- **轻量级**：无外部依赖，代码精简
- **三种生命周期**：单例（Singleton）、瞬态（Transient）、作用域（Scoped）
- **构造函数注入**：自动解析构造函数参数
- **工厂委托**：支持自定义实例创建逻辑
- **全局容器**：提供 `ObjectContainer.Current` 全局访问
- **兼容性**：实现 `IServiceProvider` 接口，可与标准 DI 互操作

## 快速开始

```csharp
using NewLife.Model;

// 获取全局容器
var ioc = ObjectContainer.Current;

// 注册服务
ioc.AddSingleton<ILogger, ConsoleLogger>();       // 单例
ioc.AddTransient<IUserService, UserService>();    // 瞬态
ioc.AddScoped<IDbContext, MyDbContext>();         // 作用域

// 构建服务提供者
var provider = ioc.BuildServiceProvider();

// 解析服务
var logger = provider.GetService<ILogger>();
var userService = provider.GetRequiredService<IUserService>();
```

## API 参考

### 全局访问

#### Current

```csharp
public static IObjectContainer Current { get; set; }
```

全局默认容器实例。应用启动时自动创建。

**示例**：
```csharp
// 在任何地方访问全局容器
var container = ObjectContainer.Current;
container.AddSingleton<IMyService, MyService>();
```

#### Provider

```csharp
public static IServiceProvider Provider { get; set; }
```

全局默认服务提供者。由 `Current` 容器构建。

**示例**：
```csharp
// 直接通过全局提供者解析服务
var service = ObjectContainer.Provider.GetService<IMyService>();
```

### 服务注册

#### AddSingleton（单例）

```csharp
// 注册类型映射
IObjectContainer AddSingleton<TService, TImplementation>()
IObjectContainer AddSingleton(Type serviceType, Type implementationType)

// 注册实例
IObjectContainer AddSingleton<TService>(TService instance)
IObjectContainer AddSingleton(Type serviceType, Object instance)

// 注册工厂
IObjectContainer AddSingleton<TService>(Func<IServiceProvider, TService> factory)
IObjectContainer AddSingleton(Type serviceType, Func<IServiceProvider, Object> factory)
```

注册单例服务。整个应用生命周期内只创建一个实例。

**示例**：
```csharp
var ioc = ObjectContainer.Current;

// 类型映射
ioc.AddSingleton<ILogger, FileLogger>();

// 直接注册实例
var config = new AppConfig { Debug = true };
ioc.AddSingleton<AppConfig>(config);

// 工厂委托（延迟创建）
ioc.AddSingleton<IDbConnection>(sp =>
{
    var config = sp.GetService<AppConfig>();
    return new SqlConnection(config.ConnectionString);
});
```

#### AddTransient（瞬态）

```csharp
IObjectContainer AddTransient<TService, TImplementation>()
IObjectContainer AddTransient<TService>()
IObjectContainer AddTransient(Type serviceType, Type implementationType)
IObjectContainer AddTransient(Type serviceType, Func<IServiceProvider, Object> factory)
```

注册瞬态服务。每次请求都创建新实例。

**示例**：
```csharp
// 每次解析都创建新实例
ioc.AddTransient<IUserService, UserService>();

// 自身注册
ioc.AddTransient<OrderProcessor>();

// 工厂委托
ioc.AddTransient<HttpClient>(sp => new HttpClient
{
    BaseAddress = new Uri("https://api.example.com"),
    Timeout = TimeSpan.FromSeconds(30)
});
```

#### AddScoped（作用域）

```csharp
IObjectContainer AddScoped<TService, TImplementation>()
IObjectContainer AddScoped<TService>()
IObjectContainer AddScoped(Type serviceType, Type implementationType)
IObjectContainer AddScoped(Type serviceType, Func<IServiceProvider, Object> factory)
```

注册作用域服务。在同一作用域内共享实例。

**示例**：
```csharp
// 同一请求内共享
ioc.AddScoped<IDbContext, AppDbContext>();

// 工厂委托
ioc.AddScoped<IUnitOfWork>(sp =>
{
    var context = sp.GetService<IDbContext>();
    return new UnitOfWork(context);
});
```

#### TryAdd 系列

```csharp
IObjectContainer TryAddSingleton<TService, TImplementation>()
IObjectContainer TryAddTransient<TService, TImplementation>()
IObjectContainer TryAddScoped<TService, TImplementation>()
```

尝试添加服务，如果已存在相同服务类型则不添加。

**示例**：
```csharp
// 仅在未注册时添加
ioc.TryAddSingleton<ILogger, ConsoleLogger>();

// 后续尝试添加不会生效
ioc.TryAddSingleton<ILogger, FileLogger>();  // 被忽略

// 但 Add 会添加（允许多个实现）
ioc.AddSingleton<ILogger, FileLogger>();     // 会添加
```

### 服务解析

#### BuildServiceProvider

```csharp
public IServiceProvider BuildServiceProvider()
```

从容器构建服务提供者。

**示例**：
```csharp
var ioc = ObjectContainer.Current;
ioc.AddSingleton<IMyService, MyService>();

var provider = ioc.BuildServiceProvider();
```

#### GetService

```csharp
// IServiceProvider 接口方法
Object? GetService(Type serviceType)
```

获取服务实例，未找到返回 null。

**示例**：
```csharp
var service = provider.GetService(typeof(IMyService));
if (service != null)
{
    // 使用服务
}
```

#### 扩展方法

```csharp
// 泛型获取
T? GetService<T>(this IServiceProvider provider)

// 必需获取（未找到抛异常）
T GetRequiredService<T>(this IServiceProvider provider)

// 获取所有实现
IEnumerable<T> GetServices<T>(this IServiceProvider provider)
```

**示例**：
```csharp
// 泛型获取
var logger = provider.GetService<ILogger>();

// 必需服务（未注册会抛异常）
var config = provider.GetRequiredService<AppConfig>();

// 获取所有实现
var handlers = provider.GetServices<IMessageHandler>();
foreach (var handler in handlers)
{
    handler.Handle(message);
}
```

## 生命周期详解

### Singleton（单例）

```csharp
ioc.AddSingleton<IMyService, MyService>();
```

- **创建时机**：首次请求时创建
- **生命周期**：整个应用生命周期
- **适用场景**：配置、日志、缓存等无状态或全局共享的服务

### Transient（瞬态）

```csharp
ioc.AddTransient<IMyService, MyService>();
```

- **创建时机**：每次请求时创建
- **生命周期**：使用完即可被 GC
- **适用场景**：轻量级、无状态的服务

### Scoped（作用域）

```csharp
ioc.AddScoped<IMyService, MyService>();
```

- **创建时机**：作用域内首次请求时创建
- **生命周期**：作用域结束时释放
- **适用场景**：Web 请求、数据库上下文等

## 构造函数注入

ObjectContainer 支持自动构造函数注入：

```csharp
public interface ILogger { }
public class ConsoleLogger : ILogger { }

public interface IRepository { }
public class UserRepository : IRepository
{
    public ILogger Logger { get; }
    
    // 构造函数注入
    public UserRepository(ILogger logger)
    {
        Logger = logger;
    }
}

// 注册
ioc.AddSingleton<ILogger, ConsoleLogger>();
ioc.AddTransient<IRepository, UserRepository>();

// 解析时自动注入 ILogger
var repo = provider.GetService<IRepository>();
// repo.Logger 已被注入
```

### 构造函数选择规则

1. 选择参数最多的公共构造函数
2. 按参数顺序解析依赖
3. 基本类型使用默认值（int=0, string=null 等）
4. 所有参数都能解析时才使用该构造函数
5. 无法解析时抛出 `InvalidOperationException`

## 使用场景

### 1. 控制台应用

```csharp
class Program
{
    static void Main()
    {
        // 配置容器
        var ioc = ObjectContainer.Current;
        ioc.AddSingleton<ILogger, ConsoleLogger>();
        ioc.AddSingleton<AppConfig>();
        ioc.AddTransient<IUserService, UserService>();
        
        // 构建提供者
        var provider = ioc.BuildServiceProvider();
        
        // 运行应用
        var app = provider.GetRequiredService<IUserService>();
        app.Run();
    }
}
```

### 2. 与 ASP.NET Core 集成

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // 先在 NewLife 容器中注册
        var ioc = ObjectContainer.Current;
        ioc.AddSingleton<IMyService, MyService>();
        
        // 复制到 ASP.NET Core 容器
        foreach (var item in ioc.Services)
        {
            // 转换并添加到 services
        }
        
        // 或者直接替换容器
        // services.AddSingleton(ObjectContainer.Provider);
    }
}
```

### 3. 插件系统

```csharp
public class PluginLoader
{
    private readonly IObjectContainer _container;
    
    public PluginLoader()
    {
        _container = new ObjectContainer();
    }
    
    public void LoadPlugins(String pluginPath)
    {
        var assemblies = Directory.GetFiles(pluginPath, "*.dll")
            .Select(Assembly.LoadFrom);
        
        foreach (var assembly in assemblies)
        {
            var pluginTypes = assembly.GetTypes()
                .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract);
            
            foreach (var type in pluginTypes)
            {
                _container.AddTransient(typeof(IPlugin), type);
            }
        }
    }
    
    public IEnumerable<IPlugin> GetPlugins()
    {
        var provider = _container.BuildServiceProvider();
        return provider.GetServices<IPlugin>();
    }
}
```

### 4. 单元测试

```csharp
[TestClass]
public class UserServiceTests
{
    private IServiceProvider _provider;
    
    [TestInitialize]
    public void Setup()
    {
        var ioc = new ObjectContainer();
        
        // 注册 Mock 对象
        ioc.AddSingleton<ILogger>(new MockLogger());
        ioc.AddSingleton<IRepository>(new MockRepository());
        ioc.AddTransient<IUserService, UserService>();
        
        _provider = ioc.BuildServiceProvider();
    }
    
    [TestMethod]
    public void TestCreateUser()
    {
        var service = _provider.GetRequiredService<IUserService>();
        var result = service.CreateUser("test");
        Assert.IsNotNull(result);
    }
}
```

## 最佳实践

### 1. 在应用启动时完成注册

```csharp
// 推荐：启动时集中注册
public static void Main()
{
    var ioc = ObjectContainer.Current;
    ConfigureServices(ioc);
    
    var provider = ioc.BuildServiceProvider();
    // 运行应用...
}

static void ConfigureServices(IObjectContainer ioc)
{
    ioc.AddSingleton<ILogger, FileLogger>();
    ioc.AddTransient<IUserService, UserService>();
    // ...
}
```

### 2. 避免服务定位器模式

```csharp
// 不推荐：服务定位器
public class BadService
{
    public void DoWork()
    {
        var logger = ObjectContainer.Provider.GetService<ILogger>();
        logger.Log("Working...");
    }
}

// 推荐：构造函数注入
public class GoodService
{
    private readonly ILogger _logger;
    
    public GoodService(ILogger logger)
    {
        _logger = logger;
    }
    
    public void DoWork()
    {
        _logger.Log("Working...");
    }
}
```

### 3. 合理选择生命周期

```csharp
// 配置、日志等 → 单例
ioc.AddSingleton<AppConfig>();
ioc.AddSingleton<ILogger, FileLogger>();

// 数据库上下文 → 作用域（Web）或瞬态（控制台）
ioc.AddScoped<IDbContext, AppDbContext>();  // Web
ioc.AddTransient<IDbContext, AppDbContext>(); // 控制台

// 轻量级服务 → 瞬态
ioc.AddTransient<IValidator, UserValidator>();
```

### 4. 使用接口而非具体类型

```csharp
// 推荐：面向接口
ioc.AddSingleton<IUserService, UserService>();

// 不推荐：直接注册具体类型（难以测试和替换）
ioc.AddSingleton<UserService>();
```

## 与 Microsoft.Extensions.DependencyInjection 对比

| 特性 | ObjectContainer | MS DI |
|------|-----------------|-------|
| 包大小 | 极小（内置） | 需要额外包 |
| 生命周期 | 支持 | 支持 |
| 构造函数注入 | 支持 | 支持 |
| 属性注入 | 不支持 | 不支持 |
| 泛型注册 | 基本支持 | 完整支持 |
| 装饰器模式 | 不支持 | 支持 |
| 验证 | 无 | 支持 |

## 相关链接

- [轻量级应用主机 Host](host-轻量级应用主机Host.md)
- [插件框架 IPlugin](plugin-插件框架IPlugin.md)
