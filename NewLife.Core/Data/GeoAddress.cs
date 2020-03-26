using System;

namespace NewLife.Data
{
    /// <summary>地理地址</summary>
    public class GeoAddress
    {
        #region 属性
        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>坐标</summary>
        public GeoPoint Location { get; set; }

        /// <summary>地址</summary>
        public String Address { get; set; }

        /// <summary>行政区域编码</summary>
        public Int32 Code { get; set; }

        /// <summary>国家</summary>
        public String Country { get; set; }

        /// <summary>省份</summary>
        public String Province { get; set; }

        /// <summary>城市</summary>
        public String City { get; set; }

        /// <summary>区县</summary>
        public String District { get; set; }

        /// <summary>乡镇</summary>
        public String Township { get; set; }

        /// <summary>乡镇编码</summary>
        public String Towncode { get; set; }

        /// <summary>街道</summary>
        public String Street { get; set; }

        /// <summary></summary>
        public String StreetNumber { get; set; }

        /// <summary>级别</summary>
        public String Level { get; set; }

        /// <summary>精确打点</summary>
        public Boolean Precise { get; set; }

        /// <summary>可信度。[0-100]</summary>
        public Int32 Confidence { get; set; }
        #endregion

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString() => Address;
    }
}