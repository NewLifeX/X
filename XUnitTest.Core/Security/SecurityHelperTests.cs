using System;
using System.Security.Cryptography;
using System.Text;
using NewLife;
using Xunit;

namespace XUnitTest.Security
{
    public class SecurityHelperTests
    {
        [Fact]
        public void DES_Test()
        {
            var text = "111111";
            var key = "16621235";

            var des = DES.Create();
            var text2 = des.Encrypt(text.GetBytes(), key.GetBytes(), CipherMode.ECB).ToBase64();
            Assert.Equal("kgAdQRZ6w20=", text2);

            var des2 = DES.Create();
            var text3 = des2.Decrypt(text2.ToBase64(), key.GetBytes(), CipherMode.ECB).ToStr();
            Assert.Equal(text, text3);
        }

        [Fact]
        public void AES_CBC_Test()
        {
            var buf = "123456".ToHex();
            var key = "86CAD727DEB54263B73960AA79C5D9B7".ToHex();
            var data = "";

            // CBC加密解密
            {
                var aes = Aes.Create();
                data = aes.Encrypt(buf, key, CipherMode.CBC, PaddingMode.PKCS7).ToHex();

                Assert.Equal("50A7CF869354EC317327671B34543AD8", data);
            }
            {
                var aes = Aes.Create();
                data = aes.Decrypt(data.ToHex(), key, CipherMode.CBC, PaddingMode.PKCS7).ToHex();

                Assert.Equal("123456", data);
            }
        }

        [Fact]
        public void AES_CBC_Test2()
        {
            var buf = "123456".ToHex();
            var key = "86CAD727DEB54263B73960AA79C5D9B7".ToHex();
            var data = "";

            // 直接CBC加密解密
            {
                // CBC需要两边一致的IV
                var aes = Aes.Create();
                aes.Key = key;
                aes.IV = key;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                var transform = aes.CreateEncryptor();
                data = transform.TransformFinalBlock(buf, 0, buf.Length).ToHex();

                Assert.Equal("50A7CF869354EC317327671B34543AD8", data);
            }
            {
                buf = data.ToHex();
                var aes = Aes.Create();
                aes.Key = key;
                aes.IV = key;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                var transform = aes.CreateDecryptor();
                data = transform.TransformFinalBlock(buf, 0, buf.Length).ToHex();

                Assert.Equal("123456", data);
            }
        }

        [Fact]
        public void AES_ECB_Test()
        {
            var buf = "123456".ToHex();
            var key = "86CAD727DEB54263B73960AA79C5D9B7".ToHex();
            var data = "";

            // ECB加密解密
            {
                var aes = Aes.Create();
                data = aes.Encrypt(buf, key, CipherMode.ECB, PaddingMode.PKCS7).ToHex();

                Assert.Equal("C0041698AD73EADC75FB886CEF6385A5", data);
            }
            {
                var aes = Aes.Create();
                data = aes.Decrypt(data.ToHex(), key, CipherMode.ECB, PaddingMode.PKCS7).ToHex();

                Assert.Equal("123456", data);
            }
        }

        [Fact]
        public void AES_ECB_Test2()
        {
            var buf = "123456".ToHex();
            var key = "86CAD727DEB54263B73960AA79C5D9B7".ToHex();
            var data = "";

            // 直接ECB加密解密
            {
                var aes = Aes.Create();
                aes.Key = key;
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.PKCS7;
                var transform = aes.CreateEncryptor();
                data = transform.TransformFinalBlock(buf, 0, buf.Length).ToHex();

                Assert.Equal("C0041698AD73EADC75FB886CEF6385A5", data);
            }
            {
                buf = data.ToHex();
                var aes = Aes.Create();
                aes.Key = key;
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.PKCS7;
                var transform = aes.CreateDecryptor();
                //data = transform.TransformFinalBlock(buf, 0, buf.Length).ToHex();

                var buf2 = new Byte[buf.Length];
                var rs = transform.TransformBlock(buf, 0, buf.Length, buf2, 0);
                var buf3 = transform.TransformFinalBlock(new Byte[1024], 0, 0);
                data = buf3.ToHex();

                Assert.Equal("123456", data);
            }
            {
                var aes = Aes.Create();
                data = aes.Decrypt(buf, key, CipherMode.ECB, PaddingMode.PKCS7).ToHex();

                Assert.Equal("123456", data);
            }
        }
    }
}