# ApiHttpClient 使用手册

## 概述

`ApiHttpClient` 是 NewLife.Core 提供的 Http 应用接口客户端，是对多个服务地址的包装。它在底层管理多个 `HttpClient`，提供统一的负载均衡和故障转移能力。

### 核心特性

- **多地址管理**：支持配置多个服务地址，自动进行负载均衡
- **故障转移**：节点不可用时自动切换到备用节点
- **负载均衡**：支持故障转移、加权轮询、竞速调用三种模式
- **令牌鉴权**：支持 Token 和 Authentication 两种鉴权方式
- **响应解析**：支持自定义状态码和数据字段名称，适配不同平台
- **可扩展性**：支持自定义 JsonHost、Filter、事件等

## 快速开始

### 基础用法

```csharp
// 创建客户端
var client = new ApiHttpClient("http://api.example.com");

// GET 请求
var result = await client.GetAsync<UserInfo>("user/info", new { id = 123 });

// POST 请求
var response = await client.PostAsync<ResultModel>("user/create", new { name = "test", age = 18 });

// 同步调用
var data = client.Get<String>("api/data");
```

### 多地址配置

```csharp
// 逗号分隔多个地址
var client = new ApiHttpClient("http://api1.example.com,http://api2.example.com,http://api3.example.com");

// 或者手动添加
var client = new ApiHttpClient();
client.Add("primary", "http://api1.example.com");
client.Add("backup", "http://api2.example.com");
```

## 负载均衡

### 三种负载均衡模式

| 模式 | 枚举值 | 说明 |
|------|--------|------|
| 故障转移 | `LoadBalanceMode.Failover` | 优先使用主节点，失败时自动切换到备用节点，过一段时间自动切回 |
| 加权轮询 | `LoadBalanceMode.RoundRobin` | 按权重分配请求到多个节点，自动屏蔽不可用节点 |
| 竞速调用 | `LoadBalanceMode.Race` | 并行请求多个节点，取最快响应，取消其它请求 |

### 故障转移模式（默认）

```csharp
var client = new ApiHttpClient("http://primary.example.com,http://backup.example.com")
{
    LoadBalanceMode = LoadBalanceMode.Failover,  // 默认值
    ShieldingTime = 60  // 不可用节点屏蔽60秒
};

// 正常情况使用 primary，primary 不可用时自动切换到 backup
// 60秒后会尝试切回 primary
var result = await client.GetAsync<Object>("api/data");
```

### 加权轮询模式

```csharp
// 格式：name=weight*url
var client = new ApiHttpClient("master=3*http://api1.example.com,slave=7*http://api2.example.com")
{
    LoadBalanceMode = LoadBalanceMode.RoundRobin
};

// master 权重3，slave 权重7
// 10次请求中，master 约3次，slave 约7次
```

### 竞速调用模式

```csharp
var client = new ApiHttpClient("http://api1.example.com,http://api2.example.com,http://api3.example.com")
{
    LoadBalanceMode = LoadBalanceMode.Race
};

// 并行请求所有节点，返回最快的响应
// 适用于对响应时间要求极高的场景
```

## 身份验证

### Token 令牌

```csharp
var client = new ApiHttpClient("http://api.example.com")
{
    Token = "your_access_token"
};

// 请求头自动添加：Authorization: Bearer your_access_token
```

### Authentication 属性

```csharp
var client = new ApiHttpClient("http://api.example.com")
{
    Authentication = new AuthenticationHeaderValue("Bearer", "your_token")
};

// 或者使用 Basic 认证
client.Authentication = new AuthenticationHeaderValue("Basic", 
    Convert.ToBase64String(Encoding.UTF8.GetBytes("user:password")));
```

### 服务节点独立 Token

```csharp
// 在 URL 中指定 Token
var client = new ApiHttpClient();
client.Add("service1", "http://api1.example.com#token=token_for_api1");
client.Add("service2", "http://api2.example.com#token=token_for_api2");
```

> **优先级**：`Token` 属性优先于 `Authentication` 属性。

## 响应解析

### 标准响应格式

默认支持以下响应格式：

```json
{
    "code": 0,
    "message": "success",
    "data": { ... }
}
```

### 自定义字段名称

```csharp
var client = new ApiHttpClient("http://api.example.com")
{
    CodeName = "status",    // 状态码字段名，默认自动识别 code/errcode/status
    DataName = "result"     // 数据字段名，默认 data
};

// 适配响应格式：{"status": 0, "result": {...}}
```

### 支持的状态码字段

- `code`
- `errcode`
- `status`

### 支持的消息字段

- `message`
- `msg`
- `errmsg`
- `error`

## Http 方法

