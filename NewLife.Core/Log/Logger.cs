using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using NewLife.Configuration;

namespace NewLife.Log
{
    /// <summary>日志基类。提供日志的基本实现</summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public abstract class Logger : ILog
    {
        #region 主方法
        /// <summary>调试日志</summary>
        /// <param name="format">格式化字符串</param>
        /// <param name="args">格式化参数</param>
        public virtual void Debug(String format, params Object[] args) { Write(LogLevel.Debug, format, args); }

        /// <summary>信息日志</summary>
        /// <param name="format">格式化字符串</param>
        /// <param name="args">格式化参数</param>
        public virtual void Info(String format, params Object[] args) { Write(LogLevel.Info, format, args); }

        /// <summary>警告日志</summary>
        /// <param name="format">格式化字符串</param>
        /// <param name="args">格式化参数</param>
        public virtual void Warn(String format, params Object[] args) { Write(LogLevel.Warn, format, args); }

        /// <summary>错误日志</summary>
        /// <param name="format">格式化字符串</param>
        /// <param name="args">格式化参数</param>
        public virtual void Error(String format, params Object[] args) { Write(LogLevel.Error, format, args); }

        /// <summary>严重错误日志</summary>
        /// <param name="format">格式化字符串</param>
        /// <param name="args">格式化参数</param>
        public virtual void Fatal(String format, params Object[] args) { Write(LogLevel.Fatal, format, args); }
        #endregion

        #region 核心方法
        /// <summary>写日志</summary>
        /// <param name="level"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public virtual void Write(LogLevel level, String format, params Object[] args)
        {
            if (level >= Level) OnWrite(level, format, args);
        }

        /// <summary>写日志</summary>
        /// <param name="level"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected abstract void OnWrite(LogLevel level, String format, params Object[] args);

        ///// <summary>输出异常日志</summary>
        ///// <param name="ex">异常信息</param>
        //public abstract void WriteException(Exception ex);

        ///// <summary>写日志</summary>
        ///// <param name="format">格式化字符串</param>
        ///// <param name="args">格式化参数</param>
        //public abstract void WriteLine(LogLevel level, String format, params Object[] args);
        #endregion

        #region 属性
        private LogLevel? _Level;
        /// <summary>日志等级，只输出大于等于该级别的日志</summary>
        public LogLevel Level
        {
            get
            {
                if (_Level != null) return _Level.Value;

                try
                {
                    return Config.GetConfig<LogLevel>("NewLife.LogLevel", LogLevel.Info);
                }
                catch { return LogLevel.Info; }
            }
            set { _Level = value; }
        }
        #endregion
    }
}