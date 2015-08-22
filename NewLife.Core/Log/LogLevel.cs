
namespace NewLife.Log
{
    /// <summary>日志等级</summary>
    public enum LogLevel : byte
    {
        /// <summary>打开所有日志记录</summary>
        All = 0,

        /// <summary>最低调试。细粒度信息事件对调试应用程序非常有帮助</summary>
        Debug,

        /// <summary>普通消息。在粗粒度级别上突出强调应用程序的运行过程</summary>
        Info,

        /// <summary>警告</summary>
        Warn,

        /// <summary>错误</summary>
        Error,

        /// <summary>严重错误</summary>
        Fatal,

        /// <summary>关闭所有日志记录</summary>
        Off = 0xFF
    }
}