# 安全扩展 SecurityHelper

## 概述

`SecurityHelper` 是 NewLife.Core 中的安全算法工具类，提供常用的哈希算法、对称加密、非对称加密等功能的扩展方法。支持 MD5、SHA 系列、CRC、AES、DES、RSA 等主流加密算法。

**命名空间**：`NewLife`  
**文档地址**：https://newlifex.com/core/security_helper

## 核心特性

- **哈希算法**：MD5、SHA1、SHA256、SHA384、SHA512、CRC16、CRC32、Murmur128
- **对称加密**：AES、DES、3DES、RC4、SM4
- **非对称加密**：RSA、DSA
- **高性能**：使用线程静态变量缓存算法实例，避免重复创建
- **易用性**：所有算法都以扩展方法形式提供

## 快速开始

```csharp
using NewLife;

// MD5 哈希
var hash = "password".MD5();           // 32位十六进制字符串
var hash16 = "password".MD5_16();      // 16位十六进制字符串

// SHA256 哈希
var sha = data.SHA256();               // 返回字节数组
var shaHex = data.SHA256().ToHex();    // 转为十六进制字符串

// AES 加密
var encrypted = data.Encrypt(Aes.Create(), key);
var decrypted = encrypted.Decrypt(Aes.Create(), key);

// CRC 校验
var crc32 = data.Crc();
var crc16 = data.Crc16();
```

## API 参考

### 哈希算法

#### MD5

```csharp
public static Byte[] MD5(this Byte[] data)
public static String MD5(this String data, Encoding? encoding = null)
public static String MD5_16(this String data, Encoding? encoding = null)
public static Byte[] MD5(this FileInfo file)
```

计算 MD5 散列值。

**示例**：
```csharp
// 字符串 MD5（32位）
"password".MD5()                 // "5F4DCC3B5AA765D61D8327DEB882CF99"

// 字符串 MD5（16位，取中间8字节）
"password".MD5_16()              // "5AA765D61D8327DE"

// 字节数组 MD5
var data = Encoding.UTF8.GetBytes("hello");
var hash = data.MD5();           // 返回 16 字节数组

// 文件 MD5
var fileHash = "large-file.zip".AsFile().MD5().ToHex();
```

#### SHA 系列

```csharp
public static Byte[] SHA1(this Byte[] data, Byte[]? key)
public static Byte[] SHA256(this Byte[] data, Byte[]? key = null)
public static Byte[] SHA384(this Byte[] data, Byte[]? key)
public static Byte[] SHA512(this Byte[] data, Byte[]? key)
```

计算 SHA 系列散列值，可选 HMAC 密钥。

**示例**：
```csharp
var data = Encoding.UTF8.GetBytes("hello");

// 普通哈希
var sha256 = data.SHA256();              // 32 字节
var sha512 = data.SHA512(null);          // 64 字节

// HMAC 哈希（带密钥）
var key = Encoding.UTF8.GetBytes("secret");
var hmac256 = data.SHA256(key);
var hmac512 = data.SHA512(key);
```

#### CRC 校验

```csharp
public static UInt32 Crc(this Byte[] data)
public static UInt16 Crc16(this Byte[] data)
```

计算 CRC 校验值。

**示例**：
```csharp
var data = new Byte[] { 1, 2, 3, 4, 5 };

var crc32 = data.Crc();          // UInt32 校验值
var crc16 = data.Crc16();        // UInt16 校验值
```

#### Murmur128

```csharp
public static Byte[] Murmur128(this Byte[] data, UInt32 seed = 0)
```

计算 Murmur128 非加密哈希，适用于哈希表等场景，速度比 MD5 快很多。

**示例**：
```csharp
var hash = data.Murmur128();                  // 默认种子
var hashWithSeed = data.Murmur128(12345);     // 指定种子
```

### 对称加密

#### Encrypt / Decrypt

