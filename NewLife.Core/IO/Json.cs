using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using NewLife.Reflection;

namespace NewLife.IO
{
    /// <summary>Json</summary>
    public class Json
    {
        #region 属性
        internal static readonly long DatetimeMinTimeTicks;
        internal const string ServerTypeFieldName = "__type";

        private Int32 _MaxJsonLength = 0x20000000;
        /// <summary>最大长度</summary>
        public Int32 MaxJsonLength
        {
            get { return _MaxJsonLength; }
            set { _MaxJsonLength = value; }
        }

        private Int32 _RecursionLimit = 100;
        /// <summary>递归限制</summary>
        public Int32 RecursionLimit
        {
            get { return _RecursionLimit; }
            set { _RecursionLimit = value; }
        }
        #endregion

        #region 构造
        static Json()
        {
            DateTime time = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DatetimeMinTimeTicks = time.Ticks;
        }
        #endregion

        #region 方法
        #region 反序列化
        /// <summary></summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public T ConvertToType<T>(object obj)
        {
            return (T)ObjectConverter.ConvertObjectToType(obj, typeof(T), this);
        }

        /// <summary></summary>
        /// <param name="obj"></param>
        /// <param name="targetType"></param>
        /// <returns></returns>
        public object ConvertToType(object obj, Type targetType)
        {
            return ObjectConverter.ConvertObjectToType(obj, targetType, this);
        }

        /// <summary></summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public T Deserialize<T>(string input)
        {
            return (T)Deserialize(this, input, typeof(T), RecursionLimit);
        }

        /// <summary></summary>
        /// <param name="input"></param>
        /// <param name="targetType"></param>
        /// <returns></returns>
        public object Deserialize(string input, Type targetType)
        {
            return Deserialize(this, input, targetType, RecursionLimit);
        }

        static object Deserialize(Json serializer, string input, Type type, int depthLimit)
        {
            if (input == null) throw new ArgumentNullException("input");
            if (input.Length > serializer.MaxJsonLength) throw new ArgumentException("input");

            return ObjectConverter.ConvertObjectToType(JsonObjectDeserializer.BasicDeserialize(input, depthLimit, serializer), type, serializer);
        }

        /// <summary></summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public object DeserializeObject(string input)
        {
            return Deserialize(this, input, null, RecursionLimit);
        }
        #endregion

        #region 序列化
        /// <summary></summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public string Serialize(object obj)
        {
            return Serialize(obj, SerializationFormat.JSON);
        }

        /// <summary></summary>
        /// <param name="obj"></param>
        /// <param name="output"></param>
        public void Serialize(object obj, StringBuilder output)
        {
            Serialize(obj, output, SerializationFormat.JSON);
        }

        string Serialize(object obj, SerializationFormat serializationFormat)
        {
            var output = new StringBuilder();
            Serialize(obj, output, serializationFormat);
            return output.ToString();
        }

        internal void Serialize(object obj, StringBuilder output, SerializationFormat serializationFormat)
        {
            SerializeValue(obj, output, 0, null, serializationFormat);
            if (serializationFormat == SerializationFormat.JSON) CheckMaxLength(output.Length);
        }

        private static void SerializeBoolean(bool obj, StringBuilder sb)
        {
            if (obj)
            {
                sb.Append("true");
            }
            else
            {
                sb.Append("false");
            }
        }

        private void SerializeCustomObject(object obj, StringBuilder sb, int depth, Hashtable objectsInUse, SerializationFormat serializationFormat)
        {
            bool flag = true;
            Type type = obj.GetType();
            sb.Append('{');

            // 不要输出__type
            //string str = type.FullName;
            //if (str != null)
            //{
            //    SerializeString(ServerTypeFieldName, sb);
            //    sb.Append(':');
            //    SerializeValue(str, sb, depth, objectsInUse, serializationFormat);
            //    flag = false;
            //}

            foreach (var fi in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (fi.IsDefined(typeof(NonSerializedAttribute), true)) continue;

                if (!flag)
                {
                    sb.Append(',');
                }
                SerializeString(fi.Name, sb);
                sb.Append(':');
                SerializeValue(FieldInfoX.Create(fi).GetValue(obj), sb, depth, objectsInUse, serializationFormat);
                flag = false;
            }
            foreach (var pi in type.GetProperties(BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance))
            {
                if (pi.IsDefined(typeof(XmlIgnoreAttribute), true)) continue;

                var getMethod = pi.GetGetMethod();
                if ((getMethod != null) && (getMethod.GetParameters().Length <= 0))
                {
                    if (!flag)
                    {
                        sb.Append(',');
                    }
                    SerializeString(pi.Name, sb);
                    sb.Append(':');
                    SerializeValue(PropertyInfoX.Create(pi).GetValue(obj), sb, depth, objectsInUse, serializationFormat);
                    flag = false;
                }
            }
            sb.Append('}');
        }

