using System;
using System.ComponentModel;
using System.Web;
using NewLife.Security;
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

        /// <summary>默认角色。注册用户得到的角色，0使用认证中心角色，-1强制使用</summary>
        [Description("默认角色。注册用户得到的角色，0使用认证中心角色，-1强制使用")]
        public Int32 DefaultRole { get; set; } = 0;

        /// <summary>启用密码登录。允许输入用户名密码进行登录</summary>
        [Description("启用密码登录。允许输入用户名密码进行登录")]
        public Boolean AllowLogin { get; set; } = true;

        /// <summary>启用注册。允许输入用户名密码进行注册</summary>
        [Description("启用注册。允许输入用户名密码进行注册")]
        public Boolean AllowRegister { get; set; } = true;

        /// <summary>自动注册。第三方登录后，如果本地未登录，自动注册新用户</summary>
        [Description("自动注册。第三方登录后，如果本地未登录，自动注册新用户")]
        public Boolean AutoRegister { get; set; } = true;

        /// <summary>强行绑定用户。根据OAuth登录返回用户名强项绑定本地同名用户，而不需要增加提供者前缀</summary>
        [Description("强行绑定用户。根据OAuth登录返回用户名强项绑定本地同名用户，而不需要增加提供者前缀")]
        public Boolean ForceBindUser { get; set; }

        /// <summary>会话超时。单点登录后会话超时时间，该时间内可借助Cookie登录，默认0s</summary>
        [Description("会话超时。单点登录后会话超时时间，该时间内可借助Cookie登录，默认0s")]
        public Int32 SessionTimeout { get; set; } = 0;

        /// <summary>登录提示。留空表示不显示登录提示信息</summary>
        [Description("登录提示。留空表示不显示登录提示信息")]
        public String LoginTip { get; set; }

        /// <summary>用户在线。记录用户在线状态</summary>
        [Description("用户在线。记录用户在线状态")]
        public Boolean WebOnline { get; set; } = true;

        /// <summary>用户行为。记录用户所有操作</summary>
        [Description("用户行为。记录用户所有操作")]
        public Boolean WebBehavior { get; set; }

        /// <summary>访问统计。统计页面访问量</summary>
        [Description("访问统计。统计页面访问量")]
        public Boolean WebStatistics { get; set; } = true;

        /// <summary>捕获所有异常。默认false只捕获魔方区域异常</summary>
        [Description("捕获所有异常。默认false只捕获魔方区域异常")]
        public Boolean CatchAllException { get; set; }

        /// <summary>表单组样式。大中小屏幕分别3/2/1列</summary>
        [Description("表单组样式。大中小屏幕分别3/2/1列")]
        public String FormGroupClass { get; set; } = "form-group col-xs-12 col-sm-6 col-lg-4";

        /// <summary>下拉选择框。使用Bootstrap，美观，但有呈现方面的性能损耗</summary>
        [Description("下拉选择框。使用Bootstrap，美观，但有呈现方面的性能损耗")]
        public Boolean BootstrapSelect { get; set; } = true;

        /// <summary>强制SSL。强制使用https访问</summary>
        [Description("强制SSL。强制使用https访问")]
        public Boolean ForceSSL { get; set; }

        /// <summary>头像目录。设定后下载远程头像到本地</summary>
        [Description("头像目录。设定后下载远程头像到本地")]
        public String AvatarPath { get; set; } = "..\\Avatars";

        ///// <summary>安全密钥。用于加密Cookie等通信内容</summary>
        //[Description("安全密钥。用于加密Cookie等通信内容")]
        //public String SecurityKey { get; set; }
        #endregion

        #region 方法
        /// <summary>实例化</summary>
        public Setting() { }

        /// <summary>加载时触发</summary>
        protected override void OnLoaded()
        {
            if (StartPage.IsNullOrEmpty()) StartPage = HttpRuntime.AppDomainAppVirtualPath.EnsureEnd("/") + "Admin/Index/Main";
            //if (SecurityKey.IsNullOrEmpty()) SecurityKey = Rand.NextString(16);

            base.OnLoaded();
        }
        #endregion
    }
}