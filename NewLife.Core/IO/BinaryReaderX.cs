using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using NewLife.Exceptions;
using NewLife.Reflection;

namespace NewLife.IO
{
    /// <summary>
    /// 二进制协议读取器
    /// </summary>
    public class BinaryReaderX : BinaryReader
    {
        #region 属性
        private Encoding _Encoding;
        /// <summary>编码</summary>
        public virtual Encoding Encoding
        {
            get { return _Encoding; }
            set { _Encoding = value; }
        }
        #endregion

        #region 构造
        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="stream"></param>
        public BinaryReaderX(Stream stream) : base(stream) { }
        #endregion

        #region 压缩编码
        /// <summary>
        /// 以压缩格式读取16位整数
        /// </summary>
        /// <returns></returns>
        public Int16 ReadEncodedInt16()
        {
            Byte b;
            Int16 rs = 0;
            Int32 n = 0;
            while (true)
            {
                b = ReadByte();
                // 必须转为Int16，否则可能溢出
                rs += (Int16)((b & 0x7f) << n);
                if ((b & 0x80) == 0) break;

                n += 7;
                if (n >= 64) throw new FormatException("数字值过大，无法使用压缩格式读取！");
            }
            return rs;
        }

        /// <summary>
        /// 以压缩格式读取32位整数
        /// </summary>
        /// <returns></returns>
        public Int32 ReadEncodedInt32()
        {
            return Read7BitEncodedInt();

            //byte num3;
            //int num = 0;
            //int num2 = 0;
            //do
            //{
            //    if (num2 == 0x23)
            //    {
            //        //throw new FormatException(Environment.GetResourceString("Format_Bad7BitInt32"));
            //        throw new FormatException("Format_Bad7BitInt32");
            //    }
            //    num3 = this.ReadByte();
            //    num |= (num3 & 0x7f) << num2;
            //    num2 += 7;
            //}
            //while ((num3 & 0x80) != 0);
            //return num;
        }

        /// <summary>
        /// 以压缩格式读取64位整数
        /// </summary>
        /// <returns></returns>
        public Int64 ReadEncodedInt64()
        {
            Byte b;
            Int64 rs = 0;
            Int32 n = 0;
            while (true)
            {
                b = ReadByte();
                // 必须转为Int64，否则可能溢出
                rs += (Int64)(b & 0x7f) << n;
                if ((b & 0x80) == 0) break;

                n += 7;
                if (n >= 64) throw new FormatException("数字值过大，无法使用压缩格式读取！");
            }
            return rs;
        }
        #endregion

        #region 类型支持
        /// <summary>
        /// 是否支持指定类型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public Boolean Support(Type type)
        {
            TypeCode code = Type.GetTypeCode(type);
            if (code != TypeCode.Object) return true;

            if (type == typeof(Byte[])) return true;
            if (type == typeof(Char[])) return true;

            if (typeof(Guid).IsAssignableFrom(type)) return true;
            if (typeof(IPAddress).IsAssignableFrom(type)) return true;
            if (typeof(IPEndPoint).IsAssignableFrom(type)) return true;

            return false;
        }
        #endregion

        #region 读取对象
        /// <summary>
        /// 从数据流中读取指定类型的对象
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns>对象</returns>
        public Object ReadObject(Type type)
        {
            Object value;
            return TryReadObject(null, TypeX.Create(type), null, true, true, false, out value) ? value : null;
        }

        /// <summary>
        /// 尝试读取目标对象指定成员的值
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="value">对象</param>
        /// <returns>是否读取成功</returns>
        public Boolean TryReadObject(Type type, ref Object value)
        {
            return TryReadObject(null, TypeX.Create(type), null, true, true, false, out value);
        }