        private static void SerializeDateTime(DateTime datetime, StringBuilder sb, SerializationFormat serializationFormat)
        {
            if (serializationFormat == SerializationFormat.JSON)
            {
                sb.Append("\"\\/Date(");
                sb.Append((long)((datetime.ToUniversalTime().Ticks - DatetimeMinTimeTicks) / 0x2710L));
                sb.Append(")\\/\"");
            }
            else
            {
                sb.Append("new Date(");
                sb.Append((long)((datetime.ToUniversalTime().Ticks - DatetimeMinTimeTicks) / 0x2710L));
                sb.Append(")");
            }
        }

        private void SerializeDictionary(IDictionary dic, StringBuilder sb, int depth, Hashtable objectsInUse, SerializationFormat serializationFormat)
        {
            sb.Append('{');
            bool flag = true;
            bool flag2 = false;
            if (dic.Contains(ServerTypeFieldName))
            {
                flag = false;
                flag2 = true;
                SerializeDictionaryKeyValue(ServerTypeFieldName, dic[ServerTypeFieldName], sb, depth, objectsInUse, serializationFormat);
            }
            foreach (DictionaryEntry entry in dic)
            {
                string key = entry.Key as string;
                if (key == null)
                {
                    throw new ArgumentException(string.Format("不支持字典类型{0}！", dic.GetType().FullName));
                }
                if (flag2 && string.Equals(key, ServerTypeFieldName, StringComparison.OrdinalIgnoreCase))
                {
                    flag2 = false;
                }
                else
                {
                    if (!flag)
                    {
                        sb.Append(',');
                    }
                    SerializeDictionaryKeyValue(key, entry.Value, sb, depth, objectsInUse, serializationFormat);
                    flag = false;
                }
            }
            sb.Append('}');
        }

        private void SerializeDictionaryKeyValue(string key, object value, StringBuilder sb, int depth, Hashtable objectsInUse, SerializationFormat serializationFormat)
        {
            SerializeString(key, sb);
            sb.Append(':');
            SerializeValue(value, sb, depth, objectsInUse, serializationFormat);
        }

        private void SerializeEnumerable(IEnumerable enumerable, StringBuilder sb, int depth, Hashtable objectsInUse, SerializationFormat serializationFormat)
        {
            sb.Append('[');
            bool flag = true;
            foreach (object obj2 in enumerable)
            {
                if (!flag)
                {
                    sb.Append(',');
                }
                SerializeValue(obj2, sb, depth, objectsInUse, serializationFormat);
                flag = false;
            }
            sb.Append(']');
        }

        private static void SerializeGuid(Guid guid, StringBuilder sb)
        {
            sb.Append("\"").Append(guid.ToString()).Append("\"");
        }

        internal static string SerializeInternal(object o)
        {
            Json serializer = new Json();
            return serializer.Serialize(o);
        }

        private static void SerializeString(string input, StringBuilder sb)
        {
            sb.Append('"');
            sb.Append(JavaScriptStringEncode(input));
            sb.Append('"');
        }

        private static void SerializeUri(Uri uri, StringBuilder sb)
        {
            sb.Append("\"").Append(uri.GetComponents(UriComponents.SerializationInfoString, UriFormat.UriEscaped)).Append("\"");
        }

