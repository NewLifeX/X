using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Text;
using NewLife.Reflection;
using NewLife.Exceptions;

namespace NewLife.Serialization
{
    /// <summary>
    /// 读取器基类
    /// </summary>
    public abstract class ReaderBase : ReaderWriterBase, IReader
    {
        #region 读取基础元数据
        #region 字节
        /// <summary>
        /// 从当前流中读取下一个字节，并使流的当前位置提升 1 个字节。
        /// </summary>
        /// <returns></returns>
        public abstract byte ReadByte();

        /// <summary>
        /// 从当前流中将 count 个字节读入字节数组，并使当前位置提升 count 个字节。
        /// </summary>
        /// <param name="count">要读取的字节数。</param>
        /// <returns></returns>
        public virtual byte[] ReadBytes(int count)
        {
            if (count <= 0) return null;

            Byte[] buffer = new Byte[count];
            for (int i = 0; i < count; i++)
            {
                buffer[i] = ReadByte();
            }

            return buffer;
        }

        /// <summary>
        /// 从此流中读取一个有符号字节，并使流的当前位置提升 1 个字节。
        /// </summary>
        /// <returns></returns>
        [CLSCompliant(false)]
        public virtual sbyte ReadSByte() { return (SByte)ReadByte(); }
        #endregion

        #region 有符号整数
        /// <summary>
        /// 读取整数的字节数组，某些写入器（如二进制写入器）可能需要改变字节顺序
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        protected virtual Byte[] ReadIntBytes(Int32 count)
        {
            return ReadBytes(count);
        }

        /// <summary>
        /// 从当前流中读取 2 字节有符号整数，并使流的当前位置提升 2 个字节。
        /// </summary>
        /// <returns></returns>
        public virtual short ReadInt16() { return BitConverter.ToInt16(ReadBytes(2), 0); }

        /// <summary>
        /// 从当前流中读取 4 字节有符号整数，并使流的当前位置提升 4 个字节。
        /// </summary>
        /// <returns></returns>
        public virtual int ReadInt32() { return BitConverter.ToInt32(ReadBytes(4), 0); }

        /// <summary>
        /// 从当前流中读取 8 字节有符号整数，并使流的当前位置向前移动 8 个字节。
        /// </summary>
        /// <returns></returns>
        public virtual long ReadInt64() { return BitConverter.ToInt64(ReadBytes(8), 0); }
        #endregion

        #region 无符号整数
        /// <summary>
        /// 使用 Little-Endian 编码从当前流中读取 2 字节无符号整数，并将流的位置提升 2 个字节。
        /// </summary>
        /// <returns></returns>
        [CLSCompliant(false)]
        public virtual ushort ReadUInt16() { return (UInt16)ReadInt16(); }

        /// <summary>
        /// 从当前流中读取 4 字节无符号整数并使流的当前位置提升 4 个字节。
        /// </summary>
        /// <returns></returns>
        [CLSCompliant(false)]
        public virtual uint ReadUInt32() { return (UInt32)ReadInt32(); }

        /// <summary>
        /// 从当前流中读取 8 字节无符号整数并使流的当前位置提升 8 个字节。
        /// </summary>
        /// <returns></returns>
        [CLSCompliant(false)]
        public virtual ulong ReadUInt64() { return (UInt64)ReadInt64(); }
        #endregion

        #region 浮点数
        /// <summary>
        /// 从当前流中读取 4 字节浮点值，并使流的当前位置提升 4 个字节。
        /// </summary>
        /// <returns></returns>
        public virtual float ReadSingle() { return BitConverter.ToSingle(ReadBytes(4), 0); }

        /// <summary>
        /// 从当前流中读取 8 字节浮点值，并使流的当前位置提升 8 个字节。
        /// </summary>
        /// <returns></returns>
        public virtual double ReadDouble() { return BitConverter.ToDouble(ReadBytes(8), 0); }
        #endregion

        #region 字符串
        /// <summary>
        /// 从当前流中读取下一个字符，并根据所使用的 Encoding 和从流中读取的特定字符，提升流的当前位置。
        /// </summary>
        /// <returns></returns>
        public virtual char ReadChar() { return ReadChars(1)[0]; }

