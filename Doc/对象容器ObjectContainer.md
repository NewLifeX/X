# 对象容器 ObjectContainer

## 简介

`ObjectContainer` 是 NewLife.Core 中的轻量级对象容器，支持依赖注入（DI）功能。提供简单而强大的 IoC 容器功能，支持单例、瞬态和作用域生命周期，兼容 `IServiceProvider` 接口，可以作为 ASP.NET Core 内置 DI 的补充或替代。

**命名空间**：`NewLife.Model`  
**文档地址**：https://newlifex.com/core/object_container

## 核心特性

- **轻量级**：无外部依赖，代码精简
- **三种生命周期**：单例（Singleton）、瞬态（Transient）、作用域（Scoped）
- **构造函数注入**：自动解析构造函数参数，优先选择参数最多的可匹配构造函数
- **工厂委托**：支持自定义实例创建逻辑
- **全局容器**：提供 `ObjectContainer.Current` 和 `ObjectContainer.Provider` 全局访问
- **标准兼容**：实现 `IServiceProvider` 接口，与标准 DI 容器互操作
- **作用域支持**：支持 `IServiceScope` 和 `IServiceScopeFactory`

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

## 核心类型

### IObjectContainer 接口

对象容器的核心接口，定义了服务注册和解析的基本能力。

| 成员 | 类型 | 说明 |
|------|------|------|
| `Services` | `IList<IObject>` | 服务注册集合 |
| `Register` | 方法 | 注册类型和名称（已标记 EditorBrowsable.Never） |
| `Add` | 方法 | 添加服务注册，允许重复添加同一个服务 |
| `TryAdd` | 方法 | 尝试添加服务注册，不允许重复添加同一个服务 |
| `GetService` | 方法 | 解析类型的实例 |

### ObjectLifetime 枚举

定义服务的生命周期策略。

| 值 | 说明 |
|------|------|
| `Singleton` | 单实例，整个应用程序生命周期内只有一个实例 |
| `Scoped` | 容器内单实例，同一作用域内共享实例 |
| `Transient` | 每次一个实例，每次请求都创建新实例 |

### IObject 接口

对象映射接口，描述服务的类型、实现和生命周期。

| 成员 | 类型 | 说明 |
|------|------|------|
| `ServiceType` | `Type` | 服务类型，接口或抽象类类型 |
| `ImplementationType` | `Type?` | 实现类型，具体实现类类型 |
| `Lifetime` | `ObjectLifetime` | 生命周期，控制实例的创建和销毁策略 |

### ServiceDescriptor 类

服务描述符，实现 `IObject` 接口，描述服务的完整信息。

| 属性 | 类型 | 说明 |
|------|------|------|
| `ServiceType` | `Type` | 服务类型，通常是接口或抽象类 |
| `ImplementationType` | `Type?` | 实现类型，具体的实现类 |
| `Lifetime` | `ObjectLifetime` | 生命周期 |
| `Instance` | `Object?` | 服务实例，仅单例模式有效 |
| `Factory` | `Func<IServiceProvider, Object>?` | 对象工厂，用于创建服务实例的委托 |

## API 参考

### 全局访问

#### Current

```csharp
public static IObjectContainer Current { get; set; }
```

全局默认容器实例，应用启动时自动创建。

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

全局默认服务提供者，由 `Current` 容器自动构建。

**示例**：
```csharp
// 直接通过全局提供者解析服务
var service = ObjectContainer.Provider.GetService<IMyService>();
```

#### SetInnerProvider

```csharp
// 设置内部服务提供者（用于 UseXxx 阶段）
public static void SetInnerProvider(IServiceProvider innerServiceProvider)

// 设置内部服务提供者工厂（用于 AddXxx 阶段延迟绑定）
public static void SetInnerProvider(Func<IServiceProvider> innerServiceProviderFactory)
```

