#if NET6_0_OR_GREATER
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using NewLife.Data;
using NewLife.Reflection;

namespace NewLife.Serialization;

/// <summary>支持IExtend接口的可扩展对象序列化转换器</summary>
/// <remarks>
/// 序列化时先输出类型的公共属性，再将 <see cref="IExtend.Items"/> 中的扩展字段追加到同一 JSON 对象层级。
/// 反序列化时按属性名匹配 JSON 字段并赋值给对应成员，无法匹配的多余字段写入 <see cref="IExtend.Items"/> 字典。
/// 属性名匹配遵循 <see cref="JsonSerializerOptions.PropertyNamingPolicy"/> 与 <see cref="JsonSerializerOptions.PropertyNameCaseInsensitive"/> 配置。
/// </remarks>
public class ExtendableConverter : JsonConverter<Object>
{
    /// <summary>是否可以转换</summary>
    /// <param name="typeToConvert"></param>
    /// <returns></returns>
    public override Boolean CanConvert(Type typeToConvert) => typeof(IExtend).IsAssignableFrom(typeToConvert);

    /// <summary>读取。已知属性按名称映射到成员，多余字段写入 IExtend.Items</summary>
    /// <param name="reader"></param>
    /// <param name="typeToConvert"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public override Object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!typeof(IExtend).IsAssignableFrom(typeToConvert))
            return JsonSerializer.Deserialize(ref reader, typeToConvert, options)!;

        var jsonRoot = JsonDocument.ParseValue(ref reader).RootElement;
        var obj = Activator.CreateInstance(typeToConvert);
        if (obj is IExtend extendable)
        {
            var propMap = SerialHelper.GetJsonPropertyMap(typeToConvert, options);
            foreach (var jsonProp in jsonRoot.EnumerateObject())
            {
                if (propMap.TryGetValue(jsonProp.Name, out var prop) && prop.CanWrite)
                    prop.SetValue(obj, jsonProp.Value.Deserialize(prop.PropertyType, options));
                else
                    extendable.Items[jsonProp.Name] = jsonProp.Value.Deserialize<Object>(options);
            }
        }

        return obj!;
    }

    /// <summary>写入。先输出类型公共属性，再展开 IExtend.Items 扩展字段</summary>
    /// <param name="writer"></param>
    /// <param name="value"></param>
    /// <param name="options"></param>
    public override void Write(Utf8JsonWriter writer, Object value, JsonSerializerOptions options)
    {
        if (value is not IExtend extendable)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
            return;
        }

        writer.WriteStartObject();

        var propMap = SerialHelper.GetJsonPropertyMap(value.GetType(), options);
        foreach (var (jsonName, prop) in propMap)
        {
            if (!prop.CanRead) continue;
            writer.WritePropertyName(jsonName);
            JsonSerializer.Serialize(writer, prop.GetValue(value), prop.PropertyType, options);
        }

        foreach (var kvp in extendable.Items)
        {
            writer.WritePropertyName(kvp.Key);
            JsonSerializer.Serialize(writer, kvp.Value, typeof(Object), options);
        }

        writer.WriteEndObject();
    }
}
#endif