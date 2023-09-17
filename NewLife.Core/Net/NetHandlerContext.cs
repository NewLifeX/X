using NewLife.Data;
using NewLife.Messaging;
using NewLife.Model;
using NewLife.Serialization;

namespace NewLife.Net;

/// <summary>网络处理器上下文</summary>
public class NetHandlerContext : HandlerContext
{
    #region 属性
    /// <summary>远程连接</summary>
    public ISocketRemote? Session { get; set; }

    /// <summary>数据帧</summary>
    public IData? Data { get; set; }

    /// <summary>读取管道过滤后最终处理消息</summary>
    /// <param name="message"></param>
    public override void FireRead(Object message)
    {
        // 经历编码器管道，万水千山来到这里！
        // 对于消息协议来说，意味着协议解包已经完成，IOCP层可以着手去接收下一消息，而无需等待当前消息是否已处理完成

        //if (!Session.ProcessAsync)
        //{
        //    var ori = Data as ReceivedEventArgs;
        //    // 如果消息使用了原来SEAE的数据包，需要拷贝，避免多线程冲突
        //    // 也可能在粘包处理时，已经拷贝了一次
        //    var flag = false;
        //    if (ori.Packet != null)
        //    {
        //        if (message is IMessage msg)
        //        {
        //            if (msg.Payload != null && ori.Packet.Data == msg.Payload.Data)
        //            {
        //                msg.Payload = msg.Payload.Clone();
        //                flag = true;
        //            }
        //        }
        //        else if (message is Packet pk)
        //        {
        //            if (pk != null && ori.Packet.Data == pk.Data)
        //            {
        //                message = pk.Clone();
        //                flag = true;
        //            }
        //        }
        //    }

        //    // 只有完成了数据包拷贝的消息，才走异步处理，避免用户消息中引用了IOCP层数据包
        //    if (flag)
        //    {
        //        var e = new ReceivedEventArgs
        //        {
        //            Remote = ori.Remote,
        //            Message = message,
        //            UserState = ori.UserState,
        //        };

        //        ThreadPoolX.QueueUserWorkItem(Session.Process, e);
        //        return;
        //    }
        //}

        //{
        var data = Data ?? new ReceivedEventArgs();
        data.Message = message;

        // 解析协议指令后，事件变量里面的数据是之前的原始报文，有可能多帧指令粘包在一起，需要拆分填充当前指令的数据报文，避免上层重复使用原始大报文
        if (message is DefaultMessage dm)
        {
            var raw = dm.GetRaw();
            if (raw != null) data.Packet = raw;
        }

        Session?.Process(data);
        //}
    }

    /// <summary>写入管道过滤后最终处理消息</summary>
    /// <param name="message"></param>
    public override Int32 FireWrite(Object message)
    {
        if (message == null) return -1;

        var session = Session;
        if (session == null) return -2;

        // 发送一包数据
        if (message is Byte[] buf) return session.Send(buf);
        if (message is Packet pk) return session.Send(pk);
        if (message is String str) return session.Send(str.GetBytes());
        if (message is IAccessor acc) return session.Send(acc.ToPacket());

        // 发送一批数据包
        if (message is IEnumerable<Packet> pks)
        {
            var rs = 0;
            foreach (var item in pks)
            {
                var count = session.Send(item);
                if (count < 0) break;

                rs += count;
            }

            return rs;
        }

        throw new XException("无法识别消息[{0}]，可能缺少编码处理器", message?.GetType()?.FullName);
    }
    #endregion
}