using System;
using System.Security.Cryptography;
using NewLife;
using NewLife.Security;
using Xunit;

namespace XUnitTest.Security
{
    public class RSAHelperTests
    {
        [Fact]
        public void TestPublicPem()
        {
            var key = @"-----BEGIN PUBLIC KEY-----
MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQDpsDr+W45aFHIkvotZaGK/THlF
FpuZfUtghhWkHAm3H7yvL42J4xHrTr6IeUDCl4eKe6qiIgvYSNoL3u4SERGOeYmV
1F+cocu9IMGnNoicbh1zVW6e8/iGT3xaYQizJoVuWA/TC/zdds2ihCJfHDBDsouO
CXecPapyWCGQNsH5sQIDAQAB
-----END PUBLIC KEY-----";

            var p = RSAHelper.ReadPem(key);
            var rsa = new RSACryptoServiceProvider();
            rsa.ImportParameters(p);

            var pubKey = rsa.ToXmlString(false);
            Assert.Equal("<RSAKeyValue><Modulus>6bA6/luOWhRyJL6LWWhiv0x5RRabmX1LYIYVpBwJtx+8ry+NieMR606+iHlAwpeHinuqoiIL2EjaC97uEhERjnmJldRfnKHLvSDBpzaInG4dc1VunvP4hk98WmEIsyaFblgP0wv83XbNooQiXxwwQ7KLjgl3nD2qclghkDbB+bE=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>", pubKey);

            var rs = rsa.VerifyData("NewLife".GetBytes(), MD5.Create(), "WfMouV+yZ0EmATNiFVsgMIsMzx1sS7zSKcOZ1FmSiUnkq7nB4wEKcketdakn859/pTWZ31l8XF1+GelhdNHjwjuQmsawdTW+imnn5Z1J+XzhNgxdnpJ6O1txcE8oHKCTd2bS2Yv55Mezu4Ih9BbX0JovSnFCsGMxLS6afYQqXUU=".ToBase64());
            Assert.True(rs);
        }

        [Fact]
        public void TestPrivatePem()
        {
            var key = @"-----BEGIN RSA PRIVATE KEY-----
MIICXQIBAAKBgQDpsDr+W45aFHIkvotZaGK/THlFFpuZfUtghhWkHAm3H7yvL42J
4xHrTr6IeUDCl4eKe6qiIgvYSNoL3u4SERGOeYmV1F+cocu9IMGnNoicbh1zVW6e
8/iGT3xaYQizJoVuWA/TC/zdds2ihCJfHDBDsouOCXecPapyWCGQNsH5sQIDAQAB
AoGBAM/JbFs4y5WbMncrmjpQj+UrOXVOCeLrvrc/4kQ+zgCvTpWywbaGWiuRo+cz
cXrVQ6bGGU362e9hr8f4XFViKemDL4SmJbgSDa1K71i+/LnnzF6sjiDBFQ/jA9SK
4PYrY7a3IkeBQnJmknanykugyQ1xmCjbuh556fOeRPaHnhx1AkEA/flrxJSy1Z+n
Y1RPgDOeDqyG6MhwU1Jl0yJ1sw3Or4qGRXhjTeGsCrKqV0/ajqdkDEM7FNkqnmsB
+vPd116J6wJBAOuNY3oOWvy2fQ32mj6XV+S2vcG1osEUaEuWvEgkGqJ9co6100Qp
j15036AQEEDqbjdqS0ShfeRSwevTJZIap9MCQCeMGDDjKrnDA5CfB0YiQ4FrchJ7
a6o90WdAHW3FP6LsAh59MZFmC6Ea0xWHdLPz8stKCMAlVNKYPRWztZ6ctQMCQQC8
iWbeAy+ApvBhhMjg4HJRdpNbwO6MbLEuD3CUrZFEDfTrlU2MeVdv20xC6ZiY3Qtq
/4FPZZNGdZcSEuc3km5RAkApGkZmWetNwDJMcUJbSBrQMFfrQObqMPBPe+gEniQq
Ttwu1OULHlmUg9eW31wRI2uiXcFCJMHuro6iOQ1VJ4Qs
-----END RSA PRIVATE KEY-----";

            var p = RSAHelper.ReadPem(key);
            var rsa = new RSACryptoServiceProvider();
            rsa.ImportParameters(p);

            var pubKey = rsa.ToXmlString(true);
            Assert.Equal("<RSAKeyValue><Modulus>6bA6/luOWhRyJL6LWWhiv0x5RRabmX1LYIYVpBwJtx+8ry+NieMR606+iHlAwpeHinuqoiIL2EjaC97uEhERjnmJldRfnKHLvSDBpzaInG4dc1VunvP4hk98WmEIsyaFblgP0wv83XbNooQiXxwwQ7KLjgl3nD2qclghkDbB+bE=</Modulus><Exponent>AQAB</Exponent><P>/flrxJSy1Z+nY1RPgDOeDqyG6MhwU1Jl0yJ1sw3Or4qGRXhjTeGsCrKqV0/ajqdkDEM7FNkqnmsB+vPd116J6w==</P><Q>641jeg5a/LZ9DfaaPpdX5La9wbWiwRRoS5a8SCQaon1yjrXTRCmPXnTfoBAQQOpuN2pLRKF95FLB69Mlkhqn0w==</Q><DP>J4wYMOMqucMDkJ8HRiJDgWtyEntrqj3RZ0AdbcU/ouwCHn0xkWYLoRrTFYd0s/Pyy0oIwCVU0pg9FbO1npy1Aw==</DP><DQ>vIlm3gMvgKbwYYTI4OByUXaTW8DujGyxLg9wlK2RRA3065VNjHlXb9tMQumYmN0Lav+BT2WTRnWXEhLnN5JuUQ==</DQ><InverseQ>KRpGZlnrTcAyTHFCW0ga0DBX60Dm6jDwT3voBJ4kKk7cLtTlCx5ZlIPXlt9cESNrol3BQiTB7q6OojkNVSeELA==</InverseQ><D>z8lsWzjLlZsydyuaOlCP5Ss5dU4J4uu+tz/iRD7OAK9OlbLBtoZaK5Gj5zNxetVDpsYZTfrZ72Gvx/hcVWIp6YMvhKYluBINrUrvWL78uefMXqyOIMEVD+MD1Irg9itjtrciR4FCcmaSdqfKS6DJDXGYKNu6Hnnp855E9oeeHHU=</D></RSAKeyValue>", pubKey);

            var sign = rsa.SignData("NewLife".GetBytes(), MD5.Create());
            Assert.Equal("WfMouV+yZ0EmATNiFVsgMIsMzx1sS7zSKcOZ1FmSiUnkq7nB4wEKcketdakn859/pTWZ31l8XF1+GelhdNHjwjuQmsawdTW+imnn5Z1J+XzhNgxdnpJ6O1txcE8oHKCTd2bS2Yv55Mezu4Ih9BbX0JovSnFCsGMxLS6afYQqXUU=", sign.ToBase64());
        }

