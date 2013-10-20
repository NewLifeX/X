using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Log
{
    /// <summary>控制台输出日志</summary>
    public class ConsoleLog : Logger
    {
        private Boolean _UseColor = true;
        /// <summary>是否使用多种颜色，默认使用</summary>
        public Boolean UseColor { get { return _UseColor; } set { _UseColor = value; } }

        /// <summary>写日志</summary>
        /// <param name="level"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected override void OnWrite(LogLevel level, String format, params Object[] args)
        {
        }


        private static Boolean LastIsNewLine = true;
        private static void ConsoleWriteLog(WriteLogEventArgs e)
        {
            if (LastIsNewLine)
            {
                // 如果上一次是换行，则这次需要输出行头信息
                if (e.IsNewLine)
                    Console.WriteLine(e.ToString());
                else
                {
                    Console.Write(e.ToString());
                    LastIsNewLine = false;
                }
            }
            else
            {
                // 如果上一次不是换行，则这次不需要行头信息
                var msg = e.Message + e.Exception;
                if (e.IsNewLine)
                {
                    Console.WriteLine(msg);
                    LastIsNewLine = true;
                }
                else
                    Console.Write(msg);
            }
        }

        static Dictionary<Int32, ConsoleColor> dic = new Dictionary<Int32, ConsoleColor>();
        static ConsoleColor[] colors = new ConsoleColor[] { ConsoleColor.White, ConsoleColor.Yellow, ConsoleColor.Magenta, ConsoleColor.Red, ConsoleColor.Cyan, ConsoleColor.Green, ConsoleColor.Blue };
        private static void XTrace_OnWriteLog2(object sender, WriteLogEventArgs e)
        {
            // 好像因为dic.TryGetValue也会引发线程冲突，真是悲剧！
            lock (dic)
            {
                ConsoleColor cc;
                var key = e.ThreadID;
                if (!dic.TryGetValue(key, out cc))
                {
                    //lock (dic)
                    {
                        //if (!dic.TryGetValue(key, out cc))
                        {
                            cc = colors[dic.Count % 7];
                            dic[key] = cc;
                        }
                    }
                }
                var old = Console.ForegroundColor;
                Console.ForegroundColor = cc;
                ConsoleWriteLog(e);
                Console.ForegroundColor = old;
            }
        }
    }
}