        /// <summary>
        /// 尝试读取目标对象指定成员的值，通过委托方法递归处理成员
        /// </summary>
        /// <param name="target">目标对象</param>
        /// <param name="member">成员</param>
        /// <param name="type">成员类型，以哪一种类型读取</param>
        /// <param name="encodeInt">是否编码整数</param>
        /// <param name="allowNull">是否允许空</param>
        /// <param name="isProperty">是否处理属性</param>
        /// <param name="value">成员值</param>
        /// <returns>是否读取成功</returns>
        public Boolean TryReadObject(Object target, MemberInfoX member, Type type, Boolean encodeInt, Boolean allowNull, Boolean isProperty, out Object value)
        {
            // 使用自己作为处理成员的方法
            return TryReadObject(target, member, type, encodeInt, allowNull, isProperty, out value, ReadMember);
        }

        /// <summary>
        /// 尝试读取目标对象指定成员的值，处理基础类型、特殊类型、基础类型数组、特殊类型数组，通过委托方法处理成员
        /// </summary>
        /// <remarks>
        /// 简单类型在value中返回，复杂类型直接填充target；
        /// </remarks>
        /// <param name="target">目标对象</param>
        /// <param name="member">成员</param>
        /// <param name="type">成员类型，以哪一种类型读取</param>
        /// <param name="encodeInt">是否编码整数</param>
        /// <param name="allowNull">是否允许空</param>
        /// <param name="isProperty">是否处理属性</param>
        /// <param name="value">成员值</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否读取成功</returns>
        public Boolean TryReadObject(Object target, MemberInfoX member, Type type, Boolean encodeInt, Boolean allowNull, Boolean isProperty, out Object value, ReadCallback callback)
        {
            if (type == null)
            {
                type = member.Type;
                if (target != null && member.IsType) type = target.GetType();
            }
            if (callback == null) callback = ReadMember;

            // 基本类型
            if (TryReadValue(type, encodeInt, out value)) return true;

            // 特殊类型
            if (TryReadX(type, out value)) return true;

            #region 枚举
            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                return TryReadEnumerable(target, member, encodeInt, allowNull, isProperty, out value, callback);
            }
            #endregion

            #region 复杂对象
            // 引用类型允许空时，先读取一个字节判断对象是否为空
            if (!type.IsValueType && allowNull && !ReadBoolean()) return true;

            // 成员对象
            //if (member.Member.MemberType == MemberTypes.TypeInfo)
            //    value = target;
            //else
            //    value = member.GetValue(target);
            value = member.IsType ? target : member.GetValue(target);

            // 如果为空，实例化并赋值。只有引用类型才会进来
            if (value == null)
            {
                value = TypeX.CreateInstance(type);
                //// 如果是成员，还需要赋值
                //if (member.Member.MemberType != MemberTypes.TypeInfo && target != null) member.SetValue(target, value);
            }

            // 以下只负责填充value的各成员
            Object obj = null;
            if (isProperty)
            {
                PropertyInfo[] pis = BinaryWriterX.FindProperties(type);
                if (pis == null || pis.Length < 1) return true;

                foreach (PropertyInfo item in pis)
                {
                    //ReadMember(target, reader, item, encodeInt, allowNull);
                    MemberInfoX member2 = item;
                    if (!callback(this, value, member2, member2.Type, encodeInt, allowNull, isProperty, out obj, callback)) return false;
                    member2.SetValue(value, obj);
                }
            }
            else
            {
                FieldInfo[] fis = BinaryWriterX.FindFields(type);
                if (fis == null || fis.Length < 1) return true;

                foreach (FieldInfo item in fis)
                {
                    //#if DEBUG
                    //                    long p = 0;
                    //                    long p2 = 0;
                    //                    if (BaseStream.CanSeek && BaseStream.CanRead)
                    //                    {
                    //                        p = BaseStream.Position;
                    //                        Console.Write("{0,-16}：", item.Name);
                    //                    }
                    //#endif
                    //ReadMember(target, this, item, encodeInt, allowNull);
                    MemberInfoX member2 = item;
                    if (!callback(this, value, member2, member2.Type, encodeInt, allowNull, isProperty, out obj, callback)) return false;
                    // 尽管有可能会二次赋值（如果callback调用这里的话），但是没办法保证用户的callback一定会给成员赋值，所以这里多赋值一次
                    member2.SetValue(value, obj);
                    //#if DEBUG
                    //                    if (BaseStream.CanSeek && BaseStream.CanRead)
                    //                    {
                    //                        p2 = BaseStream.Position;
                    //                        if (p2 > p)
                    //                        {
                    //                            BaseStream.Seek(p, SeekOrigin.Begin);
                    //                            Byte[] data = new Byte[p2 - p];
                    //                            BaseStream.Read(data, 0, data.Length);
                    //                            Console.WriteLine("[{0}] {1}", data.Length, BitConverter.ToString(data));
                    //                        }
                    //                        else
                    //                            Console.WriteLine();
                    //                    }
                    //#endif
                }
            }
            #endregion

