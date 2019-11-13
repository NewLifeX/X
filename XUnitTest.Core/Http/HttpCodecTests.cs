using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Data;
using NewLife.Http;
using Xunit;

namespace XUnitTest.Http
{
    public class HttpCodecTests
    {
        [Theory(DisplayName = "读取GET")]
        [InlineData("GET /123.html HTTP/1.1\r\nHost: www.newlifex.com\r\n\r\n")]
        [InlineData("GET /123.html HTTP/1.1\r\nHost: www.newlifex.com\r\nContent-Length:0\r\n\r\n")]
        //[InlineData("GET /123.html\r\nHost: www.newlifex.com\r\n")]
        //[InlineData("GET /123.html\r\nHost: www.newlifex.com")]
        public void ReadGetMessge(String http)
        {
            var msg = new HttpMessage();
            var rs = msg.Read(http.GetBytes());
            Assert.True(rs);

            rs = msg.ParseHeaders();
            Assert.True(rs);

            Assert.Equal("GET", msg.Method);
            Assert.Equal("/123.html", msg.Uri);
            Assert.Equal("www.newlifex.com", msg.Headers["host"]);
        }

        [Theory(DisplayName = "读取POST")]
        [InlineData("POST /123.ashx HTTP/1.1\r\nHost: www.newlifex.com\r\nContent-Length:9\r\n\r\ncode=abcd")]
        [InlineData("POST /123.ashx HTTP/1.1\r\nHost: www.newlifex.com\r\nContent-Length:0\r\n\r\n")]
        [InlineData("POST /123.ashx HTTP/1.1\r\nHost: www.newlifex.com\r\n\r\n")]
        public void ReadPostMessage(String http)
        {
            var msg = new HttpMessage();
            var rs = msg.Read(http.GetBytes());
            Assert.True(rs);

            rs = msg.ParseHeaders();
            Assert.True(rs);

            Assert.Equal("POST", msg.Method);
            Assert.Equal("/123.ashx", msg.Uri);
            Assert.Equal("www.newlifex.com", msg.Headers["host"]);

            var body = msg.Payload;
            Assert.NotNull(body);
            if (body.Total == 9)
            {
                var str = body.ToStr();
                Assert.Equal("code=abcd", str);

                Assert.Equal(body.Total, msg.ContentLength);
            }
            else
            {
                if (msg.Headers.ContainsKey("Content-Length"))
                    Assert.Equal(body.Total, msg.ContentLength);
                else
                    Assert.Equal(-1, msg.ContentLength);
            }
        }

        [Theory(DisplayName = "写入编码")]
        [InlineData("GET /123.html HTTP/1.1\r\nHost: www.newlifex.com\r\n\r\n")]
        [InlineData("POST /123.ashx HTTP/1.1\r\nHost: www.newlifex.com\r\nContent-Length:9\r\n\r\ncode=abcd")]
        public void WriteCodec(String http)
        {
            var pk = http.GetBytes();
            var msg = new HttpMessage();
            var rs = msg.Read(pk);
            Assert.True(rs);

            var codec = new HttpCodec();
            var rm = codec.Write(null, msg) as Packet;
            Assert.NotNull(rm);
            Assert.Equal(http, rm.ToStr());
        }
    }
}
