using System;
using System.Collections;

namespace NewLife.Core.Collections
{
    /// <summary>布隆过滤器</summary>
    /// <remarks>
    /// 以极小内存进行海量键值的存在判断，碰撞几率很小。
    /// </remarks>
    public class BloomFilter
    {
        #region 构造
        readonly BitArray container = null;

        /// <summary>实例化布隆过滤器</summary>
        /// <param name="length">位数组大小</param>
        public BloomFilter(Int32 length) => container = new BitArray(length);

        /// <summary>实例化布隆过滤器</summary>
        /// <param name="n">预估数据量</param>
        /// <param name="fpp">期望的误判率。小于1</param>
        public BloomFilter(Int64 n, Double fpp)
        {
            if (fpp <= 0 || fpp >= 1) fpp = 0.0001;

            // 根据Guava算法计算位数组大小
            var m = -n * Math.Log(fpp) / (Math.Log(2) * Math.Log(2));
            var k = Math.Max(1, (Int32)Math.Round(m / n * Math.Log(2)));
            Console.WriteLine(k);

            container = new BitArray((Int32)m);
        }
        #endregion

        #region 方法
        /// <summary>设置指定键进入集合</summary>
        /// <param name="key"></param>
        public void Set(String key)
        {
            var h1 = Hash1(key);
            var h2 = Hash2(key);
            var h3 = Hash3(key);
            var h4 = Hash4(key);

            container[h1] = true;
            container[h2] = true;
            container[h3] = true;
            container[h4] = true;
        }

        /// <summary>判断指定键是否存在于集合中</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Boolean Get(String key)
        {
            var h1 = Hash1(key);
            var h2 = Hash2(key);
            var h3 = Hash3(key);
            var h4 = Hash4(key);

            return container[h1] && container[h2] && container[h3] && container[h4];
        }
        #endregion

        #region 哈希函数
        Int32 Hash1(String key)
        {
            var hash = 5381;
            var ks = key.ToCharArray();
            var count = ks.Length;
            while (count > 0)
            {
                hash += (hash << 5) + (ks[ks.Length - count]);
                count--;
            }
            return (hash & 0x7FFFFFFF) % container.Length;

        }

        Int32 Hash2(String key)
        {
            var seed = 131; // 31 131 1313 13131 131313 etc..
            var hash = 0;
            var ks = (key + "key2").ToCharArray();
            var count = ks.Length;
            while (count > 0)
            {
                hash = hash * seed + (ks[ks.Length - count]);
                count--;
            }

            return (hash & 0x7FFFFFFF) % container.Length;
        }

        Int32 Hash3(String key)
        {
            var hash = 0;
            Int32 i;
            var ks = (key + "keykey3").ToCharArray();
            var count = ks.Length;
            for (i = 0; i < count; i++)
            {
                if ((i & 1) == 0)
                    hash ^= ((hash << 7) ^ (ks[i]) ^ (hash >> 3));
                else
                    hash ^= (~((hash << 11) ^ (ks[i]) ^ (hash >> 5)));

                count--;
            }

            return (hash & 0x7FFFFFFF) % container.Length;

        }

        Int32 Hash4(String key)
        {
            var hash = 5381;
            var ks = (key + "keykeyke4").ToCharArray();
            var count = ks.Length;
            while (count > 0)
            {
                hash += (hash << 5) + (ks[ks.Length - count]);
                count--;
            }
            return (hash & 0x7FFFFFFF) % container.Length;
        }
        #endregion
    }
}