using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Serialization;
using NewLife.Collections;
using NewLife.Reflection;

namespace NewLife.IO
{
    /// <summary>
    /// 二进制数据访问器
    /// </summary>
    public class BinaryAccessor : FastIndexAccessor, IBinaryAccessor
    {
        #region 读写
        /// <summary>
        /// 从读取器中读取数据到对象
        /// </summary>
        /// <param name="reader"></param>
        public virtual void Read(BinaryReaderX reader)
        {
            Read(this, reader, true, true, false);
        }

        /// <summary>
        /// 从读取器中读取数据到对象，指定读取属性还是字段
        /// </summary>
        /// <param name="target">目标</param>
        /// <param name="reader">读取器</param>
        /// <param name="encodeInt">使用7Bit编码整数</param>
        /// <param name="allowNull">是否允许对象为空，如果允许，则写入时增加一个字节表示对象是否为空</param>
        /// <param name="isProperty"></param>
        protected void Read(Object target, BinaryReaderX reader, Boolean encodeInt, Boolean allowNull, Boolean isProperty)
        {
            if (target == null) throw new ArgumentNullException("target", "目标对象不能为空！");

            if (isProperty)
            {
                PropertyInfo[] pis = FindProperties(target.GetType());
                if (pis == null || pis.Length < 1) return;

                foreach (PropertyInfo item in pis)
                {
                    ReadMember(target, reader, item, encodeInt, allowNull);
                }
            }
            else
            {
                FieldInfo[] fis = FindFields(target.GetType());
                if (fis == null || fis.Length < 1) return;

                foreach (FieldInfo item in fis)
                {
                    ReadMember(target, reader, item, encodeInt, allowNull);
                }
            }
        }

        /// <summary>
        /// 从读取器中读取数据到对象的成员中
        /// </summary>
        /// <param name="target">目标</param>
        /// <param name="reader">读取器</param>
        /// <param name="member">成员</param>
        /// <param name="encodeInt">使用7Bit编码整数</param>
        /// <param name="allowNull">是否允许对象为空，如果允许，则读取时先读取一个字节判断对象是否为空</param>
        protected virtual void ReadMember(Object target, BinaryReaderX reader, MemberInfoX member, Boolean encodeInt, Boolean allowNull)
        {
            Object value = null;
            MemberTypes mt = member.Member.MemberType;
            if (mt == MemberTypes.Field || mt == MemberTypes.Property)
            {
                if (!TryRead(target, reader, member, encodeInt, allowNull, out value)) throw new InvalidOperationException("无法读取数据，如果是复杂类型请实现IBinaryAccessor接口。");
                member.SetValue(target, value);
            }
            else
                throw new ArgumentOutOfRangeException("member", "成员只能是FieldInfo或PropertyInfo。");
        }

        Boolean TryRead(Object target, BinaryReaderX reader, MemberInfoX member, Boolean encodeInt, Boolean allowNull, out Object value)
        {
            Type type = member.Type;

            // 基本类型
            if (reader.TryReadValue(type, encodeInt, out value)) return true;

            // 允许空时，先读取一个字节判断对象是否为空
            if (allowNull && reader.ReadByte() == 0) return true;

            #region 接口支持
            //if (Array.IndexOf(member.Type.GetInterfaces(), typeof(IBinaryAccessor)) >= 0)
            if (typeof(IBinaryAccessor).IsAssignableFrom(member.Type))
            {
                // 读取对象
                Object obj = member.GetValue(target);

                // 实例化对象
                if (obj == null)
                {
                    obj = Activator.CreateInstance(type);
                    member.SetValue(target, obj);
                }
                if (obj == null) return false;

                // 调用接口
                IBinaryAccessor accessor = obj as IBinaryAccessor;
                accessor.Read(reader);

                return true;
            }
            #endregion

            #region 枚举
            if (typeof(IEnumerable).IsAssignableFrom(member.Type))
            {
                // 先读元素个数
                Int32 count = reader.ReadEncodedInt32();
                if (count < 0) throw new InvalidOperationException("无效元素个数" + count + "！");

                Type elementType = member.Type;
                if (member.Type.HasElementType)
                    elementType = member.Type.GetElementType();
                else if (member.Type.IsGenericType)
                {
                    Type[] ts = member.Type.GetGenericArguments();
                    if (ts != null && ts.Length > 0) elementType = ts[0];
                }

                Array arr = Array.CreateInstance(elementType, count);

                for (int i = 0; i < count; i++)
                {
                    if (allowNull && reader.ReadEncodedInt32() == 0) continue;

                    Object obj = null;
                    if (!reader.TryReadValue(elementType, encodeInt, out obj))
                    {
                        obj = CreateInstance(elementType);
                        Read(obj, reader, encodeInt, allowNull, member.Member.MemberType == MemberTypes.Property);
                    }
                    arr.SetValue(obj, i);
                }

                value = Activator.CreateInstance(member.Type, arr);
                return true;
            }
            #endregion

            return false;
        }

