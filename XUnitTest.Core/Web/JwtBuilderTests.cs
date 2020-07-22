using System;
using NewLife.Web;
using Xunit;

namespace XUnitTest.Web
{
    public class JwtBuilderTests
    {
        [Fact]
        public void EncodeTest()
        {
            var builder = new JwtBuilder
            {
                //Id = null,
                //Subject = "Cube",
                //Issuer = "NewLife",
                IssuedAt = 1516239022.ToDateTime(),
                Expire = DateTime.MinValue,
                Secret = "Smart",
            };

            var token = builder.Encode(new { sub = "0201", name = "stone" });
            Assert.NotNull(token);
            Assert.NotEmpty(token);

            var ts = token.Split('.');
            Assert.Equal(3, ts.Length);
            Assert.Equal("eyJhbGciOiJIUzI1NiJ9", ts[0]);
            Assert.Equal("eyJzdWIiOiIwMjAxIiwibmFtZSI6InN0b25lIiwiaWF0IjoxNTE2MjM5MDIyfQ", ts[1]);
            Assert.Equal("mY2_rvQORkyYpK3f84liG2EDpaYY7pO43sRgcli381U", ts[2]);
        }

        [Fact]
        public void EncodeTest2()
        {
            var builder = new JwtBuilder
            {
                Id = Guid.NewGuid() + "",
                Subject = "Cube",
                Issuer = "NewLife",
                IssuedAt = DateTime.Now,
                Audience = "all",
                NotBefore = DateTime.Today,
                //Expire = TimeSpan.FromHours(0),
                Secret = "Smart",
            };

            var token = builder.Encode(new { sub = "0201", name = "stone" });
            Assert.NotNull(token);
            Assert.NotEmpty(token);

            // 有效期默认2小时
            Assert.True(builder.Expire.Year > 2000);
            var ts = builder.Expire - DateTime.Now;
            Assert.True(ts <= TimeSpan.FromHours(2));
            Assert.True(ts > TimeSpan.FromMinutes(2 * 60 - 1));

            var builder2 = new JwtBuilder
            {
                Secret = builder.Secret,
            };

            var rs = builder2.TryDecode(token, out var payload);
            Assert.True(rs);
            Assert.NotEqual(builder.Subject, builder2.Subject);
            Assert.Equal("0201", builder2.Subject);
            Assert.Null(builder2.Type);
            Assert.Equal(builder.Expire.Trim(), builder2.Expire.Trim());

            Assert.Equal(builder.Id, builder2.Id);
            Assert.Equal(builder.Issuer, builder2.Issuer);
            Assert.Equal(builder.IssuedAt.Trim(), builder2.IssuedAt.Trim());
            Assert.Equal(builder.Audience, builder2.Audience);
            Assert.Equal(builder.NotBefore, builder2.NotBefore);
            Assert.Equal("stone", builder2["name"]);
        }

        [Fact]
        public void AlgorithmTest()
        {
            var builder = new JwtBuilder
            {
                Id = null,
                Subject = "Cube",
                Issuer = "NewLife",
                IssuedAt = DateTime.Now,
                //Expire = TimeSpan.FromHours(0),
                Type = "JWT",
                Algorithm = "HS512",
                Secret = "Smart",
            };

            var token = builder.Encode(new { sub = "0201", name = "stone" });
            Assert.NotNull(token);
            Assert.NotEmpty(token);

            var builder2 = new JwtBuilder
            {
                Secret = builder.Secret,
            };

            var rs = builder2.TryDecode(token, out var payload);
            Assert.True(rs);
            Assert.NotEqual(builder.Subject, builder2.Subject);
            Assert.Equal("0201", builder2.Subject);
            Assert.Equal("JWT", builder2.Type);

            Assert.Equal(builder.Issuer, builder2.Issuer);
            Assert.Equal(builder.IssuedAt.Trim(), builder2.IssuedAt.Trim());
            Assert.Equal("stone", builder2["name"]);
        }
    }
}