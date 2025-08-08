using System.Collections.Concurrent;
using NewLife.Collections;

namespace NewLife.Log;

/// <summary>控制台输出日志</summary>
public class ConsoleLog : Logger
{
    /// <summary>是否使用多种颜色，默认使用</summary>
    public Boolean UseColor { get; set; } = true;

    private readonly ConcurrentQueue<WriteLogEventArgs> _Logs = new();
    private volatile Int32 _logCount;
    private Int32 _writing;
    private Object _lock = new();
    private Pool<WriteLogEventArgs> _pool = new(64);

    /// <summary>写日志</summary>
    /// <param name="level"></param>
    /// <param name="format"></param>
    /// <param name="args"></param>
    protected override void OnWrite(LogLevel level, String format, params Object?[] args)
    {
        // 日志队列积压将会导致内存暴增
        if (_logCount > 64) return;

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
            ThreadPool.UnsafeQueueUserWorkItem(s =>
            {
                try
                {
                    WriteConsole();
                }
                catch { }
                finally
                {
                    _writing = 0;
                }
            }, null);
        }
    }

    private void WriteConsole()
    {
        // 依次把队列日志写入文件
        while (_Logs.TryDequeue(out var e))
        {
            Interlocked.Decrement(ref _logCount);

            if (!UseColor)
            {
                Console.WriteLine(e.GetAndReset());
            }
            else
            {
                var hasLock = false;
                try
                {
                    if (Monitor.TryEnter(_lock, 5_000))
                    {
                        hasLock = true;
                        var cc = Console.ForegroundColor;
                        cc = e.Level switch
                        {
                            LogLevel.Warn => ConsoleColor.Yellow,
                            LogLevel.Error or LogLevel.Fatal => ConsoleColor.Red,
                            _ => GetColor(e.ThreadID),
                        };
                        //var old = Console.ForegroundColor;
                        Console.ForegroundColor = cc;
                        Console.WriteLine(e.GetAndReset());
                        //Console.ForegroundColor = old;
                        Console.ResetColor();
                    }
                }
                finally
                {
                    if (hasLock) Monitor.Exit(_lock);
                }
            }
            _pool.Return(e);
        }
    }

    static readonly ConcurrentDictionary<Int32, ConsoleColor> dic = new();
    static readonly ConsoleColor[] colors = [
        ConsoleColor.Green, ConsoleColor.Cyan, ConsoleColor.Magenta, ConsoleColor.White, ConsoleColor.Yellow,
        ConsoleColor.DarkGreen, ConsoleColor.DarkCyan, ConsoleColor.DarkMagenta, ConsoleColor.DarkRed, ConsoleColor.DarkYellow ];
    private static ConsoleColor GetColor(Int32 threadid)
    {
        if (threadid == 1) return ConsoleColor.Gray;

        return dic.GetOrAdd(threadid, k => colors[k % colors.Length]);
    }

    /// <summary>已重载。</summary>
    /// <returns></returns>
    public override String ToString() => $"{GetType().Name} UseColor={UseColor}";
}