```csharp
public static Byte[] Encrypt(this SymmetricAlgorithm sa, Byte[] data, Byte[]? pass = null, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7)
public static Byte[] Decrypt(this SymmetricAlgorithm sa, Byte[] data, Byte[]? pass = null, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7)
```

对称加密/解密数据。

**参数说明**：
- `pass`：密码（会自动填充到合适的密钥长度）
- `mode`：加密模式（CBC/ECB 等），.NET 默认 CBC，Java 默认 ECB
- `padding`：填充模式，默认 PKCS7（等同 Java 的 PKCS5）

**示例**：
```csharp
var data = Encoding.UTF8.GetBytes("Hello World!");
var key = Encoding.UTF8.GetBytes("my-secret-key-16");

// AES 加密（CBC 模式）
var encrypted = Aes.Create().Encrypt(data, key);

// AES 解密
var decrypted = Aes.Create().Decrypt(encrypted, key);

// ECB 模式（与 Java 兼容）
var encryptedEcb = Aes.Create().Encrypt(data, key, CipherMode.ECB);
var decryptedEcb = Aes.Create().Decrypt(encryptedEcb, key, CipherMode.ECB);

// DES 加密
var desKey = Encoding.UTF8.GetBytes("12345678");
var desEncrypted = DES.Create().Encrypt(data, desKey);

// 3DES 加密
var tripleDesKey = Encoding.UTF8.GetBytes("123456789012345678901234");
var tripleDesEncrypted = TripleDES.Create().Encrypt(data, tripleDesKey);
```

#### 流式加密

```csharp
public static SymmetricAlgorithm Encrypt(this SymmetricAlgorithm sa, Stream instream, Stream outstream)
public static SymmetricAlgorithm Decrypt(this SymmetricAlgorithm sa, Stream instream, Stream outstream)
```

对数据流进行加密/解密，适合处理大文件。

**示例**：
```csharp
using var input = File.OpenRead("large-file.bin");
using var output = File.Create("large-file.enc");

var aes = Aes.Create();
aes.Key = key;
aes.IV = iv;
aes.Encrypt(input, output);
```

#### Transform

```csharp
public static Byte[] Transform(this ICryptoTransform transform, Byte[] data)
```

使用 `ICryptoTransform` 直接转换数据。

**示例**：
```csharp
var aes = Aes.Create();
aes.Key = key;
aes.IV = iv;

using var encryptor = aes.CreateEncryptor();
var encrypted = encryptor.Transform(data);

using var decryptor = aes.CreateDecryptor();
var decrypted = decryptor.Transform(encrypted);
```

#### RC4

```csharp
public static Byte[] RC4(this Byte[] data, Byte[] pass)
```

RC4 流密码加密。RC4 加密和解密使用相同的方法。

**示例**：
```csharp
var data = Encoding.UTF8.GetBytes("Hello");
var key = Encoding.UTF8.GetBytes("secret");

// 加密
var encrypted = data.RC4(key);

// 解密（同样的方法）
var decrypted = encrypted.RC4(key);
```

## 其他安全类

### RSAHelper

RSA 非对称加密辅助类。

```csharp
using NewLife.Security;

// 生成密钥对
var (publicKey, privateKey) = RSAHelper.GenerateKey(2048);

// 加密
var encrypted = RSAHelper.Encrypt(data, publicKey);

// 解密
var decrypted = RSAHelper.Decrypt(encrypted, privateKey);

// 签名
var signature = RSAHelper.Sign(data, privateKey, "SHA256");

// 验签
var isValid = RSAHelper.Verify(data, signature, publicKey, "SHA256");
```

### DSAHelper

DSA 数字签名辅助类。

```csharp
using NewLife.Security;

// 签名
var signature = DSAHelper.Sign(data, privateKey);

// 验签
var isValid = DSAHelper.Verify(data, signature, publicKey);
```

### Rand

随机数生成器。

