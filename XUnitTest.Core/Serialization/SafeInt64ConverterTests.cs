using System.Text.Json;
using NewLife.Serialization;
using Xunit;

namespace XUnitTest.Serialization;

/// <summary>安全Int64/UInt64转换器测试</summary>
public class SafeInt64ConverterTests
{
    private static readonly JsonSerializerOptions _options = new()
    {
        Converters = { new SafeInt64Converter(), new SafeUInt64Converter() }
    };

    #region SafeInt64Converter
    [Fact(DisplayName = "安全范围内的Int64序列化为数字")]
    public void Int64InSafeRangeSerializesAsNumber()
    {
        var json = JsonSerializer.Serialize(12345L, _options);
        Assert.Equal("12345", json);
    }

    [Fact(DisplayName = "超过JS安全范围的Int64序列化为字符串")]
    public void Int64BeyondSafeRangeSerializesAsString()
    {
        var value = 9_007_199_254_740_992L; // 2^53
        var json = JsonSerializer.Serialize(value, _options);
        Assert.Equal($"\"{value}\"", json);
    }

    [Fact(DisplayName = "负数超过JS安全范围序列化为字符串")]
    public void NegativeInt64BeyondSafeRangeSerializesAsString()
    {
        var value = -9_007_199_254_740_992L;
        var json = JsonSerializer.Serialize(value, _options);
        Assert.Equal($"\"{value}\"", json);
    }

    [Fact(DisplayName = "从数字反序列化Int64")]
    public void DeserializeInt64FromNumber()
    {
        var json = "12345";
        var result = JsonSerializer.Deserialize<Int64>(json, _options);
        Assert.Equal(12345L, result);
    }

    [Fact(DisplayName = "从字符串反序列化Int64")]
    public void DeserializeInt64FromString()
    {
        var json = "\"9007199254740992\"";
        var result = JsonSerializer.Deserialize<Int64>(json, _options);
        Assert.Equal(9_007_199_254_740_992L, result);
    }

    [Fact(DisplayName = "Int64安全边界值")]
    public void Int64BoundaryValues()
    {
        var safeMax = 9_007_199_254_740_991L;
        var json = JsonSerializer.Serialize(safeMax, _options);
        Assert.Equal("9007199254740991", json); // 安全范围内，序列化为数字

        var unsafeValue = safeMax + 1;
        json = JsonSerializer.Serialize(unsafeValue, _options);
        Assert.StartsWith("\"", json); // 超出范围，序列化为字符串
    }

    [Fact(DisplayName = "Int64往返序列化")]
    public void Int64RoundTrip()
    {
        var original = 9_007_199_254_740_992L;
        var json = JsonSerializer.Serialize(original, _options);
        var restored = JsonSerializer.Deserialize<Int64>(json, _options);
        Assert.Equal(original, restored);
    }
    #endregion

    #region SafeUInt64Converter
    [Fact(DisplayName = "安全范围内的UInt64序列化为数字")]
    public void UInt64InSafeRangeSerializesAsNumber()
    {
        var json = JsonSerializer.Serialize(12345UL, _options);
        Assert.Equal("12345", json);
    }

    [Fact(DisplayName = "超过JS安全范围的UInt64序列化为字符串")]
    public void UInt64BeyondSafeRangeSerializesAsString()
    {
        var value = 9_007_199_254_740_992UL;
        var json = JsonSerializer.Serialize(value, _options);
        Assert.Equal($"\"{value}\"", json);
    }

    [Fact(DisplayName = "从字符串反序列化UInt64")]
    public void DeserializeUInt64FromString()
    {
        var json = "\"18446744073709551615\""; // UInt64.MaxValue
        var result = JsonSerializer.Deserialize<UInt64>(json, _options);
        Assert.Equal(UInt64.MaxValue, result);
    }

    [Fact(DisplayName = "UInt64往返序列化")]
    public void UInt64RoundTrip()
    {
        var original = UInt64.MaxValue;
        var json = JsonSerializer.Serialize(original, _options);
        var restored = JsonSerializer.Deserialize<UInt64>(json, _options);
        Assert.Equal(original, restored);
    }
    #endregion
}
