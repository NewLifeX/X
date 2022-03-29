using System;

namespace NewLife.Data
{
    /// <summary>内存字节流编码器</summary>
    public interface IMemoryEncoder
    {
        /// <summary>数值转内存字节流</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        Memory<Byte> Encode(Object value);

        /// <summary>内存字节流转对象</summary>
        /// <param name="data"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        Object Decode(Memory<Byte> data, Type type);
    }
}