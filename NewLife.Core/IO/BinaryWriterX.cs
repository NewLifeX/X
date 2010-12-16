using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using NewLife.Collections;
using NewLife.Reflection;
using System.Text;

namespace NewLife.IO
{
    /// <summary>
    /// 二进制协议写入器
    /// </summary>
    /// <remarks>在二进制协议里面，需要定义每一种类型的序列化方式，本写入器仅处理通用的基本类型</remarks>
    public class BinaryWriterX : BinaryWriter
    {
        #region 编码
        private Encoding _Encoding;
        /// <summary>编码</summary>
        public Encoding Encoding
        {
            get { return _Encoding; }
            private set { _Encoding = value; }
        }
        #endregion

        #region 构造
        /// <summary>
        /// 实例化
        /// </summary>
        /// <param name="stream"></param>
        public BinaryWriterX(Stream stream) : this(stream, Encoding.UTF8) { }

        /// <summary>
        /// 实例化
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="encoding"></param>
        public BinaryWriterX(Stream stream, Encoding encoding) : base(stream, encoding) { Encoding = encoding; }
        #endregion

        #region 压缩编码
        /// <summary>
        /// 以压缩格式写入32位整数
        /// </summary>
        /// <param name="value"></param>
        /// <returns>写入字节数</returns>
        public Int32 WriteEncoded(Int32 value)
        {
            //Write7BitEncodedInt(value);

            Int32 count = 1;
            uint num = (uint)value;
            while (num >= 0x80)
            {
                this.Write((byte)(num | 0x80));
                num = num >> 7;

                count++;
            }
            this.Write((byte)num);

            return count;
        }

        /// <summary>
        /// 以压缩格式写入64位整数
        /// </summary>
        /// <param name="value"></param>
        /// <returns>写入字节数</returns>
        public Int32 WriteEncoded(Int64 value)
        {
            Int32 count = 1;
            UInt64 num = (UInt64)value;
            while (num >= 0x80)
            {
                this.Write((byte)(num | 0x80));
                num = num >> 7;

                count++;
            }
            this.Write((byte)num);

            return count;
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

        #region 写入对象
        /// <summary>
        /// 把对象写入数据流，空对象写入0，所有子孙成员编码整数、允许空、写入字段。
        /// </summary>
        /// <param name="value">对象</param>
        /// <returns>写入字节数</returns>
        public Int32 WriteObject(Object value)
        {
            // 因为值类型不会为空，所以不用担心这里会多写一个0而出错
            if (value == null) return WriteValue((Byte)1, true);
            //{
            //    Write(false);
            //    return 1;
            //}

            return WriteObject(value, value.GetType(), true, true, false);
        }

        /// <summary>
        /// 把目标对象指定成员写入数据流，通过委托方法递归处理成员
        /// </summary>
        /// <param name="target">对象</param>
        /// <param name="member">成员</param>
        /// <param name="encodeInt">是否编码整数</param>
        /// <param name="allowNull">是否允许空</param>
        /// <param name="isProperty">是否处理属性</param>
        /// <returns>写入字节数</returns>
        public Int32 WriteObject(Object target, MemberInfoX member, Boolean encodeInt, Boolean allowNull, Boolean isProperty)
        {
            // 使用自己作为处理成员的方法
            return WriteObject(target, member, encodeInt, allowNull, isProperty, WriteMember);
        }

        /// <summary>
        /// 把目标对象指定成员写入数据流，处理基础类型、特殊类型、基础类型数组、特殊类型数组，通过委托方法处理成员
        /// </summary>
        /// <param name="target">目标对象</param>
        /// <param name="member">成员</param>
        /// <param name="encodeInt">是否编码整数</param>
        /// <param name="allowNull">是否允许空</param>
        /// <param name="isProperty">是否处理属性</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>写入字节数</returns>
        public Int32 WriteObject(Object target, MemberInfoX member, Boolean encodeInt, Boolean allowNull, Boolean isProperty, WriteCallback callback)
        {
            Type type = member.Type;
            //Object value = null;
            //if (member.Member.MemberType == MemberTypes.TypeInfo)
            //    value = target;
            //else
            //    value = member.GetValue(target);
            Object value = member.IsType ? target : member.GetValue(target);

            if (value != null) type = value.GetType();
            if (callback == null) callback = WriteMember;

            Int32 num = 0;
            // 基本类型
            if ((num = WriteValue(value, type, encodeInt)) >= 0) return num;

            // 扩展类型
            if ((num = WriteX(value, type)) >= 0) return num;

            #region 枚举
            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                if ((num = WriteEnumerable(value as IEnumerable, type, encodeInt, allowNull, isProperty, callback)) >= 0) return num;
            }
            #endregion

            num = 0;
            #region 复杂对象
            // 值类型不会为null，只有引用类型才需要写标识
            if (!type.IsValueType)
            {
                // 允许空时，增加一个字节表示对象是否为空
                if (value == null)
                {
                    if (allowNull) return WriteValue((Byte)0, true);
                    return 0;
                }
                if (allowNull) num += WriteValue((Byte)1, true);
            }

            // 复杂类型，处理对象成员
            if (isProperty)
            {
                PropertyInfo[] pis = FindProperties(type);
                if (pis == null || pis.Length < 1) return num;

                foreach (PropertyInfo item in pis)
                {
                    Int32 m = callback(this, value, item, encodeInt, allowNull, isProperty, callback);
                    if (m < 0) return m;

                    num += m;
                }
            }
            else
            {
                FieldInfo[] fis = FindFields(type);
                if (fis == null || fis.Length < 1) return num;

                foreach (FieldInfo item in fis)
                {
#if DEBUG
                    long p = BaseStream.Position;
                    Console.Write("{0,-16}：", item.Name);
#endif
                    Int32 m = callback(this, value, item, encodeInt, allowNull, isProperty, callback);
                    if (m < 0) return m;

                    num += m;
#if DEBUG
                    long p2 = BaseStream.Position;
                    if (m != p2 - p) Console.WriteLine("写入字节数计算有问题！");
                    if (p2 > p)
                    {
                        BaseStream.Seek(p, SeekOrigin.Begin);
                        Byte[] data = new Byte[p2 - p];
                        BaseStream.Read(data, 0, data.Length);
                        Console.WriteLine("[{0}] {1}", data.Length, BitConverter.ToString(data));
                    }
                    else
                        Console.WriteLine();
#endif
                }
            }
            #endregion

            return num;
        }

