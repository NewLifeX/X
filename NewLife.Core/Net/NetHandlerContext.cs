using System;
using System.Collections.Generic;
using NewLife.Data;
using NewLife.Messaging;
using NewLife.Model;
using NewLife.Threading;

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
            // 经历编码器管道，万水千山来到这里！
            // 对于消息协议来说，意味着协议解包已经完成，IOCP层可以着手去接收下一消息，而无需等待当前消息是否已处理完成

            if (!Session.ProcessAsync)
            {
                var ori = Data as ReceivedEventArgs;
                var e = new ReceivedEventArgs
                {
                    Remote = ori.Remote,
                    Message = message,
                    UserState = ori.UserState,
                };

                // 如果消息使用了原来SEAE的数据包，需要拷贝，避免多线程冲突
                // 也可能在粘包处理时，已经拷贝了一次
                if (ori.Packet != null && message is IMessage msg)
                {
                    if (ori.Packet.Data == msg.Payload.Data) msg.Payload = msg.Payload.Clone();
                }

                // 异步处理
                ThreadPoolX.QueueUserWorkItem(Session.Process, e);
            }
            else
            {
                var data = Data ?? new ReceivedEventArgs();
                data.Message = message;
                Session.Process(data);
            }
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