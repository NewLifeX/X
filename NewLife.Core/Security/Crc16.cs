using System;
using System.IO;

namespace NewLife.Security
{
    /// <summary>CRC16校验</summary>
    public sealed class Crc16
    {
        #region 数据表
        /// <summary>CRC16表</summary>
        static readonly UInt16[] CrcTable = new UInt16[256]
        {      
            /* CRC16 余式表 */
            0x0000,0x1021,0x2042,0x3063,0x4084,0x50A5,0x60C6,0x70E7,
            0x8108,0x9129,0xA14A,0xB16B,0xC18C,0xD1AD,0xE1CE,0xF1EF,
            0x1231,0x0210,0x3273,0x2252,0x52B5,0x4294,0x72F7,0x62D6,
            0x9339,0x8318,0xB37B,0xA35A,0xD3BD,0xC39C,0xF3FF,0xE3DE,
            0x2462,0x3443,0x0420,0x1401,0x64E6,0x74C7,0x44A4,0x5485,
            0xA56A,0xB54B,0x8528,0x9509,0xE5EE,0xF5CF,0xC5AC,0xD58D,
            0x3653,0x2672,0x1611,0x0630,0x76D7,0x66F6,0x5695,0x46B4,
            0xB75B,0xA77A,0x9719,0x8738,0xF7DF,0xE7FE,0xD79D,0xC7BC,
            0x48C4,0x58E5,0x6886,0x78A7,0x0840,0x1861,0x2802,0x3823,
            0xC9CC,0xD9ED,0xE98E,0xF9AF,0x8948,0x9969,0xA90A,0xB92B,
            0x5AF5,0x4AD4,0x7AB7,0x6A96,0x1A71,0x0A50,0x3A33,0x2A12,
            0xDBFD,0xCBDC,0xFBBF,0xEB9E,0x9B79,0x8B58,0xBB3B,0xAB1A,
            0x6CA6,0x7C87,0x4CE4,0x5CC5,0x2C22,0x3C03,0x0C60,0x1C41,
            0xEDAE,0xFD8F,0xCDEC,0xDDCD,0xAD2A,0xBD0B,0x8D68,0x9D49,
            0x7E97,0x6EB6,0x5ED5,0x4EF4,0x3E13,0x2E32,0x1E51,0x0E70,
            0xFF9F,0xEFBE,0xDFDD,0xCFFC,0xBF1B,0xAF3A,0x9F59,0x8F78,
            0x9188,0x81A9,0xB1CA,0xA1EB,0xD10C,0xC12D,0xF14E,0xE16F,
            0x1080,0x00A1,0x30C2,0x20E3,0x5004,0x4025,0x7046,0x6067,
            0x83B9,0x9398,0xA3FB,0xB3DA,0xC33D,0xD31C,0xE37F,0xF35E,
            0x02B1,0x1290,0x22F3,0x32D2,0x4235,0x5214,0x6277,0x7256,
            0xB5EA,0xA5CB,0x95A8,0x8589,0xF56E,0xE54F,0xD52C,0xC50D,
            0x34E2,0x24C3,0x14A0,0x0481,0x7466,0x6447,0x5424,0x4405,
            0xA7DB,0xB7FA,0x8799,0x97B8,0xE75F,0xF77E,0xC71D,0xD73C,
            0x26D3,0x36F2,0x0691,0x16B0,0x6657,0x7676,0x4615,0x5634,
            0xD94C,0xC96D,0xF90E,0xE92F,0x99C8,0x89E9,0xB98A,0xA9AB,
            0x5844,0x4865,0x7806,0x6827,0x18C0,0x08E1,0x3882,0x28A3,
            0xCB7D,0xDB5C,0xEB3F,0xFB1E,0x8BF9,0x9BD8,0xABBB,0xBB9A,
            0x4A75,0x5A54,0x6A37,0x7A16,0x0AF1,0x1AD0,0x2AB3,0x3A92,
            0xFD2E,0xED0F,0xDD6C,0xCD4D,0xBDAA,0xAD8B,0x9DE8,0x8DC9,
            0x7C26,0x6C07,0x5C64,0x4C45,0x3CA2,0x2C83,0x1CE0,0x0CC1,
            0xEF1F,0xFF3E,0xCF5D,0xDF7C,0xAF9B,0xBFBA,0x8FD9,0x9FF8,
            0x6E17,0x7E36,0x4E55,0x5E74,0x2E93,0x3EB2,0x0ED1,0x1EF0
        };
        #endregion

        ///// <summary>取得CRC16校验码</summary>
        ///// <param name="pcrc"></param>
        ///// <returns></returns>
        //public UInt16 CRC16Code(byte[] pcrc)
        //{
        //    UInt16 crc16 = 0;

