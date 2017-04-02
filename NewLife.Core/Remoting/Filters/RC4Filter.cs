using System;
using NewLife.Data;
using NewLife.Messaging;

namespace NewLife.Remoting
{
    /// <summary>RC4加解密过滤器</summary>
    /// <remarks>响应包是否加密，取决于请求包</remarks>
    public class RC4Filter : FilterBase
    {
        /// <summary>标识。默认0x20</summary>
        public Byte Flag { get; set; } = 0x20;

        /// <summary>密钥</summary>
        public Byte[] Key { get; set; }

        /// <summary>动态获取密钥的委托</summary>
        public Func<FilterContext, Byte[]> GetKey { get; set; }

        /// <summary>执行加解密</summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected override Boolean OnExecute(FilterContext context)
        {
            var ctx = context as ApiFilterContext;
            if (ctx == null) return true;

            var msg = ctx.Message as DefaultMessage;
            if (msg == null) return true;

            if (ctx.IsSend)
            {
                // 响应消息是否加密由标识位决定
                if (msg.Reply && (msg.Flag & Flag) == 0) return true;

                // 清空标记位
                msg.Flag = (Byte)(msg.Flag & ~Flag);
            }
            else
            {
                // 加密标记位
                if ((msg.Flag & Flag) == 0) return true;
            }

            var key = Key;

            // 根据上下文从外部获取密钥
            var func = GetKey;
            if (func != null) key = func(context);
            if (key == null || key.Length == 0) return true;

            var pk = ctx.Packet;
            if (Encrypt(pk, key))
            {
                // 加密成功后再设置加密标记位
                if (ctx.IsSend) msg.Flag |= Flag;
            }

            return true;
        }

        /// <summary>加解密</summary>
        /// <param name="pk"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        protected virtual Boolean Encrypt(Packet pk, Byte[] key)
        {
            //if (Key == null || Key.Length == 0) return false;

            var buf = pk.ToArray().RC4(key);
            pk.Set(buf);

            return true;
        }
    }
}