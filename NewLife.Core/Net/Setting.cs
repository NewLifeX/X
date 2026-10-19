using System.ComponentModel;
using NewLife.Configuration;

namespace NewLife.Net;

/// <summary>网络设置</summary>
[DisplayName("Network settings")]
[Config("Socket")]
public class SocketSetting : Config<SocketSetting>
{
    #region 属性
    /// <summary>网络调试</summary>
    [Description("Network debugging.")]
    public Boolean Debug { get; set; }

    /// <summary>会话超时时间。每个Tcp/Udp连接会话，超过一定时间不活跃时做超时下线处理，默认20*60秒</summary>
    [Description("Session timeout. A TCP/UDP session is taken offline after being inactive for this period. Default 20*60 seconds.")]
    public Int32 SessionTimeout { get; set; } = 20 * 60;

    /// <summary>缓冲区大小。每个IOCP异步接收缓冲区的大小，较大的值能减少小包合并，但是当连接数很多时会浪费大量内存，默认8k。8KB 恰在 LOH 安全线（85KB）之下，是单次 recv 频率与内存占用的折中（主流框架落点 1KB~16KB）；百万连接场景应调小到 1~4KB（8K × 100 万连接 = 8GB）</summary>
    [Description("Buffer size. IOCP async receive buffer size per socket. Larger values reduce small-packet merges but waste memory with many connections. Default 8k.")]
    public Int32 BufferSize { get; set; } = 8 * 1024;
    /// <summary>内核接收缓冲（SO_RCVBUF）。0 表示不修改，使用系统默认（Windows 约 64KB）</summary>
    /// <remarks>
    /// <para>内核接收窗口是 TCP 大报文吞吐的主要限制：发送侧已按报文大小自动调优发送缓冲，接收侧却始终是系统默认值，
    /// 于是「带宽延迟积」被接收窗口卡住——实测同一服务端把接收缓冲从默认提到 4MB，64KB 报文吞吐 +33%、1MB 报文 +94%。</para>
    /// <para>按连接数计入内核内存（4MB × 1 万连接 = 40GB），只在确实搬运大报文的场景调大；小报文与海量连接场景保持 0。</para>
    /// </remarks>
    [Description("Kernel receive buffer (SO_RCVBUF). 0 means do not modify; use the system default.")]
    public Int32 ReceiveBufferSize { get; set; }

    /// <summary>收发日志数据体长度。应用于LogSend/LogReceive时的数据HEX长度，默认64字节</summary>
    [Description("Send/receive log body length. HEX length of data logged by LogSend/LogReceive. Default 64 bytes.")]
    public Int32 LogDataLength { get; set; } = 64;

    ///// <summary>启用Http压缩。内部新建的HttpClient将自动添加接受压缩的头部，并在响应中对压缩进行解码，默认true</summary>
    //[Description("启用Http压缩。内部新建的HttpClient将自动添加接受压缩的头部，并在响应中对压缩进行解码，默认true")]
    //public Boolean EnableHttpCompression { get; set; } = true;

    /// <summary>自动启用GZip压缩的请求体大小。应用于HttpHelper/ApiHttpClient发起的请求，默认1024，用0表示不压缩</summary>
    [Description("Auto GZip request body size. Applies to requests from HttpHelper/ApiHttpClient. Default 1024; 0 disables compression.")]
    public Int32 AutoGZip { get; set; } = 1024;
    #endregion

    #region 方法
    ///// <summary>实例化</summary>
    //public SocketSetting() { }
    #endregion
}