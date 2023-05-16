using System.ComponentModel;
using NewLife.Configuration;

namespace NewLife.Net;

/// <summary>网络设置</summary>
[Obsolete("=>SocketSetting")]
public class Setting : SocketSetting { }

/// <summary>网络设置</summary>
[DisplayName("网络设置")]
[Config("Socket")]
public class SocketSetting : Config<SocketSetting>
{
    #region 属性
    /// <summary>网络调试</summary>
    [Description("网络调试")]
    public Boolean Debug { get; set; }

    /// <summary>会话超时时间。默认20*60秒</summary>
    [Description("会话超时时间。默认20*60秒")]
    public Int32 SessionTimeout { get; set; } = 20 * 60;

    /// <summary>缓冲区大小。默认8k</summary>
    [Description("缓冲区大小。默认8k")]
    public Int32 BufferSize { get; set; } = 8 * 1024;

    /// <summary>收发日志数据体长度。默认64</summary>
    [Description("收发日志数据体长度。默认64")]
    public Int32 LogDataLength { get; set; } = 64;

    /// <summary>启用Http压缩。内部新建的HttpClient将自动添加接受压缩的头部，并在响应中对压缩进行解码，默认true</summary>
    [Description("启用Http压缩。内部新建的HttpClient将自动添加接受压缩的头部，并在响应中对压缩进行解码，默认true")]
    public Boolean EnableHttpCompression { get; set; } = true;

    /// <summary>自动启用GZip压缩的请求体大小。默认1024，用0表示不压缩</summary>
    [Description("自动启用GZip压缩的请求体大小。默认1024，用0表示不压缩")]
    public Int32 AutoGZip { get; set; } = 1024;
    #endregion

    #region 方法
    /// <summary>实例化</summary>
    public SocketSetting() { }
    #endregion
}