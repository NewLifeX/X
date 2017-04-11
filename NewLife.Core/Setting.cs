using System;
using System.ComponentModel;
using System.IO;
using NewLife.Log;
using NewLife.Xml;

namespace NewLife
{
    /// <summary>核心设置</summary>
    [DisplayName("核心设置")]
#if !__MOBILE__
    [XmlConfigFile(@"Config\Core.config", 15000)]
#endif
    public class Setting : XmlConfig<Setting>
    {
        #region 属性
        /// <summary>是否启用全局调试。默认启用</summary>
        [Description("全局调试。XTrace.Debug")]
        public Boolean Debug { get; set; } = true;

        /// <summary>日志等级，只输出大于等于该级别的日志</summary>
        [Description("日志等级。只输出大于等于该级别的日志")]
        public LogLevel LogLevel { get; set; } = LogLevel.Info;

        /// <summary>文件日志目录</summary>
        [Description("文件日志目录")]
        public String LogPath { get; set; } = "";

        /// <summary>网络日志。本地子网日志广播255.255.255.255:514</summary>
        [Description("网络日志。本地子网日志广播255.255.255.255:514")]
        public String NetworkLog { get; set; } = "";

        /// <summary>临时目录</summary>
        [Description("临时目录")]
        public String TempPath { get; set; } = "";

        /// <summary>插件目录</summary>
        [Description("插件目录")]
        public String PluginPath { get; set; } = "Plugins";

        /// <summary>插件服务器。将从该网页上根据关键字分析链接并下载插件</summary>
        [Description("插件服务器。将从该网页上根据关键字分析链接并下载插件")]
        public String PluginServer { get; set; } = "http://x.newlifex.com/";

        /// <summary>插件缓存目录。默认位于系统盘的X\Cache</summary>
        [Description("插件缓存目录。默认位于系统盘的X\\Cache")]
        public String PluginCache { get; set; } = "";

        ///// <summary>网络调试</summary>
        //[Description("网络调试")]
        //public Boolean NetDebug { get; set; }

        /// <summary>语音提示。默认true</summary>
        [Description("语音提示。默认true")]
        public Boolean SpeechTip { get; set; } = true;
        #endregion

        #region 方法
        /// <summary>实例化</summary>
        public Setting()
        {
        }

        /// <summary>新建时调用</summary>
        protected override void OnNew()
        {
        }

        /// <summary>加载完成后</summary>
        protected override void OnLoaded()
        {
            if (TempPath.IsNullOrEmpty())
            {
                if (Runtime.IsWeb)
                    TempPath = "..\\XTemp";
                else
                    TempPath = "XTemp";
            }
#if !__MOBILE__
            if (PluginCache.IsNullOrWhiteSpace())
            {
                // 兼容Linux Mono
                var sys = Environment.SystemDirectory;
                if (sys.IsNullOrEmpty()) sys = "/";
                PluginCache = Path.GetPathRoot(sys).CombinePath("X", "Cache");
            }
#endif
            if (PluginServer.IsNullOrWhiteSpace() || PluginServer.StartsWithIgnoreCase("ftp://")) PluginServer = "http://x.newlifex.com/";

            base.OnLoaded();
        }

        /// <summary>获取插件目录</summary>
        /// <returns></returns>
        public String GetPluginPath()
        {
            //if (Runtime.IsWeb)
            //    return "Bin".GetFullPath();
            //else
            return PluginPath.GetBasePath();
        }

        /// <summary>获取插件缓存目录</summary>
        /// <returns></returns>
        public String GetPluginCache()
        {
            var cachedir = PluginCache;

#if !__MOBILE__
            // 确保缓存目录可用
            for (int i = 0; i < 2; i++)
            {
                try
                {
                    cachedir.EnsureDirectory();
                    break;
                }
                catch
                {
                    if (i == 0)
                    {
                        var sys = Environment.SystemDirectory;
                        if (sys.IsNullOrEmpty()) sys = "/";
                        cachedir = Path.GetPathRoot(sys).CombinePath("X", "Cache");
                    }
                    else
                        cachedir = "..\\Cache".GetFullPath();

                    PluginCache = cachedir;
                }
            }
#endif

            return cachedir;
        }
        #endregion
    }
}