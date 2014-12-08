using System;
using System.IO;
//using System.Security.Cryptography;

namespace NewLife.Security
{

    /// <summary>CRC32校验</summary>
    /// <remarks>
    /// Generate a table for a byte-wise 32-bit CRC calculation on the polynomial:
    /// x^32+x^26+x^23+x^22+x^16+x^12+x^11+x^10+x^8+x^7+x^5+x^4+x^2+x+1.
    ///
    /// Polynomials over GF(2) are represented in binary, one bit per coefficient,
    /// with the lowest powers in the most significant bit.  Then adding polynomials
    /// is just exclusive-or, and multiplying a polynomial by x is a right shift by
    /// one.  If we call the above polynomial p, and represent a byte as the
    /// polynomial q, also with the lowest power in the most significant bit (so the
    /// byte 0xb1 is the polynomial x^7+x^3+x+1), then the CRC is (q*x^32) mod p,
    /// where a mod b means the remainder after dividing a by b.
    ///
    /// This calculation is done using the shift-register method of multiplying and
    /// taking the remainder.  The register is initialized to zero, and for each
    /// incoming bit, x^32 is added mod p to the register if the bit is a one (where
    /// x^32 mod p is p+x^32 = x^26+...+1), and the register is multiplied mod p by
    /// x (which is shifting right by one and adding x^32 mod p if the bit shifted
    /// out is a one).  We start with the highest power (least significant bit) of
    /// q and repeat for all eight bits of q.
    ///
    /// The table is simply the CRC of all possible eight bit values.  This is all
    /// the information needed to generate CRC's on data a byte at a time for all
    /// combinations of CRC register values and incoming bytes.
    /// </remarks>
    public sealed class Crc32 //: HashAlgorithm
    {
        const uint CrcSeed = 0xFFFFFFFF;

        #region 数据表
        /// <summary>校验表</summary>
        public readonly static uint[] Table;

        static Crc32()
        {
            Table = new uint[256];
            const uint kPoly = 0xEDB88320;
            for (uint i = 0; i < 256; i++)
            {
                uint r = i;
                for (int j = 0; j < 8; j++)
                    if ((r & 1) != 0)
                        r = (r >> 1) ^ kPoly;
                    else
                        r >>= 1;
                Table[i] = r;
            }
        }
        #endregion

        //internal static uint ComputeCrc32(uint oldCrc, byte value)
        //{
        //    return (uint)(Table[(oldCrc ^ value) & 0xFF] ^ (oldCrc >> 8));
        //}

        /// <summary>校验值</summary>
        uint crc = CrcSeed;
        /// <summary>校验值</summary>
        public UInt32 Value { get { return crc ^ CrcSeed; } set { crc = value ^ CrcSeed; } }

        /// <summary>重置清零</summary>
        public Crc32 Reset() { crc = CrcSeed; return this; }

        /// <summary>添加整数进行校验</summary>
        /// <param name = "value">
        /// the byte is taken as the lower 8 bits of value
        /// </param>
        public Crc32 Update(int value)
        {
            crc = Table[(crc ^ value) & 0xFF] ^ (crc >> 8);

            return this;
        }

        /// <summary>添加字节数组进行校验</summary>
        /// <param name = "buffer">
        /// The buffer which contains the data
        /// </param>
        /// <param name = "offset">
        /// The offset in the buffer where the data starts
        /// </param>
        /// <param name = "count">
        /// The number of data bytes to update the CRC with.
        /// </param>
        public Crc32 Update(byte[] buffer, int offset = 0, int count = 0)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");
            //if (count < 0) throw new ArgumentOutOfRangeException("count", "Count不能小于0！");
            if (count <= 0) count = buffer.Length;
            if (offset < 0 || offset + count > buffer.Length) throw new ArgumentOutOfRangeException("offset");

            while (--count >= 0)
            {
                crc = Table[(crc ^ buffer[offset++]) & 0xFF] ^ (crc >> 8);
            }

            return this;
        }

        /// <summary>添加数据流进行校验</summary>
        /// <param name="stream"></param>
        /// <param name="count">数量</param>
        public Crc32 Update(Stream stream, Int64 count = 0)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            //if (count < 0) throw new ArgumentOutOfRangeException("count", "Count不能小于0！");
            if (count <= 0) count = Int64.MaxValue;

            while (--count >= 0)
            {
                var b = stream.ReadByte();
                if (b == -1) break;

                crc = Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
            }

            return this;
        }

        /// <summary>计算校验码</summary>
        /// <param name="buf"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static UInt32 Compute(Byte[] buf, Int32 offset = 0, Int32 count = -1)
        {
            var crc = new Crc32();
            crc.Update(buf, offset, count);
            return crc.Value;
        }

        /// <summary>计算数据流校验码</summary>
        /// <param name="stream"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static UInt32 Compute(Stream stream, Int32 count = 0)
        {
            var crc = new Crc32();
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
        public static UInt32 Compute(Stream stream, Int64 position = -1, Int32 count = 0)
        {
            if (position >= 0)
            {
                if (count > 0) count = -count;
                count += (Int32)(stream.Position - position);
                stream.Position = position;
            }

            var crc = new Crc32();
            crc.Update(stream, count);
            return crc.Value;
        }

        //#region 抽象实现
        ///// <summary>哈希核心</summary>
        ///// <param name="array"></param>
        ///// <param name="ibStart"></param>
        ///// <param name="cbSize"></param>
        //protected override void HashCore(byte[] array, int ibStart, int cbSize)
        //{
        //    while (--cbSize >= 0)
        //    {
        //        crc = Table[(crc ^ array[ibStart++]) & 0xFF] ^ (crc >> 8);
        //    }
        //}

        ///// <summary>最后哈希</summary>
        ///// <returns></returns>
        //protected override byte[] HashFinal() { return BitConverter.GetBytes(Value); }

        ///// <summary>初始化</summary>
        //public override void Initialize() { }
        //#endregion
    }
}