            return true;
        }

        private static Boolean ReadMember(BinaryReaderX reader, Object target, MemberInfoX member, Type type, Boolean encodeInt, Boolean allowNull, Boolean isProperty, out Object value, ReadCallback callback)
        {
            // 使用自己作为处理成员的方法
            return reader.TryReadObject(target, member, type, encodeInt, allowNull, isProperty, out value, callback);
        }
        #endregion

        #region 读取值类型
        /// <summary>
        /// 读取值类型数据
        /// </summary>
        /// <param name="type"></param>
        /// <param name="encodeInt"></param>
        /// <returns></returns>
        public Object ReadValue(Type type, Boolean encodeInt)
        {
            Object value;
            return TryReadValue(type, encodeInt, out value) ? value : null;
        }

        /// <summary>
        /// 尝试读取值类型数据，返回是否读取成功
        /// </summary>
        /// <param name="type"></param>
        /// <param name="encodeInt"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Boolean TryReadValue(Type type, Boolean encodeInt, out Object value)
        {
            TypeCode code = Type.GetTypeCode(type);
            switch (code)
            {
                case TypeCode.Boolean:
                    value = ReadBoolean();
                    return true;
                case TypeCode.Byte:
                    value = ReadByte();
                    return true;
                case TypeCode.Char:
                    value = ReadChar();
                    return true;
                case TypeCode.DBNull:
                    //value=DBNull.Value;
                    value = ReadByte();
                    return true;
                case TypeCode.DateTime:
                    //value=new DateTime(ReadInt64());
                    if (!TryReadValue(typeof(Int64), encodeInt, out value)) return false;
                    value = new DateTime((Int64)value);
                    //value = new DateTime(2000, 1, 1).AddTicks((Int64)value);
                    return true;
                case TypeCode.Decimal:
                    value = ReadDecimal();
                    return true;
                case TypeCode.Double:
                    value = ReadDouble();
                    return true;
                case TypeCode.Empty:
                    //value=null;
                    value = ReadByte();
                    return true;
                case TypeCode.Int16:
                    value = ReadInt16();
                    return true;
                case TypeCode.Int32:
                    if (!encodeInt)
                        value = ReadInt32();
                    else
                        value = ReadEncodedInt32();
                    return true;
                case TypeCode.Int64:
                    if (!encodeInt)
                        value = ReadInt64();
                    else
                        value = ReadEncodedInt64();
                    return true;
                case TypeCode.Object:
                    break;
                case TypeCode.SByte:
                    value = ReadSByte();
                    return true;
                case TypeCode.Single:
                    value = ReadSingle();
                    return true;
                case TypeCode.String:
                    value = ReadString();
                    return true;
                case TypeCode.UInt16:
                    value = ReadUInt16();
                    return true;
                case TypeCode.UInt32:
                    if (!encodeInt)
                        value = ReadUInt32();
                    else
                        value = ReadEncodedInt32();
                    return true;
                case TypeCode.UInt64:
                    if (!encodeInt)
                        value = ReadUInt64();
                    else
                        value = ReadEncodedInt64();
                    return true;
                default:
                    break;
            }

            if (type == typeof(Byte[]))
            {
                Int32 len = ReadEncodedInt32();
                if (len < 0) throw new Exception("非法数据！字节数组长度不能为负数！");
                value = null;
                if (len > 0) value = ReadBytes(len);
                return true;
            }
            if (type == typeof(Char[]))
            {
                Int32 len = ReadEncodedInt32();
                if (len < 0) throw new Exception("非法数据！字符数组长度不能为负数！");
                value = null;
                if (len > 0) value = ReadChars(len);
                return true;
            }

            //// 尝试其它可能支持的类型
            //if (ReadX(type, out value)) return true;

            value = null;
            return false;
        }
        #endregion

