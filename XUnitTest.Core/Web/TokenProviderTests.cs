using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife;
using NewLife.Security;
using NewLife.Web;
using Xunit;

namespace XUnitTest.Web
{
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
            Assert.Equal($"{user},{time.ToInt()}", data);

            // 解码令牌
            var prv2 = new TokenProvider();
            prv2.ReadKey("keys/test.pubkey", false);
            var rs2 = prv2.TryDecode(token, out var user2, out var time2);

            Assert.True(rs2);
            Assert.Equal(user, user2);
            Assert.Equal(time.Trim(), time2.Trim());

            // 破坏数据
            token = $"Stone,{time.ToInt()}".GetBytes().ToUrlBase64() + "." + token.Substring(".");
            var rs3 = prv2.TryDecode(token, out var user3, out var time3);

            Assert.False(rs3);
            Assert.NotEqual(user, user3);
            Assert.Equal(time.Trim(), time3.Trim());
        }
    }
}