        /// <summary>
        /// 从当前流中读取 count 个字符，以字符数组的形式返回数据，并根据所使用的 Encoding 和从流中读取的特定字符，提升当前位置。
        /// </summary>
        /// <param name="count">要读取的字符数。</param>
        /// <returns></returns>
        public virtual char[] ReadChars(int count)
        {
            // count个字符可能的最大字节数
            Int32 max = Encoding.GetMaxByteCount(count);

            // 首先按最小值读取
            Byte[] data = ReadBytes(count);

            // 相同，最简单的一种
            if (max == count) return Encoding.GetChars(data);

            // 按最大值准备一个字节数组
            Byte[] buffer = new Byte[max];
            // 复制过去
            Buffer.BlockCopy(data, 0, buffer, 0, data.Length);

            // 遍历，以下算法性能较差，将来可以考虑优化
            Int32 i = 0;
            for (i = count; i < max; i++)
            {
                Int32 n = Encoding.GetCharCount(buffer, 0, i);
                if (n >= count) break;

                buffer[i] = ReadByte();
            }

            return Encoding.GetChars(buffer, 0, i);
        }

        /// <summary>
        /// 从当前流中读取一个字符串。字符串有长度前缀，一次 7 位地被编码为整数。
        /// </summary>
        /// <returns></returns>
        public virtual string ReadString()
        {
            // 先读长度
            Int32 n = ReadInt32();
            if (n <= 0) return null;

            Byte[] buffer = ReadBytes(n);

            return Encoding.GetString(buffer);
        }
        #endregion

        #region 其它
        /// <summary>
        /// 从当前流中读取 Boolean 值，并使该流的当前位置提升 1 个字节。
        /// </summary>
        /// <returns></returns>
        public virtual bool ReadBoolean() { return ReadByte() != 0; }

        /// <summary>
        /// 从当前流中读取十进制数值，并将该流的当前位置提升十六个字节。
        /// </summary>
        /// <returns></returns>
        public virtual decimal ReadDecimal()
        {
            Int32[] data = new Int32[4];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = ReadInt32();
            }
            return new Decimal(data);
        }
        #endregion
        #endregion

        #region 读取对象
        /// <summary>
        /// 从数据流中读取指定类型的对象
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns>对象</returns>
        public virtual Object ReadObject(Type type)
        {
            Object value = null;
            return TryReadObject(type, ref value, null) ? value : null;
        }

        /// <summary>
        /// 尝试读取目标对象指定成员的值
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="value">对象</param>
        /// <returns>是否读取成功</returns>
        public virtual Boolean TryReadObject(Type type, ref Object value)
        {
            return TryReadObject(type, ref value, null);
        }

        /// <summary>
        /// 尝试读取目标对象指定成员的值，通过委托方法递归处理成员
        /// </summary>
        /// <param name="type">要读取的对象类型</param>
        /// <param name="value">要读取的对象</param>
        /// <param name="config">配置</param>
        /// <returns>是否读取成功</returns>
        public virtual Boolean TryReadObject(Type type, ref Object value, ReaderWriterConfig config)
        {
            // 使用自己作为处理成员的方法
            return TryReadObject(type, ref value, config, ReadMember);
        }

        /// <summary>
        /// 尝试读取目标对象指定成员的值，处理基础类型、特殊类型、基础类型数组、特殊类型数组，通过委托方法处理成员
        /// </summary>
        /// <remarks>
        /// 简单类型在value中返回，复杂类型直接填充target；
        /// </remarks>
        /// <param name="type">要读取的对象类型</param>
        /// <param name="value">要读取的对象</param>
        /// <param name="config">配置</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否读取成功</returns>
        [CLSCompliant(false)]
        public virtual Boolean TryReadObject(Type type, ref Object value, ReaderWriterConfig config, ReadMemberCallback callback)
        {
            if (type == null && value != null) type = value.GetType();
            if (config == null) config = new ReaderWriterConfig();
            if (callback == null) callback = ReadMember;

            // 基本类型
            if (TryReadValue(type, config, out value)) return true;

            // 特殊类型
            if (TryReadX(type, out value)) return true;

            #region 枚举
            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                return TryReadEnumerable(type, ref value, config, callback);
            }
            #endregion

            #region 复杂对象
            // 引用类型允许空时，先读取一个字节判断对象是否为空
            if (!type.IsValueType && !config.Required && !ReadBoolean()) return true;

