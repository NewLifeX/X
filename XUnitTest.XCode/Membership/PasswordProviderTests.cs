using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife;
using XCode.Membership;
using Xunit;

namespace XUnitTest.XCode.Membership
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
    }
}