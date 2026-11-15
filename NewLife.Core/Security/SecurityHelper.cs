using System.Security.Cryptography;
using System.Text;
using NewLife.Collections;
using NewLife.Security;

namespace NewLife;

/// <summary>安全算法</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/security_helper
/// </remarks>
public static class SecurityHelper
{
    #region 哈希
    [ThreadStatic]
    private static MD5? _md5;
    /// <summary>MD5散列</summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static Byte[] MD5(this Byte[] data)
    {
        _md5 ??= System.Security.Cryptography.MD5.Create();

        return _md5.ComputeHash(data);
    }

    /// <summary>MD5散列</summary>
    /// <param name="data"></param>
    /// <param name="encoding">字符串编码，默认UTF8</param>
    /// <returns></returns>
    public static unsafe String MD5(this String data, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;

#if NETCOREAPP || NETSTANDARD2_1
        // 长度由输入决定，交给池；仅 16 字节的散列结果用栈
        using var src = Pool.Rent<Byte>(encoding.GetMaxByteCount(data.Length));
        var len = encoding.GetBytes(data.AsSpan(), src.Span);

        Span<Byte> buf = stackalloc Byte[16];
        _md5 ??= System.Security.Cryptography.MD5.Create();
        _md5.TryComputeHash(src.Span[..len], buf, out len);

        return buf.ToHex();
#else
        var buf = MD5(encoding.GetBytes(data + ""));
        return buf.ToHex();
#endif
    }

    /// <summary>MD5散列</summary>
    /// <param name="data"></param>
    /// <param name="encoding">字符串编码，默认Default</param>
    /// <returns></returns>
    public static String MD5_16(this String data, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;

        var buf = MD5(encoding.GetBytes(data + ""));
        return buf.ToHex(0, 8);
    }

    /// <summary>计算文件的MD5散列</summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public static Byte[] MD5(this FileInfo file)
    {
        _md5 ??= System.Security.Cryptography.MD5.Create();

        using var fs = file.OpenRead();
        return _md5.ComputeHash(fs);
    }

    /// <summary>Crc散列</summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static UInt32 Crc(this Byte[] data) => new Crc32().Update(data).Value;

    /// <summary>Crc16散列</summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static UInt16 Crc16(this Byte[] data) => new Crc16().Update(data).Value;

#if NET6_0_OR_GREATER
    /// <summary>SHA128</summary>
    /// <param name="data"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static Byte[] SHA1(this Byte[] data, Byte[]? key) => key == null ? System.Security.Cryptography.SHA1.HashData(data) : HMACSHA1.HashData(key, data);

    /// <summary>SHA256</summary>
    /// <param name="data"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static Byte[] SHA256(this Byte[] data, Byte[]? key = null) => key == null ? System.Security.Cryptography.SHA256.HashData(data) : HMACSHA256.HashData(key, data);

    /// <summary>SHA384</summary>
    /// <param name="data"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static Byte[] SHA384(this Byte[] data, Byte[]? key) => key == null ? System.Security.Cryptography.SHA384.HashData(data) : HMACSHA384.HashData(key, data);

    /// <summary>SHA512</summary>
    /// <param name="data"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static Byte[] SHA512(this Byte[] data, Byte[]? key) => key == null ? System.Security.Cryptography.SHA512.HashData(data) : HMACSHA512.HashData(key, data);
#else
    /// <summary>SHA128</summary>
    /// <param name="data"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static Byte[] SHA1(this Byte[] data, Byte[]? key)
    {
        using HashAlgorithm alg = key == null ? System.Security.Cryptography.SHA1.Create() : new HMACSHA1(key);
        return alg.ComputeHash(data);
    }

    /// <summary>SHA256</summary>
    /// <param name="data"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static Byte[] SHA256(this Byte[] data, Byte[]? key = null)
    {
        using HashAlgorithm alg = key == null ? System.Security.Cryptography.SHA256.Create() : new HMACSHA256(key);
        return alg.ComputeHash(data);
    }

    /// <summary>SHA384</summary>
    /// <param name="data"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static Byte[] SHA384(this Byte[] data, Byte[]? key)
    {
        using HashAlgorithm alg = key == null ? System.Security.Cryptography.SHA384.Create() : new HMACSHA384(key);
        return alg.ComputeHash(data);
    }

