---
applyTo: "**/Remoting/**"
---

# RPC 远程调用模块开发指令

适用于 `NewLife.Remoting` 命名空间下的 RPC 框架开发。

---

## 1. 架构分层

| 层级 | HTTP | TCP/UDP |
|------|------|---------|
| 客户端 | `ApiHttpClient` | `ApiClient` |
| 服务端 | ASP.NET 集成 | `ApiServer` |
| 负载均衡 | `ILoadBalancer` | `ILoadBalancer` |
| 过滤器 | `IHttpFilter` | `IApiHandler` |
| 传输 | HTTP/HTTPS | 自定义二进制（`StandardCodec`） |

大多数场景使用 `ApiHttpClient`（HTTP）。仅在需要高性能双向通信、自定义协议时使用 `ApiClient`/`ApiServer`（TCP/UDP）。

---

## 2. ApiHttpClient 开发规范

### 2.1 构造与初始化

```csharp
// 多地址负载均衡，逗号分隔
var client = new ApiHttpClient("http://api1:8080,http://api2:8080");

// 带权重
var client = new ApiHttpClient("main=3*http://api1,backup=1*http://api2");

// DI 模式（从配置中心获取地址）
var client = new ApiHttpClient(serviceProvider, "ServiceName");
```

### 2.2 请求方法

| 方法 | 说明 |
|------|------|
| `GetAsync<T>` / `Get<T>` | GET 请求，args 序列化为 URL 参数 |
| `PostAsync<T>` / `Post<T>` | POST 请求，args 序列化为 JSON Body |
| `PutAsync<T>` / `PatchAsync<T>` / `DeleteAsync<T>` | 对应 HTTP 方法 |
| `InvokeAsync<T>` | 通用方法，可指定 HttpMethod 和自定义请求头 |

### 2.3 关键属性

- `Token` — Bearer 令牌，自动添加到 Authorization 头
- `Timeout` — 请求超时毫秒（默认 15000）
- `LoadBalanceMode` — Failover / RoundRobin / Race
- `ShieldingTime` — 故障节点屏蔽秒数（默认 60）
- `CodeName` / `DataName` — 响应 JSON 中状态码和数据字段名（用于统一错误处理）
- `Filter` — 请求过滤器（用于签名、加密等）

### 2.4 过滤器模式

```csharp
public class MyFilter : IHttpFilter
{
    public void OnRequest(ApiHttpClient client, HttpRequestMessage request, Object? args) { }
    public void OnResponse(ApiHttpClient client, HttpResponseMessage response, Object? result) { }
    public Boolean OnError(ApiHttpClient client, HttpResponseMessage? response, Exception ex) { return false; }
}
client.Filter = new MyFilter();
```

---

## 3. ApiClient/ApiServer 规范（TCP/UDP RPC）

### 3.1 服务端

```csharp
var server = new ApiServer(8080);
server.Register<MyController>();   // 注册控制器（方法自动映射为 Action）
server.Start();
```

### 3.2 客户端

```csharp
var client = new ApiClient("tcp://127.0.0.1:8080");
client.Open();
var result = await client.InvokeAsync<String>("Hello/Say", new { name = "test" });
```

### 3.3 控制器约定

- 方法名即 Action 名：`public String Say(String name)` → 调用 `Hello/Say`
- 支持异步方法：`public async Task<String> SayAsync(String name)`
- 参数自动从请求体绑定

---

## 4. 负载均衡

| 模式 | 类 | 行为 |
|------|-----|------|
| Failover | `FailoverLoadBalancer` | 优先第一个，失败切换下一个 |
| RoundRobin | `WeightedRoundRobinLoadBalancer` | 加权轮询 |
| Race | `RaceLoadBalancer` | 并发请求多个，取最快响应 |

通过 `client.LoadBalanceMode` 或 `client.LoadBalancer` 设置。

---

## 5. 常见错误

- ❌ 每次请求 `new ApiHttpClient()`（应复用，内部管理 HttpClient 连接池）
- ❌ 未设置 `Timeout`（默认 15 秒，长请求需调大）
- ❌ TCP RPC 未添加编解码器（`ApiClient` 内置 `StandardCodec`，但自定义协议需手动添加）
- ❌ 忽略 `InvokeAsync` 返回值中的错误码（需配合 `CodeName` 做统一异常处理）
