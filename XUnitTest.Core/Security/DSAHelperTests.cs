using System.Security.Cryptography;
using System.Text;
using System.Xml;
using NewLife;
using NewLife.Log;
using NewLife.Security;
using Xunit;

namespace XUnitTest.Security;

/// <summary>DSA数字签名算法测试</summary>
public class DSAHelperTests
{
    /// <summary>DSA 在 macOS 只支持导入密钥，不支持新密钥生成（平台差异），整组跳过并留痕</summary>
    /// <returns>当前平台是否支持</returns>
    private static Boolean EnsureDsaSupported()
    {
        if (!Runtime.OSX) return true;

        XTrace.WriteLine("macOS 不支持 DSA 新密钥生成，跳过当前用例");

        return false;
    }

    [Fact(DisplayName = "生成密钥对")]
    public void GenerateKey()
    {
        if (!EnsureDsaSupported()) return;

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
        if (!EnsureDsaSupported()) return;

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
        if (!EnsureDsaSupported()) return;

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
        if (!EnsureDsaSupported()) return;

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
        if (!EnsureDsaSupported()) return;

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

    [Fact(DisplayName = "PgenCounter与Counter兼容读取")]
    public void PgenCounterRoundTrip()
    {
        if (!EnsureDsaSupported()) return;

        var dsa = new DSACryptoServiceProvider(1024);
        var xml = dsa.ToXmlStringX(true);
        Assert.Contains("<PgenCounter>", xml);

        var src = dsa.ExportParameters(true);

        // 导入自产 XML，Counter/Seed 应保持
        var dsa2 = new DSACryptoServiceProvider();
        dsa2.FromXmlStringX(xml);
        var dst = dsa2.ExportParameters(true);
        Assert.Equal(src.Counter, dst.Counter);
        Assert.Equal(src.Seed, dst.Seed);
    }

    [Fact(DisplayName = "兼容Base64编码的PgenCounter（历史/外部密钥）")]
    public void PgenCounterBase64Compatibility()
    {
        if (!EnsureDsaSupported()) return;

        // 构造含 Base64 编码 PgenCounter 的私钥 XML，模拟历史密钥或外部工具生成的密钥
        var dsa = new DSACryptoServiceProvider(1024);
        var xml = dsa.ToXmlStringX(true);

        // 将十进制 PgenCounter 替换为 Base64 编码值（如报错中的 'Azo='）
        var doc = new XmlDocument();
        doc.LoadXml(xml);
        doc.DocumentElement.SelectSingleNode("PgenCounter").InnerText = "Azo=";
        xml = doc.OuterXml;

        Assert.Contains("<PgenCounter>Azo=</PgenCounter>", xml);

        // 导入不应抛异常
        var dsa2 = new DSACryptoServiceProvider();
        dsa2.FromXmlStringX(xml);

        // 导入后签名，公钥应能验签
        var data = Encoding.UTF8.GetBytes("Hello DSA Base64 PgenCounter!");
        var sig = dsa2.SignData(data);

        var pub = new DSACryptoServiceProvider();
        pub.FromXmlStringX(dsa.ToXmlStringX(false));
        Assert.True(pub.VerifyData(data, sig));
    }
}