用于与 ASP.NET Core 等框架的 DI 容器集成，实现链式查找。当 ObjectContainer 中找不到服务时，会自动从内部提供者中查找。

**示例**：
```csharp
// 在 Startup.Configure 中设置
public void Configure(IApplicationBuilder app)
{
    ObjectContainer.SetInnerProvider(app.ApplicationServices);
}

// 或在 Program.cs 中使用工厂延迟绑定
ObjectContainer.SetInnerProvider(() => app.Services);
```

### 服务注册

#### AddSingleton（单例）

```csharp
// 注册类型映射
IObjectContainer AddSingleton<TService, TImplementation>()
IObjectContainer AddSingleton(Type serviceType, Type implementationType)

// 注册实例
IObjectContainer AddSingleton<TService>(TService? instance = null)
IObjectContainer AddSingleton(Type serviceType, Object? instance)

// 注册工厂
IObjectContainer AddSingleton<TService>(Func<IServiceProvider, TService> factory)
IObjectContainer AddSingleton(Type serviceType, Func<IServiceProvider, Object> factory)
```

注册单例服务，整个应用程序生命周期只有一个实例。

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

// 注册自身类型（无需传入实例，内部自动实例化）
ioc.AddSingleton<MyService>();
```

#### AddTransient（瞬态）

```csharp
IObjectContainer AddTransient<TService, TImplementation>()
IObjectContainer AddTransient<TService>()
IObjectContainer AddTransient(Type serviceType, Type implementationType)
IObjectContainer AddTransient(Type serviceType, Func<IServiceProvider, Object> factory)
```

注册瞬态服务，每次请求都创建新实例。

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

注册作用域服务，同一作用域内共享实例。

**示例**：
```csharp
// 同一作用域共享
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
// 单例
IObjectContainer TryAddSingleton<TService, TImplementation>()
IObjectContainer TryAddSingleton<TService>(TService? instance = null)

// 作用域
IObjectContainer TryAddScoped<TService, TImplementation>()
IObjectContainer TryAddScoped<TService>(TService? instance = null)

// 瞬态
IObjectContainer TryAddTransient<TService, TImplementation>()
IObjectContainer TryAddTransient<TService>(TService? instance = null)
```

尝试添加服务，如果已存在相同服务类型则不添加，返回 `false`。

**示例**：
```csharp
// 仅在未注册时添加
ioc.TryAddSingleton<ILogger, ConsoleLogger>();

// 第二次添加不会生效
ioc.TryAddSingleton<ILogger, FileLogger>();  // 被忽略

// 用 Add 则会添加（最后注册的优先）
ioc.AddSingleton<ILogger, FileLogger>();     // 会添加
```

### 服务构建

#### BuildServiceProvider

```csharp
public static IServiceProvider BuildServiceProvider(this IObjectContainer container)
public static IServiceProvider BuildServiceProvider(this IObjectContainer container, IServiceProvider? innerServiceProvider)
```

从对象容器创建服务提供者。

**示例**：
```csharp
var ioc = ObjectContainer.Current;
ioc.AddSingleton<IMyService, MyService>();

// 基本构建
var provider = ioc.BuildServiceProvider();

// 带内部提供者（链式查找）
var provider2 = ioc.BuildServiceProvider(existingProvider);
```

#### BuildHost

```csharp
public static IHost BuildHost(this IObjectContainer container)
public static IHost BuildHost(this IObjectContainer container, IServiceProvider? innerServiceProvider)
```

从对象容器创建应用主机。

**示例**：
```csharp
var ioc = ObjectContainer.Current;
ioc.AddSingleton<IMyService, MyService>();

var host = ioc.BuildHost();
host.Run();
```

### 服务解析

#### GetService（基本解析）

```csharp
// IServiceProvider 接口方法
Object? GetService(Type serviceType)

// 泛型扩展方法
T? GetService<T>(this IServiceProvider provider)
T? GetService<T>(this IObjectContainer container)
```

获取服务实例，未找到时返回 null。解析时优先查找最后注册的服务。

**示例**：
```csharp
var service = provider.GetService(typeof(IMyService));
var typedService = provider.GetService<IMyService>();