        #region 枚举
        /// <summary>
        /// 尝试读取目标对象指定成员的值
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="value">对象</param>
        /// <returns>是否读取成功</returns>
        public Boolean TryReadEnumerable(Type type, ref Object value)
        {
            return false;
        }

        /// <summary>
        /// 尝试读取枚举
        /// </summary>
        /// <param name="target">目标对象</param>
        /// <param name="member">成员</param>
        /// <param name="encodeInt">是否编码整数</param>
        /// <param name="allowNull">是否允许空</param>
        /// <param name="isProperty">是否处理属性</param>
        /// <param name="value">成员值</param>
        /// <returns>是否读取成功</returns>
        public Boolean TryReadEnumerable(Object target, MemberInfoX member, Boolean encodeInt, Boolean allowNull, Boolean isProperty, out Object value)
        {
            return TryReadEnumerable(target, member, encodeInt, allowNull, isProperty, out value, null);
        }

        /// <summary>
        /// 尝试读取枚举
        /// </summary>
        /// <remarks>重点和难点在于如果得知枚举元素类型，这里假设所有元素类型一致，否则实在无法处理</remarks>
        /// <param name="target">目标对象</param>
        /// <param name="member">成员</param>
        /// <param name="encodeInt">是否编码整数</param>
        /// <param name="allowNull">是否允许空</param>
        /// <param name="isProperty">是否处理属性</param>
        /// <param name="value">成员值</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否读取成功</returns>
        public Boolean TryReadEnumerable(Object target, MemberInfoX member, Boolean encodeInt, Boolean allowNull, Boolean isProperty, out Object value, ReadCallback callback)
        {
            value = null;
            if (member == null || !typeof(IEnumerable).IsAssignableFrom(member.Type)) return false;

            //// 尝试计算元素类型，通过成员的第一个元素。这个办法实在丑陋，不仅要给成员赋值，还要加一个元素
            //Type elmType = null;
            //if (target != null && !member.IsType)
            //{
            //    IEnumerable en = member.GetValue(target) as IEnumerable;
            //    if (en != null)
            //    {
            //        foreach (Object item in en)
            //        {
            //            if (item != null)
            //            {
            //                elmType = item.GetType();
            //                break;
            //            }
            //        }
            //    }
            //}

            if (!TryReadEnumerable(member.Type, Type.EmptyTypes, encodeInt, allowNull, isProperty, out value, callback)) return false;

            if (!member.IsType) member.SetValue(target, value);

            return true;
        }

