#if NET6_0_OR_GREATER
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using NewLife.Data;
using NewLife.Reflection;

namespace NewLife.Serialization;

/// <summary>支持IExtend的可扩展序列化</summary>
public class ExtendableConverter : JsonConverter<Object>
{
    /// <summary>是否可以转换</summary>
    /// <param name="typeToConvert"></param>
    /// <returns></returns>
    public override Boolean CanConvert(Type typeToConvert) => typeof(IExtend).IsAssignableFrom(typeToConvert);

    /// <summary>读取</summary>
    /// <param name="reader"></param>
    /// <param name="typeToConvert"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public override Object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!typeof(IExtend).IsAssignableFrom(typeToConvert))
            return JsonSerializer.Deserialize(ref reader, typeToConvert, options)!;

        // 解析 JSON 对象
        var jsonObject = JsonDocument.ParseValue(ref reader).RootElement;

        // 创建目标对象
        var obj = Activator.CreateInstance(typeToConvert);
        if (obj is IExtend extendable)
        {
            // 使用反射设置对象的属性
            var properties = typeToConvert.GetProperties(true);
            foreach (var property in properties)
            {
                if (property.CanWrite)
                {
                    if (jsonObject.TryGetProperty(property.Name, out var jsonProperty))
                    {
                        var propertyValue = JsonSerializer.Deserialize(jsonProperty.GetRawText(), property.PropertyType, options);
                        property.SetValue(obj, propertyValue);
                    }
                }
            }

            // 将未知属性添加到 Items 字典中
            foreach (var jsonProperty in jsonObject.EnumerateObject())
            {
                if (!properties.Any(e => e.Name == jsonProperty.Name))
                {
                    var propertyValue = JsonSerializer.Deserialize(jsonProperty.Value.GetRawText(), typeof(Object), options);
                    extendable.Items[jsonProperty.Name] = propertyValue;
                }
            }
        }

        return obj!;
    }

    /// <summary>写入</summary>
    /// <param name="writer"></param>
    /// <param name="value"></param>
    /// <param name="options"></param>
    public override void Write(Utf8JsonWriter writer, Object value, JsonSerializerOptions options)
    {
        if (value is IExtend extendable)
        {
            // 创建一个 JsonObject 并添加原始对象的成员
            var jsonObject = new JsonObject();

            // 使用反射获取对象的所有属性并添加到 JsonObject
            foreach (var property in value.GetType().GetProperties(true))
            {
                if (property.CanRead)
                {
                    var propertyValue = property.GetValue(value);
                    jsonObject[property.Name] = JsonValue.Create(propertyValue);
                }
            }

            // 动态添加 Items 字典中的成员
            foreach (var kvp in extendable.Items)
            {
                jsonObject[kvp.Key] = JsonValue.Create(kvp.Value);
            }

            // 将 JsonObject 写入 Utf8JsonWriter
            JsonSerializer.Serialize(writer, jsonObject, options);
        }
        else
        {
            // 如果对象不实现 IExtend 接口，使用默认序列化
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }
}
#endif