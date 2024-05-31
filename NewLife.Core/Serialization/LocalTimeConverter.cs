#if NETCOREAPP
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NewLife.Serialization;

/// <summary>本地时间列化</summary>
/// <remarks>
/// 符合标准格式 yyyy-MM-dd HH:mm:ss
/// 序列化时，忽略时区信息，上层应用需要自己处理好；
/// 反序列化时，如果对方带有时区，也能转为本地时区。
/// Json传输DateTime一般是不带时区的，有些框架带有时区，这里无差别去掉时区转为本地时间，避免时间偏差。
/// 如果非要传输带有时区的时间，推荐使用DateTimeOffset。
/// </remarks>
public class LocalTimeConverter : JsonConverter<DateTime>
{
    /// <summary>时间日期格式</summary>
    public String DateTimeFormat { get; set; } = "O";

    /// <summary>读取</summary>
    /// <param name="reader"></param>
    /// <param name="typeToConvert"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString();
        if (DateTimeOffset.TryParse(str, out var dto)) return dto.LocalDateTime;

        var utc = false;
        if (!str.IsNullOrEmpty() && str.EndsWith("UTC"))
        {
            str = str.TrimEnd("UTC").Trim();
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
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString(DateTimeFormat));
}
#endif