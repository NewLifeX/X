using System;
using NewLife.Security;
using NewLife.Web;
using Xunit;

namespace XUnitTest.Web
{
    public class JwtBuilderTests
    {
        static JwtBuilderTests()
        {
            JwtBuilder.RegisterAlgorithm("ES256", ECDsaHelper.SignSha256, ECDsaHelper.VerifySha256);
            JwtBuilder.RegisterAlgorithm("ES384", ECDsaHelper.SignSha384, ECDsaHelper.VerifySha384);
            JwtBuilder.RegisterAlgorithm("ES512", ECDsaHelper.SignSha512, ECDsaHelper.VerifySha512);
        }

        [Fact]
        public void HS256_Encode()
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

            var builder2 = new JwtBuilder
            {
                Secret = builder.Secret,
            };

            ts = builder2.Parse(token);
            Assert.NotNull(ts);
            Assert.Equal(3, ts.Length);

            var rs = builder2.TryDecode(token, out var msg);
            Assert.True(rs);
            Assert.Null(msg);

            Assert.Null(builder2.Type);
            Assert.Equal("0201", builder2.Subject);
            Assert.Equal("stone", builder2["name"]);
        }

        [Fact]
        public void HS256_Encode2()
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

        [Fact]
        public void RS256()
        {
            var prvKey = @"-----BEGIN RSA PRIVATE KEY-----
MIIEogIBAAKCAQEAnzyis1ZjfNB0bBgKFMSvvkTtwlvBsaJq7S5wA+kzeVOVpVWw
kWdVha4s38XM/pa/yr47av7+z3VTmvDRyAHcaT92whREFpLv9cj5lTeJSibyr/Mr
m/YtjCZVWgaOYIhwrXwKLqPr/11inWsAkfIytvHWTxZYEcXLgAXFuUuaS3uF9gEi
NQwzGTU1v0FqkqTBr4B8nW3HCN47XUu0t8Y0e+lf4s4OxQawWD79J9/5d3Ry0vbV
3Am1FtGJiJvOwRsIfVChDpYStTcHTCMqtvWbV6L11BWkpzGXSW4Hv43qa+GSYOD2
QU68Mb59oSk2OB+BtOLpJofmbGEGgvmwyCI9MwIDAQABAoIBACiARq2wkltjtcjs
kFvZ7w1JAORHbEufEO1Eu27zOIlqbgyAcAl7q+/1bip4Z/x1IVES84/yTaM8p0go
amMhvgry/mS8vNi1BN2SAZEnb/7xSxbflb70bX9RHLJqKnp5GZe2jexw+wyXlwaM
+bclUCrh9e1ltH7IvUrRrQnFJfh+is1fRon9Co9Li0GwoN0x0byrrngU8Ak3Y6D9
D8GjQA4Elm94ST3izJv8iCOLSDBmzsPsXfcCUZfmTfZ5DbUDMbMxRnSo3nQeoKGC
0Lj9FkWcfmLcpGlSXTO+Ww1L7EGq+PT3NtRae1FZPwjddQ1/4V905kyQFLamAA5Y
lSpE2wkCgYEAy1OPLQcZt4NQnQzPz2SBJqQN2P5u3vXl+zNVKP8w4eBv0vWuJJF+
hkGNnSxXQrTkvDOIUddSKOzHHgSg4nY6K02ecyT0PPm/UZvtRpWrnBjcEVtHEJNp
bU9pLD5iZ0J9sbzPU/LxPmuAP2Bs8JmTn6aFRspFrP7W0s1Nmk2jsm0CgYEAyH0X
+jpoqxj4efZfkUrg5GbSEhf+dZglf0tTOA5bVg8IYwtmNk/pniLG/zI7c+GlTc9B
BwfMr59EzBq/eFMI7+LgXaVUsM/sS4Ry+yeK6SJx/otIMWtDfqxsLD8CPMCRvecC
2Pip4uSgrl0MOebl9XKp57GoaUWRWRHqwV4Y6h8CgYAZhI4mh4qZtnhKjY4TKDjx
QYufXSdLAi9v3FxmvchDwOgn4L+PRVdMwDNms2bsL0m5uPn104EzM6w1vzz1zwKz
5pTpPI0OjgWN13Tq8+PKvm/4Ga2MjgOgPWQkslulO/oMcXbPwWC3hcRdr9tcQtn9
Imf9n2spL/6EDFId+Hp/7QKBgAqlWdiXsWckdE1Fn91/NGHsc8syKvjjk1onDcw0
NvVi5vcba9oGdElJX3e9mxqUKMrw7msJJv1MX8LWyMQC5L6YNYHDfbPF1q5L4i8j
8mRex97UVokJQRRA452V2vCO6S5ETgpnad36de3MUxHgCOX3qL382Qx9/THVmbma
3YfRAoGAUxL/Eu5yvMK8SAt/dJK6FedngcM3JEFNplmtLYVLWhkIlNRGDwkg3I5K
y18Ae9n7dHVueyslrb6weq7dTkYDi3iOYRW8HRkIQh06wEdbxt0shTzAJvvCQfrB
jg/3747WSsf/zBTcHihTRBdAv6OmdhV4/dD5YBfLAkLrd+mX7iE=
-----END RSA PRIVATE KEY-----";
            var pubKey = @"-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAnzyis1ZjfNB0bBgKFMSv
vkTtwlvBsaJq7S5wA+kzeVOVpVWwkWdVha4s38XM/pa/yr47av7+z3VTmvDRyAHc
aT92whREFpLv9cj5lTeJSibyr/Mrm/YtjCZVWgaOYIhwrXwKLqPr/11inWsAkfIy
tvHWTxZYEcXLgAXFuUuaS3uF9gEiNQwzGTU1v0FqkqTBr4B8nW3HCN47XUu0t8Y0
e+lf4s4OxQawWD79J9/5d3Ry0vbV3Am1FtGJiJvOwRsIfVChDpYStTcHTCMqtvWb
V6L11BWkpzGXSW4Hv43qa+GSYOD2QU68Mb59oSk2OB+BtOLpJofmbGEGgvmwyCI9
MwIDAQAB
-----END PUBLIC KEY-----";

            var builder = new JwtBuilder
            {
                Algorithm = "RS256",
                Type = "JWT",

                IssuedAt = 1516239022.ToDateTime(),
                Expire = DateTime.MinValue,
                Secret = prvKey,
            };

            var token = builder.Encode(new { sub = "1234567890", name = "NewLife", admin = true });
            Assert.NotNull(token);
            Assert.NotEmpty(token);

            var ts = token.Split('.');
            Assert.Equal(3, ts.Length);
            Assert.Equal("eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9", ts[0]);
            Assert.Equal("eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6Ik5ld0xpZmUiLCJhZG1pbiI6dHJ1ZSwiaWF0IjoxNTE2MjM5MDIyfQ", ts[1]);
            Assert.Equal("N4Ca2l1ucFoLtVLyU2dC4PKOW7wkEnjUXYb129_Jh8DFD9EGHIe70cSgy5ZYA6PvhZ3XT5PADpjy7-uwXCFfgww3_ChfPye2GJLy_cDOz7XcoJ-kFy_-83AUb73AjDLrMQ5M1_5WRVHl_Nw2E52b5cKuczwU3kdSVF3wEwgS3ku8xPz4iN6eOpfUOh5cjei0S4uwLLPYCf56KY7zbXlf5PXDpX5iQ098PAzvDJRf7jv21GEwaKRRdY8V8wpnPV6lDVp92qR8E2lGMA082WQWZf0RBDloG7EauulMPXiM43FE7DXBKSdbkXyFmky-xjWkCnLxSjKpVHpRS41vdFZgtA", ts[2]);

            var builder2 = new JwtBuilder
            {
                Algorithm = "RS256",

                Secret = pubKey,
            };
            var rs = builder2.TryDecode(token, out var msg);
            Assert.True(rs);
            Assert.Null(msg);

            Assert.Equal("JWT", builder2.Type);
            Assert.Equal("1234567890", builder2.Subject);
            Assert.Equal("NewLife", builder2["name"]);
            Assert.True(builder2["admin"].ToBoolean());
        }

