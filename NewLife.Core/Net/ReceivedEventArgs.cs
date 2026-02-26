using System.Net;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Model;

namespace NewLife.Net;

/// <summary>收到数据时的事件参数</summary>
public class ReceivedEventArgs : EventArgs, IData
{
    #region 池
    private static readonly Pool<ReceivedEventArgs> _pool = new();

    /// <summary>从池中借出事件参数</summary>
    public static ReceivedEventArgs Rent()
    {
        var e = _pool.Get();
        return e;
    }

    /// <summary>归还事件参数到池</summary>
    /// <param name="e">事件参数实例</param>
    public static void Return(ReceivedEventArgs e)
    {
        if (e == null) return;
        e.Reset();
        _pool.Return(e);
    }
    #endregion

    #region 属性
    /// <summary>编码处理器上下文</summary>
    /// <remarks>类似 HttpContext，用于在一次接收处理中携带请求/响应信息。</remarks>
    public IHandlerContext? Context { get; set; }

    /// <summary>本地地址</summary>
    public IPAddress? Local { get; set; }

    /// <summary>远程地址</summary>
    public IPEndPoint? Remote { get; set; }

    /// <summary>原始数据包</summary>
    /// <remarks>
    /// Packet内部直接引用网络缓冲区，以实现零拷贝，并非全部数据都属于当前消息。
    /// 需要注意所有权，当前数据事件结束时回收，不应被外部引用。
    /// </remarks>
    public IPacket? Packet { get; set; }

    /// <summary>管道处理器解码后的消息，一般就是业务消息</summary>
    public Object? Message { get; set; }

    /// <summary>用户自定义数据</summary>
    public Object? UserState { get; set; }
    #endregion

    /// <summary>获取当前事件的原始数据。避免用户错误使用Packet.Data</summary>
    /// <returns></returns>
    public Byte[]? GetBytes() => Packet?.ToArray();

    /// <summary>重置状态，清理所有引用以便对象池复用</summary>
    public void Reset()
    {
        Context = null;
        Local = null;
        Remote = null;
        Packet = null;
        Message = null;
        UserState = null;
    }
}