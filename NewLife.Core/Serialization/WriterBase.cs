using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Reflection;
using System.Net;
using NewLife.Reflection;

namespace NewLife.Serialization
{
    /// <summary>
    /// 写入器基类
    /// </summary>
    public abstract class WriterBase : ReaderWriterBase, IWriter
    {
        #region 写入基础元数据
        #region 字节
        /// <summary>
        /// 将一个无符号字节写入
        /// </summary>
        /// <param name="value">要写入的无符号字节。</param>
        public abstract void Write(Byte value);

        /// <summary>
        /// 将字节数组写入
        /// </summary>
        /// <param name="buffer">包含要写入的数据的字节数组。</param>
        public virtual void Write(byte[] buffer) { Write(buffer, 0, buffer == null ? 0 : buffer.Length); }

        /// <summary>
        /// 将一个有符号字节写入当前流，并将流的位置提升 1 个字节。
        /// </summary>
        /// <param name="value">要写入的有符号字节。</param>
        [CLSCompliant(false)]
        public virtual void Write(sbyte value) { Write((Byte)value); }

        /// <summary>
        /// 将字节数组部分写入当前流。
        /// </summary>
        /// <param name="buffer">包含要写入的数据的字节数组。</param>
        /// <param name="index">buffer 中开始写入的起始点。</param>
        /// <param name="count">要写入的字节数。</param>
        public virtual void Write(byte[] buffer, int index, int count)
        {
            if (buffer == null || buffer.Length < 1 || count <= 0 || index >= buffer.Length) return;

            for (int i = 0; i < count && index + i < buffer.Length; i++)
            {
                Write(buffer[index + i]);
            }
        }
        #endregion

        #region 有符号整数
        /// <summary>
        /// 写入整数的字节数组，某些写入器（如二进制写入器）可能需要改变字节顺序
        /// </summary>
        /// <param name="buffer"></param>
        protected virtual void WriteIntBytes(Byte[] buffer)
        {
            Write(buffer);
        }

        /// <summary>
        /// 将 2 字节有符号整数写入当前流，并将流的位置提升 2 个字节。
        /// </summary>
        /// <param name="value">要写入的 2 字节有符号整数。</param>
        public virtual void Write(short value) { WriteIntBytes(BitConverter.GetBytes(value)); }

        /// <summary>
        /// 将 4 字节有符号整数写入当前流，并将流的位置提升 4 个字节。
        /// </summary>
        /// <param name="value">要写入的 4 字节有符号整数。</param>
        public virtual void Write(int value) { WriteIntBytes(BitConverter.GetBytes(value)); }

        /// <summary>
        /// 将 8 字节有符号整数写入当前流，并将流的位置提升 8 个字节。
        /// </summary>
        /// <param name="value">要写入的 8 字节有符号整数。</param>
        public virtual void Write(long value) { WriteIntBytes(BitConverter.GetBytes(value)); }
        #endregion

        #region 无符号整数
        /// <summary>
        /// 将 2 字节无符号整数写入当前流，并将流的位置提升 2 个字节。
        /// </summary>
        /// <param name="value">要写入的 2 字节无符号整数。</param>
        [CLSCompliant(false)]
        public virtual void Write(ushort value) { Write((Int16)value); }

        /// <summary>
        /// 将 4 字节无符号整数写入当前流，并将流的位置提升 4 个字节。
        /// </summary>
        /// <param name="value">要写入的 4 字节无符号整数。</param>
        [CLSCompliant(false)]
        public virtual void Write(uint value) { Write((Int32)value); }

        /// <summary>
        /// 将 8 字节无符号整数写入当前流，并将流的位置提升 8 个字节。
        /// </summary>
        /// <param name="value">要写入的 8 字节无符号整数。</param>
        [CLSCompliant(false)]
        public virtual void Write(ulong value) { Write((Int64)value); }
        #endregion