        [Fact]
        public void TestPem()
        {
            var prvKey = @"-----BEGIN PRIVATE KEY-----
MIICdgIBADANBgkqhkiG9w0BAQEFAASCAmAwggJcAgEAAoGBAMu4IDG1XU6a7bXo
4V1jSnbKk9Eum2WguAyq+maCRcP9JoHlE/HmhPOjl91aN5gDHw3pgB7QMMkPkuyY
0aG9UiIo7PbBgjXsNBErprKa8G7GKhDN4B3m8jxEi1NLtCk2H8AEf8H/deGFXCde
fjQx0NDEDcTbJ8STfbsqrLhOq2xzAgMBAAECgYEAg1kZMNOd8IOFxqb7P2o4ZbUh
b1rciL8CS/CleBiAgOgkvtWDcZFOoYQV83sqoxFIIYEuwS88dTZcZb32U5EsdYEx
JvJwAAYnzpch/YAz0llvXSHzZwNfGGvs4qt0d74bFpPfveli82wSKMlykeajP2Ro
RQpOniTYOWrJ01UHdUECQQDt1KTj/Xs5BNmEZAkJVmGekQROADk+ztceAe9UMj/J
s5xECdXVwuFh2Rm62MMQNNoW2Pjz4Y5NqhjRu0MMZnlTAkEA20hZsgA78aqTO7s+
+y/CLgP3Cd7uG/5RkcmjBWq2eXkt6wmazZl0BMYb7vshblnMjFXJwuOmfBJl7rTr
1fg8YQJAEo4Jg0QObgdj1QFc9x6HJTDZLiC0VqMag1vRSTdWZK0fnutJhJDctp6S
dFJe/Y+yCCBLY/OP/50qrIo4k+oWwwJAIn8hTTVoOL6C5xSv9cgvnhmVlYHyp4i8
wFieQs3k4vtDVARwzANmExIvdssfGUMbQMCGOxihKkeirYjcyQ6CQQJAbsbpzCjD
wd9JCogmTu/xYqtL898ek7LeNkhgIY2KhYtlptxlHfzgLBUgiSTNTcD1YWtSSp6u
A5ImxrryDYPmfg==
-----END PRIVATE KEY-----";
            var pubKey = @"-----BEGIN PUBLIC KEY-----
MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQDLuCAxtV1Omu216OFdY0p2ypPR
LptloLgMqvpmgkXD/SaB5RPx5oTzo5fdWjeYAx8N6YAe0DDJD5LsmNGhvVIiKOz2
wYI17DQRK6aymvBuxioQzeAd5vI8RItTS7QpNh/ABH/B/3XhhVwnXn40MdDQxA3E
2yfEk327Kqy4TqtscwIDAQAB
-----END PUBLIC KEY-----";

            var prvRsa = new RSACryptoServiceProvider();
            var prv = RSAHelper.ReadPem(prvKey);
            prvRsa.ImportParameters(prv);
            var key1 = prvRsa.ToXmlString(false);

            var pubRsa = new RSACryptoServiceProvider();
            var pub = RSAHelper.ReadPem(pubKey);
            pubRsa.ImportParameters(pub);
            var key2 = pubRsa.ToXmlString(false);

            Assert.Equal(key1, key2);
        }
    }
}