        [Fact]
        public void RS384()
        {
            var prvKey = @"-----BEGIN RSA PRIVATE KEY-----
MIIEogIBAAKCAQEAnzyis1ZjfNB0bBgKFMSvvkTtwlvBsaJq7S5wA+kzeVOVpVWw
kWdVha4s38XM/pa/yr47av7+z3VTmvDRyAHcaT92whREFpLv9cj5lTeJSibyr/Mr
m/YtjCZVWgaOYIhwrXwKLqPr/11inWsAkfIytvHWTxZYEcXLgAXFuUuaS3uF9gEi
NQwzGTU1v0FqkqTBr4B8nW3HCN47XUu0t8Y0e+lf4s4OxQawWD79J9/5d3Ry0vbV
3Am1FtGJiJvOwRsIfVChDpYStTcHTCMqtvWbV6L11BWkpzGXSW4Hv43qa+GSYOD2
QU68Mb59oSk2OB+BtOLpJofmbGEGgvmwyCI9MwIDAQABAoIBACiARq2wkltjtcjs
kFvZ7w1JAORHbEufEO1Eu27zOIlqbgyAcAl7q+/1bip4Z/x1IVES84/yTaM8p0go
amMhvgry/mS8vNi1BN2SAZEnb/7xSxbflb70bX9RHLJqKnp5GZe2jexw+wyXlwaM
+bclUCrh9e1ltH7IvUrRrQnFJfh+is1fRon9Co9Li0GwoN0x0byrrngU8Ak3Y6D9
D8GjQA4Elm94ST3izJv8iCOLSDBmzsPsXfcCUZfmTfZ5DbUDMbMxRnSo3nQeoKGC
0Lj9FkWcfmLcpGlSXTO+Ww1L7EGq+PT3NtRae1FZPwjddQ1/4V905kyQFLamAA5Y
lSpE2wkCgYEAy1OPLQcZt4NQnQzPz2SBJqQN2P5u3vXl+zNVKP8w4eBv0vWuJJF+
hkGNnSxXQrTkvDOIUddSKOzHHgSg4nY6K02ecyT0PPm/UZvtRpWrnBjcEVtHEJNp
bU9pLD5iZ0J9sbzPU/LxPmuAP2Bs8JmTn6aFRspFrP7W0s1Nmk2jsm0CgYEAyH0X
+jpoqxj4efZfkUrg5GbSEhf+dZglf0tTOA5bVg8IYwtmNk/pniLG/zI7c+GlTc9B
BwfMr59EzBq/eFMI7+LgXaVUsM/sS4Ry+yeK6SJx/otIMWtDfqxsLD8CPMCRvecC
2Pip4uSgrl0MOebl9XKp57GoaUWRWRHqwV4Y6h8CgYAZhI4mh4qZtnhKjY4TKDjx
QYufXSdLAi9v3FxmvchDwOgn4L+PRVdMwDNms2bsL0m5uPn104EzM6w1vzz1zwKz
5pTpPI0OjgWN13Tq8+PKvm/4Ga2MjgOgPWQkslulO/oMcXbPwWC3hcRdr9tcQtn9
Imf9n2spL/6EDFId+Hp/7QKBgAqlWdiXsWckdE1Fn91/NGHsc8syKvjjk1onDcw0
NvVi5vcba9oGdElJX3e9mxqUKMrw7msJJv1MX8LWyMQC5L6YNYHDfbPF1q5L4i8j
8mRex97UVokJQRRA452V2vCO6S5ETgpnad36de3MUxHgCOX3qL382Qx9/THVmbma
3YfRAoGAUxL/Eu5yvMK8SAt/dJK6FedngcM3JEFNplmtLYVLWhkIlNRGDwkg3I5K
y18Ae9n7dHVueyslrb6weq7dTkYDi3iOYRW8HRkIQh06wEdbxt0shTzAJvvCQfrB
jg/3747WSsf/zBTcHihTRBdAv6OmdhV4/dD5YBfLAkLrd+mX7iE=
-----END RSA PRIVATE KEY-----";
            var pubKey = @"-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAnzyis1ZjfNB0bBgKFMSv
vkTtwlvBsaJq7S5wA+kzeVOVpVWwkWdVha4s38XM/pa/yr47av7+z3VTmvDRyAHc
aT92whREFpLv9cj5lTeJSibyr/Mrm/YtjCZVWgaOYIhwrXwKLqPr/11inWsAkfIy
tvHWTxZYEcXLgAXFuUuaS3uF9gEiNQwzGTU1v0FqkqTBr4B8nW3HCN47XUu0t8Y0
e+lf4s4OxQawWD79J9/5d3Ry0vbV3Am1FtGJiJvOwRsIfVChDpYStTcHTCMqtvWb
V6L11BWkpzGXSW4Hv43qa+GSYOD2QU68Mb59oSk2OB+BtOLpJofmbGEGgvmwyCI9
MwIDAQAB
-----END PUBLIC KEY-----";

            var builder = new JwtBuilder
            {
                Algorithm = "RS384",
                Type = "JWT",

                IssuedAt = 1516239022.ToDateTime(),
                Expire = DateTime.MinValue,
                Secret = prvKey,
            };

            var token = builder.Encode(new { sub = "1234567890", name = "NewLife", admin = true });
            Assert.NotNull(token);
            Assert.NotEmpty(token);

            var ts = token.Split('.');
            Assert.Equal(3, ts.Length);
            Assert.Equal("eyJhbGciOiJSUzM4NCIsInR5cCI6IkpXVCJ9", ts[0]);
            Assert.Equal("eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6Ik5ld0xpZmUiLCJhZG1pbiI6dHJ1ZSwiaWF0IjoxNTE2MjM5MDIyfQ", ts[1]);
            Assert.Equal("g3j77PXjYeroUclchX5SSCyyv1HjEuJRsRf8UhovRIInn1JVwmemofWJTtIt9AOlymBZdA8k6zpjdDV_AfV7uxkUmCM4vIQarBPviZZY4yp-4PCqfsAIfPeTPkRHHJmcmDNyvVHFzNqNLsrzzSvSG7O3MeKYKbjqHb5rCu38AF0gqwvGh08WyY91rVwV22ipJN16DLyp2nk8SC0lqvGKyypsUwf70XxXo_6wvekSb9Vbh6c57_513BkjFR5fVjcpqOfaIB9Lkj_tKH1ze7hWU6_xfAyYYQ3jPFCivBtRVFFjB5PrKHku3Z0DhFsDiM6zOIAoHgwII-ry0wwxYd7LUg", ts[2]);

            var builder2 = new JwtBuilder
            {
                Algorithm = "RS384",

                Secret = pubKey,
            };
            var rs = builder2.TryDecode(token, out var msg);
            Assert.True(rs);
            Assert.Null(msg);

            Assert.Equal("JWT", builder2.Type);
            Assert.Equal("1234567890", builder2.Subject);
            Assert.Equal("NewLife", builder2["name"]);
            Assert.True(builder2["admin"].ToBoolean());
        }

