using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Serialization;
#if NETCOREAPP || NET5_0_OR_GREATER
using System.Text.Json;
using System.Text.Json.Serialization;
#endif
using NewLife.Data;
using NewLife.Reflection;

namespace NewLife.Serialization;

/// <summary>序列化助手</summary>
public static class SerialHelper
{
    private static readonly ConcurrentDictionary<PropertyInfo, String> _cache = new();
    /// <summary>获取序列化名称</summary>
    /// <param name="pi"></param>
    /// <returns></returns>
    public static String GetName(PropertyInfo pi)
    {
        if (_cache.TryGetValue(pi, out var name)) return name;

#if NET6_0_OR_GREATER
        if (name.IsNullOrEmpty())
        {
            var att = pi.GetCustomAttribute<JsonPropertyNameAttribute>();
            if (att != null && !att.Name.IsNullOrEmpty()) name = att.Name;
        }
#endif
        if (name.IsNullOrEmpty())
        {
            var att = pi.GetCustomAttribute<DataMemberAttribute>();
            if (att != null && !att.Name.IsNullOrEmpty()) name = att.Name;
        }
        if (name.IsNullOrEmpty())
        {
            var att = pi.GetCustomAttribute<XmlElementAttribute>();
            if (att != null && !att.ElementName.IsNullOrEmpty()) name = att.ElementName;
        }
        if (name.IsNullOrEmpty()) name = pi.Name;

        _cache.TryAdd(pi, name);

        return name;
    }

    /// <summary>依据 Json/Xml 字典生成实体模型类</summary>
    /// <param name="dic"></param>
    /// <param name="className"></param>
    /// <returns></returns>
    public static String? BuildModelClass(this IDictionary<String, Object?> dic, String className = "Model")
    {
        if (dic == null || dic.Count == 0) return null;

        var sb = new StringBuilder();

        BuildModel(sb, dic, className, null);

        return sb.ToString();
    }

    private static void BuildModel(StringBuilder sb, IDictionary<String, Object?> dic, String className, String? prefix)
    {
        sb.AppendLine($"{prefix}public class {className}");
        sb.AppendLine($"{prefix}{{");

        var line = 0;
        foreach (var item in dic)
        {
            var name = item.Key;
            if (Char.IsLower(name[0])) name = Char.ToUpper(name[0]) + name[1..];

            if (line++ > 0) sb.AppendLine();

            var type = item.Value?.GetType() ?? typeof(Object);
            if (type.IsBaseType())
                sb.AppendLine($"{prefix}\tpublic {type.Name} {name} {{ get; set; }}");
            else if (item.Value is IDictionary<String, Object?> sub)
            {
                var subclassName = name + "Model";
                sb.AppendLine($"{prefix}\tpublic {subclassName} {name} {{ get; set; }}");
                sb.AppendLine();

                BuildModel(sb, sub, subclassName, prefix + "\t");
            }
            else if (item.Value is IList<Object> list)
            {
                var elmType = list.Count > 0 ? list[0].GetType() : type.GetElementTypeEx();
                sb.AppendLine($"{prefix}\tpublic {elmType?.Name}[] {name} {{ get; set; }}");
            }
        }

        sb.AppendLine($"{prefix}}}");
    }

#if NETCOREAPP
    /// <summary>获取类型的 JSON 属性名到 PropertyInfo 的映射字典，用于序列化/反序列化时 O(1) 查找</summary>
    /// <param name="type">目标类型</param>
    /// <param name="options">序列化选项，用于获取命名策略和大小写配置</param>
    /// <returns>属性映射字典</returns>
    public static Dictionary<String, PropertyInfo> GetJsonPropertyMap(Type type, JsonSerializerOptions? options = null)
    {
        var namingPolicy = options?.PropertyNamingPolicy;
        var comparer = options != null && options.PropertyNameCaseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var map = new Dictionary<String, PropertyInfo>(comparer);

        foreach (var prop in type.GetProperties(true))
        {
            if (prop.GetIndexParameters().Length > 0) continue;
            if (prop.Name == nameof(IExtend.Items)) continue;
            if (prop.GetCustomAttribute<IgnoreDataMemberAttribute>() != null) continue;
            if (prop.GetCustomAttribute<XmlIgnoreAttribute>() != null) continue;
#if NET6_0_OR_GREATER
            if (prop.GetCustomAttribute<JsonIgnoreAttribute>() is { Condition: JsonIgnoreCondition.Never or JsonIgnoreCondition.Always }) continue;
#else
            if (prop.GetCustomAttribute<JsonIgnoreAttribute>() != null) continue;
#endif

            var attrName = GetName(prop);
            // 有特性名则直接使用，否则应用命名策略
            var jsonName = attrName != prop.Name ? attrName : namingPolicy?.ConvertName(prop.Name) ?? prop.Name;
            map.TryAdd(jsonName, prop);
        }

        return map;
    }
#else
    /// <summary>获取类型的 JSON 属性名到 PropertyInfo 的映射字典，用于序列化/反序列化时 O(1) 查找</summary>
    /// <param name="type">目标类型</param>
    /// <returns>属性映射字典</returns>
    public static Dictionary<String, PropertyInfo> GetJsonPropertyMap(Type type)
    {
        var map = new Dictionary<String, PropertyInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var prop in type.GetProperties(true))
        {
            if (prop.GetIndexParameters().Length > 0) continue;
            if (prop.Name == nameof(IExtend.Items)) continue;
            if (prop.GetCustomAttribute<IgnoreDataMemberAttribute>() != null) continue;
            if (prop.GetCustomAttribute<XmlIgnoreAttribute>() != null) continue;

            var attrName = GetName(prop);
            map[attrName] = prop;
        }

        return map;
    }
#endif
}