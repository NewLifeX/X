using System;
using System.ComponentModel;
using System.Text;
using NewLife.Xml;

namespace XCoder
{
    [XmlConfigFile("Config\\XCoder.config")]
    public class XConfig : XmlConfig<XConfig>
    {
        #region 属性
        private String _Extend;
        /// <summary>扩展数据</summary>
        [Description("扩展数据")]
        public String Extend { get { return _Extend; } set { _Extend = value; } }

        private DateTime _LastUpdate;
        /// <summary>最后更新时间</summary>
        [DisplayName("最后更新时间")]
        public DateTime LastUpdate { get { return _LastUpdate; } set { _LastUpdate = value; } }

        private String _LastTool;
        /// <summary>最后一个使用的工具</summary>
        [DisplayName("最后一个使用的工具")]
        public String LastTool { get { return _LastTool; } set { _LastTool = value; } }
        #endregion

        #region 加载/保存
        public XConfig()
        {
        }
        #endregion
    }
}