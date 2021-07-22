using System;
using NewLife.Data;
using Xunit;

namespace XUnitTest.Data
{
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
    }
}
