using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NewLife.Log
{
    /// <summary>依托于动作的日志类</summary>
    public class ActionLog : Logger
    {
        private Action<String, Object[]> _Method;
        /// <summary>方法</summary>
        public Action<String, Object[]> Method { get { return _Method; } set { _Method = value; } }

        /// <summary>使用指定方法否则动作日志</summary>
        /// <param name="action"></param>
        public ActionLog(Action<String, Object[]> action) { Method = action; }

        /// <summary>写日志</summary>
        /// <param name="level"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected override void OnWrite(LogLevel level, String format, params Object[] args)
        {
            if (Method != null) Method(format, args);
        }
    }
}