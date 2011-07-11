using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Runtime.Serialization;
using NewLife.Reflection;
using NewLife.Collections;
using System.Xml.Serialization;

namespace NewLife.Serialization
{
    /// <summary>
    /// 对象信息
    /// </summary>
    public class ObjectInfo
    {
        #region 属性
        /// <summary>默认上下文</summary>
        public static StreamingContext DefaultStreamingContext = new StreamingContext();
        #endregion

        #region 创建成员信息
        /// <summary>
        /// 创建反射成员信息
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public static IObjectMemberInfo CreateObjectMemberInfo(MemberInfo member)
        {
            return new ReflectMemberInfo(member);
        }

        /// <summary>
        /// 创建简单成员信息
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IObjectMemberInfo CreateObjectMemberInfo(String name, Type type, Object value)
        {
            return new SimpleMemberInfo(name, type, value);
        }
        #endregion

        #region 获取成员信息
        /// <summary>
        /// 获取指定对象的成员信息。优先考虑ISerializable接口。
        /// 对于Write，该方法没有任何，问题；对于Read，如果是ISerializable接口，并且value是空，则可能无法取得成员信息。
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="value">对象</param>
        /// <param name="isField">是否字段</param>
        /// <param name="isBaseFirst">是否基类成员排在前面</param>
        /// <returns></returns>
        public static IObjectMemberInfo[] GetMembers(Type type, Object value, Boolean isField, Boolean isBaseFirst)
        {
            if (type == null && value != null) type = value.GetType();

            if (typeof(ISerializable).IsAssignableFrom(type))
            {
                // 如果异常，改用后面的方法获取成员信息
                try
                {
                    //return CreateDictionary(GetMembers(value as ISerializable, type));
                    return GetMembers(value as ISerializable, type);
                }
                catch { }
            }

            //return CreateDictionary(GetMembers(type, isField));
            return GetMembers(type, isField, isBaseFirst);
        }

        //static IDictionary<String, IObjectMemberInfo> CreateDictionary(IObjectMemberInfo[] mis)
        //{
        //    Dictionary<String, IObjectMemberInfo> dic = new Dictionary<string, IObjectMemberInfo>();
        //    foreach (IObjectMemberInfo item in mis)
        //    {
        //        dic.Add(item.Name, item);
        //    }
        //    return dic;
        //}

        static DictionaryCache<Type, IObjectMemberInfo[]> fieldCache = new DictionaryCache<Type, IObjectMemberInfo[]>();
        static DictionaryCache<Type, IObjectMemberInfo[]> fieldCache2 = new DictionaryCache<Type, IObjectMemberInfo[]>();
        static DictionaryCache<Type, IObjectMemberInfo[]> propertyCache = new DictionaryCache<Type, IObjectMemberInfo[]>();
        static DictionaryCache<Type, IObjectMemberInfo[]> propertyCache2 = new DictionaryCache<Type, IObjectMemberInfo[]>();
        static IObjectMemberInfo[] GetMembers(Type type, Boolean isField, Boolean isBaseFirst)
        {
            if (isField)
            {
                if (isBaseFirst)
                    return fieldCache.GetItem(type, delegate(Type t)
                    {
                        MemberInfo[] mis = FindFields(t, true);
                        if (mis == null || mis.Length < 1) return null;
                        return Array.ConvertAll<MemberInfo, IObjectMemberInfo>(mis, CreateObjectMemberInfo);
                    });
                else
                    return fieldCache2.GetItem(type, delegate(Type t)
                    {
                        MemberInfo[] mis = FindFields(t, false);
                        if (mis == null || mis.Length < 1) return null;
                        return Array.ConvertAll<MemberInfo, IObjectMemberInfo>(mis, CreateObjectMemberInfo);
                    });
            }
            else
            {
                if (isBaseFirst)
                    return propertyCache.GetItem(type, delegate(Type t)
                    {
                        MemberInfo[] mis = FindProperties(t, true);
                        if (mis == null || mis.Length < 1) return null;
                        return Array.ConvertAll<MemberInfo, IObjectMemberInfo>(mis, CreateObjectMemberInfo);
                    });
                else
                    return propertyCache2.GetItem(type, delegate(Type t)
                    {
                        MemberInfo[] mis = FindProperties(t, false);
                        if (mis == null || mis.Length < 1) return null;
                        return Array.ConvertAll<MemberInfo, IObjectMemberInfo>(mis, CreateObjectMemberInfo);
                    });
            }
        }

