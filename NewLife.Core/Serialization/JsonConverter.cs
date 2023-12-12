#if NETCOREAPP
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NewLife.Serialization;

/// <summary>Json反序列化时进行类型绑定。用于指定接口的实现类</summary>
/// <typeparam name="TService"></typeparam>
/// <typeparam name="TImplementation"></typeparam>
public class JsonConverter<TService, TImplementation> : JsonConverter<TService> where TImplementation : TService
{
    /// <summary>读取</summary>
    /// <param name="reader"></param>
    /// <param name="typeToConvert"></param>
    /// <param name="options"></param>
    /// <returns></returns>
#if NETCOREAPP3_1
    public override TService Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => JsonSerializer.Deserialize<TImplementation>(ref reader, options);
#else
    public override TService? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => JsonSerializer.Deserialize<TImplementation>(ref reader, options);
#endif

    /// <summary>写入</summary>
    /// <param name="writer"></param>
    /// <param name="value"></param>
    /// <param name="options"></param>
    public override void Write(Utf8JsonWriter writer, TService value, JsonSerializerOptions options) => JsonSerializer.Serialize(writer, value, options);
}
#endif