        #region 浮点数
        /// <summary>
        /// 将 4 字节浮点值写入当前流，并将流的位置提升 4 个字节。
        /// </summary>
        /// <param name="value">要写入的 4 字节浮点值。</param>
        public virtual void Write(float value) { Write(BitConverter.GetBytes(value)); }

        /// <summary>
        /// 将 8 字节浮点值写入当前流，并将流的位置提升 8 个字节。
        /// </summary>
        /// <param name="value">要写入的 8 字节浮点值。</param>
        public virtual void Write(double value) { Write(BitConverter.GetBytes(value)); }
        #endregion

        #region 字符串
        /// <summary>
        /// 将 Unicode 字符写入当前流，并根据所使用的 Encoding 和向流中写入的特定字符，提升流的当前位置。
        /// </summary>
        /// <param name="ch">要写入的非代理项 Unicode 字符。</param>
        public virtual void Write(char ch) { Write(new Char[] { ch }); }

        /// <summary>
        /// 将字符数组写入当前流，并根据所使用的 Encoding 和向流中写入的特定字符，提升流的当前位置。
        /// </summary>
        /// <param name="chars">包含要写入的数据的字符数组。</param>
        public virtual void Write(char[] chars) { Write(chars, 0, chars == null ? 0 : chars.Length); }

        /// <summary>
        /// 将字符数组部分写入当前流，并根据所使用的 Encoding（可能还根据向流中写入的特定字符），提升流的当前位置。
        /// </summary>
        /// <param name="chars">包含要写入的数据的字符数组。</param>
        /// <param name="index">chars 中开始写入的起始点。</param>
        /// <param name="count">要写入的字符数。</param>
        public virtual void Write(char[] chars, int index, int count)
        {
            if (chars == null || chars.Length < 1 || count <= 0 || index >= chars.Length)
            {
                Write(0);
                return;
            }

            // 先用写入字节长度
            Byte[] buffer = Encoding.GetBytes(chars, index, count);
            Write(buffer.Length);
            Write(buffer);
        }

        /// <summary>
        /// 写入字符串
        /// </summary>
        /// <param name="value">要写入的值。</param>
        public virtual void Write(string value)
        {
            Write(value == null ? null : value.ToCharArray());
        }
        #endregion

        #region 其它
        /// <summary>
        /// 将单字节 Boolean 值写入
        /// </summary>
        /// <param name="value">要写入的 Boolean 值</param>
        public virtual void Write(Boolean value) { Write((Byte)(value ? 1 : 0)); }

        /// <summary>
        /// 将一个十进制值写入当前流，并将流位置提升十六个字节。
        /// </summary>
        /// <param name="value">要写入的十进制值。</param>
        public virtual void Write(decimal value)
        {
            Int32[] data = Decimal.GetBits(value);
            for (int i = 0; i < data.Length; i++)
            {
                Write(data[i]);
            }
        }

        /// <summary>
        /// 将一个时间日期写入
        /// </summary>
        /// <param name="value"></param>
        public virtual void Write(DateTime value) { Write(Convert.ToDateTime(value, CultureInfo.InvariantCulture).Ticks); }
        #endregion
        #endregion

        #region 写入对象
        /// <summary>
        /// 把对象写入数据流，空对象写入0，所有子孙成员编码整数、允许空、写入字段。
        /// </summary>
        /// <param name="value">对象</param>
        /// <returns>是否写入成功</returns>
        public virtual Boolean WriteObject(Object value)
        {
            return WriteObject(value, null, null);
        }

        /// <summary>
        /// 把目标对象指定成员写入数据流，通过委托方法递归处理成员
        /// </summary>
        /// <param name="value">要写入的对象</param>
        /// <param name="type">要写入的对象类型</param>
        /// <param name="config">设置</param>
        /// <returns>是否写入成功</returns>
        public virtual Boolean WriteObject(Object value, Type type, ReaderWriterConfig config)
        {
            // 顶级对象，是必须的，避免开头就写入对象是否为空的标记
            // 因此，第一次用的成员处理方法比较特别，需要修改Required
            if (config == null)
            {
                config = CreateConfig();
                config.Required = true;
                return WriteObject(value, type, config, WriteMemberWithNotRequired);
            }
            else
            {
                return WriteObject(value, type, config, WriteMember);
            }
        }

