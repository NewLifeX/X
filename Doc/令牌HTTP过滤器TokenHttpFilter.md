# 令牌HTTP过滤器TokenHttpFilter

## 概述

`TokenHttpFilter` 是用于 HTTP API 客户端的身份认证过滤器，实现了 `IHttpFilter` 接口。它自动在请求头中注入令牌，并在收到 401/403 响应时自动尝试刷新或重新获取令牌，实现透明的身份认证。

**命名空间**：`NewLife.Http`  
**文档地址**：https://newlifex.com/core/token_http_filter

## 核心特性

- **自动注入**：在 `HttpClient` 请求前自动添加 `Authorization` 头
- **自动续期**：拦截 `401 Unauthorized` 和 `403 Forbidden` 响应，自动刷新令牌
- **安全密钥**：支持 RSA 加密用户密码，保护通信链路中的敏感信息
- **多用户支持**：可配置 `UserName`/`Password` 或自定义 `IToken`

## 快速开始

```csharp
using NewLife.Http;
using NewLife.Remoting;

// 创建令牌过滤器
var filter = new TokenHttpFilter
{
    UserName = "admin",
    Password = "123456",
    // 安全密钥（可选），用于 RSA 加密密码
    SecurityKey = "myKey$myPublicKeyValue",
    // 申请令牌的动作名，默认 OAuth/Token
    Action = "OAuth/Token",
};

// 与 ApiHttpClient 配合使用
var client = new ApiHttpClient("http://localhost:5000");
client.Filter = filter;

// 首次调用会自动获取令牌，后续自动注入
var data = await client.GetAsync<Object>("user/info");
```

## 工作原理

```
请求 ──→ TokenHttpFilter ── 检查令牌 ──→ 有令牌？ ──是──→ 注入 Authorization 头
                  │                                          │
                 否                                          │
                  ↓                                          ↓
              申请新令牌 ←───────────────────────────────── HttpClient
                  │                                          │
                  ↓                                          ↓
              缓存令牌                                   发送请求
                                                              │
                                                    响应 401/403？
                                                              │
                                                         是  ↓
                                                     清空令牌 → 重试
```

## API 参考

### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `UserName` | `String?` | 用户名 |
| `Password` | `String?` | 密码 |
| `SecurityKey` | `String?` | 安全密钥，格式 `keyName$keyValue`，用于 RSA 加密密码 |
| `Action` | `String` | 申请令牌的动作名，默认 `OAuth/Token` |
| `Token` | `IToken?` | 当前令牌信息 |
| `Expire` | `DateTime` | 令牌过期时间 |
| `ErrorCodes` | `IList<Int32>` | 触发令牌清空的状态码，默认 401 和 403 |

## 注意事项

- `TokenHttpFilter` 是线程安全的，内部使用锁定保证并发安全
- 令牌刷新采用"第一次请求刷新"策略，避免多线程同时刷新
- `SecurityKey` 中的密钥名称会传递给服务端，便于多密钥轮换
