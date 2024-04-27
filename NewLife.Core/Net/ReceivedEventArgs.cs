using System.Net;
using NewLife.Data;

namespace NewLife.Net;

/// <summary>收到数据时的事件参数</summary>
public class ReceivedEventArgs : EventArgs, IData
{
    #region 属性
    /// <summary>原始数据包</summary>
    /// <remarks>
    /// Packet内部的Data可能是网络缓冲区，并非全部数据都属于当前消息，需要ReadBytes得到有效数据。
    /// </remarks>
    public Packet? Packet { get; set; }

    /// <summary>本地地址</summary>
    public IPAddress? Local { get; set; }

    /// <summary>远程地址</summary>
    public IPEndPoint? Remote { get; set; }

    /// <summary>管道处理器解码后的消息，一般就是业务消息</summary>
    public Object? Message { get; set; }

    /// <summary>用户自定义数据</summary>
    public Object? UserState { get; set; }
    #endregion

    /// <summary>获取当前事件的原始数据。避免用户错误使用Packet.Data</summary>
    /// <returns></returns>
    public Byte[]? GetBytes() => Packet?.ReadBytes();
}