        [Fact]
        public void RS512()
        {
            var prvKey = @"-----BEGIN RSA PRIVATE KEY-----
MIIEogIBAAKCAQEAnzyis1ZjfNB0bBgKFMSvvkTtwlvBsaJq7S5wA+kzeVOVpVWw
kWdVha4s38XM/pa/yr47av7+z3VTmvDRyAHcaT92whREFpLv9cj5lTeJSibyr/Mr
m/YtjCZVWgaOYIhwrXwKLqPr/11inWsAkfIytvHWTxZYEcXLgAXFuUuaS3uF9gEi
NQwzGTU1v0FqkqTBr4B8nW3HCN47XUu0t8Y0e+lf4s4OxQawWD79J9/5d3Ry0vbV
3Am1FtGJiJvOwRsIfVChDpYStTcHTCMqtvWbV6L11BWkpzGXSW4Hv43qa+GSYOD2
QU68Mb59oSk2OB+BtOLpJofmbGEGgvmwyCI9MwIDAQABAoIBACiARq2wkltjtcjs
kFvZ7w1JAORHbEufEO1Eu27zOIlqbgyAcAl7q+/1bip4Z/x1IVES84/yTaM8p0go
amMhvgry/mS8vNi1BN2SAZEnb/7xSxbflb70bX9RHLJqKnp5GZe2jexw+wyXlwaM
+bclUCrh9e1ltH7IvUrRrQnFJfh+is1fRon9Co9Li0GwoN0x0byrrngU8Ak3Y6D9
D8GjQA4Elm94ST3izJv8iCOLSDBmzsPsXfcCUZfmTfZ5DbUDMbMxRnSo3nQeoKGC
0Lj9FkWcfmLcpGlSXTO+Ww1L7EGq+PT3NtRae1FZPwjddQ1/4V905kyQFLamAA5Y
lSpE2wkCgYEAy1OPLQcZt4NQnQzPz2SBJqQN2P5u3vXl+zNVKP8w4eBv0vWuJJF+
hkGNnSxXQrTkvDOIUddSKOzHHgSg4nY6K02ecyT0PPm/UZvtRpWrnBjcEVtHEJNp
bU9pLD5iZ0J9sbzPU/LxPmuAP2Bs8JmTn6aFRspFrP7W0s1Nmk2jsm0CgYEAyH0X
+jpoqxj4efZfkUrg5GbSEhf+dZglf0tTOA5bVg8IYwtmNk/pniLG/zI7c+GlTc9B
BwfMr59EzBq/eFMI7+LgXaVUsM/sS4Ry+yeK6SJx/otIMWtDfqxsLD8CPMCRvecC
2Pip4uSgrl0MOebl9XKp57GoaUWRWRHqwV4Y6h8CgYAZhI4mh4qZtnhKjY4TKDjx
QYufXSdLAi9v3FxmvchDwOgn4L+PRVdMwDNms2bsL0m5uPn104EzM6w1vzz1zwKz
5pTpPI0OjgWN13Tq8+PKvm/4Ga2MjgOgPWQkslulO/oMcXbPwWC3hcRdr9tcQtn9
Imf9n2spL/6EDFId+Hp/7QKBgAqlWdiXsWckdE1Fn91/NGHsc8syKvjjk1onDcw0
NvVi5vcba9oGdElJX3e9mxqUKMrw7msJJv1MX8LWyMQC5L6YNYHDfbPF1q5L4i8j
8mRex97UVokJQRRA452V2vCO6S5ETgpnad36de3MUxHgCOX3qL382Qx9/THVmbma
3YfRAoGAUxL/Eu5yvMK8SAt/dJK6FedngcM3JEFNplmtLYVLWhkIlNRGDwkg3I5K
y18Ae9n7dHVueyslrb6weq7dTkYDi3iOYRW8HRkIQh06wEdbxt0shTzAJvvCQfrB
jg/3747WSsf/zBTcHihTRBdAv6OmdhV4/dD5YBfLAkLrd+mX7iE=
-----END RSA PRIVATE KEY-----";
            var pubKey = @"-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAnzyis1ZjfNB0bBgKFMSv
vkTtwlvBsaJq7S5wA+kzeVOVpVWwkWdVha4s38XM/pa/yr47av7+z3VTmvDRyAHc
aT92whREFpLv9cj5lTeJSibyr/Mrm/YtjCZVWgaOYIhwrXwKLqPr/11inWsAkfIy
tvHWTxZYEcXLgAXFuUuaS3uF9gEiNQwzGTU1v0FqkqTBr4B8nW3HCN47XUu0t8Y0
e+lf4s4OxQawWD79J9/5d3Ry0vbV3Am1FtGJiJvOwRsIfVChDpYStTcHTCMqtvWb
V6L11BWkpzGXSW4Hv43qa+GSYOD2QU68Mb59oSk2OB+BtOLpJofmbGEGgvmwyCI9
MwIDAQAB
-----END PUBLIC KEY-----";

            var builder = new JwtBuilder
            {
                Algorithm = "RS512",
                Type = "JWT",

                IssuedAt = 1516239022.ToDateTime(),
                Expire = DateTime.MinValue,
                Secret = prvKey,
            };

            var token = builder.Encode(new { sub = "1234567890", name = "NewLife", admin = true });
            Assert.NotNull(token);
            Assert.NotEmpty(token);

            var ts = token.Split('.');
            Assert.Equal(3, ts.Length);
            Assert.Equal("eyJhbGciOiJSUzUxMiIsInR5cCI6IkpXVCJ9", ts[0]);
            Assert.Equal("eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6Ik5ld0xpZmUiLCJhZG1pbiI6dHJ1ZSwiaWF0IjoxNTE2MjM5MDIyfQ", ts[1]);
            Assert.Equal("dCReA3hZnLnvXKIK7zqDp2Ej5by_ePnRNCrRwz83Gz92YZeeKkaTPY-orOmWskJO3L1Lh2F4Sv8H3KR6p0KI35LQ2qdF3xnPchHI3p6oDJYSBnNb0vCSVG5sYKoCWEjC932BbNmohiaa62YNFla6XPt6_d2pDoqg-D-Q5Jfrj_-1mMGZxj4lOyILkaKyohM_C1OiFN8hlHKihOBXS1ER27btQcggueAUWZPBa1fzRqCU10r9yQLmnJm9K3F9HPLCTy4xgdc5vpI8wLZ1ylHjkHMAYaJyBFrX9T70iaruiG3tW3VXzv4ptGcnd3oUd__V9m_DHrfNFYh1Gy0a8JeeLQ", ts[2]);

            var builder2 = new JwtBuilder
            {
                Algorithm = "RS512",

                Secret = pubKey,
            };
            var rs = builder2.TryDecode(token, out var msg);
            Assert.True(rs);
            Assert.Null(msg);

            Assert.Equal("JWT", builder2.Type);
            Assert.Equal("1234567890", builder2.Subject);
            Assert.Equal("NewLife", builder2["name"]);
            Assert.True(builder2["admin"].ToBoolean());
        }

