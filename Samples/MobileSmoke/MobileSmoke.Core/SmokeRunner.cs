using System.Diagnostics;
using System.Text;
using NewLife.Log;

namespace MobileSmoke;

/// <summary>用例跳过标记。</summary>
public sealed class SkipException : Exception
{
    /// <summary>实例化跳过异常。</summary>
    /// <param name="message">跳过原因。</param>
    public SkipException(String message) : base(message) { }
}

/// <summary>冒烟执行器。</summary>
public static class SmokeRunner
{
    /// <summary>单个用例的超时时间。</summary>
    public static TimeSpan CaseTimeout { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>依次执行全部用例。</summary>
    /// <param name="onResult">每个用例完成后回调，用于 UI 或日志输出。</param>
    /// <returns>冒烟报告。</returns>
    public static SmokeReport RunAll(Action<SmokeResult>? onResult = null)
    {
        var report = new SmokeReport();
        var savedLog = XTrace.Log;
        XTrace.Log = new DiagnosticLog();

        try
        {
            foreach (var item in SmokeCases.All())
            {
                RunCase(item, report, onResult);
            }
        }
        finally
        {
            XTrace.Log = savedLog;
        }

        return report;
    }

    private static void RunCase((String Name, Action Action) item, SmokeReport report, Action<SmokeResult>? onResult)
    {
        var sw = Stopwatch.StartNew();
        SmokeResult result;
        try
        {
            var task = Task.Run(item.Action);
            if (!task.Wait(CaseTimeout)) throw new TimeoutException($"用例超过 {CaseTimeout.TotalSeconds:0} 秒未完成");

            result = new SmokeResult
            {
                Name = item.Name,
                Status = SmokeStatus.Pass,
                ElapsedMs = sw.ElapsedMilliseconds,
            };
        }
        catch (AggregateException ex) when (ex.InnerException is SkipException skip)
        {
            result = new SmokeResult
            {
                Name = item.Name,
                Status = SmokeStatus.Skip,
                Message = skip.Message,
                ElapsedMs = sw.ElapsedMilliseconds,
            };
        }
        catch (SkipException ex)
        {
            result = new SmokeResult
            {
                Name = item.Name,
                Status = SmokeStatus.Skip,
                Message = ex.Message,
                ElapsedMs = sw.ElapsedMilliseconds,
            };
        }
        catch (Exception ex)
        {
            result = new SmokeResult
            {
                Name = item.Name,
                Status = SmokeStatus.Fail,
                Message = FormatException(ex),
                ElapsedMs = sw.ElapsedMilliseconds,
            };
        }
        finally
        {
            sw.Stop();
        }

        report.Results.Add(result);
        onResult?.Invoke(result);
    }

    private static String FormatException(Exception ex)
    {
        if (ex is AggregateException aggregate) ex = aggregate.Flatten().InnerExceptions.FirstOrDefault() ?? aggregate;

        var sb = new StringBuilder();
        for (var current = ex; current != null; current = current.InnerException)
        {
            if (sb.Length > 0) sb.Append(" <- ");
            sb.Append(current.GetType().Name).Append(": ").Append(current.Message);
        }

        return sb.ToString();
    }
}

/// <summary>把 NewLife 内部日志转发到冒烟诊断输出。</summary>
internal sealed class DiagnosticLog : ILog
{
    public Boolean Enable { get; set; } = true;

    public LogLevel Level { get; set; } = LogLevel.Debug;

    public void Write(LogLevel level, String format, params Object?[] args)
    {
        if (!Enable || level < Level) return;

        var text = format;
        try
        {
            if (args != null && args.Length > 0) text = String.Format(format, args);
        }
        catch
        {
            // 格式化失败时保留原始格式串
        }

        SmokeEnvironment.WriteDiagnostic($"[{level}] {text}");
    }

    public void Debug(String format, params Object?[] args) => Write(LogLevel.Debug, format, args);

    public void Info(String format, params Object?[] args) => Write(LogLevel.Info, format, args);

    public void Warn(String format, params Object?[] args) => Write(LogLevel.Warn, format, args);

    public void Error(String format, params Object?[] args) => Write(LogLevel.Error, format, args);

    public void Fatal(String format, params Object?[] args) => Write(LogLevel.Fatal, format, args);
}
