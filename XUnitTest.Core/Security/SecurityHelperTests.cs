using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using NewLife;

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
    }
}