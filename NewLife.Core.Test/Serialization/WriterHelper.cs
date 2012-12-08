using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NewLife.Core.Test.Serialization
{
    static class WriterHelper
    {
        #region 字符串
        public static BinaryWriter WriteString(this BinaryWriter writer, String str, Encoding encoding, Boolean encodeSize)
        {
            var buf = encoding.GetBytes(str);
            writer.WriteInt(buf.Length, encodeSize);
            writer.Write(buf, 0, buf.Length);

            return writer;
        }
        #endregion

        #region 编码整数
        public static BinaryWriter WriteInt(this BinaryWriter writer, Int16 n, Boolean encodeInt)
        {
            if (!encodeInt)
                writer.Write(n);
            else
                writer.Write(GetEncoded(n));

            return writer;
        }
        public static BinaryWriter WriteInt(this BinaryWriter writer, UInt16 n, Boolean encodeInt)
        {
            if (!encodeInt)
                writer.Write(n);
            else
                writer.Write(GetEncoded(n));

            return writer;
        }
        public static BinaryWriter WriteInt(this BinaryWriter writer, Int32 n, Boolean encodeInt)
        {
            if (!encodeInt)
                writer.Write(n);
            else
                writer.Write(GetEncoded(n));

            return writer;
        }
        public static BinaryWriter WriteInt(this BinaryWriter writer, UInt32 n, Boolean encodeInt)
        {
            if (!encodeInt)
                writer.Write(n);
            else
                writer.Write(GetEncoded(n));

            return writer;
        }
        public static BinaryWriter WriteInt(this BinaryWriter writer, Int64 n, Boolean encodeInt)
        {
            if (!encodeInt)
                writer.Write(n);
            else
                writer.Write(GetEncoded(n));

            return writer;
        }
        public static BinaryWriter WriteInt(this BinaryWriter writer, UInt64 n, Boolean encodeInt)
        {
            if (!encodeInt)
                writer.Write(n);
            else
                writer.Write(GetEncoded(n));

            return writer;
        }
        #endregion

        #region 7位压缩编码整数
        static Byte[] GetEncoded(Int16 value) { return GetEncoded((UInt16)value); }
        static Byte[] GetEncoded(UInt16 value) { return GetEncoded((UInt64)value); }
        static Byte[] GetEncoded(Int32 value) { return GetEncoded((UInt32)value); }
        static Byte[] GetEncoded(UInt32 value) { return GetEncoded((UInt64)value); }
        static Byte[] GetEncoded(Int64 value) { return GetEncoded((UInt64)value); }

        /// <summary>
        /// 以7位压缩格式写入64位整数，小于7位用1个字节，小于14位用2个字节。
        /// 由每次写入的一个字节的第一位标记后面的字节是否还是当前数据，所以每个字节实际可利用存储空间只有后7位。
        /// </summary>
        /// <param name="value"></param>
        /// <returns>实际写入字节数</returns>
        static Byte[] GetEncoded(UInt64 value)
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

            return list.ToArray();
        }
        #endregion
    }
}