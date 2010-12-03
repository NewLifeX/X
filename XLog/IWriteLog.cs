using System;
using System.Collections.Generic;
using System.Text;

namespace XLog
{
    /// <summary>
    /// 写日志接口
    /// </summary>
    public interface IWriteLog
    {
        /// <summary>
        /// 写日志事件
        /// </summary>
        event EventHandler<WriteLogEventArgs> OnWriteLog;

        /// <summary>
        /// 写日志
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void WriteLog(Object sender, WriteLogEventArgs e);

        /// <summary>
        /// 写日志
        /// </summary>
        /// <param name="message"></param>
        void WriteLog(String message);

        /// <summary>
        /// 是否支持写日志
        /// </summary>
        Boolean CanWriteLog { get; }
    }
}
