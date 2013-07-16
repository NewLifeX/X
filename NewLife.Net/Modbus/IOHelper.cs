using System;

namespace NewLife.Net.Modbus
{
    /// <summary>IO操作助手</summary>
    static class IOHelper
    {
        /// <summary>从字节数组中读取一段数据</summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static Byte[] ReadBytes(this Byte[] data, Int32 offset = 0, Int32 count = -1)
        {
            if (count <= 0) count = data.Length - offset;

            var buf = new Byte[count];
#if MF
            Array.Copy(data, offset, buf, 0, count);
#else
            Buffer.BlockCopy(data, offset, buf, 0, count);
#endif
            return buf;
        }

        /// <summary>从字节数据指定位置读取一个无符号16位整数</summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static UInt16 ReadUInt16(this Byte[] data, Int32 offset = 0)
        {
            return (UInt16)((data[offset] << 8) + data[offset + 1]);
        }

        /// <summary>向字节数组的指定位置写入一个无符号16位整数</summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public static Byte[] WriteUInt16(this Byte[] data, Int32 offset, Int32 n)
        {
            // STM32单片机是小端
            //data[offset] = (Byte)(n & 0xFF);
            //data[offset + 1] = (Byte)(n >> 8);
            // Modbus协议规定大端
            data[offset] = (Byte)(n >> 8);
            data[offset + 1] = (Byte)(n & 0xFF);

            return data;
        }

        /// <summary>向字节数组写入一片数据</summary>
        /// <param name="data"></param>
        /// <param name="srcOffset"></param>
        /// <param name="buf"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static Byte[] Write(this Byte[] data, Int32 srcOffset, Byte[] buf, Int32 offset = 0, Int32 count = -1)
        {
            if (count <= 0) count = data.Length - offset;

#if MF
            Array.Copy(buf, srcOffset, data, offset, count);
#else
            Buffer.BlockCopy(buf, srcOffset, data, offset, count);
#endif
            return data;
        }

        #region CRC
        static readonly UInt16[] crc_ta = new UInt16[16] { 0x0000, 0xCC01, 0xD801, 0x1400, 0xF001, 0x3C00, 0x2800, 0xE401, 0xA001, 0x6C00, 0x7800, 0xB401, 0x5000, 0x9C01, 0x8801, 0x4400, };

        /// <summary>Crc校验</summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static UInt16 Crc(this Byte[] data, Int32 offset, int count = 0)
        {
            if (data == null || data.Length < 1) return 0;

            UInt16 u = 0xFFFF;
            byte b;

            if (count == 0) count = data.Length - offset;

            for (var i = offset; i < count; i++)
            {
                b = data[i];
                u = (UInt16)(crc_ta[(b ^ u) & 15] ^ (u >> 4));
                u = (UInt16)(crc_ta[((b >> 4) ^ u) & 15] ^ (u >> 4));
            }

            return u;
        }
        #endregion
    }
}