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
        public void BinaryEncode()
        {
            var msg = new DefaultMessage
            {
                Sequence = 1,
                Payload = "Open".GetBytes(),
            };
            var pk = msg.ToPacket();
            Assert.Equal(1, pk[0]);
            Assert.Equal(1, pk[1]);
            Assert.Equal(4, pk[2]);
            Assert.Equal(0, pk[3]);
            Assert.Equal("Open", pk.Slice(4).ToStr());

            var msgd = new DefaultMessage();
            var rs = msgd.Read(pk);
            Assert.True(rs);
            Assert.Equal(msg.Flag, msgd.Flag);
            Assert.Equal(msg.Sequence, msgd.Sequence);
            Assert.Equal(msg.Payload.ToStr(), msgd.Payload.ToStr());

            var msg2 = new DefaultMessage
            {
                Reply = true,
                Sequence = 1,
                Payload = "执行成功".GetBytes(),
            };
            var pk2 = msg2.ToPacket();
            Assert.Equal(0x81, pk2[0]);
            Assert.Equal(1, pk2[1]);
            Assert.Equal(12, pk2[2]);
            Assert.Equal(0, pk2[3]);
            Assert.Equal("执行成功", pk2.Slice(4).ToStr());

            var msgd2 = new DefaultMessage();
            var rs2 = msgd2.Read(pk2);
            Assert.True(rs2);
            Assert.Equal(msg2.Flag, msgd2.Flag);
            Assert.Equal(msg2.Sequence, msgd2.Sequence);
            Assert.Equal(msg2.Payload.ToStr(), msgd2.Payload.ToStr());
        }

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
            var str = msg.Encode(null, false);
            Assert.Equal("4,1:Open", str);

            var msg2 = new DefaultMessage
            {
                Reply = true,
                Sequence = 1,
                Payload = "执行成功".GetBytes(),
            };
            var str2 = msg2.Encode(null, false);
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