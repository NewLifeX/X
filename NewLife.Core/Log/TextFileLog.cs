using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using NewLife.Collections;
using NewLife.Exceptions;
using NewLife.Reflection;

namespace NewLife.Log
{
    /// <summary>文本文件日志类。提供向文本文件写日志的能力</summary>
    public class TextFileLog
    {
        #region 构造
        private TextFileLog(String path) { FilePath = path; }

        static DictionaryCache<String, TextFileLog> cache = new DictionaryCache<string, TextFileLog>();
        /// <summary>每个目录的日志实例应该只有一个，所以采用静态创建</summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static TextFileLog Create(String path)
        {
            if (String.IsNullOrEmpty(path)) path = "Log";

            String key = path.ToLower();
            return cache.GetItem<String>(key, path, (k, p) => new TextFileLog(p));
        }
        #endregion

        #region 属性
        private String _FilePath;
        /// <summary>文件路径</summary>
        public String FilePath
        {
            get { return _FilePath; }
            private set { _FilePath = value; }
        }

        private String _LogPath;
        /// <summary>日志目录</summary>
        public String LogPath
        {
            get
            {
                if (!String.IsNullOrEmpty(_LogPath)) return _LogPath;

                String dir = FilePath;
                if (!Path.IsPathRooted(dir)) dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dir);

                //保证\结尾
                if (!String.IsNullOrEmpty(dir) && dir.Substring(dir.Length - 1, 1) != @"\") dir += @"\";

                _LogPath = new DirectoryInfo(dir).FullName;
                return _LogPath;
            }
        }

        /// <summary>是否当前进程的第一次写日志</summary>
        private Boolean isFirst = false;
        #endregion

        #region 内部方法
        private StreamWriter LogWriter;

        /// <summary>初始化日志记录文件</summary>
        private void InitLog()
        {
            String path = LogPath;
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            //if (path.Substring(path.Length - 2) != @"\") path += @"\";
            String logfile = Path.Combine(path, DateTime.Now.ToString("yyyy_MM_dd") + ".log");
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
            if (i >= 10) throw new XException("无法写入日志！");

            if (!isFirst)
            {
                isFirst = true;

                WriteHead();
            }
        }

        private void WriteHead()
        {
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
            //if (File.Exists(logfile) && LogWriter.BaseStream.Length > 0) LogWriter.WriteLine();
            // 因为指定了编码，比如UTF8，开头就会写入3个字节，所以这里不能拿长度跟0比较
            if (LogWriter.BaseStream.Length > 10) LogWriter.WriteLine();
            LogWriter.WriteLine("#Software: {0}", name);
            LogWriter.WriteLine("#ProcessID: {0}", process.Id);
            LogWriter.WriteLine("#AppDomain: {0}", AppDomain.CurrentDomain.FriendlyName);
            LogWriter.WriteLine("#BaseDirectory: {0}", AppDomain.CurrentDomain.BaseDirectory);
            LogWriter.WriteLine("#Date: {0:yyyy-MM-dd}", DateTime.Now);
            LogWriter.WriteLine("#Fields: Time ThreadID IsPoolThread ThreadName Message");
        }

        /// <summary>停止日志</summary>
        private void CloseWriter(Object obj)
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
        private Timer AutoCloseWriterTimer;
        private object Log_Lock = new object();

        /// <summary>使用线程池线程异步执行日志写入动作</summary>
        /// <param name="obj"></param>
        private void PerformWriteLog(Object obj)
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
        /// <summary>输出日志</summary>
        /// <param name="msg">信息</param>
        public void WriteLine(String msg)
        {
            // 小对象，采用对象池的成本太高了
            var e = new WriteLogEventArgs(msg);

            //if (OnWriteLog != null)
            //{
            //    OnWriteLog(null, e);
            //    return;
            //}

            PerformWriteLog(e.ToString());
        }

        /// <summary>输出异常日志</summary>
        /// <param name="ex">异常信息</param>
        public void WriteException(Exception ex)
        {
            var e = new WriteLogEventArgs(null, ex);

            //if (OnWriteLog != null)
            //{
            //    OnWriteLog(null, e);
            //    return;
            //}

            PerformWriteLog(e.ToString());
        }

        /// <summary>
        /// 堆栈调试。
        /// 输出堆栈信息，用于调试时处理调用上下文。
        /// 本方法会造成大量日志，请慎用。
        /// </summary>
        public void DebugStack()
        {
            DebugStack(1, int.MaxValue);
        }

        /// <summary>堆栈调试。</summary>
        /// <param name="maxNum">最大捕获堆栈方法数</param>
        public void DebugStack(int maxNum)
        {
            DebugStack(1, maxNum);
        }

        /// <summary>堆栈调试</summary>
        /// <param name="start">开始方法数，0是DebugStack的直接调用者</param>
        /// <param name="maxNum">最大捕获堆栈方法数</param>
        public void DebugStack(int start, int maxNum)
        {
            int skipFrames = 1;
            if (maxNum == int.MaxValue) skipFrames = 2;
            var st = new StackTrace(skipFrames, true);
            var sb = new StringBuilder();
            sb.AppendLine("调用堆栈：");
            int count = Math.Min(maxNum, st.FrameCount);
            for (int i = 0; i < count; i++)
            {
                var sf = st.GetFrame(i);
                var method = sf.GetMethod();

                var name = method.ToString();
                // 去掉前面的返回类型
                if (name.Contains(" ")) name = name.Substring(name.IndexOf(" ") + 1);

                var type = method.DeclaringType ?? method.ReflectedType;
                if (type != null)
                    sb.AppendFormat("{0}.{1}", TypeX.Create(type).Name, name);
                else
                    sb.AppendFormat("UnkownType.{0}", name);
                if (i < count - 1) sb.AppendLine();
            }
            WriteLine(sb.ToString());
        }

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLine(String format, params Object[] args)
        {
            //处理时间的格式化
            if (args != null && args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] != null && args[i].GetType() == typeof(DateTime))
                    {
                        // 根据时间值的精确度选择不同的格式化输出
                        DateTime dt = (DateTime)args[i];
                        if (dt.Millisecond > 0)
                            args[i] = dt.ToString("yyyy-MM-dd HH:mm:ss.fff");
                        else if (dt.Hour > 0 || dt.Minute > 0 || dt.Second > 0)
                            args[i] = dt.ToString("yyyy-MM-dd HH:mm:ss");
                        else
                            args[i] = dt.ToString("yyyy-MM-dd");
                    }
                }
            }
            WriteLine(String.Format(format, args));
        }
        #endregion
    }
}