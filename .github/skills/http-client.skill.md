---
name: http-client
description: 使用 NewLife ApiHttpClient 进行 HTTP API 调用，支持负载均衡、故障转移和过滤器
---

# NewLife HTTP 客户端使用指南

## 适用场景

- 调用 RESTful API
- 多节点负载均衡和自动故障转移
- 统一 Token 认证
- 请求签名和加密
- 微服务间通信

## 基础用法

### 创建客户端

```csharp
// 单节点
var client = new ApiHttpClient("http://api.example.com");

// 多节点负载均衡（逗号分隔）
var client = new ApiHttpClient("http://api1:8080,http://api2:8080,http://api3:8080");

// 带权重（main 权重 3，backup 权重 1）
var client = new ApiHttpClient("main=3*http://api1:8080,backup=1*http://api2:8080");
```

### 发起请求

```csharp
// GET 请求（args 自动序列化为 URL 参数）
var users = await client.GetAsync<User[]>("api/users", new { page = 1, size = 20 });
var user = client.Get<User>("api/users/1");

// POST 请求（args 自动序列化为 JSON Body）
var result = await client.PostAsync<Int32>("api/users", new { Name = "test", Age = 25 });

// PUT / PATCH / DELETE
await client.PutAsync<Object>("api/users/1", new { Name = "updated" });
await client.PatchAsync<Object>("api/users/1", new { Age = 26 });
await client.DeleteAsync<Boolean>("api/users/1");

// 通用方法（自定义请求头等）
var result = await client.InvokeAsync<User>(HttpMethod.Get, "api/users/1",
    onRequest: req => req.Headers.Add("X-Custom", "value"));
```

## 负载均衡

```csharp
var client = new ApiHttpClient("http://api1:8080,http://api2:8080");

// 故障转移（默认）：优先第一个，失败切换
client.LoadBalanceMode = LoadBalanceMode.Failover;

// 加权轮询
client.LoadBalanceMode = LoadBalanceMode.RoundRobin;

// 竞速：并发请求所有节点，取最快响应
client.LoadBalanceMode = LoadBalanceMode.Race;

// 故障屏蔽时间（秒）
client.ShieldingTime = 60;
```

## Token 认证

```csharp
// 设置 Bearer Token（自动添加到 Authorization 头）
client.Token = "your-jwt-token";

// 动态刷新 Token（配合过滤器）
client.Filter = new TokenFilter(client);
```

## 请求过滤器

```csharp
public class SignFilter : IHttpFilter
{
    public void OnRequest(ApiHttpClient client, HttpRequestMessage request, Object? args)
    {
        // 请求前：添加签名
        var sign = ComputeSign(args);
        request.Headers.Add("X-Sign", sign);
    }

    public void OnResponse(ApiHttpClient client, HttpResponseMessage response, Object? result)
    {
        // 响应后：记录日志
    }

    public Boolean OnError(ApiHttpClient client, HttpResponseMessage? response, Exception ex)
    {
        // 错误处理，返回 true 表示已处理
        return false;
    }
}

client.Filter = new SignFilter();
```

## 统一错误处理

```csharp
// 配置响应格式（当 API 返回 { code: 0, data: {...}, message: "ok" }）
client.CodeName = "code";   // 状态码字段名
client.DataName = "data";   // 数据字段名

// 自动提取 data 字段，code != 0 时抛 ApiException
var user = await client.GetAsync<User>("api/users/1");
```

## DI 集成

```csharp
// 注册
services.AddSingleton(sp =>
{
    var client = new ApiHttpClient(sp, "UserService");  // 从配置中心获取地址
    client.Tracer = sp.GetService<ITracer>();
    client.Log = XTrace.Log;
    return client;
});

// 使用
public class UserClient(ApiHttpClient client)
{
    public Task<User?> GetAsync(Int32 id) => client.GetAsync<User>($"api/users/{id}");
}
```

## 注意事项

- **复用实例**：`ApiHttpClient` 内部管理 `HttpClient` 连接池，禁止每次请求 new
- `Timeout` 默认 15 秒，文件上传等长请求需调大
- 多节点地址变更会自动感知（通过配置中心或手动 `SetServer`）
- `Tracer` 属性可注入链路追踪，自动记录 HTTP 请求 Span
- GET 请求的参数对象会序列化为 URL 查询字符串
- POST 请求的参数对象会序列化为 JSON Body
