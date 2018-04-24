using System;
using System.ComponentModel;
using NewLife.Xml;

namespace XApi
{
    /// <summary>Api配置</summary>
    [XmlConfigFile("Config\\Api.config")]
    public class ApiConfig : XmlConfig<ApiConfig>
    {
        /// <summary>端口</summary>
        [Description("端口")]
        public Int32 Port { get; set; } = 777;

        /// <summary>地址</summary>
        [Description("地址")]
        public String Address { get; set; } = "";

        ///// <summary>用户名</summary>
        //[Description("用户名")]
        //public String UserName { get; set; } = "test";

        ///// <summary>密码</summary>
        //[Description("密码")]
        //public String Password { get; set; } = "test";

        /// <summary>模式</summary>
        [Description("模式")]
        public String Mode { get; set; } = "服务端";

        /// <summary>发送内容</summary>
        [Description("发送内容")]
        public String SendContent { get; set; } = "新生命开发团队，学无先后达者为师";

        /// <summary>发送次数</summary>
        [Description("发送次数")]
        public Int32 SendTimes { get; set; } = 1;

        /// <summary>发送间隔。毫秒</summary>
        [Description("发送间隔。毫秒")]
        public Int32 SendSleep { get; set; } = 1000;

        /// <summary>发送用户数</summary>
        [Description("发送用户数")]
        public Int32 SendUsers { get; set; } = 1;

        /// <summary>显示应用日志</summary>
        [Description("显示应用日志")]
        public Boolean ShowLog { get; set; } = true;

        /// <summary>显示编码日志</summary>
        [Description("显示编码日志")]
        public Boolean ShowEncoderLog { get; set; } = true;

        /// <summary>显示统计信息</summary>
        [Description("显示统计信息")]
        public Boolean ShowStat { get; set; } = true;

        /// <summary>日志着色</summary>
        [Description("日志着色")]
        public Boolean ColorLog { get; set; } = true;

        public ApiConfig() { }
    }
}