using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife;
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
    }
}
