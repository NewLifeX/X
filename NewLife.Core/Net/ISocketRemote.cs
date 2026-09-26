using System.Text;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Model;

namespace NewLife.Net;

/// <summary>远程通信Socket，仅具有收发功能</summary>
/// <remarks>
/// <para>提供基于网络连接的双向通信能力，专注于数据收发操作。</para>
/// <para>设计理念：轻量化接口，统一TCP/UDP处理模式，支持异步和事件驱动编程。</para>
/// <para>继承自 <see cref="ISocket"/> 和 <see cref="IExtend"/>，具备完整的Socket基础功能和扩展能力。</para>
/// </remarks>
public interface ISocketRemote : ISocket, IExtend
{
    #region 属性
    /// <summary>会话标识符</summary>
    /// <remarks>用于在多会话环境中唯一标识当前连接，便于日志追踪和会话管理</remarks>
    Int32 ID { get; }

    /// <summary>远程终结点地址</summary>
    /// <remarks>
    /// <para>发送操作的目标地址，同时也是接收数据的来源标识。</para>
    /// <para>对于TCP连接，该地址在建立连接后固定；对于UDP，可动态设置不同的目标。</para>
    /// </remarks>
    NetUri Remote { get; set; }

    /// <summary>最后一次通信时间</summary>
    /// <remarks>
    /// <para>记录最近一次成功收发数据的时间戳，用于会话活跃度检测。</para>
    /// <para>包括发送和接收操作，可用于实现超时断开、心跳检测等机制。</para>
    /// </remarks>
    DateTime LastTime { get; }
    #endregion

    #region 数据发送
    /// <summary>发送数据包</summary>
    /// <param name="data">要发送的数据包</param>
    /// <returns>实际发送的字节数，失败时返回负数</returns>
    /// <remarks>目标地址由 <see cref="Remote"/> 属性决定。流式协议（TCP）：返回时整包已提交（直发路径已续发部分发送；已创建发送管道时为入队）；数据报（UDP）：整包原子送出</remarks>
    Int32 Send(IPacket data);

    /// <summary>发送字节数组</summary>
    /// <param name="data">字节数组</param>
    /// <param name="offset">数据起始偏移量</param>
    /// <param name="count">发送字节数，-1表示发送从偏移量开始的所有数据</param>
    /// <returns>实际发送的字节数，失败时返回负数</returns>
    /// <remarks>目标地址由 <see cref="Remote"/> 属性决定。流式协议（TCP）：返回时整包已提交（直发路径已续发部分发送；已创建发送管道时为入队）；数据报（UDP）：整包原子送出</remarks>
    Int32 Send(Byte[] data, Int32 offset = 0, Int32 count = -1);

    /// <summary>发送数组段</summary>
    /// <param name="data">数组段</param>
    /// <returns>实际发送的字节数，失败时返回负数</returns>
    /// <remarks>目标地址由 <see cref="Remote"/> 属性决定。流式协议（TCP）：返回时整包已提交（直发路径已续发部分发送；已创建发送管道时为入队）；数据报（UDP）：整包原子送出</remarks>
    Int32 Send(ArraySegment<Byte> data);

    /// <summary>发送只读内存段</summary>
    /// <param name="data">只读内存段</param>
    /// <returns>实际发送的字节数，失败时返回负数</returns>
    /// <remarks>
    /// <para>目标地址由 <see cref="Remote"/> 属性决定。</para>
    /// <para>流式协议（TCP）：返回时整包已提交（直发路径已续发部分发送；已创建发送管道时为入队）；数据报（UDP）：整包原子送出。</para>
    /// <para>高性能API，避免不必要的内存拷贝，适用于.NET Core/.NET 5+环境。</para>
    /// </remarks>
    Int32 Send(ReadOnlySpan<Byte> data);
    #endregion

    #region 数据接收
    /// <summary>同步接收数据包。拉取模式接口</summary>
    /// <returns>接收到的数据包，无数据时返回null</returns>
    /// <remarks>
    /// <para>该方法会阻塞当前线程直到有数据到达或连接关闭。</para>
    /// <para>事件模式下（接收环运行）将抛出异常；拉取模式请在打开前设置 <see cref="SessionBase.AutoReceive"/> = false。</para>
    /// <para>返回的数据包需要在使用完毕后正确释放，避免内存泄漏。</para>
    /// </remarks>
    IOwnerPacket? Receive();

    /// <summary>异步接收数据包。拉取模式接口，不阻塞调用线程</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>接收到的数据包，无数据时返回null</returns>
    /// <remarks>
    /// <para>推荐的异步拉取方式。事件模式下（接收环运行）将抛出异常；拉取模式请在打开前设置 <see cref="SessionBase.AutoReceive"/> = false。</para>
    /// <para>返回的数据包需要在使用完毕后正确释放，避免内存泄漏。</para>
    /// </remarks>
    Task<IOwnerPacket?> ReceiveAsync(CancellationToken cancellationToken = default);

