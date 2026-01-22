# Web通用令牌 JwtBuilder

## 概述

`JwtBuilder` 是 NewLife.Core 中的 JSON Web Token (JWT) 生成和验证工具类。JWT 是一种紧凑、自包含的令牌格式，广泛用于 Web API 认证和授权。JwtBuilder 支持 HS256/HS384/HS512 和 RS256/RS384/RS512 等主流算法。

**命名空间**：`NewLife.Web`  
**文档地址**：https://newlifex.com/core/jwt

## 核心特性

- **多算法支持**：HS256、HS384、HS512、RS256、RS384、RS512
- **标准声明**：支持 iss、sub、aud、exp、nbf、iat、jti 等标准声明
- **自定义数据**：支持在令牌中携带任意自定义数据
- **时间验证**：自动验证过期时间和生效时间
- **可扩展**：支持注册自定义签名算法

## 快速开始

### 生成令牌

```csharp
using NewLife.Web;

var builder = new JwtBuilder
{
    Secret = "your-secret-key-at-least-32-characters",
    Expire = DateTime.Now.AddHours(2),  // 2小时后过期
    Subject = "user123",                // 用户标识
    Issuer = "MyApp"                    // 颁发者
};

// 添加自定义数据
builder["role"] = "admin";
builder["name"] = "张三";

// 生成令牌
var token = builder.Encode(new { });
Console.WriteLine(token);
```

### 验证令牌

```csharp
var builder = new JwtBuilder
{
    Secret = "your-secret-key-at-least-32-characters"
};

if (builder.TryDecode(token, out var message))
{
    Console.WriteLine($"用户: {builder.Subject}");
    Console.WriteLine($"角色: {builder["role"]}");
    Console.WriteLine($"过期时间: {builder.Expire}");
}
else
{
    Console.WriteLine($"验证失败: {message}");
}
```

## API 参考

### 属性

#### 标准声明

```csharp
/// <summary>颁发者 (iss)</summary>
public String? Issuer { get; set; }

/// <summary>主体所有人 (sub)，可存放用户ID</summary>
public String? Subject { get; set; }

/// <summary>受众 (aud)</summary>
public String? Audience { get; set; }

/// <summary>过期时间 (exp)，默认2小时</summary>
public DateTime Expire { get; set; }

/// <summary>生效时间 (nbf)，在此之前无效</summary>
public DateTime NotBefore { get; set; }

/// <summary>颁发时间 (iat)</summary>
public DateTime IssuedAt { get; set; }

/// <summary>令牌标识 (jti)</summary>
public String? Id { get; set; }
```

#### 配置属性

```csharp
/// <summary>算法，默认HS256</summary>
public String Algorithm { get; set; }

/// <summary>令牌类型，默认JWT</summary>
public String? Type { get; set; }

/// <summary>密钥</summary>
public String? Secret { get; set; }

/// <summary>自定义数据项</summary>
public IDictionary<String, Object?> Items { get; }
```

#### 索引器

```csharp
// 获取或设置自定义数据
public Object? this[String key] { get; set; }
```

### Encode - 生成令牌

```csharp
public String Encode(Object payload)
```

将数据编码为 JWT 令牌字符串。

**参数**：
- `payload`：要编码的数据对象

**返回值**：JWT 令牌字符串

**示例**：
```csharp
var builder = new JwtBuilder
{
    Secret = "my-secret-key-32-characters-long",
    Expire = DateTime.Now.AddDays(7),
    Subject = "user_001"
};

// 方式1：使用属性和索引器
builder["permissions"] = new[] { "read", "write" };
var token1 = builder.Encode(new { });

// 方式2：直接传入对象
var token2 = builder.Encode(new
{
    userId = 123,
    userName = "test",
    permissions = new[] { "read", "write" }
});
```

### TryDecode - 验证并解码令牌

```csharp
public Boolean TryDecode(String token, out String? message)
```

验证 JWT 令牌并解码数据。

