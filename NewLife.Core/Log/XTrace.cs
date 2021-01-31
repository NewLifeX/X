using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
#if __WIN__
using System.Windows.Forms;
using NewLife.Net;
#endif
using NewLife.Reflection;
using NewLife.Threading;

#nullable enable
namespace NewLife.Log
{
    /// <summary>日志类，包含跟踪调试功能</summary>
    /// <remarks>
    /// 文档 https://www.yuque.com/smartstone/nx/log
    /// 
    /// 该静态类包括写日志、写调用栈和Dump进程内存等调试功能。
    /// 
    /// 默认写日志到文本文件，可通过修改<see cref="Log"/>属性来增加日志输出方式。
    /// 对于控制台工程，可以直接通过UseConsole方法，把日志输出重定向为控制台输出，并且可以为不同线程使用不同颜色。
    /// </remarks>
    public static class XTrace
    {
        #region 写日志
        /// <summary>文本文件日志</summary>
        private static ILog _Log = Logger.Null;
        /// <summary>日志提供者，默认使用文本文件日志</summary>
        public static ILog Log { get { InitLog(); return _Log; } set { _Log = value; } }

        /// <summary>输出日志</summary>
        /// <param name="msg">信息</param>
        public static void WriteLine(String msg)
        {
            if (!InitLog()) return;

            WriteVersion();

            Log.Info(msg);
        }

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void WriteLine(String format, params Object?[] args)
        {
            if (!InitLog()) return;

            WriteVersion();

            Log.Info(format, args);
        }

        ///// <summary>异步写日志</summary>
        ///// <param name="format"></param>
        ///// <param name="args"></param>
        //public static void WriteLineAsync(String format, params Object[] args)
        //{
        //    ThreadPool.QueueUserWorkItem(s => WriteLine(format, args));
        //}

        /// <summary>输出异常日志</summary>
        /// <param name="ex">异常信息</param>
        public static void WriteException(Exception ex)
        {
            if (!InitLog()) return;

            WriteVersion();

            Log.Error("{0}", ex);
        }
        #endregion

        #region 构造
        static XTrace()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

            ThreadPoolX.Init();
        }

        static void CurrentDomain_UnhandledException(Object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex) WriteException(ex);
            if (e.IsTerminating)
            {
                Log.Fatal("异常退出！");

                if (Log is CompositeLog compositeLog)
                {
                    var log = compositeLog.Get<TextFileLog>();
                    log.TryDispose();
                }
            }
        }

        private static void TaskScheduler_UnobservedTaskException(Object? sender, UnobservedTaskExceptionEventArgs e)
        {
            if (!e.Observed)
            {
                //WriteException(e.Exception);
                foreach (var ex in e.Exception.Flatten().InnerExceptions)
                {
                    WriteException(ex);
                }
                e.SetObserved();
            }
        }

        private static void OnProcessExit(Object? sender, EventArgs e)
        {
            if (Log is CompositeLog compositeLog)
            {
                var log = compositeLog.Get<TextFileLog>();
                log.TryDispose();
            }
        }

        static readonly Object _lock = new Object();
        static Int32 _initing = 0;

        /// <summary>
        /// 2012.11.05 修正初次调用的时候，由于同步BUG，导致Log为空的问题。
        /// </summary>
        static Boolean InitLog()
        {
            /*
             * 日志初始化可能会触发配置模块，其内部又写日志导致死循环。
             * 1，外部写日志引发初始化
             * 2，标识日志初始化正在进行中
             * 3，初始化日志提供者
             * 4，此时如果再次引发写入日志，发现正在进行中，放弃写入的日志
             * 5，标识日志初始化已完成
             * 6，正常写入日志
             */

            if (_Log != null && _Log != Logger.Null) return true;
            if (_initing > 0 && _initing == Thread.CurrentThread.ManagedThreadId) return false;

            lock (_lock)
            {
                if (_Log != null && _Log != Logger.Null) return true;

                _initing = Thread.CurrentThread.ManagedThreadId;

                var set = Setting.Current;
                if (set.LogFileFormat.Contains("{1}"))
                    _Log = new LevelLog(LogPath, set.LogFileFormat);
                else
                    _Log = TextFileLog.Create(LogPath);

                if (!set.NetworkLog.IsNullOrEmpty())
                {
                    var nlog = new NetworkLog(set.NetworkLog);
                    _Log = new CompositeLog(_Log, nlog);
                }

                _initing = 0;
            }

            //WriteVersion();

            return true;
        }
        #endregion

        #region 使用控制台输出
        private static Boolean _useConsole;
        /// <summary>使用控制台输出日志，只能调用一次</summary>
        /// <param name="useColor">是否使用颜色，默认使用</param>
        /// <param name="useFileLog">是否同时使用文件日志，默认使用</param>
        public static void UseConsole(Boolean useColor = true, Boolean useFileLog = true)
        {
            if (_useConsole) return;
            _useConsole = true;

            //if (!Runtime.IsConsole) return;
            Runtime.IsConsole = true;

            // 适当加大控制台窗口
            try
            {
#if __CORE__
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    if (Console.WindowWidth <= 80) Console.WindowWidth = Console.WindowWidth * 3 / 2;
                    if (Console.WindowHeight <= 25) Console.WindowHeight = Console.WindowHeight * 3 / 2;
                }
#else
                    if (Console.WindowWidth <= 80) Console.WindowWidth = Console.WindowWidth * 3 / 2;
                    if (Console.WindowHeight <= 25) Console.WindowHeight = Console.WindowHeight * 3 / 2;
#endif
            }
            catch { }

            var clg = new ConsoleLog { UseColor = useColor };
            if (useFileLog)
                _Log = new CompositeLog(clg, Log);
            else
                _Log = clg;
        }
        #endregion

        #region 拦截WinForm异常
