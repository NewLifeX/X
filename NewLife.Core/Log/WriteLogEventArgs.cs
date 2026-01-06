using NewLife.Collections;

namespace NewLife.Log;

/// <summary>写日志事件参数</summary>
public class WriteLogEventArgs : EventArgs
{
    #region 属性
    /// <summary>日志等级</summary>
    public LogLevel Level { get; set; }

    /// <summary>日志信息</summary>
    public String? Message { get; set; }

    /// <summary>异常</summary>
    public Exception? Exception { get; set; }

    /// <summary>时间</summary>
    public DateTime Time { get; set; }

    /// <summary>线程编号</summary>
    public Int32 ThreadID { get; set; }

    /// <summary>是否线程池线程</summary>
    public Boolean IsPool { get; set; }

    /// <summary>是否Web线程</summary>
    public Boolean IsWeb { get; set; }

    /// <summary>线程名</summary>
    public String? ThreadName { get; set; }

    /// <summary>任务编号</summary>
    public Int32 TaskID { get; set; }
    #endregion

    #region 构造
    /// <summary>实例化一个日志事件参数</summary>
    public WriteLogEventArgs() { }
    #endregion

    #region 线程专有实例
    /*2015-06-01 @宁波-小董
     * 将Current以及Set方法组从internal修改为Public
     * 原因是 Logger在进行扩展时，重载OnWrite需要用到该静态属性以及方法，internal无法满足扩展要求
     * */
    [ThreadStatic]
    private static WriteLogEventArgs? _Current;
    /// <summary>线程专有实例。线程静态，每个线程只用一个，避免GC浪费</summary>
    public static WriteLogEventArgs Current => _Current ??= new WriteLogEventArgs();
    #endregion

    #region 方法
    /// <summary>初始化为新日志</summary>
    /// <param name="level">日志等级</param>
    /// <returns>返回自身，链式写法</returns>
    public WriteLogEventArgs Set(LogLevel level)
    {
        Level = level;

        return this;
    }

    /// <summary>初始化为新日志</summary>
    /// <param name="message">日志</param>
    /// <param name="exception">异常</param>
    /// <returns>返回自身，链式写法</returns>
    public WriteLogEventArgs Set(String? message, Exception? exception)
    {
        Message = message;
        Exception = exception;

        Init();

        return this;
    }

    void Init()
    {
        // todo: 如果系统使用utc时间，可以把日志时间转换为本地时间
        Time = DateTime.Now.AddHours(Setting.Current.UtcIntervalHours);
        var thread = Thread.CurrentThread;
        ThreadID = thread.ManagedThreadId;
        IsPool = thread.IsThreadPoolThread;
        ThreadName = CurrentThreadName ?? thread.Name;

        var tid = Task.CurrentId;
        TaskID = tid != null ? tid.Value : -1;

        //IsWeb = System.Web.HttpContext.Current != null;
    }

    /// <summary>重置日志事件对象，释放内存</summary>
    public void Reset()
    {
        Level = LogLevel.Info;
        Message = null;
        Exception = null;
        Time = default;
        ThreadID = 0;
        IsPool = false;
        IsWeb = false;
        ThreadName = null;
        TaskID = 0;
    }

    /// <summary>获取日志全文，并重置对象释放内存</summary>
    /// <returns></returns>
    public String GetAndReset()
    {
        var msg = ToString();
        Reset();

        return msg;
    }

    private static String[]? _lines;
    /// <summary>已重载。</summary>
    /// <returns></returns>
    public override String ToString()
    {
        if (Exception != null) Message += Exception.GetMessage();

        var name = ThreadName;
        if (name.IsNullOrEmpty())
            name = TaskID >= 0 ? TaskID + "" : "-";
        else if (name.StartsWithIgnoreCase("Threadpool", ".NET ThreadPool", ".NET TP", "Thread Pool"))
            name = TaskID >= 0 ? TaskID + "" : "P";
        else if (name.EqualIgnoreCase("IO Threadpool worker"))
            name = "IO";
        else if (name.EqualIgnoreCase(".NET Long Running Task"))
            name = "L";

        //return $"{Time:HH:mm:ss.fff} {ThreadID,2} {(IsPool ? (IsWeb ? 'W' : 'Y') : 'N')} {name} {Message}";

        var lines = _lines;
        if (lines == null || lines.Length == 0)
        {
            var format = Setting.Current.LogLineFormat;
            if (format.IsNullOrEmpty()) format = "Time|ThreadId|Kind|Name|Message";
            lines = _lines = format.Split("|");
        }

        var sb = Pool.StringBuilder.Get();

        for (var i = 0; i < lines.Length; i++)
        {
            if (i > 0) sb.Append(' ');
            switch (lines[i])
            {
                case "Time":
                    sb.Append(Time.ToString("HH:mm:ss.fff"));
                    break;
                case "ThreadId":
                    sb.Append(ThreadID.ToString("00"));
                    break;
                case "Kind":
                    sb.Append(IsPool ? (IsWeb ? 'W' : 'Y') : 'N');
                    break;
                case "Name":
                    sb.Append(name);
                    break;
                case "Level":
                    sb.Append('[');
                    sb.Append(Level);
                    sb.Append(']');
                    break;
                case "Message":
                    if (!Message.IsNullOrEmpty()) sb.Append(Message);
                    break;
                default:
                    break;
            }
        }

        return sb.Return(true);
    }
    #endregion

    #region 日志线程名
    [ThreadStatic]
    private static String? _threadName;
    /// <summary>设置当前线程输出日志时的线程名</summary>
    public static String? CurrentThreadName { get => _threadName; set => _threadName = value; }
    #endregion
}