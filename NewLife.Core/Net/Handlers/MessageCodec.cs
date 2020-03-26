using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Messaging;
using NewLife.Model;

namespace NewLife.Net.Handlers
{
    /// <summary>消息封包</summary>
    public class MessageCodec<T> : Handler
    {
        /// <summary>消息队列。用于匹配请求响应包</summary>
        public IMatchQueue Queue { get; set; } = new DefaultMatchQueue();

        /// <summary>调用超时时间。默认30_000ms</summary>
        public Int32 Timeout { get; set; } = 30_000;

        /// <summary>使用数据包，写入时数据包转消息，读取时消息自动解包返回数据负载。默认true</summary>
        public Boolean UserPacket { get; set; } = true;

        /// <summary>写入数据</summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public override Object Write(IHandlerContext context, Object message)
        {
            if (message is T msg)
            {
                message = Encode(context, msg);
                if (message == null) return null;

                // 加入队列
                AddToQueue(context, msg);
            }

            return base.Write(context, message);
        }

        /// <summary>编码</summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        protected virtual Object Encode(IHandlerContext context, T msg)
        {
            if (msg is IMessage msg2) return msg2.ToPacket();

            return null;
        }

        /// <summary>加入队列</summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        protected virtual void AddToQueue(IHandlerContext context, T msg)
        {
            if (msg != null && context["TaskSource"] is TaskCompletionSource<Object> source)
            {
                Queue.Add(context.Owner, msg, Timeout, source);
            }
        }

        /// <summary>连接关闭时，清空粘包编码器</summary>
        /// <param name="context"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public override Boolean Close(IHandlerContext context, String reason)
        {
            Queue.Clear();

            return base.Close(context, reason);
        }

        /// <summary>读取数据</summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public override Object Read(IHandlerContext context, Object message)
        {
            if (!(message is Packet pk)) return base.Read(context, message);

            // 解码得到多个消息
            var list = Decode(context, pk);
            if (list == null) return null;

            foreach (var msg in list)
            {
                if (UserPacket && msg is IMessage msg2)
                    message = msg2.Payload;
                else
                    message = msg;

                // 后续处理器，得到最终结果，匹配请求队列
                var rs = base.Read(context, message);

                if (msg is IMessage msg3)
                {
                    // 匹配
                    if (msg3.Reply)
                    {
                        //!!! 处理结果的Packet需要拷贝一份，否则交给另一个线程使用会有冲突
                        if (rs is IMessage msg4 && msg4.Payload != null && msg4.Payload == msg3.Payload) msg4.Payload = msg4.Payload.Clone();

                        Queue.Match(context.Owner, msg, rs, IsMatch);
                    }
                }
                else if (rs != null)
                {
                    // 其它消息不考虑响应
                    Queue.Match(context.Owner, msg, rs, IsMatch);
                }

                // 匹配输入回调，让上层事件收到分包信息。
                // 这里很可能处于网络IO线程，阻塞了下一个Tcp包的接收
                context.FireRead(rs);
            }

            return null;
        }

        /// <summary>解码</summary>
        /// <param name="context"></param>
        /// <param name="pk"></param>
        /// <returns></returns>
        protected virtual IList<T> Decode(IHandlerContext context, Packet pk) => null;

        /// <summary>是否匹配响应</summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        protected virtual Boolean IsMatch(Object request, Object response) => true;

        #region 粘包处理
        /// <summary>从数据流中获取整帧数据长度</summary>
        /// <param name="pk">数据包</param>
        /// <param name="offset">长度的偏移量</param>
        /// <param name="size">长度大小。0变长，1/2/4小端字节，-2/-4大端字节</param>
        /// <returns>数据帧长度（包含头部长度位）</returns>
        protected static Int32 GetLength(Packet pk, Int32 offset, Int32 size)
        {
            if (offset < 0) return pk.Total - pk.Offset;

            var p = pk.Offset;
            // 数据不够，连长度都读取不了
            if (offset >= pk.Total) return 0;

            // 读取大小
            var len = 0;
            switch (size)
            {
                case 0:
                    var ms = pk.GetStream();
                    if (offset > 0) ms.Seek(offset, SeekOrigin.Current);
                    len = ms.ReadEncodedInt();
                    len += (Int32)(ms.Position - offset);
                    break;
                case 1:
                    len = pk[offset];
                    break;
                case 2:
                    len = pk.ReadBytes(offset, 2).ToUInt16();
                    break;
                case 4:
                    len = (Int32)pk.ReadBytes(offset, 4).ToUInt32();
                    break;
                case -2:
                    len = pk.ReadBytes(offset, 2).ToUInt16(0, false);
                    break;
                case -4:
                    len = (Int32)pk.ReadBytes(offset, 4).ToUInt32(0, false);
                    break;
                default:
                    throw new NotSupportedException();
            }

            // 判断后续数据是否足够
            if (len > pk.Total) return 0;

            // 数据长度加上头部长度
            len += Math.Abs(size);

            return len;
        }
        #endregion
    }
}