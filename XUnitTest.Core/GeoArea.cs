using System;

namespace NewLife.Data
{
    /// <summary>地理区域</summary>
    public class GeoArea
    {
        #region 属性
        /// <summary>编码</summary>
        public Int32 Code { get; set; }

        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>父级</summary>
        public Int32 ParentCode { get; set; }

        /// <summary>中心</summary>
        public String Center { get; set; }

        /// <summary>边界</summary>
        public String Polyline { get; set; }

        /// <summary>级别</summary>
        public String Level { get; set; }
        #endregion

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString() => $"{Code} {Name}";
    }
}