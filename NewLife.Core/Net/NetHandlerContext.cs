using System.Net;
using System.Net.Sockets;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Messaging;
using NewLife.Model;
using NewLife.Serialization;

namespace NewLife.Net;

/// <summary>网络处理器上下文</summary>
/// <remarks>上下文按轮租借，承载处理器调用链的临时数据</remarks>
public class NetHandlerContext : HandlerContext
{
    #region 池
    private static readonly Pool<NetHandlerContext> _pool = new();

    /// <summary>从池中借出上下文</summary>
    public static NetHandlerContext Rent()
    {
        var ctx = _pool.Get();
        return ctx;
    }

    /// <summary>归还上下文到池</summary>
    public static void Return(NetHandlerContext ctx)
    {
        if (ctx == null) return;
        ctx.Reset();
        _pool.Return(ctx);
    }
    #endregion

    #region 属性
    /// <summary>远程连接</summary>
    public ISocketRemote? Session { get; set; }

    /// <summary>数据帧</summary>
    public IData? Data { get; set; }

    /// <summary>远程地址。因为ProxyProtocol协议存在，haproxy/nginx等代理会返回原始客户端IP端口</summary>
    public IPEndPoint? Remote { get; set; }

    /// <summary>Socket事件参数</summary>
    public SocketAsyncEventArgs? EventArgs { get; set; }

    /// <summary>重置上下文，清理状态以便对象池复用</summary>
    public void Reset()
    {
        Session = null;
        Data = null;
        Remote = null;
        EventArgs = null;
        Owner = null;
        Pipeline = null;
        Items.Clear();
    }
    #endregion

    #region 读取/写入
    /// <summary>读取管道过滤后最终处理消息</summary>
    /// <param name="message"></param>
    public override void FireRead(Object message)
    {
        // 经历编码器管道，万水千山来到这里！

        var data = Data ?? new ReceivedEventArgs();
        data.Message = message;

        // 把上下文带到上层
        if (data is ReceivedEventArgs e && e.Context == null)
            e.Context = this;

        // 因为ProxyProtocol协议存在，haproxy/nginx等代理会返回原始客户端IP端口
        // 这里修改Remote以后，NetSession层将会使用新的Remote地址
        if (Remote != null) data.Remote = Remote;

        var old = data.Packet;
        // 解析协议指令后，事件变量里面的数据是之前的原始报文，有可能多帧指令粘包在一起，需要拆分填充当前指令的数据报文，避免上层重复使用原始大报文。
        // Packet 只承载“当前展示数据”：事件期间临时指向当前帧，事件返回后恢复整轮视图；
        // 需要带出本轮的数据在事件内经 Slice 切出共享句柄（引用计数持有），与 Packet 恢复互不影响
        if (message is DefaultMessage dm)
        {
            var raw = dm.GetRaw();
            if (raw != null) data.Packet = raw;
        }

        try
        {
            Session?.Process(data);
        }
        finally
        {
            // 事件期间 Packet 临时指向当前帧，事件返回后恢复原值（整轮数据）；
            // 订阅者若要把数据带出本轮，经 Slice 获得共享句柄（引用计数），与 Packet 恢复互不影响
            data.Packet = old;
        }
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
        if (message is IPacket ip) return session.Send(ip);
        if (message is String str) return session.Send(str.GetBytes());
        if (message is ISpanSerializable spanSerial)
        {
            using var pk = spanSerial.ToPacket();
            return session.Send(pk);
        }
        if (message is IAccessor acc) return session.Send(acc.ToPacket());

        // 发送一批数据包
        if (message is IEnumerable<IPacket> pks)
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

        throw new XException("Unable to recognize message [{0}], possibly missing encoding processor", message?.GetType()?.FullName);
    }
    #endregion
}