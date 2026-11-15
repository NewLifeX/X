using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

#if WINDOWS || NETFRAMEWORK

using System.Windows.Forms;

#endif

using NewLife.Reflection;
using NewLife.Threading;
using NewLife.Windows;

namespace NewLife.Log;

/// <summary>日志类，包含跟踪调试功能</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/log
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
    public static ILog Log { get { InitLog(); return _Log; } set { _Log = value; _initFailed = false; } }

    /// <summary>输出日志</summary>
    /// <param name="msg">信息</param>
    public static void WriteLine(String msg)
    {
        // 只过滤null，保留包含空格的字符串
        if (msg == null) return;

        if (!InitLog()) return;

        WriteVersion();

        Log.Info(msg);
    }

    /// <summary>写日志</summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="args">格式化参数</param>
    public static void WriteLine(String format, params Object?[] args)
    {
        // 只过滤null，保留包含空格的字符串
        if (format == null) return;

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

    #endregion 写日志

    #region 构造

    static XTrace()
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
#if NETCOREAPP
        System.Runtime.Loader.AssemblyLoadContext.Default.Unloading += ctx => OnProcessExit(null, EventArgs.Empty);
#endif

        ThreadPoolX.Init();

        // 这里不能读 Setting.Current：配置初始化会写日志（FileConfigProvider.OnWrite → XTrace.WriteLine），
        // 两个线程分别首次进入 XTrace 与 Config<Setting> 时，会各自等待对方的类型初始化，形成死锁。
        // 需要配置值的 Debug/LogPath 改为首次访问时延迟加载。
    }

    private static void CurrentDomain_UnhandledException(Object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            // 全局异常埋点
            DefaultTracer.Instance?.NewError(ex.GetType().Name, ex);
            WriteException(ex);
        }
        if (e.IsTerminating)
        {
            Log.Fatal("Unhandled fatal error; exiting.");

            OnProcessExit(null, EventArgs.Empty);
        }
    }

    private static void TaskScheduler_UnobservedTaskException(Object? sender, UnobservedTaskExceptionEventArgs e)
    {
        if (!e.Observed && e.Exception != null)
        {
            //WriteException(e.Exception);
            foreach (var ex in e.Exception.Flatten().InnerExceptions)
            {
                // 全局异常埋点
                DefaultTracer.Instance?.NewError(ex.GetType().Name, ex);
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
        else
        {
            Log.TryDispose();
        }
    }

    private static readonly Object _lock = new();
    private static Boolean _initFailed;

    /// <summary>本线程是否正在初始化日志。配置初始化会写日志并再次进入本方法，用它挡住同线程递归</summary>
    [ThreadStatic]
    private static Boolean _initing;

    /// <summary>
    /// 2012.11.05 修正初次调用的时候，由于同步BUG，导致Log为空的问题。
    /// </summary>
    private static Boolean InitLog()
    {
        /*
         * 日志初始化可能会触发配置模块，其内部又写日志导致死循环。
         * 1，外部写日志引发初始化
         * 2，标识日志初始化正在进行中（按线程，配置初始化写日志会再次进入本方法）
         * 3，初始化日志提供者
         * 4，此时如果再次引发写入日志，发现正在进行中，放弃写入的日志
         * 5，标识日志初始化已完成
         * 6，正常写入日志
         */

        if (_Log != null && _Log != Logger.Null) return true;
        if (_initFailed) return false;
        if (_initing) return false;

        _initing = true;
        try
        {
            // 先在锁外读配置：配置首次初始化会反过来写日志并进入本方法（FileConfigProvider.OnWrite），
            // 持锁等待配置初始化就会与配置侧形成环形等待
            Setting? set = null;
            try { set = Setting.Current; } catch { }

            // 日志目录同样在锁外解析，避免持锁时经属性 getter 触发配置读取
            var logPath = LogPath;
            if (logPath.IsNullOrEmpty() || logPath == "Log") logPath = set?.LogPath;

            lock (_lock)
            {
                if (_Log != null && _Log != Logger.Null) return true;

                // 配置异常：退化为静默日志且不再重试
                if (set == null)
                {
                    _Log = Logger.Null;
                    _initFailed = true;

                    return true;
                }

                try
                {
                    logPath ??= set.LogPath;
                    if (set.LogFileFormat.Contains("{1}"))
                        _Log = new LevelLog(logPath, set.LogFileFormat);
                    else
                        _Log = TextFileLog.Create(logPath);

                    if (!set.NetworkLog.IsNullOrEmpty())
                    {
                        var nlog = new NetworkLog(set.NetworkLog);
                        _Log = new CompositeLog(_Log, nlog);
                    }
                }
                catch
                {
                    // 配置异常时不能把异常抛给写日志的调用方：
                    // 调用方可能是线程池任务或终结器，异常一旦外抛会中断业务流程甚至杀死进程。
                    // 退化为静默日志且不再重试，与日志系统静态构造中的处理保持一致
                    _Log = Logger.Null;
                    _initFailed = true;
                }
            }
        }
        finally
        {
            _initing = false;
        }

        //WriteVersion();

        return true;
    }

    #endregion 构造

    #region 使用控制台输出

    private static Boolean _useConsole;

    /// <summary>使用控制台输出日志。如果是后台服务则只用文本日志不用控制台日志</summary>
    public static void UseConsole()
    {
        if (Environment.UserInteractive)
        {
            UseConsole(true, true);
        }
        else
        {
            InitLog();
            //UseConsole(true, true);
        }
    }

    /// <summary>使用控制台输出日志，只能调用一次</summary>
    /// <param name="useColor">是否使用颜色，默认使用</param>
    /// <param name="useFileLog">是否同时使用文件日志，默认使用</param>
    public static void UseConsole(Boolean useColor, Boolean useFileLog = true)
    {
        if (_useConsole) return;
        _useConsole = true;

        //if (!Runtime.IsConsole) return;
        Runtime.IsConsole = true;

        // 适当加大控制台窗口
        try
        {
#if !NETFRAMEWORK
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

    #endregion 使用控制台输出

    #region 控制台禁用快捷编辑

    /// <summary>
    /// 禁用控制台快捷编辑，在UseConsole方法之后调用
    /// </summary>
    public static void DisableConsoleEdit()
    {
        if (!_useConsole) return;
        try
        {
            if (Runtime.Windows)
            {
                ConsoleHelper.DisableQuickEditMode();
            }
        }
        catch { }
    }

    /// <summary>
    /// 禁用控制台关闭按钮
    /// </summary>
    /// <param name="consoleTitle">控制台程序名称，可使用Console.Title动态设置的值</param>
    public static void DisableConsoleCloseButton(String consoleTitle)
    {
        try
        {
            if (Runtime.Windows)
            {
                ConsoleHelper.DisableCloseButton(consoleTitle);
            }
        }
        catch { }
    }

    #endregion 控制台禁用关闭按钮

    #region 拦截WinForm异常

#if WINDOWS || NETFRAMEWORK
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

    private static void CurrentDomain_UnhandledException2(Object sender, UnhandledExceptionEventArgs e)
    {
        var show = _ShowErrorMessage && Application.MessageLoop;
        var ex = e.ExceptionObject as Exception;
        var title = e.IsTerminating ? L.T("Fatal error") : L.T("Error");
        if (show) MessageBox.Show(ex?.Message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private static void Application_ThreadException(Object sender, ThreadExceptionEventArgs e)
    {
        WriteException(e.Exception);

        var show = _ShowErrorMessage && Application.MessageLoop;
        if (show) MessageBox.Show(e.Exception == null ? "" : e.Exception.Message, L.T("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        clg ??= new TextControlLog();
        clg.Control = control;
        clg.MaxLines = maxLines;

        if (!useFileLog)
        {
            Log = clg;
            ftl?.Dispose();
        }
        else
        {
            ftl ??= TextFileLog.Create(String.Empty);
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

    #endregion 拦截WinForm异常

    #region 属性

    private static Int32 _settingLoaded;
    private static Setting? _setting;
    private static Boolean? _debug;
    private static String? _logPath;

    /// <summary>是否调试。默认读取配置 Debug</summary>
    public static Boolean Debug
    {
        get => _debug ?? LoadSetting()?.Debug ?? false;
        set => _debug = value;
    }

    /// <summary>文本日志目录。默认读取配置 LogPath</summary>
    public static String? LogPath
    {
        get => _logPath ?? LoadSetting()?.LogPath;
        set => _logPath = value;
    }

    /// <summary>首次访问时延迟读取配置</summary>
    /// <remarks>
    /// 放在静态构造里会与 <c>Config&lt;Setting&gt;</c> 的初始化互等：配置初始化会写日志（FileConfigProvider.OnWrite），
    /// 日志又要求 XTrace 完成初始化。这里用无锁门闸，首个进入者加载，其余线程先用默认值，任何情况下都不阻塞。
    /// 加载只写配置快照，属性显式赋值优先于快照，避免赋值与加载并发时被覆盖。
    /// </remarks>
    /// <returns>配置快照；加载失败或其它线程正在加载时返回空</returns>
    private static Setting? LoadSetting()
    {
        if (Volatile.Read(ref _settingLoaded) == 2) return _setting;
        if (Interlocked.CompareExchange(ref _settingLoaded, 1, 0) != 0) return null;

        try
        {
            _setting = Setting.Current;
        }
        catch
        {
            // 配置异常时保持默认值，与写日志路径的处理一致：不能把异常抛给调用方
        }
        finally
        {
            Volatile.Write(ref _settingLoaded, 2);
        }

        return _setting;
    }

    ///// <summary>临时目录</summary>
    //public static String TempPath { get; set; } = Setting.Current.TempPath;

    #endregion 属性

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
            try
            {
                var tar = asm.GetCustomAttribute<TargetFrameworkAttribute>();
                if (tar != null)
                {
                    ver = tar.FrameworkDisplayName;
                    if (ver.IsNullOrEmpty()) ver = tar.FrameworkName;
                }
            }
            catch { }
            var copyright = "";
            try
            {
                var att = asm.GetCustomAttribute<AssemblyCopyrightAttribute>();
                copyright = att?.Copyright;
            }
            catch { }

            WriteLine("{0} v{1} Build {2:yyyy-MM-dd HH:mm:ss} {3}", asmx.Name, asmx.FileVersion, asmx.Compile, ver);
            WriteLine("{0} {1}", asmx.Title, copyright);
        }
    }

    #endregion 版本信息
}