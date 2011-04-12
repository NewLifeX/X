using System;
using System.Collections.Generic;
using System.Text;

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
        public Object ReadObject(Type type)
        {
            return null;
        }

        /// <summary>
        /// 尝试读取目标对象指定成员的值
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="value">对象</param>
        /// <returns>是否读取成功</returns>
        public Boolean TryReadObject(Type type, ref Object value)
        {
            return false;
        }

        ///// <summary>
        ///// 尝试读取目标对象指定成员的值，通过委托方法递归处理成员
        ///// </summary>
        ///// <param name="target">目标对象</param>
        ///// <param name="member">成员</param>
        ///// <param name="type">成员类型，以哪一种类型读取</param>
        ///// <param name="encodeInt">是否编码整数</param>
        ///// <param name="allowNull">是否允许空</param>
        ///// <param name="isProperty">是否处理属性</param>
        ///// <param name="value">成员值</param>
        ///// <returns>是否读取成功</returns>
        //Boolean TryReadObject(Object target, MemberInfoX member, Type type, Boolean encodeInt, Boolean allowNull, Boolean isProperty, out Object value);

        ///// <summary>
        ///// 尝试读取目标对象指定成员的值，处理基础类型、特殊类型、基础类型数组、特殊类型数组，通过委托方法处理成员
        ///// </summary>
        ///// <remarks>
        ///// 简单类型在value中返回，复杂类型直接填充target；
        ///// </remarks>
        ///// <param name="target">目标对象</param>
        ///// <param name="member">成员</param>
        ///// <param name="type">成员类型，以哪一种类型读取</param>
        ///// <param name="encodeInt">是否编码整数</param>
        ///// <param name="allowNull">是否允许空</param>
        ///// <param name="isProperty">是否处理属性</param>
        ///// <param name="value">成员值</param>
        ///// <param name="callback">处理成员的方法</param>
        ///// <returns>是否读取成功</returns>
        //Boolean TryReadObject(Object target, MemberInfoX member, Type type, Boolean encodeInt, Boolean allowNull, Boolean isProperty, out Object value, ReadCallback callback);
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
        #endregion
    }
}