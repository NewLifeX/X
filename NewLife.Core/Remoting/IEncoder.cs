using System;
using System.Collections;
using System.Collections.Generic;

namespace NewLife.Remoting
{
    /// <summary>编码器</summary>
    public interface IEncoder
    {
        /// <summary>编码请求</summary>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        Byte[] Encode(String action, Object args);

        /// <summary>编码响应</summary>
        /// <param name="success"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        Byte[] Encode(Boolean success, Object result);

        /// <summary>解码响应</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        T Decode<T>(Byte[] data);

        /// <summary>解码请求</summary>
        /// <param name="data"></param>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        Boolean Decode(Byte[] data, out String action, out IDictionary<String, Object> args);
    }
}