using NewLife.Collections;
using NewLife.Model;
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
    /// <summary>服务提供者。用于反序列化时构造内部成员对象</summary>
    IServiceProvider ServiceProvider { get; set; }

    /// <summary>配置项</summary>
    JsonOptions Options { get; set; }

    /// <summary>写入对象，得到Json字符串</summary>
    /// <param name="value"></param>
    /// <param name="indented">是否缩进。默认false</param>
    /// <param name="nullValue">是否写空值。默认true</param>
    /// <param name="camelCase">是否驼峰命名。默认false</param>
    /// <returns></returns>
    String Write(Object value, Boolean indented = false, Boolean nullValue = true, Boolean camelCase = false);

    /// <summary>写入对象，得到Json字符串</summary>
    /// <param name="value"></param>
    /// <param name="jsonOptions">序列化选项</param>
    /// <returns></returns>
    String Write(Object value, JsonOptions jsonOptions);

    /// <summary>从Json字符串中读取对象</summary>
    /// <param name="json"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    Object? Read(String json, Type type);

    /// <summary>类型转换</summary>
    /// <param name="obj"></param>
    /// <param name="targetType"></param>
    /// <returns></returns>
    Object? Convert(Object obj, Type targetType);

    /// <summary>分析Json字符串得到字典或列表</summary>
    /// <param name="json"></param>
    /// <returns></returns>
    Object? Parse(String json);

    /// <summary>分析Json字符串得到字典</summary>
    /// <param name="json"></param>
    /// <returns></returns>
    IDictionary<String, Object?>? Decode(String json);
}

/// <summary>Json助手</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/json
/// </remarks>
public static class JsonHelper
{
    //#if NET7_0_OR_GREATER
    //    /// <summary>默认实现</summary>
    //    public static IJsonHost Default { get; set; } = new SystemJson();
    //#else
    /// <summary>默认实现</summary>
    public static IJsonHost Default { get; set; } = new FastJson();
    //#endif

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

    /// <summary>写入对象，得到Json字符串</summary>
    /// <param name="value"></param>
    /// <param name="jsonOptions">序列化选项</param>
    /// <returns></returns>
    public static String ToJson(this Object value, JsonOptions jsonOptions) => Default.Write(value, jsonOptions);

    /// <summary>从Json字符串中读取对象</summary>
    /// <param name="json"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public static Object? ToJsonEntity(this String json, Type type)
    {
        if (json.IsNullOrEmpty()) return null;

        return Default.Read(json, type);
    }

    /// <summary>从Json字符串中读取对象</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="json"></param>
    /// <returns></returns>
    public static T? ToJsonEntity<T>(this String json)
    {
        if (json.IsNullOrEmpty()) return default;

        return (T?)Default.Read(json, typeof(T));
    }

    /// <summary>从Json字符串中反序列化对象</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="jsonHost"></param>
    /// <param name="json"></param>
    /// <returns></returns>
    public static T? Read<T>(this IJsonHost jsonHost, String json) => (T?)jsonHost.Read(json, typeof(T));

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

        return sb.Return(true);
    }

    /// <summary>Json类型对象转换实体类</summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T? Convert<T>(Object obj) => Default.Convert<T>(obj);

    /// <summary>Json类型对象转换实体类</summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T? Convert<T>(this IJsonHost jsonHost, Object obj)
    {
        if (obj == null) return default;
        if (obj is T t) return t;
        if (obj.GetType().As<T>()) return (T)obj;

        return (T?)jsonHost.Convert(obj, typeof(T));
    }

    /// <summary>分析Json字符串得到字典或列表</summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public static Object? Parse(String json) => Default.Parse(json);

    /// <summary>分析Json字符串得到字典</summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public static IDictionary<String, Object?>? DecodeJson(this String json) => Default.Decode(json);
}

/// <summary>轻量级FastJson序列化</summary>
public class FastJson : IJsonHost
{
    /// <summary>服务提供者。用于反序列化时构造内部成员对象</summary>
    public IServiceProvider ServiceProvider { get; set; } = ObjectContainer.Provider;

    /// <summary>配置项</summary>
    public JsonOptions Options { get; set; } = new JsonOptions { CamelCase = false, IgnoreNullValues = false, WriteIndented = false };

    #region IJsonHost 成员
    /// <summary>写入对象，得到Json字符串</summary>
    /// <param name="value"></param>
    /// <param name="indented">是否缩进。默认false</param>
    /// <param name="nullValue">是否写空值。默认true</param>
    /// <param name="camelCase">是否驼峰命名。默认false</param>
    /// <returns></returns>
    public String Write(Object value, Boolean indented = false, Boolean nullValue = true, Boolean camelCase = false) => JsonWriter.ToJson(value, indented, nullValue, camelCase);

    /// <summary>写入对象，得到Json字符串</summary>
    /// <param name="value"></param>
    /// <param name="jsonOptions">序列化选项</param>
    /// <returns></returns>
    public String Write(Object value, JsonOptions jsonOptions) => JsonWriter.ToJson(value, jsonOptions ?? Options);

