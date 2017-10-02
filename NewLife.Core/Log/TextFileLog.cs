using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using NewLife.Collections;
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

            _Timer = new TimerX(WriteFile, null, 1000, 1000) { Async = true };
        }

        static DictionaryCache<String, TextFileLog> cache = new DictionaryCache<String, TextFileLog>(StringComparer.OrdinalIgnoreCase);
        /// <summary>每个目录的日志实例应该只有一个，所以采用静态创建</summary>
        /// <param name="path">日志目录或日志文件路径</param>
        /// <param name="fileFormat"></param>
        /// <returns></returns>
        public static TextFileLog Create(String path, String fileFormat = null)
        {
            if (path.IsNullOrEmpty()) path = XTrace.LogPath;
            if (path.IsNullOrEmpty()) path = Runtime.IsWeb ? "../Log" : "Log";

            var key = (path + fileFormat).ToLower();
            return cache.GetItem(key, k => new TextFileLog(path, false, fileFormat));
        }

        /// <summary>每个目录的日志实例应该只有一个，所以采用静态创建</summary>
        /// <param name="path">日志目录或日志文件路径</param>
        /// <returns></returns>
        public static TextFileLog CreateFile(String path)
        {
            if (String.IsNullOrEmpty(path)) return Create(path);

            var key = path.ToLower();
            return cache.GetItem(key, k => new TextFileLog(path, true));
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
                    writer = new StreamWriter(stream, Encoding.UTF8);
                    writer.AutoFlush = true;
                }
                catch { }
            }

            if (writer == null)
            {
                logfile = Path.Combine(path, FileFormat.F(DateTime.Now));
                var ext = Path.GetExtension(logfile);
                var i = 0;
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
                        if (logfile.EndsWith("_" + i + ext))
                            logfile = logfile.Replace("_" + i + ext, "_" + (++i) + ext);
                        else
                            logfile = logfile.Replace(ext, "_0" + ext);
                    }
                }
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

        ///// <summary>停止日志</summary>
        //protected virtual void CloseWriter(Object obj)
        //{
        //    var writer = LogWriter;
        //    if (writer == null) return;
        //    lock (Log_Lock)
        //    {
        //        try
        //        {
        //            if (writer == null) return;
        //            writer.Dispose();
        //            LogWriter = null;
        //        }
        //        catch { }
        //    }
        //}
        #endregion

        #region 异步写日志
        private TimerX _Timer;
        private ConcurrentQueue<String> _Logs = new ConcurrentQueue<String>();
        private DateTime _NextClose;
        //private Object Log_Lock = new Object();

        /// <summary>写文件</summary>
        /// <param name="state"></param>
        protected virtual void WriteFile(Object state)
        {
            var writer = LogWriter;
            if (_Logs.Count == 0)
            {
                // 连续5秒没日志，就关闭
                var now = TimerX.Now;
                if (_NextClose < now)
                {
                    LogWriter = null;
                    writer.TryDispose();

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

        ///// <summary>使用线程池线程异步执行日志写入动作</summary>
        ///// <param name="e"></param>
        //protected virtual void PerformWriteLog(WriteLogEventArgs e)
        //{
        //    lock (Log_Lock)
        //    {
        //        try
        //        {
        //            // 初始化日志读写器
        //            if (LogWriter == null) InitLog();
        //            // 写日志
        //            LogWriter.WriteLine(e.ToString());
        //            // 声明自动关闭日志读写器的定时器。无限延长时间，实际上不工作
        //            if (_Timer == null) _Timer = new Timer(CloseWriter, null, Timeout.Infinite, Timeout.Infinite);
        //            // 改变定时器为5秒后触发一次。如果5秒内有多次写日志操作，估计定时器不会触发，直到空闲五秒为止
        //            _Timer.Change(5000, Timeout.Infinite);
        //        }
        //        catch { }
        //    }
        //}
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
            if (args != null && args.Length == 1 && args[0] is Exception && (String.IsNullOrEmpty(format) || format == "{0}"))
                e = e.Set(null, args[0] as Exception);
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