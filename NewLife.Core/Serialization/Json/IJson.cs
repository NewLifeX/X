using System;
using NewLife.Reflection;
using System.Web.Script.Serialization;
using NewLife.Web;
using NewLife.Log;

namespace NewLife.Serialization
{
    /// <summary>Json序列化接口</summary>
    public interface IJson
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
        public static IJson Default { get; set; }

        static JsonHelper()
        {
            if (JsonNet.Support())
                Default = new JsonNet();
            else
                Default = new JsonDefault();
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
    }

    class JsonDefault : IJson
    {
        #region IJson 成员
        public String Write(Object value, Boolean indented)
        {
            return new JavaScriptSerializer().Serialize(value);
        }

        public Object Read(String json, Type type)
        {
            // 如果有必要，可以实现JavaScriptTypeResolver，然后借助Type.GetTypeEx得到更强的反射类型能力
            return new JavaScriptSerializer().Deserialize(json, type);
        }
        #endregion
    }

    class JsonNet : IJson
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

                if (XTrace.Debug) XTrace.WriteLine("使用Json.Net，位于 {0}", _Convert.Assembly.Location);
            }
        }

        /// <summary>是否支持</summary>
        /// <returns></returns>
        public static Boolean Support() { return _Convert != null; }

        #region IJson 成员
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
}