        [Fact]
        public void ES256()
        {
            var prvKey = @"-----BEGIN PRIVATE KEY-----
MIGHAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBG0wawIBAQQgevZzL1gdAFr88hb2
OF/2NxApJCzGCEDdfSp6VQO30hyhRANCAAQRWz+jn65BtOMvdyHKcvjBeBSDZH2r
1RTwjmYSi9R/zpBnuQ4EiMnCqfMPWiZqB4QdbAd0E7oH50VpuZ1P087G
-----END PRIVATE KEY-----";
            var pubKey = @"-----BEGIN PUBLIC KEY-----
MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEEVs/o5+uQbTjL3chynL4wXgUg2R9
q9UU8I5mEovUf86QZ7kOBIjJwqnzD1omageEHWwHdBO6B+dFabmdT9POxg==
-----END PUBLIC KEY-----";

            var builder = new JwtBuilder
            {
                Algorithm = "ES256",
                Type = "JWT",

                IssuedAt = 1516239022.ToDateTime(),
                Expire = DateTime.MinValue,
                Secret = prvKey,
            };

            var token = builder.Encode(new { sub = "1234567890", name = "NewLife", admin = true });
            Assert.NotNull(token);
            Assert.NotEmpty(token);

            var ts = token.Split('.');
            Assert.Equal(3, ts.Length);
            Assert.Equal("eyJhbGciOiJFUzI1NiIsInR5cCI6IkpXVCJ9", ts[0]);
            Assert.Equal("eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6Ik5ld0xpZmUiLCJhZG1pbiI6dHJ1ZSwiaWF0IjoxNTE2MjM5MDIyfQ", ts[1]);
            //Assert.Equal("xyCWz7tNjH4UUkxi7BqlWE4V857XA6SYC-ZFukvexvIgsGQt9SBcpdglz3NfhhrslOwF7HzWZHOJu3RrIFrDFA", ts[2]);

            var builder2 = new JwtBuilder
            {
                Algorithm = "ES256",

                Secret = pubKey,
            };
            var rs = builder2.TryDecode(token, out var msg);
            Assert.True(rs);
            Assert.Null(msg);

            Assert.Equal("JWT", builder2.Type);
            Assert.Equal("1234567890", builder2.Subject);
            Assert.Equal("NewLife", builder2["name"]);
            Assert.True(builder2["admin"].ToBoolean());
        }

