using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Model;
using NewLife.Security;
using Xunit;

namespace XUnitTest.Model
{
    public class PipelineTests
    {
        class MyHandler : Handler
        {
            public Int32 Value { get; set; }

            public override Object Read(IHandlerContext context, Object message)
            {
                if (message is IList<Int32> list) list.Add(Value);

                return base.Read(context, message);
            }

            public override Object Write(IHandlerContext context, Object message)
            {
                if (message is IList<Int32> list) list.Add(Value);

                return base.Write(context, message);
            }
        }

        [Fact]
        public void AddTest()
        {
            var pp = new Pipeline();

            var h1 = new MyHandler();
            var h2 = new MyHandler();
            var h3 = new MyHandler();

            pp.Add(h1);
            pp.Add(h3);
            pp.Add(h2);

            Assert.Equal(3, pp.Handlers.Count);
            Assert.Equal(h1, pp.Handlers[0]);
            Assert.Equal(h2, pp.Handlers[2]);
            Assert.Equal(h3, pp.Handlers[1]);

            Assert.Null(h1.Prev);
            Assert.Equal(h3, h1.Next);

            Assert.Equal(h1, h3.Prev);
            Assert.Equal(h2, h3.Next);

            Assert.Equal(h3, h2.Prev);
            Assert.Null(h2.Next);
        }

        [Fact]
        public void ReadWrite()
        {
            var pp = new Pipeline();

            var h1 = new MyHandler { Value = 1 };
            var h2 = new MyHandler { Value = 2 };
            var h3 = new MyHandler { Value = 3 };

            pp.Add(h1);
            pp.Add(h2);
            pp.Add(h3);

            var list = new List<Int32>();
            var rs = pp.Read(null, list);
            Assert.Equal(list, rs);
            Assert.Equal(1, list[0]);
            Assert.Equal(2, list[1]);
            Assert.Equal(3, list[2]);

            var list2 = new List<Int32>();
            var rs2 = pp.Write(null, list2);
            Assert.Equal(list2, rs2);
            Assert.Equal(3, list2[0]);
            Assert.Equal(2, list2[1]);
            Assert.Equal(1, list2[2]);
        }
    }
}