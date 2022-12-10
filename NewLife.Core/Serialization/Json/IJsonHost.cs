using NewLife.Collections;
using NewLife.Reflection;
#if NET5_0_OR_GREATER
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
#endif

namespace NewLife.Serialization;

/// <summary>Json序列化接口</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/json
/// </remarks>
public interface IJsonHost
{
    /// <summary>写入对象，得到Json字符串</summary>
    /// <param name="value"></param>
    /// <param name="indented">是否缩进。默认false</param>
    /// <param name="nullValue">是否写空值。默认true</param>
    /// <param name="camelCase">是否驼峰命名。默认false</param>
    /// <returns></returns>
    String Write(Object value, Boolean indented = false, Boolean nullValue = true, Boolean camelCase = false);

    /// <summary>从Json字符串中读取对象</summary>
    /// <param name="json"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    Object Read(String json, Type type);

    /// <summary>类型转换</summary>
    /// <param name="obj"></param>
    /// <param name="targetType"></param>
    /// <returns></returns>
    Object Convert(Object obj, Type targetType);

    /// <summary>分析Json字符串得到字典</summary>
    /// <param name="json"></param>
    /// <returns></returns>
    IDictionary<String, Object> Decode(String json);
}

/// <summary>Json助手</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/json
/// </remarks>
public static class JsonHelper
{
    /// <summary>默认实现</summary>
    public static IJsonHost Default { get; set; } = new FastJson();

    /// <summary>写入对象，得到Json字符串</summary>
    /// <param name="value"></param>
    /// <param name="indented">是否缩进</param>
    /// <returns></returns>
    public static String ToJson(this Object value, Boolean indented = false) => Default.Write(value, indented);

    /// <summary>写入对象，得到Json字符串</summary>
    /// <param name="value"></param>
    /// <param name="indented">是否换行缩进。默认false</param>
    /// <param name="nullValue">是否写空值。默认true</param>
    /// <param name="camelCase">是否驼峰命名。默认false</param>
    /// <returns></returns>
    public static String ToJson(this Object value, Boolean indented, Boolean nullValue, Boolean camelCase) => Default.Write(value, indented, nullValue, camelCase);

    /// <summary>从Json字符串中读取对象</summary>
    /// <param name="json"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public static Object ToJsonEntity(this String json, Type type)
    {
        if (json.IsNullOrEmpty()) return null;

        return Default.Read(json, type);
    }

    /// <summary>从Json字符串中读取对象</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="json"></param>
    /// <returns></returns>
    public static T ToJsonEntity<T>(this String json)
    {
        if (json.IsNullOrEmpty()) return default;

        return (T)Default.Read(json, typeof(T));
    }

    /// <summary>格式化Json文本</summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public static String Format(String json)
    {
        var sb = Pool.StringBuilder.Get();

        var escaping = false;
        var inQuotes = false;
        var indentation = 0;

        foreach (var ch in json)
        {
            if (escaping)
            {
                escaping = false;
                sb.Append(ch);
            }
            else
            {
                if (ch == '\\')
                {
                    escaping = true;
                    sb.Append(ch);
                }
                else if (ch == '\"')
                {
                    inQuotes = !inQuotes;
                    sb.Append(ch);
                }
                else if (!inQuotes)
                {
                    if (ch == ',')
                    {
                        sb.Append(ch);
                        sb.Append("\r\n");
                        sb.Append(' ', indentation * 2);
                    }
                    else if (ch is '[' or '{')
                    {
                        sb.Append(ch);
                        sb.Append("\r\n");
                        sb.Append(' ', ++indentation * 2);
                    }
                    else if (ch is ']' or '}')
                    {
                        sb.Append("\r\n");
                        sb.Append(' ', --indentation * 2);
                        sb.Append(ch);
                    }
                    else if (ch == ':')
                    {
                        sb.Append(ch);
                        sb.Append(' ', 2);
                    }
                    else
                    {
                        sb.Append(ch);
                    }
                }
                else
                {
                    sb.Append(ch);
                }
            }
        }

        return sb.Put(true);
    }

