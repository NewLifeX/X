using System.Text;
using NewLife;
using NewLife.Security;
using Xunit;

namespace XUnitTest.Security;

public class PasswordProviderTests
{
    [Fact]
    public void HashTest()
    {
        var prv = new SaltPasswordProvider();
        var hash = prv.Hash("New#life");

        var ss = hash.Split('$');
        Assert.Equal(4, ss.Length);
        Assert.Empty(ss[0]);
        Assert.Equal(prv.Algorithm, ss[1]);

        var salt = ss[2];
        var hash2 = "New#life".GetBytes().SHA512(salt.GetBytes()).ToBase64();
        Assert.Equal(hash2, ss[3]);

        var rs = prv.Verify("New#life", hash);
        Assert.True(rs);
    }

    [Fact]
    public void SaltTime()
    {
        var prv = new SaltPasswordProvider
        {
            SaltTime = 30,
            Algorithm = "md5",
        };
        var hash = prv.Hash("New#life");

        var ss = hash.Split('$');
        Assert.Equal(4, ss.Length);
        Assert.Empty(ss[0]);
        Assert.Equal(prv.Algorithm, ss[1]);

        var salt = ss[2];
        Assert.True(salt.ToInt() > 0);

        var hash2 = ("New#life".MD5() + salt).MD5();
        Assert.Equal(hash2, ss[3]);

        var rs = prv.Verify("New#life", hash);
        Assert.True(rs);
    }

    [Fact]
    public void Md5Test_Web()
    {
        var prv = new SaltPasswordProvider { Algorithm = "md5" };

        // 数据库保存Hash
        var pass = "New#life";
        var hash = prv.Hash(pass);

        // 客户端有密码，登录时传输明文密码
        var rs = prv.Verify(pass, hash);
        Assert.True(rs);

        // 登录时传输MD5散列，数据库Hash本质上是md5+md5两重散列，所以传输MD5散列也能通过
        rs = prv.Verify(pass.MD5(), hash);
        Assert.True(rs);
    }

    [Fact]
    public void Md5Test_App()
    {
        var prv = new SaltPasswordProvider { Algorithm = "md5" };

        // 数据库保存明文密码
        var pass = "New#life";
        var hash = prv.Hash(pass);

        // 客户端有密码，登录时传输Hash
        var rs = prv.Verify(pass, hash);
        Assert.True(rs);

        // 登录时传输的Hash本质上是md5+md5两重散列，所以数据库保存MD5散列也能通过
        rs = prv.Verify(pass.MD5(), hash);
        Assert.True(rs);
    }

    [Fact]
    public void SHA1Test_Web()
    {
        var prv = new SaltPasswordProvider { Algorithm = "sha1" };

        // 数据库保存Hash
        var pass = "New#life";
        var hash = prv.Hash(pass);

        // 客户端有密码，登录时传输明文密码
        var rs = prv.Verify(pass, hash);
        Assert.True(rs);

        // 登录时传输MD5散列，数据库Hash是SHA加盐散列，所以校验无法通过
        rs = prv.Verify(pass.MD5(), hash);
        Assert.False(rs);
    }

    [Fact]
    public void SHA1Test_App()
    {
        var prv = new SaltPasswordProvider { Algorithm = "sha1" };

        // 数据库保存明文密码
        var pass = "New#life";
        var hash = prv.Hash(pass);

        // 客户端有密码，登录时传输Hash
        var rs = prv.Verify(pass, hash);
        Assert.True(rs);

        // 登录时传输的Hash是SHA加盐散列，所以数据库保存MD5散列时校验无法通过
        rs = prv.Verify(pass.MD5(), hash);
        Assert.False(rs);
    }

    [Fact]
    public void SHA512Test_Web()
    {
        var prv = new SaltPasswordProvider { Algorithm = "sha512" };

        // 数据库保存Hash
        var pass = "New#life";
        var hash = prv.Hash(pass);

        // 客户端有密码，登录时传输明文密码
        var rs = prv.Verify(pass, hash);
        Assert.True(rs);

        // 登录时传输MD5散列，数据库Hash是SHA加盐散列，所以校验无法通过
        rs = prv.Verify(pass.MD5(), hash);
        Assert.False(rs);
    }