    /// <summary>SHA512</summary>
    /// <param name="data"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static Byte[] SHA512(this Byte[] data, Byte[]? key)
    {
        using HashAlgorithm alg = key == null ? System.Security.Cryptography.SHA512.Create() : new HMACSHA512(key);
        return alg.ComputeHash(data);
    }
#endif

    /// <summary>Murmur128哈希</summary>
    /// <param name="data"></param>
    /// <param name="seed"></param>
    /// <returns></returns>
    public static Byte[] Murmur128(this Byte[] data, UInt32 seed = 0) => new Murmur128(seed).ComputeHash(data);
    #endregion

    #region 同步加密扩展
    /// <summary>对称加密算法扩展</summary>
    /// <remarks>注意：CryptoStream会把 outstream 数据流关闭</remarks>
    /// <param name="sa"></param>
    /// <param name="instream"></param>
    /// <param name="outstream"></param>
    /// <returns></returns>
    public static SymmetricAlgorithm Encrypt(this SymmetricAlgorithm sa, Stream instream, Stream outstream)
    {
        // CryptoStream 不拥有传入的 ICryptoTransform，须单独释放（.NET Framework 上的实现持有 CSP 句柄）
        using var transform = sa.CreateEncryptor();
        using (var stream = new CryptoStream(outstream, transform, CryptoStreamMode.Write))
        {
            instream.CopyTo(stream);
            stream.FlushFinalBlock();
        }

        return sa;
    }

    /// <summary>对称加密算法扩展</summary>
    /// <remarks>CBC填充依赖IV，要求加解密的IV一致，而ECB填充则不需要</remarks>
    /// <param name="sa">算法</param>
    /// <param name="data">数据</param>
    /// <param name="pass">密码</param>
    /// <param name="mode">模式。.Net默认CBC，Java默认ECB</param>
    /// <param name="padding">填充算法。默认PKCS7，等同Java的PKCS5</param>
    /// <returns></returns>
    public static Byte[] Encrypt(this SymmetricAlgorithm sa, Byte[] data, Byte[]? pass = null, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7)
    {
        if (data == null || data.Length <= 0) throw new ArgumentNullException(nameof(data));

        if (pass != null && pass.Length > 0)
        {
            if (sa.LegalKeySizes != null && sa.LegalKeySizes.Length > 0)
                sa.Key = Pad(pass, sa.LegalKeySizes[0]);
            else
                sa.Key = pass;

            // CBC填充依赖IV，要求加解密的IV一致，而ECB填充则不需要
            var iv = new Byte[sa.IV.Length];
            iv.Write(0, pass);
            sa.IV = iv;

            sa.Mode = mode;
            sa.Padding = padding;
        }

        var outstream = new MemoryStream();
        using var transform = sa.CreateEncryptor();
        using var stream = new CryptoStream(outstream, transform, CryptoStreamMode.Write);
        stream.Write(data, 0, data.Length);

        // 数据长度必须是8的倍数
        if (sa.Padding == PaddingMode.None)
        {
            var len = data.Length % 8;
            if (len > 0)
            {
                var buf = new Byte[8 - len];
                stream.Write(buf, 0, buf.Length);
            }
        }

        stream.FlushFinalBlock();

        return outstream.ToArray();
    }

    /// <summary>对称解密算法扩展
    /// <para>注意：CryptoStream会把 instream 数据流关闭</para>
    /// </summary>
    /// <param name="sa"></param>
    /// <param name="instream"></param>
    /// <param name="outstream"></param>
    /// <returns></returns>
    public static SymmetricAlgorithm Decrypt(this SymmetricAlgorithm sa, Stream instream, Stream outstream)
    {
        // CryptoStream 不拥有传入的 ICryptoTransform，须单独释放（.NET Framework 上的实现持有 CSP 句柄）
        using var transform = sa.CreateDecryptor();
        using (var stream = new CryptoStream(instream, transform, CryptoStreamMode.Read))
        {
            stream.CopyTo(outstream);
        }

        return sa;
    }