        /// <summary>
        /// 尝试读取枚举
        /// </summary>
        /// <remarks>重点和难点在于如果得知枚举元素类型，这里假设所有元素类型一致，否则实在无法处理</remarks>
        /// <param name="type">类型</param>
        /// <param name="elementTypes">元素类型数组</param>
        /// <param name="encodeInt">是否编码整数</param>
        /// <param name="allowNull">是否允许空</param>
        /// <param name="isProperty">是否处理属性</param>
        /// <param name="value">成员值</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否读取成功</returns>
        public Boolean TryReadEnumerable(Type type, Type[] elementTypes, Boolean encodeInt, Boolean allowNull, Boolean isProperty, out Object value, ReadCallback callback)
        {
            value = null;
            if (!typeof(IEnumerable).IsAssignableFrom(type)) return false;

            Type elementType = null;
            Type valueType = null;
            if (elementTypes != null)
            {
                if (elementTypes.Length >= 1) elementType = elementTypes[0];
                if (elementTypes.Length >= 2) valueType = elementTypes[1];
            }

            //// 列表
            //if (typeof(IList).IsAssignableFrom(type))
            //{
            //    if (TryReadList(type, elementType, encodeInt, allowNull, isProperty, out value, callback)) return true;
            //}

            //// 字典
            //if (typeof(IDictionary).IsAssignableFrom(type))
            //{
            //    if (TryReadDictionary(type, elementType, valueType, encodeInt, allowNull, isProperty, out value, callback)) return true;
            //}

            // 先读元素个数
            Int32 count = ReadEncodedInt32();
            if (count < 0) throw new InvalidOperationException("无效元素个数" + count + "！");

            // 没有元素
            if (count == 0) return true;

            #region 计算元素类型
            if (elementTypes == null || elementTypes.Length <= 0)
            {
                if (type.HasElementType)
                    elementTypes = new Type[] { type.GetElementType() };
                else if (type.IsGenericType)
                {
                    Type[] ts = type.GetGenericArguments();
                    if (ts != null && ts.Length > 0)
                    {
                        if (ts.Length == 1)
                            elementTypes = new Type[] { ts[0] };
                        else if (ts.Length == 2)
                            elementTypes = new Type[] { ts[0], ts[1] };
                    }
                }
                if (elementTypes != null)
                {
                    if (elementTypes.Length >= 1) elementType = elementTypes[0];
                    if (elementTypes.Length >= 2) valueType = elementTypes[1];
                }
            }

            value = null;
            // 如果不是基本类型和特殊类型，必须有委托方法
            if (elementType == null || !Support(elementType) && callback == null) return false;
            #endregion

            #region 特殊处理字节数组和字符数组
            if (TryReadValue(type, encodeInt, out value)) return true;
            #endregion

            #region 多数组取值
            //Array arr = Array.CreateInstance(elementType, count);
            //Array arr = TypeX.CreateInstance(elementType.MakeArrayType(), count) as Array;
            Array[] arrs = new Array[elementTypes.Length];
            for (int i = 0; i < count; i++)
            {
                //if (allowNull && ReadEncodedInt32() == 0) continue;

                for (int j = 0; j < elementTypes.Length; j++)
                {
                    if (arrs[j] == null) arrs[j] = TypeX.CreateInstance(elementTypes[j].MakeArrayType(), count) as Array;

                    Object obj = null;
                    if (!TryReadValue(elementTypes[j], encodeInt, out obj) &&
                        !TryReadX(elementTypes[j], out obj))
                    {
                        //obj = CreateInstance(elementType);
                        //Read(obj, reader, encodeInt, allowNull, member.Member.MemberType == MemberTypes.Property);

                        //obj = TypeX.CreateInstance(elementType);
                        if (!callback(this, null, elementTypes[j], null, encodeInt, allowNull, isProperty, out obj, callback)) return false;
                    }
                    arrs[j].SetValue(obj, i);
                }
            }
            #endregion

            //value = arr;
            //if (!type.IsArray) value = Activator.CreateInstance(type, arr);
            //if (!type.IsArray) value = TypeX.CreateInstance(type, arr);

            #region 结果处理
            // 如果是数组，直接赋值
            if (type.IsArray)
            {
                value = arrs[0];
                return true;
            }
            else
            {
                if (arrs.Length == 1)
                {
                    // 检查类型是否有指定类型的构造函数，如果有，直接创建类型，并把数组作为构造函数传入
                    ConstructorInfoX ci = ConstructorInfoX.Create(type, new Type[] { typeof(IEnumerable) });
                    if (ci == null) ci = ConstructorInfoX.Create(type, new Type[] { typeof(IEnumerable<>).MakeGenericType(elementType) });
                    if (ci != null)
                    {
                        //value = TypeX.CreateInstance(type, arrs[0]);
                        value = ci.CreateInstance(arrs[0]);
                        return true;
                    }

                    // 添加方法
                    MethodInfoX method = MethodInfoX.Create(type, "Add", new Type[] { elementType });
                    if (method != null)
                    {
                        value = TypeX.CreateInstance(type);
                        for (int i = 0; i < count; i++)
                        {
                            method.Invoke(value, arrs[0].GetValue(i));
                        }
                        return true;
                    }
                }
                else if (arrs.Length == 2)
                {
                    // 检查类型是否有指定类型的构造函数，如果有，直接创建类型，并把数组作为构造函数传入
                    ConstructorInfoX ci = ConstructorInfoX.Create(type, new Type[] { typeof(IDictionary<,>).MakeGenericType(elementType, valueType) });
                    if (ci != null)
                    {
                        Type dicType = typeof(Dictionary<,>).MakeGenericType(elementType, valueType);
                        IDictionary dic = TypeX.CreateInstance(dicType) as IDictionary;
                        for (int i = 0; i < count; i++)
                        {
                            dic.Add(arrs[0].GetValue(i), arrs[1].GetValue(i));
                        }
                        //value = TypeX.CreateInstance(type, dic);
                        value = ci.CreateInstance(dic);
                        return true;
                    }

                    // 添加方法
                    MethodInfoX method = MethodInfoX.Create(type, "Add", new Type[] { elementType, valueType });
                    if (method != null)
                    {
                        value = TypeX.CreateInstance(type);
                        for (int i = 0; i < count; i++)
                        {
                            method.Invoke(value, arrs[0].GetValue(i), arrs[1].GetValue(i));
                        }
                        return true;
                    }
                }
            }
            #endregion

            return false;
        }