**参数**：
- `token`：JWT 令牌字符串
- `message`：验证失败时的错误信息

**返回值**：验证是否成功

**验证内容**：
1. JWT 格式是否正确（三段式）
2. 签名是否有效
3. 是否在有效期内
4. 是否已生效

**示例**：
```csharp
var builder = new JwtBuilder
{
    Secret = "my-secret-key-32-characters-long"
};

if (builder.TryDecode(token, out var message))
{
    // 验证成功，读取数据
    var userId = builder.Subject;
    var expire = builder.Expire;
    var permissions = builder["permissions"];
}
else
{
    // 验证失败
    Console.WriteLine($"错误: {message}");
    // 可能的错误:
    // - "JWT格式不正确"
    // - "令牌已过期"
    // - "令牌未生效"
    // - "未设置密钥"
}
```

### Parse - 仅解析不验证

```csharp
public String[]? Parse(String token)
```

仅解析令牌结构，不验证签名。用于需要在验证前读取内容的场景。

**返回值**：三段式数组 [header, payload, signature]，格式错误返回 null

**示例**：
```csharp
var builder = new JwtBuilder();
var parts = builder.Parse(token);

if (parts != null)
{
    // 可以读取算法、过期时间等
    Console.WriteLine($"算法: {builder.Algorithm}");
    Console.WriteLine($"过期: {builder.Expire}");
    
    // 然后设置密钥进行完整验证
    builder.Secret = "...";
    if (builder.TryDecode(token, out _)) { }
}
```

### RegisterAlgorithm - 注册自定义算法

```csharp
public static void RegisterAlgorithm(
    String algorithm, 
    JwtEncodeDelegate encode, 
    JwtDecodeDelegate? decode)
```

注册自定义签名算法。

**示例**：
```csharp
// 注册自定义算法
JwtBuilder.RegisterAlgorithm(
    "ES256",
    (data, secret) => ECDsaHelper.SignSha256(data, secret),
    (data, secret, signature) => ECDsaHelper.VerifySha256(data, secret, signature)
);

// 使用自定义算法
var builder = new JwtBuilder
{
    Algorithm = "ES256",
    Secret = ecdsaPrivateKey
};
var token = builder.Encode(new { });
```

## 支持的算法

| 算法 | 类型 | 密钥要求 | 说明 |
|------|------|---------|------|
| HS256 | HMAC | 对称密钥 | 默认算法，适合大多数场景 |
| HS384 | HMAC | 对称密钥 | 更长的哈希 |
| HS512 | HMAC | 对称密钥 | 最长的哈希 |
| RS256 | RSA | 公私钥对 | 非对称加密，适合分布式 |
| RS384 | RSA | 公私钥对 | 更长的哈希 |
| RS512 | RSA | 公私钥对 | 最长的哈希 |

## 使用场景

### 1. API 认证

```csharp
// 登录接口 - 生成令牌
[HttpPost("login")]
public IActionResult Login(String username, String password)
{
    var user = ValidateUser(username, password);
    if (user == null) return Unauthorized();
    
    var builder = new JwtBuilder
    {
        Secret = Configuration["Jwt:Secret"],
        Expire = DateTime.Now.AddHours(24),
        Subject = user.Id.ToString(),
        Issuer = "MyApi"
    };
    builder["role"] = user.Role;
    builder["name"] = user.Name;
    
    var token = builder.Encode(new { });
    return Ok(new { token });
}

// 验证中间件
public class JwtMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var token = context.Request.Headers["Authorization"]
            .ToString().TrimStart("Bearer ");
        
        if (!token.IsNullOrEmpty())
        {
            var builder = new JwtBuilder
            {
                Secret = _configuration["Jwt:Secret"]
            };
            
            if (builder.TryDecode(token, out _))
            {
                context.Items["UserId"] = builder.Subject;
                context.Items["Role"] = builder["role"];
            }
        }
        
        await _next(context);
    }
}
```

### 2. 刷新令牌

