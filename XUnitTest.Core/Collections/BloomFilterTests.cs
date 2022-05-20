using System;
using NewLife.Collections;
using NewLife.Log;
using Xunit;

namespace XUnitTest.Collections
{
    public class BloomFilterTests
    {
        [Fact(DisplayName = "高精度1")]
        public void Test1()
        {
            XTrace.WriteLine("高精度1");

            var count = 10_000_000;

            //var bf = new BloomFilter(count, 0.0001);
            var bf = new BloomFilter(count * 32);
            var rs1 = 0;
            var rs2 = 0;

            for (var i = 0; i < count; i++)
            {
                var key = $"ip_{i}";
                if (bf.Get(key))
                {
                    rs1++;
                }
                else
                {
                    bf.Set(key);
                    rs2++;
                }
            }
            var p1 = (Double)rs1 / count;
            var p2 = (Double)rs2 / count;
            XTrace.WriteLine($"  存在：{rs1} {p1:p8}");
            XTrace.WriteLine($"不存在：{rs2} {p2:p8}");

            Assert.True(p1 < 0.00004);
            Assert.True(p2 > 0.99996);

            var buf = bf.GetBytes();
            XTrace.WriteLine("bf.Length={0:n0}", buf.Length);
        }

        [Fact(DisplayName = "高精度2")]
        public void Test2()
        {
            XTrace.WriteLine("高精度2");

            var count = 10_000_000;

            var bf = new BloomFilter(count, 0.0001);
            var rs1 = 0;
            var rs2 = 0;

            for (var i = 0; i < count; i++)
            {
                var key = $"ip_{i}";
                if (bf.Get(key))
                {
                    rs1++;
                }
                else
                {
                    bf.Set(key);
                    rs2++;
                }
            }
            var p1 = (Double)rs1 / count;
            var p2 = (Double)rs2 / count;
            XTrace.WriteLine($"  存在：{rs1} {p1:p8}");
            XTrace.WriteLine($"不存在：{rs2} {p2:p8}");

            Assert.True(p1 < 0.00004);
            Assert.True(p2 > 0.99996);

            var buf = bf.GetBytes();
            XTrace.WriteLine("bf.Length={0:n0}", buf.Length);
        }

        [Fact(DisplayName = "低精度1")]
        public void Test3()
        {
            XTrace.WriteLine("低精度1");

            var count = 1_000_000;

            var bf = new BloomFilter(count / 10);
            var rs1 = 0;
            var rs2 = 0;

            for (var i = 0; i < count; i++)
            {
                var key = $"ip_{i}";
                if (bf.Get(key))
                {
                    rs1++;
                }
                else
                {
                    bf.Set(key);
                    rs2++;
                }
            }
            var p1 = (Double)rs1 / count;
            var p2 = (Double)rs2 / count;
            XTrace.WriteLine($"  存在：{rs1} {p1:p8}");
            XTrace.WriteLine($"不存在：{rs2} {p2:p8}");

            //Assert.True(p1 < 0.06);
            //Assert.True(p2 > 0.94);

            var buf = bf.GetBytes();
            XTrace.WriteLine("bf.Length={0:n0}", buf.Length);

            var str = bf.GetString();
            XTrace.WriteLine("str.Length={0:n0}", str.Length);
        }

        [Fact(DisplayName = "低精度2")]
        public void Test4()
        {
            XTrace.WriteLine("低精度2");

            var count = 1_000_000;

            var bf = new BloomFilter(count, 0.01);
            var rs1 = 0;
            var rs2 = 0;

            for (var i = 0; i < count; i++)
            {
                var key = $"ip_{i}";
                if (bf.Get(key))
                {
                    rs1++;
                }
                else
                {
                    bf.Set(key);
                    rs2++;
                }
            }
            var p1 = (Double)rs1 / count;
            var p2 = (Double)rs2 / count;
            XTrace.WriteLine($"  存在：{rs1} {p1:p8}");
            XTrace.WriteLine($"不存在：{rs2} {p2:p8}");

            Assert.True(p1 < 0.01);
            Assert.True(p2 > 0.99);

            var buf = bf.GetBytes();
            XTrace.WriteLine("bf.Length={0:n0}", buf.Length);

            var str = bf.GetString();
            XTrace.WriteLine("str.Length={0:n0}", str.Length);
        }
    }
}