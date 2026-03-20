---
applyTo: "**/Security/**"
---

# 安全加密模块开发指令

适用于 `NewLife.Security` 命名空间下的安全加密开发。

---

## 1. 模块结构

| 分类 | 类型 | 说明 |
|------|------|------|
| 扩展方法 | `SecurityHelper` | 统一入口，哈希/对称加密/RC4 |
| 非对称 | `RSAHelper`、`ECDsaHelper` | RSA/ECDSA 加解密与签名 |
| 国密 | `SM4` | 国密 SM4 对称加密（继承 `SymmetricAlgorithm`） |
| 流加密 | `RC4` | RC4 流加密 |
| 校验 | `Crc16`、`Crc32`、`Murmur128` | 数据校验 |
| 证书 | `Certificate` | X.509 证书操作 |
| 密码 | `IPasswordProvider` | 密码策略接口（MD5/盐值/SCrypt 等） |

---

## 2. 哈希规范

- **MD5**：`data.MD5()` 返回 `Byte[]`，`str.MD5()` 返回 32 位十六进制字符串
- **MD5_16**：`str.MD5_16()` 返回 16 位 MD5
- **SHA 系列**：`data.SHA1(key)`、`data.SHA256(key)`、`data.SHA384(key)`、`data.SHA512(key)`
  - `key` 参数非空时使用 HMAC 变体（`HMACSHA256` 等）
  - `key` 为 `null` 时使用普通哈希
- **Murmur**：`data.Murmur128(seed)` 非加密哈希，适合布隆过滤器等场景

---

## 3. 对称加密规范

所有对称加密通过 `SecurityHelper` 扩展方法操作 `SymmetricAlgorithm`：

```csharp
// AES 加密
var encrypted = Aes.Create().Encrypt(data, key, CipherMode.CBC, PaddingMode.PKCS7);
var decrypted = Aes.Create().Decrypt(encrypted, key, CipherMode.CBC, PaddingMode.PKCS7);

// SM4 国密加密
var encrypted = SM4.Create().Encrypt(data, key, CipherMode.ECB);
```

**关键约定**：
- `pass` 参数为密钥 `Byte[]`，长度必须匹配算法要求（AES: 16/24/32，SM4: 16）
- `pass` 为 `null` 时使用算法实例已设置的 `Key`
- CBC 模式需要 IV，ECB 模式不需要
- 默认 `CipherMode.CBC` + `PaddingMode.PKCS7`

---

## 4. RSA 规范

### 4.1 密钥管理

- `RSAHelper.GenerateKey(keySize)` 返回 `[私钥XML, 公钥XML]`
- `RSAHelper.Create(key)` 自动识别 XML/PEM/Base64 格式密钥
- 长期存储用 PEM 格式，运行时传递用 Base64

### 4.2 加解密与签名

- 加密：`RSAHelper.Encrypt(data, pubKey, fOAEP)`，建议 `fOAEP = true`
- 签名有多种算法：`Sign`（MD5）、`SignSha256`（RS256）、`SignSha384`、`SignSha512`
- **禁止**用 MD5 签名新代码，推荐 SHA256 及以上

---

## 5. 安全编码要求

- ❌ 硬编码密钥或盐值（应从配置/密钥管理服务获取）
- ❌ 使用 ECB 模式加密超过一个块的数据（信息泄漏）
- ❌ MD5 用于密码存储（应使用 `IPasswordProvider` 带盐值策略）
- ❌ 忽略 `SymmetricAlgorithm` 的 `Dispose`（必须释放）
- ✅ 密钥/敏感数据用后清零：`Array.Clear(keyBytes, 0, keyBytes.Length)`
- ✅ 随机数生成使用 `RandomNumberGenerator`，不用 `Random`
