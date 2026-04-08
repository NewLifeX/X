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

    /// <summary>默认序列化选项。其他方法未显式传入 <see cref="JsonOptions"/> 时以此为基准</summary>
    JsonOptions Options { get; set; }

    /// <summary>写入对象，得到Json字符串。以 <see cref="Options"/> 为基准，再叠加指定参数</summary>
    /// <param name="value">待序列化的对象</param>
    /// <param name="indented">是否缩进。为 true 时强制开启缩进。默认false</param>
    /// <param name="nullValue">是否写空值。为 false 时强制忽略空值。默认true</param>
    /// <param name="camelCase">是否驼峰命名。为 true 时强制驼峰命名。默认false</param>
    /// <returns>Json字符串</returns>
    String Write(Object value, Boolean indented = false, Boolean nullValue = true, Boolean camelCase = false);

    /// <summary>写入对象，得到Json字符串。未传入配置时使用 <see cref="Options"/></summary>
    /// <param name="value">待序列化的对象</param>
    /// <param name="jsonOptions">序列化选项。为 null 时使用 <see cref="Options"/></param>
    /// <returns>Json字符串</returns>
    String Write(Object value, JsonOptions? jsonOptions);

    /// <summary>从Json字符串中读取对象。等同于 Read(json, type, null)</summary>
    /// <param name="json">Json字符串</param>
    /// <param name="type">目标类型</param>
    /// <returns>反序列化后的对象</returns>
    [Obsolete("请使用 Read(json, type, jsonOptions) 方法，传入 JsonOptions 以明确使用的配置，避免未来 Options 变更导致行为不确定")]
    Object? Read(String json, Type type);

    /// <summary>从Json字符串中读取对象，使用指定序列化选项</summary>
    /// <param name="json">Json字符串</param>
    /// <param name="type">目标类型</param>
    /// <param name="jsonOptions">序列化选项。为 null 时使用 <see cref="Options"/></param>
    /// <returns>反序列化后的对象</returns>
    Object? Read(String json, Type type, JsonOptions? jsonOptions);

    /// <summary>类型转换。将 Json 解析出的字典/列表转换为目标类型</summary>
    /// <param name="obj">Json解析出的对象（字典或列表）</param>
    /// <param name="targetType">目标类型</param>
    /// <returns>转换后的对象</returns>
    Object? Convert(Object obj, Type targetType);

    /// <summary>分析Json字符串得到字典或列表</summary>
    /// <param name="json">Json字符串</param>
    /// <returns>字典或列表</returns>
    Object? Parse(String json);

    /// <summary>分析Json字符串得到字典</summary>
    /// <param name="json">Json字符串</param>
    /// <returns>Key 为属性名的字典</returns>
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
    public static String ToJson(this Object value, JsonOptions? jsonOptions) => Default.Write(value, jsonOptions);

    /// <summary>从Json字符串中读取对象</summary>
    /// <param name="json">Json字符串</param>
    /// <param name="type">目标类型</param>
    /// <returns>反序列化后的对象</returns>
    [Obsolete("请使用 ToJsonEntity(json, type, jsonOptions) 方法，传入 JsonOptions 以明确使用的配置，避免未来 Options 变更导致行为不确定")]
    public static Object? ToJsonEntity(this String json, Type type)
    {
        if (json.IsNullOrEmpty()) return null;

        return Default.Read(json, type, null);
    }

    /// <summary>从Json字符串中读取对象，使用指定序列化选项</summary>
    /// <param name="json">Json字符串</param>
    /// <param name="type">目标类型</param>
    /// <param name="jsonOptions">序列化选项。为 null 时使用默认 Options</param>
    /// <returns>反序列化后的对象</returns>
    public static Object? ToJsonEntity(this String json, Type type, JsonOptions? jsonOptions)
    {
        if (json.IsNullOrEmpty()) return null;

        return Default.Read(json, type, jsonOptions);
    }

    /// <summary>从Json字符串中读取对象</summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="json">Json字符串</param>
    /// <returns>反序列化后的对象</returns>
    public static T? ToJsonEntity<T>(this String json)
    {
        if (json.IsNullOrEmpty()) return default;

        return (T?)Default.Read(json, typeof(T), null);
    }

    /// <summary>从Json字符串中读取对象，使用指定序列化选项</summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="json">Json字符串</param>
    /// <param name="jsonOptions">序列化选项。为 null 时使用默认 Options</param>
    /// <returns>反序列化后的对象</returns>
    public static T? ToJsonEntity<T>(this String json, JsonOptions? jsonOptions)
    {
        if (json.IsNullOrEmpty()) return default;

        return (T?)Default.Read(json, typeof(T), jsonOptions);
    }

    /// <summary>从Json字符串中反序列化对象</summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="jsonHost">Json序列化实现</param>
    /// <param name="json">Json字符串</param>
    /// <returns>反序列化后的对象</returns>
    [Obsolete("请使用 Read(json, jsonOptions) 方法，传入 JsonOptions 以明确使用的配置，避免未来 Options 变更导致行为不确定")]
    public static T? Read<T>(this IJsonHost jsonHost, String json) => (T?)jsonHost.Read(json, typeof(T), null);

    /// <summary>从Json字符串中反序列化对象，使用指定序列化选项</summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="jsonHost">Json序列化实现</param>
    /// <param name="json">Json字符串</param>
    /// <param name="jsonOptions">序列化选项。为 null 时使用默认 Options</param>
    /// <returns>反序列化后的对象</returns>
    public static T? Read<T>(this IJsonHost jsonHost, String json, JsonOptions? jsonOptions = null) => (T?)jsonHost.Read(json, typeof(T), jsonOptions);

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

    /// <summary>默认序列化选项</summary>
    public JsonOptions Options { get; set; } = new JsonOptions
    {
        //CamelCase = false,
        IgnoreNullValues = false,
        WriteIndented = false
    };

    #region IJsonHost 成员
    /// <summary>写入对象，得到Json字符串。以 <see cref="Options"/> 为基准，再叠加指定参数</summary>
    /// <param name="value">待序列化的对象</param>
    /// <param name="indented">是否缩进。为 true 时强制开启缩进。默认false</param>
    /// <param name="nullValue">是否写空值。为 false 时强制忽略空值。默认true</param>
    /// <param name="camelCase">是否驼峰命名。为 true 时强制驼峰命名。默认false</param>
    /// <returns>Json字符串</returns>
    public String Write(Object value, Boolean indented = false, Boolean nullValue = true, Boolean camelCase = false)
    {
        var opt = Options;
        // 入参不会改变 Options 已有配置时，直接使用 Options，避免复制开销
        var needCopy = (indented && !opt.WriteIndented) ||
                       (!nullValue && !opt.IgnoreNullValues) ||
                       (camelCase && opt.PropertyNaming != PropertyNaming.CamelCase);
        if (!needCopy)
            return Write(value, jsonOptions: null);

        var newOpt = new JsonOptions(opt);
        if (indented) newOpt.WriteIndented = true;
        if (!nullValue) newOpt.IgnoreNullValues = true;
        if (camelCase) newOpt.PropertyNaming = PropertyNaming.CamelCase;
        return Write(value, newOpt);
    }

    /// <summary>写入对象，得到Json字符串。未传入配置时使用 <see cref="Options"/></summary>
    /// <param name="value">待序列化的对象</param>
    /// <param name="jsonOptions">序列化选项。为 null 时使用 <see cref="Options"/></param>
    /// <returns>Json字符串</returns>
    public String Write(Object value, JsonOptions? jsonOptions) => JsonWriter.ToJson(value, jsonOptions ?? Options);

    /// <summary>从Json字符串中读取对象</summary>
    /// <param name="json">Json字符串</param>
    /// <param name="type">目标类型</param>
    /// <returns>反序列化后的对象</returns>
    public Object? Read(String json, Type type) => Read(json, type, null);

    /// <summary>从Json字符串中读取对象，使用指定序列化选项</summary>
    /// <param name="json">Json字符串</param>
    /// <param name="type">目标类型</param>
    /// <param name="jsonOptions">序列化选项。为 null 时使用 <see cref="Options"/></param>
    /// <returns>反序列化后的对象</returns>
    public Object? Read(String json, Type type, JsonOptions? jsonOptions) => new JsonReader { Provider = ServiceProvider, Options = jsonOptions ?? Options }.Read(json, type);

    /// <summary>类型转换。将 Json 解析出的字典/列表转换为目标类型</summary>
    /// <param name="obj">Json解析出的对象（字典或列表）</param>
    /// <param name="targetType">目标类型</param>
    /// <returns>转换后的对象</returns>
    public Object? Convert(Object obj, Type targetType) => new JsonReader { Provider = ServiceProvider, Options = Options }.ToObject(obj, targetType, null);

    /// <summary>分析Json字符串得到字典或列表</summary>
    /// <param name="json">Json字符串</param>
    /// <returns>字典或列表</returns>
    public Object? Parse(String json) => new JsonParser(json).Decode();

    /// <summary>分析Json字符串得到字典</summary>
    /// <param name="json">Json字符串</param>
    /// <returns>Key 为属性名的字典</returns>
    public IDictionary<String, Object?>? Decode(String json) => JsonParser.Decode(json);
    #endregion
}