        [Fact]
        public void ES384()
        {
            var prvKey = @"RUNTMiAAAAAoECDSEE7PqKvRx+FXWXhpTXIm/ZquCKDa6UXA9+PMQRugM35vcgKAXR2pelQ2SqYjOFktBMm84x194VyepthORPQDRkEIcGIonNbCtCg+Y62sV9prPsXACNS//2huX38=";
            var pubKey = @"RUNTMSAAAAAoECDSEE7PqKvRx+FXWXhpTXIm/ZquCKDa6UXA9+PMQRugM35vcgKAXR2pelQ2SqYjOFktBMm84x194VyepthO";

            var builder = new JwtBuilder
            {
                Algorithm = "ES384",
                Type = "JWT",

                IssuedAt = 1516239022.ToDateTime(),
                Expire = DateTime.MinValue,
                Secret = prvKey,
            };

            var token = builder.Encode(new { sub = "1234567890", name = "NewLife", admin = true });
            Assert.NotNull(token);
            Assert.NotEmpty(token);

            var ts = token.Split('.');
            Assert.Equal(3, ts.Length);
            Assert.Equal("eyJhbGciOiJFUzM4NCIsInR5cCI6IkpXVCJ9", ts[0]);
            Assert.Equal("eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6Ik5ld0xpZmUiLCJhZG1pbiI6dHJ1ZSwiaWF0IjoxNTE2MjM5MDIyfQ", ts[1]);
            //Assert.Equal("xyCWz7tNjH4UUkxi7BqlWE4V857XA6SYC-ZFukvexvIgsGQt9SBcpdglz3NfhhrslOwF7HzWZHOJu3RrIFrDFA", ts[2]);

            var builder2 = new JwtBuilder
            {
                Algorithm = "ES384",

                Secret = pubKey,
            };
            var rs = builder2.TryDecode(token, out var msg);
            Assert.True(rs);
            Assert.Null(msg);

            Assert.Equal("JWT", builder2.Type);
            Assert.Equal("1234567890", builder2.Subject);
            Assert.Equal("NewLife", builder2["name"]);
            Assert.True(builder2["admin"].ToBoolean());
        }

