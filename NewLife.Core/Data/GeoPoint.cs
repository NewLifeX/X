using System;

namespace NewLife.Data
{
    /// <summary>经纬度坐标</summary>
    public class GeoPoint
    {
        #region 属性
        /// <summary>经度</summary>
        public Double Longitude { get; set; }

        /// <summary>纬度</summary>
        public Double Latitude { get; set; }
        #endregion

        #region 构造
        /// <summary>经纬度坐标</summary>
        public GeoPoint() { }

        /// <summary>经纬度坐标</summary>
        /// <param name="location"></param>
        public GeoPoint(String location)
        {
            if (!location.IsNullOrEmpty())
            {
                var ss = location.Split(",");
                if (ss.Length >= 2)
                {
                    Longitude = ss[0].ToDouble();
                    Latitude = ss[1].ToDouble();
                }
            }
        }
        #endregion

        /// <summary>已重载</summary>
        /// <returns></returns>
        public override String ToString() => $"{Longitude},{Latitude}";
    }
}