```csharp
var client = new ApiHttpClient("http://api.example.com");

// GET - 参数拼接到 URL
var result = await client.GetAsync<T>("api/users", new { page = 1, size = 10 });

// POST - 参数 JSON 序列化到 Body
var result = await client.PostAsync<T>("api/users", new { name = "test" });

// PUT
var result = await client.PutAsync<T>("api/users/1", new { name = "updated" });

// PATCH
var result = await client.PatchAsync<T>("api/users/1", new { name = "patched" });

// DELETE
var result = await client.DeleteAsync<T>("api/users/1");

// 通用调用
var result = await client.InvokeAsync<T>(HttpMethod.Post, "api/action", args);
```

## 高级配置

### 超时设置

```csharp
var client = new ApiHttpClient("http://api.example.com")
{
    Timeout = 30_000  // 30秒，默认15秒
};
```

### 代理设置

```csharp
var client = new ApiHttpClient("http://api.example.com")
{
    UseProxy = true  // 使用系统代理，默认false
};
```

### SSL证书验证

```csharp
var client = new ApiHttpClient("https://api.example.com")
{
    CertificateValidation = false  // 不验证证书，默认false
};
```

### 自定义 UserAgent

```csharp
var client = new ApiHttpClient("http://api.example.com")
{
    DefaultUserAgent = "MyApp/1.0"
};
```

### 自定义 Json 序列化

```csharp
var client = new ApiHttpClient("http://api.example.com")
{
    JsonHost = new FastJson()  // 自定义 Json 序列化器
};
```

## 事件与过滤器

### OnRequest 事件

```csharp
var client = new ApiHttpClient("http://api.example.com");

client.OnRequest += (sender, e) =>
{
    // 添加自定义请求头
    e.Request.Headers.Add("X-Request-Id", Guid.NewGuid().ToString());
    e.Request.Headers.Add("X-Timestamp", DateTime.Now.Ticks.ToString());
};
```

### OnCreateClient 事件

```csharp
client.OnCreateClient += (sender, e) =>
{
    // 配置 HttpClient
    e.Client.DefaultRequestHeaders.Add("X-App-Version", "1.0.0");
};
```

### Http 过滤器

```csharp
// 使用内置的令牌过滤器
var filter = new TokenHttpFilter
{
    UserName = "app_id",
    Password = "app_secret"
};

var client = new ApiHttpClient("http://api.example.com")
{
    Filter = filter
};

// 过滤器会自动处理令牌的获取和刷新
```

### 自定义过滤器

```csharp
public class MyHttpFilter : IHttpFilter
{
    public Task OnRequest(HttpClient client, HttpRequestMessage request, Object? state, CancellationToken cancellationToken)
    {
        // 请求前处理
        request.Headers.Add("X-Custom", "value");
        return Task.CompletedTask;
    }

    public Task OnResponse(HttpClient client, HttpResponseMessage response, Object? state, CancellationToken cancellationToken)
    {
        // 响应后处理
        return Task.CompletedTask;
    }

    public Task OnError(HttpClient client, Exception ex, Object? state, CancellationToken cancellationToken)
    {
        // 错误处理
        return Task.CompletedTask;
    }
}
```

## 服务状态监控

### 查看当前服务

```csharp
var client = new ApiHttpClient("http://api1.example.com,http://api2.example.com");

// 当前正在使用的服务
var current = client.Current;
Console.WriteLine($"当前服务：{current?.Name} - {current?.Address}");

// 当前服务名称
Console.WriteLine($"服务源：{client.Source}");
```

### 查看服务列表状态

```csharp
foreach (var svc in client.Services)
{
    Console.WriteLine($"服务：{svc.Name}");
    Console.WriteLine($"  地址：{svc.Address}");
    Console.WriteLine($"  权重：{svc.Weight}");
    Console.WriteLine($"  调用次数：{svc.Times}");
    Console.WriteLine($"  错误次数：{svc.Errors}");
    Console.WriteLine($"  是否可用：{svc.IsAvailable()}");
    Console.WriteLine($"  下次可用时间：{svc.NextTime}");
}
```

## 链路追踪

```csharp
var client = new ApiHttpClient("http://api.example.com")
{
    Tracer = DefaultTracer.Instance,  // 设置链路追踪器
    SlowTrace = 5_000  // 超过5秒记录慢调用日志
};
```

## 依赖注入

### ASP.NET Core 集成

```csharp
// 注册服务
services.AddSingleton<IApiClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var client = new ApiHttpClient(config["ApiServer:Urls"])
    {
        Timeout = config.GetValue<Int32>("ApiServer:Timeout"),
        ServiceProvider = sp
    };
    return client;
});

// 使用配置中心
services.AddSingleton<IApiClient>(sp =>
{
    return new ApiHttpClient(sp, "ApiServerConfig");  // 从配置中心读取
});
```

