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
        public void StringMessage()
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
        public void StringMessageNoFlag()
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
    }
}