        /// <summary>
        /// 创建指定类型的实例
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        protected virtual Object CreateInstance(Type type)
        {
            return Activator.CreateInstance(type);
        }

        /// <summary>
        /// 把对象数据写入到写入器
        /// </summary>
        /// <param name="writer"></param>
        public virtual void Write(BinaryWriterX writer)
        {
            Write(this, writer, true, true, false);
        }

        /// <summary>
        /// 把指定对象写入到写入器，指定写入属性还是字段
        /// </summary>
        /// <param name="target">目标对象</param>
        /// <param name="writer">写入器</param>
        /// <param name="encodeInt">使用7Bit编码整数</param>
        /// <param name="allowNull">是否允许对象为空，如果允许，则写入时增加一个字节表示对象是否为空</param>
        /// <param name="isProperty"></param>
        protected void Write(Object target, BinaryWriterX writer, Boolean encodeInt, Boolean allowNull, Boolean isProperty)
        {
            if (target == null) throw new ArgumentNullException("target", "目标对象不能为空！");

            if (isProperty)
            {
                PropertyInfo[] pis = FindProperties(target.GetType());
                if (pis == null || pis.Length < 1) return;

                foreach (PropertyInfo item in pis)
                {
                    WriteMember(target, writer, item, encodeInt, allowNull);
                }
            }
            else
            {
                FieldInfo[] fis = FindFields(target.GetType());
                if (fis == null || fis.Length < 1) return;

                foreach (FieldInfo item in fis)
                {
                    WriteMember(target, writer, item, encodeInt, allowNull);
                }
            }
        }

        /// <summary>
        /// 把对象成员的数据写入到写入器
        /// </summary>
        /// <param name="target">目标对象</param>
        /// <param name="writer">写入器</param>
        /// <param name="member">成员</param>
        /// <param name="encodeInt">使用7Bit编码整数</param>
        /// <param name="allowNull">是否允许对象为空，如果允许，则写入时增加一个字节表示对象是否为空</param>
        protected virtual void WriteMember(Object target, BinaryWriterX writer, MemberInfoX member, Boolean encodeInt, Boolean allowNull)
        {
            MemberTypes mt = member.Member.MemberType;
            if (mt == MemberTypes.Field || mt == MemberTypes.Property)
            {
                if (!TryWrite(target, writer, member, encodeInt, allowNull)) throw new InvalidOperationException("无法读取数据，如果是复杂类型请实现IBinaryAccessor接口。");
            }
            else
                throw new ArgumentOutOfRangeException("member", "成员只能是FieldInfo或PropertyInfo。");
        }

