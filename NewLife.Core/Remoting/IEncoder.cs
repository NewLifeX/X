using System;

namespace NewLife.Remoting
{
    /// <summary>编码器</summary>
    public interface IEncoder
    {
        /// <summary>把对象转换为字节数组</summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        Byte[] Encode(Object obj);

        /// <summary>把字节数组转换为对象</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        T Decode<T>(Byte[] data);
    }
}