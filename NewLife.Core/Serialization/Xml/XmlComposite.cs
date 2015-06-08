using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using NewLife.Collections;
using NewLife.Reflection;

namespace NewLife.Serialization
{
    /// <summary>Xml复合对象处理器</summary>
    public class XmlComposite : XmlHandlerBase
    {
        /// <summary>实例化</summary>
        public XmlComposite()
        {
            Priority = 100;
        }

        /// <summary>写入对象</summary>
        /// <param name="value">目标对象</param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public override Boolean Write(Object value, Type type)
        {
            if (value == null) return false;

            // 不支持基本类型
            if (Type.GetTypeCode(type) != TypeCode.Object) return false;

            // 获取成员
            foreach (var member in GetMembers(type))
            {
                var mtype = GetMemberType(member);

                var v = value.GetValue(member);
                if (!Host.Write(v, member.Name, mtype)) return false;
            }
            return true;
        }

        /// <summary>尝试读取</summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override bool TryRead(Type type, ref object value)
        {
            throw new NotImplementedException();
        }

        #region 获取成员
        /// <summary>获取成员</summary>
        /// <param name="type"></param>
        /// <returns></returns>
        protected virtual IEnumerable<MemberInfo> GetMembers(Type type) { return GetProperties(type).Cast<MemberInfo>(); }

        private static DictionaryCache<Type, List<PropertyInfo>> _cache1 = new DictionaryCache<Type, List<PropertyInfo>>();
        /// <summary>获取字段</summary>
        /// <param name="type"></param>
        /// <returns></returns>
        protected static List<PropertyInfo> GetProperties(Type type)
        {
            return _cache1.GetItem(type, key => GetGetProperties2(key));
        }

        static List<PropertyInfo> GetGetProperties2(Type type)
        {
            var list = new List<PropertyInfo>();

            if (type == typeof(Object)) return list;

            var pis = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty);
            foreach (var pi in pis)
            {
                if (pi.GetCustomAttribute<XmlIgnoreAttribute>() != null) continue;
                if (pi.GetIndexParameters().Length > 0) continue;

                list.Add(pi);
            }

            return list;
        }

        static Type GetMemberType(MemberInfo member)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Field:
                    return (member as FieldInfo).FieldType;
                case MemberTypes.Property:
                    return (member as PropertyInfo).PropertyType;
                default:
                    throw new NotSupportedException();
            }
        }
        #endregion
    }
}