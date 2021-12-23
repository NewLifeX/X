using System;
using System.Security.Cryptography;
using NewLife.Collections;

namespace NewLife.Security
{
    /// <summary>随机数</summary>
    public static class Rand
    {
        /// <summary>返回一个小于所指定最大值的非负随机数</summary>
        /// <param name="max">返回的随机数的上界（随机数不能取该上界值）</param>
        /// <returns></returns>
        public static Int32 Next(Int32 max = Int32.MaxValue) => RandomNumberGenerator.GetInt32(max);

        /// <summary>返回一个指定范围内的随机数</summary>
        /// <remarks>
        /// 调用平均耗时37.76ns，其中GC耗时77.56%
        /// </remarks>
        /// <param name="min">返回的随机数的下界（随机数可取该下界值）</param>
        /// <param name="max">返回的随机数的上界（随机数不能取该上界值）</param>
        /// <returns></returns>
        public static Int32 Next(Int32 min, Int32 max) => RandomNumberGenerator.GetInt32(min, max);

        /// <summary>返回指定长度随机字节数组</summary>
        /// <remarks>
        /// 调用平均耗时5.46ns，其中GC耗时15%
        /// </remarks>
        /// <param name="count"></param>
        /// <returns></returns>
        public static Byte[] NextBytes(Int32 count)
        {
#if NET6_0_OR_GREATER
            return RandomNumberGenerator.GetBytes(count);
#else
            var buf = new Byte[count];
            RandomNumberGenerator.Fill(buf);
            return buf;
#endif
        }

        /// <summary>返回指定长度随机字符串</summary>
        /// <param name="length">长度</param>
        /// <param name="symbol">是否包含符号</param>
        /// <returns></returns>
        public static String NextString(Int32 length, Boolean symbol = false)
        {
            var sb = Pool.StringBuilder.Get();
            for (var i = 0; i < length; i++)
            {
                var ch = ' ';
                if (symbol)
                    ch = (Char)Next(' ', 0x7F);
                else
                {
                    var n = Next(0, 10 + 26 + 26);
                    if (n < 10)
                        ch = (Char)('0' + n);
                    else if (n < 10 + 26)
                        ch = (Char)('A' + n - 10);
                    else
                        ch = (Char)('a' + n - 10 - 26);
                }
                sb.Append(ch);
            }

            return sb.Put(true);
        }
    }
}