        private void SerializeValue(object o, StringBuilder sb, int depth, Hashtable objectsInUse, SerializationFormat serializationFormat)
        {
            CheckDepth(++depth);

            if ((o == null) || DBNull.Value.Equals(o))
            {
                sb.Append("null");
            }
            else
            {
                string input = o as string;
                if (input != null)
                {
                    SerializeString(input, sb);
                }
                else if (o is char)
                {
                    if (((char)o) == '\0')
                    {
                        sb.Append("null");
                    }
                    else
                    {
                        SerializeString(o.ToString(), sb);
                    }
                }
                else if (o is bool)
                {
                    SerializeBoolean((bool)o, sb);
                }
                else if (o is DateTime)
                {
                    SerializeDateTime((DateTime)o, sb, serializationFormat);
                }
                else if (o is DateTimeOffset)
                {
                    DateTimeOffset offset = (DateTimeOffset)o;
                    SerializeDateTime(offset.UtcDateTime, sb, serializationFormat);
                }
                else if (o is Guid)
                {
                    SerializeGuid((Guid)o, sb);
                }
                else
                {
                    Uri uri = o as Uri;
                    if (uri != null)
                    {
                        SerializeUri(uri, sb);
                    }
                    else if (o is double)
                    {
                        sb.Append(((double)o).ToString("r", CultureInfo.InvariantCulture));
                    }
                    else if (o is float)
                    {
                        sb.Append(((float)o).ToString("r", CultureInfo.InvariantCulture));
                    }
                    else if (o.GetType().IsPrimitive || (o is decimal))
                    {
                        IConvertible convertible = o as IConvertible;
                        if (convertible != null)
                        {
                            sb.Append(convertible.ToString(CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            sb.Append(o.ToString());
                        }
                    }
                    else
                    {
                        Type enumType = o.GetType();
                        if (enumType.IsEnum)
                        {
                            Type underlyingType = Enum.GetUnderlyingType(enumType);
                            if ((underlyingType == typeof(long)) || (underlyingType == typeof(ulong)))
                            {
                                throw new InvalidOperationException("无效的枚举类型！");
                            }
                            sb.Append(((Enum)o).ToString("D"));
                        }
                        else
                        {
                            try
                            {
                                if (objectsInUse == null)
                                {
                                    objectsInUse = new Hashtable(new ReferenceComparer());
                                }
                                else if (objectsInUse.ContainsKey(o))
                                {
                                    throw new InvalidOperationException(string.Format("循环引用{0}！", enumType.FullName));
                                }
                                objectsInUse.Add(o, null);
                                IDictionary dictionary = o as IDictionary;
                                if (dictionary != null)
                                {
                                    SerializeDictionary(dictionary, sb, depth, objectsInUse, serializationFormat);
                                }
                                else
                                {
                                    IEnumerable enumerable = o as IEnumerable;
                                    if (enumerable != null)
                                    {
                                        SerializeEnumerable(enumerable, sb, depth, objectsInUse, serializationFormat);
                                    }
                                    else
                                    {
                                        SerializeCustomObject(o, sb, depth, objectsInUse, serializationFormat);
                                    }
                                }
                            }
                            finally
                            {
                                if (objectsInUse != null)
                                {
                                    objectsInUse.Remove(o);
                                }
                            }
                        }
                    }
                }
            }
        }
        #endregion
        #endregion

        #region 辅助方法
        void CheckMaxLength(Int32 len)
        {
            if (len > MaxJsonLength) throw new InvalidOperationException(String.Format("长度超过所支持最大长度{0}！", MaxJsonLength));
        }

        void CheckDepth(Int32 depth)
        {
            if (depth > RecursionLimit) throw new InvalidOperationException(String.Format("分析深度超过最大限制{0}！", RecursionLimit));
        }

        static string JavaScriptStringEncode(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }
            StringBuilder builder = null;
            int startIndex = 0;
            int count = 0;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if ((((c == '\r') || (c == '\t')) || ((c == '"') || (c == '\''))) || ((((c == '<') || (c == '>')) || ((c == '\\') || (c == '\n'))) || (((c == '\b') || (c == '\f')) || (c < ' '))))
                {
                    if (builder == null)
                    {
                        builder = new StringBuilder(value.Length + 5);
                    }
                    if (count > 0)
                    {
                        builder.Append(value, startIndex, count);
                    }
                    startIndex = i + 1;
                    count = 0;
                }
                switch (c)
                {
                    case '<':
                    case '>':
                    case '\'':
                        {
                            builder.Append(@"\u");
                            builder.Append(((Int32)c).ToString("x4", CultureInfo.InvariantCulture));
                            continue;
                        }
                    case '\\':
                        {
                            builder.Append(@"\\");
                            continue;
                        }
                    case '\b':
                        {
                            builder.Append(@"\b");
                            continue;
                        }
                    case '\t':
                        {
                            builder.Append(@"\t");
                            continue;
                        }
                    case '\n':
                        {
                            builder.Append(@"\n");
                            continue;
                        }
                    case '\f':
                        {
                            builder.Append(@"\f");
                            continue;
                        }
                    case '\r':
                        {
                            builder.Append(@"\r");
                            continue;
                        }
                    case '"':
                        {
                            builder.Append("\\\"");
                            continue;
                        }
                }
                if (c < ' ')
                {
                    builder.Append(@"\u");
                    builder.Append(((Int32)c).ToString("x4", CultureInfo.InvariantCulture));
                }
                else
                {
                    count++;
                }
            }
            if (builder == null)
            {
                return value;
            }
            if (count > 0)
            {
                builder.Append(value, startIndex, count);
            }
            return builder.ToString();
        }
        #endregion

        #region 嵌套类
        private class ReferenceComparer : IEqualityComparer
        {
            // Methods
            bool IEqualityComparer.Equals(object x, object y)
            {
                return (x == y);
            }

            int IEqualityComparer.GetHashCode(object obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }

        internal enum SerializationFormat
        {
            JSON,
            JavaScript
        }

        static class ObjectConverter
        {
            #region 字段
            private static Type _dictionaryGenericType = typeof(Dictionary<,>);
            private static Type _enumerableGenericType = typeof(IEnumerable<>);
            private static Type _idictionaryGenericType = typeof(IDictionary<,>);
            private static Type _listGenericType = typeof(List<>);
            private static readonly Type[] s_emptyTypeArray = new Type[0];
            #endregion

