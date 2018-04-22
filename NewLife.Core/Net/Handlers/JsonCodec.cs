using System;
using NewLife.Data;
using NewLife.Serialization;

namespace NewLife.Net.Handlers
{
    /// <summary>Json编码解码器</summary>
    public class JsonCodec<T> : Handler
    {
        /// <summary>使用7位编码整数。默认true使用</summary>
        public Boolean EncodedInt { get; set; } = true;

        /// <summary>对象转Json</summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public override Object Write(IHandlerContext context, Object message)
        {
            if (message is T entity) return new Packet(entity.ToJson().GetBytes());

            return message;
        }

        /// <summary>Json转对象</summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public override Object Read(IHandlerContext context, Object message)
        {
            if (message is Packet pk) message = pk.ToStr();
            if (message is String str) return str.ToJsonEntity<T>();

            return message;
        }
    }
}
