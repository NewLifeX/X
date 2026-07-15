# HttpClient 增强处理器

## 概述

`NewLife.Http` 命名空间提供了两个 `HttpClientHandler` 增强处理器，分别用于自定义 DNS 解析（`DnsHttpHandler`）和 APM 链路追踪（`HttpTraceHandler`）。它们通过 `DelegatingHandler` 机制嵌入 `HttpClient` 的请求管道。

**命名空间**：`NewLife.Http`

---

## DnsHttpHandler — DNS 解析处理器

实现自定义域名解析与多 IP 轮询的 `HttpClient` 处理器。

### 核心特性

- **自定义 DNS**：通过 `IDnsResolver` 接口实现本地解析
- **多 IP 轮询**：每次请求从解析的多个 IP 中轮询选择
- **Host 头保留**：将原始域名保留在 `Host` 请求头中

### 快速开始

```csharp
using NewLife.Http;
using NewLife.Net;

// 自定义解析器
class MyResolver : IDnsResolver
{
    public IPAddress[]? Resolve(String host)
    {
        if (host == "myapi.internal")
            return [IPAddress.Parse("10.0.0.1"), IPAddress.Parse("10.0.0.2")];
        return null;
    }
}

// 创建 HttpClient
var handler = new DnsHttpHandler(new HttpClientHandler())
{
    Resolver = new MyResolver(),
};
var client = new HttpClient(handler);
var html = await client.GetStringAsync("http://myapi.internal/status");
```

> ⚠️ HTTPS 注意事项：替换为 IP 后可能导致 TLS 证书校验失败，需确保服务器证书允许通过 IP 访问或禁用证书校验。

---

## HttpTraceHandler — APM 追踪处理器

为 `HttpClient` 请求添加链路追踪（APM）能力的处理器。

### 核心特性

- **自动埋点**：每次 HTTP 请求自动创建 `ISpan` 追踪
- **异常过滤**：可配置 `ExceptionFilter` 仅记录指定异常
- **去重保护**：自动检测父级埋点，避免重复追踪

### 快速开始

```csharp
using NewLife.Http;
using NewLife.Log;

// 创建追踪 HttpClient
var tracer = new DefaultTracer { ... };
var handler = new HttpTraceHandler(new HttpClientHandler())
{
    Tracer = tracer,
};
var client = new HttpClient(handler);

// 所有请求自动埋点
var html = await client.GetStringAsync("https://newlifex.com");
```

### 与 ApiHttpClient 配合

`ApiHttpClient` 内置 APM 追踪，`HttpTraceHandler` 适用于直接使用 `HttpClient` 的场景。
