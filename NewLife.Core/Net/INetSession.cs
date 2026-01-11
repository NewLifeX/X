using System.Text;
using NewLife.Data;

namespace NewLife.Net;

/// <summary>网络服务的会话接口，每个Tcp/Udp连接作为一个会话</summary>
/// <remarks>
/// <para>网络会话是应用层的核心抽象，封装了单个客户端连接的完整生命周期管理和数据通信能力。</para>
/// <para>设计理念：</para>
/// <list type="bullet">
/// <item>统一TCP/UDP会话模型，提供一致的编程接口</item>
/// <item>支持事件驱动和管道处理双模式</item>
/// <item>会话与服务器解耦，支持自定义会话类型</item>
/// </list>
/// <para>生命周期：</para>
/// <list type="number">
/// <item>TCP连接建立或UDP首包到达时，由 <see cref="NetServer"/> 创建会话实例</item>
/// <item>调用 <see cref="Start"/> 启动会话处理，触发 <see cref="Connected"/> 事件</item>
/// <item>通过 Send 系列方法向客户端发送数据，通过 <see cref="Received"/> 事件接收数据</item>
/// <item>连接断开时触发 <see cref="Disconnected"/> 事件，会话被释放</item>
/// </list>
/// </remarks>
public interface INetSession : IDisposable2
{
    #region 属性
    /// <summary>唯一会话标识</summary>
    /// <remarks>在主服务中唯一标识当前会话，由服务器原子自增分配，用于会话管理和日志追踪</remarks>
    Int32 ID { get; }

    /// <summary>主服务</summary>
    /// <remarks>负责管理当前会话的主服务器NetServer，提供服务级配置和资源访问</remarks>
    NetServer Host { get; set; }

    /// <summary>Socket服务器</summary>
    /// <remarks>当前通讯所在的Socket服务器，实际类型为TcpServer/UdpServer，提供底层网络能力</remarks>
    ISocketServer Server { get; set; }

    /// <summary>客户端Socket会话</summary>
    /// <remarks>跟客户端通讯的Socket会话，实际类型为服务端TcpSession/UdpSession，负责底层数据收发</remarks>
    ISocketSession Session { get; set; }

    /// <summary>客户端远程地址</summary>
    /// <remarks>客户端的网络地址信息，包含协议类型、IP地址和端口号</remarks>
    NetUri Remote { get; }
    #endregion

    #region 方法
    /// <summary>开始会话处理</summary>
    /// <remarks>
    /// <para>启动会话的数据接收和事件处理流程。</para>
    /// <para>该方法由服务器在创建会话后自动调用，通常无需手动调用。</para>
    /// <para>调用后将触发 <see cref="Connected"/> 事件，并开始监听客户端数据。</para>
    /// </remarks>
    void Start();

    /// <summary>主动关闭跟客户端的网络连接</summary>
    /// <remarks>
    /// <para>主动断开与客户端的连接，触发 <see cref="Disconnected"/> 事件。</para>
    /// <para>常见断开原因：</para>
    /// <list type="bullet">
    /// <item>SendError - 发送数据失败</item>
    /// <item>RemoveNotAlive - 会话超时清理</item>
    /// <item>Dispose/GC - 对象释放</item>
    /// <item>ConnectionReset - 网络被动断开或对方主动断开</item>
    /// </list>
    /// </remarks>
    /// <param name="reason">断开原因，便于日志分析和问题排查</param>
    void Close(String reason);
    #endregion

    #region 事件
    /// <summary>连接创建事件</summary>
    /// <remarks>在会话创建并调用 <see cref="Start"/> 后触发，可用于初始化会话状态或发送欢迎消息</remarks>
    event EventHandler<EventArgs> Connected;

    /// <summary>连接断开事件</summary>
    /// <remarks>
    /// <para>包括客户端主动断开、服务端主动断开以及服务端超时下线等所有断开场景。</para>
    /// <para>事件参数可能包含断开原因信息。</para>
    /// </remarks>
    event EventHandler<EventArgs> Disconnected;

