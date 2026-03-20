---
name: security
description: 使用 NewLife 安全模块进行哈希、AES/SM4 加密、RSA 签名和 JWT 令牌操作
---

# NewLife 安全加密使用指南

## 适用场景

- 数据哈希校验（MD5/SHA/CRC）
- 对称加密（AES/SM4 国密）
- 非对称加密与签名（RSA/ECDSA）
- JWT 令牌生成与验证
- 分布式令牌签名

## 哈希

### MD5

```csharp
// 字符串 MD5（返回 32 位十六进制字符串）
var hash = "hello".MD5();

// 16 位 MD5
var hash16 = "hello".MD5_16();

// 字节数组 MD5
var hashBytes = data.MD5();

// 文件 MD5
var fileHash = new FileInfo("test.bin").MD5();
```

### SHA 系列

```csharp
// 普通 SHA
var sha1 = data.SHA1(null);
var sha256 = data.SHA256();
var sha512 = data.SHA512(null);

// HMAC 变体（传入 key）
var hmac = data.SHA256(keyBytes);
```

### CRC 校验

```csharp
var crc32 = data.Crc();     // UInt32
var crc16 = data.Crc16();   // UInt16
```

## 对称加密（AES）

```csharp
// AES-CBC 加密（默认 PKCS7 填充）
var key = "1234567890123456".GetBytes();  // 16 字节 = AES-128
var encrypted = Aes.Create().Encrypt(data, key);
var decrypted = Aes.Create().Decrypt(encrypted, key);

// 指定模式
var encrypted = Aes.Create().Encrypt(data, key, CipherMode.ECB, PaddingMode.Zeros);
```

## 国密 SM4

```csharp
// SM4 加密（API 与 AES 完全一致）
var key = "1234567890123456".GetBytes();  // 固定 16 字节
var encrypted = SM4.Create().Encrypt(data, key, CipherMode.ECB);
var decrypted = SM4.Create().Decrypt(encrypted, key, CipherMode.ECB);
```

## RSA

### 密钥管理

```csharp
// 生成 RSA 密钥对
var keys = RSAHelper.GenerateKey(2048);
var privateKey = keys[0];  // XML 格式私钥
var publicKey = keys[1];   // XML 格式公钥

// 从 PEM/Base64/XML 自动识别加载
var rsa = RSAHelper.Create(pemOrXmlOrBase64);
```

### 加解密

```csharp
var encrypted = RSAHelper.Encrypt(data, publicKey, fOAEP: true);
var decrypted = RSAHelper.Decrypt(encrypted, privateKey, fOAEP: true);
```

### 签名与验签

```csharp
// SHA256 签名（推荐）
var signature = data.SignSha256(privateKey);
var valid = data.VerifySha256(publicKey, signature);

// SHA512 签名
var signature = data.SignSha512(privateKey);
var valid = data.VerifySha512(publicKey, signature);
```

## JWT 令牌

### 生成 JWT

```csharp
var builder = new JwtBuilder
{
    Algorithm = "HS256",
    Secret = "your-secret-key-at-least-32-bytes-long!!",
    Expire = DateTime.Now.AddHours(2),
};

// 添加声明
builder.Subject = "user123";
builder.Id = Guid.NewGuid().ToString();
builder["role"] = "admin";
builder["name"] = "张三";

var token = builder.Encode(null);
```

### 解码验证 JWT

```csharp
var builder = new JwtBuilder
{
    Algorithm = "HS256",
    Secret = "your-secret-key-at-least-32-bytes-long!!",
};

if (builder.TryDecode(token, out var msg))
{
    var subject = builder.Subject;
    var role = builder["role"] as String;
    var exp = builder.Expire;
}
else
{
    XTrace.WriteLine("JWT 验证失败：{0}", msg);
}
```

### 使用 RSA 签名

```csharp
var builder = new JwtBuilder
{
    Algorithm = "RS256",
    Secret = rsaPrivateKey,  // 签名用私钥
};
var token = builder.Encode(null);

// 验证用公钥
builder.Secret = rsaPublicKey;
var valid = builder.TryDecode(token, out var msg);
```

## 分布式令牌（TokenProvider）

```csharp
var provider = new TokenProvider
{
    Key = "shared-secret-key",
};

// 生成令牌
var token = provider.Encode("user123", DateTime.Now.AddHours(1));

// 验证令牌
if (provider.TryDecode(token, out var name, out var expire))
    XTrace.WriteLine("用户 {0}，过期 {1}", name, expire);
```

## 密码策略

```csharp
// MD5 密码提供者（默认）
IPasswordProvider pp = new MD5PasswordProvider();
var hash = pp.Hash("password");
var valid = pp.Verify("password", hash);

// 盐值密码提供者（更安全）
IPasswordProvider pp = new SaltPasswordProvider();
var hash = pp.Hash("password");
var valid = pp.Verify("password", hash);
```

## 注意事项

- **密钥管理**：禁止硬编码密钥，从配置或密钥管理服务获取
- **ECB 模式**：仅适合单块数据，多块数据必须用 CBC/GCM
- **MD5 不安全**：不适合密码存储，仅用于数据摘要/校验
- **RSA 密钥长度**：新系统至少 2048 位
- **JWT Secret**：HS256 至少 32 字节，过短会降低安全性
- SM4 密钥固定 16 字节（128 位）
- `SymmetricAlgorithm` 必须 Dispose，推荐 `using` 或局部使用
