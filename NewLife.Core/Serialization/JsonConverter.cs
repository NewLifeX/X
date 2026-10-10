#if NETCOREAPP
using System.Diagnostics.CodeAnalysis;
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
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "TImplementation 为编译期已知实现类型，其元数据由调用方泛型参数保留或 SystemJson 上下文提供")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "TImplementation 为编译期已知实现类型，其元数据由调用方泛型参数保留或 SystemJson 上下文提供")]
#if NETCOREAPP3_1
    public override TService Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => JsonSerializer.Deserialize<TImplementation>(ref reader, options);
#else
    public override TService? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => JsonSerializer.Deserialize<TImplementation>(ref reader, options);
#endif

    /// <summary>写入</summary>
    /// <param name="writer"></param>
    /// <param name="value"></param>
    /// <param name="options"></param>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "TService 为编译期已知类型，其元数据由调用方泛型参数保留或 SystemJson 上下文提供")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "TService 为编译期已知类型，其元数据由调用方泛型参数保留或 SystemJson 上下文提供")]
    public override void Write(Utf8JsonWriter writer, TService value, JsonSerializerOptions options) => JsonSerializer.Serialize(writer, value, options);
}
#endif