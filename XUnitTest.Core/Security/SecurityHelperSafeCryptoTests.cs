using System.Security.Cryptography;
using NewLife;
using Xunit;

namespace XUnitTest.Security;

/// <summary>安全对称加密（随机 IV + HMAC）</summary>
public class SecurityHelperSafeCryptoTests
{
    private static readonly Byte[] _pass = "Passw0rd-2026"u8.ToArray();
    private static readonly Byte[] _data = "hello newlife core"u8.ToArray();

    [Fact(DisplayName = "同一明文两次加密结果不同（随机 IV）")]
    public void EncryptTwiceDifferent()
    {
        using var aes = Aes.Create();
        var a = aes.EncryptSafe(_data, _pass);
        var b = aes.EncryptSafe(_data, _pass);

        Assert.NotEqual(a, b);
        Assert.Equal(_data, aes.DecryptSafe(a, _pass));
        Assert.Equal(_data, aes.DecryptSafe(b, _pass));
    }

    [Fact(DisplayName = "密文被篡改时解密失败")]
    public void TamperedDataRejected()
    {
        using var aes = Aes.Create();
        var cipher = aes.EncryptSafe(_data, _pass);

        // 改中间一段密文（跳过前面的 IV）
        cipher[cipher.Length / 2] ^= 0xFF;

        Assert.Throws<InvalidDataException>(() => aes.DecryptSafe(cipher, _pass));
    }

    [Fact(DisplayName = "密码错误时解密失败")]
    public void WrongPasswordRejected()
    {
        using var aes = Aes.Create();
        var cipher = aes.EncryptSafe(_data, _pass);

        Assert.Throws<InvalidDataException>(() => aes.DecryptSafe(cipher, "bad-password"u8.ToArray()));
    }

    [Fact(DisplayName = "数据太短时直接拒绝")]
    public void TooShortRejected()
    {
        using var aes = Aes.Create();

        Assert.Throws<InvalidDataException>(() => aes.DecryptSafe(new Byte[10], _pass));
    }
}
