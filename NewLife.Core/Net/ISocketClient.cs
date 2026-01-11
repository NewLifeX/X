namespace NewLife.Net;

/// <summary>Socket客户端接口</summary>
/// <remarks>
/// <para>具备打开关闭连接能力的Socket客户端。</para>
/// <para>继承自 <see cref="ISocketRemote"/>，具备完整的数据收发功能。</para>
/// <para>支持同步和异步两种方式的连接管理。</para>
/// </remarks>
public interface ISocketClient : ISocketRemote
{
    #region 属性
    /// <summary>超时时间（毫秒）</summary>
    /// <remarks>连接、发送、接收操作的超时时间，默认3000ms</remarks>
    Int32 Timeout { get; set; }

    /// <summary>是否活动</summary>
    /// <remarks>表示当前连接是否处于活动状态</remarks>
    Boolean Active { get; set; }
    #endregion

    #region 开关连接
    /// <summary>打开连接</summary>
    /// <remarks>同步方式打开连接，阻塞直到连接建立或超时</remarks>
    /// <returns>是否成功</returns>
    Boolean Open();

    /// <summary>打开连接</summary>
    /// <remarks>异步方式打开连接，支持取消操作</remarks>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns>是否成功</returns>
    Task<Boolean> OpenAsync(CancellationToken cancellationToken = default);

    /// <summary>关闭连接</summary>
    /// <remarks>同步方式关闭连接</remarks>
    /// <param name="reason">关闭原因，便于日志分析</param>
    /// <returns>是否成功</returns>
    Boolean Close(String reason);

    /// <summary>关闭连接</summary>
    /// <remarks>异步方式关闭连接，支持取消操作</remarks>
    /// <param name="reason">关闭原因，便于日志分析</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns>是否成功</returns>
    Task<Boolean> CloseAsync(String reason, CancellationToken cancellationToken = default);

    /// <summary>打开后触发</summary>
    /// <remarks>连接成功建立后触发，此时可以开始收发数据</remarks>
    event EventHandler Opened;

    /// <summary>关闭后触发</summary>
    /// <remarks>连接关闭后触发，可用于实现掉线重连等功能</remarks>
    event EventHandler Closed;
    #endregion
}