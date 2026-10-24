using NewLife.Data;

namespace NewLife.Net;

/// <summary>网络数据处理器。可作为业务处理实现，也可以作为前置协议解析</summary>
/// <remarks>
/// <para>由服务器的 <see cref="NetServer.CreateHandler"/> 为每个会话创建，会话打开后调用 <see cref="Init"/>，此后每次收到数据调用 <see cref="Process"/>。</para>
/// <para><b>数据形态</b>：两种模式下 <c>data</c> 均为 <see cref="ReceivedEventArgs"/>（经由 <see cref="IData"/> 传入）——原始数据在 <see cref="ReceivedEventArgs.Packet"/>。</para>
/// <para><b>协议模式</b>（会话 <see cref="SessionBase.Protocol"/> 非空）：消息对象在 <see cref="ReceivedEventArgs.Message"/>，头部字段立即可用；流式负载时 <c>Packet</c> 为 null，请经消息体读取（数据未到齐会等待，属同连接串行语义）。处理器返回后消息即收尾，需要跨调用留存的数据请先物化。</para>
/// </remarks>
/// <example>
/// 业务处理器的典型实现：协议模式下消息对象随事件参数直达，负载须在处理器返回前读满。
/// <code>
/// class MyHandler : INetHandler
/// {
///     private INetSession _session = null!;
///
///     /// &lt;summary&gt;建立连接时初始化会话&lt;/summary&gt;
///     public void Init(INetSession session) => _session = session;
///
///     /// &lt;summary&gt;处理数据&lt;/summary&gt;
///     public void Process(IData data)
///     {
///         if (data is not ReceivedEventArgs e) return;
///
///         // 协议模式：消息在 e.Message，头部字段已就绪
///         if (e.Message is Message msg)
///         {
///             // 小帧直接取 Payload；大帧为流式体，须在处理器返回前读满（返回后消息即收尾）
///             var body = msg.Payload;
///             if (body == null &amp;&amp; msg.Body != null)
///                 body = msg.Body.ReadAllAsync().AsTask().GetAwaiter().GetResult();
///
///             // 业务处理完毕后应答（消息收尾由框架负责）
///             // _session.SendMessage(response);
///         }
///     }
/// }
/// </code>
/// </example>
public interface INetHandler
{
    /// <summary>建立连接时初始化会话</summary>
    /// <param name="session">会话（底层 Socket 会话，如 <see cref="TcpSession"/>）</param>
    void Init(INetSession session);

    /// <summary>处理客户端发来的数据</summary>
    /// <param name="data">接收数据。实际类型为 <see cref="ReceivedEventArgs"/>：数据在 <see cref="ReceivedEventArgs.Packet"/>，协议模式下消息在 <see cref="ReceivedEventArgs.Message"/></param>
    void Process(IData data);
}
