using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using NewLife.Configuration;
using NewLife.Exceptions;
using NewLife.Reflection;

namespace NewLife.Log
{
    /// <summary>日志类，包含跟踪调试功能</summary>
    /// <remarks>
    /// 该静态类包括写日志、写调用栈和Dump进程内存等调试功能。
    /// 
    /// 默认写日志到文本文件，可通过修改<see cref="Log"/>属性来增加日志输出方式。
    /// 对于控制台工程，可以直接通过<see cref="UseConsole"/>方法，把日志输出重定向为控制台输出，并且可以为不同线程使用不同颜色。
    /// </remarks>
    public static class XTrace
    {
        #region 写日志
        /// <summary>文本文件日志</summary>
        private static ILog _Log;
        /// <summary>日志提供者，默认使用文本文件日志</summary>
        public static ILog Log { get { InitLog(); return _Log; } set { _Log = value; } }

        /// <summary>输出日志</summary>
        /// <param name="msg">信息</param>
        public static void WriteLine(String msg)
        {
            InitLog();
            if (OnWriteLog != null) OnWriteLog(null, WriteLogEventArgs.Current.Set(msg, null, true));

            Log.Info(msg);
        }

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void WriteLine(String format, params Object[] args)
        {
            InitLog();
            if (OnWriteLog != null) OnWriteLog(null, WriteLogEventArgs.Current.Set(String.Format(format, args), null, true));

            Log.Info(format, args);
        }

        /// <summary>输出异常日志</summary>
        /// <param name="ex">异常信息</param>
        //[Obsolete("不再支持！")]
        public static void WriteException(Exception ex)
        {
            InitLog();
            if (OnWriteLog != null) OnWriteLog(null, WriteLogEventArgs.Current.Set(null, ex, true));

            Log.Error("{0}", ex);
        }

        ///// <summary>输出异常日志</summary>
        ///// <param name="ex">异常信息</param>
        //public static void WriteExceptionWhenDebug(Exception ex) { if (Debug) WriteException(ex); }

        /// <summary>写日志事件。</summary>
        [Obsolete("请直接使用CompositeLog实现赋值给Log属性")]
        public static event EventHandler<WriteLogEventArgs> OnWriteLog;
        #endregion

        #region 构造
        static XTrace()
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
        }

        static object _lock = new object();

        /// <summary>
        /// 2012.11.05 修正初次调用的时候，由于同步BUG，导致Log为空的问题。
        /// </summary>
        static void InitLog()
        {
            if (_Log != null) return;

            lock (_lock)
            {
                if (_Log != null) return;
                _Log = TextFileLog.Create(null);
            }

            var asmx = AssemblyX.Create(Assembly.GetExecutingAssembly());
            WriteLine("{0} v{1} Build {2:yyyy-MM-dd HH:mm:ss}", asmx.Name, asmx.FileVersion, asmx.Compile);
        }
        #endregion

        #region 使用控制台输出
        //private static Int32 init = 0;
        /// <summary>使用控制台输出日志，只能调用一次</summary>
        /// <param name="useColor">是否使用颜色，默认使用</param>
        /// <param name="useFileLog">是否同时使用文件日志，默认使用</param>
        public static void UseConsole(Boolean useColor = true, Boolean useFileLog = true)
        {
            //if (init > 0 || Interlocked.CompareExchange(ref init, 1, 0) != 0) return;
            if (!Runtime.IsConsole) return;

            var clg = _Log as ConsoleLog;
            var ftl = _Log as TextFileLog;
            var cmp = _Log as CompositeLog;
            if (cmp != null)
            {
                ftl = cmp.Get<TextFileLog>();
                clg = cmp.Get<ConsoleLog>();
            }

            // 控制控制台日志
            if (clg == null)
                clg = new ConsoleLog { UseColor = useColor };
            else
                clg.UseColor = useColor;

            if (!useFileLog)
            {
                Log = clg;
                if (ftl != null) ftl.Dispose();
            }
            else
            {
                if (ftl == null) ftl = TextFileLog.Create(null);
                Log = new CompositeLog(clg, ftl);
            }
        }
        #endregion

        #region 拦截WinForm异常
        private static Int32 initWF = 0;
        private static Boolean _ShowErrorMessage;
        //private static String _Title;

