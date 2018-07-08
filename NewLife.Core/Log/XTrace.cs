using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#if __MOBILE__
#elif __CORE__
#else
using System.Windows.Forms;
#endif
using NewLife.Reflection;
using NewLife.Threading;

namespace NewLife.Log
{
    /// <summary>日志类，包含跟踪调试功能</summary>
    /// <remarks>
    /// 该静态类包括写日志、写调用栈和Dump进程内存等调试功能。
    /// 
    /// 默认写日志到文本文件，可通过修改<see cref="Log"/>属性来增加日志输出方式。
    /// 对于控制台工程，可以直接通过UseConsole方法，把日志输出重定向为控制台输出，并且可以为不同线程使用不同颜色。
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
            if (!InitLog()) return;

            Log.Info(msg);
        }

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void WriteLine(String format, params Object[] args)
        {
            if (!InitLog()) return;

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

            Log.Error("{0}", ex);
        }
        #endregion

        #region 构造
        static XTrace()
        {
#if __CORE__
#else
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
#endif
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            ThreadPoolX.Init();
        }

#if __MOBILE__
        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var msg = "" + e.ExceptionObject;
            WriteLine(msg);
            if (e.IsTerminating)
            {
                Log.Fatal("异常退出！");
            }
        }
#endif

        private static void TaskScheduler_UnobservedTaskException(Object sender, UnobservedTaskExceptionEventArgs e)
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

        static Object _lock = new Object();
        static Int32 _initing = 0;

        /// <summary>
        /// 2012.11.05 修正初次调用的时候，由于同步BUG，导致Log为空的问题。
        /// </summary>
        static Boolean InitLog()
        {
            /*
             * 日志初始化可能会除法配置模块，其内部又写日志导致死循环。
             * 1，外部写日志引发初始化
             * 2，标识日志初始化正在进行中
             * 3，初始化日志提供者
             * 4，此时如果再次引发写入日志，发现正在进行中，放弃写入的日志
             * 5，标识日志初始化已完成
             * 6，正常写入日志
             */

            if (_Log != null) return true;
            if (_initing > 0 && _initing == Thread.CurrentThread.ManagedThreadId) return false;

            lock (_lock)
            {
                if (_Log != null) return true;

                _initing = Thread.CurrentThread.ManagedThreadId;
#if !__MOBILE__
                _Log = TextFileLog.Create(LogPath);
#else
                _Log = new NetworkLog();
#endif

                var set = Setting.Current;
                if (!set.NetworkLog.IsNullOrEmpty())
                {
                    var nlog = new NetworkLog(NetHelper.ParseEndPoint(set.NetworkLog, 514));
                    _Log = new CompositeLog(_Log, nlog);
                }

                _initing = 0;
            }

            WriteVersion();

            return true;
        }
        #endregion

        #region 使用控制台输出
#if !__MOBILE__
        private static Boolean _useConsole;
        /// <summary>使用控制台输出日志，只能调用一次</summary>
        /// <param name="useColor">是否使用颜色，默认使用</param>
        /// <param name="useFileLog">是否同时使用文件日志，默认使用</param>
        public static void UseConsole(Boolean useColor = true, Boolean useFileLog = true)
        {
            if (_useConsole) return;
            _useConsole = true;

#if !__CORE__
            if (!Runtime.IsConsole) return;
#endif

            // 适当加大控制台窗口
            try
            {
                if (Console.WindowWidth <= 80) Console.WindowWidth = Console.WindowWidth * 3 / 2;
                if (Console.WindowHeight <= 25) Console.WindowHeight = Console.WindowHeight * 3 / 2;
            }
            catch { }

            var clg = new ConsoleLog { UseColor = useColor };
            if (useFileLog)
                _Log = new CompositeLog(clg, Log);
            else
                _Log = clg;
        }
#endif
        #endregion

        #region 拦截WinForm异常
