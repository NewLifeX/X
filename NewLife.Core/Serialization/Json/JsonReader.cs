using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using NewLife.Reflection;

namespace NewLife.Serialization
{
    /// <summary>Json读取器</summary>
    internal class JsonReader
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
        public T ToObject<T>(String json)
        {
            return (T)ToObject(json, typeof(T));
        }

        /// <summary>读取Json到指定类型</summary>
        /// <param name="json"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public Object ToObject(String json, Type type)
        {
            // 解码得到字典或列表
            var obj = new JsonParser(json).Decode();
            if (obj == null) return null;

            return ToObject(obj, type);
        }

        /// <summary>Json字典或列表转为具体类型对象</summary>
        /// <param name="jobj"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public Object ToObject(Object jobj, Type type)
        {
            Type typeDef = null;
            if (type != null && type.IsGenericType) typeDef = type.GetGenericTypeDefinition();

            if (jobj is IDictionary)
            {
                if (type.IsDictionary()) // 字典
                    return RootDictionary(jobj, type);
                else
                    return Parse(jobj as IDictionary<String, Object>, type, null);
            }
            else if (jobj is IList<Object> vlist)
            {
                if (type.IsDictionary()) // 名值格式
                    return RootDictionary(jobj, type);
                else if (type.IsList()) // 泛型列表
                    return RootList(vlist, type);
                else
                {
                    var elmType = type.GetElementTypeEx();
                    //return (jobj as IList<Object>).Select(e => e.ChangeType(elmType)).ToArray();
                    var arr = Array.CreateInstance(elmType, vlist.Count);
                    for (var i = 0; i < vlist.Count; i++)
                    {
                        if (Type.GetTypeCode(elmType) != TypeCode.Object)
                            arr.SetValue(vlist[i].ChangeType(elmType), i);
                        else
                            arr.SetValue(ToObject(vlist[i], elmType), i);
                    }

                    return arr;
                }
            }
            else if (type == typeof(Byte[]))
            {
                if (jobj is Byte[]) return (Byte[])jobj;

                return Convert.FromBase64String(jobj + "");
            }
            else if (type != null && jobj.GetType() != type)
                return ChangeType(jobj, type);

            return jobj;
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

            if (type == typeof(Int32)) return (Int32)((Int64)value); if (type == typeof(Int64)) return (Int64)value;
            if (type == typeof(String)) return (String)value;

            if (type.IsEnum) return Enum.Parse(type, value + "");
            if (type == typeof(DateTime)) return CreateDateTime(value);

            if (type == typeof(Guid)) return new Guid((String)value);

            return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }

        /// <summary>转为泛型列表</summary>
        /// <param name="vlist"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private Object RootList(IList<Object> vlist, Type type)
        {
            var elmType = type.GetGenericArguments().FirstOrDefault();

            // 处理一下type是IList<>的情况
            if (type.IsInterface) type = typeof(List<>).MakeGenericType(elmType);

            // 创建列表
            var list = type.CreateInstance() as IList;
            foreach (var item in vlist)
            {
                var value = item;
                // 列表元素可能是字典或内层列表
                if (item is IDictionary<String, Object> dic)
                    value = Parse(dic, elmType, null);
                else if (item is IList<Object> vlist2)
                    value = RootList(vlist2, elmType);
                else
                    value = ChangeType(item, elmType);

                list.Add(value);
            }
            return list;
        }

        /// <summary>转为泛型字典</summary>
        /// <param name="obj"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private Object RootDictionary(Object obj, Type type)
        {
            var types = type.GetGenericArguments();

            // 处理一下type是Dictionary<,>的情况
            if (type.IsInterface) type = typeof(Dictionary<,>).MakeGenericType(types[0], types[1]);

            Type tkey = null;
            Type tval = null;
            if (types != null)
            {
                tkey = types[0];
                tval = types[1];
            }
            if (obj is IDictionary<String, Object>)
            {
                var dic = type.CreateInstance() as IDictionary;

                foreach (var kv in (IDictionary<String, Object>)obj)
                {
                    Object val;

                    if (kv.Value is IDictionary<String, Object>)
                        val = Parse(kv.Value as IDictionary<String, Object>, tval, null);

                    else if (tval.IsArray)
                        val = CreateArray((IList<Object>)kv.Value, tval, tval.GetElementType());

                    else if (kv.Value is IList)
                        val = CreateGenericList((IList<Object>)kv.Value, tval, types[0]);

                    else
                        val = ChangeType(kv.Value, tval);

                    var key = ChangeType(kv.Key, types[0]);
                    dic.Add(key, val);
                }

                return dic;
            }
            if (obj is IList<Object>) return CreateDictionary(obj as IList<Object>, type, types);

            return null;
        }

