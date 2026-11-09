namespace NewLife.Http;

/// <summary>Http文件下载选项</summary>
/// <remarks>
/// 用于控制 <see cref="HttpHelper.DownloadFileAsync(HttpClient, String, String, HttpDownloadOptions, CancellationToken)"/>
/// 的分段超时、进度回调、哈希校验与文件名处理行为。
/// </remarks>
public class HttpDownloadOptions
{
    #region 属性
    /// <summary>响应头超时。默认15秒，&lt;=0 表示不限制</summary>
    /// <remarks>
    /// 只约束建立连接后到收到响应头的时间。服务器不可达、网关异常或上游节点尚未就绪时，
    /// 应在较短时间内失败，不应与“已经收到响应头但传输较慢”共用同一个长超时。
    /// </remarks>
    public TimeSpan HeaderTimeout { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>响应体下载超时。默认300秒，&lt;=0 表示不限制</summary>
    /// <remarks>
    /// 从收到响应头开始计时，约束整个响应体传输时间，而不是两次读取之间的空闲时间。
    /// 大文件或弱网场景可适当调大；如需按空闲时间判断，可结合 <see cref="Progress"/> 自行处理。
    /// </remarks>
    public TimeSpan DownloadTimeout { get; set; } = TimeSpan.FromSeconds(300);

    /// <summary>重试次数。默认0，不重试；最多尝试 RetryCount+1 次</summary>
    public Int32 RetryCount { get; set; }

    /// <summary>重试延迟。默认2秒，按尝试次数线性递增</summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>HttpClient工厂。重试时通过工厂创建新客户端，以便强制新建连接；未设置时复用原客户端</summary>
    /// <remarks>工厂应返回新的客户端实例，不能返回调用方传入的客户端；工厂创建的客户端由本方法负责释放，首次尝试始终使用调用方传入的客户端</remarks>
    public Func<HttpClient>? ClientFactory { get; set; }

    /// <summary>下载进度回调。参数为（已接收字节数，总字节数），总字节数未知时为0</summary>
    /// <remarks>发生重试时，进度会从0重新开始</remarks>
    public Action<Int64, Int64>? Progress { get; set; }

    /// <summary>预期哈希字符串。支持带算法前缀（如 md5$...）或按长度自动识别；为空时不校验</summary>
    public String? ExpectedHash { get; set; }

    /// <summary>缓冲区大小。默认0，表示使用内部默认值</summary>
    public Int32 BufferSize { get; set; }

    /// <summary>是否使用响应头 Content-Disposition 中的文件名。默认true</summary>
    /// <remarks>开启后，如果响应头提供了文件名，将覆盖调用方传入的目标文件名，返回最终文件路径</remarks>
    public Boolean UseContentDispositionFileName { get; set; } = true;
    #endregion
}
