
using NewLife.Messaging;
using NewLife.Net;

namespace NewLife.Remoting;

/// <summary>收到Api请求或响应的事件参数</summary>
public class ApiReceivedEventArgs : EventArgs
{
    /// <summary>会话</summary>
    public IApiSession Session { get; set; }

    /// <summary>远程连接</summary>
    public ISocketRemote Remote { get; set; }

    /// <summary>消息</summary>
    public IMessage Message { get; set; }

    /// <summary>请求响应报文</summary>
    public ApiMessage ApiMessage { get; set; }

    /// <summary>用户状态对象</summary>
    public Object UserState { get; set; }
}
