using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Log
{
    /// <summary>控制台输出日志</summary>
    public class ConsoleLog : Logger
    {
        /// <summary>写日志</summary>
        /// <param name="level"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected override void OnWrite(LogLevel level, String format, params Object[] args)
        {
        }
    }
}