    /// <summary>数据到达事件</summary>
    /// <remarks>
    /// <para>当有新数据到达时触发，事件参数包含：</para>
    /// <list type="bullet">
    /// <item>Packet - 原始数据包</item>
    /// <item>Message - 经过管道处理器解码后的业务消息对象</item>
    /// </list>
    /// </remarks>
    event EventHandler<ReceivedEventArgs> Received;
    #endregion

    #region 数据发送
    /// <summary>发送数据包</summary>
    /// <remarks>直达网卡，不经过管道处理，适用于已编码的原始数据</remarks>
    /// <param name="data">要发送的数据包</param>
    /// <returns>当前会话实例，支持链式调用</returns>
    INetSession Send(IPacket data);

    /// <summary>发送字节数组</summary>
    /// <remarks>直达网卡，不经过管道处理，适用于已编码的原始数据</remarks>
    /// <param name="data">字节数组</param>
    /// <param name="offset">数据起始偏移量</param>
    /// <param name="count">发送字节数，-1表示发送从偏移量开始的所有数据</param>
    /// <returns>当前会话实例，支持链式调用</returns>
    INetSession Send(Byte[] data, Int32 offset = 0, Int32 count = -1);

    /// <summary>发送只读内存段</summary>
    /// <remarks>直达网卡，高性能API，避免不必要的内存拷贝</remarks>
    /// <param name="data">只读内存段</param>
    /// <returns>当前会话实例，支持链式调用</returns>
    INetSession Send(ReadOnlySpan<Byte> data);

    /// <summary>发送数据流</summary>
    /// <remarks>直达网卡，适用于大数据量流式传输场景</remarks>
    /// <param name="stream">数据流</param>
    /// <returns>当前会话实例，支持链式调用</returns>
    INetSession Send(Stream stream);

    /// <summary>发送字符串</summary>
    /// <remarks>直达网卡，使用指定编码将字符串转换为字节后发送</remarks>
    /// <param name="msg">要发送的字符串</param>
    /// <param name="encoding">字符编码，默认UTF-8</param>
    /// <returns>当前会话实例，支持链式调用</returns>
    INetSession Send(String msg, Encoding? encoding = null);

    /// <summary>通过管道发送消息，不等待响应</summary>
    /// <remarks>
    /// <para>管道内对消息进行报文封装处理，最终得到二进制数据进入网卡。</para>
    /// <para>适用于单向通知、推送等场景。</para>
    /// </remarks>
    /// <param name="message">应用层消息对象</param>
    /// <returns>实际发送的字节数</returns>
    Int32 SendMessage(Object message);

    /// <summary>通过管道发送响应消息</summary>
    /// <remarks>
    /// <para>管道内对消息进行报文封装处理，最终得到二进制数据进入网卡。</para>
    /// <para>与请求关联，用于请求-响应模式中发送响应。</para>
    /// </remarks>
    /// <param name="message">响应消息对象</param>
    /// <param name="eventArgs">接收到请求的事件参数，用于关联请求上下文</param>
    /// <returns>实际发送的字节数</returns>
    Int32 SendReply(Object message, ReceivedEventArgs eventArgs);

    /// <summary>异步发送消息并等待响应</summary>
    /// <remarks>
    /// <para>管道内对消息进行报文封装处理，最终得到二进制数据进入网卡。</para>
    /// <para>适用于请求-响应模式，会阻塞等待对方响应。</para>
    /// </remarks>
    /// <param name="message">请求消息对象</param>
    /// <returns>响应消息对象</returns>
    Task<Object> SendMessageAsync(Object message);

    /// <summary>异步发送消息并等待响应</summary>
    /// <remarks>
    /// <para>管道内对消息进行报文封装处理，最终得到二进制数据进入网卡。</para>
    /// <para>适用于请求-响应模式，支持超时取消。</para>
    /// </remarks>
    /// <param name="message">请求消息对象</param>
    /// <param name="cancellationToken">取消令牌，用于超时控制</param>
    /// <returns>响应消息对象</returns>
    Task<Object> SendMessageAsync(Object message, CancellationToken cancellationToken);
    #endregion
}

/// <summary>网络会话事件参数</summary>
public class NetSessionEventArgs : EventArgs
{
    /// <summary>网络会话实例</summary>
    public INetSession? Session { get; set; }
}