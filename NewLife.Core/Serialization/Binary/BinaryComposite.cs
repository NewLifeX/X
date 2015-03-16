using System;
using System.Collections.Generic;
using System.Linq;
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

            var ms = GetMembers(type);
            WriteLog("BinaryWrite类{0} 共有成员{1}个", type.Name, ms.Count);

            if (Host.UseFieldSize)
            {
                // 遍历成员，寻找FieldSizeAttribute特性，重新设定大小字段的值
                foreach (var member in ms)
                {
                    // 获取FieldSizeAttribute特性
                    var att = member.GetCustomAttribute<FieldSizeAttribute>();
                    if (att != null) att.SetReferenceSize(value, member, Host.Encoding);
                }
            }

            Host.Hosts.Push(value);

            // 位域偏移
            var offset = 0;
            var bit = 0;

            // 获取成员
            foreach (var member in ms)
            {
                var mtype = GetMemberType(member);
                Host.Member = member;

                var v = value.GetValue(member);
                WriteLog("    {0}.{1} {2}", type.Name, member.Name, v);

                #region 处理位域支持
                // 仅支持Byte
                if (member.GetMemberType() == typeof(Byte))
                {
                    var att = member.GetCustomAttribute<BitSizeAttribute>();
                    if (att != null)
                    {
                        // 合并位域数据
                        bit = att.Set(bit, (Byte)v, offset);

                        // 偏移
                        offset += att.Size;

                        // 不足8位，等下一次
                        if (offset < 8) continue;

                        // 足够8位，可以写入了，清空位移和bit给下一次使用
                        v = (Byte)bit;
                        offset = 0;
                        bit = 0;
                    }
                }
                #endregion

                if (!Host.Write(v, mtype))
                {
                    Host.Hosts.Pop();
                    return false;
                }
            }
            Host.Hosts.Pop();

            if (offset > 0) throw new XException("类{0}的位域字段不足8位", type);

            return true;
        }

        /// <summary>尝试读取指定类型对象</summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override Boolean TryRead(Type type, ref Object value)
        {
            if (type == null)
            {
                if (value == null) return false;
                type = value.GetType();
            }

            // 不支持基本类型
            if (Type.GetTypeCode(type) != TypeCode.Object) return false;
            // 不支持基类不是Object的特殊类型
            //if (type.BaseType != typeof(Object)) return false;
            if (!typeof(Object).IsAssignableFrom(type)) return false;

            var ms = GetMembers(type);
            WriteLog("BinaryRead类{0} 共有成员{1}个", type.Name, ms.Count);

            if (value == null) value = type.CreateInstance();

            Host.Hosts.Push(value);

            // 位域偏移
            var offset = 0;
            var bit = 0;

            // 获取成员
            foreach (var member in ms)
            {
                var mtype = GetMemberType(member);
                Host.Member = member;
                WriteLog("    {0}.{1}", type.Name, member.Name);

                #region 处理位域支持
                // 仅支持Byte
                if (member.GetMemberType() == typeof(Byte))
                {
                    var att = member.GetCustomAttribute<BitSizeAttribute>();
                    if (att != null)
                    {
                        // 仅在第一个位移处读取
                        if (offset == 0)
                        {
                            Object v2 = null;
                            if (!Host.TryRead(mtype, ref v2))
                            {
                                Host.Hosts.Pop();
                                return false;
                            }
                            bit = (Byte)v2;
                        }

                        // 取得当前字段所属部分
                        var n = att.Get(bit, offset);

                        value.SetValue(member, (Byte)n);

                        // 偏移
                        offset += att.Size;

                        // 足够8位，可以写入了，清空位移和bit给下一次使用
                        if (offset >= 8)
                        {
                            offset = 0;
                            bit = 0;
                        }

                        continue;
                    }
                }
                #endregion

                Object v = null;
                if (!Host.TryRead(mtype, ref v))
                {
                    Host.Hosts.Pop();
                    return false;
                }

                value.SetValue(member, v);
            }
            Host.Hosts.Pop();

            if (offset > 0) throw new XException("类{0}的位域字段不足8位", type);

            return true;
        }

        #region 获取成员
        /// <summary>获取成员</summary>
        /// <param name="type"></param>
        /// <param name="baseFirst"></param>
        /// <returns></returns>
        protected virtual List<MemberInfo> GetMembers(Type type, Boolean baseFirst = true) { return GetFields(type, baseFirst).Cast<MemberInfo>().ToList(); }

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

            // Void*的基类就是null
            if (type == typeof(Object) || type.BaseType == null) return list;

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