using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using NewLife.Collections;
using NewLife.Log;
using NewLife.Reflection;

namespace NewLife.Serialization
{
    /// <summary>复合对象处理器</summary>
    public class BinaryComposite : BinaryHandlerBase
    {
        /// <summary>实例化</summary>
        public BinaryComposite()
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
                //XTrace.WriteLine("{0} {1}", mtype.Name, member.Name);

                var v = value.GetValue(member);
                if (!Host.Write(v, mtype)) return false;
            }
            return true;
        }

        /// <summary>尝试读取指定类型对象</summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override Boolean TryRead(Type type, ref Object value)
        {
            return false;
        }

        #region 获取成员
        /// <summary>获取成员</summary>
        /// <param name="type"></param>
        /// <param name="baseFirst"></param>
        /// <returns></returns>
        protected virtual IEnumerable<MemberInfo> GetMembers(Type type, Boolean baseFirst = true) { return GetFields(type, baseFirst).Cast<MemberInfo>(); }

        private static DictionaryCache<Type, List<FieldInfo>> _cache1 = new DictionaryCache<Type, List<FieldInfo>>();
        private static DictionaryCache<Type, List<FieldInfo>> _cache2 = new DictionaryCache<Type, List<FieldInfo>>();
        /// <summary>获取字段</summary>
        /// <param name="type"></param>
        /// <param name="baseFirst"></param>
        /// <returns></returns>
        protected static List<FieldInfo> GetFields(Type type, Boolean baseFirst = true)
        {
            if (baseFirst)
                return _cache1.GetItem(type, key => GetFields2(key, true));
            else
                return _cache2.GetItem(type, key => GetFields2(key, false));
        }

        static List<FieldInfo> GetFields2(Type type, Boolean baseFirst = true)
        {
            var list = new List<FieldInfo>();

            if (type == typeof(Object)) return list;

            if (baseFirst) list.AddRange(GetFields(type.BaseType));

            var fis = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var fi in fis)
            {
                if (fi.GetCustomAttribute<NonSerializedAttribute>() != null) continue;

                list.Add(fi);
            }

            if (!baseFirst) list.AddRange(GetFields(type.BaseType));

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