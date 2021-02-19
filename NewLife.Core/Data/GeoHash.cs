using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Data
{
    /// <summary>经纬坐标的一维编码表示</summary>
    /// <remarks>
    /// 文档 https://www.yuque.com/smartstone/nx/geo_hash
    /// 
    /// 一维编码表示一个矩形区域，前缀表示更大区域，例如北京wx4fbzdvs80包含在wx4fbzdvs里面。
    /// 这个特性可以用于附近地点搜索。
    /// GeoHash编码位数及距离关系：
    /// 1位，+-2500km；
    /// 2位，+-630km；
    /// 3位，+-78km；
    /// 4位，+-20km；
    /// 5位，+-2.4km；
    /// 6位，+-610m；
    /// 7位，+-76m；
    /// 8位，+-19m；
    /// 9位，+-2m；
    /// </remarks>
    public static class GeoHash
    {
        #region 属性
        private static readonly Int32[] BITS = { 16, 8, 4, 2, 1 };
        private const String _base32 = "0123456789bcdefghjkmnpqrstuvwxyz";
        private static readonly Dictionary<Char, Int32> _decode = new Dictionary<Char, Int32>();
        #endregion

        #region 构造
        static GeoHash()
        {
            for (var i = 0; i < _base32.Length; i++)
            {
                _decode[_base32[i]] = i;
            }
        }
        #endregion

        #region 方法
        /// <summary>编码坐标点为GeoHash字符串</summary>
        /// <param name="longitude">经度</param>
        /// <param name="latitude">纬度</param>
        /// <param name="charCount">字符个数。默认9位字符编码，精度2米</param>
        /// <returns></returns>
        public static String Encode(Double longitude, Double latitude, Int32 charCount = 9)
        {
            Double[] longitudeRange = { -180, 180 };
            Double[] latitudeRange = { -90, 90 };

            var isEvenBit = true;
            UInt64 bits = 0;
            var len = charCount * 5;
            for (var i = 0; i < len; i++)
            {
                bits <<= 1;

                // 轮流占用信息位
                var value = isEvenBit ? longitude : latitude;
                var rang = isEvenBit ? longitudeRange : latitudeRange;

                var mid = (rang[0] + rang[1]) / 2;
                if (value >= mid)
                {
                    bits |= 0x1;
                    rang[0] = mid;
                }
                else
                {
                    rang[1] = mid;
                }

                isEvenBit = !isEvenBit;
            }

            bits <<= (64 - len);

            // base32编码
            var sb = new StringBuilder();
            for (var i = 0; i < charCount; i++)
            {
                var pointer = (Int32)((bits & 0xf800000000000000L) >> 59);
                sb.Append(_base32[pointer]);
                bits <<= 5;
            }
            return sb.ToString();
        }

        /// <summary>解码GeoHash字符串为坐标点</summary>
        /// <param name="geohash"></param>
        /// <returns></returns>
        public static GeoPoint Decode(String geohash)
        {
            Double[] latitudeRange = { -90, 90 };
            Double[] longitudeRange = { -180, 180 };

            var isEvenBit = true;
            for (var i = 0; i < geohash.Length; i++)
            {
                var ch = _decode[geohash[i]];
                for (var j = 0; j < 5; j++)
                {
                    // 轮流解码信息位
                    var rang = isEvenBit ? longitudeRange : latitudeRange;
                    var mid = (rang[0] + rang[1]) / 2;
                    if ((ch & BITS[j]) != 0)
                        rang[0] = mid;
                    else
                        rang[1] = mid;

                    isEvenBit = !isEvenBit;
                }
            }

            var longitude = (longitudeRange[0] + longitudeRange[1]) / 2;
            var latitude = (latitudeRange[0] + latitudeRange[1]) / 2;

            return new GeoPoint(longitude, latitude);
        }
        #endregion
    }
}