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
    }
}