        private static Int32 WriteMember(BinaryWriterX writer, Object value, MemberInfoX member, Boolean encodeInt, Boolean allowNull, Boolean isProperty, WriteCallback callback)
        {
            // 使用自己作为处理成员的方法
            return writer.WriteObject(value, member, encodeInt, allowNull, isProperty, callback);
        }
        #endregion

        #region 写入值类型
        /// <summary>
        /// 写入值类型，只能识别基础类型，对于不能识别的类型，方法返回false
        /// </summary>
        /// <param name="value">要写入的对象</param>
        /// <param name="encodeInt">是否编码整数</param>
        /// <returns>写入字节数</returns>
        public Int32 WriteValue(Object value, Boolean encodeInt)
        {
            // 值类型不会有空，写入器不知道该如何处理空，由外部决定吧
            if (value == null) return -1;

            return WriteValue(value, value.GetType(), encodeInt);
        }

        /// <summary>
        /// 写入值类型，只能识别基础类型，对于不能识别的类型，方法返回false
        /// </summary>
        /// <param name="value">要写入的对象</param>
        /// <param name="type">要写入的对象类型</param>
        /// <param name="encodeInt">是否编码整数</param>
        /// <returns>写入字节数</returns>
        public Int32 WriteValue(Object value, Type type, Boolean encodeInt)
        {
            // 对象不为空时，使用对象实际类型
            if (value != null) type = value.GetType();

            TypeCode code = Type.GetTypeCode(type);
            switch (code)
            {
                case TypeCode.Boolean:
                    Write(Convert.ToBoolean(value, CultureInfo.InvariantCulture));
                    return sizeof(Boolean);
                case TypeCode.Byte:
                    Write(Convert.ToByte(value, CultureInfo.InvariantCulture));
                    return sizeof(Byte);
                case TypeCode.Char:
                    Write(Convert.ToChar(value, CultureInfo.InvariantCulture));
                    return sizeof(Char);
                case TypeCode.DBNull:
                    Write((Byte)0);
                    return sizeof(Byte);
                case TypeCode.DateTime:
                    return WriteValue(Convert.ToDateTime(value, CultureInfo.InvariantCulture).Ticks, encodeInt);
                case TypeCode.Decimal:
                    Write(Convert.ToDecimal(value, CultureInfo.InvariantCulture));
                    return sizeof(Decimal);
                case TypeCode.Double:
                    Write(Convert.ToDouble(value, CultureInfo.InvariantCulture));
                    return sizeof(Double);
                case TypeCode.Empty:
                    Write((Byte)0);
                    return sizeof(Byte);
                case TypeCode.Int16:
                    Write(Convert.ToInt16(value, CultureInfo.InvariantCulture));
                    return sizeof(Int16);
                case TypeCode.Int32:
                    if (encodeInt) return WriteEncoded(Convert.ToInt32(value, CultureInfo.InvariantCulture));

                    Write(Convert.ToInt32(value, CultureInfo.InvariantCulture));
                    return sizeof(Int32);
                case TypeCode.Int64:
                    if (encodeInt) return WriteEncoded(Convert.ToInt64(value, CultureInfo.InvariantCulture));

                    Write(Convert.ToInt64(value, CultureInfo.InvariantCulture));
                    return sizeof(Int64);
                case TypeCode.Object:
                    break;
                case TypeCode.SByte:
                    Write(Convert.ToSByte(value, CultureInfo.InvariantCulture));
                    return sizeof(SByte);
                case TypeCode.Single:
                    Write(Convert.ToSingle(value, CultureInfo.InvariantCulture));
                    return sizeof(Single);
                case TypeCode.String:
                    return WriteString(Convert.ToString(value, CultureInfo.InvariantCulture));
                case TypeCode.UInt16:
                    Write(Convert.ToUInt16(value, CultureInfo.InvariantCulture));
                    return sizeof(UInt16);
                case TypeCode.UInt32:
                    if (!encodeInt) return WriteEncoded(Convert.ToInt32(value, CultureInfo.InvariantCulture));

                    Write(Convert.ToUInt32(value, CultureInfo.InvariantCulture));
                    return sizeof(UInt32);
                case TypeCode.UInt64:
                    if (!encodeInt) return WriteEncoded(Convert.ToInt64(value, CultureInfo.InvariantCulture));

                    Write(Convert.ToUInt64(value, CultureInfo.InvariantCulture));
                    return sizeof(UInt64);
                default:
                    break;
            }

            if (type == typeof(Byte[]))
            {
                Byte[] arr = (Byte[])value;
                if (arr == null || arr.Length == 0)
                    return WriteEncoded(0);
                else
                    return WriteEncoded(arr.Length) + WriteBytes(arr, 0, arr.Length);
            }
            if (type == typeof(Char[]))
            {
                Char[] arr = (Char[])value;
                if (arr == null || arr.Length == 0)
                    return WriteEncoded(0);
                else
                    return WriteEncoded(arr.Length) + WriteChars(arr, 0, arr.Length);
            }

            return -1;
        }

