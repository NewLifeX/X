using System.Diagnostics;

namespace System;

#if NETFRAMEWORK || NETSTANDARD || NETCOREAPP3_1 || NET5_0 || NET6_0 || NET7_0
/// <summary>提供时间的抽象</summary>
public abstract class TimeProvider
{
    private sealed class SystemTimeProvider : TimeProvider
    {
        internal SystemTimeProvider() { }
    }

    private static readonly Int64 s_minDateTicks = DateTime.MinValue.Ticks;

    private static readonly Int64 s_maxDateTicks = DateTime.MaxValue.Ticks;

    /// <summary>获取一个 TimeProvider ，它提供基于 UtcNow的时钟、基于 的 Local时区、基于 的 Stopwatch高性能时间戳和基于 的 Timer计时器。</summary>
    public static TimeProvider System { get; set; } = new SystemTimeProvider();

    /// <summary>根据此 TimeProvider的时间概念获取本地时区。</summary>
    public virtual TimeZoneInfo LocalTimeZone => TimeZoneInfo.Local;

    /// <summary>获取 的频率 GetTimestamp() 作为每秒时钟周期数。</summary>
    public virtual Int64 TimestampFrequency => Stopwatch.Frequency;

    /// <summary>根据此 TimeProvider的时间概念，获取当前协调世界时 (UTC) 日期和时间，偏移量为零。</summary>
    /// <returns></returns>
    public virtual DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow;

    /// <summary>根据基于 TimeProvider的时间概念 GetUtcNow()获取当前日期和时间，偏移量设置为 LocalTimeZone与协调世界时 (UTC) 的偏移量。</summary>
    /// <returns></returns>
    public DateTimeOffset GetLocalNow()
    {
        var utcNow = GetUtcNow();
        var localTimeZone = LocalTimeZone ?? throw new ArgumentNullException(nameof(LocalTimeZone));

        var utcOffset = localTimeZone.GetUtcOffset(utcNow);
        if (utcOffset.Ticks == 0L) return utcNow;

        var num = utcNow.Ticks + utcOffset.Ticks;
        if ((UInt64)num > (UInt64)s_maxDateTicks)
            num = ((num < s_minDateTicks) ? s_minDateTicks : s_maxDateTicks);

        return new DateTimeOffset(num, utcOffset);
    }

    /// <summary>获取当前高频值，该值旨在测量计时器机制中精度较高的小时间间隔。</summary>
    /// <returns></returns>
    public virtual Int64 GetTimestamp() => Stopwatch.GetTimestamp();

    /// <summary>获取使用 GetTimestamp()检索到的两个时间戳之间的已用时间。</summary>
    /// <param name="startingTimestamp"></param>
    /// <param name="endingTimestamp"></param>
    /// <returns></returns>
    public TimeSpan GetElapsedTime(Int64 startingTimestamp, Int64 endingTimestamp)
    {
        var timestampFrequency = TimestampFrequency;
        if (timestampFrequency <= 0) throw new ArgumentOutOfRangeException(nameof(TimestampFrequency));

        return new TimeSpan((Int64)((endingTimestamp - startingTimestamp) * (10000000.0 / timestampFrequency)));
    }

    /// <summary>获取自使用 GetTimestamp()检索值以来startingTimestamp的运行时间。</summary>
    /// <param name="startingTimestamp"></param>
    /// <returns></returns>
    public TimeSpan GetElapsedTime(Int64 startingTimestamp) => GetElapsedTime(startingTimestamp, GetTimestamp());
}
#endif