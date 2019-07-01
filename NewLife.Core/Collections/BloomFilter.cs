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
        private readonly BitArray container = null;
        private readonly Int32 _M;
        private readonly Int32 _K;

        /// <summary>实例化布隆过滤器</summary>
        /// <param name="length">位数组大小。建议为预估数据量的32倍，可得到0.004%的误判率</param>
        public BloomFilter(Int32 length)
        {
            container = new BitArray(length);
            _M = length;
            _K = 4;
        }

        /// <summary>实例化布隆过滤器</summary>
        /// <param name="n">预估数据量</param>
        /// <param name="fpp">期望的误判率。小于1</param>
        public BloomFilter(Int64 n, Double fpp)
        {
            if (fpp <= 0 || fpp >= 1) fpp = 0.0001;

            // 根据Guava算法计算位数组大小
            _M = (Int32)(-n * Math.Log(fpp) / (Math.Log(2) * Math.Log(2)));
            _K = Math.Max(1, (Int32)Math.Round(_M / n * Math.Log(2)));

            container = new BitArray(_M);
        }
        #endregion

        #region 方法
        /// <summary>设置指定键进入集合</summary>
        /// <param name="key"></param>
        public void Set(String key)
        {
            var buf = key.GetBytes().Murmur128();

            var hash1 = buf.ToUInt64();
            var hash2 = buf.ToUInt64(8);

            var h = hash1;
            for (var i = 0; i < _K; i++)
            {
                container[(Int32)((Int64)(h & Int64.MaxValue) % _M)] = true;
                h += hash2;
            }
        }

        /// <summary>判断指定键是否存在于集合中</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Boolean Get(String key)
        {
            var buf = key.GetBytes().Murmur128();

            var hash1 = buf.ToUInt64();
            var hash2 = buf.ToUInt64(8);

            var h = hash1;
            for (var i = 0; i < _K; i++)
            {
                if (!container[(Int32)((Int64)(h & Int64.MaxValue) % _M)]) return false;
                h += hash2;
            }

            return true;
        }
        #endregion

        #region 哈希函数
        //Int32 Hash1(String key)
        //{
        //    var hash = 5381;
        //    var ks = key.ToCharArray();
        //    var count = ks.Length;
        //    while (count > 0)
        //    {
        //        hash += (hash << 5) + (ks[ks.Length - count]);
        //        count--;
        //    }
        //    return (hash & 0x7FFFFFFF) % container.Length;

        //}

        //Int32 Hash2(String key)
        //{
        //    var seed = 131; // 31 131 1313 13131 131313 etc..
        //    var hash = 0;
        //    var ks = (key + "key2").ToCharArray();
        //    var count = ks.Length;
        //    while (count > 0)
        //    {
        //        hash = hash * seed + (ks[ks.Length - count]);
        //        count--;
        //    }

        //    return (hash & 0x7FFFFFFF) % container.Length;
        //}

        //Int32 Hash3(String key)
        //{
        //    var hash = 0;
        //    Int32 i;
        //    var ks = (key + "keykey3").ToCharArray();
        //    var count = ks.Length;
        //    for (i = 0; i < count; i++)
        //    {
        //        if ((i & 1) == 0)
        //            hash ^= ((hash << 7) ^ (ks[i]) ^ (hash >> 3));
        //        else
        //            hash ^= (~((hash << 11) ^ (ks[i]) ^ (hash >> 5)));

        //        count--;
        //    }

        //    return (hash & 0x7FFFFFFF) % container.Length;

        //}

        //Int32 Hash4(String key)
        //{
        //    var hash = 5381;
        //    var ks = (key + "keykeyke4").ToCharArray();
        //    var count = ks.Length;
        //    while (count > 0)
        //    {
        //        hash += (hash << 5) + (ks[ks.Length - count]);
        //        count--;
        //    }
        //    return (hash & 0x7FFFFFFF) % container.Length;
        //}
        #endregion
    }
}