#if __MOBILE__
#elif __CORE__
#else
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
            Application.ThreadException += Application_ThreadException;
        }

        static void CurrentDomain_UnhandledException(Object sender, UnhandledExceptionEventArgs e)
        {
            var show = _ShowErrorMessage && Application.MessageLoop;
            var ex = e.ExceptionObject as Exception;
            var msg = ex == null ? "" : ex.Message;
            WriteException(ex);
            if (e.IsTerminating)
            {
                Log.Fatal("异常退出！" + msg);
                if (show) MessageBox.Show(msg, "异常退出", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                ex = ex.GetTrue();
                if (ex != null) Log.Error(ex.Message);
                if (show) MessageBox.Show(msg, "出错", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
            if (control == null || log == null) return log;

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

        /// <summary>临时目录</summary>
        public static String TempPath { get; set; } = Setting.Current.TempPath;
        #endregion

        #region 调用栈
#if __CORE__
#else
        /// <summary>堆栈调试。
        /// 输出堆栈信息，用于调试时处理调用上下文。
        /// 本方法会造成大量日志，请慎用。
        /// </summary>
        public static void DebugStack()
        {
            var msg = GetCaller(2, 16, Environment.NewLine);
            WriteLine("调用堆栈：" + Environment.NewLine + msg);
        }

        /// <summary>堆栈调试。</summary>
        /// <param name="maxNum">最大捕获堆栈方法数</param>
        public static void DebugStack(Int32 maxNum)
        {
            var msg = GetCaller(2, maxNum, Environment.NewLine);
            WriteLine("调用堆栈：" + Environment.NewLine + msg);
        }

        /// <summary>堆栈调试</summary>
        /// <param name="start">开始方法数，0是DebugStack的直接调用者</param>
        /// <param name="maxNum">最大捕获堆栈方法数</param>
        public static void DebugStack(Int32 start, Int32 maxNum)
        {
            // 至少跳过当前这个
            if (start < 1) start = 1;
            var msg = GetCaller(start + 1, maxNum, Environment.NewLine);
            WriteLine("调用堆栈：" + Environment.NewLine + msg);
        }

        /// <summary>获取调用栈</summary>
        /// <param name="start">要跳过的方法数，默认1，也就是跳过GetCaller</param>
        /// <param name="maxNum">最大层数</param>
        /// <param name="split">分割符号，默认左箭头加上换行</param>
        /// <returns></returns>
        public static String GetCaller(Int32 start = 1, Int32 maxNum = 0, String split = null)
        {
            // 至少跳过当前这个
            if (start < 1) start = 1;
            var st = new StackTrace(start, true);

            if (String.IsNullOrEmpty(split)) split = "<-" + Environment.NewLine;

            Type last = null;
            var asm = Assembly.GetEntryAssembly();
            var entry = asm?.EntryPoint;

            var count = st.FrameCount;
            var sb = new StringBuilder(count * 20);
            //if (maxNum > 0 && maxNum < count) count = maxNum;
            for (var i = 0; i < count && maxNum > 0; i++)
            {
                var sf = st.GetFrame(i);
                var method = sf.GetMethod();

                // 跳过<>类型的匿名方法
                if (method == null || String.IsNullOrEmpty(method.Name) || method.Name[0] == '<' && method.Name.Contains(">")) continue;

                // 跳过有[DebuggerHidden]特性的方法
                if (method.GetCustomAttribute<DebuggerHiddenAttribute>() != null) continue;

                var type = method.DeclaringType ?? method.ReflectedType;
                if (type != null) sb.Append(type.Name);
                sb.Append(".");

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

                maxNum--;
            }
            return sb.ToString();
        }
#endif
        #endregion

        #region 版本信息
        /// <summary>输出核心库和启动程序的版本号</summary>
        public static void WriteVersion()
        {
#if __CORE__
            var asm2 = Assembly.GetEntryAssembly();
            WriteVersion(asm2);
#else
            var asm = Assembly.GetExecutingAssembly();
            WriteVersion(asm);

            var asm2 = Assembly.GetEntryAssembly();
            if (asm2 != asm) WriteVersion(asm2);
#endif
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
                if (tar != null) ver = tar.FrameworkDisplayName ?? tar.FrameworkName;

                WriteLine("{0} v{1} Build {2:yyyy-MM-dd HH:mm:ss} {3}", asmx.Name, asmx.FileVersion, asmx.Compile, ver);
                var att = asmx.Asm.GetCustomAttribute<AssemblyCopyrightAttribute>();
                WriteLine("{0} {1}", asmx.Title, att?.Copyright);
            }
        }
        #endregion
    }
}