            //// 成员对象
            ////if (member.Member.MemberType == MemberTypes.TypeInfo)
            ////    value = target;
            ////else
            ////    value = member.GetValue(target);
            //value = member.IsType ? target : member.GetValue(target);

            // 如果为空，实例化并赋值。只有引用类型才会进来
            if (value == null)
            {
                value = TypeX.CreateInstance(type);
                //// 如果是成员，还需要赋值
                //if (member.Member.MemberType != MemberTypes.TypeInfo && target != null) member.SetValue(target, value);
            }

            // 以下只负责填充value的各成员
            Object obj = null;

            // 复杂类型，处理对象成员
            MemberInfo[] mis = GetMembers(type);
            if (mis == null || mis.Length < 1) return true;

            foreach (MemberInfo item in mis)
            {
                if (OnMemberReading != null)
                {
                    EventArgs<MemberInfo, Boolean> e = new EventArgs<MemberInfo, Boolean>(item, false);
                    OnMemberReading(this, e);
                    if (e.Arg2) continue;
                }

                MemberInfoX mix = item;
                obj = mix.GetValue(value);
                if (!callback(this, mix.Type, ref obj, config, callback)) return false;
                if (OnMemberReaded != null)
                {
                    EventArgs<MemberInfo, Object> e = new EventArgs<MemberInfo, Object>(item, obj);
                    OnMemberReaded(this, e);
                    obj = e.Arg2;
                }
                mix.SetValue(value, obj);
            }
            #endregion

