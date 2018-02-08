using System;
using System.ComponentModel;
using System.Web;
using NewLife.Xml;

namespace NewLife.Cube
{
    /// <summary>绑定模式</summary>
    public enum BindModes
    {
        /// <summary>默认。要求已登录本地用户</summary>
        [Description("默认")]
        Default = 0,

        /// <summary>自动。本地未登录时自动注册新用户</summary>
        [Description("自动")]
        Auto = 1,

        /// <summary>覆盖。本地未登录时，覆盖绑定同名用户</summary>
        [Description("覆盖")]
        Override = 2,
    }

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
        public Int32 DefaultRole { get; set; } = 1;

        /// <summary>启用密码登录</summary>
        [Description("启用密码登录")]
        public Boolean AllowLogin { get; set; } = true;

        /// <summary>启用注册</summary>
        [Description("启用注册")]
        public Boolean AllowRegister { get; set; } = true;

        /// <summary>绑定模式。第三方登录后如何注册</summary>
        [Description("绑定模式。第三方登录后，0默认要求已登录本地用户，1自动注册，2覆盖注册同名")]
        public BindModes BindMode { get; set; } = BindModes.Auto;

        /// <summary>登录提示。留空表示不显示登录提示信息</summary>
        [Description("登录提示。留空表示不显示登录提示信息")]
        public String LoginTip { get; set; } = "请输入登录信息……";

        /// <summary>用户在线。记录用户在线状态</summary>
        [Description("用户在线。记录用户在线状态")]
        public Boolean WebOnline { get; set; } = true;

        /// <summary>用户行为。记录用户所有操作</summary>
        [Description("用户行为。记录用户所有操作")]
        public Boolean WebBehavior { get; set; }

        /// <summary>访问统计。统计页面访问量</summary>
        [Description("访问统计。统计页面访问量")]
        public Boolean WebStatistics { get; set; } = true;

        /// <summary>表单组样式。大中小屏幕分别3/2/1列</summary>
        [Description("表单组样式。大中小屏幕分别3/2/1列")]
        public String FormGroupClass { get; set; } = "form-group col-xs-12 col-sm-6 col-lg-4";

        /// <summary>下拉选择框。使用Bootstrap，美观，但有呈现方面的性能损耗</summary>
        [Description("下拉选择框。使用Bootstrap，美观，但有呈现方面的性能损耗")]
        public Boolean BootstrapSelect { get; set; } = true;

        /// <summary>强制SSL。强制使用https访问</summary>
        [Description("强制SSL。强制使用https访问")]
        public Boolean ForceSSL { get; set; }
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