    [Fact]
    public void SHA512Test_App()
    {
        var prv = new SaltPasswordProvider { Algorithm = "sha512" };

        // 数据库保存明文密码
        var pass = "New#life";
        var hash = prv.Hash(pass);

        // 客户端有密码，登录时传输Hash
        var rs = prv.Verify(pass, hash);
        Assert.True(rs);

        // 登录时传输的Hash是SHA加盐散列，所以数据库保存MD5散列时校验无法通过
        rs = prv.Verify(pass.MD5(), hash);
        Assert.False(rs);
    }

    [Fact]
    public void MD5SHA1Test_Web()
    {
        var prv = new SaltPasswordProvider { Algorithm = "md5+sha1" };

        // 数据库保存Hash
        var pass = "New#life";
        var hash = prv.Hash(pass);

        // 客户端有密码，登录时传输明文密码。兼容纯SHA散列
        var rs = prv.Verify(pass, hash);
        Assert.True(rs);

        // 登录时传输MD5散列，数据库Hash是MD5+SHA加盐散列，所以校验通过
        rs = prv.Verify(pass.MD5(), hash);
        Assert.True(rs);
    }

    [Fact]
    public void MD5SHA1Test_App()
    {
        var prv = new SaltPasswordProvider { Algorithm = "md5+sha1" };

        // 数据库保存明文密码
        var pass = "New#life";
        var hash = prv.Hash(pass);

        // 客户端有密码，登录时传输Hash
        var rs = prv.Verify(pass, hash);
        Assert.True(rs);

        // 登录时传输的Hash是MD5+SHA加盐散列，所以数据库保存MD5散列时校验通过
        rs = prv.Verify(pass.MD5(), hash);
        Assert.True(rs);
    }

    [Fact]
    public void MD5SHA512Test_Web()
    {
        var prv = new SaltPasswordProvider { Algorithm = "md5+sha512" };

        // 数据库保存Hash
        var pass = "New#life";
        var hash = prv.Hash(pass);

        // 客户端有密码，登录时传输明文密码。兼容纯SHA散列
        var rs = prv.Verify(pass, hash);
        Assert.True(rs);

        // 登录时传输MD5散列，数据库Hash是MD5+SHA加盐散列，所以校验通过
        rs = prv.Verify(pass.MD5(), hash);
        Assert.True(rs);

        // 数据库里可能是旧版sha512散列
        var prv2 = new SaltPasswordProvider { Algorithm = "sha512" };
        var hash2 = prv2.Hash(pass);

        // 客户端有密码，登录时传输明文密码。兼容纯SHA散列
        rs = prv.Verify(pass, hash2);
        Assert.True(rs);

        // 登录时传输MD5散列，旧版sha512散列不支持，所以校验无法通过
        rs = prv.Verify(pass.MD5(), hash2);
        Assert.False(rs);
    }

    [Fact]
    public void MD5SHA512Test_App()
    {
        var prv = new SaltPasswordProvider { Algorithm = "md5+sha512" };

        // 数据库保存明文密码
        var pass = "New#life";
        var hash = prv.Hash(pass);

        // 客户端有密码，登录时传输Hash
        var rs = prv.Verify(pass, hash);
        Assert.True(rs);

        // 登录时传输的Hash是MD5+SHA加盐散列，所以数据库保存MD5散列时校验通过
        rs = prv.Verify(pass.MD5(), hash);
        Assert.True(rs);

        // 客户端传输可能是旧版sha512散列
        var prv2 = new SaltPasswordProvider { Algorithm = "sha512" };
        var hash2 = prv2.Hash(pass);

        // 数据库保存明文密码，登录时传输旧版sha512散列
        rs = prv.Verify(pass, hash2);
        Assert.True(rs);

        // 数据库保存MD5散列，旧版sha512散列不支持，所以校验无法通过
        rs = prv.Verify(pass.MD5(), hash2);
        Assert.False(rs);
    }
}