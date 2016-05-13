using System;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
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
    }

    /// <summary>Json助手</summary>
    public static class JsonHelper
    {
        /// <summary>默认实现</summary>
        public static IJsonHost Default { get; set; }

        static JsonHelper()
        {
            Default = new FastJson();

            //if (JsonNet.Support())
            //    Default = new JsonNet();
            //else
            //    Default = new JsonDefault();
        }

        /// <summary>写入对象，得到Json字符串</summary>
        /// <param name="value"></param>
        /// <param name="indented">是否缩进</param>
        /// <returns></returns>
        public static String ToJson(this Object value, Boolean indented = false)
        {
            return Default.Write(value, indented);
        }

        /// <summary>从Json字符串中读取对象</summary>
        /// <param name="json"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Object ToJsonEntity(this String json, Type type)
        {
            return Default.Read(json, type);
        }

        /// <summary>从Json字符串中读取对象</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <returns></returns>
        public static T ToJsonEntity<T>(this String json)
        {
            return (T)Default.Read(json, typeof(T));
        }

        /// <summary>格式化Json文本</summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static String Format(String json)
        {
            var sb = new StringBuilder();

            bool escaping = false;
            bool inQuotes = false;
            int indentation = 0;

            foreach (char ch in json)
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

            return sb.ToString();
        }
    }

    class JsonDefault : IJsonHost
    {
        //static JsonDefault()
        //{
        //    // 用XmlIgnore特性作为忽略属性的方法
        //    var src = typeof(JavaScriptSerializer).GetMethodEx("CheckScriptIgnoreAttribute");
        //    var dst = typeof(JsonDefault).GetMethodEx("CheckScriptIgnoreAttribute");
        //    if (src != null && dst != null)
        //    {
        //        //new JavaScriptSerializer().Invoke(src, src);
        //        //new JsonDefault().CheckScriptIgnoreAttribute(dst);
        //        ApiHook.ReplaceMethod(src, dst);
        //    }
        //}

        private bool CheckScriptIgnoreAttribute(MemberInfo memberInfo)
        {
#if !Android
            if (memberInfo.IsDefined(typeof(ScriptIgnoreAttribute), true)) return true;
#endif
            if (memberInfo.IsDefined(typeof(XmlIgnoreAttribute), true)) return true;

            return false;
        }

        #region IJsonHost 成员
#if !Android
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
#endif
        //static String Process(String inputText)
        //{
        //    bool escaped = false;
        //    bool inquotes = false;
        //    int column = 0;
        //    int indentation = 0;
        //    var indentations = new Stack<int>();
        //    int TABBING = 8;
        //    var sb = new StringBuilder();
        //    foreach (char x in inputText)
        //    {
        //        sb.Append(x);
        //        column++;
        //        if (escaped)
        //        {
        //            escaped = false;
        //        }
        //        else
        //        {
        //            if (x == '\\')
        //            {
        //                escaped = true;
        //            }
        //            else if (x == '\"')
        //            {
        //                inquotes = !inquotes;
        //            }
        //            else if (!inquotes)
        //            {
        //                if (x == ',')
        //                {
        //                    // if we see a comma, go to next line, and indent to the same depth
        //                    sb.Append("\r\n");
        //                    column = 0;
        //                    for (int i = 0; i < indentation; i++)
        //                    {
        //                        sb.Append(" ");
        //                        column++;
        //                    }
        //                }
        //                else if (x == '[' || x == '{')
        //                {
        //                    // if we open a bracket or brace, indent further (push on stack)
        //                    indentations.Push(indentation);
        //                    indentation = column;
        //                }
        //                else if (x == ']' || x == '}')
        //                {
        //                    // if we close a bracket or brace, undo one level of indent (pop)
        //                    indentation = indentations.Pop();
        //                }
        //                else if (x == ':')
        //                {
        //                    // if we see a colon, add spaces until we get to the next
        //                    // tab stop, but without using tab characters!
        //                    while ((column % TABBING) != 0)
        //                    {
        //                        sb.Append(' ');
        //                        column++;
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    return sb.ToString();
        //}
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
        public static Boolean Support() { return _Convert != null; }

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

        public Object Read(String json, Type type)
        {
            return _Convert.Invoke("DeserializeObject", json, type);
        }
        #endregion
    }

    class FastJson : IJsonHost
    {

        #region IJsonHost 成员

        public string Write(object value, bool indented = false)
        {
            return new JsonWriter().ToJson(value, indented);
        }

        public object Read(string json, Type type)
        {
            return new JsonReader().ToObject(json, type);
        }

        #endregion
    }
}