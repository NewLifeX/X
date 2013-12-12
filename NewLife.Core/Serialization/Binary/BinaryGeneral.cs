using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization.Binary
{
    /// <summary>二进制基础类型处理器</summary>
    public class BinaryGeneral : BinaryReadWriteHandlerBase
    {
        /// <summary>写入一个对象</summary>
        /// <param name="value">目标对象</param>
        /// <returns>是否处理成功</returns>
        public override Boolean Write(Object value)
        {
            if (value == null) return false;

            var type = value.GetType();

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    Write((Boolean)value);
                    return true;
                case TypeCode.Byte:
                    Write((Byte)value);
                    return true;
                case TypeCode.Char:
                    Write((Char)value);
                    return true;
                case TypeCode.DBNull:
                    Write((Byte)0);
                    return true;
                case TypeCode.DateTime:
                    Write((DateTime)value);
                    return true;
                case TypeCode.Decimal:
                    Write((Decimal)value);
                    return true;
                case TypeCode.Double:
                    Write((Double)value);
                    return true;
                case TypeCode.Empty:
                    Write((Byte)0);
                    return true;
                case TypeCode.Int16:
                    Write((Int16)value);
                    return true;
                case TypeCode.Int32:
                    Write((Int32)value);
                    return true;
                case TypeCode.Int64:
                    Write((Int64)value);
                    return true;
                case TypeCode.Object:
                    break;
                case TypeCode.SByte:
                    Write((SByte)value);
                    return true;
                case TypeCode.Single:
                    Write((Single)value);
                    return true;
                case TypeCode.String:
                    Write((String)value);
                    return true;
                case TypeCode.UInt16:
                    Write((UInt16)value);
                    return true;
                case TypeCode.UInt32:
                    Write((UInt32)value);
                    return true;
                case TypeCode.UInt64:
                    Write((UInt64)value);
                    return true;
                default:
                    break;
            }

            if (type == typeof(Guid))
            {
                Write((Guid)value);
                return true;
            }

            return false;
        }

        #region 基元类型
        #region 字节
        /// <summary>将一个无符号字节写入</summary>
        /// <param name="value">要写入的无符号字节。</param>
        public virtual void Write(Byte value)
        {
            //SetDebugIndent();

            Host.Write(value);

            //AutoFlush();
        }

        /// <summary>将字节数组写入，如果设置了UseSize，则先写入数组长度。</summary>
        /// <param name="buffer">包含要写入的数据的字节数组。</param>
        public virtual void Write(byte[] buffer)
        {
            if (buffer == null)
            {
                Host.WriteSize(0);
                return;
            }

            Host.WriteSize(buffer.Length);
            Write(buffer, 0, buffer.Length);
        }

        /// <summary>将一个有符号字节写入当前流，并将流的位置提升 1 个字节。</summary>
        /// <param name="value">要写入的有符号字节。</param>
        //[CLSCompliant(false)]
        public virtual void Write(sbyte value) { Write((Byte)value); }

        /// <summary>将字节数组部分写入当前流，不写入数组长度。</summary>
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

        /// <summary>写入字节数组，自动计算长度</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="count">数量</param>
        private void Write(Byte[] buffer, Int32 count)
        {
            if (buffer == null) return;

            if (count < 0 || count > buffer.Length) count = buffer.Length;

            Write(buffer, 0, count);
        }
        #endregion

        #region 有符号整数
        /// <summary>将 2 字节有符号整数写入当前流，并将流的位置提升 2 个字节。</summary>
        /// <param name="value">要写入的 2 字节有符号整数。</param>
        public virtual void Write(short value)
        {
            if (Host.EncodeInt)
                WriteEncoded(value);
            else
                WriteIntBytes(BitConverter.GetBytes(value));
        }

        /// <summary>将 4 字节有符号整数写入当前流，并将流的位置提升 4 个字节。</summary>
        /// <param name="value">要写入的 4 字节有符号整数。</param>
        public virtual void Write(int value)
        {
            if (Host.EncodeInt)
                WriteEncoded(value);
            else
                WriteIntBytes(BitConverter.GetBytes(value));
        }

        /// <summary>将 8 字节有符号整数写入当前流，并将流的位置提升 8 个字节。</summary>
        /// <param name="value">要写入的 8 字节有符号整数。</param>
        public virtual void Write(long value)
        {
            if (Host.EncodeInt)
                WriteEncoded(value);
            else
                WriteIntBytes(BitConverter.GetBytes(value));
        }

        /// <summary>判断字节顺序</summary>
        /// <param name="buffer">缓冲区</param>
        void WriteIntBytes(byte[] buffer)
        {
            if (buffer == null || buffer.Length < 1) return;

            // 如果不是小端字节顺序，则倒序
            if (!Host.IsLittleEndian) Array.Reverse(buffer);

            Write(buffer, 0, buffer.Length);
        }
        #endregion

        #region 无符号整数
        /// <summary>将 2 字节无符号整数写入当前流，并将流的位置提升 2 个字节。</summary>
        /// <param name="value">要写入的 2 字节无符号整数。</param>
        //[CLSCompliant(false)]
        public virtual void Write(ushort value) { Write((Int16)value); }

        /// <summary>将 4 字节无符号整数写入当前流，并将流的位置提升 4 个字节。</summary>
        /// <param name="value">要写入的 4 字节无符号整数。</param>
        //[CLSCompliant(false)]
        public virtual void Write(uint value) { Write((Int32)value); }

        /// <summary>将 8 字节无符号整数写入当前流，并将流的位置提升 8 个字节。</summary>
        /// <param name="value">要写入的 8 字节无符号整数。</param>
        //[CLSCompliant(false)]
        public virtual void Write(ulong value) { Write((Int64)value); }
        #endregion

        #region 浮点数
        /// <summary>将 4 字节浮点值写入当前流，并将流的位置提升 4 个字节。</summary>
        /// <param name="value">要写入的 4 字节浮点值。</param>
        public virtual void Write(float value) { Write(BitConverter.GetBytes(value), -1); }

        /// <summary>将 8 字节浮点值写入当前流，并将流的位置提升 8 个字节。</summary>
        /// <param name="value">要写入的 8 字节浮点值。</param>
        public virtual void Write(double value) { Write(BitConverter.GetBytes(value), -1); }
        #endregion

        #region 字符串
        /// <summary>将 Unicode 字符写入当前流，并根据所使用的 Encoding 和向流中写入的特定字符，提升流的当前位置。</summary>
        /// <param name="ch">要写入的非代理项 Unicode 字符。</param>
        public virtual void Write(char ch) { Write(Convert.ToByte(ch)); }

        /// <summary>将字符数组写入当前流，并根据所使用的 Encoding 和向流中写入的特定字符，提升流的当前位置。</summary>
        /// <param name="chars">包含要写入的数据的字符数组。</param>
        public virtual void Write(char[] chars) { Write(chars, 0, chars == null ? 0 : chars.Length); }

        /// <summary>将字符数组部分写入当前流，并根据所使用的 Encoding（可能还根据向流中写入的特定字符），提升流的当前位置。</summary>
        /// <param name="chars">包含要写入的数据的字符数组。</param>
        /// <param name="index">chars 中开始写入的起始点。</param>
        /// <param name="count">要写入的字符数。</param>
        public virtual void Write(char[] chars, int index, int count)
        {
            if (chars == null)
            {
                Host.WriteSize(0);
                return;
            }

            if (chars.Length < 1 || count <= 0 || index >= chars.Length)
            {
                Host.WriteSize(0);
                return;
            }

            // 先用写入字节长度
            var buffer = Host.Encoding.GetBytes(chars, index, count);
            Write(buffer);
        }

        /// <summary>写入字符串</summary>
        /// <param name="value">要写入的值。</param>
        public virtual void Write(String value) { Write(value == null ? null : value.ToCharArray()); }
        #endregion

        #region 时间日期
        /// <summary>将一个时间日期写入</summary>
        /// <param name="value">数值</param>
        public virtual void Write(DateTime value) { Write(value.Ticks); }
        #endregion

        #region 其它
        /// <summary>将单字节 Boolean 值写入</summary>
        /// <param name="value">要写入的 Boolean 值</param>
        public virtual void Write(Boolean value) { Write((Byte)(value ? 1 : 0)); }

        /// <summary>将一个十进制值写入当前流，并将流位置提升十六个字节。</summary>
        /// <param name="value">要写入的十进制值。</param>
        public virtual void Write(Decimal value)
        {
            Int32[] data = Decimal.GetBits(value);
            for (int i = 0; i < data.Length; i++)
            {
                Write(data[i]);
            }
        }

        /// <summary>写入Guid</summary>
        /// <param name="value">数值</param>
        public virtual void Write(Guid value) { Write(value.ToByteArray(), -1); }
        #endregion
        #endregion

        #region 7位压缩编码整数
        /// <summary>
        /// 以7位压缩格式写入32位整数，小于7位用1个字节，小于14位用2个字节。
        /// 由每次写入的一个字节的第一位标记后面的字节是否还是当前数据，所以每个字节实际可利用存储空间只有后7位。
        /// </summary>
        /// <param name="value">数值</param>
        /// <returns>实际写入字节数</returns>
        public Int32 WriteEncoded(Int16 value)
        {
            List<Byte> list = new List<Byte>();

            Int32 count = 1;
            UInt16 num = (UInt16)value;
            while (num >= 0x80)
            {
                list.Add((byte)(num | 0x80));
                num = (UInt16)(num >> 7);

                count++;
            }
            list.Add((byte)num);

            Write(list.ToArray(), 0, list.Count);

            return count;
        }

        /// <summary>
        /// 以7位压缩格式写入32位整数，小于7位用1个字节，小于14位用2个字节。
        /// 由每次写入的一个字节的第一位标记后面的字节是否还是当前数据，所以每个字节实际可利用存储空间只有后7位。
        /// </summary>
        /// <param name="value">数值</param>
        /// <returns>实际写入字节数</returns>
        public Int32 WriteEncoded(Int32 value)
        {
            List<Byte> list = new List<Byte>();

            Int32 count = 1;
            UInt32 num = (UInt32)value;
            while (num >= 0x80)
            {
                list.Add((byte)(num | 0x80));
                num = num >> 7;

                count++;
            }
            list.Add((byte)num);

            Write(list.ToArray(), 0, list.Count);

            return count;
        }

        /// <summary>
        /// 以7位压缩格式写入64位整数，小于7位用1个字节，小于14位用2个字节。
        /// 由每次写入的一个字节的第一位标记后面的字节是否还是当前数据，所以每个字节实际可利用存储空间只有后7位。
        /// </summary>
        /// <param name="value">数值</param>
        /// <returns>实际写入字节数</returns>
        public Int32 WriteEncoded(Int64 value)
        {
            List<Byte> list = new List<Byte>();

            Int32 count = 1;
            UInt64 num = (UInt64)value;
            while (num >= 0x80)
            {
                list.Add((byte)(num | 0x80));
                num = num >> 7;

                count++;
            }
            list.Add((byte)num);

            Write(list.ToArray(), 0, list.Count);

            return count;
        }

        /// <summary>获取整数编码后所占字节数</summary>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static Int32 GetEncodedIntSize(Int64 value)
        {
            Int32 count = 1;
            UInt64 num = (UInt64)value;
            while (num >= 0x80)
            {
                num = num >> 7;

                count++;
            }

            return count;
        }
        #endregion
    }
}