### IConfigMapping 接口

```csharp
// ApiHttpClient 实现了 IConfigMapping 接口
// 可以通过配置中心动态更新服务地址

var configProvider = services.GetRequiredService<IConfigProvider>();
configProvider.Bind(client, true, "ApiServer");  // 绑定配置节
```

## 文件下载

```csharp
var client = new ApiHttpClient("http://download.example.com");

// 下载文件并校验哈希
await client.DownloadFileAsync(
    requestUri: "files/package.zip",
    fileName: "D:/downloads/package.zip",
    expectedHash: "sha256:abc123...",  // 可选，支持 md5/sha1/sha256/sha512
    cancellationToken: default
);
```

## 异常处理

### ApiException

```csharp
try
{
    var result = await client.GetAsync<Object>("api/data");
}
catch (ApiException ex)
{
    // 业务异常（服务端返回的错误码）
    Console.WriteLine($"错误码：{ex.Code}");
    Console.WriteLine($"错误信息：{ex.Message}");
}
catch (HttpRequestException ex)
{
    // 网络异常
    Console.WriteLine($"网络错误：{ex.Message}");
}
```

## 最佳实践

### 1. 复用客户端实例

```csharp
// ? 推荐：作为单例使用
public class MyService
{
    private static readonly ApiHttpClient _client = new("http://api.example.com");
    
    public Task<T> GetDataAsync<T>() => _client.GetAsync<T>("api/data");
}

// ? 避免：每次请求创建新实例
public async Task<T> GetDataAsync<T>()
{
    using var client = new ApiHttpClient("http://api.example.com");  // 不推荐
    return await client.GetAsync<T>("api/data");
}
```

### 2. 合理设置超时

```csharp
var client = new ApiHttpClient("http://api.example.com")
{
    Timeout = 10_000,  // 根据接口特性设置合理超时
    SlowTrace = 3_000  // 慢调用阈值
};
```

### 3. 配置故障转移

```csharp
var client = new ApiHttpClient("http://primary.example.com,http://backup.example.com")
{
    ShieldingTime = 30,  // 故障节点屏蔽30秒
    LoadBalanceMode = LoadBalanceMode.Failover
};
```

### 4. 使用链路追踪

```csharp
var client = new ApiHttpClient("http://api.example.com")
{
    Tracer = DefaultTracer.Instance,
    Log = XTrace.Log  // 开启日志
};
```

## 完整示例

```csharp
using NewLife.Log;
using NewLife.Remoting;

// 创建客户端
var client = new ApiHttpClient("master=3*http://api1.example.com,slave=7*http://api2.example.com")
{
    Token = "your_access_token",
    Timeout = 15_000,
    ShieldingTime = 60,
    LoadBalanceMode = LoadBalanceMode.RoundRobin,
    CodeName = "code",
    DataName = "data",
    Tracer = DefaultTracer.Instance,
    Log = XTrace.Log
};

// 添加请求拦截
client.OnRequest += (sender, e) =>
{
    e.Request.Headers.Add("X-Request-Id", Guid.NewGuid().ToString());
};

try
{
    // 发起请求
    var users = await client.GetAsync<List<UserInfo>>("api/users", new { page = 1, size = 10 });
    
    foreach (var user in users)
    {
        Console.WriteLine($"用户：{user.Name}");
    }
    
    // 查看当前使用的服务
    Console.WriteLine($"请求服务：{client.Source} - {client.Current?.Address}");
}
catch (ApiException ex)
{
    Console.WriteLine($"业务错误 [{ex.Code}]：{ex.Message}");
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"网络错误：{ex.Message}");
}
```

## 相关类型

| 类型 | 说明 |
|------|------|
| `ApiHttpClient` | Http 应用接口客户端 |
| `ServiceEndpoint` | 服务端点，包含地址、权重、状态等信息 |
| `ILoadBalancer` | 负载均衡器接口 |
| `FailoverLoadBalancer` | 故障转移负载均衡器 |
| `WeightedRoundRobinLoadBalancer` | 加权轮询负载均衡器 |
| `RaceLoadBalancer` | 竞速负载均衡器 |
| `IHttpFilter` | Http 过滤器接口 |
| `TokenHttpFilter` | 令牌过滤器 |
| `ApiException` | Api 业务异常 |

## 版本历史

- **v11.0+**：引入负载均衡模式枚举，支持竞速调用
- **v10.0+**：支持自定义 CodeName/DataName
- **v9.0+**：支持链路追踪
