using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace NewLife.Serialization.Protocol
{
    /// <summary>
    /// 二进制协议读取器
    /// </summary>
    public class ProtocolBinaryReader : BinaryReader
    {
        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="stream"></param>
        public ProtocolBinaryReader(Stream stream) : base(stream) { }

        /// <summary>
        /// 以压缩格式读取32位整数
        /// </summary>
        /// <returns></returns>
        public Int32 ReadEncodeInt32()
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
        public Int64 ReadEncodeInt64()
        {
            Byte b;
            Int64 rs = 0;
            Int32 n = 0;
            while (true)
            {
                b = ReadByte();
                rs += (b & 0x7f) << n;
                if ((b & 0x80) == 0) break;

                n += 7;
                if (n >= 64) throw new FormatException("数字值过大，无法使用压缩格式读取！");
            }
            return rs;
        }
    }
}
