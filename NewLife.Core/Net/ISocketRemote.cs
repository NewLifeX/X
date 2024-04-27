using System.Text;
using NewLife.Data;
using NewLife.Model;

namespace NewLife.Net;

/// <summary>远程通信Socket，仅具有收发功能</summary>
public interface ISocketRemote : ISocket, IExtend
{
    #region 属性
    /// <summary>标识</summary>
    Int32 ID { get; }

    /// <summary>远程地址</summary>
    NetUri Remote { get; set; }

    /// <summary>最后一次通信时间，主要表示会话活跃时间，包括收发</summary>
    DateTime LastTime { get; }
    #endregion

    #region 发送
    /// <summary>发送原始数据包</summary>
    /// <remarks>
    /// 目标地址由<seealso cref="Remote"/>决定
    /// </remarks>
    /// <param name="data">数据包</param>
    /// <returns>是否成功</returns>
    Int32 Send(Packet data);
    #endregion

    #region 接收
    /// <summary>接收数据。阻塞当前线程等待返回</summary>
    /// <returns></returns>
    Packet? Receive();

    /// <summary>异步接收数据</summary>
    /// <returns></returns>
    Task<Packet?> ReceiveAsync(CancellationToken cancellationToken = default);

    /// <summary>数据到达事件</summary>
    event EventHandler<ReceivedEventArgs> Received;
    #endregion

    #region 消息包
    /// <summary>异步发送消息并等待响应</summary>
    /// <param name="message">消息</param>
    /// <returns></returns>
    Task<Object> SendMessageAsync(Object message);

    /// <summary>异步发送消息并等待响应</summary>
    /// <param name="message">消息</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    Task<Object> SendMessageAsync(Object message, CancellationToken cancellationToken);

    /// <summary>发送消息，不等待响应</summary>
    /// <param name="message">消息</param>
    /// <returns></returns>
    Int32 SendMessage(Object message);

    /// <summary>处理消息数据帧</summary>
    /// <param name="data">数据帧</param>
    void Process(IData data);
    #endregion
}

/// <summary>远程通信Socket扩展</summary>
public static class SocketRemoteHelper
{
    #region 发送
    /// <summary>发送数据流</summary>
    /// <param name="session">会话</param>
    /// <param name="stream">数据流</param>
    /// <returns>返回是否成功</returns>
    public static Int32 Send(this ISocketRemote session, Stream stream)
    {
        //// 空数据直接发出
        //var remain = stream.Length - stream.Position;
        //if (remain == 0) return session.Send(new Byte[0]);

        var rs = 0;
        var buffer = new Byte[8192];
        while (true)
        {
            var count = stream.Read(buffer, 0, buffer.Length);
            if (count <= 0) break;

            var pk = new Packet(buffer, 0, count);
            var count2 = session.Send(pk);
            if (count2 < 0) break;
            rs += count2;

            if (count < buffer.Length) break;
        }
        return rs;
    }

    /// <summary>发送字符串</summary>
    /// <param name="session">会话</param>
    /// <param name="msg">要发送的字符串</param>
    /// <param name="encoding">文本编码，默认null表示UTF-8编码</param>
    /// <returns>返回自身，用于链式写法</returns>
    public static Int32 Send(this ISocketRemote session, String msg, Encoding? encoding = null)
    {
        if (String.IsNullOrEmpty(msg)) return session.Send(new Byte[0]);

        encoding ??= Encoding.UTF8;
        return session.Send(encoding.GetBytes(msg));
    }
    #endregion

    #region 接收
    /// <summary>接收字符串</summary>
    /// <param name="session">会话</param>
    /// <param name="encoding">文本编码，默认null表示UTF-8编码</param>
    /// <returns></returns>
    public static String ReceiveString(this ISocketRemote session, Encoding? encoding = null)
    {
        var pk = session.Receive();
        if (pk == null || pk.Count == 0) return String.Empty;

        return pk.ToStr(encoding ?? Encoding.UTF8);
    }
    #endregion

    #region 消息包
    /// <summary>添加处理器</summary>
    /// <typeparam name="THandler"></typeparam>
    /// <param name="session">会话</param>
    public static void Add<THandler>(this ISocket session) where THandler : IHandler, new() => GetPipe(session).Add(new THandler());

    /// <summary>添加处理器</summary>
    /// <param name="session">会话</param>
    /// <param name="handler">处理器</param>
    public static void Add(this ISocket session, IHandler handler) => GetPipe(session).Add(handler);

    private static IPipeline GetPipe(ISocket session) => session.Pipeline ??= new Pipeline();

    /// <summary>切分数据流为多个数据包消息进行发送，接收方按顺序组装</summary>
    /// <param name="session">会话</param>
    /// <param name="stream">数据流</param>
    /// <returns>拆分消息数</returns>
    public static Int32 SendMessages(this ISocketRemote session, Stream stream)
    {
        var count = 0;

        // 缓冲区大小，要减去4字节标准消息头。BufferSize默认8k，能得到最大吞吐。如果网络质量较差，这里可使用TCP最大包1448
        var bufferSize = SocketSetting.Current.BufferSize;
        bufferSize -= 4;

        var buffer = new Byte[bufferSize];
        while (true)
        {
            var rs = stream.Read(buffer, 0, buffer.Length);
            if (rs <= 0) break;

            // 打包数据，标准编码器StandardCodec将会在头部加上4字节头部，交给下层Tcp发出
            var pk = new Packet(buffer, 0, rs);
            session.SendMessage(pk);

            count++;
        }

        return count;
    }

    /// <summary>切分文件流为多个数据包发出，接收方按顺序组装</summary>
    /// <param name="session"></param>
    /// <param name="file"></param>
    /// <param name="compressed"></param>
    /// <returns></returns>
    public static Int32 SendFile(this ISocketRemote session, String file, Boolean compressed = false)
    {
        var rs = 0;
        file.AsFile().OpenRead(compressed, s =>
        {
            rs = session.SendMessages(s);
        });

        return rs;
    }
    #endregion
}
