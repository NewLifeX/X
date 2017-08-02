using System;
using System.ComponentModel;
using System.Web;
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
        public Boolean ShowRunTime { get; set; } = true;

        /// <summary>扩展插件服务器。将从该网页上根据关键字分析链接并下载插件</summary>
        [Description("扩展插件服务器。将从该网页上根据关键字分析链接并下载插件")]
        public String PluginServer { get; set; } = "http://x.newlifex.com/";

        /// <summary>工作台页面。进入后台的第一个内容页</summary>
        [Description("工作台页面。进入后台的第一个内容页")]
        public String StartPage { get; set; }

        /// <summary>布局页。</summary>
        [Description("布局页。")]
        public String Layout { get; set; } = "~/Views/Shared/_Ace_Layout.cshtml";

        /// <summary>默认角色。注册用户得到的角色</summary>
        [Description("默认角色。注册用户得到的角色")]
        public Int32 DefaultRole { get; set; } = 3;

        /// <summary>启用注册</summary>
        [Description("启用注册")]
        public Boolean AllowRegister { get; set; } = true;

        /// <summary>启用取回密码</summary>
        [Description("启用取回密码")]
        public Boolean AllowForgot { get; set; } = true;
        #endregion

        #region 方法
        /// <summary>实例化</summary>
        public Setting()
        {
        }

        /// <summary>加载时触发</summary>
        protected override void OnLoaded()
        {
            if (StartPage.IsNullOrEmpty()) StartPage = HttpRuntime.AppDomainAppVirtualPath.EnsureEnd("/") + "Admin/Index/Main";

            base.OnLoaded();
        }
        #endregion
    }
}