            return true;
        }

        private static Boolean ReadMember(IReader reader, Type type, ref Object value, ReaderWriterConfig config, ReadMemberCallback callback)
        {
            // 使用自己作为处理成员的方法
            return (reader as ReaderBase).TryReadObject(type, ref value, config, callback);
        }
        #endregion

        #region 读取值类型
        /// <summary>
        /// 读取值类型数据
        /// </summary>
        /// <param name="type"></param>
        /// <param name="config">配置</param>
        /// <returns></returns>
        public Object ReadValue(Type type, ReaderWriterConfig config)
        {
            Object value;
            return TryReadValue(type, config, out value) ? value : null;
        }

        /// <summary>
        /// 尝试读取值类型数据，返回是否读取成功
        /// </summary>
        /// <param name="type">要读取的对象类型</param>
        /// <param name="config">配置</param>
        /// <param name="value">要读取的对象</param>
        /// <returns></returns>
        public Boolean TryReadValue(Type type, ReaderWriterConfig config, out Object value)
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
                    value = ReadByte();
                    return true;
                case TypeCode.DateTime:
                    if (!TryReadValue(typeof(Int64), config, out value)) return false;
                    value = new DateTime((Int64)value);
                    return true;
                case TypeCode.Decimal:
                    value = ReadDecimal();
                    return true;
                case TypeCode.Double:
                    value = ReadDouble();
                    return true;
                case TypeCode.Empty:
                    value = ReadByte();
                    return true;
                case TypeCode.Int16:
                    value = ReadInt16();
                    return true;
                case TypeCode.Int32:
                    value = ReadInt32();
                    return true;
                case TypeCode.Int64:
                    value = ReadInt64();
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
                    value = ReadUInt32();
                    return true;
                case TypeCode.UInt64:
                    value = ReadUInt64();
                    return true;
                default:
                    break;
            }

            if (type == typeof(Byte[]))
            {
                Int32 len = ReadInt32();
                if (len < 0) throw new Exception("非法数据！字节数组长度不能为负数！");
                value = null;
                if (len > 0) value = ReadBytes(len);
                return true;
            }
            if (type == typeof(Char[]))
            {
                Int32 len = ReadInt32();
                if (len < 0) throw new Exception("非法数据！字符数组长度不能为负数！");
                value = null;
                if (len > 0) value = ReadChars(len);
                return true;
            }

            value = null;
            return false;
        }
        #endregion

        #region 枚举
        /// <summary>
        /// 尝试读取目标对象指定成员的值
        /// </summary>
        /// <param name="type">要读取的对象类型</param>
        /// <param name="value">要读取的对象</param>
        /// <returns>是否读取成功</returns>
        public Boolean TryReadEnumerable(Type type, ref Object value)
        {
            return TryReadEnumerable(type, ref value, null);
        }

        /// <summary>
        /// 尝试读取枚举
        /// </summary>
        /// <param name="type">要读取的对象类型</param>
        /// <param name="value">要读取的对象</param>
        /// <param name="config">配置</param>
        /// <returns>是否读取成功</returns>
        public Boolean TryReadEnumerable(Type type, ref Object value, ReaderWriterConfig config)
        {
            return TryReadEnumerable(type, ref value, config, ReadMember);
        }

        /// <summary>
        /// 尝试读取枚举
        /// </summary>
        /// <remarks>重点和难点在于如果得知枚举元素类型，这里假设所有元素类型一致，否则实在无法处理</remarks>
        /// <param name="type">要读取的对象类型</param>
        /// <param name="value">要读取的对象</param>
        /// <param name="config">配置</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否读取成功</returns>
        public Boolean TryReadEnumerable(Type type, ref Object value, ReaderWriterConfig config, ReadMemberCallback callback)
        {
            value = null;
            if (!typeof(IEnumerable).IsAssignableFrom(type)) return false;

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

            if (!TryReadEnumerable(type, Type.EmptyTypes, ref value, config, callback)) return false;

            //if (!member.IsType) member.SetValue(target, value);

            return true;
        }

        /// <summary>
        /// 尝试读取枚举
        /// </summary>
        /// <remarks>重点和难点在于如果得知枚举元素类型，这里假设所有元素类型一致，否则实在无法处理</remarks>
        /// <param name="type">类型</param>
        /// <param name="elementTypes">元素类型数组</param>
        /// <param name="value">要读取的对象</param>
        /// <param name="config">配置</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否读取成功</returns>
        public Boolean TryReadEnumerable(Type type, Type[] elementTypes, ref Object value, ReaderWriterConfig config, ReadMemberCallback callback)
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
            Int32 count = ReadInt32();
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
            //if (elementType == null || !Support(elementType) && callback == null) return false;
            #endregion

            #region 特殊处理字节数组和字符数组
            if (TryReadValue(type, config, out value)) return true;
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
                    if (!TryReadValue(elementTypes[j], config, out obj) &&
                        !TryReadX(elementTypes[j], out obj))
                    {
                        //obj = CreateInstance(elementType);
                        //Read(obj, reader, encodeInt, allowNull, member.Member.MemberType == MemberTypes.Property);

                        //obj = TypeX.CreateInstance(elementType);
                        if (!callback(this, elementTypes[j], ref obj, config, callback)) return false;
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
        #endregion

        #region 扩展处理类型
        /// <summary>
        /// 扩展读取，反射查找合适的读取方法
        /// </summary>
        /// <param name="type">要读取的对象类型</param>
        /// <param name="value">要读取的对象</param>
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
            p = ReadInt32();
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
            //if (ReadByte() == 0) return null;
            //BaseStream.Seek(-1, SeekOrigin.Current);

            IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
            ep.Address = ReadIPAddress();
            //// 端口实际只占2字节
            //ep.Port = ReadUInt16();
            ep.Port = ReadInt32();
            return ep;
        }

        /// <summary>
        /// 读取Type
        /// </summary>
        /// <returns></returns>
        public Type ReadType()
        {
            //if (ReadByte() == 0) return null;
            //BaseStream.Seek(-1, SeekOrigin.Current);

            String typeName = ReadString();
            if (String.IsNullOrEmpty(typeName)) return null;

            Type type = TypeX.GetType(typeName);
            if (type != null) return type;

            throw new XException("无法找到名为{0}的类型！", typeName);
        }
        #endregion

        #region 事件
        /// <summary>
        /// 读取成员先触发。参数决定是否读取成功。
        /// </summary>
        public event EventHandler<EventArgs<MemberInfo, Boolean>> OnMemberReading;

        /// <summary>
        /// 读取成员后触发
        /// </summary>
        public event EventHandler<EventArgs<MemberInfo, Object>> OnMemberReaded;
        #endregion
    }

    /// <summary>
    /// 数据读取方法
    /// </summary>
    /// <param name="reader">读取器</param>
    /// <param name="type">要读取的对象类型</param>
    /// <param name="value">要读取的对象</param>
    /// <param name="config">配置</param>
    /// <param name="callback">处理成员的方法</param>
    /// <returns>是否读取成功</returns>
    public delegate Boolean ReadMemberCallback(IReader reader, Type type, ref Object value, ReaderWriterConfig config, ReadMemberCallback callback);
}