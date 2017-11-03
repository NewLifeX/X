using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Xml;

namespace XCoder.Yun
{
    [XmlConfigFile("Config\\Map.config", 15000)]
    class MapSetting : XmlConfig<MapSetting>
    {
        #region 属性
        /// <summary>接口</summary>
        [Description("接口")]
        public String Map { get; set; }

        /// <summary>坐标系</summary>
        [Description("坐标系")]
        public String Coordtype { get; set; }

        /// <summary>方法</summary>
        [Description("方法")]
        public String Method { get; set; }

        /// <summary>地址</summary>
        [Description("地址")]
        public String Address { get; set; } = "陆家嘴银城中路501号";

        /// <summary>城市</summary>
        [Description("城市")]
        public String City { get; set; } = "上海";

        /// <summary>坐标</summary>
        [Description("坐标")]
        public String Location { get; set; }

        /// <summary>坐标2</summary>
        [Description("坐标2")]
        public String Location2 { get; set; }

        /// <summary>格式化地址</summary>
        [Description("格式化地址")]
        public Boolean FormatAddress { get; set; } = true;
        #endregion
    }
}