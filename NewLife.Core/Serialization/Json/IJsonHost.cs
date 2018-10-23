using System;
using System.Reflection;
#if !__MOBILE__ && !__CORE__
using System.Web.Script.Serialization;
#endif
using System.Xml.Serialization;
using NewLife.Collections;
using NewLife.Log;
using NewLife.Reflection;

namespace NewLife.Serialization
{
    /// <summary>Json序列化接口</summary>
    public interface IJsonHost
    {
        /// <summary>写入对象，得到Json字符串</summary>
        /// <param name="value"></param>
        /// <param name="indented">是否缩进</param>
        /// <returns></returns>
        String Write(Object value, Boolean indented = false);

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
    }

    /// <summary>Json助手</summary>
    public static class JsonHelper
    {
        /// <summary>默认实现</summary>
        public static IJsonHost Default { get; set; } = new FastJson();

        static JsonHelper()
        {
            //Default = new FastJson();

            //if (JsonNet.Support())
            //    Default = new JsonNet();
            //else
            //    Default = new JsonDefault();
        }

        /// <summary>写入对象，得到Json字符串</summary>
        /// <param name="value"></param>
        /// <param name="indented">是否缩进</param>
        /// <returns></returns>
        public static String ToJson(this Object value, Boolean indented = false) => Default.Write(value, indented);

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
            if (json.IsNullOrEmpty()) return default(T);

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
                        else if (ch == '[' || ch == '{')
                        {
                            sb.Append(ch);
                            sb.Append("\r\n");
                            sb.Append(' ', ++indentation * 2);
                        }
                        else if (ch == ']' || ch == '}')
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
            if (obj == null) return default(T);
            if (obj is T) return (T)obj;
            if (obj.GetType().As<T>()) return (T)obj;

            return (T)Default.Convert(obj, typeof(T));
        }
    }

#if !__MOBILE__ && !__CORE__
    class JsonDefault : IJsonHost
    {
        private Boolean CheckScriptIgnoreAttribute(MemberInfo memberInfo)
        {
#if !__MOBILE__ && !__CORE__
            if (memberInfo.IsDefined(typeof(ScriptIgnoreAttribute), true)) return true;
#endif
            if (memberInfo.IsDefined(typeof(XmlIgnoreAttribute), true)) return true;

            return false;
        }

    #region IJsonHost 成员
        public String Write(Object value, Boolean indented)
        {
            var json = new JavaScriptSerializer().Serialize(value);
            //if (indented) json = Process(json);
            if (indented) json = JsonHelper.Format(json);

            return json;
        }

        public Object Read(String json, Type type)
        {
            // 如果有必要，可以实现JavaScriptTypeResolver，然后借助Type.GetTypeEx得到更强的反射类型能力
            return new JavaScriptSerializer().Deserialize(json, type);
        }

        public Object Convert(Object obj, Type targetType) => new JavaScriptSerializer().ConvertToType(obj, targetType);
    #endregion
    }

    class JsonNet : IJsonHost
    {
        private static Type _Convert;
        private static Type _Formatting;
        private static Object _Set;
        static JsonNet()
        {
            var type = "Newtonsoft.Json.JsonConvert".GetTypeEx();
            if (type != null)
            {
                _Convert = type;
                _Formatting = "Newtonsoft.Json.Formatting".GetTypeEx();
                type = "Newtonsoft.Json.JsonSerializerSettings".GetTypeEx();

                // 忽略循环引用
                _Set = type.CreateInstance();
                if (_Set != null) _Set.SetValue("ReferenceLoopHandling", 1);

                // 自定义IContractResolver，用XmlIgnore特性作为忽略属性的方法
                var sc = ScriptEngine.Create(_code, false);
                sc.Compile();
                if (sc.Method != null)
                {
                    _Set.SetValue("ContractResolver", sc.Method.DeclaringType.CreateInstance());
                }

                if (XTrace.Debug) XTrace.WriteLine("使用Json.Net，位于 {0}", _Convert.Assembly.Location);
            }
        }

        private const String _code = @"
class MyContractResolver : Newtonsoft.Json.Serialization.DefaultContractResolver
{
    protected override Newtonsoft.Json.Serialization.JsonProperty CreateProperty(MemberInfo member, Newtonsoft.Json.MemberSerialization memberSerialization)
    {
        if (member.GetCustomAttribute<System.Xml.Serialization.XmlIgnoreAttribute>() != null) return null;

        return base.CreateProperty(member, memberSerialization);
    }
    public static void Main() { }
}";

        /// <summary>是否支持</summary>
        /// <returns></returns>
        public static Boolean Support() => _Convert != null;

    #region IJsonHost 成员
        public String Write(Object value, Boolean indented)
        {
            // 忽略循环引用
            //var set = _Set.CreateInstance();
            //if (set != null) set.SetValue("ReferenceLoopHandling", 1);

            if (!indented)
                return (String)_Convert.Invoke("SerializeObject", value, _Set);
            else
                return (String)_Convert.Invoke("SerializeObject", value, Enum.ToObject(_Formatting, 1), _Set);
        }

        public Object Read(String json, Type type) => _Convert.Invoke("DeserializeObject", json, type);

        public Object Convert(Object obj, Type targetType) => new JsonReader().ToObject(obj, targetType, null);
    #endregion
    }
#endif

    class FastJson : IJsonHost
    {
        #region IJsonHost 成员

        public String Write(Object value, Boolean indented = false) => JsonWriter.ToJson(value, indented);

        public Object Read(String json, Type type) => new JsonReader().Read(json, type);

        public Object Convert(Object obj, Type targetType) => new JsonReader().ToObject(obj, targetType, null);
        #endregion
    }
}