            #region 方法
            private static bool AddItemToList(IList oldList, IList newList, Type elementType, Json serializer, bool throwOnError)
            {
                foreach (object obj3 in oldList)
                {
                    object obj2;
                    if (!ConvertObjectToTypeMain(obj3, elementType, serializer, throwOnError, out obj2))
                    {
                        return false;
                    }
                    newList.Add(obj2);
                }
                return true;
            }

            private static bool AssignToPropertyOrField(object propertyValue, object o, string memberName, Json serializer, bool throwOnError)
            {
                IDictionary dictionary = o as IDictionary;
                if (dictionary != null)
                {
                    if (!ConvertObjectToTypeMain(propertyValue, null, serializer, throwOnError, out propertyValue))
                    {
                        return false;
                    }
                    dictionary[memberName] = propertyValue;
                    return true;
                }
                Type type = o.GetType();
                PropertyInfo property = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (property != null)
                {
                    MethodInfo setMethod = property.GetSetMethod();
                    if (setMethod != null)
                    {
                        if (!ConvertObjectToTypeMain(propertyValue, property.PropertyType, serializer, throwOnError, out propertyValue))
                        {
                            return false;
                        }
                        try
                        {
                            MethodInfoX.Create(setMethod).Invoke(o, new object[] { propertyValue });
                            return true;
                        }
                        catch
                        {
                            if (throwOnError)
                            {
                                throw;
                            }
                            return false;
                        }
                    }
                }
                FieldInfo field = type.GetField(memberName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (field != null)
                {
                    if (!ConvertObjectToTypeMain(propertyValue, field.FieldType, serializer, throwOnError, out propertyValue))
                    {
                        return false;
                    }
                    try
                    {
                        FieldInfoX.Create(field).SetValue(o, propertyValue);
                        return true;
                    }
                    catch
                    {
                        if (throwOnError)
                        {
                            throw;
                        }
                        return false;
                    }
                }
                return true;
            }

            private static bool ConvertDictionaryToObject(IDictionary<string, object> dictionary, Type type, Json serializer, bool throwOnError, out object convertedObject)
            {
                object obj2;
                Type t = type;
                string id = null;
                object o = dictionary;
                if (dictionary.TryGetValue(ServerTypeFieldName, out obj2))
                {
                    if (!ConvertObjectToTypeMain(obj2, typeof(string), serializer, throwOnError, out obj2))
                    {
                        convertedObject = false;
                        return false;
                    }
                    id = (string)obj2;
                    if (id != null)
                    {
                        t = TypeX.GetType(id);
                        if (t == null)
                        {
                            if (throwOnError)
                            {
                                throw new InvalidOperationException();
                            }
                            convertedObject = null;
                            return false;
                        }
                        dictionary.Remove(ServerTypeFieldName);
                    }
                }
                if ((id != null) || IsClientInstantiatableType(t, serializer))
                {
                    o = Activator.CreateInstance(t);
                }
                List<string> list = new List<string>(dictionary.Keys);
                if (IsGenericDictionary(type))
                {
                    Type type3 = type.GetGenericArguments()[0];
                    if ((type3 != typeof(string)) && (type3 != typeof(object)))
                    {
                        if (throwOnError)
                        {
                            throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "AtlasWeb.JSON_DictionaryTypeNotSupported {0}", new object[] { type.FullName }));
                        }
                        convertedObject = null;
                        return false;
                    }
                    Type type4 = type.GetGenericArguments()[1];
                    IDictionary dictionary2 = null;
                    if (IsClientInstantiatableType(type, serializer))
                    {
                        dictionary2 = (IDictionary)Activator.CreateInstance(type);
                    }
                    else
                    {
                        dictionary2 = (IDictionary)Activator.CreateInstance(_dictionaryGenericType.MakeGenericType(new Type[] { type3, type4 }));
                    }
                    if (dictionary2 != null)
                    {
                        foreach (string str2 in list)
                        {
                            object obj4;
                            if (!ConvertObjectToTypeMain(dictionary[str2], type4, serializer, throwOnError, out obj4))
                            {
                                convertedObject = null;
                                return false;
                            }
                            dictionary2[str2] = obj4;
                        }
                        convertedObject = dictionary2;
                        return true;
                    }
                }
                if ((type != null) && !type.IsAssignableFrom(o.GetType()))
                {
                    if (!throwOnError)
                    {
                        convertedObject = null;
                        return false;
                    }
                    if (type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, s_emptyTypeArray, null) == null)
                    {
                        throw new MissingMethodException(string.Format("{0}没有构造函数！", new object[] { type.FullName }));
                    }
                    throw new InvalidOperationException(string.Format("{0}声明类型丢失", new object[] { type.FullName }));
                }
                foreach (string str3 in list)
                {
                    object propertyValue = dictionary[str3];
                    if (!AssignToPropertyOrField(propertyValue, o, str3, serializer, throwOnError))
                    {
                        convertedObject = null;
                        return false;
                    }
                }
                convertedObject = o;
                return true;
            }

