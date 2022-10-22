using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife;
using NewLife.Data;
using Xunit;

namespace XUnitTest.IO
{
    public class IOHelperTests
    {
        [Fact]
        public void IndexOf()
        {
            var d = "------WebKitFormBoundary3ZXeqQWNjAzojVR7".GetBytes();

            var buf = new Byte[8 * 1024 * 1024];
            buf.Write(7 * 1024 * 1024, d);

            var p = buf.IndexOf(d);
            Assert.Equal(7 * 1024 * 1024, p);

            p = buf.IndexOf(d, 7 * 1024 * 1024 - 1);
            Assert.Equal(7 * 1024 * 1024, p);

            p = buf.IndexOf(d, 7 * 1024 * 1024 + 1);
            Assert.Equal(-1, p);
        }

        private static readonly Byte[] NewLine2 = new[] { (Byte)'\r', (Byte)'\n', (Byte)'\r', (Byte)'\n' };
        [Fact]
        public void IndexOf2()
        {
            var str = "Content-Disposition: form-data; name=\"name\"\r\n\r\n大石头";

            var buf = str.GetBytes();

            var p = buf.IndexOf("\r\n\r\n".GetBytes());
            Assert.Equal(43, p);

            p = buf.IndexOf(NewLine2);
            Assert.Equal(43, p);

            var pk = new Packet(buf);

            var value = pk.Slice(p + 4).ToStr();
            Assert.Equal("大石头", value);
        }

        [Fact]
        public void ToHex()
        {
            var buf = "NewLife".GetBytes();
            var hex = buf.ToHex();

            Assert.Equal("4E65774C696665", hex);

            hex = buf.ToHex("-");
            Assert.Equal("4E-65-77-4C-69-66-65", hex);

            hex = buf.ToHex("-", 4, 6);
            Assert.Equal("4E65774C-6966", hex);
        }

        [Fact]
        public void Swap()
        {
            var data = "12345678";

            var buf = data.ToHex().Swap(false, false);
            Assert.Equal("12345678", buf.ToHex());

            buf = data.ToHex().Swap(false, true);
            Assert.Equal("56781234", buf.ToHex());

            buf = data.ToHex().Swap(true, false);
            Assert.Equal("34127856", buf.ToHex());

            buf = data.ToHex().Swap(true, true);
            Assert.Equal("78563412", buf.ToHex());
        }

        [Fact]
        public void Swap64()
        {
            var data = "12345678AABBCCDD";

            var buf = data.ToHex().Swap(false, false);
            Assert.Equal("12345678AABBCCDD", buf.ToHex());

            buf = data.ToHex().Swap(false, true);
            Assert.Equal("56781234CCDDAABB", buf.ToHex());

            buf = data.ToHex().Swap(true, false);
            Assert.Equal("34127856BBAADDCC", buf.ToHex());

            buf = data.ToHex().Swap(true, true);
            Assert.Equal("78563412DDCCBBAA", buf.ToHex());
        }

        [Fact]
        public void ToBase64()
        {
            var buf = "Stone".GetBytes();

            var b64 = buf.ToBase64();
            Assert.Equal("U3RvbmU=", b64);

            b64 = buf.ToUrlBase64();
            Assert.Equal("U3RvbmU", b64);

            var buf2 = b64.ToBase64();
            Assert.Equal(buf.ToHex(), buf2.ToHex());

            var buf3 = (b64 + Environment.NewLine + " ").ToBase64();
            Assert.Equal(buf.ToHex(), buf3.ToHex());
        }
    }
}