        //public Boolean TryReadList(Type type, Type elementType, Boolean encodeInt, Boolean allowNull, Boolean isProperty, out Object value, ReadCallback callback)
        //{
        //    value = null;
        //    if (!typeof(IList).IsAssignableFrom(type)) return false;

        //    // 先读元素个数
        //    Int32 count = ReadEncodedInt32();
        //    if (count < 0) throw new InvalidOperationException("无效元素个数" + count + "！");

        //    if (elementType == null)
        //    {
        //        if (type.HasElementType)
        //            elementType = type.GetElementType();
        //        else if (type.IsGenericType)
        //        {
        //            Type[] ts = type.GetGenericArguments();
        //            if (ts != null && ts.Length > 0)
        //            {
        //                if (ts.Length == 1)
        //                    elementType = ts[0];
        //                else if (ts.Length == 2)
        //                    elementType = ts[0];
        //            }
        //        }
        //    }

        //    value = null;
        //    // 如果不是基本类型和特殊类型，必须有委托方法
        //    if (!Support(elementType) && callback == null) return false;

        //    //Array arr = Array.CreateInstance(elementType, count);
        //    Array arr = TypeX.CreateInstance(elementType.MakeArrayType(), count) as Array;
        //    value = arr;
        //    for (int i = 0; i < count; i++)
        //    {
        //        //if (allowNull && ReadEncodedInt32() == 0) continue;

        //        Object obj = null;
        //        if (!TryReadValue(elementType, encodeInt, out obj) &&
        //            !TryReadX(elementType, out obj))
        //        {
        //            //obj = CreateInstance(elementType);
        //            //Read(obj, reader, encodeInt, allowNull, member.Member.MemberType == MemberTypes.Property);

