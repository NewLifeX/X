#if NET7_0_OR_GREATER
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Xml.Serialization;

namespace NewLife.Serialization;

/// <summary>数据成员解析器。让System.Text.Json增加对DataMemberAttribute和IgnoreDataMemberAttribute的支持</summary>
public class DataMemberResolver : DefaultJsonTypeInfoResolver
{
    /// <summary>默认解析器实例</summary>
    public static DataMemberResolver Default { get; } = new DataMemberResolver();

    /// <summary>获取类型信息</summary>
    /// <param name="type"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        var typeInfo = base.GetTypeInfo(type, options);

        if (typeInfo.Kind == JsonTypeInfoKind.Object && !type.IsArray)
        {
            Modifier(typeInfo);
        }

        return typeInfo;
    }

    /// <summary>检测并修改成员信息</summary>
    /// <param name="typeInfo"></param>
    public static void Modifier(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object) return;

        foreach (var propertyInfo in typeInfo.Properties)
        {
            var provider = propertyInfo.AttributeProvider;
            if (provider.IsDefined(typeof(IgnoreDataMemberAttribute), true) ||
                provider.IsDefined(typeof(XmlIgnoreAttribute), false))
            {
                // 禁用
                propertyInfo.Get = null;
                propertyInfo.Set = null;
            }
            else
            {
                var attr = provider.GetCustomAttributes(typeof(DataMemberAttribute), false)?.FirstOrDefault() as DataMemberAttribute;
                if (attr != null && !attr.Name.IsNullOrEmpty())
                    propertyInfo.Name = attr.Name;
            }
        }
    }
}
#endif