        //    for (int i = 0; i < pcrc.Length; ++i)
        //    {
        //        crc16 = (UInt16)((crc16 << 8) ^ CrcTable[((crc16 >> 8) ^ pcrc[i])]);
        //    }
        //    return crc16;
        //}

        /// <summary>校验值</summary>
        UInt16 crc = 0xFFFF;
        /// <summary>校验值</summary>
        public UInt16 Value { get { return crc; } set { crc = value; } }

        /// <summary>重置清零</summary>
        public Crc16 Reset() { crc = 0xFFFF; return this; }

        /// <summary>添加整数进行校验</summary>
        /// <param name = "value">
        /// the byte is taken as the lower 8 bits of value
        /// </param>
        public Crc16 Update(Int16 value)
        {
            //crc = CrcTable[(crc ^ value) & 0xFF] ^ (crc >> 8);
            crc = (UInt16)((crc << 8) ^ CrcTable[((crc >> 8) ^ value)]);

            return this;
        }

        /// <summary>添加字节数组进行校验  CRC16-CCITT x16+x12+x5+1 1021  ISO HDLC, ITU X.25, V.34/V.41/V.42, PPP-FCS</summary>
        /// <param name = "buffer">
        /// The buffer which contains the data
        /// </param>
        /// <param name = "offset">
        /// The offset in the buffer where the data starts
        /// </param>
        /// <param name = "count">
        /// The number of data bytes to update the CRC with.
        /// </param>
        public Crc16 Update(Byte[] buffer, Int32 offset = 0, Int32 count = -1)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");
            //if (count < 0) throw new ArgumentOutOfRangeException("count", "Count不能小于0！");
            if (count <= 0) count = buffer.Length;
            if (offset < 0 || offset + count > buffer.Length) throw new ArgumentOutOfRangeException("offset");

            //while (--count >= 0)
            //{
            //    crc = CrcTable[(crc ^ buffer[offset++]) & 0xFF] ^ (crc >> 8);
            //}
            //crc16 = (UInt16)((crc16 << 8) ^ CrcTable[((crc16 >> 8) ^ pcrc[i])]);
            crc ^= crc;
            for (var i = 0; i < count; i++)
            {
                crc = (UInt16)((crc << 8) ^ CrcTable[(crc >> 8 ^ buffer[offset + i]) & 0xFF]);
            }
            //crc ^= crc;

            return this;
        }

        /// <summary>添加数据流进行校验  CRC-16 x16+x15+x2+1 8005 IBM SDLC</summary>
        /// <param name="stream"></param>
        /// <param name="count">数量</param>
        public Crc16 Update(Stream stream, Int64 count = -1)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            //if (count < 0) throw new ArgumentOutOfRangeException("count", "Count不能小于0！");
            if (count <= 0) count = Int64.MaxValue;

            while (--count >= 0)
            {
                var b = stream.ReadByte();
                if (b == -1) break;

                //crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
                //crc = (UInt16)((crc << 8) ^ CrcTable[(crc ^ b) & 0xFF]);
                crc ^= (Byte)b;
                for (var i = 0; i < 8; i++)
                {
                    if ((crc & 0x0001) != 0)
                        crc = (UInt16)((crc >> 1) ^ 0xa001);
                    else
                        crc = (UInt16)(crc >> 1);
                }
            }

            return this;
        }

        /// <summary>计算校验码</summary>
        /// <param name="buf"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static UInt16 Compute(Byte[] buf, Int32 offset = 0, Int32 count = -1)
        {
            var crc = new Crc16();
            crc.Update(buf, offset, count);
            return crc.Value;
        }

        /// <summary>计算数据流校验码</summary>
        /// <param name="stream"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static UInt16 Compute(Stream stream, Int32 count = -1)
        {
            var crc = new Crc16();
            crc.Update(stream, count);
            return crc.Value;
        }

        /// <summary>计算数据流校验码，指定起始位置和字节数偏移量</summary>
        /// <remarks>
        /// 一般用于计算数据包校验码，需要回过头去开始校验，并且可能需要跳过最后的校验码长度。
        /// position小于0时，数据流从当前位置开始计算校验；
        /// position大于等于0时，数据流移到该位置开始计算校验，最后由count决定可能差几个字节不参与计算；
        /// </remarks>
        /// <param name="stream"></param>
        /// <param name="position">如果大于等于0，则表示从该位置开始计算</param>
        /// <param name="count">字节数偏移量，一般用负数表示</param>
        /// <returns></returns>
        public static UInt16 Compute(Stream stream, Int64 position = -1, Int32 count = -1)
        {
            if (position >= 0)
            {
                if (count > 0) count = -count;
                count += (Int32)(stream.Position - position);
                stream.Position = position;
            }

            var crc = new Crc16();
            crc.Update(stream, count);
            return crc.Value;
        }
    }
}