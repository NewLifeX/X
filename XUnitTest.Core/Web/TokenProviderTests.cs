using System;
using System.IO;
using System.Text;
using NewLife;
using NewLife.Security;
using NewLife.Web;
using Xunit;

namespace XUnitTest.Web;

public class TokenProviderTests
{
    [Fact]
    public void Test()
    {
        var prv = new TokenProvider();

        // 加载或生成密钥
        var rs = prv.ReadKey("keys/test.prvkey", true);
        Assert.True(rs);
        Assert.True(File.Exists("keys/test.prvkey".GetFullPath()));
        Assert.True(File.Exists("keys/test.pubkey".GetFullPath()));
        Assert.NotEmpty(prv.Key);

        // 生成令牌
        var user = Rand.NextString(8);
        var time = DateTime.Now.AddHours(2);
        var token = prv.Encode(user, time);

        Assert.NotEmpty(token);
        var data = token.Substring(null, ".").ToBase64().ToStr();
        Assert.Equal($"{user},{time.ToUniversalTime().ToInt()}", data);

        // 解码令牌
        var prv2 = new TokenProvider();
        prv2.ReadKey("keys/test.pubkey", false);
        var rs2 = prv2.TryDecode(token, out var user2, out var time2);

        Assert.True(rs2);
        Assert.Equal(user, user2);
        Assert.Equal(time.Trim(), time2.Trim());

        // 破坏数据
        token = $"Stone,{time.ToUniversalTime().ToInt()}".GetBytes().ToUrlBase64() + "." + token.Substring(".");
        var rs3 = prv2.TryDecode(token, out var user3, out var time3);

        Assert.False(rs3);
        Assert.NotEqual(user, user3);
        Assert.Equal(time.Trim(), time3.Trim());
    }

    [Fact(DisplayName = "畸形令牌不抛异常，一律返回失败")]
    public void MalformedToken_NoThrow()
    {
        // 无密钥：解析阶段就不能抛
        var tp = new TokenProvider();
        foreach (var t in new[] { "abc", "!!.!!", "AAA!!!.BBB", "eA.a" })
        {
            var rs = tp.TryDecode(t, out _, out _);
            Assert.False(rs);
        }

        // 有密钥：畸形 Base64 与乱签名同样只返回 false
        var keys = DSAHelper.GenerateKey();
        var tp2 = new TokenProvider { Key = keys[1] };
        foreach (var t in new[] { "!!.!!", "AAA!!!.BBB", "BBBB.CCCC" })
        {
            var rs = tp2.TryDecode(t, out _, out _);
            Assert.False(rs);
        }
    }
}