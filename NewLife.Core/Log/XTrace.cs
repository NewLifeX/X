using System;
using NewLife.Configuration;

//namespace XLog
//{
//    /// <summary>
//    /// 日志类，包含跟踪调试功能
//    /// </summary>
//    public class XTrace : NewLife.Log.XTrace { }
//}

namespace NewLife.Log
{
    /// <summary>
    /// 日志类，包含跟踪调试功能
    /// </summary>
    public class XTrace
    {
        #region 写日志
        private static TextFileLog Log = TextFileLog.Create(Config.GetConfig<String>("NewLife.LogPath"));
        /// <summary>
        /// 日志路径
        /// </summary>
        public static String LogPath { get { return Log.LogPath; } }

        /// <summary>
        /// 输出日志
        /// </summary>
        /// <param name="msg">信息</param>
        public static void WriteLine(String msg)
        {
            Log.WriteLine(msg);
        }

        /// <summary>
        /// 堆栈调试。
        /// 输出堆栈信息，用于调试时处理调用上下文。
        /// 本方法会造成大量日志，请慎用。
        /// </summary>
        public static void DebugStack()
        {
            Log.DebugStack();
        }

        /// <summary>
        /// 堆栈调试。
        /// </summary>
        /// <param name="maxNum">最大捕获堆栈方法数</param>
        public static void DebugStack(int maxNum)
        {
            Log.DebugStack(maxNum);
        }

        /// <summary>
        /// 写日志事件。绑定该事件后，XTrace将不再把日志写到日志文件中去。
        /// </summary>
        public static event EventHandler<WriteLogEventArgs> OnWriteLog
        {
            add { Log.OnWriteLog += value; }
            remove { Log.OnWriteLog -= value; }
        }
        //public static event EventHandler<WriteLogEventArgs> OnWriteLog;

        /// <summary>
        /// 写日志
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void WriteLine(String format, params Object[] args)
        {
            Log.WriteLine(format, args);
        }
        #endregion

        #region 属性
        private static Boolean? _Debug;
        /// <summary>是否调试。如果代码指定了值，则只会使用代码指定的值，否则每次都读取配置。</summary>
        public static Boolean Debug
        {
            get
            {
                if (_Debug != null) return _Debug.Value;
                //String str = ConfigurationManager.AppSettings["NewLife.Debug"];
                //if (String.IsNullOrEmpty(str)) str = ConfigurationManager.AppSettings["Debug"];
                //if (String.IsNullOrEmpty(str)) return false;
                //if (str == "1") return true;
                //if (str == "0") return false;
                //if (str.Equals(Boolean.FalseString, StringComparison.OrdinalIgnoreCase)) return false;
                //if (str.Equals(Boolean.TrueString, StringComparison.OrdinalIgnoreCase)) return true;
                //return false;

                return Config.GetConfig<Boolean>("NewLife.Debug", Config.GetConfig<Boolean>("Debug", false));
            }
            set { _Debug = value; }
        }
        #endregion
    }
}