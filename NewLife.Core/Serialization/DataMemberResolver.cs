#if NET7_0_OR_GREATER
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
        var jsonTypeInfo = base.GetTypeInfo(type, options);

        if (jsonTypeInfo.Kind == JsonTypeInfoKind.Object && !type.IsArray)
        {
            var pis = jsonTypeInfo.Properties;
            for (var i = pis.Count - 1; i >= 0; i--)
            {
                var jpi = pis[i];
                var provider = jpi.AttributeProvider;
                if (provider.IsDefined(typeof(IgnoreDataMemberAttribute), false) ||
                    provider.IsDefined(typeof(XmlIgnoreAttribute), false))
                {
                    pis.RemoveAt(i);
                    continue;
                }
                else
                {
                    var attr = provider.GetCustomAttributes(typeof(DataMemberAttribute), false)?.FirstOrDefault() as DataMemberAttribute;
                    if (attr != null && !attr.Name.IsNullOrEmpty())
                        jpi.Name = attr.Name;
                }
            }
        }

        return jsonTypeInfo;
    }
}
#endif