using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Data;
using NewLife.Messaging;

namespace NewLife.Remoting
{
    /// <summary>压缩过滤器</summary>
    public class DeflateFilter : FilterBase
    {
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
                // 太小的数据包不压缩
                if (pk.Count < MinSize) return true;

                // 压缩标记位
                msg.Flag |= 0x10;

                var ms = pk.GetStream().Compress();
                pk.Set(ms.ToArray());
            }
            else
            {
                // 压缩标记位
                if ((msg.Flag & 0x10) == 0) return true;

                var ms = pk.GetStream().Decompress();
                pk.Set(ms.ToArray());
            }

            return true;
        }
    }
}