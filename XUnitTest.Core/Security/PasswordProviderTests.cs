using System;
using System.Text;
using NewLife;
using NewLife.Security;
using Xunit;

namespace XUnitTest.Security
{
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
    }
}