#if __WIN__
        private static Int32 initWF = 0;
        private static Boolean _ShowErrorMessage;
        //private static String _Title;

        /// <summary>拦截WinForm异常并记录日志，可指定是否用<see cref="MessageBox"/>显示。</summary>
        /// <param name="showErrorMessage">发为捕获异常时，是否显示提示，默认显示</param>
        public static void UseWinForm(Boolean showErrorMessage = true)
        {
            Runtime.IsConsole = false;

            _ShowErrorMessage = showErrorMessage;

            if (initWF > 0 || Interlocked.CompareExchange(ref initWF, 1, 0) != 0) return;
            //if (!Application.MessageLoop) return;

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException2;
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += Application_ThreadException;
        }

        static void CurrentDomain_UnhandledException2(Object sender, UnhandledExceptionEventArgs e)
        {
            var show = _ShowErrorMessage && Application.MessageLoop;
            var ex = e.ExceptionObject as Exception;
            var title = e.IsTerminating ? "异常退出" : "出错";
            if (show) MessageBox.Show(ex?.Message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        static void Application_ThreadException(Object sender, ThreadExceptionEventArgs e)
        {
            WriteException(e.Exception);

            var show = _ShowErrorMessage && Application.MessageLoop;
            if (show) MessageBox.Show(e.Exception == null ? "" : e.Exception.Message, "出错", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>在WinForm控件上输出日志，主要考虑非UI线程操作</summary>
        /// <remarks>不是常用功能，为了避免干扰常用功能，保持UseWinForm开头</remarks>
        /// <param name="control">要绑定日志输出的WinForm控件</param>
        /// <param name="useFileLog">是否同时使用文件日志，默认使用</param>
        /// <param name="maxLines">最大行数</param>
        public static void UseWinFormControl(this Control control, Boolean useFileLog = true, Int32 maxLines = 1000)
        {
            var clg = _Log as TextControlLog;
            var ftl = _Log as TextFileLog;
            if (_Log is CompositeLog cmp)
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

        /// <summary>控件绑定到日志，生成混合日志</summary>
        /// <param name="control"></param>
        /// <param name="log"></param>
        /// <param name="maxLines"></param>
        /// <returns></returns>
        public static ILog Combine(this Control control, ILog log, Int32 maxLines = 1000)
        {
            //if (control == null || log == null) return log;

            var clg = new TextControlLog
            {
                Control = control,
                MaxLines = maxLines
            };

            return new CompositeLog(log, clg);
        }
#endif
        #endregion

        #region 属性
        /// <summary>是否调试。</summary>
        public static Boolean Debug { get; set; } = Setting.Current.Debug;

        /// <summary>文本日志目录</summary>
        public static String LogPath { get; set; } = Setting.Current.LogPath;

        ///// <summary>临时目录</summary>
        //public static String TempPath { get; set; } = Setting.Current.TempPath;
        #endregion

        #region 版本信息
        private static Int32 _writeVersion;
        /// <summary>输出核心库和启动程序的版本号</summary>
        public static void WriteVersion()
        {
            if (_writeVersion > 0 || Interlocked.CompareExchange(ref _writeVersion, 1, 0) != 0) return;

            var asm = Assembly.GetExecutingAssembly();
            WriteVersion(asm);

            var asm2 = Assembly.GetEntryAssembly();
            if (asm2 != null && asm2 != asm) WriteVersion(asm2);
        }

        /// <summary>输出程序集版本</summary>
        /// <param name="asm"></param>
        public static void WriteVersion(this Assembly asm)
        {
            if (asm == null) return;

            var asmx = AssemblyX.Create(asm);
            if (asmx != null)
            {
                var ver = "";
                var tar = asm.GetCustomAttribute<TargetFrameworkAttribute>();
                if (tar != null)
                {
                    ver = tar.FrameworkDisplayName;
                    if (ver.IsNullOrEmpty()) ver = tar.FrameworkName;
                }

                WriteLine("{0} v{1} Build {2:yyyy-MM-dd HH:mm:ss} {3}", asmx.Name, asmx.FileVersion, asmx.Compile, ver);
                var att = asmx.Asm.GetCustomAttribute<AssemblyCopyrightAttribute>();
                WriteLine("{0} {1}", asmx.Title, att?.Copyright);
            }
        }
        #endregion
    }
}
#nullable restore