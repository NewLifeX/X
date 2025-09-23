using NewLife.Data;
using Xunit;

namespace XUnitTest.Data;

public class GeoHashTests
{
    [Fact]
    public void Encode()
    {
        // 鸟巢
        Assert.Equal("wx4g8c9vn", GeoHash.Encode(116.402843, 39.999375));
        // 水立方
        Assert.Equal("wx4g89tkz", GeoHash.Encode(116.3967, 39.99932));
        // 故宫
        Assert.Equal("wx4g0ffev", GeoHash.Encode(116.40382, 39.918118));
    }

    [Fact]
    public void Decode()
    {
        var gp1 = GeoHash.Decode("wx4g8c9vn");
        Assert.True(Math.Abs(116.402843 - gp1.Longitude) < 0.0001);
        Assert.True(Math.Abs(39.999375 - gp1.Latitude) < 0.0001);

        var gp2 = GeoHash.Decode("wx4g89tkz");
        Assert.True(Math.Abs(116.3967 - gp2.Longitude) < 0.0001);
        Assert.True(Math.Abs(39.99932 - gp2.Latitude) < 0.0001);

        var gp3 = GeoHash.Decode("wx4g0ffev");
        Assert.True(Math.Abs(116.40382 - gp3.Longitude) < 0.0001);
        Assert.True(Math.Abs(39.918118 - gp3.Latitude) < 0.0001);
    }

    [Fact]
    public void Encode_CharCountClamp()
    {
        // 小于1 → 按1
        Assert.Equal(1, GeoHash.Encode(116.402843, 39.999375, 0).Length);
        Assert.Equal(1, GeoHash.Encode(116.402843, 39.999375, -5).Length);
        // 大于最大精度 → 按12
        Assert.Equal(12, GeoHash.Encode(116.402843, 39.999375, 20).Length);
        // 正常
        Assert.Equal(9, GeoHash.Encode(116.402843, 39.999375, 9).Length);
    }

    [Fact]
    public void Decode_Uppercase_And_Invalid()
    {
        // 大小写兼容
        var pLower = GeoHash.Decode("wx4g8c9vn");
        var pUpper = GeoHash.Decode("WX4G8C9VN");
        Assert.True(Math.Abs(pLower.Longitude - pUpper.Longitude) < 1e-10);
        Assert.True(Math.Abs(pLower.Latitude - pUpper.Latitude) < 1e-10);

        // 非法字符应抛异常（包含 O）
        Assert.Throws<ArgumentException>(() => GeoHash.Decode("wx4g8O9vn"));

        // TryDecode 失败返回 false
        Assert.False(GeoHash.TryDecode("wx4g8O9vn", out _, out _));
    }

    [Fact]
    public void IsValid_Tests()
    {
        Assert.True(GeoHash.IsValid("wx4g8c9vn"));
        Assert.True(GeoHash.IsValid("WX4G8C9VN"));
        Assert.False(GeoHash.IsValid("wx4g8O9vn")); // 包含非法字符 O
        Assert.False(GeoHash.IsValid(""));
    }

    [Fact]
    public void BoundingBox_CenterMatchesEncode()
    {
        var gh = "wx4g8c9vn";
        var box = GeoHash.GetBoundingBox(gh);
        // 中心点编码应回到原 geohash
        var centerLon = (box.MinLongitude + box.MaxLongitude) / 2;
        var centerLat = (box.MinLatitude + box.MaxLatitude) / 2;
        var code = GeoHash.Encode(centerLon, centerLat, gh.Length);
        Assert.Equal(gh, code);

        // 中心点在区间内
        Assert.True(centerLon >= box.MinLongitude && centerLon <= box.MaxLongitude);
        Assert.True(centerLat >= box.MinLatitude && centerLat <= box.MaxLatitude);
    }

    [Fact]
    public void Neighbor_Directionality()
    {
        var gh = "wx4g8c9vn";
        var (lon, lat) = GeoHash.Decode(gh);

        var east = GeoHash.Neighbor(gh, 1, 0);
        var west = GeoHash.Neighbor(gh, -1, 0);
        var north = GeoHash.Neighbor(gh, 0, 1);
        var south = GeoHash.Neighbor(gh, 0, -1);

        var pEast = GeoHash.Decode(east);
        var pWest = GeoHash.Decode(west);
        var pNorth = GeoHash.Decode(north);
        var pSouth = GeoHash.Decode(south);

        Assert.True(pEast.Longitude > lon);
        Assert.True(pWest.Longitude < lon);
        Assert.True(pNorth.Latitude > lat);
        Assert.True(pSouth.Latitude < lat);

        var list = GeoHash.Neighbors(gh);
        Assert.Equal(8, list.Length);
        // 索引1应为正北 (0,1)
        Assert.Equal(north, list[1]);
    }
}
