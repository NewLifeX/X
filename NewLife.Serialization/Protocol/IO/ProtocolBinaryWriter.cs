using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace NewLife.Serialization.Protocol
{
    /// <summary>
    /// 二进制协议写入器
    /// </summary>
    public class ProtocolBinaryWriter : BinaryWriter
    {
        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="stream"></param>
        public ProtocolBinaryWriter(Stream stream) : base(stream) { }

        /// <summary>
        /// 以压缩格式写入32位整数
        /// </summary>
        /// <param name="value"></param>
        public void WriteEncodeInt32(Int32 value)
        {
            //Write7BitEncodedInt(value);

            uint num = (uint)value;
            while (num >= 0x80)
            {
                this.Write((byte)(num | 0x80));
                num = num >> 7;
            }
            this.Write((byte)num);
        }

        /// <summary>
        /// 以压缩格式写入64位整数
        /// </summary>
        /// <param name="value"></param>
        public void WriteEncodeInt64(Int64 value)
        {
            UInt64 num = (UInt64)value;
            while (num >= 0x80)
            {
                this.Write((byte)(num | 0x80));
                num = num >> 7;
            }
            this.Write((byte)num);
        }
    }
}
