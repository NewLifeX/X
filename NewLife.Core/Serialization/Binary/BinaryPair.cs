using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using NewLife.Collections;
using NewLife.Reflection;

namespace NewLife.Serialization
{
    /// <summary>二进制名值对</summary>
    public class BinaryPair : BinaryHandlerBase
    {
        #region 构造
        /// <summary>初始化</summary>
        public BinaryPair()
        {
            // 优先级
            Priority = 15;
        }
        #endregion

        #region 核心方法
        /// <summary>写入一个对象</summary>
        /// <param name="value">目标对象</param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public override Boolean Write(Object value, Type type)
        {
            // 不支持基本类型
            if (Type.GetTypeCode(type) != TypeCode.Object) return false;

            // 不写空名值对
            if (value == null) return true;

            //todo 名值对还不能很好的支持数组
            if (WriteDictionary(value, type)) return true;
            if (WriteArray(value, type)) return true;
            if (WriteObject(value, type)) return true;

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

            if (TryReadDictionary(type, ref value)) return true;
            if (TryReadArray(type, ref value)) return true;
            if (TryReadObject(type, ref value)) return true;

            return false;
        }
        #endregion

        #region 原始读写名值对
        /// <summary>写入名值对</summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public Boolean WritePair(String name, Object value)
        {
            if (value == null) return true;

            var host = Host;
            // 检测循环引用。名值对不支持循环引用
            var hs = host.Hosts.ToArray();
            if (hs.Contains(value)) return true;

            var type = value.GetType();

            Byte[] buf = null;
            if (value is String)
                buf = (value as String).GetBytes(host.Encoding);
            else if (value is Byte[])
                buf = (Byte[])value;
            else
            {
                // 准备好名值对再一起写入。为了得到数据长度，需要提前计算好数据长度，所以需要临时切换数据流
                var ms = Pool.MemoryStream.Get();
                var old = host.Stream;
                host.Stream = ms;
                var rs = host.Write(value, type);
                host.Stream = old;

                if (!rs) return false;
                buf = ms.Put(true);
            }

            WriteLog("    WritePair {0}\t= {1}", name, value);

            // 开始写入
            var key = name.GetBytes(host.Encoding);
            if (!host.Write(key, key.GetType())) return false;
            if (!host.Write(buf, buf.GetType())) return false;

            return true;
        }

        /// <summary>读取原始名值对</summary>
        /// <returns></returns>
        public IDictionary<String, Byte[]> ReadPair() => ReadPair(Host.Stream, Host.Encoding);

        /// <summary>读取原始名值对</summary>
        /// <param name="ms">数据流</param>
        /// <param name="encoding">编码</param>
        /// <returns></returns>
        public static IDictionary<String, Byte[]> ReadPair(Stream ms, Encoding encoding = null)
        {
            var dic = new Dictionary<String, Byte[]>();
            while (ms.Position < ms.Length)
            {
                var len = ms.ReadEncodedInt();
                if (len > ms.Length - ms.Position) break;

                var name = ms.ReadBytes(len).ToStr(encoding);
                // 避免名称为空导致dic[name]报错
                name += "";

                len = ms.ReadEncodedInt();
                if (len > ms.Length - ms.Position) break;

                dic[name] = ms.ReadBytes(len);
            }

            return dic;
        }

        /// <summary>从原始名值对读取数据</summary>
        /// <param name="dic"></param>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Boolean TryReadPair(IDictionary<String, Byte[]> dic, String name, Type type, ref Object value)
        {
            if (!dic.TryGetValue(name, out var buf)) return false;

            if (type == null)
            {
                if (value == null) return false;

                type = value.GetType();
            }

            WriteLog("    TryReadPair {0}\t= {1}", name, buf.ToHex("-", 0, 32));

            if (type == typeof(String))
            {
                value = buf.ToStr(Host.Encoding);
                WriteLog("        " + value + "");
                return true;
            }
            if (type == typeof(Byte[]))
            {
                value = buf;
                return true;
            }

            var old = Host.Stream;
            Host.Stream = new MemoryStream(buf);
            try
            {
                return Host.TryRead(type, ref value);
            }
            finally
            {
                Host.Stream = old;
                WriteLog("        {0}".F(value));
            }
        }
        #endregion

        #region 字典名值对
        private Boolean WriteDictionary(Object value, Type type)
        {
            if (!type.As<IDictionary>()) return false;

            var dic = value as IDictionary;

            var gs = type.GetGenericArguments();
            if (gs.Length != 2) throw new NotSupportedException("字典类型仅支持 {0}".F(typeof(Dictionary<,>).FullName));
            if (gs[0] != typeof(String)) throw new NotSupportedException("字典类型仅支持Key=String的名值对");

            foreach (DictionaryEntry item in dic)
            {
                WritePair(item.Key as String, item.Value);
            }

            return true;
        }

