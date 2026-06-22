#if NET5_0_OR_GREATER
using System.Text.Json;
using System.Text.Json.Serialization;
using NewLife.Collections;

namespace NewLife.Serialization;

/// <summary>IDictionarySource 接口的 System.Text.Json 序列化转换器</summary>
/// <remarks>
/// 实现 <see cref="IDictionarySource"/> 的对象在序列化时，调用 <see cref="IDictionarySource.ToDictionary"/>
/// 获取字典后写入，而非遍历对象公共属性。与 FastJson 中 <c>WriteObject</c> 对 IDictionarySource 的检测行为一致。
/// 反序列化不支持，因为 IDictionarySource 是单向（对象→字典）接口。
/// </remarks>
public class IDictionarySourceConverter : JsonConverter<Object>
{
    /// <summary>是否可转换。目标类型实现 IDictionarySource，且原值不是已为字典的类型</summary>
    /// <param name="typeToConvert"></param>
    /// <returns></returns>
    public override Boolean CanConvert(Type typeToConvert)
    {
        if (!typeof(IDictionarySource).IsAssignableFrom(typeToConvert)) return false;

        // 已是字典类型的不需要此转换器，避免双重序列化
        if (typeToConvert.IsGenericType)
        {
            var genericDef = typeToConvert.GetGenericTypeDefinition();
            if (genericDef == typeof(IDictionary<,>) || genericDef == typeof(Dictionary<,>))
                return false;
        }

        return true;
    }

    /// <summary>读取。不支持，IDictionarySource 是单向接口</summary>
    /// <param name="reader"></param>
    /// <param name="typeToConvert"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public override Object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => throw new NotSupportedException($"IDictionarySource 接口不支持反序列化。目标类型：{typeToConvert.FullName}");

    /// <summary>写入。调用 ToDictionary() 获取字典后序列化</summary>
    /// <param name="writer"></param>
    /// <param name="value"></param>
    /// <param name="options"></param>
    public override void Write(Utf8JsonWriter writer, Object value, JsonSerializerOptions options)
    {
        if (value is IDictionarySource source)
        {
            var dic = source.ToDictionary();
            JsonSerializer.Serialize(writer, dic, typeof(IDictionary<String, Object?>), options);
        }
        else
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }
}
#endif
