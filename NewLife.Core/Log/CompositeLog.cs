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

        /// <summary>实例化</summary>
        public CompositeLog() { }

        /// <summary>实例化</summary>
        /// <param name="log"></param>
        public CompositeLog(ILog log) { Logs.Add(log); Level = log.Level; }

        /// <summary>实例化</summary>
        /// <param name="log1"></param>
        /// <param name="log2"></param>
        public CompositeLog(ILog log1, ILog log2)
        {
            Add(log1).Add(log2);
            Level = log1.Level;
            if (Level > log2.Level) Level = log2.Level;
        }

        /// <summary>添加一个日志提供者</summary>
        /// <param name="log"></param>
        /// <returns></returns>
        public CompositeLog Add(ILog log) { Logs.Add(log); return this; }

        /// <summary>删除日志提供者</summary>
        /// <param name="log"></param>
        /// <returns></returns>
        public CompositeLog Remove(ILog log) { if (Logs.Contains(log))Logs.Remove(log); return this; }

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

        /// <summary>从复合日志提供者中提取指定类型的日志提供者</summary>
        /// <typeparam name="TLog"></typeparam>
        /// <returns></returns>
        public TLog Get<TLog>() where TLog : class
        {
            foreach (var item in Logs)
            {
                if (item != null)
                {
                    if (item is TLog) return item as TLog;

                    // 递归获取内层日志
                    var cmp = item as CompositeLog;
                    if (cmp != null)
                    {
                        var log = cmp.Get<TLog>();
                        if (log != null) return log;
                    }
                }
            }

            return null;
        }

        //public ILog Get(Type type)
        //{
        //    foreach (var item in Logs)
        //    {
        //        if (item != null && type.IsAssignableFrom(item.GetType())) return item;
        //    }

        //    return null;
        //}

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(this.GetType().Name);

            foreach (var item in Logs)
            {
                sb.Append(" ");
                sb.Append(item + "");
            }

            return sb.ToString();
        }
    }
}