        /// <summary>
        /// 写入字符串，返回写入字节数
        /// </summary>
        /// <param name="value"></param>
        /// <returns>写入字节数</returns>
        public Int32 WriteString(String value)
        {
            Int64 p = BaseStream.Position;
            Write(value);
            return (Int32)(BaseStream.Position - p);
        }

        /// <summary>
        /// 写入字节数组，返回写入字节数
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <returns>写入字节数</returns>
        public Int32 WriteBytes(Byte[] buffer, Int32 index, Int32 count)
        {
            Write(buffer, index, count);
            return count;
        }

        /// <summary>
        /// 写入字符数组，返回写入字符数
        /// </summary>
        /// <param name="chars"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <returns>写入字符数</returns>
        public Int32 WriteChars(Char[] chars, Int32 index, Int32 count)
        {
            Write(chars, index, count);
            return count;
        }
        /// <summary>
        /// 写入结构体
        /// </summary>
        /// <param name="value"></param>
        /// <returns>写入字节数</returns>
        public Int32 WriteStruct(ValueType value)
        {
            if (value == null) return 0;

            Type type = value.GetType();
            if (type.IsGenericType) return 0;

            Int32 len = Marshal.SizeOf(type);

            // 分配全局内存，一并写入
            IntPtr p = Marshal.AllocHGlobal(len);
            try
            {
                Marshal.StructureToPtr(value, p, true);

                Byte[] buffer = new Byte[len];
                Marshal.Copy(p, buffer, 0, buffer.Length);

                Write(buffer, 0, buffer.Length);

                return buffer.Length;
            }
            catch
            {
                return 0;
            }
            finally
            {
                Marshal.DestroyStructure(p, type);
            }
        }
        #endregion

