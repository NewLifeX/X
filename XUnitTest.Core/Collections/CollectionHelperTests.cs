using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace XUnitTest.Collections
{
    public class CollectionHelperTests
    {
        [Fact]
        public void ToArrayTest()
        {
            var vs = new[] { 12, 34, 56, 78, 90 };
            var list = new List<Int32>(vs);
            var list2 = list as IList<Int32>;

            var vs2 = list2.ToArray();
            Assert.Equal(vs.Length, vs2.Length);
            Assert.Equal(vs[0], vs2[0]);
            Assert.Equal(vs[1], vs2[1]);
            Assert.Equal(vs[2], vs2[2]);
            Assert.Equal(vs[3], vs2[3]);
            Assert.Equal(vs[4], vs2[4]);

            var vs3 = list2.ToArray(2);
            Assert.Equal(vs.Length + 2, vs3.Length);
            Assert.Equal(vs[0], vs3[2]);
            Assert.Equal(vs[1], vs3[3]);
            Assert.Equal(vs[2], vs3[4]);
            Assert.Equal(vs[3], vs3[5]);
            Assert.Equal(vs[4], vs3[6]);
        }
    }
}