            private static bool ConvertListToObject(IList list, Type type, Json serializer, bool throwOnError, out IList convertedList)
            {
                if (((type == null) || (type == typeof(object))) || IsArrayListCompatible(type))
                {
                    Type elementType = typeof(object);
                    if ((type != null) && (type != typeof(object)))
                    {
                        elementType = type.GetElementType();
                    }
                    ArrayList newList = new ArrayList();
                    if (!AddItemToList(list, newList, elementType, serializer, throwOnError))
                    {
                        convertedList = null;
                        return false;
                    }
                    if (((type == typeof(ArrayList)) || (type == typeof(IEnumerable))) || ((type == typeof(IList)) || (type == typeof(ICollection))))
                    {
                        convertedList = newList;
                        return true;
                    }
                    convertedList = newList.ToArray(elementType);
                    return true;
                }
                if (type.IsGenericType && (type.GetGenericArguments().Length == 1))
                {
                    Type type3 = type.GetGenericArguments()[0];
                    if (_enumerableGenericType.MakeGenericType(new Type[] { type3 }).IsAssignableFrom(type))
                    {
                        Type type5 = _listGenericType.MakeGenericType(new Type[] { type3 });
                        IList list3 = null;
                        if (IsClientInstantiatableType(type, serializer) && typeof(IList).IsAssignableFrom(type))
                        {
                            list3 = (IList)Activator.CreateInstance(type);
                        }
                        else
                        {
                            if (type5.IsAssignableFrom(type))
                            {
                                if (throwOnError)
                                {
                                    throw new InvalidOperationException(string.Format("无法建立列表类型{0}！", new object[] { type.FullName }));
                                }
                                convertedList = null;
                                return false;
                            }
                            list3 = (IList)Activator.CreateInstance(type5);
                        }
                        if (!AddItemToList(list, list3, type3, serializer, throwOnError))
                        {
                            convertedList = null;
                            return false;
                        }
                        convertedList = list3;
                        return true;
                    }
                }
                else if (IsClientInstantiatableType(type, serializer) && typeof(IList).IsAssignableFrom(type))
                {
                    IList list4 = (IList)Activator.CreateInstance(type);
                    if (!AddItemToList(list, list4, null, serializer, throwOnError))
                    {
                        convertedList = null;
                        return false;
                    }
                    convertedList = list4;
                    return true;
                }
                if (throwOnError)
                {
                    throw new InvalidOperationException(string.Format("不支持的数组类型{0}！", new object[] { type.FullName }));
                }
                convertedList = null;
                return false;
            }

            internal static object ConvertObjectToType(object o, Type type, Json serializer)
            {
                object obj2;
                ConvertObjectToTypeMain(o, type, serializer, true, out obj2);
                return obj2;
            }