        #region 枚举
        /// <summary>
        /// 写入枚举数据，只处理元素类型是基本类型的数据
        /// </summary>
        /// <param name="value">对象</param>
        /// <param name="type">类型</param>
        /// <param name="encodeInt">是否编码整数</param>
        /// <param name="allowNull">是否允许空</param>
        /// <param name="isProperty">是否处理属性</param>
        /// <returns>写入字节数</returns>
        public Int32 WriteEnumerable(IEnumerable value, Type type, Boolean encodeInt, Boolean allowNull, Boolean isProperty)
        {
            return WriteEnumerable(value, type, encodeInt, allowNull, isProperty, null);
        }

        /// <summary>
        /// 写入枚举数据，复杂类型使用委托方法进行处理
        /// </summary>
        /// <param name="value">对象</param>
        /// <param name="type">类型</param>
        /// <param name="encodeInt">是否编码整数</param>
        /// <param name="allowNull">是否允许空</param>
        /// <param name="isProperty">是否处理属性</param>
        /// <param name="callback">使用指定委托方法处理复杂数据</param>
        /// <returns>写入字节数</returns>
        public Int32 WriteEnumerable(IEnumerable value, Type type, Boolean encodeInt, Boolean allowNull, Boolean isProperty, WriteCallback callback)
        {
            //if (!type.IsArray) throw new Exception("目标类型不是数组类型！");

            if (value == null)
            {
                // 允许空，写入0字节
                if (allowNull) return WriteEncoded(0);
                return 0;
            }

            Int32 num = 0;

            #region 特殊处理字节数组和字符数组
            if ((num = WriteValue(value, type, encodeInt)) >= 0) return num;
            #endregion

            #region 初始化数据
            Int32 count = 0;
            Type elementType = null;
            List<Object> list = new List<Object>();

            if (type.IsArray)
            {
                Array arr = value as Array;
                count = arr.Length;
                elementType = type.GetElementType();
            }
            //else if (typeof(ICollection).IsAssignableFrom(type))
            //{
            //    count = (value as ICollection).Count;
            //}
            else
            {
                foreach (Object item in value)
                {
                    // 加入集合，防止value进行第二次遍历
                    list.Add(item);

                    if (item == null) continue;

                    // 找到枚举的元素类型
                    Type t = item.GetType();
                    if (elementType == null)
                        elementType = t;
                    else if (elementType != item.GetType())
                    {
                        if (elementType.IsAssignableFrom(t))
                        {
                            // t继承自elementType
                        }
                        else if (t.IsAssignableFrom(elementType))
                        {
                            // elementType继承自t
                            elementType = t;
                        }
                        else
                        {
                            // 可能是Object类型，无法支持
                            return -1;
                        }
                    }
                }
                count = list.Count;
                value = list;
            }
            if (count == 0) return WriteEncoded(0);

            // 可能是Object类型，无法支持
            if (elementType == null) return -1;

            // 如果不是基本类型和特殊类型，必须有委托方法
            if (!Support(elementType) && callback == null) return -1;
            #endregion

            // 写入长度
            num = WriteEncoded(count);

            foreach (Object item in value)
            {
                // 基本类型
                Int32 m = WriteValue(item, encodeInt);
                if (m >= 0)
                {
                    num += m;
                    continue;
                }
                // 特别支持的常用类型
                m = WriteX(item);
                if (m >= 0)
                {
                    num += m;
                    continue;
                }
                m = callback(this, item, elementType, encodeInt, allowNull, isProperty, callback);
                if (m >= 0)
                {
                    num += m;
                    continue;
                }
                else
                    return -1;

                //// 允许空时，增加一个字节表示对象是否为空
                //if (item == null)
                //{
                //    if (allowNull) Write(false);
                //    continue;
                //}
                //if (allowNull) Write(true);

                //if (!writer.WriteValue(item, encodeInt))
                //    Write(item, writer, encodeInt, allowNull, member.Member.MemberType == MemberTypes.Property);
                //// 复杂
                //Write(item, writer, encodeInt, allowNull, member.Member.MemberType == MemberTypes.Property);
            }

            return num;
        }
        #endregion

