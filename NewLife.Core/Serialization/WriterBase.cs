using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

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
            if (chars == null || chars.Length < 1 || count <= 0 || index >= chars.Length) return;

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
            return false;
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
        #endregion
    }
}