using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Messaging;
using Xunit;
using NewLife;

namespace XUnitTest.Messaging
{
    public class DefaultMessageTests
    {
        [Fact]
        public void StringEncode()
        {
            var msg = new DefaultMessage
            {
                Sequence = 1,
                Payload = "Open".GetBytes(),
            };
            var str = msg.Encode();
            Assert.Equal("4,1,1:Open", str);

            var msg2 = new DefaultMessage
            {
                Reply = true,
                Sequence = 1,
                Payload = "执行成功".GetBytes(),
            };
            var str2 = msg2.Encode();
            Assert.Equal("12,1,129:执行成功", str2);
        }

        [Fact]
        public void StringEncodeNoFlag()
        {
            var msg = new DefaultMessage
            {
                Sequence = 1,
                Payload = "Open".GetBytes(),
            };
            var str = msg.Encode(false);
            Assert.Equal("4,1:Open", str);

            var msg2 = new DefaultMessage
            {
                Reply = true,
                Sequence = 1,
                Payload = "执行成功".GetBytes(),
            };
            var str2 = msg2.Encode(false);
            Assert.Equal("12,1:执行成功", str2);
        }

        [Fact]
        public void StringDecode()
        {
            {
                var msg = new DefaultMessage();
                var rs = msg.Decode("4,1,1:Open");
                Assert.True(rs);
                Assert.Equal(1, msg.Sequence);
                Assert.Equal(1, msg.Flag);
                Assert.Equal("Open", msg.Payload.ToStr());
            }

            {
                var msg = new DefaultMessage();
                var rs = msg.Decode("12,1,129:执行成功");
                Assert.True(rs);
                Assert.Equal(1, msg.Sequence);
                Assert.Equal(0x81, msg.Flag);
                Assert.Equal("执行成功", msg.Payload.ToStr());
            }

            {
                var msg = new DefaultMessage();
                var rs = msg.Decode("12,1,129:执行成功".GetBytes());
                Assert.True(rs);
                Assert.Equal(1, msg.Sequence);
                Assert.Equal(0x81, msg.Flag);
                Assert.Equal("执行成功", msg.Payload.ToStr());
            }
        }
    }
}