        /// <summary>拦截WinForm异常并记录日志，可指定是否用<see cref="MessageBox"/>显示。</summary>
        /// <param name="showErrorMessage">发为捕获异常时，是否显示提示，默认显示</param>
        public static void UseWinForm(Boolean showErrorMessage = true)
        {
            _ShowErrorMessage = showErrorMessage;

            if (initWF > 0 || Interlocked.CompareExchange(ref initWF, 1, 0) != 0) return;
            //if (!Application.MessageLoop) return;

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            //AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var show = _ShowErrorMessage && Application.MessageLoop;
            var msg = "" + e.ExceptionObject;
            WriteLine(msg);
            if (e.IsTerminating)
            {
                //WriteLine("异常退出！");
                Log.Fatal("异常退出！");
                //XTrace.WriteMiniDump(null);
                if (show) MessageBox.Show(msg, "异常退出", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                if (show) MessageBox.Show(msg, "出错", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            WriteException(e.Exception);
            if (_ShowErrorMessage && Application.MessageLoop) MessageBox.Show("" + e.Exception, "出错", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        #endregion

        #region 使用WinForm控件输出日志
        /// <summary>在WinForm控件上输出日志，主要考虑非UI线程操作</summary>
        /// <remarks>不是常用功能，为了避免干扰常用功能，保持UseWinForm开头</remarks>
        /// <param name="control">要绑定日志输出的WinForm控件</param>
        /// <param name="useFileLog">是否同时使用文件日志，默认使用</param>
        /// <param name="maxLines">最大行数</param>
        public static void UseWinFormControl(this Control control, Boolean useFileLog = true, Int32 maxLines = 1000)
        {
            //if (handler != null)
            //    OnWriteLog += (s, e) => handler(control, e);
            //else
            //    OnWriteLog += (s, e) => UseWinFormWriteLog(control, e.ToString() + Environment.NewLine, maxLines);
            var clg = _Log as TextControlLog;
            var ftl = _Log as TextFileLog;
            var cmp = _Log as CompositeLog;
            if (cmp != null)
            {
                ftl = cmp.Get<TextFileLog>();
                clg = cmp.Get<TextControlLog>();
            }

            // 控制控制台日志
            if (clg == null) clg = new TextControlLog();
            clg.Control = control;
            clg.MaxLines = maxLines;

            if (!useFileLog)
            {
                Log = clg;
                if (ftl != null) ftl.Dispose();
            }
            else
            {
                if (ftl == null) ftl = TextFileLog.Create(null);
                Log = new CompositeLog(clg, ftl);
            }
        }

        /// <summary>在WinForm控件上输出日志，主要考虑非UI线程操作</summary>
        /// <remarks>不是常用功能，为了避免干扰常用功能，保持UseWinForm开头</remarks>
        /// <param name="control">要绑定日志输出的WinForm控件</param>
        /// <param name="msg">日志</param>
        /// <param name="maxLines">最大行数</param>
        [Obsolete("=>TextControlLog.WriteLog")]
        public static void UseWinFormWriteLog(this Control control, String msg, Int32 maxLines = 1000)
        {
            if (control == null) return;

            TextControlLog.WriteLog(control, msg, maxLines);
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

                try
                {
                    //return Config.GetConfig<Boolean>("NewLife.Debug", Config.GetConfig<Boolean>("Debug", false));
                    return Config.GetMutilConfig<Boolean>(false, "NewLife.Debug", "Debug");
                }
                catch { return false; }
            }
            set { _Debug = value; }
        }

        private static String _LogPath;
        /// <summary>文本日志目录</summary>
        public static String LogPath
        {
            get
            {
                if (_LogPath == null) _LogPath = Config.GetConfig<String>("NewLife.LogPath", "Log");
                return _LogPath;
            }
            set { _LogPath = value; }
        }

        private static String _TempPath;
        /// <summary>临时目录</summary>
        public static String TempPath
        {
            get
            {
                if (_TempPath != null) return _TempPath;

                // 这里是TempPath而不是_TempPath，因为需要格式化处理一下
                TempPath = Config.GetConfig<String>("NewLife.TempPath", "XTemp");
                return _TempPath;
            }
            set
            {
                _TempPath = value;
                if (String.IsNullOrEmpty(_TempPath)) _TempPath = "XTemp";
                if (!Path.IsPathRooted(_TempPath)) _TempPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _TempPath);
                _TempPath = Path.GetFullPath(_TempPath);
            }
        }
        #endregion

        #region Dump
        /// <summary>写当前线程的MiniDump</summary>
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
            private static extern Boolean MiniDumpWriteDump(IntPtr hProcess, Int32 processId, IntPtr fileHandle, MiniDumpType dumpType, ref MinidumpExceptionInfo excepInfo, IntPtr userInfo, IntPtr extInfo);

            /// <summary>MINIDUMP_EXCEPTION_INFORMATION</summary>
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
                using (var stream = new FileStream(dmpPath, FileMode.Create))
                {
                    //取得进程信息
                    var process = Process.GetCurrentProcess();

                    // MINIDUMP_EXCEPTION_INFORMATION 信息的初始化
                    var mei = new MinidumpExceptionInfo();

                    mei.ThreadId = (UInt32)GetCurrentThreadId();
                    mei.ExceptionPointers = Marshal.GetExceptionPointers();
                    mei.ClientPointers = 1;

                    //这里调用的Win32 API
                    var fileHandle = stream.SafeFileHandle.DangerousGetHandle();
                    var res = MiniDumpWriteDump(process.Handle, process.Id, fileHandle, dmpType, ref mei, IntPtr.Zero, IntPtr.Zero);

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

        #region 调用栈
        /// <summary>堆栈调试。
        /// 输出堆栈信息，用于调试时处理调用上下文。
        /// 本方法会造成大量日志，请慎用。
        /// </summary>
        public static void DebugStack()
        {
            var msg = GetCaller(2, 0, Environment.NewLine);
            WriteLine("调用堆栈：" + Environment.NewLine + msg);
        }

        /// <summary>堆栈调试。</summary>
        /// <param name="maxNum">最大捕获堆栈方法数</param>
        public static void DebugStack(int maxNum)
        {
            var msg = GetCaller(2, maxNum, Environment.NewLine);
            WriteLine("调用堆栈：" + Environment.NewLine + msg);
        }

        /// <summary>堆栈调试</summary>
        /// <param name="start">开始方法数，0是DebugStack的直接调用者</param>
        /// <param name="maxNum">最大捕获堆栈方法数</param>
        public static void DebugStack(int start, int maxNum)
        {
            // 至少跳过当前这个
            if (start < 1) start = 1;
            var msg = GetCaller(start + 1, maxNum, Environment.NewLine);
            WriteLine("调用堆栈：" + Environment.NewLine + msg);
        }

        /// <summary>获取调用栈</summary>
        /// <param name="start"></param>
        /// <param name="maxNum"></param>
        /// <param name="split"></param>
        /// <returns></returns>
        public static String GetCaller(int start = 1, int maxNum = 0, String split = null)
        {
            // 至少跳过当前这个
            if (start < 1) start = 1;
            var st = new StackTrace(start, true);

            if (String.IsNullOrEmpty(split)) split = "<-";

            Type last = null;
            var asm = Assembly.GetEntryAssembly();
            var entry = asm == null ? null : asm.EntryPoint;

            int count = st.FrameCount;
            var sb = new StringBuilder(count * 20);
            if (maxNum > 0 && maxNum < count) count = maxNum;
            for (int i = 0; i < count; i++)
            {
                var sf = st.GetFrame(i);
                var method = sf.GetMethod();
                // 跳过<>类型的匿名方法

                if (method == null || String.IsNullOrEmpty(method.Name) || method.Name[0] == '<' && method.Name.Contains(">")) continue;

                var type = method.DeclaringType ?? method.ReflectedType;

                var name = method.ToString();
                // 去掉前面的返回类型
                var p = name.IndexOf(" ");
                if (p >= 0) name = name.Substring(p + 1);
                // 去掉前面的System
                name = name
                    .Replace("System.Web.", null)
                    .Replace("System.", null);

                sb.Append(name);

                // 如果到达了入口点，可以结束了
                if (method == entry) break;

                if (i < count - 1) sb.Append(split);

                last = type;
            }
            return sb.ToString();
        }
        #endregion
    }
}