        private Dictionary<Object, Int32> _circobj = new Dictionary<Object, Int32>();
        private Dictionary<Int32, Object> _cirrev = new Dictionary<Int32, Object>();
        /// <summary>字典转复杂对象，反射属性赋值</summary>
        /// <param name="dic"></param>
        /// <param name="type"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        internal Object Parse(IDictionary<String, Object> dic, Type type, Object obj)
        {
            if (type == typeof(NameValueCollection)) return CreateNV(dic);
            if (type == typeof(StringDictionary)) return CreateSD(dic);
            if (type == typeof(Object)) return dic;

            if (obj == null) obj = type.CreateInstance();

            if (type.IsGenericType && type.As<IDictionary>()) return CreateDic(dic, type, obj);

            if (_circobj.TryGetValue(obj, out var circount) == false)
            {
                circount = _circobj.Count + 1;
                _circobj.Add(obj, circount);
                _cirrev.Add(circount, obj);
            }

            var props = type.GetProperties(true).ToDictionary(e => e.Name, e => e);
            foreach (var item in dic)
            {
                var v = item.Value;
                if (!props.TryGetValue(item.Key, out var pi))
                {
                    // 可能有小写
                    pi = props.Values.Where(e => e.Name.EqualIgnoreCase(item.Key)).FirstOrDefault();
                    if (pi == null) continue;
                }
                if (!pi.CanWrite) continue;

                if (v == null) continue;

                Object val = null;

                var vdic = v as IDictionary<String, Object>;
                var vlist = v as IList<Object>;

                var pt = pi.PropertyType;
                // 支持可空类型
                pt = Nullable.GetUnderlyingType(pt) ?? pt;
                if (pt.IsEnum)
                    val = Enum.Parse(pt, v + "");
                else if (pt == typeof(Object))
                    val = v;
                else if (pt == typeof(DateTime))
                    val = CreateDateTime(v);
                else if (pt == typeof(Guid))
                    val = new Guid((String)v);
                else if (pt == typeof(Byte[]))
                    val = Convert.FromBase64String((String)v);
                // 数组
                else if (pt.IsArray)
                    val = CreateArray(vlist, pt, pt.GetElementTypeEx());
                // 泛型列表
                else if (pt.IsList())
                    val = CreateGenericList(vlist, pt, pt.GetElementTypeEx());
                // 泛型字典
                else if (pt.IsDictionary())
                    val = CreateStringKeyDictionary(vdic, pt, obj.GetValue(pi));
                else if (pt.As<IDictionary>())
                    val = CreateDictionary(vlist, pt, obj.GetValue(pi));
                else if (pt == typeof(NameValueCollection))
                    val = CreateNV(vdic);
                else if (pt == typeof(StringDictionary))
                    val = CreateSD(vdic);
                else if (Type.GetTypeCode(pt) != TypeCode.Object)
                    val = v;
                else
                {
                    // 内嵌对象
                    val = Parse(vdic, pt, obj.GetValue(pi));

                    //throw new NotSupportedException();
                }

                obj.SetValue(pi, val);
            }
            return obj;
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

        private Object CreateDic(IDictionary<String, Object> dic, Type type, Object obj)
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
                    num += (Int32)(cc - '0');
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
                var num = Convert.ToInt64(value);
                var dt = new DateTime(1970, 1, 1);
                if (num > 100 * 365 * 24 * 3600L)
                    return dt.AddMilliseconds(num);
                else
                    return dt.AddSeconds(num);
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

        private Object CreateArray(IList<Object> list, Type type, Type elmType)
        {
            if (elmType == null) elmType = typeof(Object);

            var arr = Array.CreateInstance(elmType, list.Count);
            for (var i = 0; i < list.Count; i++)
            {
                var ob = list[i];
                if (ob == null)
                {
                    continue;
                }
                if (ob is IDictionary)
                    arr.SetValue(Parse((IDictionary<String, Object>)ob, elmType, null), i);
                else if (ob is ICollection)
                    arr.SetValue(CreateArray((IList<Object>)ob, elmType, elmType.GetElementType()), i);
                else
                    arr.SetValue(ChangeType(ob, elmType), i);
            }

            return arr;
        }

        private Object CreateGenericList(IList<Object> list, Type type, Type elmType)
        {
            var rs = type.CreateInstance() as IList;
            foreach (var ob in list)
            {
                if (ob is IDictionary)
                    rs.Add(Parse((IDictionary<String, Object>)ob, elmType, null));

                else if (ob is IList<Object>)
                {
                    if (elmType.IsGenericType)
                        rs.Add((IList<Object>)ob);
                    else
                        rs.Add(((IList<Object>)ob).ToArray());
                }
                else
                    rs.Add(ChangeType(ob, elmType));
            }
            return rs;
        }

        private Object CreateStringKeyDictionary(IDictionary<String, Object> dic, Type type, Object obj)
        {
            var types = type.GetGenericArguments();
            if (obj == null)
            {
                if (type.IsInterface) type = typeof(Dictionary<,>).MakeGenericType(types[0], types[1]);
                obj = type.CreateInstance();
            }
            var rs = obj as IDictionary;

            Type tkey = null;
            Type tval = null;
            if (types != null)
            {
                tkey = types[0];
                tval = types[1];
            }

            foreach (var item in dic)
            {
                var key = item.Key;
                Object val = null;

                if (item.Value is IDictionary<String, Object> vdic)
                    val = Parse(vdic, tval, null);

                else if (types != null && tval.IsArray)
                {
                    if (item.Value is Array)
                        val = item.Value;
                    else
                        val = CreateArray((IList<Object>)item.Value, tval, tval.GetElementType());
                }
                else if (item.Value is IList)
                    val = CreateGenericList((IList<Object>)item.Value, tval, tkey);

                else
                    val = ChangeType(item.Value, tval);

                rs.Add(key, val);
            }

            return rs;
        }

        private Object CreateDictionary(IList<Object> list, Type type, Object obj)
        {
            var types = type.GetGenericArguments();
            var dic = (obj ?? type.CreateInstance()) as IDictionary;
            Type tkey = null;
            Type tval = null;
            if (types != null)
            {
                tkey = types[0];
                tval = types[1];
            }

            foreach (IDictionary<String, Object> values in list)
            {
                var key = values["k"];
                var val = values["v"];

                if (key is IDictionary<String, Object>)
                    key = Parse((IDictionary<String, Object>)key, tkey, null);
                else
                    key = ChangeType(key, tkey);

                if (val is IDictionary<String, Object>)
                    val = Parse((IDictionary<String, Object>)val, tval, null);
                else
                    val = ChangeType(val, tval);

                dic.Add(key, val);
            }

            return dic;
        }
        #endregion
    }
}