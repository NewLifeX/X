using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using NewLife.Threading;

namespace NewLife.Log
{
    /// <summary>文本文件日志类。提供向文本文件写日志的能力</summary>
    /// <remarks>
    /// 两大用法：
    /// 1，Create(path, fileFormat) 指定日志目录和文件名格式
    /// 2，CreateFile(path) 指定文件，一直往里面写
    /// 
    /// 2015-06-01 为了继承TextFileLog，增加了无参构造函数，修改了异步写日志方法为虚方法，可以进行重载
    /// </remarks>
    public class TextFileLog : Logger, IDisposable
    {
        #region 属性
        /// <summary>日志目录</summary>
        public String LogPath { get; set; }

        /// <summary>日志文件格式。默认{0:yyyy_MM_dd}.log</summary>
        public String FileFormat { get; set; }

        /// <summary>日志文件上限。超过上限后拆分新日志文件，默认10MB，0表示不限制大小</summary>
        public Int32 MaxBytes { get; set; } = 10;

        /// <summary>日志文件备份。超过备份数后，最旧的文件将被删除，默认100，0表示不限制个数</summary>
        public Int32 Backups { get; set; } = 100;

        private readonly Boolean _isFile = false;

        /// <summary>是否当前进程的第一次写日志</summary>
        private Boolean _isFirst = false;
        #endregion

        #region 构造
        /// <summary>该构造函数没有作用，为了继承而设置</summary>
        public TextFileLog() { }

        private TextFileLog(String path, Boolean isfile, String fileFormat = null)
        {
            LogPath = path;
            _isFile = isfile;

            var set = Setting.Current;
            if (!fileFormat.IsNullOrEmpty())
                FileFormat = fileFormat;
            else
                FileFormat = set.LogFileFormat;

            MaxBytes = set.LogFileMaxBytes;
            Backups = set.LogFileBackups;

            _Timer = new TimerX(DoWriteAndClose, null, 0_000, 5_000) { Async = true };
        }

        private static readonly ConcurrentDictionary<String, TextFileLog> cache = new ConcurrentDictionary<String, TextFileLog>(StringComparer.OrdinalIgnoreCase);
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
            if (path.IsNullOrEmpty()) throw new ArgumentNullException(nameof(path));

            return cache.GetOrAdd(path, k => new TextFileLog(k, true));
        }

        /// <summary>销毁</summary>
        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(Boolean disposing)
        {
            _Timer.TryDispose();

            // 销毁前把队列日志输出
            if (Interlocked.CompareExchange(ref _writing, 1, 0) == 0) WriteAndClose(DateTime.MinValue);
        }
        #endregion

        #region 内部方法
        private StreamWriter LogWriter;
        private String CurrentLogFile;

