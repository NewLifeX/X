using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using NewLife.Collections;
using NewLife.Configuration;

namespace NewLife.Log
{
    /// <summary>文本文件日志类。提供向文本文件写日志的能力</summary>
    public class TextFileLog : Logger, IDisposable
    {
        #region 构造
        private TextFileLog(String path, Boolean isfile)
        {
            if (!isfile)
                LogPath = path;
            else
                LogFile = path;
        }

        static DictionaryCache<String, TextFileLog> cache = new DictionaryCache<String, TextFileLog>(StringComparer.OrdinalIgnoreCase);
        /// <summary>每个目录的日志实例应该只有一个，所以采用静态创建</summary>
        /// <param name="path">日志目录或日志文件路径</param>
        /// <returns></returns>
        public static TextFileLog Create(String path)
        {
            if (String.IsNullOrEmpty(path)) path = Config.GetConfig<String>("NewLife.LogPath", "Log");

            String key = path.ToLower();
            return cache.GetItem<String>(key, path, (k, p) => new TextFileLog(p, false));
        }

        /// <summary>每个目录的日志实例应该只有一个，所以采用静态创建</summary>
        /// <param name="path">日志目录或日志文件路径</param>
        /// <returns></returns>
        public static TextFileLog CreateFile(String path)
        {
            if (String.IsNullOrEmpty(path)) return Create(path);

            String key = path.ToLower();
            return cache.GetItem<String>(key, path, (k, p) => new TextFileLog(p, true));
        }

        /// <summary>销毁</summary>
        public void Dispose()
        {
            if (LogWriter != null)
            {
                LogWriter.Dispose();
                LogWriter = null;
            }
        }
        #endregion

        #region 属性
        private String _LogFile;
        /// <summary>日志文件</summary>
        public String LogFile { get { return _LogFile; } set { _LogFile = value; } }