    /// <summary>对称解密算法扩展</summary>
    /// <remarks>CBC填充依赖IV，要求加解密的IV一致，而ECB填充则不需要</remarks>
    /// <param name="sa">算法</param>
    /// <param name="data">数据</param>
    /// <param name="pass">密码</param>
    /// <param name="mode">模式。.Net默认CBC，Java默认ECB</param>
    /// <param name="padding">填充算法。默认PKCS7，等同Java的PKCS5</param>
    /// <returns></returns>
    public static Byte[] Decrypt(this SymmetricAlgorithm sa, Byte[] data, Byte[]? pass = null, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7)
    {
        if (data == null || data.Length <= 0) throw new ArgumentNullException(nameof(data));

        if (pass != null && pass.Length > 0)
        {
            if (sa.LegalKeySizes != null && sa.LegalKeySizes.Length > 0)
                sa.Key = Pad(pass, sa.LegalKeySizes[0]);
            else
                sa.Key = pass;

            // CBC填充依赖IV，要求加解密的IV一致，而ECB填充则不需要
            var iv = new Byte[sa.IV.Length];
            iv.Write(0, pass);
            sa.IV = iv;

            sa.Mode = mode;
            sa.Padding = padding;
        }

        using var transform = sa.CreateDecryptor();
        using var stream = new CryptoStream(new MemoryStream(data), transform, CryptoStreamMode.Read);
        return stream.ReadBytes(-1);
    }

    /// <summary>安全的对称加密：随机 IV + HMAC-SHA256 完整性校验</summary>
    /// <remarks>
    /// 输出格式：IV(块长) + 密文 + MAC(32 字节)。
    /// 与旧的 Encrypt(data, pass) 不同，同一明文每次加密结果都不同，且密文被改动会被发现。
    /// MAC 密钥取 SHA256(密码)，加密密钥沿用旧的填充规则（取最短合法密钥长度）。
    /// 这里不做口令拉伸（PBKDF2/Argon2 之类），强度取决于调用方给的密钥材料；
    /// 需要抗离线爆破时，先用 PBKDF2 等派生密钥再传入，不要直接传人类口令。
    /// </remarks>
    /// <param name="sa">算法</param>
    /// <param name="data">明文</param>
    /// <param name="pass">密码</param>
    /// <param name="mode">模式，默认 CBC</param>
    /// <param name="padding">填充算法，默认 PKCS7</param>
    /// <returns>IV + 密文 + MAC</returns>
    public static Byte[] EncryptSafe(this SymmetricAlgorithm sa, Byte[] data, Byte[] pass, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7)
    {
        if (data == null || data.Length <= 0) throw new ArgumentNullException(nameof(data));
        if (pass == null || pass.Length <= 0) throw new ArgumentNullException(nameof(pass));

        sa.Key = sa.LegalKeySizes != null && sa.LegalKeySizes.Length > 0 ? Pad(pass, sa.LegalKeySizes[0]) : pass;
        sa.Mode = mode;
        sa.Padding = padding;

        // 每次都用随机 IV，避免相同明文得到相同密文
        var iv = new Byte[sa.IV.Length];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create()) rng.GetBytes(iv);
        sa.IV = iv;

        var cipher = sa.Encrypt(data);

        // Encrypt-then-MAC：先算密文的校验码，解密时先验后解
        var mac = Hmac(pass, iv, cipher);

        var buf = new Byte[iv.Length + cipher.Length + mac.Length];
        buf.Write(0, iv);
        buf.Write(iv.Length, cipher);
        buf.Write(iv.Length + cipher.Length, mac);

