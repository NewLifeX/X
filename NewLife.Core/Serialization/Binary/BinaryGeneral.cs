using System;
using System.Collections.Generic;

namespace NewLife.Serialization
{
    /// <summary>二进制基础类型处理器</summary>
    public class BinaryGeneral : BinaryHandlerBase
    {
        /// <summary>实例化</summary>
        public BinaryGeneral()
        {
            Priority = 10;
        }

        /// <summary>写入一个对象</summary>
        /// <param name="value">目标对象</param>
        /// <param name="type">类型</param>
        /// <returns>是否处理成功</returns>
        public override Boolean Write(Object value, Type type)
        {
            if (value == null && type != typeof(String)) return false;

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    Host.Write((Byte)((Boolean)value ? 1 : 0));
                    return true;
                case TypeCode.Byte:
                case TypeCode.SByte:
                    Host.Write((Byte)value);
                    return true;
                case TypeCode.Char:
                    Write((Char)value);
                    return true;
                case TypeCode.DBNull:
                case TypeCode.Empty:
                    Host.Write(0);
                    return true;
                case TypeCode.DateTime:
                    Write(((DateTime)value).ToInt());
                    return true;
                case TypeCode.Decimal:
                    Write((Decimal)value);
                    return true;
                case TypeCode.Double:
                    Write((Double)value);
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

            var code = Type.GetTypeCode(type);
            switch (code)
            {
                case TypeCode.Boolean:
                    value = Host.ReadByte() > 0;
                    return true;
                case TypeCode.Byte:
                case TypeCode.SByte:
                    value = Host.ReadByte();
                    return true;
                case TypeCode.Char:
                    value = ReadChar();
                    return true;
                case TypeCode.DBNull:
                    value = DBNull.Value;
                    return true;
                case TypeCode.DateTime:
                    value = ReadInt32().ToDateTime();
                    return true;
                case TypeCode.Decimal:
                    value = ReadDecimal();
                    return true;
                case TypeCode.Double:
                    value = ReadDouble();
                    return true;
                case TypeCode.Empty:
                    value = null;
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

            return false;
        }

        #region 基元类型写入
        #region 字节
        /// <summary>将一个无符号字节写入</summary>
        /// <param name="value">要写入的无符号字节。</param>
        public virtual void Write(Byte value)
        {
            Host.Write(value);
        }

        /// <summary>将字节数组写入，如果设置了UseSize，则先写入数组长度。</summary>
        /// <param name="buffer">包含要写入的数据的字节数组。</param>
        public virtual void Write(Byte[] buffer)
        {
            // 可能因为FieldSize设定需要补充0字节
            if (buffer == null || buffer.Length == 0)
            {
                var size = Host.WriteSize(0);
                if (size > 0) Host.Write(new Byte[size], 0, -1);
            }
            else
            {
                var size = Host.WriteSize(buffer.Length);
                if (size > 0)
                {
                    // 写入数据，超长截断，不足补0
                    if (buffer.Length >= size)
                        Host.Write(buffer, 0, size);
                    else
                    {
                        Host.Write(buffer, 0, buffer.Length);
                        Host.Write(new Byte[size - buffer.Length], 0, -1);
                    }
                }
                else
                {
                    // 非FieldSize写入
                    Host.Write(buffer, 0, buffer.Length);
                }
            }
        }

        /// <summary>将字节数组部分写入当前流，不写入数组长度。</summary>
        /// <param name="buffer">包含要写入的数据的字节数组。</param>
        /// <param name="offset">buffer 中开始写入的起始点。</param>
        /// <param name="count">要写入的字节数。</param>
        public virtual void Write(Byte[] buffer, Int32 offset, Int32 count)
        {
            if (buffer == null || buffer.Length < 1 || count <= 0 || offset >= buffer.Length) return;

            Host.Write(buffer, offset, count);
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
        public virtual void Write(Int16 value)
        {
            if (Host.EncodeInt)
                WriteEncoded(value);
            else
                WriteIntBytes(BitConverter.GetBytes(value));
        }

        /// <summary>将 4 字节有符号整数写入当前流，并将流的位置提升 4 个字节。</summary>
        /// <param name="value">要写入的 4 字节有符号整数。</param>
        public virtual void Write(Int32 value)
        {
            if (Host.EncodeInt)
                WriteEncoded(value);
            else
                WriteIntBytes(BitConverter.GetBytes(value));
        }

        /// <summary>将 8 字节有符号整数写入当前流，并将流的位置提升 8 个字节。</summary>
        /// <param name="value">要写入的 8 字节有符号整数。</param>
        public virtual void Write(Int64 value)
        {
            if (Host.EncodeInt)
                WriteEncoded(value);
            else
                WriteIntBytes(BitConverter.GetBytes(value));
        }

        /// <summary>判断字节顺序</summary>
        /// <param name="buffer">缓冲区</param>
        void WriteIntBytes(Byte[] buffer)
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
        public virtual void Write(UInt16 value) { Write((Int16)value); }

        /// <summary>将 4 字节无符号整数写入当前流，并将流的位置提升 4 个字节。</summary>
        /// <param name="value">要写入的 4 字节无符号整数。</param>
        //[CLSCompliant(false)]
        public virtual void Write(UInt32 value) { Write((Int32)value); }

        /// <summary>将 8 字节无符号整数写入当前流，并将流的位置提升 8 个字节。</summary>
        /// <param name="value">要写入的 8 字节无符号整数。</param>
        //[CLSCompliant(false)]
        public virtual void Write(UInt64 value) { Write((Int64)value); }
        #endregion

        #region 浮点数
        /// <summary>将 4 字节浮点值写入当前流，并将流的位置提升 4 个字节。</summary>
        /// <param name="value">要写入的 4 字节浮点值。</param>
        public virtual void Write(Single value) { Write(BitConverter.GetBytes(value), -1); }

        /// <summary>将 8 字节浮点值写入当前流，并将流的位置提升 8 个字节。</summary>
        /// <param name="value">要写入的 8 字节浮点值。</param>
        public virtual void Write(Double value) { Write(BitConverter.GetBytes(value), -1); }

        /// <summary>将一个十进制值写入当前流，并将流位置提升十六个字节。</summary>
        /// <param name="value">要写入的十进制值。</param>
        protected virtual void Write(Decimal value)
        {
            var data = Decimal.GetBits(value);
            for (var i = 0; i < data.Length; i++)
            {
                Write(data[i]);
            }
        }
        #endregion

        #region 字符串
        /// <summary>将 Unicode 字符写入当前流，并根据所使用的 Encoding 和向流中写入的特定字符，提升流的当前位置。</summary>
        /// <param name="ch">要写入的非代理项 Unicode 字符。</param>
        public virtual void Write(Char ch) { Write(Convert.ToByte(ch)); }

        /// <summary>将字符数组部分写入当前流，并根据所使用的 Encoding（可能还根据向流中写入的特定字符），提升流的当前位置。</summary>
        /// <param name="chars">包含要写入的数据的字符数组。</param>
        /// <param name="index">chars 中开始写入的起始点。</param>
        /// <param name="count">要写入的字符数。</param>
        public virtual void Write(Char[] chars, Int32 index, Int32 count)
        {
            if (chars == null)
            {
                //Host.WriteSize(0);
                // 可能因为FieldSize设定需要补充0字节
                Write(new Byte[0]);
                return;
            }

            if (chars.Length < 1 || count <= 0 || index >= chars.Length)
            {
                //Host.WriteSize(0);
                // 可能因为FieldSize设定需要补充0字节
                Write(new Byte[0]);
                return;
            }

            // 先用写入字节长度
            var buffer = Host.Encoding.GetBytes(chars, index, count);
            Write(buffer);
        }

        /// <summary>写入字符串</summary>
        /// <param name="value">要写入的值。</param>
        public virtual void Write(String value)
        {
            if (value == null || value.Length == 0)
            {
                //Host.WriteSize(0);
                Write(new Byte[0]);
                return;
            }

            // 先用写入字节长度
            var buffer = Host.Encoding.GetBytes(value);
            Write(buffer);
        }
        #endregion
        #endregion

        #region 基元类型读取
        #region 字节
        /// <summary>从当前流中读取下一个字节，并使流的当前位置提升 1 个字节。</summary>
        /// <returns></returns>
        public virtual Byte ReadByte() { return Host.ReadByte(); }

        /// <summary>从当前流中将 count 个字节读入字节数组，如果count小于0，则先读取字节数组长度。</summary>
        /// <param name="count">要读取的字节数。</param>
        /// <returns></returns>
        public virtual Byte[] ReadBytes(Int32 count)
        {
            if (count < 0) count = Host.ReadSize();

            if (count <= 0) return null;

            if (count > 1024 * 2) throw new XException("安全需要，不允许读取超大变长数组 {0:n0}>{1:n0}", count, 1024 * 2);

            var buffer = Host.ReadBytes(count);

            return buffer;
        }
        #endregion

        #region 有符号整数
        /// <summary>读取整数的字节数组，某些写入器（如二进制写入器）可能需要改变字节顺序</summary>
        /// <param name="count">数量</param>
        /// <returns></returns>
        protected virtual Byte[] ReadIntBytes(Int32 count)
        {
            var buffer = ReadBytes(count);

            // 如果不是小端字节顺序，则倒序
            if (!Host.IsLittleEndian) Array.Reverse(buffer);

            return buffer;
        }

        /// <summary>从当前流中读取 2 字节有符号整数，并使流的当前位置提升 2 个字节。</summary>
        /// <returns></returns>
        public virtual Int16 ReadInt16()
        {
            if (Host.EncodeInt)
                return ReadEncodedInt16();
            else
                return BitConverter.ToInt16(ReadIntBytes(2), 0);
        }

        /// <summary>从当前流中读取 4 字节有符号整数，并使流的当前位置提升 4 个字节。</summary>
        /// <returns></returns>
        public virtual Int32 ReadInt32()
        {
            if (Host.EncodeInt)
                return ReadEncodedInt32();
            else
                return BitConverter.ToInt32(ReadIntBytes(4), 0);
        }

        /// <summary>从当前流中读取 8 字节有符号整数，并使流的当前位置向前移动 8 个字节。</summary>
        /// <returns></returns>
        public virtual Int64 ReadInt64()
        {
            if (Host.EncodeInt)
                return ReadEncodedInt64();
            else
                return BitConverter.ToInt64(ReadIntBytes(8), 0);
        }
        #endregion

        #region 无符号整数
        /// <summary>使用 Little-Endian 编码从当前流中读取 2 字节无符号整数，并将流的位置提升 2 个字节。</summary>
        /// <returns></returns>
        //[CLSCompliant(false)]
        public virtual UInt16 ReadUInt16() { return (UInt16)ReadInt16(); }

        /// <summary>从当前流中读取 4 字节无符号整数并使流的当前位置提升 4 个字节。</summary>
        /// <returns></returns>
        //[CLSCompliant(false)]
        public virtual UInt32 ReadUInt32() { return (UInt32)ReadInt32(); }

        /// <summary>从当前流中读取 8 字节无符号整数并使流的当前位置提升 8 个字节。</summary>
        /// <returns></returns>
        //[CLSCompliant(false)]
        public virtual UInt64 ReadUInt64() { return (UInt64)ReadInt64(); }
        #endregion

        #region 浮点数
        /// <summary>从当前流中读取 4 字节浮点值，并使流的当前位置提升 4 个字节。</summary>
        /// <returns></returns>
        public virtual Single ReadSingle() { return BitConverter.ToSingle(ReadBytes(4), 0); }

        /// <summary>从当前流中读取 8 字节浮点值，并使流的当前位置提升 8 个字节。</summary>
        /// <returns></returns>
        public virtual Double ReadDouble() { return BitConverter.ToDouble(ReadBytes(8), 0); }
        #endregion

        #region 字符串
        /// <summary>从当前流中读取下一个字符，并根据所使用的 Encoding 和从流中读取的特定字符，提升流的当前位置。</summary>
        /// <returns></returns>
        public virtual Char ReadChar() => Convert.ToChar(ReadByte());

        /// <summary>从当前流中读取一个字符串。字符串有长度前缀，一次 7 位地被编码为整数。</summary>
        /// <returns></returns>
        public virtual String ReadString()
        {
            // 先读长度
            var n = Host.ReadSize();
            if (n <= 0) return null;
            //if (n == 0) return String.Empty;

            var buffer = ReadBytes(n);

            return Host.Encoding.GetString(buffer);
        }
        #endregion

        #region 其它
        /// <summary>从当前流中读取十进制数值，并将该流的当前位置提升十六个字节。</summary>
        /// <returns></returns>
        public virtual Decimal ReadDecimal()
        {
            var data = new Int32[4];
            for (var i = 0; i < data.Length; i++)
            {
                data[i] = ReadInt32();
            }
            return new Decimal(data);
        }
        #endregion

        #region 7位压缩编码整数
        /// <summary>以压缩格式读取16位整数</summary>
        /// <returns></returns>
        public Int16 ReadEncodedInt16()
        {
            Byte b;
            Int16 rs = 0;
            Byte n = 0;
            while (true)
            {
                b = ReadByte();
                // 必须转为Int16，否则可能溢出
                rs += (Int16)((b & 0x7f) << n);
                if ((b & 0x80) == 0) break;

                n += 7;
                if (n >= 16) throw new FormatException("数字值过大，无法使用压缩格式读取！");
            }
            return rs;
        }

        /// <summary>以压缩格式读取32位整数</summary>
        /// <returns></returns>
        public Int32 ReadEncodedInt32()
        {
            Byte b;
            var rs = 0;
            Byte n = 0;
            while (true)
            {
                b = ReadByte();
                // 必须转为Int32，否则可能溢出
                rs += (b & 0x7f) << n;
                if ((b & 0x80) == 0) break;

                n += 7;
                if (n >= 32) throw new FormatException("数字值过大，无法使用压缩格式读取！");
            }
            return rs;
        }

        /// <summary>以压缩格式读取64位整数</summary>
        /// <returns></returns>
        public Int64 ReadEncodedInt64()
        {
            Byte b;
            Int64 rs = 0;
            Byte n = 0;
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
        #endregion

        #region 7位压缩编码整数
        /// <summary>
        /// 以7位压缩格式写入64位整数，小于7位用1个字节，小于14位用2个字节。
        /// 由每次写入的一个字节的第一位标记后面的字节是否还是当前数据，所以每个字节实际可利用存储空间只有后7位。
        /// </summary>
        /// <param name="value">数值</param>
        /// <returns>实际写入字节数</returns>
        public Int32 WriteEncoded(Int64 value)
        {
            var arr = new Byte[16];
            var k = 0;

            var count = 1;
            var num = (UInt64)value;
            while (num >= 0x80)
            {
                arr[k++] = (Byte)(num | 0x80);
                num = num >> 7;

                count++;
            }
            arr[k++] = (Byte)num;

            Write(arr, 0, k);

            return count;
        }
        #endregion
    }
}