        [Fact]
        public void ES512()
        {
            var prvKey = @"-----BEGIN PRIVATE KEY-----
MIGHAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBG0wawIBAQQgevZzL1gdAFr88hb2
OF/2NxApJCzGCEDdfSp6VQO30hyhRANCAAQRWz+jn65BtOMvdyHKcvjBeBSDZH2r
1RTwjmYSi9R/zpBnuQ4EiMnCqfMPWiZqB4QdbAd0E7oH50VpuZ1P087G
-----END PRIVATE KEY-----";
            var pubKey = @"-----BEGIN PUBLIC KEY-----
MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEEVs/o5+uQbTjL3chynL4wXgUg2R9
q9UU8I5mEovUf86QZ7kOBIjJwqnzD1omageEHWwHdBO6B+dFabmdT9POxg==
-----END PUBLIC KEY-----";

            var builder = new JwtBuilder
            {
                Algorithm = "ES512",
                Type = "JWT",

                IssuedAt = 1516239022.ToDateTime(),
                Expire = DateTime.MinValue,
                Secret = prvKey,
            };

            var token = builder.Encode(new { sub = "1234567890", name = "NewLife", admin = true });
            Assert.NotNull(token);
            Assert.NotEmpty(token);

            var ts = token.Split('.');
            Assert.Equal(3, ts.Length);
            Assert.Equal("eyJhbGciOiJFUzUxMiIsInR5cCI6IkpXVCJ9", ts[0]);
            Assert.Equal("eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6Ik5ld0xpZmUiLCJhZG1pbiI6dHJ1ZSwiaWF0IjoxNTE2MjM5MDIyfQ", ts[1]);
            //Assert.Equal("xyCWz7tNjH4UUkxi7BqlWE4V857XA6SYC-ZFukvexvIgsGQt9SBcpdglz3NfhhrslOwF7HzWZHOJu3RrIFrDFA", ts[2]);

            var builder2 = new JwtBuilder
            {
                Algorithm = "ES512",

                Secret = pubKey,
            };
            var rs = builder2.TryDecode(token, out var msg);
            Assert.True(rs);
            Assert.Null(msg);

            Assert.Equal("JWT", builder2.Type);
            Assert.Equal("1234567890", builder2.Subject);
            Assert.Equal("NewLife", builder2["name"]);
            Assert.True(builder2["admin"].ToBoolean());
        }
    }
}