// 直接从容器获取
var containerService = ioc.GetService<IMyService>();
```

#### GetRequiredService（必要解析）

```csharp
Object GetRequiredService(this IServiceProvider provider, Type serviceType)
T GetRequiredService<T>(this IServiceProvider provider)
```

获取必要的服务，未找到时抛出 `InvalidOperationException` 异常。

**示例**：
```csharp
// 如果未注册会抛出异常
var config = provider.GetRequiredService<AppConfig>();
```

#### GetServices（批量解析）

```csharp
IEnumerable<Object> GetServices(this IServiceProvider provider, Type serviceType)
IEnumerable<T> GetServices<T>(this IServiceProvider provider)
```

获取指定类型的所有服务实例（支持同一服务类型的多个注册）。返回顺序为注册的逆序（最后注册的最先返回）。

**示例**：
```csharp
// 注册多个处理器
ioc.AddSingleton<IMessageHandler, EmailHandler>();
ioc.AddSingleton<IMessageHandler, SmsHandler>();
ioc.AddSingleton<IMessageHandler, PushHandler>();

// 获取所有处理器
var handlers = provider.GetServices<IMessageHandler>();
foreach (var handler in handlers)
{
    handler.Handle(message);
}
```

### 作用域支持

#### IServiceScope 接口

范围服务接口，该范围生命周期内，每个服务类型只有一个实例。

```csharp
public interface IServiceScope : IDisposable
{
    IServiceProvider ServiceProvider { get; }
}
```

#### IServiceScopeFactory 接口

范围服务工厂接口。

```csharp
public interface IServiceScopeFactory
{
    IServiceScope CreateScope();
}
```

#### CreateScope

```csharp
IServiceScope? CreateScope(this IServiceProvider provider)
```

创建范围作用域，该作用域内 Scoped 服务共享同一实例。

**示例**：
```csharp
using var scope = provider.CreateScope();
var scopedService = scope.ServiceProvider.GetService<IDbContext>();
// scopedService 在此作用域内是唯一的

// 作用域内再次获取，返回同一实例
var scopedService2 = scope.ServiceProvider.GetService<IDbContext>();
// scopedService == scopedService2
```

#### CreateInstance

```csharp
Object? CreateInstance(this IServiceProvider provider, Type serviceType)
```

创建服务对象，使用服务提供者来填充构造函数参数。可用于创建未注册类型的实例。

**示例**：
```csharp
// 创建未注册类型的实例，自动注入已注册的依赖
var instance = provider.CreateInstance(typeof(MyController));
```

## 生命周期详解

### Singleton（单例）

```csharp
ioc.AddSingleton<IMyService, MyService>();
```

- **创建时机**：首次请求时创建
- **实例数量**：整个应用程序只有一个实例
- **释放时机**：应用程序结束时释放
- **适用场景**：配置、日志、缓存等无状态或全局共享的服务
- **注意事项**：单例服务不应依赖作用域服务

### Transient（瞬态）

```csharp
ioc.AddTransient<IMyService, MyService>();
```

- **创建时机**：每次请求时创建
- **实例数量**：每次请求都是新实例
- **释放时机**：使用完即可被 GC 回收
- **适用场景**：轻量级、无状态的服务
- **注意事项**：频繁创建可能影响性能

### Scoped（作用域）

```csharp
ioc.AddScoped<IMyService, MyService>();
```

- **创建时机**：作用域内首次请求时创建
- **实例数量**：每个作用域一个实例
- **释放时机**：作用域销毁时释放
- **适用场景**：Web 请求、数据库上下文等
- **注意事项**：需要通过 `CreateScope()` 创建作用域

## 构造函数注入

ObjectContainer 支持自动构造函数注入，选择参数最多的可匹配构造函数：

```csharp
public interface ILogger { void Log(String message); }
public class ConsoleLogger : ILogger 
{
    public void Log(String message) => Console.WriteLine(message);
}

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

