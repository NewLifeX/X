#if NET6_0_OR_GREATER
using System.Security.Cryptography;
using NewLife;
using NewLife.Security;
using Xunit;

namespace XUnitTest.Security;

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

    /// <summary>导出 CNG KeyBlob 的 Base64，与历史 CngKey.Export 产物格式一致</summary>
    /// <param name="ec">ECDsa实例</param>
    /// <param name="privateKey">是否包含私钥</param>
    /// <returns></returns>
    private static String ExportKey(ECDsa ec, Boolean privateKey)
    {
        var prm = ec.ExportParameters(privateKey);
        var key = new ECKey
        {
            X = prm.Q.X,
            Y = prm.Q.Y,
            D = prm.D,
        };
        key.Algorithm = $"{(privateKey ? "ECDSA_PRIVATE" : "ECDSA_PUBLIC")}_P{(prm.Q.X.Length == 32 ? 256 : prm.Q.X.Length == 48 ? 384 : 521)}";

        return key.ToArray().ToBase64();
    }

    [Theory]
    [InlineData(256)]
    [InlineData(384)]
    [InlineData(521)]
    public void GenerateKey(Int32 keySize)
    {
        // 生成密钥
        var ks = ECDsaHelper.GenerateKey(keySize);
        Assert.NotNull(ks);
        Assert.Equal(2, ks.Length);

        //var magic = ks[0].ToBase64().ReadBytes(0, 4).ToInt();
        //var magic2 = ks[1].ToBase64().ReadBytes(0, 4).ToInt();

        {
            // 重新导入
            var data = ks[0].ToBase64();

            // 解码KeyBlob格式
            var eckey = new ECKey();
            eckey.Read(data);
            Assert.Equal(data.ToBase64(), eckey.ToArray().ToBase64());

            // 幻数(4) + 长度len(4) + X(len) + Y(len) + D(len)
            Assert.Equal($"ECDSA_PRIVATE_P{keySize}", eckey.Algorithm);

            // 构造参数
            var ecp = eckey.ExportParameters();

            // 再次以参数导入，然后导出key进行对比（跨平台实现，不依赖 Windows CNG）
            var ec2 = ECDsa.Create();
            ec2.ImportParameters(ecp);
            Assert.Equal(ks[0], ExportKey(ec2, true));
        }

        {
            // 重新导入
            var data = ks[1].ToBase64();

            // 解码KeyBlob格式
            var eckey = new ECKey();
            eckey.Read(data);
            Assert.Equal(data.ToBase64(), eckey.ToArray().ToBase64());

            // 幻数(4) + 长度len(4) + X(len) + Y(len) + D(len)
            Assert.Equal($"ECDSA_PUBLIC_P{keySize}", eckey.Algorithm);

            // 构造参数
            var ecp = eckey.ExportParameters();

            // 再次以参数导入，然后导出key进行对比
            var ec2 = ECDsa.Create();
            ec2.ImportParameters(ecp);
            Assert.Equal(ks[1], ExportKey(ec2, false));
        }
    }

    [Fact]
    public void Create()
    {
        var ks = ECDsaHelper.GenerateKey();

        var ec = ECDsaHelper.Create(ks[0]);
        Assert.NotNull(ec);

        var ec2 = ECDsaHelper.Create(prvKey);
        Assert.NotNull(ec2);

        var ec3 = ECDsaHelper.Create(pubKey);
        Assert.NotNull(ec3);
    }

    [Fact]
    public void SignAndVerify()
    {
        var ks = ECDsaHelper.GenerateKey(256);

        var data = Rand.NextBytes(1000);

        {
            var sign = ECDsaHelper.Sign(data, ks[0]);
            Assert.NotNull(sign);

            var rs = ECDsaHelper.Verify(data, ks[1], sign);
            Assert.True(rs);
        }

        {
            var sign = ECDsaHelper.SignSha256(data, ks[0]);
            Assert.NotNull(sign);

            var rs = ECDsaHelper.VerifySha256(data, ks[1], sign);
            Assert.True(rs);
        }

        {
            var sign = ECDsaHelper.SignSha384(data, ks[0]);
            Assert.NotNull(sign);

            var rs = ECDsaHelper.VerifySha384(data, ks[1], sign);
            Assert.True(rs);
        }

        {
            var sign = ECDsaHelper.SignSha512(data, ks[0]);
            Assert.NotNull(sign);

            var rs = ECDsaHelper.VerifySha512(data, ks[1], sign);
            Assert.True(rs);
        }
    }

    [Fact]
    public void TestPublicPem()
    {
        var ec = ECDsaHelper.Create(pubKey);

        var key = ExportKey(ec, false);
        Assert.Equal("RUNTMSAAAAARWz+jn65BtOMvdyHKcvjBeBSDZH2r1RTwjmYSi9R/zpBnuQ4EiMnCqfMPWiZqB4QdbAd0E7oH50VpuZ1P087G", key);

        //var rs = ec.VerifyData("NewLife".GetBytes(), "9rW9GddDi0jjVMnbAulgqiPpXQJR3oJIz/XX9mYVI9uIMePlmW9eNbwdq34AMFa5pp31513AR2WxQ1Nz6K2aZQ==".ToBase64());
        //Assert.True(rs);
    }

    [Fact]
    public void TestPrivatePem()
    {
        var ec = ECDsaHelper.Create(prvKey);

        var key = ExportKey(ec, true);
        Assert.Equal("RUNTMiAAAAARWz+jn65BtOMvdyHKcvjBeBSDZH2r1RTwjmYSi9R/zpBnuQ4EiMnCqfMPWiZqB4QdbAd0E7oH50VpuZ1P087GevZzL1gdAFr88hb2OF/2NxApJCzGCEDdfSp6VQO30hw=", key);

        //var sign = ec.SignData("NewLife".GetBytes());
        //Assert.Equal("9rW9GddDi0jjVMnbAulgqiPpXQJR3oJIz/XX9mYVI9uIMePlmW9eNbwdq34AMFa5pp31513AR2WxQ1Nz6K2aZQ==", sign.ToBase64());
    }

    [Fact]
    public void SignAndVerifyWithPem()
    {
        var data = "NewLife".GetBytes();
        Byte[] sign;

        {
            var ec = ECDsaHelper.Create(prvKey);

            var key = ExportKey(ec, true);
            Assert.Equal("RUNTMiAAAAARWz+jn65BtOMvdyHKcvjBeBSDZH2r1RTwjmYSi9R/zpBnuQ4EiMnCqfMPWiZqB4QdbAd0E7oH50VpuZ1P087GevZzL1gdAFr88hb2OF/2NxApJCzGCEDdfSp6VQO30hw=", key);

            sign = ec.SignData(data, HashAlgorithmName.SHA256);
        }

        {
            var ec = ECDsaHelper.Create(pubKey);

            var key = ExportKey(ec, false);
            Assert.Equal("RUNTMSAAAAARWz+jn65BtOMvdyHKcvjBeBSDZH2r1RTwjmYSi9R/zpBnuQ4EiMnCqfMPWiZqB4QdbAd0E7oH50VpuZ1P087G", key);

            var rs = ec.VerifyData(data, sign, HashAlgorithmName.SHA256);
            Assert.True(rs);
        }
    }
}
#endif