        private Boolean TryReadDictionary(Type type, ref Object value)
        {
            if (!type.As<IDictionary>()) return false;

            // 子元素类型
            var gs = type.GetGenericArguments();
            if (gs.Length != 2) throw new NotSupportedException("字典类型仅支持 {0}".F(typeof(Dictionary<,>).FullName));

            var keyType = gs[0];
            var valType = gs[1];

            // 创建字典
            if (value == null && type != null)
            {
                value = type.CreateInstance();
            }

            var dic = value as IDictionary;

            if (keyType != typeof(String)) throw new NotSupportedException("字典类型仅支持Key=String的名值对");

            var ds = ReadPair();
            foreach (var item in ds)
            {
                Object v = null;
                if (TryReadPair(ds, item.Key, valType, ref v))
                    dic[item.Key] = v;
            }

            return true;
        }
        #endregion

        #region 数组名值对
        private Boolean WriteArray(Object value, Type type)
        {
            if (!type.As<IList>()) return false;

            var list = value as IList;
            if (list == null || list.Count == 0) return true;

            // 循环写入数据
            for (var i = 0; i < list.Count; i++)
            {
                WritePair(i + "", list[i]);
            }

            return true;
        }

        private Boolean TryReadArray(Type type, ref Object value)
        {
            if (!type.As<IList>()) return false;

            // 子元素类型
            var elmType = type.GetElementTypeEx();

            var list = typeof(List<>).MakeGenericType(elmType).CreateInstance() as IList;

            var ds = ReadPair();
            for (var i = 0; i < ds.Count; i++)
            {
                Object v = null;
                if (TryReadPair(ds, i + "", elmType, ref v)) list.Add(v);
            }

            // 数组的创建比较特别
            if (type.IsArray)
            {
                var arr = Array.CreateInstance(elmType, list.Count);
                list.CopyTo(arr, 0);
                value = arr;
            }
            else
                value = list;

            return true;
        }
        #endregion

        #region 复杂对象名值对
        private Boolean WriteObject(Object value, Type type)
        {
            var ims = Host.IgnoreMembers;

            var ms = GetMembers(type).Where(e => !ims.Contains(e.Name)).ToList();
            WriteLog("BinaryWrite类{0} 共有成员{1}个", type.Name, ms.Count);

            Host.Hosts.Push(value);

            // 获取成员
            foreach (var member in ms)
            {
                var mtype = GetMemberType(member);
                Host.Member = member;

                var v = value.GetValue(member);
                WriteLog("    {0}.{1} {2}", type.Name, member.Name, v);

                var name = member.Name;
                var att = member.GetCustomAttribute<XmlElementAttribute>();
                if (att != null) name = att.ElementName;

                // 特殊处理写入名值对
                if (!WritePair(name, v))
                {
                    Host.Hosts.Pop();
                    return false;
                }
            }
            Host.Hosts.Pop();

            return true;
        }

        private Boolean TryReadObject(Type type, ref Object value)
        {
            // 不支持基本类型
            if (Type.GetTypeCode(type) != TypeCode.Object) return false;
            // 不支持基类不是Object的特殊类型
            //if (type.BaseType != typeof(Object)) return false;
            //if (!type.As<Object>()) return false;
            if (!typeof(Object).IsAssignableFrom(type)) return false;

            var ims = Host.IgnoreMembers;

            var ms = GetMembers(type).Where(e => !ims.Contains(e.Name)).ToList();
            WriteLog("BinaryRead类{0} 共有成员{1}个", type.Name, ms.Count);

            if (value == null) value = type.CreateInstance();

            // 提前准备名值对
            var dic = ReadPair();
            if (dic.Count == 0) return true;

            Host.Hosts.Push(value);

            // 获取成员
            for (var i = 0; i < ms.Count; i++)
            {
                var member = ms[i];

                var mtype = GetMemberType(member);
                Host.Member = member;
                WriteLog("    {0}.{1}", member.DeclaringType.Name, member.Name);

                var name = member.Name;
                var att = member.GetCustomAttribute<XmlElementAttribute>();
                if (att != null) name = att.ElementName;

                Object v = null;
                if (TryReadPair(dic, name, mtype, ref v)) value.SetValue(member, v);
            }
            Host.Hosts.Pop();

            return true;
        }
        #endregion

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