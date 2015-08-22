using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using NewLife.Configuration;
using NewLife.Log;
using NewLife.Xml;

namespace NewLife
{
    /// <summary>核心设置</summary>
    [DisplayName("核心设置")]
    [XmlConfigFile(@"Config\\Core.config", 15000)]
    public class Setting : XmlConfig<Setting>
    {
        #region 属性
        private Boolean _Debug;
        /// <summary>是否启用全局调试。默认为不启用</summary>
        [Description("全局调试")]
        public Boolean Debug { get { return _Debug; } set { _Debug = value; } }

        private LogLevel _LogLevel;
        /// <summary>日志等级，只输出大于等于该级别的日志</summary>
        [Description("日志等级，只输出大于等于该级别的日志")]
        public LogLevel LogLevel { get { return _LogLevel; } set { _LogLevel = value; } }

        private String _LogPath;
        /// <summary>文本日志目录</summary>
        [Description("文本日志目录")]
        public String LogPath { get { return _LogPath; } set { _LogPath = value; } }

        private String _TempPath;
        /// <summary>临时目录</summary>
        [Description("临时目录")]
        public String TempPath { get { return _TempPath; } set { _TempPath = value; } }

        private Boolean _NetDebug;
        /// <summary>网络调试</summary>
        [Description("网络调试")]
        public Boolean NetDebug { get { return _NetDebug; } set { _NetDebug = value; } }

        private Boolean _ThreadDebug;
        /// <summary>多线程调试</summary>
        [Description("多线程调试")]
        public Boolean ThreadDebug { get { return _ThreadDebug; } set { _ThreadDebug = value; } }

        private String _WebCompressFiles;
        /// <summary>网页压缩文件</summary>
        [Description("网页压缩文件")]
        public String WebCompressFiles { get { return _WebCompressFiles; } set { _WebCompressFiles = value; } }
        #endregion

        #region 方法
        /// <summary>新建时调用</summary>
        protected override void OnNew()
        {
            Debug = Config.GetConfig<Boolean>("NewLife.Debug", false);
            LogLevel = Config.GetConfig<LogLevel>("NewLife.LogLevel", Debug ? LogLevel.Debug : LogLevel.Info);
            LogPath = Config.GetConfig<String>("NewLife.LogPath", Runtime.IsWeb ? "../Log" : "Log");
            TempPath = Config.GetConfig<String>("NewLife.TempPath", "XTemp");
            NetDebug = Config.GetConfig<Boolean>("NewLife.Net.Debug", false);
            ThreadDebug = Config.GetMutilConfig<Boolean>(false, "NewLife.Thread.Debug", "ThreadPoolDebug");
            WebCompressFiles = Config.GetMutilConfig<String>(".aspx,.axd,.js,.css", "NewLife.Web.CompressFiles", "NewLife.CommonEntity.CompressFiles");
        }
        #endregion
    }
}