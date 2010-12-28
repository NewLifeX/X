using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Web;

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
        #region 初始化处理
        private static StreamWriter LogWriter;
        private static String _LogDir;
        private static Object _LogDir_Lock = new object();
        /// <summary>
        /// 日志目录
        /// </summary>
        public static String LogPath
        {
            get
            {
                if (!String.IsNullOrEmpty(_LogDir)) return _LogDir;
                lock (_LogDir_Lock)
                {
                    if (!String.IsNullOrEmpty(_LogDir)) return _LogDir;

                    String dir = String.Empty;
                    if (ConfigurationManager.AppSettings["NewLife.LogPath"] != null) //读取配置
                    {
                        dir = ConfigurationManager.AppSettings["NewLife.LogPath"].ToString();
                        if (HttpContext.Current != null && dir.Substring(1, 1) != @":")
                            dir = HttpContext.Current.Server.MapPath(dir);
                    }
                    else if (HttpContext.Current != null) //网站使用默认日志目录
                    {
                        dir = HttpContext.Current.Server.MapPath("~/Log/");
                    }
                    else //使用应用程序目录
                    {
                        dir = AppDomain.CurrentDomain.BaseDirectory;
                        if (!String.IsNullOrEmpty(AppDomain.CurrentDomain.RelativeSearchPath))
                            dir = Path.Combine(dir, AppDomain.CurrentDomain.RelativeSearchPath);
                        //检查是否在网站中的Bin目录下，多线程的时候，就无法取得HttpContext.Current
                        //从而不知道当前是WinForm还是网站
                        if (dir.ToLower().EndsWith(@"\bin"))
                        {
                            String str = dir.Substring(0, dir.Length - @"bin".Length);
                            if (File.Exists(Path.Combine(str, "web.config"))) dir = str;
                        }
                        dir = Path.Combine(dir, @"Log\");
                    }

                    //保证\结尾
                    if (!String.IsNullOrEmpty(dir) && dir.Substring(dir.Length - 1, 1) != @"\") dir += @"\";

                    _LogDir = dir;
                    return _LogDir;
                }
            }
        }

        /// <summary>
        /// 是否当前进程的第一次写日志
        /// </summary>
        private static Boolean isFirst = false;

        /// <summary>
        /// 初始化日志记录文件
        /// </summary>
        private static void InitLog()
        {
            String path = LogPath;
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            if (path.Substring(path.Length - 2) != @"\") path += @"\";
            String logfile = path + DateTime.Now.ToString("yyyy_MM_dd") + ".log";
            int i = 0;
            while (i < 10)
            {
                try
                {
                    FileStream stream = new FileStream(logfile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                    LogWriter = new StreamWriter(stream, Encoding.UTF8);
                    LogWriter.AutoFlush = true;
                    break;
                }
                catch
                {
                    if (logfile.EndsWith("_" + i + ".log"))
                        logfile = logfile.Replace("_" + i + ".log", "_" + (++i) + ".log");
                    else
                        logfile = logfile.Replace(@".log", @"_0.log");
                }
            }
            if (i >= 10) throw new Exception("无法写入日志！");
            //LogWriter.WriteLine("\r\n\r\n");
            //LogWriter.WriteLine(new String('*', 80));

            //String str = DateTime.Now.ToString("HH:mm:ss");
            //str += " 进程：" + Process.GetCurrentProcess().Id;
            //str += " 线程：" + Thread.CurrentThread.ManagedThreadId;
            //if (Thread.CurrentThread.IsThreadPoolThread) str += "(线程池)";
            //LogWriter.WriteLine(str + "\t开始记录");

            #region 日志头
            if (!isFirst)
            {
                isFirst = true;

                Process process = Process.GetCurrentProcess();
                String name = String.Empty;
                Assembly asm = Assembly.GetEntryAssembly();
                if (asm != null)
                {
                    if (String.IsNullOrEmpty(name))
                    {
                        AssemblyTitleAttribute att = Attribute.GetCustomAttribute(asm, typeof(AssemblyTitleAttribute)) as AssemblyTitleAttribute;
                        if (att != null) name = att.Title;
                    }

                    if (String.IsNullOrEmpty(name))
                    {
                        AssemblyProductAttribute att = Attribute.GetCustomAttribute(asm, typeof(AssemblyProductAttribute)) as AssemblyProductAttribute;
                        if (att != null) name = att.Product;
                    }

                    if (String.IsNullOrEmpty(name))
                    {
                        AssemblyDescriptionAttribute att = Attribute.GetCustomAttribute(asm, typeof(AssemblyDescriptionAttribute)) as AssemblyDescriptionAttribute;
                        if (att != null) name = att.Description;
                    }
                }
                if (String.IsNullOrEmpty(name))
                {
                    try
                    {
                        name = process.MachineName;
                    }
                    catch { }
                }
                // 通过判断LogWriter.BaseStream.Length，解决有时候日志文件为空但仍然加空行的问题
                if (File.Exists(logfile) && LogWriter.BaseStream.Length > 0) LogWriter.WriteLine();
                LogWriter.WriteLine("#Software: {0}", name);
                LogWriter.WriteLine("#ProcessID: {0}", process.Id);
                LogWriter.WriteLine("#BaseDirectory: {0}", AppDomain.CurrentDomain.BaseDirectory);
                LogWriter.WriteLine("#Date: {0:yyyy-MM-dd}", DateTime.Now);
                LogWriter.WriteLine("#Fields: Time ThreadID IsPoolThread ThreadName Message");
            }
            #endregion
        }

        /// <summary>
        /// 停止日志
        /// </summary>
        private static void CloseWriter(Object obj)
        {
            if (LogWriter == null) return;
            lock (Log_Lock)
            {
                try
                {
                    if (LogWriter == null) return;
                    LogWriter.Close();
                    LogWriter.Dispose();
                    LogWriter = null;
                }
                catch { }
            }
        }
        #endregion

        #region 异步写日志
        private static Timer AutoCloseWriterTimer;
        private static object Log_Lock = new object();
        /// <summary>
        /// 使用线程池线程异步执行日志写入动作
        /// </summary>
        /// <param name="obj"></param>
        private static void PerformWriteLog(Object obj)
        {
            lock (Log_Lock)
            {
                try
                {
                    // 初始化日志读写器
                    if (LogWriter == null) InitLog();
                    // 写日志
                    LogWriter.WriteLine((String)obj);
                    // 声明自动关闭日志读写器的定时器。无限延长时间，实际上不工作
                    if (AutoCloseWriterTimer == null) AutoCloseWriterTimer = new Timer(new TimerCallback(CloseWriter), null, Timeout.Infinite, Timeout.Infinite);
                    // 改变定时器为5秒后触发一次。如果5秒内有多次写日志操作，估计定时器不会触发，直到空闲五秒为止
                    AutoCloseWriterTimer.Change(5000, Timeout.Infinite);
                }
                catch { }
            }
        }
        #endregion

        #region 写日志
        /// <summary>
        /// 输出日志
        /// </summary>
        /// <param name="msg">信息</param>
        public static void WriteLine(String msg)
        {
            WriteLogEventArgs e = new WriteLogEventArgs(msg);
            if (OnWriteLog != null)
            {
                OnWriteLog(null, e);
                return;
            }

            //String str = DateTime.Now.ToString("HH:mm:ss.fff");
            //str += " 线程：" + Thread.CurrentThread.ManagedThreadId;
            //if (Thread.CurrentThread.IsThreadPoolThread) str += "*";
            //if (!String.IsNullOrEmpty(Thread.CurrentThread.Name)) str += "(" + Thread.CurrentThread.Name + ")";
            //if (Thread.CurrentThread.IsThreadPoolThread) str += "(线程池)";

            //StringBuilder sb = new StringBuilder();
            //sb.Append(DateTime.Now.ToString("HH:mm:ss.fff"));
            //sb.Append(" ");
            //sb.Append(Thread.CurrentThread.ManagedThreadId.ToString("00"));
            //sb.Append(" ");
            //if (Thread.CurrentThread.IsThreadPoolThread)
            //    sb.Append("Y");
            //else
            //    sb.Append("N");
            //sb.Append(" ");
            //String name = Thread.CurrentThread.Name;
            //if (!String.IsNullOrEmpty(name))
            //    sb.Append(name);
            //else
            //    sb.Append("-");
            //sb.Append(" ");
            ////if (!String.IsNullOrEmpty(msg) && msg.Contains(@"\r"))
            ////{
            ////    msg = String.Format("\"{0}\"", msg);
            ////}
            //sb.Append(msg);

            // 此时还在异常线程中，可以使用HttpContext，强制初始化日志目录
            String s = LogPath;

            // 使用线程池线程写入日志
            //ThreadPool.QueueUserWorkItem(new WaitCallback(PerformWriteLog), str + "\t" + msg);
            //PerformWriteLog(str + "\t" + msg);

            PerformWriteLog(e.ToString());
        }

        /// <summary>
        /// 堆栈调试。
        /// 输出堆栈信息，用于调试时处理调用上下文。
        /// 本方法会造成大量日志，请慎用。
        /// </summary>
        public static void DebugStack()
        {
            DebugStack(int.MaxValue);
        }

        /// <summary>
        /// 堆栈调试。
        /// </summary>
        /// <param name="maxNum">最大捕获堆栈方法数</param>
        public static void DebugStack(int maxNum)
        {
            int skipFrames = 1;
            if (maxNum == int.MaxValue) skipFrames = 2;
            StackTrace st = new StackTrace(skipFrames, true);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("调用堆栈：");
            int count = Math.Min(maxNum, st.FrameCount);
            for (int i = 0; i < count; i++)
            {
                StackFrame sf = st.GetFrame(i);
                sb.AppendFormat("{0}->{1}", sf.GetMethod().DeclaringType.FullName, sf.GetMethod().ToString());
                if (i < count - 1) sb.AppendLine();
            }
            WriteLine(sb.ToString());
        }

        /// <summary>
        /// 写日志事件。绑定该事件后，XTrace将不再把日志写到日志文件中去。
        /// </summary>
        public static event EventHandler<WriteLogEventArgs> OnWriteLog;

        /// <summary>
        /// 写日志
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void WriteLine(String format, params Object[] args)
        {
            //处理时间的格式化
            if (args != null && args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] != null && args[i].GetType() == typeof(DateTime)) args[i] = ((DateTime)args[i]).ToString("yyyy-MM-dd HH:mm:ss.fff");
                }
            }
            WriteLine(String.Format(format, args));
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
                String str = ConfigurationManager.AppSettings["NewLife.Debug"];
                if (String.IsNullOrEmpty(str)) str = ConfigurationManager.AppSettings["Debug"];
                if (String.IsNullOrEmpty(str)) return false;
                if (str == "1") return true;
                if (str == "0") return false;
                if (str.Equals(Boolean.FalseString, StringComparison.OrdinalIgnoreCase)) return false;
                if (str.Equals(Boolean.TrueString, StringComparison.OrdinalIgnoreCase)) return true;
                return false;
            }
            set { _Debug = value; }
        }
        #endregion
    }
}