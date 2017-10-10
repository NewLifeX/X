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

        /// <summary>已重载</summary>
        /// <returns></returns>
        public override String ToString() => $"({Longitude}, {Latitude})";
    }
}