    /// <summary>数据接收事件</summary>
    /// <remarks>
    /// <para>当有新数据到达时触发，适用于事件驱动编程模式。</para>
    /// <para>事件参数包含原始数据包和经过管道处理后的消息对象。</para>
    /// </remarks>
    event EventHandler<ReceivedEventArgs> Received;
    #endregion

    #region 消息处理
    /// <summary>异步发送消息并等待响应</summary>
    /// <param name="message">要发送的消息对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>响应消息对象</returns>
    /// <remarks>
    /// <para>高级消息通信API，支持请求-响应模式和超时控制。</para>
    /// <para>协议模式下经协议（<see cref="SessionBase.Protocol"/>）构建整帧发送，响应经配对交付。</para>
    /// </remarks>
    ValueTask<Object> SendMessageAsync(Object message, CancellationToken cancellationToken = default);

    /// <summary>发送消息，不等待响应</summary>
    /// <param name="message">要发送的消息对象</param>
    /// <returns>实际发送的字节数，失败时返回负数</returns>
    /// <remarks>
    /// <para>单向消息发送，适用于通知、推送等场景。</para>
    /// <para>协议模式下经协议构建整帧发送。</para>
    /// </remarks>
    Int32 SendMessage(Object message);

    /// <summary>处理接收到的消息数据帧</summary>
    /// <param name="data">消息数据帧</param>
    /// <remarks>
    /// <para>供内部管道处理器调用，用于处理解码后的消息。</para>
    /// <para>通常由框架内部调用，应用代码一般无需直接使用。</para>
    /// </remarks>
    void Process(IData data);
    #endregion
}

/// <summary>Socket远程通信扩展方法</summary>
/// <remarks>
/// <para>提供便捷的数据发送、接收和消息处理功能。</para>
/// <para>包含字符串发送、流传输、文件传输等高级功能。</para>
/// </remarks>
public static class SocketRemoteHelper
{
    #region 扩展发送方法
    /// <summary>发送数据流</summary>
    /// <param name="session">Socket会话</param>
    /// <param name="stream">数据流</param>
    /// <param name="bufferSize">读取块大小。基准实测 64KB 分块吞吐约为 8KB 的 2 倍以上，默认已调优</param>
    /// <returns>实际发送的字节数</returns>
    /// <remarks>
    /// <para>以 64KB 缓冲区分块读取并发送流数据，适用于大文件传输。</para>
    /// <para>发送过程中如果出现错误会立即停止并返回已发送的字节数。</para>
    /// </remarks>
    public static Int32 Send(this ISocketRemote session, Stream stream, Int32 bufferSize = 64 * 1024)
    {
        var totalSent = 0;
        using var buffer = Pool.Rent(bufferSize);

        while (true)
        {
            var bytesRead = stream.Read(buffer, 0, buffer.Length);
            if (bytesRead <= 0) break;

            // 不能把“短读”当作流结束：网络流/压缩流随时可能返回部分数据，提前退出会截断
            // 逐段发送：Send 可能只发出部分数据（TCP 窗口受限），剩余部分必须续发
            var offset = 0;
            while (offset < bytesRead)
            {
                var sent = session.Send(buffer, offset, bytesRead - offset);
                if (sent <= 0) return totalSent + offset;

                offset += sent;
            }

            totalSent += bytesRead;
        }

        return totalSent;
    }

    /// <summary>发送字符串</summary>
    /// <param name="session">Socket会话</param>
    /// <param name="message">要发送的字符串</param>
    /// <param name="encoding">文本编码，null表示UTF-8</param>
    /// <returns>实际发送的字节数</returns>
    public static Int32 Send(this ISocketRemote session, String message, Encoding? encoding = null)
    {
        if (String.IsNullOrEmpty(message))
            return session.Send(Pool.Empty);

        encoding ??= Encoding.UTF8;
        return session.Send(encoding.GetBytes(message));
    }
    #endregion

    #region 扩展接收方法
    /// <summary>接收字符串数据</summary>
    /// <param name="session">Socket会话</param>
    /// <param name="encoding">文本编码，null表示UTF-8</param>
    /// <returns>接收到的字符串，无数据时返回空字符串</returns>
    /// <remarks>该方法会阻塞当前线程直到有数据到达</remarks>
    public static String ReceiveString(this ISocketRemote session, Encoding? encoding = null)
    {
        using var packet = session.Receive();
        if (packet == null || packet.Length == 0) return String.Empty;

        return packet.ToStr(encoding ?? Encoding.UTF8);
    }
    #endregion
}
