using System;
using System.Collections.Generic;
using NewLife.Data;
using NewLife.Model;

namespace NewLife.Net
{
    /// <summary>网络处理器上下文</summary>
    public class NetHandlerContext : HandlerContext
    {
        #region 属性
        /// <summary>远程连接</summary>
        public ISocketRemote Session { get; set; }

        /// <summary>数据帧</summary>
        public IData Data { get; set; }

        /// <summary>读取管道过滤后最终处理消息</summary>
        /// <param name="message"></param>
        public override void FireRead(Object message)
        {
            var data = Data ?? new ReceivedEventArgs();
            data.Message = message;
            Session.Process(data);
        }

        /// <summary>写入管道过滤后最终处理消息</summary>
        /// <param name="message"></param>
        public override Boolean FireWrite(Object message)
        {
            if (message == null) return false;

            var session = Session;

            // 发送一包数据
            if (message is Byte[] buf) return session.Send(buf);
            if (message is Packet pk) return session.Send(pk);
            if (message is String str) return session.Send(str.GetBytes());

            // 发送一批数据包
            if (message is IEnumerable<Packet> pks)
            {
                foreach (var item in pks)
                {
                    if (!session.Send(item)) return false;
                }

                return true;
            }

            throw new XException("无法识别消息[{0}]，可能缺少编码处理器", message?.GetType()?.FullName);
        }
        #endregion
    }
}