        /// <summary>
        /// 把目标对象指定成员写入数据流，处理基础类型、特殊类型、基础类型数组、特殊类型数组，通过委托方法处理成员
        /// </summary>
        /// <param name="value">要写入的对象</param>
        /// <param name="type">要写入的对象类型</param>
        /// <param name="config">设置</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否写入成功</returns>
        public virtual Boolean WriteObject(Object value, Type type, ReaderWriterConfig config, WriteMemberCallback callback)
        {
            if (value != null) type = value.GetType();
            if (config == null) config = CreateConfig();
            if (callback == null) callback = WriteMember;

            // 基本类型
            if (WriteValue(value, type, config)) return true;

            // 扩展类型
            if (WriteX(value, type)) return true;

            #region 枚举
            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                if (WriteEnumerable(value as IEnumerable, type, config, callback)) return true;
            }
            #endregion

            #region 复杂对象
            // 复杂类型，处理对象成员
            if (!WriteMembers(value, type, config, callback)) return false;
            #endregion

            return true;
        }

        /// <summary>
        /// 写对象成员
        /// </summary>
        /// <param name="value">要写入的对象</param>
        /// <param name="type">要写入的对象类型</param>
        /// <param name="config">设置</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否写入成功</returns>
        public virtual Boolean WriteMembers(Object value, Type type, ReaderWriterConfig config, WriteMemberCallback callback)
        {
            MemberInfo[] mis = GetMembers(type);
            if (mis == null || mis.Length < 1) return true;

            foreach (MemberInfo item in mis)
            {
                if (!WriteMember(value, item, config, callback)) return false;
            }

            return true;
        }

        /// <summary>
        /// 写入成员
        /// </summary>
        /// <param name="value">要写入的对象</param>
        /// <param name="member">成员</param>
        /// <param name="config">设置</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否写入成功</returns>
        protected virtual Boolean WriteMember(Object value, MemberInfo member, ReaderWriterConfig config, WriteMemberCallback callback)
        {
            try
            {
                if (OnMemberWriting != null)
                {
                    EventArgs<MemberInfo, Boolean> e = new EventArgs<MemberInfo, Boolean>(member, false);
                    OnMemberWriting(this, e);
                    if (e.Arg2) return true;
                }

                MemberInfoX mix = member;
                Boolean result = callback(this, mix.GetValue(value), mix.Type, config, callback);
                if (OnMemberWrited != null)
                {
                    EventArgs<MemberInfo, Boolean> e = new EventArgs<MemberInfo, Boolean>(member, result);
                    OnMemberWrited(this, e);
                    result = e.Arg2;
                }
                if (!result) return false;
            }
            catch (Exception ex)
            {
                throw new XSerializationException(member, ex);
            }
            return true;
        }

        private static Boolean WriteMember(IWriter writer, Object value, Type type, ReaderWriterConfig config, WriteMemberCallback callback)
        {
            return (writer as WriterBase).WriteObject(value, type, config, callback);
        }

        private static Boolean WriteMemberWithNotRequired(IWriter writer, Object value, Type type, ReaderWriterConfig config, WriteMemberCallback callback)
        {
            WriterBase wb = writer as WriterBase;
            if (config == null) config = wb.CreateConfig();
            config.Required = false;
            return wb.WriteObject(value, type, config, WriteMember);
        }
        #endregion