        /// <summary>初始化日志记录文件</summary>
        private StreamWriter InitLog()
        {
            var logfile = GetLogFile();
            logfile.EnsureDirectory(true);

            var stream = new FileStream(logfile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            var writer = new StreamWriter(stream, Encoding.UTF8);

            // 写日志头
            if (!_isFirst)
            {
                _isFirst = true;

                // 因为指定了编码，比如UTF8，开头就会写入3个字节，所以这里不能拿长度跟0比较
                if (writer.BaseStream.Length > 10) writer.WriteLine();

                writer.Write(GetHead());
            }

            CurrentLogFile = logfile;
            return LogWriter = writer;
        }

        /// <summary>获取日志文件路径</summary>
        /// <returns></returns>
        private String GetLogFile()
        {
            // 单日志文件
            if (_isFile) return LogPath.GetBasePath();

            // 目录多日志文件
            var logfile = LogPath.CombinePath(FileFormat.F(TimerX.Now)).GetBasePath();

            // 是否限制文件大小
            if (MaxBytes == 0) return logfile;

            // 找到今天第一个未达到最大上限的文件
            var max = MaxBytes * 1024L * 1024L;
            var ext = Path.GetExtension(logfile);
            var name = logfile.TrimEnd(ext);
            for (var i = 1; i < 1024; i++)
            {
                if (i > 1) logfile = $"{name}_{i}{ext}";

                var fi = logfile.AsFile();
                if (!fi.Exists || fi.Length < max) return logfile;
            }

            return null;
        }
        #endregion

        #region 异步写日志
        private readonly TimerX _Timer;
        private readonly ConcurrentQueue<String> _Logs = new ConcurrentQueue<String>();
        private volatile Int32 _logCount;
        private Int32 _writing;
        private DateTime _NextClose;

        /// <summary>写文件</summary>
        protected virtual void WriteFile()
        {
            var writer = LogWriter;

            var now = TimerX.Now;
            if (!_isFile && GetLogFile() != CurrentLogFile)
            {
                writer.TryDispose();
                writer = null;
            }

            // 初始化日志读写器
            if (writer == null) writer = InitLog();

            // 依次把队列日志写入文件
            while (_Logs.TryDequeue(out var str))
            {
                Interlocked.Decrement(ref _logCount);

                // 写日志。TextWriter.WriteLine内需要拷贝，浪费资源
                //writer.WriteLine(str);
                writer.Write(str);
                writer.WriteLine();
            }

            // 写完一批后，刷一次磁盘
            writer?.Flush();

            // 连续5秒没日志，就关闭
            _NextClose = now.AddSeconds(5);
        }

        /// <summary>关闭文件</summary>
        private void DoWriteAndClose(Object state)
        {
            // 同步写日志
            if (Interlocked.CompareExchange(ref _writing, 1, 0) == 0) WriteAndClose(_NextClose);

            // 检查文件是否超过上限
            if (!_isFile && Backups > 0)
            {
                // 判断日志目录是否已存在
                var di = LogPath.GetBasePath().AsDirectory();
                if (di.Exists)
                {
                    // 删除*.del
                    try
                    {
                        var dels = di.GetFiles("*.del");
                        if (dels != null && dels.Length > 0)
                        {
                            foreach (var item in dels)
                            {
                                item.Delete();
                            }
                        }
                    }
                    catch { }

                    var ext = Path.GetExtension(FileFormat);
                    var fis = di.GetFiles("*" + ext);
                    if (fis != null && fis.Length > Backups)
                    {
                        // 删除最旧的文件
                        var retain = fis.Length - Backups;
                        fis = fis.OrderBy(e => e.CreationTime).Take(retain).ToArray();
                        foreach (var item in fis)
                        {
                            OnWrite(LogLevel.Info, "日志文件达到上限 {0}，删除 {1}，大小 {2:n0}Byte", Backups, item.Name, item.Length);
                            try
                            {
                                item.Delete();
                            }
                            catch
                            {
                                item.MoveTo(item.FullName + ".del");
                            }
                        }
                    }
                }
            }
        }

        /// <summary>写入队列日志并关闭文件</summary>
        protected virtual void WriteAndClose(DateTime closeTime)
        {
            try
            {
                // 处理残余
                var writer = LogWriter;
                if (!_Logs.IsEmpty) WriteFile();

                // 连续5秒没日志，就关闭
                if (writer != null && closeTime < TimerX.Now)
                {
                    writer.TryDispose();
                    LogWriter = null;
                }
            }
            finally
            {
                _writing = 0;
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
            // 据@夏玉龙反馈，如果不给Log目录写入权限，日志队列积压将会导致内存暴增
            if (_logCount > 100) return;

            var e = WriteLogEventArgs.Current.Set(level);
            // 特殊处理异常对象
            if (args != null && args.Length == 1 && args[0] is Exception ex && (format.IsNullOrEmpty() || format == "{0}"))
                e = e.Set(null, ex);
            else
                e = e.Set(Format(format, args), null);

            // 推入队列
            _Logs.Enqueue(e.ToString());
            Interlocked.Increment(ref _logCount);

            // 异步写日志，实时。即使这里错误，定时器那边仍然会补上
            if (Interlocked.CompareExchange(ref _writing, 1, 0) == 0)
            {
                // 调试级别 或 致命错误 同步写日志
                if (Setting.Current.LogLevel <= LogLevel.Debug || Level >= LogLevel.Error)
                {
                    try
                    {
                        WriteFile();
                    }
                    finally
                    {
                        _writing = 0;
                    }
                }
                else
                {
                    ThreadPoolX.QueueUserWorkItem(() =>
                    {
                        try
                        {
                            WriteFile();
                        }
                        finally
                        {
                            _writing = 0;
                        }
                    });
                }
            }
        }
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString() => $"{GetType().Name} {LogPath}";
        #endregion
    }
}