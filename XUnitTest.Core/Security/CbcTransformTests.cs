using System.Security.Cryptography;
using NewLife.Security;
using Xunit;

namespace XUnitTest.Security;

/// <summary>CBC块密码模式转换测试</summary>
public class CbcTransformTests
{
    /// <summary>创建一个简单的XOR块密码变换用于测试</summary>
    private static ICryptoTransform CreateIdentityTransform(Int32 blockSize = 16)
    {
        // 使用AES的ECB模式作为底层转换
        using var aes = Aes.Create();
        aes.KeySize = 128;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        aes.GenerateKey();
        return aes.CreateEncryptor();
    }

    [Fact(DisplayName = "构造函数设置属性")]
    public void ConstructorSetsProperties()
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        var iv = new Byte[16];
        using var inner = aes.CreateEncryptor();
        using var cbc = new CbcTransform(inner, iv, true);

        Assert.True(cbc.CanTransformMultipleBlocks);
        Assert.Equal(16, cbc.InputBlockSize);
        Assert.Equal(16, cbc.OutputBlockSize);
    }

    [Fact(DisplayName = "IV长度不匹配抛异常")]
    public void IVLengthMismatchThrows()
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        using var inner = aes.CreateEncryptor();

        Assert.Throws<CryptographicException>(() => new CbcTransform(inner, new Byte[8], true));
    }

    [Fact(DisplayName = "null的IV抛异常")]
    public void NullIVThrows()
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        using var inner = aes.CreateEncryptor();

        Assert.Throws<CryptographicException>(() => new CbcTransform(inner, null, true));
    }

    [Fact(DisplayName = "加密再解密还原数据")]
    public void EncryptThenDecryptRoundTrip()
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        var iv = new Byte[16];
        new Random(42).NextBytes(iv);

        // 加密
        var plaintext = new Byte[32]; // 2个块
        new Random(100).NextBytes(plaintext);

        using var encryptor = new CbcTransform(aes.CreateEncryptor(), iv, true);
        var ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);

        // 解密
        using var decryptor = new CbcTransform(aes.CreateDecryptor(), iv, false);
        var decrypted = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);

        Assert.Equal(plaintext, decrypted);
    }

    [Fact(DisplayName = "TransformBlock处理多块数据")]
    public void TransformBlockMultipleBlocks()
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        var iv = new Byte[16];

        var plaintext = new Byte[48]; // 3个块
        new Random(200).NextBytes(plaintext);

        using var enc = new CbcTransform(aes.CreateEncryptor(), iv, true);
        var output = new Byte[48];
        var count = enc.TransformBlock(plaintext, 0, 48, output, 0);

        Assert.Equal(48, count);
        Assert.NotEqual(plaintext, output);
    }

    [Fact(DisplayName = "TransformBlock长度不对齐抛异常")]
    public void TransformBlockNonAlignedThrows()
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        var iv = new Byte[16];

        using var enc = new CbcTransform(aes.CreateEncryptor(), iv, true);
        var output = new Byte[32];

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            enc.TransformBlock(new Byte[17], 0, 17, output, 0));
    }

    [Fact(DisplayName = "TransformFinalBlock空输入返回空")]
    public void TransformFinalBlockEmptyInput()
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        var iv = new Byte[16];

        using var enc = new CbcTransform(aes.CreateEncryptor(), iv, true);
        var result = enc.TransformFinalBlock([], 0, 0);

        Assert.Empty(result);
    }

    [Fact(DisplayName = "单块加密解密")]
    public void SingleBlockRoundTrip()
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        var iv = new Byte[16];

        var plaintext = new Byte[16];
        new Random(300).NextBytes(plaintext);

        using var enc = new CbcTransform(aes.CreateEncryptor(), iv, true);
        var ciphertext = enc.TransformFinalBlock(plaintext, 0, 16);

        using var dec = new CbcTransform(aes.CreateDecryptor(), iv, false);
        var decrypted = dec.TransformFinalBlock(ciphertext, 0, 16);

        Assert.Equal(plaintext, decrypted);
    }
}