```csharp
public class TokenService
{
    public (String accessToken, String refreshToken) CreateTokenPair(User user)
    {
        // 访问令牌 - 短期有效
        var accessBuilder = new JwtBuilder
        {
            Secret = _secret,
            Expire = DateTime.Now.AddMinutes(15),
            Subject = user.Id.ToString()
        };
        accessBuilder["type"] = "access";
        
        // 刷新令牌 - 长期有效
        var refreshBuilder = new JwtBuilder
        {
            Secret = _secret,
            Expire = DateTime.Now.AddDays(7),
            Subject = user.Id.ToString()
        };
        refreshBuilder["type"] = "refresh";
        
        return (accessBuilder.Encode(new { }), refreshBuilder.Encode(new { }));
    }
    
    public String? RefreshAccessToken(String refreshToken)
    {
        var builder = new JwtBuilder { Secret = _secret };
        
        if (!builder.TryDecode(refreshToken, out _)) return null;
        if (builder["type"]?.ToString() != "refresh") return null;
        
        // 生成新的访问令牌
        var newBuilder = new JwtBuilder
        {
            Secret = _secret,
            Expire = DateTime.Now.AddMinutes(15),
            Subject = builder.Subject
        };
        newBuilder["type"] = "access";
        
        return newBuilder.Encode(new { });
    }
}
```

### 3. RSA 非对称签名

```csharp
// 服务端签名（使用私钥）
var privateKey = File.ReadAllText("private.pem");
var builder = new JwtBuilder
{
    Algorithm = "RS256",
    Secret = privateKey,
    Expire = DateTime.Now.AddHours(1),
    Subject = "user123"
};
var token = builder.Encode(new { });

// 客户端/其他服务验证（使用公钥）
var publicKey = File.ReadAllText("public.pem");
var verifier = new JwtBuilder
{
    Algorithm = "RS256",
    Secret = publicKey
};
if (verifier.TryDecode(token, out var msg))
{
    Console.WriteLine($"验证成功: {verifier.Subject}");
}
```

## 最佳实践

### 1. 安全的密钥管理

```csharp
// 不推荐：硬编码密钥
var builder = new JwtBuilder { Secret = "my-secret" };

// 推荐：从配置或环境变量读取
var builder = new JwtBuilder
{
    Secret = Environment.GetEnvironmentVariable("JWT_SECRET")
        ?? Configuration["Jwt:Secret"]
};
```

### 2. 合理的过期时间

```csharp
// 访问令牌：短期（15分钟-2小时）
Expire = DateTime.Now.AddMinutes(30);

// 刷新令牌：中期（1-7天）
Expire = DateTime.Now.AddDays(7);

// 记住我令牌：长期（30天）
Expire = DateTime.Now.AddDays(30);
```

### 3. 最小化令牌内容

```csharp
// 不推荐：存放大量数据
builder["userProfile"] = new { /* 大对象 */ };

// 推荐：仅存放必要标识
builder.Subject = user.Id.ToString();  // 需要详情时查数据库
builder["role"] = user.Role;           // 常用的授权信息
```

### 4. 验证所有声明

```csharp
if (builder.TryDecode(token, out var message))
{
    // 额外验证颁发者
    if (builder.Issuer != "MyApp")
    {
        // 颁发者不匹配
    }
    
    // 额外验证受众
    if (builder.Audience != "web-client")
    {
        // 受众不匹配
    }
}
```

## JWT 安全注意事项

1. **不要存储敏感数据**：JWT 默认不加密，payload 可被 Base64 解码
2. **使用 HTTPS**：防止令牌被中间人截获
3. **设置合理过期时间**：降低令牌被盗用的风险
4. **使用足够长的密钥**：HS256 至少 32 字节
5. **验证所有声明**：包括 iss、aud、exp 等

## 相关链接

- [分布式数字签名令牌 TokenProvider](token_provider-分布式数字签名令牌TokenProvider.md)
- [安全扩展 SecurityHelper](security_helper-安全扩展SecurityHelper.md)
