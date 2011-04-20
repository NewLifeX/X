using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace NewLife.Serialization
{
    /// <summary>
    /// 二进制写入器
    /// </summary>
    public class BinaryWriterX : WriterBase
    {
        #region 属性
        private BinaryWriter _Writer;
        /// <summary>写入器</summary>
        public BinaryWriter Writer
        {
            get { return _Writer; }
            set { _Writer = value; }
        }

        private Boolean _IsLittleEndian = true;
        /// <summary>
        /// 是否小端字节序。
        /// </summary>
        /// <remarks>
        /// 网络协议都是Big-Endian；
        /// Java编译的都是Big-Endian；
        /// Motorola的PowerPC是Big-Endian；
        /// x86系列则采用Little-Endian方式存储数据；
        /// ARM同时支持 big和little，实际应用中通常使用Little-Endian。
        /// </remarks>
        public Boolean IsLittleEndian
        {
            get { return _IsLittleEndian; }
            set { _IsLittleEndian = value; }
        }
        #endregion

        /// <summary>
        /// 写入字节
        /// </summary>
        /// <param name="value"></param>
        public override void Write(byte value)
        {
            Writer.Write(value);
        }

        /// <summary>
        /// 判断字节顺序
        /// </summary>
        /// <param name="buffer"></param>
        protected override void WriteIntBytes(byte[] buffer)
        {
            if (buffer == null || buffer.Length < 1) return;

            // 如果不是小端字节顺序，则倒序
            if (!IsLittleEndian) Array.Reverse(buffer);

            base.WriteIntBytes(buffer);
        }

        /// <summary>
        /// 写入字符串，先用压缩编码写入长度
        /// </summary>
        /// <param name="value"></param>
        public override void Write(string value)
        {
            // 先用压缩编码写入长度
            Int64 n = String.IsNullOrEmpty(value) ? 0 : value.Length;
            WriteEncoded(n);
            Write(value == null ? null : value.ToCharArray());
        }

        #region 7位压缩编码整数
        /// <summary>
        /// 以7位压缩格式写入32位整数，小于7位用1个字节，小于14位用2个字节。
        /// 由每次写入的一个字节的第一位标记后面的字节是否还是当前数据，所以每个字节实际可利用存储空间只有后7位。
        /// </summary>
        /// <param name="value"></param>
        /// <returns>实际写入字节数</returns>
        public Int32 WriteEncoded(Int16 value)
        {
            Int32 count = 1;
            UInt16 num = (UInt16)value;
            while (num >= 0x80)
            {
                this.Write((byte)(num | 0x80));
                num = (UInt16)(num >> 7);

                count++;
            }
            this.Write((byte)num);

            return count;
        }

        /// <summary>
        /// 以7位压缩格式写入32位整数，小于7位用1个字节，小于14位用2个字节。
        /// 由每次写入的一个字节的第一位标记后面的字节是否还是当前数据，所以每个字节实际可利用存储空间只有后7位。
        /// </summary>
        /// <param name="value"></param>
        /// <returns>实际写入字节数</returns>
        public Int32 WriteEncoded(Int32 value)
        {
            Int32 count = 1;
            UInt32 num = (UInt32)value;
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
        /// 以7位压缩格式写入64位整数，小于7位用1个字节，小于14位用2个字节。
        /// 由每次写入的一个字节的第一位标记后面的字节是否还是当前数据，所以每个字节实际可利用存储空间只有后7位。
        /// </summary>
        /// <param name="value"></param>
        /// <returns>实际写入字节数</returns>
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
    }
}