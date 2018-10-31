using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using NewLife.Reflection;

namespace NewLife.Serialization
{
    /// <summary>Json读取器</summary>
    public class JsonReader
    {
        #region 属性
        /// <summary>是否使用UTC时间</summary>
        public Boolean UseUTCDateTime { get; set; }
        #endregion

        #region 构造
        //public JsonReader() { }
        #endregion

        #region 转换方法
        /// <summary>读取Json到指定类型</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <returns></returns>
        public T Read<T>(String json)
        {
            return (T)Read(json, typeof(T));
        }

        /// <summary>读取Json到指定类型</summary>
        /// <param name="json"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public Object Read(String json, Type type)
        {
            // 解码得到字典或列表
            var obj = new JsonParser(json).Decode();
            if (obj == null) return null;

            return ToObject(obj, type, null);
        }

        /// <summary>Json字典或列表转为具体类型对象</summary>
        /// <param name="jobj">Json对象</param>
        /// <param name="type">模板类型</param>
        /// <param name="target">目标对象</param>
        /// <returns></returns>
        public Object ToObject(Object jobj, Type type, Object target)
        {
            if (type == null && target != null) type = target.GetType();

            if (type.IsAssignableFrom(jobj.GetType())) return jobj;

            // Json对象是字典，目标类型可以是字典或复杂对象
            if (jobj is IDictionary<String, Object> vdic)
            {
                if (type.IsDictionary())
                    return ParseDictionary(vdic, type, target as IDictionary);
                else
                    return ParseObject(vdic, type, target);
            }

            // Json对象是列表，目标类型只能是列表或数组
            if (jobj is IList<Object> vlist)
            {
                if (type.IsList()) return ParseList(vlist, type, target);
                if (type.IsArray) return ParseArray(vlist, type, target);
                // 复杂键值的字典，也可能保存为Json数组
                if (type.IsDictionary()) return CreateDictionary(vlist, type, target);

                if (vlist.Count == 0) return target;

                throw new InvalidCastException($"Json数组无法转为目标类型[{type.FullName}]，仅支持数组和List<T>/IList<T>");
            }

            if (type != null && jobj.GetType() != type)
                return ChangeType(jobj, type);

            return jobj;
        }
        #endregion

        #region 复杂类型
        /// <summary>转为泛型列表</summary>
        /// <param name="vlist"></param>
        /// <param name="type"></param>
        /// <param name="target">目标对象</param>
        /// <returns></returns>
        private IList ParseList(IList<Object> vlist, Type type, Object target)
        {
            var elmType = type.GetGenericArguments().FirstOrDefault();

            // 处理一下type是IList<>的情况
            if (type.IsInterface) type = typeof(List<>).MakeGenericType(elmType);

            // 创建列表
            var list = (target ?? type.CreateInstance()) as IList;
            foreach (var item in vlist)
            {
                if (item == null) continue;

                var val = ToObject(item, elmType, null);
                list.Add(val);
            }
            return list;
        }

        /// <summary>转为数组</summary>
        /// <param name="list"></param>
        /// <param name="type"></param>
        /// <param name="target">目标对象</param>
        /// <returns></returns>
        private Array ParseArray(IList<Object> list, Type type, Object target)
        {
            var elmType = type?.GetElementTypeEx();
            if (elmType == null) elmType = typeof(Object);

            var arr = target as Array;
            if (arr == null) arr = Array.CreateInstance(elmType, list.Count);
            // 如果源数组有值，则最大只能创建源数组那么多项，抛弃多余项
            for (var i = 0; i < list.Count && i < arr.Length; i++)
            {
                var item = list[i];
                if (item == null) continue;

                var val = ToObject(item, elmType, arr.GetValue(i));
                arr.SetValue(val, i);
            }

            return arr;
        }

        /// <summary>转为泛型字典</summary>
        /// <param name="dic"></param>
        /// <param name="type"></param>
        /// <param name="target">目标对象</param>
        /// <returns></returns>
        private IDictionary ParseDictionary(IDictionary<String, Object> dic, Type type, IDictionary target)
        {
            var types = type.GetGenericArguments();

            if (target == null)
            {
                // 处理一下type是Dictionary<,>的情况
                if (type.IsInterface) type = typeof(Dictionary<,>).MakeGenericType(types[0], types[1]);

                target = type.CreateInstance() as IDictionary;
            }
            foreach (var kv in dic)
            {
                var key = ToObject(kv.Key, types[0], null);
                var val = ToObject(kv.Value, types[1], null);
                target.Add(key, val);
            }

            return target;
        }