    /// <summary>从Json字符串中读取对象</summary>
    /// <param name="json"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public Object? Read(String json, Type type) => new JsonReader { Provider = ServiceProvider }.Read(json, type);

    /// <summary>类型转换</summary>
    /// <param name="obj"></param>
    /// <param name="targetType"></param>
    /// <returns></returns>
    public Object? Convert(Object obj, Type targetType) => new JsonReader { Provider = ServiceProvider }.ToObject(obj, targetType, null);

    /// <summary>分析Json字符串得到字典或列表</summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public Object? Parse(String json) => new JsonParser(json).Decode();

    /// <summary>分析Json字符串得到字典</summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public IDictionary<String, Object?>? Decode(String json) => JsonParser.Decode(json);
    #endregion
}

#if NET5_0_OR_GREATER
/// <summary>系统级System.Text.Json标准序列化</summary>
public class SystemJson : IJsonHost
{
    /// <summary>服务提供者。用于反序列化时构造内部成员对象</summary>
    public IServiceProvider ServiceProvider { get; set; } = ObjectContainer.Provider;

    /// <summary>配置项</summary>
    public JsonOptions Options { get; set; } = new JsonOptions { CamelCase = false, IgnoreNullValues = false, WriteIndented = false };

    #region 静态
    /// <summary>获取序列化配置项</summary>
    /// <returns></returns>
    public static JsonSerializerOptions GetDefaultOptions()
    {
        var opt = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            PropertyNamingPolicy = new MyJsonNamingPolicy(),
        };
        opt.Converters.Add(new LocalTimeConverter());
        opt.Converters.Add(new TypeConverter());
#if NET6_0_OR_GREATER
        opt.Converters.Add(new ExtendableConverter());
#endif
#if NET7_0_OR_GREATER
        opt.TypeInfoResolver = DataMemberResolver.Default;
        //opt.TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { DataMemberResolver.Modifier } };
#endif
        return opt;
    }
    #endregion

    #region 属性
    /// <summary>配置项</summary>
    public JsonSerializerOptions SerializerOptions { get; set; }
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    public SystemJson()
    {
        var opt = GetDefaultOptions();
#if NET7_0_OR_GREATER
        opt.TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver
        {
            Modifiers = {
                DataMemberResolver.Modifier,
                new ServiceTypeResolver{ GetServiceProvider = () => ServiceProvider }.Modifier,
            }
        };
#endif
        SerializerOptions = opt;
    }
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
        var opt = new JsonSerializerOptions(SerializerOptions)
        {
            WriteIndented = indented
        };
        if (!nullValue) opt.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
        if (camelCase) opt.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

        return JsonSerializer.Serialize(value, opt);
    }

    /// <summary>写入对象，得到Json字符串</summary>
    /// <param name="value"></param>
    /// <param name="jsonOptions">序列化选项</param>
    /// <returns></returns>
    public String Write(Object value, JsonOptions jsonOptions)
    {
        jsonOptions ??= Options;
        var opt = new JsonSerializerOptions(SerializerOptions)
        {
            WriteIndented = jsonOptions.WriteIndented,
        };
        if (jsonOptions.IgnoreNullValues)
            opt.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
        if (jsonOptions.CamelCase)
            opt.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        if (jsonOptions.EnumString)
            opt.Converters.Add(new JsonStringEnumConverter(null, true));
#if NET6_0_OR_GREATER
        if (jsonOptions.IgnoreCycles)
            opt.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        if (jsonOptions.Int64AsString)
        {
            opt.Converters.Add(new SafeInt64Converter());
            opt.Converters.Add(new SafeUInt64Converter());
        }
#endif

        return JsonSerializer.Serialize(value, opt);
    }

    /// <summary>从Json字符串中读取对象</summary>
    /// <param name="json"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public Object? Read(String json, Type type)
    {
        var opt = SerializerOptions;
#if NET7_0_OR_GREATER
        //opt.TypeInfoResolver = new DataMemberResolver { Modifiers = { OnModifierType } };
#endif

        return JsonSerializer.Deserialize(json, type, opt);
    }

    /// <summary>从Json字符串中读取对象</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="json"></param>
    /// <returns></returns>
    public T? Read<T>(String json) where T : class => Read(json, typeof(T)) as T;

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
    public Object? Convert(Object obj, Type targetType) => new JsonReader { Provider = ServiceProvider }.ToObject(obj, targetType, null);

    /// <summary>分析Json字符串得到字典或列表</summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public Object? Parse(String json)
    {
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.ToArray();
    }

    /// <summary>分析Json字符串得到字典</summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public IDictionary<String, Object?> Decode(String json)
    {
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.ToDictionary();
    }
    #endregion

    #region 辅助
    class MyJsonNamingPolicy : JsonNamingPolicy
    {
        public override String ConvertName(String name) => name;
    }
    #endregion
}
#endif
