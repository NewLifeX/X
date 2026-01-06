namespace NewLife.Log;

/// <summary>依托于动作的日志类</summary>
public class ActionLog : Logger
{
    /// <summary>日志输出方法</summary>
    public Action<String, Object?[]> Method { get; set; }

    /// <summary>使用指定方法实例化动作日志</summary>
    /// <param name="action">日志输出动作，接收格式化字符串和参数数组</param>
    public ActionLog(Action<String, Object?[]> action) => Method = action;

    /// <summary>写日志</summary>
    /// <param name="level">日志级别</param>
    /// <param name="format">格式化字符串</param>
    /// <param name="args">格式化参数数组</param>
    protected override void OnWrite(LogLevel level, String format, params Object?[] args) => Method?.Invoke(format, args);

    /// <summary>已重载</summary>
    /// <returns></returns>
    public override String ToString() => Method + "";
}