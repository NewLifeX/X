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
                Expire = TimeSpan.FromHours(0),
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
                Id = null,
                Subject = "Cube",
                Issuer = "NewLife",
                IssuedAt = DateTime.Now,
                //Expire = TimeSpan.FromHours(0),
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
            Assert.Null(builder2.Type);

            Assert.Equal(builder.Issuer, builder2.Issuer);
            Assert.Equal(builder.IssuedAt.Trim(), builder2.IssuedAt.Trim());
            Assert.Equal("stone", payload["name"]);
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
            Assert.Equal("stone", payload["name"]);
        }
    }
}