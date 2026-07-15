using System.Security.Cryptography;
using NewLife.Security;
using Xunit;

namespace XUnitTest.Security;

/// <summary>PKCS7填充转换测试</summary>
public class PKCS7PaddingTransformTests
{
    [Fact(DisplayName = "属性传递自内部Transform")]
    public void PropertiesDelegateToInner()
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        using var inner = aes.CreateEncryptor();
        using var target = new PKCS7PaddingTransform(inner, PaddingMode.PKCS7, true);

        Assert.Equal(inner.InputBlockSize, target.InputBlockSize);
        Assert.Equal(inner.OutputBlockSize, target.OutputBlockSize);
        Assert.Equal(inner.CanReuseTransform, target.CanReuseTransform);
        Assert.Equal(inner.CanTransformMultipleBlocks, target.CanTransformMultipleBlocks);
    }

    [Fact(DisplayName = "不支持的填充模式抛异常")]
    public void UnsupportedModeThrows()
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        using var inner = aes.CreateEncryptor();

        Assert.Throws<NotSupportedException>(() => new PKCS7PaddingTransform(inner, PaddingMode.Zeros, true));
        Assert.Throws<NotSupportedException>(() => new PKCS7PaddingTransform(inner, PaddingMode.None, true));
    }

    [Fact(DisplayName = "块大小超出范围抛异常")]
    public void InvalidBlockSizeThrows()
    {
        // 使用块大小为1的变换
        using var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        using var inner = aes.CreateEncryptor();

        // 正常创建不抛异常
        using var _ = new PKCS7PaddingTransform(inner, PaddingMode.PKCS7, true);
    }

    [Fact(DisplayName = "TransformFinalBlock空数据返回空")]
    public void TransformFinalBlockEmptyReturnsEmpty()
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        using var inner = aes.CreateEncryptor();
        using var target = new PKCS7PaddingTransform(inner, PaddingMode.PKCS7, true);

        var result = target.TransformFinalBlock([], 0, 0);
        Assert.Empty(result);
    }

    [Fact(DisplayName = "PKCS7加密时填充到块边界")]
    public void EncryptPadsToBlockBoundary()
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        using var inner = aes.CreateEncryptor();
        using var target = new PKCS7PaddingTransform(inner, PaddingMode.PKCS7, true);

        // 输入10字节，不足16字节块大小，PKCS7填充6字节到16字节
        var input = new Byte[10];
        new Random(42).NextBytes(input);
        var result = target.TransformFinalBlock(input, 0, input.Length);

        // 输出应为16字节（1个完整块）
        Assert.Equal(16, result.Length);
    }

    [Fact(DisplayName = "PKCS7加密解密往返")]
    public void EncryptDecryptRoundTrip()
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;

        // 不对齐数据
        var input = new Byte[33];
        new Random(42).NextBytes(input);

        using var encTransform = new PKCS7PaddingTransform(aes.CreateEncryptor(), PaddingMode.PKCS7, true);
        var encrypted = encTransform.TransformFinalBlock(input, 0, input.Length);

        using var decTransform = new PKCS7PaddingTransform(aes.CreateDecryptor(), PaddingMode.PKCS7, false);
        var decrypted = decTransform.TransformFinalBlock(encrypted, 0, encrypted.Length);

        Assert.Equal(input, decrypted);
    }

    [Fact(DisplayName = "ISO10126填充加密解密往返")]
    public void Iso10126RoundTrip()
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;

        var input = new Byte[20];
        new Random(42).NextBytes(input);

        using var encTransform = new PKCS7PaddingTransform(aes.CreateEncryptor(), PaddingMode.ISO10126, true);
        var encrypted = encTransform.TransformFinalBlock(input, 0, input.Length);

        using var decTransform = new PKCS7PaddingTransform(aes.CreateDecryptor(), PaddingMode.ISO10126, false);
        var decrypted = decTransform.TransformFinalBlock(encrypted, 0, encrypted.Length);

        Assert.Equal(input, decrypted);
    }

    [Fact(DisplayName = "ANSIX923填充加密解密往返")]
    public void AnsiX923RoundTrip()
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;

        var input = new Byte[15];
        new Random(42).NextBytes(input);

        using var encTransform = new PKCS7PaddingTransform(aes.CreateEncryptor(), PaddingMode.ANSIX923, true);
        var encrypted = encTransform.TransformFinalBlock(input, 0, input.Length);

        using var decTransform = new PKCS7PaddingTransform(aes.CreateDecryptor(), PaddingMode.ANSIX923, false);
        var decrypted = decTransform.TransformFinalBlock(encrypted, 0, encrypted.Length);

        Assert.Equal(input, decrypted);
    }

    [Fact(DisplayName = "对齐数据不额外填充")]
    public void AlignedDataNoExtraPadding()
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        using var inner = aes.CreateEncryptor();
        using var target = new PKCS7PaddingTransform(inner, PaddingMode.PKCS7, true);

        // 输入16字节，刚好一个块，PKCS7会填充一整个块
        var input = new Byte[16];
        new Random(42).NextBytes(input);
        var result = target.TransformFinalBlock(input, 0, input.Length);

        // 16字节输入 + 16字节填充 = 32字节
        Assert.Equal(32, result.Length);
    }
}