#if NET5_0_OR_GREATER
/// <summary>系统级System.Text.Json标准序列化</summary>
public class SystemJson : IJsonHost
{
    /// <summary>服务提供者。用于反序列化时构造内部成员对象</summary>
    public IServiceProvider ServiceProvider { get; set; } = ObjectContainer.Provider;

    private JsonOptions _options = new() { IgnoreNullValues = false, WriteIndented = false };
    /// <summary>默认序列化选项。重新赋值时自动清除缓存的 <see cref="JsonSerializerOptions"/></summary>
    public JsonOptions Options
    {
        get => _options;
        set { _options = value; _cachedSerializerOptions = null; }
    }

    #region 静态
    /// <summary>获取序列化配置项</summary>
    /// <returns></returns>
    public static JsonSerializerOptions GetDefaultOptions()
    {
        var opt = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            //Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            PropertyNamingPolicy = null,
        };
        Apply(opt, true);
        return opt;
    }

    /// <summary>将NewLife默认序列化配置应用到指定选项，常用于Web框架中统一配置</summary>
    /// <remarks>
    /// 应用以下配置：
    /// <list type="bullet">
    /// <item><term>Encoder</term><description>设为 JavaScriptEncoder(UnicodeRanges.All)，全Unicode支持，中文等非ASCII字符不被转义为 \uXXXX</description></item>
    /// <item><term>LocalTimeConverter</term><description>DateTime/DateTimeOffset 使用本地时间格式序列化</description></item>
    /// <item><term>TypeConverter</term><description>支持 System.Type 类型的序列化与反序列化</description></item>
    /// <item><term>ExtendableConverter（NET6+）</term><description>支持实现 IExtendable 接口对象的扩展属性序列化</description></item>
    /// <item><term>SafeInt64Converter / SafeUInt64Converter（NETCOREAPP，web=true）</term><description>Int64/UInt64 超出 JS 安全整数范围（2^53-1）时自动转为字符串，避免前端精度丢失</description></item>
    /// <item><term>DataMemberResolver（NET7+）</term><description>TypeInfoResolver 设为 DataMemberResolver.Default，支持 [DataMember] 特性控制序列化行为（如名称映射、顺序、忽略）</description></item>
    /// </list>
    /// 典型用法：services.Configure&lt;JsonOptions&gt;(options =&gt; SystemJson.Apply(options.JsonSerializerOptions, web: true));
    /// </remarks>
    /// <param name="options">目标序列化选项</param>
    /// <param name="web">是否为Web场景。启用后额外注册 SafeInt64Converter/SafeUInt64Converter，Int64/UInt64 超出 JS 安全整数范围时转为字符串</param>
    /// <returns>传入的选项实例，便于链式调用</returns>
    public static JsonSerializerOptions Apply(JsonSerializerOptions options, Boolean web = false)
    {
        options.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
        options.Converters.Add(new LocalTimeConverter());
        options.Converters.Add(new TypeConverter());
#if NET6_0_OR_GREATER
        options.Converters.Add(new ExtendableConverter());
#endif
#if NETCOREAPP
        if (web)
        {
            options.Converters.Add(new SafeInt64Converter());
            options.Converters.Add(new SafeUInt64Converter());
        }
#endif
#if NET7_0_OR_GREATER
        options.TypeInfoResolver = DataMemberResolver.Default;
#endif
        return options;
    }
    #endregion

    #region 属性
    /// <summary>基础 System.Text.Json 配置项。包含 Encoder、Converter 等基础设置，由 <see cref="BuildSerializerOptions"/> 在此基础上叠加 <see cref="JsonOptions"/> 配置</summary>
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

    #region 选项构建
    /// <summary>缓存默认 Options 对应的 JsonSerializerOptions，避免高频调用时重复构建</summary>
    private volatile JsonSerializerOptions? _cachedSerializerOptions;

    /// <summary>获取默认 <see cref="Options"/> 对应的 <see cref="JsonSerializerOptions"/>，使用缓存</summary>
    /// <returns>可复用的 JsonSerializerOptions 实例</returns>
    private JsonSerializerOptions GetDefaultSerializerOptions() => _cachedSerializerOptions ??= BuildSerializerOptions(Options);

    /// <summary>根据 <see cref="JsonOptions"/> 获取对应的 <see cref="JsonSerializerOptions"/></summary>
    /// <param name="jsonOptions">序列化选项。为 null 或与 <see cref="Options"/> 同引用时使用缓存</param>
    /// <returns>JsonSerializerOptions 实例</returns>
    private JsonSerializerOptions GetSerializerOptions(JsonOptions? jsonOptions)
    {
        if (jsonOptions == null || ReferenceEquals(jsonOptions, Options))
            return GetDefaultSerializerOptions();

        return BuildSerializerOptions(jsonOptions);
    }

    /// <summary>根据 <see cref="JsonOptions"/> 构建 <see cref="JsonSerializerOptions"/>。Write 和 Read 共享同一构建逻辑</summary>
    /// <param name="jsonOptions">序列化选项</param>
    /// <returns>新的 JsonSerializerOptions 实例</returns>
    private JsonSerializerOptions BuildSerializerOptions(JsonOptions jsonOptions)
    {
        var opt = new JsonSerializerOptions(SerializerOptions)
        {
            WriteIndented = jsonOptions.WriteIndented,
        };

        if (jsonOptions.IgnoreNullValues)
            opt.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;

        // 优先使用 PropertyNaming，向后兼容 CamelCase
#if NET8_0_OR_GREATER
        opt.PropertyNamingPolicy = jsonOptions.PropertyNaming switch
        {
            PropertyNaming.None => null,
            PropertyNaming.CamelCase => JsonNamingPolicy.CamelCase,
            PropertyNaming.KebabCaseLower => JsonNamingPolicy.KebabCaseLower,
            PropertyNaming.KebabCaseUpper => JsonNamingPolicy.KebabCaseUpper,
            PropertyNaming.SnakeCaseLower => JsonNamingPolicy.SnakeCaseLower,
            PropertyNaming.SnakeCaseUpper => JsonNamingPolicy.SnakeCaseUpper,
            _ => null,
        };
#else
        opt.PropertyNamingPolicy = jsonOptions.PropertyNaming switch
        {
            PropertyNaming.None => null,
            PropertyNaming.CamelCase => JsonNamingPolicy.CamelCase,
            PropertyNaming.KebabCaseLower => new KebabCaseLowerNamingPolicy(),
            PropertyNaming.KebabCaseUpper => new KebabCaseUpperNamingPolicy(),
            PropertyNaming.SnakeCaseLower => new SnakeCaseLowerNamingPolicy(),
            PropertyNaming.SnakeCaseUpper => new SnakeCaseUpperNamingPolicy(),
            _ => null,
        };
#endif

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

        return opt;
    }