        //            //obj = TypeX.CreateInstance(elementType);
        //            if (!callback(this, null, elementType, encodeInt, allowNull, isProperty, out obj, callback)) return false;
        //        }
        //        arr.SetValue(obj, i);
        //    }

        //    //if (!type.IsArray) value = Activator.CreateInstance(type, arr);
        //    if (!type.IsArray) value = TypeX.CreateInstance(type, arr);
        //    return true;
        //}

        //public Boolean TryReadDictionary(Type type, Type keyType, Type valueType, Boolean encodeInt, Boolean allowNull, Boolean isProperty, out Object value, ReadCallback callback)
        //{
        //    value = null;
        //    if (!typeof(IDictionary).IsAssignableFrom(type)) return false;

        //    value = null;
        //    return false;
        //}
        #endregion

        #region 扩展处理类型
        /// <summary>
        /// 扩展读取，反射查找合适的读取方法
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Boolean TryReadX(Type type, out Object value)
        {
            value = null;

            if (type == typeof(Guid))
            {
                value = ReadGuid();
                return true;
            }
            if (type == typeof(IPAddress))
            {
                value = ReadIPAddress();
                return true;
            }
            if (type == typeof(IPEndPoint))
            {
                value = ReadIPEndPoint();
                return true;
            }
            if (typeof(Type).IsAssignableFrom(type))
            {
                value = ReadType();
                return true;
            }

            return false;

            //MethodInfo method = this.GetType().GetMethod("Read" + type.Name, new Type[0]);
            //if (method == null) return false;

            //value = MethodInfoX.Create(method).Invoke(this, new Object[0]);
            //return true;
        }

        /// <summary>
        /// 读取Guid
        /// </summary>
        /// <returns></returns>
        public Guid ReadGuid()
        {
            return new Guid(ReadBytes(16));
        }

        /// <summary>
        /// 读取IPAddress
        /// </summary>
        /// <returns></returns>
        public IPAddress ReadIPAddress()
        {
            Int32 p = 0;
            p = ReadEncodedInt32();
            if (p == 0) return null;

            Byte[] buffer = ReadBytes(p);

            return new IPAddress(buffer);
        }

        /// <summary>
        /// 读取IPEndPoint
        /// </summary>
        /// <returns></returns>
        public IPEndPoint ReadIPEndPoint()
        {
            if (ReadByte() == 0) return null;
            BaseStream.Seek(-1, SeekOrigin.Current);

            IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
            ep.Address = ReadIPAddress();
            //// 端口实际只占2字节
            //ep.Port = ReadUInt16();
            ep.Port = ReadEncodedInt32();
            return ep;
        }

        /// <summary>
        /// 读取Type
        /// </summary>
        /// <returns></returns>
        public Type ReadType()
        {
            if (ReadByte() == 0) return null;
            BaseStream.Seek(-1, SeekOrigin.Current);

            String typeName = ReadString();
            if (String.IsNullOrEmpty(typeName)) return null;

            Type type = TypeX.GetType(typeName, true);
            if (type != null) return type;

            throw new XException("无法找到名为{0}的类型！", typeName);
        }
        #endregion

        #region 委托
        /// <summary>
        /// 数据读取方法
        /// </summary>
        /// <param name="reader">读取器</param>
        /// <param name="target">目标对象</param>
        /// <param name="member">成员</param>
        /// <param name="type">成员类型，以哪一种类型读取</param>
        /// <param name="encodeInt">是否编码整数</param>
        /// <param name="allowNull">是否允许空</param>
        /// <param name="isProperty">是否处理属性</param>
        /// <param name="value">成员值</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否写入成功</returns>
        public delegate Boolean ReadCallback(BinaryReaderX reader, Object target, MemberInfoX member, Type type, Boolean encodeInt, Boolean allowNull, Boolean isProperty, out Object value, ReadCallback callback);
        #endregion
    }
}