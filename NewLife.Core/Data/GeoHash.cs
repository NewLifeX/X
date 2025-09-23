using System.Text;

namespace NewLife.Data;

/// <summary>经纬坐标的一维编码表示</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/geo_hash
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
    private static readonly Int32[] BITS = [16, 8, 4, 2, 1];
    private const String _base32 = "0123456789bcdefghjkmnpqrstuvwxyz";

    // 将解码表从 Dictionary 切换为 ASCII 映射表以提升性能（大小写兼容，未知值为 -1）
    private static readonly SByte[] _decodeMap = new SByte[128];

    /// <summary>最大精度（字符数）。每个字符5比特，最多12个字符（60比特）</summary>
    private const Int32 _maxPrecision = 12;
    #endregion

    #region 构造
    static GeoHash()
    {
        // 初始化为 -1 表示非法
        for (var i = 0; i < _decodeMap.Length; i++) _decodeMap[i] = -1;

        // 填充合法字符映射，兼容大小写
        for (var i = 0; i < _base32.Length; i++)
        {
            var c = _base32[i];
            if (c < _decodeMap.Length) _decodeMap[c] = (SByte)i;

            var uc = Char.ToUpperInvariant(c);
            if (uc < _decodeMap.Length) _decodeMap[uc] = (SByte)i;
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
        // 规范化输入
        if (charCount < 1) charCount = 1;
        if (charCount > _maxPrecision) charCount = _maxPrecision;
        longitude = ClampLongitude(longitude);
        latitude = ClampLatitude(latitude);

        Double[] longitudeRange = [-180, 180];
        Double[] latitudeRange = [-90, 90];

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

        bits <<= 64 - len;

        // base32编码
        var sb = new StringBuilder(charCount);
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
    /// <returns>中心点经纬度（Longitude, Latitude）</returns>
    public static (Double Longitude, Double Latitude) Decode(String geohash)
    {
        if (String.IsNullOrEmpty(geohash)) throw new ArgumentException("geohash 不能为空", nameof(geohash));

        Double[] latitudeRange = [-90, 90];
        Double[] longitudeRange = [-180, 180];

        var isEvenBit = true;
        for (var i = 0; i < geohash.Length; i++)
        {
            var ch = geohash[i];
            var code = ch < 128 ? _decodeMap[ch] : -1;
            if (code < 0) throw new ArgumentException($"geohash 包含非法字符: '{ch}'", nameof(geohash));

            for (var j = 0; j < 5; j++)
            {
                // 轮流解码信息位
                var rang = isEvenBit ? longitudeRange : latitudeRange;
                var mid = (rang[0] + rang[1]) / 2;
                if ((code & BITS[j]) != 0)
                    rang[0] = mid;
                else
                    rang[1] = mid;

                isEvenBit = !isEvenBit;
            }
        }

        var longitude = (longitudeRange[0] + longitudeRange[1]) / 2;
        var latitude = (latitudeRange[0] + latitudeRange[1]) / 2;

        return (longitude, latitude);
    }

    /// <summary>尝试解码GeoHash字符串为坐标点</summary>
    /// <param name="geohash">GeoHash 字符串</param>
    /// <param name="longitude">输出经度</param>
    /// <param name="latitude">输出纬度</param>
    /// <returns>是否解析成功</returns>
    public static Boolean TryDecode(String geohash, out Double longitude, out Double latitude)
    {
        longitude = 0;
        latitude = 0;
        if (String.IsNullOrEmpty(geohash)) return false;

        Double[] latitudeRange = [-90, 90];
        Double[] longitudeRange = [-180, 180];

        var isEvenBit = true;
        for (var i = 0; i < geohash.Length; i++)
        {
            var ch = geohash[i];
            var code = ch < 128 ? (Int32)_decodeMap[ch] : -1;
            if (code < 0) return false;

            for (var j = 0; j < 5; j++)
            {
                var rang = isEvenBit ? longitudeRange : latitudeRange;
                var mid = (rang[0] + rang[1]) / 2;
                if ((code & BITS[j]) != 0)
                    rang[0] = mid;
                else
                    rang[1] = mid;

                isEvenBit = !isEvenBit;
            }
        }

        longitude = (longitudeRange[0] + longitudeRange[1]) / 2;
        latitude = (latitudeRange[0] + latitudeRange[1]) / 2;
        return true;
    }

    /// <summary>判断 GeoHash 字符串是否有效（字符合法且非空）</summary>
    /// <param name="geohash">GeoHash 字符串</param>
    /// <returns>是否有效</returns>
    public static Boolean IsValid(String geohash)
    {
        if (String.IsNullOrEmpty(geohash)) return false;
        for (var i = 0; i < geohash.Length; i++)
        {
            var ch = geohash[i];
            if (ch >= 128) return false;
            if (_decodeMap[ch] < 0) return false;
        }
        return true;
    }

    /// <summary>获取 GeoHash 对应的边界矩形（最小经纬与最大经纬）</summary>
    /// <param name="geohash">GeoHash 字符串</param>
    /// <returns>(MinLongitude, MinLatitude, MaxLongitude, MaxLatitude)</returns>
    public static (Double MinLongitude, Double MinLatitude, Double MaxLongitude, Double MaxLatitude) GetBoundingBox(String geohash)
    {
        if (String.IsNullOrEmpty(geohash)) throw new ArgumentException("geohash 不能为空", nameof(geohash));

        Double[] latitudeRange = [-90, 90];
        Double[] longitudeRange = [-180, 180];

        var isEvenBit = true;
        for (var i = 0; i < geohash.Length; i++)
        {
            var ch = geohash[i];
            var code = ch < 128 ? (Int32)_decodeMap[ch] : -1;
            if (code < 0) throw new ArgumentException($"geohash 包含非法字符: '{ch}'", nameof(geohash));

            for (var j = 0; j < 5; j++)
            {
                var rang = isEvenBit ? longitudeRange : latitudeRange;
                var mid = (rang[0] + rang[1]) / 2;
                if ((code & BITS[j]) != 0)
                    rang[0] = mid;
                else
                    rang[1] = mid;

                isEvenBit = !isEvenBit;
            }
        }

        return (longitudeRange[0], latitudeRange[0], longitudeRange[1], latitudeRange[1]);
    }

    /// <summary>获取某个 GeoHash 的邻居（同一精度下，按网格偏移）</summary>
    /// <param name="geohash">中心 GeoHash</param>
    /// <param name="deltaLongitude">经度方向偏移（-1、0、1）</param>
    /// <param name="deltaLatitude">纬度方向偏移（-1、0、1）</param>
    /// <returns>邻居 GeoHash</returns>
    public static String Neighbor(String geohash, Int32 deltaLongitude, Int32 deltaLatitude)
    {
        if (String.IsNullOrEmpty(geohash)) throw new ArgumentException("geohash 不能为空", nameof(geohash));

        var (minLon, minLat, maxLon, maxLat) = GetBoundingBox(geohash);
        var lonSpan = maxLon - minLon;
        var latSpan = maxLat - minLat;

        var centerLon = (minLon + maxLon) / 2;
        var centerLat = (minLat + maxLat) / 2;

        var nextLon = centerLon + deltaLongitude * lonSpan;
        var nextLat = centerLat + deltaLatitude * latSpan;

        nextLon = ClampLongitude(nextLon);
        nextLat = ClampLatitude(nextLat);

        return Encode(nextLon, nextLat, geohash.Length);
    }

    /// <summary>获取 8 个方向的邻居（上、下、左、右与四个对角）。结果顺序：
    /// [(-1,1), (0,1), (1,1), (-1,0), (1,0), (-1,-1), (0,-1), (1,-1)]</summary>
    /// <param name="geohash">中心 GeoHash</param>
    /// <returns>8 个邻居 GeoHash</returns>
    public static String[] Neighbors(String geohash)
    {
        return [
            Neighbor(geohash, -1,  1),
            Neighbor(geohash,  0,  1),
            Neighbor(geohash,  1,  1),
            Neighbor(geohash, -1,  0),
            Neighbor(geohash,  1,  0),
            Neighbor(geohash, -1, -1),
            Neighbor(geohash,  0, -1),
            Neighbor(geohash,  1, -1)
        ];
    }
    #endregion

    #region 辅助
    private static Double ClampLongitude(Double value)
    {
        if (value < -180) return -180;
        if (value > 180) return 180;
        return value;
    }

    private static Double ClampLatitude(Double value)
    {
        if (value < -90) return -90;
        if (value > 90) return 90;
        return value;
    }
    #endregion
}