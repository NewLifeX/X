using NewLife.Log;

namespace NewLife.Remoting;

/// <summary>应用接口服务器接口</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/srmp
/// </remarks>
public interface IApiServer
{
    /// <summary>主机</summary>
    IApiHost Host { get; set; }

    /// <summary>当前服务器所有会话</summary>
    IApiSession[] AllSessions { get; }

    /// <summary>初始化</summary>
    /// <param name="config"></param>
    /// <param name="host"></param>
    /// <returns></returns>
    Boolean Init(Object config, IApiHost host);

    /// <summary>开始</summary>
    void Start();

    /// <summary>停止</summary>
    /// <param name="reason">关闭原因。便于日志分析</param>
    void Stop(String reason);

    /// <summary>日志</summary>
    ILog Log { get; set; }
}