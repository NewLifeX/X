using System;
using System.Collections.Generic;
using System.Linq;
using NewLife;
using NewLife.Algorithms;
using NewLife.Collections;
using NewLife.Log;
using Xunit;

namespace XUnitTest.Algorithms
{
    public class SamplingTests
    {
        [Fact]
        public void SplitTest()
        {
            var times = new[] { "10:10", "10:12", "10:43", "11:10", "11:13", "12:10", "12:13", "12:43", "13:10", "13:13", "13:43" };
            var xs = times.Select(e => e[..2].ToInt() * 60 + e[3..].ToLong()).ToArray();

            //var xs = new[] { 3, 4, 6, 8, 9, 13 };
            //var ys = new[] { 3, 4, 6, 8, 9, 13 };

            var rs = SamplingHelper.Split(xs, 60);
            XTrace.WriteLine(times.Join());
            Show(times, rs);
            Assert.Equal(4, rs.Length);
            Assert.Equal(0, rs[0]);
            Assert.Equal(3, rs[1]);
            Assert.Equal(5, rs[2]);
            Assert.Equal(8, rs[3]);
        }

        private void Show(String[] times, Int64[] rs)
        {
            for (var i = 0; i < rs.Length; i++)
            {
                var end = (Int64)times.Length;
                for (var k = i + 1; k < rs.Length; k++)
                {
                    if (rs[k] > 0)
                    {
                        end = rs[k];
                        break;
                    }
                }

                var sb = Pool.StringBuilder.Get();
                sb.AppendFormat("[{0}]: ", rs[i]);
                for (var j = rs[i]; j < end; j++)
                {
                    if (j > rs[i]) sb.Append(", ");
                    if (j >= 0)
                        sb.Append(times[j]);
                    else
                    {
                        sb.Append("null");
                        break;
                    }
                }
                XTrace.WriteLine(sb.Put(true));
            }
        }

        [Fact]
        public void SplitTest2()
        {
            var times = new[] { "10:10", "10:12", "10:43", "12:10", "12:13", "12:43", "13:10", "13:13", "13:43" };
            var xs = times.Select(e => e[..2].ToInt() * 60 + e[3..].ToLong()).ToArray();

            var rs = SamplingHelper.Split(xs, 60);
            XTrace.WriteLine(times.Join());
            Show(times, rs);
            Assert.Equal(4, rs.Length);
            Assert.Equal(0, rs[0]);
            Assert.Equal(-1, rs[1]);
            Assert.Equal(3, rs[2]);
            Assert.Equal(6, rs[3]);
        }

        [Fact]
        public void SplitTest3()
        {
            var times = new[] { "10:10", "10:12", "10:43", "12:10", "12:13", "12:43", "13:10", "13:13", "13:43" };
            var xs = times.Select(e => e[..2].ToInt() * 60 + e[3..].ToLong()).ToArray();

            var rs = SamplingHelper.Split(xs, 60, 15);
            XTrace.WriteLine(times.Join());
            Show(times, rs);
            Assert.Equal(5, rs.Length);
            Assert.Equal(0, rs[0]);
            Assert.Equal(2, rs[1]);
            Assert.Equal(3, rs[2]);
            Assert.Equal(5, rs[3]);
            Assert.Equal(8, rs[4]);
        }

        [Fact]
        public void SplitTest4()
        {
            var times = new[] { "10:10", "10:12", "10:43", "12:12", "12:13", "12:43", "13:10", "13:13", "13:43" };
            var xs = times.Select(e => e[..2].ToInt() * 60 + e[3..].ToLong()).ToArray();

            var rs = SamplingHelper.Split(xs, 60, 11);
            XTrace.WriteLine(times.Join());
            Show(times, rs);
            Assert.Equal(5, rs.Length);
            Assert.Equal(0, rs[0]);
            Assert.Equal(1, rs[1]);
            Assert.Equal(-1, rs[2]);
            Assert.Equal(3, rs[3]);
            Assert.Equal(7, rs[4]);
        }
    }
}
