using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using NewLife.Threading;

namespace NewLife.Log
{
    /// <summary>文本文件日志类。提供向文本文件写日志的能力</summary>
    /// <remarks>
    /// 2015-06-01 为了继承TextFileLog，增加了无参构造函数，修改了异步写日志方法为虚方法，可以进行重载
    /// </remarks>
    public class TextFileLog : Logger, IDisposable
    {
        #region 构造
        /// <summary>该构造函数没有作用，为了继承而设置</summary>
        public TextFileLog() { }

        private TextFileLog(String path, Boolean isfile, String fileFormat = null)
        {
            if (!isfile)
                LogPath = path;
            else
                LogFile = path;

            if (!fileFormat.IsNullOrEmpty())
                FileFormat = fileFormat;
            else
                FileFormat = Setting.Current.LogFileFormat;

            _Timer = new TimerX(WriteFile, null, 1000, 1000)
            {
                Async = true,
                CanExecute = () => !_Logs.IsEmpty
            };
        }

        static ConcurrentDictionary<String, TextFileLog> cache = new ConcurrentDictionary<String, TextFileLog>(StringComparer.OrdinalIgnoreCase);
        /// <summary>每个目录的日志实例应该只有一个，所以采用静态创建</summary>
        /// <param name="path">日志目录或日志文件路径</param>
        /// <param name="fileFormat"></param>
        /// <returns></returns>
        public static TextFileLog Create(String path, String fileFormat = null)
        {
            if (path.IsNullOrEmpty()) path = XTrace.LogPath;
            if (path.IsNullOrEmpty()) path = Runtime.IsWeb ? "../Log" : "Log";

            var key = (path + fileFormat).ToLower();
            return cache.GetOrAdd(key, k => new TextFileLog(path, false, fileFormat));
        }

        /// <summary>每个目录的日志实例应该只有一个，所以采用静态创建</summary>
        /// <param name="path">日志目录或日志文件路径</param>
        /// <returns></returns>
        public static TextFileLog CreateFile(String path)
        {
            if (path.IsNullOrEmpty()) return Create(path);

            return cache.GetOrAdd(path, k => new TextFileLog(k, true));
        }

        /// <summary>销毁</summary>
        public void Dispose()
        {
            // 销毁前把队列日志输出
            if (_Logs != null && !_Logs.IsEmpty) WriteFile(null);

            var writer = LogWriter;
            if (writer != null)
            {
                writer.TryDispose();
                LogWriter = null;
            }
        }
        #endregion

        #region 属性
        /// <summary>日志文件</summary>
        public String LogFile { get; set; }

        private String _LogPath;
        /// <summary>日志目录</summary>
        public String LogPath
        {
            get
            {
                if (String.IsNullOrEmpty(_LogPath) && !String.IsNullOrEmpty(LogFile))
                    _LogPath = Path.GetDirectoryName(LogFile).GetFullPath().EnsureEnd(Path.DirectorySeparatorChar.ToString());
                return _LogPath;
            }
            set
            {
                if (String.IsNullOrEmpty(value))
                    _LogPath = value;
                else
                    _LogPath = value.GetFullPath().EnsureEnd(Path.DirectorySeparatorChar.ToString());
            }
        }

        /// <summary>日志文件格式</summary>
        public String FileFormat { get; set; }

        /// <summary>是否当前进程的第一次写日志</summary>
        private Boolean isFirst = false;
        #endregion

        #region 内部方法
        private StreamWriter LogWriter;
        private String CurrentLogFile;

        /// <summary>初始化日志记录文件</summary>
        private StreamWriter InitLog()
        {
            var path = LogPath.EnsureDirectory(false);

            StreamWriter writer = null;
            var logfile = LogFile;
            if (!String.IsNullOrEmpty(logfile))
            {
                try
                {
                    var stream = new FileStream(logfile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                    writer = new StreamWriter(stream, Encoding.UTF8)
                    {
                        AutoFlush = true
                    };
                }
                catch { }
            }

            if (writer == null)
            {
                logfile = Path.Combine(path, FileFormat.F(TimerX.Now));
                var ext = Path.GetExtension(logfile);
                var i = 0;
                while (i < 10)
                {
                    try
                    {
                        var stream = new FileStream(logfile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                        writer = new StreamWriter(stream, Encoding.UTF8)
                        {
                            AutoFlush = true
                        };
                        break;
                    }
                    catch
                    {
                        if (logfile.EndsWith("_" + i + ext))
                            logfile = logfile.Replace("_" + i + ext, "_" + (++i) + ext);
                        else
                            logfile = logfile.Replace(ext, "_0" + ext);
                    }
                }
                CurrentLogFile = logfile;
            }
            if (writer == null) throw new XException("无法写入日志！");

            // 这里赋值，会导致log文件名不会随时间而自动改变
            //LogFile = logfile;

            if (!isFirst)
            {
                isFirst = true;

                // 通过判断LogWriter.BaseStream.Length，解决有时候日志文件为空但仍然加空行的问题
                //if (File.Exists(logfile) && LogWriter.BaseStream.Length > 0) LogWriter.WriteLine();
                // 因为指定了编码，比如UTF8，开头就会写入3个字节，所以这里不能拿长度跟0比较
                if (writer.BaseStream.Length > 10) writer.WriteLine();

                //WriteHead(writer);
                writer.Write(GetHead());
            }
            return LogWriter = writer;
        }
        #endregion

        #region 异步写日志
        private readonly TimerX _Timer;
        private ConcurrentQueue<String> _Logs = new ConcurrentQueue<String>();
        private DateTime _NextClose;

        /// <summary>写文件</summary>
        /// <param name="state"></param>
        protected virtual void WriteFile(Object state)
        {
            var writer = LogWriter;

            var now = TimerX.Now;
            var logfile = Path.Combine(LogPath, FileFormat.F(now));
            if (logfile != CurrentLogFile)
            {
                writer.TryDispose();
                writer = null;
            }

            if (_Logs.IsEmpty)
            {
                // 连续5秒没日志，就关闭
                if (_NextClose < now)
                {
                    writer.TryDispose();
                    writer = null;

                    _NextClose = now.AddSeconds(5);
                }

                return;
            }

            // 初始化日志读写器
            if (writer == null) writer = InitLog();

            while (_Logs.TryDequeue(out var str))
            {
                // 写日志
                writer.WriteLine(str);
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
            var e = WriteLogEventArgs.Current.Set(level);
            // 特殊处理异常对象
            if (args != null && args.Length == 1 && args[0] is Exception ex && (format.IsNullOrEmpty() || format == "{0}"))
                e = e.Set(null, ex);
            else
                e = e.Set(Format(format, args), null);

            // 推入队列，异步写日志
            _Logs.Enqueue(e.ToString());
        }
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString()
        {
            if (!String.IsNullOrEmpty(LogFile))
                return String.Format("{0} {1}", GetType().Name, LogFile);
            else
                return String.Format("{0} {1}", GetType().Name, LogPath);
        }
        #endregion
    }
}