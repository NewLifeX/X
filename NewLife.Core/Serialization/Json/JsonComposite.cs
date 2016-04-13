using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using NewLife.Collections;
using NewLife.Reflection;

namespace NewLife.Serialization
{
    /// <summary>复合对象处理器</summary>
    public class JsonComposite : JsonHandlerBase
    {
        /// <summary>要忽略的成员</summary>
        public ICollection<String> IgnoreMembers { get; set; }

        /// <summary>实例化</summary>
        public JsonComposite()
        {
            Priority = 100;

            //IgnoreMembers = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
            IgnoreMembers = new HashSet<String>();
        }

        /// <summary>获取对象的Json字符串表示形式。</summary>
        /// <param name="value"></param>
        /// <returns>返回null表示不支持</returns>
        public override String GetString(Object value)
        {
            if (value == null) return String.Empty;

            var type = value.GetType();
            if (type == typeof(Guid)) return ((Guid)value).ToString();
            if (type == typeof(Byte[])) return Convert.ToBase64String((Byte[])value);
            if (type == typeof(Char[])) return new String((Char[])value);

            switch (Type.GetTypeCode(value.GetType()))
            {
                case TypeCode.Boolean:
                    return value + "";
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Char:
                    return value + "";
                case TypeCode.DBNull:
                case TypeCode.Empty:
                    return String.Empty;
                case TypeCode.DateTime:
                    return value + "";
                case TypeCode.Decimal:
                    return value + "";
                case TypeCode.Single:
                case TypeCode.Double:
                    return value + "";
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return value + "";
                case TypeCode.String:
                    if (((String)value).IsNullOrEmpty()) return String.Empty;
                    return "\"{0}\"".F(value);
                case TypeCode.Object:
                default:
                    return null;
            }
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
            WriteLog("JsonWrite类{0} 共有成员{1}个", type.Name, ms.Count);

            Host.Hosts.Push(value);

            // 获取成员
            foreach (var member in ms)
            {
                if (IgnoreMembers != null && IgnoreMembers.Contains(member.Name)) continue;

                var mtype = GetMemberType(member);
                Host.Member = member;

                var v = value.GetValue(member);
                WriteLog("    {0}.{1} {2}", type.Name, member.Name, v);

                if (!Host.Write(v, mtype))
                {
                    Host.Hosts.Pop();
                    return false;
                }
            }
            Host.Hosts.Pop();

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
            WriteLog("JsonRead类{0} 共有成员{1}个", type.Name, ms.Count);

            if (value == null) value = type.CreateInstance();

            Host.Hosts.Push(value);

            // 成员序列化访问器
            var ac = value as IMemberAccessor;

            // 获取成员
            for (int i = 0; i < ms.Count; i++)
            {
                var member = ms[i];
                if (IgnoreMembers != null && IgnoreMembers.Contains(member.Name)) continue;

                var mtype = GetMemberType(member);
                Host.Member = member;
                WriteLog("    {0}.{1}", member.DeclaringType.Name, member.Name);

                // 成员访问器优先
                if (ac != null)
                {
                    // 访问器直接写入成员
                    if (ac.Read(Host, member))
                    {
                        // 访问器内部可能直接操作Hosts修改了父级对象，典型应用在于某些类需要根据某个字段值决定采用哪个派生类
                        var obj = Host.Hosts.Peek();
                        if (obj != value)
                        {
                            value = obj;
                            ms = GetMembers(value.GetType());
                            ac = value as IMemberAccessor;
                        }

                        continue;
                    }
                }

                Object v = null;
                if (!Host.TryRead(mtype, ref v))
                {
                    Host.Hosts.Pop();
                    return false;
                }

                value.SetValue(member, v);
            }
            Host.Hosts.Pop();

            return true;
        }

        #region 获取成员
        /// <summary>获取成员</summary>
        /// <param name="type"></param>
        /// <param name="baseFirst"></param>
        /// <returns></returns>
        protected virtual List<MemberInfo> GetMembers(Type type, Boolean baseFirst = true)
        {
            if (Host.UseProperty)
                return type.GetProperties(baseFirst).Cast<MemberInfo>().ToList();
            else
                return type.GetFields(baseFirst).Cast<MemberInfo>().ToList();
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