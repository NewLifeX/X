using System;
using System.Collections.Concurrent;

namespace NewLife.Log
{
    /// <summary>控制台输出日志</summary>
    public class ConsoleLog : Logger
    {
        /// <summary>是否使用多种颜色，默认使用</summary>
        public Boolean UseColor { get; set; } = true;

        /// <summary>写日志</summary>
        /// <param name="level"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected override void OnWrite(LogLevel level, String format, params Object[] args)
        {
            var e = WriteLogEventArgs.Current.Set(level).Set(Format(format, args), null);

            if (!UseColor)
            {
                Console.WriteLine(e);
                return;
            }

            lock (this)
            {
                var cc = Console.ForegroundColor;
                cc = level switch
                {
                    LogLevel.Warn => ConsoleColor.Yellow,
                    LogLevel.Error or LogLevel.Fatal => ConsoleColor.Red,
                    _ => GetColor(e.ThreadID),
                };
                var old = Console.ForegroundColor;
                Console.ForegroundColor = cc;
                Console.WriteLine(e);
                Console.ForegroundColor = old;
            }
        }

        static readonly ConcurrentDictionary<Int32, ConsoleColor> dic = new ConcurrentDictionary<Int32, ConsoleColor>();
        static readonly ConsoleColor[] colors = new ConsoleColor[] {
            ConsoleColor.Green, ConsoleColor.Cyan, ConsoleColor.Magenta, ConsoleColor.White, ConsoleColor.Yellow,
            ConsoleColor.DarkGreen, ConsoleColor.DarkCyan, ConsoleColor.DarkMagenta, ConsoleColor.DarkRed, ConsoleColor.DarkYellow };
        private static ConsoleColor GetColor(Int32 threadid)
        {
            if (threadid == 1) return ConsoleColor.Gray;

            return dic.GetOrAdd(threadid, k => colors[k % colors.Length]);
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString() => $"{GetType().Name} UseColor={UseColor}";
    }
}