    /// <summary>Json类型对象转换实体类</summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T Convert<T>(Object obj)
    {
        if (obj == null) return default;
        if (obj is T t) return t;
        if (obj.GetType().As<T>()) return (T)obj;

        return (T)Default.Convert(obj, typeof(T));
    }

    /// <summary>分析Json字符串得到字典</summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public static IDictionary<String, Object> DecodeJson(this String json) => Default.Decode(json);
}

/// <summary>轻量级FastJson序列化</summary>
public class FastJson : IJsonHost
{
    #region IJsonHost 成员
    /// <summary>写入对象，得到Json字符串</summary>
    /// <param name="value"></param>
    /// <param name="indented">是否缩进。默认false</param>
    /// <param name="nullValue">是否写空值。默认true</param>
    /// <param name="camelCase">是否驼峰命名。默认false</param>
    /// <returns></returns>
    public String Write(Object value, Boolean indented = false, Boolean nullValue = true, Boolean camelCase = false) => JsonWriter.ToJson(value, indented, nullValue, camelCase);

    /// <summary>从Json字符串中读取对象</summary>
    /// <param name="json"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public Object Read(String json, Type type) => new JsonReader().Read(json, type);

    /// <summary>类型转换</summary>
    /// <param name="obj"></param>
    /// <param name="targetType"></param>
    /// <returns></returns>
    public Object Convert(Object obj, Type targetType) => new JsonReader().ToObject(obj, targetType, null);

    /// <summary>分析Json字符串得到字典</summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public IDictionary<String, Object> Decode(String json) => JsonParser.Decode(json);
    #endregion
}

#if NET5_0_OR_GREATER
/// <summary>系统级System.Text.Json标准序列化</summary>
public class SystemJson : IJsonHost
{
    #region 静态
    /// <summary>获取序列化配置项</summary>
    /// <returns></returns>
    public static JsonSerializerOptions GetDefaultOptions()
    {
        var opt = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        };
        opt.Converters.Add(new LocalTimeConverter());
        opt.Converters.Add(new TypeConverter());
#if NET7_0_OR_GREATER
        opt.TypeInfoResolver = DataMemberResolver.Default;
        //opt.TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { DataMemberResolver.Modifier } };
#endif
        return opt;
    }
    #endregion

    #region 属性
    /// <summary>配置项</summary>
    public JsonSerializerOptions Options { get; set; } = GetDefaultOptions();
    #endregion

    #region IJsonHost 成员
    /// <summary>写入对象，得到Json字符串</summary>
    /// <param name="value"></param>
    /// <param name="indented">是否缩进。默认false</param>
    /// <param name="nullValue">是否写空值。默认true</param>
    /// <param name="camelCase">是否驼峰命名。默认false</param>
    /// <returns></returns>
    public String Write(Object value, Boolean indented = false, Boolean nullValue = true, Boolean camelCase = false)
    {
        var opt = new JsonSerializerOptions(Options)
        {
            WriteIndented = indented
        };
        if (!nullValue) opt.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
        if (camelCase) opt.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

        return JsonSerializer.Serialize(value, opt);
    }

    /// <summary>从Json字符串中读取对象</summary>
    /// <param name="json"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public Object Read(String json, Type type)
    {
        var opt = Options;
#if NET7_0_OR_GREATER
        //opt.TypeInfoResolver = new DataMemberResolver { Modifiers = { OnModifierType } };
#endif

        return JsonSerializer.Deserialize(json, type, opt);
    }

#if NET7_0_OR_GREATER
    //static void OnModifierType(JsonTypeInfo typeInfo)
    //{
    //    if (typeInfo.Kind != JsonTypeInfoKind.Object) return;

    //    var type = typeInfo.Type;
    //    if (type.IsInterface || type.IsAbstract)
    //    {
    //        var t = ObjectContainer.Current.Resolve(type);
    //    }
    //}
#endif

    /// <summary>类型转换</summary>
    /// <param name="obj"></param>
    /// <param name="targetType"></param>
    /// <returns></returns>
    public Object Convert(Object obj, Type targetType) => new JsonReader().ToObject(obj, targetType, null);

    /// <summary>分析Json字符串得到字典</summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public IDictionary<String, Object> Decode(String json)
    {
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.ToDictionary();
    }
    #endregion
}
#endif
