#if NETCOREAPP
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NewLife.Serialization;

/// <summary>安全的Int64转换器</summary>
public sealed class SafeInt64Converter : JsonConverter<Int64>
{
    private const Int64 JsSafeMax = 9_007_199_254_740_991;   // 2^53 - 1
    private const Int64 JsSafeMin = -9_007_199_254_740_991;

    /// <summary>读取Int64值</summary>
    public override Int64 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // 允许数值或字符串两种形式
        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetInt64(),
            JsonTokenType.String => Int64.Parse(reader.GetString()!, System.Globalization.CultureInfo.InvariantCulture),
            _ => throw new JsonException("Invalid token for Int64")
        };
    }

    /// <summary>写入Int64值</summary>
    public override void Write(Utf8JsonWriter writer, Int64 value, JsonSerializerOptions options)
    {
        if (value > JsSafeMax || value < JsSafeMin)
            writer.WriteStringValue(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        else
            writer.WriteNumberValue(value);
    }
}

/// <summary>安全的UInt64转换器</summary>
public sealed class SafeUInt64Converter : JsonConverter<UInt64>
{
    private const UInt64 JsSafeMax = 9_007_199_254_740_991;

    /// <summary>读取UInt64值</summary>
    public override UInt64 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetUInt64(),
            JsonTokenType.String => UInt64.Parse(reader.GetString()!, System.Globalization.CultureInfo.InvariantCulture),
            _ => throw new JsonException("Invalid token for UInt64")
        };
    }

    /// <summary>写入UInt64值</summary>
    public override void Write(Utf8JsonWriter writer, UInt64 value, JsonSerializerOptions options)
    {
        if (value > JsSafeMax)
            writer.WriteStringValue(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        else
            writer.WriteNumberValue(value);
    }
}
#endif