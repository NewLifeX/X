using System.Security.Cryptography;
using System.Text;
using NewLife;
using Xunit;

namespace XUnitTest.Security;

public class SecurityHelperTests
{
    [Fact]
    public void MD5_Test()
    {
        // 先跑一次预热，避免影响性能测试
        var data = "NewLife";
        var result = "AE9F738635302667D776DE99B0A798AF";
        var rs = result.ToHex();

        var md5 = data.MD5();
        Assert.Equal("AE9F738635302667D776DE99B0A798AF", md5);

        var buf = data.GetBytes();
        buf = buf.MD5();
        Assert.Equal("AE9F738635302667D776DE99B0A798AF".ToHex(), buf);

        var md5_16 = data.MD5_16();
        Assert.Equal("AE9F738635302667", md5_16);
    }

    [Fact]
    public void DES_Test()
    {
        var text = "111111";
        var key = "16621235";

        var des = DES.Create();
        var text2 = des.Encrypt(text.GetBytes(), key.GetBytes(), CipherMode.ECB).ToBase64();
        Assert.Equal("kgAdQRZ6w20=", text2);

        var des2 = DES.Create();
        var text3 = des2.Decrypt(text2.ToBase64(), key.GetBytes(), CipherMode.ECB).ToStr();
        Assert.Equal(text, text3);
    }

    [Fact]
    public void AES_CBC_Test()
    {
        var buf = "123456".ToHex();
        var key = "86CAD727DEB54263B73960AA79C5D9B7".ToHex();
        var data = "";

        // CBC加密解密
        {
            var aes = Aes.Create();
            data = aes.Encrypt(buf, key, CipherMode.CBC, PaddingMode.PKCS7).ToHex();

            Assert.Equal("50A7CF869354EC317327671B34543AD8", data);
        }
        {
            var aes = Aes.Create();
            data = aes.Decrypt(data.ToHex(), key, CipherMode.CBC, PaddingMode.PKCS7).ToHex();

            Assert.Equal("123456", data);
        }
    }

    [Fact]
    public void SHA1_Test()
    {
        var data = "NewLife".GetBytes();

        var rs1 = data.SHA1(null);
        var expected1 = System.Security.Cryptography.SHA1.HashData(data);
        Assert.Equal(expected1, rs1);

        var key = "key".GetBytes();
        var rs2 = data.SHA1(key);
        using var hmac = new HMACSHA1(key);
        var expected2 = hmac.ComputeHash(data);
        Assert.Equal(expected2, rs2);
    }

    [Fact]
    public void SHA256_Test()
    {
        var data = "NewLife".GetBytes();

        var rs1 = data.SHA256();
        var expected1 = System.Security.Cryptography.SHA256.HashData(data);
        Assert.Equal(expected1, rs1);

        var key = "key".GetBytes();
        var rs2 = data.SHA256(key);
        using var hmac = new HMACSHA256(key);
        var expected2 = hmac.ComputeHash(data);
        Assert.Equal(expected2, rs2);
    }

    [Fact]
    public void SHA384_Test()
    {
        var data = "NewLife".GetBytes();

        var rs1 = data.SHA384(null);
        var expected1 = System.Security.Cryptography.SHA384.HashData(data);
        Assert.Equal(expected1, rs1);

        var key = "key".GetBytes();
        var rs2 = data.SHA384(key);
        using var hmac = new HMACSHA384(key);
        var expected2 = hmac.ComputeHash(data);
        Assert.Equal(expected2, rs2);
    }

    [Fact]
    public void SHA512_Test()
    {
        var data = "NewLife".GetBytes();

        var rs1 = data.SHA512(null);
        var expected1 = System.Security.Cryptography.SHA512.HashData(data);
        Assert.Equal(expected1, rs1);

        var key = "key".GetBytes();
        var rs2 = data.SHA512(key);
        using var hmac = new HMACSHA512(key);
        var expected2 = hmac.ComputeHash(data);
        Assert.Equal(expected2, rs2);
    }

    [Fact]
    public void AES_CBC_Test2()
    {
        var buf = "123456".ToHex();
        var key = "86CAD727DEB54263B73960AA79C5D9B7".ToHex();
        var data = "";

        // 直接CBC加密解密
        {
            // CBC需要两边一致的IV
            var aes = Aes.Create();
            aes.Key = key;
            aes.IV = key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            var transform = aes.CreateEncryptor();
            data = transform.TransformFinalBlock(buf, 0, buf.Length).ToHex();

            Assert.Equal("50A7CF869354EC317327671B34543AD8", data);
        }
        {
            buf = data.ToHex();
            var aes = Aes.Create();
            aes.Key = key;
            aes.IV = key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            var transform = aes.CreateDecryptor();
            data = transform.TransformFinalBlock(buf, 0, buf.Length).ToHex();

            Assert.Equal("123456", data);
        }
    }

