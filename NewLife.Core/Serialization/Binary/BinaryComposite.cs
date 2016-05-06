using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NewLife.Reflection;

namespace NewLife.Serialization
{
    /// <summary>复合对象处理器</summary>
    public class BinaryComposite : BinaryHandlerBase
    {
        ///// <summary>要忽略的成员</summary>
        //public ICollection<String> IgnoreMembers { get; set; }

        /// <summary>实例化</summary>
        public BinaryComposite()
        {
            Priority = 100;

            //IgnoreMembers = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
            //IgnoreMembers = new HashSet<String>();
        }

        /// <summary>写入对象</summary>
        /// <param name="value">目标对象</param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public override Boolean Write(Object value, Type type)
        {
            // 不支持基本类型
            if (Type.GetTypeCode(type) != TypeCode.Object) return false;

            var ims = Host.IgnoreMembers;

            var ms = GetMembers(type).Where(e => !ims.Contains(e.Name)).ToList();
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

            // 如果不是第一层，这里开始必须写对象引用
            if (WriteRef(value)) return true;

            Host.Hosts.Push(value);

            // 位域偏移
            var offset = 0;
            var bit = 0;

            // 获取成员
            foreach (var member in ms)
            {
                //if (IgnoreMembers != null && IgnoreMembers.Contains(member.Name)) continue;

                var mtype = GetMemberType(member);
                Host.Member = member;

                var v = value.GetValue(member);
                WriteLog("    {0}.{1} {2}", type.Name, member.Name, v);

                // 处理位域支持，仅支持Byte
                if (member.GetMemberType() == typeof(Byte))
                {
                    if (WriteBit(member, ref bit, ref offset, ref v)) continue;
                }

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

        Boolean WriteRef(Object value)
        {
            if (Host.Hosts.Count == 0) return false;

            if (value == null)
            {
                Host.Write((Byte)0);
                return true;
            }

            // 找到对象索引，并写入
            var hs = Host.Hosts.ToArray();
            for (int i = 0; i < hs.Length; i++)
            {
                if (value == hs[i])
                {
                    Host.WriteSize(i + 1);
                    return true;
                }
            }

            // 埋下自己
            Host.WriteSize(Host.Hosts.Count + 1);

            return false;
        }

        Boolean WriteBit(MemberInfo member, ref Int32 bit, ref Int32 offset, ref Object v)
        {
            var att = member.GetCustomAttribute<BitSizeAttribute>();
            if (att != null)
            {
                // 合并位域数据
                bit = att.Set(bit, (Byte)v, offset);

                // 偏移
                offset += att.Size;

                // 不足8位，等下一次
                if (offset < 8) return true;

                // 足够8位，可以写入了，清空位移和bit给下一次使用
                v = (Byte)bit;
                offset = 0;
                bit = 0;
            }

            return false;
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

            var ims = Host.IgnoreMembers;

            var ms = GetMembers(type).Where(e => !ims.Contains(e.Name)).ToList();
            WriteLog("BinaryRead类{0} 共有成员{1}个", type.Name, ms.Count);

            // 读取对象引用
            if (ReadRef(ref value)) return true;

            if (value == null) value = type.CreateInstance();

            Host.Hosts.Push(value);

            // 位域偏移
            var offset = 0;
            var bit = 0;

            // 成员序列化访问器
            var ac = value as IMemberAccessor;

            // 获取成员
            for (int i = 0; i < ms.Count; i++)
            {
                var member = ms[i];
                //if (IgnoreMembers != null && IgnoreMembers.Contains(member.Name)) continue;

                var mtype = GetMemberType(member);
                Host.Member = member;
                WriteLog("    {0}.{1}", member.DeclaringType.Name, member.Name);

                // 处理位域支持，仅支持Byte
                if (member.GetMemberType() == typeof(Byte))
                {
                    if (TryReadBit(member, ref bit, ref offset, value)) continue;
                }

                // 成员访问器优先
                if (ac != null && TryReadAccessor(member, value, ref ac, ref ms)) continue;

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

        Boolean ReadRef(ref Object value)
        {
            if (Host.Hosts.Count == 0) return false;

            var rf = Host.ReadSize();
            if (rf == 0)
            {
                value = null;
                return true;
            }

            // 找到对象索引
            var hs = Host.Hosts.ToArray();
            // 如果引用是对象数加一，说明有对象紧跟着
            if (rf == hs.Length + 1) return false;

            if (rf < 0 || rf > hs.Length) throw new XException("无法在 {0} 个对象中找到引用 {1}", hs.Length, rf);

            value = hs[rf - 1];

            return true;
        }

        Boolean TryReadAccessor(MemberInfo member, Object value, ref IMemberAccessor ac, ref List<MemberInfo> ms)
        {
            // 访问器直接写入成员
            if (!ac.Read(Host, member)) return false;

            // 访问器内部可能直接操作Hosts修改了父级对象，典型应用在于某些类需要根据某个字段值决定采用哪个派生类
            var obj = Host.Hosts.Peek();
            if (obj != value)
            {
                value = obj;
                ms = GetMembers(value.GetType());
                ac = value as IMemberAccessor;
            }

            return true;
        }

        Boolean TryReadBit(MemberInfo member, ref Int32 bit, ref Int32 offset, Object value)
        {
            var att = member.GetCustomAttribute<BitSizeAttribute>();
            if (att == null) return false;

            // 仅在第一个位移处读取
            if (offset == 0)
            {
                var mtype = GetMemberType(member);
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