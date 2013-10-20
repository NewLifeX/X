using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Log
{
    /// <summary>复合日志提供者，多种方式输出</summary>
    public class CompositeLog : Logger
    {
        private List<ILog> _Logs = new List<ILog>();
        /// <summary>日志提供者集合</summary>
        public List<ILog> Logs { get { return _Logs; } set { _Logs = value; } }

        /// <summary>添加一个日志提供者</summary>
        /// <param name="log"></param>
        /// <returns></returns>
        public CompositeLog Add(ILog log) { Logs.Add(log); return this; }

        /// <summary>写日志</summary>
        /// <param name="level"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected override void OnWrite(LogLevel level, String format, params Object[] args)
        {
            if (Logs != null)
            {
                foreach (var item in Logs)
                {
                    item.Write(level, format, args);
                }
            }
        }

        public ILog Get<TLog>() where TLog : ILog
        {
            return Get(typeof(TLog));
        }

        public ILog Get(Type type)
        {
            foreach (var item in Logs)
            {
                if (item != null && type.IsAssignableFrom(item.GetType())) return item;
            }

            return null;
        }
    }
}