        private Dictionary<Object, Int32> _circobj = new Dictionary<Object, Int32>();
        private Dictionary<Int32, Object> _cirrev = new Dictionary<Int32, Object>();
        /// <summary>字典转复杂对象，反射属性赋值</summary>
        /// <param name="dic"></param>
        /// <param name="type"></param>
        /// <param name="target">目标对象</param>
        /// <returns></returns>
        internal Object ParseObject(IDictionary<String, Object> dic, Type type, Object target)
        {
            if (type == typeof(NameValueCollection)) return CreateNV(dic);
            if (type == typeof(StringDictionary)) return CreateSD(dic);
            if (type == typeof(Object)) return dic;

            if (target == null) target = type.CreateInstance();

            if (type.IsDictionary()) return CreateDic(dic, type, target);

            if (!_circobj.TryGetValue(target, out var circount))
            {
                circount = _circobj.Count + 1;
                _circobj.Add(target, circount);
                _cirrev.Add(circount, target);
            }

            // 遍历所有可用于序列化的属性
            var props = type.GetProperties(true).ToDictionary(e => SerialHelper.GetName(e), e => e);
            foreach (var item in dic)
            {
                var v = item.Value;
                if (v == null) continue;

                if (!props.TryGetValue(item.Key, out var pi))
                {
                    // 可能有小写
                    pi = props.Values.FirstOrDefault(e => e.Name.EqualIgnoreCase(item.Key));
                    if (pi == null) continue;
                }
                if (!pi.CanWrite) continue;

                var pt = pi.PropertyType;
                if (pt.GetTypeCode() != TypeCode.Object)
                    target.SetValue(pi, ChangeType(v, pt));
                else
                {
                    var orig = target.GetValue(pi);
                    var val = ToObject(v, pt, orig);
                    if (val != orig) target.SetValue(pi, val);
                }
            }
            return target;
        }
        #endregion

        #region 辅助
        private Object ChangeType(Object value, Type type)
        {
            // 支持可空类型
            if (type.IsGenericType && type.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                if (value == null) return value;

                type = type.GetGenericArguments()[0];
            }

            if (type == typeof(Int32)) return value.ToInt();
            if (type == typeof(Int64)) return value.ToLong();
            if (type == typeof(String)) return value + "";

            if (type.IsEnum) return Enum.Parse(type, value + "");
            if (type == typeof(DateTime)) return CreateDateTime(value);

            if (type == typeof(Guid)) return new Guid((String)value);

            if (type == typeof(Byte[]))
            {
                if (value is Byte[]) return (Byte[])value;

                return Convert.FromBase64String(value + "");
            }

            if (type.GetTypeCode() == TypeCode.Object) return null;

            return value.ChangeType(type);
        }

        private StringDictionary CreateSD(IDictionary<String, Object> dic)
        {
            var nv = new StringDictionary();
            foreach (var item in dic)
                nv.Add(item.Key, (String)item.Value);

            return nv;
        }

        private NameValueCollection CreateNV(IDictionary<String, Object> dic)
        {
            var nv = new NameValueCollection();
            foreach (var item in dic)
                nv.Add(item.Key, (String)item.Value);

            return nv;
        }

        private IDictionary CreateDic(IDictionary<String, Object> dic, Type type, Object obj)
        {
            var nv = obj as IDictionary;
            if (type.IsGenericType && type.GetGenericArguments().Length >= 2)
            {
                var tval = type.GetGenericArguments()[1];
                foreach (var item in dic)
                    nv.Add(item.Key, item.Value.ChangeType(tval));
            }
            else
            {
                foreach (var item in dic)
                    nv.Add(item.Key, item.Value);
            }

            return nv;
        }

        private Int32 CreateInteger(String str, Int32 index, Int32 count)
        {
            var num = 0;
            var neg = false;
            for (var x = 0; x < count; x++, index++)
            {
                var cc = str[index];

                if (cc == '-')
                    neg = true;
                else if (cc == '+')
                    neg = false;
                else
                {
                    num *= 10;
                    num += cc - '0';
                }
            }
            if (neg) num = -num;

            return num;
        }

        /// <summary>创建时间</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private DateTime CreateDateTime(Object value)
        {
            if (value is DateTime) return (DateTime)value;
            if (value is Int64 || value is Int32)
            {
                var dt = value.ToDateTime();
                if (UseUTCDateTime) dt = dt.ToUniversalTime();
                return dt;
            }

            //用于解决奇葩json中时间字段使用了utc时间戳，还是用双引号包裹起来的情况。
            if (value is String)
            {
                if (long.TryParse(value + "", out var result) && result > 0)
                {
                    var sdt = result.ToDateTime();
                    if (UseUTCDateTime) sdt = sdt.ToUniversalTime();
                    return sdt;
                }
            }

            var str = (String)value;

            var utc = false;

            Int32 year;
            Int32 month;
            Int32 day;
            Int32 hour;
            Int32 min;
            Int32 sec;
            var ms = 0;

            year = CreateInteger(str, 0, 4);
            month = CreateInteger(str, 5, 2);
            day = CreateInteger(str, 8, 2);
            hour = CreateInteger(str, 11, 2);
            min = CreateInteger(str, 14, 2);
            sec = CreateInteger(str, 17, 2);
            if (str.Length > 21 && str[19] == '.')
                ms = CreateInteger(str, 20, 3);

            if (str[str.Length - 1] == 'Z') utc = true;

            if (!UseUTCDateTime && !utc)
                return new DateTime(year, month, day, hour, min, sec, ms);
            else
                return new DateTime(year, month, day, hour, min, sec, ms, DateTimeKind.Utc).ToLocalTime();
        }

        private Object CreateDictionary(IList<Object> list, Type type, Object target)
        {
            var types = type.GetGenericArguments();
            var dic = (target ?? type.CreateInstance()) as IDictionary;
            foreach (IDictionary<String, Object> values in list)
            {
                var key = ToObject(values["k"], types[0], null);
                var val = ToObject(values["v"], types[1], null);
                dic.Add(key, val);
            }

            return dic;
        }
        #endregion
    }
}