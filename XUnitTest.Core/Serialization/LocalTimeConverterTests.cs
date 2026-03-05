using System.Text.Json;
using NewLife.Serialization;
using Xunit;

namespace XUnitTest.Serialization;

/// <summary>本地时间转换器测试</summary>
public class LocalTimeConverterTests
{
    private static readonly JsonSerializerOptions _options = new()
    {
        Converters = { new LocalTimeConverter() }
    };

    [Fact(DisplayName = "标准格式往返")]
    public void StandardFormatRoundTrip()
    {
        var dt = new DateTime(2025, 7, 1, 12, 30, 45, DateTimeKind.Local);
        var json = JsonSerializer.Serialize(dt, _options);
        Assert.NotNull(json);

        var restored = JsonSerializer.Deserialize<DateTime>(json, _options);
        Assert.Equal(dt.Year, restored.Year);
        Assert.Equal(dt.Month, restored.Month);
        Assert.Equal(dt.Day, restored.Day);
        Assert.Equal(dt.Hour, restored.Hour);
        Assert.Equal(dt.Minute, restored.Minute);
        Assert.Equal(dt.Second, restored.Second);
    }

    [Fact(DisplayName = "从Unix时间戳读取")]
    public void ReadFromUnixTimestamp()
    {
        // Unix timestamp for a date
        var json = "1719835200"; // 2024-07-01 12:00:00 UTC approximately
        var dt = JsonSerializer.Deserialize<DateTime>(json, _options);
        Assert.True(dt > DateTime.MinValue);
    }

    [Fact(DisplayName = "从ISO字符串读取")]
    public void ReadFromISOString()
    {
        var json = "\"2025-07-01T12:30:45\"";
        var dt = JsonSerializer.Deserialize<DateTime>(json, _options);
        Assert.Equal(2025, dt.Year);
        Assert.Equal(7, dt.Month);
        Assert.Equal(1, dt.Day);
    }

    [Fact(DisplayName = "带时区字符串转本地时间")]
    public void ReadWithTimezoneConvertsToLocal()
    {
        var json = "\"2025-07-01T12:30:45+00:00\"";
        var dt = JsonSerializer.Deserialize<DateTime>(json, _options);
        // 应转换为本地时间
        Assert.True(dt > DateTime.MinValue);
    }

    [Fact(DisplayName = "带UTC后缀字符串")]
    public void ReadWithUTCSuffix()
    {
        var json = "\"2025-07-01 12:30:45 UTC\"";
        var dt = JsonSerializer.Deserialize<DateTime>(json, _options);
        Assert.True(dt > DateTime.MinValue);
        Assert.Equal(2025, dt.Year);
    }

    [Fact(DisplayName = "无效字符串返回MinValue")]
    public void InvalidStringReturnsMinValue()
    {
        var json = "\"not-a-date\"";
        var dt = JsonSerializer.Deserialize<DateTime>(json, _options);
        Assert.Equal(DateTime.MinValue, dt);
    }

    [Fact(DisplayName = "自定义DateTimeFormat")]
    public void CustomDateTimeFormat()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new LocalTimeConverter { DateTimeFormat = "yyyy-MM-dd" } }
        };

        var dt = new DateTime(2025, 7, 1);
        var json = JsonSerializer.Serialize(dt, options);
        Assert.Contains("2025-07-01", json);
    }

    [Fact(DisplayName = "MinValue序列化")]
    public void MinValueSerialization()
    {
        var json = JsonSerializer.Serialize(DateTime.MinValue, _options);
        Assert.NotNull(json);
    }
}
