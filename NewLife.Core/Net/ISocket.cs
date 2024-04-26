using System.Net.Sockets;
using NewLife.Log;
using NewLife.Model;

namespace NewLife.Net;

/// <summary>基础Socket接口</summary>
/// <remarks>
/// 封装所有基础接口的共有特性！
/// 
/// 核心设计理念：事件驱动，接口统一，简单易用！
/// 异常处理理念：确保主流程简单易用，特殊情况的异常通过事件处理！
/// </remarks>
public interface ISocket : IDisposable2
{
    #region 属性
    /// <summary>名称。主要用于日志输出</summary>
    String Name { get; set; }

    /// <summary>基础Socket对象</summary>
    Socket? Client { get; }

    /// <summary>本地地址</summary>
    NetUri Local { get; set; }

    /// <summary>端口</summary>
    Int32 Port { get; set; }

    /// <summary>消息管道。收发消息都经过管道处理器，进行协议编码解码</summary>
    /// <remarks>
    /// 1，接收数据解码时，从前向后通过管道处理器；
    /// 2，发送数据编码时，从后向前通过管道处理器；
    /// </remarks>
    IPipeline? Pipeline { get; set; }

    /// <summary>日志提供者</summary>
    ILog Log { get; set; }

    /// <summary>是否输出发送日志。默认false</summary>
    Boolean LogSend { get; set; }

    /// <summary>是否输出接收日志。默认false</summary>
    Boolean LogReceive { get; set; }

    /// <summary>APM性能追踪器</summary>
    ITracer? Tracer { get; set; }
    #endregion

    #region 方法
    /// <summary>已重载。日志加上前缀</summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    void WriteLog(String format, params Object?[] args);
    #endregion

    #region 事件
    /// <summary>错误发生/断开连接时</summary>
    event EventHandler<ExceptionEventArgs> Error;
    #endregion
}