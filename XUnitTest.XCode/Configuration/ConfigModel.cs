using System;
using System.ComponentModel;
using NewLife.Common;
using NewLife.Configuration;
using NewLife.Log;

namespace XUnitTest.XCode.Configuration
{
    /// <summary>配置模型</summary>
    [DisplayName("配置模型")]
    [Config("Core")]
    public class ConfigModel
    {
        #region 属性
        /// <summary>是否启用全局调试。默认启用</summary>
        [Description("全局调试。XTrace.Debug")]
        public Boolean Debug { get; set; } = true;

        /// <summary>日志等级，只输出大于等于该级别的日志，All/Debug/Info/Warn/Error/Fatal，默认Info</summary>
        [Description("日志等级。只输出大于等于该级别的日志，All/Debug/Info/Warn/Error/Fatal，默认Info")]
        public LogLevel LogLevel { get; set; } = LogLevel.Info;

        /// <summary>文件日志目录</summary>
        [Description("文件日志目录")]
        public String LogPath { get; set; } = "";

        /// <summary>网络日志。本地子网日志广播255.255.255.255:514</summary>
        [Description("网络日志。本地子网日志广播255.255.255.255:514")]
        public String NetworkLog { get; set; } = "";

        /// <summary>日志文件格式</summary>
        [Description("日志文件格式。默认{0:yyyy_MM_dd}.log")]
        public String LogFileFormat { get; set; } = "{0:yyyy_MM_dd}.log";

        /// <summary>临时目录</summary>
        [Description("临时目录")]
        public String TempPath { get; set; } = "";

        /// <summary>插件目录</summary>
        [Description("插件目录")]
        public String PluginPath { get; set; } = "Plugins";

        /// <summary>插件服务器。将从该网页上根据关键字分析链接并下载插件</summary>
        [Description("插件服务器。将从该网页上根据关键字分析链接并下载插件")]
        public String PluginServer { get; set; } = "http://x.newlifex.com/";

        [Description("系统配置")]
        public SysConfig Sys { get; set; }
        #endregion
    }
}