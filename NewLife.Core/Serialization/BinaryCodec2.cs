using System;
using System.Collections.Generic;
using NewLife.Data;
using NewLife.Model;
using NewLife.Net.Handlers;

namespace NewLife.Serialization
{
    /// <summary>二进制编码解码器</summary>
    public class BinaryCodec2 : Handler
    {
        /// <summary>使用7位编码整数。默认true使用</summary>
        public Boolean EncodedInt { get; set; } = true;

        /// <summary>对象转二进制</summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public override Object Write(IHandlerContext context, Object message)
        {
            if (message is IDictionary<String, Object> entity) return Binary.FastWrite(entity, EncodedInt);

            return message;
        }

        /// <summary>二进制转对象</summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public override Object Read(IHandlerContext context, Object message)
        {
            if (message is Packet pk)
            {
                //return Binary.FastRead<T>(pk.GetStream(), EncodedInt);
                // 读取二进制名值对
                var dic = new Dictionary<String, Object>();
                return dic;
            }

            return message;
        }
    }
}
