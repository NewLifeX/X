using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Log
{
    /// <summary>复合日志提供者，多种方式输出</summary>
    public class CompositeLog : Logger
    {
        /// <summary>日志提供者集合</summary>
        public List<ILog> Logs { get; set; } = new List<ILog>();

        /// <summary>日志等级，只输出大于等于该级别的日志，默认Info，打开NewLife.Debug时默认为最低的Debug</summary>
        public override LogLevel Level
        {
            get => base.Level; set
            {
                base.Level = value;

                foreach (var item in Logs)
                {
                    // 使用外层层级
                    item.Level = Level;
                }
            }
        }

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
        public CompositeLog Remove(ILog log) { if (Logs.Contains(log)) Logs.Remove(log); return this; }

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
                    if (item is CompositeLog cmp)
                    {
                        var log = cmp.Get<TLog>();
                        if (log != null) return log;
                    }
                }
            }

            return null;
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString()
        {
            var sb = new StringBuilder();
            sb.Append(GetType().Name);

            foreach (var item in Logs)
            {
                sb.Append(" ");
                sb.Append(item + "");
            }

            return sb.ToString();
        }
    }
}