public class UserService
{
    public IRepository Repository { get; }
    public ILogger Logger { get; }
    
    // 多参数构造函数
    public UserService(IRepository repository, ILogger logger)
    {
        Repository = repository;
        Logger = logger;
    }
}

// 注册服务
var ioc = ObjectContainer.Current;
ioc.AddSingleton<ILogger, ConsoleLogger>();
ioc.AddSingleton<IRepository, UserRepository>();
ioc.AddSingleton<UserService>();

// 解析时自动注入依赖
var provider = ioc.BuildServiceProvider();
var userService = provider.GetService<UserService>();
// userService.Logger 和 userService.Repository 都已自动注入
```

### 构造函数选择规则

1. 获取类型的所有公共实例构造函数
2. 按参数数量降序排列
3. 依次尝试匹配，选择第一个所有参数都能解析的构造函数
4. 基本类型参数（Int32、String、Boolean 等）使用默认值
5. 如果没有可匹配的构造函数，抛出 `InvalidOperationException`

### 支持的默认参数类型

| 类型 | 默认值 |
|------|--------|
| `Boolean` | `false` |
| `Char` | `(Char)0` |
| `SByte`/`Byte` | `0` |
| `Int16`/`UInt16` | `0` |
| `Int32`/`UInt32` | `0` |
| `Int64`/`UInt64` | `0` |
| `Single`/`Double` | `0` |
| `Decimal` | `0` |
| `DateTime` | `DateTime.MinValue` |
| `String` | `null` |

## 与 ASP.NET Core 集成

### 方式一：作为补充容器

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// 使用 ASP.NET Core 的 DI
builder.Services.AddControllers();
builder.Services.AddScoped<IUserService, UserService>();

var app = builder.Build();

// 设置内部提供者，实现链式查找
ObjectContainer.SetInnerProvider(app.Services);

// 现在 ObjectContainer.Provider 可以解析 ASP.NET Core 注册的服务
var userService = ObjectContainer.Provider.GetService<IUserService>();
```

### 方式二：延迟绑定

```csharp
// 在 AddXxx 阶段使用工厂延迟绑定
ObjectContainer.SetInnerProvider(() => app.Services);
```

### 方式三：混合使用

```csharp
// 在 NewLife 容器中注册
ObjectContainer.Current.AddSingleton<ICache, MemoryCache>();

// 在 ASP.NET Core 中也可以访问
builder.Services.AddSingleton(sp => 
    ObjectContainer.Provider.GetService<ICache>()!);
```

## 实战示例

### 1. 控制台应用

```csharp
public class Program
{
    public static void Main()
    {
        var ioc = ObjectContainer.Current;
        
        // 注册服务
        ioc.AddSingleton<ILogger, ConsoleLogger>();
        ioc.AddSingleton<IConfiguration, JsonConfiguration>();
        ioc.AddTransient<IUserService, UserService>();
        
        // 构建并运行
        var provider = ioc.BuildServiceProvider();
        var service = provider.GetRequiredService<IUserService>();
        service.Process();
    }
}
```

### 2. Web 应用

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // 将 NewLife 容器的注册同步到 ASP.NET Core
        var ioc = ObjectContainer.Current;
        ioc.AddSingleton<IMyService, MyService>();
        
        // 复制到 ASP.NET Core 容器
        foreach (var item in ioc.Services)
        {
            // 转换并添加到 services
        }
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
        
        // 注册 Mock 服务
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

## 高级用法

### 服务替换

使用 `Add` 方法可以重复注册同一服务类型，解析时返回最后注册的：

```csharp
ioc.AddSingleton<ILogger, ConsoleLogger>();
ioc.AddSingleton<ILogger, FileLogger>();  // 替换

var logger = provider.GetService<ILogger>(); // 返回 FileLogger
```

