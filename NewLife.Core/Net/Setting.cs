using System;
using System.ComponentModel;
using NewLife.Xml;

namespace NewLife.Net
{
    /// <summary>网络设置</summary>
    [DisplayName("网络设置")]
#if !__MOBILE__
    [XmlConfigFile(@"Config\Socket.config", 15000)]
#endif
    public class Setting : XmlConfig<Setting>
    {
        #region 属性
        /// <summary>网络调试</summary>
        [Description("网络调试")]
        public Boolean Debug { get; set; }

        /// <summary>会话超时时间。默认20*60秒</summary>
        [Description("会话超时时间。默认20*60秒")]
        public Int32 SessionTimeout { get; set; } = 20 * 60;

        /// <summary>缓冲区大小。默认64k</summary>
        [Description("缓冲区大小。默认64k")]
        public Int32 BufferSize { get; set; } = 64 * 1024;
        #endregion

        #region 方法
        /// <summary>实例化</summary>
        public Setting() { }
        #endregion
    }
}