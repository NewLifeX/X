using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Serialization;
using NewLife.Collections;
using NewLife.Reflection;
using System.IO;

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
            //Read(this, reader, true, true, false);
            Object value = null;
            reader.TryReadObject(this, TypeX.Create(this.GetType()), true, false, false, out value, ReadMember);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="target"></param>
        /// <param name="member"></param>
        /// <param name="encodeInt"></param>
        /// <param name="allowNull"></param>
        /// <param name="isProperty"></param>
        /// <param name="value"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        protected virtual Boolean ReadMember(BinaryReaderX reader, Object target, MemberInfoX member, Boolean encodeInt, Boolean allowNull, Boolean isProperty, out Object value, BinaryReaderX.ReadCallback callback)
        {
            // 接口支持
            //if (Array.IndexOf(member.Type.GetInterfaces(), typeof(IBinaryAccessor)) >= 0)
            if (typeof(IBinaryAccessor).IsAssignableFrom(member.Type))
            {
                // 读取对象
                value = member.GetValue(target);

                // 实例化对象
                if (value == null)
                {
                    //value = Activator.CreateInstance(member.Type);
                    value = TypeX.CreateInstance(member.Type);
                    //member.SetValue(target, value);
                }
                if (value == null) return false;

                // 调用接口
                IBinaryAccessor accessor = value as IBinaryAccessor;
                accessor.Read(reader);

                return true;
            }

            return reader.TryReadObject(target, member, encodeInt, true, isProperty, out value, callback);
        }

        //        /// <summary>
        //        /// 从读取器中读取数据到对象，指定读取属性还是字段
        //        /// </summary>
        //        /// <param name="target">目标</param>
        //        /// <param name="reader">读取器</param>
        //        /// <param name="encodeInt">使用7Bit编码整数</param>
        //        /// <param name="allowNull">是否允许对象为空，如果允许，则写入时增加一个字节表示对象是否为空</param>
        //        /// <param name="isProperty"></param>
        //        protected void Read(Object target, BinaryReaderX reader, Boolean encodeInt, Boolean allowNull, Boolean isProperty)
        //        {
        //            if (target == null) throw new ArgumentNullException("target", "目标对象不能为空！");

        //            if (isProperty)
        //            {
        //                PropertyInfo[] pis = FindProperties(target.GetType());
        //                if (pis == null || pis.Length < 1) return;

        //                foreach (PropertyInfo item in pis)
        //                {
        //                    ReadMember(target, reader, item, encodeInt, allowNull);
        //                }
        //            }
        //            else
        //            {
        //                FieldInfo[] fis = FindFields(target.GetType());
        //                if (fis == null || fis.Length < 1) return;

        //                foreach (FieldInfo item in fis)
        //                {
        //#if DEBUG
        //                    long p = reader.BaseStream.Position;
        //                    Console.Write("{0}：", item.Name);
        //#endif
        //                    ReadMember(target, reader, item, encodeInt, allowNull);
        //#if DEBUG
        //                    long p2 = reader.BaseStream.Position;
        //                    if (p2 > p)
        //                    {
        //                        reader.BaseStream.Seek(p, SeekOrigin.Begin);
        //                        Byte[] data = new Byte[p2 - p];
        //                        reader.BaseStream.Read(data, 0, data.Length);
        //                        Console.WriteLine(BitConverter.ToString(data));
        //                    }
        //#endif
        //                }
        //            }
        //        }

        ///// <summary>
        ///// 从读取器中读取数据到对象的成员中
        ///// </summary>
        ///// <param name="target">目标</param>
        ///// <param name="reader">读取器</param>
        ///// <param name="member">成员</param>
        ///// <param name="encodeInt">使用7Bit编码整数</param>
        ///// <param name="allowNull">是否允许对象为空，如果允许，则读取时先读取一个字节判断对象是否为空</param>
        //protected virtual void ReadMember(Object target, BinaryReaderX reader, MemberInfoX member, Boolean encodeInt, Boolean allowNull)
        //{
        //    Object value = null;
        //    MemberTypes mt = member.Member.MemberType;
        //    if (mt == MemberTypes.Field || mt == MemberTypes.Property)
        //    {
        //        if (!TryRead(target, reader, member, encodeInt, allowNull, out value)) throw new InvalidOperationException("无法读取数据，如果是复杂类型请实现IBinaryAccessor接口。");
        //        member.SetValue(target, value);
        //    }
        //    else
        //        throw new ArgumentOutOfRangeException("member", "成员只能是FieldInfo或PropertyInfo。");
        //}

        //Boolean TryRead(Object target, BinaryReaderX reader, MemberInfoX member, Boolean encodeInt, Boolean allowNull, out Object value)
        //{
        //    Type type = member.Type;

        //    // 基本类型
        //    if (reader.TryReadValue(type, encodeInt, out value)) return true;

        //    // 允许空时，先读取一个字节判断对象是否为空
        //    if (allowNull && reader.ReadByte() == 0) return true;

        //    #region 接口支持
        //    //if (Array.IndexOf(member.Type.GetInterfaces(), typeof(IBinaryAccessor)) >= 0)
        //    if (typeof(IBinaryAccessor).IsAssignableFrom(member.Type))
        //    {
        //        // 读取对象
        //        Object obj = member.GetValue(target);

        //        // 实例化对象
        //        if (obj == null)
        //        {
        //            obj = Activator.CreateInstance(type);
        //            member.SetValue(target, obj);
        //        }
        //        if (obj == null) return false;

        //        // 调用接口
        //        IBinaryAccessor accessor = obj as IBinaryAccessor;
        //        accessor.Read(reader);

        //        return true;
        //    }
        //    #endregion

        //    #region 枚举
        //    if (typeof(IEnumerable).IsAssignableFrom(member.Type))
        //    {
        //        // 先读元素个数
        //        Int32 count = reader.ReadEncodedInt32();
        //        if (count < 0) throw new InvalidOperationException("无效元素个数" + count + "！");

        //        Type elementType = member.Type;
        //        if (member.Type.HasElementType)
        //            elementType = member.Type.GetElementType();
        //        else if (member.Type.IsGenericType)
        //        {
        //            Type[] ts = member.Type.GetGenericArguments();
        //            if (ts != null && ts.Length > 0) elementType = ts[0];
        //        }

        //        Array arr = Array.CreateInstance(elementType, count);

        //        for (int i = 0; i < count; i++)
        //        {
        //            if (allowNull && reader.ReadEncodedInt32() == 0) continue;

        //            Object obj = null;
        //            if (!reader.TryReadValue(elementType, encodeInt, out obj))
        //            {
        //                obj = CreateInstance(elementType);
        //                Read(obj, reader, encodeInt, allowNull, member.Member.MemberType == MemberTypes.Property);
        //            }
        //            arr.SetValue(obj, i);
        //        }

        //        value = Activator.CreateInstance(member.Type, arr);
        //        return true;
        //    }
        //    #endregion

        //    return false;
        //}

        /// <summary>
        /// 创建指定类型的实例
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        protected virtual Object CreateInstance(Type type)
        {
            //return Activator.CreateInstance(type);
            return TypeX.CreateInstance(type);
        }

        /// <summary>
        /// 把对象数据写入到写入器
        /// </summary>
        /// <param name="writer"></param>
        public virtual void Write(BinaryWriterX writer)
        {
            //Write(this, writer, true, true, false);
            writer.WriteObject(this, TypeX.Create(this.GetType()), true, false, false, WriteMember);
        }

        /// <summary>
        /// 把对象写入数据流，处理基础类型、特殊类型、基础类型数组、特殊类型数组，通过委托方法处理成员
        /// </summary>
        /// <param name="writer">写入器</param>
        /// <param name="target">目标对象</param>
        /// <param name="member">成员</param>
        /// <param name="encodeInt">是否编码整数</param>
        /// <param name="allowNull">是否允许空</param>
        /// <param name="isProperty">是否处理属性</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否写入成功</returns>
        protected virtual Boolean WriteMember(BinaryWriterX writer, Object target, MemberInfoX member, Boolean encodeInt, Boolean allowNull, Boolean isProperty, BinaryWriterX.WriteCallback callback)
        {
            Type type = member.Type;
            Object value = member.IsType ? target : member.GetValue(target);
            //if (member.Member.MemberType == MemberTypes.TypeInfo)
            //    value = target;
            //else
            //    value = member.GetValue(target);

            if (value != null) type = value.GetType();

            // 接口支持
            //if (Array.IndexOf(member.Type.GetInterfaces(), typeof(IBinaryAccessor)) >= 0)
            if (value != null && typeof(IBinaryAccessor).IsAssignableFrom(type))
            {
                // 调用接口
                IBinaryAccessor accessor = value as IBinaryAccessor;
                accessor.Write(writer);
                return true;
            }

            return writer.WriteObject(target, member, encodeInt, true, isProperty, callback);
        }

        //        /// <summary>
        //        /// 把指定对象写入到写入器，指定写入属性还是字段
        //        /// </summary>
        //        /// <param name="target">目标对象</param>
        //        /// <param name="writer">写入器</param>
        //        /// <param name="encodeInt">使用7Bit编码整数</param>
        //        /// <param name="allowNull">是否允许对象为空，如果允许，则写入时增加一个字节表示对象是否为空</param>
        //        /// <param name="isProperty"></param>
        //        protected void Write(Object target, BinaryWriterX writer, Boolean encodeInt, Boolean allowNull, Boolean isProperty)
        //        {
        //            if (target == null) throw new ArgumentNullException("target", "目标对象不能为空！");

        //            if (isProperty)
        //            {
        //                PropertyInfo[] pis = FindProperties(target.GetType());
        //                if (pis == null || pis.Length < 1) return;

        //                foreach (PropertyInfo item in pis)
        //                {
        //                    WriteMember(target, writer, item, encodeInt, allowNull);
        //                }
        //            }
        //            else
        //            {
        //                FieldInfo[] fis = FindFields(target.GetType());
        //                if (fis == null || fis.Length < 1) return;

        //                foreach (FieldInfo item in fis)
        //                {
        //#if DEBUG
        //                    long p = writer.BaseStream.Position;
        //                    Console.Write("{0}：", item.Name);
        //#endif
        //                    WriteMember(target, writer, item, encodeInt, allowNull);
        //#if DEBUG
        //                    long p2 = writer.BaseStream.Position;
        //                    if (p2 > p)
        //                    {
        //                        writer.BaseStream.Seek(p, SeekOrigin.Begin);
        //                        Byte[] data = new Byte[p2 - p];
        //                        writer.BaseStream.Read(data, 0, data.Length);
        //                        Console.WriteLine(BitConverter.ToString(data));
        //                    }
        //#endif
        //                }
        //            }
        //        }

        ///// <summary>
        ///// 把对象成员的数据写入到写入器
        ///// </summary>
        ///// <param name="target">目标对象</param>
        ///// <param name="writer">写入器</param>
        ///// <param name="member">成员</param>
        ///// <param name="encodeInt">使用7Bit编码整数</param>
        ///// <param name="allowNull">是否允许对象为空，如果允许，则写入时增加一个字节表示对象是否为空</param>
        //protected virtual void WriteMember(Object target, BinaryWriterX writer, MemberInfoX member, Boolean encodeInt, Boolean allowNull)
        //{
        //    MemberTypes mt = member.Member.MemberType;
        //    if (mt == MemberTypes.Field || mt == MemberTypes.Property)
        //    {
        //        if (!TryWrite(target, writer, member, encodeInt, allowNull))
        //            throw new InvalidOperationException("无法写入数据，如果是复杂类型请实现IBinaryAccessor接口。");
        //    }
        //    else
        //        throw new ArgumentOutOfRangeException("member", "成员只能是FieldInfo或PropertyInfo。");
        //}

        //Boolean TryWrite(Object target, BinaryWriterX writer, MemberInfoX member, Boolean encodeInt, Boolean allowNull)
        //{
        //    Object value = member.GetValue(target);
        //    //if (value == null) throw new Exception("当前对象不能为空，空数据应该由上层调用者处理！");

        //    // 基本类型
        //    if (writer.WriteValue(value, member.Type, encodeInt)) return true;

        //    // 扩展类型
        //    if (writer.WriteX(value, member.Type)) return true;

        //    // 允许空时，增加一个字节表示对象是否为空
        //    if (value == null)
        //    {
        //        if (allowNull) writer.Write(false);
        //        return true;
        //    }
        //    if (allowNull) writer.Write(true);


        //    // 接口支持
        //    //if (Array.IndexOf(member.Type.GetInterfaces(), typeof(IBinaryAccessor)) >= 0)
        //    if (typeof(IBinaryAccessor).IsAssignableFrom(member.Type))
        //    {
        //        // 调用接口
        //        IBinaryAccessor accessor = value as IBinaryAccessor;
        //        accessor.Write(writer);
        //        return true;
        //    }

        //    #region 数组
        //    //if (member.Type.IsArray)
        //    //{
        //    //    Array arr = value as Array;
        //    //    // 写入长度
        //    //    writer.WriteEncoded(arr.Length);

        //    //    // 特殊处理字节数组
        //    //    if (member.Type == typeof(Byte[]))
        //    //    {
        //    //        writer.Write((Byte[])value);
        //    //        return true;
        //    //    }

        //    //    foreach (Object item in arr)
        //    //    {
        //    //        if (item != null)
        //    //        {
        //    //            // 基本类型
        //    //            if (writer.WriteValue(item, encodeInt)) continue;
        //    //            // 特别支持的常用类型
        //    //            if (writer.WriteX(item)) continue;
        //    //            // 复杂
        //    //            Write(item, writer, encodeInt, allowNull, member.Member.MemberType == MemberTypes.Property);
        //    //        }
        //    //        else
        //    //        {

        //    //        }
        //    //    }

        //    //    return true;
        //    //}
        //    #endregion

        //    #region 枚举
        //    if (typeof(IEnumerable).IsAssignableFrom(member.Type))
        //    {
        //        // 先写元素个数
        //        IEnumerable arr = value as IEnumerable;
        //        Int32 count = 0;
        //        if (member.Type.IsArray)
        //        {
        //            count = (value as Array).Length;
        //        }
        //        else
        //        {
        //            foreach (Object item in arr)
        //            {
        //                count++;
        //            }
        //        }
        //        // 写入长度
        //        writer.WriteEncoded(count);

        //        foreach (Object item in arr)
        //        {
        //            // 基本类型
        //            if (writer.WriteValue(item, encodeInt)) continue;
        //            // 特别支持的常用类型
        //            if (writer.WriteX(item)) continue;

        //            // 允许空时，增加一个字节表示对象是否为空
        //            if (item == null)
        //            {
        //                if (allowNull) writer.Write(false);
        //                continue;
        //            }
        //            if (allowNull) writer.Write(true);

        //            //if (!writer.WriteValue(item, encodeInt))
        //            //    Write(item, writer, encodeInt, allowNull, member.Member.MemberType == MemberTypes.Property);
        //            // 复杂
        //            Write(item, writer, encodeInt, allowNull, member.Member.MemberType == MemberTypes.Property);
        //        }
        //        return true;
        //    }
        //    #endregion

        //    return false;
        //}
        #endregion

        #region 辅助
        //static DictionaryCache<Type, FieldInfo[]> cache1 = new DictionaryCache<Type, FieldInfo[]>();
        ///// <summary>
        ///// 取得所有可序列化字段
        ///// </summary>
        ///// <param name="type"></param>
        ///// <returns></returns>
        //protected static FieldInfo[] FindFields(Type type)
        //{
        //    if (type == null) return null;

        //    return cache1.GetItem(type, delegate(Type t)
        //    {
        //        List<FieldInfo> list = new List<FieldInfo>();

        //        // GetFields只能取得本类的字段，没办法取得基类的字段
        //        FieldInfo[] fis = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        //        if (fis != null && fis.Length > 0)
        //        {
        //            foreach (FieldInfo item in fis)
        //            {
        //                //if (item.IsDefined(typeof(NonSerializedAttribute), true)) continue;
        //                if (Attribute.IsDefined(item, typeof(NonSerializedAttribute))) continue;
        //                list.Add(item);
        //            }
        //        }

        //        // 递归取父级的字段
        //        if (type.BaseType != null && type.BaseType != typeof(Object))
        //        {
        //            FieldInfo[] fis2 = FindFields(type.BaseType);
        //            if (fis2 != null) list.AddRange(fis2);
        //        }

        //        if (list == null || list.Count < 1) return null;
        //        return list.ToArray();
        //    });
        //}

        //static DictionaryCache<Type, PropertyInfo[]> cache2 = new DictionaryCache<Type, PropertyInfo[]>();
        ///// <summary>
        ///// 取得所有属性
        ///// </summary>
        ///// <param name="type"></param>
        ///// <returns></returns>
        //protected static PropertyInfo[] FindProperties(Type type)
        //{
        //    if (type == null) return null;

        //    return cache2.GetItem(type, delegate(Type t)
        //    {
        //        PropertyInfo[] pis = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        //        if (pis == null || pis.Length < 1) return null;

        //        List<PropertyInfo> list = new List<PropertyInfo>();
        //        foreach (PropertyInfo item in pis)
        //        {
        //            if (Attribute.IsDefined(item, typeof(NonSerializedAttribute))) continue;
        //            // 属性没办法用NonSerializedAttribute
        //            if (Attribute.IsDefined(item, typeof(XmlIgnoreAttribute))) continue;
        //            list.Add(item);
        //        }
        //        if (list == null || list.Count < 1) return null;
        //        return list.ToArray();
        //    });
        //}
        #endregion
    }
}
