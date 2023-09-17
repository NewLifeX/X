namespace NewLife.Log;

/// <summary>依托于动作的日志类</summary>
public class ActionLog : Logger
{
    /// <summary>方法</summary>
    public Action<String, Object?[]> Method { get; set; }

    /// <summary>使用指定方法否则动作日志</summary>
    /// <param name="action"></param>
    public ActionLog(Action<String, Object?[]> action) => Method = action;

    /// <summary>写日志</summary>
    /// <param name="level"></param>
    /// <param name="format"></param>
    /// <param name="args"></param>
    protected override void OnWrite(LogLevel level, String format, params Object?[] args) => Method?.Invoke(format, args);

    /// <summary>已重载</summary>
    /// <returns></returns>
    public override String ToString() => Method + "";
}