        return buf;
    }

    /// <summary>安全的对称解密：先校验 HMAC 再解密</summary>
    /// <remarks>数据格式与密钥派生边界见 <see cref="EncryptSafe(SymmetricAlgorithm, Byte[], Byte[], CipherMode, PaddingMode)"/></remarks>
    /// <param name="sa">算法</param>
    /// <param name="data">IV + 密文 + MAC</param>
    /// <param name="pass">密码</param>
    /// <param name="mode">模式，默认 CBC</param>
    /// <param name="padding">填充算法，默认 PKCS7</param>
    /// <returns>明文</returns>
    /// <exception cref="InvalidDataException">数据长度不足或校验码不匹配（被篡改/密码错误）</exception>
    public static Byte[] DecryptSafe(this SymmetricAlgorithm sa, Byte[] data, Byte[] pass, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7)
    {
        if (data == null || data.Length <= 0) throw new ArgumentNullException(nameof(data));
        if (pass == null || pass.Length <= 0) throw new ArgumentNullException(nameof(pass));

        var ivLength = sa.IV.Length;
        if (data.Length <= ivLength + 32) throw new InvalidDataException(L.T("The encrypted data is too short."));

        var iv = new Byte[ivLength];
        Array.Copy(data, 0, iv, 0, ivLength);

        var macLength = 32;
        var cipherLength = data.Length - ivLength - macLength;
        var cipher = new Byte[cipherLength];
        Array.Copy(data, ivLength, cipher, 0, cipherLength);

        var mac = new Byte[macLength];
        Array.Copy(data, data.Length - macLength, mac, 0, macLength);

        // 先验后解，篡改或密码错误都直接失败，不返回错误明文
        var expect = Hmac(pass, iv, cipher);
        if (!FixedTimeEquals(expect, mac)) throw new InvalidDataException(L.T("The encrypted data failed the integrity check."));

        sa.Key = sa.LegalKeySizes != null && sa.LegalKeySizes.Length > 0 ? Pad(pass, sa.LegalKeySizes[0]) : pass;
        sa.Mode = mode;
        sa.Padding = padding;
        sa.IV = iv;

        return sa.Decrypt(cipher);
    }

    private static Byte[] Hmac(Byte[] pass, Byte[] iv, Byte[] cipher)
    {
        using var mac = new HMACSHA256(pass.SHA256());
        mac.TransformBlock(iv, 0, iv.Length, null, 0);
        mac.TransformFinalBlock(cipher, 0, cipher.Length);

        return mac.Hash!;
    }

    private static Boolean FixedTimeEquals(Byte[] a, Byte[] b)
    {
        if (a.Length != b.Length) return false;

        var diff = 0;
        for (var i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];

        return diff == 0;
    }

    private static Byte[] Pad(Byte[] buf, KeySizes keySize)
    {
        var psize = buf.Length * 8;
        var size = 0;
        for (var i = keySize.MinSize; i <= keySize.MaxSize; i += keySize.SkipSize)
        {
            if (i >= psize)
            {
                size = i / 8;
                break;
            }

            // DES的SkipSize为0
            if (keySize.SkipSize == 0) break;
        }

        // 所有key大小都不合适，取最大值，此时密码过长，需要截断
        if (size == 0) size = keySize.MaxSize / 8;

        if (buf.Length == size) return buf;

        var buf2 = new Byte[size];
        buf2.Write(0, buf);

        return buf2;
    }

    /// <summary>转换数据（内部加解密）</summary>
    /// <param name="transform"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public static Byte[] Transform(this ICryptoTransform transform, Byte[] data)
    {
        // 小数据块
        if (data.Length <= transform.InputBlockSize)
            return transform.TransformFinalBlock(data, 0, data.Length);

        // 逐个数据块转换
        var blocks = data.Length / transform.InputBlockSize;
        var inputCount = blocks * transform.InputBlockSize;
        if (inputCount < data.Length) blocks++;

        var output = new Byte[blocks * transform.OutputBlockSize];
        var count = 0;
        if (inputCount > 0 && transform.CanTransformMultipleBlocks)
            count = transform.TransformBlock(data, 0, inputCount, output, 0);
        else
        {
            var pOutput = 0;
            for (var pInput = 0; pInput < inputCount;)
            {
                count += transform.TransformBlock(data, pInput, transform.InputBlockSize, output, pOutput);
                pInput += transform.InputBlockSize;
                pOutput += transform.OutputBlockSize;
            }
        }

        if (count == data.Length) return output;

        //var outstream = new MemoryStream();
        //outstream.Write(output, 0, count);

        var rs = transform.TransformFinalBlock(data, count, data.Length - count);
        Buffer.BlockCopy(rs, 0, output, count, rs.Length);

        return output;

        //outstream.Write(rs);

        //return outstream.ToArray();
    }
    #endregion

    #region RC4
    /// <summary>RC4对称加密算法</summary>
    /// <param name="data"></param>
    /// <param name="pass"></param>
    /// <returns></returns>
    public static Byte[] RC4(this Byte[] data, Byte[] pass) => NewLife.Security.RC4.Encrypt(data, pass);
    #endregion
}