        #region 扩展处理类型
        /// <summary>
        /// 扩展写入，反射查找合适的写入方法
        /// </summary>
        /// <param name="value"></param>
        /// <returns>写入字节数</returns>
        public Int32 WriteX(Object value)
        {
            // 对象为空，无法取得类型，无法写入
            if (value == null) return -1;

            return WriteX(value, value.GetType());
        }

        /// <summary>
        /// 扩展写入，反射查找合适的写入方法
        /// </summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <returns>写入字节数</returns>
        public Int32 WriteX(Object value, Type type)
        {
            if (type == null)
            {
                if (value == null) throw new Exception("没有指定写入类型，且写入对象为空，不知道如何写入！");

                type = value.GetType();
            }

            if (type == typeof(Guid)) return Write((Guid)value);
            if (type == typeof(IPAddress)) return Write((IPAddress)value);
            if (type == typeof(IPEndPoint)) return Write((IPEndPoint)value);

            return -1;

            //MethodInfo method = this.GetType().GetMethod("Write", new Type[] { type });
            //if (method == null) return false;

            //MethodInfoX.Create(method).Invoke(this, new Object[] { value });
            //return true;
        }

        /// <summary>
        /// 写入Guid
        /// </summary>
        /// <param name="value"></param>
        /// <returns>写入字节数</returns>
        public Int32 Write(Guid value)
        {
            Byte[] buffer = ((Guid)value).ToByteArray();
            return WriteBytes(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// 写入IPAddress
        /// </summary>
        /// <param name="value"></param>
        /// <returns>写入字节数</returns>
        public Int32 Write(IPAddress value)
        {
            if (value != null)
            {
                Byte[] buffer = (value as IPAddress).GetAddressBytes();
                return WriteEncoded(buffer.Length) + WriteBytes(buffer, 0, buffer.Length);
            }
            else
                return WriteEncoded(0);
        }

        /// <summary>
        /// 写入IPEndPoint
        /// </summary>
        /// <param name="value"></param>
        /// <returns>写入字节数</returns>
        public Int32 Write(IPEndPoint value)
        {
            if (value != null)
            {
                return Write(value.Address) + WriteEncoded(value.Port);
                ////// 端口实际只占2字节
                ////Write((UInt16)value.Port);
                //WriteEncoded(value.Port);
            }
            else
                return WriteEncoded(0);
        }
        #endregion

        #region 辅助
        static DictionaryCache<Type, FieldInfo[]> cache1 = new DictionaryCache<Type, FieldInfo[]>();
        /// <summary>
        /// 取得所有可序列化字段
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal protected static FieldInfo[] FindFields(Type type)
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
        internal protected static PropertyInfo[] FindProperties(Type type)
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

        //internal protected static Int32 SizeOf(Type type)
        //{

        //}

        //internal protected static Int32 SizeOf<T>()
        //{
        //    return sizeof(T);
        //}

        /// <summary>
        /// 如果value非零，则加到location上
        /// </summary>
        /// <param name="location"></param>
        /// <param name="value"></param>
        /// <returns>是否非零</returns>
        static Boolean AddIfNonZero(ref Int32 location, Int32 value)
        {
            if (value < 0) return false;

            location += value;
            return true;
        }
        #endregion

        #region 委托
        /// <summary>
        /// 数据写入方法
        /// </summary>
        /// <param name="writer">写入器</param>
        /// <param name="target">目标对象</param>
        /// <param name="member">成员</param>
        /// <param name="encodeInt">是否编码整数</param>
        /// <param name="allowNull">是否允许空</param>
        /// <param name="isProperty">是否处理属性</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>写入字节数</returns>
        public delegate Int32 WriteCallback(BinaryWriterX writer, Object target, MemberInfoX member, Boolean encodeInt, Boolean allowNull, Boolean isProperty, WriteCallback callback);
        #endregion
    }
}