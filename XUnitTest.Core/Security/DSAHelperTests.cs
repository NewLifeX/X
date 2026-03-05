using System.Security.Cryptography;
using System.Text;
using NewLife.Security;
using Xunit;

namespace XUnitTest.Security;

/// <summary>DSA数字签名算法测试</summary>
public class DSAHelperTests
{
    [Fact(DisplayName = "生成密钥对")]
    public void GenerateKey()
    {
        var keys = DSAHelper.GenerateKey(1024);

        Assert.NotNull(keys);
        Assert.Equal(2, keys.Length);
        Assert.False(String.IsNullOrEmpty(keys[0])); // 私钥
        Assert.False(String.IsNullOrEmpty(keys[1])); // 公钥
        Assert.Contains("DSAKeyValue", keys[0]);
        Assert.Contains("DSAKeyValue", keys[1]);
    }

    [Fact(DisplayName = "签名和验签")]
    public void SignAndVerify()
    {
        var keys = DSAHelper.GenerateKey(1024);
        var data = Encoding.UTF8.GetBytes("Hello DSA!");

        var signature = DSAHelper.Sign(data, keys[0]);
        Assert.NotNull(signature);
        Assert.True(signature.Length > 0);

        var result = DSAHelper.Verify(data, keys[1], signature);
        Assert.True(result);
    }

    [Fact(DisplayName = "篡改数据验签失败")]
    public void TamperedDataFailsVerification()
    {
        var keys = DSAHelper.GenerateKey(1024);
        var data = Encoding.UTF8.GetBytes("Original data");

        var signature = DSAHelper.Sign(data, keys[0]);

        var tampered = Encoding.UTF8.GetBytes("Tampered data");
        var result = DSAHelper.Verify(tampered, keys[1], signature);
        Assert.False(result);
    }

    [Fact(DisplayName = "不同密钥对验签失败")]
    public void DifferentKeyFailsVerification()
    {
        var keys1 = DSAHelper.GenerateKey(1024);
        var keys2 = DSAHelper.GenerateKey(1024);
        var data = Encoding.UTF8.GetBytes("test data");

        var signature = DSAHelper.Sign(data, keys1[0]);

        // 用另一个密钥对的公钥验签，应该失败
        var result = DSAHelper.Verify(data, keys2[1], signature);
        Assert.False(result);
    }

    [Fact(DisplayName = "XML扩展方法往返")]
    public void XmlRoundTrip()
    {
        var dsa = new DSACryptoServiceProvider(1024);
        var xmlPrivate = dsa.ToXmlStringX(true);
        var xmlPublic = dsa.ToXmlStringX(false);

        Assert.Contains("<P>", xmlPrivate);
        Assert.Contains("<X>", xmlPrivate); // 私钥应包含X
        Assert.Contains("<P>", xmlPublic);

        // 重新导入并验证
        var dsa2 = new DSACryptoServiceProvider();
        dsa2.FromXmlStringX(xmlPrivate);
        var param2 = dsa2.ExportParameters(true);
        Assert.NotNull(param2.X);
    }
}
