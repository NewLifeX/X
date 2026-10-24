using System.ComponentModel;
using System.Security.Cryptography;
using NewLife;
using NewLife.Security;
using Xunit;

namespace XUnitTest.Compat;

/// <summary>ECDsaHelper 双分支兼容性测试。net462 宿主走 Windows CNG 实现，net8.0 宿主走跨平台实现，同一套用例验证两种实现行为一致</summary>
public class ECDsaHelperTests
{
    String prvKey = @"-----BEGIN PRIVATE KEY-----
MIGHAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBG0wawIBAQQgevZzL1gdAFr88hb2
OF/2NxApJCzGCEDdfSp6VQO30hyhRANCAAQRWz+jn65BtOMvdyHKcvjBeBSDZH2r
1RTwjmYSi9R/zpBnuQ4EiMnCqfMPWiZqB4QdbAd0E7oH50VpuZ1P087G
-----END PRIVATE KEY-----";
    String pubKey = @"-----BEGIN PUBLIC KEY-----
MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEEVs/o5+uQbTjL3chynL4wXgUg2R9
q9UU8I5mEovUf86QZ7kOBIjJwqnzD1omageEHWwHdBO6B+dFabmdT9POxg==
-----END PUBLIC KEY-----";

    [Theory]
    [InlineData(256)]
    [InlineData(384)]
    [InlineData(521)]
    [DisplayName("ECDsaHelper_生成密钥_KeyBlob往返一致")]
    public void GenerateKeyAndRoundTrip(Int32 keySize)
    {
        var ks = ECDsaHelper.GenerateKey(keySize);
        Assert.Equal(2, ks.Length);

        // 私钥：解码后重新编码，字节应完全一致
        var ek = new ECKey();
        ek.Read(ks[0].ToBase64());
        Assert.Equal($"ECDSA_PRIVATE_P{keySize}", ek.Algorithm);
        Assert.NotNull(ek.D);
        Assert.Equal(ks[0], ek.ToArray().ToBase64());

        // 公钥
        var ek2 = new ECKey();
        ek2.Read(ks[1].ToBase64());
        Assert.Equal($"ECDSA_PUBLIC_P{keySize}", ek2.Algorithm);
        Assert.Null(ek2.D);
        Assert.Equal(ks[1], ek2.ToArray().ToBase64());
    }

    [Fact]
    [DisplayName("ECDsaHelper_签名验证_四种散列往返")]
    public void SignAndVerify()
    {
        var ks = ECDsaHelper.GenerateKey(256);
        var data = Rand.NextBytes(1000);

        var sign = ECDsaHelper.Sign(data, ks[0]);
        Assert.True(ECDsaHelper.Verify(data, ks[1], sign));

        sign = ECDsaHelper.SignSha256(data, ks[0]);
        Assert.True(ECDsaHelper.VerifySha256(data, ks[1], sign));

        sign = ECDsaHelper.SignSha384(data, ks[0]);
        Assert.True(ECDsaHelper.VerifySha384(data, ks[1], sign));

        sign = ECDsaHelper.SignSha512(data, ks[0]);
        Assert.True(ECDsaHelper.VerifySha512(data, ks[1], sign));
    }

    [Fact]
    [DisplayName("ECDsaHelper_PEM密钥_签名验证往返")]
    public void SignAndVerifyWithPem()
    {
        var data = "NewLife".GetBytes();

        var sign = ECDsaHelper.SignSha256(data, prvKey);
        Assert.True(ECDsaHelper.VerifySha256(data, pubKey, sign));
    }

    [Fact]
    [DisplayName("ECDsaHelper_空密钥_返回null")]
    public void CreateEmpty() => Assert.Null(ECDsaHelper.Create(""));

    [Fact]
    [DisplayName("ECDsaHelper_实现分支_老框架为CNG、现代框架为平台实现")]
    public void CheckImplementation()
    {
        var ks = ECDsaHelper.GenerateKey(256);
        var ec = ECDsaHelper.Create(ks[0]);
        Assert.NotNull(ec);

#if NETFRAMEWORK
        // 老框架分支：直接构造 ECDsaCng
        Assert.IsType<ECDsaCng>(ec);
#else
        // 现代分支：平台实现（Windows 上 ECDsaWrapper 包 ECDsaCng、Linux/macOS 为 OpenSSL），不再是直接的 ECDsaCng 实例
        Assert.NotEqual("System.Security.Cryptography.ECDsaCng", ec.GetType().FullName);
#endif
    }
}
