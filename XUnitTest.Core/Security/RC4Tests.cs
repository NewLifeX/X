using NewLife.Security;
using Xunit;

namespace XUnitTest.Security;

/// <summary>RC4对称加密算法测试</summary>
public class RC4Tests
{
    [Fact(DisplayName = "加密再解密还原数据")]
    public void EncryptDecryptRoundTrip()
    {
        var data = new Byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        var key = new Byte[] { 0xAB, 0xCD, 0xEF, 0x01 };

        var encrypted = RC4.Encrypt(data, key);
        Assert.NotNull(encrypted);
        Assert.NotEqual(data, encrypted);

        // RC4加密解密使用同一操作
        var decrypted = RC4.Encrypt(encrypted, key);
        Assert.Equal(data, decrypted);
    }

    [Fact(DisplayName = "空数据返回空数组")]
    public void EmptyDataReturnsEmpty()
    {
        var key = new Byte[] { 0x01 };
        var result = RC4.Encrypt([], key);
        Assert.Empty(result);
    }

    [Fact(DisplayName = "null数据返回空数组")]
    public void NullDataReturnsEmpty()
    {
        var key = new Byte[] { 0x01 };
        var result = RC4.Encrypt(null!, key);
        Assert.Empty(result);
    }

    [Fact(DisplayName = "空密钥返回原数据")]
    public void EmptyKeyReturnsOriginal()
    {
        var data = new Byte[] { 1, 2, 3 };
        var result = RC4.Encrypt(data, []);
        Assert.Equal(data, result);
    }

    [Fact(DisplayName = "null密钥返回原数据")]
    public void NullKeyReturnsOriginal()
    {
        var data = new Byte[] { 1, 2, 3 };
        var result = RC4.Encrypt(data, null!);
        Assert.Equal(data, result);
    }

    [Fact(DisplayName = "不同密钥产生不同密文")]
    public void DifferentKeysProduceDifferentCiphertext()
    {
        var data = new Byte[] { 1, 2, 3, 4, 5 };
        var key1 = new Byte[] { 0x01, 0x02 };
        var key2 = new Byte[] { 0x03, 0x04 };

        var enc1 = RC4.Encrypt(data, key1);
        var enc2 = RC4.Encrypt(data, key2);

        Assert.NotEqual(enc1, enc2);
    }

    [Fact(DisplayName = "输出长度等于输入长度")]
    public void OutputLengthEqualsInputLength()
    {
        var data = new Byte[100];
        new Random(42).NextBytes(data);
        var key = new Byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

        var result = RC4.Encrypt(data, key);
        Assert.Equal(data.Length, result.Length);
    }

    [Fact(DisplayName = "大数据加解密")]
    public void LargeDataRoundTrip()
    {
        var data = new Byte[4096];
        new Random(123).NextBytes(data);
        var key = new Byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };

        var encrypted = RC4.Encrypt(data, key);
        var decrypted = RC4.Encrypt(encrypted, key);

        Assert.Equal(data, decrypted);
    }
}
