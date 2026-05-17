using System.Collections.Concurrent;
using System.Diagnostics;
using NewLife.Log;

namespace XUnitTest.Integration;

/// <summary>测试专用日志，捕获日志条目用于断言服务端行为，同时透传至 XTrace。
/// 主要用于验证服务端的连接打开/关闭、数据收发等关键事件是否真实发生。</summary>
public class TestLog : ILog
{
    private readonly ConcurrentQueue<String> _entries = new();

    /// <summary>是否启用日志</summary>
    public Boolean Enable { get; set; } = true;

    /// <summary>日志级别</summary>
    public LogLevel Level { get; set; } = LogLevel.All;

    /// <summary>写入日志，同时保存到条目队列</summary>
    /// <param name="level">日志级别</param>
    /// <param name="format">格式化字符串</param>
    /// <param name="args">格式化参数数组</param>
    public void Write(LogLevel level, String format, params Object?[] args)
    {
        if (!Enable || level < Level) return;

        String entry;
        try { entry = args?.Length > 0 ? String.Format(format ?? "", args) : format ?? ""; }
        catch { entry = format ?? ""; }

        _entries.Enqueue(entry);
        XTrace.Log.Write(level, format, args);
    }

    /// <summary>调试日志</summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="args">格式化参数数组</param>
    public void Debug(String format, params Object?[] args) => Write(LogLevel.Debug, format, args);

    /// <summary>信息日志</summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="args">格式化参数数组</param>
    public void Info(String format, params Object?[] args) => Write(LogLevel.Info, format, args);

    /// <summary>警告日志</summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="args">格式化参数数组</param>
    public void Warn(String format, params Object?[] args) => Write(LogLevel.Warn, format, args);

    /// <summary>错误日志</summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="args">格式化参数数组</param>
    public void Error(String format, params Object?[] args) => Write(LogLevel.Error, format, args);

    /// <summary>严重错误日志</summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="args">格式化参数数组</param>
    public void Fatal(String format, params Object?[] args) => Write(LogLevel.Fatal, format, args);

    /// <summary>检查已捕获的日志条目中是否包含指定文本（忽略大小写）</summary>
    /// <param name="text">要查找的文本</param>
    public Boolean Contains(String text) =>
        _entries.Any(e => e.Contains(text, StringComparison.OrdinalIgnoreCase));

    /// <summary>轮询等待日志中出现指定文本，超时返回 false</summary>
    /// <param name="text">要等待的文本</param>
    /// <param name="timeoutMs">超时毫秒数，默认 3000</param>
    public async Task<Boolean> WaitForAsync(String text, Int32 timeoutMs = 3_000)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            if (Contains(text)) return true;
            await Task.Delay(10);
        }
        return false;
    }

    /// <summary>清空已记录的日志条目</summary>
    public void Clear()
    {
        while (_entries.TryDequeue(out _)) { }
    }
}