#if NET8_0_OR_GREATER
#else
    /// <summary>小写 kebab 命名策略</summary>
    private class KebabCaseLowerNamingPolicy : JsonNamingPolicy
    {
        public override String ConvertName(String name)
        {
            var sb = Pool.StringBuilder.Get();
            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (i > 0 && Char.IsUpper(c))
                    sb.Append('-');
                sb.Append(Char.ToLower(c));
            }
            return sb.Return(true);
        }
    }

    /// <summary>大写 kebab 命名策略</summary>
    private class KebabCaseUpperNamingPolicy : JsonNamingPolicy
    {
        public override String ConvertName(String name)
        {
            var sb = Pool.StringBuilder.Get();
            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (i > 0 && Char.IsUpper(c))
                    sb.Append('-');
                sb.Append(Char.ToUpper(c));
            }
            return sb.Return(true);
        }
    }

    /// <summary>小写 snake 命名策略</summary>
    private class SnakeCaseLowerNamingPolicy : JsonNamingPolicy
    {
        public override String ConvertName(String name)
        {
            var sb = Pool.StringBuilder.Get();
            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (i > 0 && Char.IsUpper(c))
                    sb.Append('_');
                sb.Append(Char.ToLower(c));
            }
            return sb.Return(true);
        }
    }

    /// <summary>大写 snake 命名策略</summary>
    private class SnakeCaseUpperNamingPolicy : JsonNamingPolicy
    {
        public override String ConvertName(String name)
        {
            var sb = Pool.StringBuilder.Get();
            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (i > 0 && Char.IsUpper(c))
                    sb.Append('_');
                sb.Append(Char.ToUpper(c));
            }
            return sb.Return(true);
        }
    }
