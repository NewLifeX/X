using System;
using System.Collections.Generic;
using System.Linq;
using NewLife;
using NewLife.Algorithms;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Log;
using Xunit;

namespace XUnitTest.Algorithms
{
    public class SamplingTests
    {
        [Fact]
        public void SplitByFixedSize()
        {
            var times = new[] { "10:10", "10:12", "10:43", "11:10", "11:13", "12:10", "12:13", "12:43", "13:10", "13:13", "13:43" };
            var xs = times.Select(e => e[..2].ToInt() * 60 + e[3..].ToLong()).ToArray();

            var rs = SamplingHelper.SplitByFixedSize(xs, 60);
            XTrace.WriteLine(times.Join());
            Show(times, rs);
            Assert.Equal(4, rs.Length);
            Assert.Equal(0, rs[0].Start);
            Assert.Equal(3, rs[1].Start);
            Assert.Equal(5, rs[2].Start);
            Assert.Equal(8, rs[3].Start);

            Assert.Equal(3, rs[0].End);
            Assert.Equal(5, rs[1].End);
            Assert.Equal(8, rs[2].End);
            Assert.Equal(11, rs[3].End);
        }

        private void Show(String[] times, IndexRange[] rs)
        {
            for (var i = 0; i < rs.Length; i++)
            {
                var sb = Pool.StringBuilder.Get();
                sb.AppendFormat("[{0}, {1}]: ", rs[i].Start, rs[i].End);
                for (var j = rs[i].Start; j < rs[i].End; j++)
                {
                    if (j > rs[i].Start) sb.Append(", ");
                    if (j >= 0)
                        sb.Append(times[j]);
                    else
                    {
                        sb.Append("null");
                        break;
                    }
                }
                XTrace.WriteLine(sb.Return(true));
            }
        }

        [Fact]
        public void SplitByFixedSize2()
        {
            var times = new[] { "10:10", "10:12", "10:43", "12:10", "12:13", "12:43", "13:10", "13:13", "13:43" };
            var xs = times.Select(e => e[..2].ToInt() * 60 + e[3..].ToLong()).ToArray();

            var rs = SamplingHelper.SplitByFixedSize(xs, 60);
            XTrace.WriteLine(times.Join());
            Show(times, rs);
            Assert.Equal(4, rs.Length);
            Assert.Equal(0, rs[0].Start);
            Assert.Equal(-1, rs[1].Start);
            Assert.Equal(3, rs[2].Start);
            Assert.Equal(6, rs[3].Start);

            Assert.Equal(3, rs[0].End);
            Assert.Equal(3, rs[1].End);
            Assert.Equal(6, rs[2].End);
            Assert.Equal(9, rs[3].End);
        }

        [Fact]
        public void SplitByFixedSize3()
        {
            var times = new[] { "10:10", "10:12", "10:43", "12:10", "12:13", "12:43", "13:10", "13:13", "13:43" };
            var xs = times.Select(e => e[..2].ToInt() * 60 + e[3..].ToLong()).ToArray();

            var rs = SamplingHelper.SplitByFixedSize(xs, 60, 15);
            XTrace.WriteLine(times.Join());
            Show(times, rs);
            Assert.Equal(5, rs.Length);
            Assert.Equal(0, rs[0].Start);
            Assert.Equal(2, rs[1].Start);
            Assert.Equal(3, rs[2].Start);
            Assert.Equal(5, rs[3].Start);
            Assert.Equal(8, rs[4].Start);

            Assert.Equal(2, rs[0].End);
            Assert.Equal(3, rs[1].End);
            Assert.Equal(5, rs[2].End);
            Assert.Equal(8, rs[3].End);
            Assert.Equal(9, rs[4].End);
        }

        [Fact]
        public void SplitByFixedSize4()
        {
            var times = new[] { "10:10", "10:12", "10:43", "12:12", "12:13", "12:43", "13:10", "13:13", "13:43" };
            var xs = times.Select(e => e[..2].ToInt() * 60 + e[3..].ToLong()).ToArray();

            var rs = SamplingHelper.SplitByFixedSize(xs, 60, 11);
            XTrace.WriteLine(times.Join());
            Show(times, rs);
            Assert.Equal(5, rs.Length);
            Assert.Equal(0, rs[0].Start);
            Assert.Equal(1, rs[1].Start);
            Assert.Equal(-1, rs[2].Start);
            Assert.Equal(3, rs[3].Start);
            Assert.Equal(7, rs[4].Start);
        }

