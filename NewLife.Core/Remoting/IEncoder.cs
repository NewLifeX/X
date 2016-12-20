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
        byte[] Encode(string action, object args);

        /// <summary>编码响应</summary>
        /// <param name="success"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        byte[] Encode(bool success, object result);

        /// <summary>解码响应</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        T Decode<T>(byte[] data);

        /// <summary>解码请求</summary>
        /// <param name="data"></param>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        bool Decode(byte[] data, out string action, out IDictionary<string, object> args);
    }
}