﻿namespace NewLife.Net;

/// <summary>用于与对方进行通讯的Socket会话，仅具有收发功能，也专用于上层应用收发数据</summary>
/// <remarks>
/// Socket会话发送数据不需要指定远程地址，因为内部已经具有。
/// 接收数据时，Tcp接收全部数据，而Udp只接受来自所属远方的数据。
/// 
/// Socket会话不具有连接和断开的能力，所以需要外部连接好之后再创建Socket会话。
/// 但是会话可以销毁，来代替断开。
/// 对于Udp额外创建的会话来说，仅仅销毁会话而已。
/// 
/// 所以，它必须具有收发数据的能力。
/// </remarks>
public interface ISocketSession : ISocketRemote
{
    #region 属性
    /// <summary>Socket服务器。当前通讯所在的Socket服务器，其实是TcpServer/UdpServer</summary>
    ISocketServer Server { get; }
    #endregion
}

/// <summary>会话事件参数</summary>
public class SessionEventArgs : EventArgs
{
    /// <summary>会话</summary>
    public ISocketSession Session { get; set; }
}