### 条件注册

使用 `TryAdd` 实现条件注册：

```csharp
// 仅在未注册时添加默认实现
ioc.TryAddSingleton<ILogger, ConsoleLogger>();

// 用户可以在此之前注册自己的实现
```

### 工厂模式

```csharp
ioc.AddSingleton<IConnectionFactory>(sp => 
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connStr = config["Database:ConnectionString"];
    return new SqlConnectionFactory(connStr);
});
```

### 装饰器模式

```csharp
// 注册基础实现
ioc.AddSingleton<ILogger, ConsoleLogger>();

// 注册装饰器
ioc.AddSingleton<ILogger>(sp =>
{
    var services = sp.GetServices<ILogger>().ToList();
    var inner = services.Count > 1 ? services[1] : services[0];
    return new LoggingDecorator(inner);
});
```

## 最佳实践

### 推荐

1. **优先使用接口注册**：便于测试和替换实现
2. **合理选择生命周期**：单例用于无状态服务，作用域用于有状态但需要共享的服务
3. **使用 TryAdd 防止覆盖**：库代码中使用 TryAdd 允许用户自定义实现
4. **早期注册，延迟解析**：在应用启动时完成所有注册

### 避免

1. **避免服务定位器模式**：不要在业务代码中直接调用 `ObjectContainer.Provider`
2. **避免单例依赖作用域**：单例服务不应依赖作用域服务
3. **避免循环依赖**：A 依赖 B，B 依赖 A 会导致解析失败
4. **避免在热点路径注册**：服务注册应在启动时完成

### 生命周期选择建议

```csharp
// 配置、日志 → 单例
ioc.AddSingleton<AppConfig>();
ioc.AddSingleton<ILogger, FileLogger>();

// 数据库上下文 → 作用域（Web）或瞬态（控制台）
ioc.AddScoped<IDbContext, AppDbContext>();  // Web
ioc.AddTransient<IDbContext, AppDbContext>(); // 控制台

// 业务服务对象 → 瞬态
ioc.AddTransient<IValidator, UserValidator>();
```

## 常见问题

### Q: 解析服务时抛出 "No suitable constructor" 异常

**原因**：构造函数的某个参数类型未注册。

**解决**：
```csharp
// 检查并注册所有依赖
ioc.AddSingleton<IDependency, Dependency>();
```

### Q: Scoped 服务在单例中无法正确工作

**原因**：单例服务的生命周期比作用域长，会持有过期的作用域实例。

**解决**：
```csharp
// 改用工厂模式
ioc.AddSingleton<IServiceFactory>(sp => 
    new ServiceFactory(() => sp.CreateScope()));
```

### Q: 同一接口注册多个实现，只能获取一个

**原因**：`GetService<T>()` 只返回最后注册的实现。

**解决**：
```csharp
// 使用 GetServices 获取所有实现
var services = provider.GetServices<IHandler>();
```

### Q: 如何与 ASP.NET Core DI 共存

**解决**：使用 `SetInnerProvider` 建立链式查找：
```csharp
ObjectContainer.SetInnerProvider(app.Services);
```

## 与 Microsoft.Extensions.DependencyInjection 对比

| 特性 | ObjectContainer | MS DI |
|------|-----------------|-------|
| 依赖大小 | 极小（无依赖） | 需要额外包 |
| 生命周期 | 支持 | 支持 |
| 构造函数注入 | 支持 | 支持 |
| 属性注入 | 不支持 | 不支持 |
| 方法注入 | 不支持 | 不支持 |
| 装饰器模式 | 手动支持 | 支持 |
| 验证 | 无 | 支持 |
| 全局访问 | 内置 | 需要自行实现 |

## 相关链接

- [NewLife.Core 项目主页](https://github.com/NewLifeX/X)
- [在线文档](https://newlifex.com/core/object_container)
- [应用主机 IHost](./应用主机Host.md)
