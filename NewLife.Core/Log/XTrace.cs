using System;
using NewLife.Configuration;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;

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
        /// 输出异常日志
        /// </summary>
        /// <param name="ex">异常信息</param>
        public static void WriteException(Exception ex)
        {
            Log.WriteLine(ex.ToString());
        }

        /// <summary>
        /// 输出异常日志
        /// </summary>
        /// <param name="ex">异常信息</param>
        public static void WriteExceptionWhenDebug(Exception ex)
        {
            if (Debug) Log.WriteLine(ex.ToString());
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

        #region Dump
        /// <summary>
        /// 写当前线程的MiniDump
        /// </summary>
        /// <param name="dumpFile">如果不指定，则自动写入日志目录</param>
        public static void WriteMiniDump(String dumpFile)
        {
            if (String.IsNullOrEmpty(dumpFile))
            {
                dumpFile = String.Format("{0:yyyyMMdd_HHmmss}.dmp", DateTime.Now);
                if (!String.IsNullOrEmpty(LogPath)) dumpFile = Path.Combine(LogPath, dumpFile);
            }

            MiniDump.TryDump(dumpFile, MiniDump.MiniDumpType.WithFullMemory);
        }

        /// <summary>
        /// 该类要使用在windows 5.1 以后的版本，如果你的windows很旧，就把Windbg里面的dll拷贝过来，一般都没有问题。
        /// DbgHelp.dll 是windows自带的 dll文件 。
        /// </summary>
        static class MiniDump
        {
            [DllImport("DbgHelp.dll")]
            private static extern Boolean MiniDumpWriteDump(
            IntPtr hProcess,
            Int32 processId,
            IntPtr fileHandle,
            MiniDumpType dumpType,
           ref MinidumpExceptionInfo excepInfo,
            IntPtr userInfo,
            IntPtr extInfo);

            /// <summary>
            /// MINIDUMP_EXCEPTION_INFORMATION
            /// </summary>
            struct MinidumpExceptionInfo
            {
                public UInt32 ThreadId;
                public IntPtr ExceptionPointers;
                public UInt32 ClientPointers;
            }

            [DllImport("kernel32.dll")]
            private static extern uint GetCurrentThreadId();

            public static Boolean TryDump(String dmpPath, MiniDumpType dmpType)
            {
                //使用文件流来创健 .dmp文件
                using (FileStream stream = new FileStream(dmpPath, FileMode.Create))
                {
                    //取得进程信息
                    Process process = Process.GetCurrentProcess();

                    // MINIDUMP_EXCEPTION_INFORMATION 信息的初始化
                    MinidumpExceptionInfo mei = new MinidumpExceptionInfo();

                    mei.ThreadId = (UInt32)GetCurrentThreadId();
                    mei.ExceptionPointers = Marshal.GetExceptionPointers();
                    mei.ClientPointers = 1;

                    //这里调用的Win32 API
                    Boolean res = MiniDumpWriteDump(
                    process.Handle,
                    process.Id,
                    stream.SafeFileHandle.DangerousGetHandle(),
                    dmpType,
                   ref mei,
                    IntPtr.Zero,
                    IntPtr.Zero);

                    //清空 stream
                    stream.Flush();
                    stream.Close();

                    return res;
                }
            }

            public enum MiniDumpType
            {
                None = 0x00010000,
                Normal = 0x00000000,
                WithDataSegs = 0x00000001,
                WithFullMemory = 0x00000002,
                WithHandleData = 0x00000004,
                FilterMemory = 0x00000008,
                ScanMemory = 0x00000010,
                WithUnloadedModules = 0x00000020,
                WithIndirectlyReferencedMemory = 0x00000040,
                FilterModulePaths = 0x00000080,
                WithProcessThreadData = 0x00000100,
                WithPrivateReadWriteMemory = 0x00000200,
                WithoutOptionalData = 0x00000400,
                WithFullMemoryInfo = 0x00000800,
                WithThreadInfo = 0x00001000,
                WithCodeSegs = 0x00002000
            }
        }
        #endregion
    }
}