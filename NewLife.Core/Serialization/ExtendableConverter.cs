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
/// 序列化时遵循 <see cref="JsonSerializerOptions.DefaultIgnoreCondition"/> 及属性级 <see cref="JsonIgnoreAttribute"/> 条件，
/// 正确跳过默认值或空值属性。
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

        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        var jsonRoot = jsonDoc.RootElement;
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

        var ignoreCondition = options.DefaultIgnoreCondition;
        var propMap = SerialHelper.GetJsonPropertyMap(value.GetType(), options);
        foreach (var (jsonName, prop) in propMap)
        {
            if (!prop.CanRead) continue;

            var propValue = prop.GetValue(value);

            // 属性级 [JsonIgnore(Condition=...)] 优先于全局 DefaultIgnoreCondition
            var attrCondition = prop.GetCustomAttribute<JsonIgnoreAttribute>()?.Condition;
            var condition = attrCondition is JsonIgnoreCondition.WhenWritingNull or JsonIgnoreCondition.WhenWritingDefault
                ? attrCondition.Value
                : ignoreCondition;

            if (condition == JsonIgnoreCondition.WhenWritingNull && propValue is null) continue;
            if (condition == JsonIgnoreCondition.WhenWritingDefault && IsDefaultValue(propValue, prop.PropertyType)) continue;

            writer.WritePropertyName(jsonName);
            JsonSerializer.Serialize(writer, propValue, prop.PropertyType, options);
        }

        var items = extendable.Items;
        if (items != null)
        {
            foreach (var kvp in items)
            {
                // 扩展字段也遵循全局忽略条件，Object 类型的默认值为 null
                if (ignoreCondition is JsonIgnoreCondition.WhenWritingNull or JsonIgnoreCondition.WhenWritingDefault && kvp.Value is null) continue;

                writer.WritePropertyName(kvp.Key);
                JsonSerializer.Serialize(writer, kvp.Value, typeof(Object), options);
            }
        }

        writer.WriteEndObject();
    }

    /// <summary>判断值是否为类型的默认值</summary>
    /// <param name="value">属性值</param>
    /// <param name="type">属性声明类型</param>
    /// <returns></returns>
    private static Boolean IsDefaultValue(Object? value, Type type)
    {
        if (value is null) return true;
        if (!type.IsValueType) return false;

        return value.Equals(Activator.CreateInstance(type));
    }
}
#endif