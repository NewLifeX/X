using System;
using NewLife.Data;
using NewLife.Log;
using NewLife.Net.Handlers;

namespace NewLife.Net.Application
{
    /// <summary>回声处理器</summary>
    public class EchoHandler : Handler
    {
        /// <summary>读取</summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public override Object Read(IHandlerContext context, Object message)
        {
            if (message is Packet pk)
            {
                var len = pk.Total;
                if (len > 100)
                    XTrace.WriteLine("Echo {0} [{1}]", context.Session, len);
                else
                    XTrace.WriteLine("Echo {0} [{1}] {2}", context.Session, len, pk.ToStr());
            }
            else
                XTrace.WriteLine("{0}", message);

            context.Session.SendMessage(message);

            return null;
        }
    }
}