        private String _LogPath;
        /// <summary>日志目录</summary>
        public String LogPath
        {
            get
            {
                if (String.IsNullOrEmpty(_LogPath) && !String.IsNullOrEmpty(LogFile))
                    _LogPath = Path.GetDirectoryName(LogFile).GetFullPath().EnsureEnd(@"\");
                return _LogPath;
            }
            set
            {
                if (String.IsNullOrEmpty(value))
                    _LogPath = value;
                else
                    _LogPath = value.GetFullPath().EnsureEnd(@"\");
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
            String path = LogPath.EnsureDirectory(false);

            StreamWriter writer = null;
            var logfile = LogFile;
            if (!String.IsNullOrEmpty(logfile))
            {
                try
                {
                    var stream = new FileStream(logfile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                    writer = new StreamWriter(stream, Encoding.UTF8);
                    writer.AutoFlush = true;
                }
                catch { }
            }

            if (writer == null)
            {
                logfile = Path.Combine(path, DateTime.Now.ToString("yyyy_MM_dd") + ".log");
                int i = 0;
                while (i < 10)
                {
                    try
                    {
                        var stream = new FileStream(logfile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                        writer = new StreamWriter(stream, Encoding.UTF8);
                        writer.AutoFlush = true;
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
            }
            if (writer == null) throw new XException("无法写入日志！");

            // 这里赋值，会导致log文件名不会随时间而自动改变
            //LogFile = logfile;

            if (!isFirst)
            {
                isFirst = true;

                WriteHead(writer);
            }
            LogWriter = writer;
        }

        private void WriteHead(StreamWriter writer)
        {
            var process = Process.GetCurrentProcess();
            var name = String.Empty;
            var asm = Assembly.GetEntryAssembly();
            if (asm != null)
            {
                if (String.IsNullOrEmpty(name))
                {
                    var att = asm.GetCustomAttribute<AssemblyTitleAttribute>();
                    if (att != null) name = att.Title;
                }

                if (String.IsNullOrEmpty(name))
                {
                    var att = asm.GetCustomAttribute<AssemblyProductAttribute>();
                    if (att != null) name = att.Product;
                }

                if (String.IsNullOrEmpty(name))
                {
                    var att = asm.GetCustomAttribute<AssemblyDescriptionAttribute>();
                    if (att != null) name = att.Description;
                }
            }
            if (String.IsNullOrEmpty(name))
            {
                try
                {
                    name = process.ProcessName;
                }
                catch { }
            }
            // 通过判断LogWriter.BaseStream.Length，解决有时候日志文件为空但仍然加空行的问题
            //if (File.Exists(logfile) && LogWriter.BaseStream.Length > 0) LogWriter.WriteLine();
            // 因为指定了编码，比如UTF8，开头就会写入3个字节，所以这里不能拿长度跟0比较
            if (writer.BaseStream.Length > 10) writer.WriteLine();
            writer.WriteLine("#Software: {0}", name);
            writer.WriteLine("#ProcessID: {0}{1}", process.Id, Runtime.Is64BitProcess ? " x64" : "");
            writer.WriteLine("#AppDomain: {0}", AppDomain.CurrentDomain.FriendlyName);

            var fileName = String.Empty;
            try
            {
                fileName = process.StartInfo.FileName;
                if (fileName.IsNullOrWhiteSpace()) fileName = process.MainModule.FileName;

                if (!String.IsNullOrEmpty(fileName)) writer.WriteLine("#FileName: {0}", fileName);
            }
            catch { }

            // 应用域目录
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            writer.WriteLine("#BaseDirectory: {0}", baseDir);

            // 当前目录。如果由别的进程启动，默认的当前目录就是父级进程的当前目录
            var curDir = Environment.CurrentDirectory;
            //if (!curDir.EqualIC(baseDir) && !(curDir + "\\").EqualIC(baseDir))
            if (!baseDir.EqualIgnoreCase(curDir, curDir + "\\"))
                writer.WriteLine("#CurrentDirectory: {0}", curDir);

            // 命令行不为空，也不是文件名时，才输出
            // 当使用cmd启动程序时，这里就是用户输入的整个命令行，所以可能包含空格和各种符号
            var line = Environment.CommandLine;
            if (!String.IsNullOrEmpty(line))
            {
                line = line.Trim().TrimStart('\"');
                if (!String.IsNullOrEmpty(fileName) && line.StartsWithIgnoreCase(fileName))
                    line = line.Substring(fileName.Length).TrimStart().TrimStart('\"').TrimStart();
                if (!String.IsNullOrEmpty(line))
                {
                    writer.WriteLine("#CommandLine: {0}", line);
                }
            }

            writer.WriteLine("#ApplicationType: {0}", Runtime.IsConsole ? "Console" : (Runtime.IsWeb ? "Web" : "WinForm"));
            writer.WriteLine("#CLR: {0}", Environment.Version);

            writer.WriteLine("#OS: {0}, {1}/{2}", Runtime.OSName, Environment.UserName, Environment.MachineName);

            writer.WriteLine("#Date: {0:yyyy-MM-dd}", DateTime.Now);
            writer.WriteLine("#Fields: Time ThreadID IsPoolThread ThreadName Message");
        }

        /// <summary>停止日志</summary>
        private void CloseWriter(Object obj)
        {
            var writer = LogWriter;
            if (writer == null) return;
            lock (Log_Lock)
            {
                try
                {
                    if (writer == null) return;
                    writer.Close();
                    writer.Dispose();
                    LogWriter = null;
                }
                catch { }
            }
        }
        #endregion

        #region 异步写日志
        private Timer AutoCloseWriterTimer;
        private object Log_Lock = new object();
        private Boolean LastIsNewLine = true;

        /// <summary>使用线程池线程异步执行日志写入动作</summary>
        /// <param name="e"></param>
        private void PerformWriteLog(WriteLogEventArgs e)
        {
            lock (Log_Lock)
            {
                try
                {
                    // 初始化日志读写器
                    if (LogWriter == null) InitLog();
                    // 写日志
                    if (LastIsNewLine)
                    {
                        // 如果上一次是换行，则这次需要输出行头信息
                        if (e.IsNewLine)
                            LogWriter.WriteLine(e.ToString());
                        else
                        {
                            LogWriter.Write(e.ToString());
                            LastIsNewLine = false;
                        }
                    }
                    else
                    {
                        // 如果上一次不是换行，则这次不需要行头信息
                        var msg = e.Message + e.Exception;
                        if (e.IsNewLine)
                        {
                            LogWriter.WriteLine(msg);
                            LastIsNewLine = true;
                        }
                        else
                            LogWriter.Write(msg);
                    }
                    // 声明自动关闭日志读写器的定时器。无限延长时间，实际上不工作
                    if (AutoCloseWriterTimer == null) AutoCloseWriterTimer = new Timer(new TimerCallback(CloseWriter), null, Timeout.Infinite, Timeout.Infinite);
                    // 改变定时器为5秒后触发一次。如果5秒内有多次写日志操作，估计定时器不会触发，直到空闲五秒为止
                    AutoCloseWriterTimer.Change(5000, Timeout.Infinite);

                    // 清空日志对象
                    e.Clear();
                }
                catch { }
            }
        }
        #endregion

        #region 写日志
        /// <summary>写日志</summary>
        /// <param name="level"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected override void OnWrite(LogLevel level, String format, params Object[] args)
        {
            // 特殊处理异常对象
            if (args != null && args.Length == 1 && args[0] is Exception && (String.IsNullOrEmpty(format) || format == "{0}"))
                PerformWriteLog(WriteLogEventArgs.Current.Set(level, null, args[0] as Exception, true));
            else
                PerformWriteLog(WriteLogEventArgs.Current.Set(level, Format(format, args), null, true));
        }

        /// <summary>输出日志</summary>
        /// <param name="msg">信息</param>
        public void Write(String msg)
        {
            PerformWriteLog(WriteLogEventArgs.Current.Set(msg, null, false));
        }

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void Write(String format, params Object[] args)
        {
            Write(Format(format, args));
        }

        /// <summary>输出日志</summary>
        /// <param name="msg">信息</param>
        public void WriteLine(String msg)
        {
            // 小对象，采用对象池的成本太高了
            PerformWriteLog(WriteLogEventArgs.Current.Set(msg, null, true));
        }

        /// <summary>写日志</summary>
        /// <param name="level"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLine(LogLevel level, String format, params Object[] args)
        {
            WriteLine(Format(format, args));
        }

        /// <summary>输出异常日志</summary>
        /// <param name="ex">异常信息</param>
        public void WriteException(Exception ex)
        {
            PerformWriteLog(WriteLogEventArgs.Current.Set(null, ex, false));
        }
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (!String.IsNullOrEmpty(LogFile))
                return String.Format("{0} {1}", this.GetType().Name, LogFile);
            else
                return String.Format("{0} {1}", this.GetType().Name, LogPath);
        }
        #endregion
    }
}