```csharp
using NewLife.Security;

// 生成随机字节
var bytes = Rand.NextBytes(16);

// 生成随机整数
var num = Rand.Next(1, 100);

// 生成随机字符串
var str = Rand.NextString(16);           // 包含数字和字母
var strWithSpecial = Rand.NextString(16, true);  // 包含特殊字符
```

## 使用场景

### 1. 密码哈希存储

```csharp
public class PasswordHelper
{
    public String HashPassword(String password, String salt)
    {
        // 使用 SHA256 + 盐值
        var data = Encoding.UTF8.GetBytes(password + salt);
        return data.SHA256().ToHex();
    }
    
    public Boolean VerifyPassword(String password, String salt, String hash)
    {
        return HashPassword(password, salt).EqualIgnoreCase(hash);
    }
}
```

### 2. API 签名验证

```csharp
public class ApiSignature
{
    public String Sign(String data, String secret)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var content = Encoding.UTF8.GetBytes(data);
        return content.SHA256(key).ToHex();
    }
    
    public Boolean Verify(String data, String signature, String secret)
    {
        return Sign(data, secret).EqualIgnoreCase(signature);
    }
}
```

### 3. 数据加密传输

```csharp
public class SecureTransport
{
    private readonly Byte[] _key;
    
    public SecureTransport(String password)
    {
        // 使用密码派生密钥
        _key = password.MD5().ToHex().GetBytes()[..16];
    }
    
    public Byte[] Encrypt(Byte[] data)
    {
        return Aes.Create().Encrypt(data, _key);
    }
    
    public Byte[] Decrypt(Byte[] data)
    {
        return Aes.Create().Decrypt(data, _key);
    }
}
```

### 4. 文件完整性校验

```csharp
public class FileVerifier
{
    public String ComputeHash(String filePath)
    {
        return filePath.AsFile().MD5().ToHex();
    }
    
    public Boolean Verify(String filePath, String expectedHash)
    {
        var actualHash = ComputeHash(filePath);
        return actualHash.EqualIgnoreCase(expectedHash);
    }
}
```

## 最佳实践

### 1. 选择合适的算法

```csharp
// 密码哈希：使用 SHA256 或更强的算法
var passwordHash = (password + salt).GetBytes().SHA256().ToHex();

// 数据完整性：MD5 足够快速
var checksum = data.MD5().ToHex();

// 高性能哈希表：使用 Murmur128
var hash = data.Murmur128();
```

### 2. 注意加密模式兼容性

```csharp
// 与 Java 系统交互时使用 ECB 模式
var encrypted = Aes.Create().Encrypt(data, key, CipherMode.ECB);

// 安全性要求高时使用 CBC 模式（默认）
var encrypted = Aes.Create().Encrypt(data, key, CipherMode.CBC);
```

### 3. 密钥管理

```csharp
// 不要硬编码密钥
var key = Environment.GetEnvironmentVariable("ENCRYPTION_KEY")?.ToHex();

// 使用安全的随机数生成密钥
var randomKey = Rand.NextBytes(32);
```

## 算法对比

| 算法 | 输出长度 | 速度 | 安全性 | 用途 |
|------|---------|------|--------|------|
| MD5 | 16字节 | 很快 | 低 | 校验和、非安全哈希 |
| SHA1 | 20字节 | 快 | 中 | 兼容旧系统 |
| SHA256 | 32字节 | 中 | 高 | 通用安全哈希 |
| SHA512 | 64字节 | 较慢 | 很高 | 高安全要求 |
| CRC32 | 4字节 | 极快 | 无 | 数据校验 |
| Murmur128 | 16字节 | 极快 | 无 | 哈希表 |

## 相关链接

- [类型转换 Utility](utility-类型转换Utility.md)
- [数据扩展 IOHelper](io_helper-数据扩展IOHelper.md)
- [Web通用令牌 JwtBuilder](jwt-Web通用令牌JwtBuilder.md)
- [分布式数字签名令牌 TokenProvider](token_provider-分布式数字签名令牌TokenProvider.md)
