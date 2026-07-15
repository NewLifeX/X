using System;
using Xunit;
using Xunit.Abstractions;

namespace XUnitTest.Common;

/// <summary>测试 TimeProvider 时间抽象功能</summary>
public class TimeProviderTests
{
    private readonly ITestOutputHelper _output;

    public TimeProviderTests(ITestOutputHelper output) => _output = output;

    [Fact(DisplayName = "TimeProvider.System 默认实例不为空")]
    public void SystemInstance()
    {
        var tp = TimeProvider.System;
        Assert.NotNull(tp);
        _output.WriteLine("System={0}", tp);
    }

    [Fact(DisplayName = "GetUtcNow 返回当前 UTC 时间")]
    public void GetUtcNow()
    {
        var tp = TimeProvider.System;
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var now = tp.GetUtcNow();
        var after = DateTimeOffset.UtcNow.AddSeconds(1);

        Assert.True(now >= before, $"GetUtcNow 不应早于 1 秒前：{now:O} < {before:O}");
        Assert.True(now <= after, $"GetUtcNow 不应晚于 1 秒后：{now:O} > {after:O}");
    }

    [Fact(DisplayName = "GetLocalNow 返回当前本地时间")]
    public void GetLocalNow()
    {
        var tp = TimeProvider.System;
        var local = tp.GetLocalNow();
        var utc = tp.GetUtcNow();

        // 本地 DateTimeOffset 的偏移量应等于本地时区偏移
        var expectedOffset = tp.LocalTimeZone?.GetUtcOffset(utc) ?? TimeSpan.Zero;
        Assert.Equal(expectedOffset, local.Offset);

        // 本地时间的 UTC 时刻应与 GetUtcNow 相近（允许 1 秒偏差）
        var diff = local.ToUniversalTime() - utc;
        Assert.True(Math.Abs(diff.TotalSeconds) < 1,
            $"GetLocalNow 转 UTC 后应与 GetUtcNow 一致：diff={diff.TotalSeconds:F2}s");
    }

    [Fact(DisplayName = "LocalTimeZone 时区信息不为空")]
    public void LocalTimeZone()
    {
        var tp = TimeProvider.System;
        Assert.NotNull(tp.LocalTimeZone);
        _output.WriteLine("LocalTimeZone={0}", tp.LocalTimeZone?.DisplayName);
    }

    [Fact(DisplayName = "TimestampFrequency 频率大于 0")]
    public void TimestampFrequency()
    {
        var tp = TimeProvider.System;
        Assert.True(tp.TimestampFrequency > 0, $"频率应大于 0，实际={tp.TimestampFrequency}");
        _output.WriteLine("TimestampFrequency={0}", tp.TimestampFrequency);
    }

    [Fact(DisplayName = "GetTimestamp 返回有效时间戳")]
    public void GetTimestamp()
    {
        var tp = TimeProvider.System;
        var ts1 = tp.GetTimestamp();
        Assert.True(ts1 > 0, $"时间戳应大于 0，实际={ts1}");

        var ts2 = tp.GetTimestamp();
        Assert.True(ts2 >= ts1, $"连续两次获取时间戳应递增：{ts2} >= {ts1}");
    }

    [Fact(DisplayName = "GetElapsedTime 计算耗时正确")]
    public void GetElapsedTime()
    {
        var tp = TimeProvider.System;
        var start = tp.GetTimestamp();
        Thread.Sleep(10); // 等待至少 10ms
        var elapsed = tp.GetElapsedTime(start);

        Assert.True(elapsed > TimeSpan.Zero, $"耗时应大于 0，实际={elapsed}");
        Assert.True(elapsed.TotalMilliseconds >= 5, $"耗时至少 5ms，实际={elapsed.TotalMilliseconds:F1}ms");
    }

    [Fact(DisplayName = "GetElapsedTime 双参数重载计算区间耗时")]
    public void GetElapsedTime_Range()
    {
        var tp = TimeProvider.System;
        var start = tp.GetTimestamp();
        Thread.Sleep(5);
        var mid = tp.GetTimestamp();
        Thread.Sleep(5);
        var end = tp.GetTimestamp();

        var elapsed1 = tp.GetElapsedTime(start, mid);
        var elapsed2 = tp.GetElapsedTime(mid, end);
        var elapsed3 = tp.GetElapsedTime(start, end);

        // 分段耗时之和应等于总耗时（允许 1 tick 舍入误差）
        Assert.True(elapsed1 > TimeSpan.Zero);
        Assert.True(elapsed2 > TimeSpan.Zero);
        Assert.True(Math.Abs((elapsed3 - (elapsed1 + elapsed2)).TotalMilliseconds) < 1,
            $"分段耗时之和应约等于总耗时：elapsed1={elapsed1.TotalMilliseconds:F1}ms, elapsed2={elapsed2.TotalMilliseconds:F1}ms, total={elapsed3.TotalMilliseconds:F1}ms");
    }
}
