namespace NewLife.Log;

/// <summary>等级日志提供者，不同等级分不同日志输出</summary>
public class LevelLog : Logger
{
    private IDictionary<LogLevel, ILog> _logs = new Dictionary<LogLevel, ILog>();

    /// <summary>通过指定路径和文件格式来实例化等级日志，每个等级使用自己的日志输出</summary>
    /// <param name="logPath"></param>
    /// <param name="fileFormat"></param>
    public LevelLog(String logPath, String fileFormat)
    {
        foreach (var item in Enum.GetValues(typeof(LogLevel)))
        {
            if (item is LogLevel level && level is > LogLevel.All and < LogLevel.Off)
            {
                _logs[level] = new TextFileLog(logPath, false, fileFormat) { Level = level };
            }
        }
    }

    /// <summary>写日志</summary>
    /// <param name="level"></param>
    /// <param name="format"></param>
    /// <param name="args"></param>
    protected override void OnWrite(LogLevel level, String format, params Object?[] args)
    {
        if (_logs.TryGetValue(level, out var log)) log.Write(level, format, args);
    }
}