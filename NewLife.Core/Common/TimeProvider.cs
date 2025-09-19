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

    /// <summary>获取系统默认的 TimeProvider，提供基于 UtcNow 的时钟、Local 时区、高性能时间戳和计时器。</summary>
    public static TimeProvider System { get; set; } = new SystemTimeProvider();

    /// <summary>根据当前 TimeProvider 的时间概念获取本地时区。</summary>
    public virtual TimeZoneInfo LocalTimeZone => TimeZoneInfo.Local;

    /// <summary>获取时间戳频率（单位：每秒时钟周期数）。</summary>
    public virtual Int64 TimestampFrequency => Stopwatch.Frequency;

    /// <summary>根据当前 TimeProvider 的时间概念，获取当前 UTC 时间。</summary>
    public virtual DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow;

    /// <summary>基于 <see cref="GetUtcNow"/> 获取当前本地时间，偏移量由 <see cref="LocalTimeZone"/> 与 UTC 的差确定。</summary>
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

    /// <summary>获取高频时间戳，用于测量小时间间隔（通常来自 <see cref="Stopwatch.GetTimestamp"/>）。</summary>
    public virtual Int64 GetTimestamp() => Stopwatch.GetTimestamp();

    /// <summary>计算两个时间戳（来自 <see cref="GetTimestamp"/>）之间的耗时。</summary>
    public TimeSpan GetElapsedTime(Int64 startingTimestamp, Int64 endingTimestamp)
    {
        var timestampFrequency = TimestampFrequency;
        if (timestampFrequency <= 0) throw new ArgumentOutOfRangeException(nameof(TimestampFrequency));

        return new TimeSpan((Int64)((endingTimestamp - startingTimestamp) * (10000000.0 / timestampFrequency)));
    }

    /// <summary>计算自指定时间戳（来自 <see cref="GetTimestamp"/>）以来的运行时间。</summary>
    public TimeSpan GetElapsedTime(Int64 startingTimestamp) => GetElapsedTime(startingTimestamp, GetTimestamp());
}
#endif