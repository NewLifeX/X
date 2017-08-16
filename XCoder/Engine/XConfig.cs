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
        /// <summary>标题</summary>
        [Description("标题")]
        public String Title { get; set; } = "";

        /// <summary>宽度</summary>
        [Description("宽度")]
        public Int32 Width { get; set; }

        /// <summary>高度</summary>
        [Description("高度")]
        public Int32 Height { get; set; }

        /// <summary>顶部</summary>
        [Description("顶部")]
        public Int32 Top { get; set; }

        /// <summary>左边</summary>
        [Description("左边")]
        public Int32 Left { get; set; }

        /// <summary>扩展数据</summary>
        [Description("扩展数据")]
        public String Extend { get; set; } = "";

        /// <summary>日志着色</summary>
        [Description("日志着色")]
        public Boolean ColorLog { get; set; } = true;

        /// <summary>语音提示。默认true</summary>
        [Description("语音提示。默认true")]
        public Boolean SpeechTip { get; set; } = true;

        /// <summary>更新服务器</summary>
        [Description("更新服务器")]
        public String UpdateServer { get; set; } = "";

        /// <summary>最后更新时间</summary>
        [DisplayName("最后更新时间")]
        public DateTime LastUpdate { get; set; }

        /// <summary>最后一个使用的工具</summary>
        [DisplayName("最后一个使用的工具")]
        public String LastTool { get; set; } = "";
        #endregion

        #region 加载/保存
        public XConfig()
        {
        }

        protected override void OnLoaded()
        {
            if (UpdateServer.IsNullOrEmpty() || UpdateServer.EqualIgnoreCase("http://x.newlifex.com/")) UpdateServer = NewLife.Setting.Current.PluginServer;

            base.OnLoaded();
        }
        #endregion
    }
}