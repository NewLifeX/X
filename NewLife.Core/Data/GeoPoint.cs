using System.Globalization;

namespace NewLife.Data;

/// <summary>经纬度坐标。值类型，栈分配，零GC压力</summary>
/// <remarks>
/// 默认值为 (0,0)，即几内亚湾。需要表示"无坐标"时使用 GeoPoint?。
/// 字符串表示 "经度,纬度"，如 "116.4,39.9"。
/// </remarks>
public readonly record struct GeoPoint(Double Longitude, Double Latitude)
{
    /// <summary>已重载，输出 "经度,纬度"。使用固定区域性，避免小数点分隔符随系统区域变化</summary>
    /// <returns></returns>
    public override String ToString() => $"{Longitude.ToString(CultureInfo.InvariantCulture)},{Latitude.ToString(CultureInfo.InvariantCulture)}";

    /// <summary>解析经纬度字符串，形如 "116.4,39.9"</summary>
    /// <param name="location">经纬度字符串</param>
    /// <returns>坐标点</returns>
    /// <exception cref="FormatException">字符串为空或格式非法</exception>
    public static GeoPoint Parse(String? location)
    {
        if (!TryParse(location, out var point))
            throw new FormatException(L.T("Invalid latitude/longitude string: {0}", location));

        return point;
    }

    /// <summary>尝试解析经纬度字符串，形如 "116.4,39.9"</summary>
    /// <param name="location">经纬度字符串</param>
    /// <param name="point">输出坐标点</param>
    /// <returns>是否解析成功</returns>
    public static Boolean TryParse(String? location, out GeoPoint point)
    {
        point = default;
        if (location.IsNullOrEmpty()) return false;

        var ss = location.Split(',');
        if (ss.Length < 2) return false;

        // 与 Utility.ToDouble 一致使用 InvariantCulture，避免小数点分隔符歧义
        if (!Double.TryParse(ss[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var lon)) return false;
        if (!Double.TryParse(ss[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat)) return false;

        point = new GeoPoint(lon, lat);
        return true;
    }

    /// <summary>编码坐标点为GeoHash字符串</summary>
    /// <param name="level">字符个数。默认9位字符编码，精度2米</param>
    /// <returns>GeoHash 字符串</returns>
    public String Encode(Int32 level = 9) => GeoHash.Encode(Longitude, Latitude, level);

    /// <summary>解码GeoHash字符串为坐标点</summary>
    /// <param name="hash">GeoHash 字符串</param>
    /// <returns>坐标点</returns>
    public static GeoPoint Decode(String hash)
    {
        var (Longitude, Latitude) = GeoHash.Decode(hash);
        return new(Longitude, Latitude);
    }

    /// <summary>尝试解码GeoHash字符串为坐标点</summary>
    /// <param name="hash">GeoHash 字符串</param>
    /// <param name="point">输出坐标点</param>
    /// <returns>是否解码成功</returns>
    public static Boolean TryDecode(String hash, out GeoPoint point)
    {
        point = default;
        if (!GeoHash.TryDecode(hash, out var lon, out var lat)) return false;

        point = new(lon, lat);
        return true;
    }
}