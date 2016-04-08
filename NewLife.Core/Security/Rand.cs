using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace NewLife.Security
{
    /// <summary>随机数</summary>
    public static class Rand
    {
        private static RandomNumberGenerator _rnd;

        static Rand()
        {
            _rnd = new RNGCryptoServiceProvider();
        }

        /// <summary>返回一个小于所指定最大值的非负随机数</summary>
        /// <param name="max">返回的随机数的上界（随机数不能取该上界值）</param>
        /// <returns></returns>
        public static Int32 Next(Int32 max = Int32.MaxValue)
        {
            if (max <= 0) throw new ArgumentOutOfRangeException("max");

            //var buf = new Byte[4];
            //_rnd.GetBytes(buf);

            //var n = BitConverter.ToInt32(buf, 0);
            //if (max == Int32.MaxValue) return n;

            //return (Int32)((Int64)n * max / Int32.MaxValue);

            return Next(0, max);
        }

        /// <summary>返回一个指定范围内的随机数</summary>
        /// <remarks>
        /// 调用平均耗时37.76ns，其中GC耗时77.56%
        /// </remarks>
        /// <param name="min">返回的随机数的下界（随机数可取该下界值）</param>
        /// <param name="max">返回的随机数的上界（随机数不能取该上界值）</param>
        /// <returns></returns>
        public static Int32 Next(Int32 min, Int32 max)
        {
            if (max <= min) throw new ArgumentOutOfRangeException("max");

            var buf = new Byte[4];
            _rnd.GetBytes(buf);

            var n = BitConverter.ToInt32(buf, 0);
            if (min == Int32.MinValue && max == Int32.MaxValue) return n;
            if (min == 0 && max == Int32.MaxValue) return Math.Abs(n);
            if (min == Int32.MinValue && max == 0) return -Math.Abs(n);

            var num = max - min;
            //return (Int32)(num * Math.Abs(n) / ((Int64)UInt32.MaxValue + 1) + min);
            return (Int32)((((Int64)num * (UInt32)n) >> 32) + min);
            //// 不要进行复杂运算，看做是生成从0到(max-min)的随机数，然后再加上min即可
            //if (num <= (Int64)Int32.MaxValue)
            //    //return (Int32)(num * Math.Abs(n) / Int32.MaxValue) + min;
            //    return (Int32)(num * Math.Abs(n) / Int32.MaxValue) + min;
            //else
            //    return (Int32)(num * Math.Abs(n) / Int32.MaxValue) + min;
        }

        /// <summary>返回指定长度随机字节数组</summary>
        /// <remarks>
        /// 调用平均耗时5.46ns，其中GC耗时15%
        /// </remarks>
        /// <param name="count"></param>
        /// <returns></returns>
        public static Byte[] NextBytes(Int32 count)
        {
            var buf = new Byte[count];
            _rnd.GetBytes(buf);
            return buf;
        }

        /// <summary>返回指定长度随机字符串</summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static String NextString(Int32 length)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                var ch = (Char)Next((Int32)' ', 0x7F);
                sb.Append(ch);
            }

            return sb.ToString();
        }
    }
}