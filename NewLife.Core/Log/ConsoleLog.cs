using System.Collections.Concurrent;
using NewLife.Collections;

namespace NewLife.Log;

/// <summary>控制台输出日志</summary>
/// <remarks>
/// 按日志级别与线程整行着色：警告、错误、致命三级使用级别色，其余按线程取色，
/// 使同一线程的日志在控制台上成块显示，便于追踪单个线程的执行上下文。
/// </remarks>
public class ConsoleLog : Logger
{
    #region 属性
    /// <summary>是否使用多种颜色，默认使用</summary>
    public Boolean UseColor { get; set; } = true;

    /// <summary>是否使用 ANSI 扩展颜色（256 色），默认使用；终端不支持时自动退回 16 色</summary>
    /// <remarks>
    /// 非 Windows 终端天然支持；Windows 需要开启虚拟终端支持。经典控制台即使开启，
    /// 也只会把 256 色映射到最近的 16 色，因此自动改走 16 色调色板。
    /// </remarks>
    public Boolean UseAnsiColor { get; set; } = true;

    /// <summary>线程颜色调色板（16 色模式）。为空时使用默认调色板</summary>
    public ConsoleColor[]? Colors { get; set; }

    /// <summary>线程颜色调色板（ANSI 256 色模式，xterm 配色索引）。为空时使用默认调色板</summary>
    public Byte[]? AnsiColors { get; set; }

    /// <summary>输出目标。默认为控制台；设置为其它写入器时只输出覆盖后的文本，不再调用控制台颜色 API</summary>
    internal TextWriter? Output { get; set; }

    /// <summary>当前目标是否支持彩色输出</summary>
    internal virtual Boolean SupportColor => Output == null && ConsoleColorPalette.SupportColor;

    /// <summary>当前目标是否支持 ANSI 256 色</summary>
    internal virtual Boolean SupportAnsiColor => Output == null && UseAnsiColor && ConsoleColorPalette.SupportAnsiColor;
    #endregion

    #region 字段
    /// <summary>颜色映射表容量上限。超过后改用线程编号，避免长跑服务持续创建线程导致内存增长</summary>
    private const Int32 MaxColorIndexes = 1024;

    private readonly ConcurrentQueue<WriteLogEventArgs> _Logs = new();
    private readonly ConcurrentDictionary<Int32, Int32> _colorIndexes = new();
    private volatile Int32 _logCount;
    private Int32 _writing;
    private Int32 _colorIndex;
    private Pool<WriteLogEventArgs> _pool = new(64);
    #endregion

    /// <summary>写日志</summary>
    /// <param name="level"></param>
    /// <param name="format"></param>
    /// <param name="args"></param>
    protected override void OnWrite(LogLevel level, String format, params Object?[] args)
    {
        // 日志队列积压将会导致内存暴增
        if (_logCount > 1024) return;

        var e = _pool.Get();
        e.Set(level);

        // 特殊处理异常对象
        if (args != null && args.Length == 1 && args[0] is Exception ex && (format.IsNullOrEmpty() || format == "{0}"))
            e = e.Set(null, ex);
        else
            e = e.Set(Format(format, args), null);

        // 推入队列
        _Logs.Enqueue(e);
        Interlocked.Increment(ref _logCount);

        // 异步写日志，实时。即使这里错误，定时器那边仍然会补上
        if (Interlocked.CompareExchange(ref _writing, 1, 0) == 0)
        {
            ThreadPool.UnsafeQueueUserWorkItem(WriteConsoleAsync, null);
        }
    }

    /// <summary>异步写控制台日志。复位写入标记后若队列仍有日志则接力调度，避免竞态窗口内日志滞留</summary>
    private void WriteConsoleAsync(Object? state)
    {
        try
        {
            WriteConsole();
        }
        catch { }
        finally
        {
            _writing = 0;

            // 复位后如果又有新日志入队，且没有其它 worker 在处理，则接力调度
            if (!_Logs.IsEmpty && Interlocked.CompareExchange(ref _writing, 1, 0) == 0)
            {
                ThreadPool.UnsafeQueueUserWorkItem(WriteConsoleAsync, null);
            }
        }
    }

