using System;
using System.Collections.Generic;
using NewLife.Data;
using NewLife.Model;
using NewLife.Net.Handlers;

namespace NewLife.Serialization
{
    /// <summary>Json编码解码器</summary>
    public class JsonCodec2 : Handler
    {
        /// <summary>对象转Json</summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public override Object Write(IHandlerContext context, Object message)
        {
            if (message is IDictionary<String, Object> entity) return new Packet(entity.ToJson().GetBytes());

            return message;
        }

        /// <summary>Json转对象</summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public override Object Read(IHandlerContext context, Object message)
        {
            if (message is Packet pk) message = pk.ToStr();
            if (message is String str) return new JsonParser(str).Decode();

            return message;
        }
    }
}