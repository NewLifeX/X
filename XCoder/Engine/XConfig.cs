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
        /// <summary>扩展数据</summary>
        [Description("扩展数据")]
        public String Extend { get; set; }

        /// <summary>更新服务器</summary>
        [Description("更新服务器")]
        public String UpdateServer { get; set; }

        /// <summary>最后更新时间</summary>
        [DisplayName("最后更新时间")]
        public DateTime LastUpdate { get; set; }

        /// <summary>最后一个使用的工具</summary>
        [DisplayName("最后一个使用的工具")]
        public String LastTool { get; set; }
        #endregion

        #region 加载/保存
        public XConfig()
        {
        }

        protected override void OnLoaded()
        {
            if (UpdateServer.IsNullOrEmpty()) UpdateServer = "http://www.newlifex.com/showtopic-260.aspx";
            
            base.OnLoaded();
        }
        #endregion
    }
}