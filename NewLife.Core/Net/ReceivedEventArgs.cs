using System.Net;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Model;

namespace NewLife.Net;

/// <summary>收到数据时的事件参数</summary>
/// <remarks><see cref="Packet"/> 为本轮数据的拥有句柄（接收层每轮包装，窗口为本轮数据）：可直接交给应答/发送链路消费（其消费/释放只影响本包装句柄）；需跨线程/跨 await 持有时用 <see cref="IPacket.Slice(Int32, Int32)"/> 切出共享句柄（用后 Dispose）。</remarks>
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

    /// <summary>当前消息的原始数据（本轮拥有句柄）</summary>
    /// <remarks>
    /// <para>接收链路中为本轮数据的拥有句柄（接收层每轮包装，窗口为本轮数据）：进入链路时指向整轮数据，管道拆帧期间指向当前消息帧。</para>
    /// <para><b>带出本轮</b>：需跨线程/跨 await 持有时，在事件内调用 <see cref="IPacket.Slice(Int32, Int32)"/> 切出共享句柄，用后 Dispose；
    /// 可直接交给应答/发送链路消费（其消费/释放只影响本包装句柄）；轮末由接收层按引用计数统一裁决。</para>
    /// <para>视图输入（非接收链路）时为借阅视图，仅在本轮同步链路内有效，跨链需 <see cref="GetBytes"/> 拷贝。</para>
    /// </remarks>
    public IPacket? Packet { get; set; }

    /// <summary>管道处理器解码后的消息，一般就是业务消息</summary>
    public Object? Message { get; set; }

    /// <summary>用户自定义数据</summary>
    public Object? UserState { get; set; }
    #endregion

    /// <summary>获取当前事件的原始数据。避免用户错误使用Packet.Data</summary>
    /// <remarks>小帧跨轮带出的“不后悔”路线：拷贝随大小线性（64B≈1.4ns、4KB≈45ns），换来合身独立缓冲、不吊住整块收包缓冲；大帧（≥4KB）改用 <see cref="IPacket.Slice(Int32, Int32)"/> 共享切片零拷贝（约 42ns 恒定）。</remarks>
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