        Boolean TryWrite(Object target, BinaryWriterX writer, MemberInfoX member, Boolean encodeInt, Boolean allowNull)
        {
            Object value = member.GetValue(target);

            // 基本类型
            if (writer.WriteValue(value, encodeInt)) return true;

            // 允许空时，增加一个字节表示对象是否为空
            if (value == null)
            {
                if (allowNull) writer.Write((Byte)0);
                return true;
            }
            if (allowNull) writer.Write((Byte)1);

            // 接口支持
            //if (Array.IndexOf(member.Type.GetInterfaces(), typeof(IBinaryAccessor)) >= 0)
            if (typeof(IBinaryAccessor).IsAssignableFrom(member.Type))
            {
                // 调用接口
                IBinaryAccessor accessor = value as IBinaryAccessor;
                accessor.Write(writer);
                return true;
            }

            #region 数组
            if (member.Type.IsArray)
            {
                // 特殊处理字节数组
                if (member.Type == typeof(Byte[]))
                {
                    writer.Write((Byte[])value);
                    return true;
                }

                Array arr = value as Array;
                if (arr != null)
                {

                }
            }
            #endregion

            #region 枚举
            if (typeof(IEnumerable).IsAssignableFrom(member.Type))
            {
                // 先写元素个数
                IEnumerable arr = value as IEnumerable;
                Int32 count = 0;
                foreach (Object item in arr)
                {
                    count++;
                }
                // 写入长度
                writer.WriteEncoded(count);

                foreach (Object item in arr)
                {
                    // 允许空时，增加一个字节表示对象是否为空
                    if (item == null)
                    {
                        if (allowNull) writer.Write((Byte)0);
                        continue;
                    }
                    if (allowNull) writer.Write((Byte)1);

                    if (!writer.WriteValue(item, encodeInt))
                        Write(item, writer, encodeInt, allowNull, member.Member.MemberType == MemberTypes.Property);
                }
                return true;
            }
            #endregion

            return false;
        }
        #endregion

        #region 辅助
        static DictionaryCache<Type, FieldInfo[]> cache1 = new DictionaryCache<Type, FieldInfo[]>();
        /// <summary>
        /// 取得所有可序列化字段
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        protected static FieldInfo[] FindFields(Type type)
        {
            if (type == null) return null;

            return cache1.GetItem(type, delegate(Type t)
            {
                List<FieldInfo> list = new List<FieldInfo>();

                // GetFields只能取得本类的字段，没办法取得基类的字段
                FieldInfo[] fis = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (fis != null && fis.Length > 0)
                {
                    foreach (FieldInfo item in fis)
                    {
                        //if (item.IsDefined(typeof(NonSerializedAttribute), true)) continue;
                        if (Attribute.IsDefined(item, typeof(NonSerializedAttribute))) continue;
                        list.Add(item);
                    }
                }

                // 递归取父级的字段
                if (type.BaseType != null && type.BaseType != typeof(Object))
                {
                    FieldInfo[] fis2 = FindFields(type.BaseType);
                    if (fis2 != null) list.AddRange(fis2);
                }

                if (list == null || list.Count < 1) return null;
                return list.ToArray();
            });
        }

        static DictionaryCache<Type, PropertyInfo[]> cache2 = new DictionaryCache<Type, PropertyInfo[]>();
        /// <summary>
        /// 取得所有属性
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        protected static PropertyInfo[] FindProperties(Type type)
        {
            if (type == null) return null;

            return cache2.GetItem(type, delegate(Type t)
            {
                PropertyInfo[] pis = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (pis == null || pis.Length < 1) return null;

                List<PropertyInfo> list = new List<PropertyInfo>();
                foreach (PropertyInfo item in pis)
                {
                    if (Attribute.IsDefined(item, typeof(NonSerializedAttribute))) continue;
                    // 属性没办法用NonSerializedAttribute
                    if (Attribute.IsDefined(item, typeof(XmlIgnoreAttribute))) continue;
                    list.Add(item);
                }
                if (list == null || list.Count < 1) return null;
                return list.ToArray();
            });
        }
        #endregion
    }
}