    private void WriteConsole()
    {
        // 依次把队列日志写入控制台。_writing 已保证同一时刻只有一个写入者，循环体不会并发，无需加锁
        while (_Logs.TryDequeue(out var e))
        {
            Interlocked.Decrement(ref _logCount);

            if (!UseColor || !SupportColor)
                WriteLine(e.GetAndReset());
            else
                WriteColor(e);

            _pool.Return(e);
        }
    }

    /// <summary>彩色写入。警告与错误使用级别色，其余按线程取色，均整行着色</summary>
    /// <param name="e">日志事件参数</param>
    private void WriteColor(WriteLogEventArgs e)
    {
        var level = e.Level;
        var ansi = SupportAnsiColor;

        // 级别色优先，保证警告与错误在任何线程下都能第一时间被看到
        if (level is LogLevel.Warn or LogLevel.Error or LogLevel.Fatal)
        {
            var line = e.GetAndReset();
            if (ansi)
            {
                var color = level switch
                {
                    LogLevel.Warn => ConsoleColorPalette.AnsiWarn,
                    LogLevel.Fatal => ConsoleColorPalette.AnsiFatal,
                    _ => ConsoleColorPalette.AnsiError,
                };
                WriteLine(ConsoleColorPalette.Wrap(color, line));
            }
            else if (Output == null)
            {
                Console.ForegroundColor = level == LogLevel.Warn ? ConsoleColorPalette.BasicWarn : ConsoleColorPalette.BasicError;
                Console.WriteLine(line);
                Console.ResetColor();
            }
            else
            {
                WriteLine(line);
            }

            return;
        }

        // 主线程使用中性色，不占用调色板
        var index = e.ThreadID == 1 ? -1 : GetColorIndex(e.ThreadID);

        var text = e.GetAndReset();
        if (ansi)
        {
            var colors = AnsiColors;
            if (colors == null || colors.Length == 0) colors = ConsoleColorPalette.Ansi;
            var color = index < 0 ? ConsoleColorPalette.AnsiMaster : colors[index % colors.Length];
            WriteLine(ConsoleColorPalette.Wrap(color, text));
        }
        else if (Output == null)
        {
            var colors = Colors;
            if (colors == null || colors.Length == 0) colors = ConsoleColorPalette.Basic;
            Console.ForegroundColor = index < 0 ? ConsoleColorPalette.BasicMaster : colors[index % colors.Length];
            Console.WriteLine(text);
            Console.ResetColor();
        }
        else
        {
            // 自定义输出目标不支持颜色，按原样输出
            WriteLine(text);
        }
    }

    /// <summary>写入一行文本，输出到自定义目标或控制台</summary>
    /// <param name="text"></param>
    private void WriteLine(String text)
    {
        if (Output != null) Output.WriteLine(text);
        else Console.WriteLine(text);
    }

    /// <summary>获取线程颜色序号。先到先得，前 N 个写日志的线程颜色不重复</summary>
    /// <param name="threadID">线程编号</param>
    /// <returns></returns>
    internal Int32 GetColorIndex(Int32 threadID)
    {
        if (_colorIndexes.TryGetValue(threadID, out var index)) return index;

        // 映射表有界，避免长跑服务持续创建线程导致内存增长
        if (_colorIndexes.Count >= MaxColorIndexes) return threadID;

        index = Interlocked.Increment(ref _colorIndex) - 1;
        if (_colorIndexes.TryAdd(threadID, index)) return index;

        return _colorIndexes[threadID];
    }

    /// <summary>已重载。</summary>
    /// <returns></returns>
    public override String ToString() => $"{GetType().Name} UseColor={UseColor}";
}
