# 分布式令牌TokenProvider

## 概述

`TokenProvider` 是 NewLife.Core 提供的轻量级分布式身份令牌方案，采用 **DSA 非对称签名**（由 `DSAHelper` 封装），无需共享密钥即可在多节点间验证令牌。签名节点持有私钥（`.prvkey`），验证节点只需持有对应公钥（`.pubkey`），适合微服务内部 API 鉴权和设备注册令牌场景。

**命名空间**：`NewLife.Security`  
**文档地址**：/core/token_provider

## 核心特性

- **DSA 非对称签名**：签发方持有私钥，验证方仅需公钥，降低密钥泄露风险
- **自动生成密钥对**：首次调用 `ReadKey()` 时自动生成并保存 `.prvkey` / `.pubkey` 文件
- **零外部依赖**：基于 `DSAHelper`（NewLife 内置），无需第三方 JWT 库
- **轻量载荷**：令牌内含用户标识和过期时间，无额外 claims，体积极小
- **多节点共享公钥**：仅需分发公钥文件到各验证节点，即可完成分布式验证

## 快速开始

### 签发令牌（服务端）

```csharp
using NewLife.Security;

// 服务端使用私钥签发
var provider = new TokenProvider();
provider.ReadKey("token.prvkey", true);  // true = 私钥模式

// 签发有效期 24 小时的令牌
var token = provider.Encode("user001", TimeSpan.FromHours(24));
Console.WriteLine($"令牌: {token}");
```

### 验证令牌（验证节点）

```csharp
// 验证节点只需公钥
var verifier = new TokenProvider();
verifier.ReadKey("token.pubkey", false);  // false = 公钥模式

if (verifier.TryDecode(token, out var user, out var expire))
{
    Console.WriteLine($"用户: {user}，过期: {expire:yyyy-MM-dd HH:mm:ss}");
}
else
{
    Console.WriteLine("令牌无效或已过期");
}
```

## API 参考

### 属性

```csharp
/// <summary>密钥内容（DER格式Base64编码）。私钥或公钥，取决于读取方式</summary>
public String? Key { get; set; }
```

### 方法

#### ReadKey - 读取或生成密钥

```csharp
/// <summary>读取密钥文件，文件不存在时自动生成密钥对</summary>
/// <param name="keyFile">密钥文件路径（.prvkey 或 .pubkey）</param>
/// <param name="isPrivate">true=私钥模式（签发），false=公钥模式（验证）</param>
public void ReadKey(String keyFile, Boolean isPrivate)
```

**行为**：
1. 若 `keyFile` 存在，直接读取写入 `Key`
2. 若不存在且 `isPrivate=true`，调用 `DSAHelper.GenerateKey()` 生成密钥对，将私钥写入 `keyFile`，将对应公钥写入 `keyFile` 追加 `.pubkey` （即同目录下生成 `xxx.pubkey`）
3. 若不存在且 `isPrivate=false`，尝试从 `keyFile.Replace(".pubkey","").prvkey` 中提取公钥；仍不存在则正常报错

#### Encode - 签发令牌

```csharp
/// <summary>签发令牌</summary>
/// <param name="user">用户标识（业务 ID 或用户名）</param>
/// <param name="expire">有效期</param>
/// <returns>令牌字符串（格式：base64url(data).base64url(signature)）</returns>
public String Encode(String user, TimeSpan expire)
```

令牌格式：
```
base64url(user + "," + unixTimestamp)  .  base64url(DSA签名)
```

其中 `unixTimestamp` 是过期绝对时间（秒级 Unix 时间戳）。

#### TryDecode - 验证令牌

```csharp
/// <summary>验证令牌并提取负载</summary>
/// <param name="token">令牌字符串</param>
/// <param name="user">输出：用户标识</param>
/// <param name="expire">输出：过期时间</param>
/// <returns>true=令牌有效且未过期</returns>
public Boolean TryDecode(String token, out String? user, out DateTime expire)
```

**验证步骤**：
1. 按 `.` 分割令牌，取数据段和签名段
2. Base64url 解码数据段和签名段
3. 使用公钥验证 DSA 签名
4. 解析 `user,unixTimestamp`，检查过期时间

## 部署模式

### 单机模式（私钥本地签发 + 验证）

```
  应用进程
    Token签发（private key）
    Token验证（public key）

  文件：token.prvkey + token.pubkey（同一目录）
```

### 网关集中鉴权模式

```
  [签发服务]   持有 token.prvkey
        签发令牌
       
  [客户端App]  携带令牌  [微服务A, B, C, D]
                                  均持有 token.pubkey
                                  本地验证，无需回调签发服务
```

### 多环境密钥隔离

```csharp
// 按环境使用不同密钥文件
var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "production";

var provider  = new TokenProvider();
provider.ReadKey($"token-{env}.prvkey", true);
```

## 使用场景

### 场景一：微服务内部调用鉴权

```csharp
// gateway 启动时初始化签发者
var issuer = new TokenProvider();
issuer.ReadKey("gateway.prvkey", true);

// 登录时签发
app.MapPost("/login", (LoginRequest req) =>
{
    if (!AuthUser(req)) return Results.Unauthorized();
    var token = issuer.Encode(req.Username, TimeSpan.FromHours(8));
    return Results.Ok(new { token });
});

// 下游服务验证（只需公钥）
var verifier = new TokenProvider();
verifier.ReadKey("gateway.pubkey", false);

app.Use(async (ctx, next) =>
{
    var token = ctx.Request.Headers["X-Token"];
    if (!verifier.TryDecode(token, out var user, out _))
    {
        ctx.Response.StatusCode = 401;
        return;
    }
    ctx.Items["user"] = user;
    await next();
});
```

### 场景二：IoT 设备激活码

```csharp
// 控制台工具生成设备激活码（私钥在内部）
var provider = new TokenProvider();
provider.ReadKey("activation.prvkey", true);

var deviceId = "DEV-20250701-001";
var code     = provider.Encode(deviceId, TimeSpan.FromDays(365));

// 设备端验证（公钥内嵌在固件中）
var verifier = new TokenProvider { Key = EMBEDDED_PUBLIC_KEY };
if (verifier.TryDecode(code, out var id, out var expire) && id == deviceId)
    Console.WriteLine("激活成功");
```

## 注意事项

- **私钥严格保护**：`.prvkey` 文件不要提交到版本控制，应通过密钥管理系统（Vault/KMS）分发。
- **令牌不可撤销**：令牌内无会话 ID，无法在过期前主动吊销，需通过短有效期 + 刷新令牌缓解。
- **不含角色信息**：载荷仅含 `user` 和过期时间，权限检查需在业务层自行实现。
- **仅适合内部服务**：对外 API 建议使用标准 JWT（OIDC），`TokenProvider` 面向内部高性能场景。
- **时钟偏差**：签发和验证方服务器时钟应保持在 30 秒内，可在有效期内预留缓冲。
