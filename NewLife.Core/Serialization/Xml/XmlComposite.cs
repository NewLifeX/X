using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
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

            var ms = GetMembers(type);
            WriteLog("XmlWrite {0} 成员{1}个", type.Name, ms.Count);

            Host.Hosts.Push(value);

            //var xml = Host as Xml;
            //xml.WriteStart(type);
            try
            {
                // 获取成员
                foreach (var member in GetMembers(type))
                {
                    var mtype = GetMemberType(member);
                    Host.Member = member;

                    var name = SerialHelper.GetName(member);
                    var v = value.GetValue(member);
                    WriteLog("    {0}.{1} {2}", type.Name, member.Name, v);

                    if (!Host.Write(v, name, mtype)) return false;
                }
            }
            finally
            {
                //xml.WriteEnd();

                Host.Hosts.Pop();
            }

            return true;
        }

        /// <summary>尝试读取</summary>
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
            if (!type.As<Object>()) return false;

            var reader = Host.GetReader();
            var xml = Host as Xml;

            // 判断类名是否一致
            var name = xml.CurrentName;
            if (!CheckName(name, type)) return false;

            var ms = GetMembers(type);
            WriteLog("XmlRead {0} 成员{1}个", type.Name, ms.Count);
            var dic = ms.ToDictionary(e => SerialHelper.GetName(e), e => e);

            if (value == null) value = type.CreateInstance();

            Host.Hosts.Push(value);

            try
            {
                if (reader.NodeType == XmlNodeType.Attribute)
                {
                    foreach (var item in dic)
                    {
                        var member = item.Value;
                        var v = reader.GetAttribute(item.Key);
                        WriteLog("    {0}.{1} {2}", type.Name, member.Name, v);

                        value.SetValue(member, v);
                    }
                }
                else
                {
                    // 获取成员
                    var member = ms[0];
                    while (reader.NodeType != XmlNodeType.None && reader.IsStartElement())
                    {
                        // 找到匹配的元素，否则跳过
                        if (!dic.TryGetValue(reader.Name, out member) || !member.CanWrite)
                        {
                            reader.Skip();
                            continue;
                        }

                        var mtype = GetMemberType(member);
                        Host.Member = member;

                        var v = value.GetValue(member);
                        WriteLog("    {0}.{1} {2}", type.Name, member.Name, v);

                        if (!Host.TryRead(mtype, ref v)) return false;

                        value.SetValue(member, v);
                    }
                }
            }
            finally
            {
                Host.Hosts.Pop();
            }

            return true;
        }

        #region 辅助
        private Boolean CheckName(String name, Type type)
        {
            if (type.Name.EqualIgnoreCase(name)) return true;

            // 当前正在序列化的成员
            var mb = Host.Member;
            if (mb != null)
            {
                var elm = mb.GetCustomAttribute<XmlElementAttribute>();
                if (elm != null) return elm.ElementName.EqualIgnoreCase(name);

                if (mb.Name.EqualIgnoreCase(name)) return true;
            }

            // 检查类型的Root
            var att = type.GetCustomAttribute<XmlRootAttribute>();
            if (att != null) return att.ElementName.EqualIgnoreCase(name);

            return false;
        }
        #endregion

        #region 获取成员
        /// <summary>获取成员</summary>
        /// <param name="type"></param>
        /// <returns></returns>
        protected virtual List<PropertyInfo> GetMembers(Type type) { return type.GetProperties(true).Cast<PropertyInfo>().ToList(); }

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