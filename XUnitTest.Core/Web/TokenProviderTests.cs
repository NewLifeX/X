using System;
using System.IO;
using System.Text;
using NewLife;
using NewLife.Log;
using NewLife.Security;
using NewLife.Web;
using Xunit;

namespace XUnitTest.Web;

public class TokenProviderTests
{
    [Fact]
    public void Test()
    {
        // TokenProvider 依赖 DSA 密钥生成，macOS 不支持；显式跳过并留痕
        if (Runtime.OSX)
        {
            XTrace.WriteLine("macOS 不支持 DSA 新密钥生成，跳过 TokenProvider 用例");

            return;
        }

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

        // 有密钥：畸形 Base64 与乱签名同样只返回 false；密钥生成依赖 DSA，macOS 不支持则跳过
        if (Runtime.OSX)
        {
            XTrace.WriteLine("macOS 不支持 DSA 新密钥生成，跳过签名侧断言");

            return;
        }

        var keys = DSAHelper.GenerateKey();
        var tp2 = new TokenProvider { Key = keys[1] };
        foreach (var t in new[] { "!!.!!", "AAA!!!.BBB", "BBBB.CCCC" })
        {
            var rs = tp2.TryDecode(t, out _, out _);
            Assert.False(rs);
        }
    }

    [Fact(DisplayName = "过期令牌返回失败，30秒容差内仍有效")]
    public void ExpiredToken_Rejected()
    {
        // TokenProvider 依赖 DSA 密钥生成，macOS 不支持；显式跳过并留痕
        if (Runtime.OSX)
        {
            XTrace.WriteLine("macOS 不支持 DSA 新密钥生成，跳过 TokenProvider 用例");

            return;
        }

        var prv = new TokenProvider();
        Assert.True(prv.ReadKey("keys/test.prvkey", true));

        var pub = new TokenProvider();
        Assert.True(pub.ReadKey("keys/test.pubkey", false));

        var user = Rand.NextString(8);

        // 远超容差（1小时前过期）：失败，但仍输出用户与过期时间
        var expired = DateTime.Now.AddHours(-1);
        var token = prv.Encode(user, expired);
        Assert.False(pub.TryDecode(token, out var user2, out var expire2));
        Assert.Equal(user, user2);
        Assert.Equal(expired.Trim(), expire2.Trim());

        // 容差内（10秒前过期）：仍按有效处理
        var grace = DateTime.Now.AddSeconds(-10);
        var token2 = prv.Encode(user, grace);
        Assert.True(pub.TryDecode(token2, out var user3, out var expire3));
        Assert.Equal(user, user3);
        Assert.Equal(grace.Trim(), expire3.Trim());

        // 未过期：有效且用户正确
        var future = DateTime.Now.AddHours(2);
        var token3 = prv.Encode(user, future);
        Assert.True(pub.TryDecode(token3, out var user4, out var expire4));
        Assert.Equal(user, user4);
        Assert.Equal(future.Trim(), expire4.Trim());
    }
}