            private static bool ConvertObjectToTypeInternal(object o, Type type, Json serializer, bool throwOnError, out object convertedObject)
            {
                IDictionary<string, object> dictionary = o as IDictionary<string, object>;
                if (dictionary != null)
                {
                    return ConvertDictionaryToObject(dictionary, type, serializer, throwOnError, out convertedObject);
                }
                IList list = o as IList;
                if (list != null)
                {
                    IList list2;
                    if (ConvertListToObject(list, type, serializer, throwOnError, out list2))
                    {
                        convertedObject = list2;
                        return true;
                    }
                    convertedObject = null;
                    return false;
                }
                if ((type == null) || (o.GetType() == type))
                {
                    convertedObject = o;
                    return true;
                }
                TypeConverter converter = TypeDescriptor.GetConverter(type);
                if (converter.CanConvertFrom(o.GetType()))
                {
                    try
                    {
                        convertedObject = converter.ConvertFrom(null, CultureInfo.InvariantCulture, o);
                        return true;
                    }
                    catch
                    {
                        if (throwOnError)
                        {
                            throw;
                        }
                        convertedObject = null;
                        return false;
                    }
                }
                if (converter.CanConvertFrom(typeof(string)))
                {
                    try
                    {
                        string str;
                        if (o is DateTime)
                        {
                            DateTime time = (DateTime)o;
                            str = time.ToUniversalTime().ToString("u", CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            str = TypeDescriptor.GetConverter(o).ConvertToInvariantString(o);
                        }
                        convertedObject = converter.ConvertFromInvariantString(str);
                        return true;
                    }
                    catch
                    {
                        if (throwOnError)
                        {
                            throw;
                        }
                        convertedObject = null;
                        return false;
                    }
                }
                if (type.IsAssignableFrom(o.GetType()))
                {
                    convertedObject = o;
                    return true;
                }
                if (throwOnError)
                {
                    throw new InvalidOperationException(string.Format("不能转换对象到指定类型{0}！", new object[] { o.GetType(), type }));
                }
                convertedObject = null;
                return false;
            }

            private static bool ConvertObjectToTypeMain(object o, Type type, Json serializer, bool throwOnError, out object convertedObject)
            {
                if (o == null)
                {
                    if (type == typeof(char))
                    {
                        convertedObject = '\0';
                        return true;
                    }
                    if (IsNonNullableValueType(type))
                    {
                        if (throwOnError)
                        {
                            throw new InvalidOperationException("值类型不能为空");
                        }
                        convertedObject = null;
                        return false;
                    }
                    convertedObject = null;
                    return true;
                }
                if (o.GetType() == type)
                {
                    convertedObject = o;
                    return true;
                }
                return ConvertObjectToTypeInternal(o, type, serializer, throwOnError, out convertedObject);
            }

            private static bool IsArrayListCompatible(Type type)
            {
                if ((!type.IsArray && !(type == typeof(ArrayList))) && (!(type == typeof(IEnumerable)) && !(type == typeof(IList))))
                {
                    return (type == typeof(ICollection));
                }
                return true;
            }

            internal static bool IsClientInstantiatableType(Type t, Json serializer)
            {
                if (((t == null) || t.IsAbstract) || (t.IsInterface || t.IsArray))
                {
                    return false;
                }
                if (t == typeof(object))
                {
                    return false;
                }
                return true;
            }

            private static bool IsGenericDictionary(Type type)
            {
                if (((type == null) || !type.IsGenericType) || (!typeof(IDictionary).IsAssignableFrom(type) && !(type.GetGenericTypeDefinition() == _idictionaryGenericType)))
                {
                    return false;
                }
                return (type.GetGenericArguments().Length == 2);
            }

            private static bool IsNonNullableValueType(Type type)
            {
                if ((type == null) || !type.IsValueType)
                {
                    return false;
                }
                if (type.IsGenericType)
                {
                    return !(type.GetGenericTypeDefinition() == typeof(Nullable<>));
                }
                return true;
            }

            internal static bool TryConvertObjectToType(object o, Type type, Json serializer, out object convertedObject)
            {
                return ConvertObjectToTypeMain(o, type, serializer, false, out convertedObject);
            }
            #endregion
        }

        class JsonObjectDeserializer
        {
            #region 字段
            private int _depthLimit;
            JsonString _s;
            private Json _serializer;
            private const string DateTimePrefix = "\"\\/Date(";
            private const int DateTimePrefixLength = 8;
            #endregion

            #region 方法
            private JsonObjectDeserializer(string input, int depthLimit, Json serializer)
            {
                _s = new JsonString(input);
                _depthLimit = depthLimit;
                _serializer = serializer;
            }

