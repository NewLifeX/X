using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Data;
using NewLife.Http;
using NewLife.Model;
using NewLife.Net;
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

        [Theory(DisplayName = "读取编码")]
        [InlineData("GET /123.html HTTP/1.1\r\nHost: www.newlifex.com\r\n\r\n", null)]
        [InlineData("POST /123.ashx HTTP/1.1\r\nHost: www.newlifex.com\r\nContent-Length:9\r\n\r\ncode=abcd", null)]
        [InlineData("POST /123.ashx HTTP/1.1\r\nHost: www.newlifex.com\r\nContent-Length:9\r\n\r\n", "code=abcd")]
        public void ReadCodec(String http, String http2)
        {
            var pk = new Packet(http.GetBytes());
            var pk2 = new Packet(http2.GetBytes());

            //var msg = new HttpMessage();
            //var rs = msg.Read(pk);
            //Assert.True(rs);

            var context = new MyHandlerContext
            {
                Owner = new HandlerContext()
            };

            var codec = new HttpCodec();
            var rm = codec.Read(context, pk);
            Assert.Null(rm);
            if (pk2.Total > 0) rm = codec.Read(context, pk2);
            Assert.Null(rm);

            var context2 = new MyHandlerContext
            {
                Owner = new HandlerContext(),
                AllowParseHeader = true,
            };

            var codec2 = new HttpCodec { AllowParseHeader = true };
            var rm2 = codec2.Read(context2, pk);
            Assert.Null(rm2);
            if (pk2.Total > 0) rm2 = codec2.Read(context2, pk2);
            Assert.Null(rm2);

            var rs = context2.Result;
            Assert.NotNull(rs);

            var str = rs.ToPacket().ToStr();
            Assert.Equal(http + http2, str);
        }

        class MyHandlerContext : HandlerContext
        {
            public HttpMessage Result { get; set; }

            public Boolean AllowParseHeader { get; set; }

            public override void FireRead(Object message)
            {
                Assert.NotNull(message);

                var msg = message as HttpMessage;
                Assert.NotNull(msg);

                Result = msg;

                if (msg.Method == "POST")
                {
                    Assert.Equal("/123.ashx", msg.Uri);
                    Assert.Equal(9, msg.ContentLength);
                }
                else
                {
                    if (!AllowParseHeader)
                    {
                        Assert.Null(msg.Method);

                        var rs = msg.ParseHeaders();
                        Assert.True(rs);
                    }

                    Assert.Equal("GET", msg.Method);

                    Assert.Equal("/123.html", msg.Uri);
                    Assert.Equal(-1, msg.ContentLength);
                }
            }
        }
    }
}
