using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace NewLife.Security
{
    /// <summary>高性能低碰撞Murmur128哈希算法</summary>
    /// <remarks>
    /// Redis等大量使用，比MD5要好
    /// </remarks>
    public class Murmur128 : HashAlgorithm
    {
        #region 属性
        const UInt64 C1 = 0x87c37b91114253d5;
        const UInt64 C2 = 0x4cf5ad432745937f;

        private readonly UInt32 _Seed;
        /// <summary>种子</summary>
        public UInt32 Seed => _Seed;

        /// <summary>哈希大小</summary>
        public override Int32 HashSize => 128;

        private Int32 _Length;
        private UInt64 _H1;
        private UInt64 _H2;
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        /// <param name="seed"></param>
        public Murmur128(UInt32 seed = 0)
        {
            _Seed = seed;
            Reset();
        }
        #endregion

        #region 方法
        private void Reset()
        {
            // 初始化哈希值到种子
            _H1 = _H2 = Seed;

            // 重置长度为0
            _Length = 0;
        }

        /// <summary>初始化</summary>
        public override void Initialize() => Reset();

        /// <summary>哈希核心</summary>
        /// <param name="array"></param>
        /// <param name="ibStart"></param>
        /// <param name="cbSize"></param>
        protected override void HashCore(Byte[] array, Int32 ibStart, Int32 cbSize)
        {
            // 增加长度
            _Length += cbSize;
            Body(array, ibStart, cbSize);
        }

#if !NET4
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private void Body(Byte[] data, Int32 start, Int32 length)
        {
            var remainder = length & 15;
            var alignedLength = start + (length - remainder);
            for (var i = start; i < alignedLength; i += 16)
            {
                _H1 ^= RotateLeft(data.ToUInt64(i) * C1, 31) * C2;
                _H1 = (RotateLeft(_H1, 27) + _H2) * 5 + 0x52dce729;

                _H2 ^= RotateLeft(data.ToUInt64(i + 8) * C2, 33) * C1;
                _H2 = (RotateLeft(_H2, 31) + _H1) * 5 + 0x38495ab5;
            }

            if (remainder > 0)
                Tail(data, alignedLength, remainder);
        }

#if !NET4
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private void Tail(Byte[] tail, Int32 start, Int32 remaining)
        {
            // create our keys and initialize to 0
            UInt64 k1 = 0, k2 = 0;

            // determine how many bytes we have left to work with based on length
            switch (remaining)
            {
                case 15: k2 ^= (UInt64)tail[start + 14] << 48; goto case 14;
                case 14: k2 ^= (UInt64)tail[start + 13] << 40; goto case 13;
                case 13: k2 ^= (UInt64)tail[start + 12] << 32; goto case 12;
                case 12: k2 ^= (UInt64)tail[start + 11] << 24; goto case 11;
                case 11: k2 ^= (UInt64)tail[start + 10] << 16; goto case 10;
                case 10: k2 ^= (UInt64)tail[start + 9] << 8; goto case 9;
                case 9: k2 ^= (UInt64)tail[start + 8] << 0; goto case 8;
                case 8: k1 ^= (UInt64)tail[start + 7] << 56; goto case 7;
                case 7: k1 ^= (UInt64)tail[start + 6] << 48; goto case 6;
                case 6: k1 ^= (UInt64)tail[start + 5] << 40; goto case 5;
                case 5: k1 ^= (UInt64)tail[start + 4] << 32; goto case 4;
                case 4: k1 ^= (UInt64)tail[start + 3] << 24; goto case 3;
                case 3: k1 ^= (UInt64)tail[start + 2] << 16; goto case 2;
                case 2: k1 ^= (UInt64)tail[start + 1] << 8; goto case 1;
                case 1: k1 ^= (UInt64)tail[start] << 0; break;
            }

            _H2 ^= RotateLeft(k2 * C2, 33) * C1;
            _H1 ^= RotateLeft(k1 * C1, 31) * C2;
        }

        /// <summary>哈希结束</summary>
        /// <returns></returns>
        protected override Byte[] HashFinal()
        {
            var len = (UInt64)_Length;
            _H1 ^= len; _H2 ^= len;

            _H1 += _H2;
            _H2 += _H1;

            _H1 = FMix(_H1);
            _H2 = FMix(_H2);

            _H1 += _H2;
            _H2 += _H1;

            var result = new Byte[16];
            Array.Copy(BitConverter.GetBytes(_H1), 0, result, 0, 8);
            Array.Copy(BitConverter.GetBytes(_H2), 0, result, 8, 8);

            return result;
        }
        #endregion

        #region 辅助
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //private static UInt32 RotateLeft(UInt32 x, Byte r) => (x << r) | (x >> (32 - r));

#if !NET4
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private static UInt64 RotateLeft(UInt64 x, Byte r) => (x << r) | (x >> (64 - r));

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //private static UInt32 FMix(UInt32 h)
        //{
        //    h = (h ^ (h >> 16)) * 0x85ebca6b;
        //    h = (h ^ (h >> 13)) * 0xc2b2ae35;
        //    return h ^ (h >> 16);
        //}

#if !NET4
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private static UInt64 FMix(UInt64 h)
        {
            h = (h ^ (h >> 33)) * 0xff51afd7ed558ccd;
            h = (h ^ (h >> 33)) * 0xc4ceb9fe1a85ec53;

            return (h ^ (h >> 33));
        }
        #endregion
    }
}