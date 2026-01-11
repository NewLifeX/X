using System.Net.Sockets;
using NewLife.Log;
using NewLife.Model;

namespace NewLife.Net;

/// <summary>基础Socket接口</summary>
/// <remarks>
/// <para>封装所有基础接口的共有特性！</para>
/// <para>核心设计理念：事件驱动，接口统一，简单易用！</para>
/// <para>异常处理理念：确保主流程简单易用，特殊情况的异常通过事件处理！</para>
/// <para>继承自 <see cref="IDisposable2"/>、<see cref="ILogFeature"/>、<see cref="ITracerFeature"/>，具备完整的生命周期管理、日志和追踪能力。</para>
/// </remarks>
public interface ISocket : IDisposable2, ILogFeature, ITracerFeature
{
    #region 属性
    /// <summary>名称</summary>
    /// <remarks>主要用于日志输出和调试识别</remarks>
    String Name { get; set; }

    /// <summary>基础Socket对象</summary>
    /// <remarks>底层的Socket实例，可用于高级操作</remarks>
    Socket? Client { get; }

    /// <summary>本地地址</summary>
    /// <remarks>指定Socket绑定的本地网络地址，包含协议类型、IP地址和端口</remarks>
    NetUri Local { get; set; }

    /// <summary>端口</summary>
    /// <remarks>本地监听或绑定的端口号，0表示由系统自动分配</remarks>
    Int32 Port { get; set; }

    /// <summary>消息管道</summary>
    /// <remarks>
    /// <para>收发消息都经过管道处理器，进行协议编码解码。</para>
    /// <para>处理顺序：</para>
    /// <list type="number">
    /// <item>接收数据解码时，从前向后通过管道处理器</item>
    /// <item>发送数据编码时，从后向前通过管道处理器</item>
    /// </list>
    /// </remarks>
    IPipeline? Pipeline { get; set; }

    /// <summary>是否输出发送日志</summary>
    /// <remarks>默认false，启用后会在日志中输出发送的数据内容</remarks>
    Boolean LogSend { get; set; }

    /// <summary>是否输出接收日志</summary>
    /// <remarks>默认false，启用后会在日志中输出接收的数据内容</remarks>
    Boolean LogReceive { get; set; }
    #endregion

    #region 方法
    /// <summary>输出日志</summary>
    /// <remarks>日志输出时自动加上前缀标识</remarks>
    /// <param name="format">格式化字符串</param>
    /// <param name="args">格式化参数</param>
    void WriteLog(String format, params Object?[] args);
    #endregion

    #region 事件
    /// <summary>错误发生/断开连接时</summary>
    /// <remarks>发生网络错误或连接断开时触发，事件参数包含错误动作和异常信息</remarks>
    event EventHandler<ExceptionEventArgs> Error;
    #endregion
}