        [Fact]
        public void SplitByFixedSize5()
        {
            var times = new[] { "10:10", "10:11", "10:12", "10:43", "13:11", "13:13", "13:43", "14:11", "14:13", "14:43" };
            var xs = times.Select(e => e[..2].ToInt() * 60 + e[3..].ToLong()).ToArray();

            var rs = SamplingHelper.SplitByFixedSize(xs, 60, 11);
            XTrace.WriteLine(times.Join());
            Show(times, rs);
            Assert.Equal(6, rs.Length);
            Assert.Equal(0, rs[0].Start);
            Assert.Equal(1, rs[1].Start);
            Assert.Equal(-1, rs[2].Start);
            Assert.Equal(-1, rs[3].Start);
            Assert.Equal(4, rs[4].Start);
            Assert.Equal(7, rs[5].Start);

            Assert.Equal(1, rs[0].End);
            Assert.Equal(4, rs[1].End);
            Assert.Equal(4, rs[2].End);
            Assert.Equal(4, rs[3].End);
            Assert.Equal(7, rs[4].End);
            Assert.Equal(10, rs[5].End);
        }

        [Fact]
        public void SplitByAverage()
        {
            var times = new[] { "10:10", "10:12", "10:43", "11:10", "11:13", "12:10", "12:13", "12:43", "13:10", "13:13", "13:43" };
            var xs = times.Select(e => e[..2].ToInt() * 60 + e[3..].ToLong()).ToArray();

            var rs = SamplingHelper.SplitByAverage(xs.Length, 5);
            XTrace.WriteLine(times.Join());
            Show(times, rs);
            Assert.Equal(5, rs.Length);
            Assert.Equal(0, rs[0].Start);
            Assert.Equal(1, rs[1].Start);
            Assert.Equal(4, rs[2].Start);
            Assert.Equal(7, rs[3].Start);
            Assert.Equal(10, rs[4].Start);

            Assert.Equal(1, rs[0].End);
            Assert.Equal(4, rs[1].End);
            Assert.Equal(7, rs[2].End);
            Assert.Equal(10, rs[3].End);
            Assert.Equal(11, rs[4].End);
        }

        [Fact]
        public void SplitByAverage2()
        {
            var times = new[] { "10:10", "10:12", "10:43", "11:10", "11:13", "12:10", "12:13", "12:43", "13:10", "13:13", "13:43" };
            var xs = times.Select(e => e[..2].ToInt() * 60 + e[3..].ToLong()).ToArray();

            var rs = SamplingHelper.SplitByAverage(xs.Length, 6);
            XTrace.WriteLine(times.Join());
            Show(times, rs);
            Assert.Equal(6, rs.Length);
            Assert.Equal(0, rs[0].Start);
            Assert.Equal(1, rs[1].Start);
            Assert.Equal(3, rs[2].Start);
            Assert.Equal(5, rs[3].Start);
            Assert.Equal(8, rs[4].Start);
            Assert.Equal(10, rs[5].Start);

            Assert.Equal(1, rs[0].End);
            Assert.Equal(3, rs[1].End);
            Assert.Equal(5, rs[2].End);
            Assert.Equal(8, rs[3].End);
            Assert.Equal(10, rs[4].End);
            Assert.Equal(11, rs[5].End);
        }

        [Fact]
        public void SplitByAverage3()
        {
            var times = new[] { "10:10", "10:12", "10:43", "11:10", "11:13", "12:10", "12:13", "12:43", "13:10", "13:13", "13:43" };
            var xs = times.Select(e => e[..2].ToInt() * 60 + e[3..].ToLong()).ToArray();

            var rs = SamplingHelper.SplitByAverage(xs.Length, 6, false);
            XTrace.WriteLine(times.Join());
            Show(times, rs);
            Assert.Equal(6, rs.Length);
            Assert.Equal(0, rs[0].Start);
            Assert.Equal(2, rs[1].Start);
            Assert.Equal(4, rs[2].Start);
            Assert.Equal(6, rs[3].Start);
            Assert.Equal(7, rs[4].Start);
            Assert.Equal(9, rs[5].Start);
        }
    }
}