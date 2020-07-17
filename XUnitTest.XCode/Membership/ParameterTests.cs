using System;
using System.Collections.Generic;
using NewLife.Security;
using XCode.Membership;
using Xunit;
using NewLife;

namespace XUnitTest.XCode.Membership
{
    public class ParameterTests
    {
        [Fact]
        public void TestBoolean()
        {
            var p = new Parameter
            {
                UserID = Rand.Next(),
                Category = Rand.NextString(8),
                Name = Rand.NextString(8)
            };

            var flag = Rand.Next(2) == 1;
            p.SetValue(flag);

            p.Insert();

            var p2 = Parameter.FindByID(p.ID);
            Assert.NotNull(p2);

            var val = p2.GetValue();
            Assert.NotNull(val);
            Assert.Equal(flag, val);
        }

        [Fact]
        public void TestInt()
        {
            var p = new Parameter
            {
                UserID = Rand.Next(),
                Category = Rand.NextString(8),
                Name = Rand.NextString(8)
            };

            var v = Rand.Next();
            p.SetValue(v);

            p.Insert();

            var p2 = Parameter.FindByID(p.ID);
            Assert.NotNull(p2);

            var val = p2.GetValue();
            Assert.NotNull(val);
            Assert.Equal(v, val);
        }

        [Fact]
        public void TestDouble()
        {
            var p = new Parameter
            {
                UserID = Rand.Next(),
                Category = Rand.NextString(8),
                Name = Rand.NextString(8)
            };

            var v = Rand.Next() / 1000d;
            p.SetValue(v);

            p.Insert();

            var p2 = Parameter.FindByID(p.ID);
            Assert.NotNull(p2);

            var val = p2.GetValue();
            Assert.NotNull(val);
            Assert.Equal(v, val);
        }

        [Fact]
        public void TestDateTime()
        {
            var p = new Parameter
            {
                UserID = Rand.Next(),
                Category = Rand.NextString(8),
                Name = Rand.NextString(8)
            };

            var v = DateTime.Now;
            p.SetValue(v);

            p.Insert();

            var p2 = Parameter.FindByID(p.ID);
            Assert.NotNull(p2);

            var val = p2.GetValue();
            Assert.NotNull(val);
            Assert.Equal(v.Trim(), val);
        }

        [Fact]
        public void TestString()
        {
            var p = new Parameter
            {
                UserID = Rand.Next(),
                Category = Rand.NextString(8),
                Name = Rand.NextString(8)
            };

            var v = Rand.NextString(8);
            p.SetValue(v);

            p.Insert();

            var p2 = Parameter.FindByID(p.ID);
            Assert.NotNull(p2);

            var val = p2.GetValue();
            Assert.NotNull(val);
            Assert.Equal(v, val);
        }

        [Fact]
        public void TestList()
        {
            var p = new Parameter
            {
                UserID = Rand.Next(),
                Category = Rand.NextString(8),
                Name = Rand.NextString(8)
            };

            var list = new List<Int32>
            {
                Rand.Next(),
                Rand.Next()
            };
            p.SetValue(list);

            p.Insert();

            var p2 = Parameter.FindByID(p.ID);
            Assert.NotNull(p2);

            var list2 = p2.GetList<Int32>();
            Assert.NotNull(list2);
            Assert.Equal(list[0], list2[0]);
            Assert.Equal(list[1], list2[1]);
        }

        [Fact]
        public void TestHash()
        {
            var p = new Parameter
            {
                UserID = Rand.Next(),
                Category = Rand.NextString(8),
                Name = Rand.NextString(8)
            };

            var dic = new Dictionary<Int32, String>
            {
                [111] = Rand.NextString(8),
                [222] = Rand.NextString(16)
            };
            p.SetValue(dic);

            p.Insert();

            var p2 = Parameter.FindByID(p.ID);
            Assert.NotNull(p2);

            var dic2 = p2.GetHash<Int32, String>();
            Assert.NotNull(dic2);
            Assert.Equal(dic.Count, dic2.Count);
            Assert.Equal(dic[111], dic2[111]);
            Assert.Equal(dic[222], dic2[222]);
        }

    }
}