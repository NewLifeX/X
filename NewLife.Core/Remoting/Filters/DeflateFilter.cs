using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Data;
using NewLife.Messaging;

namespace NewLife.Remoting
{
    /// <summary>压缩过滤器</summary>
    /// <remarks>响应包是否压缩，取决于请求包</remarks>
    public class DeflateFilter : FilterBase
    {
        /// <summary>标识。默认0x10</summary>
        public Byte Flag { get; set; } = 0x10;

        /// <summary>使用压缩的最小长度。默认64字节</summary>
        public Int32 MinSize { get; set; } = 64;

        /// <summary>执行压缩或解压缩</summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected override Boolean OnExecute(FilterContext context)
        {
            var ctx = context as ApiFilterContext;
            if (ctx == null) return true;

            var msg = ctx.Message as DefaultMessage;
            if (msg == null) return true;

            var pk = ctx.Packet;
            if (ctx.IsSend)
            {
                //// 响应消息是否加密由标识位决定
                //if (msg.Reply && (msg.Flag & Flag) == 0) return true;

                // 清空标记位
                msg.Flag = (Byte)(msg.Flag & ~Flag);

                // 太小的数据包不压缩
                if (pk.Total < MinSize) return true;

                // 压缩标记位
                msg.Flag |= Flag;

                var ms = pk.GetStream().Compress();
                pk.Set(ms.ToArray());
            }
            else
            {
                // 压缩标记位
                if ((msg.Flag & Flag) == 0) return true;

                var ms = pk.GetStream().Decompress();
                pk.Set(ms.ToArray());
            }

            return true;
        }
    }
}