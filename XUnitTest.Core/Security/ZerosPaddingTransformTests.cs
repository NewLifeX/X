using System.Security.Cryptography;
using NewLife.Security;
using Xunit;

namespace XUnitTest.Security;

/// <summary>Zero填充转换测试</summary>
public class ZerosPaddingTransformTests
{
    [Fact(DisplayName = "属性传递自内部Transform")]
    public void PropertiesDelegateToInner()
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        using var inner = aes.CreateEncryptor();
        using var zeros = new ZerosPaddingTransform(inner, true);

        Assert.Equal(inner.InputBlockSize, zeros.InputBlockSize);
        Assert.Equal(inner.OutputBlockSize, zeros.OutputBlockSize);
        Assert.Equal(inner.CanReuseTransform, zeros.CanReuseTransform);
        Assert.Equal(inner.CanTransformMultipleBlocks, zeros.CanTransformMultipleBlocks);
    }

    [Fact(DisplayName = "TransformFinalBlock空数据返回空")]
    public void TransformFinalBlockEmptyReturnsEmpty()
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        using var inner = aes.CreateEncryptor();
        using var zeros = new ZerosPaddingTransform(inner, true);

        var result = zeros.TransformFinalBlock([], 0, 0);
        Assert.Empty(result);
    }

    [Fact(DisplayName = "加密时自动补零到块边界")]
    public void EncryptPadsToBlockBoundary()
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        using var inner = aes.CreateEncryptor();
        using var zeros = new ZerosPaddingTransform(inner, true);

        // 输入10字节，不足16字节块大小，应该补零到16字节
        var input = new Byte[10];
        new Random(42).NextBytes(input);
        var result = zeros.TransformFinalBlock(input, 0, input.Length);

        Assert.Equal(16, result.Length); // 应该是一个完整块
    }

    [Fact(DisplayName = "对齐数据不额外填充")]
    public void AlignedDataNoExtraPadding()
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        using var inner = aes.CreateEncryptor();
        using var zeros = new ZerosPaddingTransform(inner, true);

        // 输入16字节，刚好一个块
        var input = new Byte[16];
        new Random(42).NextBytes(input);
        var result = zeros.TransformFinalBlock(input, 0, input.Length);

        Assert.Equal(16, result.Length);
    }

    [Fact(DisplayName = "加密解密往返")]
    public void EncryptDecryptRoundTrip()
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;

        // 对齐数据
        var input = new Byte[32];
        new Random(42).NextBytes(input);

        using var encTransform = new ZerosPaddingTransform(aes.CreateEncryptor(), true);
        var encrypted = encTransform.TransformFinalBlock(input, 0, input.Length);

        using var decTransform = new ZerosPaddingTransform(aes.CreateDecryptor(), false);
        var decrypted = decTransform.TransformFinalBlock(encrypted, 0, encrypted.Length);

        Assert.Equal(input, decrypted);
    }
}