            private void AppendCharToBuilder(char? c, StringBuilder sb)
            {
                if (((c == '"') || (c == '\'')) || (c == '/'))
                {
                    sb.Append(c);
                }
                else if (c == 'b')
                {
                    sb.Append('\b');
                }
                else if (c == 'f')
                {
                    sb.Append('\f');
                }
                else if (c == 'n')
                {
                    sb.Append('\n');
                }
                else if (c == 'r')
                {
                    sb.Append('\r');
                }
                else if (c == 't')
                {
                    sb.Append('\t');
                }
                else
                {
                    if (c != 'u')
                    {
                        throw new ArgumentException(_s.GetDebugString("错误的转义符！"));
                    }
                    sb.Append((char)int.Parse(_s.MoveNext(4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                }
            }

            internal static object BasicDeserialize(string input, int depthLimit, Json serializer)
            {
                JsonObjectDeserializer deserializer = new JsonObjectDeserializer(input, depthLimit, serializer);
                object obj2 = deserializer.DeserializeInternal(0);
                char? nextNonEmptyChar = deserializer._s.GetNextNonEmptyChar();
                int? nullable3 = nextNonEmptyChar.HasValue ? new int?(nextNonEmptyChar.GetValueOrDefault()) : null;
                if (nullable3.HasValue)
                {
                    throw new ArgumentException(string.Format("非法类型{0}！", new object[] { deserializer._s.ToString() }));
                }
                return obj2;
            }

            private char CheckQuoteChar(char? c)
            {
                if (c == '\'')
                {
                    return c.Value;
                }
                if (c != '"')
                {
                    throw new ArgumentException(_s.GetDebugString("字符串没有引号！"));
                }
                return '"';
            }

            private IDictionary<string, object> DeserializeDictionary(int depth)
            {
                IDictionary<string, object> dictionary = null;
                char? nextNonEmptyChar;
                char? nullable8;
                char? nullable11;
                if (_s.MoveNext() != '{')
                {
                    throw new ArgumentException(_s.GetDebugString("期望是左大括号！"));
                }
            Label_018D:
                nullable8 = nextNonEmptyChar = _s.GetNextNonEmptyChar();
                int? nullable10 = nullable8.HasValue ? new int?(nullable8.GetValueOrDefault()) : null;
                if (nullable10.HasValue)
                {
                    _s.MovePrev();
                    if (nextNonEmptyChar == ':')
                    {
                        throw new ArgumentException(_s.GetDebugString("无效的成员名称！"));
                    }
                    string str = null;
                    if (nextNonEmptyChar != '}')
                    {
                        str = DeserializeMemberName();
                        if (string.IsNullOrEmpty(str))
                        {
                            throw new ArgumentException(_s.GetDebugString("无效的成员名称！"));
                        }
                        if (_s.GetNextNonEmptyChar() != ':')
                        {
                            throw new ArgumentException(_s.GetDebugString("无效的对象"));
                        }
                    }
                    if (dictionary == null)
                    {
                        dictionary = new Dictionary<string, object>();
                        if (string.IsNullOrEmpty(str))
                        {
                            nextNonEmptyChar = _s.GetNextNonEmptyChar();
                            goto Label_01CB;
                        }
                    }
                    object obj2 = DeserializeInternal(depth);
                    dictionary[str] = obj2;
                    nextNonEmptyChar = _s.GetNextNonEmptyChar();
                    if (nextNonEmptyChar != '}')
                    {
                        if (nextNonEmptyChar != ',')
                        {
                            throw new ArgumentException(_s.GetDebugString("无效的对象！"));
                        }
                        goto Label_018D;
                    }
                }
            Label_01CB:
                nullable11 = nextNonEmptyChar;
                if ((nullable11.GetValueOrDefault() != '}') || !nullable11.HasValue)
                {
                    throw new ArgumentException(_s.GetDebugString("无效的对象！"));
                }
                return dictionary;
            }

            private object DeserializeInternal(int depth)
            {
                if (++depth > _depthLimit) throw new ArgumentException(_s.GetDebugString("超过深度限制！"));

                char? nextNonEmptyChar = _s.GetNextNonEmptyChar();
                char? nullable2 = nextNonEmptyChar;
                int? nullable4 = nullable2.HasValue ? new int?(nullable2.GetValueOrDefault()) : null;
                if (!nullable4.HasValue)
                {
                    return null;
                }
                _s.MovePrev();
                if (IsNextElementDateTime())
                {
                    return DeserializeStringIntoDateTime();
                }
                if (IsNextElementObject(nextNonEmptyChar))
                {
                    IDictionary<string, object> o = DeserializeDictionary(depth);
                    if (o.ContainsKey(ServerTypeFieldName))
                    {
                        return ObjectConverter.ConvertObjectToType(o, null, _serializer);
                    }
                    return o;
                }
                if (IsNextElementArray(nextNonEmptyChar))
                {
                    return DeserializeList(depth);
                }
                if (IsNextElementString(nextNonEmptyChar))
                {
                    return DeserializeString();
                }
                return DeserializePrimitiveObject();
            }

            private IList DeserializeList(int depth)
            {
                char? nextNonEmptyChar;
                char? nullable5;
                IList list = new ArrayList();
                if (_s.MoveNext() != '[')
                {
                    throw new ArgumentException(_s.GetDebugString("无效的数组开始"));
                }
                bool flag = false;
            Label_00C4:
                nullable5 = nextNonEmptyChar = _s.GetNextNonEmptyChar();
                int? nullable7 = nullable5.HasValue ? new int?(nullable5.GetValueOrDefault()) : null;
                if (nullable7.HasValue && (nextNonEmptyChar != ']'))
                {
                    _s.MovePrev();
                    object obj2 = DeserializeInternal(depth);
                    list.Add(obj2);
                    flag = false;
                    nextNonEmptyChar = _s.GetNextNonEmptyChar();
                    if (nextNonEmptyChar != ']')
                    {
                        flag = true;
                        if (nextNonEmptyChar != ',')
                        {
                            throw new ArgumentException(_s.GetDebugString("无效数组！"));
                        }
                        goto Label_00C4;
                    }
                }
                if (flag)
                {
                    throw new ArgumentException(_s.GetDebugString("无效数组！"));
                }
                if (nextNonEmptyChar != ']')
                {
                    throw new ArgumentException(_s.GetDebugString("无效的数组结束符！"));
                }
                return list;
            }

            private string DeserializeMemberName()
            {
                char? nextNonEmptyChar = _s.GetNextNonEmptyChar();
                char? nullable2 = nextNonEmptyChar;
                int? nullable4 = nullable2.HasValue ? new int?(nullable2.GetValueOrDefault()) : null;
                if (!nullable4.HasValue)
                {
                    return null;
                }
                _s.MovePrev();
                if (IsNextElementString(nextNonEmptyChar))
                {
                    return DeserializeString();
                }
                return DeserializePrimitiveToken();
            }

            private object DeserializePrimitiveObject()
            {
                double num4;
                string s = DeserializePrimitiveToken();
                if (s.Equals("null"))
                {
                    return null;
                }
                if (s.Equals("true"))
                {
                    return true;
                }
                if (s.Equals("false"))
                {
                    return false;
                }
                bool flag = s.IndexOf('.') >= 0;
                if (s.LastIndexOf("e", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    decimal num3;
                    if (!flag)
                    {
                        int num;
                        long num2;
                        if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out num))
                        {
                            return num;
                        }
                        if (long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out num2))
                        {
                            return num2;
                        }
                    }
                    if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out num3))
                    {
                        return num3;
                    }
                }
                if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out num4))
                {
                    throw new ArgumentException(string.Format("非法类型{0}！", new object[] { s }));
                }
                return num4;
            }

            private string DeserializePrimitiveToken()
            {
                char? nullable2;
                var builder = new StringBuilder();
                char? nullable = null;
            Label_0066:
                nullable2 = nullable = _s.MoveNext();
                int? nullable4 = nullable2.HasValue ? new int?(nullable2.GetValueOrDefault()) : null;
                if (nullable4.HasValue)
                {
                    if ((char.IsLetterOrDigit(nullable.Value) || (nullable.Value == '.')) || (((nullable.Value == '-') || (nullable.Value == '_')) || (nullable.Value == '+')))
                    {
                        builder.Append(nullable);
                    }
                    else
                    {
                        _s.MovePrev();
                        goto Label_00A2;
                    }
                    goto Label_0066;
                }
            Label_00A2:
                return builder.ToString();
            }

            private string DeserializeString()
            {
                var sb = new StringBuilder();
                bool flag = false;
                char? c = _s.MoveNext();
                char ch = CheckQuoteChar(c);
                while (true)
                {
                    char? nullable4 = c = _s.MoveNext();
                    int? nullable6 = nullable4.HasValue ? new int?(nullable4.GetValueOrDefault()) : null;
                    if (!nullable6.HasValue)
                    {
                        throw new ArgumentException(_s.GetDebugString("未结束的字符串！"));
                    }
                    if (c == '\\')
                    {
                        if (flag)
                        {
                            sb.Append('\\');
                            flag = false;
                        }
                        else
                        {
                            flag = true;
                        }
                    }
                    else if (flag)
                    {
                        AppendCharToBuilder(c, sb);
                        flag = false;
                    }
                    else
                    {
                        char? nullable3 = c;
                        int num = ch;
                        if ((nullable3.GetValueOrDefault() == num) && nullable3.HasValue)
                        {
                            return sb.ToString();
                        }
                        sb.Append(c);
                    }
                }
            }

            private object DeserializeStringIntoDateTime()
            {
                long num;
                Match match = Regex.Match(_s.ToString(), "^\"\\\\/Date\\((?<ticks>-?[0-9]+)(?:[a-zA-Z]|(?:\\+|-)[0-9]{4})?\\)\\\\/\"");
                if (long.TryParse(match.Groups["ticks"].Value, out num))
                {
                    _s.MoveNext(match.Length);
                    return new DateTime((num * 0x2710L) + Json.DatetimeMinTimeTicks, DateTimeKind.Utc);
                }
                return DeserializeString();
            }

            private static bool IsNextElementArray(char? c)
            {
                return (c == '[');
            }

            private bool IsNextElementDateTime()
            {
                string a = _s.MoveNext(8);
                if (a != null)
                {
                    _s.MovePrev(8);
                    return string.Equals(a, "\"\\/Date(", StringComparison.OrdinalIgnoreCase);
                }
                return false;
            }

            private static bool IsNextElementObject(char? c)
            {
                return (c == '{');
            }

            private static bool IsNextElementString(char? c)
            {
                return ((c == '"') || (c == '\''));
            }
            #endregion
        }

        class JsonString
        {
            #region 字段
            private int _index;
            private string _s;
            #endregion

            #region 方法
            internal JsonString(string s)
            {
                _s = s;
            }

            internal string GetDebugString(string message)
            {
                return string.Concat(new object[] { message, " (", _index, "): ", _s });
            }

            internal char? GetNextNonEmptyChar()
            {
                while (_s.Length > _index)
                {
                    char c = _s[_index++];
                    if (!char.IsWhiteSpace(c)) return new char?(c);
                }
                return null;
            }

            internal char? MoveNext()
            {
                if (_s.Length > _index) return new char?(_s[_index++]);

                return null;
            }

            internal string MoveNext(int count)
            {
                if (_s.Length >= (_index + count))
                {
                    string str = _s.Substring(_index, count);
                    _index += count;
                    return str;
                }
                return null;
            }

            internal void MovePrev()
            {
                if (_index > 0) _index--;
            }

            internal void MovePrev(int count)
            {
                while ((_index > 0) && (count > 0))
                {
                    _index--;
                    count--;
                }
            }

            public override string ToString()
            {
                if (_s.Length > _index) return _s.Substring(_index);

                return string.Empty;
            }
            #endregion
        }
        #endregion
    }
}