        static DictionaryCache<Type, MemberInfo[]> cache1 = new DictionaryCache<Type, MemberInfo[]>();
        /// <summary>
        /// 取得所有字段
        /// </summary>
        /// <param name="type"></param>
        /// <param name="isBaseFirst"></param>
        /// <returns></returns>
        static MemberInfo[] FindFields(Type type, Boolean isBaseFirst)
        {
            if (type == null) return null;

            List<MemberInfo> list = new List<MemberInfo>();

            // GetFields只能取得本类的字段，没办法取得基类的字段
            FieldInfo[] fis = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fis != null && fis.Length > 0)
            {
                foreach (FieldInfo item in fis)
                {
                    if (!Attribute.IsDefined(item, typeof(NonSerializedAttribute))) list.Add(item);
                }
            }

            // 递归取父级的字段
            if (type.BaseType != null && type.BaseType != typeof(Object))
            {
                MemberInfo[] mis = FindFields(type.BaseType, isBaseFirst);
                if (mis != null)
                {
                    if (isBaseFirst)
                    {
                        // 基类的字段排在子类字段前面
                        List<MemberInfo> list2 = new List<MemberInfo>(mis);
                        if (list.Count > 0) list2.AddRange(list);
                        list = list2;
                    }
                    else
                        list.AddRange(mis);
                }
            }

            if (list == null || list.Count < 1) return null;
            return list.ToArray();
        }

        /// <summary>
        /// 取得所有属性
        /// </summary>
        /// <param name="type"></param>
        /// <param name="isBaseFirst"></param>
        /// <returns></returns>
        static MemberInfo[] FindProperties(Type type, Boolean isBaseFirst)
        {
            if (type == null) return null;

            List<MemberInfo> list = new List<MemberInfo>();

            // 只返回本级的属性，递归返回，保证排序
            PropertyInfo[] pis = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            if (pis != null && pis.Length > 0)
            {
                foreach (PropertyInfo item in pis)
                {
                    // 必须可读写
                    if (!item.CanRead || !item.CanWrite) continue;

                    ParameterInfo[] ps = item.GetIndexParameters();
                    if (ps != null && ps.Length > 0) continue;

                    if (!Attribute.IsDefined(item, typeof(XmlIgnoreAttribute))) list.Add(item);
                }
            }

            // 递归取父级的属性
            if (type.BaseType != null && type.BaseType != typeof(Object))
            {
                MemberInfo[] mis = FindProperties(type.BaseType, isBaseFirst);
                if (mis != null)
                {
                    if (isBaseFirst)
                    {
                        // 基类的属性排在子类属性前面
                        List<MemberInfo> list2 = new List<MemberInfo>(mis);
                        if (list.Count > 0) list2.AddRange(list);
                        list = list2;
                    }
                    else
                        list.AddRange(mis);
                }
            }

            if (list == null || list.Count < 1) return null;
            return list.ToArray();
        }

        static Dictionary<Type, IObjectMemberInfo[]> serialCache = new Dictionary<Type, IObjectMemberInfo[]>();

        static IObjectMemberInfo[] GetMembers(ISerializable value, Type type)
        {
            IObjectMemberInfo[] mis = null;
            if (value == null)
            {
                if (serialCache.TryGetValue(type, out mis)) return mis;

                // 尝试创建type的实例
                value = GetDefaultObject(type) as ISerializable;
            }

            SerializationInfo info = new SerializationInfo(type, new FormatterConverter());

            value.GetObjectData(info, DefaultStreamingContext);

            List<IObjectMemberInfo> list = new List<IObjectMemberInfo>();
            foreach (SerializationEntry item in info)
            {
                list.Add(CreateObjectMemberInfo(item.Name, item.ObjectType, item.Value));
            }
            mis = list.ToArray();

            if (!serialCache.ContainsKey(type))
            {
                lock (serialCache)
                {
                    if (!serialCache.ContainsKey(type)) serialCache.Add(type, mis);
                }
            }

            return mis;
        }
        #endregion

        #region 默认对象
        static DictionaryCache<Type, Object> defCache = new DictionaryCache<Type, object>();
        /// <summary>
        /// 获取某个类型的默认对象
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Object GetDefaultObject(Type type)
        {
            // 使用FormatterServices.GetSafeUninitializedObject创建对象，该方法创建的对象不执行构造函数
            return defCache.GetItem(type, delegate(Type t)
            {
                if (t == typeof(String)) return null;
                //return FormatterServices.GetSafeUninitializedObject(t);

                return TypeX.CreateInstance(t);
            });
        }
        #endregion
    }
}