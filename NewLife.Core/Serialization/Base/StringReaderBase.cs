using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace NewLife.Serialization
{
    /// <summary>
    /// 字符串类型读取器基类
    /// </summary>
    /// <typeparam name="TSettings">设置类</typeparam>
    public class StringReaderBase<TSettings> : ReaderBase<TSettings>, IReader where TSettings : StringReaderWriterSetting, new()
    {
        #region 基础元数据
        #region 字节
        /// <summary>
        /// 从当前流中读取下一个字节，并使流的当前位置提升 1 个字节。
        /// </summary>
        /// <returns></returns>
        public override byte ReadByte() { return ReadBytes(1)[0]; }

        /// <summary>
        /// 从当前流中将 count 个字节读入字节数组，并使当前位置提升 count 个字节。
        /// </summary>
        /// <param name="count">要读取的字节数。</param>
        /// <returns></returns>
        public override byte[] ReadBytes(int count)
        {
            if (count <= 0) return null;

            Byte[] buffer = new Byte[count];
            //Int32 n = Reader.ReadContentAsBase64(buffer, 0, count);
            String str = ReadString();
            if (str == null) return null;
            if (str.Length == 0) return new Byte[] { };

            if (Settings.UseBase64)
                return Convert.FromBase64String(str);
            else
                return FromHex(str);

            //if (n == count) return buffer;

            //Byte[] data = new Byte[n];
            //Buffer.BlockCopy(buffer, 0, data, 0, n);

            //return data;
        }

        /// <summary>
        /// 解密
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        static Byte[] FromHex(String data)
        {
            if (String.IsNullOrEmpty(data)) return null;

            Byte[] bts = new Byte[data.Length / 2];
            for (int i = 0; i < data.Length / 2; i++)
            {
                bts[i] = (Byte)Convert.ToInt32(data.Substring(2 * i, 2), 16);
            }
            return bts;
        }
        #endregion

        #region 有符号整数
        /// <summary>
        /// 从当前流中读取 2 字节有符号整数，并使流的当前位置提升 2 个字节。
        /// </summary>
        /// <returns></returns>
        public override short ReadInt16() { return (Int16)ReadInt32(); }

        /// <summary>
        /// 从当前流中读取 4 字节有符号整数，并使流的当前位置提升 4 个字节。
        /// </summary>
        /// <returns></returns>
        public override int ReadInt32() { return (Int32)ReadInt64(); }

        /// <summary>
        /// 从当前流中读取 8 字节有符号整数，并使流的当前位置向前移动 8 个字节。
        /// </summary>
        /// <returns></returns>
        public override long ReadInt64() { return Int64.Parse(ReadString()); }
        #endregion

        #region 浮点数
        /// <summary>
        /// 从当前流中读取 4 字节浮点值，并使流的当前位置提升 4 个字节。
        /// </summary>
        /// <returns></returns>
        public override float ReadSingle() { return (Single)ReadDouble(); }

        /// <summary>
        /// 从当前流中读取 8 字节浮点值，并使流的当前位置提升 8 个字节。
        /// </summary>
        /// <returns></returns>
        public override double ReadDouble() { return Double.Parse(ReadString()); }
        #endregion

        #region 字符串
        /// <summary>
        /// 从当前流中读取 count 个字符，以字符数组的形式返回数据，并根据所使用的 Encoding 和从流中读取的特定字符，提升当前位置。
        /// </summary>
        /// <param name="count">要读取的字符数。</param>
        /// <returns></returns>
        public override char[] ReadChars(int count)
        {
            String str = ReadString();
            if (str == null) return null;

            return str.ToCharArray();
        }

        /// <summary>
        /// 从当前流中读取一个字符串。字符串有长度前缀，一次 7 位地被编码为整数。
        /// </summary>
        /// <returns></returns>
        public override string ReadString() { throw new NotImplementedException(); }
        #endregion

        #region 其它
        /// <summary>
        /// 从当前流中读取 Boolean 值，并使该流的当前位置提升 1 个字节。
        /// </summary>
        /// <returns></returns>
        public override bool ReadBoolean() { return Boolean.Parse(ReadString()); }

        /// <summary>
        /// 从当前流中读取十进制数值，并将该流的当前位置提升十六个字节。
        /// </summary>
        /// <returns></returns>
        public override decimal ReadDecimal() { return Decimal.Parse(ReadString()); }

        /// <summary>
        /// 读取一个时间日期
        /// </summary>
        /// <returns></returns>
        public override DateTime ReadDateTime() { return DateTime.Parse(ReadString()); }
        #endregion
        #endregion

        #region 扩展类型
        /// <summary>
        /// 读取Guid
        /// </summary>
        /// <returns></returns>
        public override Guid ReadGuid()
        {
            return new Guid(ReadString());
        }

        /// <summary>
        /// 读取IPAddress
        /// </summary>
        /// <returns></returns>
        public override IPAddress ReadIPAddress()
        {
            return base.ReadIPAddress();
        }

        /// <summary>
        /// 读取IPEndPoint
        /// </summary>
        /// <returns></returns>
        public override IPEndPoint ReadIPEndPoint()
        {
            return base.ReadIPEndPoint();
        }
        #endregion
    }
}