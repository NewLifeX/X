using System;
using NewLife.Data;
using NewLife.Messaging;

namespace NewLife.Remoting
{
    /// <summary>RC4加解密过滤器</summary>
    public class RC4Filter : FilterBase
    {
        /// <summary>密钥</summary>
        public Byte[] Key { get; set; }

        /// <summary>执行压缩或解压缩</summary>
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
                // 加密标记位
                msg.Flag |= 0x20;
            }
            else
            {
                // 加密标记位
                if ((msg.Flag & 0x20) == 0) return true;
            }

            var pk = ctx.Packet;
            Encrypt(pk);

            return true;
        }

        /// <summary>加解密</summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        protected virtual Boolean Encrypt(Packet pk)
        {
            if (Key == null || Key.Length == 0) return false;

            var buf = pk.ToArray().RC4(Key);
            pk.Set(buf);

            return true;
        }
    }
}