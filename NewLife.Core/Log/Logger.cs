using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using NewLife.Configuration;

namespace NewLife.Log
{
    /// <summary>日志基类。提供日志的基本实现</summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public abstract class Logger : ILog
    {
        #region 主方法
        /// <summary>调试日志</summary>
        /// <param name="format">格式化字符串</param>
        /// <param name="args">格式化参数</param>
        public virtual void Debug(String format, params Object[] args) { Write(LogLevel.Debug, format, args); }

        /// <summary>信息日志</summary>
        /// <param name="format">格式化字符串</param>
        /// <param name="args">格式化参数</param>
        public virtual void Info(String format, params Object[] args) { Write(LogLevel.Info, format, args); }

        /// <summary>警告日志</summary>
        /// <param name="format">格式化字符串</param>
        /// <param name="args">格式化参数</param>
        public virtual void Warn(String format, params Object[] args) { Write(LogLevel.Warn, format, args); }

        /// <summary>错误日志</summary>
        /// <param name="format">格式化字符串</param>
        /// <param name="args">格式化参数</param>
        public virtual void Error(String format, params Object[] args) { Write(LogLevel.Error, format, args); }

        /// <summary>严重错误日志</summary>
        /// <param name="format">格式化字符串</param>
        /// <param name="args">格式化参数</param>
        public virtual void Fatal(String format, params Object[] args) { Write(LogLevel.Fatal, format, args); }
        #endregion

        #region 核心方法
        /// <summary>写日志</summary>
        /// <param name="level"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public virtual void Write(LogLevel level, String format, params Object[] args)
        {
            if (level >= Level) OnWrite(level, format, args);
        }

        /// <summary>写日志</summary>
        /// <param name="level"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected abstract void OnWrite(LogLevel level, String format, params Object[] args);

        ///// <summary>输出异常日志</summary>
        ///// <param name="ex">异常信息</param>
        //public abstract void WriteException(Exception ex);

        ///// <summary>写日志</summary>
        ///// <param name="format">格式化字符串</param>
        ///// <param name="args">格式化参数</param>
        //public abstract void WriteLine(LogLevel level, String format, params Object[] args);
        #endregion

        #region 辅助方法
        /// <summary>格式化参数，特殊处理异常和时间</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual String Format(String format, Object[] args)
        {
            //处理时间的格式化
            if (args != null && args.Length > 0)
            {
                // 特殊处理异常
                if (args.Length == 1 && args[0] is Exception && (String.IsNullOrEmpty(format) || format == "{0}"))
                {
                    return "" + args[0];
                }

                for (var i = 0; i < args.Length; i++)
                {
                    if (args[i] != null && args[i].GetType() == typeof(DateTime))
                    {
                        // 根据时间值的精确度选择不同的格式化输出
                        var dt = (DateTime)args[i];
                        if (dt.Millisecond > 0)
                            args[i] = dt.ToString("yyyy-MM-dd HH:mm:ss.fff");
                        else if (dt.Hour > 0 || dt.Minute > 0 || dt.Second > 0)
                            args[i] = dt.ToString("yyyy-MM-dd HH:mm:ss");
                        else
                            args[i] = dt.ToString("yyyy-MM-dd");
                    }
                }
            }
            if (args == null || args.Length < 1) return format;

            //format = format.Replace("{", "{{").Replace("}", "}}");

            return String.Format(format, args);
        }
        #endregion

        #region 属性
        private LogLevel? _Level;
        /// <summary>日志等级，只输出大于等于该级别的日志，默认Info，打开NewLife.Debug时默认为最低的Debug</summary>
        public LogLevel Level
        {
            get
            {
                if (_Level != null) return _Level.Value;

                try
                {
                    var def = LogLevel.Info;
                    //if (Config.GetConfig<Boolean>("NewLife.Debug")) def = LogLevel.Debug;
                    if (XTrace.Debug) def = LogLevel.Debug;

                    return Config.GetConfig<LogLevel>("NewLife.LogLevel", def);
                }
                catch { return LogLevel.Info; }
            }
            set { _Level = value; }
        }
        #endregion

        #region 静态空实现
        private static ILog _Null = new NullLogger();
        /// <summary>空日志实现</summary>
        public static ILog Null { get { return _Null; } }

        class NullLogger : Logger
        {
            protected override void OnWrite(LogLevel level, string format, params object[] args) { }
        }
        #endregion

        #region 日志头
        /// <summary>输出日志头，包含所有环境信息</summary>

        protected static String GetHead()
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
            var sb = new StringBuilder();
            sb.AppendFormat("#Software: {0}\r\n", name);
            sb.AppendFormat("#ProcessID: {0}{1}\r\n", process.Id, Runtime.Is64BitProcess ? " x64" : "");
            sb.AppendFormat("#AppDomain: {0}\r\n", AppDomain.CurrentDomain.FriendlyName);

            var fileName = String.Empty;
            try
            {
                fileName = process.StartInfo.FileName;
#if !Android
                // MonoAndroid无法识别MainModule，致命异常
                if (fileName.IsNullOrWhiteSpace()) fileName = process.MainModule.FileName;
#endif
            }
            catch { }
            if (!String.IsNullOrEmpty(fileName)) sb.AppendFormat("#FileName: {0}\r\n", fileName);

            // 应用域目录
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            sb.AppendFormat("#BaseDirectory: {0}\r\n", baseDir);

            // 当前目录。如果由别的进程启动，默认的当前目录就是父级进程的当前目录
            var curDir = Environment.CurrentDirectory;
            //if (!curDir.EqualIC(baseDir) && !(curDir + "\\").EqualIC(baseDir))
            if (!baseDir.EqualIgnoreCase(curDir, curDir + "\\"))
                sb.AppendFormat("#CurrentDirectory: {0}\r\n", curDir);

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
                    sb.AppendFormat("#CommandLine: {0}\r\n", line);
                }
            }

#if Android
            sb.AppendFormat("#ApplicationType: {0}", "Android");
#else
            sb.AppendFormat("#ApplicationType: {0}\r\n", Runtime.IsConsole ? "Console" : (Runtime.IsWeb ? "Web" : "WinForm"));
#endif
            sb.AppendFormat("#CLR: {0}\r\n", Environment.Version);

            sb.AppendFormat("#OS: {0}, {1}/{2}\r\n", Runtime.OSName, Environment.UserName, Environment.MachineName);
#if !Android
            var hi = NewLife.Common.HardInfo.Current;
            sb.AppendFormat("#CPU: {0}\r\n", hi.Processors);
            sb.AppendFormat("#Memory: {0:n0}M/{1:n0}M\r\n", Runtime.AvailableMemory,
                Runtime.PhysicalMemory);
#endif

            sb.AppendFormat("#Date: {0:yyyy-MM-dd}\r\n", DateTime.Now);
            sb.AppendFormat("#字段: 时间 线程ID 线程池Y网页W普通N 线程名 消息内容\r\n");
            sb.AppendFormat("#Fields: Time ThreadID IsPoolThread ThreadName Message\r\n");

            return sb.ToString();
        }
        #endregion
    }
}