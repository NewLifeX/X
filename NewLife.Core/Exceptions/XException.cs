using System.ComponentModel;

namespace NewLife;

/// <summary>X组件异常</summary>
/// <remarks>
/// NewLife.X 组件库的通用异常类型，支持多种构造方式包括格式化字符串和内部异常包装。
/// 提供了比标准异常更丰富的构造函数，便于快速创建异常实例。
/// </remarks>
[Serializable]
public class XException : Exception
{
    #region 构造函数
    /// <summary>初始化 X 组件异常实例</summary>
    public XException() { }

    /// <summary>使用指定的错误消息初始化 X 组件异常实例</summary>
    /// <param name="message">描述错误的消息</param>
    public XException(String message) : base(message) { }

    /// <summary>使用指定的格式字符串和参数初始化 X 组件异常实例</summary>
    /// <param name="format">复合格式字符串</param>
    /// <param name="args">格式化参数数组</param>
    public XException(String format, params Object?[] args) : base(String.Format(format, args)) { }

    /// <summary>使用指定的错误消息和内部异常初始化 X 组件异常实例</summary>
    /// <param name="message">描述错误的消息</param>
    /// <param name="innerException">导致当前异常的异常</param>
    public XException(String message, Exception innerException) : base(message, innerException) { }

    /// <summary>使用内部异常、格式字符串和参数初始化 X 组件异常实例</summary>
    /// <param name="innerException">导致当前异常的异常</param>
    /// <param name="format">复合格式字符串</param>
    /// <param name="args">格式化参数数组</param>
    public XException(Exception innerException, String format, params Object?[] args) : base(String.Format(format, args), innerException) { }

    /// <summary>使用内部异常初始化 X 组件异常实例，自动使用内部异常的消息</summary>
    /// <param name="innerException">导致当前异常的异常</param>
    public XException(Exception innerException) : base(innerException?.Message, innerException) { }

    ///// <summary>序列化构造函数</summary>
    ///// <param name="info">序列化信息</param>
    ///// <param name="context">序列化上下文</param>
    //protected XException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    #endregion
}

/// <summary>异常事件参数</summary>
/// <remarks>
/// 用于异常处理事件的参数类，继承自 CancelEventArgs 支持取消操作。
/// 包含异常发生时的动作描述和具体异常信息。
/// </remarks>
/// <remarks>使用动作描述和异常实例初始化异常事件参数</remarks>
/// <param name="action">异常发生时的动作描述</param>
/// <param name="ex">发生的异常实例</param>
public class ExceptionEventArgs(String action, Exception ex) : CancelEventArgs
{
    /// <summary>发生异常时进行的动作描述</summary>
    /// <remarks>用于描述异常发生时正在执行的操作或上下文信息</remarks>
    public String Action { get; set; } = action;

    /// <summary>发生的异常实例</summary>
    public Exception Exception { get; set; } = ex;
}

/// <summary>异常助手类</summary>
/// <remarks>提供异常类型判断和处理的扩展方法</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ExceptionHelper
{
    /// <summary>判断异常是否为对象已释放异常</summary>
    /// <param name="ex">要检查的异常实例</param>
    /// <returns>如果是 ObjectDisposedException 则返回 true，否则返回 false</returns>
    /// <remarks>用于快速识别由于访问已释放对象而引发的异常</remarks>
    public static Boolean IsDisposed(this Exception ex) => ex is ObjectDisposedException;

    /// <summary>判断异常是否为网络相关异常</summary>
    /// <param name="ex">要检查的异常实例</param>
    /// <returns>如果是网络相关异常则返回 true，否则返回 false</returns>
    /// <remarks>包括 HttpRequestException、SocketException、TimeoutException 等网络通信异常</remarks>
    public static Boolean IsNetworkException(this Exception ex) => ex switch
    {
        System.Net.Http.HttpRequestException => true,
        System.Net.Sockets.SocketException => true,
        TimeoutException => true,
        OperationCanceledException => true,
        _ => false
    };

    /// <summary>判断异常是否为可忽略的常见异常</summary>
    /// <param name="ex">要检查的异常实例</param>
    /// <returns>如果是可忽略的异常则返回 true，否则返回 false</returns>
    /// <remarks>
    /// 用于判断是否为正常业务流程中可能出现的异常，如对象已释放、操作取消等。
    /// 这类异常通常不需要记录错误日志或进行特殊处理。
    /// </remarks>
    public static Boolean IsIgnorable(this Exception ex) => ex switch
    {
        ObjectDisposedException => true,
        TaskCanceledException => true,
        OperationCanceledException => true,
        _ => false
    };

    /// <summary>获取异常的根本原因</summary>
    /// <param name="ex">要检查的异常实例</param>
    /// <returns>最内层的异常实例</returns>
    /// <remarks>递归获取 InnerException 直到最内层异常，用于获取问题的根本原因</remarks>
    public static Exception GetBaseException(this Exception ex)
    {
        while (ex.InnerException != null)
        {
            ex = ex.InnerException;
        }
        return ex;
    }
}