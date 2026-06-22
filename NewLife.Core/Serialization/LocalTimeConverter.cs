#if NETCOREAPP
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NewLife.Serialization;

/// <summary>本地时间序列化</summary>
/// <remarks>
/// 序列化时本地时间使用 yyyy-MM-dd HH:mm:ss 格式，忽略时区信息；
/// UTC 时间使用 O 格式（ISO 8601 含 Z 后缀），便于跨时区识别。
/// 反序列化时，如果对方带有时区（含 Z 后缀或 UTC 后缀），也能转为本地时区。
/// Json传输DateTime一般是不带时区的，有些框架带有时区，这里无差别去掉时区转为本地时间，避免时间偏差。
/// 如需完整时区传输，推荐使用DateTimeOffset。
/// 设置 <see cref="FullTime"/> 为 true 时，所有 DateTime 均输出 O 格式（含毫秒和时区信息）。
/// </remarks>
public class LocalTimeConverter : JsonConverter<DateTime>
{
    /// <summary>时间日期格式。默认 yyyy-MM-dd HH:mm:ss，UTC 时间强制使用 O 格式</summary>
    public String DateTimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";

    /// <summary>完整时间格式。为 true 时所有 DateTime 统一输出 O 格式（ISO 8601 含毫秒和时区信息）</summary>
    public Boolean FullTime { get; set; }

    /// <summary>读取</summary>
    /// <param name="reader"></param>
    /// <param name="typeToConvert"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // 优先处理Unix时间戳
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out var unixTime))
            return unixTime.ToDateTime().ToLocalTime();

        var str = reader.GetString();
        if (DateTimeOffset.TryParse(str, out var dto)) return dto.LocalDateTime;

        var utc = false;
        if (!str.IsNullOrEmpty() && str.EndsWith("UTC"))
        {
            str = str.TrimSuffix("UTC").Trim();
            utc = true;
        }
        if (!DateTime.TryParse(str, out var dt)) return DateTime.MinValue;

        if (utc) dt = dt.ToLocalTime();

        return dt;
    }

    /// <summary>写入</summary>
    /// <param name="writer"></param>
    /// <param name="value"></param>
    /// <param name="options"></param>
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // UTC 时间或 FullTime 模式使用 O 格式（ISO 8601 含 Z 后缀），便于跨时区识别
        if (FullTime || value.Kind == DateTimeKind.Utc)
            writer.WriteStringValue(value.ToString("O"));
        else
            writer.WriteStringValue(value.ToString(DateTimeFormat));
    }
}
#endif