        #region 写入值类型
        /// <summary>
        /// 写入值类型，只能识别基础类型，对于不能识别的类型，方法返回false
        /// </summary>
        /// <param name="value">要写入的对象</param>
        /// <param name="type">要写入的对象类型</param>
        /// <param name="config">设置</param>
        /// <returns>是否写入成功</returns>
        public virtual Boolean WriteValue(Object value, Type type, ReaderWriterConfig config)
        {
            // 对象不为空时，使用对象实际类型
            if (value != null) type = value.GetType();

            TypeCode code = Type.GetTypeCode(type);
            switch (code)
            {
                case TypeCode.Boolean:
                    Write(Convert.ToBoolean(value, CultureInfo.InvariantCulture));
                    return true;
                case TypeCode.Byte:
                    Write(Convert.ToByte(value, CultureInfo.InvariantCulture));
                    return true;
                case TypeCode.Char:
                    Write(Convert.ToChar(value, CultureInfo.InvariantCulture));
                    return true;
                case TypeCode.DBNull:
                    Write((Byte)0);
                    return true;
                case TypeCode.DateTime:
                    //return WriteValue(Convert.ToDateTime(value, CultureInfo.InvariantCulture).Ticks, null, config);
                    Write((DateTime)value);
                    return true;
                case TypeCode.Decimal:
                    Write(Convert.ToDecimal(value, CultureInfo.InvariantCulture));
                    return true;
                case TypeCode.Double:
                    Write(Convert.ToDouble(value, CultureInfo.InvariantCulture));
                    return true;
                case TypeCode.Empty:
                    Write((Byte)0);
                    return true;
                case TypeCode.Int16:
                    Write(Convert.ToInt16(value, CultureInfo.InvariantCulture));
                    return true;
                case TypeCode.Int32:
                    Write(Convert.ToInt32(value, CultureInfo.InvariantCulture));
                    return true;
                case TypeCode.Int64:
                    Write(Convert.ToInt64(value, CultureInfo.InvariantCulture));
                    return true;
                case TypeCode.Object:
                    break;
                case TypeCode.SByte:
                    Write(Convert.ToSByte(value, CultureInfo.InvariantCulture));
                    return true;
                case TypeCode.Single:
                    Write(Convert.ToSingle(value, CultureInfo.InvariantCulture));
                    return true;
                case TypeCode.String:
                    Write(Convert.ToString(value, CultureInfo.InvariantCulture));
                    return true;
                case TypeCode.UInt16:
                    Write(Convert.ToUInt16(value, CultureInfo.InvariantCulture));
                    return true;
                case TypeCode.UInt32:
                    Write(Convert.ToUInt32(value, CultureInfo.InvariantCulture));
                    return true;
                case TypeCode.UInt64:
                    Write(Convert.ToUInt64(value, CultureInfo.InvariantCulture));
                    return true;
                default:
                    break;
            }

            if (type == typeof(Byte[]))
            {
                Write((Byte[])value);
                return true;
            }
            if (type == typeof(Char[]))
            {
                Write((Char[])value);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 写入结构体
        /// </summary>
        /// <param name="value"></param>
        /// <returns>是否写入成功</returns>
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
        /// 写入枚举类型数据
        /// </summary>
        /// <param name="value">枚举数据</param>
        /// <returns>是否写入成功</returns>
        public virtual Boolean Write(IEnumerable value)
        {
            return false;
        }

        /// <summary>
        /// 写入枚举数据，复杂类型使用委托方法进行处理
        /// </summary>
        /// <param name="value">对象</param>
        /// <param name="type">类型</param>
        /// <param name="config">配置</param>
        /// <param name="callback">使用指定委托方法处理复杂数据</param>
        /// <returns>是否写入成功</returns>
        public virtual Boolean WriteEnumerable(IEnumerable value, Type type, ReaderWriterConfig config, WriteMemberCallback callback)
        {
            //if (!type.IsArray) throw new Exception("目标类型不是数组类型！");
            Boolean allowNull = !config.Required;

            if (value == null)
            {
                // 允许空，写入0字节
                if (allowNull) Write(0);
                return true;
            }

            #region 特殊处理字节数组和字符数组
            if (WriteValue(value, type, config)) return true;
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
                            return false;
                        }
                    }
                }
                count = list.Count;
                value = list;
            }
            if (count == 0)
            {
                Write(0);
                return true;
            }

            // 可能是Object类型，无法支持
            if (elementType == null) return false;

            //TODO 如果不是基本类型和特殊类型，必须有委托方法
            //if (!Support(elementType) && callback == null) return false;
            #endregion

            // 写入长度
            Write(count);

            foreach (Object item in value)
            {
                // 基本类型
                if (WriteValue(item, null, config)) continue;
                // 特别支持的常用类型
                if (WriteX(item)) continue;

                if (!callback(this, item, elementType, config, callback)) return false;

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

            return true;
        }
        #endregion

        #region 扩展处理类型
        /// <summary>
        /// 扩展写入，反射查找合适的写入方法
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public Boolean WriteX(Object value)
        {
            // 对象为空，无法取得类型，无法写入
            if (value == null) return false;

            return WriteX(value, value.GetType());
        }

        /// <summary>
        /// 扩展写入，反射查找合适的写入方法
        /// </summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public Boolean WriteX(Object value, Type type)
        {
            if (type == null)
            {
                if (value == null) throw new Exception("没有指定写入类型，且写入对象为空，不知道如何写入！");

                type = value.GetType();
            }

            if (type == typeof(Guid))
            {
                Write((Guid)value);
                return true;
            }
            if (type == typeof(IPAddress))
            {
                Write((IPAddress)value);
                return true;
            }
            if (type == typeof(IPEndPoint))
            {
                Write((IPEndPoint)value);
                return true;
            }
            if (typeof(Type).IsAssignableFrom(type))
            {
                Write((Type)value);
                return true;
            }

            return false;

            //MethodInfo method = this.GetType().GetMethod("Write", new Type[] { type });
            //if (method == null) return false;

            //MethodInfoX.Create(method).Invoke(this, new Object[] { value });
            //return true;
        }

        /// <summary>
        /// 写入Guid
        /// </summary>
        /// <param name="value"></param>
        public void Write(Guid value)
        {
            Write(((Guid)value).ToByteArray());
        }

        /// <summary>
        /// 写入IPAddress
        /// </summary>
        /// <param name="value"></param>
        public void Write(IPAddress value)
        {
            if (value != null)
            {
                Byte[] buffer = (value as IPAddress).GetAddressBytes();
                Write(buffer.Length);
                Write(buffer);
            }
            else
                Write(0);
        }

        /// <summary>
        /// 写入IPEndPoint
        /// </summary>
        /// <param name="value"></param>
        public void Write(IPEndPoint value)
        {
            if (value != null)
            {
                Write(value.Address);
                //// 端口实际只占2字节
                //Write((UInt16)value.Port);
                Write(value.Port);
            }
            else
                Write(0);
        }

        /// <summary>
        /// 写入Type
        /// </summary>
        /// <param name="value"></param>
        public void Write(Type value)
        {
            // 尽管使用AssemblyQualifiedName更精确，但是它的长度实在太大了
            if (value != null)
                Write(value.FullName);
            else
                Write(0);
        }
        #endregion

        #region 事件
        /// <summary>
        /// 写入成员前触发。参数觉得是否忽略该成员。
        /// </summary>
        public event EventHandler<EventArgs<MemberInfo, Boolean>> OnMemberWriting;

        /// <summary>
        /// 写入成员后触发。参数决定是否写入成功。
        /// </summary>
        public event EventHandler<EventArgs<MemberInfo, Boolean>> OnMemberWrited;
        #endregion
    }

    /// <summary>
    /// 数据写入方法
    /// </summary>
    /// <param name="writer">写入器</param>
    /// <param name="value">要写入的对象</param>
    /// <param name="type">要写入的对象类型</param>
    /// <param name="config">配置</param>
    /// <param name="callback">处理成员的方法</param>
    /// <returns>是否写入成功</returns>
    public delegate Boolean WriteMemberCallback(IWriter writer, Object value, Type type, ReaderWriterConfig config, WriteMemberCallback callback);
}