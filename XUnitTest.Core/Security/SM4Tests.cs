using System.Security.Cryptography;
using System.Text;
using NewLife;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto;
using Xunit;
using System;

namespace XUnitTest.Security;

public class SM4Tests
{
    [Fact]
    public void SM4_ECB_PKCS7_Test()
    {
        var text = "111111";
        var key = "16621235";

        var sm4 = SM4.Create();
        var text2 = sm4.Encrypt(text.GetBytes(), key.GetBytes(), CipherMode.ECB, PaddingMode.PKCS7).ToBase64();
        Assert.Equal("9MWjLfAwrIl5him/2Ecg2Q==", text2);

        var sm42 = SM4.Create();
        var text3 = sm42.Decrypt(text2.ToBase64(), key.GetBytes(), CipherMode.ECB, PaddingMode.PKCS7).ToStr();
        Assert.Equal(text, text3);
    }

    [Fact]
    public void SM4_ECB_Zeros_Test()
    {
        var text = "111111";
        var key = "16621235";

        var sm4 = SM4.Create();
        var text2 = sm4.Encrypt(text.GetBytes(), key.GetBytes(), CipherMode.ECB, PaddingMode.Zeros).ToBase64();
        Assert.Equal("awB2EqG9+mlG4zkIhj9h/Q==", text2);

        var sm42 = SM4.Create();
        var text3 = sm42.Decrypt(text2.ToBase64(), key.GetBytes(), CipherMode.ECB, PaddingMode.Zeros).ToStr();
        Assert.Equal(text, text3);
    }

    [Fact]
    public void SM4_CBC_PKCS7_Test()
    {
        var buf = "123456".ToHex();
        var key = "86CAD727DEB54263B73960AA79C5D9B7".ToHex();
        var data = "";

        // CBC加密解密
        {
            var sm4 = SM4.Create();
            data = sm4.Encrypt(buf, key, CipherMode.CBC, PaddingMode.PKCS7).ToHex();

            Assert.Equal("788ED83B46F7D9541C716D9D927F7C42", data);
        }
        {
            var sm4 = SM4.Create();
            data = sm4.Decrypt(data.ToHex(), key, CipherMode.CBC, PaddingMode.PKCS7).ToHex();

            Assert.Equal("123456", data);
        }
    }

    [Fact]
    public void SM4_CBC_Zeros_Test()
    {
        var buf = "123456".ToHex();
        var key = "86CAD727DEB54263B73960AA79C5D9B7".ToHex();
        var data = "";

        // CBC加密解密
        {
            var sm4 = SM4.Create();
            data = sm4.Encrypt(buf, key, CipherMode.CBC, PaddingMode.Zeros).ToHex();

            Assert.Equal("35920C1C503E74BE5FCB1AD143982CB3", data);
        }
        {
            var sm4 = SM4.Create();
            data = sm4.Decrypt(data.ToHex(), key, CipherMode.CBC, PaddingMode.Zeros).ToHex();

            Assert.Equal("123456", data);
        }
    }

    //[Fact]
    [Fact(Skip = "仅开发使用")]
    public void SM4Transform_Test()
    {
        var plain = "0123456789abcdeffedcba9876543210".ToHex();
        var key = "0123456789abcdeffedcba9876543210".ToHex();
        var cipher = "595298c7c6fd271f0402f804c33d3f66".ToHex();
        var buf = new Byte[16];

        IBlockCipher engine = new SM4Engine();

        engine.Init(true, new KeyParameter(key));

        Array.Copy(plain, 0, buf, 0, buf.Length);

        for (var i = 0; i != 1000000; i++)
        {
            engine.ProcessBlock(buf, 0, buf, 0);
        }

        Assert.Equal(cipher, buf);

        engine.Init(false, new KeyParameter(key));

        for (var i = 0; i != 1000000; i++)
        {
            engine.ProcessBlock(buf, 0, buf, 0);
        }

        Assert.Equal(plain, buf);

        // 底层转换方法
        {
            var sm4 = new SM4Transform(key, null, true);
            buf = plain;
            var tmp = new Byte[16];
            for (var i = 0; i != 1000000; i++)
            {
                var rs = sm4.EncryptData(buf, 0, tmp, 0);
                Assert.Equal(16, rs);
                buf = tmp;
            }
            Assert.Equal(cipher, buf);
        }
        {
            var sm4 = new SM4Transform(key, null, false);
            buf = cipher;
            var tmp = new Byte[16];
            for (var i = 0; i != 1000000; i++)
            {
                var rs = sm4.EncryptData(buf, 0, tmp, 0);
                Assert.Equal(16, rs);
                buf = tmp;
            }
            Assert.Equal(plain, buf);
        }

        // 顶层加解密
        {
            var sm4 = SM4.Create();
            buf = plain;
            for (var i = 0; i != 1000000; i++)
            {
                buf = sm4.Encrypt(buf, key, CipherMode.ECB);
            }
            Assert.Equal(cipher, buf);
        }
        {
            var sm4 = SM4.Create();
            buf = cipher;
            for (var i = 0; i != 1000000; i++)
            {
                buf = sm4.Decrypt(buf, key, CipherMode.ECB);
            }
            Assert.Equal(plain, buf);
        }
    }
}
