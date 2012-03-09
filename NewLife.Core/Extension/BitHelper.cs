using System;

namespace System
{
    /// <summary>数据位助手</summary>
    public static class BitHelper
    {
        /// <summary>设置数据位</summary>
        /// <param name="value"></param>
        /// <param name="position"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        public static UInt16 SetBit(this UInt16 value, Int32 position, Boolean flag)
        {
            return SetBits(value, position, 1, (flag ? (Byte)1 : (Byte)0));
        }

        /// <summary>设置数据位</summary>
        /// <param name="value"></param>
        /// <param name="position"></param>
        /// <param name="length"></param>
        /// <param name="bits"></param>
        /// <returns></returns>
        public static UInt16 SetBits(this UInt16 value, Int32 position, Int32 length, UInt16 bits)
        {
            if (length <= 0 || position >= 16) return value;

            Int32 mask = (2 << (length - 1)) - 1;

            value &= (UInt16)~(mask << position);
            value |= (UInt16)((bits & mask) << position);

            return value;
        }

        /// <summary>设置数据位</summary>
        /// <param name="value"></param>
        /// <param name="position"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        public static Byte SetBit(this Byte value, Int32 position, Boolean flag)
        {
            if (position >= 8) return value;

            Int32 mask = (2 << (1 - 1)) - 1;

            value &= (Byte)~(mask << position);
            value |= (Byte)(((flag ? (Byte)1 : (Byte)0) & mask) << position);

            return value;
        }

        /// <summary>获取数据位</summary>
        /// <param name="value"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public static Boolean GetBit(this UInt16 value, Int32 position)
        {
            return GetBits(value, position, 1) == 1;
        }

        /// <summary>获取数据位</summary>
        /// <param name="value"></param>
        /// <param name="position"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static UInt16 GetBits(this UInt16 value, Int32 position, Int32 length)
        {
            if (length <= 0 || position >= 16) return 0;

            Int32 mask = (2 << (length - 1)) - 1;

            return (UInt16)((value >> position) & mask);
        }

        /// <summary>获取数据位</summary>
        /// <param name="value"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public static Boolean GetBit(this Byte value, Int32 position)
        {
            if (position >= 8) return false;

            Int32 mask = (2 << (1 - 1)) - 1;

            return ((Byte)((value >> position) & mask)) == 1;
        }
    }
}