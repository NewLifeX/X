namespace NewLife.Log;

/// <summary>日志扩展</summary>
/// <remarks>
/// 带实例前缀的日志，前缀不参与本地化。先按日志语言翻译并格式化消息，再拼接前缀，
/// 避免 <see cref="Logger.Format"/> 用包含动态前缀的整串查表而永远回退英文。
/// </remarks>
public static class LogExtensions
{
    /// <summary>写信息日志。前缀在翻译之后拼接</summary>
    /// <param name="log">日志对象，可为空</param>
    /// <param name="prefix">日志前缀，可为空</param>
    /// <param name="format">格式化字符串</param>
    /// <param name="args">格式化参数</param>
    public static void InfoPrefixed(this ILog? log, String? prefix, String format, params Object?[] args) => log.WritePrefixed(LogLevel.Info, prefix, format, args);

    /// <summary>写错误日志。前缀在翻译之后拼接</summary>
    /// <param name="log">日志对象，可为空</param>
    /// <param name="prefix">日志前缀，可为空</param>
    /// <param name="format">格式化字符串</param>
    /// <param name="args">格式化参数</param>
    public static void ErrorPrefixed(this ILog? log, String? prefix, String format, params Object?[] args) => log.WritePrefixed(LogLevel.Error, prefix, format, args);

    /// <summary>写日志。前缀在翻译之后拼接</summary>
    /// <param name="log">日志对象，可为空</param>
    /// <param name="level">日志等级</param>
    /// <param name="prefix">日志前缀，可为空</param>
    /// <param name="format">格式化字符串</param>
    /// <param name="args">格式化参数</param>
    /// <example>
    /// <code>
    /// Log?.WritePrefixed(LogLevel.Info, "[Session]", "Received {0} bytes", count);
    /// </code>
    /// </example>
    public static void WritePrefixed(this ILog? log, LogLevel level, String? prefix, String format, params Object?[] args)
    {
        if (log == null || !log.Enable || level < log.Level) return;

        log.Write(level, prefix + Logger.FormatMessage(log, format, args));
    }
}