#endif
    #endregion

    #region IJsonHost 成员
    /// <summary>写入对象，得到Json字符串。以 <see cref="Options"/> 为基准，再叠加指定参数</summary>
    /// <param name="value">待序列化的对象</param>
    /// <param name="indented">是否缩进。为 true 时强制开启缩进。默认false</param>
    /// <param name="nullValue">是否写空值。为 false 时强制忽略空值。默认true</param>
    /// <param name="camelCase">是否驼峰命名。为 true 时强制驼峰命名。默认false</param>
    /// <returns>Json字符串</returns>
    public String Write(Object value, Boolean indented = false, Boolean nullValue = true, Boolean camelCase = false)
    {
        var opt = Options;
        // 入参不会改变 Options 已有配置时，直接使用 Options 缓存路径，避免复制 JsonOptions 和重建 JsonSerializerOptions
        var needCopy = (indented && !opt.WriteIndented) ||
                       (!nullValue && !opt.IgnoreNullValues) ||
                       (camelCase && opt.PropertyNaming != PropertyNaming.CamelCase);
        if (!needCopy)
            return Write(value, (JsonOptions?)null);

        var newOpt = new JsonOptions(opt);
        if (indented) newOpt.WriteIndented = true;
        if (!nullValue) newOpt.IgnoreNullValues = true;
        if (camelCase) newOpt.PropertyNaming = PropertyNaming.CamelCase;
        return Write(value, newOpt);
    }

    /// <summary>写入对象，得到Json字符串。未传入配置时使用 <see cref="Options"/> 并走缓存路径</summary>
    /// <param name="value">待序列化的对象</param>
    /// <param name="jsonOptions">序列化选项。为 null 时使用 <see cref="Options"/></param>
    /// <returns>Json字符串</returns>
    public String Write(Object value, JsonOptions? jsonOptions) => JsonSerializer.Serialize(value, GetSerializerOptions(jsonOptions));

    /// <summary>从Json字符串中读取对象</summary>
    /// <param name="json">Json字符串</param>
    /// <param name="type">目标类型</param>
    /// <returns>反序列化后的对象</returns>
    public Object? Read(String json, Type type) => Read(json, type, null);

    /// <summary>从Json字符串中读取对象，使用指定序列化选项。未传入配置时使用 <see cref="Options"/> 并走缓存路径</summary>
    /// <param name="json">Json字符串</param>
    /// <param name="type">目标类型</param>
    /// <param name="jsonOptions">序列化选项。为 null 时使用 <see cref="Options"/></param>
    /// <returns>反序列化后的对象</returns>
    public Object? Read(String json, Type type, JsonOptions? jsonOptions) => JsonSerializer.Deserialize(json, type, GetSerializerOptions(jsonOptions));

    /// <summary>从Json字符串中读取对象</summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="json">Json字符串</param>
    /// <returns>反序列化后的对象</returns>
    public T? Read<T>(String json) where T : class => Read(json, typeof(T), null) as T;

    /// <summary>类型转换。将 Json 解析出的字典/列表转换为目标类型</summary>
    /// <param name="obj">Json解析出的对象（字典或列表）</param>
    /// <param name="targetType">目标类型</param>
    /// <returns>转换后的对象</returns>
    public Object? Convert(Object obj, Type targetType) => new JsonReader { Provider = ServiceProvider, Options = Options }.ToObject(obj, targetType, null);

    /// <summary>分析Json字符串得到字典或列表</summary>
    /// <param name="json">Json字符串</param>
    /// <returns>字典或列表</returns>
    public Object? Parse(String json) => JsonDocument.Parse(json).RootElement.ToArray();

    /// <summary>分析Json字符串得到字典</summary>
    /// <param name="json">Json字符串</param>
    /// <returns>Key 为属性名的字典</returns>
    public IDictionary<String, Object?> Decode(String json) => JsonDocument.Parse(json).RootElement.ToDictionary();
    #endregion
}
#endif
