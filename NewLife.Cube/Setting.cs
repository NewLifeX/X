using System;
using System.ComponentModel;
using NewLife.Configuration;
using NewLife.Log;
using NewLife.Xml;

namespace NewLife.Cube
{
    /// <summary>魔方设置</summary>
    [DisplayName("魔方设置")]
    [XmlConfigFile(@"Config\Cube.config", 15000)]
    public class Setting : XmlConfig<Setting>
    {
        #region 属性
        /// <summary>是否启用调试。默认为不启用</summary>
        [Description("调试")]
        public Boolean Debug { get; set; }

        /// <summary>显示运行时间</summary>
        [Description("显示运行时间")]
        public Boolean ShowRunTime { get; set; }

        /// <summary>扩展插件服务器。将从该网页上根据关键字分析链接并下载插件</summary>
        [Description("扩展插件服务器。将从该网页上根据关键字分析链接并下载插件")]
        public String PluginServer { get; set; }
        #endregion

        #region 方法
        /// <summary>实例化</summary>
        public Setting()
        {
            Debug = false;
            ShowRunTime = true;
            PluginServer = "http://www.newlifex.com/showtopic-1488.aspx";
        }

        /// <summary>新建时调用</summary>
        protected override void OnNew()
        {
            Debug = Config.GetConfig<Boolean>("NewLife.Cube.Debug", false);
            ShowRunTime = Config.GetConfig<Boolean>("NewLife.Cube.ShowRunTime", XTrace.Debug);
        }
        #endregion
    }
}