using System;

namespace NewLife.Log
{
    /// <summary>日志接口</summary>
    public interface ILog
    {
        /// <summary>写日志</summary>
        /// <param name="level">日志级别</param>
        /// <param name="format">格式化字符串</param>
        /// <param name="args">格式化参数</param>
        void Write(LogLevel level, String format, params Object[] args);

        /// <summary>调试日志</summary>
        /// <param name="format">格式化字符串</param>
        /// <param name="args">格式化参数</param>
        void Debug(String format, params Object[] args);

        /// <summary>信息日志</summary>
        /// <param name="format">格式化字符串</param>
        /// <param name="args">格式化参数</param>
        void Info(String format, params Object[] args);

        /// <summary>警告日志</summary>
        /// <param name="format">格式化字符串</param>
        /// <param name="args">格式化参数</param>
        void Warn(String format, params Object[] args);

        /// <summary>错误日志</summary>
        /// <param name="format">格式化字符串</param>
        /// <param name="args">格式化参数</param>
        void Error(String format, params Object[] args);

        /// <summary>严重错误日志</summary>
        /// <param name="format">格式化字符串</param>
        /// <param name="args">格式化参数</param>
        void Fatal(String format, params Object[] args);

        /// <summary>是否启用日志</summary>
        Boolean Enable { get; set; }

        /// <summary>日志等级，只输出大于等于该级别的日志，默认Info，打开NewLife.Debug时默认为最低的Debug</summary>
        LogLevel Level { get; set; }
    }
}