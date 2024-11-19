using System.Text;
using NewLife.Data;

namespace NewLife.Net;

/// <summary>网络服务的会话，每个Tcp/Udp连接作为一个会话</summary>
/// <remarks>
/// 每当收到一个Tcp连接时，创建一个INetSession会话，用于处理该连接的业务。
/// 使用Udp服务端时，收到远程节点的第一个数据包时，也会创建一个会话，处理该节点的业务。
/// 
/// 所有应用服务器以会话<see cref="INetSession"/>作为业务处理核心。
/// 应用服务器收到新会话请求后，通过<see cref="Start"/>启动一个会话处理。
/// 会话进行业务处理的过程中，可以通过多个Send方法向客户端发送数据。
/// </remarks>
public interface INetSession : IDisposable2
{
    #region 属性
    /// <summary>唯一会话标识。在主服务中唯一标识当前会话，原子自增</summary>
    Int32 ID { get; }

    /// <summary>主服务。负责管理当前会话的主服务器NetServer</summary>
    NetServer Host { get; set; }

    /// <summary>Socket服务器。当前通讯所在的Socket服务器，其实是TcpServer/UdpServer</summary>
    ISocketServer Server { get; set; }

    /// <summary>客户端。跟客户端通讯的那个Socket，其实是服务端TcpSession/UdpSession</summary>
    ISocketSession Session { get; set; }

    /// <summary>客户端地址</summary>
    NetUri Remote { get; }
    #endregion

    #region 方法
    /// <summary>开始会话处理。</summary>
    void Start();

    /// <summary>主动关闭跟客户端的网络连接</summary>
    /// <param name="reason">断开原因。包括 SendError/RemoveNotAlive/Dispose/GC 等，其中 ConnectionReset 为网络被动断开或对方断开</param>
    void Close(String reason);

    /// <summary>连接创建事件。创建会话之后</summary>
    event EventHandler<EventArgs> Connected;

    /// <summary>连接断开事件。包括客户端主动断开、服务端主动断开以及服务端超时下线</summary>
    event EventHandler<EventArgs> Disconnected;
    #endregion

    #region 收发
    /// <summary>发送数据，直达网卡</summary>
    /// <param name="data">数据包</param>
    INetSession Send(IPacket data);

    /// <summary>发送数据，直达网卡</summary>
    /// <param name="data">字节数组</param>
    /// <param name="offset">偏移</param>
    /// <param name="count">字节数</param>
    /// <returns></returns>
    INetSession Send(Byte[] data, Int32 offset = 0, Int32 count = -1);

    /// <summary>发送数据，直达网卡</summary>
    /// <param name="data">数据包</param>
    /// <returns></returns>
    INetSession Send(ReadOnlySpan<Byte> data);

    /// <summary>发送数据流，直达网卡</summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    INetSession Send(Stream stream);

    /// <summary>发送字符串，直达网卡</summary>
    /// <param name="msg"></param>
    /// <param name="encoding"></param>
    INetSession Send(String msg, Encoding? encoding = null);

    /// <summary>通过管道发送消息，不等待响应。管道内对消息进行报文封装处理，最终得到二进制数据进入网卡</summary>
    /// <param name="message"></param>
    /// <returns></returns>
    Int32 SendMessage(Object message);

    /// <summary>异步发送消息并等待响应。管道内对消息进行报文封装处理，最终得到二进制数据进入网卡</summary>
    /// <param name="message"></param>
    /// <returns></returns>
    Task<Object> SendMessageAsync(Object message);

    /// <summary>异步发送消息并等待响应。管道内对消息进行报文封装处理，最终得到二进制数据进入网卡</summary>
    /// <param name="message">消息</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    Task<Object> SendMessageAsync(Object message, CancellationToken cancellationToken);

    /// <summary>数据到达事件。包括原始数据包Packet以及管道处理器解码后的业务消息Message</summary>
    event EventHandler<ReceivedEventArgs> Received;
    #endregion
}

/// <summary>会话事件参数</summary>
public class NetSessionEventArgs : EventArgs
{
    /// <summary>会话</summary>
    public INetSession? Session { get; set; }
}