    [Fact(Skip = "仅开发使用")]
    public void AES_CBC_Test3()
    {
        var plain = "1234567890123456789012345678901234567890123456789012345678901234567890".GetBytes();
        var key = "86CAD727DEB54263B73960AA79C5D9B7".ToHex();
        var cipher = "EOo1EmuK4dJLS4Sb8tD73G68lqLr2yrwq3HtmNKLGnNduAH7yeybrmaEJWAiyKQ+yYWgAKAJ9YSLjfhRURHQT9oBNa101DeRWENSywO4XRA=";

        // 直接ECB加密解密
        {
            var aes = Aes.Create();
            var data = aes.Encrypt(plain, key, CipherMode.CBC, PaddingMode.PKCS7);

            Assert.Equal(cipher, data.ToBase64());
        }
        {
            var aes = Aes.Create();
            var data = aes.Decrypt(cipher.ToBase64(), key, CipherMode.CBC, PaddingMode.PKCS7);

            Assert.Equal(plain.ToStr(), data.ToStr());
        }

        // 内部加密解密
        {
            var aes = Aes.Create();
            aes.Key = key;
            aes.IV = key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            var data = aes.CreateEncryptor().Transform(plain);

            Assert.Equal(cipher, data.ToBase64());
        }
        {
            var aes = Aes.Create();
            aes.Key = key;
            aes.IV = key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            var data = aes.CreateDecryptor().Transform(cipher.ToBase64());

            Assert.Equal(plain.ToStr(), data.ToStr());
        }
    }

    [Fact]
    public void AES_ECB_Test()
    {
        var buf = "123456".ToHex();
        var key = "86CAD727DEB54263B73960AA79C5D9B7".ToHex();
        var data = "";

        // ECB加密解密
        {
            var aes = Aes.Create();
            data = aes.Encrypt(buf, key, CipherMode.ECB, PaddingMode.PKCS7).ToHex();

            Assert.Equal("C0041698AD73EADC75FB886CEF6385A5", data);
        }
        {
            var aes = Aes.Create();
            data = aes.Decrypt(data.ToHex(), key, CipherMode.ECB, PaddingMode.PKCS7).ToHex();

            Assert.Equal("123456", data);
        }
    }

    [Fact]
    public void AES_ECB_Test2()
    {
        var buf = "123456".ToHex();
        var key = "86CAD727DEB54263B73960AA79C5D9B7".ToHex();
        var data = "";

        // 直接ECB加密解密
        {
            var aes = Aes.Create();
            aes.Key = key;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;
            var transform = aes.CreateEncryptor();
            data = transform.TransformFinalBlock(buf, 0, buf.Length).ToHex();

            Assert.Equal("C0041698AD73EADC75FB886CEF6385A5", data);
        }
        {
            buf = data.ToHex();
            var aes = Aes.Create();
            aes.Key = key;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;
            var transform = aes.CreateDecryptor();
            //data = transform.TransformFinalBlock(buf, 0, buf.Length).ToHex();

            var buf2 = new Byte[buf.Length];
            var rs = transform.TransformBlock(buf, 0, buf.Length, buf2, 0);
            var buf3 = transform.TransformFinalBlock(new Byte[1024], 0, 0);
            data = buf3.ToHex();

            Assert.Equal("123456", data);
        }
        {
            var aes = Aes.Create();
            data = aes.Decrypt(buf, key, CipherMode.ECB, PaddingMode.PKCS7).ToHex();

            Assert.Equal("123456", data);
        }
    }

    [Fact(Skip = "仅开发使用")]
    public void AES_ECB_Test3()
    {
        var plain = "1234567890123456789012345678901234567890123456789012345678901234567890".GetBytes();
        var key = "86CAD727DEB54263B73960AA79C5D9B7".ToHex();
        var cipher = "qzPmHdAnD1wW4ujt6NEYPHy5oIn6NuuosQtW3srs3z0/uzIf5O3o637qjWyWeJqiHgLjkH8SXx1rETj3Nxu+Lclz+qK5qkwYW1OOK13Ip3Y=";

        // 直接ECB加密解密
        {
            var aes = Aes.Create();
            var data = aes.Encrypt(plain, key, CipherMode.ECB, PaddingMode.PKCS7);

            Assert.Equal(cipher, data.ToBase64());
        }
        {
            var aes = Aes.Create();
            var data = aes.Decrypt(cipher.ToBase64(), key, CipherMode.ECB, PaddingMode.PKCS7);

            Assert.Equal(plain.ToStr(), data.ToStr());
        }

        // 内部加密解密
        {
            var aes = Aes.Create();
            aes.Key = key;
            aes.IV = key;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;
            var data = aes.CreateEncryptor().Transform(plain);

            Assert.Equal(cipher, data.ToBase64());
        }
        {
            var aes = Aes.Create();
            aes.Key = key;
            aes.IV = key;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;
            var data = aes.CreateDecryptor().Transform(cipher.ToBase64());

            Assert.Equal(plain.ToStr(), data.ToStr());
        }
    }
}